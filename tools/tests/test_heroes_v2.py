"""Tests des sprites v2 des héros installés dans Gen/heroes/ (tools/heros-v2/import.py, ADR-045)."""
import os

import numpy as np
import pytest
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
HEROES_DIR = os.path.join(ROOT, "game", "Assets", "_Project", "Art", "Kenta", "Gen", "heroes")
HEROES = ("kenta", "aiko", "ryo")
SIZE = (96, 72)
HIT_SIZE = (160, 72)
HIT_REACH_PX = 76  # PlayerController.hitReach = 1,9 u × 40 px/u
INK = (0x14, 0x10, 0x1C)
ARC_CREAM = (0xFF, 0xF6, 0xC8)
ARC_WHITE = (0xFF, 0xFF, 0xFF)

# Poses seules produites par tools/heros-v2/import.py (run, jump et hit viennent des cycles dans KentaBuilder)
SINGLES = ("idle", "crouch", "vine", "wall", "hurt", "death")


def expected_families(hero):
    return {
        "idle": 6,
        "run": 9 if hero == "ryo" else 8,
        "jump": 8,
        "hit": 6,
        "climb": 8,
        "descend": 8,
        "fall": 3,
        "vine": 2,
        "hurt": 2,
    }


def expected_names(hero):
    names = list(SINGLES)
    for fam, count in expected_families(hero).items():
        names.extend(f"{fam}_{k}" for k in range(count))
    return names


def sprite_path(hero, name):
    return os.path.join(HEROES_DIR, f"hero_{hero}_{name}.png")


@pytest.mark.parametrize("hero", HEROES)
def test_every_family_exists_with_expected_count(hero):
    for fam, count in expected_families(hero).items():
        for k in range(count):
            path = sprite_path(hero, f"{fam}_{k}")
            assert os.path.isfile(path), f"image manquante : {path}"
        extra = sprite_path(hero, f"{fam}_{count}")
        assert not os.path.exists(extra), f"image en trop : {extra}"
    for name in SINGLES:
        path = sprite_path(hero, name)
        assert os.path.isfile(path), f"pose manquante : {path}"


@pytest.mark.parametrize("hero", HEROES)
def test_all_sprites_have_expected_size_rgba_and_non_empty(hero):
    for name in expected_names(hero):
        expected_size = HIT_SIZE if name.startswith("hit_") else SIZE
        with Image.open(sprite_path(hero, name)) as im:
            assert im.mode == "RGBA", (hero, name, im.mode)
            assert im.size == expected_size, (hero, name, im.size)
            alpha = np.asarray(im)[:, :, 3]
            assert (alpha >= 128).any(), (hero, name, "image vide")


@pytest.mark.parametrize("hero", HEROES)
@pytest.mark.parametrize("frame", ("hit_2", "hit_3"))
def test_hit_slash_reaches_hit_reach_px(hero, frame):
    with Image.open(sprite_path(hero, frame)) as im:
        a = np.asarray(im)
        cols = np.nonzero(a[:, :, 3].max(axis=0) >= 128)[0]
        assert len(cols) > 0, (hero, frame, "image vide")
        expected_col = HIT_SIZE[0] // 2 + HIT_REACH_PX
        assert abs(int(cols.max()) - expected_col) <= 1, (hero, frame, int(cols.max()), expected_col)


@pytest.mark.parametrize("hero", HEROES)
def test_hit_1_has_no_slash_trail(hero):
    with Image.open(sprite_path(hero, "hit_1")) as im:
        a = np.asarray(im)
        cols = np.nonzero(a[:, :, 3].max(axis=0) >= 128)[0]
        assert len(cols) > 0, (hero, "hit_1", "image vide")
        max_col = HIT_SIZE[0] // 2 + 40
        assert int(cols.max()) <= max_col, (hero, int(cols.max()), max_col)


@pytest.mark.parametrize("hero", HEROES)
def test_hit_2_slash_is_connected_to_hero(hero):
    with Image.open(sprite_path(hero, "hit_2")) as im:
        a = np.asarray(im)
        alpha = a[:, :, 3] >= 128
        rgb = a[:, :, :3]
        xs = np.arange(HIT_SIZE[0])[None, :]
        is_cream = alpha & np.all(rgb == ARC_CREAM, axis=-1)
        is_white = alpha & np.all(rgb == ARC_WHITE, axis=-1)
        trail_fill = is_cream | (is_white & (xs >= 120))
        counts = trail_fill[:, 120:].sum(axis=1)
        assert int(counts.max()) > 0, (hero, "traînée absente")
        row = int(round(float(np.mean(np.nonzero(counts == counts.max())[0]))))
        is_ink = np.all(rgb[row] == INK, axis=-1)
        hero_cols = np.nonzero(alpha[row] & ~trail_fill[row] & ~is_ink & (np.arange(HIT_SIZE[0]) < 125))[0]
        assert len(hero_cols) > 0, (hero, row, "aucun pixel du héros sur la rangée de la traînée")
        hero_front = int(hero_cols.max())
        assert alpha[row, hero_front:151].all(), (hero, row, hero_front)


