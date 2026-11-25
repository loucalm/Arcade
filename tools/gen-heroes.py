#!/usr/bin/env python3
"""Toutes les images des héros Kenta, dessinées par la marionnette de tools/hero_rig.py.

Proportions d'environ 3,5 têtes (au lieu des ~2 du PDF de la DA), mêmes palettes et mêmes signes distinctifs que le
PDF. Chaque animation est une suite de poses ; les éléments souples (pans du bandeau, écharpe de Kenta, queue de
cheval d'Aiko, crinière de Ryo) sont simulés image par image : ils traînent dans le vent de la course, retombent au
sommet d'un saut, fouettent pendant la vrille et rebondissent à l'arrêt.

Les durées viennent du jeu : 150 BPM (1 temps = 0,4 s), course à 8 u/s, 40 px par unité, saut de 1,5 u en 1 temps.

    python3 tools/gen-heroes.py   → game/Assets/_Project/Art/Kenta/Gen/heroes/hero_<id>_<anim>[_<k>].png
"""
import math
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import hero_rig as R  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "game", "Assets", "_Project", "Art", "Kenta", "Gen", "heroes")

BEAT = 0.4                  # s (150 BPM)
SPEED = 8.0 * 40            # px/s
STRIDE = 2.4 * 40           # px par cycle de course (deux pas) : doit valoir HeroAnimator.strideUnits × 40
CLIMB_STRIDE = 2.0 * 40     # cycle de montée, le long de la pente : HeroAnimator.climbStrideUnits × 40
DESCEND_STRIDE = 2.2 * 40   # cycle de descente, le long de la pente : HeroAnimator.descendStrideUnits × 40
FALL_STEP, FALL_LOOP = 0.1, 0.12   # HeroAnimator.fallStepSeconds, fallLoopSeconds
JUMP_H = 1.5 * 40           # px
HIT_BEATS = 0.45            # doit valoir HeroAnimator.hitAnimBeats
RUN_FRAMES, JUMP_FRAMES, SLIDE_FRAMES = 8, 8, 3
SLIDE_IN_STEP, SLIDE_OUT_STEP, SLIDE_TURN_FRAMES = 0.04, 0.05, 3   # HeroAnimator.slideInSeconds = 0,12 s, slideOutSeconds = 0,15 s
DT = 1 / 480


# --- éléments souples ----------------------------------------------------------------------------

class Strand:
    """Chaîne de points (intégration de Verlet) accrochée à un point du corps.

    Simulée dans le repère du héros : l'air y défile à `air_vel` (l'opposé de la vitesse du héros). Le frottement
    rapproche la vitesse de chaque point de celle de l'air : en course, la chaîne traîne derrière ; au sommet d'un
    saut, elle retombe ; à l'arrêt, elle pend. (Dans le repère du monde, le point d'accroche avance de ~1 px par pas
    et l'intégration se met à fouetter.)"""

    def __init__(self, n, seg, gravity, drag, flutter, phase=0.0, rest=(-1.0, -0.4), push=0.0):
        self.n, self.seg, self.g, self.drag, self.flutter, self.phase = n, seg, gravity, drag, flutter, phase
        self.push = push                            # ressort vers l'arrière (px/s²) : cheveux qui ne pendent pas tout droit
        self.rest = rest
        self.p = None

    def reset(self, anchor):
        d = np.array(self.rest) / np.hypot(*self.rest)
        self.p = np.array([anchor + d * self.seg * i for i in range(self.n)], float)
        self.prev = self.p.copy()

    def step(self, anchor, air_vel, t, dt):
        anchor = np.asarray(anchor, float)
        if self.p is None:
            self.reset(anchor)
        vel = self.p - self.prev
        vel += (np.asarray(air_vel) * dt - vel) * self.drag
        self.prev = self.p.copy()
        acc = np.zeros_like(self.p)
        acc[:, 1] -= self.g
        acc[1:, 0] -= self.push
        wind = np.hypot(*air_vel)
        for i in range(1, self.n):                   # flottement : une onde qui court le long de la chaîne
            seg = self.p[i] - self.p[i - 1]
            norm = np.array([-seg[1], seg[0]]) / (np.hypot(*seg) + 1e-6)
            acc[i] += norm * self.flutter * min(1.0, wind / 300.0) * math.sin(t * 30.0 - i * 0.9 + self.phase)
        self.p = self.p + vel + acc * dt * dt
        self.p[0] = anchor
        for _ in range(4):
            for i in range(1, self.n):
                d = self.p[i] - self.p[i - 1]
                L = np.hypot(*d) + 1e-9
                self.p[i] = self.p[i - 1] + d * (self.seg / L)
        self.p[0] = anchor


