#!/usr/bin/env python3
"""Extrait les sprites pixel art de docs/DA_KENTA_JEUX_BORNE.pdf vers game/Assets/_Project/Art/Kenta/Pdf.

Le PDF de la DA embarque les vrais sprites (images natives, avec transparence). On les recadre pour que
le point d'ancrage soit toujours « milieu-bas » (pieds du personnage), ce qui évite tout réglage de pivot dans Unity.

Dépendances (en plus de tools/requirements.txt) : pymupdf.   Usage : python3 tools/extract-kenta-pdf.py
Les PNG produits sont commités : inutile de relancer l'outil, sauf si le PDF change.
"""
import os
import sys

import numpy as np
import pymupdf
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PDF = os.path.join(ROOT, "docs", "DA_KENTA_JEUX_BORNE.pdf")
OUT = os.path.join(ROOT, "game", "Assets", "_Project", "Art", "Kenta", "Pdf")

HEROES = {"kenta": 17, "aiko": 18, "ryo": 19}   # pages des planches de poses
IDLE_PAGE = {"kenta": (16, 0), "aiko": (16, 1), "ryo": (16, 2)}
POSES = ["run", "jump", "slide", "hit", "vine", "wall", "hurt", "death"]
ENEMIES = ["djembe", "crapaud", "chauve", "crabe", "golem", "morse", "gargouille", "nuage"]


def page_images(doc, page):
    """Liste des images RGBA d'une page, dans l'ordre du PDF."""
    out = []
    for im in doc[page - 1].get_images(full=True):
        xref, smask = im[0], im[1]
        pix = pymupdf.Pixmap(doc, xref)
        if smask > 0:
            if pix.n > 3:
                pix = pymupdf.Pixmap(pymupdf.csRGB, pix)
            pix = pymupdf.Pixmap(pix, pymupdf.Pixmap(doc, smask))
        mode = "RGBA" if pix.alpha else "RGB"
        out.append(Image.frombytes(mode, (pix.width, pix.height), pix.samples).convert("RGBA"))
    return out


def anchor_bottom_center(img):
    """Recadre pour que les pieds soient en bas (ligne du dernier pixel opaque) et le centre de masse au milieu."""
    a = np.asarray(img)[..., 3] > 0
    ys, xs = np.nonzero(a)
    if len(ys) == 0:
        return img
    bottom, top = ys.max(), ys.min()
    cx = int(round(xs.mean()))
    half = max(cx - xs.min(), xs.max() - cx) + 1
    canvas = Image.new("RGBA", (half * 2, bottom + 1), (0, 0, 0, 0))
    canvas.alpha_composite(img.crop((0, 0, img.width, bottom + 1)), (half - cx, 0))
    return canvas


def save(img, name, anchor=True):
    img = anchor_bottom_center(img) if anchor else img
    img.save(os.path.join(OUT, name + ".png"))
    return img.size


def main():
    os.makedirs(OUT, exist_ok=True)
    doc = pymupdf.open(PDF)
    n = 0
    for hero, page in HEROES.items():
        frames = page_images(doc, page)
        assert len(frames) == 8, (hero, len(frames))
        for pose, im in zip(POSES, frames):
            save(im, f"hero_{hero}_{pose}")
            n += 1
        p, k = IDLE_PAGE[hero]
        save(page_images(doc, p)[k], f"hero_{hero}_idle")
        n += 1
    enemies = page_images(doc, 22)
    assert len(enemies) == 8
    for name, im in zip(ENEMIES, enemies):
        save(im, f"enemy_{name}")
        n += 1
    for name, im in zip(("squash", "stretch", "broken"), page_images(doc, 21)):
        save(im, f"enemy_djembe_{name}")
        n += 1
    print(f"{n} sprites écrits dans {OUT}")


if __name__ == "__main__":
    sys.exit(main())
