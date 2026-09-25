#!/usr/bin/env python3
"""Génère les bruitages d'action (WAV mono 16 bits 44,1 kHz), sans dépendance.
Usage : gen-sfx.py dossier_sortie"""
import math, os, random, struct, sys, wave
SR = 44100
rng = random.Random(7)

def save(path, s):
    peak = max(1e-9, max(abs(x) for x in s))
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(b"".join(struct.pack("<h", int(x / peak * 0.85 * 32767)) for x in s))

def sweep(f0, f1, dur, decay, wave_fn=math.sin):
    out, ph, n = [], 0.0, int(dur * SR)
    for i in range(n):
        t = i / n
        ph += 2 * math.pi * (f0 + (f1 - f0) * t) / SR
        out.append(wave_fn(ph) * math.exp(-t * decay) * min(1, i / 60))
    return out

def noise(dur, decay, lp=0.5):
    out, prev = [], 0.0
    for i in range(int(dur * SR)):
        prev = prev + lp * ((rng.random() * 2 - 1) - prev)
        out.append(prev * math.exp(-i / SR * decay))
    return out

def mix(*parts):
    n = max(len(p) for p in parts)
    return [sum(p[i] for p in parts if i < len(p)) for i in range(n)]

square = lambda ph: 1.0 if math.sin(ph) >= 0 else -1.0
out = sys.argv[1]; os.makedirs(out, exist_ok=True)
save(os.path.join(out, "sfx_jump.wav"), sweep(320, 980, 0.12, 4, square))
save(os.path.join(out, "sfx_hit.wav"), noise(0.07, 45, 0.9))
save(os.path.join(out, "sfx_kill.wav"), mix(noise(0.16, 22, 0.6), [0.8 * x for x in sweep(520, 90, 0.16, 6)]))
save(os.path.join(out, "sfx_slide.wav"), noise(0.22, 9, 0.25))
save(os.path.join(out, "sfx_lum.wav"), mix(sweep(880, 880, 0.18, 14), [0.3 * x for x in sweep(1760, 1760, 0.18, 22)]))
save(os.path.join(out, "sfx_land.wav"), sweep(140, 50, 0.07, 10))
save(os.path.join(out, "sfx_hurt.wav"), sweep(260, 55, 0.4, 3, lambda ph: (ph / math.pi % 2) - 1))
save(os.path.join(out, "sfx_checkpoint.wav"), sweep(523, 523, 0.12, 5) + sweep(784, 784, 0.3, 6))
print("ok")
