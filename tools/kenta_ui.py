"""Police pixel 5×7, logo KENTA, cœurs, cadres, pièces, Sourdine et totem (pixel art). Utilisé par gen-kenta-assets.py."""
import math
import random

from pixlib import Pix, blob, col, mix, outlined, shade

INK = "#14101C"
WHITE = "#FFFFFF"

# ---------------------------------------------------------------- police 5×7
_G = {}


def g(ch, rows):
    assert len(rows) == 7 and all(len(r) == 5 for r in rows), ch
    _G[ch] = rows


g("A", [".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"])
g("B", ["####.", "#...#", "#...#", "####.", "#...#", "#...#", "####."])
g("C", [".###.", "#...#", "#....", "#....", "#....", "#...#", ".###."])
g("D", ["####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####."])
g("E", ["#####", "#....", "#....", "####.", "#....", "#....", "#####"])
g("F", ["#####", "#....", "#....", "####.", "#....", "#....", "#...."])
g("G", [".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".####"])
g("H", ["#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"])
g("I", [".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###."])
g("J", ["..###", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##.."])
g("K", ["#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#"])
g("L", ["#....", "#....", "#....", "#....", "#....", "#....", "#####"])
g("M", ["#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#"])
g("N", ["#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#"])
g("O", [".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."])
g("P", ["####.", "#...#", "#...#", "####.", "#....", "#....", "#...."])
g("Q", [".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#"])
g("R", ["####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#"])
g("S", [".####", "#....", "#....", ".###.", "....#", "....#", "####."])
g("T", ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.."])
g("U", ["#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."])
g("V", ["#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.."])
g("W", ["#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#"])
g("X", ["#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#"])
g("Y", ["#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.."])
g("Z", ["#####", "....#", "...#.", "..#..", ".#...", "#....", "#####"])
g("0", [".###.", "#..##", "#.#.#", "#.#.#", "#.#.#", "##..#", ".###."])
g("1", ["..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###."])
g("2", [".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####"])
g("3", ["####.", "....#", "....#", ".###.", "....#", "....#", "####."])
g("4", ["...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#."])
g("5", ["#####", "#....", "####.", "....#", "....#", "#...#", ".###."])
g("6", [".###.", "#....", "#....", "####.", "#...#", "#...#", ".###."])
g("7", ["#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..."])
g("8", [".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###."])
g("9", [".###.", "#...#", "#...#", ".####", "....#", "....#", ".###."])
g(" ", ["....."] * 7)
g(".", [".....", ".....", ".....", ".....", ".....", ".##..", ".##.."])
g(",", [".....", ".....", ".....", ".....", ".##..", "..#..", ".#..."])
g(":", [".....", ".##..", ".##..", ".....", ".##..", ".##..", "....."])
g("!", ["..#..", "..#..", "..#..", "..#..", "..#..", ".....", "..#.."])
g("?", [".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.."])
g("-", [".....", ".....", ".....", "#####", ".....", ".....", "....."])
g("+", [".....", "..#..", "..#..", "#####", "..#..", "..#..", "....."])
g("/", ["....#", "....#", "...#.", "..#..", ".#...", "#....", "#...."])
g("×", [".....", ".....", "#...#", ".#.#.", "..#..", ".#.#.", "#...#"])
g("'", ["..#..", "..#..", ".#...", ".....", ".....", ".....", "....."])
g("(", ["...#.", "..#..", ".#...", ".#...", ".#...", "..#..", "...#."])
g(")", [".#...", "..#..", "...#.", "...#.", "...#.", "..#..", ".#..."])
g("·", [".....", ".....", ".....", "..#..", ".....", ".....", "....."])
g("%", ["##..#", "##..#", "...#.", "..#..", ".#...", "#..##", "#..##"])
g("▼", [".....", ".....", "#####", ".###.", "..#..", ".....", "....."])
g("▲", [".....", ".....", "..#..", ".###.", "#####", ".....", "....."])
g("◄", [".....", "...#.", "..##.", ".###.", "..##.", "...#.", "....."])
g("►", [".....", ".#...", ".##..", ".###.", ".##..", ".#...", "....."])
g("Œ", [".####", "#.#..", "#.#..", "#.###", "#.#..", "#.#..", ".####"])

ACCENTS = {
    "É": ("E", "acute"), "È": ("E", "grave"), "Ê": ("E", "circ"), "Ë": ("E", "diaer"), "À": ("A", "grave"), "Â": ("A", "circ"),
    "Ç": ("C", "cedil"), "Ô": ("O", "circ"), "Î": ("I", "circ"), "Ï": ("I", "diaer"), "Ù": ("U", "grave"), "Û": ("U", "circ"),
}
ACC_ROWS = {"acute": ["...#.", "..#.."], "grave": [".#...", "..#.."], "circ": ["..#..", ".#.#."], "diaer": [".....", ".#.#."], "cedil": None}

CELL_W, CELL_H = 5, 10          # 2 lignes d'accent + 7 de lettre + 1 de cédille


def glyph_rows(ch):
    """Retourne les 10 lignes (2 accent + 7 + 1 cédille) d'un caractère, sous forme de listes de 5 booléens."""
    blank = "....."
    if ch in _G:
        return [blank, blank] + list(_G[ch]) + [blank]
    if ch in ACCENTS:
        base, acc = ACCENTS[ch]
        top = ACC_ROWS[acc] or [blank, blank]
        bottom = [blank]
        if acc == "cedil":
            bottom = ["..#.."]
        return list(top) + list(_G[base]) + bottom
    raise KeyError(ch)


def glyph_image(ch, color=WHITE, outline=INK):
    rows = glyph_rows(ch)
    p = Pix(CELL_W, CELL_H)
    for y, r in enumerate(rows):
        for x, c in enumerate(r):
            if c == "#":
                p.px(x, y, color)
    return outlined(p, outline)


ALL_CHARS = list(_G.keys()) + list(ACCENTS.keys())


# ---------------------------------------------------------------- logo KENTA
def draw_logo():
    letters = "KENTA"
    s = 4
    gap = 3
    w = len(letters) * (5 * s) + (len(letters) - 1) * gap
    h = 7 * s
    body = Pix(w + 4, h + 4)
    shadow = Pix(w + 4, h + 4)
    hi = "#FFA566"
    base = "#FF6A30"
    low = "#D8401A"
    for i, ch in enumerate(letters):
        x0 = i * (5 * s + gap)
        for y, r in enumerate(_G[ch]):
            for x, c in enumerate(r):
                if c == "#":
                    for dy in range(s):
                        for dx in range(s):
                            yy = y * s + dy
                            color = hi if yy % (7 * s) < 5 else (low if yy % (7 * s) >= 7 * s - 6 else base)
                            body.px(x0 + x * s + dx, yy, color)
                            shadow.px(x0 + x * s + dx + 2, yy + 2, "#8A1A0A")
    out = Pix(w + 4, h + 4)
    out.paste(shadow, 0, 0)
    out.paste(body, 0, 0)
    return outlined(outlined(out, "#14101C"), "#14101C", 1)


# ---------------------------------------------------------------- cœurs, cadres, boutons
def draw_heart(full=True):
    pat = [".###...###.", "#####.#####", "###########", "###########", ".#########.", "..#######..", "...#####...", "....###....", ".....#....."]
    p = Pix(11, 9)
    base = "#E03A58" if full else "#3A3048"
    hi = "#FF9AAA" if full else "#4C4260"
    lo = "#B0203E" if full else "#2A2238"
    for y, r in enumerate(pat):
        for x, c in enumerate(r):
            if c == "#":
                p.px(x, y, base)
    if full:
        p.px(1, 1, hi)
        p.px(2, 1, hi)
        p.px(1, 2, hi)
        for (x, y) in ((9, 2), (9, 3), (8, 4), (7, 5), (6, 6)):
            p.px(x, y, lo)
    return outlined(p)


def draw_panel():
    """Cadre 16×16 (9-slice, bordure 4) : fond sombre, contour clair, coins coupés."""
    p = Pix(16, 16)
    p.rect(1, 0, 15, 16, INK)
    p.rect(0, 1, 16, 15, INK)
    p.rect(1, 1, 15, 15, "#8A84B0")
    p.rect(2, 2, 14, 14, "#231E3C")
    p.rect(2, 2, 14, 3, "#322C54")
    for x, y in ((0, 0), (15, 0), (0, 15), (15, 15)):
        p.px(x, y, (0, 0, 0, 0))
    return p


def draw_button(color):
    p = Pix(12, 12)
    p.circle(5, 5, 5, color)
    p.circle(4, 3, 2, shade(color, 0.45))
    p.rect(2, 8, 9, 10, shade(color, -0.35))
    return outlined(p)


def draw_stick():
    p = Pix(14, 14)
    p.ellipse(7, 10, 6, 3, "#4C426C")
    p.rect(6, 4, 8, 10, "#8A84B0")
    p.circle(7, 3, 3, "#E03A58")
    p.px(6, 2, "#FF9AAA")
    return outlined(p)


# ---------------------------------------------------------------- pièces, totem
def draw_coin(frame):
    widths = [11, 9, 4, 9]
    wv = widths[frame]
    p = Pix(13, 13)
    cx, cy = 6, 6
    if wv > 4:
        p.ellipse(cx, cy, wv // 2, 5, "#FFD23A")
        p.ellipse(cx, cy, wv // 2 - 2, 3, "#E8A020")
        p.ellipse(cx - 1, cy - 1, max(1, wv // 2 - 3), 2, "#FFE87A")
        p.px(cx - wv // 2 + 1, cy - 3, "#FFF8C8")
        # croche
        p.rect(cx, cy - 2, cx + 1, cy + 2, "#7A4A10")
        p.rect(cx - 1, cy + 1, cx, cy + 3, "#7A4A10")
        p.px(cx + 1, cy - 2, "#7A4A10")
    else:
        p.rect(cx - 1, cy - 5, cx + 2, cy + 6, "#E8A020")
        p.rect(cx - 1, cy - 5, cx, cy + 6, "#FFD23A")
    return outlined(p)


def draw_totem():
    """Gardien miniature : borne de pierre gravée d'une croche dorée (checkpoint)."""
    p = Pix(20, 46)
    p.rect(3, 40, 17, 46, "#7A7E8E")
    p.rect(4, 6, 16, 42, "#9AA0B2")
    p.rect(4, 6, 8, 42, "#B4BAC8")
    p.rect(13, 6, 16, 42, "#7C8296")
    p.ellipse(10, 6, 7, 5, "#9AA0B2")
    p.ellipse(9, 5, 4, 2, "#B4BAC8")
    p.rect(2, 44, 18, 46, "#5A5E6E")
    # croche gravée
    p.circle(8, 28, 3, "#FFD23A")
    p.rect(10, 14, 12, 28, "#FFD23A")
    p.poly([(12, 14), (17, 18), (17, 22), (12, 18)], "#FFD23A")
    # yeux clos
    p.line((6, 11), (8, 11), "#5A5E6E", 1)
    p.line((12, 11), (14, 11), "#5A5E6E", 1)
    return outlined(p)


# ---------------------------------------------------------------- la Sourdine
def draw_sourdine():
    """Mur de silence : masse noire à deux grands yeux blancs (pupille rouge) et un sourire, bord droit en grumeaux."""
    rng = random.Random("sourdine")
    p = Pix(104, 135)
    body, edge = "#14101C", "#1C1830"
    p.rect(0, 0, 80, 135, body)
    y = -6
    while y < 140:
        r = rng.randint(14, 22)
        p.circle(80 + rng.randint(-2, 6), y + r // 2, r, body)
        y += r + rng.randint(-2, 6)
    for i in range(0, 135, 9):                      # rayures de pluie
        p.line((10 + (i * 3) % 70, i), (22 + (i * 3) % 70, i + 22), "#262240", 1)
    # yeux
    for ex in (30, 62):
        p.ellipse(ex, 55, 9, 13, "#FFFFFF")
        p.ellipse(ex + 2, 57, 4, 6, "#D82446")
        p.ellipse(ex + 2, 55, 2, 3, "#14101C")
        p.px(ex - 2, 50, "#FFFFFF")
    # sourire
    pts = [(20, 84), (28, 92), (40, 96), (52, 96), (64, 92), (72, 84)]
    for a, b in zip(pts, pts[1:]):
        p.line(a, b, "#0A0810", 3)
    return p


def draw_veil():
    """Calque gris « sans shader » : la couleur disparaît près de la Sourdine, tramage ordonné sur 4 niveaux."""
    w, h = 56, 135
    p = Pix(w, h)
    bayer = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
    for x in range(w):
        level = 1 - x / (w - 1)                      # 1 à gauche → 0 à droite
        for y in range(h):
            if bayer[y % 4][x % 4] / 16.0 < level * 0.9:
                p.px(x, y, (92, 98, 108, 150))
    return p