def strands_for(hero):
    """Éléments souples d'un héros : nom → (chaîne, fonction d'ancrage sur le squelette)."""
    s = {
        "coat": (Strand(4, 2.4, 900, 0.28, 5000, 0.4, rest=(-0.4, -1.0)), R.coat_anchor),
        "tail0": (Strand(8, 2.3, 500, 0.3, 9000, 0.0), lambda sk: R.knot(sk)),
        "tail1": (Strand(7, 2.3, 500, 0.3, 9000, 1.9), lambda sk: R.add(R.knot(sk), sk.head_up, -1.4)),
    }
    if hero.id == "kenta":
        s["scarf"] = (Strand(6, 2.4, 700, 0.3, 7000, 0.7), R.scarf_anchor)
    if hero.id == "aiko":                            # pans courts (PDF) : la queue de cheval reste visible au-dessus
        s["tail0"] = (Strand(4, 2.2, 500, 0.3, 9000, 0.0), s["tail0"][1])
        s["tail1"] = (Strand(3, 2.2, 500, 0.3, 9000, 1.9), s["tail1"][1])
        s["pony"] = (Strand(10, 2.4, 3200, 0.11, 2600, 0.3, push=700), R.ponytail_anchor)
    if hero.id == "ryo":
        for i, (dx, dy) in enumerate(((-5.2, 2.0), (-5.6, -0.4), (-4.4, -2.6))):
            s[f"mane{i}"] = (Strand(4, 2.2, 1000, 0.25, 2500, i * 1.1),
                             (lambda dx, dy: lambda sk: R.add(R.add(sk.head, (sk.head_up[1], -sk.head_up[0]), dx * R.HS),
                                                              sk.head_up, dy * R.HS))(dx, dy))
    return s


class Take:
    """Un plan continu : le héros suit `timeline(t) → (pose, position du pivot dans le monde)`, on simule les
    éléments souples et on photographie aux instants demandés."""

    def __init__(self, hero):
        self.hero = hero
        self.strands = strands_for(hero)

    def shoot(self, timeline, until, shots, extras_fn=None):
        frames = []
        shots = sorted(shots)
        t, k = 0.0, 0
        prev_root = None
        while k < len(shots):
            pose, root = timeline(t)
            sk = R.place(pose)
            vel = (0.0, 0.0) if prev_root is None else ((root[0] - prev_root[0]) / DT, (root[1] - prev_root[1]) / DT)
            prev_root = root
            air = (-vel[0], -vel[1])
            for name, (strand, anchor) in self.strands.items():
                strand.step(anchor(sk), air, t, DT)
            if t >= shots[k] - 1e-9:
                frames.append(self.render(pose, t, extras_fn))
                k += 1
            t += DT
            if t > until + 1.0:
                raise RuntimeError("plan trop court")
        return frames

    def render(self, pose, t, extras_fn):
        local = lambda strand: [tuple(p) for p in strand.p]
        ex = R.Extras()
        st = self.strands
        ex.tails = [local(st["tail0"][0]), local(st["tail1"][0])]
        ex.coat = local(st["coat"][0])
        if "scarf" in st:
            ex.scarf = local(st["scarf"][0])
        if "pony" in st:
            ex.ponytail = local(st["pony"][0])
        manes = [local(v[0]) for k, v in st.items() if k.startswith("mane")]
        if manes:
            ex.mane = manes
        if extras_fn:
            extras_fn(ex, t)
        return R.draw(self.hero, pose, ex)


