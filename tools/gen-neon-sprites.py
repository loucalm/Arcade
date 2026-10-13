#!/usr/bin/env python3
"""Génère tous les sprites du jeu, en néon filaire façon Rider (voir docs/art-direction.md).

Usage :
  python3 tools/gen-neon-sprites.py [dossier_sortie] [--only hero,enemy,...] [--sheet preview.png]

Dossier de sortie par défaut : game/Assets/_Project/Art/Neon
Les pivots, bordures de 9-slice et PPU sont réglés dans Unity par le menu « Rythme Runner > Importer les sprites
néon » (Assets/_Project/Editor/NeonSpriteImporter.cs). Les constantes y sont documentées dans docs/art-direction.md.
"""
import argparse
import math
import os
import random
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from neonlib import (AMBER, CYAN, LUM, MAGENTA, TEAL_FILL, WHITE, Canvas, arc_pts, fk, hex_rgb, rotate,  # noqa: E402
                     save, scale_pts, vertical_gradient)

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DEFAULT_OUT = os.path.join(ROOT, "game", "Assets", "_Project", "Art", "Neon")

# ============================================================================
# HÉROS : silhouette filaire (turquoise) + écharpe et gants magenta
# ============================================================================
HERO_W = HERO_H = 224
HERO_GROUND = 176          # y du sol dans l'image (pivot du sprite = pieds)
HERO_CX = 108
SC = 1.2                   # échelle du squelette
THIGH, SHIN, FOOT = 25.0 * SC, 25.0 * SC, 9.0 * SC
UPPER, FORE = 17.0 * SC, 17.0 * SC
TORSO, NECK, HEAD_R = 31.0 * SC, 4.0 * SC, 11.0 * SC
STROKE = 6.0


def leg_points(hip, thigh_a, flex, foot_a=None):
    knee = fk(hip, thigh_a, THIGH)
    shin_a = thigh_a - flex
    ankle = fk(knee, shin_a, SHIN)
    if foot_a is None:
        foot_a = 78 + 0.35 * shin_a
    toe = fk(ankle, foot_a, FOOT)
    return [hip, knee, ankle, toe]


def arm_points(shoulder, upper_a, elbow):
    el = fk(shoulder, upper_a, UPPER)
    hand = fk(el, upper_a + elbow, FORE)
    return [shoulder, el, hand]


def pose_points(p):
    """p : dict de paramètres -> dict de segments (hip placé après calage au sol)."""
    hip = (HERO_CX, 110.0)
    lean = p.get("lean", 12)
    neck = fk(hip, 180 - lean, TORSO)
    shoulder = fk(hip, 180 - lean, TORSO * 0.88)
    head_tilt = p.get("head", lean * 0.5)
    head_c = fk(neck, 180 - head_tilt, NECK + HEAD_R)
    legA = leg_points(hip, *p["legA"])
    legB = leg_points(hip, *p["legB"])
    armA = arm_points(shoulder, *p["armA"])
    armB = arm_points(shoulder, *p["armB"])
    return dict(hip=hip, neck=neck, head=head_c, head_tilt=head_tilt, legA=legA, legB=legB, armA=armA, armB=armB,
                lean=lean)


def shift_pts(pts, dx, dy):
    return [(x + dx, y + dy) for x, y in pts]


