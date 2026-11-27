"""Niveau 9 — La Sourdine, le défi de l'élite (~300 essais), sur « BIKE » de Tangermusic (140 BPM).

Morceau : Newgrounds 1191782, licence CC BY-NC-ND ; utilisé **entier, sans coupe**, avec l'accord de l'artiste (projet
étudiant non commercial, Audio/LICENSES.md). Converti en .ogg tel quel (aucune coupe : la licence l'interdit).

**Grille du jeu : 280 BPM** (le double du morceau, demande de Lou) : un saut dure 0,21 s, la course fait 15 u/s, une
action laisse au plus ~50 ms de marge. **BIKE est en triolets** : ses frappes tombent tous les ⅔ de temps de jeu (croches
en triolet, 0,143 s). Chaque action est posée sur une attaque relevée dans l'audio (tools/data/n9_sourdine_accents.json,
écrit par tools/accents-song.py) ; une grille droite tombait entre les frappes.
  python3 tools/analyse-song.py game/Assets/_Project/Audio/Music/n9_sourdine.ogg tools/data/n9_sourdine_analysis.json \\
      --bpm 280 --offset 0.028 --beats 464
  python3 tools/accents-song.py game/Assets/_Project/Audio/Music/n9_sourdine.ogg tools/data/n9_sourdine_accents.json \\
      --bpm 280 --offset 0.028 --beats 464 --seuil 0.4
Forme (temps de jeu) : intro 0-48, rafale de triolets 48-64, blanc 64-72, drop 72-144, cassure 144-192, reprise 192-240,
roulement 240-272, blanc 272-288 (la Sourdine arrive), second drop 288-416 (combat), dernier coup 416-448, couleur 448-464.

Le niveau (ADR-040) : on part de la Sylve du niveau 1, en couleur et au calme. La musique monte, l'image se déchire, un
tambour nous jette dans le drop : la Sourdine a avalé la couleur. Le monde est son négatif (planches de
tools/gemini-niveau9), il passe du noir au blanc sur les temps forts (`invert`), se retourne, se dédouble. Au second drop,
**la Sourdine en personne** flotte à droite de l'écran et monte et descend sur le rythme (FX/SourdineBoss) : elle descend
avant un **laser bas** (sauter sur le temps) et monte avant un **laser haut** (rester au sol), et crache des **notes
noires** à frapper en vol. Au dernier temps fort, elle fond sur le chemin : le **coup de katana** final la fend en deux
(ennemi de variante « boss »), et la couleur revient.
Inversions : un état sur un temps fort, au plus une bascule par temps du morceau (borne publique).
"""
import json
import os

from levelgen.builder import Level, cuirasses, grotte, lianes, notes
from levelgen.fiche import ALL, Fiche

SECTIONS = [("intro", 0), ("fissure", 48), ("refrain : le silence", 72), ("écho", 144), ("montée", 192),
            ("arrivée", 272), ("refrain : la sourdine", 288), ("le dernier coup", 416), ("la couleur", 448)]
SYLVE, SOURD = "Biome_01_sylve", "Biome_09_sourd"

_HERE = os.path.dirname(os.path.abspath(__file__))
ACCENTS = [(b, s) for b, s in json.load(open(os.path.join(_HERE, "..", "data", "n9_sourdine_accents.json")))]


def acc(a, b, strong=0.0):
    """Les attaques du morceau entre a (inclus) et b (exclu), au-dessus de la force relative `strong`."""
    return [x for x, s in ACCENTS if a <= x < b and s >= strong]


