# Kenta — écran de sélection de niveau (design)

Date : 2026-11-25. Branche : `feat/selection-niveau` (partie de `main` après les PR #20 et #21).
Demande de Lou : « Il faudrait faire maintenant l'écran de sélection de niveau (un thème / musique par niveau) ». « On voulait le faire depuis longtemps, la doc n'était pas à jour. On aimerait une musique par niveau ; pour l'instant, mets la même musique et le même parcours à chaque fois. La prochaine fois, on fera quelque chose d'unique pour chacun. »
Ce document remplace la partie « Sélection du niveau » de [2026-11-04-kenta-polish-niveaux-design.md](2026-11-04-kenta-polish-niveaux-design.md), jamais réalisée (l'ADR-021 avait mis à la place un choix du monde haut/bas sur l'écran du héros).

## Intention
Un niveau = **un thème (biome) + un morceau + une chart**. Le joueur choisit son niveau sur un écran dédié, une liste façon osu!, avant de jouer. Cette PR pose la structure, l'écran et les classements par niveau. Le contenu propre à chaque niveau (un morceau et une chart chacun) viendra dans une PR suivante, **sans code à écrire** : il suffira de changer le clip et la chart dans les assets du niveau.

## Objectifs
1. **Écran dédié « Choisis ton niveau »** entre le choix du héros et la partie (maquette validée : liste à droite, cartes avec vignette).
2. **Un `SongData` par niveau** (`Song_01_sylve` … `Song_08_orage`), qui pointent provisoirement vers le même clip et la même chart.
3. **Un thème par niveau** : plus de changement de monde en cours de partie (les 3 changements de biome de la chart commune sont retirés).
4. **Classement par niveau** (top 10, stocké dans l'Extradata de la borne). Le score part aussi dans la liste officielle de la borne.
5. **Extrait du morceau** du niveau survolé, joué depuis un temps réglable par morceau.

## Non-objectifs
- Une musique et une chart propres à chaque niveau (roadmap : « Contenu unique par niveau »).
- Les **états du thème** au fil du niveau (pluie, nuit, lever du jour) : roadmap, à écrire avec la chart propre à chaque niveau.
- Le choix du score envoyé dans la liste officielle (score brut, cumul…) : « on tranchera plus tard » (Lou). Pour l'instant, score brut de la partie.
- Le mode 2 joueurs, le mode marathon, le verrouillage de niveaux (tout est débloqué).

## Anti-objectifs
- Casser le flow imposé ou une règle de la borne : bouton blanc ≤ 1,5 s, AFK 60 s, 3 boutons, `ArcadeInput` seulement, score et nom via `HighscoreManager`.
- Une interface floue ou des pixels de tailles inégales : tout en 320×180 ×6, positions entières, aucune échelle fractionnaire (invariant n° 8). La liste glisse, mais chaque carte est posée au pixel entier.
- Une nouvelle erreur dans la console de l'éditeur quand la borne locale ne tourne pas.
- Un écran qui retarde trop le joueur : un seul geste (haut/bas) et un bouton.
- Une reconstruction (`Kenta > Reconstruire…`) qui écrase le morceau ou la chart qu'un niveau aura reçus plus tard.

## Contraintes
- Unity 6000.3.23f1, build Web, Firefox ESR, GPU intégré : pas de RenderTexture en plus pour les vignettes.
- Scène unique `Main` ; écrans = prefabs **générés par `KentaBuilder`** (comme tous les autres) ; `.unity`, `.prefab`, `.asset`, `.meta` jamais édités à la main (MCP Unity ou code éditeur).
- Toolkit Anatidae en lecture seule : on utilise `ExtradataManager` tel quel.
- Le menu « Reconstruire thèmes, prefabs et scène » ajoute à `Main.unity` un objet `Bandana` absent de `main` : ne pas l'apporter dans la PR.

## Approches considérées
**Construction de l'écran** — une seule voie retenue : prefab généré par `KentaBuilder` (comme les 7 autres écrans), cartes de la liste créées à l'exécution à partir du `LevelCatalog` (autant de cartes que de niveaux, sans retoucher le prefab). Écarté : construire l'écran à la main via le MCP (la prochaine reconstruction l'effacerait).

**Mise en page** (maquettes du 25/11 dans `.superpowers/brainstorm/`) — A carrousel (un niveau en grand), B grille 4×2, C écran Config enrichi, puis liste façon osu! en deux variantes de carte. **Choix de Lou : écran dédié, liste façon osu!, cartes avec vignette du décor.**

**Stockage des classements par niveau** — A Extradata de la borne, B `PlayerPrefs` (stockage local du navigateur), C notre VPS. **Choix de Lou : A.** B se perd si le profil du navigateur est réinitialisé et reste invisible pour le serveur de la borne ; C n'existe pas encore (ADR-006).

## Ce qui a été décidé avec Lou
- **Ordre du flow** : Accueil → Consignes → **Héros** → **Niveau** → Partie → Game Over (+ classement) → Saisie du nom si top 10 → Accueil. L'écran du héros perd le choix du monde (haut/bas). Le bouton 2 ramène du choix du niveau au choix du héros.
- **Un thème par niveau** : on retire les 3 champs `"biome"` de `level01_shovel_knight.json` et on vide `BIOMES` dans `tools/gen-level-vertical.py`. Le mécanisme `ChartSection.biome` (ADR-025) reste dans le code pour une future chart. L'ambiance par section (`BiomeBackdrop.SetSection` : teinte et lumière) reste.
- **Musique et parcours provisoires** : les 8 niveaux gardent « Main Theme » (Shovel Knight, provisoire, `Audio/LICENSES.md`) et `level01_shovel_knight.json`.
- **Classement par niveau** dans l'Extradata (clé par niveau) ; le score part aussi dans la liste officielle (`HighscoreManager.SetHighscore`).
- **Roadmap** (déjà commitée sur la branche) : sélection de niveau (en cours), contenu unique par niveau, états du thème.

## L'écran (320×180)
Maquette de référence : [../../img/selection-niveau-maquette.png](../../img/selection-niveau-maquette.png).
- **Fond** : le décor complet du niveau sélectionné (`BiomeBackdrop.SetBiome`), qui défile lentement comme dans les autres menus, assombri d'un voile.
- **En-tête** (bandeau sombre, 16 px) : « CHOISIS TON NIVEAU » en or à gauche, compte à rebours à droite.
- **Panneau d'infos à gauche** (plaque `panel.png`, ~134×142) : nom du niveau (×2 s'il tient en largeur, sinon ×1) ; titre du morceau ; « 150 BPM  1:43 » (BPM du `SongData`, durée du clip) ; étoiles (`LevelDef.stars`) ; trait ; « CLASSEMENT DU NIVEAU » ; top 5 (rang, nom, score ; « --- » pour les places vides ; la 1re place en or).
- **Liste à droite** : une carte par niveau (fond sombre, vignette 40×24 du décor, « N NOM », titre du morceau, étoiles). La carte sélectionnée est au milieu de l'écran, plus haute (28 px contre 26), sortie vers la gauche, cadre or ; les autres s'écartent vers la droite de 6 px par rang d'écart et sont assombries. La liste **boucle** (après le 8, le 1).
- **Bas** (bandeau sombre) : « ▲▼ NIVEAU   1 JOUER   2 RETOUR ».