# --- poses -----------------------------------------------------------------------------------------

def run_pose(phase, lean=14.0):
    """Course : jambes opposées, genou très plié quand la jambe revient vers l'avant, bras opposés aux jambes."""
    def leg(ph):
        thigh = 42 * math.sin(ph)
        knee = 12 + 85 * max(0.0, math.cos(ph)) ** 1.2
        return thigh, knee
    tb, kb = leg(phase + math.pi)
    tf, kf = leg(phase)
    arm = 48 * math.sin(phase)
    return R.Pose(lean=lean, head=-6, thigh=(tb, tf), knee=(kb, kf), ankle=(10 * math.cos(phase + math.pi), 10 * math.cos(phase)),
                  upper=(arm, -arm), elbow=(85, 85))


def climb_pose(phase):
    """Montée d'une pente : penché en avant, genoux hauts, bras qui pompent."""
    def leg(ph):
        return 12 + 46 * math.sin(ph), 22 + 95 * max(0.0, math.cos(ph)) ** 1.2
    tb, kb = leg(phase + math.pi)
    tf, kf = leg(phase)
    arm = 55 * math.sin(phase)
    return R.Pose(lean=30, head=-18, thigh=(tb, tf), knee=(kb, kf), ankle=(15, 15), upper=(arm, -arm), elbow=(95, 95))


def descend_pose(phase):
    """Descente d'une pente : penché en arrière, foulée retenue, bras un peu ouverts pour l'équilibre."""
    def leg(ph):
        return -4 + 32 * math.sin(ph), 10 + 60 * max(0.0, math.cos(ph))
    tb, kb = leg(phase + math.pi)
    tf, kf = leg(phase)
    arm = 25 * math.sin(phase)
    return R.Pose(lean=-8, head=6, thigh=(tb, tf), knee=(kb, kf), ankle=(-5, 10), upper=(25 + arm, 25 - arm), elbow=(40, 40))


FALL_KEYS = [  # quitter un bord : un pas dans le vide, puis la chute bras levés (2 images en boucle)
    dict(lean=6, head=-6, thigh=(-20, 40), knee=(30, 15), upper=(40, 120), elbow=(40, 30)),
    dict(lean=-4, head=4, thigh=(10, 30), knee=(40, 25), upper=(150, 120), elbow=(20, 30)),
    dict(lean=-4, head=4, thigh=(20, 15), knee=(30, 45), upper=(130, 155), elbow=(30, 20)),
]


def crouch_pose():
    return R.Pose(lean=24, head=-12, thigh=(55, 80), knee=(115, 120), upper=(30, 55), elbow=(60, 55))


def lerp_pose(a, b, t):
    """Pose intermédiaire entre a et b (angles interpolés ; posée au sol)."""
    mix = lambda x, y: x + (y - x) * t
    tup = lambda x, y: tuple(mix(p, q) for p, q in zip(x, y))
    return R.Pose(lean=mix(a.lean, b.lean), head=mix(a.head, b.head), thigh=tup(a.thigh, b.thigh), knee=tup(a.knee, b.knee),
                  ankle=tup(a.ankle, b.ankle), upper=tup(a.upper, b.upper), elbow=tup(a.elbow, b.elbow))


def slide_in_pose(k):
    """Se jeter au sol : les genoux plient d'abord, puis le buste bascule en arrière et la jambe avant part devant."""
    if k == 0:
        return lerp_pose(run_pose(0.0), crouch_pose(), 0.6)
    return lerp_pose(crouch_pose(), slide_pose(0), (0.45, 0.8)[k - 1])


def slide_out_pose(k):
    """Se relever : le buste se redresse, on passe par l'accroupi, puis on repart en courant."""
    if k == 0:
        return lerp_pose(slide_pose(0), crouch_pose(), 0.45)
    if k == 1:
        return lerp_pose(slide_pose(0), crouch_pose(), 0.9)
    return lerp_pose(crouch_pose(), run_pose(math.pi / 2), 0.55)


