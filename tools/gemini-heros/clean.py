#!/usr/bin/env python3
"""Installe les héros dessinés par Gemini dans le jeu Kenta (pixel art à la taille exacte, mêmes noms de fichiers).

Entrée : les planches raw/sheets/<héros>_<n>.jpg (fond magenta) et manifest.json, qui dit quelle image de quelle
planche devient quelle image du jeu (les numéros viennent de blobs.py : `python3 blobs.py planche.jpg annotee.png`).

Pour chaque héros :
  1. chaque image est découpée (le personnage, son katana, la traînée du coup ; pas les textes ni les voisins) ;
  2. une seule échelle par héros : ses images d'attente font 54 px de haut dans le jeu ;
  3. réduction (moyenne par zone), alpha tranché, couleurs ramenées à une palette commune de 32 couleurs par héros
     (les images d'une animation ne papillotent pas), contour sombre #14101C sur le bord ;
  4. mise en case : 96×72 (160×72 pour les coups, 64×64 pour les portraits), pieds sur la dernière rangée, axe du
     corps au milieu (à x = 80 pour les coups ; la traînée est coupée à 76 px devant l'axe : la portée de la frappe,
     `PlayerController.hitReach`).

Sortie : out/<héros>_controle.png (toutes les animations ×2) et out/<héros>_<anim>.gif ; avec --install, les PNG dans
game/Assets/_Project/Art/Kenta/Gen/heroes/ (hero_<id>_<anim>_<k>.png + images seules idle, crouch, vine, wall, hurt,
death), et suppression des anciennes images en trop d'une animation (ex. climb_7 quand il n'y en a plus que 7).
Ensuite, dans Unity : menu Kenta > Installer les sprites des héros et des ennemis.

Usage : python3 tools/gemini-heros/clean.py [kenta aiko ryo] [--install]   (dépendances : pillow, numpy, scipy)
"""
import argparse
import glob
import json
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from blobs import background, blobs  # noqa: E402

RAW, OUT = os.path.join(HERE, "raw", "sheets"), os.path.join(HERE, "out")
GAME = os.path.normpath(os.path.join(HERE, "..", "..", "game", "Assets", "_Project", "Art", "Kenta", "Gen", "heroes"))
MANIFEST = json.load(open(os.path.join(HERE, "manifest.json"), encoding="utf-8"))

STAND_PX = 54                      # hauteur de l'attente dans le jeu
CELL, HIT_CELL, PORTRAIT = (96, 72), (160, 72), (64, 64)
HIT_AXIS, HIT_REACH = 80, 76       # axe du héros dans une image de coup, portée de la frappe (px)
OUTLINE = (0x14, 0x10, 0x1C)
COLORS = 32
SINGLE = {"idle": 0, "crouch": ("land", 0), "vine": 0, "wall": 0, "hurt": 0, "death": -1}   # images seules (anciens noms)


class Sheet:
    def __init__(self, name, erase):
        path = os.path.join(RAW, name + ".jpg")
        if not os.path.exists(path):
            path = os.path.join(RAW, name + ".png")
        self.blobs, self.a, self.fg = blobs(path)
        for x0, y0, x1, y1 in erase:                     # textes : on n'y garde que les tons chauds (cheveux, peau)
            p = self.a[y0:y1, x0:x1]
            warm = (p[..., 0] > p[..., 1] + 40) & (p[..., 2] < p[..., 1] + 30) & (p[..., 0] > 120)
            self.fg[y0:y1, x0:x1] &= warm


def split_box(sheet, box, side):
    """Moitié d'un blob qui a fusionné deux images : coupe à la colonne (ou rangée) la plus vide près du milieu."""
    x0, y0, x1, y1 = box
    m = sheet.fg[y0:y1, x0:x1]
    if side in "LR":
        prof = m.sum(axis=0); n = x1 - x0
        lo, hi = int(n * 0.3), int(n * 0.7)
        c = x0 + lo + int(np.argmin(prof[lo:hi]))
        return (x0, y0, c, y1) if side == "L" else (c, y0, x1, y1)
    prof = m.sum(axis=1); n = y1 - y0
    lo, hi = int(n * 0.3), int(n * 0.7)
    c = y0 + lo + int(np.argmin(prof[lo:hi]))
    return (x0, y0, x1, c) if side == "T" else (x0, c, x1, y1)


