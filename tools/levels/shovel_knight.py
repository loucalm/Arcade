"""Niveau « Main Theme » (Shovel Knight, 150 BPM, 256 beats) : la chart commune aux 8 niveaux aujourd'hui
(`level01_shovel_knight.json`), et le brouillon du niveau 6, le Grand Orage, qui cumule tout (ADR-034).

Le morceau est découpé en scènes calées sur sa structure. Chaque scène a une idée de mise en scène : escalier qu'on
grimpe sur les temps, tambour qui projette dans le ciel au drop, lianes au-dessus de l'eau, course au plafond, passage
dans l'arrière-plan, tour à gravir sur le refrain final. Les hauteurs sont relatives (on monte de +1, on plonge
de −8…) : le passage 2 rejoue le passage 1 à partir de la hauteur où l'on est.

⚠️ Musique provisoire, protégée (`Audio/LICENSES.md`) : ce niveau sera réécrit sur *Boss Fight* (Juhani Junkala).
Commande : `python3 tools/gen-level.py shovel_knight` (format des événements : docs/chart-format.md).
"""
from levelgen.builder import Level
from levelgen.fiche import ALL, Fiche

SECTIONS = [
    ("intro", 0), ("couplet", 32), ("pont", 64), ("roulement", 80), ("refrain", 96), ("final", 128),
    ("couplet 2", 152), ("pont 2", 184), ("roulement 2", 200), ("refrain 2", 216), ("fin", 248),
]


# ---------------------------------------------------------------- les scènes
# Principes : docs/level-design-musical.md. Les temps viennent de la carte du morceau (tools/data/level01_analysis.json,
# voir `python3 tools/gen-level.py shovel_knight --map`) : grosse caisse → sauts, caisse claire → coups de poing.
# Les pièces (arcs au-dessus des sauts, lignes de mélodie) et la hauteur des ennemis volants sont posées ensuite,
# automatiquement, sur la trajectoire simulée (resolve).

def scene_intro(L):
    """0-32 : un geste nouveau toutes les deux mesures, avec de l'air autour (la musique est encore calme).
    Sauter (4), une souche (6), frapper (8, 10), monter (12, 14), frapper (16,5), sauter sur un cuirassé et appuyer au rebond (19 → 20),
    traverser un gouffre de note de saut en note de saut (23 → 24, 25, 26), puis la montée de grosse caisse et le
    premier tambour : envol sur le drop du couplet."""
    L.gap(4, 0.5)
    L.wall(6)                   # une souche
    L.enemy(8)                  # k
    L.enemy(10)
    L.step(12, +1)
    L.step(14, +1)              # K
    L.enemy(16.5)               # (avant : une glissade, retirée du jeu)
    L.add(19, "jump")           # on saute sur le cuirassé…
    L.stomp(20, 2.0)            # S : … et on appuie au rebond : grand rebond
    L.cliff(21.25, 1.0)         # on se pose un étage plus haut (22)
    L.gap(23, 3.6)              # le gouffre : on saute…
    h = L.h
    L.orb(24, 0.5)              # … et on relance de note en note
    L.orb(25, 1.8)
    L.orb(26, 2.5)
    L.cliff(26.25, 2.0)         # l'autre bord, plus haut (27)
    L.gap(28.5, 0.5)            # K : la montée commence
    L.bounce(30, 2.0, +5)       # X X : envol, on atterrit sur le premier temps du couplet


