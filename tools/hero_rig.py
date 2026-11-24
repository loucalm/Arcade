"""Marionnette pixel art des héros Kenta : proportions, palettes, rendu d'une pose.

Un héros est dessiné à partir d'un squelette (bassin, buste, tête, bras et jambes articulés), pas à partir d'une image :
chaque pose de chaque animation est une simple liste d'angles. Rendu sans anti-aliasing : formes pleines et
effilées, ombre d'un côté, reflet de l'autre ; contour noir d'1 px autour de la silhouette seulement, et lignes
intérieures dans un ton sombre de la pièce de devant (« sel-out ») : plus doux qu'un trait noir autour de chaque pièce.

Repère : x vers la droite (le héros regarde à droite), y vers le haut, en pixels ; (0, 0) = milieu-bas de l'image,
c'est le pivot du sprite dans Unity (les pieds). Le sol est à y = 1 : la dernière rangée reçoit le contour des semelles.

Les éléments souples (pans du bandeau, pan de veste, écharpe, queue de cheval, crinière) sont des chaînes de points
simulées par
tools/gen-heroes.py ; ce module ne fait que les dessiner.
"""
import math
from dataclasses import dataclass, field, replace

import numpy as np

INK = (0x1D, 0x1F, 0x20)
ARC_CREAM = (0xFF, 0xF6, 0xC8)
ARC_WHITE = (0xFF, 0xFF, 0xFF)
SPEED_LINE = (0x74, 0x9D, 0xC4)
DUST = ((0xC8, 0xCC, 0xD4), (0xF2, 0xF2, 0xF3))

# --- proportions (px) : ~3,5 têtes de haut (le PDF : ~2) -----------------------------------------
THIGH, SHIN, FOOT = 7.5, 7.5, 4.0
TORSO, NECK = 10.0, 1.0
HEAD_RX, HEAD_RY = 6.4, 6.6
UPPER_ARM, FOREARM = 6.0, 5.2
HIP_HALF = 1.4            # écart des deux hanches
SHOULDER_DROP = 2.2       # épaules sous la base du cou
R_THIGH, R_SHIN, R_FOOT = 2.6, 2.1, 1.6
R_KNEE, R_ANKLE = 2.0, 1.4            # les membres s'affinent vers les extrémités
R_UPPER, R_FORE, R_HAND = 2.0, 1.6, 1.7
R_ELBOW, R_WRIST = 1.6, 1.2
WAIST_HALF, CHEST_HALF = 3.8, 5.2
HS = HEAD_RX / 5.0        # échelle des détails de la tête (cheveux, traits)


def hexc(s):
    s = s.lstrip("#")
    return tuple(int(s[i:i + 2], 16) for i in (0, 2, 4))


def tone(dark, base, light=None):
    """Matériau : (ombre, base, reflet)."""
    return (hexc(dark), hexc(base), hexc(light) if light else hexc(base))


@dataclass
class Hero:
    id: str
    skin: tuple
    hair: tuple
    band: tuple                 # bandeau
    top: tuple                  # veste (buste et manches)
    pants: tuple
    shoes: tuple
    eye: tuple                  # (iris, reflet)
    mouth: tuple
    blush: tuple
    hair_style: str             # "spikes" | "ponytail" | "mane"
    extra: dict = field(default_factory=dict)