def cut(sheet, box, wide):
    """Le personnage dans `box` : le plus gros morceau et ce qui le touche presque (katana, cheveux) ; avec `wide`, aussi
    la traînée du coup à sa droite. Renvoie l'image RGBA recadrée."""
    x0, y0, x1, y1 = box
    sub = sheet.fg[y0:y1, x0:x1]
    lab, n = ndimage.label(ndimage.binary_dilation(sub, iterations=2))
    objs = ndimage.find_objects(lab)
    sizes = ndimage.sum(sub, lab, range(1, n + 1))
    m = int(np.argmax(sizes)); sy, sx = objs[m]
    keep = (lab == m + 1) & sub
    for i, o in enumerate(objs):
        if i == m or sizes[i] < 40:
            continue
        oy, ox = o
        h = oy.stop - oy.start
        vov = min(oy.stop, sy.stop) - max(oy.start, sy.start)
        near = ox.start <= sx.stop + (160 if wide else 6) and ox.stop >= sx.start - 6
        if vov > 0.3 * h and near:
            keep |= (lab == i + 1) & sub
    rgba = np.zeros(sub.shape + (4,), np.uint8)
    rgba[..., :3] = sheet.a[y0:y1, x0:x1]
    rgba[..., 3] = keep * 255
    ys, xs = np.where(keep)
    return Image.fromarray(rgba[ys.min():ys.max() + 1, xs.min():xs.max() + 1])


def body_axis(im):
    """Colonne du corps : médiane des pixels sombres (vêtements, contour), pas du katana ni de la traînée."""
    a = np.asarray(im).astype(int)
    lum = a[..., :3].mean(axis=2)
    m = (a[..., 3] > 0) & (lum < 95)
    if m.sum() < 20:
        m = a[..., 3] > 0
    return float(np.median(np.where(m)[1]))


def shrink(im, s):
    w, h = max(1, round(im.width * s)), max(1, round(im.height * s))
    small = im.convert("RGBa").resize((w, h), Image.BOX).convert("RGBA")
    a = np.asarray(small).copy()
    a[..., 3] = np.where(a[..., 3] >= 128, 255, 0)
    return Image.fromarray(a)


def quantizer(images):
    """Palette commune (médiane) sur les pixels opaques de toutes les images d'un héros."""
    px = np.concatenate([np.asarray(i)[..., :3][np.asarray(i)[..., 3] > 0] for i in images])
    strip = Image.fromarray(px.reshape(1, -1, 3).astype(np.uint8))
    pal = strip.quantize(colors=COLORS, method=Image.MEDIANCUT)

    def q(im):
        a = np.asarray(im)
        rgb = Image.fromarray(a[..., :3]).quantize(palette=pal, dither=Image.NONE).convert("RGB")
        out = np.dstack([np.asarray(rgb), a[..., 3]])
        return Image.fromarray(out.astype(np.uint8))
    return q


LIFT = 1.45                        # les tons très sombres (vêtements) éclaircis : lisibles sur les décors sombres


def lift(im):
    """Éclaircit les couleurs sombres (luminance < 90) sans toucher aux autres : la tenue garde sa teinte."""
    a = np.asarray(im).astype(float)
    lum = a[..., :3].mean(axis=2, keepdims=True)
    k = np.clip((90 - lum) / 90, 0, 1)                     # 1 pour le noir, 0 à partir de 90
    a[..., :3] = np.clip(a[..., :3] * (1 + (LIFT - 1) * k) + 14 * k, 0, 255)
    return Image.fromarray(a.astype(np.uint8))


def outline(im):
    """Contour sombre d'1 px : les pixels opaques qui touchent le vide (lisible sur tous les décors)."""
    a = np.asarray(im).copy()
    op = a[..., 3] > 0
    inner = ndimage.binary_erosion(op, structure=np.ones((3, 3)), border_value=0)
    edge = op & ~inner
    dark = a[..., :3].mean(axis=2) < 70
    a[edge & ~dark, :3] = OUTLINE
    return Image.fromarray(a)


def place(im, cell, axis, anim):
    """Pose l'image dans sa case : pieds en bas, axe du corps au milieu (coups : à x = 80, traînée coupée à 156)."""
    cw, ch = cell
    if im.height > ch:                                        # trop haute (bras levé) : réduite pour tenir
        r = ch / im.height
        axis *= r
        im = im.resize((max(1, round(im.width * r)), ch), Image.NEAREST)
    c = Image.new("RGBA", cell, (0, 0, 0, 0))
    if anim == "portrait":
        x, y = (cw - im.width) // 2, ch - im.height
    else:
        cx = HIT_AXIS if cell == HIT_CELL else cw // 2
        x, y = int(round(cx - axis)), ch - im.height
    c.paste(im, (x, y), im)                                   # paste coupe ce qui dépasse de la case
    if cell == HIT_CELL:
        a = np.asarray(c).copy()
        a[:, HIT_AXIS + HIT_REACH:, 3] = 0
        c = Image.fromarray(a)
    return c


