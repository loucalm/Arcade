#!/usr/bin/env python3
"""Génère la chart du niveau 01 « vertical » : un parcours façon niveau musical, écrit à la main scène par scène.

Le morceau (« Main Theme », 150 BPM, 256 beats) est découpé en scènes calées sur sa structure. Chaque scène a
une idée de mise en scène : escalier qu'on grimpe sur les temps, tambour qui projette dans le ciel au drop,
plongeon dans un puits, plateformes au-dessus du vide, dévalée d'une pente, tour à gravir sur le refrain final.
Les temps des actions reprennent les attaques fortes de la musique (mesurées par l'ancien générateur, librosa).

Les hauteurs sont relatives (on monte de +1, on plonge de −8…) : le passage 2 rejoue le passage 1 à partir de la
hauteur où l'on est. Un simulateur reproduit la physique du jeu (PlayerController : saut 1,5 u en 1 beat,
8 u/s, tambours, pentes, mort sous KillY) et joue l'autoplay image par image : la chart n'est écrite que si
l'autoplay finit le niveau sans mourir.

Usage : python3 tools/gen-level-vertical.py [sortie.json] [--table]
Format des événements : docs/chart-format.md.
"""
import argparse
import json
import math
import os
import sys

BPM, OFFSET, NB = 150.0, 0.18, 256
SPB = 60.0 / BPM
RUN_SPEED = 8.0
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DEFAULT_OUT = os.path.join(ROOT, "game", "Assets", "_Project", "Charts", "level01_shovel_knight.json")

# Physique (identique à PlayerController / LevelBuilder)
JUMP_HEIGHT, JUMP_BEATS = 1.5, 1.0
GRAVITY = 8 * JUMP_HEIGHT / (JUMP_BEATS * SPB) ** 2
JUMP_V = 4 * JUMP_HEIGHT / (JUMP_BEATS * SPB)
STEP_FACE_START = 0.5
STEP_START, GAP_START, WALL_START, ENEMY_CENTER = 0.3, 0.2, 0.4, 0.6
AIR_HIT_BELOW, AIR_HIT_ABOVE = 3.5, 3.5             # zone de frappe en l'air (au sol : de −0,5 à +1,6)
STOMP_TOUCH = 0.15                           # on touche un cuirassé 0,15 beat avant son temps (fin d'un saut)
ORB_HOVER = 3.0                              # chute ralentie (u/s) près d'une note, le temps d'appuyer
ORB_REACH = 3.0                              # note de saut : portée verticale (on la prend même en tombant vite)
PRESS_EARLY = PRESS_LATE = 0.35              # appui d'une note de saut ou d'un grand rebond : fenêtre autour du temps
VINE_EARLY = VINE_LATE = 0.35                # lâcher d'une liane : fenêtre autour de son temps (PlayerController)
JUMP_BUFFER = 0.35                           # appui en l'air juste avant d'atterrir : le saut part à l'atterrissage (140 ms)
LEDGE_ASSIST = 0.4                           # pieds à moins de 0,4 u sous le haut d'une marche ou d'un mur : on passe dessus
STEP_FACE = 0.25
MAX_RISE = 1.0
LUM_LANE_Y = (0.45, 1.9, 2.4)
PICKUP = 0.7

# Changements de monde en cours de niveau (nom de l'asset du biome) ; les autres sections gardent le biome du niveau.
BIOMES = {"refrain": "Biome_08_orage", "final": "Biome_03_echos", "refrain 2": "Biome_08_orage"}

SECTIONS = [
    ("intro", 0), ("couplet", 32), ("pont", 64), ("roulement", 80), ("refrain", 96), ("final", 128),
    ("couplet 2", 152), ("pont 2", 184), ("roulement 2", 200), ("refrain 2", 216), ("fin", 248),
]


def X(beat):
    return beat * SPB * RUN_SPEED


