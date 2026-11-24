"""Le niveau vertical doit rester faisable et lisible : autoplay à 60 et 30 i/s, appuis décalés de ±40 ms,
toutes les pièces sur la trajectoire, un vrai dénivelé, un rail de caméra qui garde le héros dans l'image."""
import importlib.util
import os

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("gen_level_vertical", os.path.join(HERE, "..", "gen-level-vertical.py"))
gen = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gen)

EVENTS, RAIL, STATS, PROBLEMS = gen.generate()


def test_playable_everywhere():
    assert PROBLEMS == []


def test_all_lums_on_path_and_real_verticality():
    assert STATS["missed"] == []
    assert STATS["max_y"] - STATS["min_y"] > 20


def test_lums_play_notes_in_pentatonic():
    for e in EVENTS:
        if e["type"] == "lum_at":
            n = e["params"]["note"]
            assert -9 <= n <= 10
            assert (gen.LUM_BASE_MIDI + n) % 12 in gen.PENTA


def test_rail_keeps_hero_in_view():
    track = []
    gen.simulate(EVENTS, 120, track)
    beats = [b for b, _ in track]
    for i, c in enumerate(RAIL):
        feet = gen.track_at(track, beats, i * 0.25) - 0.5
        assert feet + 1.4 <= c + gen.VIEW_H * 0.5 + 1e-6      # la tête dans l'image
        assert feet >= c - gen.VIEW_H * 0.5 - 1e-6            # les pieds dans l'image


def test_slides_are_part_of_the_level():
    """La glissade est 1 des 3 commandes : elle doit revenir plusieurs fois, dans les deux passages du morceau."""
    slides = [e["beat"] for e in EVENTS if e["type"] == "slide_bar"]
    assert len(slides) >= 5
    assert any(b < 128 for b in slides) and any(b >= 152 for b in slides)


def test_obstacle_variety():
    """Pas seulement des champignons et des trous : murs, glissades, tambours, plateformes et ennemis en vol."""
    kinds = {e["type"] for e in EVENTS}
    for k in ("gap", "wall", "slide_bar", "enemy", "bounce", "platform", "step", "ramp"):
        assert k in kinds, k
    assert sum(1 for e in EVENTS if e["type"] == "wall") >= 4
    assert sum(1 for e in EVENTS if e["type"] == "enemy" and e["lane"] == 1) >= 10
