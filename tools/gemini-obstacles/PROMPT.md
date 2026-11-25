# Kenta : obstacles des 8 biomes, dossier pour Gemini

## Mode d'emploi (pour Lou)

- **Gemini agent** (Antigravity, Gemini CLI) : ouvrir ce dossier et lui donner ce fichier (« Fais ce que dit PROMPT.md »). Il lit les images de `refs/`, génère dans `raw/`, lance `clean.py` et vérifie `out/preview.png`.
- **Gemini web** : un message par biome. Joindre `refs/maquettes_toutes.png`, `refs/obstacles_dans_maquettes.png`, `refs/obstacles_actuels.png` et `refs/maquettes/maquette_<n>_<biome>.png`. Coller la partie « Prompt » ci-dessous, puis ajouter : « Do only the biome <biome> now, as one sheet (option B) ». Enregistrer l'image dans `raw/sheet_<biome>.png`.
- **Planches avec étiquettes, reflets ou fonds** (ce que rend Gemini web) : les mettre dans `raw/sheets/<n>_<biome>.jpg`, puis lancer `python3 cut_sheets.py` (dépendance : `scipy`). Il garde les 5 plus grandes taches de chaque planche et écrit `raw/<sorte>_<biome>.png`. Les corrections propres à chaque objet (socle, fond rectangulaire, halo) sont dans `FIXES`.
- **Ensuite** (Lou ou Claude) : `python3 tools/gemini-obstacles/clean.py` (dépendances : `pillow`, `numpy`) → 40 PNG à la taille exacte dans `out/`, plus l'aperçu `out/preview.png`. Si l'aperçu est bon, lancer `clean.py --install`. Les fichiers vont alors dans `game/Assets/_Project/Art/Kenta/Gen/biomes/`. Puis :
  1. Dans `tools/gen-kenta-assets.py` (`gen_biomes`), retirer les 5 lignes `put(... wall/block/ceil/post/platform ...)`. Sinon `--only biomes` réécrase les sprites.
  2. Dans Unity : menu **Kenta > Reconstruire thèmes, prefabs et scène**.

Contenu du dossier : `refs/` (références), `palettes.json` (couleurs par biome), `raw/` (ce que Gemini produit), `out/` (sprites finaux), `clean.py`.

---

## Prompt

You are making the obstacle sprites of **Kenta**, a 2D side-scrolling rhythm runner in pixel art for an arcade cabinet (1920×1080, integer upscaling). The game has 8 biomes; each biome needs 5 obstacle sprites, so 40 sprites in total. Work only inside this folder. Do not modify any other file.

### References (in `refs/`)
- `maquettes/maquette_<n>_<biome>.png`: the official mockup of each biome from the art-direction PDF (240×135 pixel art, shown ×4). **This is the style to match.** Look at how the small obstacles on the ground (stump, barrel, crystals, rocks, ice cube, stone block, cloud) and the hanging platforms are drawn.
- `maquettes_toutes.png`: the 8 mockups on one sheet.
- `obstacles_dans_maquettes.png`: close-ups of the obstacles and platforms inside the mockups.
- `obstacles_actuels.png` and `actuels/<sprite>.png`: the current in-game sprites at their **exact final size** (first Gemini pass, ADR-026). Use them for size, silhouette, tiling and to stay consistent with the set; a redone sprite must be at least as rich and closer to the mockups.
- `palettes.json`: the allowed colours of each biome.

