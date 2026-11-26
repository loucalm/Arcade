"""Niveau 8 — Le Grand Orage, l'épreuve ultime, sur le morceau fourni par Lou (`Niveau8.ogg`), 180 BPM.

Le niveau le plus dur : 180 BPM (9,6 u/s), toutes les mécaniques, des croches, et un écran qui se détraque au rythme
de la musique (dédoublé, en miroir, tête en bas, grésillement), même pendant les passages denses. Il demande de
connaître les sept autres niveaux : rien n'y est appris, tout y est combiné.

Morceau (1 min 53, 336 temps à 180 BPM ; la structure se lit à 90, en demi-temps) :
  python3 tools/analyse-song.py game/Assets/_Project/Audio/Music/n8_orage.ogg tools/data/n8_orage_analysis.json \
      --bpm 180 --offset 0.055 --beats 336
Forme (temps) : intro 0-48 (calme), montée 48-64, couplet 64-96 (grosse caisse sur les temps), refrain 96-128 (le
drop), couplet 2 128-160, pont 160-224 (caisse claire seule, grosse caisse rare), montée 2 224-240, couplet 3 240-272,
refrain final 272-304 (le plus fort), fin 304-336.
"""
from levelgen.builder import Level, cuirasses, grotte, lianes, notes
from levelgen.fiche import ALL, Fiche

SECTIONS = [("intro", 0), ("montée", 48), ("couplet", 64), ("refrain", 96), ("couplet 2", 128), ("pont", 160),
            ("montée 2", 224), ("couplet 3", 240), ("refrain 2", 272), ("fin", 304)]


def scene_intro(L):
    """0-48 : la musique est calme, le niveau ne l'est pas. Coups sur les temps et les « et », souches, trous et
    marches serrés : de quoi vérifier qu'on est prêt."""
    L.enemy(4)
    L.enemy(5)
    L.gap(6, 0.5)
    L.enemy(8)
    L.wall(10)
    L.enemy(11.5)
    L.step(13, +1)
    L.enemy(14.5)
    L.enemy(15.5)
    L.gap(17, 0.5)
    L.wall(19)
    L.enemy(20.5)
    L.enemy(21.5)
    L.step(23, +1)
    L.step(24, +1)
    L.enemy(25.5)
    cuirasses(L, 28)                            # 27 saut, 28 appui, → 30
    L.enemy(31)
    L.enemy(32)
    L.gap(33, 0.5)
    L.enemy(34.5)
    lianes(L, 36, [1.25])                       # → 39
    L.flyer(37)
    L.enemy(41.5)
    L.wall(43)
    L.enemy(44.5)
    L.step(46, -2)
    L.enemy(47)


def scene_montee(L):
    """48-64 : la grosse caisse revient : on gravit un escalier, une marche par coup, de toit en toit, et le
    tambour lance le couplet."""
    for k in (49, 50, 51, 52):
        L.step(k, +1)
    L.enemy(53)
    L.gap(54, 0.5)
    L.step(55.5, +1)
    L.gap(56.5, 0.5)
    L.step(57.5, +1)
    L.enemy(58.5)
    L.enemy(59.5)
    L.bounce(62, 2.0, +3)                       # → 64 : on atterrit sur le premier temps du couplet


def scene_couplet(L, b):
    """64-96 : la grosse caisse tape tous les temps : cuirassés en chaîne, lianes avec chauves-souris, notes de
    saut au-dessus du vide, un coup entre chaque geste."""
    L.enemy(b + 1)
    cuirasses(L, b + 3, (1.0, 1.5))             # 2 saut, 3, 4 → 5,5
    L.enemy(b + 6.5)
    L.enemy(b + 7.5)
    lianes(L, b + 8, [1.25, 1.0])               # → 13
    L.flyer(b + 9)
    L.enemy(b + 14)
    notes(L, b + 16, [0.5, 1.5, 2.5], rise=1.0) # 17, 18, 19 → 20
    L.enemy(b + 21)
    L.enemy(b + 22)
    L.wall(b + 23)
    L.enemy(b + 24.5)
    lianes(L, b + 25, [1.25])                   # → 28
    L.flyer(b + 26)
    L.step(b + 29, -2)
    L.enemy(b + 30)
    L.enemy(b + 31)


