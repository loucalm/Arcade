"""Mini bibliothèque de rendu « néon » pour générer les sprites du jeu (DA façon Rider).

Principe : on dessine des TRAITS (masques) par couleur, puis on les transforme en lumière :
  - corps du trait    : la couleur, nette (anti-aliasée par sur-échantillonnage x4) ;
  - cœur              : même trait, plus fin, presque blanc (effet tube néon) ;
  - halo              : le masque flouté à trois rayons (serré, moyen, large).
Les couches s'additionnent comme de la lumière, puis on convertit en RGBA « alpha droit » :
alpha = canal max, couleur = rgb / alpha. Sur fond sombre, le rendu est celui d'un néon.

Dépendances : Pillow, numpy (tools/requirements.txt).
"""
import math

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SS = 4  # sur-échantillonnage du dessin


def hex_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


# Palette Rider : turquoise + magenta, ambre pour les dangers, jaune pâle pour les lums.
CYAN = hex_rgb("#3ff0ff")
MAGENTA = hex_rgb("#ff2bb4")
AMBER = hex_rgb("#ffb12e")
LUM = hex_rgb("#fff0a0")
WHITE = (1.0, 1.0, 1.0)
TEAL_FILL = hex_rgb("#1d6f86")


class Layer:
    """Une couleur de néon : un masque « corps » et un masque « cœur »."""

    def __init__(self, canvas, color, strength=1.0, glow=1.0, core=True):
        self.c = canvas
        self.color = color
        self.strength = strength
        self.glow = glow
        self.has_core = core
        size = (canvas.w * SS, canvas.h * SS)
        self.body = Image.new("L", size, 0)
        self.core = Image.new("L", size, 0)
        self.db = ImageDraw.Draw(self.body)
        self.dc = ImageDraw.Draw(self.core)

    # ---------- primitives (coordonnées en pixels finaux, flottants) ----------

    def stroke(self, pts, width=5.0, closed=False, core_ratio=0.42, taper=None):
        """Polyligne aux bouts et jointures arrondis. taper = largeur finale (affinage progressif)."""
        pts = list(pts)
        if closed:
            pts = pts + [pts[0]]
        n = len(pts) - 1
        for i in range(max(n, 1)):
            a = pts[i]
            b = pts[min(i + 1, len(pts) - 1)]
            w0 = width if taper is None else width + (taper - width) * (i / max(n, 1))
            w1 = width if taper is None else width + (taper - width) * ((i + 1) / max(n, 1))
            self._seg(a, b, w0, w1, core_ratio)

    def _seg(self, a, b, w0, w1, core_ratio):
        steps = 1 if abs(w0 - w1) < 0.01 else 6
        for k in range(steps):
            t0, t1 = k / steps, (k + 1) / steps
            p0 = (a[0] + (b[0] - a[0]) * t0, a[1] + (b[1] - a[1]) * t0)
            p1 = (a[0] + (b[0] - a[0]) * t1, a[1] + (b[1] - a[1]) * t1)
            w = w0 + (w1 - w0) * (t0 + t1) * 0.5
            self._line(self.db, p0, p1, w)
            if self.has_core:
                self._line(self.dc, p0, p1, max(w * core_ratio, 1.2))

    @staticmethod
    def _line(draw, p0, p1, w):
        s = SS
        draw.line([(p0[0] * s, p0[1] * s), (p1[0] * s, p1[1] * s)], fill=255, width=max(1, int(round(w * s))))
        r = w * s / 2
        for (x, y) in (p0, p1):
            draw.ellipse((x * s - r, y * s - r, x * s + r, y * s + r), fill=255)

    def circle(self, cx, cy, r, width=5.0, core_ratio=0.42, fill=False):
        if fill:
            self._disc(self.db, cx, cy, r)
            if self.has_core:
                self._disc(self.dc, cx, cy, r * 0.55)
            return
        pts = [(cx + math.cos(a) * r, cy + math.sin(a) * r) for a in (i * math.tau / 72 for i in range(72))]
        self.stroke(pts, width, closed=True, core_ratio=core_ratio)

    @staticmethod
    def _disc(draw, cx, cy, r):
        s = SS
        draw.ellipse(((cx - r) * s, (cy - r) * s, (cx + r) * s, (cy + r) * s), fill=255)

    def poly_fill(self, pts, core_scale=0.55):
        s = SS
        self.db.polygon([(x * s, y * s) for x, y in pts], fill=255)
        if self.has_core:
            mx = sum(p[0] for p in pts) / len(pts)
            my = sum(p[1] for p in pts) / len(pts)
            sm = [(mx + (x - mx) * core_scale, my + (y - my) * core_scale) for x, y in pts]
            self.dc.polygon([(x * s, y * s) for x, y in sm], fill=255)