class Level:
    """Construit les événements ; suit la hauteur du sol (relative) au fil de l'écriture."""

    def __init__(self):
        self.ev = []
        self.h = 0.0

    def add(self, beat, t, lane=0, **params):
        e = {"beat": round(beat, 3), "type": t, "lane": lane}
        if params:
            e["params"] = {k: (round(v, 3) if isinstance(v, float) else v) for k, v in params.items()}
        self.ev.append(e)
        return e

    # --- sol ---
    def step(self, beat, dh):
        """Marche : montée (à sauter sur `beat`, ≤ +1) ou descente (on tombe)."""
        self.h += dh
        self.add(beat, "step", height=self.h)

    def cliff(self, beat, dh):
        """Falaise atteinte en vol (tambour) : pas de saut attendu."""
        self.h += dh
        self.add(beat, "step", lane=1, height=self.h)

    def ramp(self, beat, length, dh):
        """Rampe automatique (raide = course sur le mur)."""
        self.h += dh
        self.add(beat, "ramp", length=length, height=self.h)

    def bounce(self, beat, air, dh, lums=True):
        """Tambour au beat : envol de `air` beats, on retombe `dh` plus haut (ou plus bas)."""
        h0 = self.h
        self.add(beat, "bounce", length=air)
        if dh > 0:
            self.cliff(beat + air - 1.0, dh)
        elif dh < 0:
            self.cliff(beat + 1.0, dh)
        if lums:
            self.lums(beat + 0.25, int(air * 4) - 1, 0.25)   # l'arc de l'envolée

    # --- obstacles ---
    def gap(self, beat, length=0.6):
        self.add(beat, "gap", length=length)

    def void(self, beat, length):
        self.add(beat, "gap", lane=1, length=length)

    def platform(self, beat, length, dh_from_ground):
        self.add(beat, "platform", length=length, height=self.h + dh_from_ground)

    def wall(self, beat):
        self.add(beat, "wall")

    def enemy(self, beat):
        self.add(beat, "enemy")


    def lums(self, beat, count, step=0.5, lane=0):
        """Ligne de pièces posées sur la trajectoire de l'autoplay (calculée par le simulateur, voir place_lums)."""
        for i in range(count):
            self.add(beat + i * step, "lum_trail", lane=i)

    def flyer(self, beat):
        """Ennemi en vol à frapper sur `beat` : sa hauteur est posée sur la trajectoire simulée (resolve)."""
        self.add(beat, "enemy_air", lane=1)

    # --- mécaniques du « niveau qui change » (lianes, gravité, arrière-plan, effets d'écran)
    def vine(self, beat, length, grab, release, sag=1.6, air=1.0):
        """Liane prise au beat (pieds à self.h + grab), balancier de `length` beats, lâchée à self.h + release, puis
        `air` beats de vol vers la liane suivante (si elle commence là) ou vers le sol. Le lâcher est jugé."""
        self.add(beat, "vine", length=length, height=self.h + grab, releaseHeight=self.h + release, sag=sag, step=air)
        self.lums(beat + 0.25, int(length * 4) - 1, 0.25)

    def ceiling(self, beat, length, above):
        """Plafond (dessous à self.h + above) de beat à beat + length : on y court en gravité inversée."""
        self.add(beat, "ceiling", length=length, height=self.h + above)

    def gravity(self, beat, up):
        """Portail : on tombe vers le plafond (up) ou on revient au sol."""
        self.add(beat, "gravity", lane=1 if up else 0)

    def hanging(self, beat, above):
        """Ennemi pendu au plafond (dessous à self.h + above), à frapper en courant au plafond."""
        self.add(beat, "enemy", lane=2, height=self.h + above)

    def back(self, beat, length, dh):
        """Chemin de l'arrière-plan à self.h + dh, de beat à beat + length."""
        self.add(beat, "back", length=length, height=self.h + dh)

    def back_enemy(self, beat):
        self.add(beat, "enemy", variant="back")

    def to_plane(self, beat, air, where):
        """Tambour qui change de plan : « to_back » (posé devant) ou « to_front » (posé au fond)."""
        self.add(beat, "bounce", length=air, variant=where)
        self.lums(beat + 0.25, int(air * 4) - 1, 0.25)

    def stomp(self, beat, air=2.0):
        """Ennemi cuirassé : on lui saute dessus (on le touche juste avant `beat`), on appuie sur `beat` pour le grand
        rebond de `air` beats (vers le cuirassé suivant, une note, ou le sol)."""
        self.add(beat, "enemy", variant="stomp", length=air)

    def orb(self, beat, dy, air=1.0):
        """Note de saut au beat, centre à self.h + dy : en l'air près d'elle, on appuie sur `beat` pour relancer le saut
        pendant `air` beats (vers la note suivante, un cuirassé ou le sol)."""
        self.add(beat, "orb", height=self.h + dy, step=air)

    def screen_split(self, beat, copies):
        self.add(beat, "screen_split", copies=copies)

    def screen_flip(self, beat, mode):
        """L'image se retourne : « mirror » (on court vers la gauche), « up » (tête en bas), « both », « none »."""
        self.add(beat, "screen_flip", variant=mode)

    def glitch(self, beat, intensity, length):
        self.add(beat, "glitch", intensity=intensity, length=length)


# ---------------------------------------------------------------- les scènes
# Principes : docs/level-design-musical.md. Les temps viennent de la carte du morceau (tools/data/level01_analysis.json,
# voir `python3 tools/gen-level-vertical.py --map`) : grosse caisse → sauts, caisse claire → coups de poing.
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
    L.screen_split(216, 9)      # refrain 2, sur le drop : neuf écrans (3×3), en petit
    L.screen_split(232, 1)      # un écran…
    L.screen_flip(232, "up")    # … tête en bas pour le grand plongeon (on a l'air de monter)
    L.glitch(232, 0.4, 8)
    L.screen_flip(240, "mirror")    # la fin du refrain en miroir
    L.screen_flip(248, "none")
    L.glitch(248, 0.5, 6)       # la fin : un dernier grésillement
    for name, beat in SECTIONS:
        if beat > 0:
            L.add(beat, "section_change", intensity=1.2 if "refrain" in name else 0.5)   # drops doux, sauf les refrains
    L.ev.sort(key=lambda e: (e["beat"], e["type"]))
    return L.ev


# ---------------------------------------------------------------- simulateur (miroir de LevelBuilder + PlayerController)

class Span:
    def __init__(self, x0, x1, t0, t1, one_way=False):
        self.x0, self.x1, self.t0, self.t1, self.one_way = x0, x1, t0, t1, one_way

    def top(self, x):
        if abs(self.t1 - self.t0) < 1e-4:
            return self.t0
        u = min(1.0, max(0.0, (x - self.x0) / (self.x1 - self.x0)))
        return self.t0 + (self.t1 - self.t0) * u

    @property
    def sloped(self):
        return abs(self.t1 - self.t0) > 1e-4


