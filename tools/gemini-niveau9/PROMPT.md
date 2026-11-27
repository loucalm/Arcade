# Kenta : niveau 9 « La Sourdine », dossier pour Gemini

Niveau extrême (le défi de l'élite, après le niveau 8). Il commence au calme, dans la Sylve, avec la DA normale du jeu. Puis **la Sourdine** revient : c'est la tache de silence de la DA (docs/da-kenta.md § 2 et § 5), qui « avale le son et la couleur ». Le monde se fissure, puis il bascule dans une **DA en négatif** : noir et blanc, inversions sur les temps forts, yeux qui s'ouvrent sur le rythme. Enfin vient le combat contre la Sourdine. Seuls Kenta, son bandeau et les pièces gardent leur couleur.

Inspiration : l'ambiance d'un niveau extrême de Geometry Dash (yeux qui observent, glitchs, inversions noir/blanc). On n'en reprend **ni les dessins ni le texte** : la Sourdine, ses yeux rouges, son sourire et le monde décoloré viennent de notre DA.

## Le niveau en 5 phases (pour savoir à quoi sert chaque planche)
| Phase | Ce qu'on voit | Assets |
|---|---|---|
| 1. Calme | La Sylve normale (niveau 1), au crépuscule | rien de nouveau |
| 2. Fissure | Sur certains temps, un obstacle bascule une fraction de temps dans sa version « sourde » (même silhouette), puis revient | planches A |
| 3. Monde sourd | Tout est en négatif ; des yeux s'ouvrent et se ferment sur le temps ; inversions noir ↔ blanc sur les temps forts | planches A, B1-B3, C |
| 4. La Sourdine | Combat : une masse noire, deux yeux et un sourire en clavier de piano, à droite de l'écran. Elle tire des rayons de silence et lance des notes noires, que Kenta renvoie en frappant sur le temps | planches B4-B5, D, E |
| 5. Dernier coup | Le dernier coup fend la Sourdine ; la couleur revient depuis le bandeau | œil fêlé (D2), le reste en code |

## Ce que Gemini ne fait pas (fait par le code ou un script)
- Ennemis et héros dans le monde sourd : le héros et les pièces restent en couleur ; les ennemis (djembé, cuirassé, chauve-souris) sont repassés en noir et blanc par un script, à partir de leurs images actuelles. Toutes leurs images d'animation restent ainsi cohérentes.
- Inversion noir ↔ blanc, flashs, glitchs, clignotement du niveau, écran retourné : code (shader d'écran, `ScreenGlitch`, `ScreenFx`).
- Texte (« CHUT. », « SILENCE ») : police pixel du jeu (`PixelText`).
- Ciel du monde sourd (noir, soleil devenu éclipse) : script.
- Animation de la Sourdine : le code assemble les morceaux (corps, yeux, pupilles, bouche) et les anime sur le temps. Gemini dessine donc des **pièces détachées**, pas des animations.

## Mode d'emploi
- **Gemini web** : un message par planche (11 planches, liste plus bas). Joindre les références indiquées pour la planche, coller la partie « Prompt » ci-dessous, puis ajouter : « Do only sheet `<id>` now. » Enregistrer l'image dans `raw/sheets/<id>.png` (ou `.jpg`).
- **Gemini agent** (Antigravity, Gemini CLI) : ouvrir ce dossier et lui donner ce fichier (« Fais ce que dit PROMPT.md »).
- **Ensuite (Claude)** : `clean.py` n'existe pas encore. Il sera écrit quand les premières planches seront là, sur le modèle de `tools/gemini-obstacles/` : découpe, réduction à la taille exacte, couleurs ramenées à `palettes.json`, tuiles raccordées. Pour les obstacles et le terrain, la silhouette est reprise au pixel près du sprite de la Sylve.

| Planche (`id`) | Références à joindre |
|---|---|
| `a1_obstacles`, `a2_terrain` | `refs/sylve_actuel_x4.png`, `refs/maquette_1_sylve.png` |
| `b1_far`, `b2_mid`, `b3_near` | `refs/sylve_decor_x2.png`, `refs/actuels/bg_sylve_<far, mid ou near>.png` |
| `b4_vide_far`, `b5_vide_near` | `refs/echelle_sourdine_x2.png` |
| `c_yeux`, `d1_sourdine_corps`, `d2_sourdine_visage`, `e_attaques` | `refs/echelle_sourdine_x2.png`, `refs/actuels/hero_kenta_idle.png` |

Contenu : `refs/` (références : sprites actuels de la Sylve à leur taille exacte dans `refs/actuels/`, planche ×4, décor ×2, maquette, échelle de la Sourdine), `palettes.json` (couleurs autorisées), `raw/sheets/` (images de Gemini).

---

## Prompt

You are drawing the sprites of **level 9 of Kenta**, a 2D side-scrolling rhythm runner in pixel art for a school arcade cabinet (1920×1080, integer upscaling, 40 px = 1 game unit, the hero is about 50 px tall). Work only inside this folder. Do not modify any other file.

### The story of this level
In Kenta, nature makes music: trees are drums, crystals are xylophones. **The Sourdine** is a stain of silence that swallows the sound and the colour of everything it touches. Its look comes from the game's art direction: **a black mass, two white eyes with red pupils, and a smile**.
Level 9 starts in the colourful drum forest of level 1 (La Sylve). Then the Sourdine drains it: the same forest becomes **its own photographic negative**, like chalk lines on a blackboard. Eyes open in the dark and blink on the beat. At the end, the Sourdine itself appears on the right of the screen and attacks.
Mood: unsettling, watched, glitchy, very high contrast, but still in the cute, chunky Kenta style. No gore, no realistic horror: the game is played by everyone at a school arcade. **Original designs only: do not imitate any existing game.**

### References (in `refs/`)
- `actuels/*.png`: the current sprites of La Sylve at their **exact in-game size**. `sylve_actuel_x4.png` shows them ×4 (tiles repeated), with the enemies and the hero for scale.
- `sylve_decor_x2.png`: the Sylve background (sky + 3 parallax layers) ×2. `actuels/bg_sylve_far|mid|near.png` are the layers alone.
- `maquette_1_sylve.png`: the official mockup of La Sylve (style reference for the colour version).
- `echelle_sourdine_x2.png`: the wide camera view (640×360 px, shown ×2) with the hero, the Sourdine and its parts at their in-game size.
- `palettes.json`: the allowed colours.

### Style of the silent world (all sheets)
- Pixel art with clean square pixels, all of the same size. No anti-aliasing, no gradients, no blur, no dithering noise, no text, no drop shadow. Side view.
- **Negative ink style**: every object is a **solid black mass with a 1-pixel white outline**. Volume and material come from a few **white line strokes** (wood grain, tree rings, leaf edges, grass ticks), placed on the right and bottom sides (light from the top-left). No white fills, except small highlights and the Sourdine's eyes and teeth.
- **Colours** (`palettes.json`, palette `sourd`): black #000000 and white #FFFFFF for everything the hero touches; dark grey #3A3842 and grey #7C7A86 only for backgrounds and deep ground; red #FF2A4A and dark red #8A0F24 **only for the Sourdine** (pupils, beam, mouth glow, the eye of the black note). Gold (palette `or`) only for the gold note.
- Depth: the gameplay objects are the brightest thing on screen (pure white lines). The further away a background layer is, the darker its lines (white → grey → dark grey).
- The game will sometimes **invert the whole screen** (black ↔ white, red → cyan) on strong beats. Every sprite must read just as well inverted: bold silhouettes, no large white areas.
- Glitch: on `wall` and `block` only, one clean horizontal tear (a band of 2 to 3 pixel rows shifted 2 px sideways). Nowhere else; never on tiles.

### The 11 sheets
Final sizes are in game pixels. Draw each sheet at any resolution, as long as the pixel grid stays clean, square and the same for every object of the sheet.

**`a1_obstacles`: the 5 Sylve obstacles, silent version.** Same silhouette and size as the Sylve sprite (see `refs/actuels/`), redrawn in the negative ink style. In one row, left to right:
| Sprite | Final size | Subject |
|---|---|---|
| `wall` | 38×34 | the cut tree stump: white rings on top, bark grooves, the moss patch becomes a few white specks |
| `block` | 40×40 | the hollow log standing on end: the hollow is pure black, white bark lines |
| `ceil` | strip of 4 tiles of 16×16, seamless left-right | the low branch: black band with a white edge, hanging leaves drawn as white outlines |
| `post` | column of 3 tiles of 8×16, seamless top-bottom | the vine: black with white outline, small leaf ticks |
| `platform` | strip of 3 tiles of 32×10, seamless left-right | flat white top line with white grass ticks, black slab, grey strokes underneath |

**`a2_terrain`: the ground, silent version** (same sizes as the Sylve tiles). In one row:
| Sprite | Final size | Subject |
|---|---|---|
| `ground` | strip of 3 tiles of 32×20, seamless | walking surface: 1 px white top line with white grass ticks; black body with a few grey horizontal strokes |
| `fill` | 32×32, seamless on all sides | earth under the ground: black with a sparse grey brick-like pattern |
| `fill_deep` | 32×32, seamless on all sides | deeper earth: the same pattern in dark grey, sparser |
| `hazard` | strip of 2 tiles of 32×20, seamless | the bottom of a pit (dark water in the Sylve): black, a few dark-grey wavy lines, no white |
| `cliff` | strip of 3 tiles of 32×8, seamless | edge of a pillar top: white grass line on black |
| `deco_0`..`deco_3` | 16×12, 14×18, 14×14, 16×10 | pebble, twig, small cut ring, two stones: white line art on black |

**`b1_far`, `b2_mid`, `b3_near`: the 3 background layers of the silent Sylve.** One sheet per layer, 16:9 image showing **one 640×360 layer, seamless left-right** (its right edge continues into its left edge). Empty sky = flat magenta #FF00FF. The silhouettes stand on a horizon line at 57 % of the image height, like the Sylve layers; below the horizon, solid black down to the bottom edge.
- `b1_far`: the snowy mountains of the Sylve as black shapes outlined in **dark grey** (#3A3842), snow caps as dark-grey line work.
- `b2_mid`: the pine trees as black shapes outlined in **grey** (#7C7A86), a few grey branch strokes.
- `b3_near`: the drum trees with round crowns and the round bushes, black with **white** outlines; some trunks are broken. No eyes in the backgrounds (the game adds them).

**`b4_vide_far`, `b5_vide_near`: inside the Sourdine (boss arena).** Same format: one 640×360 layer per sheet, seamless left-right, magenta where empty. No ground: the hero runs on platforms.
- `b4_vide_far`: the **torn music staff**: five long parallel lines (a music staff) crossing the whole width, bent in slow waves, broken in two or three places, with a few frozen note heads; dark grey only.
- `b5_vide_near`: floating debris of the drained world, upside down or tilted 90° (never other angles): chunks of ground with grass, a broken drum-tree trunk, a few piano keys. Black with white outlines, scattered in the upper two thirds, plenty of empty space.

**`c_yeux`: eyes of the Sourdine that open in the scenery.** Two sizes, 4 frames each, all frames of a size in the same box so they swap in place. Top row the small eye, bottom row the large one:
| Sprite | Final size | Frames, left to right |
|---|---|---|
| `eye_s` | 16×10 | open (white almond, round red pupil in the centre); open looking left (pupil on the left side, toward the hero); half closed (black upper lid with a white lid line); closed (a single white curved line with 2 tiny lashes) |
| `eye_l` | 32×18 | same 4 frames |

**`d1_sourdine_corps`: the Sourdine's body.** Final size **220×360** (it fills the right third of the wide view, see `echelle_sourdine_x2.png`). A towering mass of silence coming in from the right edge of the screen. Pure black inside. Its **left edge is alive**: dripping ink tendrils and horizontal glitch streaks, like a torn video signal, with a 1-px white outline along the left contour and a few white scratches near the edge. Its right, top and bottom edges are straight (they are cut by the screen). **No face**: the eyes and mouth are separate sprites; keep the upper middle plain black for them.

**`d2_sourdine_visage`: the face parts**, all centred in fixed boxes so frames swap in place. Three rows:
| Sprite | Final size | Frames, left to right |
|---|---|---|
| `face_eye` | 48×28 | open (white almond, **no pupil**: the pupil is a separate sprite); half closed (black upper lid, white lid line); closed (white curved line with short lashes); wide open (rounder, angry); cracked (open, with black crack lines and a chipped corner, for the final blow) |
| `face_pupil` | 16×16, 12×12, 8×8 | dilated, normal, pin-point (the stare). Red iris with a dark-red ring, black centre, one 2×2 white glint top-left |
| `face_mouth` | 120×24, 120×40, 120×56 | closed smile (a thin white crescent); half open (black inside, a row of small white rectangular teeth like **piano keys**, with black-key gaps); wide open, screaming (piano-key teeth top and bottom, deep black inside with a faint red glow at the back: it is charging a beam) |

**`e_attaques`: the Sourdine's attacks.** In one row:
| Sprite | Final size | Subject |
|---|---|---|
| `beam` | strip of 4 tiles of 16×12, seamless left-right | the beam of silence: 4 px white core with a few short black notches, 2 px red edges above and below, 1 px dark-red outer fringe |
| `beam_warning` | strip of 4 tiles of 16×4, seamless | warning line before the beam: thin red dashes, 8 px on, 8 px off |
| `beam_muzzle` | 32×32 | the burst where the beam leaves the mouth: white flare with a red rim, rays pointing left |
| `note_black` | 16×16, 2 frames | an eighth note (head, stem, flag), black with a 1-px white outline; the note head holds a tiny red eye, open then closed |
| `note_gold` | 16×16 | the same eighth note, retuned by the hero's punch: gold like the game's coins (palette `or`, outline #14101C), no eye, bright highlight |

### Output
For each sheet: one image `raw/sheets/<id>.png`, objects in the order of the table, on a **flat #FF00FF magenta background**, with wide magenta gaps between objects, nothing touching the edges, no labels. Tiles as strips (as listed) so a seamless tile can be cut from the middle. For the background layers (`b1`–`b5`): the layer fills the whole 16:9 image, magenta only where it is empty.

### Checklist (per sheet)
- Clean square pixels, no noise, no stray single pixels, outlines closed.
- Only the colours of `palettes.json`, used as described (red only on the Sourdine, gold only on `note_gold`).
- `a1` and `a2`: each sprite has the exact silhouette of its Sylve version and reads as the same object, drained of colour.
- Every sprite still reads if black and white are swapped.
- Animation frames of a sprite are drawn in identical boxes, aligned, and differ only where they should.
- Seamless tiles and layers show no visible seam.
- Creepy but cute: chunky shapes in the Kenta style, nothing gory.

When done, list the files of `raw/sheets/` and say which sprites you are not happy with.
