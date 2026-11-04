# Kenta — polish, niveaux par biome, mort subite, jugement, verticalité : plan d'action

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal :** rendre Kenta fluide et propre (320×180 ×6, défilement au sous-pixel, course animée, interface refaite), passer à 8 niveaux choisis dans un écran façon osu!, mort subite, jugement PARFAIT/BIEN/BOF avec note S+…D, et un parcours vertical (marches, plateformes).

**Architecture :** le rendu passe par trois calques pixel (monde, acteurs, interface) agrandis ×6, la caméra donne une position exacte dont la fraction de pixel décale l'image agrandie. Le terrain devient une liste de segments de sol à hauteur variable (`Terrain`), lu par le joueur et la caméra. Une partie charge un `LevelDef` (biome + morceau + chart) ; le score passe par `TimingJudge` et `GradeCalculator`, purs et testés.

**Tech Stack :** Unity 6000.3.23f1 (Built-in RP, ancien Input Manager, WebGL), C#, Unity Test Framework (EditMode, tests sans asmdef dans `Assets/_Project/Editor/Tests/`), Python 3 + Pillow/numpy/librosa (`tools/`, tests `pytest` dans `tools/tests/`), MCP Unity (CoplayDev v10.2.0).

**Spec :** [docs/superpowers/specs/2026-11-04-kenta-polish-niveaux-design.md](../specs/2026-11-04-kenta-polish-niveaux-design.md)

## Global Constraints
- Contraintes borne du CLAUDE.md racine : 1920×1080, Firefox ESR, GPU intégré, `MenuManager` présent, `ArcadeInput` seulement, 3 boutons, flow imposé.
- Horloge maître = `Conductor.SongTime` ; `x = BeatToX(SongBeat)` ; vitesse constante (8 u/s) ; aucun moteur physique (ADR-009) ; aucune allocation par frame.
- Pixel art : monde à **40 px/unité**, définition **320 × 180**, agrandissement **×6 entier**, aucune rotation ni échelle fractionnaire de sprite.
- Saut : **1,5 u** en **1 beat** (inchangé). Hitbox héros 0,5 × 1,0 u, glissade 0,5 u.
- Jugement : **PARFAIT ≤ 45 ms**, **BIEN ≤ 90 ms**, **BOF** au-delà ; facteurs de points **1,0 / 0,6 / 0,2** ; BOF remet le combo à 0.
- Note : précision `acc = (P + 0,6·B + 0,2·F) / N` ; **S+** = tout PARFAIT et toutes les pièces ; **S** acc ≥ 0,95 et pièces ≥ 90 % ; **A** acc ≥ 0,85 ; **B** ≥ 0,70 ; **C** ≥ 0,50 ; sinon **D**. Niveau non fini : note calculée pareil mais plafonnée à **C**.
- Sélection de niveau : liste en arc à **gauche**, infos + top 3 à **droite**, délai **15 s**, liste qui boucle.
- Pour l'instant, les **8 `LevelDef` pointent vers le même `SongData` et la même chart** ; seul le biome change.
- Toolkit `Assets/Anatidae/**` en lecture seule (sauf `GameName` et le style des UI Highscore). Jamais d'édition YAML à la main : scène et prefabs via **Kenta > Reconstruire** ou MCP.
- Docs et CLAUDE.md mis à jour **dans le même commit** que le changement concerné ; commits en français, à l'impératif.

## Review Focus
1. **À-coup du navigateur (dt de 0,3 s) sur une marche montante ou une plateforme** : le joueur ne doit ni traverser la face d'une marche ni passer à travers le dessus d'une plateforme → test `Terrain_SweptLanding_*` (Task 6).
2. **Fenêtre qui n'est pas 1920×1080** (éditeur, 1366×768, 2560×1440) : agrandissement entier le plus grand possible, bandes noires, jamais de flou → test `PixelMath_IntegerScale_*` (Task 1).
3. **Appui très en avance (buffer de saut) ou action sans appui (marche franchie par un saut lancé plus tôt)** : jugement BOF, pas d'exception, pas de double comptage → test `TimingJudge_*` + `RunJudge_NoDoubleCount` (Task 5).
4. **Extradata indisponible** (borne locale éteinte, réponse vide ou corrompue) : la sélection affiche « — », aucune exception → test `LevelRecords_Decode_Garbage` (Task 9).
5. **Joystick maintenu dans la sélection** : défilement répété régulier, boucle 8 → 1, jamais deux crans dans la même frame → test `ArcList_Wrap_*` + `RepeatTimer_*` (Task 8).

