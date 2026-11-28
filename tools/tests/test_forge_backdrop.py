"""Décor de la Forge redessiné d'après la maquette du PDF (page 9) et cratères de l'éruption (vents.json)."""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
import kenta_art as art  # noqa: E402
from kenta_biomes import BIOMES  # noqa: E402

FORGE = next(b for b in BIOMES if b["id"] == "forge")
GLOW = {(0xFF, 0xA0, 0x40), (0xFF, 0x7A, 0x1A)}


def test_vents_json_has_forge():
    j = art.vents_json(FORGE)
    assert j["id"] == "forge"
    assert len(j["vents"]) >= 1
    for x, y in j["vents"]:
        assert 0 <= x < 1280 and 0 <= y < 360


def test_crater_is_glowing():
    a = np.array(art.draw_far(FORGE).im)
    for x, y in art.vents_json(FORGE)["vents"]:
        x, y = int(x), int(y)
        win = a[max(0, y - 3):y + 4, max(0, x - 3):x + 4]
        colors = {tuple(c[:3]) for c in win.reshape(-1, 4) if c[3] == 255}
        assert colors & GLOW, f"cratère ({x}, {y}) sans lueur"


def test_single_big_volcano():
    vents = art.vents_json(FORGE)["vents"]
    assert len(vents) == 2            # un volcan par boucle de 640 (composition), 2 copies dans l'image de 1280
    for _, y in vents:
        assert y < 140                # sommet dans le tiers haut de l'image


def test_mid_not_repeated():
    a = np.array(art.draw_mid(FORGE, foot=False).im)[:, :, 3]
    """Plus le même rocher répété : les pics varient en hauteur ET en largeur (le calque actuel ne varie qu'en
    hauteur, ~46 px de large partout)."""
    tops, widths, in_peak, top, x0 = [], [], False, 999, 0
    for x in range(a.shape[1]):
        ys = np.nonzero(a[:, x])[0]
        if len(ys):
            if not in_peak:
                x0 = x
            top, in_peak = min(top, ys[0]), True
        elif in_peak:
            tops.append(top)
            widths.append(x - x0)
            in_peak, top = False, 999
    assert len({t // 6 for t in tops}) >= 3, f"sommets trop semblables : {tops}"
    assert len({w // 16 for w in widths}) >= 3, f"largeurs trop semblables : {widths}"


def test_other_biomes_have_no_vents():
    sylve = next(b for b in BIOMES if b["id"] == "sylve")
    assert art.vents_json(sylve)["vents"] == []


def test_weather_sprites_sizes():
    """Sprites de la météo et de l'éruption (WeatherFx) : tailles fixes, au moins un pixel opaque."""
    sizes = {"rain": (1, 5), "snow_0": (1, 1), "snow_1": (2, 2), "ember": (2, 2),
             "eruption_blob": (4, 4), "eruption_smoke": (12, 8)}
    for name, size in sizes.items():
        im = art.draw_weather(name)
        assert im.size == size, name
        assert np.array(im)[:, :, 3].max() == 255, name