def max_consecutive(mask_1d):
    best = cur = 0
    for val in mask_1d:
        if val:
            cur += 1
            if cur > best:
                best = cur
        else:
            cur = 0
    return best


@pytest.mark.parametrize("hero", HEROES)
def test_hit_3_slash_is_thin(hero):
    with Image.open(sprite_path(hero, "hit_3")) as im:
        a = np.asarray(im)
        cream = (a[:, :, 3] >= 128) & np.all(a[:, :, :3] == ARC_CREAM, axis=-1)
        assert cream.any(), (hero, "traînée crème absente sur hit_3")
        # Balayage principal (y < 56) et épaisseur verticale (colonnes) <= 22 px ;
        # courbe basse tangentielle (a1 = 166°, rx = 70) <= 42 px.
        mid_row_max = max(max_consecutive(cream[y]) for y in range(56))
        col_max = max(max_consecutive(cream[:, x]) for x in range(HIT_SIZE[0]))
        all_row_max = max(max_consecutive(cream[y]) for y in range(HIT_SIZE[1]))
        assert mid_row_max <= 22, (hero, "rangée trop épaisse", mid_row_max)
        assert col_max <= 22, (hero, "colonne trop épaisse", col_max)
        assert all_row_max <= 42, (hero, "courbe basse trop large", all_row_max)


@pytest.mark.parametrize("hero", HEROES)
@pytest.mark.parametrize("frame", ("hit_2", "hit_3"))
def test_hit_slash_has_ink_outline(hero, frame):
    with Image.open(sprite_path(hero, frame)) as im:
        a = np.asarray(im)
        alpha = a[:, :, 3] >= 128
        rgb = a[:, :, :3]
        xs = np.arange(HIT_SIZE[0])[None, :]
        trail = alpha & (np.all(rgb == ARC_CREAM, axis=-1) | np.all(rgb == ARC_WHITE, axis=-1)) & (xs >= 120)
        assert trail.any(), (hero, frame, "traînée absente")
        adj = np.zeros_like(trail)
        adj[1:] |= trail[:-1]
        adj[:-1] |= trail[1:]
        adj[:, 1:] |= trail[:, :-1]
        adj[:, :-1] |= trail[:, 1:]
        ink = alpha & np.all(rgb == INK, axis=-1)
        border_ink = ink & adj & ~trail
        assert int(border_ink.sum()) > 0, (hero, frame, "aucun contour #14101C autour de la traînée")
        rightmost = int(np.nonzero(alpha.max(axis=0))[0].max())
        assert ink[:, rightmost].any(), (hero, frame, rightmost, "bord extérieur sans #14101C")


def horizontal_center_from_pivot(path):
    with Image.open(path) as im:
        a = np.asarray(im)
        cols = np.nonzero(a[:, :, 3].max(axis=0) >= 128)[0]
        assert len(cols) > 0, f"image vide : {path}"
        return (int(cols.min()) + int(cols.max())) / 2.0 - im.width / 2.0


@pytest.mark.parametrize("hero", HEROES)
def test_hit_0_and_idle_share_horizontal_center_around_pivot(hero):
    idle_cx = horizontal_center_from_pivot(sprite_path(hero, "idle"))
    hit0_cx = horizontal_center_from_pivot(sprite_path(hero, "hit_0"))
    assert abs(hit0_cx - idle_cx) <= 2.0, (hero, idle_cx, hit0_cx)


@pytest.mark.parametrize("hero", HEROES)
def test_grounded_poses_touch_bottom_row(hero):
    run_count = expected_families(hero)["run"]
    grounded = ["idle", "crouch"] + [f"idle_{k}" for k in range(6)] + [f"run_{k}" for k in range(run_count)]
    for name in grounded:
        with Image.open(sprite_path(hero, name)) as im:
            a = np.asarray(im)
            rows = np.nonzero(a[:, :, 3].max(axis=1) >= 128)[0]
            assert len(rows) > 0, (hero, name, "image vide")
            assert rows.max() == SIZE[1] - 1, (hero, name, int(rows.max()))


@pytest.mark.parametrize("hero", HEROES)
def test_v2_marker_exists(hero):
    marker = os.path.join(HEROES_DIR, f".v2_{hero}")
    assert os.path.isfile(marker), f"marqueur manquant : {marker}"
    with open(marker, encoding="utf-8") as f:
        assert f.read().strip(), f"marqueur vide : {marker}"
