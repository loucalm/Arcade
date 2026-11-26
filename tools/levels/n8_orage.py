"""Niveau 8 — Le Grand Orage, l'épreuve ultime, sur le morceau fourni par Lou (`Niveau8.ogg`), 180 BPM.

Le niveau le plus dur : 180 BPM, le tempo le plus rapide du jeu (9,6 u/s ; plus vite, les frappes en avance ratent
l'ennemi : tolérances réglées pour 3,2 u par temps), toutes les mécaniques, des rafales de frappes en croches sur les passages où la musique tape en croches (la sensation d'un
tempo doublé, sans raccourcir les sauts), et un écran qui se détraque au rythme du morceau : tremblements et coups de
zoom sur les frappes, dédoublé, miroir, tête en bas, grésillement sur les phrases. Rien n'y est appris.

**Chaque action tombe sur une attaque du morceau** (carte des accents, mesure par mesure, dans le commentaire de
chaque scène : # forte, sinon moyenne). Le morceau a des blancs (56-64, 232-240) : on y vole ou on y dévale, sans appui.

Morceau (1 min 53, 336 temps ; la structure se lit à 90, en demi-temps) :
  python3 tools/analyse-song.py game/Assets/_Project/Audio/Music/n8_orage.ogg tools/data/n8_orage_analysis.json \
      --bpm 180 --offset 0.035 --beats 336
Forme (temps) : A 0-56, blanc 56-64, B 64-96, C (drop) 96-128, D 128-160, E (rafales) 160-176, A' 176-232,
blanc 232-240, B' 240-272, C' (drop final) 272-304, D' (fin) 304-336.
"""
from levelgen.builder import Level, cuirasses, grotte, lianes, notes
from levelgen.fiche import ALL, Fiche

SECTIONS = [("intro", 0), ("blanc", 56), ("couplet", 64), ("refrain", 96), ("couplet 2", 128), ("tempête", 160),
            ("reprise", 176), ("blanc 2", 232), ("couplet 3", 240), ("refrain 2", 272), ("fin", 304)]


def hits(L, *beats):
    for b in beats:
        L.enemy(b)


def scene_a(L, b, second=False):
    """A (0-56, puis 176-232). Accents : 4 5 6 7 | 8 9 10 11# | 12# 13 14 15# | 16# 16,5 17 17,5 18,5 19# |
    20 21# 22 22,5 23# | 24 24,5 25# 26 26,5 27 27,5 | 28,5# 29,5 30 31# | 32# 33 34 35 | 36# 37# 38# 39 |
    40 41 42,5 43 | 44 45# 46# 47# | 48# 48,5 49 49,5 50,5# 51# 51,5 | 52# 52,5# 53 54# 54,5# 55 | 56# 57."""
    hits(L, b + 4, b + 5)
    L.gap(b + 6, 0.5)
    L.wall(b + 7)
    hits(L, b + 9, b + 10)
    L.step(b + 11, +1)
    hits(L, b + 12, b + 13, b + 14)
    L.gap(b + 15, 0.5)
    if not second:
        hits(L, b + 16, b + 16.5, b + 17)
        L.wall(b + 19)
        hits(L, b + 20, b + 21, b + 22, b + 22.5)
        L.gap(b + 23, 0.5)
        hits(L, b + 24, b + 24.5)
        cuirasses(L, b + 26, (1.5,))            # 25# saut, 26 appui, → 27,5
        L.gap(b + 28.5, 0.5)
        hits(L, b + 29.5, b + 30)
        L.wall(b + 31)
    else:
        # la reprise : le monde se retourne sur la même musique
        grotte(L, b + 16, b + 31, jumps=(b + 19, b + 23, b + 28.5), hanging=(b + 21, b + 25, b + 27, b + 30))
    hits(L, b + 32, b + 33, b + 34)
    L.step(b + 35, +1)
    if not second:
        hits(L, b + 36, b + 37)
        L.gap(b + 38, 0.5)
        lianes(L, b + 40, [1.75])               # 40 saut, lâcher 42,5, → 43,5
    else:
        # l'arrière-plan : on passe derrière le premier plan
        h = L.h
        L.to_plane(b + 36, 1.5, "to_back")      # → 37,5
        L.back(b + 37, 2.2, 1.0)                # trou de 39,2 à 39,7
        L.back(b + 39.7, 4.3, 1.0)
        L.add(b + 39, "jump", variant="back")
        L.back_enemy(b + 38)
        L.back_enemy(b + 40)
        L.back_enemy(b + 41)
        L.to_plane(b + 43, 1.0, "to_front")     # → 44
        L.h = h
    hits(L, b + 45, b + 46)                    # 43,5-45 : on reprend pied (point de reprise)
    L.gap(b + 47, 0.5)
    hits(L, b + 48, b + 48.5, b + 49, b + 49.5)
    L.step(b + 50.5, +1)
    hits(L, b + 52, b + 52.5)
    L.gap(b + 53, 0.5)
    hits(L, b + 54, b + 54.5)
    L.wall(b + 55)
    L.enemy(b + 56)
    # le blanc : la musique s'arrête, on s'envole et on dévale sans un appui
    L.bounce(b + 57, 2.5, +4)                   # 57 → 59,5
    L.ramp(b + 60, 2.0, -8)


