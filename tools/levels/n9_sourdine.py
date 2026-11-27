"""Niveau 9 — La Sourdine, le défi de l'élite (~300 essais), sur « BIKE » de Tangermusic (140 BPM).

Morceau : Newgrounds 1191782, licence CC BY-NC-ND ; utilisé **entier, sans coupe**, avec l'accord de l'artiste (projet
étudiant non commercial, Audio/LICENSES.md). Converti en .ogg tel quel (aucune coupe : la licence l'interdit).
  python3 tools/analyse-song.py game/Assets/_Project/Audio/Music/n9_sourdine.ogg tools/data/n9_sourdine_analysis.json \\
      --bpm 140 --offset 0.034 --beats 232
Forme (temps ; énergie du morceau) : intro calme 0-28 (le vélo qui dévale), montée 28-36, drop 36-76, cassure 76-96
(caisse claire sur chaque temps), reprise 96-120, roulement de caisse 120-144 (croches), second drop 144-224, fin 224-232.

Le niveau (ADR-040) : on part de la Sylve du niveau 1, en couleur et au calme. La musique monte, l'image grésille, un
tambour nous jette dans le drop : la Sourdine a avalé la couleur. Le monde est son négatif (planches de
tools/gemini-niveau9), il passe du noir au blanc sur les temps forts (`invert` : seul le monde s'inverse, Kenta et
l'interface gardent leurs couleurs), se retourne, se dédouble. La Sourdine nous regarde depuis le fond (le gardien du
monde sourd). Au second drop, tout s'accélère ; le dernier coup sur le dernier temps fort ramène la couleur.
Inversions : un état sur un temps fort, jamais plus d'une par temps (2,3 par seconde au plus à 140 BPM, borne publique).
"""
from levelgen.builder import Level, cuirasses, grotte, lianes, notes
from levelgen.fiche import ALL, Fiche

SECTIONS = [("intro", 0), ("fissure", 28), ("refrain : le silence", 36), ("écho", 76), ("montée", 96),
            ("refrain : la sourdine", 144), ("le dernier coup", 208), ("la couleur", 224)]
SYLVE, SOURD = "Biome_01_sylve", "Biome_09_sourd"


def hits(L, *beats):
    for b in beats:
        L.enemy(b)


def intro(L):
    """0-28, la Sylve au calme. Accents : 0 S | 3 s | 5 k | 6,5 k | 19 k | 24 k | 25,5 s | 26,5 s."""
    hits(L, 5, 8)
    L.gap(11, 0.5)
    hits(L, 13, 16)
    L.wall(19)
    hits(L, 21, 24)
    L.step(25.5, +1)
    hits(L, 26.5)


def fissure(L):
    """28-36, la montée : l'image grésille de plus en plus. Accents : 31 s | 35 X (le drop arrive)."""
    hits(L, 28, 29, 31)
    L.gap(32, 0.5)
    hits(L, 33.5)
    L.bounce(34, 2.0, -1)                       # 34 → 36 : le tambour nous jette dans le drop


def silence(L):
    """36-76, le drop : demi-temps, caisse claire et basse. Accents : 37,5 k | 41,5 S | 43,5 k | 44 S | 46,5 s | 47 S |
    48 s | 50 s | 51 s | 52 K | 53 k | 55,5 s | 56,5 s | 57,5 s | 59 s | 60,5 s | 61 s | 62 s | 63 S | 68,5 s | 70,5 s | 73 k."""
    hits(L, 37.5, 38, 38.5)                   # pas de coup à 36 : on retombe du tambour
    L.gap(39.5, 0.5)
    hits(L, 40.5, 41, 41.5, 42)
    L.wall(43)
    hits(L, 44, 44.5)
    cuirasses(L, 46, (1.0, 1.0))                # 45 saut, 46 47 appuis, → 48
    hits(L, 48.5, 49, 49.5)
    L.gap(50.5, 0.5)
    hits(L, 51.5, 52, 52.5, 53)
    lianes(L, 54, [1.5, 1.0])                   # 54 saut, lâchers 56,25 et 58,25 → 59,25
    L.flyer(55)
    hits(L, 60, 60.5, 61)                      # 61-64 : on souffle (point de reprise)
    hits(L, 64, 64.5)
    L.gap(65.5, 0.5)
    notes(L, 67, [0.5, 1.5, 2.5])               # 67 saut, notes 68 69 70 → 71
    hits(L, 71.5, 72, 72.5)
    L.wall(73.5)
    hits(L, 74.5, 75, 75.5)