HEROES = {
    "kenta": Hero(
        "kenta",
        skin=tone("#DDA47E", "#F6C9A0"), hair=tone("#A8321C", "#E8542C", "#FF9A4A"),
        band=tone("#C8CCD4", "#F2F2F3", "#FFFFFF"), top=tone("#18213E", "#233056", "#3A4458"),
        pants=tone("#2A2A36", "#3A3A4A"), shoes=tone("#4A2816", "#6A3A22"),
        eye=(hexc("#C8862A"), hexc("#FFFFFF")), mouth=hexc("#7A3A2A"), blush=hexc("#F0998A"),
        hair_style="spikes",
        extra={"scarf": tone("#A8321C", "#D8502A", "#FF6B3D"), "belt": hexc("#6A3A22"), "buckle": hexc("#E8B84A"),
               "weapon": ("stick", hexc("#E8D2A0"), hexc("#C8862A")), "back": "sticks", "band_plate": hexc("#9AA4B4")}),
    "aiko": Hero(
        "aiko",
        skin=tone("#E2B39A", "#F8D8C0"), hair=tone("#1E6E70", "#2FA3A0", "#5FD0C8"),
        band=tone("#5A3AA8", "#8A5CFF", "#C8C0E8"), top=tone("#C8C0D8", "#F2EEF6", "#FFFFFF"),
        pants=tone("#2A2448", "#3A3260"), shoes=tone("#3A2418", "#5A3A2A"),
        eye=(hexc("#5A3AA8"), hexc("#FFFFFF")), mouth=hexc("#7A3A2A"), blush=hexc("#EAA8A2"),
        hair_style="ponytail",
        extra={"sash": tone("#5A3AA8", "#8A5CFF"), "weapon": ("flute", hexc("#C8A060"), hexc("#6A4A20"))}),
    "ryo": Hero(
        "ryo",
        skin=tone("#C48E6C", "#E8B890"), hair=tone("#A8B0BC", "#E8ECF0", "#FFFFFF"),
        band=tone("#7A1A28", "#B02A3A", "#D04A5A"), top=tone("#1E4A34", "#2E6A4A", "#3AA86A"),
        pants=tone("#2A2622", "#3A3430"), shoes=tone("#30261A", "#4A3A2A"),
        eye=(hexc("#3AA86A"), hexc("#FFFFFF")), mouth=hexc("#7A3A2A"), blush=hexc("#D98A70"),
        hair_style="mane",
        extra={"shirt": tone("#C8CCD4", "#F2F2F3"), "belt": hexc("#5A3A2A"), "back": "bass",
               "bass": tone("#7A1A28", "#B02A3A", "#D04A5A"), "bass_neck": tone("#2A1A1A", "#5A3A2A")}),
}


@dataclass
class Pose:
    """Angles en degrés. Membres : 0 = vers le bas, positif = vers l'avant. Buste : 0 = droit, positif = penché en avant."""
    lean: float = 0.0
    head: float = 0.0                       # inclinaison de la tête par rapport au buste (positif = vers l'avant)
    hip: tuple = (0.0, None)                # position du bassin ; y None = posé : le pied le plus bas touche le sol
    thigh: tuple = (-7.0, 9.0)              # (jambe arrière, jambe avant)
    knee: tuple = (8.0, 6.0)                # flexion du genou (positif : le tibia part vers l'arrière)
    ankle: tuple = (0.0, 0.0)               # pied relevé (positif = pointe vers le haut)
    upper: tuple = (-14.0, 16.0)            # bras (arrière, avant)
    elbow: tuple = (28.0, 24.0)             # flexion du coude (positif : l'avant-bras part vers l'avant)
    rot: float = 0.0                        # rotation de tout le corps autour du milieu du buste (positif = sens horaire)
    center: tuple = None                    # si donné : position du milieu du buste (en vol) ; sinon posé au sol
    weapon: bool = False                    # arme dans la main avant (frappe)
    eyes: str = "open"                      # "open" | "closed" | "hurt"
    ground: float = 1.0                     # hauteur du sol (pour les poses posées)


def vdown(a):
    """Vecteur unitaire d'un membre : a degrés depuis la verticale basse, vers l'avant."""
    r = math.radians(a)
    return (math.sin(r), -math.cos(r))


def vup(a):
    r = math.radians(a)
    return (math.sin(r), math.cos(r))


def add(p, v, k=1.0):
    return (p[0] + v[0] * k, p[1] + v[1] * k)


@dataclass
class Skeleton:
    hip: tuple
    neck: tuple
    head: tuple
    head_up: tuple
    shoulder: tuple
    hips: tuple                 # (arrière, avant)
    knees: tuple
    ankles: tuple
    toes: tuple
    elbows: tuple
    hands: tuple
    rot: float = 0.0


def solve(pose, hip=(0.0, 0.0)):
    """Cinématique directe, sans rotation globale ni pose au sol."""
    up = vup(pose.lean)
    neck = add(hip, up, TORSO)
    head_up = vup(pose.lean + pose.head)
    head = add(neck, head_up, NECK + HEAD_RY)
    shoulder = add(neck, up, -SHOULDER_DROP)
    hips, knees, ankles, toes, elbows, hands = [], [], [], [], [], []
    for side, dx in ((0, -HIP_HALF), (1, HIP_HALF)):
        h = (hip[0] + dx * 0.6, hip[1])
        k = add(h, vdown(pose.thigh[side]), THIGH)
        shin = pose.thigh[side] - pose.knee[side]
        a = add(k, vdown(shin), SHIN)
        foot_dir = vdown(shin + 90 - pose.ankle[side])
        t = add(a, foot_dir, FOOT)
        hips.append(h), knees.append(k), ankles.append(a), toes.append(t)
        e = add(shoulder, vdown(pose.upper[side]), UPPER_ARM)
        hd = add(e, vdown(pose.upper[side] + pose.elbow[side]), FOREARM)
        elbows.append(e), hands.append(hd)
    return Skeleton(hip, neck, head, head_up, shoulder, tuple(hips), tuple(knees), tuple(ankles), tuple(toes),
                    tuple(elbows), tuple(hands))