class World:
    def __init__(self, events):
        self.events = events
        self.knots = []          # (x, h, linear)
        self.faces = []          # (x, h0, h1, beat)
        self.jumps, self.hits = [], []
        h0 = 0.0
        for e in events:
            p = e.get("params", {})
            if e["type"] == "step":
                h = p["height"]
                x = X(e["beat"] + STEP_FACE_START)
                self.knots.append((x, h, False))
                if h - h0 > STEP_FACE and e["lane"] != 1:
                    self.faces.append((x, h0, h, e["beat"]))
                    self.jumps.append(e["beat"])
                h0 = h
            elif e["type"] == "ramp":
                h = p["height"]
                self.knots.append((X(e["beat"]), h0, True))
                self.knots.append((X(e["beat"] + p["length"]), h, False))
                h0 = h
        # chemin de l'arrière-plan (plan 1) et plafond (dessous réel ; en gravité inversée on calcule en miroir)
        self.back = sorted((Span(X(e["beat"]), X(e["beat"] + e["params"].get("length", 1.0)), e["params"]["height"],
                                 e["params"]["height"]) for e in events if e["type"] == "back"), key=lambda s_: s_.x0)
        self.ceil = sorted((Span(X(e["beat"]), X(e["beat"] + e["params"].get("length", 1.0)), -e["params"]["height"],
                                 -e["params"]["height"]) for e in events if e["type"] == "ceiling"), key=lambda s_: s_.x0)
        self.flips = sorted((e["beat"], e["lane"] == 1) for e in events if e["type"] == "gravity")
        self.gaps, self.walls, self.enemies, self.platforms, self.bounces = [], [], [], [], []
        self.orbs = []           # [x, y du centre, beat, vol, prise, plan]
        self.vines = []          # [x_prise, x_lâcher, beat_prise, beat_lâcher, y_prise, y_lâcher, creux, vol, prise_faite]
        for e in events:
            p, b, t = e.get("params", {}), e["beat"], e["type"]
            plane = plane_of(e)
            if t == "gap":
                x0 = X(b + GAP_START)
                self.gaps.append((x0, x0 + X(p.get("length", 0.6))))
                if e["lane"] != 1:
                    self.jumps.append(b)
            elif t == "wall":
                x0 = X(b + WALL_START)
                self.walls.append((x0, self.height(x0 + 0.45), b))
                self.jumps.append(b)
            elif t in ("enemy", "block"):
                stomp = p.get("variant") == "stomp"
                cx = X(b - STOMP_TOUCH) if stomp else X(b + ENEMY_CENTER)
                if e["lane"] == 1:
                    ey = p["height"]
                elif e["lane"] == 2:
                    ey = p["height"] - 0.9          # pendu au plafond
                else:
                    ey = self.height_on(cx, plane)
                self.enemies.append([cx, ey, b, True, plane, stomp, p.get("length", 2.0)])
                (self.jumps if stomp else self.hits).append(b)      # cuirassé : l'appui du grand rebond
            elif t == "orb":
                self.orbs.append([X(b), p["height"], b, p.get("step", 1.0), False, plane])
                self.jumps.append(b)
            elif t == "platform":
                x0, x1 = X(b + STEP_START), X(b + STEP_START + p.get("length", 1.0))
                self.platforms.append(Span(x0, x1, p["height"], p["height"], True))
                self.jumps.append(b)
            elif t == "bounce":
                cx = X(b)
                v = p.get("variant", "")
                target = 1 if v == "to_back" else 0 if v == "to_front" else plane
                self.bounces.append([cx, self.height_on(cx, plane), p.get("length", 2.0), b, False, plane, target])
                self.jumps.append(b)
            elif t == "jump":
                self.jumps.append(b)                     # saut sans obstacle au sol (trou du plafond, du chemin du fond)
            elif t == "vine":
                ln = p.get("length", 2.0)
                self.vines.append([X(b), X(b + ln), b, b + ln, p["height"], p["releaseHeight"], p.get("sag", 0.0),
                                   p.get("step", 1.0), False])
                self.jumps.append(b + ln)                # le lâcher est jugé (appui de saut)
        self.vines.sort()
        self.jumps.sort()
        self.hits.sort()
        self.spans = self.main_spans() + self.platforms
        self.spans.sort(key=lambda s_: s_.x0)
        # pièces (miroir de LevelBuilder : lum_line au-dessus du sol ou d'une plateforme, lum_at à hauteur absolue)
        self.lums = []
        for e in events:
            p, b = e.get("params", {}), e["beat"]
            if e["type"] == "lum_line":
                for i in range(p.get("count", 4)):
                    lb = b + i * p.get("step", 0.5)
                    x = X(lb)
                    base = self.height(x)
                    for pl in self.platforms:
                        if pl.x0 <= x <= pl.x1:
                            base = max(base, pl.t0)
                    self.lums.append([x, LUM_LANE_Y[min(e["lane"], 2)] + base, lb, True, 0])
            elif e["type"] == "lum_at":
                self.lums.append([X(b), p["height"], b, True, plane_of(e)])

    def height(self, x):
        h = 0.0
        for i, (kx, kh, lin) in enumerate(self.knots):
            if kx > x:
                break
            h = kh
            if lin and i + 1 < len(self.knots) and self.knots[i + 1][0] > x:
                nx, nh, _ = self.knots[i + 1]
                return kh + (nh - kh) * (x - kx) / (nx - kx)
        return h

    def back_height(self, x):
        best, dist = 0.0, math.inf
        for s_ in self.back:
            d = s_.x0 - x if x < s_.x0 else x - s_.x1 if x > s_.x1 else 0.0
            if d < dist:
                best, dist = s_.t0, d
        return best

    def height_on(self, x, plane):
        return self.back_height(x) if plane == 1 else self.height(x)

    def terrain(self, plane, inv):
        return self.back if plane == 1 else self.ceil if inv else self.spans

    def main_spans(self):
        bounds, frm, h, lin = [], -40.0, 0.0, False
        end = X(NB + 20)
        for kx, kh, klin in self.knots:
            if kx > frm:
                bounds.append((frm, kx, h, kh if lin else h))
            frm, h, lin = max(frm, kx), kh, klin
        bounds.append((frm, end, h, h))
        out = []
        for x0, x1, h0, h1 in bounds:
            cur = x0
            for g0, g1 in sorted(self.gaps):
                if g1 <= cur or g0 >= x1:
                    continue
                if g0 > cur:
                    out.append(self.cut(x0, x1, h0, h1, cur, g0))
                cur = max(cur, g1)
            if x1 > cur:
                out.append(self.cut(x0, x1, h0, h1, cur, x1))
        return out

    @staticmethod
    def cut(x0, x1, h0, h1, a, b):
        f = lambda x: h0 + (h1 - h0) * (x - x0) / (x1 - x0) if x1 > x0 else h0
        return Span(a, b, f(a), f(b))

    # miroir de LevelTerrain (spans : le sol, le plafond en miroir ou le chemin du fond)
    @staticmethod
    def overl(s, x, hw):
        return x + hw > s.x0 and x - hw < s.x1

    def support(self, x, hw, y, spans):
        best = -math.inf
        for i, s in enumerate(spans):
            if not self.overl(s, x, hw):
                continue
            t = s.top(x)
            joint = any(o.sloped and self.overl(o, x, hw + 0.3) for o in spans[max(0, i - 1):i + 2])
            tol = 0.45 if (s.sloped or joint) else 0.02
            if abs(y - t) <= tol and t > best:
                best = t
        return best

    def land(self, x, hw, yp, yn, spans):
        if yn > yp:
            return None
        best = None
        for s in spans:
            if not self.overl(s, x, hw):
                continue
            t = s.top(x)
            tol = 0.45 if s.sloped else 0.02
            if yp >= t - tol and yn <= t and (best is None or t > best):
                best = t
        return best

    def hits_side(self, x, hw, y, spans):
        return any(not s.one_way and self.overl(s, x, hw) and y < s.top(x) - 0.4 for s in spans)

    @staticmethod
    def kill_y(x, spans):
        left = right = None
        under = math.inf
        for s in spans:
            if s.one_way:
                continue
            if s.x1 <= x:
                left = s.t1
            elif s.x0 >= x:
                right = s.t0 if right is None else right
            else:
                under = min(under, s.top(x))
        if under < math.inf:
            return under - 2.5
        c = [v for v in (left, right) if v is not None]
        lowest = min((s.t0 for s in spans), default=0.0)
        return (min(c) if c else lowest) - 2.5


