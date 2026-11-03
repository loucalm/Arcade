"""Dessin des décors, du sol et des obstacles de chaque biome (pixel art 240×135). Utilisé par gen-kenta-assets.py."""
import math
import random

from pixlib import Pix, banded_gradient, blob, col, mix, outlined, shade, tile_wrap

W_SCREEN, H_SCREEN, GROUND_Y = 240, 135, 115
STRIP_W = 480


def rng_for(biome, salt):
    return random.Random(f"{biome['id']}-{salt}")


# ============================================================================
# CIEL (240×135, fixe) : bandes, soleil / étoiles, nuages, gardien
# ============================================================================
def draw_sky(b):
    rng = rng_for(b, "sky")
    p = banded_gradient(W_SCREEN, H_SCREEN, b["sky"], 0, GROUND_Y, jitter=0.35, rng=rng)
    p.rect(0, GROUND_Y, W_SCREEN, H_SCREEN, b["sky"][-1])
    if b["sun"]:
        c, x, y, r = b["sun"]
        p.circle(x, y, r + 5, mix(c, b["sky"][2], 0.7))
        p.circle(x, y, r + 2, mix(c, b["sky"][2], 0.4))
        p.circle(x, y, r, c)
    if b["stars"]:
        for _ in range(46):
            x, y = rng.randrange(W_SCREEN), rng.randrange(0, 95)
            p.px(x, y, b["stars"])
            if rng.random() < 0.18:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    p.px(x + dx, y + dy, mix(b["stars"], b["sky"][1], 0.5))
    draw_statue(p, b, 172, GROUND_Y)
    if b["clouds"] and b["id"] not in ("forge", "echine", "givre", "cite", "orage"):
        pass
    return p


def draw_statue(p, b, cx, base):
    """Gardien : énorme statue endormie, teintée de l'air du biome (très peu contrastée, pour ne pas gêner la lecture)."""
    s = b["statue"]
    sky = b["sky"][3]
    body = mix(s["body"], sky, 0.18)
    line = mix(s["line"], sky, 0.1)
    kind = s["kind"]
    hx, hy = cx, base - 54          # tête
    # corps assis puis tête
    p.ellipse(cx, base - 22, 38, 26, body)
    p.ellipse(cx, base - 10, 42, 14, body)
    # pattes
    p.ellipse(cx - 22, base - 4, 14, 8, mix(body, line, 0.3))
    p.ellipse(cx + 22, base - 4, 14, 8, mix(body, line, 0.3))
    # oreilles / cornes / crêtes derrière la tête
    ac = line
    if kind == "deer":
        for sgn in (-1, 1):
            x0 = hx + sgn * 16
            pts = [(x0, hy - 18), (x0 + sgn * 10, hy - 34), (x0 + sgn * 8, hy - 52), (x0 + sgn * 22, hy - 62)]
            for a, c2 in zip(pts, pts[1:]):
                p.line(a, c2, ac, 3)
            p.line((x0 + sgn * 9, hy - 36), (x0 + sgn * 24, hy - 40), ac, 2)
            p.line((x0 + sgn * 8, hy - 50), (x0 - sgn * 4, hy - 60), ac, 2)
        p.ellipse(hx - 12, hy - 20, 9, 3, s["moss"])
    elif kind == "frog":
        for sgn in (-1, 1):
            p.circle(hx + sgn * 19, hy - 22, 11, body)
            p.line((hx + sgn * 14, hy - 22), (hx + sgn * 24, hy - 22), line, 2)
    elif kind == "bat":
        for sgn in (-1, 1):
            p.poly([(hx + sgn * 10, hy - 18), (hx + sgn * 26, hy - 58), (hx + sgn * 30, hy - 14)], body)
            p.poly([(hx + sgn * 30, hy - 8), (hx + sgn * 62, hy - 20), (hx + sgn * 40, hy + 14)], mix(body, line, 0.35))
    elif kind == "bull":
        for sgn in (-1, 1):
            pts = [(hx + sgn * 22, hy - 14), (hx + sgn * 36, hy - 16), (hx + sgn * 42, hy - 32), (hx + sgn * 38, hy - 46)]
            for a, c2 in zip(pts, pts[1:]):
                p.line(a, c2, ac, 4)
        p.rect(hx - 6, hy + 8, hx + 6, hy + 11, s["moss"])
    elif kind in ("cat", "fox"):
        tall = 44 if kind == "fox" else 30
        for sgn in (-1, 1):
            p.poly([(hx + sgn * 8, hy - 16), (hx + sgn * 24, hy - 16 - tall), (hx + sgn * 30, hy - 8)], body)
            p.poly([(hx + sgn * 14, hy - 18), (hx + sgn * 24, hy - 12 - tall), (hx + sgn * 27, hy - 10)], mix(body, line, 0.45))
        if kind == "fox":
            p.ellipse(cx + 48, base - 14, 18, 8, mix(body, line, 0.25))
    elif kind == "rabbit":
        for sgn in (-1, 1):
            p.ellipse(hx + sgn * 14, hy - 44, 8, 30, body)
            p.ellipse(hx + sgn * 14, hy - 44, 4, 24, mix(body, line, 0.4))
    elif kind == "bird":
        for i, dx in enumerate((-16, -6, 4, 14)):
            p.poly([(hx + dx - 5, hy - 18), (hx + dx, hy - 40 - (i % 2) * 8), (hx + dx + 5, hy - 18)], body)
    p.ellipse(hx, hy, 33, 26, body)
    # visage endormi : yeux clos, petite bouche
    p.line((hx - 20, hy - 2), (hx - 8, hy - 2), line, 2)
    p.line((hx + 8, hy - 2), (hx + 20, hy - 2), line, 2)
    p.rect(hx - 2, hy + 10, hx + 3, hy + 12, line)
    if kind == "bird":
        p.poly([(hx - 6, hy + 4), (hx + 6, hy + 4), (hx, hy + 16)], s["moss"])
    if kind == "frog":
        p.rect(hx - 14, hy + 9, hx + 15, hy + 11, line)
    if kind == "bat":
        p.rect(hx - 3, hy + 8, hx - 1, hy + 12, "#E8EEFF")
        p.rect(hx + 2, hy + 8, hx + 4, hy + 12, "#E8EEFF")