def rotate_point(p, c, deg_cw):
    r = -math.radians(deg_cw)
    x, y = p[0] - c[0], p[1] - c[1]
    return (c[0] + x * math.cos(r) - y * math.sin(r), c[1] + x * math.sin(r) + y * math.cos(r))


def mid_torso(sk):
    return ((sk.hip[0] + sk.neck[0]) / 2, (sk.hip[1] + sk.neck[1]) / 2)


def place(pose, hip_y_default=THIGH + SHIN + 2.0):
    """Squelette posé : rotation globale autour du milieu du buste, puis buste placé (`center`), bassin fixé (`hip`),
    ou partie du corps la plus basse posée sur le sol."""
    hx, hy = pose.hip
    sk = solve(pose, (hx, hip_y_default if hy is None else hy))
    if pose.rot:
        c = mid_torso(sk)
        R = lambda p: rotate_point(p, c, pose.rot)
        sk = Skeleton(R(sk.hip), R(sk.neck), R(sk.head), rotate_point(sk.head_up, (0, 0), pose.rot), R(sk.shoulder),
                      tuple(map(R, sk.hips)), tuple(map(R, sk.knees)), tuple(map(R, sk.ankles)),
                      tuple(map(R, sk.toes)), tuple(map(R, sk.elbows)), tuple(map(R, sk.hands)), pose.rot)
    if pose.center is not None:
        m = mid_torso(sk)
        sk = shift(sk, (pose.center[0] - m[0], pose.center[1] - m[1]))
    elif hy is None:
        sk = shift(sk, (0.0, pose.ground + 0.35 - lowest(sk)))   # 0,35 et pas 0,5 : la rangée du bas est bien pleine
    return sk


def lowest(sk):
    """Point le plus bas du corps (pieds, genoux, bassin, mains, tête), rayons compris."""
    pts = [(p, R_FOOT + 0.3) for p in sk.ankles] + [(p, R_FOOT - 0.3) for p in sk.toes] + \
          [(p, R_SHIN) for p in sk.knees] + \
          [(sk.hip, R_THIGH + 0.5), (sk.head, HEAD_RY)] + [(p, R_HAND) for p in sk.hands] + \
          [(p, R_FORE) for p in sk.elbows]
    return min(p[1] - r for p, r in pts)


def shift(sk, d):
    S = lambda p: (p[0] + d[0], p[1] + d[1])
    return Skeleton(S(sk.hip), S(sk.neck), S(sk.head), sk.head_up, S(sk.shoulder), tuple(map(S, sk.hips)),
                    tuple(map(S, sk.knees)), tuple(map(S, sk.ankles)), tuple(map(S, sk.toes)),
                    tuple(map(S, sk.elbows)), tuple(map(S, sk.hands)), sk.rot)


def knot(sk):
    """Nœud du bandeau, à l'arrière de la tête : point d'attache des pans."""
    u = sk.head_up
    right = (u[1], -u[0])
    return add(add(sk.head, right, -HEAD_RX + 0.5), u, 2.85 * HS)


def scarf_anchor(sk):
    u = sk.head_up
    right = (u[1], -u[0])
    return add(add(sk.neck, right, -2.0), u, -0.5)


def ponytail_anchor(sk):
    u = sk.head_up
    right = (u[1], -u[0])
    return add(add(sk.head, right, -HEAD_RX + 0.5), u, 4.6 * HS)


def coat_anchor(sk):
    """Bas du dos de la veste : point d'attache du pan de veste."""
    up = (sk.neck[0] - sk.hip[0], sk.neck[1] - sk.hip[1])
    n = math.hypot(*up) or 1.0
    up = (up[0] / n, up[1] / n)
    right = (up[1], -up[0])
    return add(add(sk.hip, right, -WAIST_HALF + 1.2), up, 1.5)


# --- rendu ---------------------------------------------------------------------------------------

