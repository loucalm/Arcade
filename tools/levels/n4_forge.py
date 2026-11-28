"""Niveau 4 — La Forge, sur *Chaos ID* de Soularflair et @canmastersmash (Free Music Archive, CC BY 4.0), 125 BPM
(ADR-035, ADR-044, plan : docs/superpowers/plans/2026-11-27-niveau-4-volcan.md).

La marche entre les Échos (niveau 3) et le Main Theme (niveau 5). Deux nouveautés, apprises en 4 temps (seule,
répétée, variée, combinée) :
- le **volcan qui monte** : tambours (un seul, deux à la suite, la chaîne de quatre sur les coups forts) et rampes
  (la course sur le mur, la dévalée, le plongeon) ;
- les **effets d'écran**, le premier niveau qui en a : un grésillement sur chaque coup fort, un miroir sur une phrase
  calme (rien à faire pendant le retournement), l'écran dédoublé (la grille 2×2, le seul dédoublé du jeu) sur des
  gestes simples, puis les deux au final. Pas de tête en bas (niveau 5 et plus). Chaque effet est annoncé une mesure
  avant par le bandeau.
Les gestes des Échos restent (trous, marches, murs, ennemis, cuirassés, lianes, chauves-souris, notes de saut, grotte).
Le morceau est plus lent que les Échos (125 contre 140 BPM) : la difficulté monte par la densité (ennemis en croches,
doublés), et la course est accélérée à 8 u/s (3,84 u par temps au lieu de 3,2 : Échos 7,5 u/s, niveau de base 8). Les
actions restent sur les mêmes temps, les obstacles s'écartent. Au-delà de 8,35 u/s, les frappes en avance ratent
l'ennemi (marge sous 120 ms).

Forme du morceau (temps, mesurée sur l'original) : riffs de 12 mesures (0, 48, 108), coups forts de 3 mesures plus
brillants (96, 156, 216), pont (168), montée de plus en plus forte (192), fin (228) et fondu.

Morceau : l'original, entier (MP3 320 kbps de FMA, 1 min 55, joué au clic : 125,00 BPM, dérive < 3 ms ; premier temps
à 0,052 s, au début de l'attaque : à 0,069 s, sur son pic, les actions tombaient 18 ms trop tard), seulement converti
en OGG. Une version 8-bit rejouée par la machine (essai du 27/11) sonnait faux sur tout le morceau : abandonnée.
  python3 tools/cut-song.py "Soularflair - Chaos ID (tribal, instrumental, metal).mp3" \\
      game/Assets/_Project/Audio/Music/n4_forge_chaos_id.ogg --bpm 125 --offset 0.052 --bars 0-end
  python3 tools/analyse-song.py game/Assets/_Project/Audio/Music/n4_forge_chaos_id.ogg \\
      tools/data/n4_forge_analysis.json --bpm 125 --offset 0.052 --beats 236
Crédit (demandé par l'artiste) : « Music by Soularflair and @canmastersmash (IG) - Chaos ID ». L'artiste demande
aussi qu'on le prévienne quand on utilise sa musique.
"""
from levelgen.builder import Level, cuirasses, grotte, lianes, notes
from levelgen.fiche import Fiche

SECTIONS = [("intro", 0), ("couplet", 48), ("refrain", 96), ("couplet 2", 108), ("refrain 2", 156), ("pont", 168),
            ("montée", 192), ("refrain 3", 216), ("final", 228)]


def drop(L, b, glitch=0.35, descend=0.0):
    """Un coup fort (12 temps, plan large) : un tambour sur le drop, un ennemi, la chaîne de quatre tambours sur les
    temps (on grimpe à chaque rebond), puis un ennemi, un trou, un ennemi ; avec `descend`, on finit en redescendant
    (le plongeon, 2 temps). Le grésillement souligne le coup."""
    L.glitch(b, glitch, 4)
    L.bounce(b - 0.5, 2.5, +4)                  # le drop : → b + 2
    L.enemy(b + 3)
    for k in (4, 5, 6, 7):                      # la chaîne, un tambour par temps
        L.bounce(b + k, 1.0, +1.5, lums=False)
    L.lums(b + 4.25, 15, 0.25)
    L.enemy(b + 9)
    if descend:
        L.ramp(b + 10, 2.0, descend)            # → b + 12
        return
    L.gap(b + 10, 0.5)
    L.enemy(b + 11)


def scene_intro(L):
    """0-48 : ce que les Échos ont appris, puis le premier tambour, seul, sur la fin de la phrase."""
    L.gap(4, 0.5)
    L.enemy(6)
    L.step(8, +1)
    L.enemy(10)
    L.enemy(11)                                 # doublé (125 BPM : la densité remplace la vitesse)
    L.wall(12)
    L.enemy(14)
    L.enemy(16)
    L.enemy(17)
    L.gap(18, 0.5)
    L.enemy(20)
    cuirasses(L, 23)                            # 22 saut, 23 appui, → 25
    L.enemy(26)
    L.enemy(28)
    notes(L, 30, [0.5, 1.5])                    # 31, 32, → 33
    L.enemy(34)
    L.enemy(35)
    L.step(36, +1)
    L.enemy(38)
    L.enemy(40)
    L.enemy(41)
    L.gap(43, 0.5)
    L.bounce(45, 2.0, +2)                       # le premier tambour : on se pose un étage plus haut (47)


