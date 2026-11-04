"""Tests de la police pixel proportionnelle à ombre portée (tools/kenta_ui.py)."""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
import kenta_ui as ui  # noqa: E402


def arr(ch):
    return np.array(ui.glyph_image(ch).im)


def test_narrow_letters_are_narrower():
    assert arr("I").shape[1] < arr("M").shape[1]
    assert arr(".").shape[1] < arr("A").shape[1]


def test_space_has_a_width():
    assert arr(" ").shape[1] >= 3


def test_all_glyphs_same_height():
    heights = {arr(c).shape[0] for c in ui.ALL_CHARS}
    assert len(heights) == 1


def test_shadow_one_pixel_below_body():
    a = arr("A")
    body = (a[:, :, 3] > 0) & (a[:, :, 0] > 200)
    shadow = (a[:, :, 3] > 0) & (a[:, :, 0] < 100)
    assert body.any() and shadow.any()
    ys, xs = np.nonzero(body)
    sy, sx = np.nonzero(shadow)
    assert sx.max() == xs.max() and sy.max() == ys.max() + 1


def test_shadow_only_under_body():
    for ch in "AKMW":
        a = arr(ch)
        dark = (a[:, :, 3] > 0) & (a[:, :, 0] < 100)
        body = (a[:, :, 3] > 0) & (a[:, :, 0] > 200)
        h, w = body.shape
        for y in range(h):
            for x in range(w):
                if dark[y, x]:
                    # une ombre est toujours juste sous un pixel du corps
                    assert y > 0 and body[y - 1, x], (ch, x, y)
