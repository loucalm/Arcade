# Kenta : ennemis, objets, lianes et boss v2, dossier pour Gemini

But : une **v2 des ennemis et des objets** dans le style des nouveaux héros d'Antigravity (planches de
`/Users/Eleve/Desktop/ASSETV2/`, installées par `tools/import-heroes-v2.py`), plus des **lianes propres à chaque biome**
et un **nouveau boss pour le niveau 9** (la Sourdine).

## Ce qui ne va pas aujourd'hui (v1, ADR-043)
- **Le cuirassé (le « bump qui tue ») ne colle pas à sa zone de contact.** Son dessin fait 34 × 29 px, mais sa zone fait
  32 × 36 px, et le héros rebondit sur le haut de la zone : 7 px au-dessus de la carapace. Les ennemis ont été réduits de
  20 % sans toucher aux collisions. En v2, **chaque dessin est calé sur sa zone** (image `refs/zones_de_contact_x6.png`).
- **Les chauves-souris se perdent dans les lianes** : violet sombre sur feuillage sombre.
- **Certains ennemis sont moches, et leur mort ne se lit pas** : l'image « touché » et la première image « cassé »
  montrent encore l'ennemi debout, les yeux ouverts. On croit qu'il est vivant, alors que la casse ne dure que
  0,28 s (4 images à 0,07 s, `BreakFx`).
- **Les lianes sont les mêmes partout** : un trait vert tracé par le code et un nœud feuillu.
- **Le boss du niveau 9 est moche** : c'est une masse d'encre dessinée par code (`gen_sheets.py`).
- Leçon de la v1 des héros (ADR-043) : Gemini redessinait chaque image un peu différemment (taille, tête, jambes), et les
  boucles sautaient. D'où les règles de constance du prompt commun.

## Mode d'emploi
1. Gemini web, **une seule conversation** pour tout. Joindre :
   - `/Users/Eleve/Desktop/ASSETV2/image.jpeg`, la course de Kenta, pour **le style** ;
   - `/Users/Eleve/Desktop/ASSETV2/17.jpeg`, touché et mort des héros, pour le style des chutes ;
   - `refs/zones_de_contact_x6.png`, pour **l'échelle** ;
   - `tools/gemini-ennemis/raw/sheets/ennemis_1.jpg`, la v1, pour **les personnages** (on garde leurs idées).

   Puis coller le **Prompt commun**.
2. Ensuite, **une planche par message** : coller le bloc de la planche (« Sheet `E1` … »). Si une planche sort mal, la
   redemander dans le même message avec ce qui ne va pas (« frame 3 is bigger than frame 1, redo with the same size »).
3. Enregistrer chaque image telle quelle dans `tools/gemini-v2/raw/<id>.png` (ou `.jpeg`), avec l'id de la planche
   (`E1`, `E2`… `B3`).
4. **Ensuite (Claude)** : un script d'import sur le modèle de `import-heroes-v2.py`, qui :
   - détoure le fond gris ;
   - découpe la grille de 4 × 2 ;
   - **met chaque sprite à l'échelle de sa zone de contact** et recale la base sur la ligne de sol ;
   - pose un contour d'1 px et installe les images sous les noms du jeu.

   Puis il reste à brancher les lianes par biome (`VineView`, `BiomeTheme`) et le nouveau boss (`SourdineBoss`).

