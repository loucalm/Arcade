"""Niveau 1 — La Sylve, sur *We're the Resistors* (Eric Skiff, *Resistor Anthems*, CC BY 4.0), 117 BPM (ADR-034).

Le niveau qui apprend le jeu. Deux gestes seulement : **sauter** (trous, souches, marches) et **frapper** (ennemis
au sol). Ni piège, ni écran qui bouge, ni plan large : la vue reste fixe (480 px), et tout tombe sur des temps :
- les sauts sur le temps 1 (la grosse caisse et l'accord) ;
- les frappes sur les temps 2 et 4 (la caisse claire).
Pas de contretemps : la grosse caisse du « 2 et » est laissée à la musique.
Chaque geste s'apprend en 4 temps (docs/superpowers/specs/2026-11-26-niveaux-par-monde-design.md, § 3) :
seul, répété, varié, puis combiné.

Morceau monté par `tools/cut-song.py` (mesures 0-31 puis 64 à la fin, soit 1 min 16) :
  python3 tools/cut-song.py "<Resistor Anthems>/07 We're the Resistors.mp3" \
      game/Assets/_Project/Audio/Music/n1_sylve_resistors.ogg --bpm 117 --offset 0.054 --bars 0-31,64-end
  python3 tools/analyse-song.py game/Assets/_Project/Audio/Music/n1_sylve_resistors.ogg \
      tools/data/n1_sylve_analysis.json --bpm 117 --offset 0.054 --beats 144
Forme (temps) : A 0-32, B 32-64, A plus chargé 64-96 (le refrain), B 96-128, fin 128-144.
Carte des attaques : `python3 tools/gen-level.py n1_sylve --map` (X sur chaque temps 1, caisse claire sur 2 et 4).
"""
from levelgen.builder import Level
from levelgen.fiche import Fiche

SECTIONS = [("intro", 0), ("couplet", 32), ("refrain", 64), ("couplet 2", 96), ("fin", 128)]


def scene_intro(L):
    """0-32 : sauter. Huit mesures de course d'abord (les pièces montrent le chemin), puis un premier trou seul, deux
    trous espacés, une souche (même geste, autre obstacle) et un dernier trou sur la relance de batterie."""
    L.gap(8, 0.5)
    L.gap(12, 0.5)
    L.gap(16, 0.5)
    L.gap(20, 0.5)
    L.wall(24)                  # une souche
    L.gap(28, 0.5)


def scene_couplet(L):
    """32-64 : frapper, sur la caisse claire du temps 4. Trois ennemis seuls, puis on mélange avec les sauts appris
    (un trou, une souche), et des coups sur le temps 2."""
    L.enemy(35)
    L.enemy(39)
    L.enemy(41)                 # temps 2 : deux coups dans la mesure
    L.enemy(43)
    L.enemy(45)
    L.enemy(47)
    L.gap(48, 0.5)
    L.enemy(51)
    L.enemy(53)
    L.wall(56)
    L.enemy(59)
    L.enemy(61)


def scene_refrain(L):
    """64-96 : le moment du niveau. On grimpe quatre marches, une par mesure sur le temps 1, avec un coup entre
    chacune (temps 4). Au sommet, un ennemi et un trou, puis on redescend marche par marche (on se laisse tomber,
    sans appui) en frappant."""
    for b in (64, 68, 72, 76):
        L.step(b, +1)
        L.enemy(b + 3)
    L.gap(80, 0.5)              # au sommet
    L.step(84, -1)              # on redescend
    L.enemy(85)
    L.step(88, -1)
    L.enemy(89)
    L.enemy(91)
    L.step(92, -1)
    L.enemy(93)


def scene_couplet2(L):
    """96-128 : le couplet revient, avec tout ce qu'on sait, deux gestes par mesure : saut sur le 1, coup sur le 4."""
    L.gap(96, 0.5)
    L.enemy(99)
    L.wall(100)
    L.enemy(103)
    L.step(104, +1)
    L.enemy(107)
    L.gap(108, 0.5)
    L.enemy(111)
    L.step(112, -1)
    L.enemy(113)
    L.gap(116, 0.5)
    L.enemy(119)             # la mesure presque vide du morceau : un saut, puis rien
    L.step(120, +1)
    L.enemy(123)
    L.gap(124, 0.5)
    L.enemy(127)


def scene_fin(L):
    """128-144 : la batterie s'emballe pour finir ; on refait les gestes une dernière fois et on redescend au sol."""
    L.gap(128, 0.5)
    L.enemy(131)
    L.wall(132)
    L.enemy(135)
    L.step(136, -2)             # on retombe au sol
    L.enemy(139)
    L.gap(140, 0.5)             # le dernier saut


def build():
    L = Level()
    scene_intro(L)
    scene_couplet(L)
    scene_refrain(L)
    scene_couplet2(L)
    scene_fin(L)
    for name, beat in SECTIONS:
        if beat > 0:
            L.add(beat, "section_change", intensity=0.5)
    L.ev.sort(key=lambda e: (e["beat"], e["type"]))
    return L.ev


FICHE = Fiche(
    id="n1_sylve",
    title="We're the Resistors - Eric Skiff",
    bpm=117.0, offset=0.054, beats=144,
    sections=SECTIONS,
    analysis="n1_sylve_analysis.json",
    build=build,
    zoom=480, zoom_wide=480,                 # vue fixe : aucun changement de plan au niveau 1
    scale=(0, 3, 5, 7, 10),                  # do mineur pentatonique (do, mi♭, fa, sol, si♭) : la mélodie du morceau
    mechanics=frozenset({"gap", "wall", "step", "enemy"}),
    # le niveau le plus tolérant (ADR-034 ; 120 ms ailleurs). 180 ms était visé, mais un saut en retard plafonne vers
    # 170 ms à 117 BPM (on peut sauter 0,12 temps après avoir quitté le bord) ; mesuré : 161 à 196 ms pour les sauts,
    # 200 à 225 ms pour les frappes (à une image près : ±17 ms)
    margin_ms=160.0,
)
