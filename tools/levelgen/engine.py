"""Moteur commun des niveaux (ADR-035) : simulateur de l'autoplay, pièces, rail de caméra, vérifications, écriture.

Le simulateur reproduit la physique du jeu (miroir de PlayerController et LevelBuilder : saut 1,5 u en 1 beat,
tambours, pentes, lianes, gravité inversée, arrière-plan, mort sous KillY) et joue l'autoplay image par image : une
chart n'est écrite que si l'autoplay finit le niveau sans mourir et si chaque action laisse la marge de la fiche.

Le moteur sert **un niveau à la fois** : `use(fiche)` pose le tempo et les réglages du niveau dans les variables du
module (BPM, SPB, NB, X()…), puis toutes les fonctions les lisent. `generate(fiche)` le fait lui-même.
Temps : la physique du jeu est réglée en beats (comme PlayerController) ; les durées faites pour un humain (marge de
survie, tolérance, délai pour voir venir un ennemi ou se remettre d'une mort) sont en ms et converties au tempo.
"""
import json
import math
import os

from levelgen.fiche import MECHANICS, UNITS_PER_BEAT, mechanics_of

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CHARTS = os.path.join(ROOT, "game", "Assets", "_Project", "Charts")
DATA = os.path.join(ROOT, "tools", "data")

# Physique (identique à PlayerController / LevelBuilder), en beats
JUMP_HEIGHT, JUMP_BEATS = 1.5, 1.0
STEP_FACE_START = 0.5
STEP_START, GAP_START, WALL_START, ENEMY_CENTER = 0.3, 0.2, 0.4, 0.6
AIR_HIT_BELOW, AIR_HIT_ABOVE = 3.5, 3.5             # zone de frappe en l'air (au sol : de −0,5 à +1,6)
STOMP_TOUCH = 0.15                           # on touche un cuirassé 0,15 beat avant son temps (fin d'un saut)
ORB_HOVER = 3.0                              # chute ralentie (u/s) près d'une note, le temps d'appuyer
ORB_REACH = 3.0                              # note de saut : portée verticale (on la prend même en tombant vite)
PRESS_EARLY = PRESS_LATE = 0.35              # appui d'une note de saut ou d'un grand rebond : fenêtre autour du temps
VINE_EARLY = VINE_LATE = 0.35                # lâcher d'une liane : fenêtre autour de son temps (PlayerController)
JUMP_BUFFER = 0.35                           # appui en l'air juste avant d'atterrir : le saut part à l'atterrissage
LEDGE_ASSIST = 0.4                           # pieds à moins de 0,4 u sous le haut d'une marche ou d'un mur : on passe dessus
STEP_FACE = 0.25
MAX_RISE = 1.0
LUM_LANE_Y = (0.45, 1.9, 2.4)
PICKUP = 0.7

# Durées faites pour un humain (ms), converties en beats au tempo du niveau par use()
HUMAN_JITTER_MS = 40.0                       # tout le niveau survit à des appuis tous en avance ou tous en retard de 40 ms
ENEMY_LEAD_MS = 600.0                        # le rail cherche à montrer chaque ennemi 600 ms avant son coup…
MIN_LEAD_MS = 400.0                          # … et le générateur refuse un ennemi vu moins de 400 ms avant
CHECKPOINT_LEAD_MS = 500.0                   # un point de reprise laisse 500 ms avant la prochaine action (+ 0,7 s de pose de mort)

# Réglages du niveau en cours (posés par use)
FICHE = None
BPM = OFFSET = SPB = RUN_SPEED = GRAVITY = JUMP_V = None
NB = 0
SECTIONS, BIOMES = [], {}
ZOOM = ZOOM_WIDE = 480
PENTA = ()
ANALYSIS = None
MIN_MARGIN = HUMAN_JITTER = ENEMY_LEAD = MIN_LEAD = CHECKPOINT_LEAD = None
WIDE_MARGIN = []
CHECKPOINT_EVERY = 8


def ms_to_beats(ms):
    return ms * BPM / 60000.0