def echo(L):
    """76-96, la cassure : la caisse claire tombe sur chaque temps (76,5 X 77 s | 79,5 X 80 s 81 X 81,5 X 82 s 83 X 83,5 X |
    85 X 85,5 S | 87 X 87,5 S | 89 X 89,5 S | 90 S 91 X | 93 S 93,5 X 94 s 94,5 s 95 S 95,5 s)."""
    hits(L, 76.5, 77)
    L.gap(78, 0.5)
    hits(L, 79.5, 80, 80.5, 81, 81.5)
    L.wall(82.5)
    hits(L, 83.5, 84, 85, 85.5)
    L.gap(86.5, 0.5)
    hits(L, 87.5, 88, 89, 89.5, 90)
    L.step(91, +1)
    hits(L, 92, 93, 93.5, 94, 94.5)
    L.step(95.5, -1)


def montee(L):
    """96-144, la reprise puis le roulement de caisse (120-144 : croches sur presque tout). La première moitié se joue
    tête en bas, au plafond ; la seconde en rafales de frappes, une par croche."""
    hits(L, 96, 96.5, 97, 98, 98.5, 99)
    grotte(L, 101, 111, jumps=(104, 108), hanging=(102.5, 103, 105.5, 106, 106.5, 109.5, 110))
    hits(L, 112, 112.5, 113)
    L.gap(114, 0.5)
    cuirasses(L, 117, (1.0, 1.0))              # 116 saut, 117 118 appuis, → 119
    # le roulement : rafales en croches, séparées par un saut
    hits(L, 120, 120.5, 121, 121.5, 122, 122.5)
    L.gap(123.5, 0.5)
    hits(L, 125, 125.5, 126, 126.5, 127, 127.5)
    L.wall(128.5)
    hits(L, 130, 130.5, 131, 131.5, 132, 132.5, 133, 133.5)
    L.gap(134.5, 0.5)
    hits(L, 136, 136.5, 137, 137.5)
    L.step(138.5, +1)
    hits(L, 139.5, 140)
    L.bounce(141, 3.0, 0)                       # 141 → 144 : l'envolée vers le second drop


def sourdine(L):
    """144-208, le second drop : tout, vite, sans répit. Accents : 145 s | 156 S | 158,5 S | 159,5 S | 161,5 k | 162 S |
    163 k | 165,5 s | 176 s | 180 S | 182 S | 185,5 S | 187 s | 188,5 s | 189 s | 191 S | 192 s | 195 s | 196 s | 197,5 s |
    201,5 S | 203,5 s | 205 X | 206,5 s."""
    hits(L, 145, 145.5, 146)                  # on retombe de l'envolée sur 144
    L.gap(147, 0.5)
    hits(L, 148, 148.5, 149)
    lianes(L, 150, [1.25, 1.0, 1.0])            # 150 saut, lâchers ~152 154 156 → 157
    L.flyer(151)
    hits(L, 157.5, 158, 158.5, 159, 159.5)
    L.wall(160.5)
    hits(L, 161.5, 162, 162.5, 163)
    cuirasses(L, 165, (1.0, 1.0, 1.0))          # 164 saut, 165 166 167 appuis, → 168
    hits(L, 170.5, 171)                        # 168-170 : on souffle (point de reprise)
    notes(L, 172, [0.5, 1.5, 2.5, 1.5])         # 172 saut, notes 173-176 → 177
    hits(L, 177.5, 178, 178.5, 179)
    L.step(180, +1)
    hits(L, 181, 181.5, 182, 182.5)
    grotte(L, 184, 192, jumps=(187,), hanging=(185, 185.5, 186, 188.5, 189, 189.5, 190.5, 191))
    hits(L, 193, 193.5)
    L.gap(194.5, 0.5)
    hits(L, 195.5, 196, 196.5, 197, 197.5)
    L.wall(198.5)
    hits(L, 199.5, 200, 200.5, 201, 201.5, 202)
    L.step(203, -1)
    hits(L, 204, 204.5, 205, 205.5)
    L.gap(206.5, 0.5)


