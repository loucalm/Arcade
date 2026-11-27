#!/usr/bin/env python3
"""Dessine le cuirassé (ennemi sur lequel on saute) en pixel art : deux images, au repos et tremplin écrasé (sur le temps).

Il doit se distinguer au premier coup d'œil du champignon qu'on frappe : une carapace de métal bleu acier (on ne la
frappe pas), une visière sombre aux yeux jaunes, et surtout un **tremplin jaune à ressorts** sur le dessus (on saute
dessus). Commun à tous les biomes, comme la chauve-souris. Sortie : game/Assets/_Project/Art/Kenta/Gen/world/stomper_{0,1}.png
(pivot aux pieds, voir KentaSpritePostprocessor). Usage : python3 tools/gen-stomper.py
"""
import os

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "game", "Assets", "_Project", "Art", "Kenta", "Gen", "world")

O = (27, 36, 48, 255)          # contour
S, S2, S3 = (79, 151, 173, 255), (51, 105, 124, 255), (155, 211, 223, 255)   # carapace, ombre, reflet
RIV = (190, 228, 236, 255)     # rivets
V, E = (27, 36, 48, 255), (255, 216, 74, 255)                                # visière, yeux
P, P2, P3 = (255, 201, 60, 255), (217, 141, 28, 255), (255, 241, 168, 255)  # tremplin, ombre, reflet
K, K2 = (170, 178, 188, 255), (93, 102, 112, 255)                            # ressorts
F, F2 = (59, 42, 32, 255), (90, 64, 48, 255)                                 # pieds
W, H = 48, 52


def frame(squash):
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = im.load()

    def put(x, y, c):
        if 0 <= x < W and 0 <= y < H:
            px[x, y] = c

    base = H - 2                                      # pieds sur l'avant-dernière ligne (contour dessous)
    # pieds
    for x0 in (11, 29):
        for x in range(x0, x0 + 8):
            for y in range(base - 3, base + 1):
                put(x, y, F2 if y == base - 3 else F)
    # carapace : demi-ellipse posée sur les pieds
    cx, by, rx, ry = 23.5, base - 3, 19.5, 21.0
    for y in range(int(by - ry), by + 1):
        for x in range(W):
            if ((x - cx) / rx) ** 2 + ((y - by) / ry) ** 2 <= 1.0:
                put(x, y, S2 if y >= by - 4 else S)
    # reflet en haut à gauche
    for y in range(int(by - ry), by):
        for x in range(W):
            inside = ((x - cx) / rx) ** 2 + ((y - by) / ry) ** 2 <= 1.0
            if inside and ((x - 16) / 7.5) ** 2 + ((y - (by - 12)) / 3.2) ** 2 <= 1.0:
                put(x, y, S3)
    # visière et yeux (en colère)
    vy = by - 11
    for y in (vy, vy + 1, vy + 2, vy + 3, vy + 4):
        for x in range(10, 38):
            if px[x, y][3] and ((x - cx) / rx) ** 2 + ((y - by) / ry) ** 2 <= 0.86:
                put(x, y, V)
    for ex, slant in ((15, 1), (28, -1)):
        for dx in range(5):
            put(ex + dx, vy + 2, E)
            put(ex + dx, vy + 3, E)
        for dx in range(5):
            if (slant > 0 and dx >= 2) or (slant < 0 and dx <= 2):
                put(ex + dx, vy + 1, E)
    # rivets sur la bande basse
    for x in (8, 14, 20, 27, 33, 39):
        if px[x, by - 2][3]:
            put(x, by - 2, RIV)
    # ressorts (zigzag) entre la carapace et le tremplin
    top = int(by - ry) + 1
    pad_y = top - (7 if not squash else 4)            # haut du tremplin
    for sx in (15, 31):
        for i, y in enumerate(range(pad_y + 3, top + 1)):
            off = 1 if i % 2 == 0 else -1
            put(sx + off, y, K)
            put(sx + off + 1, y, K2)
    # tremplin
    for y in range(pad_y, pad_y + 4):
        for x in range(7, 41):
            corner = (x in (7, 40)) and y in (pad_y, pad_y + 3)
            if corner:
                continue
            c = P3 if y == pad_y and 9 <= x <= 38 else P2 if y == pad_y + 3 else P
            put(x, y, c)
    # contour d'un pixel autour de tout
    solid = [[px[x, y][3] > 0 for y in range(H)] for x in range(W)]
    for x in range(W):
        for y in range(H):
            if solid[x][y]:
                continue
            if any(0 <= x + dx < W and 0 <= y + dy < H and solid[x + dx][y + dy]
                   for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                put(x, y, O)
    return im


def main():
    if os.path.exists(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "game", "Assets", "_Project", "Art",
                                   "Kenta", "Pdf", ".gemini")) and "--force" not in __import__("sys").argv:
        raise SystemExit("Les ennemis et objets viennent de Gemini (tools/gemini-ennemis/clean.py) : rien n'est écrasé. "
                         "--force pour revenir aux anciens sprites.")
    frames = [frame(False), frame(True)]
    # recadrage commun (les deux images gardent la même taille et les pieds au même endroit)
    boxes = [f.getbbox() for f in frames]
    x0 = min(b[0] for b in boxes)
    y0 = min(b[1] for b in boxes)
    x1 = max(b[2] for b in boxes)
    y1 = max(b[3] for b in boxes)
    os.makedirs(OUT, exist_ok=True)
    for i, f in enumerate(frames):
        f.crop((x0, y0, x1, y1)).save(os.path.join(OUT, f"stomper_{i}.png"))
    print(f"stomper_0.png, stomper_1.png : {x1 - x0}×{y1 - y0} px dans {OUT}")


if __name__ == "__main__":
    main()