class FillLayer:
    """Aplat translucide sans halo (remplissages, dégradés). `data` = tableau float 0..1 à la taille finale."""

    def __init__(self, color, data, strength=1.0):
        self.color = color
        self.data = data
        self.strength = strength


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.layers = []
        self.fills = []

    def layer(self, color, strength=1.0, glow=1.0, core=True):
        L = Layer(self, color, strength, glow, core)
        self.layers.append(L)
        return L

    def fill(self, color, data, strength=1.0):
        self.fills.append(FillLayer(color, data, strength))

    def render(self, glow_scale=1.0):
        w, h = self.w, self.h
        rgb = np.zeros((h, w, 3), dtype=np.float64)
        for f in self.fills:
            rgb += np.asarray(f.color)[None, None, :] * (f.data * f.strength)[:, :, None]
        for L in self.layers:
            body = L.body.resize((w, h), Image.LANCZOS)
            core = np.asarray(L.core.resize((w, h), Image.LANCZOS), dtype=np.float64) / 255.0
            m = np.asarray(body, dtype=np.float64) / 255.0
            g = L.glow * glow_scale
            b1 = np.asarray(body.filter(ImageFilter.GaussianBlur(1.6 * g)), dtype=np.float64) / 255.0
            b2 = np.asarray(body.filter(ImageFilter.GaussianBlur(5.0 * g)), dtype=np.float64) / 255.0
            b3 = np.asarray(body.filter(ImageFilter.GaussianBlur(13.0 * g)), dtype=np.float64) / 255.0
            light = np.clip(m + 0.85 * b1 + 0.75 * b2 + 0.5 * b3, 0.0, 1.0) * L.strength
            col = np.asarray(L.color)
            hot = col + (1.0 - col) * 0.78  # cœur : couleur éclaircie vers le blanc
            rgb += col[None, None, :] * light[:, :, None]
            if L.has_core:
                rgb += hot[None, None, :] * (core * L.strength)[:, :, None]
        rgb = np.clip(rgb, 0.0, 1.0)
        alpha = rgb.max(axis=2)
        out = np.zeros((h, w, 4), dtype=np.float64)
        nz = alpha > 1e-4
        out[..., :3][nz] = rgb[nz] / alpha[nz][:, None]
        out[..., 3] = alpha
        return Image.fromarray((np.clip(out, 0, 1) * 255.0 + 0.5).astype(np.uint8), "RGBA")


# ---------- aides géométriques ----------

def fk(p, angle_deg, length):
    """Point à `length` de `p`, angle mesuré depuis « vers le bas », positif vers l'avant (+x). y vers le bas."""
    a = math.radians(angle_deg)
    return (p[0] + math.sin(a) * length, p[1] + math.cos(a) * length)


def rotate(pts, deg, origin):
    a = math.radians(deg)
    ca, sa = math.cos(a), math.sin(a)
    ox, oy = origin
    return [(ox + (x - ox) * ca - (y - oy) * sa, oy + (x - ox) * sa + (y - oy) * ca) for x, y in pts]


def scale_pts(pts, sx, sy, origin):
    ox, oy = origin
    return [(ox + (x - ox) * sx, oy + (y - oy) * sy) for x, y in pts]


def arc_pts(cx, cy, r, a0, a1, n=24):
    return [(cx + math.cos(math.radians(a0 + (a1 - a0) * i / n)) * r,
             cy + math.sin(math.radians(a0 + (a1 - a0) * i / n)) * r) for i in range(n + 1)]


def vertical_gradient(w, h, y0, y1, a0, a1):
    """Tableau float (h, w) : alpha de a0 (en y0) à a1 (en y1), 0 en dehors vers le haut, a1 vers le bas."""
    ys = np.arange(h, dtype=np.float64)
    t = np.clip((ys - y0) / max(y1 - y0, 1e-6), 0, 1)
    col = np.where(ys < y0, 0.0, a0 + (a1 - a0) * t)
    return np.repeat(col[:, None], w, axis=1)


def save(img, path):
    img.save(path, optimize=True)