def scene_refrain(L, b):
    """96-128 : le drop. Tambour dans le ciel, course sur le mur, tambours en chaîne sur la grosse caisse, puis le
    grand plongeon en pente raide et des coups à la réception."""
    L.bounce(b - 0.5, 2.5, +6)                  # 95,5 → 98
    L.enemy(b + 3)
    L.ramp(b + 4, 1.5, +5)                      # course sur le mur
    L.enemy(b + 6.5)
    L.bounce(b + 7.5, 1.5, +2)
    L.flyer(b + 8)
    for k in (10, 11, 12, 13):                  # quatre tambours, un par temps
        L.bounce(b + k, 1.0, +2.0, lums=False)
    L.lums(b + 10.25, 15, 0.25)
    L.enemy(b + 15)
    L.enemy(b + 16)
    L.gap(b + 17, 0.5)
    L.enemy(b + 18.5)
    L.ramp(b + 19.5, 2.0, -12)                  # le plongeon
    L.ramp(b + 22, 1.5, -6)
    L.enemy(b + 24.5)
    L.enemy(b + 25.5)
    notes(L, b + 26, [0.5, 1.5])                # 27, 28 → 29
    L.enemy(b + 30)
    L.enemy(b + 31)


def scene_couplet2(L, b):
    """128-160 : la gravité s'inverse deux fois ; entre les deux, des cuirassés."""
    grotte(L, b + 1, b + 11, jumps=(b + 3, b + 6, b + 9), hanging=(b + 4, b + 7.5, b + 10))
    L.enemy(b + 13)
    cuirasses(L, b + 15, (1.0, 2.0))            # 14 saut, 15, 16 → 18
    L.enemy(b + 19)
    grotte(L, b + 21, b + 30, jumps=(b + 23, b + 26), hanging=(b + 24, b + 27.5, b + 29))
    L.enemy(b + 31)


def scene_pont(L, b):
    """160-224 : la caisse claire seule. L'arrière-plan (160-176), les lianes au-dessus du vide (176-192), les
    notes (192-208) et des plateformes qui flottent (208-224)."""
    h = L.h
    L.to_plane(b + 1, 1.5, "to_back")           # 161 → 162,5
    L.back(b + 2, 3.7, 1.0)                     # 162 → 165,7
    L.back(b + 6.2, 3.5, 1.0)                   # 166,2 → 169,7 (trou de 165,7 à 166,2)
    L.back(b + 10.2, 4.3, 1.0)                  # 170,2 → 174,5 (trou de 169,7 à 170,2)
    L.add(b + 5.5, "jump", variant="back")
    L.add(b + 9.5, "jump", variant="back")
    L.back_enemy(b + 4)
    L.back_enemy(b + 7.5)
    L.back_enemy(b + 11)
    L.back_enemy(b + 12)
    L.to_plane(b + 13, 1.5, "to_front")         # 173 → 174,5
    L.h = h
    L.enemy(b + 15.5)
    # 176-192 : lianes
    lianes(L, b + 16, [1.25, 1.0, 1.0])         # → 23
    L.flyer(b + 17)
    L.step(b + 24, -2)
    L.enemy(b + 25)
    lianes(L, b + 26, [1.25, 1.0])              # → 31
    L.flyer(b + 27)
    # 192-208 : notes
    L.enemy(b + 32)
    notes(L, b + 33, [0.5, 1.5, 2.5, 1.5], rise=1.0)   # 34 … 37 → 38
    L.enemy(b + 39)
    L.enemy(b + 40)
    notes(L, b + 41, [0.5, 1.5, 2.5], rise=1.0)        # → 45
    L.flyer(b + 43.5)
    L.enemy(b + 46)
    L.enemy(b + 47)
    # 208-224 : plateformes au-dessus du vide, chauves-souris entre deux sauts
    L.void(b + 48.5, 13.0)
    h0 = L.h
    for i, r in enumerate([1, 2, 1, 2, 3, 2, 1]):
        L.add(b + 48 + 2 * i, "platform", length=1.9, height=h0 + r)
        if i in (1, 3):
            L.flyer(b + 48 + 2 * i + 1)
    L.h = h0
    L.enemy(b + 63)


def scene_montee2(L, b):
    """224-240 : escalier sur les « et », puis on file dans la pente et un tambour lance le couplet 3."""
    for k in (0.5, 1.5, 2.5, 3.5):
        L.step(b + k, +1)
    L.enemy(b + 4.5)
    L.gap(b + 5.5, 0.5)
    L.enemy(b + 7)
    L.ramp(b + 8, 1.5, -6)
    L.enemy(b + 10.5)
    L.enemy(b + 11.5)
    L.bounce(b + 14, 2.0, +2)                   # → 240


def scene_couplet3(L, b):
    """240-272 : tout, vite."""
    L.enemy(b + 1)
    cuirasses(L, b + 3, (1.0, 1.0, 1.5))        # 2 saut, 3, 4, 5 → 6,5
    L.enemy(b + 7.5)
    lianes(L, b + 8, [1.25, 1.0])               # → 13
    L.flyer(b + 9)
    L.enemy(b + 14)
    grotte(L, b + 16, b + 24, jumps=(b + 18, b + 21), hanging=(b + 19, b + 22.5))
    L.enemy(b + 25.5)
    notes(L, b + 26.5, [0.5, 1.5], rise=1.0)    # → 29,5
    L.enemy(b + 30.5)


