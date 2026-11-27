"""Repère les images d'une planche Gemini (fond magenta) : chaque personnage, avec son katana et sa traînée, devient
un « blob » numéroté. `python3 tools/gemini-heros/blobs.py planche.jpg sortie.png` écrit la planche annotée (numéros)
pour écrire le manifeste (manifest.json : quel blob est quelle image de quelle animation)."""
import sys
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage


def background_loose(a):
    """Masque large du fond, seulement pour repérer et numéroter les images (manifest.json en dépend : ne pas changer)."""
    R, G, B = a[..., 0], a[..., 1], a[..., 2]
    return (R > 150) & (B > 150) & (G < 215) & (np.abs(R - B) < 80) & (R - G > 40)


def background(a):
    """Masque du fond pour découper les pixels du personnage."""
    R, G, B = a[..., 0], a[..., 1], a[..., 2]
    # magenta (#FF00FF) et cases rose vif (≈ 250, 100, 245) : très peu de vert par rapport au rouge et au bleu ; la peau
    # (≈ 238, 191, 177) et les tons pâles ont beaucoup de vert : ils restent au personnage
    return (R > 150) & (B > 150) & (np.abs(R - B) < 80) & (G < 0.6 * np.minimum(R, B))


def blobs(path, min_px=900, gap=3):
    """Blobs triés en lignes (haut → bas) puis de gauche à droite : [(x0, y0, x1, y1), ...]."""
    a = np.asarray(Image.open(path).convert("RGB")).astype(int)
    fg = ndimage.binary_opening(~background_loose(a), iterations=2)
    lab, n = ndimage.label(ndimage.binary_dilation(fg, iterations=gap))
    out = []
    for i, s in enumerate(ndimage.find_objects(lab)):
        if (fg[s] & (lab[s] == i + 1)).sum() < min_px:
            continue
        out.append((s[1].start, s[0].start, s[1].stop, s[0].stop))
    out.sort(key=lambda b: ((b[1] + b[3]) / 2, b[0]))
    rows, cur = [], []
    for b in out:                                            # une ligne = blobs dont le centre est proche
        cy = (b[1] + b[3]) / 2
        if cur and abs(cy - np.mean([(c[1] + c[3]) / 2 for c in cur])) > 45:
            rows.append(sorted(cur)); cur = []
        cur.append(b)
    if cur:
        rows.append(sorted(cur))
    fg = ndimage.binary_opening(~background(a), iterations=1)
    return [b for r in rows for b in r], a, fg


if __name__ == "__main__":
    bl, a, fg = blobs(sys.argv[1])
    im = Image.open(sys.argv[1]).convert("RGB"); d = ImageDraw.Draw(im)
    for i, (x0, y0, x1, y1) in enumerate(bl):
        d.rectangle([x0, y0, x1, y1], outline=(0, 255, 0), width=2)
        d.rectangle([x0, y0, x0 + 26, y0 + 16], fill=(0, 0, 0)); d.text((x0 + 3, y0 + 2), str(i), fill=(255, 255, 0))
    im.save(sys.argv[2])
    for i, b in enumerate(bl):
        print(i, b, "h", b[3] - b[1])
