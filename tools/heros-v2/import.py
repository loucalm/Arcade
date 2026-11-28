#!/usr/bin/env python3
"""Importe et nettoie les sprites v2 des 3 héros (Kenta, Aiko, Ryo) depuis tools/heros-v2/raw/ vers Gen/heroes/.

Pipeline :
  - Suppression du fond gris neutre par flood-fill depuis les bords + nettoyage du halo JPEG.
  - Suppression des lignes de pente (climb/descend), libellés texte, et décors (liane/mur) pour ne garder que le héros.
  - Découpe chirurgicale des planches où des arcs d'épée se chevauchent.
  - Réduction pixel art nette (préservation du contour sombre d'1 px, pas de flou entre contour et couleur).
  - Ajout de frames manquantes pour un jeu complet et homogène sur les 3 héros (172 images) :
      idle_0..5 (6) + idle, run_0..7 (8, plus run_8 pour Ryo), jump_0..7 (8),
      hit_0..5 (6), climb_0..7 (8), descend_0..7 (8), fall_0..2 (3), crouch,
      vine_0..1 (2) + vine, wall, hurt_0..1 (2) + hurt, death.
  - Écriture d'un marqueur Gen/heroes/.v2_<héros> pour empêcher les anciens générateurs d'écraser la v2.

Usage : python3 tools/heros-v2/import.py [dossier_source]
Ensuite, dans Unity : menu Kenta > Installer les sprites des héros et des ennemis
"""
import argparse
import glob
import os
import re
from collections import deque

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
OUT = os.path.join(ROOT, "game", "Assets", "_Project", "Art", "Kenta", "Gen", "heroes")
DEFAULT_SRC = os.path.join(HERE, "raw")
CW, CH = 96, 72
HIT_W = 160
HIT_REACH_PX = 76  # PlayerController.hitReach = 1,9 u × 40 px/u : changer les deux ensemble
INK = (0x14, 0x10, 0x1C)
ARC_CREAM = (0xFF, 0xF6, 0xC8)
ARC_WHITE = (0xFF, 0xFF, 0xFF)
HIT_SLASH = {  # mêmes réglages (a0, a1, épaisseur, estompée) que HIT_KEYS 2 et 3 dans tools/gen-heroes.py
    2: (4, 166, 0.58, False),
    3: (70, 166, 0.34, True),
}
AIRHIT_SLASH = {1: HIT_SLASH[2], 2: HIT_SLASH[3]}  # coup en l'air en 4 images : coupe pleine, puis traînée qui s'efface
# Liane : le jeu tire la corde jusqu'à la main, à VineView.HandHeight = 1,35 u = 54 px au-dessus du pivot. Les images de
# liane sortent sur une toile plus haute, le héros pendu sous ses mains, les pieds sous le pivot ;
# KentaSpritePostprocessor met leur pivot à VINE_PIVOT_PX du bas. Changer les trois ensemble.
VINE_GRIP_PX = 54
VINE_CH = 96
VINE_PIVOT_PX = 24
MARKER_TEXT = "Sprites v2 (tools/heros-v2/import.py) : ne pas écraser\n"

# Échelle de réduction par héros et par planche (hauteur debout homogène : ~52 px Kenta/Ryo, ~49 px Aiko)
SCALES = {
    "kenta": {"idle": 0.180, "run": 0.180, "jump": 0.205, "hit": 0.195, "climb": 0.222, "descend": 0.225, "vw": 0.175, "17": 0.097},
    "aiko":  {"idle": 0.255, "run": 0.242, "jump": 0.260, "hit": 0.235, "climb": 0.315, "descend": 0.315, "vw": 0.240, "17": 0.104},
    "ryo":   {"idle": 0.205, "run": 0.200, "jump": 0.225, "hit": 0.202, "climb": 0.265, "descend": 0.270, "vw": 0.200, "17": 0.097},
}


def load_rgb(src_dir, filename):
    return np.array(Image.open(os.path.join(src_dir, filename)).convert("RGB"))


def flood_bg_mask(rgb, tol=24, max_chroma=18):
    """Détoure le fond gris neutre (bords + poches fermées) et retire le liseré JPEG."""
    h, w, _ = rgb.shape
    samples = np.concatenate([
        rgb[0:10, 0:10].reshape(-1, 3),
        rgb[0:10, -10:].reshape(-1, 3),
        rgb[-10:, 0:10].reshape(-1, 3),
        rgb[-10:, -10:].reshape(-1, 3),
    ], axis=0)
    bg = np.median(samples, axis=0)
    diff = np.abs(rgb.astype(np.int16) - bg).max(axis=-1)
    chroma = rgb.max(axis=-1).astype(np.int16) - rgb.min(axis=-1).astype(np.int16)
    is_bg = (diff <= tol) & (chroma <= max_chroma)

    vis = np.zeros((h, w), dtype=bool)
    q = deque()
    for x in range(w):
        if is_bg[0, x]:
            vis[0, x] = True; q.append((0, x))
        if is_bg[h - 1, x]:
            vis[h - 1, x] = True; q.append((h - 1, x))
    for y in range(1, h - 1):
        if is_bg[y, 0]:
            vis[y, 0] = True; q.append((y, 0))
        if is_bg[y, w - 1]:
            vis[y, w - 1] = True; q.append((y, w - 1))

    # Ajoute aussi comme graines les poches internes franchement gris-fond (diff <= 10, chroma <= 6)
    # pour nettoyer l'espace fermé entre bras/jambes et liane/mur sans toucher les détails du héros.
    inner_seed = (diff <= 10) & (chroma <= 6) & ~vis
    sy, sx = np.nonzero(inner_seed)
    for y, x in zip(sy.tolist(), sx.tolist()):
        if not vis[y, x]:
            vis[y, x] = True
            q.append((y, x))

    while q:
        y, x = q.popleft()
        if y > 0 and not vis[y - 1, x] and is_bg[y - 1, x]:
            vis[y - 1, x] = True; q.append((y - 1, x))
        if y + 1 < h and not vis[y + 1, x] and is_bg[y + 1, x]:
            vis[y + 1, x] = True; q.append((y + 1, x))
        if x > 0 and not vis[y, x - 1] and is_bg[y, x - 1]:
            vis[y, x - 1] = True; q.append((y, x - 1))
        if x + 1 < w and not vis[y, x + 1] and is_bg[y, x + 1]:
            vis[y, x + 1] = True; q.append((y, x + 1))

    fg = ~vis
    for _ in range(2):
        pad = np.pad(fg, 1, constant_values=False)
        touches_bg = fg & ~(pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:])
        luma = rgb.mean(axis=-1)
        halo = touches_bg & (diff <= 42) & (chroma <= 20) & (luma > 70)
        if not halo.any():
            break
        fg[halo] = False
    return fg


def keep_main_components(fg, min_area=1200):
    """Garde uniquement les composantes connexes d'au moins `min_area` pixels (supprime textes et miettes)."""
    h, w = fg.shape
    h2, w2 = h // 2, w // 2
    if h2 == 0 or w2 == 0:
        return fg
    fg2 = fg[:h2 * 2, :w2 * 2].reshape(h2, 2, w2, 2).any(axis=(1, 3))
    lab = np.zeros((h2, w2), dtype=np.int32)
    cid = 0
    keep = set()
    for y in range(h2):
        for x in range(w2):
            if fg2[y, x] and lab[y, x] == 0:
                cid += 1
                q = deque([(y, x)])
                lab[y, x] = cid
                cnt = 0
                while q:
                    cy, cx = q.popleft()
                    cnt += 1
                    for dy in (-1, 0, 1):
                        for dx in (-1, 0, 1):
                            if dy == 0 and dx == 0:
                                continue
                            ny, nx = cy + dy, cx + dx
                            if 0 <= ny < h2 and 0 <= nx < w2 and fg2[ny, nx] and lab[ny, nx] == 0:
                                lab[ny, nx] = cid
                                q.append((ny, nx))
                if cnt * 4 >= min_area:
                    keep.add(cid)
    mask2 = np.isin(lab, list(keep))
    out = np.zeros_like(fg)
    out[:h2 * 2, :w2 * 2] = np.repeat(np.repeat(mask2, 2, axis=0), 2, axis=1)
    return fg & out