### Style (all sprites)
- Same pixel art as the mockups: chunky 16-bit look, clean square pixels all of the same size. No anti-aliasing, no gradients, no blur, no dithering noise, no text, no drop shadow.
- Each material has 2–3 flat tones (shadow, base, highlight). Light comes from the top-left: highlight on the top and left, shadow on the right and bottom.
- A 1-pixel near-black outline (#14101C) around every shape.
- Only the colours of the biome in `palettes.json`, plus darker or lighter tones of them.
- Side view, like the mockups (the camera looks at the side of the level).
- Readable at a glance from 2 m away: a simple, bold silhouette, one or two details (moss, crack, ring, glow), and it must stand out against the biome background of the mockup.

### The 5 sprites of each biome (final size in pixels)
| File | Final size | Role in the game | Constraints |
|---|---|---|---|
| `wall_<biome>` | 38×34 (art 36×32 + outline) | Low wall the hero **jumps over** | Stands on the ground: flat bottom on the last row. A little wider than tall. Fills about 30–36 px wide and 28–32 px high (hitbox 28×30, bottom centre). |
| `block_<biome>` | 40×40 (art 38×38 + outline) | Block the hero **punches**; also used half buried as a springboard (only the top 14 px show) | Almost fills the whole square (hitbox 36×38). Flat bottom. The top must read well on its own. |
| `ceil_<biome>` | 16×16 tile, repeated left-right | Beam the hero **slides under** | A solid band at the top (6–9 px), details hanging below. **Seamless left-right**: the right edge continues into the left edge. |
| `post_<biome>` | 8×16 tile, repeated top-bottom | Thin post that hangs from the top of the screen down to each end of the beam | Narrow (4–6 px of content), centred. **Seamless top-bottom.** |
| `platform_<biome>` | 32×10 tile, repeated left-right | Floating platform the hero lands on (hung from vines or ropes in the mockups) | Top row is the walking surface: flat. Top layer (grass, snow, metal…) then a darker slab, darkest underneath. **Seamless left-right.** |

### Subjects per biome (from the art-direction PDF)
| Biome (`id`) | Place | wall | block | ceil | post | platform |
|---|---|---|---|---|---|---|
| La Sylve (`sylve`) | drum forest, teal sky, red-crowned trees | cut tree stump, light rings on top, bark grooves, moss patch | hollow log standing on end, dark hollow, bark | low branch: thick brown branch band with green leaves hanging | green vine (liana) | grass top, brown earth slab, dark underside |
| Le Bayou (`bayou`) | misty swamp, purple dusk | mossy stump with dripping moss | wooden barrel with hoops, moss on the lid | hanging roots under a dark muddy band | twisted hanging root | mossy wooden plank, dark underside, moss strands |
| Les Échos (`echos`) | crystal cave, night blue | cluster of 3 pale blue-violet crystals pointing up | geode: dark rock split open on violet crystals | stalactites under a rock band | thin crystal pillar | dark indigo stone slab with small drips under it |
| La Forge (`forge`) | volcano, lava river | basalt columns with glowing orange cracks | molten rock: dark crust, glowing orange cracks | iron forge beam with rivets, hot glowing edge | iron chain | dark basalt slab with a glowing orange top edge |
| L'Échine (`echine`) | mountain, autumn trees | grey boulder with moss on top | split stone: big crack, small amber gem | rocky arch: grey rock band, jagged bottom | hemp rope | grass top on a grey rock slab |
| Le Givre (`givre`) | glacier, snowy pines | block of ice with a snow cap | cracked ice cube with white highlights | icicles under a snow and ice band | ice column | snow top on a pale ice slab |
| La Cité Sourde (`cite`) | sandstone ruins of a music city | fallen column on its side (drums and capital) | carved sandstone statue block | stone arch: carved frieze band, small hanging ornaments | thin sandstone pillar | carved sandstone slab |
| Le Grand Orage (`orage`) | summit in storm clouds, night | dense grey-blue storm cloud, solid looking | charged cloud with a yellow lightning bolt | horizontal yellow lightning bolt inside a cloud band | dark cloud wisp column | white cloud slab with a flat top |

### Output
Option A (you can save files; preferred): one image per sprite, saved as `raw/<file>.png` (for example `raw/wall_sylve.png`, `raw/ceil_givre.png`). Use a transparent or flat #FF00FF magenta background, one object per image and nothing touching the edges. Any resolution is fine if the pixel grid stays clean and square. For `ceil`, `post` and `platform`, draw a strip 3 to 4 tiles long (for example 4 tiles of `ceil` side by side), so a seamless tile can be cut from it.

Option B (one image per biome): a sheet `raw/sheet_<biome>.png` with the 5 objects in one row, in this order from left to right: wall, block, ceil (strip of 4 tiles), post (column of 3 tiles), platform (strip of 3 tiles). Use a flat #FF00FF magenta background, with wide magenta gaps between objects. All 5 objects share the same pixel size.

Then run `python3 clean.py` (needs `pillow` and `numpy`). It cuts, reduces to the exact sizes with nearest neighbour, maps colours to the palette, adds the outline and makes the tiles seamless. It writes `out/<file>.png` and `out/preview.png` (all sprites ×4, tiles repeated). Open `out/preview.png` and redo any sprite that fails the checklist below. Do **not** run `clean.py --install`.

### Checklist (per sprite, on `out/preview.png`)
- It reads instantly as the subject of the table, in the style of the mockup of its biome.
- `wall` and `block` sit on the bottom edge; the `block` fills its square; the `wall` is wider than tall.
- Repeated tiles show no visible seam or cut-off shape.
- Clean pixels: no stray single pixels, no noise, outline closed all around.
- The 5 sprites of a biome look like a set, and the 8 biomes look like the same game.

When done, list the 40 files of `out/` and report which ones you are not happy with.
