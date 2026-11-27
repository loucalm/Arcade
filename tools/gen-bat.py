#!/usr/bin/env python3
"""Anime la chauve-souris du PDF : quatre images de battement d'ailes, et un liseré clair pour la voir sur tous les décors.

Retour de Lucas (2026-11-26) : on ne voyait presque pas les chauves-souris (violet foncé cerné de noir, sur la grotte
violette des Échos), et le halo jaune ajouté derrière elles le 25/11 se lisait comme un bouclier, alors qu'on doit les
frapper. On garde le dessin du PDF (`Art/Kenta/Pdf/enemy_chauve.png`) : seules les ailes bougent, autour de la ligne
de l'épaule (haut = le dessin du PDF, milieu = ailes ramenées à l'horizontale, bas = ailes rabattues sous l'épaule) ; la
tête, le corps et les pattes ne changent pas. Puis un liseré d'1 px, clair et froid (pas la couleur des pièces), tout
autour de la silhouette : il la détache d'un fond sombre sans rien suggérer d'autre.
Sortie : game/Assets/_Project/Art/Kenta/Gen/world/bat_{0..3}.png (pivot aux pieds, KentaSpritePostprocessor), dans
l'ordre haut, milieu, bas, milieu. Usage : python3 tools/gen-bat.py
"""
import os

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "game", "Assets", "_Project", "Art", "Kenta", "Pdf", "enemy_chauve.png")
OUT = os.path.join(ROOT, "game", "Assets", "_Project", "Art", "Kenta", "Gen", "world")

BODY = (19, 34)          # colonnes du corps et de la tête (incluses) : tout ce qui est en dehors est une aile
SHOULDER = 24            # ligne de l'épaule : les ailes pivotent autour d'elle
WING_BOTTOM = 34         # dernière ligne des ailes : en dessous, les pattes et les chaussures ne bougent pas
RIM = (238, 232, 255, 255)   # liseré : blanc lavande, ni or (pièces) ni rouge (danger)
POSES = (("haut", 1.0), ("milieu", 0.45), ("bas", -0.7), ("milieu", 0.45))   # facteur vertical des ailes


def pose(src, factor):
    """Ailes étirées verticalement autour de l'épaule (facteur négatif : rabattues dessous). Lecture inverse au plus
    proche voisin : chaque pixel cible va chercher son pixel source, donc ni trou ni flou. Le corps, et tout ce qui
    est sous la ligne des ailes (pattes, chaussures), reste en place."""
    w, h = src.size
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    s, o = src.load(), out.load()
    for x in range(w):
        wing = x < BODY[0] or x > BODY[1]
        for y in range(h):
            if not wing or y > WING_BOTTOM:
                o[x, y] = s[x, y]
                continue
            sy = int(round(SHOULDER + (y - SHOULDER) / factor))
            if 0 <= sy <= WING_BOTTOM:
                o[x, y] = s[x, sy]
    return out


def rim(im):
    """Liseré d'1 px autour des pixels opaques (sur une toile agrandie d'1 px de chaque côté)."""
    w, h = im.size
    out = Image.new("RGBA", (w + 2, h + 2), (0, 0, 0, 0))
    out.paste(im, (1, 1))
    p = out.load()
    solid = {(x, y) for x in range(w + 2) for y in range(h + 2) if p[x, y][3] >= 128}
    for x, y in list(solid):
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + dx, y + dy)
            if n not in solid and 0 <= n[0] < w + 2 and 0 <= n[1] < h + 2:
                p[n] = RIM
    return out


def frames():
    src = Image.open(SRC).convert("RGBA")
    return [rim(pose(src, f)) for _, f in POSES]


def main():
    if os.path.exists(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "game", "Assets", "_Project", "Art",
                                   "Kenta", "Pdf", ".gemini")) and "--force" not in __import__("sys").argv:
        raise SystemExit("Les ennemis et objets viennent de Gemini (tools/gemini-ennemis/clean.py) : rien n'est écrasé. "
                         "--force pour revenir aux anciens sprites.")
    os.makedirs(OUT, exist_ok=True)
    for i, im in enumerate(frames()):
        im.save(os.path.join(OUT, f"bat_{i}.png"))
    print(f"{len(POSES)} images de chauve-souris dans {OUT}")


if __name__ == "__main__":
    main()
