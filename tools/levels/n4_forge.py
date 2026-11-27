"""Niveau 4 — La Forge, sur *Fire Maze* (JadeWizard, OpenGameArt, CC BY 4.0), 151,3 BPM (ADR-035).

Un volcan. La verticalité arrive. Trois nouveautés, apprises en 4 temps (seule, répétée, variée, combinée) :
- les **tambours** (montée, puis refrain) : un tambour seul qui envoie sur un étage, deux à la suite, puis la chaîne
  de quatre tambours au refrain, un par temps ;
- les **rampes** : la course sur le mur (automatique) et la **dévalée** ; au refrain, le plongeon (deux pentes) ;
- l'**arrière-plan** : un tambour nous pose sur le chemin du fond, on y frappe et on y saute pendant que les piliers
  du premier plan passent devant, puis un tambour nous ramène devant.
Les gestes des niveaux 1 à 3 restent (trous, souches, ennemis, cuirassés, lianes, notes de saut, chauves-souris, et
la gravité inversée au début du passage 2). **Pas d'effet d'écran** (niveau 5).

Progression (mesurée par le simulateur, voir `tools/tests/test_levels.py`) : chaque niveau monte d'un cran sur chaque
critère, sans saut brutal. Actions par seconde : 0,70 → 0,95 → 1,19 → ≈ 1,3 ; pic sur une phrase de 8 temps entre
celui des Échos et celui du niveau de base ; un peu de contretemps (les Échos n'en ont presque pas, le niveau de base
43 %) ; dénivelé de 25 à 35 u (Échos 11 u, niveau de base 70 u) ; plans larges seulement sur les refrains.

Morceau monté par `tools/cut-song.py` : la boucle de 32 mesures jouée deux fois, puis 4 mesures, en copies entières
(`--offset 0`), fondu sur 8 temps, 1 min 48 :
  python3 tools/cut-song.py "<Fire Maze>/fire_maze.mp3" \
      game/Assets/_Project/Audio/Music/n4_forge_fire_maze.ogg --bpm 151.29 --offset 0 --bars 0-31,0-31,0-3 --fade 8
  python3 tools/analyse-song.py game/Assets/_Project/Audio/Music/n4_forge_fire_maze.ogg \
      tools/data/n4_forge_analysis.json --bpm 151.29 --offset 0.026 --beats 272
Tempo mesuré : 151,29 BPM (dérive < 10 ms après les raccords), premier temps à 26 ms ; la mineur. Grosse caisse sur
les temps 1 à 3 de chaque mesure, phrases de 4 mesures. Forme (temps) : boucle 0-128, rejouée 128-256, coda 256-272.
"""
from levelgen.builder import Level, cuirasses, grotte, lianes, notes
from levelgen.fiche import Fiche

SECTIONS = [("intro", 0), ("montée", 32), ("couplet", 48), ("refrain", 80), ("arrière-plan", 112),
            ("intro 2", 128), ("roulement", 160), ("couplet 2", 176), ("refrain 2", 208), ("final", 240)]


def scene_intro(L):
    """0-32 : ce que les Échos ont appris, en accéléré ; puis le premier tambour, sur la fin de la phrase."""
    L.gap(4, 0.5)
    L.enemy(6)
    L.step(8, +1)
    L.enemy(10)
    L.wall(12)
    L.enemy(14)
    cuirasses(L, 17)                            # 16 saut, 17 appui, → 19
    L.enemy(20)
    notes(L, 22, [0.5, 1.5])                    # 23, 24, → 25
    L.enemy(26)
    L.gap(28, 0.5)
    L.bounce(30, 2.0, +2)                       # le premier tambour : on se pose un étage plus haut (32)


def scene_montee(L):
    """32-48 : un tambour seul, la course sur le mur, un trou au sommet."""
    L.enemy(33)
    L.bounce(36, 2.0, +2)                       # → 38
    L.enemy(39)
    L.ramp(40, 1.5, +4)                         # course sur le mur
    L.enemy(43)
    L.gap(44, 0.5)
    L.enemy(45)
    L.enemy(47)


def scene_couplet(L, b, variant=False):
    """48-80 : deux tambours à la suite, deux cuirassés enchaînés, la dévalée, une liane et sa chauve-souris, une
    course sur le mur, des notes de saut avant le refrain. Au passage 2, un peu plus haut et plus bas."""
    for k in (0, 1):                            # deux tambours, on grimpe à chaque rebond
        L.bounce(b + k, 1.0, +1.5, lums=False)
    L.lums(b + 0.25, 7, 0.25)
    L.enemy(b + 4)                              # pas trop tôt après les tambours : il doit se voir venir
    L.enemy(b + 5)
    L.add(b + 6, "jump")                        # on saute sur le premier cuirassé…
    L.stomp(b + 7, 1.0)                         # … rebond court (le second reste à l'image)…
    L.stomp(b + 8, 2.0)                         # … puis vers la falaise
    L.cliff(b + 9.25, 1.0)
    L.enemy(b + 13)
    L.ramp(b + 14, 1.5, -5)                     # la dévalée (≤ 5,5 u : pas de plan large hors des refrains)
    L.enemy(b + 17)
    lianes(L, b + 18, [1.25])                   # → b + 21
    L.flyer(b + 19)
    L.enemy(b + 22)
    L.ramp(b + 23, 1.5, +5 if variant else +4)  # course sur le mur
    L.enemy(b + 25)                             # au sommet du mur
    notes(L, b + 26, [0.5, 1.5])                # → b + 29
    L.enemy(b + 30)