def draw_hero(p, scarf_phase=0.0, fist=False):
    seg = pose_points(p)
    # calage : le point le plus bas touche le sol
    lows = [pt[1] for k in ("legA", "legB", "armA", "armB") for pt in seg[k]] + [seg["hip"][1]]
    dy = HERO_GROUND - max(lows) - p.get("lift", 0)
    for k in ("legA", "legB", "armA", "armB"):
        seg[k] = shift_pts(seg[k], 0, dy)
    for k in ("hip", "neck", "head"):
        seg[k] = (seg[k][0], seg[k][1] + dy)
    # décalage horizontal propre à la pose (glissade) : les autres gardent la hanche à HERO_CX
    dx = p.get("dx", 0)
    if dx:
        for k in ("legA", "legB", "armA", "armB"):
            seg[k] = shift_pts(seg[k], dx, 0)
        for k in ("hip", "neck", "head"):
            seg[k] = (seg[k][0] + dx, seg[k][1])

    cv = Canvas(HERO_W, HERO_H)
    far = cv.layer(CYAN, strength=0.5)
    near = cv.layer(CYAN, strength=1.0)
    acc = cv.layer(MAGENTA, strength=1.0)

    far.stroke(seg["legB"], STROKE * 0.9)
    far.stroke(seg["armB"], STROKE * 0.85)
    near.stroke([seg["hip"], seg["neck"]], STROKE * 1.2)
    near.stroke(seg["legA"], STROKE)
    near.stroke(seg["armA"], STROKE * 0.9)
    hx, hy = seg["head"]
    near.circle(hx, hy, HEAD_R, width=STROKE * 0.95)
    # visière magenta : repère le sens de la course
    v0 = fk((hx, hy), 90 - seg["head_tilt"] * 0.4 - 25, HEAD_R * 0.55)
    v1 = fk((hx, hy), 90 - seg["head_tilt"] * 0.4 + 25, HEAD_R * 0.55)
    acc.stroke([(v0[0] + 3, v0[1]), (v1[0] + 3, v1[1] + 1)], 3.6)
    # gants
    r = 6.5 if fist else 4.2
    for hand in (seg["armA"][-1], seg["armB"][-1]):
        acc.circle(hand[0], hand[1], r, fill=True)
    # écharpe
    nx, ny = seg["neck"]
    sx, sy = nx - 4, ny - 1
    pts = [(sx, sy)]
    sag = p.get("scarf_sag", 3.0)
    amp = p.get("scarf_amp", 4.0)
    rise = p.get("scarf_rise", 0.0)
    for i in range(1, 6):
        t = i / 5
        x = pts[-1][0] - 11.5 * math.cos(math.radians(rise))
        y = sy + sag * i * 0.6 + amp * math.sin(scarf_phase * math.tau - i * 1.0) * t - rise * 0.7 * i * 0.4
        pts.append((x, y))
    acc.stroke(pts, 6.4, taper=2.0)
    return cv.render()


def run_frame(i, n):
    p = i / n
    ph = p * math.tau
    thigh = lambda q: 42 * math.sin(q) + 3
    flex = lambda q: 24 + 78 * max(0.0, math.cos(q)) ** 1.3
    return dict(
        lean=15,
        legA=(thigh(ph), flex(ph)),
        legB=(thigh(ph + math.pi), flex(ph + math.pi)),
        armA=(-38 * math.sin(ph) - 4, 82),
        armB=(38 * math.sin(ph) - 4, 82),
        scarf_sag=2.0, scarf_amp=5.0,
    ), p


HERO_POSES = {
    "jump_up": dict(lean=12, legA=(70, 80), legB=(-12, 55), armA=(150, 25), armB=(-45, 60),
                    scarf_sag=6, scarf_amp=3, scarf_rise=-10),
    "jump_apex": dict(lean=5, legA=(105, 125), legB=(78, 115), armA=(125, 35), armB=(40, 70),
                      scarf_sag=1, scarf_amp=5),
    "jump_fall": dict(lean=0, head=-4, legA=(30, 14), legB=(-6, 30), armA=(165, 8), armB=(112, 18),
                      scarf_sag=-4, scarf_amp=4, scarf_rise=22),
    "land": dict(lean=40, legA=(78, 118), legB=(55, 100), armA=(48, 65), armB=(22, 80), scarf_sag=5, scarf_amp=2),
    "slide": dict(lean=-74, head=-62, dx=-4, legA=(86, 6), legB=(58, 78), armA=(-72, -8), armB=(35, 35),
                  scarf_sag=-1, scarf_amp=2.5, scarf_rise=-6),
    "hit": dict(lean=26, legA=(58, 58), legB=(-42, 14), armA=(90, 0), armB=(-62, 70), scarf_sag=0, scarf_amp=5),
    "hurt": dict(lean=-16, head=-20, legA=(36, 42), legB=(-26, 50), armA=(172, 10), armB=(128, 25),
                 scarf_sag=-3, scarf_amp=6, scarf_rise=15),
}