def scene_couplet(L):
    """48-96 : le tambour encore, la course sur le mur, deux tambours à la suite, la dévalée, une liane et sa
    chauve-souris, deux cuirassés, des notes de saut ; le drop du refrain arrive par un tambour."""
    L.enemy(49)
    L.enemy(50)
    L.enemy(51)
    L.bounce(52, 2.0, +2)                       # → 54
    L.enemy(55)
    L.enemy(57)
    L.ramp(58, 1.5, +4)                         # la première course sur le mur
    L.enemy(61)
    L.gap(62, 0.5)
    L.enemy(64)
    L.enemy(64.5)                               # en croches : la densité remplace la vitesse
    L.enemy(65)
    for k in (0, 1):                            # deux tambours à la suite
        L.bounce(66 + k, 1.0, +1.5, lums=False)
    L.lums(66.25, 7, 0.25)
    L.enemy(70)
    L.enemy(70.5)
    L.enemy(71)
    L.ramp(72, 1.5, -5)                         # la dévalée
    L.enemy(75)
    lianes(L, 76, [1.25])                       # → 79
    L.flyer(77)
    L.enemy(80)
    L.enemy(81)
    cuirasses(L, 84, (1.0, 2.0))                # 83 saut, 84, 85, → 87
    L.enemy(88)
    notes(L, 89, [0.5, 1.5])                    # → 92
    L.enemy(93)
    L.enemy(94)


def scene_couplet2(L):
    """108-156 : on redescend, puis le premier miroir (120-132) sur une phrase calme : rien pendant le retournement,
    des ennemis et une marche ensuite. Puis la grotte (gravité inversée) et deux cuirassés."""
    L.enemy(109)
    L.ramp(110, 2.5, -8)                        # le plongeon après le coup fort
    L.enemy(113)
    L.enemy(114)
    L.enemy(115)
    L.enemy(116)
    L.gap(117, 0.5)
    L.enemy(118)
    L.screen_flip(120, "mirror")                # on court vers la gauche
    L.enemy(124)
    L.enemy(126)
    L.step(127, +1)
    L.enemy(129)
    L.enemy(130)
    L.screen_flip(132, "none")
    L.enemy(133)
    L.enemy(134)
    grotte(L, 137, 145, jumps=(139, 142), hanging=(140, 144))
    L.enemy(147)
    cuirasses(L, 150, (1.0, 2.0))               # 149 saut, 150, 151, → 153
    L.enemy(154)


def scene_pont(L):
    """168-192 : l'écran dédoublé en 2×2 (168-180) sur des gestes simples, puis un escalier à contretemps et de toit en
    toit."""
    L.screen_split(168, 4)
    L.enemy(169)
    L.enemy(170)
    L.enemy(171)
    L.gap(172, 0.5)
    L.enemy(174)
    L.enemy(175)
    L.step(177, +1)
    L.enemy(178)
    L.screen_split(180, 1)
    for k in (181.5, 182.5, 183.5):             # une marche par demi-temps de retard
        L.step(k, +1)
    L.gap(185, 0.5)
    L.step(186, +1)
    L.gap(187, 0.5)
    L.ramp(188, 1.0, -5)                        # on redescend du toit
    L.enemy(190)
    L.enemy(190.5)
    L.enemy(191)


def scene_montee(L):
    """192-216 : la montée : deux lianes, des notes de saut, puis le second miroir (208-216) avec un tambour."""
    L.enemy(193)
    lianes(L, 194, [1.25, 1.0])                 # → 199
    L.flyer(195)
    L.enemy(200)
    L.enemy(200.5)
    L.enemy(201)
    notes(L, 202, [0.5, 1.5, 0.5])              # → 206
    L.enemy(207)
    L.screen_flip(208, "mirror")
    L.enemy(210)
    L.enemy(210.5)
    L.enemy(211)
    L.bounce(212, 1.5, +2)                      # → 213,5
    L.enemy(214)
    L.screen_flip(216, "none")


def scene_final(L):
    """216-236 : le dernier coup fort, écran dédoublé (2×2) et grésillement ; on redescend et on finit en
    frappant."""
    L.screen_split(216, 4)
    drop(L, 216, glitch=0.45)
    L.screen_split(228, 1)
    L.ramp(228, 2.0, -8)
    L.enemy(231)
    L.enemy(231.5)
    L.enemy(232)
    L.wall(233)
    L.enemy(234)
    L.enemy(235)                                # le dernier coup


def build():
    L = Level()
    scene_intro(L)
    scene_couplet(L)
    drop(L, 96)
    scene_couplet2(L)
    drop(L, 156, glitch=0.4, descend=-8)        # on redescend vers le pont
    scene_pont(L)
    scene_montee(L)
    scene_final(L)
    for name, beat in SECTIONS:
        if beat > 0:
            L.add(beat, "section_change", intensity=1.2 if "refrain" in name else 0.5)
    L.ev.sort(key=lambda e: (e["beat"], e["type"]))
    return L.ev


FICHE = Fiche(
    id="n4_forge",
    title="Chaos ID - Soularflair & canmastersmash",
    bpm=125.0, offset=0.052, beats=236,
    run_speed=8.0,                           # 3,84 u par temps : à 3,2, le niveau défilait plus lentement que les Échos
    sections=SECTIONS,
    analysis="n4_forge_analysis.json",
    build=build,
    zoom=480, zoom_wide=640, wide_on_refrains=True,     # plans larges sur les coups forts (la chaîne de tambours)
    scale=(1, 4, 6, 8, 11),                  # do dièse mineur pentatonique (do#, mi, fa#, sol#, si)
    mechanics=frozenset({"gap", "wall", "step", "enemy", "stomp", "vine", "flyer", "orb", "gravity",
                         "bounce", "ramp", "glitch", "screen_flip", "screen_split"}),
    margin_ms=120.0,
)
