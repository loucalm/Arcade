#!/usr/bin/env python3
"""Héros « hybrides » : le mouvement de la marionnette (tools/hero_rig.py, tools/gen-heroes.py : poses bien espacées,
animations fluides) et la tête dessinée par Gemini (cheveux, bandeau, visage : le nouveau style).

Pourquoi : les animations dessinées par Gemini sautaient (course toujours jambes écartées, jamais la pose de passage
ni la phase en l'air) ; la marionnette, elle, a toutes les poses. On garde donc ses images et on remplace sa tête par
celle de Gemini, posée à chaque image au centre de la tête de la marionnette et tournée comme elle (par quarts de
tour seulement : pas de rotation fractionnaire en pixel art).

La tête vient de l'image d'attente de Gemini (tools/gemini-heros/clean.py), coupée sous le menton (HEAD_ROWS).

Usage : python3 tools/gemini-heros/hybrid.py [kenta aiko ryo] [--install] [--only run,jump,...]
  sans --install : aperçus dans tools/gemini-heros/out/hybride_<héros>.png
"""
import argparse
import importlib.util
import math
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
TOOLS = os.path.normpath(os.path.join(HERE, ".."))
sys.path.insert(0, TOOLS)
sys.path.insert(0, HERE)
import hero_rig as R  # noqa: E402

GAME = os.path.normpath(os.path.join(TOOLS, "..", "game", "Assets", "_Project", "Art", "Kenta", "Gen", "heroes"))
OUT = os.path.join(HERE, "out")

# rangées de la tête dans l'image d'attente de Gemini (recadrée), du haut des cheveux au menton
HEAD_ROWS = {"kenta": 21, "aiko": 20, "ryo": 20}


def load(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def gemini_head(hero):
    """Tête de Gemini (RGBA) et son centre (x, y) : le milieu de la peau du visage."""
    clean = load(os.path.join(HERE, "clean.py"), "clean")
    frames, _ = clean.build(hero)
    idle = frames["idle"][0]
    idle = idle.crop(idle.getbbox())
    head = np.asarray(idle.crop((0, 0, idle.width, HEAD_ROWS[hero]))).copy()
    a = head.astype(int)
    skin = (a[..., 3] > 0) & (a[..., 0] > 170) & (a[..., 1] > 110) & (a[..., 2] > 80) & (a[..., 0] > a[..., 2] + 30)
    ys, xs = np.where(skin)
    center = (float(np.median(xs)) - 1.0, float(np.median(ys)) - 2.0)   # le crâne est un peu en arrière et au-dessus du visage
    return head, center


def install_hooks(head_img, head_center):
    """La marionnette ne dessine plus sa tête (ni ses traits) : à la place, la tête de Gemini est collée après le
    contour, au centre de sa tête, tournée par quarts de tour."""
    state = {}

    def no_head(cv, hero, sk, pose, up, right):
        state["c"], state["up"] = sk.head, up

    def paste_head(cv, hero, sk, pose, up, right):
        c, up = state["c"], state["up"]
        ang = math.degrees(math.atan2(up[0], up[1]))           # 0 = tête droite ; positif = penchée vers l'avant
        q = int(round(ang / 90.0)) % 4                          # quart de tour le plus proche
        img = np.rot90(head_img, -q)                            # sens horaire
        cx, cy = head_center
        h, w = head_img.shape[:2]
        for _ in range(q):                                      # le centre tourne avec l'image
            cx, cy = (h - 1) - cy, cx
            h, w = w, h
        H, W = cv.rgba.shape[:2]
        # repère de la marionnette : y vers le haut depuis le bas du canevas
        x0 = int(round(c[0] + W / 2 - 0.5 - cx))                # x centré sur le canevas
        y0 = int(round((H - 0.5 - c[1]) - cy))
        ih, iw = img.shape[:2]
        for yy in range(ih):
            ty = y0 + yy
            if not 0 <= ty < H:
                continue
            for xx in range(iw):
                tx = x0 + xx
                if 0 <= tx < W and img[yy, xx, 3] > 0:
                    cv.rgba[ty, tx] = img[yy, xx]

    R.head = no_head
    R.face = paste_head
    # la tête de Gemini a déjà son nœud et ses pans de bandeau : ceux de la marionnette feraient doublon
    if not getattr(R.draw, "_hybrid", False):
        draw = R.draw

        def draw_without_tails(hero, pose, extras=None, w=96, h=72):
            if extras is not None:
                extras.tails = None
            return draw(hero, pose, extras, w, h)
        draw_without_tails._hybrid = True
        R.draw = draw_without_tails


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("heroes", nargs="*", default=["kenta", "aiko", "ryo"])
    ap.add_argument("--install", action="store_true")
    ap.add_argument("--only", default="", help="animations à rendre (ex. run,jump) ; toutes par défaut")
    args = ap.parse_args()
    gen = load(os.path.join(TOOLS, "gen-heroes.py"), "gen_heroes")
    only = [s for s in args.only.split(",") if s]
    os.makedirs(OUT, exist_ok=True)
    for hero in args.heroes:
        head, center = gemini_head(hero)
        install_hooks(head, center)
        frames = gen.all_frames(hero)
        if only:
            frames = {k: v for k, v in frames.items() if k.split("_")[0] in only}
        names = sorted(frames)
        # aperçu : une rangée par animation, ×3
        rows = {}
        for n in names:
            rows.setdefault(n.rsplit("_", 1)[0] if n[-1].isdigit() else n, []).append(n)
        W = max(sum(frames[n].shape[1] for n in v) for v in rows.values())
        H = sum(max(frames[n].shape[0] for n in v) for v in rows.values())
        sheet = Image.new("RGBA", (W, H), (48, 52, 72, 255))
        y = 0
        for anim, ns in rows.items():
            x = 0
            for n in ns:
                im = Image.fromarray(frames[n], "RGBA")
                sheet.alpha_composite(im, (x, y))
                x += im.width
            y += max(frames[n].shape[0] for n in ns)
        sheet.resize((W * 3, H * 3), Image.NEAREST).save(os.path.join(OUT, f"hybride_{hero}.png"))
        print(f"{hero} : {len(frames)} images ; aperçu out/hybride_{hero}.png")
        if args.install:
            for n, a in frames.items():
                Image.fromarray(a, "RGBA").save(os.path.join(GAME, f"hero_{hero}_{n}.png"))
            open(os.path.join(GAME, f".gemini_{hero}"), "w").write(
                "Héros hybrides (marionnette + tête Gemini, tools/gemini-heros/hybrid.py) : gen-kenta-assets.py ne les "
                "écrase pas ; pour les refaire : python3 tools/gemini-heros/hybrid.py --install\n")
            print(f"  installé dans {GAME}")


if __name__ == "__main__":
    main()
