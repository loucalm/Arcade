#!/usr/bin/env python3
"""Découpe les planches Gemini (raw/sheets/<n>_<biome>.jpg|png) en un objet par image : raw/<sorte>_<biome>.png.

Les 5 plus grandes taches (hors fond magenta) sont les 5 objets, pris de gauche à droite : wall, block, ceil, post,
platform. Ainsi les étiquettes de texte, les reflets et les halos détachés sont ignorés. Chaque objet est découpé selon
son propre masque. FIXES corrige ce que Gemini a ajouté autour de certains objets (fond rectangulaire, socle, halo).
Ensuite : python3 clean.py

Usage : python3 cut_sheets.py      (dépendances : pillow, numpy, scipy)
"""
import glob
import os

import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
KINDS = ["wall", "block", "ceil", "post", "platform"]

# (biome, sorte) → corrections, en pixels de la planche :
#   "max_y" : coupe tout ce qui est sous cette ligne (socle ou halo sous l'objet) ;
#   "flood" : (r, g, b, tolérance) fond uni à retirer par remplissage depuis le bord de l'objet ;
#   "key"   : fonction (r, g, b) → masque des pixels à rendre transparents (halo coloré).
FIXES = {
    ("orage", "wall"): dict(max_y=290),                              # nuage posé sur un socle plat : on garde le haut
    ("orage", "block"): dict(flood=(78, 88, 155, 16)),               # fond rectangulaire bleu
    ("orage", "post"): dict(flood=(56, 58, 118, 22)),                # fond rectangulaire bleu nuit
    ("echos", "wall"): dict(max_y=340),                              # socle de lumière violette sous les cristaux
    ("echos", "block"): dict(max_y=372, key=lambda r, g, b: (g < 45) & (r > 90) & (b > 140)),   # halo sous la géode
}


def load(path):
    a = np.array(Image.open(path).convert("RGBA")).astype(np.int32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    magenta = (r > 170) & (b > 170) & (g < 110) | ((r - g > 90) & (b - g > 90) & (abs(r - b) < 60))
    a[magenta, 3] = 0
    return a


def flood_remove(a, mask, rgb, tol):
    """Retire le fond uni d'un objet : pixels proches de `rgb` reliés au bord de la boîte de l'objet."""
    near = mask & (np.abs(a[..., :3] - np.array(rgb)).max(axis=-1) <= tol)
    near = (ndimage.binary_closing(near, structure=np.ones((3, 3)), iterations=2) | near) & mask   # bruit JPG
    lab, _ = ndimage.label(near, structure=np.ones((3, 3)))
    e = 3                                             # bande du bord (franges du fond magenta)
    border = set(np.unique(np.concatenate([lab[:e].ravel(), lab[-e:].ravel(), lab[:, :e].ravel(), lab[:, -e:].ravel()]))) - {0}
    keep = mask & ~np.isin(lab, list(border))
    keep[:e] = keep[-e:] = False                      # liseré du fond (frange magenta) au bord de la boîte
    keep[:, :e] = keep[:, -e:] = False
    # miettes laissées par le bruit : on garde les taches d'au moins 300 px (l'éclair sous le nuage en est une)
    lab, n = ndimage.label(keep, structure=np.ones((3, 3)))
    areas = ndimage.sum(keep, lab, range(1, n + 1))
    return np.isin(lab, [i + 1 for i, s in enumerate(areas) if s >= 300])


def cut(path):
    bid = os.path.basename(path).split(".")[0].split("_", 1)[1]
    a = load(path)
    solid = a[..., 3] >= 128
    lab, n = ndimage.label(solid, structure=np.ones((3, 3)))
    areas = ndimage.sum(solid, lab, range(1, n + 1))
    boxes = ndimage.find_objects(lab)
    top = sorted((int(i) + 1 for i in np.argsort(areas)[-5:]), key=lambda i: boxes[i - 1][1].start)
    for kind, i in zip(KINDS, top):
        ys, xs = boxes[i - 1]
        sub = a[ys, xs].copy()
        mask = lab[ys, xs] == i
        fx = FIXES.get((bid, kind), {})
        if "max_y" in fx:
            mask[max(0, fx["max_y"] - ys.start):] = False
        if "key" in fx:
            mask &= ~fx["key"](sub[..., 0], sub[..., 1], sub[..., 2])
        if "flood" in fx:
            mask = flood_remove(sub, mask, fx["flood"][:3], fx["flood"][3])
        sub[..., 3] = np.where(mask, 255, 0)
        out = Image.fromarray(sub.astype(np.uint8), "RGBA")
        out = out.crop(out.getbbox())
        out.save(os.path.join(HERE, "raw", f"{kind}_{bid}.png"))
        print("ok", f"{kind}_{bid}", out.size)


def main():
    for p in sorted(glob.glob(os.path.join(HERE, "raw", "sheets", "*.*"))):
        cut(p)


if __name__ == "__main__":
    main()
