# Kenta : ennemis et objets redessinés, dossier pour Gemini

But : des ennemis **plus stylés et plus vivants** (aujourd'hui extraits du PDF de la DA, une seule image plus une version écrasée et étirée par un script), avec les animations qui manquent (touché, cassé pour tous), et les **objets provisoires** remplacés : tambour-tremplin (lu comme un tonneau), note de saut (la pièce ×2 teintée), portail de gravité (dessiné en code), nœud de liane.

On garde : la règle commune de la DA (docs/da-kenta.md § 6) : **yeux blancs, une danse sur 2 temps, un état « cassé » qui libère des pièces** ; l'instrument de chaque ennemi ; leur taille dans le jeu (`refs/echelle_avec_kenta_x4.png`).

## Contraintes du jeu
- **Case 64×64 px** par image d'ennemi (cuirassé : 64×48), **pieds ou base sur la dernière rangée de pixels**, centré horizontalement (pivot en bas au centre dans Unity). Les ennemis volants (chauve-souris) sont centrés dans la case.
- Taille : comme aujourd'hui, de 40 à 54 px de haut (le héros fait ~52 px).
- **Même orientation que le sprite actuel** (de face ou de trois quarts, comme dans `refs/`) : le jeu ne les retourne pas.
- Pixel art pur, contour sombre d'un pixel (#14101C), lumière en haut à gauche, 3 tons par matière.
- **Contraste** : un ennemi doit toujours se détacher de son décor (règle de couleur de la roadmap : héros, ennemis et pièces jamais dans les couleurs du fond). Les yeux blancs sont le repère commun.

## Ce que Gemini ne fait pas (code)
Les pièces qui s'échappent à la casse, le flash blanc au coup, la secousse, le vol stationnaire (±4 px), le calage des images sur le temps, la teinte par biome du portail.

## Mode d'emploi
1. Gemini web, **une conversation pour tous les ennemis**. Joindre `refs/echelle_avec_kenta_x4.png` et `refs/gabarit_case_64x64_x4.png`, coller le **Prompt commun**.
2. Puis une planche par message : joindre `refs/actuel_<ennemi>_x4.png` et écrire « Do only sheet `<id>` now. »
3. Enregistrer chaque image dans `raw/sheets/<id>.png`.
4. **Ensuite (Claude)** : `clean.py` (découpe, palette, base sur la ligne de sol, installation dans `Pdf/` et `Gen/world/` sous les mêmes noms), puis branchement des nouvelles images (touché, cassé, tambour, note, portail) dans `BiomeTheme`, `LevelTheme` et `LevelBuilder`.

| id | Contenu | Images | Référence |
|---|---|---|---|
| `m01_djembe` | Djembé-champignon (Sylve) | 6 | `actuel_djembe_x4.png` |
| `m02_crapaud` | Crapaud-grosse caisse (Bayou) | 6 | `actuel_crapaud_x4.png` |
| `m03_chauve` | Chauve-souris maracas posée au sol (Échos) | 6 | `actuel_chauve_souris_x4.png` |
| `m04_crabe` | Ampli-crabe (Forge) | 6 | `actuel_crabe_x4.png` |
| `m05_golem` | Golem-gong (Échine) | 6 | `actuel_golem_x4.png` |
| `m06_morse` | Morse-xylophone (Givre) | 6 | `actuel_morse_x4.png` |
| `m07_gargouille` | Gargouille-trompette (Cité Sourde) | 6 | `actuel_gargouille_x4.png` |
| `m08_nuage` | Nuage-timbale (Orage) | 6 | `actuel_nuage_x4.png` |
| `m09_chauve_souris_vol` | Chauve-souris en vol (tous les niveaux) : 4 battements, pendue au plafond 2, touchée 1, cassée 2 | 9 | `actuel_chauve_souris_x4.png` |
| `m10_cuirasse` | Cuirassé (on lui saute dessus, il fait rebondir) : marche 2, écrasé 2, se relève 1 | 5 | `actuel_cuirasse_x4.png` |
| `o1_objets` | Tambour-tremplin 3, note de saut 4, pièce 4, nœud de liane 1 | 12 | `actuel_pieces_x4.png` |
| `o2_portail` | Portail de gravité : montant, linteau, voile (pièces détachées) | 3 | aucune |

---

## Prompt commun

You are redrawing the **enemies and gameplay objects of Kenta**, a 2D side-scrolling rhythm runner in pixel art for a school arcade cabinet (1920×1080, integer upscaling, 40 px = 1 game unit). The hero (about 52 px tall) runs to the right on the beat; he jumps over obstacles and **slashes enemies with a katana on the beat**. Every enemy is a **musical instrument driven mad by the Sourdine** (a stain of silence); hitting it **retunes** it and it bursts into coins.

### Common rules for every enemy (the game's art direction)
- **White eyes** (big, readable, with a dark pupil): the shared signature of all enemies.
- **A dance over 2 beats**: beat 1 = **squashed** (wider, lower), beat 2 = **stretched** (taller, thinner, feet off the ground). The game alternates these two frames on the beat.
- **A "broken" state**: the instrument cracks open (the game adds the coins flying out).
- Keep the **same facing as the current sprite** (front or three-quarter view, as in the reference): the game does not flip enemies.
- Each enemy is **an animal or creature fused with an instrument**, cute and chunky, mischievous rather than scary. No gore. Original designs only: do not imitate any existing game.

### Make them much cooler
- Stronger silhouettes with a clear instrument shape, bigger expressive eyes, a grumpy or crazy expression (they are detuned), a little **musical detail that moves** (drum skin, strings, bell of the horn, keys).
- Material and light: light from the top-left, 3 tones per material, 1-pixel dark outline #14101C around the whole sprite, a few selective inner lines, a small highlight on metal, wood and drum skins.
- **Contrast**: the enemy must read on a busy background: saturated body colour, darker outline, bright white eyes. Keep the main colours of the current sprite (see the reference for each sheet), you may push saturation and contrast.

### Frames of each local enemy sheet (`m01` to `m08`), in this order, in one row
1. `idle`: neutral pose.
2. `squash`: beat 1, squashed (about 15 % wider, 10 % lower), the instrument part "hit" (skin pressed, horn blowing…).
3. `stretch`: beat 2, stretched and hopping (feet 2-3 px off the bottom row), eyes wide.
4. `hurt`: the moment the katana hits: recoils backwards, eyes squeezed shut, mouth open.
5. `broken_0`: the instrument cracks (a clean crack line across it), eyes turned into spirals or crosses.
6. `broken_1`: it falls apart in 2-3 pieces, still inside the cell.

Per-enemy details:
- `m01_djembe` (forest): mushroom whose red cap with white dots is a djembe drum skin; small wooden legs. Squash: the cap flattens like a hit drum skin. Broken: the cap splits in two.
- `m02_crapaud` (swamp): green toad whose belly is a bass drum (white drum skin with a red rim). Squash: throat and belly inflate on the strong beat.
- `m03_chauve` (crystal cave): purple bat standing on the ground, holding two maracas in its feet or wings; wings half open. Squash: shakes the maracas down; stretch: wings up.
- `m04_crabe` (volcano): crab whose body is a black guitar amplifier with a speaker cone, glowing orange cracks like lava, eye stalks. Squash: claws snap down like a snare hit.
- `m05_golem` (mountain): small stone golem framing a golden gong with a face. Squash: the gong vibrates (ring lines); stretch: sways.
- `m06_morse` (glacier): brown walrus whose tusks are xylophone mallets, xylophone bars on its belly. Squash: strikes its own belly bars.
- `m07_gargouille` (ruined city): stone gargoyle with small wings, blowing a golden trumpet. Squash: puffed cheeks, blows on the strong beat.
- `m08_nuage` (storm summit): grumpy blue storm cloud sitting on a copper kettledrum (timpani) with a small lightning spark. Squash: the cloud swells over the bar.

### `m09_chauve_souris_vol`: the flying bat used in every level (64×64 cells, centred)
In one row: 4 wing-flap frames (wings up, middle, down, middle), 2 frames **hanging upside down** from the ceiling (asleep, then eyes open), 1 `hurt`, 2 `broken` (maracas fly away, then the bat spins down). Purple, white eyes, small fangs, maracas in its feet. The silhouette must stay readable on dark cave backgrounds: a thin light rim on the top edges is welcome.

### `m10_cuirasse`: the armoured beetle (64×48 cells)
The hero **jumps on it** and bounces high: it must look like a **springboard enemy**. Teal armoured shell like a drum machine, a bright yellow pad on top (the "press here" spot), little legs, angry yellow eyes in the shell slit. In one row: 2 walking/dancing frames, 2 `stomped` frames (shell squashed flat, pad pushed in, spring lines), 1 `recover` frame (pops back up).

### `o1_objets`: gameplay objects (in one row, each centred in its own cell)
| Object | Cell | Frames | Subject |
|---|---|---|---|
| `drum` | 48×40 | 3 | **drum-trampoline**: must read instantly as "bounce here" (today it looks like a barrel): a big drum with a **taut white skin on top**, a bright coloured rim, tension ropes on the side, a small upward chevron painted on the shell. Frames: idle, pressed (skin pushed down, sides bulging), launch (skin bulging up, two short motion lines). Base on the bottom row. |
| `jump_note` | 24×24 | 4 | **jump note** floating in the air: a glowing cyan eighth note (head, stem, flag) inside a ring, a thin white rim; frames pulse (small, big, glow ring, small). |
| `coin` | 20×20 | 4 | the coin: gold disc with an eighth note engraved, outline #14101C, spinning (front, three-quarter, edge, three-quarter back). |
| `vine_knot` | 12×12 | 1 | the knot where the hero grabs a vine: a green leafy knot with a small flower. |

### `o2_portail`: gravity portal (pieces the game assembles and tints)
The portal flips gravity: the hero falls up to the ceiling. Today it is two coloured posts. Draw it **in grey-scale** (the game tints it with the level colour): a mystical arch.
| Piece | Size | Subject |
|---|---|---|
| `portal_post` | 16×64, seamless top-bottom | a carved stone pillar with a glowing rune channel down the middle |
| `portal_cap` | 80×20 | the lintel joining the two posts, with a double-arrow rune (up and down) in the centre |
| `portal_veil` | 48×32, seamless on all sides | the shimmering veil between the posts: soft horizontal bands with a few sparkles, light grey on transparent (draw on magenta) |

### Output
One image per message: the frames of the requested sheet **in the order listed, in one row** (two rows if there are more than 8), each frame in a **fixed cell of the listed size**, all cells the same size within a sheet, on a **flat #FF00FF magenta background**, nothing crossing a cell border. Grounded enemies: feet or base on the bottom row of the cell, centred. No ground, no shadow, no text, no labels, no frame borders. Draw at any integer scale (×4 or more recommended) as long as every game pixel is a clean square of the same size.