| id | Contenu | Images | Taille dans le jeu (zone de contact) |
|---|---|---|---|
| `E1` | Djembé-champignon (Sylve) | 8 | ~38 px de haut, corps 32 × 36 |
| `E2` | Crapaud-grosse caisse (Bayou) | 8 | idem |
| `E3` | Chauve-souris maracas posée au sol (Échos) | 8 | idem |
| `E4` | Ampli-crabe (Forge) | 8 | idem |
| `E5` | Golem-gong (Échine) | 8 | idem |
| `E6` | Morse-xylophone (Givre) | 8 | idem |
| `E7` | Gargouille-trompette (Cité Sourde) | 8 | idem |
| `E8` | Nuage-timbale (Orage) | 8 | idem |
| `E9` | Chauve-souris en vol (tous les niveaux) | 8 | corps 32 × 36, ailes jusqu'à 56 de large |
| `E10` | Chauve-souris pendue au plafond + cuirassé | 4 + 4 | pendue 32 × 40 ; cuirassé **32 × 36, dessus plat = surface de rebond** |
| `O1` | Tambour-tremplin 3, note de saut 4, note prise 1 | 8 | tambour 44 × 40 (peau = bord du haut, 36 de large) ; note 28 × 28 |
| `V1` | Lianes : Sylve, Bayou, Échos | 3 × 4 | nœud 16, maillon rond 6, poignée 12, ornement 10 |
| `V2` | Lianes : Forge, Échine, Givre | 3 × 4 | idem |
| `V3` | Lianes : Cité Sourde, Orage, Sourd (niveau 9) | 3 × 4 | idem |
| `B1` | Sourdine : corps sans visage (3 images de vol) + morte, coupée en deux (2 moitiés) | 5 | corps 176 × 168 |
| `B2` | Sourdine : yeux (5), pupilles (3), bouches (3) | 11 | œil 48 × 28 ; bouche 120 de large (24, 40 et 56 de haut) |
| `B3` | Sourdine : rayon, alerte, éclat de bouche, note noire (2), note dorée | 6 | rayon 64 × 12 (raccord) ; note 34 × 34 |

