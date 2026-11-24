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
STEP_START, GAP_START, WALL_START, ENEMY_CENTER, SLIDE_START = 0.3, 0.2, 0.36, 0.4, 0.1
STEP_FACE = 0.25
MAX_RISE = 1.0
LUM_LANE_Y = (0.45, 1.9, 2.4)
PICKUP = 0.7

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

    def slide(self, beat, length=1.0):
        self.add(beat, "slide_bar", length=length)

    def lums(self, beat, count, step=0.5, lane=0):
        """Ligne de pièces posées sur la trajectoire de l'autoplay (calculée par le simulateur, voir place_lums)."""
        for i in range(count):
            self.add(beat + i * step, "lum_trail", lane=i)

    def flyer(self, beat):
        """Ennemi en vol à frapper sur `beat` : sa hauteur est posée sur la trajectoire simulée (resolve)."""
        self.add(beat, "enemy_air", lane=1)


# ---------------------------------------------------------------- les scènes
# Principes : docs/level-design-musical.md. Les temps viennent de la carte du morceau (tools/data/level01_analysis.json,
# voir `python3 tools/gen-level-vertical.py --map`) : grosse caisse → sauts, caisse claire → coups de poing.
# Les pièces (arcs au-dessus des sauts, lignes de mélodie) et la hauteur des ennemis volants sont posées ensuite,
# automatiquement, sur la trajectoire simulée (resolve).

def scene_intro(L):
    """0-32 : une leçon par geste, avec de l'air autour. Sauter (8), monter (14), frapper (20), glisser (25),
    puis la montée de grosse caisse et le premier tambour : envol sur le drop du couplet."""
    L.gap(8)                    # k
    L.step(14, +1)              # K
    L.enemy(20)                 # S
    L.slide(25, 2.0)            # le morceau retient son souffle
    L.gap(28.5)                 # K : la montée commence
    L.bounce(30, 2.0, +5)       # X X : envol, on atterrit sur le premier temps du couplet


def scene_couplet(L, base, variant=False):
    """32-64 : on grimpe en suivant la caisse claire. Motif : deux coups, une marche ; puis saut + coup en l'air.
    Signature : la course sur le mur (note tenue) puis la dévalée."""
    b = base
    L.enemy(b + 1)              # 33 S
    L.enemy(b + 2)              # 34 S
    L.enemy(b + 4)              # 36 S
    L.enemy(b + 5)              # 37 S
    L.step(b + 6, +1)           # 38 k : une marche
    L.enemy(b + 7.5)            # 39,5 S
    L.enemy(b + 9)              # 41 S
    L.step(b + 10.5, +1)        # 42,5 k : saut…
    L.flyer(b + 11)             # 43 S : … et coup en plein vol
    L.enemy(b + 13)             # 45 S
    if variant:
        L.flyer(b + 15)         # 47 S (passage 2 : un saut de plus)
        L.step(b + 14.5, +1)
    # course sur le mur : de 48 à 49,5, +5 (+6 au passage 2), sur la note tenue
    L.ramp(b + 16, 1.5, +6 if variant else +5)
    L.enemy(b + 19)             # 51
    L.enemy(b + 22.5)           # 54,5 S
    L.enemy(b + 23.5)           # 55,5 S
    L.enemy(b + 25)             # 57 S
    # la dévalée : on file dans la pente sur 2 beats
    L.ramp(b + 25.5, 2.0, -8)
    L.enemy(b + 28)             # 60 S
    L.enemy(b + 29)             # 61 S
    L.gap(b + 31)               # 63


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