class Canvas:
    """Toile de rendu : formes (masques booléens), puis pièces empilées de l'arrière vers l'avant."""

    def __init__(self, w=96, h=72):
        self.w, self.h = w, h
        self.rgba = np.zeros((h, w, 4), np.uint8)
        self.ids = np.full((h, w), -1, int)
        self.meta = []                              # par pièce : (couleur des lignes intérieures, n° de groupe)
        self.groups = {}
        cols = np.arange(w) + 0.5 - w / 2
        rows = h - (np.arange(h) + 0.5)
        self.X, self.Y = np.meshgrid(cols, rows)

    # formes
    def cone(self, a, b, r0, r1=None):
        """Segment épais qui s'affine de r0 (en a) à r1 (en b). r1 = r0 : capsule."""
        r1 = r0 if r1 is None else r1
        ax, ay = a
        dx, dy = b[0] - ax, b[1] - ay
        L2 = dx * dx + dy * dy
        t = np.zeros_like(self.X) if L2 < 1e-9 else np.clip(((self.X - ax) * dx + (self.Y - ay) * dy) / L2, 0, 1)
        px, py = ax + t * dx, ay + t * dy
        r = r0 + (r1 - r0) * t
        return (self.X - px) ** 2 + (self.Y - py) ** 2 <= r * r

    def capsule(self, a, b, r):
        return self.cone(a, b, r, r)

    def ellipse(self, c, rx, ry, up=(0.0, 1.0)):
        right = (up[1], -up[0])
        x, y = self.X - c[0], self.Y - c[1]
        u = x * right[0] + y * right[1]
        v = x * up[0] + y * up[1]
        return (u / rx) ** 2 + (v / ry) ** 2 <= 1.0

    def polygon(self, pts):
        inside = np.zeros_like(self.X, bool)
        n = len(pts)
        for i in range(n):
            x1, y1 = pts[i]
            x2, y2 = pts[(i + 1) % n]
            cond = ((y1 > self.Y) != (y2 > self.Y))
            with np.errstate(divide="ignore", invalid="ignore"):
                xint = (x2 - x1) * (self.Y - y1) / (y2 - y1 + 1e-12) + x1
            inside ^= cond & (self.X < xint)
        return inside

    def chain(self, pts, r0, r1):
        """Chaîne de points épaisse, qui s'affine régulièrement de r0 à r1."""
        m = np.zeros_like(self.X, bool)
        n = len(pts) - 1
        for i in range(n):
            ra = r0 + (r1 - r0) * i / max(1, n)
            rb = r0 + (r1 - r0) * (i + 1) / max(1, n)
            m |= self.cone(pts[i], pts[i + 1], ra, rb)
        return m

    def arc(self, c, r, a0, a1, width):
        """Arc de cercle épais, angles en degrés depuis la verticale haute, sens horaire."""
        x, y = self.X - c[0], self.Y - c[1]
        d = np.sqrt(x * x + y * y)
        ang = np.degrees(np.arctan2(x, y)) % 360
        span = (a1 - a0) % 360                       # de a0 à a1 dans le sens horaire
        within = ((ang - a0) % 360) <= span
        return within & (np.abs(d - r) <= width / 2)

    # peinture
    def piece(self, mask, material, light=True, group=None):
        """Une pièce du corps, posée devant les précédentes : remplissage ombré (ombre en bas-arrière, reflet en
        haut-avant). Les contours viennent à la fin (finish)."""
        if not mask.any():
            return
        dark, base, high = material
        fill = np.zeros(mask.shape + (3,), np.uint8)
        fill[:] = base
        below = np.zeros_like(mask)
        below[:-1] = mask[1:]
        back = np.zeros_like(mask)
        back[:, 1:] = mask[:, :-1]
        fill[mask & ~(below & back)] = dark
        if light:
            above = np.zeros_like(mask)
            above[1:] = mask[:-1]
            front = np.zeros_like(mask)
            front[:, :-1] = mask[:, 1:]
            fill[mask & ~above & front & below] = high
        self.rgba[mask, :3] = fill[mask]
        self.rgba[mask, 3] = 255
        k = len(self.meta)
        key = group if group is not None else ("piece", k)
        self.meta.append((line_color(material), self.groups.setdefault(key, len(self.groups))))
        self.ids[mask] = k

    def finish(self):
        """Lignes intérieures (bord d'une pièce posée sur une autre, dans le ton sombre de la pièce de devant) puis
        contour noir de la silhouette."""
        ids = self.ids
        solid = ids >= 0
        lines = np.zeros_like(solid)
        h, w = ids.shape
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nb = np.full_like(ids, -1)
            ys = slice(max(0, dy), h + min(0, dy))
            yd = slice(max(0, -dy), h + min(0, -dy))
            xs = slice(max(0, dx), w + min(0, dx))
            xd = slice(max(0, -dx), w + min(0, -dx))
            nb[yd, xd] = ids[ys, xs]
            far = np.full_like(ids, -1)                 # pixel de l'autre côté : la pièce doit avoir 2 px d'épaisseur
            fs = slice(max(0, -dy), h + min(0, -dy))
            fd = slice(max(0, dy), h + min(0, dy))
            fxs = slice(max(0, -dx), w + min(0, -dx))
            fxd = slice(max(0, dx), w + min(0, dx))
            far[fd, fxd] = ids[fs, fxs]
            behind = solid & (nb >= 0) & (nb < ids) & (far == ids)
            if behind.any():
                groups = np.array([g for _, g in self.meta])
                same = np.zeros_like(behind)
                yy, xx = np.nonzero(behind)
                same[yy, xx] = groups[ids[yy, xx]] == groups[nb[yy, xx]]
                lines |= behind & ~same
        yy, xx = np.nonzero(lines)
        for y, x in zip(yy, xx):
            self.rgba[y, x, :3] = self.meta[ids[y, x]][0]
        ring = dilate(solid) & ~solid
        self.rgba[ring] = (*INK, 255)

    def stamp(self, mask, material, light=True):
        """Effet posé par-dessus (arc, poussière) : son propre contour noir."""
        if not mask.any():
            return
        ring = dilate(mask) & ~mask
        self.rgba[ring] = (*INK, 255)
        dark, base, high = material
        self.rgba[mask, :3] = base
        if light:
            below = np.zeros_like(mask)
            below[:-1] = mask[1:]
            self.rgba[mask & ~below, :3] = dark
        self.rgba[mask, 3] = 255

    def flat(self, mask, color):
        self.rgba[mask, :3] = color
        self.rgba[mask, 3] = 255

    def dot(self, p, color):
        c = int(math.floor(p[0] + self.w / 2))
        r = int(math.floor(self.h - p[1]))
        if 0 <= r < self.h and 0 <= c < self.w:
            self.rgba[r, c, :3] = color
            self.rgba[r, c, 3] = 255