Ce que Gemini ne fait pas (code) : les pièces qui jaillissent à la casse, le flash blanc, la secousse, le vol
stationnaire, le calage des images sur le temps, la courbe de la liane (le code pose les maillons le long de l'arc),
la place du visage du boss sur son corps.

---

## Prompt commun

You are redrawing the **enemies, gameplay objects, vines and the final boss of Kenta**, a 2D side-scrolling rhythm runner in pixel art for a school arcade cabinet (1920×1080, integer upscaling, 40 px = 1 game unit). The hero is about **52 px tall in game**. He runs to the right on the beat, jumps over obstacles and **slashes enemies with a katana on the beat**. Every enemy is a **musical instrument fused with a creature, driven mad by the Sourdine** (a stain of silence). Hitting an enemy retunes it, and it **bursts into coins**.

### Style: match the attached hero sheets exactly
- Same pixel-art style as the attached Kenta sheets: crisp 16-bit-style pixel art, **1-pixel dark outline (#14101C)**, light from the top-left, **3 to 4 tones per material**, a few selective inner lines, small highlights on metal, wood and drum skins. No blur, no anti-aliasing, no gradients, no painterly texture.
- Saturated colours that **always stand out from the background**: the creature never uses the colours of its own biome's scenery.
- **Big white eyes with a dark pupil** are the shared signature of all enemies. They are cute and chunky, mischievous rather than scary. No gore.
- Original designs only: do not imitate any existing game. Keep the **idea** of each enemy from the attached v1 sheet (same creature, same instrument), but redraw it much better.

### Sheet layout (every sheet, very important: a script cuts it automatically)
- One **16:9 image**, flat **neutral grey background (#808080)** everywhere. No text, labels, numbers, grid lines, ground line, shadows or scenery.
- **4 columns × 2 rows of equal cells**, one frame per cell, read left to right, then the second row. Every frame is **centred in its cell with empty grey space all around**. Nothing touches or crosses a cell border, and no frame touches another.
- Draw big: each creature is about **220 to 280 px tall in the image**. We downscale it ourselves to its game size, so keep shapes bold and readable at about 36 px tall: a clear silhouette, big eyes, few small details.
- Grounded things: **every frame of a row stands on the same invisible baseline** (the bottom of the feet or base at the same height in every cell). Flying things: centred in their cell.

### Consistency (the most important rule)
- **Draw frame 1 first, then derive every other frame from it**: same character design, **same size**, same proportions, same colours, same position in the cell. Only the parts that animate move.
- Never change the head size, the body size, the eye size or the feet position between frames of the same sheet.
- Within a sheet, frames must **loop smoothly** when played in order.

### Death must read instantly
When the katana hits, the enemy dies in a quarter of a second. From the **very first "break" frame** it must be **obviously destroyed**: the instrument shatters, the eyes become **X or spirals**, pieces fly off. **No break frame may show the enemy standing intact with open eyes.**

---

## Sheet `E1` to `E8`: the 8 local enemies (send one per message)

> Do only sheet `E1` now (then `E2`… in the next messages).
>
> **Frames, in this order (4 × 2 grid):**
> 1. `idle`: neutral pose, eyes open, standing on the baseline.
> 2. `squash`: beat 1, the instrument is hit: about 15 % wider and 10 % lower, still on the baseline.
> 3. `stretch`: beat 2, taller and thinner, hopping a little above the baseline, eyes wide.
> 4. `blink`: same as idle, eyes half closed (for a 3-frame idle).
> 5. `hurt`: the katana hits it: violent recoil backwards, **eyes already turned into X**, a white impact flash shape on the body, the instrument dented.
> 6. `break_0`: the instrument **shatters**: a crack splits it in two, 2-3 big pieces flying apart, X eyes, little white sparks.
> 7. `break_1`: the pieces are far apart and falling, smaller, a puff of dust.
> 8. `break_2`: only a few small fragments and a fading puff remain (almost empty cell).
>
> The **body** (what the katana hits) fills a box about **32 px wide × 36 px tall in game** (see the red box of the scale image), feet on the baseline. Ears, antennae or horns may stick out a little above, but stay thin.

Per-enemy details (keep their colours and instrument from v1, make them cooler):
- `E1` **Djembe-mushroom** (forest, the basic enemy): red cap with white dots that **is** a djembe drum skin, carved wooden djembe body, small legs. Squash: the cap flattens like a hit drum skin. Break: the cap splits in two.
- `E2` **Bass-drum toad** (swamp): fat green toad whose belly is a bass drum (white skin, red rim, chrome lugs). Squash: throat and belly inflate.
- `E3` **Maracas bat on the ground** (crystal cave): standing bat holding two maracas. **Bright colours that pop on a dark purple cave**: deep violet body with a light lavender rim on its top edges, orange-yellow maracas, white eyes. Squash: shakes the maracas down. Stretch: wings up.
- `E4` **Amp-crab** (volcano): crab whose body is a black guitar amplifier with a speaker cone, glowing orange lava cracks, eye stalks. Squash: claws snap down like a snare hit.
- `E5` **Gong-golem** (mountain): small stone golem framing a golden gong with a face. Squash: the gong vibrates (ring lines). Stretch: sways.
- `E6` **Xylophone-walrus** (glacier): brown walrus whose tusks are mallets, colourful xylophone bars on its belly. Squash: strikes its own bars.
- `E7` **Trumpet-gargoyle** (ruined city): stone gargoyle with small wings, blowing a golden trumpet. Squash: puffed cheeks.
- `E8` **Kettledrum-cloud** (storm summit): grumpy blue storm cloud sitting on a copper timpani, a small lightning spark. Squash: the cloud swells.

---

## Sheet `E9`: the flying bat (every level)

> Do only sheet `E9` now. The flying bat appears in every level, often **in front of jungle vines and dark caves, where the v1 bat disappeared**. It must be readable at a glance on dark green, dark brown, dark purple and black backgrounds:
> - deep violet body with a **bright light-lavender rim** on all outer edges;
> - **hot orange wing membranes**;
> - bright yellow maracas in its feet;
> - big white eyes and small white fangs.
>
> Body (head + torso) about **32 × 36 px in game**, wings spanning up to **56 px**, centred in its cell.
>
> **Frames (4 × 2):** 1-4 wing flap (wings up, middle, down, middle; the body stays in the same place) · 5 `hurt` (X eyes, recoil, flash) · 6 `break_0` (maracas fly away, the bat shatters into 2-3 pieces) · 7 `break_1` (pieces spinning down) · 8 `break_2` (a few small fragments).

## Sheet `E10`: hanging bat (row 1) and the armoured beetle (row 2)

> Do only sheet `E10` now.
>
> **Row 1: the bat hanging upside down from the ceiling**, same design and colours as `E9`, wings wrapped around its body, about **32 × 40 px in game**. **Its feet are at the top of the cell** (they grip the ceiling) and its head is at the bottom. Frames: 1 asleep · 2 eyes open · 3 `hurt` (X eyes) · 4 `break` (it shatters and falls).
>
> **Row 2: the armoured beetle**, a springboard enemy. The hero **lands on top of it and bounces high**, but touching its sides hurts. This is the most important size rule of the whole set:
> - it fits **exactly a 32 px wide × 36 px tall box in game** (the red box with the yellow top in the scale image);
> - its **top is a flat, bright yellow pad that spans its whole width and is its highest point**: nothing (no antenna, no horn) sticks out above the pad;
> - its **sides have small spikes**, to show where it hurts.
>
> Teal armoured shell like a drum machine, angry yellow eyes in the shell slit, little legs, base on the baseline.
>
> Frames: 5 walk A · 6 walk B · 7 `stomped` (the pad pushed in, the shell squashed to about 70 % height, spring lines) · 8 `recover` (pops back to full height, the pad glowing).

## Sheet `O1`: drum-trampoline and jump note

> Do only sheet `O1` now.
>
> **Row 1: the drum-trampoline**, about **44 × 40 px in game**. It must read instantly as "bounce here", not as a barrel:
> - a big drum with a **taut white skin that is exactly its flat top edge** (about 36 px wide in game);
> - a bright coloured rim, tension ropes, and a small upward chevron painted on the shell;
> - base on the baseline (the game sinks its lower half into the ground).
>
> Frames: 1 idle · 2 pressed (skin pushed down, sides bulging) · 3 launch (skin bulging up, two short motion lines) · 4 idle with the chevron glowing.
>
> **Row 2: the jump note**, about **28 × 28 px in game**, centred. The hero bounces off it in mid-air: a glowing **cyan eighth note** (head, stem, flag) inside a thin white ring. It must read clearly on any background, so give it a dark outline and a light halo. Frames: 5-7 pulse (small, big, glowing ring) · 8 `taken`: the note bursts into cyan sparks.

---

## Sheets `V1` to `V3`: vines, one style per biome

> Do only sheet `V1` now (then `V2`, `V3`).
>
> The hero grabs a hanging vine in mid-air, swings and lets go. **The game draws the rope itself** by placing small links one after the other along a curved swinging line. So we need **parts, not a whole vine**, and the links must look right **at any angle**: they are round or almost round, never long or directional.
>
> **This sheet has 3 rows (one biome per row) of 4 cells**, same grey background, same rules. In each row:
> 1. **anchor**: what the vine hangs from (branch knot, hook, ring…), about **16 × 16 px in game**;
> 2. **link**: one round piece of the rope, about **6 × 6 px in game**, that tiles into a rope when repeated;
> 3. **grip**: the bottom end that the hero holds, about **12 × 12 px in game**;
> 4. **ornament**: a small thing hanging along the rope (leaf, bead, charm…), about **10 × 10 px in game**.
>
> The vine must **stand out from its biome's background** (a lighter rim or a bright accent colour), because enemies and the hero pass in front of it.

- `V1`:
  - row 1 **Sylve** (drum forest): green flowering vine, leaf-bud links, a small pink flower ornament;
  - row 2 **Bayou** (misty swamp): mossy rope with hanging Spanish moss, a firefly-lantern ornament;
  - row 3 **Échos** (crystal cave): glowing violet crystal beads on a thin chain, a small crystal shard ornament.
- `V2`:
  - row 1 **Forge** (volcano): dark iron chain with red-hot links, a glowing ember ornament;
  - row 2 **Échine** (mountain): braided climbing rope, a little prayer-flag ornament;
  - row 3 **Givre** (glacier): frosted rope with ice-bead links, an icicle ornament.
- `V3`:
  - row 1 **Cité Sourde** (ruined musical city): brass chain with small bell links, a tiny bell ornament;
  - row 2 **Orage** (storm summit): kite string with electric blue beads, a lightning-spark ornament;
  - row 3 **Sourd** (level 9): **only pure black, pure white and red (#FF2A4A)**, an ink rope of black beads with a white rim, and a red musical-note ornament.

---

## Sheets `B1` to `B3`: the Sourdine, final boss of level 9

The last level is a world that loses its colour: **everything is black (#000000), white (#FFFFFF), two greys (#3A3842,
#7C7A86) and one red (#FF2A4A, dark #8A0F24)**. The Sourdine floats on the right of the screen, goes up and down on the
beat, fires horizontal lasers from its mouth and spits black notes. At the end, the hero **slices it in two with one
katana slash**.

> Do only sheet `B1` now.
>
> **The Sourdine**: a giant floating **trumpet mute** (the cone-shaped plug that silences a brass instrument) that became a creature of black ink. The wide round end faces the hero and is its face. The narrow end is a tail that drips ink. Small broken rings of ink float around it, like silenced sound waves. Elegant and menacing, but still readable and a bit funny, like the enemies. **Palette: only the black, white, greys and red above.** A thin white rim around the whole silhouette, so it reads on black backgrounds.
>
> - Size in game: about **176 px wide × 168 px tall**.
> - **The face is drawn separately** (sheet `B2`). Leave a **flat, empty black oval face area** in the wide end, about 140 × 110 px in game, centred a little above the middle.
>
> **Frames (4 × 2):** 1-3 floating loop (ink drips and rings move, the body breathes slightly; same size and position) · 4 charging a laser (the body leans forward, white cracks of light) · 5 **dead, top half** and 6 **dead, bottom half**: the body cut cleanly along a diagonal line (from top-left to bottom-right), with a white glowing cut edge, the face already drawn on these halves with cracked X eyes and a screaming mouth · 7-8 leave empty.

> Do only sheet `B2` now: the **face parts** of the Sourdine, same palette, each centred in its cell. This sheet may use **4 × 3 cells** (3 rows).
>
> - **Row 1, eyes**, about **48 × 28 px in game**: big white almond eyes with a black outline and a red inner glow. Frames: 1 open · 2 wide (angry, before a laser) · 3 half closed · 4 closed.
> - **Row 2**: 5 cracked eye (death) · then the **pupils**, round, red with a white glint: 6 pinpoint (about 8 px) · 7 medium (12 px) · 8 big (16 px).
> - **Row 3, mouths**, all **120 px wide in game, attached by their top edge**:
>   - 9 smile, 24 px tall: a thin grin of white piano keys;
>   - 10 half open, 40 px tall;
>   - 11 screaming, 56 px tall: the laser comes out of it, with a white-hot inside.
>
>   Cell 12 stays empty.

> Do only sheet `B3` now: the **attacks**, same palette.
>
> 1. **Laser beam tile**, about **64 × 12 px in game**: white-hot core, red edges. It must **tile seamlessly left to right**.
> 2. **Warning line tile**, **64 × 4 px**: a thin red dashed line, tiling left to right.
> 3. **Muzzle flash**, **32 × 32**: a burst at the mouth where the laser starts.
> 4. **Black note A**, **34 × 34**: an eighth note of black ink with a white rim and one red eye, open.
> 5. **Black note B**: the same note with the eye closed (blink).
> 6. **Gold note**, **34 × 34**: the same note retuned, in gold, shining (shown when the hero slashes it).
>
> Cells 7-8 stay empty.
