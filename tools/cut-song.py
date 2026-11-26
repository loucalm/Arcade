#!/usr/bin/env python3
"""Monte le morceau d'un niveau : garde des mesures entières d'un morceau à tempo fixe, les recolle bout à bout et
écrit un .ogg (Vorbis) pour Unity (ADR-034).

Les coupes tombent sur des débuts de mesure, avec un fondu enchaîné très court (15 ms, fini pile sur la coupe) :
la grille des temps reste exacte, donc le beat 0 du niveau est à `--offset` secondes, comme dans le morceau
d'origine (le premier morceau commence au début du fichier).
Une **boucle** (fichier qui se raccorde sur lui-même, comme les packs de Juhani Junkala) se rejoue en copies entières :
passer `--offset 0` (les coupes tombent alors sur le début du fichier), et garder l'offset mesuré pour la chart et
`analyse-song.py`. Couper la boucle à son offset mesuré décale la grille des temps à chaque raccord (vérifié le
2026-11-26 sur *Stage 1* : attaques entre les croches après chaque raccord).

Exemples :
  python3 tools/cut-song.py source.mp3 sortie.ogg --bpm 117 --offset 0.054 --bars 0-31,64-end
  python3 tools/cut-song.py boucle.wav sortie.ogg --bpm 140 --offset 0 --bars 0-23,0-23,0-15 --fade 8
Dépendances : numpy, librosa, soundfile (tools/requirements.txt).
"""
import argparse

import numpy as np


def parse_bars(spec):
    out = []
    for part in spec.split(","):
        a, b = part.split("-")
        out.append((int(a), None if b == "end" else int(b)))
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("source")
    ap.add_argument("out")
    ap.add_argument("--bpm", type=float, required=True)
    ap.add_argument("--offset", type=float, required=True, help="secondes du début du fichier au premier temps fort")
    ap.add_argument("--bars", required=True, help="mesures gardées, incluses : 0-31,64-end")
    ap.add_argument("--beats-per-bar", type=int, default=4)
    ap.add_argument("--fade", type=float, default=0.0, help="fondu de sortie sur les N derniers temps")
    ap.add_argument("--xfade-ms", type=float, default=15.0)
    ap.add_argument("--quality", type=float, default=0.5, help="qualité Vorbis de 0 à 1")
    args = ap.parse_args()

    import librosa
    import soundfile as sf
    y, sr = librosa.load(args.source, sr=None, mono=False)
    y = np.atleast_2d(y)                                     # (canaux, échantillons)
    bar = args.beats_per_bar * 60.0 / args.bpm
    total = y.shape[1]
    xf = int(args.xfade_ms / 1000 * sr)
    pieces = []
    for i, (a, b) in enumerate(parse_bars(args.bars)):
        s0 = 0 if i == 0 and a == 0 else int(round((args.offset + a * bar) * sr))
        s1 = total if b is None else min(total, int(round((args.offset + (b + 1) * bar) * sr)))
        if i == 0 and a != 0:
            raise SystemExit("le premier morceau doit commencer à la mesure 0 (sinon l'offset ne tient plus)")
        pieces.append((s0, s1))
    out = y[:, pieces[0][0]:pieces[0][1]].copy()
    for s0, s1 in pieces[1:]:
        c = min(xf, s0, out.shape[1])
        if c > 0:                                            # fondu enchaîné qui finit pile sur la coupe
            ramp = np.linspace(0.0, 1.0, c, endpoint=False)
            out[:, -c:] = out[:, -c:] * np.cos(ramp * np.pi / 2) + y[:, s0 - c:s0] * np.sin(ramp * np.pi / 2)
        out = np.concatenate([out, y[:, s0:s1]], axis=1)
    if args.fade > 0:
        n = min(out.shape[1], int(args.fade * 60.0 / args.bpm * sr))
        out[:, -n:] *= np.linspace(1.0, 0.0, n) ** 2
    peak = float(np.abs(out).max())
    if peak > 0.999:
        out *= 0.999 / peak
    # écriture par blocs : en un seul appel, l'encodeur Vorbis de libsndfile plante (erreur 139) au-delà d'environ
    # 80 s de stéréo 44,1 kHz
    with sf.SoundFile(args.out, "w", sr, out.shape[0], format="OGG", subtype="VORBIS") as fh:
        frames = out.T
        for i in range(0, frames.shape[0], 1 << 15):
            fh.write(frames[i:i + (1 << 15)])
    beats = (out.shape[1] / sr - args.offset) * args.bpm / 60.0
    print(f"{args.out} : {out.shape[1] / sr:.2f} s, {sr} Hz, {out.shape[0]} canal(aux), {beats:.1f} temps après l'offset")


if __name__ == "__main__":
    main()
