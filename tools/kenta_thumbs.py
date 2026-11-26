"""Vignettes des niveaux pour l'écran de sélection (ADR-032).

Le décor 640×360 d'un biome (ciel + calques) est recadré (600×360, rapport 5:3), réduit en moyenne par blocs de
15×15 px, puis chaque pixel est ramené à la couleur la plus proche parmi celles du décor : la vignette 40×24 reste du
pixel art du biome, sans demi-teintes floues.
"""
import numpy as np
from PIL import Image


def make_thumbnail(layers, size=(40, 24), window=(0, 0, 600, 360)):
    """layers : images RGBA de même taille, du fond (ciel) vers l'avant. Retourne une image RGBA opaque `size`."""
    comp = Image.new("RGBA", layers[0].size, (0, 0, 0, 255))
    for im in layers:
        comp.alpha_composite(im)
    x, y, w, h = window
    a = np.array(comp.crop((x, y, x + w, y + h)).convert("RGB"), dtype=np.float64)
    tw, th = size
    bx, by = w // tw, h // th
    blocks = a[:th * by, :tw * bx].reshape(th, by, tw, bx, 3).mean(axis=(1, 3))

    palette = np.unique(a.reshape(-1, 3), axis=0)
    dist = ((blocks[:, :, None, :] - palette[None, None, :, :]) ** 2).sum(axis=3)
    out = palette[dist.argmin(axis=2)].astype(np.uint8)
    alpha = np.full((th, tw, 1), 255, dtype=np.uint8)
    return Image.fromarray(np.concatenate([out, alpha], axis=2), "RGBA")
