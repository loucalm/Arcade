"""Petite bibliothèque de dessin pixel art (sans anti-aliasing) pour tools/gen-kenta-assets.py.

Tout est dessiné à la résolution finale du jeu (1 px = 1 pixel de l'écran 240×135), avec des primitives
aliasées. Les couleurs sont des tuples RGBA 0-255 ou des chaînes « #RRGGBB ».
"""
import math
import random

import numpy as np
from PIL import Image, ImageDraw


def col(c, a=255):
    if isinstance(c, str):
        c = c.lstrip("#")
        return int(c[0:2], 16), int(c[2:4], 16), int(c[4:6], 16), a
    return tuple(c) if len(c) == 4 else (c[0], c[1], c[2], a)


def mix(c1, c2, t):
    a, b = col(c1), col(c2)
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3)) + (255,)


def shade(c, f):
    """f > 0 éclaircit vers le blanc, f < 0 assombrit vers le noir."""
    return mix(c, (255, 255, 255), f) if f > 0 else mix(c, (0, 0, 0), -f)


class Pix:
    def __init__(self, w, h, bg=None):
        self.w, self.h = w, h
        self.im = Image.new("RGBA", (w, h), col(bg) if bg else (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.im)

    # --- primitives ---
    def px(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.im.putpixel((int(x), int(y)), col(c))

    def rect(self, x0, y0, x1, y1, c):
        """Rectangle plein, x1 / y1 exclus."""
        if x1 > x0 and y1 > y0:
            self.d.rectangle([x0, y0, x1 - 1, y1 - 1], fill=col(c))

    def poly(self, pts, c):
        self.d.polygon([(int(round(x)), int(round(y))) for x, y in pts], fill=col(c))

    def circle(self, cx, cy, r, c):
        self.d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=col(c))

    def ellipse(self, cx, cy, rx, ry, c):
        self.d.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=col(c))

    def line(self, a, b, c, width=1):
        self.d.line([a, b], fill=col(c), width=width)

    def paste(self, other, x, y):
        self.im.alpha_composite(other.im if isinstance(other, Pix) else other, (int(x), int(y)))

    # --- helpers ---
    def array(self):
        return np.asarray(self.im).copy()

    def save(self, path):
        self.im.save(path, optimize=True)


def outlined(img, color="#14101C", thickness=1):
    """Ajoute un contour noir d'un pixel (4-voisinage) autour des pixels opaques."""
    im = img.im if isinstance(img, Pix) else img
    a = np.asarray(im)
    pad = thickness
    big = np.zeros((a.shape[0] + 2 * pad, a.shape[1] + 2 * pad, 4), dtype=np.uint8)
    big[pad:-pad, pad:-pad] = a
    solid = big[..., 3] > 0
    grown = solid.copy()
    for _ in range(thickness):
        g = grown.copy()
        g[1:, :] |= grown[:-1, :]
        g[:-1, :] |= grown[1:, :]
        g[:, 1:] |= grown[:, :-1]
        g[:, :-1] |= grown[:, 1:]
        grown = g
    ring = grown & ~solid
    out = big.copy()
    out[ring] = col(color)
    res = Pix(out.shape[1], out.shape[0])
    res.im = Image.fromarray(out, "RGBA")
    res.d = ImageDraw.Draw(res.im)
    return res


def banded_gradient(w, h, colors, y0=0, y1=None, jitter=0, rng=None):
    """Dégradé vertical en bandes (comme les ciels des maquettes) : une bande par couleur, entre y0 et y1."""
    y1 = y1 or h
    p = Pix(w, h)
    n = len(colors)
    for i, c in enumerate(colors):
        a = y0 + (y1 - y0) * i // n
        b = y0 + (y1 - y0) * (i + 1) // n
        p.rect(0, a, w, b, c)
    if jitter and rng:
        # bord de bande en escalier irrégulier (tramage simple)
        for i in range(1, n):
            y = y0 + (y1 - y0) * i // n
            for x in range(0, w):
                if rng.random() < jitter:
                    p.px(x, y, colors[i - 1])
                    if rng.random() < 0.5:
                        p.px(x, y - 1, colors[i])
    return p


def blob(p, cx, cy, rx, ry, c, hi=None, lo=None, rng=None, lumps=5):
    """Nuage / couronne d'arbre : amas de disques avec reflet et ombre."""
    rng = rng or random.Random(1)
    parts = []
    for i in range(lumps):
        a = (i / lumps) * math.tau
        parts.append((cx + math.cos(a) * rx * 0.55 + rng.uniform(-2, 2), cy + math.sin(a) * ry * 0.45 + rng.uniform(-1, 1),
                      max(3, int(min(rx, ry) * rng.uniform(0.55, 0.8)))))
    parts.append((cx, cy, int(min(rx, ry) * 0.9)))
    if lo:
        for x, y, r in parts:
            p.circle(int(x), int(y) + 2, r, lo)
    for x, y, r in parts:
        p.circle(int(x), int(y), r, c)
    if hi:
        for x, y, r in parts:
            p.circle(int(x) - r // 3, int(y) - r // 3, max(1, r // 2), hi)


def tile_wrap(p, x, draw):
    """Appelle draw(dx) trois fois (-W, 0, +W) pour un décor qui boucle sans couture."""
    for dx in (-p.w, 0, p.w):
        draw(x + dx)
