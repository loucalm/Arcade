"""Niveau 3 — Les Échos, sur *Stage 1* (Juhani Junkala, *4 Chiptunes (Adventure)*, CC0), 140 BPM (ADR-035).

Une grotte de cristal. Trois nouveautés, une par section, apprises en 4 temps (seule, répétée, variée, combinée) :
- les **chauves-souris** (couplet) : on les frappe en l'air, au milieu d'un balancier de liane ou entre deux notes de
  saut, là où le héros reste à leur hauteur (sur un rebond de cuirassé, elles passaient sous le héros ou hors de
  l'image : la règle du générateur les veut à hauteur du héros ou au-dessus, et visibles 400 ms avant) ;
- les **notes de saut** (refrain) : au-dessus du gouffre, un appui en passant sur chaque note relance le saut ;
- la **gravité inversée** (pont) : un portail nous fait tomber au plafond de la grotte, on y court la tête en bas,
  on saute ses trous et on frappe les ennemis pendus, puis un portail nous ramène au sol.
Plus rapide (140 BPM) et plus dense que le Bayou ; premières **croches** (les chauves-souris au sommet d'un rebond
tombent sur un « et »). Vue de 480 px (plan large seulement si un ennemi se verrait trop tard) ; pas d'effet d'écran.

Morceau monté par `tools/cut-song.py` : la boucle de 24 mesures (A calme 8 mesures, B pleine 16) jouée deux fois et
demie, en copies entières (`--offset 0`, sinon la grille se décale à chaque raccord), fondu sur 8 temps, 1 min 50 :
  python3 tools/cut-song.py "<4 Chiptunes (Adventure)>/... 1. Stage 1.wav" \
      game/Assets/_Project/Audio/Music/n3_echos_stage1.ogg --bpm 140 --offset 0 --bars 0-23,0-23,0-15 --fade 8
  python3 tools/analyse-song.py game/Assets/_Project/Audio/Music/n3_echos_stage1.ogg \
      tools/data/n3_echos_analysis.json --bpm 140 --offset 0.020 --beats 256
Forme (temps) : A 0-32, B 32-96, A 96-128, B 128-192, A 192-224, B 224-256 (fondu).
"""
from levelgen.builder import Level, cuirasses, grotte, lianes, notes
from levelgen.fiche import Fiche

SECTIONS = [("intro", 0), ("couplet", 32), ("refrain", 64), ("pont", 96), ("couplet 2", 128), ("refrain 2", 160),
            ("final", 192), ("fin", 224)]


def scene_intro(L):
    """0-32 (A) : ce que le Bayou a appris, en accéléré : sauts, coups, une marche, une souche, un cuirassé, une
    liane."""
    L.gap(4, 0.5)
    L.enemy(7)
    L.step(8, +1)
    L.enemy(11)
    L.wall(12)
    L.enemy(15)
    cuirasses(L, 18)                            # 17 saut, 18 appui, → 20
    L.enemy(21)
    lianes(L, 24, [1.25])                       # 26 lâcher, → 27
    L.enemy(29)
    L.enemy(31)


def scene_couplet(L):
    """32-64 (B) : les chauves-souris, frappées en l'air là où le héros reste à leur hauteur. Au milieu d'un
    balancier de liane, deux fois ; entre deux notes de saut (sur le « et ») ; puis sur une double traversée."""
    lianes(L, 36, [1.25])                       # liane de 36,75 à 38, → 39
    L.flyer(37)                                 # au milieu du balancier
    L.enemy(41)
    lianes(L, 44, [1.25])                       # → 47
    L.flyer(45)
    notes(L, 48, [0.5, 1.5])                    # 49, 50, → 51
    L.flyer(49.5)                               # entre les deux notes
    L.enemy(53)
    lianes(L, 56, [1.25, 1.0])                  # 58 lâcher, 59 → 60 lâcher, → 61
    L.flyer(57)                                 # (sur la seconde liane, elle passerait sous le héros)
    L.enemy(63)


def scene_refrain(L):
    """64-96 (B) : les notes de saut. Une note au-dessus d'un gouffre, une deuxième, trois qui montent, puis des
    notes qui mènent à un cuirassé et une liane (avec leurs chauves-souris)."""
    notes(L, 64, [0.5])                         # 64 saut, 65 note, → 66
    L.enemy(67)
    notes(L, 68, [0.5])                         # → 70
    L.enemy(71)
    notes(L, 72, [0.5, 1.5, 2.5], rise=2.0)     # 73, 74, 75, → 76 deux crans plus haut
    L.enemy(77)
    L.enemy(79)
    notes(L, 80, [0.5, 1.5])                    # 81, 82, → 83
    cuirasses(L, 85)                            # 84 saut, 85 rebond, → 87
    lianes(L, 88, [1.25])                       # → 91
    L.flyer(89)
    L.step(92, -2)                              # on redescend
    L.enemy(93)
    L.enemy(95)