def scene_roulement(L, base, variant=False):
    """80-96 : la grosse caisse tape à contretemps : on gravit un escalier, une marche par coup, puis on saute
    de toit en toit. Le tambour géant ouvre le refrain."""
    b = base
    for k in (1.5, 2.5, 3.5, 4.5, 5.5, 6.5):          # 81,5 … 86,5 : X X S X K K
        L.step(b + k, +1)
    for k, kind in ((8.5, "gap"), (9.5, "step"), (10.5, "gap"), (11.5, "step"), (12.5, "gap"), (13.5, "step")):
        if kind == "gap":
            L.gap(b + k)
        else:
            L.step(b + k, +1)
    L.bounce(b + 15.5, 2.5, +6)                         # 95,5 → 98 : dans le ciel


def scene_refrain(L, base, variant=False):
    """96-120 : dans le ciel. Signature : les tambours en chaîne, un ennemi à frapper à chaque envolée.
    Puis la grande dévalée et le plongeon vers la retombée."""
    b = base
    L.enemy(b + 3)              # 99 S
    L.enemy(b + 4)              # 100 S
    # tambours en chaîne : 102 → 104 → 106 → 108 (rebonds courts, le sol reste à l'image), coup en l'air entre deux
    L.bounce(b + 6, 2.0, +1)
    L.flyer(b + 7)              # 103 S
    L.bounce(b + 8, 2.0, +1)
    L.flyer(b + 9)              # 105 S
    L.bounce(b + 10, 2.0, +1)
    L.flyer(b + 11)             # 107 k
    if variant:
        L.bounce(b + 12, 2.0, +1)
        L.flyer(b + 13)
        L.ramp(b + 14.5, 2.0, -11)
    else:
        L.ramp(b + 12.5, 2.0, -10)                      # 108,5 : la dévalée
        L.enemy(b + 15)         # 111 S
    L.enemy(b + 17)             # 113 S
    L.gap(b + 18.5)
    L.enemy(b + 20)             # 116 S
    L.enemy(b + 21)             # 117 S
    L.step(b + 22.5, -6)        # 118,5 : le plongeon (la caméra l'a anticipé)


def scene_retombee(L, base):
    """120-128 : on souffle. Des pièces qui chantent la mélodie, deux coups, deux sauts."""
    b = base
    L.gap(b + 0.5)              # 120,5 K
    L.enemy(b + 2)              # 122 S
    L.enemy(b + 4)              # 124 S
    L.gap(b + 6.5)              # 126,5 K


def scene_final(L):
    """128-152 : la tour. Une marche par temps sur la montée, trois coups au sommet, un tambour avec un coup en
    l'air, et la plus grande dévalée du niveau vers le couplet 2."""
    for k in range(7):          # 129,5 … 135,5
        L.step(129.5 + k, +1)
    L.enemy(137)
    L.enemy(138)
    L.enemy(139)
    L.bounce(140, 2.0, +3)
    L.flyer(141)
    L.gap(142.5)                # K
    L.enemy(144)
    L.gap(145.5)                # k
    L.enemy(147)
    L.ramp(148, 3.5, -16)       # la grande dévalée


def scene_fin(L):
    L.enemy(249)
    L.gap(250.5)
    L.enemy(252)