def plane_of(e):
    v = e.get("params", {}).get("variant", "")
    return 1 if v in ("back", "to_front") else 0


def simulate(events, fps=60, track=None, late=0.0, stop=None):
    """Autoplay image par image. Renvoie (None, stats) si le niveau est fini, sinon (message, stats).
    track : liste où noter (beat, centre du corps, gravité inversée, plan) à chaque image. late : retard (beats,
    négatif = avance) de chaque appui, pour vérifier qu'un humain a une marge ; un nombre (tous les appuis) ou une
    fonction beat de l'action → retard. stop : beat où l'on arrête (survie jusque-là = réussite).
    En gravité inversée, y et vy sont dans le repère retourné (y' = −y monde), comme dans PlayerController."""
    w = World(events)
    offset = late if callable(late) else (lambda _b, d=late: d)
    end = NB - 1 if stop is None else min(NB - 1, stop)
    dt = 1.0 / fps
    hw, stand = 0.25, 1.0
    y, vy, grounded = w.height(0), 0.0, True
    inv, plane = False, 0
    fi = 0
    vine = None
    last_press = -99
    pending = None               # cuirassé écrasé : en attente de l'appui du grand rebond

    def arrival(arrive, x_land):
        """Miroir de PlayerController.ArrivalTarget : (pieds visés, temps de contact)."""
        for o in w.orbs:
            if not o[4] and abs(o[2] - arrive) < 0.05:
                return o[1] - 0.5, arrive
        for en in w.enemies:
            if en[5] and en[3] and abs(en[2] - arrive) < 0.05:
                return en[1] + 0.9, arrive - STOMP_TOUCH
        for v in w.vines:
            if not v[8] and abs(v[2] - arrive) < 0.05:
                return v[4], arrive
        return w.height_on(x_land, plane), arrive

    def launch(arrive, beat_, x_, y_):
        target, touch = arrival(arrive, x_ + (arrive - beat_) * SPB * RUN_SPEED)
        ta = max(0.3, touch - beat_) * SPB
        return (target - y_ + 0.5 * GRAVITY * ta * ta) / ta
    ji = hi = 0
    jump_buf = -99
    last_ground = 0.0
    hit_until = -99
    prev_x = 0.0
    prev_cy = y + 0.5
    top_y = bot_y = y
    t = 0.0

    def ov(a, b):
        return a[0] < b[2] and b[0] < a[2] and a[1] < b[3] and b[1] < a[3]

    while True:
        beat = t / SPB
        if beat > end:
            missed = [round(l[2], 2) for l in w.lums if l[3]]
            return None, {"max_y": top_y, "min_y": bot_y, "lums": len(w.lums), "missed": missed}
        x = X(beat)
        jump = False
        while ji < len(w.jumps) and w.jumps[ji] + offset(w.jumps[ji]) <= beat:
            jump, ji = True, ji + 1
        while hi < len(w.hits) and w.hits[hi] + offset(w.hits[hi]) <= beat:
            hit_until, hi = beat + 0.3, hi + 1
        # portails de gravité : même position et même vitesse dans le monde
        while fi < len(w.flips) and w.flips[fi][0] <= beat:
            up = w.flips[fi][1]
            fi += 1
            if up != inv:
                y, vy, inv, grounded, jump_buf, vine = -y, -vy, up, False, -99, None
        spans = w.terrain(plane, inv)
        if grounded:
            s = w.support(x, hw, y, spans)
            if s == -math.inf:
                grounded, vy = False, 0.0
            else:
                y = s
        if grounded:
            last_ground = beat
        if jump:
            last_press = beat
        if jump and vine is None:
            jump_buf = beat
        coyote = not grounded and vy <= 0 and beat - last_ground <= 0.12
        if beat - jump_buf <= JUMP_BUFFER and (grounded or coyote):
            vy, grounded, jump_buf = JUMP_V, False, -99
        # cuirassé écrasé : l'appui (en avance ou juste après) donne le grand rebond
        if pending is not None:
            if last_press >= pending[2] - PRESS_EARLY:
                vy, grounded, jump_buf = launch(pending[2] + pending[6], beat, x, y), False, -99
                pending = None
            elif beat > pending[2] + PRESS_LATE:
                pending = None
        # notes de saut : en l'air près d'une note, l'appui relance le saut
        if not inv:
            for o in w.orbs:
                if o[4] or o[5] != plane or abs(o[0] - x) > 1.2 or abs(o[1] - (y + 0.5)) > ORB_REACH:
                    continue
                if beat < o[2] - PRESS_EARLY or beat > o[2] + PRESS_LATE:
                    continue
                if last_press < o[2] - PRESS_EARLY:
                    vy = max(vy, -ORB_HOVER)                 # la note retient le héros pendant sa fenêtre
                    continue
                o[4] = True
                vy, grounded, jump_buf = launch(o[2] + o[3], beat, x, y), False, -99
                break
        # lianes : prise en passant, balancier sur un arc, lâcher vers la liane suivante ou le sol
        if vine is None and not inv:
            for v in w.vines:
                if not v[8] and v[0] - 0.05 <= x <= v[0] + 0.8 and abs(y - v[4]) <= 1.6:
                    v[8], vine, grounded, jump_buf = True, v, False, -99
                    break
        swinging = False
        if vine is not None:
            # miroir de PlayerController.Swing : on lâche en sautant (de −VINE_EARLY à +VINE_LATE autour du temps)
            xg, xr, bg, br, yg, yr, sag, air, _ = vine
            u = (beat - bg) / (br - bg)
            arc_y = yg + (yr - yg) * u - sag * math.sin(math.pi * u)
            arc_vy = ((yr - yg) - sag * math.pi * math.cos(math.pi * u)) / ((br - bg) * SPB)
            pressed = last_press >= br - VINE_EARLY and last_press >= bg
            if not pressed and beat <= br + VINE_LATE:
                y, vy, grounded, swinging = arc_y, arc_vy, False, True
            elif not pressed:
                y, vy, jump_buf, vine = arc_y, min(arc_vy, 0.0), -99, None      # la main glisse : on tombe
            else:
                arrive = br + air
                ta = max(0.3, arrive - beat) * SPB
                target = w.height_on(x + ta * RUN_SPEED, plane)
                for nv in w.vines:
                    if nv is not vine and abs(nv[2] - arrive) < 0.05:
                        target = nv[4]
                        break
                y = arc_y
                vy = (target - y + 0.5 * GRAVITY * ta * ta) / ta
                jump_buf, vine = -99, None
        frame_start_y = y
        if not grounded and not swinging:
            yp = y
            y += vy * dt - 0.5 * GRAVITY * dt * dt
            vy -= GRAVITY * dt
            top = w.land(x, hw, yp, y, spans) if vy <= 0 else None
            if top is not None:
                y, vy, grounded = top, 0.0, True
            elif w.hits_side(x, hw, y, spans):
                return f"cogne le bord d'un trou au beat {beat:.2f} (x {x:.1f}, y {y:.2f})", {}
        if not grounded and not swinging and y < w.kill_y(x, spans):
            return f"tombe dans le vide au beat {beat:.2f} (x {x:.1f}, y {y:.2f})", {}
        # tambours (de plan : on retombe sur l'autre chemin)
        for pad in w.bounces:
            cx, top, air, pb, used, pplane, target = pad
            if used or inv or pplane != plane or abs(cx - x) > 0.6 or x < cx - 0.15 or y > top + 0.35 + 1.2 or y < top - 0.4:
                continue
            pad[4] = True
            plane = target
            ta = air * SPB
            land_top = w.height_on(x + ta * RUN_SPEED, plane)
            vy = (land_top - y + 0.5 * GRAVITY * ta * ta) / ta
            grounded, jump_buf = False, -99
        # collisions, dans le monde
        h = stand
        wy = -y if inv else y
        x_from = max(prev_x, x - 3)

        def body_at(feet):
            return (x_from - hw, feet - h, x + hw, feet) if inv else (x_from - hw, feet, x + hw, feet + h)

        body = body_at(wy)
        hitting = beat <= hit_until
        # frappe : 1,9 u devant ; en l'air la zone s'étire (on frappe ce qu'on croise en montant ou en tombant)
        a_, b_ = (-0.5, 1.6) if grounded else (-AIR_HIT_BELOW, AIR_HIT_ABOVE)
        hit_box = (x_from, wy - b_, x + 1.9, wy - a_) if inv else (x_from, wy + a_, x + 1.9, wy + b_)
        if plane == 0 and not inv:
            for fx_, h0, h1, fb in w.faces:
                if ov(body, (fx_, h0, fx_ + 0.05, h1)):
                    if y < h1 - LEDGE_ASSIST:
                        return f"se cogne dans une marche (beat {fb}) au beat {beat:.2f}", {}
                    y, vy = h1, max(vy, 0.0)             # coup de pouce : on se hisse sur la marche
                    wy = y
                    body = body_at(wy)
            for wx, wyy, wb in w.walls:
                if ov(body, (wx + 0.1, wyy, wx + 0.8, wyy + 0.75)):
                    if y < wyy + 0.75 - LEDGE_ASSIST:
                        return f"se cogne dans le mur du beat {wb} au beat {beat:.2f}", {}
                    y, vy = wyy + 0.75, max(vy, 0.0)     # coup de pouce : on passe par-dessus la souche
                    wy = y
                    body = body_at(wy)
        for en in w.enemies:
            cx, ey, eb, alive, eplane, stomp, sair = en
            if not alive or eplane != plane:
                continue
            r = (cx - 0.4, ey, cx + 0.4, ey + 0.9)
            if stomp and not inv and vy <= 0 and frame_start_y >= ey + 0.9 - 0.45 and y <= ey + 0.9 + 0.8 \
                    and abs(x - cx) <= 1.15:
                # écrasé : petit rebond d'un demi-temps ; l'appui déjà fait donne tout de suite le grand rebond
                en[3] = False
                y = max(y, ey + 0.9)
                vy, grounded = 0.5 * GRAVITY * 0.5 * SPB, False
                pending = en
                if last_press >= eb - PRESS_EARLY:
                    vy, jump_buf, pending = launch(eb + sair, beat, x, y), -99, None
                wy = y
                body = body_at(wy)
                continue
            if hitting and not stomp and ov(hit_box, r):
                en[3] = False
            elif ov(body, r):
                return f"touché par l'ennemi du beat {eb} au beat {beat:.2f} (y {wy:.2f}, ennemi {ey:.2f})", {}
        cy = wy - h * 0.5 if inv else wy + h * 0.5
        if track is not None:
            track.append((beat, cy, inv, plane))
        for lum in w.lums:
            if lum[3] and lum[4] == plane and abs(lum[0] - x) < 1.0:
                # balayage depuis l'image précédente (comme CollectLums)
                ax, ay, bx, by = prev_x, prev_cy, x, cy
                dx_, dy_ = bx - ax, by - ay
                L2 = dx_ * dx_ + dy_ * dy_
                u = 0.0 if L2 < 1e-9 else max(0.0, min(1.0, ((lum[0] - ax) * dx_ + (lum[1] - ay) * dy_) / L2))
                if (ax + dx_ * u - lum[0]) ** 2 + (ay + dy_ * u - lum[1]) ** 2 <= PICKUP * PICKUP:
                    lum[3] = False
        prev_cy = cy
        top_y, bot_y = max(top_y, wy), min(bot_y, wy)
        prev_x = x
        t += dt