def keep_largest_component(fg):
    """Ne garde que la plus grande composante connexe d'une vignette."""
    h, w = fg.shape
    h2, w2 = max(1, h // 2), max(1, w // 2)
    fg2 = fg[:h2 * 2, :w2 * 2].reshape(h2, 2, w2, 2).any(axis=(1, 3))
    lab = np.zeros((h2, w2), dtype=np.int32)
    cid = 0
    best_id, best_cnt = 0, 0
    for y in range(h2):
        for x in range(w2):
            if fg2[y, x] and lab[y, x] == 0:
                cid += 1
                q = deque([(y, x)])
                lab[y, x] = cid
                cnt = 0
                while q:
                    cy, cx = q.popleft()
                    cnt += 1
                    for dy in (-1, 0, 1):
                        for dx in (-1, 0, 1):
                            if dy == 0 and dx == 0:
                                continue
                            ny, nx = cy + dy, cx + dx
                            if 0 <= ny < h2 and 0 <= nx < w2 and fg2[ny, nx] and lab[ny, nx] == 0:
                                lab[ny, nx] = cid
                                q.append((ny, nx))
                if cnt > best_cnt:
                    best_cnt, best_id = cnt, cid
    if best_id == 0:
        return fg
    mask2 = (lab == best_id)
    out = np.zeros_like(fg)
    out[:h2 * 2, :w2 * 2] = np.repeat(np.repeat(mask2, 2, axis=0), 2, axis=1)
    return fg & out


def strip_slope_line(rgb, fg):
    """Retire la fine ligne de pente grise (1-5 px d'épaisseur) sous les pieds dans climb/descend."""
    h, w = fg.shape
    chroma = rgb.max(axis=-1).astype(np.int16) - rgb.min(axis=-1).astype(np.int16)
    luma = rgb.mean(axis=-1)
    thin_grey = fg & (chroma < 18) & (luma > 60) & (luma < 155)
    # Un pixel de ligne de sol a peu de voisins verticaux solides (< 6 px dans une fenêtre verticale de 11 px)
    csum = np.zeros((h + 1, w), dtype=np.int32)
    csum[1:] = np.cumsum(fg.astype(np.int32), axis=0)
    y_top = np.maximum(0, np.arange(h) - 5)
    y_bot = np.minimum(h, np.arange(h) + 6)
    vthick = csum[y_bot] - csum[y_top]
    line_px = thin_grey & (vthick <= 6)
    cleaned = fg & ~line_px
    # Si la ligne reste reliée uniquement par 1-2 px foncés sous une semelle, on nettoie aussi le bord inférieur fin
    csum2 = np.zeros((h + 1, w), dtype=np.int32)
    csum2[1:] = np.cumsum(cleaned.astype(np.int32), axis=0)
    vthick2 = csum2[y_bot] - csum2[y_top]
    cleaned &= ~(fg & (vthick2 <= 4))
    return cleaned


def downsample_crisp(rgb, fg, scale):
    """Réduit le sprite au facteur `scale` en gardant des contours nets d'1 px et zéro bavure."""
    ys, xs = np.nonzero(fg)
    if len(xs) == 0:
        return Image.new("RGBA", (1, 1), (0, 0, 0, 0))
    y0, y1 = ys.min(), ys.max() + 1
    x0, x1 = xs.min(), xs.max() + 1
    sub_rgb = rgb[y0:y1, x0:x1]
    sub_fg = fg[y0:y1, x0:x1]
    sh, sw = sub_fg.shape
    nh = max(1, int(round(sh * scale)))
    nw = max(1, int(round(sw * scale)))

    out = np.zeros((nh, nw, 4), dtype=np.uint8)
    luma = sub_rgb.mean(axis=-1)
    cov = np.zeros((nh, nw), dtype=np.float32)
    dark_frac = np.zeros((nh, nw), dtype=np.float32)
    cells = [[None] * nw for _ in range(nh)]
    for oy in range(nh):
        sy0 = int(oy * sh / nh)
        sy1 = max(sy0 + 1, int((oy + 1) * sh / nh))
        for ox in range(nw):
            sx0 = int(ox * sw / nw)
            sx1 = max(sx0 + 1, int((ox + 1) * sw / nw))
            m = sub_fg[sy0:sy1, sx0:sx1]
            c = float(m.mean())
            cov[oy, ox] = c
            if c > 0:
                px_rgb = sub_rgb[sy0:sy1, sx0:sx1][m]
                px_lum = luma[sy0:sy1, sx0:sx1][m]
                cells[oy][ox] = (px_rgb, px_lum)
                dark_frac[oy, ox] = float((px_lum < 68).mean())

    solid = (cov >= 0.42) | ((cov >= 0.22) & (dark_frac >= 0.45))
    pad = np.pad(solid, 1, constant_values=False)
    is_edge = solid & ~(pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:])

    for oy in range(nh):
        for ox in range(nw):
            if not solid[oy, ox] or cells[oy][ox] is None:
                continue
            px_rgb, px_lum = cells[oy][ox]
            dark = px_lum < 68
            if is_edge[oy, ox] and dark.mean() >= 0.20:
                col = np.median(px_rgb[dark], axis=0)
            elif dark.mean() >= 0.50:
                col = np.median(px_rgb[dark], axis=0)
            else:
                fill = px_rgb[~dark] if (~dark).any() else px_rgb
                col = np.median(fill, axis=0)
            out[oy, ox, :3] = np.clip(np.round(col), 0, 255).astype(np.uint8)
            out[oy, ox, 3] = 255

    # Supprime les pixels isolés (4-voisinage)
    s2 = out[:, :, 3] > 0
    p2 = np.pad(s2, 1, constant_values=False)
    nb = p2[:-2, 1:-1].astype(int) + p2[2:, 1:-1] + p2[1:-1, :-2] + p2[1:-1, 2:]
    out[s2 & (nb == 0), 3] = 0
    return Image.fromarray(out, "RGBA")


def remove_small_islands(im, min_pixels=6):
    """Supprime les petits îlots isolés (< min_pixels) après réduction (miettes de liane ou d'arc)."""
    a = np.array(im)
    alpha = a[:, :, 3] >= 128
    h, w = alpha.shape
    lab = np.zeros((h, w), dtype=np.int32)
    cid = 0
    keep = set()
    for y in range(h):
        for x in range(w):
            if alpha[y, x] and lab[y, x] == 0:
                cid += 1
                q = deque([(y, x)])
                lab[y, x] = cid
                cnt = 0
                while q:
                    cy, cx = q.popleft()
                    cnt += 1
                    for dy in (-1, 0, 1):
                        for dx in (-1, 0, 1):
                            if dy == 0 and dx == 0:
                                continue
                            ny, nx = cy + dy, cx + dx
                            if 0 <= ny < h and 0 <= nx < w and alpha[ny, nx] and lab[ny, nx] == 0:
                                lab[ny, nx] = cid
                                q.append((ny, nx))
                if cnt >= min_pixels:
                    keep.add(cid)
    a[~np.isin(lab, list(keep)), 3] = 0
    return Image.fromarray(a, "RGBA")


