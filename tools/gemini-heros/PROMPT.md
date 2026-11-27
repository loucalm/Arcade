# Kenta : les trois héros redessinés, dossier pour Gemini

But : des héros **beaucoup plus stylés** (silhouette forte, poses d'action dynamiques, matière et lumière) et **les animations qui manquent**. Aujourd'hui, les héros sont dessinés par une marionnette en code (`tools/hero_rig.py`, ADR-022). Elle est propre mais raide : corps fin, peu de détails, et plusieurs poses n'ont qu'une image (liane, course sur le mur, touché, mort). Pas de pose de victoire, pas de coup en l'air, pas de rebond sur un tambour.

On garde : les trois héros, leurs couleurs et signes distinctifs (docs/da-kenta.md § 4), le katana (ADR-039), la taille dans le jeu et la boîte de collision. On change le dessin.

## Ce qui ne change pas (contraintes du jeu)
- **Cases fixes** : 96×72 px par image (coup au katana : 160×72). Le héros est centré horizontalement, **pieds sur la dernière rangée de pixels** : c'est le point d'ancrage dans Unity (pivot en bas au centre). Un saut ou une vrille reste dans la case, sans la déborder.
- **Taille** : le héros debout fait **52 à 56 px** de haut, coiffure comprise (aujourd'hui environ 50). La boîte de collision fait 20×40 px (`refs/gabarit_case_96x72_x4.png` : jaune = collision, cyan = silhouette visée).
- **Face à droite** dans toutes les images. Le jeu retourne l'image lui-même au plafond (gravité inversée) et en miroir.
- **Traînée du coup** : sur les images de coup, la traînée du katana s'arrête à **76 px** devant l'axe du héros (`PlayerController.hitReach` × 40). La dépasser montrerait une portée qui n'existe pas.
- Images en **pixel art pur** : pixels carrés tous de la même taille, aucun anti-aliasing, aucun dégradé, contour sombre d'un pixel (#14101C). Couleurs de `palettes.json` (des tons intermédiaires sont acceptés, ils seront ramenés à la palette par le script).

## Ce que Gemini ne fait pas (fait par le code ou un script)
- La traînée du bandeau (les deux pans qui s'allongent avec le combo) : `FX/BandanaTrail`. Gemini dessine le nœud du bandeau et des pans **courts** seulement.
- Le flash blanc du touché, les notes qui s'envolent à la mort, la poussière, les étincelles de la frappe : `FX/`.
- Les sons, le calage sur le temps : `HeroAnimator` répartit les images sur la durée de l'action.

## Mode d'emploi
1. **Gemini web**, une conversation par héros. Joindre `refs/actuel_<héros>_x4.png`, `refs/actuel_<héros>_coup_x4.png` et `refs/gabarit_case_96x72_x4.png` (+ `gabarit_coup_160x72_x4.png` pour les coups). Coller le **Prompt commun** ci-dessous, puis : « Hero: `<kenta|aiko|ryo>`. Do only sheet `<id>` now. »
2. **D'abord la planche `0_model`** (fiche du personnage). Si elle plaît, la joindre à **toutes** les planches suivantes du même héros : c'est elle qui garde le personnage identique d'une image à l'autre. Si elle ne plaît pas, la refaire avant d'aller plus loin.
3. **Commencer par Kenta et la planche `1_run`** : on teste toute la chaîne (Gemini → script → jeu) sur la course avant de lancer les 30 planches.
4. Enregistrer chaque image dans `raw/sheets/<héros>_<id>.png` (ex. `raw/sheets/kenta_1_run.png`).
5. **Ensuite (Claude)** : `clean.py` sera écrit quand les premières planches seront là, sur le modèle de `tools/gemini-obstacles/` : découpe des cases, réduction à la taille exacte, couleurs ramenées à `palettes.json`, pieds recalés sur la ligne de sol, puis installation dans `Gen/heroes/` (mêmes noms qu'aujourd'hui) et ajout des nouvelles animations dans `HeroSpriteSet` et `HeroAnimator`.

## Les planches (une par message, pour chaque héros)
| id | Contenu | Images | Case |
|---|---|---|---|
| `0_model` | Fiche du personnage : debout de profil (face à droite), de trois quarts, de dos, plus un buste ×2 avec 3 expressions (neutre, déterminé, cri de victoire). **Sert de référence seulement**, pas dans le jeu. | 6 | libre, mais même grille de pixels |
| `1_run` | Course : cycle de 2 pas | 8 | 96×72 |
| `2_idle_victory` | Attente (respiration, clignement sur la 4e) ; **victoire** (nouveau) : moulinet du katana, le rengaine d'un coup sec, poing levé, bandeau qui claque | 4 + 6 | 96×72 |
| `3_jump_land` | Saut : impulsion, genoux remontés, **vrille d'un tour** (4 images), ouverture, réception ; **atterrissage** (nouveau) : écrasé, se relève ; chute depuis un bord : pas dans le vide, chute (2 images en boucle) | 8 + 2 + 3 | 96×72 |
| `4_climb` | Montée d'une pente : course penchée en avant, genoux hauts | 8 | 96×72 |
| `5_descend` | Descente d'une pente : penché en arrière, foulée retenue, bras écartés pour l'équilibre | 8 | 96×72 |
| `6_slash` | Coup au sol : dégaine, la coupe part, coupe pleine avec **traînée en arc** (≤ 76 px devant l'axe), la traînée s'efface, suivi, retour en garde ; **coup en l'air** (nouveau) : même coup, jambes repliées, buste tourné | 6 + 4 | 160×72 |
| `7_moves` | **Liane** (nouveau, 4 images) : suspendu d'une main, jambes qui balancent d'avant en arrière ; **course sur le mur** (nouveau, 6 images) : sprint presque à la verticale, penché à 60° vers l'avant, pieds qui griffent la paroi ; **envol de tambour** (nouveau, 4 images) : accroupi, détente, bras levés, étoile en l'air | 4 + 6 + 4 | 96×72 |
| `8_hurt_death` | **Touché** (nouveau, 3 images) : recul, grimace, yeux plissés ; **mort** (nouveau, 5 images) : projeté en arrière, tombe, rebondit au sol, couché, immobile (le katana tombe à côté) | 3 + 5 | 96×72 |
| `9_portrait` | Portraits pour les menus (choix du héros, saisie du nom) : buste de trois quarts, 3 expressions (neutre, déterminé, victoire), **64×64** | 3 | 64×64 |

Total : 10 planches par héros, 30 en tout.

---

## Prompt commun

You are redrawing the **three playable heroes of Kenta**, a 2D side-scrolling rhythm runner in pixel art for a school arcade cabinet (1920×1080, integer upscaling, 40 px = 1 game unit). The hero runs to the right at a constant speed and **jumps (button 1) and slashes with a katana (button 2) on the beat of the music**. Each action plays a sound that completes the song. Work only inside this folder. Do not modify any other file.

### The heroes (keep their colours and signature details, see `palettes.json`)
The three heroes carry the **Bandana of the Score**: a headband engraved with the last intact melody of the world. Each is a young musician-swordsman with a katana on the hip and their instrument on the back.
- **Kenta**, the drummer (the main hero): spiky red-orange hair standing up like flames, **white headband** with a small engraved eighth note and a steel plate, **orange scarf** whose ends fly behind him, navy short jacket with a brown belt and a gold buckle, dark trousers, brown boots, **two drumsticks crossed on his back**, katana with a gold guard. Energetic, cocky grin, always leaning forward.
- **Aiko**, the flautist: long **turquoise ponytail** that dances behind her, **violet headband**, white crossed jacket (like a kimono top) with a violet sash, dark-indigo trousers, brown shoes, **bamboo flute at the belt**, katana with a silver guard. Elegant, precise, light on her feet.
- **Ryo**, the bassist: wild **white mane**, **red headband**, green jacket over a white shirt, dark trousers, **bass guitar slung on the back like a greatsword**, katana with a bronze guard. Older, calm, heavy and powerful movements.

### Make them much cooler (the goal of this redesign)
- **Stronger silhouette**: readable at 52-56 px tall on a busy background. Slightly bigger hands and feet, a clear head shape, big hair, scarf and ponytail that exaggerate the motion. About 3.5 heads tall (not chibi).
- **Dynamic action poses**: clear line of action, anticipation before each move, overshoot after it. Hair, headband knots, scarf and coat tails **follow through** one or two frames behind the body. Smear frames are welcome in fast moves (one stretched frame), as pixel art, not blur.
- **Material and light**: light from the top-left. 3 tones per material (shadow, base, light) taken from `palettes.json`, a 1-pixel dark outline #14101C around the whole character, selective inner lines. A small bright glint on the katana blade and on the buckle.
- **Expressive faces**: 2-pixel eyes with a white highlight, eyebrows that show effort, a grin or a shout on the big moves.
- Original design only: do not imitate any existing game or character.

### Frame rules (very important: the game uses these images directly)
- Each frame sits in a **fixed cell**: 96×72 px (the katana slash: 160×72; portraits: 64×64). See `refs/gabarit_case_96x72_x4.png`: the green line is the ground, the blue line the axis of the hero, yellow the collision box (20×40), cyan the target silhouette (about 28×54).
- The hero **faces right**, centred on the blue axis, **feet on the bottom row of pixels** of the cell in every grounded frame. In the air (jump, spin, vine, wall run), keep the whole body inside the cell.
- All frames of one action are drawn **with the exact same character**: same proportions, same colours, same pixel size. Only the pose changes.
- Slash frames (`6_slash`): the katana trail is a clean crescent in pale cream (#FFF6D8, #F6E6B0) with a thin coloured edge in the hero's headband colour; it **must stop 76 px in front of the hero's axis** (the reach of the hit in the game).
- Do not draw the long ends of the headband: only the knot and two short tails (the game draws the long trail).
- No ground, no shadow under the feet, no dust, no sparks, no text, no numbers, no frame borders.

### Output
One image per message: the frames of the requested sheet, **left to right in the order of the table, one action per row**, on a **flat #FF00FF magenta background**. Draw on an exact grid of cells (96×72, 160×72 or 64×64 game pixels), all cells the same size, nothing crossing a cell border, with a 1-cell-wide magenta gap between actions if it helps. Draw at any integer scale (×4 or more recommended) as long as every game pixel is a clean square of the same size.
Save it as `raw/sheets/<hero>_<id>.png`.

### Per-sheet details
- `0_model` (reference only): standing side view facing right, three-quarter view, back view (instrument and katana visible), and three head-and-shoulders busts (neutral, determined, victory shout). Same pixel scale for everything.
- `1_run` (8 frames): two strides. Contact, down, passing, up, then the other leg. Torso leaning forward 10-15°, arms pumping, the free hand open, the katana hand on the sheath. Hair and scarf (Kenta), ponytail (Aiko) or mane (Ryo) flow back and bounce once per stride.
- `2_idle_victory`: idle (4 frames): weight on the back leg, hand on the katana grip, slow breathing, blink on frame 4. Victory (6 frames): spin the katana in the right hand, sheathe it with a sharp click, fist (Kenta), bow with the flute raised (Aiko) or bass raised (Ryo), headband knot snapping.
- `3_jump_land`: jump (8 frames): crouch, push-off, knees up, a full **somersault in 4 frames**, open up, ready to land. Landing (2 frames): squash with bent knees, stand up. Fall (3 frames): step into the void, then 2 looping falling frames with arms up.
- `4_climb` (8 frames): running up a steep slope, torso leaning far forward, high knees, short strides.
- `5_descend` (8 frames): running down a slope, torso leaning back, long braking strides, arms out for balance.
- `6_slash`: ground slash (6 frames, 160×72): draw, the cut starts, full cut with the crescent trail, trail fading, follow-through, back on guard. Air slash (4 frames, 160×72): same cut in the air, knees tucked, torso twisted.
- `7_moves`: vine (4 frames): hanging by one hand above the head (the rope is not drawn), the other arm out, legs swinging forward then back. Wall run (6 frames): sprinting up an almost vertical wall, body tilted 60° forward, feet clawing, a loop. Drum launch (4 frames): crouched on the drum, explosive extension, arms up in a star, spinning up.
- `8_hurt_death`: hurt (3 frames): knocked back, grimace with squeezed eyes, recover. Death (5 frames): thrown backwards, falling, bouncing on the ground, lying on the back, still; the katana falls beside the hero. No blood, no gore: the game is played by everyone.
- `9_portrait` (3 frames, 64×64): three-quarter bust facing right, headband and hair framing the face, neutral, determined, victory shout.
