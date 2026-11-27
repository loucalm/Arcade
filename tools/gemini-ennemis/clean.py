#!/usr/bin/env python3
"""Installe les ennemis et les objets dessinés par Gemini dans le jeu Kenta (pixel art à la taille du jeu).

Entrée : raw/sheets/ennemis_1.jpg (fond magenta), découpée en grille : GRID donne les lignes (y) et, pour chaque
ligne, les colonnes (x) de chaque image et son nom dans le jeu. Tout ce qui est dans une case est gardé (un ennemi
cassé est en plusieurs morceaux), sauf les miettes.

Taille : chaque ennemi garde la taille de son ancien sprite (hauteur de l'image d'attente, ou largeur pour ce qui vole),
toutes ses images à la même échelle. Couleurs ramenées à une palette de 24 couleurs par objet, contour #14101C.
Chaque animation est posée dans une case commune (la plus grande de ses images) : pieds en bas, centrée ; les images
s'échangent sans bouger.

Sortie : out/controle.png ; avec --install :
  Pdf/enemy_<id>.png, _squash, _stretch, _hurt, _broken_0..2 (+ _broken.png = _broken_0)          (8 ennemis)
  Gen/world/bat_0..3 (vol), bat_hang_0..1 (pendue), bat_hurt, bat_broken ;
  Gen/world/stomper_0..1, stomper_stomped_0..1, stomper_recover ; drum_0..2 ; orb_0..3 ; coin_0..4 ; vine_knot ;
  Gen/world/portal_post, portal_cap, portal_veil.
Ensuite, dans Unity : menu Kenta > Installer les sprites des héros et des ennemis.

Usage : python3 tools/gemini-ennemis/clean.py [--install]      (dépendances : pillow, numpy, scipy)
"""
import argparse
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "gemini-heros"))
from blobs import background  # noqa: E402

ART = os.path.normpath(os.path.join(HERE, "..", "..", "game", "Assets", "_Project", "Art", "Kenta"))
SHEET = os.path.join(HERE, "raw", "sheets", "ennemis_1.jpg")
OUT = os.path.join(HERE, "out")
OUTLINE = (0x14, 0x10, 0x1C)

ENEMY_FRAMES = ["", "_squash", "_stretch", "_hurt", "_broken_0", "_broken_1", "_broken_2"]
COLS7 = None  # colonnes mesurées par ligne (voir GRID)

# (y0, y1, [(x0, x1, nom), ...], groupe, taille visée) ; taille : ("h", px) hauteur de la 1re image, ("w", px) largeur
GRID = [
    (6, 105, [(20, 93), (129, 211), (249, 321), (363, 437), (480, 547), (593, 664), (698, 789)], "djembe", ("h", 49)),
    (108, 204, [(22, 92), (133, 207), (250, 320), (364, 436), (476, 552), (594, 669), (594, 669)], "crapaud", ("h", 49)),
    (216, 301, [(12, 102), (119, 222), (239, 331), (357, 442), (468, 558), (585, 672), (696, 796)], "chauve", ("h", 47)),
    (307, 399, [(6, 107), (117, 225), (239, 331), (353, 446), (461, 565), (575, 684), (695, 794)], "crabe", ("h", 50)),
    (401, 495, [(3, 110), (117, 224), (232, 338), (350, 451), (463, 568), (580, 678), (693, 794)], "golem", ("h", 49)),
    (500, 592, [(16, 97), (131, 211), (252, 319), (362, 433), (470, 552), (586, 665), (691, 794)], "morse", ("h", 49)),
    (598, 690, [(5, 111), (122, 222), (230, 341), (345, 455), (463, 569), (575, 682), (694, 789)], "gargouille", ("h", 48)),
    (691, 786, [(17, 95), (131, 210), (245, 324), (360, 438), (473, 550), (589, 667), (698, 790)], "nuage", ("h", 51)),
]
# chauve-souris volante : 4 battements, 2 pendue, (1 pendue fâchée, non utilisée), touchée, cassée
BAT = (787, 854, [(8, 105, "bat_0"), (116, 226, "bat_1"), (237, 334, "bat_2"), (352, 449, "bat_3"),
                  (482, 536, "bat_hang_0"), (575, 630, "bat_hang_1"), (742, 820, "bat_hurt"), (822, 910, "bat_broken")],
       ("w", 56))
