#!/usr/bin/env python3
"""Met les obstacles dessinés par Gemini à la taille exacte du jeu Kenta (pixel art propre).

Entrée (dossier raw/), au choix :
  - raw/<sorte>_<biome>.png : un objet par image (ex. raw/wall_sylve.png) ;
  - raw/sheet_<biome>.png   : une planche par biome, 5 objets de gauche à droite : wall, block, ceil, post, platform,
                              séparés par du fond magenta.
Fond : transparent ou magenta uni #FF00FF. N'importe quelle taille.

Sortie : out/<sorte>_<biome>.png à la taille exacte + out/preview.png (planche ×4 pour vérifier).
  wall 38×34 · block 40×40 · ceil 16×16 (répétée en largeur) · post 8×16 (répété en hauteur) · platform 32×10 (en largeur)

Étapes : fond magenta retiré, alpha tranché, couleurs ramenées à la palette du biome (palettes.json), réduction au plus
proche voisin par vote majoritaire, mur/bloc posés en bas et centrés, tuiles raccordées (fenêtre qui se répète le mieux),
contour sombre d'1 px (#14101C) sur tout pixel qui touche le vide.

Usage : python3 clean.py [noms…] [--install]      (dépendances : pillow, numpy)
  --install : copie aussi out/*.png dans game/Assets/_Project/Art/Kenta/Gen/biomes/ du dépôt.
"""
import argparse
import json
import os
import shutil
from collections import Counter

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
RAW, OUT = os.path.join(HERE, "raw"), os.path.join(HERE, "out")
GAME = os.path.normpath(os.path.join(HERE, "..", "..", "game", "Assets", "_Project", "Art", "Kenta", "Gen", "biomes"))
PALETTES = json.load(open(os.path.join(HERE, "palettes.json"), encoding="utf-8"))

OUTLINE = "#14101C"
KINDS = ["wall", "block", "ceil", "post", "platform"]          # ordre des objets dans une planche
SIZES = {  # sorte → (largeur, hauteur, raccord) ; "x" tuile répétée en largeur, "y" en hauteur, None objet posé au sol
    "wall": (38, 34, None),
    "block": (40, 40, None),
    "ceil": (16, 16, "x"),
    "post": (8, 16, "y"),
    "platform": (32, 10, "x"),
}


def rgb(h):
    h = h.lstrip("#")
    return int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)


def palette(bid):
    """Couleurs du biome + une teinte plus sombre (×0,7) et une plus claire (+25 % vers le blanc) de chacune."""
    cols = [OUTLINE]
    for c in PALETTES[bid]["colors"]:
        r, g, b = rgb(c)
        for v in ((r, g, b), (int(r * 0.7), int(g * 0.7), int(b * 0.7)),
                  (int(r + (255 - r) * 0.25), int(g + (255 - g) * 0.25), int(b + (255 - b) * 0.25))):
            h = "#%02X%02X%02X" % v
            if h not in cols:
                cols.append(h)
    return cols


