#!/usr/bin/env python3
"""Découpe les 8 gardiens (statues géantes endormies) dans les maquettes de biome du PDF de la DA (pages 6 à 13).

Dans les maquettes, le gardien est en partie caché (arbres, pièces, lianes, ennemis). Pour chaque biome :
  1. silhouette tracée à la main (ellipses, polygones, traits) sur la maquette ;
  2. palette de la pierre = couleurs dominantes autour du visage + quelques points (mousse, fissure, bec, neige) ;
  3. dans la silhouette, ce qui n'est pas de la pierre (sapins, pièces, couronnes devant lui) prend la couleur voisine ;
  4. sous `cut`, le corps est prolongé jusqu'au sol (en jeu, nos forêts et rochers passent devant) ;
  5. agrandi ×2 : ses pixels ont la taille de ceux du décor (640×360, dessiné en coordonnées 320×180).

Sortie : game/Assets/_Project/Art/Kenta/Gen/biomes/guardian_<id>.png et guardians.json (position, lue par KentaBuilder).
Dépendances : pymupdf, pillow, numpy.   Usage : python3 tools/extract-guardians.py [--debug dossier]
"""
import argparse
import json
import os

import numpy as np
import pymupdf
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PDF = os.path.join(ROOT, "docs", "DA_KENTA_JEUX_BORNE.pdf")
OUT = os.path.join(ROOT, "game", "Assets", "_Project", "Art", "Kenta", "Gen", "biomes")

MOCK_W, MOCK_GROUND = 240, 108     # maquette 240×135, sol vers y = 108
GROUND_Y = 132                     # sol de référence du décor (coordonnées logiques 320×180, voir kenta_art.py)
SCALE = 2                          # 1 px de maquette = 1 px logique du décor = 2 px réels

# Silhouettes tracées sur les maquettes (coordonnées 240×135) : ("e", cx, cy, rx, ry) ellipse, ("p", [(x, y)…]) polygone,
# ("l", x0, y0, x1, y1, épaisseur) trait (bois du cerf). `cut` : au-dessous, le corps est prolongé jusqu'au sol.
# `samples` : points dont la couleur appartient au gardien même hors du visage (mousse, fissure de lave, bec, neige…).
GUARDIANS = {
    "sylve": dict(page=6, face=(161, 62), cut=84, samples=[(150, 44), (171, 44)], shapes=[
        ("e", 161, 62, 33, 21), ("e", 160, 90, 38, 16),
        ("p", [(124, 49), (135, 44), (134, 57)]), ("p", [(198, 47), (187, 44), (189, 57)]),
        # bois : pris à la couleur, depuis leur base, dans la zone au-dessus de la tête (ciel, nuages autour)
        ("f", 146, 40, (118, 2, 205, 46), 18), ("f", 176, 40, (118, 2, 205, 46), 18)]),
    "bayou": dict(page=7, face=(160, 76), cut=92, samples=[(142, 52), (175, 52)], shapes=[
        ("e", 143, 60, 12, 10), ("e", 175, 60, 12, 10), ("e", 160, 77, 31, 19), ("e", 160, 96, 36, 14)]),
    "echos": dict(page=8, face=(155, 66), cut=92, samples=[(138, 57), (137, 61)], shapes=[
        ("e", 155, 64, 29, 20), ("p", [(127, 60), (135, 31), (151, 47)]), ("p", [(160, 47), (182, 29), (187, 60)]),
        ("e", 153, 92, 38, 16)]),
    "forge": dict(page=9, face=(176, 70), pal_w=16, cut=90, samples=[], keep=[(183, 44, 191, 70)], drop=[(159, 76, 167, 90), (187, 69, 196, 83)], shapes=[   # fissure de lave
        ("e", 178, 68, 28, 20), ("p", [(153, 60), (142, 40), (150, 29), (158, 52)]),
        ("p", [(198, 52), (204, 33), (209, 42), (204, 60)]), ("e", 178, 92, 38, 14)]),
    "echine": dict(page=10, face=(164, 66), cut=86, samples=[(152, 45), (175, 47)], shapes=[
        ("e", 165, 64, 30, 19), ("p", [(135, 54), (149, 29), (161, 46)]), ("p", [(169, 46), (182, 29), (195, 54)]),
        ("e", 163, 92, 40, 15)]),
    "givre": dict(page=11, face=(158, 68), cut=94, samples=[(145, 54), (146, 52)], shapes=[
        ("e", 158, 66, 30, 19), ("p", [(131, 54), (140, 24), (154, 45)]), ("p", [(163, 45), (176, 24), (186, 54)]),
        ("e", 158, 92, 40, 15)]),
    "cite": dict(page=12, face=(156, 70), cut=94, samples=[(143, 22), (166, 22), (163, 60), (164, 66)], shapes=[
        ("e", 156, 68, 28, 19), ("e", 143, 40, 7, 20), ("e", 166, 40, 7, 20), ("e", 155, 92, 40, 15)]),
    "orage": dict(page=13, face=(160, 68), cut=92, samples=[(161, 76), (160, 77), (164, 35), (157, 40)], shapes=[
        ("e", 160, 66, 29, 19), ("p", [(149, 50), (152, 29), (157, 48)]), ("p", [(157, 48), (161, 24), (166, 48)]),
        ("p", [(164, 49), (169, 29), (173, 51)]), ("e", 160, 92, 40, 15)]),
}
TOLERANCE = 24


