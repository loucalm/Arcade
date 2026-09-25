#!/usr/bin/env python3
"""Génère les sprites de base (PNG RGBA blancs, à teinter dans Unity) : carré, cercle, halo.
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
print("ok")
