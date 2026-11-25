#!/usr/bin/env python3
"""Met les gardiens (statues géantes, statiques, derrière les montagnes) dessinés par Gemini au format du jeu Kenta.

Entrée : raw/sheets/<n>_<biome>.png|jpg (une statue par image) ou raw/guardian_<biome>.png. Fond transparent ou magenta
uni #FF00FF, n'importe quelle taille. Les petites taches (étiquettes de texte, poussières) sont ignorées.

Sortie (out/) : guardian_<biome>.png, guardians.json (position) et preview.png (sur le ciel du biome, à côté du gardien
de la maquette).
  - même hauteur que le gardien actuel (palettes.json → size_now), ou --height N ;
  - bruit JPG effacé (filtre médian), couleurs ramenées à la palette du biome (corps, trait, accent, yeux, ciel et
    leurs teintes), pixels isolés retirés, alpha tranché ;
  - pas de contour noir imposé (--edge : bord d'1 px dans la teinte « line ») ;
  - position : même centre horizontal et même pied que le gardien actuel (guardians.json, px réels de la vue 640×360,
    y vers le haut, centre de l'image par rapport au centre de l'écran).

Usage : python3 clean.py [biomes…] [--height N] [--edge] [--install]      (dépendances : pillow, numpy, scipy)
  --install : copie les PNG dans game/Assets/_Project/Art/Kenta/Gen/biomes/ et y met à jour guardians.json.
"""
import argparse
import glob
import json
import os
import shutil
from collections import Counter

import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
RAW, OUT = os.path.join(HERE, "raw"), os.path.join(HERE, "out")
GAME = os.path.normpath(os.path.join(HERE, "..", "..", "game", "Assets", "_Project", "Art", "Kenta", "Gen", "biomes"))
PAL = json.load(open(os.path.join(HERE, "palettes.json"), encoding="utf-8"))
IDS = list(PAL)


def rgb(h):
    h = h.lstrip("#")
    return int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)


def palette(bid):
    """Corps, trait, accent (mousse, lave, cristal…), yeux et ciel, chacun en 5 teintes (ombre à lumière)."""
    p = PAL[bid]
    cols = []
    for c in (p["body"], p["line"], p["accent"], p["eye"], p["sky"]):
        r, g, b = rgb(c)
        for f in (-0.45, -0.25, 0.0, 0.2, 0.4):
            if f < 0:
                v = (int(r * (1 + f)), int(g * (1 + f)), int(b * (1 + f)))
            else:
                v = (int(r + (255 - r) * f), int(g + (255 - g) * f), int(b + (255 - b) * f))
            h = "#%02X%02X%02X" % v
            if h not in cols:
                cols.append(h)
    return cols


def load(path):
    """RGBA, fond magenta (et ses franges) rendu transparent, alpha tranché."""
    a = np.array(Image.open(path).convert("RGBA")).astype(np.int32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    magenta = (r > 170) & (b > 170) & (g < 110) | ((r - g > 90) & (b - g > 90) & (abs(r - b) < 60))
    a[magenta, 3] = 0
    a[..., 3] = np.where(a[..., 3] >= 128, 255, 0)
    return a


def keep_big(mask, frac=0.02):
    """Garde les taches d'au moins `frac` de la plus grande (retire étiquettes et poussières)."""
    lab, n = ndimage.label(mask, structure=np.ones((3, 3)))
    if n == 0:
        return mask
    areas = ndimage.sum(mask, lab, range(1, n + 1))
    keep = [i + 1 for i, s in enumerate(areas) if s >= frac * areas.max()]
    return np.isin(lab, keep)


def crop_to(a, mask):
    ys, xs = np.nonzero(mask)
    sub = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy()
    sub[..., 3] = np.where(mask[ys.min():ys.max() + 1, xs.min():xs.max() + 1], 255, 0)
    return sub


def denoise(rgba, size=5):
    """Filtre médian sur les couleurs (pas sur l'alpha) : efface le bruit JPG avant de choisir les couleurs de palette."""
    out = rgba.copy()
    for c in range(3):
        out[..., c] = ndimage.median_filter(rgba[..., c], size=size)
    return out


def despeckle(idx):
    """Un pixel entouré (4-voisinage) d'au moins 3 voisins d'une même autre couleur prend cette couleur."""
    out = idx.copy()
    h, w = idx.shape
    for y in range(1, h - 1):
        for x in range(1, w - 1):
            v = idx[y, x]
            if v < 0:
                continue
            n = [idx[y - 1, x], idx[y + 1, x], idx[y, x - 1], idx[y, x + 1]]
            c, k = Counter(n).most_common(1)[0]
            if k >= 3 and c >= 0 and c != v:
                out[y, x] = c
    return out


def quantize(rgba, pal_hex):
    pal = np.array([rgb(c) for c in pal_hex], dtype=np.float32)
    idx = np.full(rgba.shape[:2], -1, dtype=np.int16)
    solid = rgba[..., 3] > 0
    px = rgba[solid][:, :3].astype(np.float32)
    out = np.empty(len(px), dtype=np.int16)
    for s in range(0, len(px), 200000):                       # par paquets : images Gemini de plusieurs mégapixels
        q = px[s:s + 200000][:, None, :]
        rm = (q[..., 0] + pal[None, :, 0]) / 2
        d = q - pal[None]
        dist = (2 + rm / 256) * d[..., 0] ** 2 + 4 * d[..., 1] ** 2 + (2 + (255 - rm) / 256) * d[..., 2] ** 2
        out[s:s + 200000] = np.argmin(dist, axis=1)
    idx[solid] = out
    return idx


def vote_resize(idx, w, h):
    """Réduction au plus proche voisin par vote : chaque pixel prend la couleur la plus fréquente de son bloc."""
    sh, sw = idx.shape
    out = np.full((h, w), -1, dtype=np.int16)
    for y in range(h):
        y0 = int(y * sh / h)
        y1 = max(y0 + 1, int((y + 1) * sh / h))
        for x in range(w):
            x0 = int(x * sw / w)
            x1 = max(x0 + 1, int((x + 1) * sw / w))
            cell = idx[y0:y1, x0:x1].ravel().tolist()
            c = Counter(cell)
            if (len(cell) - c[-1]) * 2 < len(cell):
                continue
            del c[-1]
            out[y, x] = c.most_common(1)[0][0]
    return out


def edge(idx, line_index):
    solid = idx >= 0
    p = np.pad(solid, 1)
    e = solid & ~(p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:])
    out = idx.copy()
    out[e] = line_index
    return out