def dilate(m):
    d = m.copy()
    d[1:] |= m[:-1]
    d[:-1] |= m[1:]
    d[:, 1:] |= m[:, :-1]
    d[:, :-1] |= m[:, 1:]
    return d


def darker(material):
    """Version « arrière-plan » d'un matériau (bras et jambe du fond)."""
    d, b, h = material
    return (line_color(material), d, b)


def line_color(material):
    """Ton des lignes intérieures d'un matériau : son ombre, assombrie (juste l'ombre pour les tons très clairs,
    sinon un bandeau blanc vire au gris)."""
    if sum(material[1]) > 600:
        return material[0]
    return tuple(int(v * 0.72) for v in material[0])


@dataclass
class Extras:
    """Éléments ajoutés à une pose : chaînes simulées, effets."""
    tails: list = None          # pans du bandeau : liste de 2 chaînes de points
    coat: list = None           # pan de veste, dans le dos
    scarf: list = None          # pan de l'écharpe (Kenta)
    ponytail: list = None       # queue de cheval (Aiko)
    mane: list = None           # mèches arrière (Ryo) : liste de chaînes
    arc: tuple = None           # (a0, a1) arc de frappe, degrés depuis le haut, sens horaire
    dust: list = None           # [(x, y, r)]
    speed: list = None          # [(x, y, longueur)]


