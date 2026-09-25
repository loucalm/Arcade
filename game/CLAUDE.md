# game/ — Projet Unity (Rythme Runner)

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
Namespace : `RythmeRunner.<Dossier>` (ex. `RythmeRunner.Rhythm`). Un fichier = une classe. Noms en anglais.
**Pas d'asmdef** dans `_Project/` : les scripts Anatidae sont dans `Assembly-CSharp`, et un asmdef ne peut pas le référencer.
Les `.gitkeep` gardent les dossiers vides dans git (Unity ignore les fichiers qui commencent par un point).

## Toolkit Anatidae — ce qu'on réutilise
- `HighscoreManager.GameName` → **`"RythmeRunner"`** (`Assets/Anatidae/Scripts/HighscoreManager.cs`). Doit être identique au nom du dossier de build.
- Le prefab **`AnatidaeInterface`** doit être présent dans **chaque scène**. Il contient `MenuManager` (bouton blanc 1,5 s + AFK 60 s), `HighscoreNameInput` et `HighscoreUI`.
- Au démarrage : `StartCoroutine(HighscoreManager.FetchHighscores())`, **avant** tout `IsHighscore()`.
- Fin de partie : `if (HighscoreManager.IsHighscore(score)) HighscoreManager.ShowHighscoreInput(score);`. Bloquer nos menus tant que `IsHighscoreInputScreenShown` est vrai.
- Appels vers notre VPS : **uniquement `AnatidaeProxyWebRequest.Get/Post`**, qui passe par `localhost:3000/proxy`. URL de base dans un ScriptableObject `NetConfig` (en local : `http://localhost:8080/api`). Contrat dans [../docs/api-contract.md](../docs/api-contract.md).
- Normal en éditeur : `EntryPointNotFoundException: BackToMenu` en sortant du Play mode (ou après 60 s d'AFK, ou avec Échap maintenu). Le `.jslib` n'existe qu'en build Web. À ignorer, ne pas « corriger » le toolkit.
- `TextMesh Pro/.../LiberationSans SDF - Fallback.asset` change tout seul (atlas de police dynamique) : **ne pas le commiter** (`git restore` dessus).
- `ExtradataManager` sert à stocker des clés/valeurs sur la borne (stats globales, ex. nombre total de morts).

## Flow d'écrans (ADR-007)
Scène unique `Scenes/Main.unity`. `RythmeRunner.Core.GameFlow` est une machine à états : `Attract → Explain → Config (facultatif) → Playing → GameOver → NameEntry (si top 10) → Attract`. Un prefab par écran dans `Prefabs/Screens/`. Détails : [../docs/game-design.md](../docs/game-design.md).
- Attract : démo en autoplay qui alterne avec `HighscoreUI`. N'importe quel bouton lance la partie (et débloque l'audio).
- GameOver : score + classement, puis `IsHighscore` → `ShowHighscoreInput`. Attendre que `IsHighscoreInputScreenShown` repasse à faux avant de revenir à Attract.

## Entrées
- Classe unique `RythmeRunner.Core.ArcadeInput` : lit `P{n}_Horizontal`/`P{n}_Vertical` (dead zone **0,3** sur `GetAxisRaw`) et `P{n}_B1`, `P{n}_B2`, `P{n}_Start`.
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

