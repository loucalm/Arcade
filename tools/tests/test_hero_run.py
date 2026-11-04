"""Tests du générateur de course à 6 images (tools/gen-hero-run.py)."""
import importlib.util
import os

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PDF = os.path.join(ROOT, "game", "Assets", "_Project", "Art", "Kenta", "Pdf")
HEROES = ("kenta", "aiko", "ryo")


def load_module():
    spec = importlib.util.spec_from_file_location("gen_hero_run", os.path.join(ROOT, "tools", "gen-hero-run.py"))
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def colors(img):
    a = np.array(img.convert("RGBA"))
    return {tuple(p[:3]) for p in a.reshape(-1, 4) if p[3] >= 128}


def test_six_frames_same_size_as_source():
    mod = load_module()
    for h in HEROES:
        src = Image.open(os.path.join(PDF, f"hero_{h}_run.png")).convert("RGBA")
        frames = mod.run_cycle(src)
        assert len(frames) == 6
        assert all(f.size == src.size for f in frames)


def test_feet_touch_the_ground_on_most_frames():
    mod = load_module()
    for h in HEROES:
        src = Image.open(os.path.join(PDF, f"hero_{h}_run.png")).convert("RGBA")
        frames = mod.run_cycle(src)
        grounded = sum(1 for f in frames if np.array(f)[-1, :, 3].max() >= 128)
        assert grounded >= 4, (h, grounded)


def test_frames_are_all_different():
    mod = load_module()
    for h in HEROES:
        src = Image.open(os.path.join(PDF, f"hero_{h}_run.png")).convert("RGBA")
        frames = [np.array(f).tobytes() for f in mod.run_cycle(src)]
        assert len(set(frames)) == 6, h


def test_only_source_palette_is_used():
    mod = load_module()
    for h in HEROES:
        src = Image.open(os.path.join(PDF, f"hero_{h}_run.png")).convert("RGBA")
        allowed = colors(src)
        for f in mod.run_cycle(src):
            assert colors(f) <= allowed, (h, colors(f) - allowed)


def test_upper_body_is_kept():
    mod = load_module()
    src = Image.open(os.path.join(PDF, "hero_kenta_run.png")).convert("RGBA")
    a = np.array(src)
    for f in mod.run_cycle(src):
        b = np.array(f)
        top = a.shape[0] // 3   # la tête est identique, à un pixel de rebond près
        assert any(np.array_equal(a[2:top], b[2 + d:top + d]) for d in (0, 1))