def mockup(doc, page):
    im = doc[page - 1].get_images(full=True)[0]
    pix = pymupdf.Pixmap(doc, im[0])
    if pix.n - pix.alpha > 3:
        pix = pymupdf.Pixmap(pymupdf.csRGB, pix)
    return np.asarray(Image.frombytes("RGB", (pix.width, pix.height), pix.samples)).astype(int)


def palette(a, face, samples, half_w=26, keep=()):
    """Couleurs de la pierre : dominantes autour du visage (yeux et bouche compris) + points échantillons.
    Les zones `keep` (fissure de lave…) n'entrent pas dans la palette : leurs couleurs ressemblent au décor."""
    cx, cy = face
    a = a.copy()
    for x0, y0, x1, y1 in keep:
        a[y0:y1, x0:x1] = -999
    box = a[max(0, cy - 14):cy + 14, max(0, cx - half_w):cx + half_w].reshape(-1, 3)
    box = box[box[:, 0] >= 0]
    cols, counts = np.unique(box, axis=0, return_counts=True)
    keep = [c for c in cols[counts >= max(3, len(box) * 0.01)]]
    for x, y in samples:
        for c in a[y - 1:y + 2, x - 1:x + 2].reshape(-1, 3):
            keep.append(c)
    return np.unique(np.array(keep), axis=0)


def flood(a, seed, box, tol):
    """Pixels reliés au point `seed`, de couleur proche (≤ tol), dans la zone box = (x0, y0, x1, y1)."""
    x0, y0, x1, y1 = box
    ref = a[seed[1], seed[0]]
    out = np.zeros(a.shape[:2], bool)
    stack = [seed]
    while stack:
        x, y = stack.pop()
        if x0 <= x < x1 and y0 <= y < y1 and not out[y, x] and np.abs(a[y, x] - ref).sum() <= tol:
            out[y, x] = True
            stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    return out


def silhouette(a, shape_list, cut, w=240, h=135):
    from PIL import ImageDraw
    m = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(m)
    for sh in shape_list:
        if sh[0] == "e":
            _, cx, cy, rx, ry = sh
            d.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=255)
        elif sh[0] == "p":
            d.polygon(sh[1], fill=255)
        elif sh[0] == "l":
            _, x0, y0, x1, y1, t = sh
            d.line([(x0, y0), (x1, y1)], fill=255, width=t)
    mask = np.asarray(m) > 0
    for sh in shape_list:
        if sh[0] == "f":
            _, x, y, box, tol = sh
            mask |= flood(a, (x, y), box, tol)
    for sh in shape_list:
        if sh[0] == "x":
            _, x0, y0, x1, y1 = sh
            mask[y0:y1, x0:x1] = False
    mask[cut:, :] = False
    return mask


def fill_holes(mask):
    """Trous = zones vides non reliées au bord de l'image."""
    h, w = mask.shape
    outside = np.zeros_like(mask)
    stack = [(y, x) for y in range(h) for x in (0, w - 1)] + [(y, x) for x in range(w) for y in (0, h - 1)]
    while stack:
        y, x = stack.pop()
        if 0 <= y < h and 0 <= x < w and not mask[y, x] and not outside[y, x]:
            outside[y, x] = True
            stack += [(y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)]
    return ~outside


def close(mask, n=1):
    """Fermeture morphologique (bouche les encoches d'1 px laissées par les pièces au bord)."""
    def dil(m):
        o = m.copy()
        o[1:] |= m[:-1]; o[:-1] |= m[1:]; o[:, 1:] |= m[:, :-1]; o[:, :-1] |= m[:, 1:]
        return o

    def ero(m):
        o = m.copy()
        o[1:] &= m[:-1]; o[:-1] &= m[1:]; o[:, 1:] &= m[:, :-1]; o[:, :-1] &= m[:, 1:]
        return o
    m = mask
    for _ in range(n):
        m = dil(m)
    for _ in range(n):
        m = ero(m)
    return m | mask