# ============================================================================
# CALQUES EN PARALLAXE (480×135, boucle sans couture)
# ============================================================================
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


def draw_far(b):
    rng = rng_for(b, "far")
    p = Pix(STRIP_W, H_SCREEN)
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
    return p


def draw_mid(b):
    rng = rng_for(b, "mid")
    p = Pix(STRIP_W, H_SCREEN)
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
    return p


def draw_near(b):
    rng = rng_for(b, "near")
    p = Pix(STRIP_W, H_SCREEN)
    n = b["near"]
    for x in (40, 290):
        h = rng.randint(66, 80)

        def tree(x0, h=h):
            p.rect(x0 - 4, GROUND_Y - h, x0 + 4, GROUND_Y, n["trunk"])
            p.rect(x0 - 4, GROUND_Y - h, x0 - 1, GROUND_Y, shade(n["trunk"], 0.18))
            blob(p, x0, GROUND_Y - h, 34, 22, n["crown"], hi=n["hi"], lo=n["lo"], rng=rng, lumps=6)
            blob(p, x0 - 18, GROUND_Y - h + 14, 14, 9, n["crown"], hi=n["hi"], lo=n["lo"], rng=rng)
            blob(p, x0 + 22, GROUND_Y - h + 10, 14, 9, n["crown"], hi=n["hi"], lo=n["lo"], rng=rng)
        tile_wrap(p, x, tree)
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


# ============================================================================
# OBSTACLES : mur bas 36×32, bloc 38×38, plafond (tuile 16×16 + poteau 8×16)
# ============================================================================
def finish(p):
    return outlined(p)


