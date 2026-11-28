"""Tests du validateur de l'ancien générateur (tools/gen-level-chart.py). La chart du niveau 5, qu'il produisait,
est maintenant générée et vérifiée par tools/gen-level.py (fiche shovel_knight, tools/tests/test_levels.py)."""
import importlib.util
import os

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def gen():
    spec = importlib.util.spec_from_file_location("gen_level_chart", os.path.join(ROOT, "tools", "gen-level-chart.py"))
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def ev(beat, t, **params):
    return {"beat": beat, "type": t, "lane": 0, "params": params}


def test_validate_rejects_too_high_step():
    problems = gen().validate([ev(4, "step", height=1.2)])
    assert problems and "montée" in problems[0]


def test_validate_rejects_too_high_platform():
    problems = gen().validate([ev(4, "step", height=0.5), ev(8, "platform", height=1.8, length=2)])
    assert any("montée" in p for p in problems)


def test_validate_rejects_jumps_too_close():
    problems = gen().validate([ev(4, "gap"), ev(4.5, "wall")])
    assert any("sauts" in p for p in problems)


def test_validate_rejects_enemy_during_jump():
    problems = gen().validate([ev(4, "gap"), ev(4.5, "enemy")])
    assert any("ennemi" in p for p in problems)


def test_validate_accepts_enemy_on_landing():
    assert gen().validate([ev(4, "gap"), ev(5, "enemy")]) == []


def test_validate_rejects_height_out_of_range():
    problems = gen().validate([ev(4, "step", height=0.8), ev(8, "step", height=1.8), ev(12, "step", height=2.6)])
    assert any("hauteur" in p for p in problems)


def test_validate_rejects_short_platform():
    problems = gen().validate([ev(4, "platform", height=0.5, length=0.5)])
    assert any("plateforme" in p for p in problems)


def test_descending_step_needs_no_jump():
    assert gen().validate([ev(4, "step", height=1.0), ev(8, "step", height=0.0), ev(8.5, "enemy")]) == []