def gen_hero(out, sheet):
    n = 12
    for i in range(n):
        p, ph = run_frame(i, n)
        img = draw_hero(p, scarf_phase=-ph)
        save(img, os.path.join(out, f"hero_run_{i:02d}.png"))
        sheet.append(img)
    for name, p in HERO_POSES.items():
        img = draw_hero(p, scarf_phase=0.3, fist=(name == "hit"))
        save(img, os.path.join(out, f"hero_{name}.png"))
        sheet.append(img)


# ============================================================================
# ENNEMIS, OBSTACLES, LUMS (palette : magenta = à frapper, cyan = terrain, ambre = laser, jaune = lum)
# ============================================================================
def gen_enemy(out, sheet):
    """Gemme à un oeil, 2 images qui alternent sur le beat. Pivot : bas de la gemme (y=142 sur 160)."""
    base = [(80, 50), (106, 76), (120, 100), (104, 130), (80, 142), (56, 130), (40, 100), (54, 76)]
    spikes = [[(64, 67), (52, 40)], [(80, 52), (80, 22)], [(96, 67), (108, 40)]]
    for f, (tilt, sx, sy, look) in enumerate([(-7, 1.0, 1.0, -5), (7, 1.06, 0.92, 5)]):
        cv = Canvas(160, 160)
        body = cv.layer(MAGENTA)
        facets = cv.layer(MAGENTA, strength=0.5, core=False)
        eye = cv.layer(WHITE, strength=0.95)
        origin = (80, 142)

        def T(pts):
            return rotate(scale_pts(pts, sx, sy, origin), tilt, origin)
        poly = T(base)
        body.stroke(poly, 5.8, closed=True)
        for sp in spikes:
            body.stroke(T(sp), 5.0, taper=2.0)
        ex, ey = T([(80, 100)])[0]
        for v in (poly[1], poly[3], poly[5], poly[7]):
            facets.stroke([(ex + (v[0] - ex) * 0.32, ey + (v[1] - ey) * 0.32), v], 2.6)
        eye.circle(ex, ey, 15, width=4.4)
        eye.circle(ex + look, ey + 2, 4.2, fill=True)
        img = cv.render()
        save(img, os.path.join(out, f"enemy_{f}.png"))
        sheet.append(img)


def gen_block(out, sheet):
    """Caisse à frapper. Pivot : bas de la caisse (y=140 sur 160)."""
    cv = Canvas(160, 160)
    a = cv.layer(MAGENTA)
    b = cv.layer(MAGENTA, strength=0.55, core=False)
    x0, y0, x1, y1 = 34, 48, 126, 140
    a.stroke([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], 6.0, closed=True)
    i0 = 20
    b.stroke([(x0 + i0, y0 + i0), (x1 - i0, y0 + i0), (x1 - i0, y1 - i0), (x0 + i0, y1 - i0)], 3.6, closed=True)
    for (cx, cy, ix, iy) in ((x0, y0, x0 + i0, y0 + i0), (x1, y0, x1 - i0, y0 + i0),
                             (x1, y1, x1 - i0, y1 - i0), (x0, y1, x0 + i0, y1 - i0)):
        b.stroke([(cx, cy), (ix, iy)], 3.0)
    img = cv.render()
    save(img, os.path.join(out, "block.png"))
    sheet.append(img)