def action_windows(events, lo=0.0, hi=NB, limit=0.5, fps=60):
    """Marge de survie de chaque action (saut ou frappe) entre les beats lo et hi : de combien de beats on peut
    appuyer en avance (early) ou en retard (late) sans mourir, les autres appuis restant justes. Renvoie
    [(beat, genre, early, late)], marges en beats (0,25 beat = 100 ms à 150 BPM), plafonnées à `limit`."""
    w = World(events)
    acts = sorted({(b, "saut") for b in w.jumps} | {(b, "frappe") for b in w.hits})
    out = []
    for b, kind in acts:
        if not lo <= b < hi:
            continue
        res = []
        for sign in (-1, 1):
            ok, ko = 0.0, limit + 0.01
            if simulate(events, fps, late=lambda a, d=sign * limit, b=b: d if a == b else 0.0, stop=b + 4)[0] is None:
                res.append(limit)
                continue
            for _ in range(6):
                mid = (ok + ko) / 2
                err, _ = simulate(events, fps, late=lambda a, d=sign * mid, b=b: d if a == b else 0.0, stop=b + 4)
                ok, ko = (mid, ko) if err is None else (ok, mid)
            res.append(ok)
        out.append((b, kind, res[0], res[1]))
    return out


MIN_MARGIN = 0.3                             # 120 ms : un humain peut appuyer un peu tôt ou un peu tard sans mourir
# passages déjà passés à cette règle
WIDE_MARGIN = [(0, 256)]                     # tout le niveau