def scene_pont(L):
    """96-128 (A, la musique se calme) : la grotte se retourne. Portail sur le 1, on court au plafond, deux trous et
    deux ennemis pendus, retour au sol ; puis un passage plus long au plafond."""
    L.enemy(97)
    grotte(L, 100, 108, jumps=(102, 105), hanging=(103, 107))
    L.enemy(111)                                # pas sur 109 : la caméra redescend du plafond
    grotte(L, 112, 124, jumps=(114, 117, 120), hanging=(115, 119, 122))
    L.enemy(127)


def scene_couplet2(L):
    """128-160 (B) : chauves-souris, cuirassés et lianes enchaînés, plus serrés."""
    cuirasses(L, 130, (1.0, 2.0))               # 129 saut, 130, 131 grand rebond, → 133
    L.enemy(135)
    lianes(L, 136, [1.25, 1.0])                 # → 141
    L.flyer(137)
    L.enemy(143)
    L.gap(144, 0.5)
    L.wall(146)
    L.enemy(147)
    cuirasses(L, 150)                           # → 152
    L.enemy(153)
    L.step(154, +1)
    L.step(156, +1)
    L.enemy(157)
    L.enemy(159)


def scene_refrain2(L):
    """160-192 (B) : notes de saut, un passage au plafond, et la fin du refrain en chauves-souris."""
    notes(L, 160, [0.5, 1.5, 2.5], rise=2.0)    # → 164
    L.enemy(165)
    grotte(L, 168, 176, jumps=(170, 173), hanging=(171, 175))
    L.enemy(179)
    notes(L, 180, [0.5, 1.5])                   # → 183
    L.step(184, -2)
    L.enemy(185)
    cuirasses(L, 188)                           # → 190
    L.enemy(191)


def scene_final(L):
    """192-224 (A) : tout, une dernière fois."""
    L.gap(192, 0.5)
    L.wall(194)
    L.enemy(195)
    lianes(L, 196, [1.25])                      # → 199
    L.flyer(197)
    notes(L, 200, [0.5, 1.5, 2.5], rise=1.0)    # → 204
    L.enemy(205)
    grotte(L, 208, 216, jumps=(210, 213), hanging=(211, 215))
    cuirasses(L, 220, (1.0, 2.0))               # 219 saut, 220, 221, → 223


def scene_fin(L):
    """224-256 (B, fondu à la fin) : on redescend en frappant ; une dernière liane."""
    L.enemy(225)
    L.gap(226, 0.5)
    L.step(228, -2)
    L.enemy(229)
    lianes(L, 232, [1.25])                      # → 235
    L.flyer(233)
    L.step(236, -2)
    L.enemy(237)
    L.enemy(239)
    L.gap(240, 0.5)
    L.enemy(243)
    L.wall(244)
    L.enemy(247)


def build():
    L = Level()
    scene_intro(L)
    scene_couplet(L)
    scene_refrain(L)
    scene_pont(L)
    scene_couplet2(L)
    scene_refrain2(L)
    scene_final(L)
    scene_fin(L)
    for name, beat in SECTIONS:
        if beat > 0:
            L.add(beat, "section_change", intensity=1.2 if name in ("refrain", "pont") else 0.5)
    L.ev.sort(key=lambda e: (e["beat"], e["type"]))
    return L.ev


FICHE = Fiche(
    id="n3_echos",
    title="Stage 1 - Juhani Junkala",
    bpm=140.0, offset=0.020, beats=256,
    sections=SECTIONS,
    analysis="n3_echos_analysis.json",
    build=build,
    # vue 480 ; plan large (640) seulement là où un ennemi se verrait trop tard en vue serrée
    zoom=480, zoom_wide=640, wide_on_refrains=False,
    scale=(5, 7, 9, 10, 0),                  # fa, sol, la, si♭, do : les notes de la mélodie (fa majeur)
    mechanics=frozenset({"gap", "wall", "step", "enemy", "stomp", "vine", "flyer", "orb", "gravity"}),
    margin_ms=120.0,
)
