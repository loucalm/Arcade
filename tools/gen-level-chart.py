#!/usr/bin/env python3
"""Génère la chart du niveau 01 à partir de la musique (« Main Theme », 150 BPM).

Le morceau est analysé (librosa) : à chaque croche on mesure l'attaque dans trois bandes (grave = grosse caisse,
médium = mélodie / caisse claire, aigu = charleston). Le plan ci-dessous découpe le morceau selon sa structure réelle
(voir PLAN) ; dans chaque section, les obstacles sont posés sur les attaques les plus
fortes, avec des règles de faisabilité (espacement minimal entre actions). Les lums suivent la mélodie.

Le morceau se répète : la musique de [32, 128) revient à [152, 248). Le niveau reprend donc le même dessin
pour que les mêmes sons déclenchent les mêmes actions.

Usage (dépendances en plus : librosa, voir tools/requirements.txt) :
  python3 tools/gen-level-chart.py chemin/vers/morceau.wav [sortie.json] [--table]
"""
import argparse
import json
import os
import sys

import numpy as np

BPM, OFFSET, NB = 150.0, 0.18, 256
SPB = 60.0 / BPM
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DEFAULT_OUT = os.path.join(ROOT, "game", "Assets", "_Project", "Charts", "level01_shovel_knight.json")

JUMPS = {"gap", "wall"}
HITS = {"enemy", "block"}

# Plan : (nom, début, fin, espacement visé en beats, motif de types)
# Les types : gap/wall = sauter ; enemy/block = frapper ; slide = glisser.
PLAN = [
    ("intro", 4, 22, 4.0, ["gap", "gap", "wall", "gap"]),
    ("silence", 24, 28, 99, []),                                  # le morceau respire : un laser long, voir SPECIAL
    ("montée", 28, 32, 1.0, ["enemy", "enemy", "enemy"]),          # attaques de grosse caisse décalées
    ("couplet", 32, 64, 2.0, ["gap", "enemy", "wall", "enemy", "gap", "block", "enemy", "gap"]),
    ("pont", 64, 80, 4.0, ["gap", "enemy", "wall", "enemy"]),     # le morceau s'allège : on souffle
    ("roulement", 80, 96, 1.0, ["enemy", "gap", "enemy", "enemy", "gap", "block"]),   # la grosse caisse tape sur tous les temps
    ("refrain", 96, 120, 2.0, ["gap", "enemy", "gap", "wall", "enemy", "block", "slide", "enemy"]),
    ("retombée", 120, 128, 4.0, ["gap", "enemy"]),
    ("final", 128, 150, 1.5, ["enemy", "enemy", "gap", "enemy", "wall", "enemy", "block", "gap"]),
]
# Passage 2 : la musique de [32,128) revient en [152,248) (décalage de 120 beats).
REPEAT_FROM, REPEAT_TO, REPEAT_SHIFT = 32, 128, 120

SECTIONS = [  # (nom, beat de début) : un checkpoint par section
    ("intro", 0), ("couplet", 32), ("pont", 64), ("roulement", 80), ("refrain", 96), ("final", 128),
    ("couplet 2", 152), ("pont 2", 184), ("roulement 2", 200), ("refrain 2", 216), ("outro", 248),
]


def analyse(path):
    import librosa
    y, sr = librosa.load(path, sr=22050, mono=True)
    H = 128
    fps = sr / H
    S = np.abs(librosa.stft(y, n_fft=2048, hop_length=H))
    f = librosa.fft_frequencies(sr=sr, n_fft=2048)

    def onset(lo, hi):
        b = S[(f >= lo) & (f < hi)].sum(axis=0)
        return np.maximum(0, np.diff(np.log1p(b), prepend=np.log1p(b[0])))

    bands = {"lo": onset(0, 180), "mid": onset(180, 2000), "hi": onset(2000, 10000)}
    grids = {}
    for name, arr in bands.items():
        g = []
        for k in range(NB * 2):
            t0 = OFFSET + k * SPB / 2 - SPB / 8
            a = int(t0 * fps)
            b = max(int((t0 + SPB / 4) * fps), a + 1)
            g.append(arr[max(a, 0):min(b, len(arr))].max() if 0 <= a < len(arr) else 0.0)
        g = np.array(g)
        grids[name] = g / max(np.percentile(g, 80), 1e-9)
    return grids


