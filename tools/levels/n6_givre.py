"""Niveau 6 — Le Givre, sur *Boss Battle #2 [8-bit] V2* de nene (OpenGameArt, CC0, accéléré à 168 BPM + boost basses).

Calage audio vérifié au milliseconde (`offset = 0.021 s`, écart médian +4,8 ms sur les attaques K/S) :
- **0-16 (appel/réponse batterie)** : frappes uniquement sur les vraies réponses de batterie (0, 4-7, 8, silence 9-11,
  roulement 12-15), aucune action dans les silences !
- **Actions posées sur les vraies attaques relevées** (`tools/data/n6_givre_accents.json`), y compris les relances en
  triolets (+1/3, +2/3) et croches de la batterie boss-battle ;
- **Difficulté relevée (~1,9 action/s, marge ≥ 100 ms)** : vrai pont nerveux entre L'Échine (150 BPM) et L'Orage (180 BPM),
  avec reflets soutenus (`mirror`, `up`, `both` et écran divisé `/4` `screen_split(4)`), sans `glitch` (gardé pour N7).
"""
import json
import os

from levelgen.builder import Level, cuirasses, grotte, lianes, notes
from levelgen.fiche import ALL, Fiche

SECTIONS = [
    ("intro", 0), ("couplet", 16), ("pont de glace", 48), ("refrain", 80), ("caverne", 112),
    ("reprise", 144), ("éclats", 176), ("refrain 2", 208), ("avalanche", 240), ("fin", 272),
]

_HERE = os.path.dirname(os.path.abspath(__file__))
ACCENTS = [(b, s) for b, s in json.load(open(os.path.join(_HERE, "..", "data", "n6_givre_accents.json")))]


def acc(a, b, strong=0.45):
    return [x for x, s in ACCENTS if a <= x < b and s >= strong]


def near(target, tol=0.20, strong=0.40):
    """L'attaque réelle la plus proche de `target` (dans ±tol), sinon `target`."""
    cand = [x for x, s in ACCENTS if abs(x - target) <= tol and s >= strong]
    return min(cand, key=lambda x: abs(x - target)) if cand else target


class Placer168:
    """Pose les sauts et frappes sur les vraies attaques de Boss Battle #2 à 168 BPM (marge ≥ 100 ms = 0,28 beat)."""

    def __init__(self, L):
        self.L = L
        self.jumps = []
        self.hits = []
        self.busy = []

    def block(self, a, b):
        self.busy.append((a, b))

    def free(self, t, m=0.0):
        return all(not (a - m <= t <= b + m) for a, b in self.busy)

    def can_jump(self, t):
        return (self.free(t, 0.35) and all(abs(t - j) >= 1.30 for j in self.jumps)
                and all(not (t - 0.50 <= h <= t + 1.02) for h in self.hits))

    def can_hit(self, t):
        return (self.free(t, 0.30) and all(abs(t - h) >= 0.48 for h in self.hits)
                and all(not (j - 0.50 <= t <= j + 1.02) for j in self.jumps))

    def jump(self, t, kind="gap"):
        if kind == "gap":
            self.L.gap(t, 0.5)
        elif kind == "wall":
            self.L.wall(t)
        self.jumps.append(t)

    def hit(self, t):
        self.L.enemy(t)
        self.hits.append(t)

    def fill(self, a, b, pattern, strong=0.48, kinds=("gap", "wall")):
        k = kj = 0
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


def pulse168(a, b, step=0.5):
    """Croches et temps forts réguliers (K-S-K-S) là où la basse continue couvre le pic d'attaque."""
    marks = [x for x, s in ACCENTS if s >= 0.28]
    t = a - (a % step)
    out = []
    while t < b:
        if t >= a and all(abs(t - m) > 0.18 for m in marks):
            out.append(t)
        t += step
    return out


def fill_pulse(P, times, pattern, kinds=("gap", "wall"), max_actions=208):
    k = kj = 0
    for t in times:
        if len(P.jumps) + len(P.hits) >= max_actions:
            break
        want = pattern[k % len(pattern)]
        if want == "J" and P.can_jump(t):
            P.jump(t, kinds[kj % len(kinds)])
            kj += 1
            k += 1
        elif P.can_hit(t):
            P.hit(t)
            k += 1


