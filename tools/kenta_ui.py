"""Police pixel 5×7, logo KENTA, cœurs, cadres, pièces et totem (pixel art). Utilisé par gen-kenta-assets.py."""
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


SHADOW = "#1A1426"


def glyph_image(ch, color=WHITE, shadow=SHADOW):
    """Glyphe proportionnel : colonnes vides retirées (espace = 3 px), ombre portée d'1 px vers le bas
    (lisible même agrandi). L'avance du texte = largeur de l'image + 1 px (PixelFontData.AdvanceOf)."""
    rows = glyph_rows(ch)
    cols = [x for x in range(len(rows[0])) if any(r[x] == "#" for r in rows)]
    if cols:
        x0, x1 = min(cols), max(cols)
    else:
        x0, x1 = 0, 2
    w = x1 - x0 + 1
    p = Pix(w, len(rows) + 1)
    for y, r in enumerate(rows):
        for x in range(x0, x1 + 1):
            if r[x] == "#":
                p.px(x - x0, y + 1, shadow)
    for y, r in enumerate(rows):
        for x in range(x0, x1 + 1):
            if r[x] == "#":
                p.px(x - x0, y, color)
    return p


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
    """Cadre 16×16 (9-slice, bordure 4) : fond sombre, coins arrondis de 2 px, contour fin, liseré clair en haut."""
    p = Pix(16, 16)
    bg, edge, hi, ink = "#1D1830", "#5A5088", "#9A90C8", INK
    p.rect(0, 0, 16, 16, ink)
    p.rect(1, 1, 15, 15, edge)
    p.rect(2, 2, 14, 14, bg)
    p.rect(2, 2, 14, 3, hi)                      # liseré clair
    p.rect(2, 13, 14, 14, "#16122A")             # ombre interne en bas
    for (x, y) in ((0, 0), (1, 0), (0, 1), (15, 0), (14, 0), (15, 1), (0, 15), (1, 15), (0, 14), (15, 15), (14, 15), (15, 14)):
        p.px(x, y, (0, 0, 0, 0))                 # coins arrondis
    for (x, y) in ((1, 1), (14, 1), (1, 14), (14, 14)):
        p.px(x, y, ink)
    return p


def draw_star():
    """Étoile de difficulté 5×5, blanche (teintée or ou atténuée dans Unity) : écran de sélection de niveau."""
    p = Pix(5, 5)
    for y, row in enumerate(("..#..", ".###.", "#####", ".###.", ".#.#.")):
        for x, ch in enumerate(row):
            if ch == "#":
                p.px(x, y, "#FFFFFF")
    return p


def draw_plate():
    """Plaque 8×8 (9-slice, bordure 3) : fond sombre translucide aux coins arrondis, sous les textes posés sur le décor."""
    p = Pix(8, 8)
    p.rect(0, 0, 8, 8, (20, 16, 28, 190))
    for (x, y) in ((0, 0), (7, 0), (0, 7), (7, 7)):
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
    """Pièce frappée d'une croche, 4 images de rotation. Assez grande pour se lire en vue large (640 px), et jamais de
    profil (l'ancienne image de 4 px se lisait comme un trait orange parasite dans les arcs de pièces)."""
    widths = [15, 13, 9, 13]
    wv = widths[frame]
    p = Pix(17, 17)
    cx, cy = 8, 8
    p.ellipse(cx, cy, wv // 2, 7, "#FFD23A")
    p.ellipse(cx, cy, max(1, wv // 2 - 2), 5, "#E8A020")
    p.ellipse(cx - 1, cy - 1, max(1, wv // 2 - 3), 3, "#FFE87A")
    p.px(cx - wv // 2 + 2, cy - 4, "#FFF8C8")
    # croche (seulement quand la pièce est assez large pour la montrer)
    if wv >= 11:
        p.rect(cx, cy - 3, cx + 1, cy + 3, "#7A4A10")
        p.rect(cx - 2, cy + 2, cx, cy + 4, "#7A4A10")
        p.rect(cx + 1, cy - 3, cx + 3, cy - 2, "#7A4A10")
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
