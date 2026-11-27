"""Les niveaux doivent rester faisables et lisibles : autoplay à 60 et 30 i/s, appuis décalés de ±40 ms, toutes les
pièces sur la trajectoire, un rail de caméra qui garde le héros dans l'image, et seulement les mécaniques de leur
fiche (ADR-035). Le moteur commun doit aussi marcher à un autre tempo que celui du niveau 01."""
import importlib
import json
import os
import pkgutil
import sys

import pytest

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, ".."))

from levelgen import engine  # noqa: E402
from levelgen.builder import Level  # noqa: E402
from levelgen.fiche import Fiche, mechanics_of  # noqa: E402
import levels  # noqa: E402

NAMES = sorted(m.name for m in pkgutil.iter_modules(levels.__path__))
FICHES = {n: importlib.import_module(f"levels.{n}").FICHE for n in NAMES}
RESULTS = {n: engine.generate(f) for n, f in FICHES.items()}      # (events, rail, stats, problèmes)
FICHE = FICHES["shovel_knight"]
EVENTS, RAIL, STATS, PROBLEMS = RESULTS["shovel_knight"]


@pytest.mark.parametrize("name", NAMES)
def test_playable_everywhere(name):
    assert RESULTS[name][3] == []


@pytest.mark.parametrize("name", NAMES)
def test_generated_chart_is_the_committed_one(name):
    """La chart du dépôt est bien celle que produit le générateur (personne ne l'a retouchée à la main)."""
    events, rail, stats, _ = RESULTS[name]
    with open(engine.chart_path(FICHES[name]), encoding="utf-8") as f:
        chart = json.load(f)
    assert chart["events"] == events
    assert chart["rail"] == rail
    assert chart["checkpoints"] == [round(c, 2) for c in stats["checkpoints"]]


@pytest.mark.parametrize("name", NAMES)
def test_all_lums_on_path(name):
    assert RESULTS[name][2]["missed"] == []


def test_real_verticality_in_the_full_level():
    assert STATS["max_y"] - STATS["min_y"] > 20


@pytest.mark.parametrize("name", NAMES)
def test_lums_play_notes_in_the_scale_of_the_song(name):
    for e in RESULTS[name][0]:
        if e["type"] == "lum_at":
            n = e["params"]["note"]
            assert -9 <= n <= 10
            assert (engine.LUM_BASE_MIDI + n) % 12 in FICHES[name].scale


@pytest.mark.parametrize("name", NAMES)
def test_rail_keeps_hero_in_view(name):
    events, rail, _, _ = RESULTS[name]
    engine.use(FICHES[name])
    track = []
    engine.simulate(events, 120, track)
    beats = [tr[0] for tr in track]
    zooms = engine.zooms_from_events(events)                    # vue de la fiche, plans larges (événements `camera`)
    for i, c in enumerate(rail):
        vh = zooms[i] * 9 / 16 / 40.0
        feet = engine.feet_at(track, beats, i * 0.25)
        inverted, _ = engine.track_state(track, beats, i * 0.25)
        top, bottom = (feet, feet - 1.0) if inverted else (feet + 1.0, feet)   # au plafond, la tête est en bas
        assert top + 0.4 <= c + vh * 0.5 + 1e-6, (i * 0.25, "tête")       # le haut du corps dans l'image
        assert bottom >= c - vh * 0.5 - 1e-6, (i * 0.25, "pieds")        # le bas du corps dans l'image


@pytest.mark.parametrize("name", NAMES)
def test_level_keeps_one_world(name):
    """Un niveau = un thème (le décor suit le LevelDef, ADR-032) : aucune section ne change de monde, sauf celles que
    la fiche nomme (niveau 9 : la Sylve, puis le monde sourd, ADR-040)."""
    named = FICHES[name].biomes
    if name != "n9_sourdine":
        assert named == {}
    with open(engine.chart_path(FICHES[name]), encoding="utf-8") as f:
        chart = json.load(f)
    assert all(s.get("biome") == named.get(s["name"]) for s in chart["sections"])