def recolor(a, good, full, radius=4):
    """Les pixels de `full` hors de `good` (arbre, pièce devant le gardien) prennent la couleur la plus fréquente
    parmi les bons pixels autour d'eux ; on recommence pour ceux qui n'en ont pas encore."""
    out = a.copy()
    have = good.copy()
    h, w = full.shape
    for _ in range(40):
        todo = np.argwhere(full & ~have)
        if len(todo) == 0:
            break
        new = []
        for y, x in todo:
            y0, y1, x0, x1 = max(0, y - radius), min(h, y + radius + 1), max(0, x - radius), min(w, x + radius + 1)
            m = have[y0:y1, x0:x1]
            if not m.any():
                continue
            cols, n = np.unique(out[y0:y1, x0:x1][m], axis=0, return_counts=True)
            new.append((y, x, cols[n.argmax()]))
        if not new:
            break
        for y, x, c in new:
            out[y, x] = c
            have[y, x] = True
    return out


def extrude(img, mask, cut, ground):
    """Prolonge le corps sous `cut` jusqu'au sol, en pierre unie ombrée (reflet à gauche, ombre à droite) : recopier
    la dernière ligne ferait des rayures (mousse, reflets). En jeu, forêts et rochers passent devant."""
    last = cut - 1
    while last > 0 and not mask[last].any():
        last -= 1
    cols = np.nonzero(mask[last])[0]
    rows = img[max(0, last - 4):last + 1][:, cols].reshape(-1, 3)

    def mode(c):
        u, n = np.unique(c, axis=0, return_counts=True)
        return u[n.argmax()]
    base = mode(rows)
    left, right = cols.min(), cols.max()
    w = max(1, right - left)
    lo = mode(img[max(0, last - 4):last + 1, int(left + w * 0.8):right + 1].reshape(-1, 3))
    hi = mode(img[max(0, last - 4):last + 1, left:int(left + w * 0.15) + 1].reshape(-1, 3))
    for y in range(last + 1, ground):
        for x in cols:
            t = (x - left) / w
            img[y, x] = hi if t < 0.1 else (lo if t > 0.78 else base)
            mask[y, x] = True
    return img, mask


def extract(doc, gid, g, debug=None):
    a = mockup(doc, g["page"])
    full = silhouette(a, g["shapes"], g["cut"])
    pal = palette(a, g["face"], g["samples"], g.get("pal_w", 26), g.get("keep", ()))
    dist = np.min(np.abs(a[:, :, None, :] - pal[None, None, :, :]).sum(axis=3), axis=2)
    good = full & (dist <= TOLERANCE)
    for x0, y0, x1, y1 in g.get("keep", []):        # détails gardés tels quels (fissure de lave…)
        good[y0:y1, x0:x1] |= full[y0:y1, x0:x1]
    for x0, y0, x1, y1 in g.get("drop", []):        # pièces dont le contour ressemble à la pierre : recoloriées
        good[y0:y1, x0:x1] = False
    img = recolor(a, good, full)                 # sapins, pièces, couronnes devant lui : recoloriés en pierre
    img, full = extrude(img, full, g["cut"], MOCK_GROUND)
    ys, xs = np.nonzero(full)
    x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    rgba = np.zeros((y1 - y0, x1 - x0, 4), np.uint8)
    rgba[..., :3] = img[y0:y1, x0:x1]
    rgba[..., 3] = full[y0:y1, x0:x1] * 255
    sprite = Image.fromarray(rgba, "RGBA").resize(((x1 - x0) * SCALE, (y1 - y0) * SCALE), Image.NEAREST)
    if debug:
        big = Image.fromarray(a.astype(np.uint8), "RGB").resize((MOCK_W * 3, 135 * 3), Image.NEAREST).convert("RGBA")
        ov = np.zeros((135, MOCK_W, 4), np.uint8)
        ov[full & ~good] = (255, 0, 255, 140)
        ov[good] = (0, 255, 0, 50)
        big.alpha_composite(Image.fromarray(ov, "RGBA").resize(big.size, Image.NEAREST))
        big.save(os.path.join(debug, f"seg_{gid}.png"))
        sprite.save(os.path.join(debug, f"spr_{gid}.png"))
    # centre du sprite dans le décor 640×360 (réel), y vers le haut, depuis le centre de l'écran
    cx_log = (x0 + x1) / 2 * 320 / MOCK_W
    cy_log = GROUND_Y - (MOCK_GROUND - (y0 + y1) / 2)
    return sprite, (round(cx_log * SCALE - 320), round(180 - cy_log * SCALE))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--debug")
    args = ap.parse_args()
    doc = pymupdf.open(PDF)
    os.makedirs(OUT, exist_ok=True)
    items = []
    for gid, g in GUARDIANS.items():
        sprite, (dx, dy) = extract(doc, gid, g, args.debug)
        if sprite.width % 2:
            sprite = sprite.crop((0, 0, sprite.width + 1, sprite.height))
        sprite.save(os.path.join(OUT, f"guardian_{gid}.png"), optimize=True)
        items.append({"id": gid, "x": dx, "y": dy})
        print(f"{gid}: {sprite.size} décalage ({dx}, {dy})")
    with open(os.path.join(OUT, "guardians.json"), "w", encoding="utf-8") as f:
        json.dump({"items": items}, f, indent=1)


if __name__ == "__main__":
    main()