def use(fiche):
    """Pose le niveau en cours : tempo, longueur, sections, vue, gamme, marges (converties en beats)."""
    global FICHE, BPM, OFFSET, SPB, RUN_SPEED, GRAVITY, JUMP_V, NB, SECTIONS, BIOMES, ZOOM, ZOOM_WIDE, PENTA
    global ANALYSIS, MIN_MARGIN, HUMAN_JITTER, ENEMY_LEAD, MIN_LEAD, CHECKPOINT_LEAD, WIDE_MARGIN, CHECKPOINT_EVERY
    FICHE = fiche
    BPM, OFFSET, NB = float(fiche.bpm), fiche.offset, fiche.beats
    SPB = 60.0 / BPM
    RUN_SPEED = fiche.run_speed if fiche.run_speed is not None else round(UNITS_PER_BEAT * BPM / 60.0, 4)
    GRAVITY = 8 * JUMP_HEIGHT / (JUMP_BEATS * SPB) ** 2
    JUMP_V = 4 * JUMP_HEIGHT / (JUMP_BEATS * SPB)
    SECTIONS, BIOMES = list(fiche.sections), dict(fiche.biomes)
    ZOOM, ZOOM_WIDE = fiche.zoom, fiche.zoom_wide
    PENTA = tuple(fiche.scale)
    ANALYSIS = os.path.join(DATA, fiche.analysis)
    MIN_MARGIN = ms_to_beats(fiche.margin_ms)
    HUMAN_JITTER = ms_to_beats(HUMAN_JITTER_MS)
    ENEMY_LEAD = ms_to_beats(ENEMY_LEAD_MS)
    MIN_LEAD = ms_to_beats(MIN_LEAD_MS)
    CHECKPOINT_LEAD = ms_to_beats(CHECKPOINT_LEAD_MS)
    WIDE_MARGIN = list(fiche.margin_ranges) if fiche.margin_ranges is not None else [(0, NB)]
    CHECKPOINT_EVERY = fiche.checkpoint_every


def chart_path(fiche):
    return os.path.join(CHARTS, fiche.id + ".json")


def X(beat):
    return beat * SPB * RUN_SPEED


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


def simulate(events, fps=60, track=None, late=0.0, stop=None, start=0.0):
    """Autoplay image par image. Renvoie (None, stats) si le niveau est fini, sinon (message, stats).
    track : liste où noter (beat, centre du corps, gravité inversée, plan) à chaque image. late : retard (beats,
    négatif = avance) de chaque appui, pour vérifier qu'un humain a une marge ; un nombre (tous les appuis) ou une
    fonction beat de l'action → retard. stop : beat où l'on arrête (survie jusque-là = réussite). start : beat de
    départ, au sol (reprise après une mort : miroir de RunManager.RespawnAfterDeath).
    En gravité inversée, y et vy sont dans le repère retourné (y' = −y monde), comme dans PlayerController."""
    w = World(events)
    offset = late if callable(late) else (lambda _b, d=late: d)
    end = NB - 1 if stop is None else min(NB - 1, stop)
    dt = 1.0 / fps
    hw, stand = 0.25, 1.0
    y, vy, grounded = w.height(X(start)), 0.0, True
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
    while ji < len(w.jumps) and w.jumps[ji] < start - 1e-6:
        ji += 1
    while hi < len(w.hits) and w.hits[hi] < start - 1e-6:
        hi += 1
    while fi < len(w.flips) and w.flips[fi][0] <= start:
        fi += 1
    if start > 0:
        # comme RunManager.PrepareStartAt : rien ne chevauche le point de départ
        for en in w.enemies:
            if abs(en[0] - X(start)) < 0.9:
                en[3] = False
    jump_buf = -99
    last_ground = start
    hit_until = -99
    prev_x = X(start)
    prev_cy = y + 0.5
    top_y = bot_y = y
    t = start * SPB

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
            track.append((beat, cy, inv, plane, grounded))
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


def checkpoints(events, track):
    """Points de reprise après une mort (miroir de RunManager.RespawnAfterDeath) : un par phrase de 8 temps, au plus tard
    sur la phrase, là où le héros court au sol sur le chemin principal (premier plan, gravité normale), au moins
    CHECKPOINT_LEAD_MS avant la prochaine action et sans action juste avant. Ce qui chevauche le point de départ
    est retiré à la reprise (RunManager.PrepareStartAt), comme dans le simulateur. Chaque point est rejoué."""
    w = World(events)
    beats = [tr[0] for tr in track]
    actions = sorted(w.jumps + w.hits)

    def at(b):
        import bisect
        i = min(max(0, bisect.bisect_left(beats, b)), len(track) - 1)
        return track[i]

    def busy(b):
        return any(b - 0.25 <= a < b + CHECKPOINT_LEAD for a in actions)

    out = [0.0]
    for c in range(CHECKPOINT_EVERY, NB - CHECKPOINT_EVERY, CHECKPOINT_EVERY):
        for k in range(0, 4 * CHECKPOINT_EVERY):
            b = c - k * 0.25
            if b <= out[-1] + 2:
                break
            _, cy, inv, plane, grounded = at(b)
            if not grounded or not at(b + 0.25)[4] or inv or plane or busy(b):
                continue
            if abs((cy - 0.5) - w.height(X(b))) > 0.15:
                continue                      # pas au sol du chemin principal (plateforme au-dessus du vide…)
            out.append(b)
            break
    return out