class Placer:
    """Pose les actions sur les attaques, en respectant le jeu : deux sauts à 1,25 temps au moins, pas de frappe pendant un
    saut (ni dans le temps qui suit), deux frappes à ⅔ de temps au moins (un coup = un ennemi). `free` : instants déjà pris
    par un geste composé (lianes, notes, cuirassés, grotte, tambour, laser)."""

    def __init__(self, L):
        self.L = L
        self.jumps = []
        self.hits = []
        self.busy = []                      # (de, à) : rien d'autre ici

    def block(self, a, b):
        self.busy.append((a, b))

    def free(self, t, margin=0.0):
        return all(not (a - margin <= t <= b + margin) for a, b in self.busy)

    def can_jump(self, t):
        return (self.free(t, 0.6) and all(abs(t - j) >= 1.25 for j in self.jumps)
                and all(not (t - 0.6 <= h <= t + 1.05) for h in self.hits))   # aucune frappe pendant ce saut

    def can_hit(self, t):
        return (self.free(t, 0.6) and all(abs(t - h) >= 0.66 for h in self.hits)
                and all(not (j - 0.6 <= t <= j + 1.05) for j in self.jumps))

    def jump(self, t, kind="gap"):
        if kind == "gap":
            self.L.gap(t, 0.5)
        elif kind == "wall":
            self.L.wall(t)
        elif kind == "step+":
            self.L.step(t, +1)
        elif kind == "step-":
            self.L.step(t, -1)
        elif kind == "laser":
            self.L.laser(t)
        self.jumps.append(t)

    def hit(self, t):
        self.L.enemy(t)
        self.hits.append(t)

    def fill(self, a, b, pattern, strong=0.0, kinds=("gap", "wall")):
        """Remplit [a, b) en suivant `pattern` (H = frappe, J = saut) sur les attaques ; une attaque qui ne peut pas
        recevoir l'action du motif reçoit l'autre si elle passe, sinon reste libre."""
        k = 0
        kj = 0
        for t in acc(a, b, strong):
            want = pattern[k % len(pattern)]
            if want == "J" and self.can_jump(t):
                self.jump(t, kinds[kj % len(kinds)])
                kj += 1
                k += 1
            elif want == "H" and self.can_hit(t):
                self.hit(t)
                k += 1
            elif want == "J" and self.can_hit(t):
                self.hit(t)
            elif want == "H" and self.can_jump(t) and pattern.count("H") < len(pattern):
                self.jump(t, kinds[kj % len(kinds)])
                kj += 1
                k += 1


def pulse(a, b, step=2.0):
    """La pulsation du morceau (un temps de BIKE = 2 temps de jeu) entre a et b, hors des attaques déjà relevées (à ⅓
    de temps près) : là où la basse tient ses notes, on garde le rythme sans attaque à suivre."""
    marks = [x for x, _ in ACCENTS]
    t, out = a - (a % step), []
    while t < b:
        if t >= a and all(abs(t - m) > 0.34 for m in marks):
            out.append(t)
        t += step
    return out


def fill_on(P, times, pattern, kinds=("gap", "wall")):
    """Comme Placer.fill, sur des instants donnés (la pulsation)."""
    k = kj = 0
    for t in times:
        want = pattern[k % len(pattern)]
        if want == "J" and P.can_jump(t):
            P.jump(t, kinds[kj % len(kinds)])
            kj += 1
            k += 1
        elif P.can_hit(t):
            P.hit(t)
            k += 1


def first(a, b, strong=0.0):
    """La première attaque de [a, b) (au-dessus de `strong`)."""
    xs = acc(a, b, strong)
    return xs[0] if xs else a


