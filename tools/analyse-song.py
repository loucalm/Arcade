#!/usr/bin/env python3
"""Analyse un morceau et écrit sa « carte » rythmique et mélodique (JSON, versionné à côté des outils).

Pour chaque double-croche (0,25 beat) du morceau : la force des attaques dans trois bandes (grave = grosse caisse,
médium = caisse claire / accords, aigu = charleston), normalisées 0…1 sur le morceau, et la note de mélodie
dominante (MIDI, ou null) estimée sur la bande 400-2000 Hz. Le générateur de niveau s'en sert pour poser les
actions sur les vraies attaques et faire jouer la mélodie aux pièces (comme les lums d'un niveau musical).

Dépendances : numpy, librosa, soundfile (tools/requirements.txt). Le générateur, lui, ne lit que le JSON.
Usage : python3 tools/analyse-song.py game/Assets/_Project/Audio/Music/level01_shovel_knight.ogg \
          --bpm 150 --offset 0.18 --beats 256 [sortie.json]
"""
import argparse
import json
import os

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("audio")
    ap.add_argument("out", nargs="?", default=os.path.join(ROOT, "tools", "data", "level01_analysis.json"))
    ap.add_argument("--bpm", type=float, default=150.0)
    ap.add_argument("--offset", type=float, default=0.18)
    ap.add_argument("--beats", type=int, default=256)
    args = ap.parse_args()

    import librosa
    y, sr = librosa.load(args.audio, sr=22050, mono=True)
    hop = 128
    S = np.abs(librosa.stft(y, n_fft=2048, hop_length=hop))
    f = librosa.fft_frequencies(sr=sr, n_fft=2048)
    fps = sr / hop
    spb = 60.0 / args.bpm
    n = args.beats * 4

    def onset(lo, hi):
        band = np.log1p(S[(f >= lo) & (f < hi)].sum(axis=0))
        d = np.maximum(0.0, np.diff(band, prepend=band[0]))
        return d

    bands = {"lo": onset(30, 150), "mid": onset(150, 2500), "hi": onset(5000, 11000)}
    grid = {}
    for name, d in bands.items():
        vals = []
        for k in range(n):
            t = args.offset + k * 0.25 * spb
            i0, i1 = int((t - 0.03) * fps), int((t + 0.05) * fps)
            vals.append(float(d[max(0, i0):max(i0 + 1, i1)].max()) if i0 < len(d) else 0.0)
        v = np.array(vals)
        top = np.percentile(v, 98) or 1.0
        grid[name] = [round(float(min(1.0, x / top)), 3) for x in v]

    # mélodie : hauteur dominante sur la bande 400-2000 Hz (spectre « harmonique » pour écarter les percussions)
    H, _ = librosa.decompose.hpss(S)
    band = (f >= 400) & (f < 2000)
    fb = f[band]
    melody = []
    for k in range(n):
        t = args.offset + k * 0.25 * spb
        i0, i1 = int(t * fps), int((t + 0.25 * spb) * fps)
        if i0 >= H.shape[1]:
            melody.append(None)
            continue
        spec = H[band, i0:max(i0 + 1, i1)].mean(axis=1)
        if spec.max() < np.percentile(H[band], 75):
            melody.append(None)
            continue
        midi = int(round(librosa.hz_to_midi(fb[int(spec.argmax())])))
        melody.append(midi)
    chroma = librosa.feature.chroma_cqt(y=y, sr=sr).mean(axis=1)
    key = ["do", "do#", "ré", "ré#", "mi", "fa", "fa#", "sol", "sol#", "la", "la#", "si"][int(chroma.argmax())]

    data = {"bpm": args.bpm, "offset": args.offset, "beats": args.beats, "step": 0.25, "tonic": key,
            "lo": grid["lo"], "mid": grid["mid"], "hi": grid["hi"], "melody": melody}
    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    with open(args.out, "w", encoding="utf-8") as fh:
        json.dump(data, fh, ensure_ascii=False, separators=(",", ":"))
    notes = [m for m in melody if m is not None]
    print(f"{n} double-croches, tonique estimée {key}, mélodie : {len(notes)} notes (MIDI {min(notes)}-{max(notes)})")


if __name__ == "__main__":
    main()