def place_in_canvas(spr, dy=0, dx=0, cw=CW, ch=CH, min_island=8):
    """Centre horizontalement et pose sur le bas du canevas (pivot (0.5, 0)), avec décalage vertical `dy` (vers le haut)."""
    if min_island > 0:
        spr = remove_small_islands(spr, min_pixels=min_island)
    canvas = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
    sw, sh = spr.size
    if sw > cw - 2 or sh > ch - 2:
        s = min((cw - 2) / sw, (ch - 2) / sh)
        spr = spr.resize((max(1, int(sw * s)), max(1, int(sh * s))), Image.NEAREST)
        sw, sh = spr.size
    x = max(1, min(cw - sw - 1, (cw - sw) // 2 + dx))
    y = max(1, min(ch - sh, ch - sh - dy))
    canvas.alpha_composite(spr, (x, y))
    return canvas


def dilate4(mask):
    """Dilatation 4-connexe d'un pixel (contour extérieur)."""
    d = mask.copy()
    d[1:] |= mask[:-1]
    d[:-1] |= mask[1:]
    d[:, 1:] |= mask[:, :-1]
    d[:, :-1] |= mask[:, 1:]
    return d


def detect_arc_cy(hero):
    """Centre vertical (px au-dessus des pieds) des pixels clairs de l'arc devant le héros, sinon 31."""
    alpha = hero[:, :, 3] >= 128
    rgb = hero[:, :, :3].astype(np.int16)
    luma = rgb.mean(axis=-1)
    chroma = rgb.max(axis=-1) - rgb.min(axis=-1)
    xs = np.arange(HIT_W)[None, :]
    bright = alpha & (xs >= HIT_W // 2 + 18) & (luma >= 185) & (chroma <= 35)
    ys = np.nonzero(bright)[0]
    if len(ys) < 12:
        return 31.0
    cy = float(CH - (np.median(ys) + 0.5))
    return float(np.clip(cy, 22.0, 48.0))


def slash_mask(a0, a1, tmax, cy=31.0, target_x=None, exp=0.65):
    """Traînée elliptique du katana (formule de hero_rig.slash_mask) sur une toile HIT_W × CH."""
    cols = np.arange(HIT_W, dtype=np.float64) + 0.5 - HIT_W / 2.0
    rows = CH - (np.arange(CH, dtype=np.float64) + 0.5)
    X, Y = np.meshgrid(cols, rows)
    cx = 6.0
    rx = HIT_REACH_PX - cx
    ry = min(29.0, cy - 2.0, CH - 2.0 - cy)
    u, v = (X - cx) / rx, (Y - cy) / ry
    rho = np.sqrt(u * u + v * v)
    ang = np.degrees(np.arctan2(u, v)) % 360.0
    span = max(1e-6, (a1 - a0) % 360.0)
    t = ((ang - a0) % 360.0) / span
    inside = t <= 1.0
    eff_tmax = tmax
    if target_x is not None:
        t90 = ((90.0 - a0) % 360.0) / span
        if 0.0 < t90 <= 1.0:
            rho_in = float(np.clip((target_x - cx) / rx, 0.08, 0.85))
            eff_tmax = max(eff_tmax, (1.0 - rho_in) / (t90 ** exp))
    thick = np.minimum(0.95, eff_tmax * np.clip(t, 0.0, 1.0) ** exp)
    outer = inside & (rho <= 1.0) & (rho >= 1.0 - thick) & (thick > 0.035)
    core = outer & (rho >= 1.0 - np.minimum(thick, tmax) * 0.45)
    return outer, core


def widen_hit_frame(im, frame_idx):
    """Étend hit_0..5 en 160×72 (+32 px transparents par côté, pivot inchangé) et trace la traînée derrière le héros."""
    pad = (HIT_W - CW) // 2
    canvas = Image.new("RGBA", (HIT_W, CH), (0, 0, 0, 0))
    canvas.alpha_composite(im, (pad, 0))
    if frame_idx in HIT_SLASH:
        return draw_slash(canvas, *HIT_SLASH[frame_idx])
    return canvas


def draw_slash(canvas, a0, a1, tmax, faded):
    """Trace la traînée du katana derrière le héros d'une toile HIT_W × CH, jusqu'à HIT_REACH_PX devant le pivot.
    Pleine : elle part de l'avant du héros (2 px de chevauchement, sans trou). Estompée : croissant fin, sans comblement."""
    hero = np.asarray(canvas)
    hero_opaque = hero[:, :, 3] > 0
    if faded:
        # croissant fin de la marionnette (HIT_KEYS 3 tel quel), sans comblement ni épaississement
        outer, core = slash_mask(a0, a1, tmax, cy=31.0, target_x=None, exp=0.9)
    else:
        main_alpha = np.asarray(remove_small_islands(canvas, min_pixels=6))[:, :, 3] >= 128
        main_right = np.full(CH, -1, dtype=np.int32)
        for y in range(CH):
            xs = np.nonzero(main_alpha[y])[0]
            if len(xs) > 0:
                main_right[y] = int(xs.max())

        cy = detect_arc_cy(hero)
        ry = min(29.0, cy - 2.0, CH - 2.0 - cy)
        row_cy = int(round(CH - 0.5 - cy))
        band_rights = [main_right[y] for y in range(max(0, row_cy - 6), min(CH, row_cy + 7)) if main_right[y] >= 0]
        target_x = None
        if band_rights:
            front_col = float(np.median(band_rights))
            target_x = (front_col - 2.0) + 0.5 - HIT_W / 2.0

        outer, core = slash_mask(a0, a1, tmax, cy=cy, target_x=target_x)

        bridge_y0 = max(0, int(round(row_cy - ry * 0.55)))
        bridge_y1 = min(CH - 1, int(round(row_cy + ry * 0.85)))
        for y in range(CH):
            h_right = int(main_right[y])
            if h_right < 0 or not outer[y].any():
                continue
            c_cols = np.nonzero(outer[y])[0]
            c_left = int(c_cols.min())
            overlap = max(0, h_right - 2)
            if c_left < overlap:
                outer[y, :overlap] = False
                core[y, :overlap] = False
            elif bridge_y0 <= y <= bridge_y1 and c_left > overlap:
                outer[y, overlap:c_left] = True

    arr = np.zeros((CH, HIT_W, 4), dtype=np.uint8)
    outline = dilate4(outer) & ~outer & ~hero_opaque
    arr[outline, :3] = INK
    arr[outline, 3] = 255
    arr[outer, :3] = ARC_CREAM
    arr[outer, 3] = 255
    if not faded:
        arr[core, :3] = ARC_WHITE
    out = Image.fromarray(arr, "RGBA")
    out.alpha_composite(canvas)
    return out


def split_row_by_valleys(fg_row, expected_n, min_gap_frac=0.35):
    col = fg_row.sum(axis=0)
    xs = np.where(col > 0)[0]
    if len(xs) == 0:
        return []
    x_min, x_max = int(xs[0]), int(xs[-1]) + 1
    step = (x_max - x_min) / expected_n
    cuts = [x_min]
    for i in range(1, expected_n):
        est = x_min + i * step
        lo = max(cuts[-1] + 20, int(est - step * min_gap_frac))
        hi = min(x_max - 20, int(est + step * min_gap_frac))
        mx = lo + int(np.argmin(col[lo:hi]))
        cuts.append(mx)
    cuts.append(x_max)
    return [(cuts[i], cuts[i + 1]) for i in range(expected_n)]


def extract_grid(rgb, fg, row_bounds, counts, scale, keep_biggest=True, dys=None):
    frames = []
    idx = 0
    for (y0, y1), n in zip(row_bounds, counts):
        ranges = split_row_by_valleys(fg[y0:y1], n)
        for x0, x1 in ranges:
            sub_fg = fg[y0:y1, x0:x1]
            if keep_biggest:
                sub_fg = keep_largest_component(sub_fg)
            spr = downsample_crisp(rgb[y0:y1, x0:x1], sub_fg, scale)
            dy = dys[idx] if dys and idx < len(dys) else 0
            frames.append(place_in_canvas(spr, dy=dy))
            idx += 1
    return frames


def extract_custom_boxes(rgb, fg, boxes, scale, keep_biggest=False, dys=None):
    frames = []
    for i, b in enumerate(boxes):
        x0, y0, x1, y1 = b["box"]
        sub_fg = fg[y0:y1, x0:x1].copy()
        if "exclude" in b:
            for ex0, ey0, ex1, ey1 in b["exclude"]:
                sub_fg[max(0, ey0 - y0):max(0, ey1 - y0), max(0, ex0 - x0):max(0, ex1 - x0)] = False
        if "exclude_cond" in b:
            sub_rgb = rgb[y0:y1, x0:x1]
            sub_fg &= ~b["exclude_cond"](sub_rgb, x0, y0)
        if keep_biggest:
            sub_fg = keep_largest_component(sub_fg)
        spr = downsample_crisp(rgb[y0:y1, x0:x1], sub_fg, scale)
        dy = dys[i] if dys and i < len(dys) else 0
        frames.append(place_in_canvas(spr, dy=dy))
    return frames


def synth_inbetween(im_a, im_b, shift_y=0):
    """Synthétise une frame intermédiaire nette (sans demi-transparence) entre im_a et im_b."""
    a = np.array(im_a)
    b = np.array(im_b)
    # On prend les pixels où au moins l'un est opaque, et en cas de recouvrement on alterne proprement
    # pour créer une pose intermédiaire nette avec contour d'1 px conservé
    aa = a[:, :, 3] >= 128
    ba = b[:, :, 3] >= 128
    # Interpolation de silhouette : pixels dans A∩B + moitié des pixels exclusifs (dépendants de la parité)
    h, w = aa.shape
    yy, xx = np.mgrid[0:h, 0:w]
    both = aa & ba
    only_a = aa & ~ba
    only_b = ba & ~aa
    # Prend les pixels proches du tronc commun (dilatation d'1 px de both) + damier doux pour mouvement fluide
    pad = np.pad(both, 1, constant_values=False)
    near_both = pad[:-2, 1:-1] | pad[2:, 1:-1] | pad[1:-1, :-2] | pad[1:-1, 2:]
    mask = both | ((only_a | only_b) & near_both)
    out = np.zeros_like(a)
    out[both] = a[both]
    sel_a = (only_a & mask) & ((xx + yy) % 2 == 0)
    sel_b = (only_b & mask) & ~sel_a
    out[sel_a] = a[sel_a]
    out[sel_b] = b[sel_b]
    # Bouche les trous internes d'1 px
    s = out[:, :, 3] >= 128
    p = np.pad(s, 1, constant_values=False)
    nb = p[:-2, 1:-1].astype(int) + p[2:, 1:-1] + p[1:-1, :-2] + p[1:-1, 2:]
    fill_hole = (~s) & (nb >= 3) & (aa | ba)
    out[fill_hole] = np.where(aa[fill_hole][..., None], a[fill_hole], b[fill_hole])
    if shift_y != 0:
        out = np.roll(out, shift_y, axis=0)
        if shift_y < 0:
            out[shift_y:] = 0
        else:
            out[:shift_y] = 0
    return Image.fromarray(out, "RGBA")


def synth_flutter_variant(im, tail_dx=-1, tail_dy=-1, body_dy=0):
    """Crée une frame d'animation subtile (bandeau/cheveux qui flottent + léger décalage) pour boucles idle/fall/hurt/vine."""
    a = np.array(im).copy()
    h, w, _ = a.shape
    ys, xs = np.nonzero(a[:, :, 3] >= 128)
    if len(xs) == 0:
        return im.copy()
    y_top, y_bot = int(ys.min()), int(ys.max())
    x_mid = int(np.median(xs))
    # Décale la zone arrière-haute (pans du bandeau / pointes de cheveux à gauche du milieu)
    sub = a.copy()
    z_y1 = y_top + (y_bot - y_top) // 2
    z_x1 = max(2, x_mid - 2)
    region = a[max(0, y_top):z_y1, 1:z_x1].copy()
    sub[max(0, y_top):z_y1, 1:z_x1] = 0
    ny0 = max(1, y_top + tail_dy)
    nx0 = max(1, 1 + tail_dx)
    rh, rw = region.shape[:2]
    ny1 = min(h - 1, ny0 + rh)
    nx1 = min(w - 1, nx0 + rw)
    m = region[:ny1 - ny0, :nx1 - nx0, 3] >= 128
    sub[ny0:ny1, nx0:nx1][m] = region[:ny1 - ny0, :nx1 - nx0][m]
    if body_dy != 0:
        # Ne déplace jamais les pieds hors du bas si déjà au sol
        sub = np.roll(sub, body_dy, axis=0)
        if body_dy < 0:
            sub[body_dy:] = 0
        else:
            sub[:body_dy] = 0
    return Image.fromarray(sub, "RGBA")


def strip_green_vine(rgb, fg, y_top, y_bot):
    """Retire la liane verte et ses contours au-dessus/en dessous du héros et entre ses membres."""
    h, w = fg.shape
    r, g, b = rgb[:, :, 0].astype(np.int16), rgb[:, :, 1].astype(np.int16), rgb[:, :, 2].astype(np.int16)
    luma = rgb.mean(axis=-1)
    green = fg & (g > r + 7) & (g > b + 9)
    # Dilate le vert de 3 px pour emporter le contour noir propre à la liane, uniquement sur les pixels très sombres
    dil = green.copy()
    for _ in range(3):
        p = np.pad(dil, 1, constant_values=False)
        dil = p[:-2, 1:-1] | p[2:, 1:-1] | p[1:-1, :-2] | p[1:-1, 2:] | dil
    cleaned = fg & ~green & ~(dil & (luma < 65))
    # Coupe net au-dessus de la tête/main haute et sous les chaussures
    cleaned[:max(0, y_top), :] = False
    cleaned[min(h, y_bot):, :] = False
    return keep_largest_component(cleaned)


def strip_stone_wall(rgb, fg, wall_x_start, hand_y_range, foot_y_range):
    """Retire le mur de pierre gris à droite de wall_pose en gardant les doigts/pieds qui s'y agrippent."""
    h, w = fg.shape
    yy, xx = np.mgrid[0:h, 0:w]
    chroma = rgb.max(axis=-1).astype(np.int16) - rgb.min(axis=-1).astype(np.int16)
    luma = rgb.mean(axis=-1)
    in_grip = ((yy >= hand_y_range[0]) & (yy <= hand_y_range[1])) | ((yy >= foot_y_range[0]) & (yy <= foot_y_range[1]))
    # Tout ce qui est à droite de wall_x_start hors prises, ou pierre grise sur la bordure des prises
    stone = (xx >= wall_x_start) & (~in_grip | ((chroma < 16) & (luma > 75)))
    stone |= (xx >= wall_x_start + 12)
    return keep_largest_component(fg & ~stone)


def build_kenta(src_dir):
    sc = SCALES["kenta"]
    res = {}

    # 1. Run (8)
    rgb = load_rgb(src_dir, "image.jpeg")
    fg = keep_main_components(flood_bg_mask(rgb))
    run = extract_grid(rgb, fg, [(0, 370), (370, 768)], [4, 4], sc["run"])
    for k, im in enumerate(run):
        res[f"run_{k}"] = im

    # 2. Jump (8) + Fall (3) + Crouch (1)
    rgb2 = load_rgb(src_dir, "image2.jpeg")
    fg2 = keep_main_components(flood_bg_mask(rgb2))
    jump = extract_grid(rgb2, fg2, [(0, 373)], [8], sc["jump"], dys=[0, 4, 8, 10, 12, 10, 6, 0])
    for k, im in enumerate(jump):
        res[f"jump_{k}"] = im
    bot2 = extract_grid(rgb2, fg2, [(373, 768)], [3], sc["jump"])
    res["fall_0"] = bot2[0]
    res["fall_1"] = bot2[1]
    res["fall_2"] = synth_flutter_variant(bot2[1], tail_dx=1, tail_dy=-1)
    res["crouch"] = bot2[2]

    # 3. Hit (6) - séparation chirurgicale de hit_2 et hit_3
    rgb3 = load_rgb(src_dir, "image3.jpeg")
    fg3 = keep_main_components(flood_bg_mask(rgb3))
    hit_boxes = [
        {"box": (30, 230, 250, 535)},
        {"box": (250, 230, 478, 535)},
        {   # hit_2 : arc blanc large jusqu'à x=806, épée jusqu'à x=798 ; exclure tête/corps/pieds/épée haute de hit_3
            "box": (480, 230, 810, 535),
            "exclude": [(780, 330, 810, 535), (722, 460, 780, 535)],
            "exclude_cond": lambda r, x0, y0: (
                ((np.arange(r.shape[0])[:, None] + y0 < 342)
                 & (np.arange(r.shape[1])[None, :] + x0 > 662)
                 & (r.mean(axis=-1) < 238))
            ),
        },
        {   # hit_3 : lame haute jusqu'à x=665 ; exclure cheveux/chaussure/arc/lame de hit_2 à gauche
            "box": (665, 230, 945, 535),
            "exclude": [(665, 265, 706, 535), (665, 345, 777, 480)],
            "exclude_cond": lambda r, x0, y0: (
                ((np.arange(r.shape[1])[None, :] + x0 < 796)
                 & (np.arange(r.shape[0])[:, None] + y0 >= 338)
                 & (np.arange(r.shape[0])[:, None] + y0 <= 485)
                 & ((r.mean(axis=-1) > 195) | (np.arange(r.shape[1])[None, :] + x0 < 782)))
                | ((np.arange(r.shape[1])[None, :] + x0 < 765)
                   & (np.arange(r.shape[0])[:, None] + y0 < 338)
                   & (r.mean(axis=-1) > 230))
            ),
        },
        {"box": (935, 230, 1135, 535)},
        {"box": (1135, 230, 1365, 535)},
    ]
    hits = extract_custom_boxes(rgb3, fg3, hit_boxes, sc["hit"], keep_biggest=True)
    for k, im in enumerate(hits):
        res[f"hit_{k}"] = widen_hit_frame(im, k)

    # 4. Climb (7 -> 8) & Descend (8)
    rgb12 = load_rgb(src_dir, "image12.jpeg")
    fg12 = keep_main_components(strip_slope_line(rgb12, flood_bg_mask(rgb12)), min_area=1500)
    climb = extract_grid(rgb12, fg12, [(0, 356)], [7], sc["climb"])
    for k, im in enumerate(climb):
        res[f"climb_{k}"] = im
    res["climb_7"] = synth_inbetween(climb[6], climb[0])
    descend = extract_grid(rgb12, fg12, [(356, 768)], [8], sc["descend"])
    for k, im in enumerate(descend):
        res[f"descend_{k}"] = im

    # 5. Idle (5 -> 6), Vine (2), Wall (1)
    rgb14 = load_rgb(src_dir, "image14.jpeg")
    fg14 = keep_main_components(flood_bg_mask(rgb14), min_area=1500)
    fg14_idle = fg14.copy()
    fg14_idle[:, 710:] = False
    idles = extract_grid(rgb14, fg14_idle, [(0, 385), (385, 768)], [3, 2], sc["idle"])
    for k, im in enumerate(idles):
        res[f"idle_{k}"] = im
    res["idle_5"] = synth_inbetween(idles[4], idles[0])
    res["idle"] = res["idle_0"]

    fg14_v = strip_green_vine(rgb14[:, 730:960], fg14[:, 730:960], y_top=142, y_bot=595)
    vine0 = place_in_canvas(downsample_crisp(rgb14[:, 730:960], fg14_v, sc["vw"]))
    res["vine_0"] = vine0
    res["vine_1"] = synth_flutter_variant(vine0, tail_dx=-1, tail_dy=1)
    res["vine"] = vine0

    fg14_w = strip_stone_wall(rgb14[:, 1080:1320], fg14[:, 1080:1320], wall_x_start=1246 - 1080,
                              hand_y_range=(185, 250), foot_y_range=(395, 535))
    res["wall"] = place_in_canvas(downsample_crisp(rgb14[:, 1080:1320], fg14_w, sc["vw"]))

    # 6. Hurt (2) & Death (1) depuis 17.jpeg
    rgb17 = load_rgb(src_dir, "17.jpeg")
    fg17 = keep_main_components(flood_bg_mask(rgb17), min_area=4000)
    hurt0 = place_in_canvas(downsample_crisp(rgb17[:550, 150:570], keep_largest_component(fg17[:550, 150:570]), sc["17"]))
    hurt1 = place_in_canvas(downsample_crisp(rgb17[:550, 570:1020], keep_largest_component(fg17[:550, 570:1020]), sc["17"]))
    res["hurt_0"] = hurt0
    res["hurt_1"] = hurt1
    res["hurt"] = hurt0
    res["death"] = place_in_canvas(downsample_crisp(rgb17[:520, 1450:2450], fg17[:520, 1450:2450], sc["17"]), min_island=3)

    return res


def build_aiko(src_dir):
    sc = SCALES["aiko"]
    res = {}

    # 1. Run (7 -> 8)
    rgb4 = load_rgb(src_dir, "image4.jpeg")
    fg4 = keep_main_components(flood_bg_mask(rgb4))
    run = extract_grid(rgb4, fg4, [(0, 768)], [7], sc["run"])
    for k, im in enumerate(run):
        res[f"run_{k}"] = im
    res["run_7"] = synth_inbetween(run[6], run[0])

    # 2. Jump (8) + Fall (3) + Crouch (1)
    rgb6 = load_rgb(src_dir, "image6.jpeg")
    fg6 = keep_main_components(flood_bg_mask(rgb6))
    jump = extract_grid(rgb6, fg6, [(0, 340)], [8], sc["jump"], dys=[0, 4, 8, 10, 12, 10, 6, 0])
    for k, im in enumerate(jump):
        res[f"jump_{k}"] = im
    bot6 = extract_grid(rgb6, fg6, [(340, 768)], [3], sc["jump"])
    res["fall_0"] = bot6[0]
    res["fall_1"] = bot6[1]
    res["fall_2"] = synth_flutter_variant(bot6[1], tail_dx=1, tail_dy=-1)
    res["crouch"] = bot6[2]

    # 3. Hit (6)
    rgb7 = load_rgb(src_dir, "image7.jpeg")
    fg7 = keep_main_components(flood_bg_mask(rgb7))
    hit_boxes = [
        {"box": (20, 200, 232, 510)},
        {"box": (235, 200, 448, 510)},
        {"box": (455, 200, 709, 510)},
        {   # hit_3 : exclure le bout d'arc résiduel à gauche et le corps de hit_4 à droite
            "box": (706, 200, 1015, 510),
            "exclude": [(706, 200, 725, 295), (962, 240, 1015, 355), (962, 412, 1015, 510)],
            "exclude_cond": lambda r, x0, y0: (
                (np.arange(r.shape[1])[None, :] + x0 >= 962) & (r.mean(axis=-1) < 110)
            ),
        },
        {   # hit_4 : exclure l'arc & lame de hit_3 à gauche de x=1005
            "box": (955, 200, 1142, 510),
            "exclude": [(955, 250, 995, 360)],
            "exclude_cond": lambda r, x0, y0: (
                (np.arange(r.shape[1])[None, :] + x0 < 1005)
                & (np.arange(r.shape[0])[:, None] + y0 >= 360)
                & (np.arange(r.shape[0])[:, None] + y0 <= 394)
                & (r.mean(axis=-1) > 140)
            ),
        },
        {"box": (1165, 200, 1355, 510)},
    ]
    hits = extract_custom_boxes(rgb7, fg7, hit_boxes, sc["hit"], keep_biggest=True)
    for k, im in enumerate(hits):
        res[f"hit_{k}"] = widen_hit_frame(im, k)

    # 4. Climb (8) & Descend (8)
    rgb11 = load_rgb(src_dir, "iage11.jpeg")
    fg11 = keep_main_components(strip_slope_line(rgb11, flood_bg_mask(rgb11)), min_area=1200)
    climb = extract_grid(rgb11, fg11, [(0, 359)], [8], sc["climb"])
    for k, im in enumerate(climb):
        res[f"climb_{k}"] = im
    descend = extract_grid(rgb11, fg11, [(359, 768)], [8], sc["descend"])
    for k, im in enumerate(descend):
        res[f"descend_{k}"] = im

    # 5. Idle (5 -> 6), Vine (2), Wall (1)
    rgb15 = load_rgb(src_dir, "image15.jpeg")
    fg15 = keep_main_components(flood_bg_mask(rgb15), min_area=1200)
    fg15_idle = fg15.copy()
    fg15_idle[:, 780:] = False
    fg15_idle[475:, :] = False
    idles = extract_grid(rgb15, fg15_idle, [(0, 475)], [5], sc["idle"])
    for k, im in enumerate(idles):
        res[f"idle_{k}"] = im
    res["idle_5"] = synth_inbetween(idles[4], idles[0])
    res["idle"] = res["idle_0"]

    fg15_v = strip_green_vine(rgb15[:520, 825:990], fg15[:520, 825:990], y_top=188, y_bot=468)
    vine0 = place_in_canvas(downsample_crisp(rgb15[:520, 825:990], fg15_v, sc["vw"]))
    res["vine_0"] = vine0
    res["vine_1"] = synth_flutter_variant(vine0, tail_dx=-1, tail_dy=1)
    res["vine"] = vine0

    fg15_w = strip_stone_wall(rgb15[:520, 1130:1335], fg15[:520, 1130:1335], wall_x_start=1244 - 1130,
                              hand_y_range=(215, 270), foot_y_range=(395, 455))
    res["wall"] = place_in_canvas(downsample_crisp(rgb15[:520, 1130:1335], fg15_w, sc["vw"]))

    # 6. Hurt (1 -> 2) & Death (1)
    rgb17 = load_rgb(src_dir, "17.jpeg")
    fg17 = keep_main_components(flood_bg_mask(rgb17), min_area=4000)
    hurt0 = place_in_canvas(downsample_crisp(rgb17[550:1010, 450:1050], keep_largest_component(fg17[550:1010, 450:1050]), sc["17"]))
    res["hurt_0"] = hurt0
    res["hurt_1"] = synth_flutter_variant(hurt0, tail_dx=-1, tail_dy=1, body_dy=-1)
    res["hurt"] = hurt0
    res["death"] = place_in_canvas(downsample_crisp(rgb17[550:990, 1450:2450], fg17[550:990, 1450:2450], sc["17"]), min_island=3)

    return res


def build_ryo(src_dir):
    sc = SCALES["ryo"]
    res = {}

    # 1. Run (9)
    rgb5 = load_rgb(src_dir, "image5.jpeg")
    fg5 = keep_main_components(flood_bg_mask(rgb5))
    run = extract_grid(rgb5, fg5, [(0, 370), (370, 768)], [5, 4], sc["run"])
    for k, im in enumerate(run):
        res[f"run_{k}"] = im

    # 2. Jump (8) + Fall (3) + Crouch (1)
    rgb8 = load_rgb(src_dir, "image8.jpeg")
    fg8 = keep_main_components(flood_bg_mask(rgb8))
    jump = extract_grid(rgb8, fg8, [(0, 362)], [8], sc["jump"], dys=[0, 4, 8, 10, 12, 10, 6, 0])
    for k, im in enumerate(jump):
        res[f"jump_{k}"] = im
    bot8 = extract_grid(rgb8, fg8, [(362, 768)], [4], sc["jump"])
    res["fall_0"] = jump[6]
    res["fall_1"] = bot8[2]
    res["fall_2"] = synth_flutter_variant(bot8[2], tail_dx=1, tail_dy=-1)
    res["crouch"] = bot8[3]

    # 3. Hit (6)
    rgb9 = load_rgb(src_dir, "image9.jpeg")
    fg9 = keep_main_components(flood_bg_mask(rgb9), min_area=200)
    hit_boxes = [
        {"box": (50, 90, 290, 380)},
        {"box": (300, 90, 608, 380), "exclude": [(585, 300, 608, 380)]},
        {
            "box": (582, 90, 888, 380),
            "exclude_cond": lambda r, x0, y0: (
                (np.arange(r.shape[1])[None, :] + x0 >= 855)
                & (np.arange(r.shape[0])[:, None] + y0 >= 305)
                & (r.mean(axis=-1) < 220)
            ),
        },
        {"box": (855, 90, 1090, 380)},
        {"box": (1092, 90, 1325, 380)},
        {"box": (1085, 395, 1325, 690)},
    ]
    hits = extract_custom_boxes(rgb9, fg9, hit_boxes, sc["hit"], keep_biggest=False)
    for k, im in enumerate(hits):
        # Garde les étincelles de hit_2 (min_island=1)
        base = im if k != 2 else place_in_canvas(
            downsample_crisp(rgb9[90:380, 582:888], fg9[90:380, 582:888] & ~(
                (np.arange(290)[:, None] + 90 >= 305)
                & (np.arange(306)[None, :] + 582 >= 855)
                & (rgb9[90:380, 582:888].mean(axis=-1) < 220)
            ), sc["hit"]), min_island=1)
        res[f"hit_{k}"] = widen_hit_frame(base, k)

    # 4. Climb (8) & Descend (8)
    rgb10 = load_rgb(src_dir, "image10.jpeg")
    fg10 = keep_main_components(strip_slope_line(rgb10, flood_bg_mask(rgb10)), min_area=1500)
    climb = extract_grid(rgb10, fg10, [(0, 304)], [8], sc["climb"])
    for k, im in enumerate(climb):
        res[f"climb_{k}"] = im
    descend = extract_grid(rgb10, fg10, [(304, 768)], [8], sc["descend"])
    for k, im in enumerate(descend):
        res[f"descend_{k}"] = im

    # 5. Idle (6), Vine (2), Wall (1)
    rgb16 = load_rgb(src_dir, "image16.jpeg")
    fg16 = keep_main_components(flood_bg_mask(rgb16), min_area=1500)
    fg16_idle = fg16.copy()
    fg16_idle[:, 680:] = False
    idles = extract_grid(rgb16, fg16_idle, [(0, 368), (368, 768)], [3, 3], sc["idle"])
    for k, im in enumerate(idles):
        res[f"idle_{k}"] = im
    res["idle"] = res["idle_0"]

    fg16_v = strip_green_vine(rgb16[:, 695:970], fg16[:, 695:970], y_top=132, y_bot=555)
    vine0 = place_in_canvas(downsample_crisp(rgb16[:, 695:970], fg16_v, sc["vw"]))
    res["vine_0"] = vine0
    res["vine_1"] = synth_flutter_variant(vine0, tail_dx=-1, tail_dy=1)
    res["vine"] = vine0

    fg16_w = strip_stone_wall(rgb16[:, 1035:1325], fg16[:, 1035:1325], wall_x_start=1250 - 1035,
                              hand_y_range=(138, 215), foot_y_range=(330, 455))
    res["wall"] = place_in_canvas(downsample_crisp(rgb16[:, 1035:1325], fg16_w, sc["vw"]))

    # 6. Hurt (1 -> 2) & Death (1)
    rgb17 = load_rgb(src_dir, "17.jpeg")
    fg17 = keep_main_components(flood_bg_mask(rgb17), min_area=4000)
    hurt0 = place_in_canvas(downsample_crisp(rgb17[1000:1510, 320:1120], keep_largest_component(fg17[1000:1510, 320:1120]), sc["17"]))
    res["hurt_0"] = hurt0
    res["hurt_1"] = synth_flutter_variant(hurt0, tail_dx=-1, tail_dy=1, body_dy=-1)
    res["hurt"] = hurt0
    res["death"] = place_in_canvas(downsample_crisp(rgb17[1020:1500, 1550:2450], fg17[1020:1500, 1550:2450], sc["17"]), min_island=3)

    return res


def _find_gemini_sheet(src_dir, hero_id, sheet_name):
    gdir = os.path.join(src_dir, "gemini")
    for ext in (".png", ".jpg", ".jpeg"):
        p = os.path.join(gdir, f"{hero_id}_{sheet_name}{ext}")
        if os.path.isfile(p):
            return p
    return None


def _load_gemini(path):
    rgb = np.array(Image.open(path).convert("RGB"))
    fg = keep_main_components(flood_bg_mask(rgb), min_area=600)
    return rgb, fg


def _split_rows_by_valleys(fg, nrows):
    """Trouve les bornes verticales des `nrows` rangées d'une planche."""
    row_sum = fg.sum(axis=1)
    ys = np.where(row_sum > 0)[0]
    if len(ys) == 0:
        return [(0, fg.shape[0])]
    y_min, y_max = int(ys[0]), int(ys[-1]) + 1
    if nrows == 1:
        return [(y_min, y_max)]
    step = (y_max - y_min) / nrows
    cuts = [y_min]
    for i in range(1, nrows):
        est = y_min + i * step
        lo = max(cuts[-1] + 20, int(est - step * 0.30))
        hi = min(y_max - 20, int(est + step * 0.30))
        my = lo + int(np.argmin(row_sum[lo:hi]))
        cuts.append(my)
    cuts.append(y_max)
    return [(cuts[i], cuts[i + 1]) for i in range(nrows)]


def _fix_ryo_colour_drift(rgb, fg):
    """Corrige la dérive occasionnelle vers Kenta (cheveux orange / veste noire) sur quelques frames de Ryo."""
    out = rgb.copy()
    r = out[:, :, 0].astype(np.int16)
    g = out[:, :, 1].astype(np.int16)
    b = out[:, :, 2].astype(np.int16)
    # Cheveux orange -> blanc/gris Ryo
    orange_hair = fg & (r > 130) & (r - g > 30) & (r - b > 45)
    hi_hair = orange_hair & (r > 180)
    lo_hair = orange_hair & ~hi_hair
    out[hi_hair] = (0xE8, 0xEC, 0xF0)
    out[lo_hair] = (0xA8, 0xB0, 0xBC)
    return out


def _align_vine_fist(spr, fist_y_from_bottom=54):
    """Centre la main levée (sommet du sprite) sur x=CW//2 et y=CH-fist_y_from_bottom."""
    spr = remove_small_islands(spr, min_pixels=6)
    a = np.asarray(spr)
    alpha = a[:, :, 3] >= 128
    ys, xs = np.nonzero(alpha)
    if len(xs) == 0:
        return place_in_canvas(spr)
    y_top = int(ys.min())
    fist_band = alpha[y_top:min(a.shape[0], y_top + 6), :]
    fys, fxs = np.nonzero(fist_band)
    fist_cx = int(round(float(np.median(fxs)))) if len(fxs) > 0 else spr.width // 2
    fist_cy = y_top + 2

    canvas = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
    dx = (CW // 2) - fist_cx
    dy = (CH - fist_y_from_bottom) - fist_cy
    # Clamp pour ne rien couper hors du canevas 96x72
    dx = max(1, min(CW - spr.width - 1, dx))
    dy = max(1, min(CH - spr.height, dy))
    canvas.alpha_composite(spr, (dx, dy))
    return canvas


def _place_airhit(spr):
    """Place un sprite de coup en l'air dans 160×72 avec le corps centré autour de x=80 et l'arc vers la droite."""
    spr = remove_small_islands(spr, min_pixels=4)
    a = np.asarray(spr)
    alpha = a[:, :, 3] >= 128
    rgb = a[:, :, :3].astype(np.int16)
    luma = rgb.mean(axis=-1)
    # Le corps (hors traînée crème très claire) sert à centrer le héros sur x = HIT_W // 2
    body = alpha & (luma < 210)
    ys, xs = np.nonzero(body)
    if len(xs) == 0:
        ys, xs = np.nonzero(alpha)
    if len(xs) == 0:
        return Image.new("RGBA", (HIT_W, CH), (0, 0, 0, 0))
    body_cx = int(round(float(np.median(xs))))
    body_bot = int(ys.max())
    dx = (HIT_W // 2) - body_cx
    dy = (CH - 10) - body_bot
    dx = max(1, min(HIT_W - spr.width - 1, dx))
    dy = max(1, min(CH - spr.height - 1, dy))
    canvas = Image.new("RGBA", (HIT_W, CH), (0, 0, 0, 0))
    canvas.alpha_composite(spr, (dx, dy))
    # Coupe tout pixel d'arc dépassant HIT_REACH_PX devant l'axe (HIT_W//2 + 76 = 156)
    out = np.array(canvas)
    max_x = HIT_W // 2 + HIT_REACH_PX
    if max_x < HIT_W:
        out[:, max_x:, :] = 0
    return Image.fromarray(out, "RGBA")


def apply_gemini_overrides(hero_id, res, src_dir):
    """Charge les planches complémentaires/corrigées de tools/heros-v2/raw/gemini/ si elles existent."""
    base_scale = {"kenta": 0.180, "aiko": 0.195, "ryo": 0.185}[hero_id]

    p_run = _find_gemini_sheet(src_dir, hero_id, "run")
    if p_run:
        rgb, fg = _load_gemini(p_run)
        sc = base_scale * (1376.0 / rgb.shape[1])
        rows = _split_rows_by_valleys(fg, 2)
        frames = extract_grid(rgb, fg, rows, [4, 4], sc)
        if hero_id == "ryo" and len(frames) == 8:
            # Recolorise les pixels de cheveux/veste qui auraient dérivé sur la frame 5
            r_list = []
            for idx, (y0, y1) in enumerate(rows):
                for c_idx, (x0, x1) in enumerate(split_row_by_valleys(fg[y0:y1], 4)):
                    f_idx = idx * 4 + c_idx
                    sub_rgb = _fix_ryo_colour_drift(rgb[y0:y1, x0:x1], fg[y0:y1, x0:x1]) if f_idx == 5 else rgb[y0:y1, x0:x1]
                    sub_fg = keep_largest_component(fg[y0:y1, x0:x1])
                    r_list.append(place_in_canvas(downsample_crisp(sub_rgb, sub_fg, sc)))
            frames = r_list
        # Supprime run_8 éventuel (ancien Ryo) pour garder un cycle propre à 8 frames
        res.pop("run_8", None)
        for k, im in enumerate(frames):
            res[f"run_{k}"] = im

    p_jl = _find_gemini_sheet(src_dir, hero_id, "jump_land")
    if p_jl:
        rgb, fg = _load_gemini(p_jl)
        sc = base_scale * (1376.0 / rgb.shape[1])
        rows = _split_rows_by_valleys(fg, 2)
        dys = [0, 4, 10, 14, 12, 8, 4, 0, 0, 0]
        jl_frames = []
        idx = 0
        for (y0, y1) in rows:
            for (x0, x1) in split_row_by_valleys(fg[y0:y1], 5):
                sub_rgb = rgb[y0:y1, x0:x1]
                sub_fg = keep_largest_component(fg[y0:y1, x0:x1])
                if hero_id == "ryo" and idx in (2, 5, 9):
                    sub_rgb = _fix_ryo_colour_drift(sub_rgb, sub_fg)
                spr = downsample_crisp(sub_rgb, sub_fg, sc)
                jl_frames.append(place_in_canvas(spr, dy=dys[idx]))
                idx += 1
        for k in range(8):
            res[f"jump_{k}"] = jl_frames[k]
        res["land_0"] = jl_frames[8]
        res["land_1"] = jl_frames[9]

    p_vd = _find_gemini_sheet(src_dir, hero_id, "vine_drum")
    if p_vd:
        rgb, fg = _load_gemini(p_vd)
        sc = base_scale * (1376.0 / rgb.shape[1])
        rows = _split_rows_by_valleys(fg, 2)
        # Rangée 1 : liane (4 frames) avec la main levée alignée au centre-haut
        y0, y1 = rows[0]
        vine_cols = 5 if hero_id == "ryo" else 4
        vine_ranges = split_row_by_valleys(fg[y0:y1], vine_cols)
        if hero_id == "ryo":
            vine_ranges = [vine_ranges[i] for i in (0, 1, 3, 4)]
        for k, (x0, x1) in enumerate(vine_ranges):
            sub_fg = keep_largest_component(fg[y0:y1, x0:x1])
            spr = downsample_crisp(rgb[y0:y1, x0:x1], sub_fg, sc)
            if hero_id == "aiko" and k == 2:
                spr = spr.transpose(Image.FLIP_LEFT_RIGHT)
            res[f"vine_{k}"] = _align_vine_fist(spr, fist_y_from_bottom=54)
        res["vine"] = res["vine_0"]
        # Rangée 2 : tambour (4 frames)
        y0, y1 = rows[1]
        drum_dys = [0, 8, 12, 10]
        for k, (x0, x1) in enumerate(split_row_by_valleys(fg[y0:y1], 4)):
            sub_fg = keep_largest_component(fg[y0:y1, x0:x1])
            spr = downsample_crisp(rgb[y0:y1, x0:x1], sub_fg, sc)
            res[f"drum_{k}"] = place_in_canvas(spr, dy=drum_dys[k])

    p_wv = _find_gemini_sheet(src_dir, hero_id, "wall_victory")
    if p_wv:
        rgb, fg = _load_gemini(p_wv)
        sc = base_scale * (1376.0 / rgb.shape[1])
        rows = _split_rows_by_valleys(fg, 2)
        fg_wall = fg.copy()
        if hero_id == "ryo":
            # Retire la fine ligne verticale noire dessinée sur le bord droit de la frame 6
            y0, y1 = rows[0]
            col_h = fg_wall[y0:y1].sum(axis=0)
            fg_wall[y0:y1, col_h > (y1 - y0) * 0.72] = False
        wall_frames = extract_grid(rgb, fg_wall, [rows[0]], [6], sc)
        for k, im in enumerate(wall_frames):
            res[f"wall_{k}"] = im
        res["wall"] = res["wall_0"]
        vic_frames = extract_grid(rgb, fg, [rows[1]], [6], sc)
        for k, im in enumerate(vic_frames):
            res[f"victory_{k}"] = im

    p_hd = _find_gemini_sheet(src_dir, hero_id, "hurt_death")
    if p_hd:
        rgb, fg = _load_gemini(p_hd)
        sc = base_scale * (1376.0 / rgb.shape[1])
        rows = _split_rows_by_valleys(fg, 2)
        hd_frames = []
        counts = [5, 4] if hero_id == "ryo" else [4, 4]
        for (y0, y1), cnt in zip(rows, counts):
            for (x0, x1) in split_row_by_valleys(fg[y0:y1], cnt):
                sub_fg = fg[y0:y1, x0:x1]
                spr = downsample_crisp(rgb[y0:y1, x0:x1], sub_fg, sc)
                hd_frames.append(place_in_canvas(spr, min_island=3))
        if hero_id == "ryo":
            # 9 frames : 0..2 = hurt, 3 + 5..8 = death (5 frames)
            hd_frames = hd_frames[:3] + [hd_frames[3]] + hd_frames[5:9]
        elif hero_id == "aiko":
            # Garde la tête à gauche sur la dernière frame de mort
            hd_frames[7] = hd_frames[7].transpose(Image.FLIP_LEFT_RIGHT)
        for k in range(3):
            res[f"hurt_{k}"] = hd_frames[k]
        res["hurt"] = res["hurt_0"]
        for k in range(5):
            res[f"death_{k}"] = hd_frames[3 + k]
        res["death"] = res["death_4"]

    p_ah = _find_gemini_sheet(src_dir, hero_id, "airhit")
    if p_ah:
        rgb, fg = _load_gemini(p_ah)
        sc = base_scale * (1376.0 / rgb.shape[1])
        rows = _split_rows_by_valleys(fg, 2)
        if hero_id == "ryo":
            r1 = split_row_by_valleys(fg[rows[0][0]:rows[0][1]], 5)
            y20, y21 = rows[1]
            col2 = fg[y20:y21].sum(axis=0) > 0
            xs2 = np.nonzero(col2)[0]
            mid2 = (int(xs2[0]) + int(xs2[-1])) // 2
            picks = [
                (rows[0][0], rows[0][1], r1[1][0], r1[1][1]),
                (y20, y21, int(xs2[0]), mid2),
                (rows[0][0], rows[0][1], r1[2][0], r1[2][1]),
                (y20, y21, mid2, int(xs2[-1]) + 1),
            ]
            for idx, (y0, y1, x0, x1) in enumerate(picks):
                spr = downsample_crisp(rgb[y0:y1, x0:x1], fg[y0:y1, x0:x1], sc)
                res[f"airhit_{idx}"] = _place_airhit(spr)
        else:
            idx = 0
            for (y0, y1) in rows:
                for (x0, x1) in split_row_by_valleys(fg[y0:y1], 2):
                    spr = downsample_crisp(rgb[y0:y1, x0:x1], fg[y0:y1, x0:x1], sc)
                    res[f"airhit_{idx}"] = _place_airhit(spr)
                    idx += 1
        for idx, params in AIRHIT_SLASH.items():
            if f"airhit_{idx}" in res:
                # l'arc de Gemini s'arrête vers +50 px : la traînée va jusqu'à la portée de frappe, comme au sol
                res[f"airhit_{idx}"] = draw_slash(res[f"airhit_{idx}"], *params)

    p_pt = _find_gemini_sheet(src_dir, hero_id, "portrait")
    if p_pt:
        rgb, fg = _load_gemini(p_pt)
        rows = _split_rows_by_valleys(fg, 1)
        y0, y1 = rows[0]
        for k, (x0, x1) in enumerate(split_row_by_valleys(fg[y0:y1], 3)):
            sub_fg = keep_largest_component(fg[y0:y1, x0:x1])
            ys, xs = np.nonzero(sub_fg)
            bh = max(1, int(ys.max() - ys.min() + 1)) if len(ys) > 0 else 1
            bw = max(1, int(xs.max() - xs.min() + 1)) if len(xs) > 0 else 1
            sc_p = min(56.0 / bh, 56.0 / bw)
            spr = downsample_crisp(rgb[y0:y1, x0:x1], sub_fg, sc_p)
            res[f"portrait_{k}"] = place_in_canvas(spr, cw=64, ch=64)


def align_vine(im):
    """Héros pendu à la liane : le poing (2 px sous le haut des mains levées) à VINE_GRIP_PX au-dessus du pivot, centré,
    sur une toile CW × VINE_CH dont le pivot est à VINE_PIVOT_PX du bas (les pieds pendent dessous)."""
    spr = remove_small_islands(im, min_pixels=6)
    alpha = np.asarray(spr)[:, :, 3] >= 128
    ys, xs = np.nonzero(alpha)
    assert len(xs) > 0, "image de liane vide"
    y0, y1, x0, x1 = int(ys.min()), int(ys.max()), int(xs.min()), int(xs.max())
    crop = spr.crop((x0, y0, x1 + 1, y1 + 1))
    fist_cx = int(round(float(np.median(np.nonzero(alpha[y0:y0 + 6, :])[1])))) - x0
    top = VINE_CH - VINE_PIVOT_PX - (VINE_GRIP_PX + 2)
    left = CW // 2 - fist_cx
    assert 0 <= left and left + crop.width <= CW and top + crop.height <= VINE_CH, (left, top, crop.size)
    canvas = Image.new("RGBA", (CW, VINE_CH), (0, 0, 0, 0))
    canvas.alpha_composite(crop, (left, top))
    return canvas


def is_vine(name):
    return name == "vine" or name.startswith("vine_")


def save_hero(hero_id, frames, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    for name, im in frames.items():
        if name.startswith(("hit_", "airhit_")):
            expected = (HIT_W, CH)
        elif name.startswith("portrait_"):
            expected = (64, 64)
        elif is_vine(name):
            expected = (CW, VINE_CH)
        else:
            expected = (CW, CH)
        assert im.size == expected, f"{hero_id}_{name}: {im.size}"
        im.save(os.path.join(out_dir, f"hero_{hero_id}_{name}.png"), optimize=True)
    # une famille refaite avec moins d'images (ex. la course de Ryo : 9 → 8) ne garde pas ses anciennes images en trop ;
    # Unity retire leur .meta à la prochaine actualisation
    families = {m.group(1) for m in (re.fullmatch(r"(.+)_(\d+)", n) for n in frames) if m}
    prefix = f"hero_{hero_id}_"
    for path in glob.glob(os.path.join(out_dir, prefix + "*.png")):
        name = os.path.basename(path)[len(prefix):-4]
        m = re.fullmatch(r"(.+)_(\d+)", name)
        if m and m.group(1) in families and name not in frames:
            os.remove(path)
            print(f"retirée : {os.path.basename(path)}")
    with open(os.path.join(out_dir, f".v2_{hero_id}"), "w", encoding="utf-8") as f:
        f.write(MARKER_TEXT)


def make_contact_sheet(all_heroes, path):
    rows = []
    for hid in ("kenta", "aiko", "ryo"):
        f = all_heroes[hid]
        order = (
            [f"idle_{k}" for k in range(6)]
            + [k for k in sorted(f) if k.startswith("run_")]
            + [f"jump_{k}" for k in range(8)]
            + [k for k in sorted(f) if k.startswith("land_")]
            + [f"hit_{k}" for k in range(6)]
            + [k for k in sorted(f) if k.startswith("airhit_")]
            + [f"climb_{k}" for k in range(8)]
            + [f"descend_{k}" for k in range(8)]
            + [f"fall_{k}" for k in range(3)]
            + ["crouch"]
            + [k for k in sorted(f) if k.startswith("vine_")]
            + [k for k in sorted(f) if k.startswith("drum_")]
            + ([k for k in sorted(f) if k.startswith("wall_")] or ["wall"])
            + [k for k in sorted(f) if k.startswith("victory_")]
            + [k for k in sorted(f) if k.startswith("hurt_")]
            + ([k for k in sorted(f) if k.startswith("death_")] or ["death"])
            + [k for k in sorted(f) if k.startswith("portrait_")]
        )
        rows.append([f[k] for k in order if k in f])
    cols = 12
    total_rows = sum((len(r) + cols - 1) // cols for r in rows)
    cell_w = HIT_W
    rh = VINE_CH                                          # rangées assez hautes pour la liane, images posées par le bas
    sheet = Image.new("RGBA", (cols * cell_w, total_rows * rh), (65, 68, 82, 255))
    ry = 0
    for r in rows:
        for i, im in enumerate(r):
            ox = (cell_w - im.width) // 2
            sheet.alpha_composite(im, ((i % cols) * cell_w + ox, (ry + i // cols) * rh + rh - im.height))
        ry += (len(r) + cols - 1) // cols
    sheet.resize((sheet.width * 2, sheet.height * 2), Image.NEAREST).save(path)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("src", nargs="?", default=DEFAULT_SRC)
    ap.add_argument("--out", default=OUT)
    ap.add_argument("--preview", default=None)
    args = ap.parse_args()

    all_heroes = {
        "kenta": build_kenta(args.src),
        "aiko": build_aiko(args.src),
        "ryo": build_ryo(args.src),
    }
    for hid, frames in all_heroes.items():
        apply_gemini_overrides(hid, frames, args.src)
        for name in [n for n in frames if is_vine(n)]:
            frames[name] = align_vine(frames[name])
    total = 0
    for hid, frames in all_heroes.items():
        save_hero(hid, frames, args.out)
        total += len(frames)
        print(f"{hid}: {len(frames)} sprites")
    if args.preview:
        make_contact_sheet(all_heroes, args.preview)
        print("preview:", args.preview)
    print(f"ok : {total} images dans {args.out}")


if __name__ == "__main__":
    main()
