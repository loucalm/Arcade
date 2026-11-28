"""Niveau 7 — Le Grand Orage (ex-niveau 8 rétrogradé et enrichi en verticalité), 180 BPM (`n8_orage.ogg`).

180 BPM (9,6 u/s), toutes les mécaniques hors boss/accélération (16 mécaniques, dont le premier grésillement `glitch`),
marge ≥ 100 ms, rafales de frappes en croches sur les drops, éclairs sur les temps forts, et une vraie verticalité
d'orage dans les nuages (ascensions, plateformes d'altitude, grands plongeons, dénivelé ~35 u) avec un palier de
reprise franc au milieu de la seconde partie (plus de tunnel de 20 s sans checkpoint).
"""
from levelgen.builder import Level, cuirasses, grotte, lianes, notes
from levelgen.fiche import ALL, Fiche

SECTIONS = [("intro", 0), ("blanc", 56), ("couplet", 64), ("refrain", 96), ("couplet 2", 128), ("tempête", 160),
            ("reprise", 176), ("blanc 2", 232), ("couplet 3", 240), ("refrain 2", 272), ("fin", 304)]


def hits(L, *beats):
    for b in beats:
        L.enemy(b)


def scene_a(L, b, second=False):
    """A (0-56, puis 176-232) : montée progressive dans les nuages d'orage."""
    hits(L, b + 4, b + 5)
    L.gap(b + 6, 0.5)
    L.wall(b + 7)
    hits(L, b + 9, b + 10)
    L.step(b + 11, +1)
    hits(L, b + 12, b + 13, b + 14)
    L.gap(b + 15, 0.5)
    if not second:
        hits(L, b + 16, b + 16.5, b + 17)
        L.ramp(b + 18, 1.5, +4)                 # ascension sur une colonne de nuages
        hits(L, b + 20.5, b + 21.5, b + 22.5)
        L.gap(b + 23.5, 0.5)
        cuirasses(L, b + 26, (1.5,), rise=2.0)  # 25 saut, 26 rebond → 27.5 plus haut
        L.gap(b + 28.5, 0.5)
        hits(L, b + 29.5, b + 30)
        L.wall(b + 31)
    else:
        # la reprise : grotte électrique (192-206), puis palier franc au sol (207-211) pour poser un checkpoint !
        grotte(L, b + 16, b + 30, jumps=(b + 19, b + 23, b + 27.5), hanging=(b + 21, b + 25, b + 29))
    # Sur la reprise (second=True), on laisse b+31..b+34 libre après la sortie du plafond → checkpoint garanti vers 209 !
    if not second:
        hits(L, b + 32, b + 33, b + 34)
        L.step(b + 35, +1)
        hits(L, b + 36, b + 37)
        L.gap(b + 38, 0.5)
        lianes(L, b + 40, [1.75])               # 40 saut, lâcher 42.5 → 43.5
        L.flyer(b + 41.5)
    else:
        L.enemy(b + 34.5)
        # l'arrière-plan dans les nuages lointains
        h = L.h
        L.to_plane(b + 36, 1.5, "to_back")
        L.back(b + 37, 2.2, 1.0)
        L.back(b + 39.7, 4.3, 1.0)
        L.add(b + 39, "jump", variant="back")
        L.back_enemy(b + 38)
        L.back_enemy(b + 40)
        L.back_enemy(b + 41)
        L.to_plane(b + 43, 1.0, "to_front")
        L.h = h
    hits(L, b + 45, b + 46)
    L.gap(b + 47, 0.5)
    hits(L, b + 48, b + 48.5, b + 49, b + 49.5)
    L.step(b + 50.5, +1)
    hits(L, b + 52, b + 52.5)
    L.gap(b + 53, 0.5)
    hits(L, b + 54, b + 54.5)
    L.wall(b + 55)
    L.enemy(b + 56)
    # le blanc : grand envol dans l'œil du cyclone puis plongeon sans appui
    L.bounce(b + 57, 2.5, +6)
    L.ramp(b + 60, 2.5, -10)


def scene_b(L, b, third=False):
    """B (64-96, puis 240-272) : escalade vers le drop."""
    L.enemy(b + 0)
    L.gap(b + 3, 0.5)
    L.enemy(b + 5)
    cuirasses(L, b + 8, (1.0, 1.0, 1.0), rise=2.0)
    hits(L, b + 13, b + 14)
    L.gap(b + 15, 0.5)
    if not third:
        lianes(L, b + 17, [1.25, 1.0])
        L.flyer(b + 18)
    else:
        notes(L, b + 17, [0.5, 1.5, 2.5, 1.5], rise=2.0)
    L.wall(b + 23)
    hits(L, b + 24, b + 24.5, b + 25)
    L.ramp(b + 26, 1.5, +5)                     # course sur paroi nuageuse avant le drop
    hits(L, b + 28.5, b + 29.5, b + 30.5)
    L.bounce(b + 31.5, 1.5, +4)                 # grand tremplin du drop