def load(path):
    """Image RGBA avec le fond magenta (et ses franges) rendu transparent."""
    a = np.array(Image.open(path).convert("RGBA")).astype(np.int32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    magenta = (r > 170) & (b > 170) & (g < 110) | ((r - g > 90) & (b - g > 90) & (abs(r - b) < 60))
    a[magenta, 3] = 0
    return a.astype(np.uint8)


def quantize(rgba, pal_hex):
    pal = np.array([rgb(c) for c in pal_hex], dtype=np.float32)
    idx = np.full(rgba.shape[:2], -1, dtype=np.int16)
    solid = rgba[..., 3] >= 128
    if solid.any():
        px = rgba[solid][:, :3].astype(np.float32)[:, None, :]
        rm = (px[..., 0] + pal[None, :, 0]) / 2
        d = px - pal[None]
        dist = (2 + rm / 256) * d[..., 0] ** 2 + 4 * d[..., 1] ** 2 + (2 + (255 - rm) / 256) * d[..., 2] ** 2
        idx[solid] = np.argmin(dist, axis=1)
    return idx


def trim(idx):
    ys, xs = np.nonzero(idx >= 0)
    if len(xs) == 0:
        raise ValueError("image vide (tout est fond ?)")
    return idx[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def split_sheet(idx):
    """Coupe une planche en 5 objets aux 4 plus larges bandes verticales vides."""
    idx = trim(idx)
    empty = ~(idx >= 0).any(axis=0)
    gaps, x = [], 0
    while x < len(empty):
        if empty[x]:
            x0 = x
            while x < len(empty) and empty[x]:
                x += 1
            gaps.append((x - x0, x0, x))
        x += 1
    if len(gaps) < 4:
        raise ValueError(f"planche : {len(gaps) + 1} objets trouvés au lieu de 5 (séparer les objets par du magenta)")
    cuts = sorted(g[1:] for g in sorted(gaps, reverse=True)[:4])
    parts, start = [], 0
    for x0, x1 in cuts:
        parts.append(idx[:, start:x0])
        start = x1
    parts.append(idx[:, start:])
    return [trim(p) for p in parts]


def native_scale(idx):
    """Facteur entier si l'image est un pixel art agrandi proprement (chaque pixel = carré k×k), 1 sinon."""
    h, w = idx.shape
    for k in range(min(h, w) // 4, 1, -1):
        if h % k or w % k:
            continue
        blocks = idx.reshape(h // k, k, w // k, k)
        if (blocks == blocks[:, :1, :, :1]).mean() > 0.97:
            return k
    return 1


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


def seam_window(idx, n, axis):
    """Dans une bande plus longue que la tuile, la fenêtre de longueur n dont la colonne suivante ressemble le plus à
    la première : répétée, elle se raccorde sans couture."""
    a = idx if axis == 1 else idx.T
    if a.shape[1] <= n:
        return idx
    best = min(range(a.shape[1] - n), key=lambda x0: int((a[:, x0] != a[:, x0 + n]).sum()))
    win = a[:, best:best + n]
    return win if axis == 1 else win.T


def fit(idx, w, h, wrap):
    if wrap == "x":
        sh, sw = idx.shape
        nw = max(w, round(sw * h / sh))
        idx = vote_resize(idx, nw, h)
        return seam_window(idx, w, 1)
    if wrap == "y":
        sh, sw = idx.shape
        nh = max(h, round(sh * w / sw))
        idx = vote_resize(idx, w, nh)
        return seam_window(idx, h, 0)
    sh, sw = idx.shape
    s = min(w / sw, h / sh, 1.0)
    nw, nh = max(1, round(sw * s)), max(1, round(sh * s))
    small = vote_resize(idx, nw, nh) if (nw, nh) != (sw, sh) else idx
    out = np.full((h, w), -1, dtype=np.int16)
    x0 = (w - nw) // 2
    out[h - nh:, x0:x0 + nw] = small
    return out


def outline(idx, wrap):
    """Contour d'1 px dans la silhouette (4-voisinage). Sur une tuile, le voisin au-delà du raccord est la tuile suivante."""
    solid = idx >= 0
    p = np.pad(solid, 1)
    if wrap == "x":
        p[1:-1, 0], p[1:-1, -1] = solid[:, -1], solid[:, 0]
    if wrap == "y":
        p[0, 1:-1], p[-1, 1:-1] = solid[-1, :], solid[0, :]
    edge = solid & ~(p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:])
    out = idx.copy()
    out[edge] = 0                                   # palette[0] = contour
    return out


def finish(idx, kind, bid):
    w, h, wrap = SIZES[kind]
    k = native_scale(idx)
    if k > 1:
        idx = idx[::k, ::k]
    idx = outline(fit(idx, w, h, wrap), wrap)
    pal = np.array([rgb(c) + (255,) for c in palette(bid)], dtype=np.uint8)
    rgba = np.zeros((h, w, 4), dtype=np.uint8)
    m = idx >= 0
    rgba[m] = pal[idx[m]]
    im = Image.fromarray(rgba, "RGBA")
    assert im.size == (w, h)
    if wrap is None:
        assert rgba[-1, :, 3].any(), f"{kind}_{bid} : le pied ne touche pas le bas"
    return im


def jobs(names):
    """(nom de sortie, tableau d'indices) pour chaque objet trouvé dans raw/."""
    files = sorted(f[:-4] for f in os.listdir(RAW) if f.endswith(".png"))
    for f in files:
        if f.startswith("sheet_"):
            bid = f[6:]
            idx = quantize(load(os.path.join(RAW, f + ".png")), palette(bid))
            for kind, part in zip(KINDS, split_sheet(idx)):
                if not names or f"{kind}_{bid}" in names:
                    yield kind, bid, part
        else:
            kind, bid = f.split("_", 1)
            if kind in SIZES and (not names or f in names):
                yield kind, bid, trim(quantize(load(os.path.join(RAW, f + ".png")), palette(bid)))


def preview(done):
    z, pad = 4, 12
    rows = {}
    for (kind, bid), im in sorted(done.items(), key=lambda t: (list(PALETTES).index(t[0][1]), KINDS.index(t[0][0]))):
        reps = {"ceil": (3, 1), "platform": (2, 1), "post": (1, 2)}.get(kind, (1, 1))
        t = Image.new("RGBA", (im.width * reps[0], im.height * reps[1]))
        for a in range(reps[0]):
            for b in range(reps[1]):
                t.alpha_composite(im, (a * im.width, b * im.height))
        rows.setdefault(bid, []).append(t.resize((t.width * z, t.height * z), Image.NEAREST))
    W = max(sum(i.width + pad for i in r) for r in rows.values()) + pad
    H = sum(max(i.height for i in r) + pad for r in rows.values()) + pad
    sh = Image.new("RGBA", (W, H), (90, 90, 110, 255))
    y = pad
    for r in rows.values():
        x = pad
        for im in r:
            sh.alpha_composite(im, (x, y))
            x += im.width + pad
        y += max(i.height for i in r) + pad
    sh.save(os.path.join(OUT, "preview.png"))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("names", nargs="*")
    ap.add_argument("--install", action="store_true")
    a = ap.parse_args()
    os.makedirs(OUT, exist_ok=True)
    done = {}
    for kind, bid, idx in jobs(set(a.names)):
        im = finish(idx, kind, bid)
        name = f"{kind}_{bid}"
        im.save(os.path.join(OUT, name + ".png"), optimize=True)
        if a.install:
            shutil.copyfile(os.path.join(OUT, name + ".png"), os.path.join(GAME, name + ".png"))
        done[(kind, bid)] = im
        print("ok", name, im.size)
    if done:
        preview(done)
        missing = [f"{k}_{b}" for b in PALETTES for k in KINDS if (k, b) not in done]
        print(f"{len(done)} sprites ; aperçu : out/preview.png" + (f" ; manquants : {len(missing)}" if missing else ""))
    else:
        print("rien dans raw/")


if __name__ == "__main__":
    main()