def build():
    L = Level()
    scene_intro(L)
    scene_couplet(L, 32)
    scene_pont(L, 64)
    scene_roulement(L, 80)
    scene_refrain(L, 96)
    scene_retombee(L, 120)
    scene_final(L)
    # passage 2 : la musique de [32, 128) revient en [152, 248) : mêmes idées, variées et plus hautes
    scene_couplet(L, 152, variant=True)
    scene_pont(L, 184, variant=True)
    scene_roulement(L, 200, variant=True)
    scene_refrain(L, 216, variant=True)
    scene_retombee(L, 240)
    scene_fin(L)
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
        self.jumps, self.hits, self.slides = [], [], []
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
        self.gaps, self.walls, self.enemies, self.bars, self.platforms, self.bounces = [], [], [], [], [], []
        for e in events:
            p, b, t = e.get("params", {}), e["beat"], e["type"]
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
                cx = X(b + ENEMY_CENTER)
                ey = p["height"] if e["lane"] == 1 else self.height(cx)
                self.enemies.append([cx, ey, b, True])
                self.hits.append(b)
            elif t == "slide_bar":
                ln = p.get("length", 1.0)
                x0, x1 = X(b + SLIDE_START), X(b + SLIDE_START + ln)
                self.bars.append((x0, x1, self.height(x0) + 0.62))
                self.slides.append((b - 0.05, b + SLIDE_START + ln + 0.35))
            elif t == "platform":
                x0, x1 = X(b + STEP_START), X(b + STEP_START + p.get("length", 1.0))
                self.platforms.append(Span(x0, x1, p["height"], p["height"], True))
                self.jumps.append(b)
            elif t == "bounce":
                cx = X(b)
                self.bounces.append([cx, self.height(cx), p.get("length", 2.0), b, False])
                self.jumps.append(b)
        self.jumps.sort()
        self.hits.sort()
        self.spans = self.main_spans() + self.platforms
        self.spans.sort(key=lambda s: s.x0)
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
                    self.lums.append([x, LUM_LANE_Y[min(e["lane"], 2)] + base, lb, True])
            elif e["type"] == "lum_at":
                self.lums.append([X(b), p["height"], b, True])

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

    # miroir de LevelTerrain
    def overl(self, s, x, hw):
        return x + hw > s.x0 and x - hw < s.x1

    def support(self, x, hw, y):
        best = -math.inf
        for i, s in enumerate(self.spans):
            if not self.overl(s, x, hw):
                continue
            t = s.top(x)
            joint = any(o.sloped and self.overl(o, x, hw + 0.3) for o in self.spans[max(0, i - 1):i + 2])
            tol = 0.45 if (s.sloped or joint) else 0.02
            if abs(y - t) <= tol and t > best:
                best = t
        return best

    def land(self, x, hw, yp, yn):
        if yn > yp:
            return None
        best = None
        for s in self.spans:
            if not self.overl(s, x, hw):
                continue
            t = s.top(x)
            tol = 0.45 if s.sloped else 0.02
            if yp >= t - tol and yn <= t and (best is None or t > best):
                best = t
        return best

    def hits_side(self, x, hw, y):
        return any(not s.one_way and self.overl(s, x, hw) and y < s.top(x) - 0.4 for s in self.spans)

    def kill_y(self, x):
        left = right = None
        under = math.inf
        for s in self.spans:
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
        return (min(c) if c else 0.0) - 2.5


