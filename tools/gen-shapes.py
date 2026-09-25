#!/usr/bin/env python3
"""Génère les sprites de base (PNG RGBA blancs, à teinter dans Unity) : carré, cercle, halo,
triangle (pointe en haut), anneau, étincelle à 4 branches, carré arrondi, dégradé vertical (opaque en bas).
Usage : gen-shapes.py dossier_sortie"""
import math, os, struct, sys, zlib

def write_png(path, w, h, pixels):
    raw = b"".join(b"\x00" + bytes(pixels[y * w * 4:(y + 1) * w * 4]) for y in range(h))
    def chunk(t, d): return struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xFFFFFFFF)
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
                + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))

def shape(size, alpha_fn):
    px = []
    for y in range(size):
        for x in range(size):
            a = max(0.0, min(1.0, alpha_fn((x + 0.5) / size * 2 - 1, (y + 0.5) / size * 2 - 1)))
            px += [255, 255, 255, int(a * 255)]
    return px

out = sys.argv[1]; os.makedirs(out, exist_ok=True)
S = 64
write_png(os.path.join(out, "shape_square.png"), S, S, [255] * (S * S * 4))
write_png(os.path.join(out, "shape_circle.png"), S, S, shape(S, lambda u, v: (1 - math.hypot(u, v)) * S / 2))
G = 128
write_png(os.path.join(out, "shape_glow.png"), G, G, shape(G, lambda u, v: max(0.0, 1 - math.hypot(u, v)) ** 2.2))
write_png(os.path.join(out, "shape_triangle.png"), S, S, shape(S, lambda u, v: min((1 - v) * S / 2, (1 + v - 2 * abs(u)) * S / 4)))
write_png(os.path.join(out, "shape_ring.png"), S, S, shape(S, lambda u, v: (0.22 - abs(math.hypot(u, v) - 0.72)) * S / 2))
write_png(os.path.join(out, "shape_sparkle.png"), S, S, shape(S, lambda u, v: max(0.0, 1 - (abs(u) ** 0.5 + abs(v) ** 0.5)) ** 1.5 * 2.5))
write_png(os.path.join(out, "shape_rounded.png"), S, S, shape(S, lambda u, v: (0.3 - math.hypot(max(abs(u) - 0.7, 0), max(abs(v) - 0.7, 0))) * S / 2))
# Dégradé vertical : alpha 1 en bas vers 0 en haut (les lignes PNG vont du haut vers le bas).
write_png(os.path.join(out, "shape_gradient.png"), S, S, [c for y in range(S) for c in [255, 255, 255, int(255 * (y + 0.5) / S)] * S])
print("ok")