def draw(hero, pose, extras=None, w=96, h=72):
    """Rend une pose : tableau RGBA (h, w, 4)."""
    ex = extras or Extras()
    cv = Canvas(w, h)
    sk = place(pose)
    up = sk.head_up
    right = (up[1], -up[0])
    body_up = vup(pose.lean + pose.rot)
    body_right = (body_up[1], -body_up[0])

    # 1. éléments souples derrière le corps
    if ex.ponytail:
        cv.piece(cv.chain(ex.ponytail, 3.0, 0.6), hero.hair, group="hair")
    if ex.mane:
        for m in ex.mane:
            cv.piece(cv.chain(m, 2.2, 0.5), hero.hair, group="hair")
    if ex.scarf:
        cv.piece(cv.chain(ex.scarf, 1.7, 0.7), hero.extra["scarf"], light=False, group="scarf")
    if ex.coat:
        cv.piece(cv.chain(ex.coat, 2.6, 1.2), darker(hero.top), group="top")

    # 2. objet porté dans le dos
    back = hero.extra.get("back")
    if back == "bass":
        body_c = add(add(sk.shoulder, body_up, -6.0), body_right, -3.4)
        cv.piece(cv.ellipse(body_c, 3.4, 4.6, body_up), hero.extra["bass"])
        neck_end = add(add(sk.shoulder, body_up, 7.5), body_right, -4.5)
        cv.piece(cv.cone(body_c, neck_end, 1.0, 0.8) | cv.cone(neck_end, add(neck_end, body_up, 1.8), 1.3, 1.1),
                 hero.extra["bass_neck"], light=False)
    elif back == "sticks":                           # une baguette en travers du dos, qui dépasse de l'épaule
        base = add(add(sk.shoulder, body_right, -2.8), body_up, -5.0)
        tilt = pose.lean + pose.rot - 32
        w_ = hero.extra["weapon"]
        cv.piece(cv.cone(add(base, vup(tilt), -1.0), add(base, vup(tilt), 12.0), 0.9, 0.7), (w_[2], w_[1], w_[1]),
                 light=False)

    # 3. bras et jambe du fond
    arm(cv, sk, 0, darker(hero.top), darker(hero.skin))
    leg(cv, sk, 0, darker(hero.pants), darker(hero.shoes))

    # 4. buste
    torso(cv, hero, sk, pose, body_up, body_right)

    # 5. jambe avant
    leg(cv, sk, 1, hero.pants, hero.shoes)

    # 6. tête
    head(cv, hero, sk, pose, up, right)
    if ex.tails:
        for t in ex.tails:
            cv.piece(cv.chain(t, 1.2, 0.5), hero.band, light=False, group="band")

    # 7. bras avant (+ arme)
    if pose.weapon:
        weapon(cv, hero, sk, pose)
    arm(cv, sk, 1, hero.top, hero.skin)

    cv.finish()
    for p in getattr(cv, "finish_marks", []):       # trous de la flûte
        cv.dot(p, hero.extra["weapon"][2])
    face(cv, hero, sk, pose, up, right)

    # 8. effets (par-dessus, avec leur propre contour)
    if ex.arc:
        a0, a1 = ex.arc
        c = add(sk.shoulder, (3.5, -1.0))
        outer = cv.arc(c, 12.5, a0, a1, 4.2)
        cv.stamp(outer, (ARC_CREAM, ARC_CREAM, ARC_WHITE), light=False)
        cv.flat(cv.arc(c, 13.7, a0 + 6, a1 - 6, 1.0) & outer, ARC_WHITE)
    if ex.dust:
        for x, y, r in ex.dust:
            cv.stamp(cv.ellipse((x, y), r, r * 0.8), (DUST[0], DUST[1], DUST[1]))
    if ex.speed:
        for x, y, length in ex.speed:
            cv.stamp(cv.capsule((x, y), (x + length, y), 0.5), (SPEED_LINE, SPEED_LINE, SPEED_LINE), light=False)
    return cv.rgba


def arm(cv, sk, side, sleeve, skin):
    s, e, hnd = sk.shoulder, sk.elbows[side], sk.hands[side]
    cv.piece(cv.cone(s, e, R_UPPER, R_ELBOW) | cv.cone(e, hnd, R_ELBOW, R_WRIST), sleeve, group=("arm", side))
    d = (hnd[0] - e[0], hnd[1] - e[1])
    n = math.hypot(*d) or 1.0
    fist = add(hnd, (d[0] / n, d[1] / n), 0.8)
    cv.piece(cv.ellipse(fist, R_HAND, R_HAND * 0.9), skin, light=False)


def leg(cv, sk, side, pants, shoes):
    hip, k, a, t = sk.hips[side], sk.knees[side], sk.ankles[side], sk.toes[side]
    cv.piece(cv.cone(hip, k, R_THIGH, R_KNEE) | cv.cone(k, a, R_KNEE, R_ANKLE), pants, group=("leg", side))
    d = (t[0] - a[0], t[1] - a[1])
    n = math.hypot(*d) or 1.0
    heel = add(a, (d[0] / n, d[1] / n), -0.8)
    cv.piece(cv.cone(heel, t, R_FOOT + 0.3, R_FOOT - 0.3), shoes)


