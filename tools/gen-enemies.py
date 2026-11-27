#!/usr/bin/env python3
"""Danse des ennemis du PDF qui n'ont qu'une image : une version écrasée et une version étirée, comme le djembé.

Règle de la DA (docs/da-kenta.md § 6) : « une danse sur 2 temps ». Le jeu alterne les deux images sur le temps
(`BiomeTheme.enemyFrames`, `FX/BeatFrames`). Pour rester en pixel art net, on ne redimensionne pas : on retire ou on
double des rangées et des colonnes entières, choisies là où le dessin est le plus uni, loin des yeux (pixels blancs) et
des pieds. Les pieds restent sur la dernière rangée (pivot en bas au milieu) et la largeur change d'un nombre pair de
pixels (le milieu ne bouge pas).

    python3 tools/gen-enemies.py   → game/Assets/_Project/Art/Kenta/Pdf/enemy_<nom>_squash.png, enemy_<nom>_stretch.png
"""
import os

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PDF = os.path.join(ROOT, "game", "Assets", "_Project", "Art", "Kenta", "Pdf")
ENEMIES = ["crapaud", "crabe", "golem", "morse", "gargouille", "nuage"]   # le djembé a déjà ses images (PDF)
ROWS = 4                    # rangées retirées (écrasé) ou doublées (étiré)
COLS = 3                    # colonnes ajoutées de chaque côté (écrasé) ou retirées (étiré), symétriques


def load(name):
    return np.array(Image.open(os.path.join(PDF, f"enemy_{name}.png")).convert("RGBA"))


def is_eye(px):
    return px[3] >= 128 and min(px[:3]) >= 225                 # yeux blancs (règle de la DA)


def busy(line):
    """Détail d'une rangée ou d'une colonne : nombre de couleurs différentes, très cher si elle touche un œil."""
    opaque = [tuple(p[:3]) for p in line if p[3] >= 128]
    if not opaque:
        return 1e9
    eye = any(is_eye(p) for p in line)
    return len(set(opaque)) + (1000 if eye else 0)


def pick(scores, lo, hi, n):
    """n indices dans [lo, hi), les plus unis, jamais voisins."""
    order = sorted(range(lo, hi), key=lambda i: (scores[i], abs(i - (lo + hi) / 2)))
    out = []
    for i in order:
        if all(abs(i - j) > 1 for j in out):
            out.append(i)
        if len(out) == n:
            break
    return sorted(out)


def body_rows(a):
    rows = np.nonzero(a[:, :, 3].max(axis=1) >= 128)[0]
    top, bottom = rows.min(), rows.max()
    h = bottom - top + 1
    return top + int(h * 0.3), bottom - max(3, int(h * 0.15))   # pas la tête, pas les pieds


def side_cols(a):
    """Deux bandes de colonnes, une de chaque côté du milieu (pour un changement de largeur symétrique)."""
    cols = np.nonzero(a[:, :, 3].max(axis=0) >= 128)[0]
    left, right = cols.min(), cols.max()
    mid = a.shape[1] // 2
    w = right - left + 1
    return (left + int(w * 0.2), mid - 1), (mid + 1, right - int(w * 0.2) + 1)


def squash(a):
    row_scores = [busy(a[y]) for y in range(a.shape[0])]
    lo, hi = body_rows(a)
    drop = set(pick(row_scores, lo, hi, ROWS))
    out = a[[y for y in range(a.shape[0]) if y not in drop]]
    col_scores = [busy(out[:, x]) for x in range(out.shape[1])]
    (l0, l1), (r0, r1) = side_cols(out)
    dup = set(pick(col_scores, l0, l1, COLS)) | set(pick(col_scores, r0, r1, COLS))
    cols = []
    for x in range(out.shape[1]):
        cols.append(x)
        if x in dup:
            cols.append(x)
    return pad_top(out[:, cols], ROWS)


def stretch(a):
    row_scores = [busy(a[y]) for y in range(a.shape[0])]
    lo, hi = body_rows(a)
    dup = set(pick(row_scores, lo, hi, ROWS))
    rows = []
    for y in range(a.shape[0]):
        rows.append(y)
        if y in dup:
            rows.append(y)
    out = a[rows]
    col_scores = [busy(out[:, x]) for x in range(out.shape[1])]
    (l0, l1), (r0, r1) = side_cols(out)
    drop = set(pick(col_scores, l0, l1, COLS)) | set(pick(col_scores, r0, r1, COLS))
    return out[:, [x for x in range(out.shape[1]) if x not in drop]]


def pad_top(a, n):
    """Garde une hauteur de toile proche de l'original (rangées vides en haut) : même cadrage dans Unity."""
    return np.concatenate([np.zeros((n, a.shape[1], 4), a.dtype), a], axis=0)


def frames(name):
    a = load(name)
    return {"squash": squash(a), "stretch": stretch(a)}


def main():
    for name in ENEMIES:
        for kind, a in frames(name).items():
            Image.fromarray(a, "RGBA").save(os.path.join(PDF, f"enemy_{name}_{kind}.png"))
    print(f"ok : {2 * len(ENEMIES)} images dans {os.path.relpath(PDF, ROOT)}")


if __name__ == "__main__":
    main()