def scene_b(L, b, third=False):
    """B (64-96, puis 240-272). Accents : 64# 67 | 69 71 | 72 72,5 73 74 75# | 76 77# 78 79 | 81 83 | 85# 87 |
    88 88,5 89 90,5 91 | 92 93 94 94,5 95 95,5."""
    L.enemy(b + 0)
    L.gap(b + 3, 0.5)
    L.enemy(b + 5)
    cuirasses(L, b + 8, (1.0, 1.0, 1.0))        # 7 saut, 8 9 10 appuis, → 11
    hits(L, b + 13, b + 14)                    # 11-13 : on reprend pied (point de reprise)
    L.gap(b + 15, 0.5)
    if not third:
        lianes(L, b + 17, [1.25, 1.0])          # 17 saut, lâchers 19 et 21, → 22
        L.flyer(b + 18)
    else:
        notes(L, b + 17, [0.5, 1.5, 2.5, 1.5])  # 17 saut, notes 18 19 20 21, → 22
    L.wall(b + 23)
    hits(L, b + 24, b + 24.5, b + 25)
    L.gap(b + 26.5, 0.5)
    hits(L, b + 28, b + 29, b + 30, b + 30.5)
    L.bounce(b + 31.5, 1.5, +2)                 # 95,5 → 97 : le drop


def scene_c(L, b, final=False):
    """C, le drop (96-128, puis 272-304). Accents : 96 97# 99 | 100 101# 101,5 102 102,5 103 |
    104 105 105,5# 106# 106,5 107# 107,5# | 108-111,5 : toutes les croches # | 112-114 # 114,5 115 115,5 |
    116# 116,5 117# 118 119# | 120 121# 121,5 122 122,5# 123# | 124# 124,5 125# 126# 127# 127,5."""
    L.enemy(b + 3)
    if not final:
        hits(L, b + 5, b + 5.5)
        L.gap(b + 6.5, 0.5)
    else:                                       # le drop final : 276,5 277# 278 279
        hits(L, b + 4.5, b + 5)
        L.gap(b + 6, 0.5)
    cuirasses(L, b + 9, (1.0, 1.0))             # 8 saut, 9 et 10 appuis, → 11
    # 108-112 : la rafale, une frappe par croche (la musique double)
    hits(L, b + 12, b + 12.5, b + 13, b + 13.5, b + 14, b + 14.5, b + 15, b + 15.5)
    L.gap(b + 16, 0.5)
    hits(L, b + 17, b + 17.5, b + 18)
    L.wall(b + 19)
    hits(L, b + 20, b + 20.5, b + 21)
    L.step(b + 22, +1)
    L.enemy(b + 23)
    L.ramp(b + 24, 1.5, -6 if not final else -8)   # la dévalée sur 124-125
    if not final:
        hits(L, b + 26, b + 27, b + 27.5)
    else:
        hits(L, b + 26, b + 26.5, b + 27)


def scene_d(L, b):
    """D (128-160). Accents : 128# 129 130 131 | 133 135 | 136 137 138 138,5 139# 139,5 | 140# 140,5 141# 142# 143 |
    145 147 | 149 150 151 | 152 152,5 153 155# | 156 157 158 158,5 159,5."""
    grotte(L, b + 0, b + 8, jumps=(b + 2,), hanging=(b + 3, b + 5, b + 7))
    hits(L, b + 9, b + 10, b + 10.5)
    L.wall(b + 11)
    hits(L, b + 12, b + 12.5, b + 13)
    L.gap(b + 14, 0.5)
    L.enemy(b + 15)
    lianes(L, b + 17, [1.25, 1.0])              # 17 saut, lâchers 19 et 21, → 22
    L.flyer(b + 18)
    hits(L, b + 22, b + 23)
    L.gap(b + 24, 0.5)
    hits(L, b + 25)
    L.step(b + 27, +1)
    hits(L, b + 28, b + 29, b + 30, b + 30.5, b + 31.5)


def scene_e(L, b):
    """E, la tempête (160-176) : croches et doubles croches. Accents : 160 161# 162 163 163,5 |
    164# 164,5# 165# 165,75 166# 167,5# 167,75 | 168# 168,5# 169# 170# 171 | 172# 172,5# 173# 173,5 174# 175 175,75."""
    notes(L, b + 0, [0.5, 1.5, 2.5])            # 160 saut, notes 161 162 163, → 164
    hits(L, b + 4.5, b + 5)
    L.gap(b + 6, 0.5)
    hits(L, b + 7.5, b + 8, b + 8.5, b + 9)
    L.wall(b + 10)
    hits(L, b + 11, b + 12, b + 12.5, b + 13)
    L.step(b + 14, +1)
    L.enemy(b + 15)