def simulate(events, fps=60, track=None, late=0.0):
    """Autoplay image par image. Renvoie (None, stats) si le niveau est fini, sinon (message, stats).
    track : liste où noter (beat, centre du corps) à chaque image. late : retard (beats, négatif = avance) de
    chaque appui, pour vérifier qu'un humain a une marge."""
    w = World(events)
    dt = 1.0 / fps
    hw, stand, slide_h = 0.25, 1.0, 0.5
    y, vy, grounded = w.height(0), 0.0, True
    ji = hi = 0
    jump_buf = -99
    last_ground = 0.0
    hit_until = -99
    prev_x = 0.0
    prev_cy = y + 0.5
    top_y = bot_y = y
    t = 0.0
    while True:
        beat = t / SPB
        if beat > NB - 1:
            missed = [round(l[2], 2) for l in w.lums if l[3]]
            return None, {"max_y": top_y, "min_y": bot_y, "lums": len(w.lums), "missed": missed}
        x = X(beat)
        jump = False
        while ji < len(w.jumps) and w.jumps[ji] + late <= beat:
            jump, ji = True, ji + 1
        while hi < len(w.hits) and w.hits[hi] + late <= beat:
            hit_until, hi = beat + 0.3, hi + 1
        sliding_cmd = any(a <= beat <= b for a, b in w.slides)
        if grounded:
            s = w.support(x, hw, y)
            if s == -math.inf:
                grounded, vy = False, 0.0
            else:
                y = s
        if grounded:
            last_ground = beat
        if jump:
            jump_buf = beat
        coyote = not grounded and vy <= 0 and beat - last_ground <= 0.12
        if beat - jump_buf <= 0.2 and (grounded or coyote):
            vy, grounded, jump_buf = JUMP_V, False, -99
        if not grounded and sliding_cmd and vy > -JUMP_V:
            vy = -JUMP_V
        sliding = sliding_cmd and grounded
        if not grounded:
            yp = y
            y += vy * dt - 0.5 * GRAVITY * dt * dt
            vy -= GRAVITY * dt
            top = w.land(x, hw, yp, y) if vy <= 0 else None
            if top is not None:
                y, vy, grounded = top, 0.0, True
            elif w.hits_side(x, hw, y):
                return f"cogne le bord d'un trou au beat {beat:.2f} (x {x:.1f}, y {y:.2f})", {}
        if not grounded and y < w.kill_y(x):
            return f"tombe dans le vide au beat {beat:.2f} (x {x:.1f}, y {y:.2f})", {}
        # tambours
        for pad in w.bounces:
            cx, top, air, pb, used = pad
            if used or abs(cx - x) > 0.6 or x < cx - 0.15 or y > top + 0.35 + 1.2 or y < top - 0.4:
                continue
            pad[4] = True
            ta = air * SPB
            land_top = w.height(x + ta * RUN_SPEED)
            vy = (land_top - y + 0.5 * GRAVITY * ta * ta) / ta
            grounded, jump_buf = False, -99
        # collisions
        h = slide_h if sliding else stand
        x_from = max(prev_x, x - 3)
        body = (x_from - hw, y, x + hw, y + h)
        hitting = beat <= hit_until
        hit_box = (x_from, y - 0.5, x + 1.9, y - 0.5 + 2.1)

        def ov(a, b):
            return a[0] < b[2] and b[0] < a[2] and a[1] < b[3] and b[1] < a[3]

        for fx_, h0, h1, fb in w.faces:
            if ov(body, (fx_, h0, fx_ + 0.05, h1)):
                return f"se cogne dans une marche (beat {fb}) au beat {beat:.2f}", {}
        for wx, wy, wb in w.walls:
            if ov(body, (wx + 0.1, wy, wx + 0.8, wy + 0.75)):
                return f"se cogne dans le mur du beat {wb} au beat {beat:.2f}", {}
        for en in w.enemies:
            cx, ey, eb, alive = en
            if not alive:
                continue
            r = (cx - 0.4, ey, cx + 0.4, ey + 0.9)
            if hitting and ov(hit_box, r):
                en[3] = False
            elif ov(body, r):
                return f"touché par l'ennemi du beat {eb} au beat {beat:.2f} (y {y:.2f}, sol {ey:.2f})", {}
        for x0, x1, yb in w.bars:
            if ov(body, (x0, yb, x1, yb + 8)):
                return f"se cogne dans un plafond au beat {beat:.2f}", {}
        cy = y + h * 0.5
        if track is not None:
            track.append((beat, cy))
        for lum in w.lums:
            if lum[3] and abs(lum[0] - x) < 1.0:
                # balayage depuis l'image précédente (comme CollectLums)
                ax, ay, bx, by = prev_x, prev_cy, x, cy
                dx_, dy_ = bx - ax, by - ay
                L2 = dx_ * dx_ + dy_ * dy_
                u = 0.0 if L2 < 1e-9 else max(0.0, min(1.0, ((lum[0] - ax) * dx_ + (lum[1] - ay) * dy_) / L2))
                if (ax + dx_ * u - lum[0]) ** 2 + (ay + dy_ * u - lum[1]) ** 2 <= PICKUP * PICKUP:
                    lum[3] = False
        prev_cy = cy
        top_y, bot_y = max(top_y, y), min(bot_y, y)
        prev_x = x
        t += dt


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
    import bisect
    i = min(max(1, bisect.bisect_left(beats, beat)), len(track) - 1)
    (b0, y0), (b1, y1) = track[i - 1], track[i]
    return y0 + (y1 - y0) * ((beat - b0) / (b1 - b0) if b1 > b0 else 0.0)


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
    for a, c in world.slides:
        busy.append((a - 0.5, c + 0.5))
    for pad in world.bounces:
        busy.append((pad[3] - 0.5, pad[3] + pad[2] + 0.5))
    out = []

    def free(b):
        return all(abs(b - t) > 0.2 for t in taken) and all(abs(b - t) > 0.35 for t in air)

    for j in world.jumps:
        if any(abs(j - pad[3]) < 1e-6 for pad in world.bounces):
            continue                          # les envolées ont déjà leur arc
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