def torso(cv, hero, sk, pose, up, right):
    w0, w1 = WAIST_HALF, CHEST_HALF
    hip, neck = add(sk.hip, up, -1.2), add(sk.neck, up, -0.4)       # la veste descend un peu sous les hanches
    P = lambda base, dx, dy: add(add(base, right, dx), up, dy)
    pts = [P(hip, -w0 - 0.3, 0), P(hip, w0 - 0.2, 0), P(hip, w0 + 0.3, 3.0),      # taille
           P(neck, w1 - 0.2, -4.0), P(neck, w1 - 0.6, -1.4), P(neck, w1 - 2.2, 0.2),   # poitrine, épaule avant
           P(neck, -w1 + 1.6, 0.2), P(neck, -w1, -1.6), P(neck, -w1 + 0.4, -5.0)]       # épaule et dos
    m = cv.polygon(pts)
    cv.piece(m, hero.top, group="top")
    e = hero.extra
    if "shirt" in e:                                 # veste ouverte (Ryo)
        a, b = add(neck, right, 1.4), add(hip, right, 2.2)
        cv.piece(cv.cone(a, b, 1.3, 0.9) & m, e["shirt"], light=False)
    if "sash" in e:                                  # ceinture croisée (Aiko)
        a, b = P(neck, -w1 + 0.8, -1.4), P(hip, w0, 3.4)
        cv.piece(cv.capsule(a, b, 0.9) & m, e["sash"], light=False)
    if "belt" in e:
        belt = cv.capsule(P(hip, -w0, 2.0), P(hip, w0, 2.0), 0.7) & m
        cv.flat(belt, e["belt"])
        if "buckle" in e:
            cv.dot(P(hip, w0 - 1.5, 2.0), e["buckle"])
    if "scarf" in e:                                 # écharpe autour du cou (Kenta)
        cv.piece(cv.cone(add(sk.neck, right, -2.8), add(sk.neck, right, 2.6), 1.7, 1.4), e["scarf"])


def lock(cv, c, up, right, dx, dy, angle, length, bend, r0):
    """Mèche courbe et effilée : part de (dx, dy) (repère de la tête), direction `angle` (0 = haut, 90 = avant,
    -90 = arrière), qui tourne de `bend` degrés sur sa longueur."""
    P = lambda x, y: add(add(c, right, x * HS), up, y * HS)
    pts = [P(dx, dy)]
    x, y = dx, dy
    steps = 3
    for i in range(steps):
        a = math.radians(angle + bend * (i + 0.5) / steps)
        x += math.sin(a) * length / steps
        y += math.cos(a) * length / steps
        pts.append(P(x, y))
    return cv.chain(pts, r0 * HS, 0.45)


HAIR = {  # (dx, dy, angle, longueur, courbure, épaisseur), repère de la tête, unités de HS
    "spikes": [(-0.5, 5.4, -20, 7.5, -25, 2.3), (2.0, 5.2, 8, 6.5, -32, 2.1), (4.0, 4.0, 38, 5.0, -38, 1.8),
               (-3.0, 4.8, -50, 8.0, -22, 2.3), (-5.0, 3.0, -85, 7.5, -16, 2.1), (-5.6, 0.5, -115, 6.0, -6, 1.9),
               (-5.0, -2.0, -140, 4.5, 10, 1.6), (4.8, 3.2, 128, 3.8, 28, 1.4), (3.0, 3.8, 150, 3.4, 22, 1.3)],
    "mane": [(-0.5, 5.4, -14, 7.0, -30, 2.3), (2.2, 5.0, 16, 5.5, -36, 2.0), (-3.0, 4.8, -55, 8.5, -26, 2.4),
             (-5.0, 2.8, -90, 9.0, -20, 2.4), (-5.6, 0.0, -120, 8.0, -10, 2.2), (-5.2, -2.6, -150, 6.5, 6, 2.0),
             (4.6, 3.0, 134, 4.5, 30, 1.6), (2.6, 3.6, 156, 4.0, 16, 1.5)],
    "ponytail": [(4.6, 3.4, 140, 4.6, 26, 1.7), (2.8, 4.0, 160, 5.0, 16, 1.7), (0.8, 4.4, 176, 4.0, 10, 1.5),
                 (-4.2, -0.5, 180, 6.0, 12, 1.8)],
}