def slide_pose(k=0):
    """Glissade façon baseball : couché sur le dos, jambe avant tendue, jambe arrière repliée à plat sous elle,
    main arrière qui frotte le sol, tête relevée qui regarde devant. ~25 px de haut (boîte de glissade : 20 px)."""
    wob = (0, 2, -2)[k % 3]
    return R.Pose(lean=-82, head=32, thigh=(80, 88), knee=(160, 0), ankle=(100, 20),
                  upper=(-100 + wob, 60 - wob), elbow=(5, 50))


JUMP_KEYS = [  # (pose, rotation) le long du saut ; la vrille fait un tour complet entre la 2e et la 7e image
    dict(lean=8, head=-4, thigh=(-28, 30), knee=(30, 70), upper=(120, 150), elbow=(30, 20)),     # impulsion
    dict(lean=30, head=-10, thigh=(70, 95), knee=(110, 120), upper=(90, 110), elbow=(60, 50)),   # genoux remontés
    dict(lean=45, head=10, thigh=(95, 105), knee=(135, 140), upper=(60, 75), elbow=(80, 80), rot=60),
    dict(lean=45, head=10, thigh=(95, 105), knee=(135, 140), upper=(60, 75), elbow=(80, 80), rot=150),
    dict(lean=45, head=10, thigh=(95, 105), knee=(135, 140), upper=(60, 75), elbow=(80, 80), rot=240),
    dict(lean=45, head=10, thigh=(95, 105), knee=(135, 140), upper=(60, 75), elbow=(80, 80), rot=320),
    dict(lean=15, head=-8, thigh=(25, 55), knee=(55, 60), upper=(110, 70), elbow=(30, 40)),      # ouverture
    dict(lean=6, head=-6, thigh=(5, 25), knee=(25, 35), upper=(130, 100), elbow=(20, 30)),       # réception
]
AIR_CENTER = 28.0           # hauteur du milieu du buste pendant la vrille (assez haut pour que la tête ne sorte pas)


def jump_pose(k):
    kw = dict(JUMP_KEYS[k])
    if k in (0, len(JUMP_KEYS) - 1):
        return R.Pose(**kw)
    return R.Pose(center=(0.0, AIR_CENTER), **kw)


HIT_KEYS = [  # frappe : l'arc part du haut, arc complet (gelé), fin de l'arc, retour
    (dict(lean=-4, head=-4, upper=(-30, 160), elbow=(20, 10)), (5, 55)),
    (dict(lean=16, head=-8, upper=(-55, 92), elbow=(25, 0)), (5, 160)),
    (dict(lean=22, head=-10, upper=(-60, 45), elbow=(25, 10)), (105, 165)),
    (dict(lean=12, head=-6, upper=(-35, 70), elbow=(25, 60)), None),
]


def hit_pose(k, phase):
    kw, _ = HIT_KEYS[k]
    base = run_pose(phase)
    p = R.Pose(**{**dict(thigh=base.thigh, knee=base.knee, ankle=base.ankle), **kw})
    p.weapon = True
    return p


# --- animations --------------------------------------------------------------------------------------

def running(phase0=0.0):
    def timeline(t):
        ph = phase0 + 2 * math.pi * SPEED * t / STRIDE
        return run_pose(ph), (SPEED * t, 0.0)
    return timeline


def anim_run(hero):
    cycle = STRIDE / SPEED
    warm = 3 * cycle
    return Take(hero).shoot(running(), warm + cycle, [warm + cycle * k / RUN_FRAMES for k in range(RUN_FRAMES)])


def anim_jump(hero):
    warm = 0.45
    run = running()

    def timeline(t):
        if t < warm:
            return run(t)
        p = min(1.0, (t - warm) / BEAT)
        k = min(JUMP_FRAMES - 1, int(p * JUMP_FRAMES))
        return jump_pose(k), (SPEED * t, JUMP_H * 4 * p * (1 - p))
    shots = [warm + BEAT * (k + 0.5) / JUMP_FRAMES for k in range(JUMP_FRAMES)]
    return Take(hero).shoot(timeline, shots[-1], shots)


