"""Vignettes des niveaux (écran de sélection, ADR-030) : 40×24, opaques, sans couleur étrangère au décor du biome."""
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
import kenta_thumbs as thumbs  # noqa: E402
import kenta_ui as ui  # noqa: E402


def layers():
    """Ciel en deux bandes, un calque lointain à moitié transparent, un sol plein : 640×360 comme les vrais décors."""
    sky = Image.new("RGBA", (640, 360), (120, 180, 200, 255))
    sky.paste((150, 200, 210, 255), (0, 180, 640, 360))
    far = Image.new("RGBA", (640, 360), (0, 0, 0, 0))
    far.paste((60, 70, 110, 255), (100, 120, 400, 300))
    near = Image.new("RGBA", (640, 360), (0, 0, 0, 0))
    near.paste((40, 30, 20, 255), (0, 300, 640, 360))
    near.paste((220, 60, 90, 255), (500, 200, 560, 300))
    return [sky, far, near]


def source_colors(ls, window=(0, 0, 600, 360)):
    comp = Image.new("RGBA", ls[0].size, (0, 0, 0, 255))
    for im in ls:
        comp.alpha_composite(im)
    a = np.array(comp.crop((window[0], window[1], window[0] + window[2], window[1] + window[3])))
    return {tuple(c[:3]) for c in a.reshape(-1, 4) if c[3] == 255}


def test_thumbnail_size_and_opaque():
    im = thumbs.make_thumbnail(layers())
    assert im.size == (40, 24)
    assert np.array(im)[:, :, 3].min() == 255


def test_thumbnail_uses_only_source_colors():
    im = np.array(thumbs.make_thumbnail(layers()))
    used = {tuple(c[:3]) for c in im.reshape(-1, 4)}
    assert used <= source_colors(layers())
    assert len(used) >= 3                      # on reconnaît encore le décor (ciel, montagne, sol)


def test_thumbnail_is_deterministic():
    assert thumbs.make_thumbnail(layers()).tobytes() == thumbs.make_thumbnail(layers()).tobytes()


def test_star_shape():
    im = np.array(ui.draw_star().im)
    assert im.shape[:2] == (5, 5)
    assert (im[:, :, 3] > 0).sum() == 14             # 1 + 3 + 5 + 3 + 2
