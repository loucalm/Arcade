"""Niveau 8 — La Cité Sourde, sur *Atama no Taisou* (hapi⇒, montage MikuMikuMikuMiku : 120 BPM chill → 240 BPM hardcore).

Le piège psychologique avant La Sourdine :
- **0-64 (16 s, `speed = 0.5` = 120 BPM, 6,4 u/s)** : promenade innocente et ensoleillée au son du piano et de la voix
  calme de Miku. Quelques sauts et coups très simples.
- **64-80 (4 s, « ichi, ni, san, shi ! »)** : l'image se fissure, un point de reprise est posé juste avant l'impact.
- **80-400 (`speed = 1.0` = 240 BPM, 12,8 u/s, marge ≥ 65 ms)** : **ACCÉLÉRATION DOUBLÉE** — le niveau explose en un
  défi top-level ultra nerveux et très vertical (ascensions de clochers à +36 u, plongeons vertigineux, grotte,
  arrière-plan, reflets 2×2 / miroir / tête en bas et premières inversions négatives annonçant La Sourdine).
"""
import json
import os

from levelgen.builder import Level, cuirasses, grotte, lianes, notes
from levelgen.fiche import ALL, Fiche

SECTIONS = [
    ("balade", 0), ("1, 2, 3, 4 !", 64), ("le drop", 80), ("couplet furieux", 128),
    ("miku-miku", 160), ("refrain : la cité", 192), ("fausse accalmie", 256),
    ("hyper refrain", 272), ("sprint final", 336), ("fin", 400),
]

_HERE = os.path.dirname(os.path.abspath(__file__))
ACCENTS = [(b, s) for b, s in json.load(open(os.path.join(_HERE, "..", "data", "n8_cite_accents.json")))]


def acc(a, b, strong=0.55):
    return [x for x, s in ACCENTS if a <= x < b and s >= strong]


class Placer240:
    """Pose les actions sur les vraies attaques à 240 BPM (marge ≥ 65 ms = 0,26 beat)."""

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
        return (self.free(t, 0.35) and all(abs(t - j) >= 1.35 for j in self.jumps)
                and all(not (t - 0.6 <= h <= t + 1.05) for h in self.hits))

    def can_hit(self, t):
        return (self.free(t, 0.30) and all(abs(t - h) >= 0.48 for h in self.hits)
                and all(not (j - 0.55 <= t <= j + 1.02) for j in self.jumps))

    def jump(self, t, kind="gap"):
        if kind == "gap":
            self.L.gap(t, 0.5)
        elif kind == "wall":
            self.L.wall(t)
        elif kind == "step+":
            self.L.step(t, +1)
        elif kind == "step-":
            self.L.step(t, -1)
        self.jumps.append(t)

    def hit(self, t):
        self.L.enemy(t)
        self.hits.append(t)

    def fill(self, a, b, pattern, strong=0.55, kinds=("gap", "wall")):
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


def pulse240(a, b, step=0.5, mark_min=0.35):
    """Pulsation gabber / syllabes Miku en croches à 240 BPM sur les temps non déjà pris par une attaque franche."""
    marks = [x for x, s in ACCENTS if s >= mark_min]
    t = a - (a % step)
    out = []
    while t < b:
        if t >= a and all(abs(t - m) > 0.22 for m in marks):
            out.append(t)
        t += step
    return out


