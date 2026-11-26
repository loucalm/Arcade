#!/usr/bin/env python3
"""Génère la chart d'un niveau à partir de sa fiche et de ses scènes (`tools/levels/<niveau>.py`, ADR-034).

La chart n'est écrite que si l'autoplay finit le niveau sans mourir (à 60 et 30 i/s, appuis décalés de ±40 ms), si
chaque action laisse la marge de la fiche des deux côtés, si chaque point de reprise est rejouable, si chaque ennemi
se voit assez tôt et si le niveau n'utilise que les mécaniques de sa fiche.

Usage :
  python3 tools/gen-level.py <niveau> [sortie.json] [--table]   écrire la chart (défaut : Charts/<id de la fiche>.json)
  python3 tools/gen-level.py <niveau> --map                     carte des attaques du morceau, mesure par mesure
  python3 tools/gen-level.py <niveau> --marges DE A             marge de survie (ms) de chaque action entre deux beats
  python3 tools/gen-level.py --liste                            niveaux disponibles et leur fiche
"""
import argparse
import importlib
import json
import os
import pkgutil
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import levels  # noqa: E402
from levelgen import engine  # noqa: E402


def load_level(name):
    return importlib.import_module(f"levels.{name}").FICHE


def level_names():
    return sorted(m.name for m in pkgutil.iter_modules(levels.__path__))


def main():
    ap = argparse.ArgumentParser(description="Génère la chart d'un niveau de Kenta.")
    ap.add_argument("niveau", nargs="?", help="nom du fichier dans tools/levels (sans .py)")
    ap.add_argument("out", nargs="?", help="chart à écrire (défaut : Charts/<id>.json)")
    ap.add_argument("--table", action="store_true", help="afficher le niveau mesure par mesure")
    ap.add_argument("--map", action="store_true", help="afficher la carte des attaques du morceau")
    ap.add_argument("--marges", nargs=2, type=float, metavar=("DE", "A"),
                    help="marge de survie de chaque action entre deux beats (en ms)")
    ap.add_argument("--liste", action="store_true", help="lister les niveaux")
    args = ap.parse_args()
    if args.liste or not args.niveau:
        for name in level_names():
            f = load_level(name)
            print(f"{name:20s} {f.id:28s} {f.bpm:6.1f} BPM  {f.beats} beats  marge {f.margin_ms:.0f} ms  "
                  f"{len(f.mechanics)} mécaniques")
        return
    fiche = load_level(args.niveau)
    out = args.out or engine.chart_path(fiche)
    engine.use(fiche)
    if args.map:
        print(engine.song_map(engine.load_analysis()))
        return
    if args.marges:
        with open(out, encoding="utf-8") as f:
            evs = json.load(f)["events"]
        for b, kind, early, late in engine.action_windows(evs, *args.marges):
            ms = lambda v: f"{v * engine.SPB * 1000:4.0f}"
            flag = "  <-- serré" if min(early, late) < engine.MIN_MARGIN - 1e-6 else ""
            print(f"beat {b:6.2f} {kind:6s} avance {ms(early)} ms, retard {ms(late)} ms{flag}")
        return
    events, rail, stats, problems = engine.generate(fiche)
    if args.table:
        print(engine.table(events))
    if problems:
        print("Chart non jouable :", *problems, sep="\n  - ")
        sys.exit(1)
    engine.write(events, rail, out, stats.get("checkpoints", ()))
    n = {t: sum(1 for e in events if e["type"] == t) for t in sorted({e["type"] for e in events})}
    print(f"{len(events)} événements : {n}")
    print(f"dénivelé : de {stats['min_y']:.1f} à {stats['max_y']:.1f} u")
    print(f"densité : {stats['density']} actions par mesure")
    print(f"pièces ramassées par l'autoplay : {stats['lums'] - len(stats['missed'])}/{stats['lums']}")
    print(f"points de reprise (die and retry) : {len(stats['checkpoints'])} : {stats['checkpoints']}")
    if stats["missed"]:
        print("  ratées aux beats :", stats["missed"])


if __name__ == "__main__":
    main()