### Commandes et temps
- Joystick haut/bas (`ArcadeInput.Stick`) : niveau précédent / suivant, **avec répétition** si on maintient (premier pas, puis un pas toutes les ~0,15 s après ~0,4 s).
- Bouton 1 / Start (`ArcadeInput.ConfirmDown`) : jouer, avec la même garde de 0,3 s que l'écran du héros (l'appui qui a validé le héros ne lance pas la partie).
- Bouton 2 (`ArcadeInput.HitDown`) : retour au choix du héros.
- **15 s** pour choisir ; à zéro, on part sur le niveau en surbrillance.
- La liste **glisse** d'une case à l'autre (interpolation courte), chaque carte arrondie au pixel entier.
- **Rejouer vite** (`GameSession.IsQuickRetry`) : le niveau de la partie précédente est présélectionné ; sinon le niveau 1.

### Extrait de musique
- À l'entrée de l'écran et à chaque changement de niveau, le `Conductor` joue le morceau du niveau **en boucle depuis `SongData.previewBeat`** (nouveau champ ; 96 = début du refrain pour « Main Theme »), au volume des menus.
- Pour ne pas relancer l'extrait à chaque cran quand on fait défiler vite, il démarre quand la sélection est stable depuis ~0,25 s.
- Retour au choix du héros : la musique des menus reprend (comportement actuel de `PlayMenuMusic`).

