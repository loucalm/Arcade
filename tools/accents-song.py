#!/usr/bin/env python3
"""Relève les attaques d'un morceau (les temps où quelque chose frappe), sur une grille fine (1/6 de temps : croches,
triolets et doubles croches), avec un seuil relatif au voisinage (±8 temps) : les passages calmes ont aussi leurs
accents. Sortie : liste JSON [[beat, force relative]], lue par un niveau pour poser ses actions sur la musique.
Usage : python3 tools/accents-song.py audio.ogg sortie.json --bpm 280 --offset 0.054 --beats 464"""
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
    y, sr = librosa.load(a.audio, sr=22050)
    hop = 128
    o = librosa.onset.onset_strength(y=y, sr=sr, hop_length=hop, aggregate=np.median)
    spb = 60 / a.bpm
    n = a.beats * 6
    v = np.zeros(n)
    for i in range(n):
        t = a.offset + i * spb / 6
        f0, f1 = int((t - 0.015) * sr / hop), int((t + 0.02) * sr / hop) + 1
        v[i] = o[max(0, f0):f1].max() if f1 > f0 else 0
    loc = np.array([np.percentile(v[max(0, i - 48):i + 48], 97) for i in range(n)])
    r = v / np.maximum(loc, 1e-6)
    peaks = [i for i in range(1, n - 1) if r[i] >= a.seuil and v[i] >= v[i - 1] and v[i] >= v[i + 1]]
    json.dump([(round(i / 6, 3), round(float(r[i]), 2)) for i in peaks], open(a.out, "w"))
    print(f"{len(peaks)} accents")


if __name__ == "__main__":
    main()