def draw_wall(b):
    w = b["wall"]
    k = w["kind"]
    rng = rng_for(b, "wall")
    p = Pix(36, 32)
    c1, c2, c3 = w["c1"], w["c2"], w["c3"]
    if k in ("stump", "mossy"):
        p.rect(4, 8, 32, 31, c1)
        p.rect(4, 8, 12, 31, shade(c1, 0.15))
        p.rect(26, 8, 32, 31, c2)
        for x in (10, 17, 22):
            p.line((x, 12), (x, 30), c2, 1)
        p.ellipse(18, 9, 14, 5, c3)
        p.ellipse(18, 9, 10, 3, shade(c3, 0.15))
        p.ellipse(18, 9, 5, 1, mix(c3, c2, 0.5))
        p.rect(4, 29, 32, 32, c2)
        if w["moss"]:
            p.ellipse(10, 7, 6, 3, w["moss"])
            p.rect(6, 9, 11, 14 if k == "stump" else 20, w["moss"])
            if k == "mossy":
                p.rect(24, 10, 30, 22, w["moss"])
    elif k in ("rock", "cloud"):
        if k == "rock":
            pts = [(2, 31), (4, 16), (11, 6), (22, 3), (31, 10), (34, 22), (33, 31)]
            p.poly(pts, c1)
            p.poly([(2, 31), (4, 16), (11, 6), (16, 8), (14, 31)], shade(c1, 0.18))
            p.poly([(26, 12), (31, 10), (34, 22), (33, 31), (27, 31)], c2)
            p.line((18, 10), (22, 20), c2, 1)
            p.line((22, 20), (20, 28), c2, 1)
            if w["moss"]:
                p.ellipse(15, 5, 7, 2, w["moss"])
        else:
            blob(p, 18, 20, 16, 11, c1, hi=c3, lo=c2, rng=rng, lumps=6)
            p.rect(2, 29, 34, 32, c2)
    elif k == "crystal":
        for x0, hh, ww in ((3, 20, 8), (12, 31, 10), (23, 24, 9)):
            p.poly([(x0, 31), (x0, 31 - hh + 7), (x0 + ww // 2, 31 - hh), (x0 + ww, 31 - hh + 7), (x0 + ww, 31)], c1)
            p.poly([(x0, 31), (x0, 31 - hh + 7), (x0 + ww // 2, 31 - hh), (x0 + ww // 2, 31)], c3)
            p.poly([(x0 + ww // 2 + 1, 31), (x0 + ww // 2 + 1, 31 - hh + 2), (x0 + ww, 31 - hh + 7), (x0 + ww, 31)], c2)
    elif k == "basalt":
        for x0, hh in ((2, 22), (13, 31), (24, 26)):
            p.rect(x0, 31 - hh, x0 + 10, 31, c1)
            p.rect(x0, 31 - hh, x0 + 3, 31, shade(c1, 0.2))
            p.rect(x0 + 8, 31 - hh, x0 + 10, 31, c2)
            p.line((x0 + 5, 31 - hh + 3), (x0 + 6, 28), c3, 1)
            p.rect(x0, 31 - hh, x0 + 10, 31 - hh + 1, shade(c1, 0.3))
    elif k == "ice":
        p.poly([(2, 31), (2, 14), (10, 6), (24, 3), (34, 12), (34, 31)], c1)
        p.poly([(2, 31), (2, 14), (10, 6), (14, 8), (9, 31)], shade(c1, 0.35))
        p.poly([(26, 10), (34, 12), (34, 31), (26, 31)], c2)
        p.line((8, 12), (8, 22), c3, 1)
        p.line((17, 8), (22, 8), c3, 1)
        p.rect(2, 28, 34, 31, c2)
    elif k == "column":
        p.rect(2, 12, 34, 31, c1)
        p.rect(2, 12, 34, 17, c3)
        p.rect(2, 27, 34, 31, c2)
        p.rect(0, 9, 7, 31, c3)
        p.rect(0, 9, 7, 12, shade(c3, 0.2))
        p.rect(28, 9, 36, 31, c2)
        for x in (12, 19, 26):
            p.line((x, 14), (x, 28), c2, 1)
        p.ellipse(4, 20, 4, 11, c1)
    return finish(p)


def draw_block(b):
    bl = b["block"]
    k = bl["kind"]
    rng = rng_for(b, "block")
    p = Pix(38, 38)
    c1, c2, c3 = bl["c1"], bl["c2"], bl["c3"]
    if k == "barrel":
        p.ellipse(19, 21, 17, 16, c1)
        p.rect(2, 8, 36, 36, c1)
        p.rect(2, 8, 10, 36, shade(c1, 0.14))
        p.rect(29, 8, 36, 36, c2)
        for y in (13, 29):
            p.rect(1, y, 37, y + 3, c2)
            p.rect(1, y, 37, y + 1, c3)
        p.ellipse(19, 7, 15, 4, c3)
        p.ellipse(19, 7, 11, 2, shade(c3, 0.2))
    elif k in ("geode", "molten", "cracked", "statuebox"):
        p.poly([(2, 36), (2, 12), (8, 3), (30, 3), (36, 12), (36, 36)], c1)
        p.poly([(2, 36), (2, 12), (8, 3), (14, 4), (10, 36)], shade(c1, 0.15))
        p.poly([(28, 4), (36, 12), (36, 36), (28, 36)], c2)
        if k == "geode":
            p.ellipse(19, 21, 9, 10, c2)
            for dx, hh in ((-5, 8), (0, 11), (5, 7)):
                p.poly([(19 + dx - 2, 30), (19 + dx - 2, 30 - hh + 3), (19 + dx, 30 - hh), (19 + dx + 2, 30 - hh + 3), (19 + dx + 2, 30)], c3)
        elif k == "molten":
            for pts in (((10, 8), (15, 18), (13, 28)), ((26, 10), (22, 20), (25, 32)), ((18, 14), (19, 24), (16, 34))):
                p.line(pts[0], pts[1], c3, 2)
                p.line(pts[1], pts[2], c3, 2)
        elif k == "cracked":
            p.line((10, 6), (16, 17), c2, 2)
            p.line((16, 17), (13, 30), c2, 2)
            p.circle(25, 22, 4, c3)
            p.circle(24, 21, 1, shade(c3, 0.5))
        else:
            p.rect(9, 12, 29, 24, c2)
            p.line((13, 16), (17, 16), c3, 2)
            p.line((21, 16), (25, 16), c3, 2)
            p.line((15, 21), (23, 21), c3, 1)
    elif k in ("iceblock",):
        p.poly([(2, 36), (2, 8), (8, 3), (32, 3), (36, 8), (36, 36)], c1)
        p.poly([(2, 36), (2, 8), (8, 3), (14, 4), (10, 36)], c3)
        p.poly([(28, 4), (36, 8), (36, 36), (28, 36)], c2)
        p.line((16, 10), (16, 28), c3, 1)
        p.line((22, 8), (30, 8), c3, 1)
    elif k == "stormcloud":
        blob(p, 19, 22, 17, 13, c1, hi=shade(c1, 0.25), lo=c2, rng=rng, lumps=6)
        p.poly([(18, 16), (13, 26), (19, 26), (15, 36), (25, 22), (19, 22)], c3)
    return finish(p)


def draw_ceil(b):
    c = b["ceil"]
    k = c["kind"]
    rng = rng_for(b, "ceil")
    p = Pix(16, 16)
    c1, c2, c3 = c["c1"], c["c2"], c["c3"]
    if k == "leafy":
        p.rect(0, 0, 16, 5, "#5A3A2A")
        p.rect(0, 0, 16, 1, "#7A5238")
        for x in range(0, 16, 2):
            hh = rng.randint(5, 14)
            p.rect(x, 5, x + 2, 5 + hh, c1 if x % 4 else c2)
            p.px(x, 4 + hh, c3)
    elif k == "roots":
        p.rect(0, 0, 16, 6, "#3A2E3E")
        for x in (1, 5, 9, 13):
            hh = rng.randint(7, 15)
            for y in range(6, 6 + hh):
                p.px(x + (1 if y % 4 < 2 else 0), y, c2)
            p.px(x, 6 + hh, c3)
        p.rect(0, 5, 16, 7, c1)
    elif k in ("spikes", "icicles"):
        p.rect(0, 0, 16, 6, c1)
        p.rect(0, 0, 16, 2, c3)
        for x0, hh in ((0, 9), (5, 14), (10, 11)):
            p.poly([(x0, 6), (x0 + 5, 6), (x0 + 2, 6 + hh)], c1)
            p.poly([(x0, 6), (x0 + 2, 6), (x0 + 2, 6 + hh)], c3)
    elif k == "beam":
        p.rect(0, 0, 16, 8, c1)
        p.rect(0, 0, 16, 2, shade(c1, 0.25))
        p.rect(0, 6, 16, 8, c2)
        for x in (2, 8, 14):
            p.px(x, 4, c3)
        for x in (4, 12):
            for y in range(8, 16, 3):
                p.rect(x, y, x + 1, y + 2, "#8A8A94")
    elif k == "archrock":
        p.rect(0, 0, 16, 9, c1)
        for x in range(0, 16, 4):
            p.rect(x, 9, x + 4, 9 + rng.randint(1, 5), c1)
        p.rect(0, 0, 16, 2, c3)
        p.rect(0, 6, 16, 9, c2)
    elif k == "arch":
        p.rect(0, 0, 16, 9, c1)
        p.rect(0, 0, 16, 2, c3)
        for x in (0, 8):
            p.line((x, 3), (x + 7, 3), c2, 1)
            p.line((x + 4, 6), (x + 11, 6), c2, 1)
        for x in (3, 11):
            p.poly([(x, 9), (x + 4, 9), (x + 2, 15)], c3)
    elif k == "cloudbar":
        for x in (3, 8, 13):
            blob(p, x, 6, 7, 5, c1, hi=shade(c1, 0.25), lo=c2, rng=rng, lumps=4)
        p.poly([(7, 8), (5, 12), (8, 12), (6, 16), (10, 11), (8, 11)], c3)
    return p


def draw_post(b):
    c = b["ceil"]
    k = c["kind"]
    p = Pix(8, 16)
    base = c["post"]
    if k == "beam":
        for y in range(0, 16, 3):
            p.rect(2, y, 6, y + 2, "#8A8A94")
            p.rect(3, y + 1, 5, y + 2, "#5A5A66")
    elif k in ("spikes", "icicles"):
        p.poly([(0, 0), (8, 0), (4, 15)], c["c1"])
        p.poly([(0, 0), (4, 0), (4, 15)], c["c3"])
    elif k == "roots":
        for y in range(16):
            p.px(3 + (1 if y % 6 < 3 else 0), y, c["c2"])
            p.px(4 + (1 if y % 6 < 3 else 0), y, c["c1"])
    else:
        p.rect(2, 0, 6, 16, base)
        p.rect(2, 0, 4, 16, shade(base, 0.2))
        p.rect(5, 0, 6, 16, shade(base, -0.25))
    return p
