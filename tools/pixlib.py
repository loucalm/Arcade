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
    """Image pixel art. `scale` > 1 : on dessine en coordonnées « logiques » (celles d'une image w×h), mais l'image fait
    (w·scale)×(h·scale) et les primitives sont tracées à cette définition : même composition, pixels plus fins.
    Sert aux décors 640×360 dessinés avec les coordonnées de l'ancien écran 320×180."""

    def __init__(self, w, h, bg=None, scale=1):
        self.w, self.h, self.s = w, h, scale
        self.im = Image.new("RGBA", (w * scale, h * scale), col(bg) if bg else (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.im)

    # --- primitives (coordonnées logiques) ---
    def px(self, x, y, c):
        """Un seul pixel réel (détail fin : étoile, tramage), à la position logique (x, y)."""
        s = self.s
        if 0 <= x < self.w and 0 <= y < self.h:
            self.im.putpixel((int(x * s), int(y * s)), col(c))

    def rect(self, x0, y0, x1, y1, c):
        """Rectangle plein, x1 / y1 exclus."""
        s = self.s
        if x1 > x0 and y1 > y0:
            self.d.rectangle([x0 * s, y0 * s, x1 * s - 1, y1 * s - 1], fill=col(c))

    def poly(self, pts, c):
        s = self.s
        self.d.polygon([(int(round(x * s)), int(round(y * s))) for x, y in pts], fill=col(c))

    def circle(self, cx, cy, r, c):
        s = self.s
        self.d.ellipse([(cx - r) * s, (cy - r) * s, (cx + r) * s, (cy + r) * s], fill=col(c))

    def ellipse(self, cx, cy, rx, ry, c):
        s = self.s
        self.d.ellipse([(cx - rx) * s, (cy - ry) * s, (cx + rx) * s, (cy + ry) * s], fill=col(c))

    def line(self, a, b, c, width=1):
        s = self.s
        self.d.line([(a[0] * s, a[1] * s), (b[0] * s, b[1] * s)], fill=col(c), width=max(1, int(width * s)))

    def paste(self, other, x, y):
        self.im.alpha_composite(other.im if isinstance(other, Pix) else other, (int(x * self.s), int(y * self.s)))

    # --- helpers ---
    def array(self):
        return np.asarray(self.im).copy()

    def save(self, path):
        self.im.save(path, optimize=True)


def outlined(img, color="#14101C", thickness=1, outer_only=False):
    """Ajoute un contour noir d'un pixel (4-voisinage) autour des pixels opaques.
    outer_only : ignore les trous intérieurs (le centre des « 0 » reste vide, la police reste lisible)."""
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
    if outer_only:
        outside = np.zeros_like(solid)
        outside[0, :] = outside[-1, :] = outside[:, 0] = outside[:, -1] = True
        outside &= ~solid
        while True:                                   # remplissage du vide extérieur (4-voisinage)
            n = outside.copy()
            n[1:, :] |= outside[:-1, :]
            n[:-1, :] |= outside[1:, :]
            n[:, 1:] |= outside[:, :-1]
            n[:, :-1] |= outside[:, 1:]
            n &= ~solid
            if (n == outside).all():
                break
            outside = n
        ring &= outside
    out = big.copy()
    out[ring] = col(color)
    res = Pix(out.shape[1], out.shape[0])
    res.im = Image.fromarray(out, "RGBA")
    res.d = ImageDraw.Draw(res.im)
    return res


def banded_gradient(w, h, colors, y0=0, y1=None, jitter=0, rng=None, scale=1):
    """Dégradé vertical en bandes (comme les ciels des maquettes) : une bande par couleur, entre y0 et y1."""
    y1 = y1 or h
    p = Pix(w, h, scale=scale)
    n = len(colors)
    for i, c in enumerate(colors):
        a = y0 + (y1 - y0) * i // n
        b = y0 + (y1 - y0) * (i + 1) // n
        p.rect(0, a, w, b, c)
    if jitter and rng:
        # bord de bande en escalier irrégulier (tramage simple)
        for i in range(1, n):
            y = y0 + (y1 - y0) * i // n
            for x in range(0, w * scale):
                xl = x / scale
                if rng.random() < jitter:
                    p.rect(xl, y, xl + 1 / scale, y + 1 / scale, colors[i - 1])
                    if rng.random() < 0.5:
                        p.rect(xl, y - 1 / scale, xl + 1 / scale, y, colors[i])
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


# ============================================================================
# VOLUMES : forme (masque) ombrée comme dans les maquettes du PDF
# ============================================================================
class Shape:
    """Masque en pixels réels construit avec les coordonnées logiques d'un Pix (même `scale`). On l'ombre ensuite
    d'un coup (shade_into) : reflet sur le bord éclairé (haut-gauche), ombre sur le bord opposé, contour foncé."""

    def __init__(self, p):
        self.s = p.s
        self.m = Image.new("L", p.im.size, 0)
        self.d = ImageDraw.Draw(self.m)

    def ellipse(self, cx, cy, rx, ry):
        s = self.s
        self.d.ellipse([(cx - rx) * s, (cy - ry) * s, (cx + rx) * s, (cy + ry) * s], fill=255)
        return self

    def circle(self, cx, cy, r):
        return self.ellipse(cx, cy, r, r)

    def poly(self, pts):
        s = self.s
        self.d.polygon([(int(round(x * s)), int(round(y * s))) for x, y in pts], fill=255)
        return self

    def rect(self, x0, y0, x1, y1):
        s = self.s
        if x1 > x0 and y1 > y0:
            self.d.rectangle([x0 * s, y0 * s, x1 * s - 1, y1 * s - 1], fill=255)
        return self

    def line(self, a, b, width=1):
        s = self.s
        self.d.line([(a[0] * s, a[1] * s), (b[0] * s, b[1] * s)], fill=255, width=max(1, int(width * s)))
        return self

    def cut(self, other):
        """Retire une autre forme (trou, échancrure)."""
        a = np.asarray(self.m) > 0
        b = np.asarray(other.m) > 0
        self.m = Image.fromarray(((a & ~b) * 255).astype(np.uint8), "L")
        self.d = ImageDraw.Draw(self.m)
        return self

    def mask(self):
        return np.asarray(self.m) > 0


def _shift(m, dx, dy):
    """Masque décalé de (dx, dy) pixels (hors champ = vide)."""
    h, w = m.shape
    out = np.zeros_like(m)
    ys = slice(max(0, dy), min(h, h + dy))
    yd = slice(max(0, -dy), min(h, h - dy))
    xs = slice(max(0, dx), min(w, w + dx))
    xd = slice(max(0, -dx), min(w, w - dx))
    out[ys, xs] = m[yd, xd]
    return out


def shade_into(p, shape, base, hi=None, lo=None, edge=None, hi_w=2, lo_w=3, light=(-1, -1), texture=None, rng=None,
               lo2=None, lo2_w=2):
    """Peint la forme dans p : aplat `base`, reflet `hi` (bande de hi_w px du côté de la lumière), ombre `lo` (bande de
    lo_w px du côté opposé), contour `edge` d'1 px. texture = (couleur, densité) : mouchetis de la matière (pierre…)."""
    m = shape.mask() if isinstance(shape, Shape) else shape
    a = np.asarray(p.im).copy()
    lx, ly = light
    a[m] = col(base)
    if texture and rng is not None:
        tc, dens = texture
        r = np.random.default_rng(rng.randint(0, 1 << 30)).random(m.shape)
        a[m & (r < dens)] = col(tc)
    if lo:
        # pixel à l'ombre : son voisin à lo_w px vers l'ombre (opposé à la lumière) sort de la forme
        away = _shift(m, lx * lo_w, ly * lo_w)
        a[m & ~away] = col(lo)
        if lo2:
            away2 = _shift(m, lx * lo2_w, ly * lo2_w)
            a[m & ~away2] = col(lo2)
    if hi:
        toward = _shift(m, -lx * hi_w, -ly * hi_w)
        a[m & ~toward] = col(hi)
    if edge:
        inner = m & _shift(m, 1, 0) & _shift(m, -1, 0) & _shift(m, 0, 1) & _shift(m, 0, -1)
        a[m & ~inner] = col(edge)
    p.im = Image.fromarray(a, "RGBA")
    p.d = ImageDraw.Draw(p.im)
    return p


def cloud(p, cx, cy, w, body, hi=None, lo=None, rng=None):
    """Nuage pixel art des maquettes : grosses bulles au-dessus d'une base plate, reflet en haut, ombre en dessous."""
    rng = rng or random.Random(1)
    sh = Shape(p)
    h = max(4, w // 4)
    base_y = cy + h // 2
    sh.rect(cx - w // 2 + h // 3, base_y - h // 2, cx + w // 2 - h // 3, base_y)
    x = cx - w // 2 + h // 2
    while x < cx + w // 2 - h // 3:
        r = rng.randint(max(2, h // 2), max(3, int(h * 0.95)))
        sh.circle(x, base_y - r + 1, r)
        x += r + rng.randint(0, max(1, r // 2))
    flat = Shape(p).rect(cx - w, base_y, cx + w, base_y + h * 2)
    sh.cut(flat)
    return shade_into(p, sh, body, hi=hi, lo=lo, hi_w=1, lo_w=2, light=(0, -1))


def canopy(p, cx, cy, rx, ry, base, hi, lo, edge=None, rng=None, lumps=7):
    """Couronne d'arbre en grappes (comme les arbres des maquettes) : chaque grappe est ombrée à part, de l'arrière
    vers l'avant, ce qui dessine les volumes. Renvoie la liste des grappes (x, y, r)."""
    rng = rng or random.Random(1)
    parts = []
    for i in range(lumps):
        a = rng.uniform(math.pi * 0.95, math.tau * 1.05) if i < lumps - 2 else rng.uniform(0, math.tau)
        d = rng.uniform(0.35, 0.75)
        parts.append((cx + math.cos(a) * rx * d, cy + math.sin(a) * ry * d * 0.9, max(3, int(min(rx, ry) * rng.uniform(0.45, 0.7)))))
    parts.append((cx, cy, int(min(rx, ry) * 0.75)))
    parts.sort(key=lambda t: t[1])                  # l'arrière (haut) d'abord, l'avant (bas) recouvre
    back = Shape(p)
    for x, y, r in parts:
        back.circle(x, y + 2, r)
    shade_into(p, back, lo, edge=edge)               # masse sombre derrière : creux entre les grappes
    for x, y, r in parts:
        shade_into(p, Shape(p).circle(x, y, r), base, hi=hi, lo=lo, hi_w=2, lo_w=2)
    return parts


def speckle(p, shape, c, density, rng, size=1):
    """Mouchetis dans une forme (pierre, terre) : `size` en pixels logiques."""
    m = shape.mask() if isinstance(shape, Shape) else shape
    ys, xs = np.nonzero(m)
    a = np.asarray(p.im).copy()
    s = p.s
    n = int(len(xs) * density / max(1, (size * s) ** 2))
    for _ in range(n):
        k = rng.randrange(len(xs))
        x0, y0 = xs[k] - xs[k] % (size * s), ys[k] - ys[k] % (size * s)
        a[y0:y0 + size * s, x0:x0 + size * s][m[y0:y0 + size * s, x0:x0 + size * s]] = col(c)
    p.im = Image.fromarray(a, "RGBA")
    p.d = ImageDraw.Draw(p.im)