def slot_score(grids, k):
    return min(grids["lo"][k], 2) * 1.0 + min(grids["mid"][k], 2) * 0.8 + min(grids["hi"][k], 2) * 0.35


def action_end(a):
    """Beat jusqu'auquel l'action occupe le joueur (pour l'espacement)."""
    if a["type"] == "slide_bar":
        return a["beat"] + 0.1 + a["params"].get("length", 1.0) + 0.35
    return a["beat"]


def min_gap(prev, cur):
    """Espacement minimal (beats) entre deux actions consécutives."""
    if prev["type"] == "slide_bar":
        return action_end(prev) + 0.9 - prev["beat"]
    if cur["type"] == "slide_bar":
        return 1.5 if prev["type"] in JUMPS else 1.0
    return 1.0


def make(t, beat):
    if t == "slide":
        return {"beat": beat, "type": "slide_bar", "lane": 0, "params": {"length": 1.0}}
    return {"beat": beat, "type": t, "lane": 0}


def build(grids):
    actions = []

    def feasible(a):
        if not actions:
            return True
        prev = actions[-1]
        return a["beat"] - prev["beat"] >= min_gap(prev, a) - 1e-9

    for name, s, e, spacing, pattern in PLAN:
        if not pattern:
            continue
        i = 0
        w = s
        while w < e - 1e-9:
            # fenêtre [w, w+spacing) : on cherche l'attaque la plus forte (croche), la plus tôt en cas d'égalité
            slots = [k for k in range(int(w * 2), int(min(w + spacing, e) * 2)) if k / 2 >= w]
            slots.sort(key=lambda k: (-slot_score(grids, k) + (0.3 if k % 2 else 0), k))
            placed = False
            for k in slots:
                a = make(pattern[i % len(pattern)], k / 2)
                if feasible(a):
                    actions.append(a)
                    i += 1
                    placed = True
                    break
            w += spacing if placed else 0.5
            if not placed and w >= e:
                break
    # laser long dans le silence (le morceau retient son souffle)
    actions.append({"beat": 25.0, "type": "slide_bar", "lane": 0, "params": {"length": 2.0}})
    actions.sort(key=lambda a: a["beat"])
    # le passage 2 est une copie du passage 1 : sans risque d'incohérence aux jonctions
    first = [a for a in actions if REPEAT_FROM <= a["beat"] < REPEAT_TO]
    for a in first:
        b = json.loads(json.dumps(a))
        b["beat"] += REPEAT_SHIFT
        actions.append(b)
    actions.sort(key=lambda a: a["beat"])
    # vérification dure : jamais deux actions trop proches
    for p, c in zip(actions, actions[1:]):
        assert c["beat"] - p["beat"] >= min_gap(p, c) - 1e-9, (p, c)
    return actions