## Gameplay (démo jouable)
| Script | Rôle |
|---|---|
| `Core/RunManager` | Une partie : build du niveau, cœurs (3), score/combo/multiplicateur, checkpoints = sections, respawn (seek audio), fin → `GameFlow.EndRun()`. Mode `demo` = autoplay sur l'Attract. |
| `Level/ChartData` | Parse la chart JSON (`JsonUtility`, champ `@params`). |
| `Level/LevelBuilder` | Chart → visuels (sprites teintés) + données de collision (`LevelObject`). Géométrie : voir docs/chart-format.md § Placement. |
| `Level/LevelTheme` (SO `Theme_Neon`) | Sprites de base + palette. Les artistes remplacent ici, sans code. |
| `Player/PlayerController` | X = `BeatToX(SongBeat)` ; Y = physique maison réglée en beats. Collisions AABB **balayées** entre frames (anti-traversée). |
| `Player/PlayerInputs` | `IPlayerInput` : `ArcadePlayerInput` (borne) ou `AutoPlayerInput` (joue la chart parfaitement). |
| `Rhythm/ActionSfx` | Sons d'action quantifiés à la double-croche (`PlayDelayed`), lums = gamme pentatonique. |
| `FX/CameraRig`, `FX/FxPool`, `FX/Backdrop`, `FX/BeatPulse` | Caméra (suivi, shake, punch, flash du fond), particules en pool, décor en parallaxe, pulsation sur le beat. |
| `UI/RunHud` | HUD sur le prefab `Screen_Playing`. |
| `Player/PlayerRig` | Héros néon « sans membres » construit en code (ADR-011), posé chaque frame par `PlayerController` selon `PlayerPose`. |
| `FX/Chaser`, `FX/DropDirector` | Mur de flammes qui poursuit le joueur ; drops sur les sections `refrain`/`drop`/`final` (effondrement du décor, zoom). |
| `FX/Telegraph`, `FX/BeatDance`, `FX/BeatFlipbook`, `FX/GroundWave` | Télégraphie 1 mesure avant l'action, danse sur le beat, animation de sprites, onde sur le sol. |
- Aucun moteur physique (ADR-009). Aucun `Instantiate` en jeu : le niveau est construit une fois par partie, puis on réactive au respawn.
- Assets provisoires générés : `tools/gen-shapes.py` (formes 1×1 u, PPU 64), `tools/gen-sfx.py` (bruitages), `tools/gen-test-beat.py --style rock --structure …` (musique de test).
- Niveau actuel : `Charts/level01_neon_rock.json` + `Song_NeonRock` (piste provisoire 120 BPM). Vérifier une chart : `python3 tools/check-chart.py <chart.json>` (0 problème), puis autoplay (F9, F10).
- Piège : une frame très longue (capture d'écran, onglet en arrière-plan) peut faire tomber le perso, même en autoplay. Ne pas conclure à une chart infaisable sans rejouer.

## Juice / perfs (GPU intégré)
- Cible **60 fps** en build Web sur la borne. Pas de post-process plein écran coûteux, pas d'ombres, peu de lumières.
- Juice = screen shake, squash & stretch, hit-stop court, sprites **additifs**, particules légères (< ~300 actives), flash sur `OnBeat`, tweening de l'UI.
- Sprites en **Sprite Atlas**, textures en puissance de 2 si possible, taille max 2048.
- Pas d'allocation dans `Update` (pas de LINQ ni de `new` par frame) : le GC WebGL provoque des saccades.

## Debug
- `Debug/` (namespace **`RythmeRunner.DebugTools`**, jamais `.Debug`, qui masquerait `UnityEngine.Debug`) : `RhythmDebugOverlay` affiche état, temps, beat, dérive et fps, avec un carré qui flashe sur le beat. Visible en éditeur et en Development Build, **F9** pour basculer (F1 ouvre l'aide de Firefox). Avec l'overlay affiché : grille de beats dans le niveau, et **F10 = autoplay** pour valider une chart.
- `GameFlow` (`Core/GameFlow.cs`) : les écrans sont des prefabs `Prefabs/Screens/Screen_*.prefab` sous le canvas `Screens` (sortingOrder −10, sous l'overlay Anatidae). En attendant le gameplay : +10 points par beat, **Start = fin de partie**.

## Build → borne
1. File > Build Profiles > **Web** → Build dans **`game/Build`** (gitignoré).
2. `tools/package-build.sh` : copie vers `dist/RythmeRunner/`, ajoute `BuildExtras/{info.json,thumbnail.png,attract.mp4?}` et installe le tout dans la borne locale.
3. `cd tools/anatidae-arcade && node server.js` → `http://localhost:3000`. Le jeu apparaît dans le menu (Entrée/B1 pour lancer). Accès direct : `http://localhost:3000/RythmeRunner/`.
4. Vérifier : **Échap maintenu 1,5 s → retour menu** (testé OK le 2026-09-25), AFK 60 s → menu, `GET /api/?game=RythmeRunner` en 200, saisie du highscore, dead zone, 60 fps.
`thumbnail.png` est une vignette provisoire (dégradé) : à remplacer par la vraie jaquette carrée.

## MCP Unity
Package `com.coplaydev.unity-mcp` **figé en `#v10.2.0`** dans `Packages/manifest.json`. On ne pointe jamais sur `#main` : les 3 postes doivent avoir la même version. Pour monter de version, mettre à jour le manifest et la version du serveur MCP dans la même PR.
Quand il est connecté, s'en servir pour : créer et modifier GameObjects, prefabs et composants, lire la console après compilation, lancer le Play mode. Toujours vérifier la console (0 erreur) avant de rendre la main.
Sans MCP : écrire le C# et lister les étapes éditeur. **Jamais** de modification manuelle des fichiers YAML (`.unity`, `.prefab`, `.asset`, `.meta`).
Piège : le bridge Unity parle au serveur MCP **HTTP** `127.0.0.1:8080/mcp`, enregistré pour une session Claude Code lancée **dans le dossier du repo**. Lancée ailleurs, la session utilise un autre serveur et répond « No Unity Editor instances found ».