def build():
    L = Level()
    P = Placer168(L)

    # ---- 0-16 : Intro fidèle à l'appel/réponse de la batterie (silences 1-3 et 9-11 respectés !)
    P.hit(near(4.0))
    P.hit(near(5.0))
    P.jump(near(6.0), "gap")
    P.hit(near(8.0))                                     # impact sec de la mesure 3, puis 3 temps de silence pur !
    P.hit(near(12.0))
    P.hit(near(13.0))
    L.bounce(14, 2.0, +4)                                # envol sur le roulement 14-15 → arrivée au couplet à 16 !
    P.jumps.append(14)
    P.block(0, 16.4)

    # ---- 16-48 : Couplet 1 — groove K-S nerveux, cuirassés, paroi de glace (+5 u), notes et glissade
    cuirasses(L, 25, (1.0, 1.5), rise=1.0)               # 24 saut (kick), 25 (snare), 26 → 27.5
    P.jumps += [24, 25, 26]
    P.block(23.2, 28.5)
    L.ramp(32, 2.0, +5)                                  # montée sur le fortissimo de la mesure 32 (+10 u)
    P.block(31.4, 34.4)
    end = notes(L, 38, [0.5, 1.5], rise=1.0)             # → 41 (+11 u)
    P.jumps.append(38)
    P.block(37.2, end + 1.5)
    L.ramp(44, 1.8, -5)                                  # glissade avant le pont (+6 u)
    P.block(43.4, 46.2)
    P.fill(16.5, 48, "HHJH", strong=0.28)
    fill_pulse(P, pulse168(17, 48, 0.5), "HHJH")

    # ---- 48-80 : Pont de glace — plateformes en miroir (48-64) puis prisme 2×2 `/4` soutenu (64-80)
    L.void(48.4, 13.2)
    h0 = L.h
    for i, r in enumerate((1, 2, 3, 2, 1, 2, 1)):
        L.add(48 + 2 * i, "platform", length=2.0, height=h0 + r)
        P.jumps.append(48 + 2 * i)
        if i in (0, 1):
            L.flyer(48 + 2 * i + 1)
            P.hits.append(48 + 2 * i + 1)
    L.h = h0
    P.block(47.2, 62.5)
    cuirasses(L, 73, (1.0, 1.5))                         # cuirassés en plein écran `/4` !
    P.jumps += [72, 73, 74]
    P.block(71.2, 76.2)
    P.fill(62.5, 78.6, "HHJH", strong=0.28)
    fill_pulse(P, pulse168(63, 78.6, 0.5), "HHJH")

    # ---- 80-112 : Refrain 1 (plan large, 96-112 tête en bas !) — tambours jusqu'au sommet (+22 u), plongeon et fond
    L.bounce(80, 2.0, +4)
    P.jumps.append(80)
    P.block(79.2, 82.6)
    L.ramp(84, 2.0, +4)                                  # +14 u
    P.block(83.4, 86.4)
    for k in (88, 89, 90, 91):
        L.bounce(k, 1.0, +2.0, lums=False)               # chaîne de 4 tambours sur les kicks 88..91 (+22 u !)
        P.jumps.append(k)
    L.lums(88.25, 15, 0.25)
    P.block(87.2, 92.6)
    L.ramp(96, 2.0, -9)                                  # grand plongeon tête en bas !
    L.ramp(98, 2.0, -7)                                  # → +6 u
    P.block(95.4, 100.5)
    h = L.h
    L.to_plane(101, 1.5, "to_back")
    L.back(102, 3.35, 1.0)
    L.back(105.75, 4.35, 1.0)
    for bh in (103.0, 104.0, 106.75, 108.0):
        L.back_enemy(bh)
        P.hits.append(bh)
    L.add(105, "jump", variant="back")
    L.cliff(105.2, 2.0)
    L.cliff(105.9, -2.0)
    L.to_plane(109, 1.5, "to_front")
    L.h = h
    P.jumps += [101, 105, 109]
    P.block(100.4, 111.2)
    P.fill(82.6, 112, "HHHJ", strong=0.28)
    fill_pulse(P, pulse168(83, 112, 0.5), "HHHJ")

    # ---- 112-144 : Caverne de glace (gravité inversée 114-124), puis `/4` + miroir (128-144)
    grotte(L, 114, 124, jumps=(116, 120), hanging=(117.333, 118.5, 121.5, 122.667))
    P.jumps += [116, 120]
    P.hits += [117.333, 118.5, 121.5, 122.667]
    P.block(112.5, 125.6)
    P.block(125.8, 128.0)                                # palier de reprise franc avant le `/4` !
    cuirasses(L, 137, (1.0, 1.5))
    P.jumps += [136, 137, 138]
    P.block(135.2, 140.2)
    P.fill(128.0, 144, "HHJH", strong=0.28)
    fill_pulse(P, pulse168(128, 144, 0.5), "HHJH")

    # ---- 144-176 : Reprise nerveuse (160-176 en demi-tour `both` !)
    cuirasses(L, 153, (1.0, 1.5), rise=2.0)              # → +8 u
    P.jumps += [152, 153, 154]
    P.block(151.2, 156.4)
    L.ramp(160, 2.0, +6)                                 # → +14 u
    P.block(159.4, 162.4)
    end = notes(L, 166, [0.5, 1.5, 2.5], rise=1.0)       # → 170 (+15 u)
    P.jumps.append(166)
    P.block(165.2, end + 1.6)
    L.ramp(172, 2.0, -5)                                 # → +10 u
    P.block(171.4, 174.4)
    P.fill(144, 176, "HHHHJ", strong=0.26)
    fill_pulse(P, pulse168(144, 176, 0.5), "HHHHJ")

    # ---- 176-208 : Éclats — lianes, notes et `/4` + tête en bas (192-208)
    end = lianes(L, 176, [1.25, 1.25])                   # → 181.25
    L.flyer(177.25)
    P.jumps.append(176)
    P.hits.append(177.25)
    P.block(175.2, end + 1.4)
    end = notes(L, 184, [0.5, 1.5, 2.5], rise=2.0)       # → 188 (+12 u)
    P.jumps.append(184)
    P.block(183.2, end + 1.6)
    P.block(190.2, 192.0)                                # checkpoint avant `/4` + tête en bas
    cuirasses(L, 201, (1.0, 1.5))
    P.jumps += [200, 201, 202]
    P.block(199.2, 204.2)
    P.fill(182.6, 206.6, "HHHHJ", strong=0.26)
    fill_pulse(P, pulse168(192, 206.6, 0.5), "HHHHJ")

    # ---- 208-240 : Refrain 2 (224-240 en miroir, sommet à +28 u puis grand plongeon)
    L.bounce(208, 2.0, +4)                               # +16 u
    P.jumps.append(208)
    P.block(207.2, 210.6)
    L.ramp(212, 2.0, +4)                                 # +20 u
    P.block(211.4, 214.4)
    for k in (216, 217, 218, 219):
        L.bounce(k, 1.0, +2.0, lums=False)               # +28 u !
        P.jumps.append(k)
    L.lums(216.25, 15, 0.25)
    P.block(215.2, 220.6)
    L.ramp(224, 2.0, -10)
    L.ramp(226, 2.0, -8)                                 # → +10 u
    P.block(223.4, 228.5)
    end = lianes(L, 230, [1.25, 1.0])                    # → 235
    L.flyer(231.25)
    P.jumps.append(230)
    P.hits.append(231.25)
    P.block(229.2, end + 1.3)
    P.fill(210.6, 240, "HHHJ", strong=0.26)
    fill_pulse(P, pulse168(211, 240, 0.5), "HHHJ")

    # ---- 240-272 : Avalanche finale — grotte rapide, puis sprint `/4` + demi-tour (256-272) !
    grotte(L, 242, 250, jumps=(244, 247.5), hanging=(245.5, 246.5, 249.0))
    P.jumps += [244, 247.5]
    P.hits += [245.5, 246.5, 249.0]
    P.block(240.5, 251.6)
    P.block(253.5, 256.0)                                # checkpoint avant le sprint final `/4`
    L.ramp(260, 2.0, -6)                                 # → +4 u
    P.block(259.4, 262.4)
    cuirasses(L, 266, (1.5,), rise=-4.0)                 # retour à 0 u
    P.jumps += [265, 266]
    P.block(264.2, 268.2)
    P.fill(251.6, 272, "HHHHJ", strong=0.25)
    fill_pulse(P, pulse168(252, 272, 0.5), "HHHHJ")

    # ---- 272-284 : Fin sur les derniers accords
    P.fill(272, 281, "HHJ", strong=0.48)

    effects(L)
    for name, beat in SECTIONS:
        if beat > 0:
            L.add(beat, "section_change", intensity=1.2 if "refrain" in name else 0.5)
    L.ev.sort(key=lambda e: (e["beat"], e["type"]))
    return L.ev


