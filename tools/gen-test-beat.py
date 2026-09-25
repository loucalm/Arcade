#!/usr/bin/env python3
"""Génère une piste de test (WAV mono 16 bits, 44,1 kHz), sans dépendance.

Mode simple : kick sur chaque temps, charleston sur les croches, basse par mesure.
Mode structuré (--structure) : l'instrumentation change par section, pour entendre les sections :
  intro  = kick + charleston      verse = + caisse claire (2 et 4) + basse
  chorus = + arpège lead          break = charleston + basse seulement
  final  = tout + kicks syncopés
Style rock (--style rock) : groove blues-rock énergique en si mineur, avec batterie,
  basse, riff de power chords saturées et lead filtré dans chorus/final. Dans un break,
  seuls la basse et le charleston sont conservés. La durée fait un nombre entier de beats :
  la boucle reste calée sur la grille.

Usage :
  gen-test-beat.py sortie.wav --bpm 120 --beats 16
  gen-test-beat.py sortie.wav --bpm 120 --structure intro:16,verse:32,chorus:32,break:16,final:32
  gen-test-beat.py sortie.wav --bpm 120 --style rock --structure intro:16,verse:32,chorus:32,break:16,final:32
"""
import argparse, math, random, struct, wave

p = argparse.ArgumentParser()
p.add_argument("out")
p.add_argument("--bpm", type=float, default=120)
p.add_argument("--beats", type=int, default=32)
p.add_argument("--structure", default="")
p.add_argument("--style", default="classic", choices=("classic", "rock"))
a = p.parse_args()

sections = []
if a.structure:
    start = 0
    for part in a.structure.split(","):
        name, length = part.split(":")
        sections.append((name, start, int(length)))
        start += int(length)
    total_beats = start
else:
    total_beats = a.beats
    sections = [("simple", 0, total_beats)]

SR = 44100
spb = 60.0 / a.bpm
n = int(round(total_beats * spb * SR))
buf = [0.0] * n
rng = random.Random(42)

def add(beat, samples):
    start = int(round(beat * spb * SR))
    for i, v in enumerate(samples):
        j = start + i
        if j >= n: break
        buf[j] += v

def kick(gain):
    out, phase = [], 0.0
    for i in range(int(0.18 * SR)):
        t = i / SR
        phase += 2 * math.pi * (45 + 110 * math.exp(-t * 35)) / SR
        out.append(gain * math.sin(phase) * math.exp(-t * 18))
    return out

def hat(gain):
    return [gain * (rng.random() * 2 - 1) * math.exp(-i / SR * 120) for i in range(int(0.035 * SR))]

def snare(gain):
    out = []
    for i in range(int(0.16 * SR)):
        t = i / SR
        out.append(gain * ((rng.random() * 2 - 1) * 0.8 + 0.4 * math.sin(2 * math.pi * 190 * t)) * math.exp(-t * 22))
    return out

def tone(freq, dur, gain, decay=3.0, square=False):
    out = []
    for i in range(int(dur * SR)):
        s = math.sin(2 * math.pi * freq * i / SR)
        if square: s = 0.6 * (1 if s >= 0 else -1) + 0.4 * s
        out.append(gain * s * min(1, i / 300) * math.exp(-i / SR * decay))
    return out

def power_chord(freq, dur, gain):
    out = [0.0] * int(dur * SR)
    for ratio in (1.0, 1.5, 2.0):
        phase = 0.0
        for i in range(len(out)):
            phase += ratio * freq / SR
            out[i] += 2.0 * (phase % 1.0) - 1.0
    return [gain * math.tanh(4.0 * s / 3.0) * min(1, i / 300) * math.exp(-i / SR * 4.0) for i, s in enumerate(out)]

def filtered_square(freq, dur, gain):
    out, phase, filtered = [], 0.0, 0.0
    alpha = 1.0 - math.exp(-2.0 * math.pi * 4200.0 / SR)
    for i in range(int(dur * SR)):
        phase = (phase + freq / SR) % 1.0
        square = 1.0 if phase < 0.5 else -1.0
        filtered += alpha * (square - filtered)
        out.append(gain * filtered * min(1, i / 300) * math.exp(-i / SR * 5.0))
    return out

