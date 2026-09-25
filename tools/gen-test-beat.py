#!/usr/bin/env python3
"""Génère une piste de test de synchro (WAV mono 16 bits, 44,1 kHz), sans dépendance.
Kick sur chaque temps (accentué sur le 1er temps de la mesure), charleston sur les croches,
basse simple par mesure. La durée fait un nombre entier de beats : la boucle reste calée sur la grille.
Usage : gen-test-beat.py sortie.wav --bpm 120 --beats 32
"""
import argparse, math, random, struct, wave

p = argparse.ArgumentParser()
p.add_argument("out"); p.add_argument("--bpm", type=float, default=120); p.add_argument("--beats", type=int, default=32)
a = p.parse_args()

SR = 44100
spb = 60.0 / a.bpm
n = int(round(a.beats * spb * SR))
buf = [0.0] * n
rng = random.Random(42)

def add(start, samples):
    for i, v in enumerate(samples):
        j = start + i
        if j >= n: break
        buf[j] += v

def kick(gain):
    L = int(0.18 * SR); out = []; phase = 0.0
    for i in range(L):
        t = i / SR
        f = 45 + 110 * math.exp(-t * 35)
        phase += 2 * math.pi * f / SR
        out.append(gain * math.sin(phase) * math.exp(-t * 18))
    return out

def hat(gain):
    L = int(0.035 * SR)
    return [gain * (rng.random() * 2 - 1) * math.exp(-i / SR * 120) for i in range(L)]

def bass(freq, dur, gain):
    L = int(dur * SR)
    return [gain * math.sin(2 * math.pi * freq * i / SR) * min(1, i / 400) * math.exp(-i / SR * 3) for i in range(L)]

roots = [55.0, 55.0, 73.42, 65.41]  # La, La, Ré, Do
for b in range(a.beats):
    s = int(round(b * spb * SR))
    add(s, kick(0.9 if b % 4 == 0 else 0.6))
    add(int(round((b + 0.5) * spb * SR)), hat(0.18))
    if b % 4 == 0:
        add(s, bass(roots[(b // 4) % len(roots)], spb * 3.5, 0.35))

peak = max(1e-9, max(abs(x) for x in buf))
with wave.open(a.out, "wb") as w:
    w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
    w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, x / peak * 0.9)) * 32767)) for x in buf))
print(f"{a.out} : {a.beats} beats à {a.bpm} BPM = {n / SR:.3f} s")