def action_windows(events, lo=0.0, hi=None, limit=0.5, fps=60):
    """Marge de survie de chaque action (saut ou frappe) entre les beats lo et hi : de combien de beats on peut
    appuyer en avance (early) ou en retard (late) sans mourir, les autres appuis restant justes. Renvoie
    [(beat, genre, early, late)], marges en beats (0,25 beat = 100 ms à 150 BPM), plafonnées à `limit`."""
    hi = NB if hi is None else hi
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


def check_margins(events, ranges, margin=None, fps=60):
    """Vérifie que chaque action des passages `ranges` survit seule à un appui `margin` beats (par défaut, la marge de
    la fiche) en avance et en retard (2 simulations par action ; --marges donne le détail). Renvoie les problèmes."""
    margin = MIN_MARGIN if margin is None else margin
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


WIDE_DROP = 5.5                              # dénivelé (u) sur ±2 temps au-delà duquel on passe en plan large
FRAME_Y = 0.32                               # sol sous le héros à 32 % du bas
FRAME_Y_UP, FRAME_Y_DOWN = 0.24, 0.56        # bornes du cadrage selon la pente : montée, descente
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


def enemy_windows(world):
    """Pour chaque ennemi : (beat du coup, bas, haut) de sa boîte dans le monde."""
    return [(en[2], en[1], en[1] + 0.9) for en in world.enemies]


def zoom_plan(track, world, step=0.25, extra=()):
    """Largeur de vue à chaque pas du rail : ZOOM par défaut, ZOOM_WIDE sur chaque refrain entier et sur les mesures où
    un ennemi ne se verrait pas assez tôt (`extra`). Peu de coupes : les plans larges proches sont fusionnés.
    Renvoie (zooms par pas, événements `camera`)."""
    beats = [tr[0] for tr in track]
    n = NB * 4
    feet = [feet_at(track, beats, i * step) for i in range(n)]
    # peu de changements de zoom (retour de Jeremy le 25/11 : il y en avait trop) : un plan large sur chaque refrain
    # entier (chaînes de tambours, plongeons), et seulement là où un ennemi ne se verrait pas assez tôt en vue serrée
    bounds = [b for _, b in SECTIONS] + [NB]
    wins = [(b0, b1) for (name, b0), b1 in zip(SECTIONS, bounds[1:])
            if FICHE.wide_on_refrains and name.startswith("refrain")] + list(extra)
    wins = sorted((math.floor(a), math.ceil(b)) for a, b in wins)
    merged = []
    for a, b in wins:
        if merged and a <= merged[-1][1] + 8:          # deux plans larges à moins de 2 mesures : on n'en fait qu'un
            merged[-1][1] = max(merged[-1][1], b)
        else:
            merged.append([a, max(b, a + 2)])
    zooms = [ZOOM] * n
    events = []
    if ZOOM_WIDE == ZOOM:                    # vue fixe : aucun changement de plan
        return zooms, events
    for a, b in merged:
        a, b = max(0, a), min(NB - 1, b)
        for i in range(int(a / step), min(n, int(b / step))):
            zooms[i] = ZOOM_WIDE
        events.append({"beat": float(a), "type": "camera", "lane": 0, "params": {"zoom": ZOOM_WIDE}})
        events.append({"beat": float(b), "type": "camera", "lane": 0, "params": {"zoom": ZOOM}})
    return zooms, events


def zooms_from_events(events, step=0.25):
    """Largeur de vue à chaque pas du rail, d'après les événements `camera` de la chart."""
    cams = sorted((e["beat"], e["params"]["zoom"]) for e in events if e["type"] == "camera")
    zooms, z, k = [], ZOOM, 0
    for i in range(NB * 4):
        while k < len(cams) and cams[k][0] <= i * step + 1e-9:
            z = cams[k][1]
            k += 1
        zooms.append(z)
    return zooms


