"""Dessin des décors et du sol de chaque biome (les obstacles viennent de tools/gemini-obstacles/). Utilisé par gen-kenta-assets.py.

Les décors (ciel, calques en parallaxe, gardien) sont composés dans les coordonnées de l'écran 320×180 mais tracés
×BG_SCALE (640×360) : en vue large (640 px), leurs pixels ont la même taille que ceux du niveau et du héros."""
import math
import random

from PIL import Image, ImageDraw

from pixlib import Pix, banded_gradient, blob, col, mix, outlined, shade, tile_wrap

W_SCREEN, H_SCREEN, GROUND_Y = 320, 180, 132     # le sol de référence est à 48 px du bas (CameraRig.baseY)
STRIP_W = 640
BG_SCALE = 2                                      # décors tracés en 640×360 (calques : 1280×360)
# les palettes de kenta_biomes.py donnent les positions (soleil) pour l'ancien écran de 240×135 au sol à 115
SX, SY = W_SCREEN / 240, GROUND_Y / 115


def rng_for(biome, salt):
    return random.Random(f"{biome['id']}-{salt}")


# ============================================================================
# CIEL (320×180, fixe) : bandes, soleil / étoiles, nuages
# ============================================================================
def draw_sky(b, scale=BG_SCALE, sun=True):
    """Ciel fixe : bandes, astre, étoiles. Le gardien est un sprite à part (tools/gemini-gardiens/, ADR-028).
    `sun=False` : ciel du jeu, sans le soleil, qui est un sprite à part (draw_sun) que BiomeBackdrop couche au fil du
    morceau (ADR-041) ; la jaquette garde le soleil peint."""
    rng = rng_for(b, "sky")
    p = banded_gradient(W_SCREEN, H_SCREEN, b["sky"], 0, GROUND_Y, jitter=0.35, rng=rng, scale=scale)
    p.rect(0, GROUND_Y, W_SCREEN, H_SCREEN, b["sky"][-1])
    if sun and b["sun"]:
        c, x, y, r = b["sun"]
        x, y = round(x * SX), round(y * SY)
        p.circle(x, y, r + 5, mix(c, b["sky"][2], 0.7))
        p.circle(x, y, r + 2, mix(c, b["sky"][2], 0.4))
        p.circle(x, y, r, c)
    if b["stars"]:
        for _ in range(46):
            x, y = rng.randrange(W_SCREEN), rng.randrange(0, GROUND_Y - 20)
            p.px(x, y, b["stars"])
            if rng.random() < 0.18:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    p.px(x + dx, y + dy, mix(b["stars"], b["sky"][1], 0.5))
    return p


# ============================================================================
# ASTRES ET ÉCLAIRS (sprites à part, animés par BiomeBackdrop, ADR-041)
# ============================================================================
def sun_place(b):
    """Centre du soleil (coordonnées logiques 320×180) et rayon, ou None."""
    if not b["sun"]:
        return None
    _, x, y, r = b["sun"]
    return round(x * SX), round(y * SY), r


def draw_sun(b, scale=BG_SCALE):
    """Soleil seul (disque + deux halos translucides) : il glisse sur les bandes du ciel en se couchant, des halos
    opaques couleur de ciel s'y verraient. Toile carrée, centre du disque au centre."""
    c, _, _, r = b["sun"]
    h = r + 6
    p = Pix(2 * h, 2 * h, scale=scale)
    p.circle(h, h, r + 5, col(c, 70))
    p.circle(h, h, r + 2, col(c, 140))
    p.circle(h, h, r, c)
    return p


def sun_json(b, scale=BG_SCALE):
    """Place du soleil pour BiomeBackdrop : centre par rapport au centre du ciel (px du ciel 640×360, y vers le haut)
    et hauteur du centre à laquelle il est entièrement passé sous la ligne de sol."""
    x, y, r = sun_place(b)
    return dict(id=b["id"], x=(x - W_SCREEN / 2) * scale, y=(H_SCREEN / 2 - y) * scale,
                set=(H_SCREEN / 2 - GROUND_Y - r - 6) * scale)