def camera_rail(track, step=0.25):
    """Rail de la caméra : trajectoire des pieds lissée dans les deux sens (elle anticipe montées et chutes),
    contrainte à garder le héros dans l'image, avec le cadrage par défaut (sol à FRAME_Y)."""
    beats = [b for b, _ in track]
    n = NB * 4
    feet = [track_at(track, beats, i * step) - 0.5 for i in range(n)]
    lo = [y + 1.4 - VIEW_H * 0.4 for y in feet]        # la tête reste sous la marge du haut (10 %)
    hi = [y + VIEW_H * 0.42 for y in feet]             # les pieds restent au-dessus de la marge du bas (8 %)
    target = [y + VIEW_H * (0.5 - FRAME_Y) for y in feet]

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

    c = smooth(target, 6.0)                            # σ = 1,5 beat
    for _ in range(12):
        c = [min(max(x, a), b) for x, a, b in zip(c, lo, hi)]
        c = smooth(c, 3.0)
    c = [min(max(x, a), b) for x, a, b in zip(c, lo, hi)]
    return [round(x, 2) for x in c]


def resolve(events, song):
    """Pose ce qui dépend de la trajectoire : pièces (et leurs notes), ennemis en vol, rail de caméra."""
    base = [e for e in events if e["type"] not in ("lum_trail", "enemy_air")]
    track = []
    err, _ = simulate(base, 120, track)
    if err:
        return events, None, err
    beats = [b for b, _ in track]
    world = World(base)
    events = events + auto_lums(events, world, song)
    out = []
    for e in events:
        if e["type"] == "lum_trail":
            y = track_at(track, beats, e["beat"])
            out.append({"beat": round(e["beat"], 3), "type": "lum_at", "lane": 0,
                        "params": {"height": round(y, 3), "hasNote": True, "note": note_for(e["beat"], song)}})
        elif e["type"] == "enemy_air":
            y = track_at(track, beats, e["beat"] + ENEMY_CENTER) - 0.5    # au niveau des pieds quand on l'atteint
            out.append({"beat": e["beat"], "type": "enemy", "lane": 1, "params": {"height": round(y + 0.1, 3)}})
        else:
            out.append(e)
    out.sort(key=lambda e: (e["beat"], e["type"]))
    return out, camera_rail(track), None


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
        if e["type"] in ("enemy", "block") and e["lane"] != 1:   # un ennemi en vol se frappe justement en sautant
            for j in jumps:
                if j < e["beat"] < j + 1.0 - 1e-9:
                    problems.append(f"ennemi au beat {e['beat']} pendant le saut du beat {j}")
    return problems


def table(events):
    sym = {"gap": "G", "wall": "W", "enemy": "E", "slide_bar": "S", "step": "^", "ramp": "/", "platform": "P",
           "bounce": "T", "camera": "c"}
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
    sections = [{"name": n, "startBeat": b} for n, b in SECTIONS]
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
    return events, rail, stats, problems


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("out", nargs="?", default=DEFAULT_OUT)
    ap.add_argument("--table", action="store_true", help="afficher le niveau mesure par mesure")
    ap.add_argument("--map", action="store_true", help="afficher la carte des attaques du morceau")
    args = ap.parse_args()
    if args.map:
        print(song_map(load_analysis()))
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