def check_margins(events, ranges, margin=MIN_MARGIN, fps=60):
    """Vérifie que chaque action des passages `ranges` survit seule à un appui `margin` beats en avance et en retard
    (2 simulations par action ; --marges donne le détail). Renvoie la liste des problèmes."""
    w = World(events)
    acts = sorted({(b, "saut") for b in w.jumps} | {(b, "frappe") for b in w.hits})
    problems = []
    for b, kind in acts:
        if not any(lo <= b < hi for lo, hi in ranges):
            continue
        for d in (-margin, margin):
            err, _ = simulate(events, fps, late=lambda a, d=d, b=b: d if a == b else 0.0, stop=b + 4)
            if err:
                side = "d'avance" if d < 0 else "de retard"
                problems.append(f"{kind} du beat {b} : moins de {margin * SPB * 1000:.0f} ms {side} ({err})")
    return problems


ANALYSIS = os.path.join(ROOT, "tools", "data", "level01_analysis.json")
ZOOM = 640                                   # vue de 16 × 9 u : une mesure d'avance devant le héros
VIEW_H = ZOOM * 9 / 16 / 40.0
FRAME_Y = 0.32                               # sol sous le héros à 32 % du bas
PENTA = (9, 0, 2, 4, 7)                      # la mineur pentatonique (la, do, ré, mi, sol)
LUM_BASE_MIDI = 81                           # le son de pièce est un la (880 Hz)


def load_analysis():
    with open(ANALYSIS, encoding="utf-8") as f:
        return json.load(f)


def note_for(beat, song):
    """Note de la pièce : la mélodie du morceau à ce moment, ramenée dans la pentatonique, autour du la."""
    mel = song["melody"]
    k = max(0, min(len(mel) - 1, int(round(beat * 4))))
    m = None
    for d in range(0, 8):                    # note la plus proche (en arrière, puis en avant)
        for kk in (k - d, k + d):
            if 0 <= kk < len(mel) and mel[kk] is not None:
                m = mel[kk]
                break
        if m is not None:
            break
    if m is None:
        return 0
    best = min(range(m - 6, m + 7), key=lambda c: (c % 12 not in PENTA, abs(c - m)))
    s_ = best - LUM_BASE_MIDI
    while s_ > 10:
        s_ -= 12
    while s_ < -9:
        s_ += 12
    return s_


def track_at(track, beats, beat):
    """Centre du corps (monde) au beat donné, interpolé sur la trajectoire simulée."""
    import bisect
    i = min(max(1, bisect.bisect_left(beats, beat)), len(track) - 1)
    b0, y0 = track[i - 1][0], track[i - 1][1]
    b1, y1 = track[i][0], track[i][1]
    return y0 + (y1 - y0) * ((beat - b0) / (b1 - b0) if b1 > b0 else 0.0)


def track_state(track, beats, beat):
    """(gravité inversée, plan) du héros au beat donné."""
    import bisect
    i = min(max(0, bisect.bisect_left(beats, beat)), len(track) - 1)
    return track[i][2], track[i][3]


def feet_at(track, beats, beat):
    """Pieds du héros dans le monde (au-dessus du corps en gravité inversée)."""
    inv, _ = track_state(track, beats, beat)
    return track_at(track, beats, beat) + (0.5 if inv else -0.5)


def auto_lums(events, world, song):
    """Pièces automatiques : un arc au-dessus de chaque saut, et des lignes de mélodie dans les espaces libres
    (sur les croches où la mélodie ou le charleston attaque)."""
    taken = [e["beat"] for e in events if e["type"] == "lum_trail"]
    air = [e["beat"] for e in events if e["type"] == "enemy_air"]
    busy = []                                 # (début, fin) : moments occupés par une action
    for j in world.jumps:
        busy.append((j - 0.75, j + 1.0))
    for h in world.hits:
        busy.append((h - 0.75, h + 0.75))
    for pad in world.bounces:
        busy.append((pad[3] - 0.5, pad[3] + pad[2] + 0.5))
    for v in world.vines:
        busy.append((v[2] - 0.5, v[3] + v[7] + 0.5))
    out = []

    def free(b):
        return all(abs(b - t) > 0.2 for t in taken) and all(abs(b - t) > 0.35 for t in air)

    for j in world.jumps:
        if any(abs(j - pad[3]) < 1e-6 for pad in world.bounces) or any(abs(j - v[3]) < 1e-6 for v in world.vines):
            continue                          # les envolées et les lianes ont déjà leur arc
        for d in (0.25, 0.5, 0.75):
            if free(j + d):
                out.append({"beat": j + d, "type": "lum_trail", "lane": 0})
                taken.append(j + d)
    k = 10
    while k < (NB - 4) * 2:
        b = k / 2
        k += 1
        if b < 5 or any(lo <= b <= hi for lo, hi in busy) or not free(b):
            continue
        i = int(b * 4)
        if max(song["mid"][i], song["hi"][i]) < 0.3:
            continue
        out.append({"beat": b, "type": "lum_trail", "lane": 0})
        taken.append(b)
    return out


SAFE_BOTTOM, SAFE_TOP = 0.12, 0.2               # marges de CameraRig.safeMargins (le haut : sous le HUD)
FLYER_BELOW, FLYER_ABOVE = 0.6, 2.4               # chauve-souris par rapport au centre du héros, à l'appui
ENEMY_LEAD = 1.5                                # un ennemi est dans l'image au moins 1,5 temps (600 ms) avant son coup


def enemy_windows(world):
    """Pour chaque ennemi : (beat du coup, bas, haut) de sa boîte dans le monde."""
    return [(en[2], en[1], en[1] + 0.9) for en in world.enemies]