def fill_pulse(P, times, pattern, kinds=("gap", "wall"), max_actions=261):
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
    P = Placer240(L)

    # ---- 0-64 : la balade super chill à demi-vitesse (120 BPM ressenti, 6,4 u/s)
    # 1 temps de 120 BPM = 2 beats de la chart : un saut dure 2 beats, tout suit le chant doux de Miku.
    L.speed(0, 0.5)
    L.gap(8, 1.0)
    L.enemy(12)
    L.enemy(16)
    L.wall(20)
    L.enemy(24)
    L.enemy(28)
    L.step(32, +1)
    L.enemy(36)
    L.gap(40, 1.0)
    L.enemy(44)
    L.wall(48)
    L.enemy(52)
    L.enemy(56)
    L.step(60, -1)
    P.block(0, 82)

    # ---- 64-80 : « ta... ta... ichi, ni, san, shi ! » — sol nu jusqu'à 80 (checkpoint posé juste avant le drop !)
    # ---- 80 : LE DROP À 240 BPM (vitesse doublée à 12,8 u/s juste avant le tambour d'envol !)
    L.speed(78, 1.0)
    L.bounce(80, 2.0, +6)
    P.jumps.append(80)
    P.block(77, 83.5)

    # ---- 80-128 : gabber drop vertical (+6 → +16 → +6 u)
    L.ramp(92, 3.5, +8)                                  # course sur colonne brisée (+14 u)
    P.block(91.5, 95.8)
    cuirasses(L, 105, (1.25, 1.25), rise=2.0)            # 104 saut, 105, 106.25 → 107.5 (+16 u)
    P.jumps += [104, 105, 106.25]
    P.block(103.2, 109.2)
    L.ramp(116, 2.5, -10)                                # plongeon (+6 u)
    P.block(115.5, 119.0)
    P.block(125, 128)                                    # respiration : checkpoint
    P.fill(83.0, 125, "HJH", strong=0.32)
    fill_pulse(P, pulse240(83.0, 125, 0.5, 0.32), "HHJH")

    # ---- 128-160 : couplet furieux (grotte inversée + lianes aériennes)
    grotte(L, 132, 144, jumps=(135, 139), hanging=(136.5, 137.5, 140.5, 141.5, 143))
    P.hits += [136.5, 137.5, 140.5, 141.5, 143]
    P.jumps += [135, 139]
    P.block(130.5, 145.8)
    end = lianes(L, 148, [1.5, 1.5], air=1.25)           # → 154.25
    L.flyer(149.5)
    P.hits.append(149.5)
    P.jumps.append(148)
    P.block(147.2, end + 1.5)
    P.fill(128, 160, "HJH", strong=0.32)
    fill_pulse(P, pulse240(128, 160, 0.5, 0.32), "HHJ")

    # ---- 160-192 : « Miku-Miku-Miku-Miku ! » (rafales en 2×2 + ascension + notes)
    L.ramp(168, 4.0, +8)                                 # → +14 u
    P.block(167.5, 172.3)
    end = notes(L, 180, [0.5, 1.5, 2.5], air=1.25, rise=2.0)  # → +16 u
    P.jumps.append(180)
    P.block(179.2, end + 2.2)
    P.block(189.5, 192)
    P.fill(160, 189.5, "HHJH", strong=0.30)
    fill_pulse(P, pulse240(160, 189.5, 0.5, 0.30), "HHJH")

    # ---- 192-256 : refrain « La Cité » — tour géante (+34 u), plateformes du clocher, grand plongeon et arrière-plan
    for k, b_ in enumerate((194, 195.5, 197, 198.5)):
        L.bounce(b_, 1.5, +2.5, lums=False)              # +10 u → +26 u
        P.jumps.append(b_)
    L.lums(194.25, 14, 0.5)
    P.block(193.2, 200.8)
    L.ramp(202, 4.0, +8)                                 # sommet du clocher (+34 u !)
    P.block(201.5, 206.3)
    # 3 plateformes suspendues au sommet (212..220)
    L.void(212.4, 7.6)
    h0 = L.h
    for i, r in enumerate((1, 2, 1)):
        L.add(212 + 2.5 * i, "platform", length=2.55, height=h0 + r)
        P.jumps.append(212 + 2.5 * i)
    L.h = h0
    P.block(211.2, 221.2)
    # effondrement du clocher : double plongeon géant (−22 u → +12 u)
    L.ramp(224, 3.0, -12)
    L.ramp(227, 3.0, -10)
    P.block(223.5, 230.5)
    # colonnade d'arrière-plan (236..249)
    h = L.h
    L.to_plane(236, 1.5, "to_back")
    L.back(237, 4.35, 1.0)
    L.back(241.75, 6.25, 1.0)
    for bh in (238.0, 239.0, 240.0, 242.75, 243.75, 244.75, 245.75):
        L.back_enemy(bh)
        P.hits.append(bh)
    L.add(241, "jump", variant="back")
    L.cliff(241.2, 2.5)
    L.cliff(242.0, -2.5)
    L.to_plane(247, 1.5, "to_front")
    L.h = h
    P.jumps += [236, 241, 247]
    P.block(235.2, 249.5)
    P.block(253, 256)                                    # checkpoint avant la fausse accalmie
    P.fill(192, 253, "HHHJ", strong=0.35)
    fill_pulse(P, pulse240(192, 253, 0.5, 0.35), "HHHJ")

    # ---- 256-272 : fausse accalmie grise avant l'explosion finale
    L.ramp(260, 3.0, -6)                                 # → +6 u
    P.block(259.5, 263.3)
    P.block(269, 272)
    P.fill(256, 269, "HJ", strong=0.45)
    fill_pulse(P, pulse240(256, 269, 1.0, 0.45), "HHJ")

    # ---- 272-336 : hyper refrain (premières inversions négatives, cuirassés, paroi +8 u et plongeon −10 u)
    cuirasses(L, 281, (1.25, 1.25), rise=2.0)            # → +8 u
    P.jumps += [280, 281, 282.25]
    P.block(279.2, 285.0)
    L.ramp(296, 4.0, +8)                                 # → +16 u
    P.block(295.5, 300.3)
    end = notes(L, 312, [0.5, 1.5, 2.5], air=1.25)
    P.jumps.append(312)
    P.block(311.2, end + 2.2)
    L.ramp(324, 3.0, -10)                                # → +6 u
    P.block(323.5, 327.5)
    P.block(333, 336)                                    # checkpoint avant le sprint final
    P.fill(272, 333, "HJH", strong=0.30, kinds=("gap", "wall"))
    fill_pulse(P, pulse240(272, 333, 0.5, 0.30), "HHJH")

    # ---- 336-400 : sprint final déchaîné jusqu'au crash !
    grotte(L, 344, 354, jumps=(346.5, 351), hanging=(348.0, 349.0, 350.0, 352.25, 353.0))
    P.hits += [348.0, 349.0, 350.0, 352.25, 353.0]
    P.jumps += [346.5, 351]
    P.block(342.5, 355.8)
    L.ramp(368, 3.0, +6)
    P.block(367.5, 371.3)
    L.ramp(384, 3.0, -12)                                # retour vers le sol de la cité
    P.block(383.5, 387.5)
    P.fill(336, 398, "HHJH", strong=0.28)
    fill_pulse(P, pulse240(336, 398, 0.5, 0.28), "HHJH")

    # ---- 400-416 : le silence retombe, accord final de piano
    P.block(398, 416)
    L.enemy(400)

    effects(L)
    for name, beat in SECTIONS:
        if beat > 0:
            L.add(beat, "section_change", intensity=1.2 if "drop" in name or "refrain" in name else 0.5)
    L.ev.sort(key=lambda e: (e["beat"], e["type"]))
    return L.ev