def camera_rail(track, step=0.25, enemies=(), zooms=None):
    """Rail de la caméra : trajectoire des pieds lissée dans les deux sens (elle anticipe montées et chutes),
    contrainte à garder le héros dans l'image (marges de CameraRig) et à montrer chaque ennemi au moins ENEMY_LEAD
    temps avant son coup, avec le cadrage par défaut (sol à FRAME_Y). Le héros passe avant les ennemis."""
    beats = [tr[0] for tr in track]
    n = NB * 4
    vh = [(zooms[i] if zooms else ZOOM) * 9 / 16 / 40.0 for i in range(n)]
    feet = [feet_at(track, beats, i * step) for i in range(n)]
    inv = [track_state(track, beats, i * step)[0] for i in range(n)]
    # corps : des pieds vers la tête (vers le bas en gravité inversée)
    top = [f if v else f + 1.0 for f, v in zip(feet, inv)]
    bottom = [f - 1.0 if v else f for f, v in zip(feet, inv)]
    lo = [t_ + 0.4 - h * (0.5 - SAFE_TOP) for t_, h in zip(top, vh)]       # le haut du corps sous la marge du haut (HUD)
    hi = [b_ + h * (0.5 - SAFE_BOTTOM) for b_, h in zip(bottom, vh)]        # le bas du corps au-dessus de la marge du bas
    # ennemis : visibles (sous le HUD, au-dessus du bas) pendant les ENEMY_LEAD temps avant leur coup
    elo = [-math.inf] * n
    ehi = [math.inf] * n
    for eb, y0, y1 in enemies:
        for i in range(max(0, int((eb - ENEMY_LEAD) / step)), min(n, int(eb / step) + 1)):
            elo[i] = max(elo[i], y1 + 0.05 - vh[i] * (0.5 - SAFE_TOP))     # 0,05 u de plus que la vérification
            ehi[i] = min(ehi[i], y0 - 0.35 + vh[i] * 0.5)
    # le sol sous le héros à FRAME_Y du bas ; au plafond, le plafond à FRAME_Y du haut
    # cadrage selon la pente devant (2 temps) : en descente le héros monte dans l'image (on voit où l'on va), en montée
    # il descend un peu (on voit le haut de la marche) ; au plafond, pas de changement
    ahead = int(2 / step)
    fy = []
    for i in range(n):
        d = feet[min(n - 1, i + ahead)] - feet[i]
        fy.append(FRAME_Y if inv[i] else min(FRAME_Y_DOWN, max(FRAME_Y_UP, FRAME_Y - 0.04 * d)))
    target = [f - h * (0.5 - y) if v else f + h * (0.5 - y) for f, v, h, y in zip(feet, inv, vh, fy)]

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


def enemy_leads(rail, world, step=0.25, zooms=None):
    """Pour chaque ennemi : depuis combien de temps (beats) il est entièrement dans l'image au moment du coup."""
    out = []
    for cx, ey, eb, *_ in world.enemies:
        first, b = None, eb
        while b > eb - 4:
            f = max(0.0, b / step)
            i = min(int(f), len(rail) - 2)
            c = rail[i] + (rail[i + 1] - rail[i]) * (f - i)
            z = zooms[i] if zooms else ZOOM
            view_w, view_h = z / 40.0, z * 9 / 16 / 40.0
            x0 = X(b) - view_w * 0.22
            if not (cx - 0.4 >= x0 and cx + 0.4 <= x0 + view_w and ey >= c - view_h / 2 + 0.3
                    and ey + 0.9 <= c + view_h * (0.5 - SAFE_TOP)):
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
    # plans larges : tambours, grandes dénivellations, et ennemis qu'on ne verrait pas venir en vue serrée
    extra = []
    for _ in range(4):
        zooms, cams = zoom_plan(track, World(base), extra=extra)
        full = sorted(out + cams, key=lambda e: (e["beat"], e["type"]))
        rail = camera_rail(track, enemies=enemy_windows(World(full)), zooms=zooms)
        late = [eb for eb, lead in enemy_leads(rail, World(full), zooms=zooms) if lead < MIN_LEAD - 1e-6]
        if not late:
            break
        # calé sur les phrases de 8 temps de la musique : le plan large commence et finit avec une phrase
        extra += [(math.floor((eb - 2.5) / 8) * 8, math.ceil((eb + 0.5) / 8) * 8) for eb in late]   # phrase de 8 temps
    return full, rail, None


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