def build(hero):
    spec = MANIFEST[hero]
    sheets = {}

    def sheet(name):
        if name not in sheets:
            sheets[name] = Sheet(name, spec.get("erase", {}).get(name, []))
        return sheets[name]

    def frame(ref, wide):
        parts = ref.split(":")
        sh = sheet(parts[0])
        box = sh.blobs[int(parts[1])]
        if len(parts) > 2:
            box = split_box(sh, box, parts[2])
        return cut(sh, box, wide)

    stand = [frame(r, False).height for r in spec["stand"]]
    s = STAND_PX / float(np.median(stand))
    raw = {}
    for anim, refs in spec["anims"].items():
        wide = anim in ("hit", "airhit")
        ims = [frame(r, wide) for r in refs]
        if anim == "portrait":
            sp = (PORTRAIT[1] - 2) / max(i.height for i in ims)
            raw[anim] = [(shrink(i, sp), 0.0) for i in ims]
        else:
            raw[anim] = [(shrink(i, s), body_axis(i) * s) for i in ims]
    q = quantizer([im for v in raw.values() for im, _ in v])
    out = {}
    for anim, lst in raw.items():
        cell = HIT_CELL if anim in ("hit", "airhit") else PORTRAIT if anim == "portrait" else CELL
        out[anim] = [place(outline(lift(q(im))), cell, ax, anim) for im, ax in lst]
    return out, s


def previews(hero, frames):
    os.makedirs(OUT, exist_ok=True)
    rows = []
    for anim, lst in frames.items():
        cw, ch = lst[0].size
        r = Image.new("RGBA", (cw * len(lst), ch), (48, 52, 72, 255))
        for i, f in enumerate(lst):
            r.alpha_composite(f, (i * cw, 0))
        rows.append(r)
        big = [Image.alpha_composite(Image.new("RGBA", f.size, (48, 52, 72, 255)), f).resize((cw * 4, ch * 4), Image.NEAREST)
               for f in lst]
        big[0].save(os.path.join(OUT, f"{hero}_{anim}.gif"), save_all=True, append_images=big[1:],
                    duration=300 if anim in ("idle", "portrait") else 110, loop=0, disposal=2)
    w, h = max(r.width for r in rows), sum(r.height for r in rows)
    sheet = Image.new("RGBA", (w, h), (30, 30, 40, 255)); y = 0
    for r in rows:
        sheet.paste(r, (0, y)); y += r.height
    sheet.resize((w * 2, h * 2), Image.NEAREST).save(os.path.join(OUT, f"{hero}_controle.png"))


def install(hero, frames):
    p = os.path.join(GAME, f"hero_{hero}_")
    for anim, lst in frames.items():
        for k, f in enumerate(lst):
            f.save(f"{p}{anim}_{k}.png")
        for old in glob.glob(f"{p}{anim}_*.png"):                       # anciennes images en trop
            k = old[len(p) + len(anim) + 1:-4]
            if k.isdigit() and int(k) >= len(lst):
                os.remove(old)
                if os.path.exists(old + ".meta"):
                    os.remove(old + ".meta")
    for name, src in SINGLE.items():
        anim, k = (src if isinstance(src, tuple) else (name, src))
        frames[anim][k].save(f"{p}{name}.png")
    open(os.path.join(GAME, f".gemini_{hero}"), "w").write("Sprites dessinés par Gemini (tools/gemini-heros/clean.py) : "
                                                         "ne pas les écraser avec tools/gen-heroes.py.\n")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("heroes", nargs="*", default=list(k for k in MANIFEST if not k.startswith("_")))
    ap.add_argument("--install", action="store_true")
    args = ap.parse_args()
    for hero in args.heroes:
        frames, s = build(hero)
        previews(hero, frames)
        n = sum(len(v) for v in frames.values())
        print(f"{hero} : {n} images, échelle {s:.3f} ; " + ", ".join(f"{a} {len(v)}" for a, v in frames.items()))
        if args.install:
            install(hero, frames)
            print(f"  installé dans {GAME}")


if __name__ == "__main__":
    main()
