# Écran de sélection de niveau — plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal :** un écran dédié « Choisis ton niveau » (liste façon osu!, cartes avec vignette) entre le choix du héros et la partie, un `SongData` par niveau, un thème par niveau, et un classement par niveau stocké dans l'Extradata de la borne.

**Architecture :** comme les 7 autres écrans, le prefab `Screen_LevelSelect` est généré par `KentaBuilder` ; ses cartes sont créées à l'exécution depuis `LevelCatalog`. `GameFlow` gagne l'état `LevelSelect`. Les classements par niveau passent par une classe statique `LevelScores` (mémoire + `ExtradataManager`). Le calcul pur (liste, répétition du joystick, classement) est isolé dans de petites classes testées en EditMode.

**Tech Stack :** Unity 6000.3.23f1 (C#, uGUI, NUnit EditMode, MCP Unity), Python 3 (Pillow, numpy, pytest).

**Spec :** [docs/superpowers/specs/2026-11-25-selection-niveau-design.md](../specs/2026-11-25-selection-niveau-design.md)

## Global Constraints
- Tout en français (docs, commits à l'impératif terminés par `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`, textes affichés) ; identifiants en anglais ; namespaces `Kenta.<Dossier>`, tests `Kenta.EditorTests` dans `game/Assets/_Project/Editor/Tests/`.
- Jamais d'édition à la main de `.unity`, `.prefab`, `.asset`, `.meta` : code éditeur (`KentaBuilder`) lancé via le MCP Unity (`execute_menu_item`), ou `manage_asset`.
- Toolkit `game/Assets/Anatidae/**` en lecture seule.
- Entrées : `ArcadeInput` uniquement (`Stick`, `ConfirmDown`, `HitDown`), jamais `Input.*`.
- Pixel art : interface 320×180, coordonnées UI entières (repère centré : x ∈ [−160, 160], y ∈ [−90, 90]), sprites à l'échelle 1, aucune échelle fractionnaire.
- La police pixel n'a que des majuscules (accents compris) : tout texte affiché passe par `ToUpperInvariant()`.
- Après toute reconstruction : `git restore "game/Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset"` si modifié ; `Main.unity` ne doit pas gagner d'objet `Bandana`.
- Valeurs de la spec : 15 s pour choisir ; garde de 0,3 s ; répétition du joystick après 0,4 s puis toutes les 0,15 s ; extrait après 0,25 s de sélection stable ; `previewBeat` 96 ; vignettes 40×24 ; cartes 28 px (sélection) / 26 px, pas vertical 29 px, décalage 6 px par rang ; top 5 dans le panneau, top 10 au classement ; 10 entrées max, une par nom ; clé Extradata `top_<id>`.

## Review Focus
1. **Borne absente** (éditeur sans `localhost:3000`) : aucune erreur nouvelle en console, ni au démarrage ni en fin de partie ; le classement du niveau marche en mémoire → Task 3 (`Record` sans réseau) et vérification console de la Task 8.
2. **Valeur Extradata vide, `{}`, tronquée ou d'une autre version** : liste vide, aucune exception → test `FromJson_GarbageGivesEmpty` (Task 3).
3. **Défilement rapide ou joystick maintenu** : la liste suit sans sauter de pixel, l'extrait ne repart qu'une fois la sélection stable → tests `StickRepeat` (Task 4), vérification en jeu (Task 8).
4. **Nom long** (« LA CITÉ SOURDE », « LE GRAND ORAGE ») dans le panneau de 118 px et dans le titre du classement : repli à l'échelle 1 → test `AllLevelNamesFitInfoPanel` (Task 5).
5. **Appui qui a validé le héros** : il ne lance pas la partie, et B2 dès l'entrée ramène bien au héros avec le même héros présélectionné → garde 0,3 s (Task 5), vérification en jeu (Task 8).

---

### Task 1 : un thème par niveau (chart et générateur)

**Files :**
- Modify : `tools/gen-level-vertical.py:48-49` (`BIOMES`)
- Modify : `game/Assets/_Project/Charts/level01_shovel_knight.json` (retirer les 3 champs `"biome"`, rien d'autre)
- Test : `tools/tests/test_level_vertical.py` (remplacer `test_world_changes_mid_level`)

**Interfaces :** Produces : chart sans `biome` par section ; `BIOMES = {}` (le mécanisme `ChartSection.biome` reste en C#).

- [ ] **Step 1 : remplacer le test**

```python
def test_level_keeps_one_world():
    """Un niveau = un thème : aucune section ne change de monde (le décor suit le LevelDef)."""
    assert gen.BIOMES == {}
    with open(gen.DEFAULT_OUT, encoding="utf-8") as f:
        chart = json.load(f)
    assert all("biome" not in s for s in chart["sections"])
```
(ajouter `import json` en tête si absent.)

- [ ] **Step 2 :** `pytest tools/tests/test_level_vertical.py::test_level_keeps_one_world -v` → FAIL.
- [ ] **Step 3 :** `BIOMES = {}` (garder le commentaire, le compléter : « vide : un thème par niveau, ADR-030 ») ; retirer `, "biome": "…"` des sections `refrain`, `final`, `refrain 2` du JSON (lignes 15, 16, 20). **Ne pas régénérer la chart.**
- [ ] **Step 4 :** `pytest tools/tests -v` → tout passe sauf `test_generated_chart_is_feasible_and_vertical` (déjà en échec sur `main`).
- [ ] **Step 5 : commit** `Garde un seul thème par niveau : retire les changements de monde de la chart`.

### Task 2 : vignettes des niveaux et étoile

**Files :**
- Create : `tools/kenta_thumbs.py`
- Modify : `tools/gen-kenta-assets.py` (générateur `thumbs`), `tools/kenta_ui.py` (`draw_star`)
- Create (générés) : `game/Assets/_Project/Art/Kenta/Gen/ui/thumb_<id>.png` ×8, `Gen/ui/star.png`
- Test : `tools/tests/test_thumbs.py`

**Interfaces :**
- Produces : `kenta_thumbs.make_thumbnail(layers: list[PIL.Image.Image], size=(40, 24), window=(0, 0, 600, 360)) -> PIL.Image.Image` ; `kenta_ui.draw_star() -> Pix` (5×5, blanc, motif `..#.. / .###. / ##### / .###. / .#.#.`) ; sprites `Gen/ui/thumb_<id>.png` et `Gen/ui/star.png` (importés en sprites d'interface par `KentaSpritePostprocessor`).

- [ ] **Step 1 : tests**

```python
def test_thumbnail_size_and_opaque():          # 40×24, alpha 255 partout
def test_thumbnail_uses_only_source_colors():  # chaque couleur de la vignette existe dans les calques composés (fenêtre)
def test_thumbnail_is_deterministic():         # deux appels → mêmes octets
def test_star_shape():                         # 5×5, 13 pixels opaques
```
Les tests composent des calques synthétiques (2 ou 3 images RGBA 640×360 à aplats) : pas de dépendance aux PNG installés.

- [ ] **Step 2 :** `pytest tools/tests/test_thumbs.py -v` → FAIL (module absent).
- [ ] **Step 3 : implémenter.** `make_thumbnail` : compose les calques dans l'ordre (ciel, lointain, milieu, proche), recadre `window` (600×360 = rapport 5:3), réduit par moyenne de blocs (15×15 px → 1 px), puis remplace chaque pixel par la couleur **la plus proche (distance RVB) parmi les couleurs opaques de la fenêtre composée** : aucune couleur nouvelle. `gen_thumbs(out, sheet)` lit `out/biomes/bg_<id>_{sky,far,mid,near}.png` (les calques `far/mid/near` font 1280 px : prendre x 0…640), écrit `ui/thumb_<id>.png` pour chaque biome de `kenta_biomes.BIOMES`, et `ui/star.png`. Ajouter `"thumbs": gen_thumbs` à `GENERATORS` (sans toucher aux autres générateurs).
- [ ] **Step 4 :** `pytest tools/tests -v` (même attendu que Task 1) ; `python3 tools/gen-kenta-assets.py --only thumbs` → 9 fichiers ; `git status` ne montre que ces 9 PNG en plus des sources.
- [ ] **Step 5 : import Unity** via MCP `refresh_unity`, puis `read_console` : aucune erreur ; les `.meta` sont créés par Unity.
- [ ] **Step 6 : commit** `Génère les vignettes des niveaux et l'étoile de difficulté` (PNG + `.meta` + scripts + test).

### Task 3 : classements par niveau (`LevelScores`)

**Files :**
- Create : `game/Assets/_Project/Scripts/Core/LevelScores.cs`
- Test : `game/Assets/_Project/Editor/Tests/LevelScoresTests.cs`

**Interfaces :**
- Produces (namespace `Kenta.Core`) :
  - `[Serializable] public struct LevelScoreEntry { public string name; public int score; }`
  - `public static class LevelScores` :
    - `public const int Max = 10;` `public const string KeyPrefix = "top_";`
    - `public static bool Online { get; }` — vrai après une lecture réussie de l'Extradata
    - `public static int Version { get; }` — incrémenté à chaque changement (pour rafraîchir l'affichage)
    - `public static IReadOnlyList<LevelScoreEntry> Top(string levelId)` — jamais null
    - `public static bool IsTopTen(string levelId, int score)` — `score > 0` et (moins de `Max` entrées, ou `score >` la dernière)
    - `public static void Record(string levelId, string name, int score)` — en mémoire seulement
    - `public static IEnumerator Upload(string levelId)` — `ExtradataManager.SetExtraData(KeyPrefix + levelId, ToJson(...))` ; ne fait rien si `!Online`
    - `public static IEnumerator FetchFromBorne()` — `yield return ExtradataManager.FetchExtraData()` ; si `HasFetchedExtraData` : charge toutes les clés `top_*`, `Online = true`
    - Purs (testés) : `public static List<LevelScoreEntry> Insert(List<LevelScoreEntry> list, string name, int score)` (nouvelle liste : tri décroissant, une entrée par nom en gardant la meilleure, `Max` au plus ; nom vide → `"---"`), `public static string ToJson(List<LevelScoreEntry> list)` (enveloppe `{"entries":[…]}`), `public static List<LevelScoreEntry> FromJson(string json)` (null, vide, illisible ou sans `entries` → liste vide), `public static void ResetForTests()`.

- [ ] **Step 1 : tests** (`LevelScoresTests`, `[SetUp] LevelScores.ResetForTests()`) :
  - `Insert_SortsDescending` (3 scores dans le désordre → ordre décroissant)
  - `Insert_KeepsTen` (12 insertions → 10, la plus faible écartée)
  - `Insert_OneEntryPerName_KeepsBest` (LOU 100 puis LOU 50 → une entrée 100 ; puis LOU 200 → une entrée 200)
  - `IsTopTen_EmptyBoard` (`IsTopTen("sylve", 1)` vrai, `IsTopTen("sylve", 0)` faux)
  - `IsTopTen_FullBoard` (10 entrées de 100 à 1000 : 100 → faux, 101 → vrai)
  - `Json_RoundTrip` (`FromJson(ToJson(l))` égale `l`)
  - `FromJson_GarbageGivesEmpty` (`null`, `""`, `"{}"`, `"[1,2"`, `"{\"entries\":null}"` → `Count == 0`, sans exception)
  - `Record_IsPerLevel` (Record sylve n'apparaît pas dans `Top("bayou")`) et `Record_BumpsVersion`
- [ ] **Step 2 :** MCP `run_tests` (EditMode, filtre `LevelScoresTests`) → échec de compilation / FAIL.
- [ ] **Step 3 : implémenter** (cache `Dictionary<string, List<LevelScoreEntry>>` ; `JsonUtility` avec une classe enveloppe privée `[Serializable] class Wrapper { public List<LevelScoreEntry> entries; }`).
- [ ] **Step 4 :** `run_tests` EditMode complet → vert ; `read_console` → vide.
- [ ] **Step 5 : commit** `Ajoute les classements par niveau (Extradata de la borne, repli en mémoire)`.

### Task 4 : calculs de la liste et du joystick

**Files :**
- Create : `game/Assets/_Project/Scripts/UI/LevelListLayout.cs`, `game/Assets/_Project/Scripts/UI/StickRepeat.cs`
- Test : `game/Assets/_Project/Editor/Tests/LevelListTests.cs`

**Interfaces (namespace `Kenta.UI`) :**
- `public static class LevelListLayout` :
  - `public const int Spacing = 29, SelectedHeight = 28, Height = 26, SelectedLeft = -18, Shift = 6, Right = 156;`
  - `public static int Wrap(int index, int count)` → `[0, count)`
  - `public static float Offset(int index, float scroll, int count)` → écart circulaire le plus court, dans `[-count/2, count/2)`
  - `public static RectInt CardRect(float offset)` → `x` = bord gauche = `SelectedLeft + RoundToInt(Shift * |offset|)`, `y` = centre = `RoundToInt(-offset * Spacing)`, `width = Right - x`, `height = |offset| < 0.5 ? SelectedHeight : Height` (pivot gauche-milieu de la carte)
  - `public static string FormatInfo(float bpm, float seconds)` → `"150 BPM  1:43"` (BPM arrondi, durée m:ss tronquée)
- `public class StickRepeat` : `public StickRepeat(float delay = 0.4f, float interval = 0.15f)` ; `public int Step(float axis, float dt)` → −1, 0 ou +1 (seuil 0,5) : un pas à la poussée, puis un pas après `delay`, puis tous les `interval` tant qu'on maintient ; 0 au retour au centre (réarme).

- [ ] **Step 1 : tests** — `Wrap(-1, 8) == 7`, `Wrap(8, 8) == 0` ; `Offset(7, 0, 8) == -1`, `Offset(1, 0, 8) == 1`, `Offset(4, 0, 8) == -4`, `Offset(0, 7.5f, 8) == 0.5f` ; `CardRect(0) == new RectInt(-18, 0, 174, 28)`, `CardRect(1) == new RectInt(-12, -29, 168, 26)`, `CardRect(-2).y == 58` ; `FormatInfo(150, 103.4f) == "150 BPM  1:43"`, `FormatInfo(120, 59.9f) == "120 BPM  0:59"` ; `StickRepeat` : poussée à 1 → `+1` à la 1re image, 0 jusqu'à 0,4 s, puis `+1` toutes les 0,15 s ; relâché puis repoussé → `+1` immédiat ; `axis = -1` → `-1` ; `axis = 0.3` → 0.
- [ ] **Step 2 :** `run_tests` (filtre `LevelListTests`) → FAIL.
- [ ] **Step 3 : implémenter.**
- [ ] **Step 4 :** `run_tests` EditMode → vert ; console vide.
- [ ] **Step 5 : commit** `Ajoute le calcul de la liste des niveaux et la répétition du joystick`.

### Task 5 : composants de l'écran et du classement

**Files :**
- Modify : `Scripts/Rhythm/SongData.cs` (`[Min(0f)] public float previewBeat;` + tooltip « Début de l'extrait joué à l'écran de sélection (beat) »)
- Modify : `Scripts/Level/LevelDef.cs` (`public Sprite thumbnail;` ; retirer la phrase « pour l'instant… » du résumé au profit de « provisoirement le même morceau, ADR-030 »)
- Create : `Scripts/UI/LevelCard.cs`, `Scripts/UI/LevelSelectScreen.cs`
- Modify : `Scripts/UI/HighscoreBoard.cs`
- Test : `Editor/Tests/LevelSelectTests.cs`

**Interfaces :**
- Consumes : `LevelListLayout`, `StickRepeat` (Task 4) ; `LevelScores.Top`, `LevelScores.Version` (Task 3) ; `GameSession`, `Conductor.Instance.Play(SongData, double startBeat, bool loop)`.
- Produces :
  - `LevelCard : MonoBehaviour` — champs `Image frame, fill, thumb; PixelText title, song; Image[] stars;` ; `public void Show(LevelDef level, bool selected)` (titre `"{number} {Label}"`, morceau `song.title`, vignette, étoiles or / `(70, 64, 100)`, cadre or si sélection sinon `(80, 74, 120)`, textes blanc / atténués) ; `public void Place(RectInt r)` (taille et position entières ; `fill` inséré d'1 px dans `frame`).
  - `LevelSelectScreen : MonoBehaviour` — champs sérialisés `RectTransform listRoot; LevelCard cardTemplate; PixelText nameText, songText, infoText, timerText; Image[] stars; PixelText[] boardNames, boardScores; PixelFontData font; float seconds = 15f, guard = 0.3f, previewDelay = 0.25f, scrollSharpness = 18f; int nameMaxWidth = 118;`
    - `public void Setup(LevelCatalog catalog, int startIndex)` (avant activation)
    - `public bool Done { get; }`, `public bool Back { get; }`, `public int SelectedIndex { get; }`, `public LevelDef Selected { get; }`
    - `public event Action<LevelDef> OnLevelChanged`
    - `public static int NameScale(PixelFontData font, string label, int maxWidth)` → 2 si `PixelText.MeasureWidth(font, label, 2) <= maxWidth`, sinon 1
    - Comportement : `OnEnable` crée les cartes manquantes (copies de `cardTemplate`, inactif dans le prefab), remet le minuteur, `Done = Back = false`, lit le joystick courant pour ne pas compter une poussée déjà en cours ; `Update` : minuteur → `timerText` `D2` ; `StickRepeat.Step(ArcadeInput.Stick(1).y, dt)` : `+1` (haut) = niveau précédent ; scroll suit le curseur (`1 - exp(-dt * scrollSharpness)`) ; `Place` + `Show` de chaque carte, masquée si `|y| > 104` ; panneau mis à jour au changement (nom en `NameScale`, `song.title`, `FormatInfo(song.bpm, song.clip.length)`, étoiles, top 5 de `LevelScores.Top(level.id)` avec `"---"` aux places vides, 1re place en or, rafraîchi si `LevelScores.Version` change) ; extrait : quand la sélection n'a pas bougé depuis `previewDelay` et que `Conductor.Instance.Song != level.song`, `Play(level.song, level.song.previewBeat, loop: true)` ; après `guard` : `ConfirmDown(1)` → `GameSession.Level/LevelIndex` remplis, `Done = true` ; `HitDown(1)` → `Back = true` ; minuteur à 0 → comme `ConfirmDown`.
  - `HighscoreBoard` : nouveaux champs `PixelText title; PixelFontData font;` ; `public void ShowOfficial()` (titre « MEILLEURS SCORES », données `HighscoreManager.Highscores`) ; `public void ShowLevel(LevelDef level)` (titre `"CLASSEMENT : " + level.Label`, échelle 2 si `MeasureWidth(font, titre, 2) <= 304` sinon 1, données `LevelScores.Top(level.id)`) ; `Refresh` lit une liste `(name, score)` commune ; `Update` rafraîchit aussi quand `LevelScores.Version` change ; défaut = officiel. `Highlight`/`ClearHighlight` inchangés.

- [ ] **Step 1 : tests** (`LevelSelectTests`, assets chargés par `AssetDatabase`) :
  - `AllLevelNamesFitInfoPanel` : pour chaque niveau de `LevelCatalog.asset`, `PixelText.MeasureWidth(font, Label, NameScale(font, Label, 118)) <= 118`
  - `NameScale_ShortNameIsBig` : `NameScale(font, "LA SYLVE", 118) == 2`
- [ ] **Step 2 :** `run_tests` → FAIL (types absents).
- [ ] **Step 3 : implémenter** les 4 fichiers + les 2 champs de données.
- [ ] **Step 4 :** `run_tests` EditMode → vert ; `read_console` → aucune erreur ni avertissement de compilation.
- [ ] **Step 5 : commit** `Ajoute l'écran de sélection de niveau et le classement par niveau à l'écran des scores`.

### Task 6 : flow (`GameFlow`, écran du héros)

**Files :**
- Modify : `Scripts/Core/GameFlow.cs`, `Scripts/UI/HeroSelectScreen.cs`

**Interfaces :**
- Consumes : `LevelSelectScreen` (Task 5), `HighscoreBoard.ShowOfficial/ShowLevel` (Task 5), `LevelScores` (Task 3).
- Produces :
  - `GameState { Attract, Explain, Config, LevelSelect, Playing, GameOver, NameEntry }`
  - `GameFlow` : champs `[SerializeField] GameObject levelSelectScreen; [SerializeField] LevelSelectScreen levelSelect;` (noms exacts : le builder de la Task 7 les remplit)
  - `HeroSelectScreen.Setup(int startIndex)` remplace `Setup(LevelCatalog)` ; retrait de `worldText`, `level`, `MoveLevel`, `OnLevelChanged`, `catalog`.

- [ ] **Step 1 : `HeroSelectScreen`** — `OnEnable` part de l'index passé à `Setup` (borné) ; seul gauche/droite déplace ; le reste inchangé (12 s, garde 0,3 s, `GameSession.HeroIndex/Hero` à la validation).
- [ ] **Step 2 : `GameFlow`** :
  - `Start` : `StartCoroutine(FetchScores())` — mémorise `HighscoreManager.Highscores`, `yield return HighscoreManager.FetchHighscores()`, et seulement si la liste a été **remplacée** et `HasFetchedHighscores` : `yield return LevelScores.FetchFromBorne()` (sans borne : aucune requête Extradata). S'abonne à `levelSelect.OnLevelChanged += ShowWorld` (retirer l'abonnement à `heroSelect`).
  - Champs privés : `int pendingLevel` ; `bool backToHero` ; `LevelDef boardLevel` (classement montré à l'accueil).
  - `Enter(Config)` : `heroSelect.Setup(backToHero || GameSession.IsQuickRetry ? GameSession.HeroIndex : 0)` avant activation ; `backToHero = false` ensuite. En venant d'Explain ou d'Attract : `pendingLevel = GameSession.IsQuickRetry ? GameSession.LevelIndex : 0`.
  - `Enter(LevelSelect)` : `levelSelect.Setup(catalog, pendingLevel)` avant activation ; `run?.StopRun(hideWorld: true, stopMusic: false)` ; `ShowMenuWorld(scroll: true)` ; `menuLooping = false` ; volume `attractVolume`. Activer `levelSelectScreen` seulement dans cet état.
  - `Update` : `Config` → `heroSelect.Done` ⇒ `Enter(LevelSelect)` ; `LevelSelect` → `levelSelect.Back` ⇒ `pendingLevel = levelSelect.SelectedIndex; backToHero = true; Enter(Config)` ; `levelSelect.Done` ⇒ `Enter(Playing)`.
  - Phase démo : `boardLevel = level` (le niveau joué en démo). Phase classement de l'accueil et 2e panneau de fin : `highscoreBoard.ShowLevel(...)` avec `boardLevel` (accueil) ou le niveau joué (fin), `FirstLevel()` en repli ; `ShowOfficial()` si aucun niveau.
  - Fin : `IsTopTen(score)` devient `(officiel comme avant) || LevelScores.IsTopTen(PlayedLevel.id, score)`. `FinishNameEntry` : `LevelScores.Record(id, nom, score)`, puis `if (LevelScores.Online) StartCoroutine(LevelScores.Upload(id))`, `SetHighscore` inchangé, `boardLevel = PlayedLevel`, highlight. Si le score n'entre nulle part : rien n'est enregistré.
  - `PlayedLevel` = `GameSession.Level ?? FirstLevel()`.
  - Docstrings de `GameFlow`/`GameState` mises à jour (Héros → Niveau).
- [ ] **Step 3 :** `read_console` → compilation propre ; `run_tests` EditMode → vert.
- [ ] **Step 4 : commit** `Insère le choix du niveau entre le héros et la partie`.

### Task 7 : construction de l'écran et câblage de la scène

**Files :**
- Modify : `game/Assets/_Project/Editor/KentaBuilder.cs`
- Generated (via Unity) : `Prefabs/Screens/Screen_LevelSelect.prefab`, `Screen_HeroSelect.prefab`, `Screen_Highscores.prefab`, `ScriptableObjects/Songs/Song_0N_<id>.asset` ×8, `ScriptableObjects/Levels/*.asset`, `Scenes/Main.unity`

**Interfaces :**
- Consumes : champs sérialisés de la Task 5 et de la Task 6 (noms exacts ci-dessus).
- Produces : menu `Kenta/Construire l'écran de niveaux` → `public static string BuildLevelSelectOnly()` ; `BuildAll` inclut l'écran.

- [ ] **Step 1 : `BuildLevels`** — pour chaque biome : `Song_{i+1:00}_{id}.asset` créé **seulement s'il n'existe pas**, copie de `Song_Level01` (clip, remixClip, bpm, offsetSeconds, beatsPerBar, chart), `title = "MAIN THEME"`, `previewBeat = 96` ; `LevelDef.song` assigné **seulement s'il est vide ou égal à `Song_Level01`** ; `LevelDef.thumbnail` assigné seulement s'il est vide (`Gen/ui/thumb_<id>.png`) ; `stars` inchangé.
- [ ] **Step 2 : `BuildHeroSelect`** — sans le texte du monde ; aide « ◄ ► HÉROS   1 VALIDER ».
- [ ] **Step 3 : `BuildHighscores`** — `Set(comp, "title", <le texte Title>)`, `Set(comp, "font", font)`.
- [ ] **Step 4 : `BuildLevelSelect(PixelFontData font, LevelTheme theme)`** → `Screen_LevelSelect` selon la maquette (`docs/img/selection-niveau-maquette.png`) : voile plein écran `pixel.png` `(20,16,28)` alpha 90/255 ; bandeau haut 320×16 (alpha 230/255) avec « CHOISIS TON NIVEAU » or à gauche (x −154) et minuteur à droite ; panneau `panel.png` découpé 134×142 à gauche (centre −87, −1) avec nom, morceau, infos, 5 étoiles (`star.png`), trait, « CLASSEMENT DU NIVEAU », 5 lignes rang+nom à gauche / score à droite ; `ListRoot` (RectTransform centré) avec `CardTemplate` inactif (`LevelCard` : `frame` et `fill` en `pixel.png`, `thumb` 40×24 à 2 px du bord gauche, `title` et `song` alignés à gauche à x +46, 5 étoiles à droite) ; bandeau bas 320×14 avec « ▲▼ NIVEAU   1 JOUER   2 RETOUR ». Câbler tous les champs de `LevelSelectScreen`.
- [ ] **Step 5 : câblage commun** — extraire `static void WireScreens(GameFlow flow, GameObject hero, GameObject levelSelect, GameObject board)` (champs `configScreen`, `heroSelect`, `levelSelectScreen`, `levelSelect`, `highscoreBoard`) ; `BuildScene` l'appelle (le prefab de niveau ajouté **en fin** du tableau `prefabs` pour ne pas décaler les index existants, placé juste après l'écran du héros dans la hiérarchie).
- [ ] **Step 6 : `BuildLevelSelectOnly`** — charge `PixelFont.asset` et `Theme_Kenta.asset` existants (pas de `BuildFont`/`BuildBiomes`), `BuildLevels(theme.biomes)`, reconstruit les 3 prefabs, ouvre `Main.unity` si besoin, et sous l'objet `PixelUI` : remplace les instances `Screen_HeroSelect` et `Screen_Highscores` à leur rang, ajoute `Screen_LevelSelect` juste après le héros, couche UI récursive, `WireScreens`, sauvegarde la scène. Ne touche à rien d'autre (ni `Bandana`, ni caméras).
- [ ] **Step 7 :** MCP `execute_menu_item` « Kenta/Construire l'écran de niveaux » ; `read_console` vide ; `git status` / `git diff --stat` : seuls les fichiers attendus ; `grep -c "m_Name: Bandana$" Main.unity` inchangé (26) ; restaurer `LiberationSans SDF - Fallback.asset` si touché.
- [ ] **Step 8 : commit** `Construit l'écran de sélection de niveau et donne un morceau à chaque niveau`.

### Task 8 : vérification en jeu, docs, PR

**Files :**
- Modify : `docs/game-design.md` (tableau des écrans : 3 Héros, 3 bis Niveau, 1 et 5 classement du niveau ; « Rejouer vite »), `docs/decisions.md` (**ADR-030 — Sélection de niveau, un thème par niveau, classements par niveau dans l'Extradata (2026-11-25)**), `docs/roadmap.md` (cocher « Sélection de niveau » avec la date ; ajouter le bug « `Main.unity` contient 26 objets `Bandana` (reconstructions successives) : à nettoyer » en P2), `game/CLAUDE.md` (flow, `LevelScores`, menu « Construire l'écran de niveaux », vignettes, règle de non-écrasement), `CLAUDE.md` racine (commande `python3 tools/gen-kenta-assets.py --only thumbs` après un changement de décor), `docs/chart-format.md` (le biome par section reste possible mais aucune chart ne l'utilise : un thème par niveau)

- [ ] **Step 1 : vérifier en Play** (MCP, `EditorArcadeShim` désactivé, `Application.runInBackground = true`, `Temp/caps` créé) : `GameFlow.Instance.Enter(GameState.Config)` puis valider → écran des niveaux ; parcourir haut/bas (via `execute_code` en appelant l'état ou en simulant par `GameSession`) ; vérifier : décor qui change, extrait qui démarre au refrain, top 5 « --- » sans borne, B2 → héros présélectionné, validation → partie dans le biome choisi du début à la fin, fin → classement du niveau. `read_console` : aucune erreur nouvelle (sans borne locale).
- [ ] **Step 2 : capture** 1920×1080 via `ScreenCapture.CaptureScreenshot` depuis un `EditorApplication.update`, sur l'écran des niveaux (niveau 3 sélectionné) → `Temp/caps/selection-niveau.png`, montrée à Lou (`SendUserFile`).
- [ ] **Step 3 :** `run_tests` EditMode → vert ; `pytest tools/tests` → vert sauf `test_generated_chart_is_feasible_and_vertical`.
- [ ] **Step 4 : docs** (liste ci-dessus), dans le même commit que les derniers ajustements : `Documente l'écran de sélection de niveau (ADR-030)`.
- [ ] **Step 5 :** `git push -u origin feat/selection-niveau`, PR vers `main` (titre « Écran de sélection de niveau », corps : résumé, captures, tests, ce qui reste à la roadmap ; fin : `🤖 Generated with [Claude Code](https://claude.com/claude-code)`), puis `get_status` / `bind_pr`.