def dernier_coup(L):
    """208-224, le dernier passage : croches, sauts serrés, et le coup final sur le dernier temps fort (223)."""
    hits(L, 208, 208.5, 209, 209.5, 210)
    L.wall(211)
    hits(L, 212, 212.5, 213, 213.5)
    L.gap(214.5, 0.5)
    hits(L, 215.5, 216, 216.5, 217)
    cuirasses(L, 219, (1.0,))                  # 218 saut, 219 appui → 220
    hits(L, 220.5, 221, 221.5, 222, 222.5)
    L.enemy(223)                                # le dernier coup


def couleur(L):
    """224-232 : la couleur revient. On court, la Sylve respire."""
    hits(L, 226, 228)


def effects(L):
    """La Sourdine prend la couleur, puis l'image. Inversions (le monde en négatif, `invert`) sur les temps forts, au plus
    une par temps ; miroir, tête en bas, dédoublé sur les débuts de phrase ; grésillement de plus en plus fort."""
    # la fissure : le grésillement monte, puis le monde bascule dans le négatif au drop
    L.glitch(28, 0.25, 4)
    L.glitch(32, 0.6, 4)
    L.add(35, "fx_flash", intensity=0.5)
    # le silence : un temps fort sur deux mesures, le monde passe du noir au blanc
    for b in (44, 52, 60, 68):
        L.invert(b, True)
        L.invert(b + 4, False)
    L.screen_flip(48, "mirror")
    L.screen_flip(56, "none")
    L.glitch(62, 0.4, 2)
    # l'écho : le monde cligne sur chaque caisse claire, deux mesures (au plus une bascule par temps)
    for b in (80, 82, 84, 86):
        L.invert(b, True)
        L.invert(b + 1, False)
    L.screen_split(88, 4)
    L.screen_split(92, 1)
    # la montée : tête en bas dans la grotte, puis l'image se dédouble et grésille sur le roulement
    L.screen_flip(104, "up")
    L.screen_flip(112, "none")
    L.screen_split(128, 4)
    L.screen_split(132, 1)
    L.glitch(136, 0.8, 8)
    L.add(141, "fx_flash", intensity=0.5)
    # la Sourdine : le monde reste blanc sur les phrases paires, l'image se retourne sur les autres
    L.invert(144, True)
    L.invert(152, False)
    L.screen_flip(152, "mirror")
    L.invert(160, True)
    L.screen_flip(160, "none")
    L.invert(168, False)
    L.screen_split(168, 4)
    L.screen_split(172, 1)
    L.screen_flip(176, "up")
    L.invert(176, True)
    L.invert(184, False)
    L.screen_flip(184, "none")
    L.glitch(184, 0.5, 8)
    L.screen_flip(192, "both")
    L.invert(192, True)
    L.screen_flip(200, "none")
    L.invert(200, False)
    for b in (204, 205, 206, 207):              # avant le dernier passage : une bascule par temps, une mesure
        L.invert(b, b % 2 == 0)
    # le dernier coup : le monde reste blanc, l'image grésille, puis tout revient
    L.invert(208, True)
    L.glitch(208, 0.7, 14)
    L.invert(216, False)
    L.screen_flip(216, "mirror")
    L.screen_flip(220, "none")
    L.add(223, "fx_flash", intensity=0.5)
    L.invert(224, False)


def build():
    L = Level()
    intro(L)
    fissure(L)
    silence(L)
    echo(L)
    montee(L)
    sourdine(L)
    dernier_coup(L)
    couleur(L)
    effects(L)
    for name, beat in SECTIONS:
        if beat > 0:
            L.add(beat, "section_change", intensity=1.2 if "refrain" in name else 0.5)
    L.ev.sort(key=lambda e: (e["beat"], e["type"]))
    return L.ev


FICHE = Fiche(
    id="n9_sourdine",
    title="La Sourdine",
    bpm=140.0, offset=0.034, beats=232,
    sections=SECTIONS,
    analysis="n9_sourdine_analysis.json",
    build=build,
    zoom=480, zoom_wide=640,
    scale=(6, 9, 11, 1, 4),                     # fa# mineur pentatonique (tonique estimée : fa#)
    mechanics=ALL,
    margin_ms=100.0,                            # comme le niveau 8 ; la difficulté vient de la densité et de l'image
    checkpoint_every=16,                        # un point de reprise toutes les 4 mesures (8 ailleurs) : pas de répit
    biomes={"intro": SYLVE, "fissure": SYLVE, "la couleur": SYLVE},   # le reste : le monde sourd (biome du niveau)
)