def build():
    L = Level()
    P = Placer(L)

    # ---- intro 0-48 : la Sylve au calme, une action sur les attaques fortes
    P.fill(2, 48, "HHJ", strong=0.8)
    P.block(44, 48)                                      # on souffle avant la rafale (point de reprise)

    # ---- fissure 48-64 : la rafale de triolets, une frappe par triolet, un saut de temps en temps
    P.fill(48, 63.5, "HHHJ", strong=0.55)

    # ---- blanc 64-72 : la musique s'arrête ; le tambour sur l'attaque du blanc nous jette dans le drop
    t = first(68, 70)
    L.bounce(t, 72 - t, -1)
    P.block(64, 74)                                      # on retombe du tambour : rien avant 74 (lisibilité)
    P.jumps.append(t)

    # ---- drop 72-144 : gestes composés sur les attaques fortes, frappes autour
    t = first(80, 82, 0.8)                               # cuirassés : saut sur l'attaque, rebonds d'un temps
    cuirasses(L, t + 1, (1.0, 1.0))
    P.jumps += [t, t + 1, t + 2]
    P.block(t - 0.5, t + 3.5)
    t = first(96, 98, 0.8)                               # lianes : saut sur l'attaque, deux balanciers
    end = lianes(L, t, [2.0, 2.0], air=1.33)
    L.flyer(t + 2.0)
    P.jumps.append(t)
    P.block(t - 0.5, end + 0.8)
    t = first(120, 122, 0.8)                             # notes de saut : une par ⅔ de temps… trop serré ; une par temps
    end = notes(L, t, [0.5, 1.5, 2.5, 1.5], air=1.33)
    P.jumps.append(t)
    P.block(t - 0.5, end + 0.8)
    P.fill(72.5, 144, "HHJH", strong=0.4)              # zone lente durcie (27/11) : les attaques discrètes aussi
    fill_on(P, pulse(72.5, 140), "HH")
    P.block(140, 144)

    # ---- cassure 144-192 : les attaques fortes décalées (x,17 / x,5 / x,83) : saut, frappe, saut ; la basse tient ses
    # notes, peu d'attaques : une chaîne de cuirassés, des notes de saut, et la pulsation entre les attaques (durci le 27/11)
    t = first(150, 153, 0.8)
    if P.can_jump(t) and P.free(t + 1, 0.8) and P.free(t + 3, 0.6):
        cuirasses(L, t + 1, (1.33, 1.33, 1.33))
        P.jumps += [t, t + 1, t + 2.33, t + 3.67]
        P.block(t - 0.5, t + 5.5)
    t = first(170, 173, 0.6)
    if P.can_jump(t) and P.free(t + 6, 0.8):
        end = notes(L, t, [0.5, 1.5, 2.5, 1.5], air=1.33)
        P.jumps.append(t)
        P.block(t - 0.5, end + 0.8)
    P.fill(144, 192, "JHH", strong=0.4, kinds=("gap", "wall", "step+", "step-"))
    fill_on(P, pulse(144, 190), "HJH", kinds=("gap", "wall"))

    # ---- montée 192-240 : la grotte (au plafond, tête en bas), puis des rafales
    g0 = first(200, 202, 0.8)
    g1 = first(216, 218, 0.8)
    jumps_in = [x for x in acc(g0 + 2, g1 - 1.5, 0.8)][::3][:3]
    hang = [x for x in acc(g0 + 1, g1 - 1, 0.55) if all(abs(x - j) >= 1.2 for j in jumps_in)][::2][:5]
    grotte(L, g0, g1, jumps=tuple(jumps_in), hanging=tuple(hang))
    P.block(g0 - 2, g1 + 1.5)
    P.fill(192, 240, "HHHJ", strong=0.4)

    # ---- roulement 240-272 : triolets sur presque tout, le plus d'appuis du niveau
    P.fill(240, 270, "HHHHJ", strong=0.55)

    # ---- blanc 272-288 : la Sourdine arrive par la droite ; deux lasers d'essai
    L.boss(272, True)
    P.block(270, 280)
    P.jump(first(282, 284, 0.6), "laser")
    P.jump(first(285.5, 287.5, 0.4), "laser")

    # ---- combat 288-416 : lasers bas et hauts, notes noires, frappes, cuirassés
    for i, t in enumerate(x for x in acc(288, 412, 0.8)):
        if i % 4 == 0 and P.can_jump(t):
            P.jump(t, "laser")
        elif i % 4 == 2 and P.free(t, 1.0) and all(abs(t - j) >= 1.6 for j in P.jumps) \
                and all(abs(t - h) >= 1.0 for h in P.hits):
            L.laser(t, high=True)                        # rester au sol : le piège de lecture
            P.block(t - 0.7, t + 0.8)
    for t in acc(296, 412, 0.55)[::3]:                   # une note noire toutes les 3 attaques (5 avant le 27/11)
        if P.can_hit(t) and all(abs(t - j) >= 1.3 for j in P.jumps):
            L.note(t)                                    # crachée par la bouche, frappée en vol
            P.hits.append(t)
            P.block(t - 0.6, t + 0.6)
    t = first(352, 354, 0.6)
    if P.can_jump(t - 1) and P.free(t, 0.8) and P.free(t + 1, 0.8) and P.free(t + 2, 0.5):
        cuirasses(L, t, (1.0, 1.0))
        P.jumps += [t - 1, t, t + 1]
        P.block(t - 1.5, t + 2.6)
    P.fill(288, 412, "HHHJ", strong=0.4)
    fill_on(P, pulse(288, 410), "HH")

    # ---- dernier coup 416-448 : rafale, sauts serrés, le coup final sur la dernière attaque forte
    P.fill(412, 441, "HHHHJ", strong=0.4)
    # le dernier coup : la Sourdine fond sur le chemin (rien d'autre à l'écran), le katana la fend en deux
    last = [x for x in acc(443, 448, 0.6) if P.can_hit(x)]
    final = last[-1] if last else 446.0
    L.add(final, "enemy", variant="boss")
    P.hits.append(final)
    L.boss(final + 6, False)                             # elle sort de scène après (déjà fendue)

    # ---- couleur 448-464 : on court, la Sylve respire
    P.fill(450, 462, "H", strong=0.8)

    effects(L)
    for name, beat in SECTIONS:
        if beat > 0:
            L.add(beat, "section_change", intensity=1.2 if "refrain" in name else 0.5)
    L.ev.sort(key=lambda e: (e["beat"], e["type"]))
    return L.ev