def draw_moon(scale=BG_SCALE):
    """Croissant de lune (14×14 logiques) et son halo : se lève à la nuit dans les biomes qui ont un soleil."""
    p = Pix(16, 16, scale=scale)
    p.circle(8, 8, 7, col("#F4F0DC", 60))
    p.circle(8, 8, 5, "#F4F0DC")
    p.circle(7, 7, 2, "#DCD6BC")
    p.circle(10, 10, 1, "#DCD6BC")
    s = p.s
    p.d.ellipse([(10 - 5) * s, (6 - 5) * s, (10 + 5) * s, (6 + 5) * s], fill=(0, 0, 0, 0))   # la part dans l'ombre
    return p


def draw_night_stars(scale=BG_SCALE):
    """Étoiles de la nuit (ciel 320×180, transparent) : posées sur le ciel du jour, elles s'allument à la section fin."""
    rng = random.Random("night-stars")
    p = Pix(W_SCREEN, H_SCREEN, scale=scale)
    for _ in range(60):
        x, y = rng.randrange(W_SCREEN), rng.randrange(0, GROUND_Y - 24)
        p.px(x, y, "#FFF6DC")
        if rng.random() < 0.2:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                p.px(x + dx, y + dy, col("#FFF6DC", 120))
    return p


def draw_bolt(k, scale=BG_SCALE):
    """Éclair k (40×120 logiques) : zigzag blanc dans un liseré bleuté, une ou deux fourches. Montré une fraction de
    seconde dans le ciel de l'Orage, sur les temps (événement `lightning` de la chart)."""
    rng = random.Random(f"bolt-{k}")
    w, h = 40, 120
    p = Pix(w, h, scale=scale)
    pts, x, y = [], w // 2 + rng.randint(-6, 6), 0
    while y < h - 4:
        pts.append((x, y))
        y += rng.randint(8, 15)
        x = max(6, min(w - 6, x + rng.choice((-1, 1)) * rng.randint(3, 8)))
    pts.append((x, min(y, h - 2)))
    forks = []
    for i in rng.sample(range(1, len(pts) - 2), 2 if k != 1 else 1):
        fx, fy = pts[i]
        d = rng.choice((-1, 1))
        a = (fx + d * rng.randint(5, 9), fy + rng.randint(6, 10))
        forks.append([(fx, fy), a, (a[0] + d * rng.randint(2, 5), a[1] + rng.randint(5, 9))])
    glow, core = col("#A8B4F0", 200), "#FFFFFF"
    for f in forks:
        for a, b in zip(f, f[1:]):
            p.line(a, b, glow, width=1.5)
    for a, b in zip(pts, pts[1:]):
        p.line(a, b, glow, width=2.5)
    for f in forks:
        for a, b in zip(f, f[1:]):
            p.line(a, b, core, width=0.5)
    for a, b in zip(pts, pts[1:]):
        p.line(a, b, core, width=1.5)
    return p


# ============================================================================
# CALQUES EN PARALLAXE (640×180, boucle sans couture)
# ============================================================================

def dominant_color(p):
    """Couleur la plus présente juste au-dessus de la ligne de sol d'un calque (RGB), ou None s'il est vide là."""
    import numpy as np
    from collections import Counter
    s = p.s
    band = np.asarray(p.im)[max(0, (GROUND_Y - 8) * s):GROUND_Y * s].reshape(-1, 4)
    opaque = [tuple(int(v) for v in px[:3]) for px in band if px[3] >= 200]
    return Counter(opaque).most_common(1)[0][0] if opaque else None


def add_foot(p, b, depth, base=None):
    """Pied du calque sous sa ligne de sol (sous-bois, terre) jusqu'au bas de l'image : en jeu, BiomeBackdrop ancre
    les calques au sol du niveau, et sans pied on voyait un vide pâle sous les arbres dès que le sol était bas.
    `base` : couleur de départ (par défaut, la couleur dominante du calque juste au-dessus du sol), assombrie selon la
    profondeur (`depth` : 0 = loin, plus clair et proche du ciel ; 1 = proche, plus sombre). Deux tons, une arête
    claire ; la dernière rangée est unie : BiomeBackdrop l'étire pour prolonger le pied sous l'image."""
    base = base or dominant_color(p) or col(b["sky"][-1])[:3]
    air = 0.35 * (1 - depth)
    rim = mix(shade(base, -0.05), b["sky"][3], air)
    top = mix(shade(base, -0.25 - 0.2 * depth), b["sky"][3], air)
    deep = mix(shade(base, -0.4 - 0.2 * depth), b["sky"][3], air)
    p.rect(0, GROUND_Y, STRIP_W, GROUND_Y + 1, rim)
    p.rect(0, GROUND_Y + 1, STRIP_W, GROUND_Y + 7, top)
    p.rect(0, GROUND_Y + 7, STRIP_W, H_SCREEN, deep)


