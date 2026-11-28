"""Stabilisation des cycles des héros v2 (course, montée, descente, mur, attente).

Les planches dessinées (ADR-045) donnent des images justes une à une, mais leurs cycles sautent : la tête monte et
descend de 4 à 9 px, le corps glisse de gauche à droite, des poses accroupies s'intercalent dans la course et
l'ordre des jambes ne suit pas toujours la foulée. Ce module corrige ce qui peut l'être sans redessiner, selon les
règles du pipeline d'animation d'Agent Sprite Forge (video2dsprite : une seule transformation par image, pieds
verrouillés au sol, tête verrouillée, cycle choisi par mesure puis vérifié à l'œil) :

1. **Pieds** : ils restent sur la dernière rangée ; on ne déplace jamais l'image entière vers le haut ou le bas.
2. **Tête, en hauteur** : la tête est ramenée près de la hauteur médiane du cycle en retirant ou en doublant des
   rangées dans les cuisses (comme `pixel_motion.py breath` de Forge, qui retire une rangée des jambes). On garde
   la moitié du rebond d'origine, plafonnée à ±`bob` px : la course rebondit encore, sans sauter. Au plus
   `MAX_ADJUST` rangées par image, jamais voisines : au-delà, la cuisse s'étire et se voit.
3. **Tête, en largeur** : l'image est décalée d'un bloc pour poser le centre de la tête sur la médiane du cycle.
4. **Ordre et nombre d'images inchangés** : mesuré sur les pieds, l'ordre dessiné alterne déjà foulée large et
   foulée serrée, et les images « accroupies » sont les poses basses de la foulée (seulement trop basses). Un tri
   par ressemblance des jambes (essayé) met les foulées larges à la suite et casse le rythme ; retirer les poses
   basses crée deux foulées larges d'affilée.

`python3 tools/heros-v2/stabilize.py --report` mesure les cycles installés sans rien écrire.
"""
import os
import sys

import numpy as np
from PIL import Image

FEET_ROW = 71          # dernière rangée (images 96 × 72, pieds posés dessus)
THIGH_BAND = (10, 22)  # rangées retirées ou doublées : entre 10 et 22 px au-dessus des pieds (cuisses, genoux)
INCOMPLETE = 0.6       # image dont la silhouette fait moins de 60 % de la médiane du cycle : ni mesurée ni recalée
MAX_ADJUST = 2        # au plus 2 rangées retirées ou doublées par image : au-delà, la jambe se voit étirée

# famille -> rebond gardé (px, de part et d'autre de la hauteur médiane de la tête)
CYCLES = {"run": 2, "climb": 1, "descend": 1, "wall": 1, "idle": 1}


def alpha(im):
    return np.asarray(im.getchannel("A")) > 0


def head_top(a):
    rows = np.where(a.sum(axis=1) >= 2)[0]
    return int(rows[0]) if len(rows) else 0


def head_cx(a, top):
    """Centre de la tête : médiane des colonnes opaques entre 6 et 14 px sous le haut (visage, sans les mèches)."""
    band = a[top + 6: top + 14]
    xs = np.where(band)[1]
    return float(np.median(xs)) if len(xs) else a.shape[1] / 2


def shift_x(im, dx):
    if dx == 0:
        return im
    out = Image.new("RGBA", im.size, (0, 0, 0, 0))
    out.paste(im, (dx, 0))
    return out


def _row_cost(arr, y):
    """Coût visuel de retirer ou doubler la rangée y : écart avec la rangée voisine (0 = rangée répétée)."""
    return float(np.abs(arr[y].astype(int) - arr[y - 1].astype(int)).sum())


