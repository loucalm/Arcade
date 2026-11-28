"""Événements de décor `weather` et `eruption` (météo calée sur la musique, ADR-047). Fichier à part de test_levels.py,
qui génère tous les niveaux à l'import."""
import os
import sys

import pytest

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
from levelgen.builder import Level  # noqa: E402
from levelgen.fiche import mechanics_of  # noqa: E402


def test_weather_event():
    L = Level()
    L.weather(128, "rain", 0.4, 8)
    e = L.ev[-1]
    assert e["type"] == "weather"
    assert e["beat"] == 128
    assert e["params"] == {"variant": "rain", "intensity": 0.4, "length": 8.0}


def test_weather_unknown_kind():
    with pytest.raises(ValueError):
        Level().weather(0, "fog", 1)


def test_eruption_spacing():
    L = Level()
    L.eruption(96)
    with pytest.raises(ValueError):
        L.eruption(97.5)
    L.eruption(98)
    assert [e["beat"] for e in L.ev if e["type"] == "eruption"] == [96, 98]


def test_eruption_spacing_any_order():
    L = Level()
    L.eruption(100)
    with pytest.raises(ValueError):
        L.eruption(99)


def test_decor_events_are_not_mechanics():
    L = Level()
    L.weather(0, "snow", 0.3, 0)
    L.eruption(4)
    assert mechanics_of(L.ev) == set()
