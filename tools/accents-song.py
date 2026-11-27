#!/usr/bin/env python3
"""Relève les attaques d'un morceau (les temps où quelque chose frappe), rangées sur une grille fine (1/6 de temps :
croches, triolets et doubles croches), avec une force relative au voisinage (±8 temps) : les passages calmes ont aussi
leurs accents. Sortie : liste JSON [[beat, force relative]], lue par un niveau pour poser ses actions sur la musique.

Chaque attaque est datée à son **début** (pic de l'enveloppe d'attaque, recalé en arrière jusqu'au creux qui le
précède, à 1,5 ms près), puis rangée dans la case de la grille la plus proche. L'ancienne version prenait le maximum de
l'enveloppe autour de chaque case : ce maximum arrive après le début du son (fenêtre d'analyse de 93 ms), et les
actions tombaient 25 à 40 ms trop tard (mesuré sur le niveau 9, 27/11).
Usage : python3 tools/accents-song.py audio.ogg sortie.json --bpm 280 --offset 0.028 --beats 464 [--seuil 0.4]"""
import argparse
import json

import librosa
import numpy as np


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("audio")
    ap.add_argument("out")
    ap.add_argument("--bpm", type=float, required=True)
    ap.add_argument("--offset", type=float, required=True)
    ap.add_argument("--beats", type=int, required=True)
    ap.add_argument("--seuil", type=float, default=0.55)
    a = ap.parse_args()
    y, sr = librosa.load(a.audio, sr=44100)
    hop = 64
    env = librosa.onset.onset_strength(y=y, sr=sr, hop_length=hop)
    peaks = librosa.onset.onset_detect(onset_envelope=env, sr=sr, hop_length=hop, units="frames", delta=0.02, wait=8)
    starts = librosa.onset.onset_backtrack(peaks, env)
    t_start = librosa.frames_to_time(starts, sr=sr, hop_length=hop)
    cell = 60.0 / a.bpm / 6
    n = a.beats * 6
    v = np.zeros(n)
    for f, t in zip(peaks, t_start):
        i = int(round((t - a.offset) / cell))
        if 0 <= i < n:
            v[i] = max(v[i], float(env[f]))                    # la plus forte attaque de la case
    loc = np.array([np.percentile(v[max(0, i - 48):i + 48], 97) for i in range(n)])
    r = v / np.maximum(loc, 1e-6)
    keep = [i for i in range(n) if r[i] >= a.seuil and v[i] > 0]
    json.dump([(round(i / 6, 3), round(float(r[i]), 2)) for i in keep], open(a.out, "w"))
    print(f"{len(keep)} accents")


if __name__ == "__main__":
    main()