def effects(L):
    """Reflets soutenus du Givre : miroir, renversé, demi-tour et prisme `/4` combinés sur les phrases musicales."""
    # Pont de glace : miroir sur les plateformes, puis `/4`
    L.screen_flip(48, "mirror")
    L.screen_flip(64, "none")
    L.screen_split(64, 4)
    L.screen_split(80, 1)

    # Refrain 1 : seconde moitié tête en bas sur le grand plongeon et l'arrière-plan
    L.screen_flip(96, "up")
    L.screen_flip(112, "none")

    # Caverne : sortie en `/4` + miroir
    L.screen_split(128, 4)
    L.screen_flip(128, "mirror")
    L.screen_split(144, 1)
    L.screen_flip(144, "none")

    # Reprise : demi-tour complet sur la seconde moitié
    L.screen_flip(160, "both")
    L.screen_flip(176, "none")

    # Éclats : `/4` + tête en bas
    L.screen_split(192, 4)
    L.screen_flip(192, "up")
    L.screen_split(208, 1)
    L.screen_flip(208, "none")

    # Refrain 2 : miroir sur la dévalée et les lianes
    L.screen_flip(224, "mirror")
    L.screen_flip(240, "none")

    # Avalanche finale : `/4` + miroir jusqu'à la fin !
    L.screen_split(256, 4)
    L.screen_flip(256, "mirror")
    L.screen_split(272, 1)
    L.screen_flip(272, "none")

    for base in (80, 208):
        for k in (0, 8, 9, 10, 11, 16):
            L.add(base + k, "camera_zoom", intensity=0.75, length=0.25)


FICHE = Fiche(
    id="n6_givre",
    title="Boss Battle #2 - nene",
    bpm=168.0, offset=0.021, beats=284,
    sections=SECTIONS,
    analysis="n6_givre_analysis.json",
    build=build,
    zoom=480, zoom_wide=640, wide_on_refrains=True,
    scale=(0, 3, 5, 7, 10),                     # do mineur pentatonique
    mechanics=ALL - {"laser", "speed_change"},
    margin_ms=100.0,
)