def test_level_1_only_teaches_jump_and_hit():
    """Niveau 1 (ADR-035) : sauter et frapper seulement, vue fixe, tout sur des temps (sauts sur le 1, frappes sur
    le 2 ou le 4), une action par mesure environ."""
    events, _, stats, _ = RESULTS["n1_sylve"]
    assert {e["type"] for e in events} <= {"gap", "wall", "step", "enemy", "lum_at", "section_change"}
    assert not any(e["type"] == "camera" for e in events)
    for e in events:
        if e["type"] in ("gap", "wall", "step"):
            assert e["beat"] % 4 == 0, e
        if e["type"] == "enemy":
            assert e["beat"] % 4 in (1, 3), e
    assert stats["density"] <= 1.5


def test_difficulty_increases_from_one_level_to_the_next():
    """ADR-035 : chaque niveau garde les mécaniques du précédent et en ajoute, il est plus dense, plus rapide, et sa
    marge de survie n'est jamais plus large."""
    ordered = sorted(n for n in NAMES if n[0] == "n" and n[1:2].isdigit())
    assert ordered[:2] == ["n1_sylve", "n2_bayou"]
    for a, b in zip(ordered, ordered[1:]):
        fa, fb = FICHES[a], FICHES[b]
        # actions par seconde (la densité est par mesure de jeu ; le niveau 9 joue sur une grille doublée, 280 BPM)
        per_s = lambda n: RESULTS[n][2]["density"] * FICHES[n].bpm / 240.0
        assert per_s(a) < per_s(b), (a, b, per_s(a), per_s(b))
        assert fa.margin_ms >= fb.margin_ms, (a, b)
        if b == "n9_sourdine":                       # le défi de l'élite (ADR-040) : tout, plus dense, sur un tempo plus lent
            assert set(fa.mechanics) <= set(fb.mechanics), (a, b)
            continue
        assert set(fa.mechanics) < set(fb.mechanics), (a, b)
        assert fa.bpm <= fb.bpm, (a, b)


def test_level_2_teaches_vines_and_stompers():
    events = RESULTS["n2_bayou"][0]
    assert sum(1 for e in events if e["type"] == "vine") >= 8
    assert sum(1 for e in events if e["type"] == "enemy" and e.get("params", {}).get("variant") == "stomp") >= 8
    assert not any(e["type"] in ("camera", "screen_flip", "screen_split", "glitch", "gravity") for e in events)


def test_level_3_teaches_bats_jump_notes_and_gravity():
    events = RESULTS["n3_echos"][0]
    assert sum(1 for e in events if e["type"] == "enemy" and e["lane"] == 1) >= 6          # chauves-souris
    assert sum(1 for e in events if e["type"] == "orb") >= 10
    assert sum(1 for e in events if e["type"] == "gravity") >= 6
    assert sum(1 for e in events if e["type"] == "enemy" and e["lane"] == 2) >= 6          # ennemis pendus
    assert not any(e["type"] in ("screen_flip", "screen_split", "glitch", "ramp", "bounce", "back") for e in events)


def test_level_4_teaches_drums_ramps_and_back_plane():
    """La Forge : tambours (dont une chaîne), rampes (course sur le mur et plongeon), arrière-plan, plans larges ; pas
    encore d'effet d'écran (niveau 5). Un cran au-dessus des Échos et en dessous du niveau de base (le cumul) sur chaque
    critère : actions par seconde, contretemps, dénivelé ; moins de plans larges que le niveau de base."""
    events, _, stats, _ = RESULTS["n4_forge"]
    assert sum(1 for e in events if e["type"] == "bounce" and "variant" not in e.get("params", {})) >= 16
    ramps = [e["params"]["height"] for e in events if e["type"] == "ramp"]
    assert len(ramps) >= 8
    assert sum(1 for e in events if e["type"] == "back") >= 4
    assert any(e["type"] == "camera" for e in events)
    assert stats["max_y"] - stats["min_y"] > 30
    assert not any(e["type"] in ("screen_flip", "screen_split", "glitch") for e in events)
    per_second = lambda n: RESULTS[n][2]["density"] * FICHES[n].bpm / 240.0

    def offbeat(n):
        w = engine.World(RESULTS[n][0])
        acts = list(w.jumps) + list(w.hits)
        return sum(1 for a in acts if abs(a - round(a)) > 1e-6) / len(acts)

    def height(n):
        return RESULTS[n][2]["max_y"] - RESULTS[n][2]["min_y"]

    def wide(n):
        zooms = engine.zooms_from_events(RESULTS[n][0])
        return sum(1 for z in zooms if z > FICHES[n].zoom) / len(zooms)

    for crit in (per_second, offbeat, height):
        assert crit("n3_echos") < crit("n4_forge") < crit("shovel_knight"), crit.__name__
    assert wide("n4_forge") < wide("shovel_knight")