STOMPER = (857, 928, [(15, 97, "stomper_0"), (130, 212, "stomper_1"), (245, 328, "stomper_stomped_0"),
                      (361, 439, "stomper_stomped_1"), (465, 548, "stomper_recover")], ("w", 42))
DRUM = (931, 1019, [(6, 93, "drum_0"), (131, 210, "drum_1"), (252, 329, "drum_2")], ("w", 44))
ORB = (931, 1019, [(476, 527, "orb_0"), (558, 626, "orb_1"), (642, 729, "orb_2"), (736, 822, "orb_3")], ("w", 15))
COIN = (1023, 1096, [(14, 79, "coin_0"), (106, 171, "coin_1"), (198, 262, "coin_2"), (295, 346, "coin_3"),
                     (383, 439, "coin_4")], ("w", 19))
KNOT = (1023, 1096, [(463, 540, "vine_knot")], ("w", 14))
PORTAL = (1097, 1184, [(25, 68, "portal_post"), (93, 273, "portal_cap"), (275, 456, "portal_veil")], ("w", 16))


def load():
    a = np.asarray(Image.open(SHEET).convert("RGB")).astype(int)
    R, G, B = a[..., 0], a[..., 1], a[..., 2]
    shadow = (R > 60) & (B > 60) & (G < 0.45 * np.minimum(R, B))      # ombres violettes sous les sauts : pas dessinées
    fg = ndimage.binary_opening(~(background(a) | shadow), iterations=1)
    return a, fg


def cut(a, fg, x0, x1, y0, y1):
    """Tout ce qui est dans la case (morceaux d'un ennemi cassé compris), sans les miettes de moins de 25 px."""
    sub = fg[y0:y1, x0:x1]
    lab, n = ndimage.label(sub)
    sizes = ndimage.sum(sub, lab, range(1, n + 1))
    keep = np.isin(lab, [i + 1 for i, s in enumerate(sizes) if s >= 25])
    rgba = np.zeros(sub.shape + (4,), np.uint8)
    rgba[..., :3] = a[y0:y1, x0:x1]
    rgba[..., 3] = keep * 255
    ys, xs = np.where(keep)
    return Image.fromarray(rgba[ys.min():ys.max() + 1, xs.min():xs.max() + 1])


def shrink(im, s):
    w, h = max(1, round(im.width * s)), max(1, round(im.height * s))
    small = im.convert("RGBa").resize((w, h), Image.BOX).convert("RGBA")
    a = np.asarray(small).copy()
    a[..., 3] = np.where(a[..., 3] >= 128, 255, 0)
    return Image.fromarray(a)


def palette_of(images, colors=24):
    px = np.concatenate([np.asarray(i)[..., :3][np.asarray(i)[..., 3] > 0] for i in images])
    pal = Image.fromarray(px.reshape(1, -1, 3).astype(np.uint8)).quantize(colors=colors, method=Image.MEDIANCUT)

    def q(im):
        a = np.asarray(im)
        rgb = Image.fromarray(a[..., :3]).quantize(palette=pal, dither=Image.NONE).convert("RGB")
        return Image.fromarray(np.dstack([np.asarray(rgb), a[..., 3]]).astype(np.uint8))
    return q


def outline(im):
    a = np.asarray(im).copy()
    op = a[..., 3] > 0
    edge = op & ~ndimage.binary_erosion(op, structure=np.ones((3, 3)), border_value=0)
    a[edge & (a[..., :3].mean(axis=2) >= 70), :3] = OUTLINE
    return Image.fromarray(a)


