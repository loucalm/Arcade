#!/usr/bin/env python3
"""Course à 6 images pour les héros Kenta, à partir du sprite « course » du PDF de la DA.

Le PDF ne fournit qu'une image par pose. On garde le haut du corps (et les traits de vitesse),
on efface les jambes et on les redessine à 6 phases d'un cycle de course, avec les couleurs
du pantalon et des chaussures prélevées sur le sprite (aucune couleur nouvelle).

    python3 tools/gen-hero-run.py            → game/Assets/_Project/Art/Kenta/Gen/heroes/hero_<id>_run_<0..5>.png
"""
import math
import os
from collections import Counter

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PDF = os.path.join(ROOT, "game", "Assets", "_Project", "Art", "Kenta", "Pdf")
OUT = os.path.join(ROOT, "game", "Assets", "_Project", "Art", "Kenta", "Gen", "heroes")
HEROES = ("kenta", "aiko", "ryo")
FRAMES = 6
LEG_LEFT = 18          # à gauche : traits de vitesse, jamais touchés


def _ink(a):
    """Couleur du contour : la plus fréquente des couleurs très sombres."""
    dark = [tuple(p[:3]) for p in a.reshape(-1, 4) if p[3] >= 128 and int(p[0]) + int(p[1]) + int(p[2]) < 120]
    return Counter(dark).most_common(1)[0][0]


def _most_common(a, y0, y1, x0, x1, exclude):
    c = Counter()
    for y in range(max(0, y0), min(a.shape[0], y1)):
        for x in range(max(0, x0), min(a.shape[1], x1)):
            p = a[y, x]
            if p[3] >= 128 and tuple(p[:3]) not in exclude:
                c[tuple(p[:3])] += 1
    return c.most_common(1)[0][0] if c else None


def _thick_line(layer, x0, y0, x1, y1, color, width=3):
    n = max(abs(x1 - x0), abs(y1 - y0), 1)
    r = width // 2
    for i in range(n + 1):
        x = round(x0 + (x1 - x0) * i / n)
        y = round(y0 + (y1 - y0) * i / n)
        for dy in range(-r, width - r):
            for dx in range(-r, width - r):
                if 0 <= y + dy < layer.shape[0] and 0 <= x + dx < layer.shape[1]:
                    layer[y + dy, x + dx] = (*color, 255)


def _outline(layer, ink):
    """Ajoute un contour d'1 px (extérieur) autour des pixels opaques d'un calque."""
    h, w = layer.shape[:2]
    solid = layer[:, :, 3] >= 128
    out = layer.copy()
    for y in range(h):
        for x in range(w):
            if solid[y, x]:
                continue
            if any(0 <= y + dy < h and 0 <= x + dx < w and solid[y + dy, x + dx]
                   for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                out[y, x] = (*ink, 255)
    return out


def _over(dst, src):
    m = src[:, :, 3] >= 128
    dst[m] = src[m]


def run_cycle(src):
    """6 images de course (même taille que `src`, pieds sur la dernière ligne)."""
    a = np.array(src.convert("RGBA"))
    h, w = a.shape[:2]
    ink = _ink(a)
    hip_y = h - 11                                   # haut des cuisses (même gabarit pour les 3 héros du PDF)
    torso = [x for x in range(LEG_LEFT, w) if a[h - 13, x, 3] >= 128]
    xc = (min(torso) + max(torso)) // 2 if torso else w // 2

    skin = {ink}
    front_pants = _most_common(a, h - 10, h - 4, xc, w, skin)
    front_shoe = _most_common(a, h - 4, h - 1, xc, w, skin | {front_pants})
    back_pants = _most_common(a, h - 11, h - 5, LEG_LEFT, xc - 2, skin | {front_pants, front_shoe}) or front_pants
    back_shoe = _most_common(a, h - 14, h - 9, LEG_LEFT, xc - 4, skin | {front_pants, front_shoe, back_pants}) or front_shoe

    # haut du corps : on efface les jambes (rangées basses), y compris la jambe arrière relevée
    body = a.copy()
    body[hip_y:, LEG_LEFT:] = 0
    for y in range(h - 14, hip_y):
        for x in range(LEG_LEFT, xc - 3):
            if tuple(body[y, x, :3]) in (back_pants, back_shoe, front_pants):
                body[y, x] = 0
    # referme le contour sous le torse
    for y in range(h - 15, h):
        for x in range(LEG_LEFT, w):
            if body[y, x, 3] < 128 and y > 0 and body[y - 1, x, 3] >= 128 and tuple(body[y - 1, x, :3]) != ink:
                body[y, x] = (*ink, 255)

    frames = []
    foot_ground = h - 2                              # bas de la chaussure ; le contour est sur la dernière ligne
    for k in range(FRAMES):
        phi = 2 * math.pi * k / FRAMES
        img = np.zeros_like(a)
        for leg, (hip_x, pants, shoe, phase) in enumerate(((xc - 2, back_pants, back_shoe, phi + math.pi),
                                                            (xc + 1, front_pants, front_shoe, phi))):
            stride = math.cos(phase)
            lift = max(0.0, -math.sin(phase))
            fx = round(hip_x + 6 * stride)
            fy = round(foot_ground - 4 * lift)
            kx = round((hip_x + fx) / 2 + (2 if lift > 0 else 1))
            ky = round((hip_y + fy) / 2)
            layer = np.zeros_like(a)
            _thick_line(layer, hip_x, hip_y, kx, ky, pants)
            _thick_line(layer, kx, ky, fx, fy - 1, pants)
            for yy in (fy - 1, fy):
                for xx in range(fx - 1, fx + 4):
                    if 0 <= xx < w and 0 <= yy < h:
                        layer[yy, xx] = (*shoe, 255)
            _over(img, _outline(layer, ink))
        bob = 1 if k in (1, 4) else 0                # le corps descend d'1 px au passage du pied sous le bassin
        shifted = np.zeros_like(body)
        shifted[bob:] = body[:h - bob]
        _over(img, shifted)
        frames.append(Image.fromarray(img, "RGBA"))
    return frames


def main():
    os.makedirs(OUT, exist_ok=True)
    for h in HEROES:
        src = Image.open(os.path.join(PDF, f"hero_{h}_run.png")).convert("RGBA")
        for k, f in enumerate(run_cycle(src)):
            f.save(os.path.join(OUT, f"hero_{h}_run_{k}.png"))
    print(f"ok : {len(HEROES) * FRAMES} images dans {os.path.relpath(OUT, ROOT)}")


if __name__ == "__main__":
    main()