def scene_refrain2(L, b):
    """272-304 : le refrain le plus fort : le drop rejoué plus haut et plus serré."""
    L.bounce(b - 0.5, 2.5, +6)
    L.enemy(b + 3)
    L.ramp(b + 4, 1.5, +6)
    L.enemy(b + 6.5)
    L.bounce(b + 7.5, 1.5, +2)
    L.flyer(b + 8)
    for k in (10, 11, 12, 13):
        L.bounce(b + k, 1.0, +2.5, lums=False)
    L.lums(b + 10.25, 15, 0.25)
    L.enemy(b + 15)
    L.gap(b + 16, 0.5)
    L.enemy(b + 17.5)
    L.enemy(b + 18.5)
    L.ramp(b + 19.5, 2.0, -14)
    L.ramp(b + 22, 1.5, -6)
    L.enemy(b + 24.5)
    cuirasses(L, b + 27, (1.0, 1.0))            # 26 saut, 27, 28 → 29
    L.enemy(b + 30)
    L.enemy(b + 31)


def scene_fin(L, b):
    """304-336 : la tempête retombe ; quelques coups, puis on savoure son score (écran net)."""
    L.enemy(b + 1)
    L.enemy(b + 2)
    L.gap(b + 3, 0.5)
    L.enemy(b + 5)
    L.wall(b + 6)
    L.enemy(b + 9)
    L.step(b + 11, -2)
    L.enemy(b + 13)
    L.enemy(b + 17)
    L.enemy(b + 21)
    L.enemy(b + 25)


def effects(L):
    """L'écran suit la musique : il se dédouble sur les drops, se retourne à chaque phrase du refrain, grésille sur
    les montées. Toujours sur un début de phrase de 8 temps ; la fin reste nette."""
    L.glitch(56, 0.3, 8)                        # montée : l'image grésille
    L.screen_split(64, 4)                       # couplet : 2×2
    L.screen_split(80, 1)
    L.screen_flip(80, "mirror")                 # on court vers la gauche
    L.screen_flip(88, "none")
    L.screen_split(96, 4)                       # le drop : 2×2…
    L.screen_split(104, 1)
    L.screen_flip(104, "up")                    # … tête en bas pour les tambours
    L.screen_flip(112, "both")                  # demi-tour pour le plongeon
    L.screen_flip(120, "none")
    L.glitch(120, 0.4, 8)
    L.screen_flip(136, "mirror")                # la gravité inversée en miroir
    L.screen_flip(144, "none")
    L.screen_flip(152, "up")                    # gravité inversée tête en bas : on a l'air de courir au sol
    L.screen_flip(160, "none")
    L.glitch(176, 0.3, 8)                       # pont : grésillement sur les lianes
    L.screen_flip(192, "mirror")                # notes en miroir
    L.screen_flip(200, "up")
    L.screen_flip(208, "none")
    L.screen_split(208, 4)                      # plateformes en 2×2
    L.screen_split(216, 1)
    L.glitch(224, 0.5, 16)                      # montée 2 : le grésillement monte
    L.screen_split(240, 4)                      # couplet 3
    L.screen_split(248, 1)
    L.screen_flip(248, "both")
    L.screen_flip(256, "mirror")
    L.screen_flip(264, "none")
    L.screen_split(272, 4)                      # refrain final : tout à la fois
    L.screen_split(280, 1)
    L.screen_flip(280, "up")
    L.glitch(280, 0.4, 8)
    L.screen_flip(288, "both")
    L.screen_flip(296, "mirror")
    L.screen_flip(304, "none")                  # la fin reste nette


def build():
    L = Level()
    scene_intro(L)
    scene_montee(L)
    scene_couplet(L, 64)
    scene_refrain(L, 96)
    scene_couplet2(L, 128)
    scene_pont(L, 160)
    scene_montee2(L, 224)
    scene_couplet3(L, 240)
    scene_refrain2(L, 272)
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
    bpm=180.0, offset=0.055, beats=336,
    sections=SECTIONS,
    analysis="n8_orage_analysis.json",
    build=build,
    zoom=480, zoom_wide=640,
    scale=(4, 7, 9, 11, 2),                     # mi mineur pentatonique (tonique estimée : mi)
    mechanics=ALL,                              # tout
    margin_ms=100.0,                            # le plus serré des niveaux (120 ailleurs)
)