def to_image(idx, pal_hex):
    pal = np.array([rgb(c) + (255,) for c in pal_hex], dtype=np.uint8)
    rgba = np.zeros(idx.shape + (4,), dtype=np.uint8)
    m = idx >= 0
    rgba[m] = pal[idx[m]]
    return Image.fromarray(rgba, "RGBA")


def source(bid):
    """La statue en RGBA rognée (la plus grande tache et ce qui est assez gros autour), ou None."""
    paths = sorted(glob.glob(os.path.join(RAW, "sheets", f"*_{bid}.*"))) or sorted(glob.glob(os.path.join(RAW, f"guardian_{bid}.*")))
    if not paths:
        return None
    a = load(paths[-1])
    return crop_to(a, keep_big(a[..., 3] > 0))


def process(bid, height=None, add_edge=False):
    src = source(bid)
    if src is None:
        return None
    pal = palette(bid)
    idx = quantize(denoise(src), pal)
    H = (height or PAL[bid]["size_now"][1]) - 2                # 1 px de marge en haut et en bas
    s = H / idx.shape[0]
    sl = despeckle(vote_resize(idx, max(1, round(idx.shape[1] * s)), H))
    # 1 px de marge, dimensions paires : le centre (pivot) tombe sur un pixel entier
    W, Hc = sl.shape[1] + 2, sl.shape[0] + 2
    W += W % 2
    Hc += Hc % 2
    a = np.full((Hc, W), -1, dtype=np.int16)
    a[1:1 + sl.shape[0], 1:1 + sl.shape[1]] = sl
    if add_edge:
        a = edge(a, pal.index("#%02X%02X%02X" % rgb(PAL[bid]["line"])))
    cx, cy = PAL[bid]["center_now"]
    foot = cy - PAL[bid]["size_now"][1] / 2                    # même pied que le gardien actuel (y vers le haut)
    return to_image(a, pal), {"id": bid, "x": cx, "y": foot + Hc / 2}


def preview(results):
    """Chaque gardien sur le ciel de son biome, à côté du gardien de la maquette (refs/gardiens/)."""
    tiles = []
    for bid, (im, _) in results.items():
        n = IDS.index(bid) + 1
        mock = Image.open(os.path.join(HERE, "refs", "gardiens", f"gardien_{n}_{bid}.png")).convert("RGBA")
        mock = mock.resize((mock.width * im.height // mock.height, im.height), Image.NEAREST)
        t = Image.new("RGBA", (im.width + mock.width + 30, im.height + 20), rgb(PAL[bid]["sky"]) + (255,))
        t.alpha_composite(im, (10, 10))
        t.alpha_composite(mock, (im.width + 20, 10))
        tiles.append(t)
    cw = max(t.width for t in tiles) + 10
    rows = [tiles[i:i + 4] for i in range(0, len(tiles), 4)]
    H = sum(max(t.height for t in r) + 10 for r in rows) + 10
    sh = Image.new("RGBA", (cw * min(4, len(tiles)) + 10, H), (40, 40, 40, 255))
    y = 10
    for r in rows:
        for k, t in enumerate(r):
            sh.alpha_composite(t, (10 + k * cw, y))
        y += max(t.height for t in r) + 10
    sh.save(os.path.join(OUT, "preview.png"))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("biomes", nargs="*")
    ap.add_argument("--height", type=int, default=None)
    ap.add_argument("--edge", action="store_true")
    ap.add_argument("--install", action="store_true")
    a = ap.parse_args()
    os.makedirs(OUT, exist_ok=True)
    results = {}
    for bid in a.biomes or IDS:
        r = process(bid, a.height, a.edge)
        if r is None:
            continue
        r[0].save(os.path.join(OUT, f"guardian_{bid}.png"), optimize=True)
        results[bid] = r
        print(f"ok {bid} {r[0].size}")
    if not results:
        print("rien dans raw/")
        return
    preview(results)
    # guardians.json : positions des gardiens traités, celles des autres gardées
    gpath = os.path.join(GAME, "guardians.json")
    items = {it["id"]: it for it in json.load(open(gpath, encoding="utf-8"))["items"]}
    for bid, (_, pos) in results.items():
        items[bid] = pos
    data = {"items": [items[i] for i in IDS if i in items]}
    json.dump(data, open(os.path.join(OUT, "guardians.json"), "w", encoding="utf-8"), indent=1)
    if a.install:
        for bid in results:
            shutil.copyfile(os.path.join(OUT, f"guardian_{bid}.png"), os.path.join(GAME, f"guardian_{bid}.png"))
        shutil.copyfile(os.path.join(OUT, "guardians.json"), gpath)
    missing = [i for i in IDS if i not in results]
    print(f"{len(results)} gardiens ; aperçu : out/preview.png" + (f" ; manquants : {', '.join(missing)}" if missing else ""))


if __name__ == "__main__":
    main()
