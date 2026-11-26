"""La chauve-souris animée (tools/gen-bat.py) : 4 images du dessin du PDF, ailes qui bougent, pattes fixes, liseré
clair tout autour (pour la voir sur un décor sombre, sans halo)."""
import importlib.util
import os

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("gen_bat", os.path.join(HERE, "..", "gen-bat.py"))
gen = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gen)

FRAMES = gen.frames()


def opaque(im):
    w, h = im.size
    p = im.load()
    return {(x, y) for x in range(w) for y in range(h) if p[x, y][3] >= 128}


def test_four_frames_of_the_same_size():
    assert len(FRAMES) == 4
    assert len({f.size for f in FRAMES}) == 1


def test_wings_move_but_legs_stay():
    w, h = FRAMES[0].size
    legs = lambda im: {(x, y) for x, y in opaque(im) if y > gen.WING_BOTTOM + 1}       # +1 : liseré, toile décalée
    wings = lambda im: {(x, y) for x, y in opaque(im) if y <= gen.WING_BOTTOM + 1 and not gen.BODY[0] + 1 <= x <= gen.BODY[1] + 1}
    assert legs(FRAMES[0]) == legs(FRAMES[1]) == legs(FRAMES[2])
    assert wings(FRAMES[0]) != wings(FRAMES[1]) != wings(FRAMES[2])
    assert FRAMES[1].tobytes() == FRAMES[3].tobytes()                                   # milieu, à l'aller et au retour


def test_light_rim_around_the_whole_silhouette():
    for im in FRAMES:
        p = im.load()
        solid = opaque(im)
        rim = {xy for xy in solid if p[xy] == gen.RIM}
        drawing = solid - rim
        assert len(rim) > 100
        for x, y in drawing:                                     # le dessin est entièrement entouré (dessin ou liseré)
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                assert (x + dx, y + dy) in solid, (x, y)
