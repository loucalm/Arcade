#!/usr/bin/env python3
"""Planches du niveau 9 (La Sourdine) dans tools/gemini-niveau9/raw/sheets/ : dessinées par code (premier jet
d'Antigravity, corrigé par Claude, voir CORRECTIONS.md). python3 gen_sheets.py [a1 a2 b1 … e]"""
import json
import math
import os
import random
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
NIV9 = os.path.join(ROOT, "tools", "gemini-niveau9")
REFS = os.path.join(NIV9, "refs", "actuels")
OUT = os.path.join(NIV9, "raw", "sheets")
os.makedirs(OUT, exist_ok=True)

PAL_DATA = json.load(open(os.path.join(NIV9, "palettes.json"), encoding="utf-8"))

def hex_rgba(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)

MAGENTA = (255, 0, 255, 255)
BLACK = hex_rgba(PAL_DATA["sourd"]["colors"]["noir"])
WHITE = hex_rgba(PAL_DATA["sourd"]["colors"]["blanc"])
DARK_GREY = hex_rgba(PAL_DATA["sourd"]["colors"]["gris_fonce"])
GREY = hex_rgba(PAL_DATA["sourd"]["colors"]["gris"])
RED = hex_rgba(PAL_DATA["sourd"]["colors"]["rouge"])
DARK_RED = hex_rgba(PAL_DATA["sourd"]["colors"]["rouge_sombre"])

GOLD = {k: hex_rgba(v) for k, v in PAL_DATA["or"]["colors"].items()}

# chaque planche y range ses objets à leur taille finale (pour --install)
EXPORT = {}


def load_mask(name):
    im = Image.open(os.path.join(REFS, name)).convert("RGBA")
    w, h = im.size
    px = im.load()
    mask = [[px[x, y][3] >= 128 for x in range(w)] for y in range(h)]
    return w, h, mask


def outline_silhouette(mask, w, h, wrap_x=False, wrap_y=False):
    """Create RGBA image with BLACK fill and 1-px WHITE outline inside the silhouette (4-neighborhood)."""
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = im.load()
    for y in range(h):
        for x in range(w):
            if not mask[y][x]:
                continue
            edge = False
            for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                nx, ny = x + dx, y + dy
                if wrap_x:
                    nx %= w
                if wrap_y:
                    ny %= h
                if nx < 0 or nx >= w or ny < 0 or ny >= h or not mask[ny][nx]:
                    edge = True
                    break
            px[x, y] = WHITE if edge else BLACK
    return im


def apply_horizontal_tear(im, y_start, y_end, shift_x):
    """Shift rows [y_start, y_end) horizontally by shift_x pixels, keeping clean 1-px white outline on shifted band."""
    w, h = im.size
    src = im.copy()
    spx = src.load()
    dpx = im.load()
    for y in range(y_start, y_end):
        for x in range(w):
            sx = x - shift_x
            if 0 <= sx < w:
                dpx[x, y] = spx[sx, y]
            else:
                dpx[x, y] = (0, 0, 0, 0)
    # Restore 1-px white outline on any exposed solid boundary pixel (left/right/top/bottom of shifted rows)
    for y in range(y_start, y_end):
        for x in range(w):
            if dpx[x, y][3] >= 128:
                for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                    nx, ny = x + dx, y + dy
                    if nx < 0 or nx >= w or ny < 0 or ny >= h or dpx[nx, ny][3] < 128:
                        dpx[x, y] = WHITE
                        break
    return im