def effects(L):
    """Mise en scène déchaînée de N8 : calme limpide (0-64), déchirure sur le décompte (64-80), explosion au drop (80),
    puis pluie soutenue d'écrans `/4` (2×2), de retournements (`mirror`, `up`, `both`) et d'inversions négatives !"""
    # Le décompte « ta... ta... ichi, ni, san, shi ! »
    L.corrupt(64, 0.25, 8)
    L.corrupt(72, 0.75, 8)
    L.glitch(68, 0.35, 4)
    L.glitch(72, 0.65, 8)
    for b in (72, 74, 76, 78):
        L.add(b, "camera_shake", intensity=0.45 + 0.1 * ((b - 72) // 2), length=0.4)
        L.add(b, "camera_zoom", intensity=0.6, length=0.3)

    # 80-128 : L'EXPLOSION DU DROP, puis `/4` dès 96 et miroir négatif à 112 !
    L.corrupt(80, 0.04, 2)
    L.add(80, "fx_flash", intensity=1.0)
    L.add(80, "camera_shake", intensity=1.0, length=1.2)
    L.add(80, "camera_zoom", intensity=1.0, length=0.5)
    L.screen_split(96, 4)
    L.screen_split(112, 1)
    L.screen_flip(112, "mirror")
    L.invert(112, True)
    L.screen_flip(128, "none")
    L.invert(128, False)

    # 128-160 : Couplet furieux — sortie de grotte tête en bas + négatif sur les lianes !
    L.screen_flip(146, "up")
    L.invert(146, True)

    # 160-192 : Miku-Miku-Miku-Miku — tout le passage en `/4`, d'abord en miroir puis tête en bas + négatif !
    L.screen_split(160, 4)
    L.screen_flip(160, "mirror")
    L.invert(160, False)
    L.glitch(168, 0.45, 8)
    L.screen_flip(176, "up")
    L.invert(176, True)
    L.screen_split(192, 1)

    # 192-256 : Refrain « La Cité » — montée en miroir, sommet à +34 u en `/4` négatif, plongeon en demi-tour, fond inversé !
    for b in (194, 195.5, 197, 198.5, 202):
        L.add(b, "camera_zoom", intensity=0.8, length=0.25)
    L.screen_flip(192, "mirror")
    L.invert(192, False)
    L.screen_split(208, 4)
    L.screen_flip(208, "none")
    L.invert(208, True)
    L.screen_split(224, 1)
    L.screen_flip(224, "both")
    L.invert(224, False)
    L.screen_flip(236, "up")
    L.invert(242, True)
    L.screen_flip(252, "none")
    L.invert(252, False)

    # 256-272 : Fausse accalmie grise avant la tempête finale
    L.gray(256, 0.85, 4)
    L.corrupt(266, 0.55, 6)
    L.gray(272, 0.0, 1)
    L.corrupt(272, 0.05, 1)

    # 272-336 : Hyper refrain — combinaisons extrêmes `/4` + retournements + clignotements négatifs calés à 1 Hz !
    L.add(272, "fx_flash", intensity=0.85)
    L.screen_split(272, 4)
    L.screen_flip(272, "mirror")
    L.invert(272, True)
    L.screen_split(288, 1)
    L.screen_flip(288, "both")
    L.invert(288, False)
    L.invert(296, True)
    L.screen_split(304, 4)
    L.screen_flip(304, "up")
    L.invert(304, False)
    L.glitch(308, 0.55, 12)
    L.screen_flip(320, "both")
    for b in (320, 324, 328, 332):                       # 1 bascule toutes les 4 pulsations = 1,0 s (1 Hz, sûr borne)
        L.invert(b, (b // 4) % 2 == 0)
    L.screen_split(336, 1)

    # 336-400 : Sprint final déchaîné — grotte en miroir négatif, puis `/4` tête en bas et `/4` demi-tour !
    L.screen_flip(336, "mirror")
    L.invert(336, True)
    L.screen_split(356, 4)
    L.screen_flip(356, "up")
    L.invert(356, False)
    L.screen_flip(372, "both")
    L.invert(372, True)
    L.glitch(372, 0.65, 16)
    L.screen_split(388, 1)
    L.screen_flip(388, "mirror")
    for b in (388, 392, 396):
        L.invert(b, b != 392)
    L.screen_flip(398, "none")
    L.invert(398, False)
    L.corrupt(398, 0.0, 2)


FICHE = Fiche(
    id="n8_cite",
    title="Atama no Taisou - hapi",
    bpm=240.0, offset=0.461, beats=416,
    sections=SECTIONS,
    analysis="n8_cite_analysis.json",
    build=build,
    zoom=640, zoom_wide=640,                    # à 12,8 u/s après le drop, la vue large fixe s'impose
    scale=(0, 2, 4, 7, 9),                      # do majeur pentatonique (contraste joyeux avec le chaos !)
    mechanics=ALL - {"laser"},
    margin_ms=65.0,
    checkpoint_every=16,
)