def pine(p, x, base, h, c, snow=None):
    w = int(h * 0.62)
    tiers = 3
    p.rect(x - 1, base - h // 6, x + 2, base, mix(c["dark"], "#000000", 0.4))
    for i in range(tiers):
        top = base - h + i * (h // 4)
        bot = base - h // 6 - (tiers - 1 - i) * (h // 6)
        half = int(w / 2 * (0.55 + 0.25 * i))
        p.poly([(x, top), (x - half, bot), (x + half, bot)], c["dark"])
        p.poly([(x, top), (x - half, bot), (x, bot)], c["mid"])
        p.poly([(x, top + 2), (x - half // 2, bot - 2), (x - half // 3, bot - 2)], c["light"])
        if snow:
            p.poly([(x, top), (x - half // 2, top + (bot - top) // 2), (x + half // 2, top + (bot - top) // 2)], snow)


def draw_far(b, scale=BG_SCALE, foot=True):
    rng = rng_for(b, "far")
    p = Pix(STRIP_W, H_SCREEN, scale=scale)
    f = b["far"]
    k = f["kind"]
    if k in ("mountains", "icepeaks"):
        n = 6
        for i in range(n):
            x = int(i * STRIP_W / n + rng.randint(-14, 14))
            h = rng.randint(40, 62) + (14 if k == "icepeaks" else 0)
            w = rng.randint(80, 120)

            def peak(x0, h=h, w=w):
                p.poly([(x0, GROUND_Y - h), (x0 - w // 2, GROUND_Y), (x0 + w // 2, GROUND_Y)], f["body"])
                p.poly([(x0, GROUND_Y - h), (x0 - w // 2, GROUND_Y), (x0 - w // 8, GROUND_Y)], f["shade"])
                cap = int(h * 0.3)
                if f["snow"]:
                    p.poly([(x0, GROUND_Y - h), (x0 - int(w * 0.5 * 0.3), GROUND_Y - h + cap),
                            (x0 - 4, GROUND_Y - h + cap - 3), (x0, GROUND_Y - h + cap + 2),
                            (x0 + 5, GROUND_Y - h + cap - 3), (x0 + int(w * 0.5 * 0.3), GROUND_Y - h + cap)], f["snow"])
            tile_wrap(p, x, peak)
    elif k == "treeline":
        for i in range(34):
            x = int(i * STRIP_W / 34 + rng.randint(-5, 5))
            h = rng.randint(34, 60)

            def tree(x0, h=h):
                p.rect(x0 - 1, GROUND_Y - h, x0 + 1, GROUND_Y, f["shade"])
                p.ellipse(x0, GROUND_Y - h, rng.randint(7, 11), rng.randint(5, 8), f["body"])
            tile_wrap(p, x, tree)
        for i in range(10):
            x = i * 48 + rng.randint(0, 20)
            hh = rng.randint(10, 18)
            tile_wrap(p, x, lambda x0, hh=hh: (p.rect(x0, GROUND_Y - hh, x0 + 1, GROUND_Y, f["shade"]), p.rect(x0 - 1, GROUND_Y - hh - 3, x0 + 2, GROUND_Y - hh + 1, "#6A5A4A")))
    elif k == "cave":
        for i in range(30):
            x = int(i * STRIP_W / 30 + rng.randint(-4, 4))
            h = rng.randint(14, 56)
            w = rng.randint(8, 16)

            def stal(x0, h=h, w=w):
                p.poly([(x0 - w // 2, 0), (x0 + w // 2, 0), (x0, h)], f["body"])
                p.poly([(x0 - w // 2, 0), (x0, 0), (x0, h)], f["shade"])
            tile_wrap(p, x, stal)
        for i in range(8):
            x = int(i * STRIP_W / 8 + rng.randint(-6, 6))
            h = rng.randint(38, 70)
            tile_wrap(p, x, lambda x0, h=h: (p.poly([(x0 - 5, GROUND_Y), (x0 - 5, GROUND_Y - h + 8), (x0, GROUND_Y - h), (x0 + 5, GROUND_Y - h + 8), (x0 + 5, GROUND_Y)], f["body"]),
                                              p.poly([(x0 - 5, GROUND_Y), (x0 - 5, GROUND_Y - h + 8), (x0, GROUND_Y - h), (x0, GROUND_Y)], f["shade"])))
    elif k == "volcano":
        for cx, h, w in ((120, 74, 190), (380, 58, 150)):
            def volc(x0, h=h, w=w):
                p.poly([(x0 - w // 2, GROUND_Y), (x0 - 16, GROUND_Y - h), (x0 + 16, GROUND_Y - h), (x0 + w // 2, GROUND_Y)], f["body"])
                p.poly([(x0 - w // 2, GROUND_Y), (x0 - 16, GROUND_Y - h), (x0 - 2, GROUND_Y - h), (x0 - 6, GROUND_Y)], f["shade"])
                for dx in (-10, 4, 14):
                    p.line((x0 + dx, GROUND_Y - h + 2), (x0 + dx * 2, GROUND_Y - h + 26), f["snow"], 2)
                p.rect(x0 - 16, GROUND_Y - h - 1, x0 + 17, GROUND_Y - h + 2, f["snow"])
                blob(p, x0 + 6, GROUND_Y - h - 18, 22, 12, b["clouds"], hi=mix(b["clouds"], "#FFFFFF", 0.1), lo=b["cloud_shadow"], rng=rng)
            tile_wrap(p, cx, volc)
    elif k == "ruins":
        x = 0
        while x < STRIP_W:
            w = rng.randint(14, 30)
            h = rng.randint(22, 64)
            broken = rng.random() < 0.4
            x0 = x

            def bld(xx, w=w, h=h, broken=broken):
                p.rect(xx, GROUND_Y - h, xx + w, GROUND_Y, f["body"])
                p.rect(xx, GROUND_Y - h, xx + w // 3, GROUND_Y, f["shade"])
                if broken:
                    p.poly([(xx + w // 2, GROUND_Y - h), (xx + w, GROUND_Y - h), (xx + w, GROUND_Y - h + 9)], b["sky"][4])
                for wy in range(GROUND_Y - h + 7, GROUND_Y - 6, 12):
                    p.rect(xx + w // 2 - 1, wy, xx + w // 2 + 2, wy + 4, f["snow"])
            tile_wrap(p, x0, bld)
            x += w + rng.randint(0, 6)
    elif k == "stormclouds":
        for i in range(14):
            x = int(i * STRIP_W / 14 + rng.randint(-8, 8))
            y = rng.randint(60, 108)
            rx = rng.randint(34, 56)
            tile_wrap(p, x, lambda x0, y=y, rx=rx: blob(p, x0, y, rx, rx // 2, f["body"], hi=f["snow"], lo=f["shade"], rng=rng))
        for x in (90, 330):
            tile_wrap(p, x, lambda x0: p.poly([(x0, 0), (x0 - 7, 36), (x0 - 1, 36), (x0 - 9, 74), (x0 + 6, 28), (x0, 28)], "#FFFFFF"))
    if foot:
        add_foot(p, b, 0.0)
    return p


def draw_mid(b, scale=BG_SCALE, foot=True):
    rng = rng_for(b, "mid")
    p = Pix(STRIP_W, H_SCREEN, scale=scale)
    m = b["mid"]
    k = m["kind"]
    n = m["count"]
    for i in range(n):
        x = int(i * STRIP_W / n + rng.randint(-8, 8))
        h = rng.randint(28, 58)
        if k in ("pines", "frostpines"):
            tile_wrap(p, x, lambda x0, h=h: pine(p, x0, GROUND_Y, h, m, snow="#FFFFFF" if k == "frostpines" else None))
        elif k == "swamp":
            def tree(x0, h=h):
                p.rect(x0 - 1, GROUND_Y - h, x0 + 2, GROUND_Y, m["dark"])
                blob(p, x0, GROUND_Y - h, 15, 8, m["mid"], hi=m["light"], lo=m["dark"], rng=rng)
                for d in range(-12, 13, 5):
                    hh = rng.randint(6, 18)
                    p.rect(x0 + d, GROUND_Y - h + 5, x0 + d + 1, GROUND_Y - h + 5 + hh, m["mid"])
            tile_wrap(p, x, tree)
            tile_wrap(p, x + 20, lambda x0: [p.rect(x0 + d, GROUND_Y - rng.randint(6, 14), x0 + d + 1, GROUND_Y, m["light"]) for d in (0, 3, 6)])
        elif k == "crystals":
            def cl(x0, h=h):
                for dx, hh in ((-6, int(h * 0.7)), (0, h), (6, int(h * 0.55))):
                    wv = 4
                    p.poly([(x0 + dx - wv, GROUND_Y), (x0 + dx - wv, GROUND_Y - hh + 6), (x0 + dx, GROUND_Y - hh), (x0 + dx + wv, GROUND_Y - hh + 6), (x0 + dx + wv, GROUND_Y)], m["mid"])
                    p.poly([(x0 + dx - wv, GROUND_Y), (x0 + dx - wv, GROUND_Y - hh + 6), (x0 + dx, GROUND_Y - hh), (x0 + dx, GROUND_Y)], m["light"])
                    p.poly([(x0 + dx + 1, GROUND_Y), (x0 + dx + 1, GROUND_Y - hh + 3), (x0 + dx + wv, GROUND_Y - hh + 6), (x0 + dx + wv, GROUND_Y)], m["dark"])
            tile_wrap(p, x, cl)
        elif k == "lavarocks":
            def rk(x0, h=h):
                hh = int(h * 0.7)
                p.poly([(x0 - 22, GROUND_Y), (x0 - 12, GROUND_Y - hh), (x0 + 4, GROUND_Y - hh - 6), (x0 + 16, GROUND_Y - hh + 8), (x0 + 24, GROUND_Y)], m["mid"])
                p.poly([(x0 - 22, GROUND_Y), (x0 - 12, GROUND_Y - hh), (x0 - 3, GROUND_Y - hh + 4), (x0 - 4, GROUND_Y)], m["light"])
                p.line((x0, GROUND_Y - hh + 2), (x0 + 5, GROUND_Y - 6), "#FF7A1A", 1)
                p.line((x0 - 9, GROUND_Y - hh + 10), (x0 - 12, GROUND_Y - 4), "#D8401A", 1)
            tile_wrap(p, x, rk)
        elif k == "autumn":
            def tr(x0, h=h):
                p.rect(x0 - 2, GROUND_Y - h, x0 + 2, GROUND_Y, "#6B4A32")
                blob(p, x0, GROUND_Y - h - 4, 18, 11, m["mid"], hi=m["light"], lo=m["dark"], rng=rng)
            tile_wrap(p, x, tr)
        elif k == "columns":
            def col_(x0, h=h):
                broken = rng.random() < 0.4
                hh = h if not broken else int(h * 0.55)
                p.rect(x0 - 5, GROUND_Y - hh, x0 + 5, GROUND_Y, m["mid"])
                p.rect(x0 - 5, GROUND_Y - hh, x0 - 1, GROUND_Y, m["light"])
                p.rect(x0 + 3, GROUND_Y - hh, x0 + 5, GROUND_Y, m["dark"])
                p.rect(x0 - 8, GROUND_Y - hh - 3, x0 + 8, GROUND_Y - hh, m["light"])
                p.rect(x0 - 8, GROUND_Y - 4, x0 + 8, GROUND_Y, m["dark"])
            tile_wrap(p, x, col_)
        elif k == "cloudpuffs":
            tile_wrap(p, x, lambda x0, h=h: blob(p, x0, GROUND_Y - 4 - h // 3, 28, 14, m["mid"], hi=m["light"], lo=m["dark"], rng=rng))
    if foot:
        add_foot(p, b, 0.5)
    return p


def draw_near(b, scale=BG_SCALE, foot=True):
    """Calque proche : trois motifs (grand arbre, petit arbre, buissons) répartis sur la largeur. Avant : un seul grand
    arbre, très gros et répété à l'identique, qui finissait par se voir (audit du 2026-11-25)."""
    rng = rng_for(b, "near")
    p = Pix(STRIP_W, H_SCREEN, scale=scale)
    n = b["near"]

    def tree(x0, h, rx, ry, lumps):
        p.rect(x0 - 3, GROUND_Y - h, x0 + 3, GROUND_Y, n["trunk"])
        p.rect(x0 - 3, GROUND_Y - h, x0 - 1, GROUND_Y, shade(n["trunk"], 0.18))
        blob(p, x0, GROUND_Y - h, rx, ry, n["crown"], hi=n["hi"], lo=n["lo"], rng=rng, lumps=lumps)
        blob(p, x0 - rx // 2 - 2, GROUND_Y - h + ry // 2 + 2, rx // 2, ry // 2, n["crown"], hi=n["hi"], lo=n["lo"], rng=rng)

    def bush(x0, rx):
        blob(p, x0, GROUND_Y - rx // 2, rx, rx // 2 + 2, n["crown"], hi=n["hi"], lo=n["lo"], rng=rng, lumps=4)
        blob(p, x0 + rx, GROUND_Y - rx // 3, rx // 2 + 2, rx // 3 + 2, n["crown"], hi=n["hi"], lo=n["lo"], rng=rng)

    motifs = [
        (55, lambda x0: tree(x0, rng.randint(58, 68), 26, 17, 5)),        # grand arbre
        (200, lambda x0: bush(x0, rng.randint(13, 17))),                  # buissons
        (300, lambda x0: tree(x0, rng.randint(36, 44), 16, 11, 4)),       # petit arbre
        (455, lambda x0: tree(x0, rng.randint(52, 62), 22, 15, 5)),       # autre grand arbre
        (575, lambda x0: bush(x0, rng.randint(10, 14))),
    ]
    for x, draw in motifs:
        tile_wrap(p, x, draw)
    if foot:                                     # sous les arbres proches : le sol de la forêt (couleur du calque milieu)
        add_foot(p, b, 1.0, base=dominant_color(draw_mid(b, scale, foot=False)))
    return p


# ============================================================================
# SOL ET TROU (tuiles 32×20 : herbe, terre, reflet de l'eau)
# ============================================================================
def draw_ground(b):
    g = b["ground"]
    rng = rng_for(b, "ground")
    p = Pix(32, 20)
    p.rect(0, 0, 32, 20, g["water"])
    for y in range(7, 20):
        if y % 3 == 0:
            p.rect(0, y, 32, y + 1, g["water2"])
    for dx, dy, ln in ((3, 10, 7), (18, 12, 9), (8, 15, 6), (22, 17, 8), (26, 9, 4)):
        for i in range(ln):
            p.px((dx + i) % 32, dy, g["dash"])
    p.rect(0, 2, 32, 7, g["dirt"])
    for _ in range(14):
        p.px(rng.randrange(32), rng.randrange(2, 7), g["dirt2"])
    p.rect(0, 6, 32, 7, g["dirt2"])
    p.rect(0, 0, 32, 3, g["grass"])
    p.rect(0, 2, 32, 3, g["grass2"])
    for _ in range(8):
        p.px(rng.randrange(32), 0, shade(g["grass"], 0.3))
    for x in range(0, 32, 4):
        if rng.random() < 0.6:
            p.px(x + rng.randrange(3), 3, g["grass2"])
    return p


def draw_fill(b):
    """Flanc d'un sol surélevé (marche) : 32×32, terre en briques irrégulières, se répète en hauteur et en largeur."""
    g = b["ground"]
    rng = rng_for(b, "fill")
    p = Pix(32, 32)
    p.rect(0, 0, 32, 32, g["dirt"])
    for row in range(0, 32, 8):
        off = 0 if (row // 8) % 2 == 0 else 8
        p.rect(0, row + 7, 32, row + 8, g["dirt2"])
        for x in range(off, 32 + 16, 16):
            p.rect((x - 1) % 32, row, (x - 1) % 32 + 1, row + 7, g["dirt2"])
    for _ in range(18):
        p.px(rng.randrange(32), rng.randrange(32), shade(g["dirt"], 0.12))
    return p


def draw_fill_deep(b):
    """Flanc en profondeur (sous ~2 u) : la même terre, un peu plus sombre et noyée dans l'air du biome, avec des pierres
    enfouies. Les piliers s'effacent vers le bas au lieu de faire de grandes masses sombres (audit du 2026-11-25)."""
    g = b["ground"]
    rng = rng_for(b, "filldeep")
    p = draw_fill(b)
    a = p.array()
    air = col(b["sky"][3])[:3]
    for i in range(3):
        a[..., i] = (a[..., i] * 0.62 + air[i] * 0.22).clip(0, 255).astype(a.dtype)
    p.im = Image.fromarray(a, "RGBA")
    p.d = ImageDraw.Draw(p.im)
    for _ in range(3):
        x, y = rng.randrange(3, 29), rng.randrange(3, 29)
        p.ellipse(x, y, 2, 1, shade(g["dirt2"], -0.2))
    return p


def draw_deco(b, k):
    """Détails semés sur le flanc des piliers (pierre, racine, fossile, cailloux) : contour d'1 px, couleurs de la terre."""
    g = b["ground"]
    rng = rng_for(b, f"deco{k}")
    stone = mix(g["dirt2"], "#C8C0B0", 0.45)
    if k == 0:                                   # grosse pierre
        p = Pix(14, 10)
        p.ellipse(7, 5, 6, 4, stone)
        p.ellipse(6, 4, 4, 2, shade(stone, 0.18))
        p.px(9, 6, shade(stone, -0.25))
    elif k == 1:                                 # racine qui pend
        p = Pix(12, 16)
        x = 6
        for y in range(16):
            x += rng.choice((-1, 0, 0, 1))
            x = max(2, min(9, x))
            p.rect(x, y, x + 2, y + 1, shade(g["dirt2"], -0.15))
            if y % 5 == 3:
                p.px(x + 2, y + 1, g["dirt2"])
    elif k == 2:                                 # fossile en spirale
        p = Pix(12, 12)
        p.circle(6, 6, 5, stone)
        p.circle(6, 6, 3, shade(stone, -0.2))
        p.circle(6, 6, 2, stone)
        p.px(6, 6, shade(stone, -0.3))
    else:                                        # cailloux
        p = Pix(14, 8)
        for cx, cy, r in ((3, 4, 2), (8, 5, 2), (11, 3, 1)):
            p.circle(cx, cy, r, stone)
            p.px(cx - 1, cy - 1, shade(stone, 0.25))
    return outlined(p, color=shade(g["dirt"], -0.55))


def draw_cliff_top(b):
    """Dessus d'un sol surélevé : 32×8, herbe puis terre (même tête que la tuile de sol, sans la rivière)."""
    g = b["ground"]
    rng = rng_for(b, "cliff")
    p = Pix(32, 8)
    p.rect(0, 0, 32, 8, g["dirt"])
    for _ in range(10):
        p.px(rng.randrange(32), rng.randrange(3, 7), g["dirt2"])
    p.rect(0, 7, 32, 8, g["dirt2"])
    p.rect(0, 0, 32, 3, g["grass"])
    p.rect(0, 2, 32, 3, g["grass2"])
    for x in range(0, 32, 4):
        if rng.random() < 0.6:
            p.px(x + rng.randrange(3), 3, g["grass2"])
    return p


def draw_hazard(b):
    h = b["hazard"]
    rng = rng_for(b, "hazard")
    p = Pix(32, 20)
    p.rect(0, 0, 32, 20, h["base"])
    for dx, dy, ln in ((2, 3, 9), (16, 7, 12), (6, 11, 7), (20, 14, 9), (10, 17, 8)):
        for i in range(ln):
            p.px((dx + i) % 32, dy, h["ring"])
    if h["glow"]:
        p.rect(0, 0, 32, 2, shade(h["glow"], 0.35))
        for x in range(0, 32, 5):
            p.px(x + rng.randrange(3), 3, shade(h["glow"], 0.25))
    p.rect(0, 0, 32, 1, h["ring"])
    return p