def negative_of_sylve(name, rings):
    """Version « sourde » d'un obstacle de la Sylve : masse noire, contour blanc, traits sombres d'origine en blanc."""
    im = Image.open(os.path.join(REFS, name)).convert("RGBA")
    w, h = im.size
    src = im.load()
    solid = [[src[x, y][3] >= 128 for x in range(w)] for y in range(h)]
    lum = [[0.3 * src[x, y][0] + 0.59 * src[x, y][1] + 0.11 * src[x, y][2] for x in range(w)] for y in range(h)]
    dark = [[solid[y][x] and lum[y][x] < 30 for x in range(w)] for y in range(h)]
    green = [[solid[y][x] and src[x, y][1] > src[x, y][0] + 20 for x in range(w)] for y in range(h)]
    top = [[solid[y][x] and lum[y][x] > 110 and not green[y][x] for x in range(w)] for y in range(h)]

    def inside(x, y):
        return 0 <= x < w and 0 <= y < h and solid[y][x]

    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = out.load()
    for y in range(h):
        for x in range(w):
            if not solid[y][x]:
                continue
            edge = any(not inside(x + dx, y + dy) for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)))
            if edge:
                px[x, y] = WHITE
            elif dark[y][x]:
                thick = all(inside(x + dx, y + dy) and dark[y + dy][x + dx]
                            for dx in (-1, 0, 1) for dy in (-1, 0, 1))
                px[x, y] = BLACK if thick else WHITE
            else:
                px[x, y] = BLACK
    # les reflets clairs de l'écorce, sous le dessus coupé : un trait d'1 px sur leur bord droit (pas de tache pleine) ;
    # seulement pour la bûche (sur la souche, les reflets sont des racines : ça ferait des points)
    for y in range(h // 2 if not rings else h, h):
        for x in range(w - 1):
            if top[y][x] and not top[y][x + 1] and px[x, y][:3] == BLACK[:3]:
                px[x, y] = WHITE
    # la mousse : une seule ligne blanche à sa limite basse, pas de points
    for y in range(h - 1):
        for x in range(w):
            if green[y][x] and solid[y + 1][x] and not green[y + 1][x] and px[x, y][:3] == BLACK[:3]:
                px[x, y] = WHITE
    # pas de pixel blanc isolé à l'intérieur (il se lirait comme du bruit)
    for y in range(1, h - 1):
        for x in range(1, w - 1):
            if px[x, y][:3] == WHITE[:3] and all(solid[y + dy][x + dx] for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
                if not any(px[x + dx, y + dy][:3] == WHITE[:3] for dx in (-1, 0, 1) for dy in (-1, 0, 1) if dx or dy):
                    px[x, y] = BLACK
    if rings:
        rim = max(y for y in range(h) for x in range(w) if top[y][x] and y < h // 2)
        pts = [(x, y) for y in range(rim) for x in range(w) if top[y][x] or green[y][x]]
        cx = sum(p[0] for p in pts) / len(pts)
        cy = sum(p[1] for p in pts) / len(pts)
        rx = (max(p[0] for p in pts) - min(p[0] for p in pts)) / 2
        ry = (max(p[1] for p in pts) - min(p[1] for p in pts)) / 2
        for k in (0.62, 0.3):
            for t in range(360):
                a = math.radians(t)
                x, y = round(cx + k * rx * math.cos(a)), round(cy + k * ry * math.sin(a))
                if 0 <= x < w and 0 <= y < h and top[y][x]:
                    px[x, y] = WHITE
    return out


def make_a1_obstacles():
    # 1. wall (38x34) et 2. block (40x40) : le négatif du sprite de la Sylve (relecture de Claude, CORRECTIONS.md).
    # Les traits sombres du dessin d'origine (rainures, contour) deviennent des traits blancs continus ; une masse sombre
    # épaisse (le creux de la bûche) reste noire, bordée de blanc ; le dessus coupé de la souche reçoit deux anneaux.
    wall = negative_of_sylve("wall_sylve.png", rings=True)
    wall = apply_horizontal_tear(wall, 19, 22, 2)
    block = negative_of_sylve("block_sylve.png", rings=False)
    block = apply_horizontal_tear(block, 21, 24, 2)

    # 3. ceil (strip of 4 tiles of 16x16 = 64x16, seamless left-right)
    cw, ch, cmask = load_mask("ceil_sylve.png")
    # Make ceil_sylve 16x16 seamless left-right if edge pixel differs slightly (check x=0 vs x=15)
    # In ceil_sylve: x=0..15 has branch y=0..4, leaves y=5..15. Let's tile it 4x cleanly.
    ceil_mask = [[cmask[y][x % cw] for x in range(cw * 4)] for y in range(ch)]
    ceil = outline_silhouette(ceil_mask, cw * 4, ch, wrap_x=True)
    cpx = ceil.load()
    # Branch edge line along y=4 (separating branch band from hanging leaves) + bark dashes
    for x in range(cw * 4):
        if ceil_mask[4][x]:
            if x % 6 != 2:
                cpx[x, 4] = WHITE
        if x % 8 in (3, 4) and ceil_mask[2][x]:
            cpx[x, 2] = WHITE
        # Leaf vein/edge strokes on right edge of leaves (y=6..12)
        for y in range(6, 13):
            if ceil_mask[y][x] and (x + 1 < cw * 4 and not ceil_mask[y][(x + 1) % (cw * 4)]):
                pass  # already outlined
            elif ceil_mask[y][x] and x % 4 == 2 and y in (7, 8, 9):
                cpx[x, y] = WHITE

    # 4. post (column of 3 tiles of 8x16 = 8x48, seamless top-bottom)
    pw, ph, pmask = load_mask("post_sylve.png")
    post_mask = [[pmask[y % ph][x] for x in range(pw)] for y in range(ph * 3)]
    # Ensure seamless vertical connection at y%16==0 by bridging stem x=2 if needed
    for t in range(3):
        y0 = t * ph
        post_mask[y0][2] = True
        post_mask[y0 + ph - 1][2] = True
    post = outline_silhouette(post_mask, pw, ph * 3, wrap_y=True)
    ppx = post.load()
    # Ensure inner 1-px black core where vine stem is at least 3 px wide, plus tiny leaf ticks
    for y in range(ph * 3):
        # if a row has solid pixels, keep the middle pixel black if width >= 3
        xs = [x for x in range(pw) if post_mask[y][x]]
        if len(xs) >= 3:
            ppx[xs[len(xs) // 2], y] = BLACK
        # leaf tick accent on right leaf protrusion
        if y % 16 in (4, 13) and len(xs) >= 3:
            ppx[xs[-1], y] = WHITE

    # 5. platform (strip of 3 tiles of 32x10 = 96x10, seamless left-right)
    plw, plh = 32 * 3, 10
    plat = Image.new("RGBA", (plw, plh), (0, 0, 0, 0))
    plpx = plat.load()
    for x in range(plw):
        plpx[x, 0] = WHITE
        for y in range(1, 9):
            plpx[x, y] = BLACK
        plpx[x, 9] = WHITE
        # white grass ticks hanging from top line (periodic every 32 px)
        xm = x % 32
        if xm in (2, 6, 9, 14, 17, 21, 26, 29):
            plpx[x, 1] = WHITE
            if xm in (6, 17, 26):
                plpx[x, 2] = WHITE
        # grey strokes underneath (y=6, 7, 8)
        if xm in (1, 2, 3, 4, 10, 11, 12, 19, 20, 21, 22, 27, 28):
            plpx[x, 7] = GREY
        if xm in (6, 7, 15, 16, 24, 25, 30, 31):
            plpx[x, 5] = GREY

    # Assemble a1_obstacles sheet (one row, wide magenta gaps, nothing touching edges)
    items = [wall, block, ceil, post, plat]
    EXPORT.update(wall=wall, block=block, ceil=ceil.crop((0, 0, 16, 16)), post=post.crop((0, 0, 8, 16)),
                  platform=plat.crop((0, 0, 32, 10)))
    pad = 16
    gap = 20
    W = pad * 2 + sum(i.width for i in items) + gap * (len(items) - 1)
    H = pad * 2 + max(i.height for i in items)
    sheet = Image.new("RGBA", (W, H), MAGENTA)
    cx = pad
    for item in items:
        cy = (H - item.height) // 2
        sheet.alpha_composite(item, (cx, cy))
        cx += item.width + gap
    sheet_x4 = sheet.resize((W * 4, H * 4), Image.NEAREST)
    sheet_x4.save(os.path.join(OUT, "a1_obstacles.png"))
    print("Saved a1_obstacles.png", sheet_x4.size, "logical:", (W, H))


def make_a2_terrain():
    # 1. ground: strip of 3 tiles of 32x20 = 96x20, seamless
    gw, gh = 96, 20
    ground = Image.new("RGBA", (gw, gh), BLACK)
    gpx = ground.load()
    for x in range(gw):
        xm = x % 32
        gpx[x, 0] = WHITE
        if xm in (1, 4, 7, 11, 13, 17, 20, 23, 27, 30):
            gpx[x, 1] = WHITE
            if xm in (4, 13, 23, 30):
                gpx[x, 2] = WHITE
        # grey horizontal strokes in earth/water reflection body
        if xm in (2, 3, 4, 5, 14, 15, 16, 24, 25, 26):
            gpx[x, 4] = GREY
        if xm in (8, 9, 10, 11, 20, 21, 22):
            gpx[x, 6] = GREY
        # sparse reflection dashes lower down (matching ground_sylve dashes)
        if 18 <= xm <= 26:
            gpx[x, 10] = GREY
        if 3 <= xm <= 9:
            gpx[x, 13] = GREY
        if 14 <= xm <= 21:
            gpx[x, 16] = GREY
        if 25 <= xm <= 30:
            gpx[x, 18] = GREY

    # 2. fill: 32x32, seamless on all sides (sparse grey brick-like pattern)
    fill = Image.new("RGBA", (32, 32), BLACK)
    fpx = fill.load()
    for row in range(0, 32, 8):
        off = 0 if (row // 8) % 2 == 0 else 8
        y_line = row + 7
        for x in range(32):
            if (x + row) % 5 != 0:  # sparse broken brick line
                fpx[x, y_line] = GREY
        for bx in range(off, 32 + 16, 16):
            vx = (bx - 1) % 32
            for y in range(row + 1, row + 6):
                if y != row + 3:
                    fpx[vx, y] = GREY
    for sx, sy in ((4, 3), (21, 11), (11, 19), (27, 27)):
        fpx[sx, sy] = GREY

    # 3. fill_deep: 32x32, seamless on all sides (same pattern in dark grey, sparser clean strokes)
    fill_deep = Image.new("RGBA", (32, 32), BLACK)
    fdpx = fill_deep.load()
    for row in range(0, 32, 8):
        off = 0 if (row // 8) % 2 == 0 else 8
        y_line = row + 7
        for seg_start in (2 + (row % 5), 18 + ((row // 2) % 4)):
            for dx in range(5):
                fdpx[(seg_start + dx) % 32, y_line] = DARK_GREY
        for bx in range(off, 32 + 16, 16):
            vx = (bx - 1) % 32
            for y in (row + 2, row + 3, row + 4):
                fdpx[vx, y] = DARK_GREY
    for sx, sy in ((5, 4), (22, 12), (10, 21)):
        fdpx[sx, sy] = DARK_GREY
        fdpx[(sx + 1) % 32, sy] = DARK_GREY

    # 4. hazard: strip of 2 tiles of 32x20 = 64x20, seamless, dark-grey wavy lines, no white
    hazard = Image.new("RGBA", (64, 20), BLACK)
    hpx = hazard.load()
    for x in range(64):
        xm = x % 32
        # top wavy surface line (dark grey only)
        y_top = 1 if (xm % 16) < 8 else 2
        if xm % 7 != 3:
            hpx[x, y_top] = DARK_GREY
        if 2 <= xm <= 10:
            hpx[x, 5 + (xm % 2)] = DARK_GREY
        if 16 <= xm <= 27:
            hpx[x, 9 + ((xm + 1) % 2)] = DARK_GREY
        if 6 <= xm <= 14:
            hpx[x, 13] = DARK_GREY
        if 20 <= xm <= 29:
            hpx[x, 17 - (xm % 2)] = DARK_GREY

    # 5. cliff: strip of 3 tiles of 32x8 = 96x8, seamless
    cliff = Image.new("RGBA", (96, 8), BLACK)
    clpx = cliff.load()
    for x in range(96):
        xm = x % 32
        clpx[x, 0] = WHITE
        if xm in (2, 5, 9, 12, 16, 19, 24, 28, 31):
            clpx[x, 1] = WHITE
            if xm in (5, 16, 28):
                clpx[x, 2] = WHITE
        if xm in (3, 4, 5, 14, 15, 22, 23, 24):
            clpx[x, 5] = GREY

    # 6. deco_0..deco_3: exact silhouettes of deco_sylve_0..3
    decos = []
    # deco_0 (16x12): pebble
    w, h, m0 = load_mask("deco_sylve_0.png")
    d0 = outline_silhouette(m0, w, h)
    d0px = d0.load()
    for x, y in [(9, 8), (10, 8), (11, 7), (12, 6), (12, 5)]:
        if m0[y][x]:
            d0px[x, y] = WHITE
    decos.append(d0)

    # deco_1 (14x18): twig
    w, h, m1 = load_mask("deco_sylve_1.png")
    d1 = outline_silhouette(m1, w, h)
    decos.append(d1)

    # deco_2 (14x14): small cut ring
    w, h, m2 = load_mask("deco_sylve_2.png")
    d2 = outline_silhouette(m2, w, h)
    d2px = d2.load()
    for x, y in [(6, 4), (7, 4), (8, 5), (9, 6), (9, 7), (8, 8), (7, 9), (6, 9), (5, 8), (4, 7), (4, 6), (5, 5), (7, 7)]:
        if m2[y][x]:
            d2px[x, y] = WHITE
    decos.append(d2)

    # deco_3 (16x10): two stones
    w, h, m3 = load_mask("deco_sylve_3.png")
    d3 = outline_silhouette(m3, w, h)
    d3px = d3.load()
    if m3[5][11]:
        d3px[11, 5] = WHITE
    decos.append(d3)

    items = [ground, fill, fill_deep, hazard, cliff] + decos
    EXPORT.update(ground=ground.crop((0, 0, 32, 20)), fill=fill, fill_deep=fill_deep, hazard=hazard.crop((0, 0, 32, 20)),
                  cliff=cliff.crop((0, 0, 32, 8)), decos=decos)
    pad = 14
    gap = 16
    W = pad * 2 + sum(i.width for i in items) + gap * (len(items) - 1)
    H = pad * 2 + max(i.height for i in items)
    sheet = Image.new("RGBA", (W, H), MAGENTA)
    cx = pad
    for item in items:
        cy = (H - item.height) // 2
        sheet.alpha_composite(item, (cx, cy))
        cx += item.width + gap
    sheet_x4 = sheet.resize((W * 4, H * 4), Image.NEAREST)
    sheet_x4.save(os.path.join(OUT, "a2_terrain.png"))
    print("Saved a2_terrain.png", sheet_x4.size, "logical:", (W, H))


def draw_wrapped_outlined(canvas, w, h, cx, draw_fn, outline_col, detail_fn=None):
    """Draw a shape centered around cx onto canvas with 1-px 4-neighbor outline and horizontal wrap."""
    tmp = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(tmp)
    for dx in (-w, 0, w):
        draw_fn(d, cx + dx)
    # Apply 1-px outline on solid pixels (4-neighborhood)
    tpx = tmp.load()
    solid = [[tpx[x, y][3] >= 128 for x in range(w)] for y in range(h)]
    out_im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    opx = out_im.load()
    for y in range(h):
        for x in range(w):
            if not solid[y][x]:
                continue
            edge = False
            for ox, oy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                nx = (x + ox) % w
                ny = y + oy
                if ny < 0 or ny >= h or not solid[ny][nx]:
                    edge = True
                    break
            opx[x, y] = outline_col if edge else BLACK
    if detail_fn is not None:
        od = ImageDraw.Draw(out_im)
        for dx in (-w, 0, w):
            detail_fn(od, cx + dx, out_im)
    canvas.alpha_composite(out_im)


def apply_horizon_black(canvas, w, h, hy, outline_col):
    """Fill below horizon hy with BLACK and place 1-px outline_col where sky touches horizon."""
    cpx = canvas.load()
    for x in range(w):
        sky_above = cpx[x, hy - 1][:3] == (255, 0, 255)
        cpx[x, hy] = outline_col if sky_above else BLACK
        for y in range(hy + 1, h):
            cpx[x, y] = BLACK


def save_bg(im, name):
    EXPORT[name.split(".")[0]] = im.copy()
    # Save at 2x (1280x720, clean 2x2 square pixel grid, 16:9)
    out_im = im.resize((im.width * 2, im.height * 2), Image.NEAREST)
    out_im.save(os.path.join(OUT, name))
    print("Saved", name, out_im.size)


def apply_glitch_bands(im, bands, wrap_x=True, outline_col=None):
    """Apply clean horizontal glitch slice shifts (y0, y1, shift_x) to break up background layers."""
    w, h = im.size
    src = im.copy()
    spx = src.load()
    dpx = im.load()
    for y0, y1, sx in bands:
        for y in range(max(0, y0), min(h, y1)):
            for x in range(w):
                ox = (x - sx) % w if wrap_x else (x - sx)
                if 0 <= ox < w:
                    dpx[x, y] = spx[ox, y]
                else:
                    dpx[x, y] = MAGENTA
            if outline_col is not None:
                for x in range(w):
                    if dpx[x, y][:3] != (255, 0, 255):
                        lx = (x - 1) % w
                        rx = (x + 1) % w
                        if dpx[lx, y][:3] == (255, 0, 255) or dpx[rx, y][:3] == (255, 0, 255):
                            dpx[x, y] = outline_col


def make_b1_far():
    W, H, HY = 640, 360, 205
    im = Image.new("RGBA", (W, H), MAGENTA)
    # 5 sharp, faceted overlapping mountains (breaking Sylve's smooth peaks)
    peaks = [
        (75, 92, 178),
        (210, 118, 208),
        (335, 78, 160),
        (465, 126, 216),
        (585, 98, 182),
    ]
    for cx, mh, mw in peaks:
        def draw_m(d, x0, mh=mh, mw=mw):
            pts = [
                (x0 - mw // 2, HY),
                (x0 - mw // 4, HY - mh // 2 - 4),
                (x0 - 10, HY - mh + 7),
                (x0 - 3, HY - mh),
                (x0 + 1, HY - mh + 9),
                (x0 + 7, HY - mh + 2),
                (x0 + 15, HY - mh + 11),
                (x0 + mw // 4 + 4, HY - mh // 2 + 2),
                (x0 + mw // 2, HY),
            ]
            d.polygon(pts, fill=BLACK)

        def detail_m(d, x0, out_im, mh=mh, mw=mw):
            # Sharp crystalline snow-cap zig-zag & geometric ridge facets in DARK_GREY
            cap_y = HY - int(mh * 0.65)
            hw = int(mw * 0.21)
            zig = [
                (x0 - hw, cap_y),
                (x0 - hw // 2, cap_y + 8),
                (x0 - 3, cap_y - 5),
                (x0 + 4, cap_y + 9),
                (x0 + hw // 2 + 2, cap_y - 3),
                (x0 + hw, cap_y + 4),
            ]
            d.line(zig, fill=DARK_GREY, width=1)
            d.line([(x0 + 1, HY - mh + 9), (x0 + 9, HY - mh + 30), (x0 + 18, HY - 10)], fill=DARK_GREY, width=1)
            d.line([(x0 - 10, HY - mh + 7), (x0 - 18, HY - mh + 36)], fill=DARK_GREY, width=1)

        draw_wrapped_outlined(im, W, H, cx, draw_m, DARK_GREY, detail_m)

    # pas de glitch dans les décors : le code s'en charge (CORRECTIONS.md)
    apply_horizon_black(im, W, H, HY, DARK_GREY)
    save_bg(im, "b1_far.png")


def make_b2_mid():
    W, H, HY = 640, 360, 205
    im = Image.new("RGBA", (W, H), MAGENTA)
    # 11 sharp, jagged pine trees across 640 px
    pines = [
        (32, 98), (96, 66), (156, 90), (218, 104), (275, 76),
        (332, 58), (390, 72), (448, 108), (512, 82), (566, 62), (610, 88),
    ]
    for idx, (cx, ph) in enumerate(pines):
        pw = int(ph * 0.64)
        def draw_p(d, x0, ph=ph, pw=pw, idx=idx):
            d.rectangle([x0 - 2, HY - ph // 5, x0 + 2, HY], fill=BLACK)
            for i in range(3):
                top = HY - ph + i * (ph // 4)
                bot = HY - ph // 6 - (2 - i) * (ph // 6)
                half = max(6, int(pw / 2 * (0.55 + 0.25 * i)))
                # Jagged spiked lower bough edge
                skew = 3 if (idx % 3 == 0 and i == 0) else 0
                d.polygon([
                    (x0 + skew, top),
                    (x0 - half, bot),
                    (x0 - half // 2, bot - 3),
                    (x0, bot + 1),
                    (x0 + half // 2, bot - 3),
                    (x0 + half, bot),
                ], fill=BLACK)

        def detail_p(d, x0, out_im, ph=ph, pw=pw):
            for i in range(3):
                top = HY - ph + i * (ph // 4)
                bot = HY - ph // 6 - (2 - i) * (ph // 6)
                half = max(6, int(pw / 2 * (0.55 + 0.25 * i)))
                d.line([(x0 + 2, top + 6), (x0 + half // 2 + 1, bot - 4)], fill=GREY, width=1)

        draw_wrapped_outlined(im, W, H, cx, draw_p, GREY, detail_p)

    # pas de glitch dans les décors : le code s'en charge (CORRECTIONS.md)
    apply_horizon_black(im, W, H, HY, GREY)
    save_bg(im, "b2_mid.png")


def make_b3_near():
    W, H, HY = 640, 360, 205
    im = Image.new("RGBA", (W, H), MAGENTA)

    def draw_drum_tree(cx, h, rx, ry, broken=False, glitched=False):
        def draw_t(d, x0):
            if broken:
                top_y = HY - h
                pts = [
                    (x0 - 7, HY),
                    (x0 - 6, top_y + 12),
                    (x0 - 8, top_y + 4),
                    (x0 - 2, top_y + 15),
                    (x0 + 2, top_y),
                    (x0 + 5, top_y + 11),
                    (x0 + 9, top_y + 6),
                    (x0 + 7, top_y + 22),
                    (x0 + 7, HY),
                ]
                d.polygon(pts, fill=BLACK)
            else:
                # Trunk with jagged crack notch if glitched
                if glitched:
                    d.polygon([(x0 - 5, HY), (x0 - 5, HY - h // 2), (x0 - 2, HY - h // 2 - 4),
                               (x0 - 6, HY - h // 2 - 8), (x0 - 5, HY - h), (x0 + 5, HY - h),
                               (x0 + 6, HY - h // 2 + 4), (x0 + 3, HY - h // 2 + 8), (x0 + 5, HY)], fill=BLACK)
                else:
                    d.rectangle([x0 - 5, HY - h, x0 + 5, HY], fill=BLACK)
                lobes = [
                    (0, 0, rx, ry),
                    (-rx * 0.55, ry * 0.25, rx * 0.58, ry * 0.62),
                    (rx * 0.55, ry * 0.22, rx * 0.55, ry * 0.60),
                    (-rx * 0.28, -ry * 0.45, rx * 0.52, ry * 0.58),
                    (rx * 0.30, -ry * 0.40, rx * 0.48, ry * 0.55),
                ]
                cy = HY - h - ry // 3
                for lx, ly, lrx, lry in lobes:
                    d.ellipse([x0 + lx - lrx, cy + ly - lry, x0 + lx + lrx, cy + ly + lry], fill=BLACK)

        def detail_t(d, x0, out_im):
            if broken:
                top_y = HY - h
                d.line([(x0 + 2, top_y + 18), (x0 + 2, HY - 10)], fill=WHITE, width=1)
                d.line([(x0 - 2, top_y + 28), (x0 - 2, HY - 22)], fill=WHITE, width=1)
                d.line([(x0 + 4, top_y + 34), (x0 - 3, top_y + 42)], fill=WHITE, width=1)
            else:
                cy = HY - h - ry // 3
                d.line([(x0 + 2, HY - h + 8), (x0 + 2, HY - 14)], fill=WHITE, width=1)
                d.line([(x0 - 2, HY - h + 22), (x0 - 2, HY - 34)], fill=WHITE, width=1)
                d.arc([x0 - rx // 2, cy - ry // 3, x0 + rx - 4, cy + ry - 4], start=10, end=105, fill=WHITE, width=1)
                d.arc([x0 + 2, cy - ry // 2, x0 + int(rx * 0.85), cy + ry // 3], start=350, end=85, fill=WHITE, width=1)
                if glitched:
                    # Sharp diagonal fracture lines across the drum crown
                    d.line([(x0 - rx // 3, cy - ry // 2), (x0 - 4, cy - 2), (x0 + 6, cy + 4)], fill=WHITE, width=1)

        draw_wrapped_outlined(im, W, H, cx, draw_t, WHITE, detail_t)

    def draw_bush(cx, rx):
        def draw_b(d, x0):
            d.ellipse([x0 - rx, HY - rx + 4, x0 + rx, HY + 6], fill=BLACK)
            d.ellipse([x0 + rx // 2, HY - int(rx * 0.65), x0 + int(rx * 1.6), HY + 6], fill=BLACK)
            d.ellipse([x0 - int(rx * 1.3), HY - int(rx * 0.55), x0 - rx // 3, HY + 6], fill=BLACK)

        def detail_b(d, x0, out_im):
            d.arc([x0 - rx // 3, HY - rx + 8, x0 + rx - 4, HY - 2], start=5, end=95, fill=WHITE, width=1)

        draw_wrapped_outlined(im, W, H, cx, draw_b, WHITE, detail_b)

    draw_drum_tree(78, 108, 40, 26, broken=False, glitched=True)
    draw_bush(190, 22)
    draw_drum_tree(278, 68, 0, 0, broken=True)
    draw_drum_tree(355, 76, 26, 18, broken=False, glitched=False)
    draw_drum_tree(482, 102, 36, 24, broken=False, glitched=True)
    draw_bush(570, 18)
    draw_drum_tree(618, 44, 0, 0, broken=True)

    # pas de glitch dans les décors : le code s'en charge (CORRECTIONS.md)

    apply_horizon_black(im, W, H, HY, WHITE)
    save_bg(im, "b3_near.png")


def make_b4_vide_far():
    W, H = 640, 360
    im = Image.new("RGBA", (W, H), MAGENTA)
    px = im.load()

    def staff_y(x, line_idx):
        t = 2.0 * math.pi * x / W
        base = 148 + 28 * math.sin(t) + 12 * math.cos(2 * t)
        return base + (line_idx - 2) * 11

    breaks = [(118, 168), (324, 384), (506, 552)]

    for li in range(5):
        b0, b1 = breaks[0]
        b2, b3 = breaks[1]
        b4, b5 = breaks[2]
        local_breaks = [
            (b0 + (li % 3) * 5, b1 - (2 - li % 3) * 4),
            (b2 + ((li + 1) % 3) * 6, b3 - (li % 2) * 6),
            (b4 + (li % 2) * 5, b5 - ((li + 1) % 3) * 5),
        ]
        for x in range(W):
            if any(a <= x <= b for a, b in local_breaks):
                continue
            y = round(staff_y(x, li))
            # Glitch step offsets right before breaks
            if 104 <= x < 118 and li in (1, 3):
                y += 4
            elif 384 < x <= 402 and li in (0, 2, 4):
                y -= 5
            for dy in (0, 1):
                if 0 <= y + dy < H:
                    px[x, y + dy] = DARK_GREY

    d = ImageDraw.Draw(im)
    # Sharp jagged glitch fragments & radial void spikes in DARK_GREY at the staff tears
    for fx, fy, flen in [(132, 178, 18), (150, 144, 12), (344, 108, 20), (362, 134, 14), (518, 140, 16)]:
        d.rectangle([fx, fy, fx + flen, fy + 1], fill=DARK_GREY)
    # Subtle dark-grey geometric starburst shards behind the center tear (ISpy mood!)
    for sx, sy, sr in [(354, 124, 28), (144, 162, 22)]:
        for ang in range(0, 360, 45):
            rad = math.radians(ang)
            x2 = int(sx + math.cos(rad) * sr)
            y2 = int(sy + math.sin(rad) * sr)
            d.line([(sx, sy), (x2, y2)], fill=DARK_GREY, width=1)

    # Frozen note heads (dark grey only) along the warped staff
    notes = [
        (68, 1, True),
        (192, 3, False),
        (262, 0, True),
        (418, 2, True),
        (472, 4, False),
        (586, 1, True),
    ]
    for nx, li, has_stem in notes:
        ny = round(staff_y(nx, li))
        d.ellipse([nx - 5, ny - 4, nx + 5, ny + 4], fill=DARK_GREY)
        if has_stem:
            d.rectangle([nx + 4, ny - 24, nx + 5, ny], fill=DARK_GREY)
            if li in (0, 4):
                d.polygon([(nx + 5, ny - 24), (nx + 13, ny - 16), (nx + 10, ny - 11), (nx + 5, ny - 18)], fill=DARK_GREY)

    save_bg(im, "b4_vide_far.png")


def make_b5_vide_near():
    W, H = 640, 360
    im = Image.new("RGBA", (W, H), MAGENTA)

    def make_debris_ground():
        # 58x24 upright ground chunk with jagged grass spikes & hanging root ticks, then rotated 180 or 90
        g = Image.new("RGBA", (58, 24), (0, 0, 0, 0))
        gd = ImageDraw.Draw(g)
        gd.polygon([(2, 0), (56, 0), (57, 9), (49, 19), (38, 22), (29, 15), (19, 21), (7, 18), (0, 9)], fill=BLACK)
        gpx = g.load()
        solid = [[gpx[x, y][3] >= 128 for x in range(58)] for y in range(24)]
        res = Image.new("RGBA", (58, 24), (0, 0, 0, 0))
        rpx = res.load()
        for y in range(24):
            for x in range(58):
                if not solid[y][x]:
                    continue
                edge = any(
                    x + ox < 0 or x + ox >= 58 or y + oy < 0 or y + oy >= 24 or not solid[y + oy][x + ox]
                    for ox, oy in ((-1, 0), (1, 0), (0, -1), (0, 1))
                )
                rpx[x, y] = WHITE if edge else BLACK
        # Sharp grass ticks + fracture strokes
        for gx in (7, 13, 21, 27, 35, 42, 49):
            rpx[gx, 2] = WHITE
            if gx in (13, 27, 42):
                rpx[gx, 3] = WHITE
        for gx in range(13, 23):
            rpx[gx, 10] = WHITE
        for gx in range(32, 43):
            rpx[gx, 13] = WHITE
        return res

    def make_debris_trunk():
        # Upright broken drum-tree trunk (22x64), then rotated 90 deg
        t = Image.new("RGBA", (22, 64), (0, 0, 0, 0))
        td = ImageDraw.Draw(t)
        td.polygon([(4, 6), (8, 0), (12, 8), (16, 2), (18, 12), (19, 54), (14, 63), (9, 56), (4, 62), (3, 50)], fill=BLACK)
        tpx = t.load()
        solid = [[tpx[x, y][3] >= 128 for x in range(22)] for y in range(64)]
        res = Image.new("RGBA", (22, 64), (0, 0, 0, 0))
        rpx = res.load()
        for y in range(64):
            for x in range(22):
                if not solid[y][x]:
                    continue
                edge = any(
                    x + ox < 0 or x + ox >= 22 or y + oy < 0 or y + oy >= 64 or not solid[y + oy][x + ox]
                    for ox, oy in ((-1, 0), (1, 0), (0, -1), (0, 1))
                )
                rpx[x, y] = WHITE if edge else BLACK
        for y in range(18, 48, 2):
            if y % 6 != 0:
                rpx[13, y] = WHITE
        for y in range(24, 42):
            if y % 4 != 0:
                rpx[8, y] = WHITE
        return res

    def make_debris_piano_keys():
        # Cluster of 4 piano keys (black with white outline, black-key notches, 46x28)
        p = Image.new("RGBA", (46, 28), (0, 0, 0, 0))
        pd = ImageDraw.Draw(p)
        keys = [(0, 2, 10, 25), (11, 0, 21, 27), (22, 3, 32, 24), (34, 1, 44, 26)]
        for x0, y0, x1, y1 in keys:
            pd.rectangle([x0, y0, x1, y1], fill=BLACK, outline=WHITE)
        # Black-key notches at top between keys
        pd.rectangle([8, 2, 13, 12], fill=BLACK, outline=WHITE)
        pd.rectangle([19, 1, 24, 11], fill=BLACK, outline=WHITE)
        return p

    def make_debris_crown():
        # Small round bush/crown fragment (34x24)
        c = Image.new("RGBA", (34, 24), (0, 0, 0, 0))
        cd = ImageDraw.Draw(c)
        cd.ellipse([4, 4, 24, 21], fill=BLACK)
        cd.ellipse([14, 2, 31, 18], fill=BLACK)
        cd.ellipse([1, 8, 16, 22], fill=BLACK)
        cpx = c.load()
        solid = [[cpx[x, y][3] >= 128 for x in range(34)] for y in range(24)]
        res = Image.new("RGBA", (34, 24), (0, 0, 0, 0))
        rpx = res.load()
        for y in range(24):
            for x in range(34):
                if not solid[y][x]:
                    continue
                edge = any(
                    x + ox < 0 or x + ox >= 34 or y + oy < 0 or y + oy >= 24 or not solid[y + oy][x + ox]
                    for ox, oy in ((-1, 0), (1, 0), (0, -1), (0, 1))
                )
                rpx[x, y] = WHITE if edge else BLACK
        ImageDraw.Draw(res).arc([10, 7, 25, 18], start=10, end=110, fill=WHITE, width=1)
        return res

    g_up = make_debris_ground()
    trunk_up = make_debris_trunk()
    keys_up = make_debris_piano_keys()
    crown_up = make_debris_crown()

    # Strictly 90, 180, or 270 deg rotations only! Scattered in upper two thirds (y=18..235)
    placements = [
        (g_up.rotate(180, expand=True), 58, 48),       # upside-down ground chunk
        (keys_up.rotate(90, expand=True), 185, 115),    # 90-deg tilted piano keys
        (trunk_up.rotate(90, expand=True), 295, 38),    # 90-deg horizontal broken drum-tree trunk
        (crown_up.rotate(180, expand=True), 405, 148),  # upside-down crown chunk
        (g_up.rotate(270, expand=True), 495, 58),       # 90-deg sideways ground chunk
        (keys_up.rotate(180, expand=True), 575, 165),   # upside-down piano keys
    ]
    for spr, px0, py0 in placements:
        for dx in (-W, 0, W):
            im.alpha_composite(spr, (px0 + dx, py0))

    save_bg(im, "b5_vide_near.png")


def draw_almond_mask(w, h, pointy=True):
    """Return boolean mask of a symmetric almond eye inside w x h (leaving 1-px margin for black border)."""
    mask = [[False] * w for _ in range(h)]
    cx, cy = (w - 1) / 2.0, (h - 1) / 2.0
    rx, ry = (w - 2) / 2.0, (h - 2) / 2.0
    for y in range(1, h - 1):
        for x in range(1, w - 1):
            nx = abs(x - cx) / rx
            if nx > 1.0:
                continue
            # Almond profile: pinched corners if pointy else rounder
            max_y = (1.0 - nx ** 1.65) ** 0.75 if pointy else math.sqrt(max(0.0, 1.0 - nx * nx))
            ny = abs(y - cy) / ry
            if ny <= max_y:
                mask[y][x] = True
    return mask


def border_mask(mask, w, h):
    """Return outer 1-px border mask around True pixels of mask."""
    b = [[False] * w for _ in range(h)]
    for y in range(h):
        for x in range(w):
            if mask[y][x]:
                continue
            if any(
                0 <= x + ox < w and 0 <= y + oy < h and mask[y + oy][x + ox]
                for ox, oy in ((-1, 0), (1, 0), (0, -1), (0, 1))
            ):
                b[y][x] = True
    return b


def draw_small_or_large_eye(w, h, mode, is_large):
    """Draw eye_s (16x10) or eye_l (32x18) for modes: open, look_left, half, closed."""
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = im.load()
    sclera = draw_almond_mask(w, h, pointy=True)
    bdr = border_mask(sclera, w, h)

    if mode == "closed":
        for y in range(h):
            for x in range(w):
                if sclera[y][x] or bdr[y][x]:
                    px[x, y] = BLACK
        cy = h // 2
        margin = 2 if not is_large else 3
        for x in range(margin, w - margin):
            t = (x - (w - 1) / 2.0) / ((w - 2 * margin) / 2.0)
            ly = round(cy + (1.5 if not is_large else 2.6) * (1.0 - t * t) - 0.5)
            if 0 <= ly < h:
                px[x, ly] = WHITE
        lash_xs = [w // 2 - 2, w // 2 + 2] if not is_large else [w // 2 - 6, w // 2, w // 2 + 6]
        for lx in lash_xs:
            t = (lx - (w - 1) / 2.0) / ((w - 2 * margin) / 2.0)
            ly = round(cy + (1.5 if not is_large else 2.6) * (1.0 - t * t) - 0.5)
            for dy in (1, 2) if is_large else (1,):
                if 0 <= ly + dy < h:
                    px[lx, ly + dy] = WHITE
        return im

    for y in range(h):
        for x in range(w):
            if bdr[y][x]:
                px[x, y] = BLACK
            elif sclera[y][x]:
                px[x, y] = WHITE

    # Red pupil (intense ringed iris on eye_l, bold round pupil on eye_s)
    cx = (w - 1) / 2.0
    if mode == "look_left":
        cx -= (1.8 if not is_large else 5.4)
    cy = (h - 1) / 2.0
    pr = 1.6 if not is_large else 4.2
    for y in range(h):
        for x in range(w):
            if not sclera[y][x]:
                continue
            d = math.hypot(x - cx, y - cy)
            if d <= pr:
                if is_large:
                    if d <= 1.4:
                        px[x, y] = BLACK
                    elif d >= pr - 1.0:
                        px[x, y] = DARK_RED
                    else:
                        px[x, y] = RED
                else:
                    px[x, y] = RED
    if is_large:
        gx, gy = round(cx - 1.6), round(cy - 1.6)
        if 0 <= gx < w and 0 <= gy < h and sclera[gy][gx]:
            px[gx, gy] = WHITE
        # Subtle piercing inner-corner shadow ticks
        for sx, sy in ((3, h // 2), (4, h // 2), (w - 4, h // 2), (w - 5, h // 2)):
            if sclera[sy][sx]:
                px[sx, sy] = BLACK

    if mode == "half":
        lid_y = (h // 2 - 1) if not is_large else (h // 2)
        for y in range(h):
            for x in range(w):
                if sclera[y][x]:
                    if y < lid_y:
                        px[x, y] = BLACK
                    elif y == lid_y:
                        px[x, y] = WHITE
    return im


def make_c_yeux():
    modes = ["open", "look_left", "half", "closed"]
    small_frames = [draw_small_or_large_eye(16, 10, m, False) for m in modes]
    large_frames = [draw_small_or_large_eye(32, 18, m, True) for m in modes]

    pad = 10
    col_gap = 12
    row_gap = 14
    col_w = 32
    W = pad * 2 + col_w * 4 + col_gap * 3
    H = pad * 2 + 10 + row_gap + 18
    sheet = Image.new("RGBA", (W, H), MAGENTA)
    for i, f in enumerate(small_frames):
        x = pad + i * (col_w + col_gap) + (col_w - f.width) // 2
        y = pad
        sheet.alpha_composite(f, (x, y))
    for i, f in enumerate(large_frames):
        x = pad + i * (col_w + col_gap)
        y = pad + 10 + row_gap
        sheet.alpha_composite(f, (x, y))

    EXPORT.update(eye_s=small_frames, eye_l=large_frames)
    sheet_x4 = sheet.resize((W * 4, H * 4), Image.NEAREST)
    sheet_x4.save(os.path.join(OUT, "c_yeux.png"))
    print("Saved c_yeux.png", sheet_x4.size, "logical:", (W, H))


def make_d1_sourdine_corps():
    """Corps de la Sourdine, 220×360 : masse noire, bord gauche d'encre vivante (coulures arrondies qui pendent et
    traînées horizontales de signal déchiré), contour blanc d'1 px à gauche ; bords droit, haut et bas droits.
    Réécrit par Claude (CORRECTIONS.md) : l'ancien bord en dents de scie se lisait comme du papier déchiré."""
    W, H = 220, 360
    rng = random.Random(9)
    body = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = body.load()
    solid = [[False] * W for _ in range(H)]

    def fill(x, y):
        if 0 <= x < W and 0 <= y < H:
            solid[y][x] = True

    # bord de base : une ondulation lente, jamais en dessous de x = 34 (le visage commence vers x = 60)
    edge = [int(round(44 + 6 * math.sin(y * 0.031 + 0.7) + 3 * math.sin(y * 0.083 + 2.1))) for y in range(H)]
    for y in range(H):
        for x in range(edge[y], W):
            fill(x, y)

    # coulures : un renflement arrondi du bord, et dessous une coulure verticale épaisse (6 px) qui finit en goutte
    # à peine plus large (forme de peinture qui coule ; une goutte bien plus large que son col ferait un tenon de puzzle)
    drip = Image.new("L", (W, H), 0)
    dd = ImageDraw.Draw(drip)
    for yc, R, dl in [(40, 12, 18), (120, 16, 30), (190, 10, 12), (250, 18, 36), (322, 12, 16)]:
        ex = edge[yc]
        dd.ellipse([ex - R, yc - int(R * 0.8), ex + R, yc + int(R * 0.8)], fill=255)
        dx = ex - int(R * 0.55)
        bottom = yc + int(R * 0.8) + dl
        dd.rectangle([dx - 3, yc, dx + 2, bottom], fill=255)
        dd.ellipse([dx - 4, bottom - 3, dx + 3, bottom + 4], fill=255)
    dpx = drip.load()
    for y in range(H):
        for x in range(W):
            if dpx[x, y]:
                fill(x, y)

    # traînées de signal : paquets de 2 à 4 barres horizontales qui dépassent vers la gauche
    for yb in [6, 70, 138, 196, 266, 300, 340]:
        y = yb
        for _ in range(rng.randint(2, 4)):
            hgt = rng.randint(1, 3)
            ln = rng.randint(4, 30)
            for yy in range(y, min(H, y + hgt)):
                for x in range(edge[yy] - ln, edge[yy] + 1):
                    fill(x, yy)
            y += hgt + rng.randint(1, 3)

    for y in range(H):
        for x in range(W):
            if solid[y][x]:
                px[x, y] = BLACK
    # contour blanc d'1 px sur le bord gauche seulement (haut, bas et droite coupés par l'écran restent noirs)
    for y in range(H):
        for x in range(W - 1):
            if not solid[y][x]:
                continue
            if any((0 <= x + ox and 0 <= y + oy < H and not solid[y + oy][x + ox]) or x + ox < 0
                   for ox, oy in ((-1, 0), (1, 0), (0, -1), (0, 1))):
                px[x, y] = WHITE
    # le signal continue un peu dans la masse : quelques tirets blancs horizontaux près du bord
    for y in [9, 74, 141, 201, 270, 344]:
        x0 = edge[y] + 4
        for x in range(x0, x0 + rng.randint(4, 9)):
            px[x, y] = WHITE

    CW, CH = 260, 400
    canvas = Image.new("RGBA", (CW, CH), MAGENTA)
    canvas.alpha_composite(body, (20, 20))
    out_x2 = canvas.resize((CW * 2, CH * 2), Image.NEAREST)
    out_x2.save(os.path.join(OUT, "d1_sourdine_corps.png"))
    EXPORT["body"] = body
    print("Saved d1_sourdine_corps.png", out_x2.size, "logical:", (CW, CH))


def make_d2_sourdine_visage():
    # Row 1: face_eye (48x28, 5 frames: open, half, closed, wide_angry, cracked)
    ew, eh = 48, 28
    face_eyes = []
    for mode in ("open", "half", "closed", "wide", "cracked"):
        im = Image.new("RGBA", (ew, eh), (0, 0, 0, 0))
        px = im.load()
        if mode == "wide":
            # œil grand ouvert, rond, paupière du haut en biais (colère) : coin intérieur (à droite) plus bas.
            # C'est l'œil gauche ; l'œil droit est le même sprite retourné (flipX).
            sclera = draw_almond_mask(ew, eh, pointy=False)
            for y in range(eh):
                for x in range(ew):
                    if y < 1 + 8.0 * x / (ew - 1):
                        sclera[y][x] = False
        else:
            sclera = draw_almond_mask(ew, eh, pointy=True)
        bdr = border_mask(sclera, ew, eh)

        if mode == "closed":
            for y in range(eh):
                for x in range(ew):
                    if sclera[y][x] or bdr[y][x]:
                        px[x, y] = BLACK
            for x in range(4, ew - 4):
                t = (x - (ew - 1) / 2.0) / ((ew - 8) / 2.0)
                ly = round(13 + 5.2 * (1.0 - t * t))
                for dy in (0, 1):
                    if 0 <= ly + dy < eh:
                        px[x, ly + dy] = WHITE
            for lx in (11, 18, 24, 30, 37):
                t = (lx - (ew - 1) / 2.0) / ((ew - 8) / 2.0)
                ly = round(13 + 5.2 * (1.0 - t * t))
                for dy in range(2, 5):
                    if 0 <= ly + dy < eh:
                        px[lx, ly + dy] = WHITE
            face_eyes.append(im)
            continue

        for y in range(eh):
            for x in range(ew):
                if bdr[y][x]:
                    px[x, y] = BLACK
                elif sclera[y][x]:
                    px[x, y] = WHITE


        if mode == "half":
            for y in range(eh):
                for x in range(ew):
                    if sclera[y][x]:
                        lid_y = 11 + int(abs(x - 23.5) * 0.14)
                        if y < lid_y:
                            px[x, y] = BLACK
                        elif y in (lid_y, lid_y + 1):
                            px[x, y] = WHITE
        elif mode == "cracked":
            # Dramatic chipped wedge + branching fracture cracks across the whole eye
            for y in range(1, 13):
                for x in range(31, 47):
                    if (x - 31) + (12 - y) >= 14 and sclera[y][x]:
                        sclera[y][x] = False
            bdr_c = border_mask(sclera, ew, eh)
            for y in range(eh):
                for x in range(ew):
                    if bdr_c[y][x]:
                        px[x, y] = BLACK
                    elif sclera[y][x]:
                        px[x, y] = WHITE
                    else:
                        px[x, y] = (0, 0, 0, 0)
            d = ImageDraw.Draw(im)
            d.line([(36, 9), (29, 13), (31, 16), (22, 20), (16, 25)], fill=BLACK, width=2)
            d.line([(29, 13), (22, 11), (15, 15), (9, 13)], fill=BLACK, width=1)
            d.line([(31, 16), (38, 21), (42, 21)], fill=BLACK, width=1)
            d.line([(22, 20), (24, 24)], fill=BLACK, width=1)

        face_eyes.append(im)

    # Row 2: face_pupil (16x16, 12x12, 8x8) with notched dark-red starburst outer ring
    pupils = []
    for sz, cr in ((16, 2.8), (12, 1.8), (8, 1.0)):
        im = Image.new("RGBA", (sz, sz), (0, 0, 0, 0))
        px = im.load()
        c = (sz - 1) / 2.0
        rmax = sz / 2.0 - 0.3
        for y in range(sz):
            for x in range(sz):
                d = math.hypot(x - c, y - c)
                ang = math.atan2(y - c, x - c)
                # Subtle 8-point geometric iris ring modulation on 16x16 and 12x12
                notch = 0.45 * math.cos(8 * ang) if sz >= 12 else 0.0
                if d <= rmax:
                    if d <= cr:
                        px[x, y] = BLACK
                    elif d >= rmax - 1.3 + notch:
                        px[x, y] = DARK_RED
                    else:
                        px[x, y] = RED
        gx0 = max(1, int(c - rmax * 0.55))
        gy0 = max(1, int(c - rmax * 0.55))
        for dy in (0, 1):
            for dx in (0, 1):
                if 0 <= gx0 + dx < sz and 0 <= gy0 + dy < sz:
                    px[gx0 + dx, gy0 + dy] = WHITE
        pupils.append(im)

    # Row 3: face_mouth (120x24 closed smile, 120x40 half open piano, 120x56 wide screaming)
    # 3a. Closed smile (120x24)
    m0 = Image.new("RGBA", (120, 24), (0, 0, 0, 0))
    m0px = m0.load()
    smile_mask = [[False] * 120 for _ in range(24)]
    for x in range(4, 116):
        t = (x - 59.5) / 56.0
        cy = 5 + 13.0 * (1.0 - t * t)
        half_th = 1.6 * (1.0 - 0.55 * t * t)
        for y in range(24):
            if abs(y - cy) <= half_th:
                smile_mask[y][x] = True
    sbdr = border_mask(smile_mask, 120, 24)
    for y in range(24):
        for x in range(120):
            if smile_mask[y][x]:
                m0px[x, y] = WHITE
            elif sbdr[y][x]:
                m0px[x, y] = BLACK

    def draw_piano_mouth(mw, mh, has_bottom_teeth, has_glow):
        im = Image.new("RGBA", (mw, mh), (0, 0, 0, 0))
        px = im.load()
        interior = [[False] * mw for _ in range(mh)]
        for x in range(3, mw - 3):
            t = (x - (mw - 1) / 2.0) / ((mw - 6) / 2.0)
            y_top = 3 + 4.0 * (t * t)
            y_bot = (mh - 3) - (mh - 10) * (t * t) * 0.85
            for y in range(mh):
                if y_top <= y <= y_bot:
                    interior[y][x] = True
        bdr = border_mask(interior, mw, mh)
        for y in range(mh):
            for x in range(mw):
                if bdr[y][x]:
                    px[x, y] = WHITE
                elif interior[y][x]:
                    px[x, y] = BLACK

        # Faint red starburst glow at back when wide open screaming (charging a beam)
        if has_glow:
            gx, gy = mw / 2.0, mh * 0.52
            for y in range(mh):
                for x in range(mw):
                    if not interior[y][x]:
                        continue
                    ang = math.atan2((y - gy) * 1.6, x - gx)
                    ray = 0.25 * max(0.0, math.cos(6 * ang))
                    d = math.hypot((x - gx) / 22.0, (y - gy) / 10.0) - ray
                    if d <= 0.45:
                        px[x, y] = RED
                    elif d <= 0.92:
                        px[x, y] = DARK_RED

        # Piano-key teeth along upper arch (and lower arch if screaming)
        # 14 white keys across x=12..108 (width 6 each, 1px black gap) + black-key cutouts in 2-3-2-3 pattern
        black_key_slots = {1, 2, 4, 5, 6, 8, 9, 11, 12}  # indices between keys where black keys sit
        key_w = 7
        start_x = 11
        n_keys = 14
        for ki in range(n_keys):
            kx0 = start_x + ki * key_w
            kx1 = kx0 + key_w - 2  # 1-px gap between white keys
            # Upper teeth
            t = ((kx0 + kx1) / 2.0 - (mw - 1) / 2.0) / ((mw - 6) / 2.0)
            y_top = int(round(3 + 4.0 * (t * t))) + 1
            tooth_len = 9 if not has_bottom_teeth else 10
            for x in range(kx0, kx1 + 1):
                for y in range(y_top, min(mh - 3, y_top + tooth_len)):
                    if interior[y][x]:
                        px[x, y] = WHITE
            # Lower teeth
            if has_bottom_teeth:
                y_bot = int(round((mh - 3) - (mh - 10) * (t * t) * 0.85)) - 1
                for x in range(kx0, kx1 + 1):
                    for y in range(max(y_top + 14, y_bot - 9), y_bot + 1):
                        if interior[y][x]:
                            px[x, y] = WHITE

        # Cut black-key notches at gumline between white keys (2-3-2-3 piano rhythm!)
        for ki in black_key_slots:
            if ki >= n_keys - 1:
                continue
            bx = start_x + (ki + 1) * key_w - 1
            t = (bx - (mw - 1) / 2.0) / ((mw - 6) / 2.0)
            y_top = int(round(3 + 4.0 * (t * t))) + 1
            for dx in (-1, 0, 1):
                for y in range(y_top, min(mh - 3, y_top + 5)):
                    if 0 <= bx + dx < mw and interior[y][bx + dx]:
                        px[bx + dx, y] = BLACK
                if has_bottom_teeth:
                    y_bot = int(round((mh - 3) - (mh - 10) * (t * t) * 0.85)) - 1
                    for y in range(max(4, y_bot - 4), y_bot + 1):
                        if 0 <= bx + dx < mw and interior[y][bx + dx]:
                            px[bx + dx, y] = BLACK
        return im

    m1 = draw_piano_mouth(120, 40, False, False)
    m2 = draw_piano_mouth(120, 56, True, True)
    mouths = [m0, m1, m2]

    # Layout 3 rows with wide magenta gaps
    pad = 14
    gap_x = 18
    gap_y = 18
    row1_w = sum(i.width for i in face_eyes) + gap_x * (len(face_eyes) - 1)
    row2_w = sum(i.width for i in pupils) + gap_x * (len(pupils) - 1)
    row3_w = sum(i.width for i in mouths) + gap_x * (len(mouths) - 1)
    W = pad * 2 + max(row1_w, row2_w, row3_w)
    H = pad * 2 + 28 + gap_y + 16 + gap_y + 56
    sheet = Image.new("RGBA", (W, H), MAGENTA)

    cx, cy = pad, pad
    for item in face_eyes:
        sheet.alpha_composite(item, (cx, cy))
        cx += item.width + gap_x

    cx, cy = pad, pad + 28 + gap_y
    for item in pupils:
        sheet.alpha_composite(item, (cx, cy + (16 - item.height) // 2))
        cx += item.width + gap_x

    cx, cy = pad, pad + 28 + gap_y + 16 + gap_y
    for item in mouths:
        sheet.alpha_composite(item, (cx, cy + (56 - item.height) // 2))
        cx += item.width + gap_x

    sheet_x4 = sheet.resize((W * 4, H * 4), Image.NEAREST)
    sheet_x4.save(os.path.join(OUT, "d2_sourdine_visage.png"))
    EXPORT.update(face_eyes=face_eyes, pupils=pupils, mouths=mouths)
    print("Saved d2_sourdine_visage.png", sheet_x4.size, "logical:", (W, H))


def make_e_attaques():
    # 1. beam: strip of 4 tiles of 16x12 = 64x12, seamless left-right
    beam = Image.new("RGBA", (64, 12), (0, 0, 0, 0))
    bpx = beam.load()
    for x in range(64):
        xm = x % 16
        # 1 px dark-red outer fringe (y=1, y=10) + crackling ticks at y=0, y=11
        if xm in (2, 3, 9, 10, 11):
            bpx[x, 0] = DARK_RED
        if xm in (5, 6, 13, 14):
            bpx[x, 11] = DARK_RED
        bpx[x, 1] = DARK_RED
        bpx[x, 10] = DARK_RED
        # 2 px red edges above and below
        for y in (2, 3, 8, 9):
            bpx[x, y] = RED
        # 4 px white core with short black notches
        for y in (4, 5, 6, 7):
            bpx[x, y] = WHITE
        if xm in (3, 4, 5):
            bpx[x, 4] = BLACK
        if xm in (10, 11):
            bpx[x, 7] = BLACK
        if xm == 14:
            bpx[x, 5] = BLACK

    # 2. beam_warning: strip of 4 tiles of 16x4 = 64x4, thin red dashes, 8 px on, 8 px off
    warn = Image.new("RGBA", (64, 4), (0, 0, 0, 0))
    wpx = warn.load()
    for x in range(64):
        if (x % 16) < 8:
            wpx[x, 1] = RED
            wpx[x, 2] = RED

    # 3. beam_muzzle: 32x32, razor-sharp spike starburst where beam leaves mouth (white flare + red rim, rays pointing left)
    muzzle = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    md = ImageDraw.Draw(muzzle)
    # Outer dark-red razor spikes shooting left
    rays_dr = [
        [(31, 16), (0, 16), (16, 12)],
        [(31, 14), (2, 6), (19, 12)],
        [(31, 18), (2, 26), (19, 20)],
        [(31, 11), (8, 0), (22, 10)],
        [(31, 21), (8, 31), (22, 22)],
        [(29, 7), (18, 0), (27, 10)],
        [(29, 25), (18, 31), (27, 22)],
    ]
    for pts in rays_dr:
        md.polygon(pts, fill=DARK_RED)
    md.ellipse([18, 4, 31, 28], fill=DARK_RED)
    # Bright red mid spikes & flare
    rays_r = [
        [(31, 16), (4, 16), (18, 13)],
        [(31, 15), (7, 9), (21, 13)],
        [(31, 17), (7, 23), (21, 19)],
        [(31, 12), (12, 4), (25, 11)],
        [(31, 20), (12, 28), (25, 21)],
    ]
    for pts in rays_r:
        md.polygon(pts, fill=RED)
    md.ellipse([20, 6, 31, 26], fill=RED)
    # White core starburst
    md.polygon([(31, 14), (9, 16), (31, 18)], fill=WHITE)
    md.polygon([(31, 12), (14, 8), (26, 15)], fill=WHITE)
    md.polygon([(31, 20), (14, 24), (26, 17)], fill=WHITE)
    md.ellipse([23, 9, 31, 23], fill=WHITE)

    # 4. Eighth note mask (16x16), for note_gold (note_black is drawn bigger, below)
    # Stem is 3 px wide (x=7..9) and flag is 3-4 px thick so 1-px outline leaves a clear interior core!
    note_mask = [[False] * 16 for _ in range(16)]
    # Oval note head bottom-left: centered around (5.5, 11.5), rx=4.3, ry=3.3
    for y in range(8, 16):
        for x in range(1, 11):
            dx = (x - 5.5) / 4.4
            dy = (y - 11.5) / 3.3
            if dx * dx + dy * dy <= 1.0:
                note_mask[y][x] = True
    # Vertical stem (x=7..9, y=1..12)
    for y in range(1, 13):
        for x in (7, 8, 9):
            note_mask[y][x] = True
    # Curved eighth flag top-right (y=1..7, thick enough to hold interior fill)
    flag_rows = {
        1: (7, 11),
        2: (7, 13),
        3: (7, 14),
        4: (9, 14),
        5: (11, 14),
        6: (11, 14),
        7: (12, 13),
    }
    for y, (x0, x1) in flag_rows.items():
        for x in range(x0, x1 + 1):
            note_mask[y][x] = True

    # Croche noire à frapper (34x34, 0,85 u : la taille de sa zone de frappe). En 16x16, elle faisait la moitié de sa
    # zone et se perdait dans le décor (« quasiment invisibles », 27/11). Tête ovale penchée, hampe de 4 px, crochet
    # courbe épais ; même dessin : corps noir, contour blanc de 1 px, œil rouge qui cligne.
    NB = 34
    note_big = [[False] * NB for _ in range(NB)]
    tilt = math.radians(-22)
    for y in range(NB):
        for x in range(NB):
            dx, dy = x + 0.5 - 12.0, y + 0.5 - 25.0
            u = (dx * math.cos(tilt) + dy * math.sin(tilt)) / 8.6
            v = (-dx * math.sin(tilt) + dy * math.cos(tilt)) / 6.2
            if u * u + v * v <= 1.0:
                note_big[y][x] = True
    for y in range(3, 25):                                   # hampe, à droite de la tête
        for x in range(17, 21):
            note_big[y][x] = True
    for k in range(41):                                      # crochet : courbe de Bézier épaissie
        t = k / 40
        bx = (1 - t) ** 2 * 19 + 2 * (1 - t) * t * 31 + t * t * 27
        by = (1 - t) ** 2 * 4 + 2 * (1 - t) * t * 9 + t * t * 19
        r = 2.6 - 1.1 * t
        for y in range(NB):
            for x in range(NB):
                if (x + 0.5 - bx) ** 2 + (y + 0.5 - by) ** 2 <= r * r:
                    note_big[y][x] = True

    def make_note_black(eye_open):
        im = outline_silhouette(note_big, NB, NB)
        npx = im.load()
        # œil rouge dans la tête, reflet blanc en haut à gauche ; fermé : une fente
        if eye_open:
            for y in (23, 24, 25):
                for x in range(8, 14):
                    npx[x, y] = RED
            for x in range(9, 14):
                npx[x, 26] = DARK_RED
            npx[8, 23] = npx[9, 23] = WHITE
        else:
            for x in range(8, 14):
                npx[x, 25] = RED
                npx[x, 26] = DARK_RED
        return im

    def make_note_gold():
        im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
        npx = im.load()
        for y in range(16):
            for x in range(16):
                if not note_mask[y][x]:
                    continue
                edge = any(
                    x + ox < 0 or x + ox >= 16 or y + oy < 0 or y + oy >= 16 or not note_mask[y + oy][x + ox]
                    for ox, oy in ((-1, 0), (1, 0), (0, -1), (0, 1))
                )
                if edge:
                    npx[x, y] = GOLD["c0"]
                else:
                    # Gold shading (light top-left -> dark bottom-right)
                    if (y == 10 and x in (4, 5)) or (y == 2 and x in (10, 11)):
                        npx[x, y] = GOLD["c5"]
                    elif y in (10, 11) and x in (3, 4, 5, 6):
                        npx[x, y] = GOLD["c4"]
                    elif y == 13 or (x == 8 and y > 7):
                        npx[x, y] = GOLD["c1"]
                    elif x >= 11 and y >= 4:
                        npx[x, y] = GOLD["c2"]
                    else:
                        npx[x, y] = GOLD["c3"]
        return im

    nb0 = make_note_black(True)
    nb1 = make_note_black(False)
    ng = make_note_gold()

    EXPORT.update(beam=beam, beam_warning=warn, beam_muzzle=muzzle, note_black=[nb0, nb1], note_gold=ng)
    items = [beam, warn, muzzle, nb0, nb1, ng]
    pad = 12
    gap = 16
    W = pad * 2 + sum(i.width for i in items) + gap * (len(items) - 1)
    H = pad * 2 + max(i.height for i in items)
    sheet = Image.new("RGBA", (W, H), MAGENTA)
    cx = pad
    for item in items:
        cy = (H - item.height) // 2
        sheet.alpha_composite(item, (cx, cy))
        cx += item.width + gap
    sheet_x4 = sheet.resize((W * 4, H * 4), Image.NEAREST)
    sheet_x4.save(os.path.join(OUT, "e_attaques.png"))
    print("Saved e_attaques.png", sheet_x4.size, "logical:", (W, H))



# ---------------------------------------------------------------- installation dans le jeu (--install)

GAME = os.path.join(ROOT, "game", "Assets", "_Project", "Art", "Kenta")


def transparent(im):
    """Le magenta des planches devient transparent."""
    im = im.copy()
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            if px[x, y][:3] == MAGENTA[:3]:
                px[x, y] = (0, 0, 0, 0)
    return im


def sky():
    """Ciel du monde sourd : noir, le soleil de la Sylve devenu une éclipse (anneau blanc, halo gris foncé)."""
    im = Image.new("RGBA", (640, 360), BLACK)
    d = ImageDraw.Draw(im)
    cx, cy = 500, 58
    d.ellipse([cx - 27, cy - 27, cx + 27, cy + 27], outline=DARK_GREY, width=1)
    d.ellipse([cx - 22, cy - 22, cx + 22, cy + 22], fill=BLACK, outline=WHITE, width=1)
    return im


def guardian():
    """La Sourdine en gardien du monde sourd : une masse noire bordée de blanc, deux yeux à pupille rouge qui
    regardent vers le héros (à gauche), un sourire. Le bas est caché derrière les montagnes."""
    W, H = 180, 240
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    mask = Image.new("L", (W, H), 0)
    md = ImageDraw.Draw(mask)
    md.ellipse([8, 6, W - 9, 2 * H], fill=255)
    m = mask.load()
    px = im.load()
    for y in range(H):
        for x in range(W):
            if m[x, y]:
                edge = any(not (0 <= x + dx < W and 0 <= y + dy < H) or not m[x + dx, y + dy]
                           for dx, dy in ((-1, 0), (1, 0), (0, -1)))
                px[x, y] = WHITE if edge else BLACK
    eye, pupil, mouth = EXPORT["face_eyes"][0], EXPORT["pupils"][1], EXPORT["mouths"][0]
    for ex, flip in ((30, False), (W - 30 - eye.width, True)):
        e = eye.transpose(Image.FLIP_LEFT_RIGHT) if flip else eye
        im.alpha_composite(e, (ex, 70))
        im.alpha_composite(pupil, (ex + (eye.width - pupil.width) // 2 - 7, 70 + (eye.height - pupil.height) // 2))
    im.alpha_composite(mouth, ((W - mouth.width) // 2, 128))
    return im


def thumbnail(layers):
    """Vignette 40×24 de l'écran des niveaux : le décor sourd réduit, en noir, gris et blanc."""
    im = Image.new("RGBA", (640, 360), BLACK)
    for layer in layers:
        im.alpha_composite(layer)
    small = im.crop((120, 90, 520, 330)).convert("L").resize((40, 24), Image.BOX)
    out = Image.new("RGBA", (40, 24), BLACK)
    for y in range(24):
        for x in range(40):
            v = small.getpixel((x, y))
            out.putpixel((x, y), WHITE if v > 70 else GREY if v > 30 else DARK_GREY if v > 12 else BLACK)
    return out


def negative_enemy(src):
    """Un ennemi de la DA en négatif : contour et blancs (yeux, points) en blanc, le reste en noir."""
    im = Image.open(src).convert("RGBA")
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a < 128:
                continue
            lum = 0.3 * r + 0.59 * g + 0.11 * b
            px[x, y] = WHITE if lum < 45 or lum > 200 else BLACK
    return im


# ---------------------------------------------------------------- la Sourdine flottante (boss, 27/11 après-midi)

FLOAT_W, FLOAT_H = 176, 168
FLOAT_EYES = ((20, 46), (108, 46))          # coin haut-gauche des deux yeux (48×28) ; l'œil droit est retourné
FLOAT_MOUTH_TOP = 88                         # haut de la bouche (120 de large, centrée)


def float_mask():
    """Silhouette de la Sourdine flottante : une masse arrondie, deux cornes, des coulures qui pendent, des traînées de
    signal sur les côtés (le glitch de la DA)."""
    W, H = FLOAT_W, FLOAT_H
    m = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(m)
    d.ellipse([8, 16, W - 9, 148], fill=255)
    d.polygon([(34, 40), (62, 22), (20, 2)], fill=255)                       # corne gauche, vers le haut et l'extérieur
    d.polygon([(W - 35, 40), (W - 63, 22), (W - 21, 2)], fill=255)          # corne droite
    for x, w, length in ((40, 8, 20), (66, 6, 12), (104, 8, 24), (132, 6, 14)):   # coulures sous le corps
        d.rectangle([x - w // 2, 136, x + w // 2 - 1, 136 + length], fill=255)
        d.ellipse([x - w // 2 - 1, 136 + length - w // 2, x + w // 2, 136 + length + w // 2 + 1], fill=255)
    for y, x0, x1, h in ((60, 0, 26, 2), (110, 2, 30, 1), (44, W - 30, W - 1, 2), (96, W - 28, W - 1, 3)):
        d.rectangle([x0, y, x1, y + h - 1], fill=255)                          # traînées de signal déchiré
    return m


def outline_from_mask(m):
    """Masse noire, contour blanc d'1 px (voisins en croix)."""
    W, H = m.size
    mp = m.load()
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = im.load()
    for y in range(H):
        for x in range(W):
            if not mp[x, y]:
                continue
            edge = any(not (0 <= x + dx < W and 0 <= y + dy < H) or not mp[x + dx, y + dy]
                       for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)))
            px[x, y] = WHITE if edge else BLACK
    return im


def make_float_body():
    """La Sourdine flottante (le visage est posé par le jeu : yeux, pupilles et bouche animés), et ses deux moitiés
    tranchées par le dernier coup de katana (yeux fêlés, bouche qui hurle), coupées en diagonale avec un fil blanc."""
    body = outline_from_mask(float_mask())
    EXPORT["float_body"] = body
    dead = body.copy()
    eye = EXPORT["face_eyes"][4]                                              # fêlé
    for k, (ex, ey) in enumerate(FLOAT_EYES):
        dead.alpha_composite(eye.transpose(Image.FLIP_LEFT_RIGHT) if k else eye, (ex, ey))
    mouth = EXPORT["mouths"][2]                                               # hurlement
    dead.alpha_composite(mouth, ((FLOAT_W - mouth.width) // 2, FLOAT_MOUTH_TOP))
    W, H = FLOAT_W, FLOAT_H
    cut = lambda x: H / 2 + (x - W / 2) * -0.35                               # du bas-gauche vers le haut-droit
    halves = []
    for top in (True, False):
        h = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        src, dst = dead.load(), h.load()
        for y in range(H):
            for x in range(W):
                a = src[x, y]
                if a[3] < 128 or (y < cut(x)) != top:
                    continue
                across = y + (1 if top else -1)                               # le pixel de l'autre côté de la coupe
                on_cut = 0 <= across < H and src[x, across][3] >= 128 and (across < cut(x)) != top
                dst[x, y] = WHITE if on_cut else a
        halves.append(h)
    EXPORT["float_half_top"], EXPORT["float_half_bottom"] = halves


def install():
    B = os.path.join(GAME, "Gen", "biomes")
    single = {"wall": "wall_sourd", "block": "block_sourd", "ceil": "ceil_sourd", "post": "post_sourd",
              "platform": "platform_sourd", "ground": "ground_sourd", "fill": "fill_sourd", "fill_deep": "fill_sourd_deep",
              "hazard": "hazard_sourd", "cliff": "cliff_sourd"}
    written = []
    for key, name in single.items():
        EXPORT[key].save(os.path.join(B, name + ".png"))
        written.append(name)
    for k, d in enumerate(EXPORT["decos"]):
        d.save(os.path.join(B, f"deco_sourd_{k}.png"))
        written.append(f"deco_sourd_{k}")
    layers = []
    for key, name in (("b1_far", "far"), ("b2_mid", "mid"), ("b3_near", "near")):
        layer = transparent(EXPORT[key])                     # 640×360, raccord gauche-droite
        layers.append(layer)
        wide = Image.new("RGBA", (1280, 360), (0, 0, 0, 0))
        wide.alpha_composite(layer, (0, 0))
        wide.alpha_composite(layer, (640, 0))
        wide.save(os.path.join(B, f"bg_sourd_{name}.png"))
        written.append(f"bg_sourd_{name}")
    sky().save(os.path.join(B, "bg_sourd_sky.png"))
    guardian().save(os.path.join(B, "guardian_sourd.png"))
    written += ["bg_sourd_sky", "guardian_sourd"]
    gj = os.path.join(B, "guardians.json")
    data = json.load(open(gj, encoding="utf-8"))
    data["items"] = [g for g in data["items"] if g["id"] != "sourd"] + [{"id": "sourd", "x": 139.0, "y": 20.0}]
    json.dump(data, open(gj, "w", encoding="utf-8"), indent=1)
    thumbnail(layers).save(os.path.join(GAME, "Gen", "ui", "thumb_sourd.png"))
    written.append("ui/thumb_sourd")
    for kind in ("squash", "stretch", "broken"):
        negative_enemy(os.path.join(GAME, "Pdf", f"enemy_djembe_{kind}.png")).save(
            os.path.join(GAME, "Pdf", f"enemy_djembe_sourd_{kind}.png"))
        written.append(f"Pdf/enemy_djembe_sourd_{kind}")
    # la Sourdine en boss (phase 4) : corps, visage, attaques, yeux du décor (Art/Kenta/Gen/boss)
    Bo = os.path.join(GAME, "Gen", "boss")
    os.makedirs(Bo, exist_ok=True)
    def put(im, name):
        transparent(im).save(os.path.join(Bo, name + ".png"))
        written.append("boss/" + name)
    put(EXPORT["body"], "sourdine_body")
    make_float_body()
    put(EXPORT["float_body"], "sourdine_float")
    put(EXPORT["float_half_top"], "sourdine_half_top")
    put(EXPORT["float_half_bottom"], "sourdine_half_bottom")
    for k, mode in enumerate(("open", "half", "closed", "wide", "cracked")):
        put(EXPORT["face_eyes"][k], f"sourdine_eye_{mode}")
    for k, size in enumerate(("big", "mid", "pin")):
        put(EXPORT["pupils"][k], f"sourdine_pupil_{size}")
    for k, mode in enumerate(("smile", "half", "scream")):
        put(EXPORT["mouths"][k], f"sourdine_mouth_{mode}")
    put(EXPORT["beam"], "beam")
    put(EXPORT["beam_warning"], "beam_warning")
    put(EXPORT["beam_muzzle"], "beam_muzzle")
    for k, im in enumerate(EXPORT["note_black"]):
        put(im, f"note_black_{k}")
    put(EXPORT["note_gold"], "note_gold")
    for size in ("s", "l"):
        for k, mode in enumerate(("open", "look", "half", "closed")):
            put(EXPORT[f"eye_{size}"][k], f"eye_{size}_{mode}")
    print("installé :", ", ".join(written))

if __name__ == "__main__":
    import sys
    only = set(a for a in sys.argv[1:] if not a.startswith("--"))  # ex. : python3 gen_sheets.py a1 b1 (rien = toutes)
    if "--install" in sys.argv:
        only = set()                                      # l'installation a besoin de toutes les planches

    if not only or 'a1' in only:
        make_a1_obstacles()
    if not only or 'a2' in only:
        make_a2_terrain()
    if not only or 'b1' in only:
        make_b1_far()
    if not only or 'b2' in only:
        make_b2_mid()
    if not only or 'b3' in only:
        make_b3_near()
    if not only or 'b4' in only:
        make_b4_vide_far()
    if not only or 'b5' in only:
        make_b5_vide_near()
    if not only or 'c' in only:
        make_c_yeux()
    if not only or 'd1' in only:
        make_d1_sourdine_corps()
    if not only or 'd2' in only:
        make_d2_sourdine_visage()
    if not only or 'e' in only:
        make_e_attaques()
    if "--install" in sys.argv:
        install()