def gen_wall(out, sheet):
    """Mur (9-slice, bordure 44 px) : contour cyan, double filet, remplissage translucide."""
    n = 128
    cv = Canvas(n, n)
    o = cv.layer(CYAN)
    i = cv.layer(CYAN, strength=0.45, core=False)
    d0, d1 = 30, n - 30
    r = 9
    pts = []
    for (cx, cy, a0) in ((d1 - r, d0 + r, -90), (d1 - r, d1 - r, 0), (d0 + r, d1 - r, 90), (d0 + r, d0 + r, 180)):
        pts += arc_pts(cx, cy, r, a0, a0 + 90, 8)
    o.stroke(pts, 6.0, closed=True)
    e0, e1 = 44, n - 44
    i.stroke([(e0, e0), (e1, e0), (e1, e1), (e0, e1)], 3.2, closed=True)
    for (x, y) in ((d0 + 2, d0 + 2), (d1 - 2, d0 + 2), (d1 - 2, d1 - 2), (d0 + 2, d1 - 2)):
        o.circle(x, y, 3.2, fill=True)
    data = np.zeros((n, n))
    data[d0:d1, d0:d1] = 0.34
    cv.fill(TEAL_FILL, data)
    img = cv.render()
    save(img, os.path.join(out, "wall.png"))
    sheet.append(img)


def gen_laser(out, sheet):
    # faisceau (9-slice horizontal, bordures 24)
    cv = Canvas(128, 64)
    b = cv.layer(AMBER)
    b.stroke([(-10, 32), (138, 32)], 6.0)
    img = cv.render()
    save(img, os.path.join(out, "laser_beam.png"))
    sheet.append(img)
    # émetteur
    cv = Canvas(64, 64)
    n = cv.layer(AMBER)
    hexa = [(32 + math.cos(math.radians(60 * k + 30)) * 15, 32 + math.sin(math.radians(60 * k + 30)) * 15) for k in range(6)]
    n.stroke(hexa, 4.6, closed=True)
    n.circle(32, 32, 4.5, fill=True)
    img = cv.render()
    save(img, os.path.join(out, "laser_node.png"))
    sheet.append(img)
    # poteau (9-slice vertical, bordures 32)
    cv = Canvas(64, 128)
    p = cv.layer(CYAN, strength=0.55, core=False)
    p.stroke([(32, -10), (32, 138)], 3.4)
    img = cv.render()
    save(img, os.path.join(out, "laser_post.png"))
    sheet.append(img)


def gen_lum(out, sheet):
    cv = Canvas(96, 96)
    d = cv.layer(LUM)
    c = (48, 48)
    d.stroke([(c[0], c[1] - 25), (c[0] + 25, c[1]), (c[0], c[1] + 25), (c[0] - 25, c[1])], 5.0, closed=True)
    d.circle(c[0], c[1], 5.5, fill=True)
    img = cv.render()
    save(img, os.path.join(out, "lum.png"))
    sheet.append(img)


def gen_checkpoint(out, sheet):
    """Balise de section. Pivot : pied du mât (x=40, y=496 sur 512)."""
    cv = Canvas(128, 512)
    pole = cv.layer(CYAN, strength=0.85)
    flag = cv.layer(MAGENTA)
    pole.stroke([(40, 496), (40, 64)], 5.0)
    pole.stroke([(40, 36), (52, 50), (40, 64), (28, 50)], 4.2, closed=True)
    flag.stroke([(40, 78), (104, 104), (40, 130)], 5.0, closed=True)
    pole.stroke([(14, 496), (66, 496)], 4.0)
    img = cv.render()
    save(img, os.path.join(out, "checkpoint.png"))
    sheet.append(img)


# ============================================================================
# SOL, DÉCOR, EFFETS, UI
# ============================================================================
def gen_ground(out, sheet):
    """Ligne de sol (9-slice horizontal, bordures 24) : trait néon + dégradé + deux filets de fuite."""
    w, h = 64, 256
    cv = Canvas(w, h)
    top = cv.layer(CYAN)
    top.stroke([(20, 48), (44, 48)], 6.2)
    cv.fill(CYAN, vertical_gradient(w, h, 52, 210, 0.26, 0.0), 1.0)
    f = cv.layer(CYAN, strength=0.28, core=False, glow=0.4)
    f.stroke([(20, 120), (44, 120)], 2.6)
    f2 = cv.layer(CYAN, strength=0.15, core=False, glow=0.3)
    f2.stroke([(20, 190), (44, 190)], 2.4)
    img = cv.render()
    save(img, os.path.join(out, "ground_top.png"))
    sheet.append(img)