def scene_couplet(L, base, variant=False):
    """32-64 : on grimpe en suivant la caisse claire. Coups par deux, une marche, deux cuirassés enchaînés (on rebondit
    de l'un à l'autre en appuyant sur 41 et 43), la course sur le mur (note tenue), une glissade, deux notes de saut
    au-dessus du vide (54,5 ; 55,5), puis la dévalée."""
    b = base
    L.enemy(b + 1)              # 33 S
    L.enemy(b + 2)              # 34 S
    L.enemy(b + 4)              # 36 S
    L.enemy(b + 5)              # 37 S
    L.step(b + 6, +1)           # 38 k : une marche
    L.add(b + 8, "jump")        # 40 : on saute sur le premier cuirassé
    L.stomp(b + 9, 1.5)         # 41 S : rebond vers le second (bas : le second reste à l'image)
    L.stomp(b + 10.5, 2.0)      # 42,5 k : rebond vers la falaise
    L.cliff(b + 11.75, 1.0)     # on se pose plus haut (44,5)
    L.enemy(b + 15)             # 47 S
    # course sur le mur : de 48 à 49,5, +5 (+6 au passage 2), sur la note tenue
    L.ramp(b + 16, 1.5, +6 if variant else +5)
    L.enemy(b + 18.5)           # 50,5 : au sommet du mur, un coup
    L.gap(b + 21.5, 2.6)        # 53,5 : dans le vide…
    L.orb(b + 22.5, 0.5)        # 54,5 S : … de note en note
    L.orb(b + 23.5, 1.5)        # 55,5 S
    L.cliff(b + 23.75, 1.0)     # l'autre bord (56,5)
    L.enemy(b + 25)             # 57 S
    # la dévalée : on file dans la pente sur un temps et demi
    L.ramp(b + 25.5, 1.5, -8)
    L.enemy(b + 28)             # 60 S
    L.enemy(b + 29)             # 61 S
    L.gap(b + 31, 0.5)          # 63


def scene_pont(L, base, variant=False):
    """64-80 : on flotte. Le sol disparaît ; plateformes à la hauteur de la mélodie, une par caisse claire paire,
    et des ennemis en vol à frapper entre deux sauts."""
    b = base
    L.void(b + 0.5, 13.0)
    rises = [1, 2, 1, 2, 3, 2, 1] if not variant else [1, 2, 3, 2, 3, 4, 3]
    h0 = L.h
    for i, r in enumerate(rises):
        L.add(b + 2 * i, "platform", length=1.9, height=h0 + r)
        if i in (2, 4, 5):
            L.flyer(b + 2 * i + 1)
    L.h = h0
    if rises[-1] > 1:
        L.cliff(b + 13.5, rises[-1] - 1)   # on reprend pied un cran plus haut


def scene_pont_lianes(L, base):
    """64-80 : au-dessus de l'eau, de liane en liane. On saute dans le vide (64,5), on attrape la première liane,
    chaque lâcher tombe sur un temps (67, 70, 73, 76) et on frappe une chauve-souris à chaque balancier (caisse claire
    66, 69, 72). On remonte d'une liane à l'autre et on se pose sur une falaise (77,5)."""
    b = base
    L.gap(b + 0.5, 12.6)        # 64,5 : le saut dans le vide (on attrape la liane en l'air)
    h0 = L.h
    L.vine(b + 1.25, 1.75, 1.1, 1.6)                   # 65,25 → 67
    L.flyer(b + 2)              # 66 S
    L.vine(b + 4, 2.0, 2.1, 2.6)                       # 68 → 70
    L.flyer(b + 5)              # 69 S
    L.vine(b + 7, 2.0, 3.1, 3.6)                       # 71 → 73
    L.flyer(b + 8)              # 72 s
    L.vine(b + 10, 2.0, 4.1, 4.6, air=1.5)             # 74 → 76, vol jusqu'à 77,5
    L.h = h0
    L.cliff(b + 12.75, 4.0)     # la falaise où l'on se pose (atteinte en vol)


def scene_roulement(L, base, variant=False, launch=True):
    """80-96 : la grosse caisse tape à contretemps : on gravit un escalier, une marche par coup, puis on saute
    de toit en toit. Le tambour géant ouvre le refrain."""
    b = base
    for k in (1.5, 2.5, 3.5, 4.5, 5.5, 6.5):          # 81,5 … 86,5 : X X S X K K
        L.step(b + k, +1)
    for k, kind in ((8.5, "gap"), (9.5, "step"), (10.5, "gap"), (11.5, "step"), (12.5, "gap"), (13.5, "step")):
        if kind == "gap":
            L.gap(b + k, 0.5)
        else:
            L.step(b + k, +1)
    if launch:
        L.bounce(b + 15.5, 2.5, +6)                     # 95,5 → 98 : dans le ciel