def write(events, rail, out, checkpoints_=()):
    head = {"id": FICHE.id, "title": FICHE.title, "bpm": BPM,
            "offsetMs": int(OFFSET * 1000), "beatsPerBar": 4, "runSpeed": RUN_SPEED, "zoom": ZOOM, "railStep": 0.25}
    sections = [dict({"name": n, "startBeat": b}, **({"biome": BIOMES[n]} if n in BIOMES else {})) for n, b in SECTIONS]
    s = json.dumps(head, ensure_ascii=False, indent=2)[:-2]
    s += ',\n  "sections": [\n' + ",\n".join("    " + json.dumps(x, ensure_ascii=False) for x in sections) + "\n  ],\n"
    s += '  "checkpoints": ' + json.dumps([round(c, 2) for c in checkpoints_]) + ",\n"
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


def check_mechanics(events):
    """La fiche du niveau dit ce qu'il a le droit de demander au joueur : une mécanique de plus est refusée."""
    extra = mechanics_of(events) - set(FICHE.mechanics)
    return [f"mécanique hors de la fiche du niveau : {m} ({MECHANICS[m]})" for m in sorted(extra)]


def density(events):
    """Actions (sauts et frappes) par mesure, en moyenne sur le niveau (grille de difficulté, ADR-035)."""
    w = World(events)
    return round((len(w.jumps) + len(w.hits)) / (NB / 4), 2)


def generate(fiche):
    """Construit, résout et vérifie le niveau de la fiche. Renvoie (events, rail, stats, problèmes)."""
    use(fiche)
    song = load_analysis()
    built = fiche.build()
    events, rail, err = resolve(built, song)
    if err:
        return events, rail, {}, [f"autoplay : {err}"]
    problems = lint(events) + check_mechanics(built)
    stats = {}
    for fps in (60, 30):
        err, st = simulate(events, fps)
        if err:
            problems.append(f"autoplay à {fps} i/s : {err}")
        elif fps == 60:
            stats = st
    # tolérance humaine : appuis 40 ms en avance ou en retard partout
    for late in (-HUMAN_JITTER, HUMAN_JITTER):
        err, _ = simulate(events, 60, late=late)
        if err:
            problems.append(f"appuis décalés de {late * SPB * 1000:+.0f} ms : {err}")
    # passages réécrits avec la nouvelle règle : chaque action laisse au moins 120 ms de marge, seule, des deux côtés
    problems += check_margins(events, WIDE_MARGIN)
    # lisibilité : au moment de l'appui, une chauve-souris est à hauteur du héros ou au-dessus (jamais en bas du saut)
    track = []
    simulate(events, 120, track)
    beats = [tr[0] for tr in track]
    # die and retry : on peut repartir de chaque point de reprise (jusqu'au suivant, et un peu au-delà)
    cps = checkpoints(events, track)
    stats["checkpoints"] = cps
    stats["density"] = density(events)
    for i, cp in enumerate(cps):
        nxt = cps[i + 1] if i + 1 < len(cps) else NB - 1
        err, _ = simulate(events, 60, start=cp, stop=nxt + 4)
        if err:
            problems.append(f"reprise au beat {cp} : {err}")
    if len(cps) < NB / CHECKPOINT_EVERY * 0.5:
        problems.append(f"trop peu de points de reprise ({len(cps)})")
    for e in events:
        if e["type"] == "enemy" and e["lane"] == 1:
            inv, _ = track_state(track, beats, e["beat"])
            hero_c = track_at(track, beats, e["beat"])
            bat_c = e["params"]["height"] + 0.45
            if bat_c < hero_c - FLYER_BELOW or bat_c > hero_c + FLYER_ABOVE:
                problems.append(f"chauve-souris du beat {e['beat']} à {bat_c - hero_c:+.1f} u du héros au moment de la frapper "
                                f"(de −{FLYER_BELOW} à +{FLYER_ABOVE} u)")
    # lisibilité : chaque ennemi apparaît assez tôt pour qu'on ait le temps de le voir venir
    for eb, lead in enemy_leads(rail, World(events), zooms=zooms_from_events(events)):
        if lead < MIN_LEAD - 1e-6:
            problems.append(f"ennemi du beat {eb} visible seulement {lead * SPB * 1000:.0f} ms avant le coup "
                            f"(min {MIN_LEAD_MS:.0f} ms)")
    return events, rail, stats, problems