def gen_rocks(out, sheet, seed=7):
    """Rochers « low poly » en aplats bleu-vert (facettes claires/sombres) + fin liseré cyan sur la crête."""
    rng = random.Random(seed)
    for k in range(4):
        w, h = 256, 384
        img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        from PIL import ImageDraw
        d = ImageDraw.Draw(img)
        n = rng.randint(5, 7)
        xs = sorted(rng.uniform(14, w - 14) for _ in range(n - 2))
        xs = [10] + xs + [w - 10]
        peak = rng.randint(1, n - 2)
        top = []
        for i, x in enumerate(xs):
            if i == 0 or i == n - 1:
                y = h - 6 - rng.uniform(0, 20)
            else:
                dist = abs(i - peak) / max(peak, n - 1 - peak)
                y = h - 40 - (h - 90) * (1 - dist * 0.85) * rng.uniform(0.72, 1.0)
            top.append((x, y))
        top[peak] = (top[peak][0], min(p[1] for p in top) - 6)
        base_c = (24, 66, 82)
        cx = xs[peak]
        d.polygon(top + [(w - 10, h), (10, h)], fill=base_c + (255,))
        bottom_mid = (cx, h)
        for i in range(n - 1):
            a, b = top[i], top[i + 1]
            slope = (b[1] - a[1]) / max(b[0] - a[0], 1)
            shade = max(-1.0, min(1.0, -slope / 1.4))
            col = tuple(int(c * (1 + 0.55 * shade)) for c in base_c)
            d.polygon([a, b, bottom_mid], fill=col + (255,))
        # liseré cyan sur la crête
        cv = Canvas(w, h)
        edge = cv.layer(CYAN, strength=0.32, core=False, glow=0.5)
        edge.stroke(top, 2.6)
        img.alpha_composite(cv.render())
        save(img, os.path.join(out, f"rock_{k}.png"))
        sheet.append(img)


def gen_bg(out, sheet):
    """Voile de fond : assombrit le haut (vignette), lueur turquoise vers l'horizon."""
    h = 256
    ys = np.linspace(0, 1, h)
    rgba = np.zeros((h, 4, 4))
    top_a = np.clip(0.42 * (1 - ys / 0.55), 0, 1) * (ys < 0.55)
    glow_a = 0.20 * np.exp(-((ys - 0.78) / 0.14) ** 2)
    rgba[..., 0] = np.where(top_a[:, None] > 0, 0.0, hex_rgb("#20c8d8")[0])
    rgba[..., 1] = np.where(top_a[:, None] > 0, 0.05, hex_rgb("#20c8d8")[1])
    rgba[..., 2] = np.where(top_a[:, None] > 0, 0.09, hex_rgb("#20c8d8")[2])
    rgba[..., 3] = np.clip(top_a + glow_a, 0, 1)[:, None]
    img = Image.fromarray((rgba * 255 + 0.5).astype(np.uint8), "RGBA")
    save(img, os.path.join(out, "bg_gradient.png"))
    sheet.append(img.resize((64, 256)))


def gen_spark(out, sheet):
    n = 48
    ys, xs = np.mgrid[0:n, 0:n]
    r = np.hypot(xs - (n - 1) / 2, ys - (n - 1) / 2) / (n / 2)
    a = np.clip(1 - r, 0, 1) ** 1.7
    core = np.clip(1 - r / 0.35, 0, 1)
    rgba = np.zeros((n, n, 4))
    rgba[..., :3] = 1.0
    rgba[..., 3] = np.clip(a * 0.9 + core, 0, 1)
    img = Image.fromarray((rgba * 255 + 0.5).astype(np.uint8), "RGBA")
    save(img, os.path.join(out, "spark.png"))
    sheet.append(img)