def lums_for(actions, grids):
    """Arcs de lums au-dessus des sauts, et lignes de mélodie dans les espaces libres."""
    ev = []
    occupied = []
    for a in actions:
        end = action_end(a)
        occupied.append((a["beat"] - 0.9, end + (1.0 if a["type"] in JUMPS else 0.9)))
        if a["type"] in JUMPS:
            ev.append({"beat": a["beat"] + 0.25, "type": "lum_line", "lane": 1, "params": {"count": 3, "step": 0.25}})
    occupied.sort()
    # espaces libres entre actions
    cursor = 1.0
    free = []
    for lo, hi in occupied:
        if lo - cursor >= 2.0:
            free.append((cursor, lo))
        cursor = max(cursor, hi)
    if NB - 3 - cursor >= 2.0:
        free.append((cursor, NB - 3))
    for lo, hi in free:
        start = np.ceil(lo * 2) / 2
        count = int((hi - start) / 0.5)
        count = min(count, 12)
        if count >= 3:
            # mélodie : pas de 0,25 si le morceau est dense (aigus fréquents) sur la zone
            k0, k1 = int(start * 2), int((start + count * 0.5) * 2)
            dense = grids["hi"][k0:k1].mean() > 0.9
            step = 0.25 if dense and count >= 6 else 0.5
            ev.append({"beat": start, "type": "lum_line", "lane": 0, "params": {"count": count, "step": step}})
    # final : volée de lums
    ev.append({"beat": 250.0, "type": "lum_line", "lane": 0, "params": {"count": 10, "step": 0.5}})
    return ev


def fx_events():
    ev = []
    for name, beat in SECTIONS:
        if beat > 0:
            ev.append({"beat": beat, "type": "section_change", "lane": 0, "params": {"intensity": 1.5 if name.startswith(("refrain", "final")) else 0.8}})
    for b in (32, 152):
        ev.append({"beat": b, "type": "camera_shake", "lane": 0, "params": {"intensity": 1.3, "length": 1}})
    for b in (96, 216):
        ev.append({"beat": b, "type": "camera_zoom", "lane": 0, "params": {"intensity": 1.5, "length": 1}})
    for b in list(range(80, 96, 4)) + list(range(128, 150, 4)) + list(range(200, 216, 4)):
        ev.append({"beat": b, "type": "fx_flash", "lane": 0, "params": {"intensity": 1}})
    ev.append({"beat": 128, "type": "camera_shake", "lane": 0, "params": {"intensity": 1.5, "length": 1}})
    return ev


def table(events):
    rows = {}
    sym = {"gap": "G", "wall": "W", "enemy": "E", "block": "B", "slide_bar": "S", "lum_line": "·"}
    for e in events:
        if e["type"] in sym:
            bar, pos = divmod(e["beat"], 4)
            rows.setdefault(int(bar), ["    "] * 4)
            slot = int(pos)
            cell = list(rows[int(bar)][slot])
            cell[int((pos - slot) * 4) % 4] = sym[e["type"]]
            rows[int(bar)][slot] = "".join(cell)
    out = []
    for b in range(NB // 4):
        r = rows.get(b, ["    "] * 4)
        out.append(f"mesure {b:2d} (beat {b * 4:3d}) | " + " | ".join(r))
    return "\n".join(out)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("audio")
    ap.add_argument("out", nargs="?", default=DEFAULT_OUT)
    ap.add_argument("--table", action="store_true")
    args = ap.parse_args()
    grids = analyse(args.audio)
    actions = build(grids)
    events = sorted(actions + lums_for(actions, grids) + fx_events(), key=lambda e: (e["beat"], e["type"]))
    head = {"id": "level01_shovel_knight", "title": "Main Theme - Shovel Knight", "bpm": BPM,
            "offsetMs": int(OFFSET * 1000), "beatsPerBar": 4, "runSpeed": 8.0}
    sections = [{"name": n, "startBeat": b} for n, b in SECTIONS]
    with open(args.out, "w", encoding="utf-8") as f:
        s = json.dumps(head, ensure_ascii=False, indent=2)[:-2]
        s += ',\n  "sections": [\n' + ",\n".join("    " + json.dumps(x, ensure_ascii=False) for x in sections) + "\n  ],\n"
        s += '  "events": [\n' + ",\n".join("    " + json.dumps(x, ensure_ascii=False) for x in events) + "\n  ]\n}\n"
        f.write(s)
    n = {t: sum(1 for e in events if e["type"] == t) for t in sorted({e["type"] for e in events})}
    print(f"{len(events)} événements : {n}")
    if args.table:
        print(table(events))


if __name__ == "__main__":
    main()
