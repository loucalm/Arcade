"""Niveau 2 — Le Bayou, sur *Arpanauts* (Eric Skiff, *Resistor Anthems*, CC BY 4.0), 120 BPM (ADR-034).

Deux mécaniques nouvelles, apprises l'une après l'autre (seule, répétée, variée, combinée) :
- les **cuirassés** : on leur saute dessus, et un appui au rebond donne le grand rebond (couplet) ;
- les **lianes** au-dessus du marais : on s'y accroche en sautant dans le vide, et on saute pour lâcher, sur un temps
  (refrain).
Plus dense que le niveau 1 (≈ 1,8 action par mesure, contre 1,2) et sur tous les temps (noires) : les sauts et les
lâchers sur la grosse caisse (temps 1 et 3), les coups sur la caisse claire (temps 2 et 4). Pas de contretemps, pas
d'écran qui bouge, vue fixe.

Morceau monté par `tools/cut-song.py` (mesures 0 à 51, fondu sur les 8 derniers temps, 1 min 44) :
  python3 tools/cut-song.py "<Resistor Anthems>/10 Arpanauts.mp3" \
      game/Assets/_Project/Audio/Music/n2_bayou_arpanauts.ogg --bpm 120 --offset 0.048 --bars 0-51 --fade 8
  python3 tools/analyse-song.py game/Assets/_Project/Audio/Music/n2_bayou_arpanauts.ogg \
      tools/data/n2_bayou_analysis.json --bpm 120 --offset 0.048 --beats 208
Forme (temps) : arpèges 0-32, la batterie entre 32-64, refrain 64-128, pont (caisse claire) 128-160, pause 160-176,
remontée 176-192, fin 192-208. Premier temps trouvé aux changements d'accord et à la basse (phase 0).
"""
from levelgen.builder import Level, cuirasses, lianes
from levelgen.fiche import Fiche

SECTIONS = [("intro", 0), ("couplet", 32), ("refrain", 64), ("refrain 2", 96), ("pont", 128), ("pause", 160),
            ("montée", 176), ("fin", 192)]


def scene_intro(L):
    """0-32 (arpèges) : les gestes du niveau 1, plus serrés. Sauter sur le 1, frapper sur le 4, une marche, une
    souche, et on finit sur un coup."""
    L.gap(4, 0.5)
    L.enemy(7)
    L.wall(8)
    L.enemy(11)
    L.step(12, +1)
    L.enemy(15)
    L.gap(16, 0.5)
    L.enemy(19)
    L.gap(20, 0.5)
    L.step(22, -1)              # on se laisse tomber
    L.enemy(23)
    L.gap(24, 0.5)
    L.enemy(27)
    L.wall(28)
    L.enemy(31)


def scene_couplet(L):
    """32-64 (la batterie entre) : les cuirassés. Un seul (on saute sur le 4, rebond sur le 1), un deuxième, un
    troisième qui nous pose plus haut, puis deux enchaînés."""
    L.enemy(33)
    cuirasses(L, 36)                            # 35 saut, 36 appui, on se pose à 38
    L.enemy(39)
    cuirasses(L, 44)                            # → 46
    L.enemy(47)
    L.enemy(49)
    cuirasses(L, 52, rise=1.0)                  # → 54, une marche plus haut
    L.enemy(55)
    cuirasses(L, 58, (1.0, 2.0))                # 57 saut, 58 petit rebond vers le second (il reste à l'image), 59, → 61
    L.enemy(63)


def scene_refrain(L):
    """64-96 : les lianes au-dessus du marais. Une liane (on saute sur le 1, on lâche sur le 3), une deuxième, puis
    deux à la suite, et pour finir une liane qui mène droit sur un cuirassé."""
    lianes(L, 64, [1.25])                       # 66 lâcher, → 67
    L.enemy(69)
    lianes(L, 72, [1.25])                       # 74, → 75
    L.enemy(77)
    lianes(L, 80, [1.25, 1.0])                  # 82, 84, → 85
    L.enemy(87)
    t = lianes(L, 88, [1.25])                   # 90, → 91
    cuirasses(L, t + 1)                         # 91 saut, 92 appui, → 94
    L.enemy(95)


def scene_refrain2(L):
    """96-128 : le refrain revient, et on mélange tout en grimpant : deux marches, une liane, une souche, deux
    cuirassés qui nous posent plus haut, un trou, et trois lianes à la suite pour finir."""
    L.step(96, +1)
    L.step(98, +1)
    L.enemy(99)
    lianes(L, 100, [1.25])                      # 102, → 103
    L.wall(104)
    L.enemy(105)
    cuirasses(L, 108, (1.0, 2.0), rise=1.0)     # 107 saut, 108, 109, → 111 plus haut
    L.enemy(113)
    L.gap(114, 0.5)
    L.enemy(115)
    L.step(116, -1)
    L.enemy(117)
    lianes(L, 120, [1.25, 1.0, 1.0])            # 122, 124, 126, → 127


def scene_pont(L):
    """128-160 (caisse claire, on flotte) : une longue traversée de liane en liane, des coups, deux cuirassés et deux
    marches qui préparent la pause."""
    L.enemy(129)
    lianes(L, 132, [1.25, 1.0, 1.0])            # 134, 136, 138, → 139
    L.enemy(141)
    cuirasses(L, 144)                           # 143 saut, → 146
    L.enemy(147)
    L.gap(148, 0.5)
    L.wall(150)
    L.enemy(151)
    lianes(L, 152, [1.25])                      # 154, → 155
    L.step(156, +1)
    L.step(158, +1)
    L.enemy(159)


def scene_pause(L):
    """160-176 : la musique se vide, on souffle. On redescend, un coup, deux sauts."""
    L.step(164, -2)
    L.enemy(165)
    L.gap(168, 0.5)
    L.gap(172, 0.5)


def scene_montee(L):
    """176-192 : la batterie revient. On regrimpe une marche par temps fort avec un coup entre chacune, puis deux
    cuirassés enchaînés."""
    for b in (176, 178, 180):
        L.step(b, +1)
        L.enemy(b + 1)
    cuirasses(L, 184, (1.0, 2.0))               # 183 saut, 184, 185, → 187
    L.enemy(189)
    L.enemy(191)


def scene_fin(L):
    """192-208 : une dernière liane, on retombe au sol en frappant, une souche, et le morceau s'éteint."""
    lianes(L, 192, [1.25])                      # 194, → 195
    L.step(196, -3)
    L.enemy(197)
    L.enemy(199)
    L.wall(200)
    L.enemy(203)


def build():
    L = Level()
    scene_intro(L)
    scene_couplet(L)
    scene_refrain(L)
    scene_refrain2(L)
    scene_pont(L)
    scene_pause(L)
    scene_montee(L)
    scene_fin(L)
    for name, beat in SECTIONS:
        if beat > 0:
            L.add(beat, "section_change", intensity=1.2 if name == "refrain" else 0.5)
    L.ev.sort(key=lambda e: (e["beat"], e["type"]))
    return L.ev


FICHE = Fiche(
    id="n2_bayou",
    title="Arpanauts - Eric Skiff",
    bpm=120.0, offset=0.048, beats=208,
    sections=SECTIONS,
    analysis="n2_bayou_analysis.json",
    build=build,
    zoom=480, zoom_wide=480,                 # vue fixe
    scale=(2, 5, 7, 9, 10, 0),               # ré, fa, sol, la, si♭, do : les notes de la mélodie (ré mineur sans mi)
    mechanics=frozenset({"gap", "wall", "step", "enemy", "stomp", "vine"}),
    margin_ms=150.0,
)