def effects(L):
    """La Sourdine prend la couleur, puis l'image. Entrée dans le monde sourd (48-72) : l'image grésille de plus en plus
    sur la rafale, tremble sur chaque triolet, se fige en quatre copies dans le blanc, puis flash, secousse et négatif sur
    le drop. Ensuite : inversions sur les temps forts (au plus une par temps du morceau), miroir, tête en bas, dédoublé."""
    for k, b in enumerate((48, 52, 56, 60)):
        L.glitch(b, 0.2 + 0.15 * k, 4)
    for t in acc(56, 64, 0.8):
        L.add(t, "camera_shake", intensity=0.4 + 0.08 * (t - 56), length=0.3)
    L.screen_split(64, 4)                               # le blanc : l'image se fige en quatre copies…
    L.glitch(66, 0.9, 2)
    L.screen_split(68, 1)                               # … qui se recollent sur le tambour
    L.add(72, "fx_flash", intensity=1.0)                # le drop : flash, secousse, le monde passe en négatif
    L.add(72, "camera_shake", intensity=1.0, length=1.0)
    L.add(72, "camera_zoom", intensity=1.0, length=0.5)
    L.invert(72, True)
    L.invert(80, False)
    for b in (88, 104, 120, 136):
        L.invert(b, True)
        L.invert(b + 8, False)
    L.screen_flip(96, "mirror")
    L.screen_flip(112, "none")
    for b in (160, 164, 168, 172):                      # la cassure : le monde cligne, un temps du morceau sur deux
        L.invert(b, True)
        L.invert(b + 2, False)
    L.screen_split(176, 4)
    L.screen_split(184, 1)
    L.screen_flip(208, "up")                            # la grotte, tête en bas
    L.screen_flip(224, "none")
    L.screen_split(256, 4)
    L.screen_split(264, 1)
    L.glitch(264, 0.8, 8)
    L.add(272, "fx_flash", intensity=0.6)               # la Sourdine arrive
    L.add(272, "camera_shake", intensity=0.6, length=2.0)
    L.invert(288, True)
    L.invert(304, False)
    L.screen_flip(304, "mirror")
    L.invert(320, True)
    L.screen_flip(320, "none")
    L.invert(336, False)
    L.screen_flip(352, "up")
    L.invert(352, True)
    L.invert(368, False)
    L.screen_flip(368, "none")
    L.glitch(368, 0.5, 8)
    L.screen_flip(384, "both")
    L.invert(384, True)
    L.screen_flip(400, "none")
    L.invert(400, False)
    for b in (408, 410, 412, 414):                      # avant le dernier passage : une bascule par temps du morceau
        L.invert(b, b % 4 == 0)
    L.invert(416, True)
    L.glitch(416, 0.7, 28)
    L.invert(432, False)
    L.screen_flip(432, "mirror")
    L.screen_flip(440, "none")
    L.add(446, "fx_flash", intensity=0.8)               # le dernier coup : la couleur revient
    L.invert(448, False)


FICHE = Fiche(
    id="n9_sourdine",
    title="La Sourdine",
    bpm=280.0, offset=0.028, beats=464,                 # le double du morceau (140), triolets à ⅔ de temps ; premier temps mesuré sur les attaques (0,022 au début, 0,030 au pic)
    sections=SECTIONS,
    analysis="n9_sourdine_analysis.json",
    build=build,
    zoom=640, zoom_wide=640,                            # vue large fixe : à 15 u/s, la vue 480 montrait les ennemis trop tard
    scale=(6, 9, 11, 1, 4),                             # fa# mineur pentatonique (tonique estimée : fa#)
    mechanics=ALL,
    margin_ms=45.0,                                     # à 280 BPM, ~50 ms est le plus qu'une action peut laisser (mesuré)
    checkpoint_every=32,                                # un point de reprise toutes les 4 mesures du morceau : pas de répit
    biomes={"intro": SYLVE, "fissure": SYLVE, "la couleur": SYLVE},   # le reste : le monde sourd (biome du niveau)
)