def scene_refrain_ciel(L, base):
    """Prototype (92-128) : le refrain comme des montagnes russes, 3 écrans de haut. Chaque geste tombe sur la
    batterie (carte : --map) et chaque action laisse au moins 120 ms de marge (--marges).
      95,5  tambour géant sur le drop : on décolle de 14 u, coup de poing en plein vol sur la caisse claire (97)
      100   course sur le mur (automatique) sur la note tenue, puis un tambour qui relance (102)
      104,5 le roulement de grosse caisse : quatre tambours en chaîne, un par coup, on grimpe à chaque rebond
      108,5 le sommet : deux ennemis, un trou, sur la caisse claire
      112   le grand plongeon : deux dévalées en pente raide (−26 u), le sol reste toujours à l'image
      120   on rebondit (chauve-souris au sommet), on finit au sol en frappant sur les derniers temps."""
    b = base
    L.bounce(b - 0.5, 2.5, +10)  # 95,5 → 98 : le drop
    # pas de chauve-souris sur le tambour géant : au sommet, on est trop haut pour l'avoir vue venir
    L.enemy(b + 3)              # 99 S
    L.ramp(b + 4, 1.5, +6)      # 100 → 101,5 : course sur le mur
    L.bounce(b + 6, 1.5, +3)    # 102 k : relance (courte : la chauve-souris reste à l'image)
    L.flyer(b + 6.5)            # 102,5 S : juste avant le sommet, à hauteur du héros
    for k in (8.5, 9.5, 10.5, 11.5):                   # 104,5 … 107,5 : K K K K, on grimpe à chaque rebond
        L.bounce(b + k, 1.0, +2.5, lums=False)
    L.lums(b + 8.75, 15, 0.25)
    L.enemy(b + 13)             # 109 S : au sommet
    L.gap(b + 14, 0.5)          # 110 k
    L.enemy(b + 15)             # 111 S
    L.ramp(b + 16, 2.5, -14)    # 112 : le plongeon, en pente raide (le sol reste à l'image, pas de chute dans le vide)
    L.ramp(b + 18.5, 2.0, -12)  # 114,5 : on replonge aussitôt
    L.enemy(b + 21.5)           # 117,5 s : en bas, une fois la caméra posée
    L.gap(b + 22.5, 0.5)        # 118,5
    L.bounce(b + 24.5, 1.5, +2) # 120,5 K : on rebondit
    L.flyer(b + 25)             # 121 S : au sommet du rebond
    L.enemy(b + 27)             # 123 S
    L.enemy(b + 28)             # 124 S
    L.enemy(b + 30)             # 126 S
    L.enemy(b + 31)             # 127 S


def scene_final_echos(L):
    """128-152 (les Échos) : le monde se retourne. Portail (128,5) : on tombe vers le plafond et on court la tête en
    bas, on saute ses trous (130,5 ; 132,5) et on frappe les ennemis pendus (133 ; 135). Portail retour (136), un coup
    au sol (137), puis un tambour nous envoie dans l'arrière-plan (138) : on court derrière les piliers du premier
    plan (trou 142,5 ; coups 141, 144, 146), et un tambour nous ramène devant (148)."""
    h = L.h
    up = 4.5
    # plafond de 127 à 136,5, troué aux sauts (comme un trou : de beat + 0,2 à beat + 0,7)
    L.ceiling(127, 3.7, up)                            # 127 → 130,7
    L.ceiling(131.2, 1.5, up)                          # 131,2 → 132,7
    L.ceiling(133.2, 3.3, up)                          # 133,2 → 136,5
    L.gravity(128.5, True)
    L.add(130.5, "jump")                               # sauts au plafond (par-dessus les trous)
    L.add(132.5, "jump")
    L.hanging(133, up)          # 133 S
    L.hanging(135, up)          # 135 S
    L.gravity(136, False)       # retour au sol (temps creux)
    L.enemy(137.5)              # 137,5 : à la réception
    # l'arrière-plan : chemin du fond 1 u plus haut, deux piliers du premier plan passent devant le héros
    L.to_plane(138.5, 1.5, "to_back")                  # 138,5 → 140
    L.back(139.5, 3.2, 1.0)                            # 139,5 → 142,7
    L.back(143.2, 5.8, 1.0)                            # 143,2 → 149 (trou de 142,7 à 143,2)
    L.add(142.5, "jump", variant="back")               # 142,5 K
    L.back_enemy(141)           # 141 S
    L.back_enemy(144)           # 144 S
    L.back_enemy(146)           # 146 S
    # deux piliers du premier plan, plus hauts que le héros : il passe derrière (jamais devant un ennemi)
    L.cliff(142.75, 2.5)        # de 143,25 …
    L.cliff(143.4, -2.5)        # … à 143,9
    L.cliff(144.5, 2.5)         # de 145 …
    L.cliff(145.3, -2.5)        # … à 145,8
    L.to_plane(148.5, 1.5, "to_front")                 # 148,5 → 150 : retour devant
    L.h = h
    L.enemy(151)                # 151 S


