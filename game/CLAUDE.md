# game/ — Projet Unity (Kenta)

Base : clone de [anatidae-toolkit](https://github.com/XariusExcl/anatidae-toolkit) (sans son `.git`).
Ouvrir **ce dossier `game/`** dans Unity Hub, avec **exactement Unity 6000.3.23f1** (ADR-008). Ne pas changer de version sans décision d'équipe : ça réécrit `ProjectSettings/` et crée des conflits.
Scène de travail : `Assets/_Project/Scenes/Main.unity`, la seule dans les Build Settings. Plateforme active : **Web**.
Piège : l'ancien 6000.0.40f1 bloque sur « Initialize Package Manager » sur les Mac Apple Silicon sans Rosetta 2. Ne plus l'utiliser.

## Configuration figée (ne pas changer sans ADR)
- **Built-in Render Pipeline** (pas d'URP ni de HDRP). Espace couleur Gamma.
- **Ancien Input Manager** (`activeInputHandler: 0`). Ne pas installer le package Input System.
- Web : template **Minimal**, compression **Disabled** (le serveur Express de la borne n'envoie pas les en-têtes gzip/brotli), 1920×1080, data caching activé.
- Mode éditeur par défaut en 3D : on fait de la 2D en sprites et caméra orthographique, sans changer le template.

## Arborescence
```
Assets/
├── Anatidae/            ← TOOLKIT, lecture seule (sauf GameName + style UI)
├── Plugin/BackToMenu.jslib  ← toolkit, lecture seule
├── Scenes/InputTester.unity ← scène de test des manettes (toolkit)
└── _Project/            ← TOUT notre code et nos assets
    ├── Scripts/{Core,Rhythm,Player,Level,FX,UI,Net,Debug}/
    ├── Prefabs/  Scenes/  Audio/  Charts/  Art/  Materials/  ScriptableObjects/
```
Namespace : `Kenta.<Dossier>` (ex. `Kenta.Rhythm`). Un fichier = une classe. Noms en anglais.
**Pas d'asmdef** dans `_Project/` : les scripts Anatidae sont dans `Assembly-CSharp`, et un asmdef ne peut pas le référencer.
Les `.gitkeep` gardent les dossiers vides dans git (Unity ignore les fichiers qui commencent par un point).

## Toolkit Anatidae — ce qu'on réutilise
- `HighscoreManager.GameName` → **`"Kenta"`** (`Assets/Anatidae/Scripts/HighscoreManager.cs`). Doit être identique au nom du dossier de build.
- Le prefab **`AnatidaeInterface`** doit être présent dans **chaque scène**. Il contient `MenuManager` (bouton blanc 1,5 s + AFK 60 s), `HighscoreNameInput` et `HighscoreUI`.
- Au démarrage : `StartCoroutine(HighscoreManager.FetchHighscores())`, **avant** tout `IsHighscore()`.
- Fin de partie : `if (HighscoreManager.IsHighscore(score)) HighscoreManager.ShowHighscoreInput(score);`. Bloquer nos menus tant que `IsHighscoreInputScreenShown` est vrai.
- Appels vers notre VPS : **uniquement `AnatidaeProxyWebRequest.Get/Post`**, qui passe par `localhost:3000/proxy`. URL de base dans un ScriptableObject `NetConfig` (en local : `http://localhost:8080/api`). Contrat dans [../docs/api-contract.md](../docs/api-contract.md).
- Normal en éditeur : `EntryPointNotFoundException: BackToMenu` en sortant du Play mode (ou après 60 s d'AFK, ou avec Échap maintenu). Le `.jslib` n'existe qu'en build Web. À ignorer, ne pas « corriger » le toolkit.
- `TextMesh Pro/.../LiberationSans SDF - Fallback.asset` change tout seul (atlas de police dynamique) : **ne pas le commiter** (`git restore` dessus).
- `ExtradataManager` sert à stocker des clés/valeurs sur la borne (stats globales, ex. nombre total de morts).

## Flow d'écrans (ADR-007)
Scène unique `Scenes/Main.unity`. `Kenta.Core.GameFlow` est une machine à états : `Attract → Explain → Config (facultatif) → Playing → GameOver → NameEntry (si top 10) → Attract`. Un prefab par écran dans `Prefabs/Screens/`. Détails : [../docs/game-design.md](../docs/game-design.md).
- Attract : démo en autoplay qui alterne avec `HighscoreUI`. N'importe quel bouton lance la partie (et débloque l'audio).
- GameOver : score + classement, puis `IsHighscore` → `ShowHighscoreInput`. Attendre que `IsHighscoreInputScreenShown` repasse à faux avant de revenir à Attract.

## Entrées
- Classe unique `Kenta.Core.ArcadeInput` : lit `P{n}_Horizontal`/`P{n}_Vertical` (dead zone **0,3** sur `GetAxisRaw`) et `P{n}_B1`, `P{n}_B2`, `P{n}_Start`.
- Mapping de jeu : **B1 = saut** (maintenu = saut plus haut), **B2 = frappe**, **joystick bas = glissade**. Start = valider dans les menus.
- Clavier de dev (P1, AZERTY) : ZQSD, F = B1, G = B2, X = Start, **Échap = bouton blanc**. Voir [../docs/anatidae.md](../docs/anatidae.md).
- Axe vertical manette déjà inversé dans l'InputManager : **haut = positif**.

## Rythme (cœur technique)
- `Conductor` (singleton, `Rhythm/Conductor.cs`) : `SongTime` vient de `timeSamples / frequency`, interpolé avec `unscaledDeltaTime` et recalé à chaque nouveau paquet audio (correction douce, saut au-delà de 20 ms). API : `Play(SongData, startBeat, loop, useRemix)`, `Seek(beat)`, `Stop()`, `SongTime`, `SongBeat`, `BeatToSeconds()` (relatif au beat 0, offset déjà retiré), `OnBeat(int)`, `OnSongEnd`. Il gère les clips en boucle (menu).
- Démarrage via `Play()` + `timeSamples`, **pas** `PlayScheduled`/`dspTime` (non garantis sur WebGL).
- Un morceau = un asset `SongData` (`ScriptableObjects/Songs/`) : clip, remixClip, bpm, offsetSeconds, beatsPerBar, chart.
- S'abonner aux événements du Conductor dans `Start()` (pas `OnEnable`), ou en différé comme `FX/BeatPulse` : l'ordre des `Awake` n'est pas garanti.
- Pistes de test générées par `tools/gen-test-beat.py` (kick sur chaque temps) : `Audio/Test/test_*_120.wav`.
- ⚠️ Le navigateur exige une interaction avant de jouer du son. À vérifier **sur la borne** (l'appui sur un bouton de manette compte-t-il ?). Attract = « APPUIE SUR UN BOUTON », et ce premier appui débloque l'audio.
- `LevelBuilder` : lit la chart JSON (`Charts/*.json` via `TextAsset`) et instancie les prefabs à `x = BeatToSeconds(beat) * runSpeed`. Pooling obligatoire.
- `ActionSfx` : quantifie les sons d'action à la double-croche (`round(beat * 4) / 4`).
- Clips musicaux : Vorbis, `Compressed In Memory`, `Preload Audio Data` activé. SFX courts : `Decompress On Load`.

## Gameplay et DA Kenta (voir [../docs/art-direction.md](../docs/art-direction.md))
Le jeu est en **pixel art 320 × 180 px, agrandi ×6** (3 calques : monde, acteurs, interface ; défilement au sous-pixel, voir docs/art-direction.md). Monde : 40 px/unité. Hitbox héros 0,5 × 1,0 u (glissade 0,5 u), saut 1,5 u en 1 beat, vitesse 8 u/s. Mort subite.
| Script | Rôle |
|---|---|
| `Core/RunManager` | Une partie : build du niveau, score/combo, drops (flash, secousse), **mort subite** (un coup = fin de partie, pas de cœurs ni de checkpoint). |
| `Core/GameFlow` | Flow d'écrans imposé : Attract → Explain → Config (choix du héros) → Playing → GameOver → NameEntry. |
| `Core/GameSession` | Héros choisi (statique). |
| `Level/BiomeTheme` ×8, `Level/LevelTheme` (`Theme_Kenta`) | Un biome par section de la chart : décor, sol, trou, obstacles, ennemi local. Remplacer un sprite = changer la référence, sans code. |
| `Level/LevelBuilder` | Chart → obstacles + collisions, visuels du biome du beat. Géométrie : docs/chart-format.md. |
| `FX/PixelScreen`, `FX/PixelLayer`, `FX/PixelMath`, `FX/CameraRig`, `FX/BiomeBackdrop` | Rendu 320×180 en 3 calques (couche Unity `Actors` pour le héros), caméra au sous-pixel qui suit aussi en hauteur, ciel + 3 calques en parallaxe. |
| `FX/BandanaTrail`, `FX/Sourdine`, `FX/ScreenFx`, `FX/FxPool`, `FX/BeatFrames` | Traînée du bandeau, mur de silence, flash, particules en pixels, djembé qui change d'image sur le temps. |
| `Player/PlayerController`, `HeroAnimator`, `HeroProfile`, `HeroSpriteSet` | Physique maison réglée en beats ; pose du héros (8 poses du PDF) ; 3 héros. |
| `UI/PixelText`, `PixelFontData`, `RunHud`, `AttractScreen`, `HeroSelectScreen`, `ExplainScreen`, `GameOverScreen` | Interface en pixels (canvas 320×180) : police proportionnelle à ombre, plaques sous les textes posés sur le décor. |
| `Editor/KentaSpritePostprocessor`, `Editor/KentaBuilder` | Import des sprites ; menu **Kenta > Reconstruire thèmes, prefabs et scène**. |
- Aucun moteur physique (ADR-009). Aucun `Instantiate` en jeu : le niveau est construit une fois par partie, puis on réactive au respawn.
- **Sprites : jamais dessinés dans Unity.** Héros et ennemis viennent du PDF (`tools/extract-kenta-pdf.py`), le reste est généré (`tools/gen-kenta-assets.py`). Après toute régénération : menu Kenta > Reconstruire.
- Pas de rotation ni d'échelle fractionnaire d'un sprite ; l'interface utilise des agrandissements **entiers** (`PixelText.scale`). En code UI, ne pas utiliser `SetNativeSize()` (il applique le PPU du monde) : `sizeDelta = sprite.rect.size`.
- Autres assets générés : `tools/gen-sfx.py` (bruitages), `tools/gen-test-beat.py` (musique de test). La chart du niveau vient de `tools/gen-level-chart.py`.

## Juice / perfs (GPU intégré)
- Cible **60 fps** en build Web sur la borne. Pas de post-process plein écran coûteux, pas d'ombres, peu de lumières.
- Juice = secousses en pixels entiers, flash des drops, particules en pixels (< ~300 actives). **Pas d'animation par crans sur le beat** (lu comme un tremblement) : mouvements continus ou cycles d'images.
- Sprites pixel art : filtre Point, aucune compression (réglé par `KentaSpritePostprocessor`).
- Pas d'allocation dans `Update` (pas de LINQ ni de `new` par frame) : le GC WebGL provoque des saccades.

## Debug
- `Debug/` (namespace **`Kenta.DebugTools`**, jamais `.Debug`, qui masquerait `UnityEngine.Debug`) : `RhythmDebugOverlay` affiche état, temps, beat, dérive et fps, avec un carré qui flashe sur le beat. Visible en éditeur et en Development Build, **F9** pour basculer (F1 ouvre l'aide de Firefox). Avec l'overlay affiché : grille de beats dans le niveau, et **F10 = autoplay** pour valider une chart.
- `GameFlow` (`Core/GameFlow.cs`) : les écrans sont des prefabs `Prefabs/Screens/Screen_*.prefab` sous le canvas `Screens` (sortingOrder −10, sous l'overlay Anatidae). En attendant le gameplay : +10 points par beat, **Start = fin de partie**.

- **Tests** : Unity Test Framework, tests EditMode sans asmdef dans `Assets/_Project/Editor/Tests/` (MCP `run_tests`, mode EditMode). Tests Python des générateurs : `pytest tools/tests`.
- ⚠️ Piège MCP : `manage_camera screenshot` recompose mal les calques pixel (image fausse). Pour voir le vrai rendu : `ScreenCapture.CaptureScreenshot("Temp/gv.png")` via `execute_code`, puis lire le fichier.
- ⚠️ Piège MCP : chaque `execute_code` compile du C# dans l'éditeur et fige le jeu ~300 ms (`dt` = 0,333). Pendant une partie en autoplay, ça fait rater un saut. Pour valider une chart : lancer l'autoplay, **ne rien appeler** pendant la durée du morceau, puis lire le résultat une seule fois. Même chose pour les captures (`manage_camera screenshot`) et les gros calculs CPU (génération de sprites).

## Build → borne
1. File > Build Profiles > **Web** → Build dans **`game/Build`** (gitignoré).
2. `tools/package-build.sh` : copie vers `dist/Kenta/`, ajoute `BuildExtras/{info.json,thumbnail.png,attract.mp4?}` et installe le tout dans la borne locale.
3. `cd tools/anatidae-arcade && node server.js` → `http://localhost:3000`. Le jeu apparaît dans le menu (Entrée/B1 pour lancer). Accès direct : `http://localhost:3000/Kenta/`.
4. Vérifier : **Échap maintenu 1,5 s → retour menu** (testé OK le 2026-09-25), AFK 60 s → menu, `GET /api/?game=Kenta` en 200, saisie du highscore, dead zone, 60 fps.
`game/BuildExtras/thumbnail.png` est générée : `python3 tools/gen-kenta-assets.py --only thumbnail` (la Sylve, Kenta et le logo).

## MCP Unity
Package `com.coplaydev.unity-mcp` **figé en `#v10.2.0`** dans `Packages/manifest.json`. On ne pointe jamais sur `#main` : les 3 postes doivent avoir la même version. Pour monter de version, mettre à jour le manifest et la version du serveur MCP dans la même PR.
Quand il est connecté, s'en servir pour : créer et modifier GameObjects, prefabs et composants, lire la console après compilation, lancer le Play mode. Toujours vérifier la console (0 erreur) avant de rendre la main.
Sans MCP : écrire le C# et lister les étapes éditeur. **Jamais** de modification manuelle des fichiers YAML (`.unity`, `.prefab`, `.asset`, `.meta`).
