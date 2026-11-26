"""Le niveau vertical doit rester faisable et lisible : autoplay à 60 et 30 i/s, appuis décalés de ±40 ms,
toutes les pièces sur la trajectoire, un vrai dénivelé, un rail de caméra qui garde le héros dans l'image."""
import importlib.util
import json
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
    beats = [tr[0] for tr in track]
    zooms = gen.zooms_from_events(EVENTS)                       # vue 480, plans larges 640 (événements `camera`)
    for i, c in enumerate(RAIL):
        vh = zooms[i] * 9 / 16 / 40.0
        feet = gen.feet_at(track, beats, i * 0.25)
        inverted, _ = gen.track_state(track, beats, i * 0.25)
        top, bottom = (feet, feet - 1.0) if inverted else (feet + 1.0, feet)   # au plafond, la tête est en bas
        assert top + 0.4 <= c + vh * 0.5 + 1e-6, (i * 0.25, "tête")       # le haut du corps dans l'image
        assert bottom >= c - vh * 0.5 - 1e-6, (i * 0.25, "pieds")        # le bas du corps dans l'image


def test_level_keeps_one_world():
    """Un niveau = un thème : aucune section ne change de monde (le décor suit le LevelDef, ADR-032)."""
    assert gen.BIOMES == {}
    with open(gen.DEFAULT_OUT, encoding="utf-8") as f:
        chart = json.load(f)
    assert all("biome" not in s for s in chart["sections"])


def test_obstacle_variety():
    """Pas seulement des champignons et des trous : murs, tambours, plateformes, ennemis en vol, et les
    mécaniques qui changent le niveau (lianes, gravité inversée, arrière-plan, effets d'écran)."""
    kinds = {e["type"] for e in EVENTS}
    for k in ("gap", "wall", "enemy", "bounce", "platform", "step", "ramp",
              "vine", "gravity", "ceiling", "back", "screen_split", "glitch"):
        assert k in kinds, k
    assert sum(1 for e in EVENTS if e["type"] == "wall") >= 2
    assert sum(1 for e in EVENTS if e["type"] == "enemy" and e["lane"] == 1) >= 10