def camera_rail(track, step=0.25, enemies=()):
    """Rail de la caméra : trajectoire des pieds lissée dans les deux sens (elle anticipe montées et chutes),
    contrainte à garder le héros dans l'image (marges de CameraRig) et à montrer chaque ennemi au moins ENEMY_LEAD
    temps avant son coup, avec le cadrage par défaut (sol à FRAME_Y). Le héros passe avant les ennemis."""
    beats = [tr[0] for tr in track]
    n = NB * 4
    feet = [feet_at(track, beats, i * step) for i in range(n)]
    inv = [track_state(track, beats, i * step)[0] for i in range(n)]
    # corps : des pieds vers la tête (vers le bas en gravité inversée)
    top = [f if v else f + 1.0 for f, v in zip(feet, inv)]
    bottom = [f - 1.0 if v else f for f, v in zip(feet, inv)]
    lo = [t_ + 0.4 - VIEW_H * (0.5 - SAFE_TOP) for t_ in top]       # le haut du corps sous la marge du haut (HUD)
    hi = [b_ + VIEW_H * (0.5 - SAFE_BOTTOM) for b_ in bottom]        # le bas du corps au-dessus de la marge du bas
    # ennemis : visibles (sous le HUD, au-dessus du bas) pendant les ENEMY_LEAD temps avant leur coup
    elo = [-math.inf] * n
    ehi = [math.inf] * n
    for eb, y0, y1 in enemies:
        for i in range(max(0, int((eb - ENEMY_LEAD) / step)), min(n, int(eb / step) + 1)):
            elo[i] = max(elo[i], y1 + 0.05 - VIEW_H * (0.5 - SAFE_TOP))     # 0,05 u de plus que la vérification
            ehi[i] = min(ehi[i], y0 - 0.35 + VIEW_H * 0.5)
    # le sol sous le héros à FRAME_Y du bas ; au plafond, le plafond à FRAME_Y du haut
    target = [f - VIEW_H * (0.5 - FRAME_Y) if v else f + VIEW_H * (0.5 - FRAME_Y) for f, v in zip(feet, inv)]

    def smooth(v, sigma):
        r = int(sigma * 3)
        w = [math.exp(-0.5 * (d / sigma) ** 2) for d in range(-r, r + 1)]
        out = []
        for i in range(len(v)):
            acc = tot = 0.0
            for d in range(-r, r + 1):
                j = min(len(v) - 1, max(0, i + d))
                acc += w[d + r] * v[j]
                tot += w[d + r]
            out.append(acc / tot)
        return out

    def clamp(c):
        c = [min(max(x, a), b) if a <= b else x for x, a, b in zip(c, elo, ehi)]   # ennemis (si possible)
        return [min(max(x, a), b) for x, a, b in zip(c, lo, hi)]                    # héros (toujours)

    c = smooth(target, 4.0)                            # σ = 1 beat : la caméra colle au héros, avec un peu d'avance
    for _ in range(12):
        c = smooth(clamp(c), 3.0)
    c = clamp(c)
    return [round(x, 2) for x in c]


def enemy_leads(rail, world, step=0.25):
    """Pour chaque ennemi : depuis combien de temps (beats) il est entièrement dans l'image au moment du coup."""
    view_w = ZOOM / 40.0
    out = []
    for cx, ey, eb, *_ in world.enemies:
        first, b = None, eb
        while b > eb - 4:
            f = max(0.0, b / step)
            i = min(int(f), len(rail) - 2)
            c = rail[i] + (rail[i + 1] - rail[i]) * (f - i)
            x0 = X(b) - view_w * 0.22
            if not (cx - 0.4 >= x0 and cx + 0.4 <= x0 + view_w and ey >= c - VIEW_H / 2 + 0.3
                    and ey + 0.9 <= c + VIEW_H * (0.5 - SAFE_TOP)):
                break
            first, b = b, b - 0.05
        out.append((eb, 0.0 if first is None else eb - first))
    return out


def resolve(events, song):
    """Pose ce qui dépend de la trajectoire : pièces (et leurs notes), ennemis en vol, rail de caméra."""
    base = [e for e in events if e["type"] not in ("lum_trail", "enemy_air")]
    track = []
    err, _ = simulate(base, 120, track)
    if err:
        return events, None, err
    beats = [tr[0] for tr in track]
    world = World(base)
    events = events + auto_lums(events, world, song)
    out = []
    for e in events:
        if e["type"] == "lum_trail":
            y = track_at(track, beats, e["beat"])
            params = {"height": round(y, 3), "hasNote": True, "note": note_for(e["beat"], song)}
            if track_state(track, beats, e["beat"])[1] == 1:
                params["variant"] = "back"               # pièce de l'arrière-plan : on ne la prend que là-bas
            out.append({"beat": round(e["beat"], 3), "type": "lum_at", "lane": 0, "params": params})
        elif e["type"] == "enemy_air":
            y = feet_at(track, beats, e["beat"] + ENEMY_CENTER)           # au niveau des pieds quand on l'atteint
            out.append({"beat": e["beat"], "type": "enemy", "lane": 1, "params": {"height": round(y + 0.1, 3)}})
        else:
            out.append(e)
    out.sort(key=lambda e: (e["beat"], e["type"]))
    return out, camera_rail(track, enemies=enemy_windows(World(out))), None


def lint(events):
    """Règles de faisabilité simples (en plus du simulateur)."""
    problems = []
    w = World(events)
    jumps = w.jumps
    for a, c in zip(jumps, jumps[1:]):
        if c - a < 1.0 - 1e-9:
            problems.append(f"sauts trop proches : {a} et {c}")
    for x, h0, h1, b in w.faces:
        if h1 - h0 > MAX_RISE + 1e-9:
            problems.append(f"marche de {h1 - h0:.2f} u au beat {b} (max {MAX_RISE})")
    for e in events:
        if e["type"] in ("enemy", "block") and e["lane"] == 0:   # en vol ou au plafond, on le frappe justement en l'air
            for j in jumps:
                if j < e["beat"] < j + 1.0 - 1e-9:
                    problems.append(f"ennemi au beat {e['beat']} pendant le saut du beat {j}")
    return problems


