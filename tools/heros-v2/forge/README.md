# Héros : course, montée et descente refaites dans Gemini avec un guide de poses (Agent Sprite Forge)

Les cycles actuels (course, montée, descente) sont des dessins indépendants : les jambes ne suivent pas une foulée
(pas de vraie phase de passage ni de phase en l'air), et `stabilize.py` ne peut que recaler la tête (ADR-045, mise à
jour « animations »). Cette fois, Gemini reçoit en plus un **guide de poses** fait par Agent Sprite Forge
(`plan_guide.py --frames 8 --cycle run --facing right --aspect 2:1`) : 8 cases, un bonhomme bâton par case, la jambe
côté spectateur en orange, l'autre en bleu, qui alternent sur deux pas (appui, flexion, passage, en l'air), une ligne
de sol et un point d'ancrage fixes.

## Fichiers à joindre
- `tools/heros-v2/refs/style_<héros>_v2_x4.png` : le héros tel qu'il est dans le jeu (référence de dessin) ;
- `tools/heros-v2/forge/guide_course.png` : le guide de poses (référence de mise en page et de poses, rien à recopier).

## Mode d'emploi
1. Gemini (appli ou web), **une conversation par héros**, en commençant par Kenta.
2. Joindre les deux fichiers ci-dessus, puis coller le **Prompt course** (remplacer `<HERO>` par Kenta, Aiko ou Ryo).
3. Vérifier à l'œil avant d'enregistrer :
   - 8 héros, 2 rangées de 4, le même dessin et la même taille partout ;
   - images 1 à 4 et 5 à 8 **pas identiques** : la jambe devant change de côté ;
   - images 4 et 8 : les deux pieds en l'air ;
   - aucun trait du guide (bâtons, cadres bleus, ligne noire) recopié.

   Sinon, répondre par exemple : « frame 6 is bigger than frame 1, redo the whole sheet with the exact same size » ou
   « frames 5-8 repeat frames 1-4: in frames 5-8 the other leg must be in front ».
4. Enregistrer l'image dans `tools/heros-v2/raw/forge/<héros>_course.png` (`kenta_course.png`, `aiko_course.png`,
   `ryo_course.png`).
5. Même conversation : **Prompt montée** → `<héros>_montee.png`, puis **Prompt descente** → `<héros>_descente.png`.
6. Prévenir Claude : contrôle de la foulée (`sheet_qc.py frames --cycle run` de Forge), découpe et réduction au gabarit
   96 × 72, `stabilize.py`, tests, branchement dans Unity.

Commencer par la course des trois héros : c'est l'animation qu'on voit tout le temps. Montée et descente ensuite.

---

## Prompt course

```text
You are drawing a new RUN CYCLE for an existing pixel-art hero, <HERO>, of "Kenta", a 2D side-scrolling rhythm
runner. The hero runs to the RIGHT.

Two images are attached:
1. style_<hero>_v2: the hero exactly as it is in the game. Draw exactly this character: same face, hair, headband,
   clothes, colours, katana and proportions, same crisp 16-bit pixel-art style, 1-pixel dark outline (#14101C), light
   from the top-left, 3 to 4 tones per material, no blur, no anti-aliasing, no gradient.
2. guide_course: a layout and pose reference ONLY. Do not draw any of its lines, boxes, colours, grey bands or stick
   figures.

Request aspect 2:1. Do not state pixel sizes.
- 8 poses in 2 rows x 4 columns, read left to right, top row first.
- The grey band around every cell is empty gutter: no pixel of the character may enter it.
- Keep the whole character (hair, headband tails, both feet, the katana) inside the blue safe box of its cell.
- The black line is the ground and the tick is the fixed root: one body scale and one root in every cell.
- The character faces RIGHT in every cell.
- Match each cell's stick pose. ORANGE limbs are NEAR limbs (viewer side): normal brightness, drawn in front. BLUE
  limbs are FAR limbs (away side): one shade darker, partly hidden behind the body.
Poses (NEAR/FAR, never left/right):
0 contact: NEAR leg reaches forward, heel on the ground line; FAR leg trails, foot lifted.
1 down: NEAR leg under the body, knee bent, carrying the weight; FAR leg folds up behind.
2 passing: NEAR leg pushes off behind; FAR knee swings forward past it.
3 flight: both feet off the ground; FAR leg reaches forward for the next contact, NEAR leg trails.
4 contact: FAR leg reaches forward, heel on the ground line; NEAR leg trails, foot lifted.
5 down: FAR leg under the body, knee bent, carrying the weight; NEAR leg folds up behind.
6 passing: FAR leg pushes off behind; NEAR knee swings forward past it.
7 flight: both feet off the ground; NEAR leg reaches forward for the next contact, FAR leg trails.
Frames 4-7 must not repeat frames 0-3: the leg drawn in front swaps between NEAR and FAR.

Consistency, the most important rule: draw frame 0 first, then derive every other frame from it. Same character,
same scale, same head size in all 8 frames. Torso leaning forward about 10 degrees, arms swinging opposite to the
legs, hair and headband tails blown back. The katana keeps exactly the same place on the body in every frame, as in
the style image. The head only moves up and down with the phases (lowest on "down", highest on "flight").

Background: one flat neutral grey (#808080) everywhere, absent from the character.
No text, labels, numbers, grid lines, shadows, dust or checkerboard.
```

## Prompt montée

```text
Same character, same rules, same guide, same layout and same 8 leg phases as the run cycle you just drew.
This time the hero RUNS UP A SLOPE: the torso leans further forward (about 25 degrees), steps are shorter, knees lift
higher, arms pump harder. Keep the ground line flat in the drawing (the game tilts nothing): only the posture changes.
Same scale as the run cycle: the head of frame 0 at the same height as in the run cycle, minus the lean.
```

## Prompt descente

```text
Same character, same rules, same guide, same layout and same 8 leg phases as the run cycle you just drew.
This time the hero RUNS DOWN A SLOPE: the torso leans slightly back (about 10 degrees), arms a little out for balance,
longer bouncing steps, the front heel reaching further forward. Keep the ground line flat in the drawing: only the
posture changes. Same scale as the run cycle.
```
