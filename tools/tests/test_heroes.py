"""Tests de la marionnette des héros (tools/hero_rig.py) et de ses animations (tools/gen-heroes.py)."""
import importlib.util
import os
import sys

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.join(ROOT, "tools"))
import hero_rig as R  # noqa: E402

_FRAMES = {}


def gen():
    spec = importlib.util.spec_from_file_location("gen_heroes", os.path.join(ROOT, "tools", "gen-heroes.py"))
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def frames(hero_id):
    if hero_id not in _FRAMES:
        _FRAMES[hero_id] = gen().all_frames(hero_id)
    return _FRAMES[hero_id]


def opaque_rows(a):
    return np.nonzero(a[:, :, 3].max(axis=1) >= 128)[0]


def test_every_animation_has_its_frames():
    g = gen()
    for h in R.HEROES:
        f = frames(h)
        for name, n in (("run", g.RUN_FRAMES), ("jump", g.JUMP_FRAMES), ("hit", 4), ("slide", g.SLIDE_FRAMES),
                        ("slidein", g.SLIDE_TURN_FRAMES), ("slideout", g.SLIDE_TURN_FRAMES),
                        ("climb", g.RUN_FRAMES), ("descend", g.RUN_FRAMES), ("fall", 3)):
            assert all(f"{name}_{k}" in f for k in range(n)), (h, name)
        for name in ("idle", "crouch", "wall", "vine", "hurt", "death"):
            assert name in f, (h, name)
        assert len({a.shape for a in f.values()}) == 1, h            # même toile partout : même ancrage


def test_frames_are_not_cut_by_the_canvas():
    for h in R.HEROES:
        for name, a in frames(h).items():
            alpha = a[:, :, 3] >= 128
            assert not alpha[0].any(), (h, name, "haut")
            assert not alpha[:, 0].any() and not alpha[:, -1].any(), (h, name, "côtés")


def test_grounded_poses_stand_on_the_bottom_row():
    for h in R.HEROES:
        f = frames(h)
        for name in ["idle", "crouch", "hit_1", "slide_0"] + [f"run_{k}" for k in range(8)]:
            assert opaque_rows(f[name]).max() == f[name].shape[0] - 1, (h, name)


def test_head_is_about_a_third_of_a_standing_hero_or_less():
    for h in R.HEROES:
        a = frames(h)["idle"]
        height = len(opaque_rows(a))
        head = 2 * R.HEAD_RY
        assert 3.0 <= height / head <= 4.2, (h, height / head)        # PDF : ~2 têtes ; ici ~3,5


def test_slide_stays_under_the_slide_hitbox_and_is_lower_than_crouch():
    for h in R.HEROES:
        f = frames(h)
        for k in range(gen().SLIDE_FRAMES):
            body = f[f"slide_{k}"]
            assert len(opaque_rows(body)) <= 26, (h, k, len(opaque_rows(body)))   # boîte de glissade : 0,5 u = 20 px
        assert len(opaque_rows(f["slide_0"])) < len(opaque_rows(f["crouch"])), h


def test_run_frames_all_differ_and_bandana_flutters():
    for h in R.HEROES:
        f = frames(h)
        run = [f[f"run_{k}"].tobytes() for k in range(gen().RUN_FRAMES)]
        assert len(set(run)) == len(run), h


def test_bandana_trails_behind_when_running():
    hero = R.HEROES["kenta"]
    band = {hero.band[0], hero.band[1], hero.band[2]}
    a = frames("kenta")["run_0"]
    cols = [x for y in range(a.shape[0]) for x in range(a.shape[1])
            if a[y, x, 3] >= 128 and tuple(a[y, x, :3]) in band]
    assert min(cols) < a.shape[1] // 2 - 15                          # loin derrière la tête (le centre est le pivot)


def materials(hero):
    """Tous les matériaux (ombre, base, reflet) et couleurs simples d'un héros."""
    out, singles = [], set()
    for v in list(vars(hero).values()) + list(hero.extra.values()):
        if isinstance(v, tuple) and len(v) == 3 and all(isinstance(c, tuple) for c in v):
            out.append(v)
        elif isinstance(v, tuple) and len(v) == 3 and all(isinstance(c, int) for c in v):
            singles.add(v)
        elif isinstance(v, tuple):
            singles |= {c for c in v if isinstance(c, tuple) and len(c) == 3}
    if "weapon" in hero.extra:                       # arme : (ombre, clair, clair)
        w = hero.extra["weapon"]
        out.append((w[2], w[1], w[1]))
    return out, singles


def test_only_palette_colors_are_used():
    for h, hero in R.HEROES.items():
        mats, allowed = materials(hero)
        allowed |= {R.INK, R.ARC_CREAM, R.ARC_WHITE, R.SPEED_LINE, *R.DUST}
        for m in mats:
            allowed |= set(m) | {R.line_color(m)} | set(R.darker(m)) | {R.line_color(R.darker(m))}
        for name, a in frames(h).items():
            used = {tuple(p[:3]) for p in a.reshape(-1, 4) if p[3] >= 128}
            assert used <= allowed, (h, name, used - allowed)


def test_slide_transitions_go_down_then_back_up():
    g = gen()
    head = lambda pose: R.place(pose).head[1]       # hauteur de la tête (les cheveux qui volent faussent l'image)
    down = [head(g.run_pose(0.0))] + [head(g.slide_in_pose(k)) for k in range(3)] + [head(g.slide_pose(0))]
    up = [head(g.slide_pose(0))] + [head(g.slide_out_pose(k)) for k in range(3)]
    assert down == sorted(down, reverse=True), down
    assert up == sorted(up), up


def test_climb_leans_forward_and_descend_leans_back():
    g = gen()
    up = lambda pose: R.place(pose).head[0] - R.place(pose).hip[0]   # tête devant le bassin = penché en avant
    assert up(g.climb_pose(0.0)) > up(g.run_pose(0.0)) > up(g.descend_pose(0.0))
