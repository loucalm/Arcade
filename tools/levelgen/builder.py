"""Écriture d'un niveau : la classe `Level` construit les événements de la chart (docs/chart-format.md) et suit la
hauteur du sol (relative) au fil de l'écriture. Les temps sont en beats : rien ici ne dépend du tempo."""


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


# ---------------------------------------------------------------- gestes composés, réutilisés d'un niveau à l'autre

def cuirasses(L, beat, airs=(2.0,), rise=0.0):
    """On saute (un temps avant) sur un cuirassé posé au `beat`, on appuie au rebond ; avec plusieurs `airs`, on
    rebondit de cuirassé en cuirassé. Dans un enchaînement, garder le premier rebond à 1 temps : un rebond de 2 temps
    monte à 6 u et cacherait le cuirassé suivant en vue fixe (6,75 u de haut). `rise` : on se pose plus haut (falaise
    atteinte en vol). Renvoie le temps où l'on se pose."""
    L.add(beat - 1, "jump")
    t = beat
    for air in airs:
        L.stomp(t, air)
        t += air
    if rise:
        L.cliff(t - 0.75, rise)
    return t


def lianes(L, beat, lengths, air=1.0):
    """On saute dans le vide au `beat` et on attrape une liane ¾ de temps plus tard ; chaque liane se lâche en
    sautant (au bout de sa longueur, sur un temps), et le vol (`air` temps) mène à la suivante, un cran plus haut,
    ou à l'autre bord. Le trou s'arrête un quart de temps avant l'atterrissage. Renvoie le temps où l'on se pose."""
    t = beat + 0.75
    for i, length in enumerate(lengths):
        L.vine(t, length, 1.1 + i, 1.6 + i, air=air)
        t += length + air
    L.gap(beat, t - 0.25 - (beat + 0.2))
    return t


def notes(L, beat, dys, air=1.0, rise=0.0):
    """On saute dans le vide au `beat`, puis on relance le saut de note en note (une note par temps, centre à
    self.h + dy), jusqu'à l'autre bord (`rise` : plus haut). Renvoie le temps où l'on se pose."""
    t = beat + 1.0
    for dy in dys:
        L.orb(t, dy, air)
        t += air
    if rise:
        L.cliff(t - 0.75, rise)
    L.gap(beat, t - 0.25 - (beat + 0.2))
    return t


def grotte(L, start, end, jumps=(), hanging=(), up=4.5):
    """Gravité inversée : portail au `start`, on court au plafond (dessous à self.h + up) jusqu'au portail retour au
    `end`. Le plafond est troué aux `jumps` (comme un trou : de j + 0,2 à j + 0,7) ; des ennemis pendus aux `hanging`.
    Le plafond commence 1,5 temps avant le portail et finit 0,5 temps après le retour (comme le niveau 01)."""
    a = start - 1.5
    for j in sorted(jumps):
        L.ceiling(a, j + 0.2 - a, up)
        a = j + 0.7
        L.add(j, "jump")
    L.ceiling(a, end + 0.5 - a, up)
    L.gravity(start, True)
    for h in hanging:
        L.hanging(h, up)
    L.gravity(end, False)