def heart_points(cx, cy, s):
    pts = []
    for i in range(72):
        t = i / 72 * math.tau
        x = 16 * math.sin(t) ** 3
        y = -(13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t))
        pts.append((cx + x * s, cy + y * s))
    return pts


def gen_heart(out, sheet):
    for name, color, strength, core in (("heart_on", MAGENTA, 1.0, True), ("heart_off", CYAN, 0.32, False)):
        cv = Canvas(96, 96)
        h = cv.layer(color, strength=strength, core=core)
        h.stroke(heart_points(48, 50, 1.9), 5.0, closed=True)
        img = cv.render()
        save(img, os.path.join(out, name + ".png"))
        sheet.append(img)


def gen_slash(out, sheet):
    """Croissant du coup de poing. Pivot : centre de l'arc (x=30, y=96 sur 224x192)."""
    cv = Canvas(224, 192)
    s = cv.layer(MAGENTA)
    R, t0 = 104.0, 30.0
    outer, inner = [], []
    n = 28
    for i in range(n + 1):
        a = -48 + 96 * i / n
        th = t0 * max(0.0, math.cos(math.radians(a) * 90 / 48)) ** 1.1
        outer.append((30 + math.cos(math.radians(a)) * (R + th / 2), 96 + math.sin(math.radians(a)) * (R + th / 2)))
        inner.append((30 + math.cos(math.radians(a)) * (R - th / 2), 96 + math.sin(math.radians(a)) * (R - th / 2)))
    s.poly_fill(outer + inner[::-1], core_scale=0.7)
    img = cv.render()
    save(img, os.path.join(out, "slash.png"))
    sheet.append(img)


GLYPHS = {
    "R": [[(0, 0), (0, 1), (0.46, 1), (0.62, 0.86), (0.62, 0.64), (0.46, 0.5), (0, 0.5)], [(0.3, 0.5), (0.62, 0)]],
    "Y": [[(0, 1), (0.31, 0.5), (0.62, 1)], [(0.31, 0.5), (0.31, 0)]],
    "T": [[(0, 1), (0.62, 1)], [(0.31, 1), (0.31, 0)]],
    "H": [[(0, 0), (0, 1)], [(0.62, 0), (0.62, 1)], [(0, 0.5), (0.62, 0.5)]],
    "M": [[(0, 0), (0, 1), (0.31, 0.55), (0.62, 1), (0.62, 0)]],
    "E": [[(0.62, 1), (0, 1), (0, 0), (0.62, 0)], [(0, 0.5), (0.46, 0.5)]],
    "U": [[(0, 1), (0, 0.15), (0.15, 0), (0.47, 0), (0.62, 0.15), (0.62, 1)]],
    "N": [[(0, 0), (0, 1), (0.62, 0), (0.62, 1)]],
}


def draw_text(layer, text, x, y, height, gap, width=9.0):
    for ch in text:
        if ch == " ":
            x += height * 0.55
            continue
        for stroke in GLYPHS[ch]:
            layer.stroke([(x + gx * height, y + (1 - gy) * height) for gx, gy in stroke], width)
        x += 0.62 * height + gap


def gen_logo(out, sheet):
    cv = Canvas(1152, 224)
    t = cv.layer(MAGENTA)
    draw_text(t, "RYTHME RUNNER", 48, 58, 104, 22, width=10.0)
    img = cv.render()
    save(img, os.path.join(out, "logo.png"))
    sheet.append(img)


