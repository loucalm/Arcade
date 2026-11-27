"""Niveau 9 — La Sourdine, le défi de l'élite (~300 essais), sur « BIKE » de Tangermusic (140 BPM, joué sur une grille de
280 BPM : voir double_time).

Morceau : Newgrounds 1191782, licence CC BY-NC-ND ; utilisé **entier, sans coupe**, avec l'accord de l'artiste (projet
étudiant non commercial, Audio/LICENSES.md). Converti en .ogg tel quel (aucune coupe : la licence l'interdit).
  python3 tools/analyse-song.py game/Assets/_Project/Audio/Music/n9_sourdine.ogg tools/data/n9_sourdine_analysis.json \\
      --bpm 280 --offset 0.034 --beats 464        (grille du jeu : le double du morceau)
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

SECTIONS_SONG = [("intro", 0), ("fissure", 28), ("refrain : le silence", 36), ("écho", 76), ("montée", 96),
                 ("refrain : la sourdine", 144), ("le dernier coup", 208), ("la couleur", 224)]
SECTIONS = [(n, b * 2) for n, b in SECTIONS_SONG]   # grille du jeu à 280 BPM (voir double_time)
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


# Grille à 280 BPM (le double du morceau, demande de Lou) : les scènes restent écrites en temps du morceau (140) et
# `double_time` les recale sur la grille du jeu. Un saut dure toujours 1 temps de jeu (0,21 s), la course fait 3,2 u
# par temps (15 u/s) : tout va deux fois plus vite. Les positions musicales doublent, mais les écarts liés au saut restent
# ceux du jeu : cuirassé 1 temps après le saut, liane prise 0,75 temps après, première note 1 temps après, trou du
# plafond de j + 0,2 à j + 0,7, falaise d'un tambour. Mesuré : à 280 BPM, une action laisse au plus ~50 ms de marge.
TEMPO_FACTOR = 2


def double_time(song_events):
    k = TEMPO_FACTOR
    prev_song_end = None
    stomps = {e["beat"] for e in song_events if e["type"] == "enemy" and e.get("params", {}).get("variant") == "stomp"}
    gaps = [e for e in song_events if e["type"] == "gap" and e.get("lane", 0) == 0]
    bounces = [e for e in song_events if e["type"] == "bounce"]
    jumps = {e["beat"] for e in song_events if e["type"] == "jump"}
    first_vines = {g["beat"] + 0.75 for g in gaps}               # liane prise 3/4 de temps après le saut dans le vide
    first_orbs = {g["beat"] + 1.0 for g in gaps}                  # première note 1 temps après le saut
    # cuirassés enchaînés : un rebond par temps de jeu (une croche du morceau). Un rebond de 2 temps de jeu monterait à
    # 6 u et cacherait le cuirassé suivant (règle du générateur) ; le premier garde sa place dans la musique.
    chain = {}
    prev_end = None
    for e in sorted((x for x in song_events if x["type"] == "enemy" and x.get("params", {}).get("variant") == "stomp"),
                    key=lambda x: x["beat"]):
        b, air = e["beat"], e["params"]["length"]
        nb = prev_end if prev_end is not None and abs(b - prev_song_end) < 1e-6 else b * k
        last_of_chain = not any(abs(x["beat"] - (b + air)) < 1e-6 for x in song_events
                                if x["type"] == "enemy" and x.get("params", {}).get("variant") == "stomp")
        new_air = air * k if last_of_chain else 1.0
        chain[b] = (nb, new_air)
        prev_end, prev_song_end = nb + new_air, b + air
    out = []
    for e in song_events:
        b = e["beat"]
        p = dict(e.get("params", {}))
        t = e["type"]
        nb = b * k
        if t == "enemy" and b in chain and p.get("variant") == "stomp":
            nb, p["length"] = chain[b]
        elif t == "jump" and (b + 1.0) in stomps:
            nb = b * k + k - 1.0                                  # on saute 1 temps de jeu avant le cuirassé
        elif t == "vine" and b in first_vines:
            gap = b - 0.75
            nb = gap * k + 0.75
            p["length"] = round((b + p["length"]) * k - nb, 3)     # le lâcher reste à sa place dans la musique
            p["step"] = p["step"] * k
        elif t == "orb" and b in first_orbs:
            gap = b - 1.0
            nb = gap * k + 1.0
            p["step"] = round((b + p["step"]) * k - nb, 3)         # la note suivante reste à sa place
        elif t == "ceiling":
            nb = b * k
            length = p["length"] * k
            end = b + p["length"]
            if any(abs(end - (j + 0.2)) < 1e-6 for j in jumps):    # finit au bord d'un trou : 0,2 temps de jeu après le saut
                length = (end - 0.2) * k + 0.2 - nb
            if any(abs(b - (j + 0.7)) < 1e-6 for j in jumps):      # reprend après un trou : 0,7 temps de jeu après le saut
                j = b - 0.7
                new_start = j * k + 0.7
                length += nb - new_start
                nb = new_start
            p["length"] = round(length, 3)
        elif t == "step" and e.get("lane") == 1:                   # falaise d'un tambour (builder.bounce)
            for bo in bounces:
                b0, air = bo["beat"], bo["params"]["length"]
                if not (b0 < b <= b0 + air):
                    continue
                before = [x for x in song_events if x["type"] == "step" and x["beat"] < b0]
                h0 = before[-1]["params"]["height"] if before else 0.0
                up = p["height"] > h0                              # on monte : falaise à la fin de l'envol
                nb = b0 * k + (air - 1.0 if up else 1.0)        # l'envol garde sa durée en temps de jeu
                break
        elif t == "bounce":
            pass                                                   # envol en temps de jeu : 4 temps monteraient à 55 u
        else:
            for key in ("length", "step"):
                if key not in p:
                    continue
                if t == "gap" and e.get("lane", 0) == 0 and p[key] < 1.0:
                    continue                                       # trou simple : franchi d'un saut, longueur inchangée
                p[key] = round(p[key] * k, 3)
        ne = dict(e, beat=round(nb, 3))
        if p:
            ne["params"] = p
        out.append(ne)
    # un ennemi juste après une réception (notes, liane, cuirassé) : le héros est haut, la caméra ne voit pas encore le
    # sol ; il recule d'un temps de jeu (une croche du morceau) pour se voir 400 ms avant le coup
    aerial = sorted(e["beat"] for e in out if e["type"] in ("orb", "vine")
                    or (e["type"] == "enemy" and e.get("params", {}).get("variant") == "stomp"))
    actions = [x for x in out if x["type"] in ("enemy", "jump", "gap", "wall", "step", "orb", "vine", "bounce")]
    for e in out:
        if e["type"] == "enemy" and e.get("lane", 0) == 0 and not e.get("params", {}).get("variant"):
            landing = [a for a in aerial if 0 < e["beat"] - a <= 5.0]
            prev = [x["beat"] for x in actions if x is not e and landing and landing[-1] < x["beat"] < e["beat"]]
            free = not any(x is not e and abs(x["beat"] - (e["beat"] + 1.0)) < 1.0 for x in actions)
            if landing and not prev and free:          # la première action après la réception, et la place est libre
                e["beat"] = round(e["beat"] + 1.0, 3)
            elif landing and not prev:                 # rafale juste après la réception : son premier coup est retiré
                e["drop"] = True
    out = [e for e in out if not e.pop("drop", False)]
    out.sort(key=lambda e: (e["beat"], e["type"]))
    return out


def build_song_time():
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
    for name, beat in SECTIONS_SONG:
        if beat > 0:
            L.add(beat, "section_change", intensity=1.2 if "refrain" in name else 0.5)
    L.ev.sort(key=lambda e: (e["beat"], e["type"]))
    return L.ev


def build():
    return double_time(build_song_time())


FICHE = Fiche(
    id="n9_sourdine",
    title="La Sourdine",
    bpm=280.0, offset=0.034, beats=464,         # le double du morceau (140) : voir double_time
    sections=SECTIONS,
    analysis="n9_sourdine_analysis.json",
    build=build,
    zoom=640, zoom_wide=640,                    # vue large fixe : à 15 u/s, la vue 480 montrait les ennemis trop tard
    scale=(6, 9, 11, 1, 4),                     # fa# mineur pentatonique (tonique estimée : fa#)
    mechanics=ALL,
    margin_ms=50.0,                             # à 280 BPM, ~50 ms est le plus qu'une action peut laisser (mesuré)
    checkpoint_every=32,                        # un point de reprise toutes les 4 mesures du morceau : pas de répit
    biomes={"intro": SYLVE, "fissure": SYLVE, "la couleur": SYLVE},   # le reste : le monde sourd (biome du niveau)
)
