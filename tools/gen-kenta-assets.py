#!/usr/bin/env python3
"""Génère les décors, interface et effets de Kenta (pixel art ; décors en 640×360, voir docs/art-direction.md).

Les sprites des ennemis ne sont PAS générés : ils viennent du PDF de la DA (tools/extract-kenta-pdf.py). Les obstacles
non plus : ils viennent de Gemini (tools/gemini-obstacles/, ADR-026). Ici : 8 biomes (ciel, 3 calques en parallaxe,
sol, trou), pièces, totem, logo, cœurs, cadres et police pixel.

Usage : python3 tools/gen-kenta-assets.py [dossier_sortie] [--only biomes,ui,hud,font,thumbs] [--sheet planche.png]
Dossier par défaut : game/Assets/_Project/Art/Kenta/Gen   (dépendances : tools/requirements.txt)
"""
import argparse
import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kenta_art as art  # noqa: E402
import kenta_thumbs as thumbs  # noqa: E402
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
    # gardiens (guardian_<id>.png + guardians.json) : dessinés par Gemini, nettoyés par tools/gemini-gardiens/clean.py
    # (ADR-028). Ils ne sont plus générés ici : ne pas les réécraser.
    for b in BIOMES:
        i = b["id"]
        put(out, "biomes", f"bg_{i}_sky", art.draw_sky(b), sheet)
        put(out, "biomes", f"fill_{i}_deep", art.draw_fill_deep(b))
        for k in range(4):
            put(out, "biomes", f"deco_{i}_{k}", art.draw_deco(b, k), sheet)
        put(out, "biomes", f"bg_{i}_far", art.draw_far(b))
        put(out, "biomes", f"bg_{i}_mid", art.draw_mid(b))
        put(out, "biomes", f"bg_{i}_near", art.draw_near(b))
        put(out, "biomes", f"ground_{i}", art.draw_ground(b))
        put(out, "biomes", f"hazard_{i}", art.draw_hazard(b))
        put(out, "biomes", f"fill_{i}", art.draw_fill(b))
        put(out, "biomes", f"cliff_{i}", art.draw_cliff_top(b))
        # obstacles (wall, block, ceil, post, platform) : dessinés par Gemini, nettoyés par tools/gemini-obstacles/clean.py
        # (ADR-026). Ils ne sont plus générés ici : ne pas les réécraser.


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


def text_image(text, scale=1):
    """Texte dans la police pixel du jeu (glyphes de kenta_ui, avance = largeur + 1 px), agrandi d'un facteur entier."""
    glyphs = [ui.glyph_image(ch).im for ch in text]
    w = sum(g.width + 1 for g in glyphs) - 1
    im = Image.new("RGBA", (w, max(g.height for g in glyphs)), (0, 0, 0, 0))
    x = 0
    for g in glyphs:
        im.alpha_composite(g, (x, 0))
        x += g.width + 1
    return im.resize((im.width * scale, im.height * scale), Image.NEAREST)


