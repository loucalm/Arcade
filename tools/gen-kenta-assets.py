#!/usr/bin/env python3
"""Génère les décors, obstacles, interface et effets de Kenta (pixel art 240×135, voir docs/da-kenta.md).

Les sprites des héros et des ennemis ne sont PAS générés : ils viennent du PDF de la DA
(tools/extract-kenta-pdf.py). Ici : 8 biomes (ciel, 3 calques en parallaxe, sol, trou, obstacles), pièces,
Sourdine, totem, logo, cœurs, cadres et police pixel.

Usage : python3 tools/gen-kenta-assets.py [dossier_sortie] [--only biomes,ui,font] [--sheet planche.png]
Dossier par défaut : game/Assets/_Project/Art/Kenta/Gen   (dépendances : tools/requirements.txt)
"""
import argparse
import os
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kenta_art as art  # noqa: E402
import kenta_ui as ui  # noqa: E402
from kenta_biomes import BIOMES  # noqa: E402
from pixlib import Pix, col  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DEFAULT_OUT = os.path.join(ROOT, "game", "Assets", "_Project", "Art", "Kenta", "Gen")


def put(out, sub, name, img, sheet=None):
    d = os.path.join(out, sub)
    os.makedirs(d, exist_ok=True)
    im = img.im if isinstance(img, Pix) else img
    im.save(os.path.join(d, name + ".png"), optimize=True)
    if sheet is not None:
        sheet.append(im)


def gen_biomes(out, sheet):
    for b in BIOMES:
        i = b["id"]
        put(out, "biomes", f"bg_{i}_sky", art.draw_sky(b), sheet)
        put(out, "biomes", f"bg_{i}_far", art.draw_far(b))
        put(out, "biomes", f"bg_{i}_mid", art.draw_mid(b))
        put(out, "biomes", f"bg_{i}_near", art.draw_near(b))
        put(out, "biomes", f"ground_{i}", art.draw_ground(b))
        put(out, "biomes", f"hazard_{i}", art.draw_hazard(b))
        put(out, "biomes", f"wall_{i}", art.draw_wall(b), sheet)
        put(out, "biomes", f"block_{i}", art.draw_block(b), sheet)
        put(out, "biomes", f"ceil_{i}", art.draw_ceil(b))
        put(out, "biomes", f"post_{i}", art.draw_post(b))
        put(out, "biomes", f"fill_{i}", art.draw_fill(b))
        put(out, "biomes", f"cliff_{i}", art.draw_cliff_top(b))
        put(out, "biomes", f"platform_{i}", art.draw_platform(b), sheet)


def gen_ui(out, sheet):
    put(out, "ui", "logo", ui.draw_logo(), sheet)
    put(out, "ui", "heart_full", ui.draw_heart(True), sheet)
    put(out, "ui", "heart_empty", ui.draw_heart(False), sheet)
    put(out, "ui", "panel", ui.draw_panel(), sheet)
    put(out, "ui", "plate", ui.draw_plate(), sheet)
    put(out, "ui", "button_red", ui.draw_button("#E03A58"), sheet)
    put(out, "ui", "button_blue", ui.draw_button("#4A90D8"), sheet)
    put(out, "ui", "stick", ui.draw_stick(), sheet)
    put(out, "ui", "pixel", Pix(1, 1, "#FFFFFF"))
    put(out, "ui", "veil", ui.draw_veil())
    put(out, "ui", "sourdine", ui.draw_sourdine(), sheet)
    put(out, "world", "px", Pix(1, 1, "#FFFFFF"))
    put(out, "world", "totem", ui.draw_totem(), sheet)
    for f in range(4):
        put(out, "world", f"coin_{f}", ui.draw_coin(f), sheet)


def gen_font(out, sheet):
    for ch in ui.ALL_CHARS:
        put(out, "font", f"glyph_{ord(ch):04X}", ui.glyph_image(ch))
    # planche d'aperçu : toute la police sur 2 lignes
    if sheet is not None:
        line = Image.new("RGBA", (len(ui.ALL_CHARS) * 6 + 4, 14), (0, 0, 0, 0))
        for i, ch in enumerate(ui.ALL_CHARS):
            line.alpha_composite(ui.glyph_image(ch).im, (2 + i * 6, 0))
        sheet.append(line)


def gen_thumbnail(out, sheet):
    """Jaquette carrée de la borne (game/BuildExtras/thumbnail.png) : la Sylve, Kenta en pleine course et le logo."""
    b = BIOMES[0]
    scene = art.draw_sky(b)
    for layer, off in ((art.draw_far(b), 120), (art.draw_mid(b), 150), (art.draw_near(b), 230)):
        scene.im.alpha_composite(layer.im.crop((off, 0, off + 240, 135)))
    ground = art.draw_ground(b)
    for x in range(0, 240, 32):
        scene.im.alpha_composite(ground.im, (x, 115))
    hero = Image.open(os.path.join(ROOT, "game", "Assets", "_Project", "Art", "Kenta", "Pdf", "hero_kenta_run.png")).convert("RGBA")
    scene.im.alpha_composite(hero, (116 - hero.width // 2, 116 - hero.height))
    for i, (x, y) in enumerate(((150, 95), (162, 92), (174, 94))):
        scene.im.alpha_composite(ui.draw_coin(i % 2).im, (x, y))
    sq = scene.im.crop((52, 0, 187, 135))
    logo = ui.draw_logo().im
    sq.alpha_composite(logo, ((135 - logo.width) // 2, 8))
    big = sq.resize((135 * 4, 135 * 4), Image.NEAREST)
    path = os.path.join(ROOT, "game", "BuildExtras", "thumbnail.png")
    big.convert("RGB").save(path, optimize=True)
    if sheet is not None:
        sheet.append(sq)


def gen_heroes(out, sheet):
    """Cycles de course à 6 images (tools/gen-hero-run.py), à partir des sprites du PDF."""
    import importlib.util
    spec = importlib.util.spec_from_file_location("gen_hero_run", os.path.join(ROOT, "tools", "gen-hero-run.py"))
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    mod.main()


GENERATORS = {"biomes": gen_biomes, "ui": gen_ui, "font": gen_font, "heroes": gen_heroes, "thumbnail": gen_thumbnail}


def contact_sheet(imgs, path, width=1500, scale=2, bg=(60, 60, 80)):
    x = y = row_h = 0
    pos = []
    ims = [i.resize((i.width * scale, i.height * scale), Image.NEAREST) for i in imgs]
    for im in ims:
        if x + im.width > width:
            x, y, row_h = 0, y + row_h + 6, 0
        pos.append((x, y))
        x += im.width + 6
        row_h = max(row_h, im.height)
    sh = Image.new("RGBA", (width, y + row_h), bg + (255,))
    for im, p in zip(ims, pos):
        sh.alpha_composite(im, p)
    sh.convert("RGB").save(path)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("out", nargs="?", default=DEFAULT_OUT)
    ap.add_argument("--only", default="")
    ap.add_argument("--sheet", default="")
    a = ap.parse_args()
    names = [n for n in a.only.split(",") if n] or list(GENERATORS)
    unknown = [n for n in names if n not in GENERATORS]
    if unknown:
        sys.exit(f"générateur inconnu : {unknown} (dispo : {list(GENERATORS)})")
    sheet = []
    for n in names:
        GENERATORS[n](a.out, sheet)
        print("ok", n)
    if a.sheet:
        contact_sheet(sheet, a.sheet)


if __name__ == "__main__":
    main()