def test_obstacle_variety():
    """Le niveau 01 (futur niveau 6, le cumul) a toutes les mécaniques du jeu."""
    kinds = {e["type"] for e in EVENTS}
    for k in ("gap", "wall", "enemy", "bounce", "platform", "step", "ramp",
              "vine", "gravity", "ceiling", "back", "screen_split", "glitch"):
        assert k in kinds, k
    assert sum(1 for e in EVENTS if e["type"] == "wall") >= 2
    assert sum(1 for e in EVENTS if e["type"] == "enemy" and e["lane"] == 1) >= 10
    assert mechanics_of(EVENTS) == set(FICHE.mechanics)


# ------------------------------------------------------------------ le moteur à un autre tempo, avec une fiche réduite

def tiny_build():
    """Un mini-niveau façon niveau 1 : sauter, une souche, frapper, deux marches."""
    L = Level()
    for b in (4, 9, 13, 21):
        L.gap(b, 0.5)
    L.wall(7)
    for b in (11, 17, 25):
        L.enemy(b)
    L.step(15, +1)
    L.step(19, +1)
    L.enemy(28)
    L.add(16, "section_change", intensity=0.5)
    L.ev.sort(key=lambda e: (e["beat"], e["type"]))
    return L.ev


def tiny_fiche(**kw):
    base = dict(id="test_tiny", title="Test", bpm=117.0, offset=0.0, beats=32,
                sections=[("intro", 0), ("couplet", 16)], analysis="level01_analysis.json", build=tiny_build,
                zoom=480, zoom_wide=480, mechanics=frozenset({"gap", "wall", "enemy", "step"}), margin_ms=150.0)
    base.update(kw)
    return Fiche(**base)


def test_margins_are_in_ms_whatever_the_tempo():
    engine.use(tiny_fiche())
    assert abs(engine.MIN_MARGIN - 150.0 / 1000 * 117 / 60) < 1e-9        # 150 ms à 117 BPM ≈ 0,29 beat
    assert abs(engine.HUMAN_JITTER * engine.SPB - 0.040) < 1e-9
    engine.use(FICHE)
    assert engine.MIN_MARGIN == 0.3                                      # 120 ms à 150 BPM, comme avant (ADR-024)


def test_other_tempo_level_is_generated_and_checked():
    events, rail, stats, problems = engine.generate(tiny_fiche())
    assert problems == []
    assert abs(engine.RUN_SPEED - 6.24) < 1e-9                           # 3,2 u par temps à 117 BPM
    assert not any(e["type"] == "camera" for e in events)                # vue fixe : aucun changement de plan
    assert stats["missed"] == []
    assert len(stats["checkpoints"]) >= 2


def test_fast_run_at_slow_tempo_loses_the_early_hit_margin():
    """Pourquoi la vitesse suit le tempo : à 117 BPM et 8 u/s, une frappe 150 ms en avance n'atteint pas l'ennemi."""
    _, _, _, problems = engine.generate(tiny_fiche(run_speed=8.0))
    assert any(p.startswith("frappe") and "d'avance" in p for p in problems)


def test_mechanic_outside_the_fiche_is_refused():
    _, _, _, problems = engine.generate(tiny_fiche(mechanics=frozenset({"gap", "wall", "enemy"})))
    assert any("hors de la fiche" in p and "step" in p for p in problems)