def head(cv, hero, sk, pose, up, right):
    c = sk.head
    P = lambda dx, dy: add(add(c, right, dx * HS), up, dy * HS)
    skull = cv.ellipse(c, HEAD_RX, HEAD_RY, up)
    jaw = cv.polygon([P(-3.6, -1.0), P(4.9, -1.6), P(4.4, -4.4), P(2.6, -5.7), P(-1.6, -4.6)])   # menton vers l'avant
    face = skull | jaw
    cv.piece(face, hero.skin, group="skin")
    # cheveux : calotte + mèches courbes
    cap = cv.ellipse(P(-0.9, 1.6), HEAD_RX + 0.9 * HS, HEAD_RY + 0.2 * HS, up)
    x, y = cv.X - c[0], cv.Y - c[1]
    u = (x * right[0] + y * right[1]) / HS
    v = (x * up[0] + y * up[1]) / HS
    hair = cap & ~((u > -2.6) & (v < 1.9)) & ~((u < -2.6) & (v < -1.6))
    for spec in HAIR[hero.hair_style]:
        hair |= lock(cv, c, up, right, *spec)
    # ombre des cheveux sur le front : 1 px de peau plus sombre sous la frange
    shadow = np.zeros_like(hair)
    shadow[1:] = hair[:-1]
    shadow &= face & ~hair
    cv.piece(hair, hero.hair, group="hair")
    cv.flat(shadow, hero.skin[0])
    # bandeau : bande sur le front et le haut de la tête, nœud à l'arrière
    band = (face | hair) & band_region(cv, c, up, 2.3 * HS, 3.9 * HS) & cv.ellipse(c, HEAD_RX + 1.2, HEAD_RY + 1.5, up)
    band |= cv.ellipse(knot(sk), 1.4, 1.2, up)
    cv.piece(band, hero.band, light=False, group="band")


def face(cv, hero, sk, pose, up, right):
    """Traits du visage (vue de trois quarts vers la droite), après les contours."""
    c = sk.head
    P = lambda dx, dy: add(add(c, right, dx), up, dy)
    if "band_plate" in hero.extra:                   # plaque gravée de la croche (Kenta)
        for dx in (3.0, 4.0, 5.0):
            cv.dot(P(dx, 2.9 * HS), hero.extra["band_plate"])
    ear = P(-2.6, -1.4)
    cv.dot(ear, hero.skin[0])
    cv.dot(add(ear, up, -1.0), hero.skin[0])
    eye = P(3.2, -0.7)
    if pose.eyes == "open":
        for dx in (-0.5, 0.5, 1.5):                   # paupière
            cv.dot(add(add(eye, up, 1.0), right, dx), INK)
        cv.dot(eye, INK)                             # pupille
        cv.dot(add(eye, right, 1.0), hero.eye[1])    # reflet
        cv.dot(add(eye, up, -1.0), hero.eye[0])
        cv.dot(add(add(eye, up, -1.0), right, 1.0), hero.eye[0])
        back_eye = add(eye, right, -3.6)
        cv.dot(add(back_eye, up, 1.0), INK)
        cv.dot(back_eye, INK)
        cv.dot(add(back_eye, up, -1.0), hero.eye[0])
    else:                                            # yeux fermés (« > » si touché)
        cv.dot(eye, INK)
        cv.dot(add(eye, right, 1.0), INK)
        cv.dot(add(eye, right, -3.6), INK)
        if pose.eyes == "hurt":
            cv.dot(add(add(eye, up, 1.0), right, -0.5), INK)
            cv.dot(add(add(eye, up, -1.0), right, -0.5), INK)
    cv.dot(P(4.4, -3.9), hero.mouth)
    cv.dot(P(1.8, -2.2), hero.blush)
    cv.dot(P(2.8, -2.2), hero.blush)


def band_region(cv, c, up, lo, hi):
    x, y = cv.X - c[0], cv.Y - c[1]
    v = x * up[0] + y * up[1]
    return (v >= lo) & (v <= hi)


def weapon(cv, hero, sk, pose):
    kind, light, dark = hero.extra.get("weapon", (None, None, None))
    if kind is None:
        return
    hand = sk.hands[1]
    d = (hand[0] - sk.elbows[1][0], hand[1] - sk.elbows[1][1])
    n = math.hypot(*d) or 1.0
    d = (d[0] / n, d[1] / n)
    length = 7.0 if kind == "stick" else 9.0
    tip = add(hand, d, length)
    tail = add(hand, d, -2.0)
    cv.piece(cv.cone(tail, tip, 0.9, 0.7) if kind == "stick" else cv.capsule(tail, tip, 0.9), (dark, light, light),
             light=False)
    if kind == "flute":
        cv.finish_marks = [add(hand, d, k) for k in (3.0, 5.0, 7.0)]
