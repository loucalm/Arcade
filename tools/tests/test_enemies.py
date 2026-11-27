"""Tests de la danse des ennemis du PDF (tools/gen-enemies.py) : écrasé et étiré, sans nouvelle couleur."""
import importlib.util
import os

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def gen():
    spec = importlib.util.spec_from_file_location("gen_enemies", os.path.join(ROOT, "tools", "gen-enemies.py"))
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def bbox_size(a):
    rows = np.nonzero(a[:, :, 3].max(axis=1) >= 128)[0]
    cols = np.nonzero(a[:, :, 3].max(axis=0) >= 128)[0]
    return cols.max() - cols.min() + 1, rows.max() - rows.min() + 1


def test_squash_is_lower_and_wider_stretch_is_taller_and_narrower():
    g = gen()
    for name in g.ENEMIES:
        base = g.load(name)
        f = g.frames(name)
        bw, bh = bbox_size(base)
        sw, sh = bbox_size(f["squash"])
        tw, th = bbox_size(f["stretch"])
        assert sh < bh < th, (name, sh, bh, th)
        assert tw < bw < sw, (name, tw, bw, sw)
        assert (f["squash"].shape[1] - base.shape[1]) % 2 == 0, name        # le milieu (pivot) ne bouge pas
        assert (base.shape[1] - f["stretch"].shape[1]) % 2 == 0, name


def test_feet_stay_on_the_bottom_row_and_no_new_colour():
    g = gen()
    for name in g.ENEMIES:
        base = g.load(name)
        colours = {tuple(p[:3]) for p in base.reshape(-1, 4) if p[3] >= 128}
        for kind, a in g.frames(name).items():
            assert a[-1, :, 3].max() >= 128, (name, kind)
            used = {tuple(p[:3]) for p in a.reshape(-1, 4) if p[3] >= 128}
            assert used <= colours, (name, kind)


def test_white_eyes_are_kept():
    """Les yeux blancs ne sont jamais rognés (les défenses du morse, blanches aussi, peuvent perdre 1 ou 2 px)."""
    g = gen()
    for name in g.ENEMIES:
        base = g.load(name)
        eyes = sum(1 for p in base.reshape(-1, 4) if g.is_eye(p))
        for kind, a in g.frames(name).items():
            assert sum(1 for p in a.reshape(-1, 4) if g.is_eye(p)) >= 0.9 * eyes, (name, kind)