def scene_refrain(L, b):
    """80-112 (plan large) : on monte au sommet du volcan, puis on plonge.
      79,5 tambour sur le drop ; 84 course sur le mur ; 86 un tambour qui relance ;
      89    la chaîne : quatre tambours, un par temps, on grimpe à chaque rebond ;
      94    le sommet : deux ennemis et un trou ;
      97    le plongeon : deux pentes, le sol reste à l'image ;
      105   on rebondit (chauve-souris), et on finit au sol en frappant."""
    L.bounce(b - 0.5, 2.5, +4)                  # le drop
    L.enemy(b + 3)
    L.ramp(b + 4, 1.5, +3)
    L.bounce(b + 6, 1.5, +2)
    for k in (9, 10, 11, 12):                   # la chaîne de tambours, sur les temps
        L.bounce(b + k, 1.0, +1.5, lums=False)
    L.lums(b + 9.25, 15, 0.25)
    L.enemy(b + 14)
    L.gap(b + 15, 0.5)
    L.enemy(b + 16)
    L.ramp(b + 17, 2.5, -9)                     # le plongeon
    L.ramp(b + 19.5, 2.0, -6)
    L.enemy(b + 23)
    L.gap(b + 24, 0.5)
    L.bounce(b + 25, 1.5, +2)
    L.flyer(b + 25.5)
    L.enemy(b + 28)
    L.enemy(b + 29)
    L.enemy(b + 30)
    L.enemy(b + 31)


def back_passage(L, b):
    """L'arrière-plan (de b à b + 11,5) : un tambour nous pose sur le chemin du fond, 1 u plus haut ; on y frappe trois
    fois et on saute son trou pendant que deux piliers du premier plan passent devant ; un tambour nous ramène."""
    h = L.h
    L.to_plane(b, 1.5, "to_back")               # b → b + 1,5
    L.back(b + 1, 3.2, 1.0)
    L.back(b + 4.7, 5.8, 1.0)                   # trou de b + 4,2 à b + 4,7
    L.back_enemy(b + 2.5)
    L.add(b + 4, "jump", variant="back")
    L.back_enemy(b + 5.5)
    L.back_enemy(b + 7.5)
    L.cliff(b + 4.25, 2.5)                      # deux piliers du premier plan, plus hauts que le héros
    L.cliff(b + 4.9, -2.5)
    L.cliff(b + 6.0, 2.5)
    L.cliff(b + 6.8, -2.5)
    L.to_plane(b + 10, 1.5, "to_front")         # b + 10 → b + 11,5
    L.h = h


def scene_arriere_plan(L):
    """112-128 : le premier passage dans l'arrière-plan."""
    L.enemy(113)
    back_passage(L, 114.5)                      # → 126
    L.enemy(127)


def scene_intro2(L):
    """128-160 : la boucle revient, plus dure : la grotte de lave (gravité inversée), deux cuirassés, deux lianes."""
    L.enemy(129)
    grotte(L, 132, 140, jumps=(134, 137), hanging=(135, 139))
    L.enemy(142)
    L.enemy(143)
    cuirasses(L, 146, (1.0, 2.0))               # 145 saut, 146, 147, → 149
    L.enemy(150)
    lianes(L, 152, [1.25, 1.0])                 # → 157
    L.flyer(153)
    L.enemy(158)


def scene_roulement(L):
    """160-176 : les premières croches enchaînées : un escalier à contretemps (une marche par demi-temps de retard),
    puis de toit en toit sur les temps."""
    for k in (161.5, 162.5, 163.5, 164.5):
        L.step(k, +1)
    L.gap(166, 0.5)
    L.step(167, +1)
    L.gap(168, 0.5)
    L.step(169, +1)
    L.ramp(170, 1.0, -5)                        # on redescend du toit
    L.enemy(172)
    L.enemy(173)
    L.enemy(174)
    L.enemy(175)


def scene_final(L):
    """240-272 (fondu à la fin) : un dernier passage dans l'arrière-plan, et on finit au sol en frappant."""
    L.enemy(241)
    back_passage(L, 242.5)                      # → 254
    L.enemy(255)
    L.enemy(257)
    L.wall(259)
    L.enemy(261)
    L.enemy(263)                                # le dernier coup


def build():
    L = Level()
    scene_intro(L)
    scene_montee(L)
    scene_couplet(L, 48)
    scene_refrain(L, 80)
    scene_arriere_plan(L)
    scene_intro2(L)
    scene_roulement(L)
    scene_couplet(L, 176, variant=True)
    scene_refrain(L, 208)
    scene_final(L)
    for name, beat in SECTIONS:
        if beat > 0:
            L.add(beat, "section_change", intensity=1.2 if "refrain" in name else 0.5)
    L.ev.sort(key=lambda e: (e["beat"], e["type"]))
    return L.ev


FICHE = Fiche(
    id="n4_forge",
    title="Fire Maze - JadeWizard",
    bpm=151.29, offset=0.026, beats=272,
    sections=SECTIONS,
    analysis="n4_forge_analysis.json",
    build=build,
    zoom=480, zoom_wide=640, wide_on_refrains=True,     # plans larges sur les refrains (montée et plongeon)
    scale=(9, 0, 2, 4, 7),                   # la mineur pentatonique (la, do, ré, mi, sol)
    mechanics=frozenset({"gap", "wall", "step", "enemy", "stomp", "vine", "flyer", "orb", "gravity",
                         "bounce", "ramp", "back"}),
    margin_ms=120.0,
)