def gen_thumbnail(out, sheet):
    """Jaquette carrée 512×512 de la borne (game/BuildExtras/thumbnail.png). Lit les rochers déjà générés dans `out`."""
    from PIL import ImageDraw
    n = 512
    bg = np.zeros((n, n, 3))
    top, bot = np.array(hex_rgb("#08202b")), np.array(hex_rgb("#164a5c"))
    t = np.linspace(0, 1, n)[:, None, None]
    bg[:] = top * (1 - t) + bot * t
    img = Image.fromarray((bg * 255).astype(np.uint8), "RGB").convert("RGBA")
    # rochers
    try:
        for k, (x, scale, tint) in enumerate(((-20, 0.95, 0.55), (250, 0.8, 0.7))):
            rock = Image.open(os.path.join(out, f"rock_{k}.png")).convert("RGBA")
            rock = rock.resize((int(rock.width * scale), int(rock.height * scale)), Image.LANCZOS)
            r, g, b, a = rock.split()
            rock = Image.merge("RGBA", (r.point(lambda v: int(v * tint)), g.point(lambda v: int(v * tint)),
                                        b.point(lambda v: int(v * tint)), a))
            img.alpha_composite(rock, (x, 410 - rock.height))
    except FileNotFoundError:
        sys.exit("thumbnail : lancer d'abord le générateur « rocks » (ou tout générer)")
    # sol
    cv = Canvas(n, n)
    ground = cv.layer(CYAN)
    ground.stroke([(-10, 412), (n + 10, 412)], 6.0)
    cv.fill(CYAN, vertical_gradient(n, n, 416, 500, 0.22, 0.0))
    img.alpha_composite(cv.render())
    # héros (grand, en pleine foulée)
    p, ph = run_frame(3, 12)
    hero = draw_hero(p, scarf_phase=-ph)
    k = 1.4
    hero = hero.resize((int(hero.width * k), int(hero.height * k)), Image.LANCZOS)
    img.alpha_composite(hero, (int(310 - HERO_CX * k), int(412 - HERO_GROUND * k)))
    # logo sur deux lignes
    cv = Canvas(n, n)
    t1 = cv.layer(MAGENTA)
    draw_text(t1, "RYTHME", 40, 30, 90, 18, width=9.5)
    draw_text(t1, "RUNNER", 40, 140, 90, 18, width=9.5)
    img.alpha_composite(cv.render())
    path = os.path.join(ROOT, "game", "BuildExtras", "thumbnail.png")
    img.convert("RGB").save(path, optimize=True)
    sheet.append(img.resize((256, 256)))


# ============================================================================
# main
# ============================================================================
GENERATORS = {
    "hero": gen_hero, "enemy": gen_enemy, "block": gen_block, "wall": gen_wall, "laser": gen_laser,
    "lum": gen_lum, "checkpoint": gen_checkpoint, "ground": gen_ground, "rocks": gen_rocks, "bg": gen_bg,
    "spark": gen_spark, "heart": gen_heart, "slash": gen_slash, "logo": gen_logo, "thumbnail": gen_thumbnail,
}


def contact_sheet(imgs, path, width=1800, bg=(11, 36, 48)):
    """Feuille de contact : les images à leur taille, rangées en lignes."""
    if not imgs:
        return
    x = y = row_h = 0
    pos = []
    for im in imgs:
        if x + im.width > width:
            x, y, row_h = 0, y + row_h + 8, 0
        pos.append((x, y))
        x += im.width + 8
        row_h = max(row_h, im.height)
    sheet = Image.new("RGBA", (width, y + row_h), bg + (255,))
    for im, p in zip(imgs, pos):
        sheet.alpha_composite(im, p)
    sheet.convert("RGB").save(path)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("out", nargs="?", default=DEFAULT_OUT)
    ap.add_argument("--only", default="")
    ap.add_argument("--sheet", default="")
    args = ap.parse_args()
    os.makedirs(args.out, exist_ok=True)
    names = [n for n in args.only.split(",") if n] or list(GENERATORS)
    unknown = [n for n in names if n not in GENERATORS]
    if unknown:
        sys.exit(f"générateur inconnu : {unknown} (dispo : {list(GENERATORS)})")
    sheet = []
    for n in names:
        GENERATORS[n](args.out, sheet)
        print("ok", n)
    if args.sheet:
        contact_sheet(sheet, args.sheet)


if __name__ == "__main__":
    main()