def anim_hit(hero):
    warm = 0.45
    dur = HIT_BEATS * BEAT
    edges = (0.0, 0.12, 0.55, 0.78, 1.0)          # mêmes seuils que HeroAnimator.HitFrameIndex
    run = running()
    phase_at = lambda t: 2 * math.pi * SPEED * t / STRIDE

    def frame_at(t):
        p = (t - warm) / dur
        return max(k for k in range(4) if p >= edges[k])

    def timeline(t):
        if t < warm:
            return run(t)
        return hit_pose(frame_at(t), phase_at(warm)), (SPEED * t, 0.0)

    def extras(ex, t):
        if t >= warm:
            ex.arc = HIT_KEYS[frame_at(t)][1]
    shots = [warm + dur * (edges[k] + edges[k + 1]) / 2 for k in range(4)]
    return Take(hero).shoot(timeline, shots[-1], shots, extras)


def anim_slide(hero):
    warm = 0.45
    run = running()
    step = 0.06

    def timeline(t):
        if t < warm:
            return run(t)
        k = int((t - warm) / step) % SLIDE_FRAMES
        return slide_pose(k), (SPEED * t, 0.0)

    def extras(ex, t):
        if t < warm:
            return
        k = int((t - warm) / step) % SLIDE_FRAMES
        sk = R.place(slide_pose(k))
        heel = sk.ankles[1]
        puffs = ((-3.0, 2.0, 1.6), (-7.5, 3.2, 1.2), (-11.5, 2.4, 0.9))
        ex.dust = [(heel[0] + dx - 2.5 * k, dy + (k % 2) * 0.6, r) for dx, dy, r in puffs[: 2 + k % 2]]
        ex.speed = [(-28.0 + 2 * k, 13.0, 6.0 - k), (-25.0 - 2 * k, 8.0, 5.0 + k), (-30.0 + k, 4.0, 4.0)]
    shots = [warm + 3 * step * SLIDE_FRAMES + step * (k + 0.5) for k in range(SLIDE_FRAMES)]
    return Take(hero).shoot(timeline, shots[-1], shots, extras)


def anim_slide_in(hero):
    warm = 0.45
    run = running()

    def timeline(t):
        if t < warm:
            return run(t)
        k = min(SLIDE_TURN_FRAMES - 1, int((t - warm) / SLIDE_IN_STEP))
        return slide_in_pose(k), (SPEED * t, 0.0)
    shots = [warm + SLIDE_IN_STEP * (k + 0.5) for k in range(SLIDE_TURN_FRAMES)]
    return Take(hero).shoot(timeline, shots[-1], shots)


def anim_slide_out(hero):
    warm, slide = 0.45, 0.4
    run = running()

    def timeline(t):
        if t < warm:
            return run(t)
        if t < warm + slide:
            return slide_pose(0), (SPEED * t, 0.0)
        k = min(SLIDE_TURN_FRAMES - 1, int((t - warm - slide) / SLIDE_OUT_STEP))
        return slide_out_pose(k), (SPEED * t, 0.0)
    shots = [warm + slide + SLIDE_OUT_STEP * (k + 0.5) for k in range(SLIDE_TURN_FRAMES)]
    return Take(hero).shoot(timeline, shots[-1], shots)


def anim_slope(hero, pose_fn, stride, climb):
    """Cycle de montée ou de descente : le héros avance le long d'une pente (le vent relatif suit la pente)."""
    rise = 0.75 if climb else -0.6
    speed_along = SPEED * math.hypot(1.0, rise)
    cycle = stride / speed_along
    warm = 3 * cycle

    def timeline(t):
        return pose_fn(2 * math.pi * speed_along * t / stride), (SPEED * t, SPEED * rise * t)
    return Take(hero).shoot(timeline, warm + cycle, [warm + cycle * k / RUN_FRAMES for k in range(RUN_FRAMES)])