## Données
- **`SongData`** : nouveau champ `previewBeat` (float, défaut 0).
- **`LevelDef`** : nouveau champ `thumbnail` (Sprite) ; `Label` inchangé.
- **8 `SongData`** `ScriptableObjects/Songs/Song_0N_<biome>.asset`, créés par `KentaBuilder.BuildLevels` en recopiant `Song_Level01` (clip, BPM, offset, chart), titre « MAIN THEME », `previewBeat` 96. **Règle de non-écrasement** : le builder ne crée un `SongData` que s'il n'existe pas, et n'assigne `LevelDef.song` / `LevelDef.thumbnail` que s'ils sont vides. Ainsi la PR « contenu unique » change un clip et une chart sans qu'une reconstruction ne les remette.
- `Song_Level01` reste (musique des menus et repli de `GameFlow`).
- **Chart** : les 3 champs `"biome"` retirés de `level01_shovel_knight.json` (seule modification du fichier) ; `BIOMES = {}` dans le générateur ; le test `test_world_changes_mid_level` devient « un niveau garde son monde » (aucune section n'a de biome).

## Vignettes
- Générées par `tools/gen-kenta-assets.py --only thumbs` → `Art/Kenta/Gen/ui/thumb_<biome>.png`, **40×24**.
- Composition : ciel + calques lointain, milieu et proche du décor 640×360 (fenêtre fixe 600×360, rapport 5:3), réduits en moyenne par blocs de 15×15 px, puis **chaque pixel ramené à la couleur la plus proche parmi celles du décor du biome** (couleurs opaques de la fenêtre) : aucune couleur nouvelle, pas de demi-teintes floues. Pas de contour : le cadre de la carte en tient lieu. Une étoile 5×5 (`Gen/ui/star.png`, blanche, teintée dans Unity) sert aux difficultés.
- Importées comme les autres sprites d'interface (`KentaSpritePostprocessor`, dossier `Gen/ui/`).

## Classements par niveau
- Nouvelle classe `Kenta.Core.LevelScores` (statique) :
  - clé Extradata `top_<LevelDef.id>` ; valeur = JSON `{"entries":[{"name":"LOU","score":18420},…]}` (`JsonUtility`, enveloppe obligatoire) ;
  - `Top(levelId)` → liste triée (score décroissant), **10 entrées max**, **une entrée par nom** (on garde la meilleure, comme le serveur de la borne pour la liste officielle) ;
  - `IsTopTen(levelId, score)` : `score > 0` et (moins de 10 entrées, ou meilleur que la 10e) ;
  - `Submit(levelId, name, score)` : insère en mémoire tout de suite, puis envoie avec `ExtradataManager.SetExtraData` si la borne répond.
- **Sans la borne** (éditeur, ou serveur absent) : classements en mémoire seulement, **aucun appel réseau**, donc aucune erreur nouvelle dans la console. On ne lance `ExtradataManager.FetchExtraData()` qu'après une réponse réelle de `HighscoreManager.FetchHighscores()` (la liste reçue remplace celle que pose `EditorArcadeShim`).
- **Fin de partie** :
  - le 2e panneau de l'écran de fin montre le **classement du niveau joué** (titre « CLASSEMENT : <NOM DU NIVEAU> ») au lieu de la liste officielle ;
  - saisie du nom si le score entre dans le top 10 **du niveau** ou dans la liste officielle ;
  - envoi : `LevelScores.Submit` et `HighscoreManager.SetHighscore` (liste officielle, inchangé).
- **Accueil** : la phase « classement » montre le **classement du niveau de la démo qui vient de passer** (démo de la Forge, puis classement de la Forge). Après une saisie de nom, elle montre celui du niveau joué, le nouveau score en or. La liste officielle reste celle du menu de la borne.
- `HighscoreBoard` reçoit une source d'entrées et un titre (liste officielle ou niveau), au lieu de lire seulement `HighscoreManager.Highscores`.

## Flow (`GameFlow`)
- Nouvel état **`GameState.LevelSelect`** entre `Config` et `Playing`. `Config` = héros seul.
- `Config` (héros validé) → `LevelSelect` ; `LevelSelect` (validé ou temps écoulé) → `Playing` ; bouton 2 → `Config`, avec le héros déjà choisi présélectionné et le compte à rebours du héros relancé.
- `HeroSelectScreen` : retrait du choix du monde (haut/bas, `worldText`, `OnLevelChanged`). Indication en bas : « ◄ ► HÉROS   1 VALIDER ».
- `LevelSelectScreen` expose `Done`, `Back` et l'événement `OnLevelChanged` (GameFlow change le décor, l'écran gère l'extrait) ; à la validation, il remplit `GameSession.Level` et `GameSession.LevelIndex`.
- Le reste (démo de l'accueil qui change de niveau, rejouer vite, bouton blanc, AFK 60 s) est inchangé.

## Construction (éditeur)
- `KentaBuilder.BuildLevelSelect` construit `Prefabs/Screens/Screen_LevelSelect.prefab` (en-tête, panneau, textes, top 5, bandeau d'aide, gabarit de carte) ; `BuildAll` l'inclut et câble `GameFlow`.
- Pour ne pas reconstruire toute la scène (objet `Bandana`), un menu **Kenta > Construire l'écran de niveaux** construit seulement ce prefab, les `SongData`, les vignettes des `LevelDef`, l'écran du héros modifié, et les branche dans la scène ouverte. C'est lui qu'on lance dans cette PR.

## Tests et vérification
- **EditMode** : `LevelScores` (tri, 10 max, une entrée par nom, `IsTopTen`, aller-retour JSON, valeur absente ou illisible = liste vide) ; navigation de la liste (bouclage, index de la carte centrale, décalage des cartes).
- **pytest** : vignettes (40×24, couleurs toutes dans la palette du biome) ; chart sans biome par section. `test_generated_chart_is_feasible_and_vertical` échoue déjà sur `main` : hors périmètre.
- **Console Unity vide** (compilation, et en Play sans borne locale) ; tests EditMode verts ; `pytest tools/tests` vert sauf le test déjà en échec.
- **Capture en jeu** de l'écran (1920×1080, via `ScreenCapture.CaptureScreenshot`) montrée à Lou.

## Documentation (même commit que le code)
`docs/game-design.md` (tableau des écrans : 3 Héros, 3 bis Niveau, 5 classement du niveau, accueil), `docs/roadmap.md` (cocher la sélection de niveau), `game/CLAUDE.md` (écrans, `LevelScores`, menu de construction, vignettes), `docs/decisions.md` (**ADR-030** : sélection de niveau, un thème par niveau, classements par niveau dans l'Extradata), `docs/chart-format.md` si la mention du biome par section change, commande des vignettes dans le `CLAUDE.md` racine.

## Laissé au constructeur
- Valeurs exactes de l'animation de la liste et de la répétition au joystick, dans l'esprit ci-dessus.
- Fenêtre du décor retenue pour les vignettes.
- Découpage du code de l'écran (une classe pour l'écran, éventuellement une petite classe de calcul de liste testable).