def tom(freq, gain):
    out, phase = [], 0.0
    for i in range(int(0.13 * SR)):
        t = i / SR
        phase += 2 * math.pi * freq / SR
        out.append(gain * math.sin(phase) * math.exp(-t * 28.0))
    return out

def crash(gain):
    out, filtered = [], 0.0
    alpha = 1.0 - math.exp(-2.0 * math.pi * 9000.0 / SR)
    for i in range(int(1.8 * SR)):
        filtered += alpha * ((rng.random() * 2 - 1) - filtered)
        out.append(gain * filtered * math.exp(-i / SR * 1.8))
    return out

roots = [55.0, 55.0, 73.42, 65.41]            # La, La, Ré, Do
arp = [0, 3, 7, 12, 7, 3, 7, 10]              # arpège mineur (demi-tons)
rock_riff = [123.47, 123.47, 146.83, 123.47, 164.81, 123.47, 146.83, 110.0]  # Si mineur : B B D B E B D A

if a.style == "rock":
    for section_index, (name, s0, length) in enumerate(sections):
        has_next_section = section_index < len(sections) - 1
        for b in range(s0, s0 + length):
            bar_beat = b % 4
            local_beat = b - s0
            if name != "break":
                if bar_beat in (0, 2):
                    add(b, kick(0.9))
                if bar_beat in (1, 3):
                    add(b, snare(0.48))
            add(b, hat(0.16))
            add(b + 0.5, hat(0.12))

            if name != "break" and b == s0:
                add(b, crash(0.22))
            if name != "break" and has_next_section and b == s0 + length - 1:
                for step in range(4):
                    hit = b + step * 0.25
                    if step % 2:
                        add(hit, snare(0.22))
                    else:
                        add(hit, tom(120.0 - step * 18.0, 0.26))

            measure = local_beat // 4
            beat_in_measure = local_beat % 4
            riff_index = (measure * 8 + beat_in_measure * 2) % len(rock_riff)
            add(b, tone(rock_riff[riff_index] / 2.0, spb * 0.9, 0.36, decay=4.0))

            if name in ("verse", "chorus", "final"):
                for step in range(2):
                    note = rock_riff[(measure * 8 + beat_in_measure * 2 + step) % len(rock_riff)]
                    add(b + step * 0.5, power_chord(note, spb * 0.47, 0.22))
            if name in ("chorus", "final"):
                lead_note = rock_riff[riff_index] * 2.0
                add(b, filtered_square(lead_note, spb * 0.75, 0.1))
else:
    for name, s0, length in sections:
        for b in range(s0, s0 + length):
            bar_root = roots[(b // 4) % len(roots)]
            downbeat = b % 4 == 0
            if name != "break":
                add(b, kick(0.9 if downbeat else 0.6))
            add(b + 0.5, hat(0.18))
            if name in ("verse", "chorus", "final", "break", "simple") and downbeat:
                add(b, tone(bar_root, spb * 3.5, 0.35))
            if name in ("verse", "chorus", "final") and b % 2 == 1:
                add(b, snare(0.45))
            if name in ("chorus", "final"):
                for k in range(2):
                    note = bar_root * 4 * 2 ** (arp[(b * 2 + k) % len(arp)] / 12)
                    add(b + k * 0.5, tone(note, spb * 0.45, 0.12, decay=6, square=True))
            if name == "final" and b % 4 == 3:
                add(b + 0.75, kick(0.5))

peak = max(1e-9, max(abs(x) for x in buf))
normalization = 10 ** (-1.0 / 20.0) if a.style == "rock" else 0.9
with wave.open(a.out, "wb") as w:
    w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
    w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, x / peak * normalization)) * 32767)) for x in buf))
print(f"{a.out} : {total_beats} beats à {a.bpm} BPM = {n / SR:.3f} s ; sections : {[(s[0], s[1]) for s in sections]}")