def table(events):
    sym = {"gap": "G", "wall": "W", "enemy": "E", "step": "^", "ramp": "/", "platform": "P",
           "bounce": "T", "camera": "c", "vine": "V", "gravity": "g", "back": "B", "ceiling": "C",
           "screen_split": "#", "glitch": "~", "orb": "o", "screen_flip": "%"}
    rows = {}
    for e in events:
        if e["type"] not in sym:
            continue
        bar, pos = divmod(e["beat"], 4)
        r = rows.setdefault(int(bar), [list("    ") for _ in range(4)])
        r[int(pos)][int((pos - int(pos)) * 4) % 4] = sym[e["type"]]
    return "\n".join(f"mesure {b:2d} (beat {b * 4:3d}) | " + " | ".join("".join(c) for c in rows.get(b, [list("    ")] * 4))
                     for b in range(NB // 4))


def write(events, rail, out):
    head = {"id": "level01_shovel_knight", "title": "Main Theme - Shovel Knight", "bpm": BPM,
            "offsetMs": int(OFFSET * 1000), "beatsPerBar": 4, "runSpeed": RUN_SPEED, "zoom": ZOOM, "railStep": 0.25}
    sections = [dict({"name": n, "startBeat": b}, **({"biome": BIOMES[n]} if n in BIOMES else {})) for n, b in SECTIONS]
    s = json.dumps(head, ensure_ascii=False, indent=2)[:-2]
    s += ',\n  "sections": [\n' + ",\n".join("    " + json.dumps(x, ensure_ascii=False) for x in sections) + "\n  ],\n"
    s += '  "rail": ' + json.dumps(rail) + ",\n"
    s += '  "events": [\n' + ",\n".join("    " + json.dumps(x, ensure_ascii=False) for x in events) + "\n  ]\n}\n"
    with open(out, "w", encoding="utf-8") as f:
        f.write(s)


def song_map(song):
    """Carte du morceau, une ligne par mesure : X = grosse caisse + caisse claire, K/k = grosse caisse, S/s = caisse claire."""
    rows = []
    for bar in range(NB // 4):
        cells = ""
        for k in range(bar * 16, (bar + 1) * 16, 2):
            lo, mi = song["lo"][k], song["mid"][k]
            cells += ("X" if lo > 0.55 and mi > 0.55 else "K" if lo > 0.55 else "S" if mi > 0.55
                      else "k" if lo > 0.35 else "s" if mi > 0.35 else ".")
            if k % 4 == 2:
                cells += " "
        rows.append(f"beat {bar * 4:3d} | {cells}")
    return "\n".join(rows)


def generate():
    """Construit, résout et vérifie le niveau. Renvoie (events, rail, stats, problèmes)."""
    song = load_analysis()
    events, rail, err = resolve(build(), song)
    if err:
        return events, rail, {}, [f"autoplay : {err}"]
    problems = lint(events)
    stats = {}
    for fps in (60, 30):
        err, st = simulate(events, fps)
        if err:
            problems.append(f"autoplay à {fps} i/s : {err}")
        elif fps == 60:
            stats = st
    # tolérance humaine : appuis 40 ms en avance ou en retard partout
    for late in (-0.1, 0.1):
        err, _ = simulate(events, 60, late=late)
        if err:
            problems.append(f"appuis décalés de {late * SPB * 1000:+.0f} ms : {err}")
    # passages réécrits avec la nouvelle règle : chaque action laisse au moins 120 ms de marge, seule, des deux côtés
    problems += check_margins(events, WIDE_MARGIN)
    # lisibilité : au moment de l'appui, une chauve-souris est à hauteur du héros ou au-dessus (jamais en bas du saut)
    track = []
    simulate(events, 120, track)
    beats = [tr[0] for tr in track]
    for e in events:
        if e["type"] == "enemy" and e["lane"] == 1:
            inv, _ = track_state(track, beats, e["beat"])
            hero_c = track_at(track, beats, e["beat"])
            bat_c = e["params"]["height"] + 0.45
            if bat_c < hero_c - FLYER_BELOW or bat_c > hero_c + FLYER_ABOVE:
                problems.append(f"chauve-souris du beat {e['beat']} à {bat_c - hero_c:+.1f} u du héros au moment de la frapper "
                                f"(de −{FLYER_BELOW} à +{FLYER_ABOVE} u)")
    # lisibilité : chaque ennemi apparaît assez tôt pour qu'on ait le temps de le voir venir
    for eb, lead in enemy_leads(rail, World(events)):
        if lead < 1.0 - 1e-6:
            problems.append(f"ennemi du beat {eb} visible seulement {lead * SPB * 1000:.0f} ms avant le coup (min 400 ms)")
    return events, rail, stats, problems


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("out", nargs="?", default=DEFAULT_OUT)
    ap.add_argument("--table", action="store_true", help="afficher le niveau mesure par mesure")
    ap.add_argument("--map", action="store_true", help="afficher la carte des attaques du morceau")
    ap.add_argument("--marges", nargs=2, type=float, metavar=("DE", "A"),
                    help="marge de survie de chaque action entre deux beats (en ms)")
    args = ap.parse_args()
    if args.map:
        print(song_map(load_analysis()))
        return
    if args.marges:
        with open(args.out, encoding="utf-8") as f:
            evs = json.load(f)["events"]
        for b, kind, early, late in action_windows(evs, *args.marges):
            ms = lambda v: f"{v * SPB * 1000:4.0f}"
            flag = "  <-- serré" if min(early, late) < 0.3 - 1e-6 else ""
            print(f"beat {b:6.2f} {kind:6s} avance {ms(early)} ms, retard {ms(late)} ms{flag}")
        return
    events, rail, stats, problems = generate()
    if args.table:
        print(table(events))
    if problems:
        print("Chart non jouable :", *problems, sep="\n  - ")
        sys.exit(1)
    write(events, rail, args.out)
    n = {t: sum(1 for e in events if e["type"] == t) for t in sorted({e["type"] for e in events})}
    print(f"{len(events)} événements : {n}")
    print(f"dénivelé : de {stats['min_y']:.1f} à {stats['max_y']:.1f} u")
    print(f"pièces ramassées par l'autoplay : {stats['lums'] - len(stats['missed'])}/{stats['lums']}")
    if stats["missed"]:
        print("  ratées aux beats :", stats["missed"])


if __name__ == "__main__":
    main()
