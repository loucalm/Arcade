#!/usr/bin/env python3
"""Vérifie la structure et la faisabilité d'une chart Rythme Runner."""
import json
import sys
from collections import defaultdict

KNOWN = {"gap", "wall", "enemy", "block", "slide_bar", "wall_run", "hook",
         "lum_line", "fx_flash", "camera_shake", "camera_zoom", "section_change"}
JUMPS = {"gap", "wall"}
OBSTACLES = {"gap", "wall", "enemy", "block", "slide_bar", "wall_run", "hook"}
PRESENTATION = {"fx_flash", "camera_shake", "camera_zoom", "section_change"}
EPS = 1e-7


def beat_ok(value):
    return abs(value * 4 - round(value * 4)) < EPS


def param(event, name, default=None):
    return event.get("params", {}).get(name, default)


def main(path):
    problems = []
    try:
        with open(path, encoding="utf-8") as stream:
            chart = json.load(stream)
    except (OSError, json.JSONDecodeError) as exc:
        print(f"0 | json | {exc}")
        return 1

    sections = chart.get("sections", [])
    events = chart.get("events", [])
    starts = [s.get("startBeat") for s in sections]
    section_names = defaultdict(int)
    for index, section in enumerate(sections):
        name = section.get("name")
        section_names[name] += 1
        if not isinstance(section.get("startBeat"), (int, float)):
            problems.append((0, "section", f"startBeat manquant/invalide à l'index {index}"))
        if index and section.get("startBeat", 0) <= starts[index - 1]:
            problems.append((section.get("startBeat", 0), "sections-triées", "les débuts de section doivent croître"))
    if not sections:
        problems.append((0, "sections", "au moins une section est requise"))
    if len(set(starts)) != len(starts):
        problems.append((0, "sections-uniques", "deux sections ont le même début"))

    previous = None
    for index, event in enumerate(events):
        beat = event.get("beat")
        kind = event.get("type")
        if not isinstance(beat, (int, float)):
            problems.append((0, "beat", f"événement {index} sans beat numérique"))
            continue
        if previous is not None and beat < previous - EPS:
            problems.append((beat, "événements-triés", f"événement {index} après un beat {previous}"))
        previous = beat
        if not beat_ok(beat):
            problems.append((beat, "granularité", "le beat doit être un multiple de 0,25"))
        if kind not in KNOWN:
            problems.append((beat, "type", f"type inconnu: {kind}"))
            continue
        if kind in ("hook", "wall_run"):
            minimum = 1.5 if kind == "hook" else 2
            length = param(event, "length", 2 if kind == "hook" else 4)
            if not isinstance(length, (int, float)) or length < minimum:
                problems.append((beat, "longueur-auto", f"{kind}: length doit être >= {minimum}"))
        if kind == "slide_bar" and param(event, "length", 1) <= 0:
            problems.append((beat, "glissade", "length doit être positif"))
        if kind == "lum_line":
            count, step = param(event, "count", 1), param(event, "step", 0.5)
            if not isinstance(count, int) or count <= 0 or not isinstance(step, (int, float)) or step <= 0:
                problems.append((beat, "lums", "count doit être entier positif et step positif"))

    def section_for(beat):
        result = None
        for index, start in enumerate(starts):
            if beat >= start:
                result = index
            else:
                break
        return result

    actions_by_section = defaultdict(int)
    for event in events:
        beat, kind = event.get("beat"), event.get("type")
        if not isinstance(beat, (int, float)) or kind in PRESENTATION:
            continue
        index = section_for(beat)
        if index is not None:
            actions_by_section[index] += 1

    # Chaque section annonce sa première action après une mesure.
    for index, start in enumerate(starts):
        end = starts[index + 1] if index + 1 < len(starts) else chart.get("lengthBeats", 200)
        gameplay = [e["beat"] for e in events if e.get("type") not in PRESENTATION
                    and isinstance(e.get("beat"), (int, float)) and start <= e["beat"] < end]
        if gameplay and gameplay[0] < start + chart.get("beatsPerBar", 4) - EPS:
            problems.append((gameplay[0], "télégraphie", f"première action de {sections[index].get('name')} avant une mesure"))

    # Sauts, ennemis pendant un saut et incompatibilités avec les glissades.
    jumps = [e for e in events if e.get("type") in JUMPS]
    for left, right in zip(jumps, jumps[1:]):
        if right["beat"] - left["beat"] < 1 - EPS:
            problems.append((right["beat"], "espacement-sauts", "deux sauts doivent être espacés d'au moins 1 beat"))
    for jump in jumps:
        start, finish = jump["beat"], jump["beat"] + 1
        for event in events:
            if event.get("type") == "enemy" and start + EPS < event.get("beat", -1) < finish - EPS:
                problems.append((event["beat"], "ennemi-en-saut", f"ennemi pendant le saut lancé à {start}"))
        for bar in events:
            if bar.get("type") == "slide_bar":
                bar_start = bar["beat"] - 1.2
                bar_end = bar["beat"] + param(bar, "length", 1)
                if start < bar_end - EPS and finish > bar_start + EPS:
                    problems.append((jump["beat"], "saut-glissade", "saut interdit dans la fenêtre de glissade"))

    # Fenêtres exclusives des obstacles automatiques et limites de section.
    for event in events:
        kind, beat = event.get("type"), event.get("beat")
        if kind not in ("hook", "wall_run") or not isinstance(beat, (int, float)):
            continue
        length = param(event, "length", 2 if kind == "hook" else 4)
        margin = 1.5 if kind == "hook" else 1
        end = beat + length
        section = section_for(beat)
        if section is None or end > (starts[section + 1] if section + 1 < len(starts) else chart.get("lengthBeats", 200)) - 1 + EPS:
            problems.append((beat, "marge-fin-section", f"{kind} doit finir au moins 1 beat avant la fin de section"))
        if any(start > beat + EPS and start < end + margin - EPS for start in starts[1:]):
            problems.append((beat, "début-section", f"{kind} chevauche un début de section"))
        for other in events:
            other_kind, other_beat = other.get("type"), other.get("beat")
            if other is event or other_kind not in OBSTACLES or not isinstance(other_beat, (int, float)):
                continue
            if beat - 1 - EPS <= other_beat <= end + margin + EPS:
                problems.append((other_beat, "marge-auto", f"{other_kind} dans la fenêtre interdite autour de {kind}"))

    for event in events:
        if event.get("type") in ("hook", "wall_run") and param(event, "count", 0) > 0:
            if param(event, "count") < 1:
                problems.append((event["beat"], "lums-auto", "count doit être positif"))

    for beat, rule, detail in sorted(set(problems), key=lambda item: (item[0], item[1], item[2])):
        print(f"{beat:g} | {rule} | {detail}")
    if problems:
        print(f"PROBLÈMES: {len(problems)}")
    else:
        print("PROBLÈMES: 0")

    for index, section in enumerate(sections):
        start = section["startBeat"]
        end = starts[index + 1] if index + 1 < len(starts) else chart.get("lengthBeats", 200)
        count = actions_by_section[index]
        density = count / (end - start) if end > start else 0
        print(f"SECTION {section.get('name')} {start:g}-{end:g}: {count} actions, densité {density:.3f} action/beat")
    return 1 if problems else 0


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("usage: check-chart.py CHART.json")
        sys.exit(2)
    sys.exit(main(sys.argv[1]))