def demo_banner(img, y0, h, text="DÉMO"):
    """Bandeau rouge sur toute la largeur (bord sombre, reflet clair), texte blanc à ombre centré, agrandi ×2."""
    w = img.width
    band = Image.new("RGBA", (w, h), col("#D7263D"))
    d = ImageDraw.Draw(band)
    d.rectangle([0, 0, w - 1, 0], fill=col("#5A0A16"))
    d.rectangle([0, 1, w - 1, 1], fill=col("#F25467"))
    d.rectangle([0, h - 2, w - 1, h - 2], fill=col("#A8172C"))
    d.rectangle([0, h - 1, w - 1, h - 1], fill=col("#5A0A16"))
    t = text_image(text, scale=2)
    band.alpha_composite(t, ((w - t.width) // 2, (h - t.height) // 2 + 1))
    img.alpha_composite(band, (0, y0))


def gen_thumbnail(out, sheet):
    """Jaquette carrée de la borne (game/BuildExtras/thumbnail.png) : la Sylve, Kenta (marionnette, ADR-022) en pleine
    course, le logo, et un bandeau rouge « DÉMO » en bas."""
    b = BIOMES[0]
    scene = art.draw_sky(b, scale=1)
    for layer, off in ((art.draw_far(b, scale=1, foot=False), 120), (art.draw_mid(b, scale=1, foot=False), 150), (art.draw_near(b, scale=1, foot=False), 230)):
        scene.im.alpha_composite(layer.im.crop((off, 0, off + 240, 135)))
    ground = art.draw_ground(b)
    for x in range(0, 240, 32):
        scene.im.alpha_composite(ground.im, (x, 115))
    hero = Image.open(os.path.join(ROOT, "game", "Assets", "_Project", "Art", "Kenta", "Gen", "heroes", "hero_kenta_run_1.png")).convert("RGBA")
    scene.im.alpha_composite(hero, (116 - hero.width // 2, 116 - hero.height))     # toile 96×72, pieds en bas au milieu
    for i, (x, y) in enumerate(((150, 95), (162, 92), (174, 94))):
        scene.im.alpha_composite(ui.draw_coin(i % 2).im, (x, y))
    # la scène remonte de SHIFT px pour laisser la place au bandeau sous le sol (l'eau est prolongée vers le bas)
    SHIFT, BAND = 10, 23
    tall = Image.new("RGBA", (scene.im.width, 135 + SHIFT))
    tall.alpha_composite(scene.im.crop((0, 0, scene.im.width, 135)), (0, 0))
    tall.alpha_composite(scene.im.crop((0, 135 - SHIFT, scene.im.width, 135)), (0, 135))
    sq = tall.crop((52, SHIFT, 187, 135 + SHIFT))
    logo = ui.draw_logo().im
    sq.alpha_composite(logo, ((135 - logo.width) // 2, 8))
    demo_banner(sq, 135 - BAND, BAND)
    big = sq.resize((135 * 4, 135 * 4), Image.NEAREST)
    path = os.path.join(ROOT, "game", "BuildExtras", "thumbnail.png")
    big.convert("RGB").save(path, optimize=True)
    if sheet is not None:
        sheet.append(sq)


def gen_heroes(out, sheet):
    """Toutes les images des héros, dessinées par la marionnette de tools/hero_rig.py (tools/gen-heroes.py)."""
    import importlib.util
    spec = importlib.util.spec_from_file_location("gen_heroes", os.path.join(ROOT, "tools", "gen-heroes.py"))
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    mod.main()


def gen_thumbs(out, sheet):
    """Vignettes 40×24 des niveaux (ui/thumb_<id>.png) tirées des décors installés, et l'étoile de difficulté (ADR-032).
    À relancer après un changement de décor : python3 tools/gen-kenta-assets.py --only thumbs"""
    for b in BIOMES:
        i = b["id"]
        layers = [Image.open(os.path.join(out, "biomes", f"bg_{i}_{k}.png")).convert("RGBA").crop((0, 0, 640, 360))
                  for k in ("sky", "far", "mid", "near")]
        put(out, "ui", f"thumb_{i}", thumbs.make_thumbnail(layers), sheet)
    put(out, "ui", "star", ui.draw_star(), sheet)


def gen_hud(out, sheet):
    """Icônes du HUD en partie (ui/hud_*.png, 9×9 : corps 7×7 à la hauteur des lettres + contour). Seules : relancer
    avec --only hud ne réécrit rien d'autre."""
    put(out, "ui", "hud_heart", ui.draw_hud_heart(True), sheet)
    put(out, "ui", "hud_heart_empty", ui.draw_hud_heart(False), sheet)
    put(out, "ui", "hud_coin", ui.draw_hud_coin(), sheet)
    put(out, "ui", "hud_best", ui.draw_hud_best(), sheet)


GENERATORS = {"biomes": gen_biomes, "ui": gen_ui, "hud": gen_hud, "font": gen_font, "heroes": gen_heroes,
              "thumbnail": gen_thumbnail, "thumbs": gen_thumbs}


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
