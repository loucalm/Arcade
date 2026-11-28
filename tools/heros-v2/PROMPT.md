# Héros v2 : images à refaire ou à ajouter, dossier pour Gemini

Les héros v2 (ADR-045) ont repris les animations de la marionnette **image pour image**, mais plusieurs ne tiennent pas en
jeu, et le code sait jouer des animations que personne n'a encore dessinées. Ce dossier sert à les faire dessiner par
Gemini (Antigravity ne génère plus d'images). L'import se fait ensuite avec `tools/heros-v2/import.py`, étendu par
Antigravity (`CONSIGNE_ANTIGRAVITY.md`).

## Ce qui ne va pas (mesuré le 28/11)
| Animation | Défaut | Mesure |
|---|---|---|
| Course (8, Ryo 9) | pas de vrai cycle : la 2e moitié marche, les deux pieds au sol, sans phase en l'air ; la tête monte et descend sans rythme | hauteur de Kenta 52→57 px au hasard (marionnette : 52, 52, 50, 51 ×2) ; appui au sol de 6 à 30 px |
| Saut (8) | **plus de salto** : saut droit, aucune image tournée | marionnette : salto avant sur les images 3 à 6 |
| Liane (2) | les mains ne sont pas au point de prise : la corde du jeu arrive à la poitrine | prise du jeu à 44 px au-dessus des pieds ; mains à 70 px (Kenta, Ryo), 54 px (Aiko) |
| Chute (3) | poses debout, pas une chute | — |
| Vides | coup en l'air, atterrissage, envol de tambour, victoire : le code les joue s'ils existent (`HeroSpriteSet`) | aujourd'hui, la frappe au sol est jouée en l'air, jambes de course comprises |

À corriger aussi dans le code, sans nouvelle image (voir `CONSIGNE_ANTIGRAVITY.md`) : l'attache de la traînée du
bandeau (0,95 u au-dessus des pieds, la tête de la marionnette : celle des héros v2 est plus haut) et la place des mains
sur la liane.

## Mode d'emploi
1. Gemini web, **une conversation par héros**. Joindre :
   - `refs/style_<héros>_v2_x4.png` : le héros tel qu'il est dans le jeu, la référence de dessin ;
   - `refs/modele_course_x4.png`, `refs/modele_saut_salto_x4.png` et `refs/modele_liane_chute_x4.png` : les mouvements de
     la marionnette, la référence de poses et de rythme (pas de dessin).

   Puis coller le **Prompt commun**.
2. Une planche par message : coller son bloc (« Sheet `R` … »). Si une image sort d'une autre taille ou d'un autre dessin,
   la redemander tout de suite (« frame 6 is bigger than frame 1, redo it with the exact same size »).
3. Enregistrer chaque image dans `tools/heros-v2/raw/v3/<héros>_<id>.png` (ex. `kenta_R.png`, `aiko_J.png`).
4. Puis Antigravity : `CONSIGNE_ANTIGRAVITY.md`, partie 2 (import, tests), et relecture par Claude.

| id | Contenu | Images | Priorité |
|---|---|---|---|
| `R` | Course : vrai cycle de 2 pas | 8 | P0 |
| `J` | Saut avec salto avant | 8 | P0 |
| `L` | Liane 4 + chute 3 + atterrissage 1 | 8 | P0 |
| `A` | Coup de katana en l'air | 6 | P1 |
| `T` | Envol de tambour 3 + victoire 5 | 8 | P1 |

Soit 5 planches par héros, 15 en tout ; commencer par les P0 des trois héros.

---

## Prompt commun

You are drawing **new animation frames for an existing pixel-art hero** of *Kenta*, a 2D side-scrolling rhythm runner for an arcade cabinet (1920×1080, integer upscaling, 40 px = 1 game unit). The hero runs **to the right**, jumps on the beat and slashes enemies with a katana. In game the hero is about **52 px tall** (Aiko about 49 px).

### The hero must be exactly the same character
- The attached `style_<hero>_v2` image shows the hero **as it already is in the game**: same face, hair, headband, clothes, colours, katana and proportions. Draw exactly that character, with the same crisp 16-bit pixel-art style: **1-pixel dark outline (#14101C)**, light from the top-left, 3 to 4 tones per material, no blur, no anti-aliasing, no gradient, no painterly texture.
- The attached `modele_*` images show **the poses and the rhythm** we want (a simple puppet of the same hero). Copy their **movement**, not their drawing.
- Always facing **right**. The katana stays the same object (same length, same colours) in every frame.

### Sheet layout (every sheet, very important: a script cuts it automatically)
- One **16:9 image**, flat **neutral grey background (#808080)** everywhere. No text, labels, numbers, grid lines, ground line, shadow, rope, drum or scenery.
- **4 columns × 2 rows of equal cells**, one frame per cell, read left to right, then the second row. Each frame is **centred in its cell with empty grey space all around**; nothing touches a cell border or another frame. Leave the unused cells empty.
- Draw big: the standing hero is about **250 px tall** in the image.

### Consistency (the most important rule)
- **Draw frame 1 first, then derive every other frame from it**: same character, **same scale**, same head size, same proportions. Never redraw the hero bigger or smaller from one frame to the next.
- Grounded frames: **the soles of the feet on the same invisible baseline** in every cell of the row.
- The frames must **loop smoothly** when played in order (except one-shot animations: takeoff, landing, victory).

---

## Sheet `R`: run cycle, 8 frames (P0)

> Do only sheet `R` now.
>
> A real **running** cycle at full speed, two steps: frames 1-4 = right foot forward, frames 5-8 = the same with the left foot. Copy the rhythm of `modele_course`:
> - frame 1, **contact**: front heel touches the baseline;
> - frame 2, **down**: knee bent, the body at its lowest (2 px lower in game);
> - frame 3, **passing**: support leg straight under the body, the other knee coming forward;
> - frame 4, **flight**: **both feet off the ground**, the body at its highest (2 px higher in game), legs split.
>
> Then frames 5-8 = the same four phases with the other leg.
>
> - Torso leaning forward about 10°, **arms swinging opposite to the legs**, hair and headband tails blown back.
> - The katana keeps exactly the same place on the body (sheathed at the hip, or on the back, like in the style image).
> - The **head moves only up and down** with the phases (low, mid, high), never forward or back. The body stays centred in the cell.

## Sheet `J`: jump with a forward somersault, 8 frames (P0)

> Do only sheet `J` now.
>
> The hero jumps on the beat and does a **forward somersault (front flip)** in the air, like `modele_saut_salto`:
> 1. takeoff: crouched, feet on the baseline, arms back;
> 2. launch: body stretched upward, arms up, feet leaving the ground;
> 3. tuck: knees pulled to the chest, still upright;
> 4. **rotated 90° forward**: tucked ball, head to the right;
> 5. **rotated 180°**: tucked ball **upside down**, head at the bottom;
> 6. **rotated 270°**: head to the left;
> 7. untuck: upright again, legs extending down;
> 8. landing prep: legs slightly forward, arms out for balance.
>
> - Frames 3-7: the tucked hero keeps **the same size** and **turns around the centre of the body**, which stays at the centre of the cell. Frames 1, 2 and 8 stand on (or just above) the baseline.
> - The katana stays attached to the body and turns with it.

## Sheet `L`: vine swing, fall and landing, 8 frames (P0)

> Do only sheet `L` now. **Do not draw the vine or the rope: the game draws it.**
>
> **Row 1, hanging from a vine (4 frames)**: the hero hangs from a vertical rope with **both hands closed together above the head, gripping an invisible rope**.
> - The **gripping hands are at exactly the same point in all 4 frames**: top centre of the cell. The game puts the hands exactly on the end of the rope.
> - The arms stay straight, and the body swings below the hands like a pendulum:
>   1. swung back, legs behind;
>   2. hanging straight down;
>   3. swung forward, legs forward and slightly bent;
>   4. hanging straight, knees lifted.
>
> **Row 2, falling and landing (4 frames)**, like `modele_liane_chute`:
> - frames 5, 6 and 7, **falling** from a ledge: looking down, arms up and flailing, legs bent apart, hair blown upward. A loop of three frames, with no ground under the feet.
> - frame 8, **landing**: knees bent, body squashed, arms out for balance, feet on the baseline.

## Sheet `A`: katana slash in mid-air, 6 frames (P1)

> Do only sheet `A` now.
>
> The same katana slash as on the ground, with the same 6 steps, but **in mid-air** (during a jump): legs tucked under the body instead of running legs.
>
> The 6 steps: 1 drawing the blade · 2 the slash starts · 3 **full slash**, the strongest pose · 4 follow-through · 5 blade low behind · 6 back on guard.
>
> - Draw the blade and only a **short white arc right next to the blade**: the game adds the long slash trail itself, up to the hit reach.
> - Centred in the cell, no baseline (the hero is in the air).
> - Cells 7-8 stay empty.

## Sheet `T`: drum launch and victory, 8 frames (P1)

> Do only sheet `T` now.
>
> **Frames 1-3, launch from a drum-trampoline** (do not draw the drum):
> 1. deep crouch, feet on the baseline;
> 2. explosive extension, arms thrown up, feet leaving the ground;
> 3. **star pose** high in the air (arms and legs spread), a big confident grin.
>
> **Frames 4-8, victory at the end of a level** (one-shot, the last frame is held):
> 4. lands, feet on the baseline;
> 5. straightens up;
> 6. spins the katana;
> 7. **raises the katana to the sky**, other fist clenched;
> 8. the same pose, the headband tails floating (the held frame).