def anim_fall(hero):
    """Chute depuis un bord : on court, le sol disparaît, on tombe (même gravité que le saut)."""
    warm = 0.45
    run = running()
    g = 8 * JUMP_H / (BEAT * BEAT)

    def timeline(t):
        if t < warm:
            return run(t)
        dt = t - warm
        k = 0 if dt < FALL_STEP else 1 + int((dt - FALL_STEP) / FALL_LOOP) % 2
        return R.Pose(**FALL_KEYS[k]), (SPEED * t, -0.5 * g * dt * dt)
    shots = [warm + FALL_STEP * 0.5, warm + FALL_STEP + FALL_LOOP * 0.5, warm + FALL_STEP + FALL_LOOP * 1.5]
    return Take(hero).shoot(timeline, shots[-1], shots)


def anim_single(hero, pose, moving=True, settle=0.6, stop_at=None):
    """Une image : le héros court puis prend la pose (et s'arrête net si `stop_at`)."""
    run = running()
    warm = 0.45

    def timeline(t):
        if t < warm:
            return run(t)
        x = SPEED * (min(t, warm + (stop_at or 99)) if moving else warm)
        return pose, (x, 0.0)
    return Take(hero).shoot(timeline, warm + settle, [warm + settle])[0]


def anim_idle(hero):
    def timeline(t):
        breath = 0.6 * math.sin(t * 2.5)
        return R.Pose(lean=1 + breath), (0.0, 0.0)
    return Take(hero).shoot(timeline, 2.0, [2.0])[0]


def all_frames(hero_id):
    hero = R.HEROES[hero_id]
    f = {}
    f.update({f"run_{k}": a for k, a in enumerate(anim_run(hero))})
    f.update({f"jump_{k}": a for k, a in enumerate(anim_jump(hero))})
    f.update({f"hit_{k}": a for k, a in enumerate(anim_hit(hero))})
    f.update({f"slide_{k}": a for k, a in enumerate(anim_slide(hero))})
    f.update({f"slidein_{k}": a for k, a in enumerate(anim_slide_in(hero))})
    f.update({f"slideout_{k}": a for k, a in enumerate(anim_slide_out(hero))})
    f.update({f"climb_{k}": a for k, a in enumerate(anim_slope(hero, climb_pose, CLIMB_STRIDE, True))})
    f.update({f"descend_{k}": a for k, a in enumerate(anim_slope(hero, descend_pose, DESCEND_STRIDE, False))})
    f.update({f"fall_{k}": a for k, a in enumerate(anim_fall(hero))})
    f["idle"] = anim_idle(hero)
    f["crouch"] = anim_single(hero, crouch_pose(), settle=0.04)
    f["wall"] = anim_single(hero, run_pose(math.pi / 2, lean=34), settle=0.05)
    f["vine"] = anim_single(hero, R.Pose(lean=-5, head=-5, upper=(160, 172), elbow=(5, 8), thigh=(-12, 25), knee=(30, 40),
                                         hip=(0.0, 20.0)), settle=0.2)
    f["hurt"] = anim_single(hero, R.Pose(lean=-22, head=-18, thigh=(-30, 35), knee=(40, 20), upper=(150, -130),
                                         elbow=(20, 20), eyes="hurt"), stop_at=0.0, settle=0.12)
    f["death"] = anim_single(hero, R.Pose(lean=-8, head=-10, thigh=(5, 25), knee=(15, 40), upper=(-40, 60), elbow=(20, 30),
                                          rot=-90, eyes="closed"), stop_at=0.0, settle=0.8)
    return f


def main(only=None):
    os.makedirs(OUT, exist_ok=True)
    count = 0
    for hero_id in R.HEROES:
        if only and hero_id not in only:
            continue
        for name, a in all_frames(hero_id).items():
            Image.fromarray(a, "RGBA").save(os.path.join(OUT, f"hero_{hero_id}_{name}.png"))
            count += 1
    print(f"ok : {count} images dans {os.path.relpath(OUT, ROOT)}")


if __name__ == "__main__":
    main(sys.argv[1:] or None)
