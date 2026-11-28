# Météo du décor et Forge fidèle au PDF — plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Le décor de la Forge est redessiné comme la maquette du PDF. Pluie, neige, braises et éruptions jouent derrière le niveau, sur la musique (niveaux 2, 4, 6).

**Architecture:** Des événements de chart `weather` (état, courbes `CorruptionCurve` lues sur `SongBeat`) et `eruption` (coup unique, comme `lightning`) sont posés par `levelgen/builder.py`. `LevelBuilder` les range, `RunManager` les transmet. Un nouveau `FX/WeatherFx` (pool de 120 sprites, derrière le niveau) dessine les particules ; `BiomeBackdrop.Erupt` fait jaillir la lave du cratère lu dans `vents.json`.

**Tech Stack:** Unity 6000.3.23f1 (C#, Built-in RP, WebGL), Python 3 (Pillow, `tools/pixlib.py`), pytest, Unity Test Framework (EditMode).

**Spec:** [docs/superpowers/specs/2026-11-28-meteo-decor-design.md](../specs/2026-11-28-meteo-decor-design.md)

## Global Constraints
- Météo **derrière le niveau** : `sortingOrder` = `firstSortingOrder + 38` de `BiomeBackdrop` (entre calque proche et brume). Jamais devant le héros, les ennemis ou les pièces.
- **120 particules météo au plus** (pool créé une fois, aucun `new` ni LINQ dans `Update`). `FxPool` garde ses 256.
- Pas de `ParticleSystem`, pas d'effet DSP, pas de rotation ni d'échelle fractionnaire. Positions passées par `PixelScreen.Snap`.
- Mouvement continu ; seul l'**instant d'apparition** suit le rythme (double-croche pour la pluie, 1 de la mesure pour les rafales de neige, temps 1 et 3 pour les braises).
- Lueur de l'éruption : teinte par paliers d'un quart, jamais de flash blanc. **Deux `eruption` jamais à moins de 2 temps** (vérifié par le générateur).
- Palette Forge du PDF : `#3A1A16 #5A3A30 #D8401A #FFA040 #FF7A1A #8A1A0A`.
- Charts générées par `python3 tools/gen-level.py <niveau>`, jamais éditées à la main. Pas d'édition manuelle de `.unity/.prefab/.asset/.meta` : passer par le MCP Unity ou `KentaBuilder`.
- Commits en français, à l'impératif. Docs à jour dans le même commit que le changement (CLAUDE.md racine).

## Review Focus
- **Reprise au checkpoint pendant une averse** : intensité juste tout de suite, et pas de rafale de centaines de gouttes. `WeatherMath.Ticks` plafonne les apparitions par image → test `Ticks_CapsAfterSeek` (Task 4).
- **Image longue (lag, GC de la borne)** : même plafond, aucune explosion de particules → couvert par `Ticks_CapsAfterSeek`.
- **Fin de partie, retour à l'accueil ou démo** : la météo d'un niveau ne reste pas sur l'écran suivant → `WeatherFx.SetTracks(null)` coupe tout ; test `WeatherTracks_Null_IsZero` (Task 4).
- **Biome sans cratère (éruption dans un autre biome, niveau 9 qui change de monde)** : `Erupt` ne fait rien, sans exception → test `EruptionGlow_NoVents_Zero` (Task 5).
- **Pool plein** : la particule la plus ancienne est recyclée, aucune allocation → test `Pool_Recycles_Oldest` (Task 4).

---

### Task 1 : Forge redessinée par code (fidèle au PDF) + `vents.json`

**Files:**
- Modify: `tools/kenta_art.py` (branche `k == "volcano"` de `draw_far`, `k == "lavarocks"` de `draw_mid`, `draw_near` pour la chaîne ; nouvelle `vents_json(b)`)
- Modify: `tools/kenta_biomes.py:52-65` (entrée `forge` : couleurs du PDF, `near` avec `chain=True`)
- Modify: `tools/gen-kenta-assets.py:58-64` (écrire `Gen/biomes/vents.json`)
- Test: `tools/tests/test_forge_backdrop.py`

**Interfaces:**
- Produces: `Gen/biomes/vents.json` = `{"items":[{"id":"forge","vents":[[x,y],...]}]}`, x et y en **px de l'image 1280×360 du calque lointain**, y depuis le **haut** (repère image). `kenta_art.VOLCANO_VENTS` (liste de `(x, y)` en px du calque 640 de composition, avant `scale`).

- [ ] **Step 1 : Écrire les tests** (`test_forge_backdrop.py`)
  - `test_vents_json_has_forge` : `vents_json(forge)` renvoie `id == "forge"` et au moins un cratère, x dans `[0, 1280)`, y dans `[0, 360)`.
  - `test_crater_is_glowing` : dans `draw_far(forge)`, le pixel à chaque cratère (± 2 px) contient `#FFA040` ou `#FF7A1A`.
  - `test_single_big_volcano` : un seul cratère par boucle, et son y est dans le tiers haut de l'image (`y < 140`).
  - `test_mid_not_repeated` : dans `draw_mid(forge)`, au moins 3 hauteurs de pics différentes (colonne la plus haute opaque, mesurée par pic).
- [ ] **Step 2 : Lancer** `pytest tools/tests/test_forge_backdrop.py -v` → FAIL (`vents_json` absente).
- [ ] **Step 3 : Redessiner** (spec § « Refonte du décor de la Forge ») :
  - lointain : un grand volcan centré par boucle de 640 (base ~260 px, sommet à ~y 40 en composition), flancs en 2 tons avec arêtes, lèvre du cratère `#FFA040`, 2 coulées sinueuses (cœur `#FFA040`, bords `#D8401A`, ombre `#8A1A0A`), nuage de cendre en bandes au-dessus ; pics de basalte bas et 2–3 silhouettes de cheminées/piliers autour. `tile_wrap` pour la couture.
  - milieu : pics déchiquetés de hauteurs tirées (`rng`), sombres, 1 fissure de lave sur un pic sur trois.
  - proche : arbres orange gardés (couronne ronde + reflets `#FFA040`), plus une chaîne qui pend (maillons de 3×4 px) entre deux arbres, une fois par boucle.
  - `vents_json(b)` multiplie `VOLCANO_VENTS` par `BG_SCALE`.
- [ ] **Step 4 : Lancer** `pytest tools/tests -v` → PASS (tous, pas seulement les nouveaux).
- [ ] **Step 5 : Générer et regarder** : `python3 tools/gen-kenta-assets.py --only biomes && python3 tools/gen-kenta-assets.py --only thumbs`, puis recomposer ciel + 3 calques (comme `docs/img/forge-pdf-vs-jeu.png`, image remplacée) et comparer à la maquette. **Montrer l'image à Lou avant de continuer.**
- [ ] **Step 6 : Commit**
```bash
git add tools/kenta_art.py tools/kenta_biomes.py tools/gen-kenta-assets.py tools/tests/test_forge_backdrop.py game/Assets/_Project/Art/Kenta/Gen/biomes/ game/Assets/_Project/Art/Kenta/Gen/ui/thumb_forge.png docs/img/forge-pdf-vs-jeu.png
git commit -m "Redessine le décor de la Forge d'après la maquette du PDF"
```

### Task 2 : Événements `weather` et `eruption` dans le générateur

**Files:**
- Modify: `tools/levelgen/builder.py:145-149` (à côté de `lightning`)
- Modify: `docs/chart-format.md` (2 lignes dans le tableau du placement et dans « Types d'événements »)
- Test: `tools/tests/test_levels.py` (ajouts)

**Interfaces:**
- Produces: `Level.weather(beat: float, kind: str, intensity: float, length: float = 4.0)` → événement `{"type":"weather","variant":kind,"intensity":…,"length":…}` ; `kind` ∈ `{"rain","snow","embers"}`, sinon `ValueError`. `Level.eruption(beat: float, intensity: float = 1.0)` → `{"type":"eruption","intensity":…}` ; `ValueError` si une autre éruption est à moins de 2 temps.

- [ ] **Step 1 : Tests**
  - `test_weather_event` : `L.weather(128, "rain", 0.4, 8)` ajoute un événement de type `weather`, `variant == "rain"`, `intensity == 0.4`, `length == 8`.
  - `test_weather_unknown_kind` : `L.weather(0, "fog", 1)` lève `ValueError`.
  - `test_eruption_spacing` : `L.eruption(96)` puis `L.eruption(97.5)` lève `ValueError` ; `L.eruption(98)` passe.
- [ ] **Step 2 :** `pytest tools/tests/test_levels.py -v` → FAIL.
- [ ] **Step 3 : Implémenter** les deux méthodes (docstrings en français, comme `lightning`). Le simulateur (`engine.py`) n'en tient pas compte : les ajouter à sa liste d'événements visuels s'il en a une, sinon rien.
- [ ] **Step 4 :** `pytest tools/tests -v` → PASS.
- [ ] **Step 5 : Commit** — `git commit -m "Ajoute les événements de chart weather et eruption"` (builder.py, test, chart-format.md).

### Task 3 : Sprites de météo et câblage dans Unity

**Files:**
- Modify: `tools/kenta_art.py` (`draw_weather(kind)`), `tools/gen-kenta-assets.py` (`Gen/world/weather_rain.png` 1×5, `weather_snow_0/1.png` 1×1 et 2×2, `weather_ember.png` 2×2, `eruption_blob.png` 4×4, `eruption_smoke.png` 12×8)
- Modify: `game/Assets/_Project/Scripts/Level/BiomeTheme.cs` (champ `public Vector2[] vents;`, tooltip : px de l'image 1280×360 du calque lointain, y depuis le haut)
- Modify: `game/Assets/_Project/Editor/KentaBuilder.cs:215-260` (lire `vents.json` comme `suns.json` → `b.vents` ; ajouter `WeatherFx` sous le décor et lui donner les 6 sprites)
- Test: `tools/tests/test_forge_backdrop.py` (`test_weather_sprites_sizes`)

**Interfaces:**
- Consumes: `vents.json` (Task 1).
- Produces: `BiomeTheme.vents` ; sprites `Gen/world/weather_*.png`, `eruption_*.png` ; un `WeatherFx` dans la scène, référencé par `BiomeBackdrop` (champ `[SerializeField] WeatherFx weather`).

- [ ] **Step 1 : Test** `test_weather_sprites_sizes` (tailles ci-dessus, pixels opaques présents).
- [ ] **Step 2 :** pytest → FAIL ; **Step 3 :** dessiner (gris clair bleuté pour pluie et neige, `#FFA040`/`#FF7A1A` pour braise et lave, `#5A3A30` pour la fumée) ; pytest → PASS.
- [ ] **Step 4 :** générer (`--only world` ou la section concernée), écrire les champs C#, puis via MCP : `refresh_unity`, `read_console` (0 erreur), menu **Kenta > Reconstruire thèmes, prefabs et scène**. Vérifier `Biome_04_forge.asset` : `vents` non vide.
- [ ] **Step 5 : Commit** — PNG + `.meta` créés par Unity, scripts, `Main.unity`, assets de biome : `git commit -m "Dessine les sprites de météo et branche les cratères de la Forge"`.

### Task 4 : `WeatherFx` (pluie, neige, braises)

**Files:**
- Create: `game/Assets/_Project/Scripts/FX/WeatherMath.cs` (calculs purs), `game/Assets/_Project/Scripts/FX/WeatherFx.cs`
- Modify: `game/Assets/_Project/Scripts/Level/LevelBuilder.cs:445-465` (`case "weather"` → `Weather.Add(...)`), `game/Assets/_Project/Scripts/Core/RunManager.cs:253,436` (passer / retirer les courbes)
- Test: `game/Assets/_Project/Editor/Tests/WeatherTests.cs`

**Interfaces:**
- Consumes: `CorruptionCurve.Add(double beat, float target, float length)`, `CorruptionCurve.Evaluate(double beat)`.
- Produces:
  - `public class WeatherTracks { public readonly CorruptionCurve Rain, Snow, Embers; public void Clear(); public CorruptionCurve For(string kind); }` (dans `WeatherMath.cs`) ; `LevelBuilder.Weather` (`WeatherTracks`, vidé à chaque build).
  - `public static int WeatherMath.Ticks(double prevBeat, double beat, double step, int max)` : nombre de multiples de `step` franchis dans `(prevBeat, beat]`, plafonné à `max` (0 si `beat <= prevBeat`).
  - `public static int WeatherMath.SpawnCount(float intensity, int perTickAtFull)` = `round(intensity × perTickAtFull)`.
  - `public static int WeatherMath.NextSlot(int next, int capacity)` = `(next + 1) % capacity` (recyclage du plus ancien).
  - `public static float WeatherFx.IntensityOf(WeatherTracks tracks, string kind, double beat)` : 0 si `tracks` est null.
  - `public void WeatherFx.SetTracks(WeatherTracks tracks)` (null = tout s'éteint, particules cachées) ; `public void WeatherFx.Spawn(Sprite s, Vector2 pos, Vector2 vel, float life, Color c)` (sert aussi à l'éruption, Task 5) ; `public Color WeatherFx.SkyTint` (multiplicateur de ciel : pluie assombrit jusqu'à ×0,7, neige bleuit jusqu'à (0,9 ; 0,95 ; 1)).
- Réglages pinés : pas de la pluie `0.25` temps, 6 gouttes par pas à pleine intensité ; neige : un flocon par croche × intensité, rafale de 12 sur chaque 1 de mesure (`beatsPerBar` de la chart) ; braises : 5 sur les temps 1 et 3. Plafond `Ticks(..., max: 2)`.

- [ ] **Step 1 : Tests** (`WeatherTests.cs`)
  - `Ticks_CountsSixteenths` : `Ticks(0.0, 1.0, 0.25, 99) == 4` ; `Ticks(0.1, 0.2, 0.25, 99) == 0`.
  - `Ticks_CapsAfterSeek` : `Ticks(10, 200, 0.25, 2) == 2` ; `Ticks(50, 10, 0.25, 2) == 0` (reprise en arrière).
  - `WeatherTracks_FromChart` : chart JSON minimale avec `weather rain 0.5 length 0` au temps 8 → `Rain.Evaluate(7.9) == 0`, `Rain.Evaluate(8.1) == 0.5`, `Snow.Evaluate(9) == 0`.
  - `WeatherTracks_Null_IsZero` : `WeatherFx.IntensityOf(null, "rain", 10) == 0` (statique).
  - `Pool_Recycles_Oldest` : `WeatherMath.NextSlot(int next, int capacity)` renvoie `(next + 1) % capacity` ; avec capacité 120, 121 appels reviennent au slot 0.
- [ ] **Step 2 :** MCP `run_tests` (EditMode, filtre `WeatherTests`) → FAIL (types absents).
- [ ] **Step 3 : Implémenter** `WeatherMath`, `WeatherTracks`, `WeatherFx` (pool créé dans `Awake`, `Update` lit `Conductor.Instance.SongBeat`, garde `prevBeat`, appelle `Ticks` par effet, fait apparaître au-dessus de la vue (pluie, neige) ou au bas (braises), déplace en continu, `PixelScreen.Snap`). `BiomeBackdrop` multiplie la teinte du ciel par `weather.SkyTint`. Garde-fou « scripts rechargés » comme `FxPool`.
- [ ] **Step 4 :** `run_tests` EditMode complet → PASS ; `read_console` → 0 erreur.
- [ ] **Step 5 : Commit** — `git commit -m "Ajoute la pluie, la neige et les braises du décor (WeatherFx)"`.

### Task 5 : Éruption (`BiomeBackdrop.Erupt`)

**Files:**
- Modify: `game/Assets/_Project/Scripts/FX/BiomeBackdrop.cs` (près de `Strike`), `game/Assets/_Project/Scripts/Level/LevelBuilder.cs` (`case "eruption"` avec `lightning` → `FxEvents`), `game/Assets/_Project/Scripts/Core/RunManager.cs:772` (`case "eruption"`)
- Test: `game/Assets/_Project/Editor/Tests/WeatherTests.cs` (ajouts)

**Interfaces:**
- Consumes: `WeatherFx.Spawn` (Task 4), `BiomeTheme.vents` (Task 3).
- Produces: `public void BiomeBackdrop.Erupt(float intensity, float beat)` ; `public static float BiomeBackdrop.EruptionGlow(float t, float intensity, bool hasVents)` : 0 si `!hasVents` ; sinon monte à `0.35 × intensity` puis redescend en 1 s, **par paliers de 0,25 × max**.

- [ ] **Step 1 : Tests**
  - `EruptionGlow_NoVents_Zero` : `EruptionGlow(0.2f, 1f, false) == 0`.
  - `EruptionGlow_Stepped` : pour t de 0 à 1,2 par 0,01, chaque valeur est un multiple de `0.35/4` (± 1e-4), max ≤ 0,35, valeur à t = 1,1 égale à 0.
- [ ] **Step 2 :** `run_tests` → FAIL.
- [ ] **Step 3 : Implémenter** `Erupt` : choisit le cratère `vents[seed % n]` (seed tiré du beat, comme `Strike`), le convertit en position monde via la copie du calque lointain à l'écran (même calcul que son placement dans `LateUpdate`, `k / (float)PixelScreen.Ppu`), lance ~14 `eruption_blob` vers le haut (gravité, retombée) et 4 `eruption_smoke` lents, applique `EruptionGlow` en rouge (`#D8401A`) sur ciel et calque lointain. Dans `RunManager` : `backdrop.Erupt(intensity, e.beat)` puis `rig.Shake(0.1f * intensity, 0.25f)` ; **pas** de `rig.Flash`.
- [ ] **Step 4 :** `run_tests` → PASS, console 0 erreur.
- [ ] **Step 5 : Commit** — `git commit -m "Fait jaillir la lave du volcan de la Forge sur l'événement eruption"`.

### Task 6 : Mise en scène des niveaux 2, 4, 6, test en jeu, docs

**Files:**
- Modify: `tools/levels/n2_bayou.py`, `tools/levels/n4_forge.py`, `tools/levels/n6_givre.py` (appels dans la fonction qui assemble les scènes, à côté des `section_change`)
- Regenerate: leurs charts (`python3 tools/gen-level.py n2_bayou`, `n4_forge`, `n6_givre`)
- Modify: `game/Assets/_Project/Editor/KentaDevMenu.cs` (menus **Kenta > Tester la pluie du Bayou (niveau 2)**, **… la neige du Givre (niveau 6)**, **… les éruptions de la Forge (niveau 4)**, sur le modèle des menus de l'ADR-041)
- Modify: `docs/decisions.md` (**ADR-047**), `docs/roadmap.md`, `game/CLAUDE.md` (ligne `FX/WeatherFx`), `CLAUDE.md` racine si une commande change

- [ ] **Step 1 : Poser les événements** (valeurs de la spec) :
  - Bayou : `weather(128,"rain",0.35,8)`, `weather(176,"rain",1.0,4)`, `weather(192,"rain",0,16)`.
  - Givre : `weather(0,"snow",0.3,0)`, `weather(80,"snow",0.7,4)`, `weather(112,"snow",0.1,4)`, `weather(144,"snow",0.4,4)`, `weather(208,"snow",0.7,4)`, `weather(240,"snow",1.0,2)`, `weather(272,"snow",0.3,8)`.
  - Forge : `weather(0,"embers",0.4,0)`, `0.8` sur chaque refrain (96, 156, 216) et `0.4` après ; `eruption` à 96, 156, 216, puis tous les 8 temps dans le `final` (228 → fin).
- [ ] **Step 2 :** générer les 3 charts → le générateur passe (aucune `ValueError`), `pytest tools/tests -v` → PASS.
- [ ] **Step 3 : En jeu** (MCP) : chaque menu **Tester …** en autoplay, **sans aucun appel MCP pendant le morceau** (piège du CLAUDE.md). À la fin : 0 chute, console 0 erreur. Ensuite, une capture hors autoplay (`ScreenCapture.CaptureScreenshot`) par effet en vue 480, montrée à Lou.
- [ ] **Step 4 : Perfs** : build Web (**Kenta > Build Web (borne)**), `tools/package-build.sh`, niveau 6 pendant l'`avalanche` avec l'overlay F9 : noter les fps dans l'ADR. S'ils tombent sous 60, baisser les densités (pas le pool).
- [ ] **Step 5 : Docs** : ADR-047 daté (approche C, météo derrière le niveau, 120 particules, éruption sans flash, Forge redessinée par code, vents.json) ; roadmap : cocher « États du thème » pour Bayou/Givre/Forge, ajouter **P2 lac de lave qui reflète le héros (Forge)** et **P2 météo des autres biomes** ; `game/CLAUDE.md` : `FX/WeatherFx` dans le tableau FX ; `chart-format.md` déjà fait (Task 2).
- [ ] **Step 6 : Commit** — `git commit -m "Met en scène la pluie, la neige et les éruptions des niveaux 2, 4 et 6"`.