def scene_fin(L, b):
    """D' (304-336), la tempête retombe. Accents : 304# 305 306 307 | 309 311 | 312 314 314,5 315# |
    316# 316,5 317# 318# 319 | 320 321 | 325 327 | 328 328,5 329 331 | 332 333 334 334,5 335,5."""
    hits(L, b + 0, b + 1, b + 2)
    L.gap(b + 3, 0.5)
    L.enemy(b + 5)
    L.wall(b + 7)
    hits(L, b + 8, b + 10, b + 10.5)
    L.gap(b + 11, 0.5)
    hits(L, b + 12, b + 12.5, b + 13, b + 14)
    L.step(b + 15, -2)
    hits(L, b + 16, b + 17, b + 21, b + 23, b + 24, b + 25, b + 27, b + 28)


def effects(L):
    """L'écran suit la musique. Sur les phrases : dédoublé, miroir, tête en bas, demi-tour, grésillement (la fin
    reste nette). Sur les frappes : tremblements (rafales de croches) et coups de zoom (grosse caisse des drops).
    Pas de flash en rafale (au plus un tous les 2 temps : épilepsie photosensible)."""
    def shake(beats, k=0.6, length=0.3):
        for x in beats:
            L.add(x, "camera_shake", intensity=k, length=length)

    def punch(beats, k=0.8):
        for x in beats:
            L.add(x, "camera_zoom", intensity=k, length=0.25)

    def eighths(a, z):
        return [a + i * 0.5 for i in range(int((z - a) * 2))]

    for base in (96, 272):                      # les deux drops
        punch([base + 1, base + 5, base + 9.5, base + 10, base + 11, base + 20, base + 21, base + 25, base + 26,
               base + 27])
        shake(eighths(base + 12, base + 18), 0.7, 0.25)      # la rafale tremble à chaque croche
    shake([164, 164.5, 165, 166, 167.5, 168, 168.5, 169, 170, 172, 172.5, 173, 174], 0.6, 0.25)   # la tempête
    shake([52, 52.5, 54, 54.5, 228, 228.5, 230, 230.5], 0.5, 0.25)                                # avant les blancs
    for x in (57, 59, 61, 63, 233, 235, 237, 239):                                                # les blancs : éclairs
        L.add(x, "fx_flash", intensity=0.5)
    # phrases
    L.glitch(48, 0.3, 8)
    L.screen_split(64, 4)
    L.screen_split(80, 1)
    L.screen_flip(80, "mirror")
    L.screen_flip(88, "none")
    L.screen_split(96, 4)
    L.screen_split(104, 1)
    L.screen_flip(104, "up")
    L.screen_flip(112, "both")
    L.screen_flip(120, "none")
    L.glitch(120, 0.4, 8)
    L.screen_flip(136, "mirror")
    L.screen_flip(144, "none")
    L.screen_flip(152, "up")
    L.glitch(160, 0.5, 16)                      # la tempête grésille…
    L.screen_flip(168, "both")                  # … et fait demi-tour
    L.screen_flip(176, "none")
    L.screen_split(184, 4)
    L.screen_split(192, 1)
    L.screen_flip(200, "up")                    # la grotte de la reprise, tête en bas
    L.screen_flip(208, "mirror")                # l'arrière-plan en miroir
    L.screen_flip(216, "none")
    L.glitch(224, 0.5, 8)
    L.screen_split(240, 4)
    L.screen_split(248, 1)
    L.screen_flip(248, "both")
    L.screen_flip(256, "mirror")
    L.screen_flip(264, "none")
    L.screen_split(272, 4)
    L.screen_split(280, 1)
    L.screen_flip(280, "up")
    L.glitch(280, 0.4, 8)
    L.screen_flip(288, "both")
    L.screen_flip(296, "mirror")
    L.screen_flip(304, "none")                  # la fin reste nette


def build():
    L = Level()
    scene_a(L, 0)
    scene_b(L, 64)
    scene_c(L, 96)
    scene_d(L, 128)
    scene_e(L, 160)
    scene_a(L, 176, second=True)
    scene_b(L, 240, third=True)
    scene_c(L, 272, final=True)
    scene_fin(L, 304)
    effects(L)
    for name, beat in SECTIONS:
        if beat > 0:
            L.add(beat, "section_change", intensity=1.2 if "refrain" in name else 0.5)
    L.ev.sort(key=lambda e: (e["beat"], e["type"]))
    return L.ev


FICHE = Fiche(
    id="n8_orage",
    title="Le Grand Orage",
    bpm=180.0, offset=0.035, beats=336,
    sections=SECTIONS,
    analysis="n8_orage_analysis.json",
    build=build,
    zoom=480, zoom_wide=640,
    scale=(4, 7, 9, 11, 2),                     # mi mineur pentatonique (tonique estimée : mi)
    mechanics=ALL,
    margin_ms=100.0,                            # le plus serré des niveaux (120 ailleurs)
)