---

## Branche et commits
Le travail part de `feat/da-kenta` (PR loucalm/Arcade#6), dont il dépend. Voir la section « Git » en fin de document pour l'intégration à `main`.

---

### Task 1 : rendu 320×180 ×6 au sous-pixel, caméra verticale

**Files :**
- Create : `game/Assets/_Project/Scripts/FX/PixelMath.cs`, `game/Assets/_Project/Scripts/FX/PixelLayer.cs`
- Modify : `game/Assets/_Project/Scripts/FX/PixelScreen.cs`, `game/Assets/_Project/Scripts/FX/CameraRig.cs`, `game/Assets/_Project/Editor/KentaBuilder.cs` (section scène), `game/Packages/manifest.json` (via MCP `manage_packages` : `com.unity.test-framework`)
- Test : `game/Assets/_Project/Editor/Tests/PixelMathTests.cs`

**Interfaces :**
- Produces :
  - `PixelScreen.Width = 320`, `Height = 180`, `Ppu = 40`, `Margin = 1` (RT de 322 × 182).
  - `static int PixelMath.IntegerScale(int screenW, int screenH, int w, int h)` → plus grand entier ≥ 1 qui tient.
  - `static (float snapped, float fracPx) PixelMath.Split(float worldUnits, int ppu)` → `snapped` multiple de 1/ppu, `fracPx` ∈ [−0,5 ; 0,5).
  - `PixelLayer` (MonoBehaviour) : `Camera layerCamera`, `RawImage display`, `bool followX`, `bool followY` ; `void SetFraction(Vector2 fracPx)`.
  - Couches Unity : `Actors` (héros, bandeau) et `PixelUI` (canvas). Calques : `World` (followX, followY), `Actors` (followY seulement), `UI` (aucun).
  - `CameraRig.ExactPosition` (Vector2, non arrondie) ; `CameraRig.Fraction` (Vector2 en px) ; la caméra Y suit `CameraRig.Target` avec zone morte **0,6 u** au-dessus / **0,25 u** au-dessous du point d'ancrage et amorti `SmoothDamp` 0,18 s ; ancrage = héros à **48 px** du bas de l'écran quand il est au sol à y = 0.

- [ ] **Step 1 : installer `com.unity.test-framework`** (MCP `manage_packages`), vérifier console vide. Écrire `PixelMathTests` :
  - `IntegerScale(1920,1080,320,180) == 6`, `(1366,768,…) == 4`, `(2560,1440,…) == 8`, `(300,170,…) == 1`.
  - `Split(1.013f, 40)` → snapped `1.0` (à 1e-5), fracPx `0.52` arrondi → vérifier qu'il bascule à snapped `1.025`, fracPx ≈ `-0.48` ; `Split(-0.3f, 40)` → snapped `-0.3`, fracPx `0`.
- [ ] **Step 2 :** lancer les tests (MCP `run_tests`, EditMode) → FAIL (type manquant).
- [ ] **Step 3 :** implémenter `PixelMath`, puis `PixelScreen` à 3 calques : trois RenderTextures 322 × 182 (Point), trois RawImage de 322·s × 182·s sous un parent masqué (`RectMask2D`) de 320·s × 180·s centré, `s = IntegerScale(Screen.width, Screen.height, 320, 180)` recalculé quand la fenêtre change. `PixelLayer.SetFraction` décale `anchoredPosition` de `-fracPx * s` sur les axes suivis. `SetDivider` conservé (REMIX).
- [ ] **Step 4 :** `CameraRig` : position exacte → `Split` → caméras du monde et des acteurs placées au `snapped`, `PixelScreen` reçoit la fraction. Le héros (calque Actors) est rendu à x exact = `snappedCamX - offsetX` (donc immobile à l'écran). Secousses : en px entiers, appliquées au `snapped`.
- [ ] **Step 5 :** `KentaBuilder.BuildScene` : créer les couches, caméras, calques ; canvas `PixelUI` sur la caméra UI ; passer héros + bandeau sur la couche `Actors`. Menu Kenta > Reconstruire, tests → PASS, console 0 erreur.
- [ ] **Step 6 : vérifier en jeu** (autoplay, aucune commande MCP pendant 15 s puis une capture) : décor qui glisse, héros fixe, rien ne vibre. Redimensionner la Game view en 1366×768 → ×4 + bandes noires.
- [ ] **Step 7 :** mettre à jour invariant n°8 du `CLAUDE.md` racine (320×180 ×6), `docs/art-direction.md` (calques, sous-pixel), `game/CLAUDE.md`. Commit `Passe le rendu en 320x180 x6 avec défilement au sous-pixel et caméra verticale`.

### Task 2 : animations continues et course à 6 images

**Files :**
- Create : `tools/gen-hero-run.py`, `tools/tests/test_hero_run.py`
- Modify : `tools/gen-kenta-assets.py` (appel du générateur de course), `game/Assets/_Project/Scripts/Player/HeroSpriteSet.cs`, `game/Assets/_Project/Scripts/Player/HeroAnimator.cs`, `game/Assets/_Project/Scripts/FX/BeatHop.cs`, `game/Assets/_Project/Scripts/UI/AttractScreen.cs`, `game/Assets/_Project/Scripts/UI/RunHud.cs`, `game/Assets/_Project/Editor/KentaBuilder.cs`, `game/Assets/_Project/Editor/KentaSpritePostprocessor.cs`

**Interfaces :**
- Produces :
  - fichiers `Art/Kenta/Gen/heroes/hero_{kenta|aiko|ryo}_run_{0..5}.png`, même taille et même ancrage (milieu-bas) que `Pdf/hero_*_run.png`.
  - `HeroSpriteSet.runCycle : Sprite[]` (6) ; `HeroAnimator` : image de course = `runCycle[floor(distance / strideUnits * 6) % 6]`, `strideUnits = 1.6` (une foulée complète = 1,6 u).
  - `BeatHop` remplacé par un balancement sinus continu (`amplitudePx` 1, période = 2 beats, position arrondie au pixel, phase lissée — jamais de saut d'un cran sur le beat).

- [ ] **Step 1 :** `test_hero_run.py` : pour chaque héros, 6 images ; tailles égales à la source ; ligne du bas non vide (pieds au sol sur ≥ 4 images) ; les 6 images sont toutes différentes ; aucune couleur hors de la palette de la source + contour `#14101C`.
- [ ] **Step 2 :** `pytest tools/tests/test_hero_run.py` → FAIL.
- [ ] **Step 3 :** `gen-hero-run.py` : détecte la ligne de ceinture (première rangée sous le torse où la couleur du pantalon domine), efface sous cette ligne, redessine 2 jambes (couleurs pantalon et chaussures échantillonnées) aux 6 phases d'un cycle de course classique (contact, descente, passage, poussée ×2 côtés), contour 1 px ; le haut du corps descend d'1 px aux phases de contact.
- [ ] **Step 4 :** tests → PASS. Générer, importer (postprocessor : PPU 40, pivot bas-centre pour `Gen/heroes`).
- [ ] **Step 5 :** `HeroAnimator` (cycle par distance, plus de rebond au beat), `BeatHop` continu, suppression des sauts de texte sur le beat (`AttractScreen` : clignotement en fondu de « APPUIE SUR START », plus de pièces en orbite sur le logo ; `RunHud` : plus de rebond du score). Reconstruire, capture en jeu (autoplay) : la course s'anime, plus rien ne saute sur le temps.
- [ ] **Step 6 :** docs (`art-direction.md` : course générée, règle « pas d'animation par crans sur le beat »). Commit `Anime la course sur 6 images et remplace les sautillements au beat par des mouvements continus`.

### Task 3 : police proportionnelle avec ombre, interface refaite

**Files :**
- Modify : `tools/kenta_ui.py` (`glyph_image`, `draw_panel`, nouveaux `draw_list_entry`, `draw_judge_word`), `tools/gen-kenta-assets.py`, `game/Assets/_Project/Scripts/UI/PixelFontData.cs`, `game/Assets/_Project/Scripts/UI/PixelText.cs`, `game/Assets/_Project/Editor/KentaBuilder.cs` (police + tous les écrans), `game/Assets/_Project/Scripts/UI/RunHud.cs`
- Test : `tools/tests/test_font.py`, `game/Assets/_Project/Editor/Tests/PixelTextTests.cs`

**Interfaces :**
- Produces :
  - glyphes : corps 5×7 sans contour, **ombre portée 1 px** (bas-droite, `#1A1426`), largeur variable ; fichier `Gen/font/advances.json` `{ "A": 6, "I": 4, … }` (largeur + 1 px d'espace).
  - `PixelFontData.advances : int[]` (aligné sur `chars`) ; `int PixelFontData.AdvanceOf(char c)` (repli 6).
  - `static int PixelText.MeasureWidth(PixelFontData font, string text, int scale)`.
  - Mise en page 320×180 de tous les écrans (valeurs choisies par le builder, cohérentes avec la maquette validée) ; HUD : score (haut gauche), ×multiplicateur dessous, barre de progression fine (haut centre), pièces (haut droite). **Aucun cœur, aucune bande 1UP/2UP/CRÉD.**
  - Le titre « Highscores » de `AnatidaeInterface` est repositionné/stylé pour ne pas chevaucher le logo (style UI Highscore = modification autorisée du toolkit).

- [ ] **Step 1 :** tests : `test_font.py` (« I » plus étroit que « M », ombre présente sous chaque glyphe non vide, aucun pixel de contour autour du corps) ; `PixelTextTests.MeasureWidth` (« II » < « MM » ; chaîne vide → 0 ; caractère inconnu → avance 6 × scale).
- [ ] **Step 2 :** lancer → FAIL.
- [ ] **Step 3 :** implémenter générateur + `PixelFontData`/`PixelText` (placement par avance cumulée, alignement via `MeasureWidth`).
- [ ] **Step 4 :** nouveaux cadres (`draw_panel` : coins arrondis 2 px, liseré clair 1 px en haut, fond `#1D1830` à 92 %). Refaire la mise en page de tous les écrans dans `KentaBuilder` pour 320×180.
- [ ] **Step 5 :** tests → PASS ; reconstruire ; captures de l'accueil, consignes, choix du héros, HUD, game over ; rien ne se chevauche.
- [ ] **Step 6 :** docs. Commit `Refait l'interface : police proportionnelle avec ombre, nouveaux cadres, HUD allégé`.

### Task 4 : mort subite

**Files :**
- Modify : `game/Assets/_Project/Scripts/Core/RunManager.cs`, `game/Assets/_Project/Scripts/FX/Sourdine.cs`, `game/Assets/_Project/Scripts/UI/RunHud.cs`, `game/Assets/_Project/Scripts/Debug/RhythmDebugOverlay.cs`, `game/Assets/_Project/Scripts/UI/GameOverScreen.cs`
- Test : `game/Assets/_Project/Editor/Tests/SourdineTests.cs`

**Interfaces :**
- Produces :
  - `RunManager.Damage()` → `player.Die()`, arrêt de la musique, `Finish(false)` après **0,8 s** (pose de mort visible), sauf en démo (la démo n'est jamais touchée : autoplay).
  - Supprimés : `Hearts`, `StartHearts`, `startHearts`, `Deaths` (garder un compteur de debug interne si utile), checkpoints (`checkpointScore`, `checkpointLums`, respawn), `completionBonusPerHeart`, `OnRespawned`. `EnterSection` ne garde que le drop visuel (flash + secousse).
  - `static float Sourdine.TargetEdge(int multiplier)` = `10 + (8 − multiplier) × 5` px, borné [4 ; 140].

- [ ] **Step 1 :** `SourdineTests` : `TargetEdge(1) == 45`, `TargetEdge(8) == 10`, `TargetEdge(0)` borné à `≤ 140`.
- [ ] **Step 2 :** FAIL → implémenter → PASS.
- [ ] **Step 3 :** retirer cœurs et checkpoints (RunManager, HUD, overlay, game over) ; compiler ; console vide.
- [ ] **Step 4 : vérifier en jeu** sans autoplay : toucher un ennemi → pose de mort, Sourdine qui recouvre, Game Over ; tomber dans un trou → idem.
- [ ] **Step 5 :** docs (`game-design.md` : mort subite, plus de cœurs ; `game/CLAUDE.md`). Commit `Passe en mort subite : un coup suffit, plus de cœurs ni de checkpoints`.

### Task 5 : jugement PARFAIT / BIEN / BOF et note de fin

**Files :**
- Create : `game/Assets/_Project/Scripts/Core/TimingJudge.cs`, `game/Assets/_Project/Scripts/Core/GradeCalculator.cs`, `game/Assets/_Project/Scripts/UI/JudgePopup.cs`
- Modify : `game/Assets/_Project/Scripts/Player/PlayerController.cs`, `game/Assets/_Project/Scripts/Core/RunManager.cs`, `game/Assets/_Project/Scripts/UI/GameOverScreen.cs`, `game/Assets/_Project/Editor/KentaBuilder.cs`, `tools/kenta_ui.py` (mots PARFAIT/BIEN/BOF et lettres de note S+…D en grand)
- Test : `game/Assets/_Project/Editor/Tests/TimingJudgeTests.cs`, `GradeCalculatorTests.cs`

**Interfaces :**
- Produces :
  - `enum Judgement { Perfect, Good, Meh }` ; `static Judgement TimingJudge.Judge(double offsetSeconds)` ; `static float TimingJudge.Factor(Judgement j)` (1 / 0,6 / 0,2).
  - `struct RunJudgeStats { int perfect, good, meh; }` dans `RunManager.JudgeStats`.
  - `static string GradeCalculator.Grade(int perfect, int good, int meh, int lums, int totalLums, bool completed)` → `"S+" | "S" | "A" | "B" | "C" | "D"` (seuils : Global Constraints).
  - `PlayerController` mémorise `LastJumpPressBeat`, `LastHitPressBeat`, `SlideStartBeat` (beat de l'appui, buffer compris) et les passe : `RunManager.OnObstacleCleared(LevelObject o, double actionBeat)`, `RunManager.OnEnemyKilled(LevelObject o, double actionBeat)`. Un objet n'est jugé qu'une fois (`LevelObject.judged`).
  - `RunManager.OnJudged : event Action<Judgement, Vector2>` → `JudgePopup` (mot coloré au-dessus du héros, 0,5 s, montée continue).
  - Points : `base × Factor × Multiplier` ; Perfect/Good : combo +1 ; Meh : combo = 0.
- Comportement : l'autoplay appuie pile sur le beat → tout PARFAIT.

- [ ] **Step 1 :** tests `TimingJudge_*` : `Judge(0) == Perfect`, `Judge(0.045) == Perfect`, `Judge(-0.046) == Good`, `Judge(0.09) == Good`, `Judge(0.2) == Meh`, `Judge(-1.0) == Meh`. `GradeCalculator_*` : `(10,0,0,20,20,true) == "S+"`, `(10,0,0,19,20,true) == "S"`, `(9,1,0,20,20,true) == "S"` (acc 0,96), `(8,2,0,…) == "A"`, `(5,5,0,…) == "B"`, `(0,0,10,…) == "D"`, `(10,0,0,20,20,false) == "C"`, `(0,0,0,0,0,true) == "S+"` (niveau vide). `RunJudge_NoDoubleCount` : un `LevelObject` déjà `judged` n'ajoute rien (tester via méthode statique `RunManager.ApplyJudgement(ref RunJudgeStats, LevelObject, Judgement)` qui retourne `false` si déjà jugé).
- [ ] **Step 2 :** FAIL → implémenter → PASS.
- [ ] **Step 3 :** brancher `PlayerController` / `RunManager` / `JudgePopup` ; écran de fin : grande lettre de note, puis PARFAIT / BIEN / BOF, pièces, score.
- [ ] **Step 4 : vérifier en jeu** : autoplay → que des PARFAIT et note S+ ; à la main → mélange visible, BOF casse le combo.
- [ ] **Step 5 :** docs (`game-design.md` : barème, seuils, notes). Commit `Ajoute le jugement PARFAIT/BIEN/BOF et la note de fin S+ à D`.

### Task 6 : terrain à hauteur variable (marches, plateformes)

**Files :**
- Create : `game/Assets/_Project/Scripts/Level/Terrain.cs`
- Modify : `game/Assets/_Project/Scripts/Level/LevelBuilder.cs`, `game/Assets/_Project/Scripts/Level/LevelObject.cs`, `game/Assets/_Project/Scripts/Player/PlayerController.cs`, `game/Assets/_Project/Scripts/Player/PlayerInputs.cs` (autoplay), `game/Assets/_Project/Scripts/FX/CameraRig.cs` (ancrage sur le sol courant), `tools/kenta_art.py` + `tools/gen-kenta-assets.py` (tuiles de plateforme et de face de marche par biome), `game/Assets/_Project/Scripts/Level/BiomeTheme.cs` (`platform`, `stepFace`), `docs/chart-format.md`
- Test : `game/Assets/_Project/Editor/Tests/TerrainTests.cs`

**Interfaces :**
- Produces :
  - `struct GroundSpan { float x0, x1, top; bool oneWay; }` ; `class Terrain` : `void Add(GroundSpan s)`, `void Build()` (tri), `bool TryLand(float x, float halfWidth, float yPrev, float yNow, out float top)` (vrai si le bas du corps a croisé `top` par le dessus entre `yPrev` et `yNow`, avec un pied au-dessus du segment), `float SupportTop(float x, float halfWidth, float y)` (sol sous les pieds à ≤ 0,02 u, sinon `float.NegativeInfinity`), `float LowestTop`.
  - Événements de chart (documentés dans `chart-format.md`) :
    - `step` `{beat, params.height}` : à partir de `x = BeatToX(beat + 0.3)`, le sol principal passe à `height` (0 ≤ height ≤ **2,0**). Montée de plus de 0,25 u → face de marche = `LevelObject` de type `StepFace` (`Rect(x, ancienTop, 0.05, height − ancienTop)`), mortelle, et saut autoplay à `beat`. Descente → rien à sauter.
    - `platform` `{beat, params.length, params.height}` : segment `oneWay` de `BeatToX(beat + 0.3)` à `BeatToX(beat + 0.3 + length)` à `height` ; saut autoplay à `beat`. Généralement posé au-dessus d'un `gap`.
    - `gap` : coupe le sol principal à sa hauteur courante (inchangé sinon).
    - Une montée ne dépasse jamais **+1,0 u** par rapport au sol d'où l'on saute (saut 1,5 u).
  - `LevelObjectKind.StepFace` (non frappable, jugée comme un saut).
  - `PlayerController` : atterrissage via `Terrain.TryLand`, perte d'appui via `SupportTop`, mort si `y < Terrain.LowestTop − 2.5`.
  - `CameraRig` : point d'ancrage Y = dernier `top` sur lequel le héros s'est posé (pas sa hauteur en l'air).

- [ ] **Step 1 :** `TerrainTests` : sol plat → `TryLand` vrai en croisant 0 ; trou → `SupportTop` = −∞ au milieu du trou ; marche à 1,0 → atterrit à 1,0 ; plateforme `oneWay` traversée par en dessous (`yPrev < top < yNow`) → pas d'atterrissage ; **`Terrain_SweptLanding_BigDt`** : `yPrev = 2.4`, `yNow = -0.6` au-dessus d'une plateforme à 1,0 → atterrit à 1,0 (pas de traversée) ; segments non triés ajoutés → `Build()` les trie.
- [ ] **Step 2 :** FAIL → implémenter `Terrain` → PASS.
- [ ] **Step 3 :** `LevelBuilder` : construire `Terrain` à partir de `gap`/`step`/`platform`, visuels par biome (sol en tuiles à la bonne hauteur, faces de marche, plateformes) ; `IsOverGap` remplacé par `Terrain`. Autoplay : sauts aux `step` montants et aux `platform`.
- [ ] **Step 4 :** `PlayerController` + `CameraRig` ; chart de test à la main `Charts/test_vertical.json` (marches 0 → 0,5 → 1,0 → 0, plateforme sur trou, plateforme haute avec pièces) ; autoplay → fin sans mort, caméra qui suit les hauteurs sans à-coup.
- [ ] **Step 5 :** docs (`chart-format.md` : nouveaux types, contraintes ; corriger le sommet de saut 2,2 u → 1,5 u). Commit `Ajoute un terrain à hauteur variable : marches et plateformes`.

### Task 7 : chart verticale générée et un biome par niveau

**Files :**
- Modify : `tools/gen-level-chart.py`, `game/Assets/_Project/Charts/level01_shovel_knight.json` (régénérée), `game/Assets/_Project/Scripts/Level/LevelBuilder.cs`, `game/Assets/_Project/Scripts/Core/RunManager.cs`
- Create : `tools/tests/test_level_chart.py`, `game/Assets/_Project/Scripts/Level/LevelDef.cs`, `game/Assets/_Project/Scripts/Level/LevelCatalog.cs`
- Modify : `game/Assets/_Project/Scripts/Core/GameSession.cs`, `game/Assets/_Project/Editor/KentaBuilder.cs` (8 `LevelDef` + `LevelCatalog`)

**Interfaces :**
- Produces :
  - Python : `validate(events, jump_height=1.5) -> list[str]` (vide si faisable : montée ≤ 1,0 u, 1 beat mini entre deux sauts, pas d'ennemi pendant un saut, hauteur ∈ [0 ; 2,0], plateformes ≥ 1 beat de long) ; `PLAN` enrichi d'une colonne `heights` (liste de hauteurs visées par section, écrite à la main) ; `SECTIONS` redevenues musicales (intro, couplet, pont, refrain…).
  - C# : `LevelDef` (SO) : `string id`, `int number`, `BiomeTheme biome`, `SongData song`, `TextAsset chart`, `int stars` (1–5), `string guardianName`. `LevelCatalog` (SO) : `LevelDef[] levels`. `GameSession.Level : LevelDef`.
  - `RunManager.BeginRun(LevelDef level, bool demo)` ; `LevelBuilder.Build(ChartData chart, float bpm, BiomeTheme biome)` → biome unique pour tout le niveau (`BiomeAt` supprimé ou renvoie ce biome).
  - Étoiles par défaut : Sylve 1, Bayou 1, Échos 2, Forge 3, Échine 3, Givre 4, Cité 4, Orage 5.

- [ ] **Step 1 :** `test_level_chart.py` : `validate` détecte une montée de 1,2 u, deux sauts à 0,5 beat, un ennemi à `j + 0.5` ; la chart générée passe `validate` ; elle contient ≥ 4 `step` et ≥ 2 `platform` ; hauteurs max ≥ 1,0.
- [ ] **Step 2 :** FAIL → implémenter (générateur + plan des hauteurs) → PASS ; régénérer la chart avec le WAV d'origine (venv du scratchpad, voir ADR-015).
- [ ] **Step 3 :** `LevelDef`/`LevelCatalog`, `RunManager`, `LevelBuilder` ; `KentaBuilder` crée `ScriptableObjects/Levels/Level_0N_<biome>.asset` + `LevelCatalog.asset`, tous sur `Song_Level01` et la même chart.
- [ ] **Step 4 : autoplay complet** sur 2 biomes (Sylve, Orage) : 0 mort, note S+ (ne rien appeler pendant le morceau, une lecture à la fin).
- [ ] **Step 5 :** ADR-017 (niveaux par biome, mort subite, jugement, 320×180, terrain vertical ; remplace en partie ADR-016 point 3), `game-design.md`, `chart-format.md`, `game/CLAUDE.md`. Commit `Génère une chart verticale et fait un niveau par biome`.

### Task 8 : écran de sélection de niveau (façon osu!)

**Files :**
- Create : `game/Assets/_Project/Scripts/UI/LevelSelectScreen.cs`, `game/Assets/_Project/Scripts/UI/ArcList.cs`, `game/Assets/_Project/Scripts/Core/RepeatTimer.cs`
- Modify : `game/Assets/_Project/Scripts/Core/GameFlow.cs` (nouvel état), `game/Assets/_Project/Scripts/FX/BiomeBackdrop.cs` (défilement lent hors partie), `game/Assets/_Project/Editor/KentaBuilder.cs` (prefab `Screen_LevelSelect`), `tools/kenta_ui.py` (entrée de liste, vignette 40×22 du ciel de chaque biome)
- Test : `game/Assets/_Project/Editor/Tests/ArcListTests.cs`, `RepeatTimerTests.cs`

**Interfaces :**
- Consumes : `LevelCatalog`, `GameSession.Level`, `LevelRecords` (Task 9 ; avant Task 9, afficher « — »).
- Produces :
  - `enum GameState { Attract, Explain, Config, LevelSelect, Playing, GameOver, NameEntry }`.
  - `static int ArcList.Wrap(int index, int count)` ; `static Vector2 ArcList.EntryOffset(float relative)` → x (px, vers la droite) = `56 − 7·r²` borné ≥ 0 pour |r| ≤ 3, y = `−r × 24` ; `relative = index − scrollPosition` (flottant, interpolé).
  - `class RepeatTimer` : `bool Tick(bool held, float dt)` → vrai au premier appui, puis après **0,35 s**, puis toutes les **0,12 s** tant que maintenu ; au plus une fois par appel.
  - `LevelSelectScreen.Done`, `LevelSelectScreen.Selected : LevelDef` ; délai **15 s** → lance la sélection courante ; joystick haut/bas (via `ArcadeInput`), B1 ou Start = jouer.
  - Mise en page (maquette validée, inversée) : liste en arc à gauche (entrée sélectionnée qui ressort vers la droite, plus grande, fond clair) ; à droite « MONDE N / 8 », nom, gardien, « ♪ titre · BPM · durée », étoiles, nombre d'obstacles, top 3 ; fond = décor du biome sélectionné (ciel + calques qui défilent lentement), assombri côté infos.

- [ ] **Step 1 :** tests : `ArcList_Wrap_*` (`Wrap(-1,8) == 7`, `Wrap(8,8) == 0`, `Wrap(17,8) == 1`) ; `EntryOffset(0).x == 56`, `EntryOffset(2).x == 28`, symétrie `EntryOffset(-1).x == EntryOffset(1).x` ; `RepeatTimer_*` : premier appui vrai ; 0,2 s maintenu → faux ; 0,36 s → vrai ; puis vrai toutes les 0,12 s ; relâché puis rappuyé → vrai immédiatement ; un `Tick(true, 1.0f)` ne renvoie qu'un vrai.
- [ ] **Step 2 :** FAIL → implémenter `ArcList`, `RepeatTimer` → PASS.
- [ ] **Step 3 :** `LevelSelectScreen` + prefab + état `GameFlow` (Config → LevelSelect → Playing ; la musique de menu continue ; le décor change avec la sélection).
- [ ] **Step 4 : vérifier en jeu** : défilement fluide, boucle, maintien du joystick, délai 15 s, lancement du bon biome. Capture.
- [ ] **Step 5 :** docs (`game-design.md` : écran 3 bis ; flow ; `game/CLAUDE.md`). Commit `Ajoute l'écran de sélection de niveau façon osu!`.

### Task 9 : top 3 par niveau

**Files :**
- Create : `game/Assets/_Project/Scripts/Core/LevelRecords.cs`
- Modify : `game/Assets/_Project/Scripts/Core/GameFlow.cs` (enregistrement en fin de flow), `game/Assets/_Project/Scripts/UI/LevelSelectScreen.cs`, `game/Assets/_Project/Scripts/UI/GameOverScreen.cs`
- Test : `game/Assets/_Project/Editor/Tests/LevelRecordsTests.cs`

**Interfaces :**
- Produces :
  - `struct LevelRecord { string name; int score; string grade; }`.
  - `static string LevelRecords.Encode(IReadOnlyList<LevelRecord> top)` → `"LOU|34120|S;JRM|29880|A"` ; `static List<LevelRecord> LevelRecords.Decode(string raw)` (tolérant : vide, `null`, morceaux invalides ignorés) ; `static List<LevelRecord> LevelRecords.Insert(List<LevelRecord> top, LevelRecord r, int max = 3)` (trié par score décroissant).
  - Stockage : `ExtradataManager.SetExtraData("rec_" + level.id, Encode(top))` ; lecture `GetDataWithKey` après `FetchExtraData()` au démarrage (une fois). Nom = `HighscoreManager.PlayerName` si la saisie a eu lieu, sinon `"???"`.

- [ ] **Step 1 :** tests : aller-retour `Encode`/`Decode` ; **`LevelRecords_Decode_Garbage`** (`null`, `""`, `"abc"`, `"A|x|S"`, `"A|10"` → liste vide ou entrées valides seules, aucune exception) ; `Insert` garde les 3 meilleurs, trié ; égalité de score → l'ancien reste devant.
- [ ] **Step 2 :** FAIL → implémenter → PASS.
- [ ] **Step 3 :** brancher : écriture après Game Over / NameEntry, affichage dans la sélection et sur l'écran de fin (« NOUVEAU RECORD DU MONDE » si 1er).
- [ ] **Step 4 : vérifier dans la borne locale** (`tools/anatidae-arcade`) : record écrit, relu après rechargement ; borne arrêtée → « — » sans erreur.
- [ ] **Step 5 :** docs (`docs/anatidae.md` : clés `rec_<levelId>` ; `game-design.md`). Commit `Enregistre le top 3 de chaque niveau sur la borne`.

### Task 10 : build, roadmap, PR

**Files :**
- Modify : `docs/roadmap.md`, `README.md`, `game/BuildExtras/thumbnail.png` (régénérée en 320×180 si besoin), `game/BuildExtras/info.json` (description)

- [ ] **Step 1 :** roadmap : cocher ce qui est fait ; ajouter avec priorité : **mode marathon** (P1), **éditeur de niveau sur le rythme** (P1), **une musique et une chart par niveau** (P0, morceau protégé à remplacer), bifurcations route haute/basse (P2), réglage des seuils de jugement sur la borne (P1).
- [ ] **Step 2 :** build Web (MCP `manage_build`), `tools/package-build.sh`, borne locale : parcours complet Accueil → Consignes → Héros → Niveau → Partie → mort → Game Over → saisie ; 60 fps dans Firefox.
- [ ] **Step 3 :** tous les tests (EditMode + `pytest tools/tests`) → PASS ; console 0 erreur ; `git restore` du fichier TMP Fallback.
- [ ] **Step 4 :** commit, push, PR (voir « Git »).

---

## Git
- Ce plan et le design sont commités sur **`main`** (demande de Lou : « fait les commits nécessaires sur le main »).
- Le code dépend de `feat/da-kenta` (PR loucalm/Arcade#6, pas encore mergée). Deux options, **à confirmer par Lou** :
  1. merger la PR #6 dans `main`, puis travailler sur une branche `feat/kenta-polish` partie de `main` (une PR par groupe de tâches) ;
  2. merger la PR #6, puis commiter chaque tâche directement sur `main`.
- Dans tous les cas : un commit par tâche, messages en français à l'impératif, attribution Claude en fin de message.