def scene_fin(L):
    L.enemy(249)
    L.wall(250.5)
    L.enemy(252)
    L.enemy(254)                # le dernier coup


def build():
    L = Level()
    scene_intro(L)
    scene_couplet(L, 32)
    scene_pont_lianes(L, 64)                           # avant : scene_pont (plateformes)
    scene_roulement(L, 80, launch=False)
    scene_refrain_ciel(L, 96)                          # prototype du refrain (avant : scene_refrain + scene_retombee)
    scene_final_echos(L)                               # avant : scene_final (la tour)
    # passage 2 : la musique de [32, 128) revient en [152, 248) : mêmes idées, variées et plus hautes
    scene_couplet(L, 152, variant=True)
    scene_pont(L, 184, variant=True)
    scene_roulement(L, 200, variant=True, launch=False)
    scene_refrain_ciel(L, 216)                         # le refrain 2 rejoue le refrain 1 (avant : ancien refrain + retombée)
    scene_fin(L)
    # l'écran se détraque dès le couplet 2 (≈ 1 min), sur des passages déjà joués, toujours sur un début de phrase
    # (8 temps) : la grille remplit tout l'écran, l'image se retourne (miroir entier, le pixel art reste net)
    L.screen_split(152, 4)      # couplet 2 : quatre écrans (2×2)
    L.screen_split(168, 1)      # un écran…
    L.screen_flip(168, "mirror")    # … en miroir : on court vers la gauche (course sur le mur, notes de saut)
    L.screen_flip(176, "none")
    L.glitch(184, 0.35, 8)      # pont 2 : un grésillement léger…
    L.screen_flip(192, "up")    # … puis la tête en bas
    L.screen_flip(200, "none")
    L.screen_split(216, 4)      # refrain 2, sur le drop : quatre écrans (2×2, ×2 en vue 480 : le héros reste lisible)
    L.screen_split(232, 1)      # un écran…
    L.screen_flip(232, "up")    # … tête en bas pour le grand plongeon (on a l'air de monter)
    L.glitch(232, 0.4, 8)
    L.screen_flip(240, "mirror")    # la fin du refrain en miroir
    L.screen_flip(248, "none")
    # la fin reste nette : c'est là qu'on savoure son score (audit du 25/11)
    for name, beat in SECTIONS:
        if beat > 0:
            L.add(beat, "section_change", intensity=1.2 if "refrain" in name else 0.5)   # drops doux, sauf les refrains
    L.ev.sort(key=lambda e: (e["beat"], e["type"]))
    return L.ev


FICHE = Fiche(
    id="level01_shovel_knight",
    title="Main Theme - Shovel Knight",
    bpm=150.0, offset=0.18, beats=256,
    sections=SECTIONS,
    analysis="level01_analysis.json",
    build=build,
    zoom=480, zoom_wide=640,                 # vitesse : 3,2 u par temps, soit 8 u/s à 150 BPM
    scale=(9, 0, 2, 4, 7),                   # la mineur pentatonique (la, do, ré, mi, sol)
    mechanics=ALL,                           # tout : c'est le cumul (futur niveau 6)
    margin_ms=120.0,
)