def scene_c(L, b, final=False):
    """C, le drop (96-128, puis 272-304) : sommet de l'orage, rafale de 8 croches et grand plongeon (−14 u)."""
    L.enemy(b + 3)
    if not final:
        hits(L, b + 5, b + 5.5)
        L.gap(b + 6.5, 0.5)
    else:
        hits(L, b + 4.5, b + 5)
        L.gap(b + 6, 0.5)
    cuirasses(L, b + 9, (1.0, 1.0), rise=2.0)
    # la rafale de 8 croches au sommet de l'orage
    hits(L, b + 12, b + 12.5, b + 13, b + 13.5, b + 14, b + 14.5, b + 15, b + 15.5)
    L.gap(b + 16, 0.5)
    hits(L, b + 17, b + 17.5, b + 18)
    L.wall(b + 19)
    hits(L, b + 20, b + 20.5, b + 21)
    # double plongeon dramatique à travers les nuages
    L.ramp(b + 22, 2.0, -8)
    L.ramp(b + 24, 1.8, -6 if not final else -8)
    if not final:
        hits(L, b + 26.5, b + 27.5)
    else:
        hits(L, b + 26.5, b + 27, b + 27.5)


def scene_d(L, b):
    """D (128-160) : grotte d'orage, plateformes de nuages et lianes."""
    grotte(L, b + 0, b + 8, jumps=(b + 2,), hanging=(b + 3, b + 5, b + 7))
    L.enemy(b + 9.5)
    # 3 plateformes d'altitude au-dessus du vide (b+11 .. b+16)
    L.void(b + 11.4, 5.4)
    h0 = L.h
    for i, r in enumerate((1, 2, 1)):
        L.add(b + 11 + 2 * i, "platform", length=2.0, height=h0 + r)
    L.h = h0
    lianes(L, b + 18, [1.25, 1.0])              # 18 → 23
    L.flyer(b + 19)
    hits(L, b + 24, b + 25)
    L.step(b + 27, +1)
    hits(L, b + 28, b + 29, b + 30, b + 30.5, b + 31.5)


def scene_e(L, b):
    """E, la tempête (160-176) : notes en altitude et rafales serrées."""
    notes(L, b + 0, [0.5, 1.5, 2.5], rise=2.0)
    hits(L, b + 4.5, b + 5)
    L.gap(b + 6, 0.5)
    hits(L, b + 7.5, b + 8, b + 8.5, b + 9)
    L.ramp(b + 10, 1.5, -4)
    hits(L, b + 12, b + 12.5, b + 13)
    L.step(b + 14, +1)
    L.enemy(b + 15)


def scene_fin(L, b):
    """D' (304-336), la tempête retombe."""
    hits(L, b + 0, b + 1, b + 2)
    L.gap(b + 3, 0.5)
    L.enemy(b + 5)
    L.wall(b + 7)
    hits(L, b + 8, b + 10, b + 10.5)
    L.gap(b + 11, 0.5)
    hits(L, b + 12, b + 12.5, b + 13, b + 14)
    L.ramp(b + 15, 2.0, -5)
    hits(L, b + 18, b + 21, b + 23, b + 24, b + 25, b + 27, b + 28)


def effects(L):
    def shake(beats, k=0.6, length=0.3):
        for x in beats:
            L.add(x, "camera_shake", intensity=k, length=length)

    def punch(beats, k=0.8):
        for x in beats:
            L.add(x, "camera_zoom", intensity=k, length=0.25)

    def eighths(a, z):
        return [a + i * 0.5 for i in range(int((z - a) * 2))]

    for base in (96, 272):
        punch([base + 1, base + 5, base + 9.5, base + 10, base + 11, base + 20, base + 21, base + 26.5, base + 27.5])
        shake(eighths(base + 12, base + 18), 0.7, 0.25)
    shake([164.5, 165, 167.5, 168, 168.5, 169, 172, 172.5, 173], 0.6, 0.25)
    shake([52, 52.5, 54, 54.5, 228, 228.5, 230, 230.5], 0.5, 0.25)
    lightning(L)
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
    L.glitch(160, 0.5, 16)
    L.screen_flip(168, "both")
    L.screen_flip(176, "none")
    L.screen_split(184, 4)
    L.screen_split(192, 1)
    L.screen_flip(200, "up")
    L.screen_flip(212, "mirror")
    L.screen_flip(220, "none")
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
    L.screen_flip(304, "none")


def lightning(L):
    plan = []
    plan += [(b, 0.6) for b in range(8, 56, 8)]
    plan += [(b, 1.0) for b in (57, 59, 61, 233, 235, 237)]
    for a, z in ((64, 96), (128, 160), (240, 272)):
        plan += [(b, 0.7) for b in range(a, z, 4)]
    for a, z in ((96, 128), (160, 176), (272, 304)):
        plan += [(b, 1.0 if (b - a) % 4 == 0 else 0.6) for b in range(a, z, 2)]
    plan += [(b, 0.7) for b in range(176, 232, 8)]
    plan += [(b, 0.8 - 0.1 * i) for i, b in enumerate(range(304, 336, 8))]
    last = -99
    for b, k in sorted(plan):
        assert b - last >= 2, f"éclairs trop rapprochés au temps {b}"
        L.lightning(b, round(k, 2))
        last = b


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
    id="n7_orage",
    title="Le Grand Orage",
    bpm=180.0, offset=0.035, beats=336,
    sections=SECTIONS,
    analysis="n8_orage_analysis.json",
    build=build,
    zoom=480, zoom_wide=640,
    scale=(4, 7, 9, 11, 2),
    mechanics=ALL - {"laser", "speed_change"},
    margin_ms=100.0,
)