def group(a, fg, y0, y1, cols, target, cell=True, align="bottom"):
    """Les images d'un groupe à la même échelle, dans une case commune (sauf cell=False : recadrées serrées)."""
    ims = [cut(a, fg, c[0], c[1], y0, y1) for c in cols]
    kind, px = target
    s = px / (ims[0].height if kind == "h" else ims[0].width)
    small = [shrink(i, s) for i in ims]
    q = palette_of(small)
    small = [outline(q(i)) for i in small]
    if not cell:
        return small
    W, H = max(i.width for i in small), max(i.height for i in small)
    out = []
    for i in small:
        c = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        y = H - i.height if align == "bottom" else (H - i.height) // 2
        c.paste(i, ((W - i.width) // 2, y), i)
        out.append(c)
    return out


# Les ennemis, la chauve-souris et le cuirassé un peu plus petits que leurs anciens sprites (retour de Jeremy,
# 2026-11-27). Leur zone de collision ne change pas : elle est fixe dans LevelBuilder (0,8 × 0,9 u), pas tirée du sprite.
ENEMY_SCALE = 0.8


# les chauves-souris (vol, pendues, et celle des Échos au sol) un peu plus grosses que les autres (retour de Jeremy)
BAT_SCALE = 1.15


def smaller(target, scale=None):
    kind, px = target
    return kind, round(px * (ENEMY_SCALE if scale is None else scale))


def build():
    a, fg = load()
    files = {}
    for y0, y1, cols, eid, target in GRID:
        scale = 1.0 if eid == "chauve" else None
        for name, im in zip(ENEMY_FRAMES, group(a, fg, y0, y1, cols, smaller(target, scale))):
            files[f"Pdf/enemy_{eid}{name}.png"] = im
        files[f"Pdf/enemy_{eid}_broken.png"] = files[f"Pdf/enemy_{eid}_broken_0.png"]
    for spec, align, cell in ((BAT, "center", True), (STOMPER, "bottom", True), (DRUM, "bottom", True),
                              (ORB, "center", True), (COIN, "center", True), (KNOT, "center", False),
                              (PORTAL, "center", False)):
        y0, y1, cols, target = spec
        if spec is STOMPER:
            target = smaller(target)
        if spec is BAT:
            target = smaller(target, BAT_SCALE)
        if spec is BAT:                       # vol (case commune), pendue et cassée : leur propre case
            fly = group(a, fg, y0, y1, cols[:4], target, align="center")
            rest = group(a, fg, y0, y1, [cols[0]] + cols[4:], target, cell=False)[1:]
            for (_, _, n), im in zip(cols, fly + rest):
                files[f"Gen/world/{n}.png"] = im
            continue
        if spec is PORTAL:                    # une échelle pour les trois pièces (le pilier fait 16 px de large)
            ims = group(a, fg, y0, y1, cols, target, cell=False)
        else:
            ims = group(a, fg, y0, y1, cols, target, cell=cell, align=align)
        for (_, _, n), im in zip(cols, ims):
            files[f"Gen/world/{n}.png"] = im
    return files


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--install", action="store_true")
    args = ap.parse_args()
    files = build()
    os.makedirs(OUT, exist_ok=True)
    ims = list(files.items())
    W = 720; x = y = rh = 0; pos = []
    for _, im in ims:
        if x + im.width + 4 > W:
            x, y, rh = 0, y + rh + 4, 0
        pos.append((x, y)); x += im.width + 4; rh = max(rh, im.height)
    sheet = Image.new("RGBA", (W, y + rh), (48, 52, 72, 255))
    for (_, im), p in zip(ims, pos):
        sheet.alpha_composite(im, p)
    sheet.resize((sheet.width * 2, sheet.height * 2), Image.NEAREST).save(os.path.join(OUT, "controle.png"))
    print(f"{len(files)} images ; " + ", ".join(f"{os.path.basename(k)} {v.size[0]}×{v.size[1]}" for k, v in files.items()))
    if args.install:
        for rel, im in files.items():
            im.save(os.path.join(ART, rel))
        open(os.path.join(ART, "Pdf", ".gemini"), "w").write(
            "Ennemis dessinés par Gemini (tools/gemini-ennemis/clean.py) : ne pas les écraser avec tools/extract-kenta-pdf.py "
            "ni tools/gen-enemies.py.\n")
        print(f"installé dans {ART}")


if __name__ == "__main__":
    main()