def adjust_height(im, delta):
    """Monte (delta > 0) ou baisse (delta < 0) tout ce qui est au-dessus des cuisses de |delta| px, pieds fixes.

    Baisser : retirer des rangées de la bande des cuisses. Monter : doubler des rangées de cette bande. On prend
    chaque fois la rangée la plus semblable à sa voisine, pour que la coupure ne se voie pas.
    """
    if delta == 0:
        return im
    arr = np.asarray(im).copy()
    h = arr.shape[0]
    lo, hi = FEET_ROW - THIGH_BAND[1], FEET_ROW - THIGH_BAND[0]
    used = set()
    for _ in range(abs(delta)):
        # jamais deux rangées voisines : deux rangées doublées côte à côte étirent la cuisse
        y = min((r for r in range(lo + 1, hi) if not used & {r - 2, r - 1, r, r + 1, r + 2}),
                key=lambda r: _row_cost(arr, r))
        used.add(y)
        if delta < 0:   # baisser la tête : retirer la rangée y, tout le haut descend d'1 px
            arr = np.concatenate([np.zeros_like(arr[:1]), arr[:y], arr[y + 1:]], axis=0)
        else:           # monter la tête : doubler la rangée y, tout le haut monte d'1 px
            arr = np.concatenate([arr[1:y + 1], arr[y:y + 1], arr[y + 1:]], axis=0)
        assert arr.shape[0] == h
    return Image.fromarray(arr, "RGBA")


def measure(frames):
    tops = [head_top(alpha(im)) for im in frames]
    cxs = [head_cx(alpha(im), t) for im, t in zip(frames, tops)]
    return tops, cxs


def stabilize_cycle(frames, bob=1):
    """Images du cycle, pieds fixes, tête recalée en hauteur (moitié du rebond, ±`bob` px) et en largeur."""
    tops, cxs = measure(frames)
    areas = [int(alpha(im).sum()) for im in frames]
    whole = [a >= INCOMPLETE * float(np.median(areas)) for a in areas]
    if sum(whole) < 3:
        return list(frames)
    tmed = float(np.median([t for t, w in zip(tops, whole) if w]))
    cmed = float(np.median([c for c, w in zip(cxs, whole) if w]))
    out = []
    for im, t, cx, w in zip(frames, tops, cxs, whole):
        if not w:           # silhouette incomplète (corps coupé à l'import) : laissée telle quelle
            out.append(im)
            continue
        target = tmed + max(-bob, min(bob, round((t - tmed) / 2)))
        im = adjust_height(im, max(-MAX_ADJUST, min(MAX_ADJUST, int(round(t - target)))))
        out.append(shift_x(im, int(round(cmed - cx))))
    return out


def families(frames, name):
    """Images `name_0..n` d'un dictionnaire {nom: image}, dans l'ordre."""
    ks = []
    i = 0
    while f"{name}_{i}" in frames:
        ks.append(f"{name}_{i}")
        i += 1
    return ks


def stabilize_hero(frames, log=print, hero=""):
    """Stabilise sur place les cycles d'un héros ({nom: image})."""
    for fam, bob in CYCLES.items():
        ks = families(frames, fam)
        if len(ks) < 3:
            continue
        src = [frames[k] for k in ks]
        before = measure(src)
        out = stabilize_cycle(src, bob)
        after = measure(out)
        for k, im in zip(ks, out):
            frames[k] = im
        log(f"  {hero} {fam} ({len(src)} images) : "
            f"tête {min(before[0])}-{max(before[0])} → {min(after[0])}-{max(after[0])} px, "
            f"centre ±{np.ptp(before[1]) / 2:.1f} → ±{np.ptp(after[1]) / 2:.1f} px")
    return frames


def _load_installed(hero, out_dir):
    import glob
    import re
    frames = {}
    for p in glob.glob(os.path.join(out_dir, f"hero_{hero}_*.png")):
        name = os.path.basename(p)[len(f"hero_{hero}_"):-4]
        if re.fullmatch(r"[a-z]+_\d+", name):
            frames[name] = Image.open(p).convert("RGBA")
    return frames


if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__))
    out_dir = os.path.join(os.path.dirname(os.path.dirname(here)), "game", "Assets", "_Project", "Art", "Kenta", "Gen", "heroes")
    if "--report" not in sys.argv:
        sys.exit("usage : stabilize.py --report  (la stabilisation s'applique dans import.py)")
    for h in ("kenta", "aiko", "ryo"):
        stabilize_hero(_load_installed(h, out_dir), hero=h)
