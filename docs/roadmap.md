# Roadmap & backlog

Liste centrale des features **à faire**, **en cours** et **faites**. C'est ici qu'on regarde avant de choisir sa prochaine tâche.
Règles :
- Cocher `[x]` dans le même commit que la feature, et ajouter la date.
- Une idée ou un bug découvert en cours de route va dans la bonne section, avec sa priorité.
- En prenant une tâche, mettre son prénom : `[ ] (Lou) …`.
- Priorités : **P0** = requis pour le rendu ou la borne · **P1** = cœur du gameplay « Rayman » · **P2** = polish / bonus.

## Déjà fait
- [x] Setup monorepo, CLAUDE.md, docs, toolkit Anatidae en Unity 6000.3.23f1 (2026-09-25)
- [x] Borne locale + packaging `tools/package-build.sh` (2026-09-25)
- [x] `ArcadeInput` (dead zone), `Conductor` (horloge audio WebGL), `GameFlow` (flow d'écrans imposé) (2026-09-25)
- [x] Démo jouable : niveau depuis chart JSON, saut/frappe/glissade, 3 cœurs, checkpoints, score × combo, HUD, Game Over + saisie du nom, démo autoplay sur l'Attract, juice de base (2026-09-25)

## Jeu — P0 (rendu / borne)
- [ ] **Tester sur la vraie borne** : 60 fps dans Firefox ESR, manettes (correctif ADR-029 : à vérifier sur la vraie machine, et le sens haut/bas du joystick), bouton blanc, AFK 60 s, et surtout **l'audio démarre-t-il après un appui manette ?** (politique d'autoplay du navigateur)
- [ ] **Tester le ressenti dans Firefox** : durée et hauteur du saut, fenêtres de tolérance, latence des sons quantifiés (jusqu'à ~60 ms)
- [ ] **Réduire le build** (`Build.wasm` ≈ 31 Mo) : Managed Stripping Level = High (à tester sur un build : réflexion du toolkit), IL2CPP réglé sur la vitesse depuis le 25/11 (ADR-031), retirer les packages inutiles (navigation, multiplayer center, timeline, modules VR/XR…)
- [ ] **Musique libre de droits** : le niveau utilise provisoirement « Main Theme » de Shovel Knight (protégé, voir `Audio/LICENSES.md`) ; la remplacer par un morceau original ou libre (110–160 BPM, sections marquées), avec sa **version 8-bit** au même BPM + licence dans `Audio/LICENSES.md`
- [x] Chart recalée sur la structure réelle de « Main Theme » : obstacles posés sur les attaques de la musique, sections alignées, `tools/gen-level-chart.py` (2026-11-03)
- [ ] **Vérifier à l'oreille** que le niveau colle à la musique (offset 0,18 s : sinon essayer 0,38 s dans `SongData` et `tools/gen-level-chart.py`)
- [ ] **Tour REMIX 8-bit** : à la fin du morceau, on reboucle sur la même chart avec `remixClip` et des perturbations visuelles croissantes, jusqu'à la mort. C'est la « difficulté croissante » exigée. Aujourd'hui, fin du morceau = fin de partie.
- [ ] **Communication jeu → site** (exigée par le sujet) : `Scripts/Net/` + `NetConfig`, `POST /api/runs/*` et `GET /api/live-config` via `AnatidaeProxyWebRequest`. Contrat : [api-contract.md](api-contract.md)
- [x] `thumbnail.png` Kenta (la Sylve, le héros et le logo ; `tools/gen-kenta-assets.py --only thumbnail`) (2026-11-03)
- [ ] `attract.mp4` (≤ 20 s, muet) en option : trailer pour le mode attract de la borne

**Perf et bugs borne (2026-11-25, ADR-031)**
- [x] (2026-11-25) Page qui défile, pas en plein écran → gabarit Web « Kenta » (rendu fixe 1920×1080, sans défilement)
- [x] (2026-11-25) Peu de fps → niveau par tranches (≈ 560 sprites actifs au lieu de 6 000), un seul bandeau dans la scène (au lieu de 30), build release, moins d'allocations par image
- [x] (2026-11-25) Morts « dans l'escalier » et dans les cuirassés au lieu du rebond → physique du héros en pas de 1/60 s (vérifié à 15 i/s avec des à-coups)
- [x] (2026-11-26) Build Web de release refait après les niveaux 1 à 3, la difficulté par héros, le nouveau HUD et le CRT (`dist/Kenta`, 66 Mo dont `Build.wasm` 31 Mo, 1 min 30 de build) ; vérifié dans un navigateur : l'accueil et la démo tournent
- [ ] **P0** Refaire un build Web (menu Kenta > Build Web (borne), module Web de 6000.3.23f1) et vérifier sur la borne : fps (overlay F9 absent en release : compter à l'œil ou via le panneau des performances de Firefox), plein écran, plus de morts sur les rampes
- [x] **P1** L'autoplay de Unity ramasse 373 pièces sur 395, le simulateur du générateur 395/395 : écart à trouver (déjà avant le 25/11). Trouvé le 2026-11-27 : le jeu ramenait le sol à 60 u au plus (`LevelBuilder.MaxHeight`), le refrain 2 de *Main Theme* monte à 69 u (voir plus bas)
- [x] (2026-11-26) Trop de changements de zoom (10 plans larges, laids) → 4 : chaque refrain entier et deux phrases de 8 temps (beats 40 et 160) pour voir venir des chauves-souris
- [x] (2026-11-26) Les cuirassés mouraient au rebond → ils restent en place (tremplin, son ressort s'écrase) et ne blessent plus une fois touchés
- [x] (2026-11-26) Saut sur un cuirassé qui tuait au lieu de faire rebondir → rebond aussi si on le touche les pieds à moins de 0,6 u sous son dessus (saut un peu court ou encore en montée)

## Jeu — P1 (gameplay « music level »)
- [x] **Niveau vertical** (ADR-018) : hauteur libre, rampes et course sur le mur (`ramp`), tambours-tremplins (`bounce`), caméra par plans (zoom ×3 à ×6, cadrage), niveau 01 réécrit scène par scène avec simulateur de l'autoplay (2026-11-24)
- [ ] (Lucas) **Tester le niveau vertical à la main** (clavier puis borne) : lisibilité des tambours, des chutes et des changements de zoom ; régler `tools/levels/shovel_knight.py`
- [ ] Chutes de plus de ~6 u : en vue 640 px, le sol d'arrivée n'apparaît qu'en cours de chute (la vue fait 9 u de haut). Option : vue encore plus large pour les plongeons, ou chutes en deux paliers
- [x] **Niveau musical, 2ᵉ passe** (docs/level-design-musical.md) : carte du morceau (`tools/analyse-song.py`), actions sur les attaques (grosse caisse → sauts, caisse claire → coups), pièces qui jouent la mélodie et dessinent le chemin, ennemis en vol, tambours en chaîne, caméra large sur rail précalculé sans coupes, brume de fond et piliers ombrés, HUD sans texte au milieu (2026-11-24)
- [x] (2026-11-25) **Prototype du refrain** (beats 96-128, testé depuis 78,5, `scene_refrain_ciel`) : 33 u de dénivelé, tambours en chaîne sur le roulement, grand plongeon en deux dévalées raides (−26 u, sol toujours à l'image) ; marge de survie ≥ 120 ms par action (avant : 48 à 83 ms), mesurée par `gen-level-vertical.py --marges` ; menu éditeur **Kenta > Tester le refrain (prototype)**
- [ ] (Lucas) **Tester le prototype du refrain à la main** ; s'il plaît, passer tout le niveau 01 à la même règle (marge ≥ 120 ms partout, `WIDE_MARGIN` = tout le morceau) et à 10-12 écrans de dénivelé
- [x] (2026-11-25, ADR-025) **Le niveau qui change** : lianes au pont (`vine`), changement de monde par section (refrain dans l'Orage, final dans les Échos), gravité inversée et course au plafond (`gravity`, `ceiling`), saut dans l'arrière-plan (`bounce` `to_back`/`to_front`, `back`), écran dédoublé et grésillement au refrain 2 (`screen_split`, `glitch`) ; menus **Kenta > Tester …** pour chaque passage ; règle des 120 ms étendue au pont, au final et au refrain 2
- [ ] (Lucas) **Tester à la main** le pont, le final et le refrain 2 (lisibilité du plafond, de l'arrière-plan, des 5 écrans)
- [ ] Le reste du niveau 01 (intro, couplets, pont 2, roulement 2) n'a que ±40 ms de marge par endroits (marches juste après un trou : 64 ms) : à repasser à la règle des 120 ms (`WIDE_MARGIN`)
- [ ] Arrière-plan plus lisible : aujourd'hui seulement assombri et derrière les piliers ; idées : décor du premier plan qui s'assombrit pendant le passage, son étouffé
- [ ] Lianes : sprite de nœud dédié (aujourd'hui la tuile de poteau du biome), corde au repos qui pend verticalement
- [x] (2026-11-25, ADR-027) **Retour de test** : chauves-souris qui apparaissaient au moment de les frapper → rail de caméra qui montre chaque ennemi 1,5 temps avant (minimum garanti 400 ms, médiane 1,26 s ; avant : 40 ms) et zone de frappe en l'air de 3,5 u ; lianes lâchées **au saut** (plus automatiques) ; deux mécaniques nouvelles, **cuirassés** (écraser + appui au rebond) et **notes de saut** ; intro et couplet réécrits ; règle des 120 ms sur tout le niveau sauf le pont 2 et le roulement 2 ; marges de caméra de Jeremy (0,12 / 0,2) appliquées dans la scène (elle gardait 0,08 / 0,1)
- [x] (2026-11-25) Cuirassé : sprite dédié commun à tous les biomes (carapace bleu acier + tremplin jaune, `tools/gen-stomper.py`), chevrons qui rebondissent sur le temps
- [ ] Cuirassé : son d'écrasement dédié
- [x] (2026-11-25, ADR-029) **Commandes de la borne** : « rien ne répond » reproduit (Unity 6.3 ne voit en Web que la manette à l'index 0) ; manettes lues dans le navigateur (`KentaGamepad.jslib`, `ArcadePads`), `ArcadeMenu` remplace le `MenuManager` en build ; vérifié dans un build Web avec une fausse manette à l'index 1 (accueil, partie, 80 s d'appuis sans retour au menu, bouton blanc → `localhost:3000`)
- [x] (2026-11-25) Chauves-souris à hauteur du héros (règle du générateur), refrain 2 = refrain 1 rejoué, écrans dédoublés en grille pleine (4 puis 9) dès le couplet 2
- [ ] Note de saut : sprite dédié (aujourd'hui la pièce ×2 teintée) et son (une note de la mélodie)
- [x] (2026-11-25) Pont 2 et roulement 2 (184-212) à la règle des 120 ms : **tout le niveau** est vérifié (`WIDE_MARGIN` = 0-256)
- [ ] Pont 2 et roulement 2 : y mettre les nouvelles mécaniques (aujourd'hui plateformes et escalier, comme avant)
- [x] (2026-11-25, ADR-030) **Glissade retirée** (une touche de plus pour rien) : commandes, physique, animation, barres, consignes (2 gestes), chart et simulateur ; les 4 glissades du niveau sont devenues des coups
- [x] (2026-11-25, ADR-030) Effets d'écran : **miroir** et **tête en bas** (`screen_flip`), sur des débuts de phrase ; cordes de liane qui pendent droit au repos ; premier plan semi-transparent quand le héros court dans l'arrière-plan
- [x] (2026-11-25, ADR-033) **Die and retry** : 10 vies, 23 points de reprise dans le niveau 01 (rejoués par le générateur), cœur et vies dans le HUD, bonus par vie restante ; décompte « 3 2 1 GO » retiré (il tombait pendant le niveau) ; halo clair qui pulse derrière les chauves-souris (invisibles sur les lianes) ; jeu en plein cadre hors 1920×1080 (`PixelScreen.FitScale`, interface calculée sur le cadre) ; optimisation (pièces animées seulement à l'écran ; le reste est venu avec l'ADR-031)
- [ ] Choisir entre 10 et 15 vies pour Kenta après test (réglage `HeroProfile.lives`, ADR-036)
- [x] (2026-11-26, ADR-036) **La difficulté dépend du héros** : Kenta facile (10 vies, reprise au point), Aiko moyen (3 essais, reprise au début, le meilleur compte), Ryo dur (une seule vie) ; `Core/RetryRules` + tests ; écran du héros (FACILE / MOYEN / DUR, règle du héros) ; consignes « TON HÉROS FIXE LA DIFFICULTÉ »
- [ ] **P1** Difficulté par héros : tester en jeu (reprise au début avec la musique, le décor, la caméra, la gravité inversée, les lianes ; HUD à chaque nouvel essai ; placement des textes de l'écran du héros)
- [x] (2026-11-26, retour de Lucas) Gravité inversée : la traînée du bandeau restait au-dessus du héros retourné → elle part de sa tête, et se retourne avec lui au portail (sens gardé dans l'historique, `BandanaTrail.AnchorPoint`, test) ; poussière du saut au plafond qui apparaissait de l'autre côté de l'écran (`y` retourné au lieu de la position monde) et éclat du portail : corrigés (`PlayerController.BodySideY`)
- [x] (2026-11-26, retour de Lucas) Chauves-souris : peu visibles et halo jaune qui se lisait comme un bouclier → battement d'ailes en 4 images tirées du PDF et liseré clair d'1 px (`tools/gen-bat.py`, test), halo retiré (`BeatGlow` supprimé) ; puis aussi au sol (ennemi des Échos) et au plafond, avec un vol stationnaire de ±4 px (`FX/Hover`, test)
- [ ] **P2** Une ligne de pièces passe parfois sur une chauve-souris (pièces posées sur la trajectoire de l'autoplay) : les écarter de l'ennemi dans `auto_lums`
- [ ] **P2** Reprise au point (Kenta) : les plateformes déjà franchies après le point de reprise ne rapportent plus de points (défaut existant)
- [x] (2026-11-26, ADR-037) Bonus de score par difficulté (×1,5 Aiko, ×2 Ryo) ; classement refait : une ligne par trigramme avec son héros et sa couleur (rouge franc en DUR), légende des bonus ; musique calme des menus (*Behind the waterfall…*, `Song_Menu`)
- [ ] **P2** Liste officielle de la borne (« MEILLEURS SCORES ») : le serveur ne garde que nom et score, ses lignes restent sans héros ni couleur
- [ ] Dessins de glissade du héros (`slide_*`, `slidein_*`, `slideout_*`) et leur génération (`tools/gen-heroes.py`, `test_heroes.py`) : inutilisés depuis ADR-030, à retirer côté art
- [ ] Portail de gravité : son dédié et sprite (aujourd'hui deux colonnes de pixels)
- [ ] Décor qui joue avec la musique (éléments qui réagissent au temps, foule d'ennemis qui danse au fond)
- [ ] Musique libre de droits **écrite pour le niveau** : idéalement, les sons des actions manquent dans la piste et c'est le joueur qui les joue
- [ ] Tambour : animation d'écrasement au rebond et son dédié (aujourd'hui : son de saut)
- [ ] **Menace qui poursuit** le joueur (mur de son / créature) à l'écran
- [ ] **Télégraphie** : chaque obstacle s'annonce 1 mesure avant (couleur, clignotement, animation)
- [x] Ennemis en hauteur (`lane 1`) à frapper pendant un saut : chauves-souris du niveau 01 (2026-11-24, PR de Lucas)
- [ ] **Écran Config** : 1 ou 2 joueurs
- [x] (2026-11-25, ADR-032) **Sélection de niveau** : écran dédié après le héros, liste façon osu! avec vignettes, extrait du morceau, top 5 du niveau ; un `SongData` par niveau ; un thème par niveau (plus de changement de monde dans la chart) ; **classement par niveau** (top 10 dans l'Extradata de la borne, le score part aussi dans la liste officielle). Provisoire : les 8 niveaux gardent le même morceau et la même chart
- [ ] Choisir le score envoyé à la liste officielle de la borne (aujourd'hui le score brut, tous niveaux mélangés : cumul des meilleurs scores par niveau ?)
- [ ] Écran des niveaux : son au déplacement et à la validation ; tester à la main sur la borne (défilement maintenu, extrait, retour au héros)
- [ ] **Contenu unique par niveau** : un morceau et une chart propres à chacun des 8 niveaux (aujourd'hui tous sur « Main Theme » et `level01_shovel_knight.json`). Un thème par niveau, sans changement de monde en cours de partie. Plan proposé le 2026-11-26 : 6 niveaux à difficulté croissante, voir la section « Niveaux par monde »
- [x] (2026-11-27, ADR-041) **Ciel qui vit** : le soleil se couche au fil du morceau, la nuit tombe à la section `fin` (lune, étoiles) ; éclairs sur les temps dans l'Orage (événement `lightning`, niveau 8)
- [ ] **P2** Autres états du thème, sur le même modèle (événement de chart ou section) : pluie au Bayou, lucioles la nuit dans la Sylve, braises qui montent à la Forge sur les temps forts, cristaux des Échos qui brillent sur le temps
- [ ] **P2** Gardien du décor toujours au centre du ciel : `BiomeBackdrop` multiplie `guardianOffset` par `k / PixelScreen.Ppu`, une division entière (= 0). Corriger (`(float)`) déplacerait chaque gardien à la place écrite par `gemini-gardiens/clean.py` : à regarder biome par biome avant
- [ ] **États du thème** au fil d'un niveau : pluie, nuit, lever du jour… Visuels par biome (particules, palette, ciel) + un champ par section dans la chart, appliqué au décor par `RunManager`. À écrire avec la chart propre à chaque niveau (un orage se cale sur un drop du morceau)
- [ ] **Mode 2 joueurs** hot-seat (P1 puis P2 sur le même morceau, comparaison à la fin)
- [ ] Stems audio : couches (batterie, mélodie) qui montent selon le combo ou la section

## Niveaux par monde (plan proposé le 2026-11-26, à valider)
Détail : [superpowers/specs/2026-11-26-niveaux-par-monde-design.md](superpowers/specs/2026-11-26-niveaux-par-monde-design.md). 6 niveaux, un par monde, du plus facile au plus dur ; le niveau 01 actuel devient le brouillon du niveau 6.
- [x] (2026-11-26, ADR-035, choix de Lucas) 6 niveaux : Sylve, Bayou, Échos, Forge, Givre, Grand Orage (Échine et Cité en réserve), grille de difficulté et musiques du plan ; tour REMIX en bonus P2
- [ ] **P0** Présenter l'ADR-035 à Lou et Jeremy
- [ ] **P1** Niveau 2 : choisir entre *Arpanauts* et *HHavok-main* (Skiff, 120 BPM)
- [x] (2026-11-26) Musiques : short-list téléchargée dans `tools/music-candidates/` (gitignoré : Eric Skiff *Resistor Anthems* CC BY 4.0, Juhani Junkala *Adventure* et *Action* CC0) et mesurée (BPM, dérive, durée) ; échelle proposée 117 → 120 → 140 → 154 → 162 → 174 BPM (tableau dans le plan)
- [ ] **P0** Musiques retenues (ADR-035) : les installer dans `Audio/Music/` (coupe sur une fin de phrase, Vorbis), licences dans `Audio/LICENSES.md`, crédit CC BY sur la carte du niveau et sur le site. Abstraction (*Three Red Hearts*, CC0) reste à télécharger à la main depuis itch.io
- [x] (2026-11-26) Générateur découpé : moteur commun `tools/levelgen/` + un fichier par niveau `tools/levels/` avec sa fiche (mécaniques autorisées, marge en ms, vue, gamme des pièces) ; commande `python3 tools/gen-level.py <niveau>` ; le niveau 01 ressort **à l'identique** (`shovel_knight`, test `test_generated_chart_is_the_committed_one`) ; densité affichée (2,27 actions par mesure pour le niveau 01)
- [x] (2026-11-26) Marges, tolérance (±40 ms), délai pour voir un ennemi (400 / 600 ms) et point de reprise (500 ms) en ms, convertis au tempo ; vitesse de 3,2 u par temps (à 117 BPM et 8 u/s, une frappe 150 ms en avance ratait l'ennemi)
- [ ] **P1** Fenêtres du jeu encore en beats dans `PlayerController` (tampon de saut, lâcher de liane, appui des notes : 0,35 temps) : 140 ms à 150 BPM, 121 ms à 174 BPM. Le simulateur les vérifie à chaque niveau (marge de la fiche) ; à passer en ms si le niveau 6 coince
- [ ] **P0** `analyse-song.py` : mesure du BPM et de l'offset, proposition de sections, une carte par morceau
- [ ] **P1** Unity : catalogue de 6 niveaux, étoiles, crédit du morceau sur la carte du niveau
- [x] (2026-11-26) Niveau 1 (Sylve) **écrit** : `tools/levels/n1_sylve.py`, *We're the Resistors* monté en 1 min 16 (`tools/cut-song.py`), do mineur pentatonique pour les pièces, sauter et frapper seulement (sauts sur le temps 1, frappes sur 2 et 4), vue fixe 480, marge ≥ 160 ms (180 impossible : un saut en retard plafonne vers 170 ms), 17 points de reprise ; chart `Charts/n1_sylve.json`. Après le premier test (« un poil trop facile ») : 8 actions de plus, **1,44 action par mesure** (avant 1,22), 122/122 pièces
- [x] (2026-11-26) Niveau 1 dans Unity : `n1_sylve_resistors.ogg` (Vorbis, compressé en mémoire, préchargé) et `n1_sylve.json` importés, `Song_01_sylve` réglé (titre, clip, 117 BPM, offset 0,054, chart, extrait au beat 64) ; **partie complète en autoplay dans l'éditeur : finie à 75,9 s, 0 mort, 131/131 pièces** ; titre trop long sur la carte de l'écran des niveaux → coupé avant les étoiles (« WE'RE THE RESIS. », `LevelCard.FitText`, test EditMode) ; 141/141 tests EditMode
- [ ] **P0** Crédit CC BY dans le jeu (Eric Skiff, niveaux 1, 2 et 5 ; JadeWizard, niveau 4) : un champ « artiste » au `SongData`, affiché à l'écran des niveaux, et sur le site
- [ ] **P1** Niveau 1 : écouter le montage (raccord à 1 min 06, mesure 31 → 64), tester à la main, faire tester par 2 ou 3 personnes extérieures, DA de la Sylve irréprochable
- [x] (2026-11-26) Niveau 2 (Bayou) **écrit** : `tools/levels/n2_bayou.py`, *Arpanauts* (choisi plutôt que *HHavok-main* : temps fort net aux changements d'accord et à la basse), 1 min 44 avec fondu, ré mineur pour les pièces ; cuirassés appris au couplet, lianes au refrain (seul, répété, varié, combiné), puis tout mélangé ; noires sur tous les temps, vue fixe, **1,9 action par mesure**, marge ≥ 150 ms, 273/273 pièces, 25 points de reprise. Dans un enchaînement de cuirassés, le premier rebond dure 1 temps (un rebond de 2 temps monte à 6 u et cache le suivant). Test `test_difficulty_increases_from_one_level_to_the_next`
- [x] (2026-11-26) Niveau 2 dans Unity : `Song_02_bayou` (ARPANAUTS, 120 BPM, offset 0,048, extrait au beat 64), `Level_02_bayou` 2 étoiles ; **testé à la main et validé par Lucas**
- [ ] **P1** Niveau 2 : autoplay complet dans l'éditeur (pas encore fait)
- [x] (2026-11-26) Niveau 3 (Échos) **écrit** : `tools/levels/n3_echos.py`, *Stage 1* (Junkala, CC0, 140 BPM), boucle jouée deux fois et demie en copies entières (1 min 50), fa majeur pour les pièces ; chauves-souris au couplet (sur les lianes, entre deux notes ; pas sur les rebonds de cuirassé : elles y passaient sous le héros ou hors de l'image), notes de saut au refrain, gravité inversée au pont (au plafond de la grotte), puis tout mélangé ; premières croches ; **2,05 actions par mesure**, marge ≥ 120 ms, 355/355 pièces, 24 points de reprise, vue fixe (plan large possible seulement si un ennemi se verrait trop tard : `Fiche.wide_on_refrains`). Branché dans Unity (`Song_03_echos` STAGE 1, `Level_03_echos` 3 étoiles)
- [ ] **P1** Niveau 3 : autoplay complet dans l'éditeur, puis test à la main (lisibilité du plafond, des chauves-souris)
- [ ] **P2** La détection du premier temps (peigne sur l'enveloppe d'attaque) est en retard de 12 ms (mesuré sur des clics) : les offsets des niveaux 1 à 3 sont donc 12 ms trop grands. Sans effet sur les jugements (PARFAIT ≤ 45 ms), à corriger d'un coup si on le sent
- [x] (2026-11-26, choix de Lucas) **Niveau 4 provisoire = le niveau de base** (`shovel_knight`, *Main Theme*, 150 BPM, toutes les mécaniques dont les effets d'écran) : on « commence à mettre de la difficulté » ; `Level_04_forge` 4 étoiles. Musique protégée : pas de diffusion publique tant qu'elle reste
- [x] (2026-11-26) Niveau 4 (Forge) **écrit** : `tools/levels/n4_forge.py`, *Fire Maze* (JadeWizard, CC BY 4.0, 151,3 BPM, un morceau de volcan : tags fire, lava), boucle de 32 mesures jouée deux fois plus 4 mesures (1 min 48), la mineur pour les pièces ; tambours (seul, par deux, chaîne de quatre au refrain), course sur le mur et dévalée, plongeon, deux passages dans l'arrière-plan, gravité inversée au début du passage 2, escalier à contretemps ; plans larges sur les refrains seulement ; pas d'effet d'écran. **Progression mesurée** (N1 → N2 → N3 → **N4** → niveau de base) : actions par seconde 0,70 → 0,95 → 1,19 → **1,32** → 1,42 ; contretemps 0 → 0 → 1 % → **≈ 10 %** → 43 % ; dénivelé 5 → 13 → 11 → **35 u** → 70 u ; plans larges 0 → 0 → 0 → **24 %** → 31 % ; pic de 7 actions par phrase de 8 temps, comme les Échos ; marge ≥ 120 ms, 311/311 pièces, 25 points de reprise. Vérifié par `test_level_4_teaches_drums_ramps_and_back_plane`. Branché par le menu **Kenta > Brancher les morceaux des niveaux** (`Editor/KentaLevelSongs.cs` : `Song_04_forge` FIRE MAZE) ; Shovel Knight ne sert plus qu'aux niveaux 5 à 8. Crédit CC BY de JadeWizard à afficher (voir la ligne P0 du crédit)
- [ ] **P1** Niveau 4 : autoplay complet dans l'éditeur, puis test à la main (lisibilité de la chaîne de tambours et de l'arrière-plan à 151 BPM)
- [ ] **P1** Niveau 5 (Givre : écrans retournés) ; niveaux 6 à 8 : encore sur la chart de base, identiques au niveau 4
- [ ] **P1** Niveau 6 (Grand Orage) : le niveau 01 actuel réécrit sur sa musique, avec contretemps et doubles croches
- [x] (2026-11-26, Lou) **Niveau 8, Le Grand Orage, l'épreuve ultime** (`tools/levels/n8_orage.py`) : 180 BPM, toutes les mécaniques, marge 100 ms (120 ailleurs), chaque action sur une attaque du morceau (264 sur 269), rafales de frappes en croches, tremblements et coups de zoom sur les frappes, effets d'écran sur chaque phrase (dédoublé, miroir, tête en bas, demi-tour, grésillement), éclairs dans les blancs, 22 points de reprise ; fini par l'autoplay en jeu (10 vies sur 10). Course gardée à 3,2 u par temps : à 4 u, les frappes en avance ratent l'ennemi
- [x] (2026-11-27) Unity figé à 100 % (« crash ») : boucle infinie dans `RunManager.Update` sur les annonces quand aucun écran n'y est abonné (`OnWarning?.Invoke(warnings[warnCursor++].text)` : l'argument n'est pas évalué) ; corrigé. Aussi : régler Preferences > General > Script Changes While Playing = Recompile After Finished Playing
- [ ] **P0** Vérifier sur la vraie borne la sortie au bout de 60 s sans geste (corrigée le 27/11 : comptée sur les gestes, plus sur l'état tenu ; non testée sur la borne)
- [x] (2026-11-27, Lou) Niveau 9 sur une grille de **280 BPM** (le double du morceau) : sauts de 0,21 s, course à 15 u/s, ~50 ms de marge par action (le plus qu'on puisse laisser à ce tempo), vue large fixe ; fini par l'autoplay en jeu (10 vies sur 10)
- [x] (2026-11-27) Niveau 9 calé sur la musique (premier temps 0,028 s, attaques datées à leur début : actions à +6 ms des attaques au lieu de +25 à 40 ms) et zones lentes durcies (3,25 actions/s, niveau 8 : 2,48) ; boss vérifié en jeu (32 lasers, 16 notes noires) ; pièces dans le calque des acteurs (elles restent dorées en négatif) ; lignes CRT allégées (ADR-040)
- [x] (2026-11-27) Niveau 9 : la Sourdine flotte, monte et descend pour annoncer ses lasers, et le dernier coup de katana la fend en deux ; notes de saut : un appui un peu en avance n'est plus ignoré (ADR-040)
- [x] (2026-11-27, Lou) Niveau 9 : transition corrompue de la Sylve au monde sourd (totems 48 → 72) : bandes décalées, couleurs séparées, blocs en couleurs pures, barres R/V/B, trame, puis noir et blanc (`fx_corrupt`, `fx_gray`, ADR-042) ; vérifiée en jeu (autoplay)
- [ ] **P1** Niveau 9 : mesurer sur la borne le coût du shader de corruption (60 fps ? le reste de corruption 0,05 le garde actif dans tout le monde sourd : le passer à 0 si besoin)
- [ ] **P1** Niveau 9 : y utiliser tous ses sprites (planches `tools/gemini-niveau9`) ; aucun point de reprise entre les temps 72 et 192 (26 s) : à voir avec les testeurs
- [ ] **P1** Niveau 8 : renseigner l'auteur, le titre et la licence de `n8_orage.ogg` (`Audio/LICENSES.md`) ; faire tester le niveau par un humain (difficulté réelle)
- [x] (2026-11-27) **Niveau 9, La Sourdine** (ADR-040, `tools/levels/n9_sourdine.py`) : *BIKE* de Tangermusic (140 BPM, morceau entier, accord de l'artiste), la Sylve en couleur puis le monde sourd en négatif (9e biome, planches de `tools/gemini-niveau9` installées par `gen_sheets.py --install`), inversions du monde sur les temps forts (`fx_invert`), miroirs, dédoublés, grésillement, toutes les mécaniques, 100 ms de marge, un point de reprise toutes les 16 temps
- [ ] **P1** Niveau 9 : faire jouer des humains (vise ~300 essais) et resserrer si besoin (densité, marge sous 100 ms : ADR) ; vérifier la lisibilité des inversions et du gardien (hauteur derrière les montagnes)
- [ ] **P2** Niveau 9 : la Sourdine vivante (yeux qui clignent et suivent le héros, bouche qui s'ouvre sur les attaques, rayons et notes noires à renvoyer : planches C, D, E de `tools/gemini-niveau9`, pas encore utilisées) ; obstacles qui basculent une fraction de temps dans le monde sourd pendant la fissure ; chauves-souris et cuirassés en négatif
- [ ] **P1** DA : règle de couleur (héros, ennemis, pièces jamais dans les couleurs du fond) vérifiée par script sur les palettes des mondes

## Jeu — P2 (juice / direction artistique)
- [x] (2026-11-27, ADR-043) Héros : essais Gemini (complets, puis tête Gemini sur la marionnette) ; retour de Jeremy : **la marionnette d'origine reste**, tête comprise. Outils gardés : `tools/gemini-heros/clean.py`, `hybrid.py`
- [ ] **P2** Héros : corps encore dessiné par la marionnette (tenue fine, sans le détail de Gemini) ; pour aller plus loin, faire repeindre par Gemini les poses de la marionnette image par image (dossier « Kenta - Assets pour Gemini », fichiers 5 et 6), puis les installer à la place
- [x] (2026-11-27, ADR-043) Ennemis et objets redessinés par Gemini : 8 ennemis (danse, touché, casse en 3 images jouée à l'endroit du coup : `FX/BreakFx`), chauve-souris (vol, pendue au plafond, cassée), cuirassé écrasé (`FX/StompFrames`), tambour-tremplin, note de saut, pièce, nœud de liane ; `tools/gemini-ennemis/clean.py` ; ennemis, chauve-souris et cuirassé réduits de 20 % par rapport aux anciens sprites (retour de Jeremy : « un peu plus petits » ; zone de collision inchangée, fixe dans `LevelBuilder`)
- [ ] **P2** Portail de gravité dessiné par Gemini (`Gen/world/portal_post`, `portal_cap`, `portal_veil`) : installé, pas encore branché dans `LevelBuilder.MakeGravityPortal` (toujours dessiné en code)
- [x] (2026-11-27) **Héros et ennemis plus vivants** (ADR-039) : un katana par héros, frappe en 6 images dont la traînée s'arrête à la portée de frappe (images de frappe en 160×72), attente en 4 images (portraits animés à l'écran du héros), balancement dans la course ; danse écrasé / étiré pour les 6 ennemis du PDF à une seule image (`tools/gen-enemies.py`)
- [ ] **P1** Katana : vérifier en jeu sur la borne (lisibilité de la traînée à 60 i/s, les 6 images à 180 BPM)
- [ ] **P1** Test Python `test_level_chart.py::test_generated_chart_is_feasible_and_vertical` en échec sur `main` (hauteurs hors de [0 ; 2] dans la chart du niveau 01), depuis le passage à 3,2 u par temps sans doute
- [x] Effet CRT « bombé léger » (ADR-034, `FX/CrtScreen`).
- [ ] **P1** Mesurer les i/s de l'effet CRT en build release sur la borne (F8 n'existe pas en release : comparer avec un build sans `CrtScreen` si besoin) ; s'il coûte trop, réduire la lueur.
- [x] DA Kenta intégrée : rendu pixel 240×135 ×8, 8 biomes, 3 héros, 8 ennemis, Sourdine, bandeau de combo, interface pixel, écrans accueil / choix du héros / consignes / game over (2026-11-03). Voir [art-direction.md](art-direction.md)
- [x] Animations du perso : course calée sur le beat, saut en 3 poses, atterrissage, glissade, coup de poing (2026-10-13)
- [ ] Animation de mort et de victoire du héros ; foule d'ennemis qui dansent en arrière-plan
- [ ] Textes flottants (+50, PARFAIT) dans le monde, traînée du perso à fort multiplicateur
- [ ] Mise en scène des drops : effondrement du décor, changement de caméra
- [ ] Effets remix : brouillage, image inversée, zones sombres (shaders simples, compatibles GPU intégré)
- [ ] Écran « Comment jouer » animé (pictos des boutons au lieu du texte)
- [ ] **DA Kenta, à finir** (voir [da-kenta.md](da-kenta.md)) :
  - [x] Héros redessinés en marionnette pixel art (~3,5 têtes au lieu de ~2) : course 8 images, saut 8, frappe 4, glissade 3 + entrée 3 + sortie 3, accroupi, bandeau / pan de veste / écharpe / cheveux simulés, contour de silhouette seulement (`tools/gen-heroes.py`, ADR-022) (2026-11-24)
  - [x] Obstacles des 8 biomes (mur, bloc, plafond, poteau, plateforme) redessinés par Gemini au niveau des maquettes du PDF, ramenés en pixel art par script (`tools/gemini-obstacles/`, ADR-026) (2026-11-25)
  - [x] Gardiens des 8 biomes redessinés par Gemini au niveau des maquettes, **immobiles** (plus d'yeux qui s'ouvrent) et **derrière les montagnes** (`tools/gemini-gardiens/`, ADR-028) (2026-11-25)
  - [ ] **P2** Obstacles à refaire dans Gemini : `wall_orage` (socle plat), `block_orage` et `post_orage` (fond rectangulaire retiré, contour haché), `block_cite` (copie de l'ancien bloc), `post_echine` (corde en rayures jaunes et noires)
  - [ ] Faire valider le nouveau gabarit par l'équipe, puis **vérifier sur la borne** : lisibilité à 2 m, rendu des héros à ×3 (vue large)
  - [ ] Animations restantes : attente animée (menus), pose de victoire, mort animée ; liane et course sur mur à brancher avec leurs mécaniques
  - [x] Vignette (`thumbnail.png`) : Kenta en marionnette, bandeau rouge « DÉMO » ; `info.json` à jour (plus de Sourdine, mentions « démo ») (2026-11-25)
  - [ ] Pages du site : encore avec le Kenta du PDF
  - [ ] Version finale : retirer le bandeau « DÉMO » de la vignette (`demo_banner` dans `gen_thumbnail`) et les mentions « démo » de `info.json`
  - décors des biomes plus riches (aujourd'hui générés : arbres, rochers, cristaux, volcan, ruines, nuages) : éléments qui jouent sur le temps (champignons-cymbales, bulles, cristaux en arpège, geysers, aurore, éclairs…)
  - scènes de drop entre biomes (tronc qui frappe, chute vers le marais, éruption, avalanche…) : aujourd'hui flash + secousse + changement de décor
  - ennemis : variante « cassé » (pièces qui s'échappent) pour les 8, comme le djembé
  - tour REMIX : `PixelScreen.SetDivider(3)` (80×45), palette 4 tons, perturbations (voir § 9) ; il manque la version 8-bit du morceau
  - mode 2 joueurs : le joueur 2 rejoint à l'écran de choix du héros (cadre turquoise)
  - écran « Continuer ? » : la borne n'a pas de crédit (point à trancher, § 11 de la DA)
  - tester la lisibilité des obstacles sur la borne, debout à 2 m

## Audit du 2026-11-24 (après le merge du niveau vertical)
Partie complète en autoplay dans l'éditeur : niveau fini, note S, 142 PARFAIT, 339/340 pièces. Points relevés, absents des listes ci-dessus.

**Gameplay**
- [x] (2026-11-24) **P0** Une seule glissade dans tout le niveau 01 → 6 glissades (25, 51, 123,5, 171, 243,5, 253,5), test Python (`slide_bar` au beat 25) alors que c'est 1 des 3 commandes, enseignée à l'écran Consignes : en poser sur les notes tenues (`gen-level-vertical.py`)
- [x] (2026-11-24, ADR-020) **P0** Le multiplicateur atteint ×8 vers le beat 70 (~28 s) et n'évolue plus pendant les 3/4 du niveau ; la Sourdine reste alors loin. Pistes : les pièces ne comptent plus dans le combo, ou un palier plus long (`comboPerMultiplier`)
- [x] (2026-11-24, ADR-020) **P0** La Sourdine couvre le héros à ×1 : bord à 45 px + voile de 56 px = 101 px, alors que le héros est à ~70 px (cadrage 0,22 de la vue). Le héros est grisé en début de partie et après chaque BOF. Recaler `Sourdine.TargetEdge` sur la place du héros à l'écran
- [x] (2026-11-24, ADR-023) Puis **Sourdine retirée du jeu** à la demande de Jeremy : avec la mort subite, elle n'a plus de rôle
- [x] (2026-11-24, avec la chart régénérée : 326/326 et S+ en autoplay) **P1** 339/340 pièces en autoplay : une pièce est inatteignable, donc le S+ (« toutes les pièces ») est impossible. Trouver laquelle dans le simulateur
- [x] (2026-11-24, ADR-021) **P1** Un seul monde jouable → choix du monde à l'écran du héros (haut/bas). Reste : une chart par monde (« niveau par biome »). Avant : : les 8 `LevelDef` partagent la même chart et le jeu lance toujours `catalog.levels[0]` (la Sylve). Il manque l'écran ou le mode qui donne accès aux 7 autres (« niveau par biome », en cours)
- [x] (2026-11-24, ADR-021) **P1** Peu de variété d'obstacles → 6 murs, 6 glissades, test Python de variété. Avant : : 49 champignons au sol, 17 chauves-souris, 21 trous, 45 marches ; plus aucun mur, bloc ni plafond (types du PDF)
- [x] (2026-11-24, ADR-021) **P1** Rejouer est long → consignes sautées et choix repris dans la minute, écrans de fin passables au bouton 1, game over 4,5 s + classement 3,5 s. Avant : : game over (5,5 s), classement (4 s), accueil, consignes (6 s), choix du héros (12 s), soit ~25 s entre deux essais en mort subite. Pistes : passer les consignes si on rejoue dans la minute, raccourcir le game over
- [x] (2026-11-24) **P2** Choix du héros : au bout de 12 s, le code force Kenta même si le joueur a déplacé le curseur (`HeroSelectScreen`, `index = 0`). Garder le héros sélectionné
- [x] (2026-11-24, garde de 0,3 s) **P2** Choix du héros sans garde d'entrée : l'appui B1 qui passe les consignes peut valider le héros dans la même frame (ordre des `Update`), à vérifier
- [x] (2026-11-25, un son de frappe par héros) **P2** Les 3 héros sont seulement cosmétiques (mêmes sons, même gameplay) : au minimum un son de frappe par héros (caisse claire, flûte, basse) comme dans le PDF

**Visuel**
- [x] (2026-11-24, ADR-020) **P0** Flash blanc plein écran + secousse + son de « checkpoint » à chacune des 10 sections, en double (`section_change` de la chart **et** `RunManager.EnterSection`), alors qu'il n'y a plus ni checkpoint ni changement de monde : ça masque les obstacles en début de section. Garder un seul effet discret, ou le réserver aux vrais drops
- [x] (2026-11-24, ADR-020) **P0** Victoire et défaite ont le même écran : la Sourdine recouvre tout et grise l'image, même sur « BRAVO ! ». Faire une fin heureuse (feu d'artifice, pose de victoire, couleurs)
- [x] (2026-11-24, ADR-021) **P1** Densités de pixels mélangées → décors régénérés en 640×360 (`pixlib.Pix(scale=2)`). Avant : en vue 640 px : le décor (320×180) est agrandi ×2, donc ses pixels font 6 px d'écran contre 3 px pour le niveau et le héros. L'arbre rose du fond est plus gros et plus « cubique » que le premier plan. Dessiner le décor en 640×360, ou revenir à une vue plus serrée
- [x] (2026-11-24, ADR-021) **P1** Piliers de terre → trois tons (bandeau, terre, profondeur `fill_deep`) et détails semés (`deco_*`). Avant : : de grands aplats de briques brun foncé (jusqu'à la moitié de l'écran), tous identiques. Ajouter racines, pierres, mousse, variations de tuiles, dégradé vers le bas
- [x] (2026-11-24, ADR-021) **P1** Trous et chutes → liquide du biome 1 u sous le bord, plus sombre en profondeur. Avant : : le fond d'un trou est vide (couleur de la brume). Montrer le danger : eau, ronces, obscurité ou la Sourdine en bas
- [x] (2026-11-24, ADR-021) **P1** Un seul décor pendant 1 min 43 → ambiance par section (teinte, lumière qui baisse) ; la démo de l'accueil change de monde. Avant : : même arbre rose, même gardien, même ciel. Varier par section (heure, météo, couleurs) même sans changer de biome
- [x] (2026-11-24, ADR-021) **P1** Le gardien (Cerf de pierre) → sprite à part, plus contrasté, yeux ouverts une mesure à chaque section et tout le refrain. Avant : est une tache grise floue derrière la brume, jamais animé : le rendre lisible et le faire réagir aux sections
- [x] (2026-11-25) **P2** Pièces qui tournent : les images de profil (1 à 2 px de large) se lisent comme des traits orange parasites dans les arcs. Garder des images plus larges, ou ne pas les faire tourner
- [ ] **P2** `pytest tools/tests` : `test_generated_chart_is_feasible_and_vertical` échoue (`validate` de l'ancien `tools/gen-level-chart.py` refuse des hauteurs, ex. « hauteur 6.0 … beat 66 ») ; déjà le cas sur `main` le 2026-11-25. Mettre le test à jour ou le retirer avec l'ancien générateur
- [x] (2026-11-25, dessous en îlot) **P2** Plateformes flottantes trop fines (2 à 3 px) : elles se lisent comme des lignes. Les épaissir, avec un dessous
- [ ] **P2** Tambours-tremplins dessinés en tonneaux : rien ne dit « rebond ». Peau tendue, couleur vive, flèche ou animation d'appel
- [ ] **P2** Héros petit en vue 640 px (~50 px sur 360, soit 150 px d'écran) : vérifier la lisibilité à 2 m sur la borne
- [x] (2026-11-25, ruban plein à partir de ×4) **P2** Bandeau (traînée de combo) : une ligne pointillée fine, presque invisible même à ×8
- [x] (2026-11-25, `JudgePopup` suit le héros) **P2** PARFAIT reste à l'endroit où l'action a été jugée : sur un tremplin, le mot apparaît sous le héros qui s'envole. Le faire suivre le héros

**Écrans et interface**
- [x] (2026-11-24, ADR-021) **P1** Accueil → cycle logo, démo jouée (un monde différent à chaque passage), classement ; sol et terre sous le décor des menus. Avant : décor seul et texte. Ni démo jouée (pourtant décrite dans `game-design.md` et `game/CLAUDE.md`), ni héros, ni pièces autour du logo. Pour attirer les passants : alterner logo, démo en autoplay et classement. Mettre la doc à jour selon le choix
- [x] (2026-11-24, ADR-021) **P1** « APPUIE SUR START » → jamais sous 85 %, passe de l'or au blanc ; en bas de l'écran pendant la démo. Avant : il descendait à 55 % d'opacité : jaune sale sur plaque grise, peu lisible au creux de la pulsation
- [x] (2026-11-24, ADR-021) **P1** Classement et saisie du nom → `HighscoreBoard` et `NameEntryScreen` en pixel art (données et envoi par le toolkit), vérifiés avec la borne locale. Avant : : style par défaut du toolkit (police Arial, panneau gris), en rupture avec le pixel art. Le restyler (autorisé par le CLAUDE.md) : police pixel, pièces or / argent / bronze
- [x] (2026-11-24, ADR-021) **P1** Consignes → 3 gestes avec les boutons de la borne, le héros mime sur le temps. Avant : 6 lignes de texte et une image fixe qui change toutes les 1,1 s, sans lien avec le tempo. Trop long pour « compris en 10 s ». Garder 3 gestes avec des pictos de la borne, et le héros qui mime en boucle sur le temps
- [ ] **P2** Choix du héros : grands cadres sombres presque vides, sélection signalée par la seule couleur du cadre, pas de pose de victoire ni de son au déplacement
- [x] (2026-11-25, un segment par section) **P2** La frise du HUD a 8 segments (8 biomes) alors que le niveau a 11 sections et un seul monde : la caler sur les sections du niveau
- [x] (2026-11-24, les menus sont aussi en vue large) **P2** Retour à l'accueil après une partie : le zoom de la partie (640 px) n'est pas remis à 320, et l'accueil change d'échelle après la première partie (`GameFlow.Enter(Attract)`)
- [x] (2026-11-24, vérifié en 1920×1080) **P1** Faux problème : le HUD est bien dessiné au-dessus de la Sourdine ; l'impression venait du texte de debug (F9) qui le recouvre en éditeur. Constat initial : le score et le multiplicateur (HUD en haut à gauche) passent sous la Sourdine : la masse et le voile recouvrent la plaque du score, même à ×8. Dessiner le HUD au-dessus de la Sourdine, ou le décaler
- [ ] **P2** Feu d'artifice de victoire en particules simples : gerbes petites, sans fusée ni traînée
- [x] (2026-11-25, ADR-033 : agrandissement de l'interface calculé sur le cadre ; vérifié dans un build en 1024×768 et 1600×900) **P2** Hors 1920×1080 (confirmé le 24/11 : en 1920×1080 dans l'éditeur, HUD et panneaux sont entiers) (fenêtre de l'éditeur), l'interface 320×180 est rognée au cadre du monde : le HUD disparaît et le panneau de fin est coupé. Sur 1920×1080 les deux cadres font la même taille (×3 et ×6) : à confirmer dans un build

**Nouveaux points (après les P1)**
- [x] (2026-11-26) **P2** « SILENCE… » et « ENCORE ! » au milieu de l'écran → sous la frise, en fondu ; la mort se lit sur les vies
- [x] (2026-11-26) **P1** HUD en jeu brouillon (retour de Lucas : espaces trop grands et incohérents) → 3 plaques alignées sur une grille unique (`HudLayout`), chiffres en colonnes fixes, animations douces (score qui défile, ×N qui rebondit, fondus), vies selon le héros, fondu quand le HUD couvre l'action, PARFAIT sous le héros en gravité inversée ; 19 tests EditMode
- [ ] **P2** HUD : les « 1 » paraissent espacés dans les chiffres en colonnes fixes (« 000111 ») : un « 1 » plus large dans la police, ou des chiffres à chasse fixe dans `PixelText`
- [ ] **P2** HUD : vérifier la lisibilité à 2 m sur la borne
- [ ] **P2** Liquide des trous parfois hors de l'image quand la caméra anticipe une montée (le sol est alors en bas de l'écran)
- [x] (2026-11-25, ignoré pendant l'autoplay et texte masqué par `EditorArcadeShim`) **P2** En éditeur, l'AFK du toolkit (60 s sans appui) affiche « Retour au menu… » pendant une partie en autoplay : sans effet sur la borne (on y joue), mais l'accueil en cycle (logo, démo, classement) renvoie au menu de la borne après 60 s d'inactivité, comme le veut le toolkit

## Audit du 2026-11-25
Partie complète en autoplay, 784 captures. Détail, causes et pistes : [audit-2026-11-25.md](audit-2026-11-25.md). La **verticalité** a finalement été traitée ici (demande de Jeremy), sans toucher à la physique de `PlayerController`.
- [x] (2026-11-25) **P0** Décor collé à l'écran verticalement (arbres qui « volent ») → `BiomeBackdrop` pose la ligne de sol des calques sur le sol le plus bas de la vue (lu sur `CameraRig.HeightProbe`, lissé), une part par calque
- [x] (2026-11-25) **P0** Bande vide et pâle sous les arbres → pied des calques (`kenta_art.add_foot`), étiré jusqu'au bas de la vue
- [x] (2026-11-25) **P0** Gardien → entier, opaque, **devant** les montagnes lointaines : c'est l'élément principal du décor (choix de Jeremy, au lieu de l'enfoncer). Remplacé le même jour par ADR-028 : derrière les montagnes, à la demande de Lou
- [x] (2026-11-25) **P1** Brume blanche à 32 % → dégradé de 4 % en haut à 20 % en bas
- [x] (2026-11-25) **P1** « PARFAIT ! » suit le héros (`JudgePopup`)
- [x] (2026-11-25) **P1** Traînée du bandeau → ruban plein, seulement à partir de ×4
- [x] (2026-11-25) **P1** Pièces de 17 px, sans image de profil (aussi le point P2 des « traits parasites »)
- [x] (2026-11-25, cycles montée / descente / chute dans `HeroAnimator`) **Verticalité** Le héros **glisse** en montant une colonne de marches ou une rampe : ses pieds suivent le terrain pendant que la course continue à plat ; en descente, il tombe du bord sans transition. Piste : une pose d'escalade / d'appui sur la marche, et une réception à chaque marche descendue
- [x] (2026-11-25, marge haute 0,2) **Verticalité, P1** Pièces parfois hors de l'image en montée (placement et rail)
- [x] (2026-11-25, `CameraRig.safeMargins` 0,12 / 0,2) **Verticalité, P1** Cadrage : héros au bord haut sur certains tambours, sol collé au bas avec un grand vide au-dessus (`safeMargins`, rail)
- [x] (2026-11-25, `fill_deep` éclairci vers le ciel) **Verticalité, P1** Piliers de terre encore massifs et sombres sur les marches hautes
- [x] (2026-11-25) **P2** Écran « BRAVO ! » : note S coincée dans une case étroite
- [x] (2026-11-25, affichée 3 s) **P2** Étiquette de section du HUD qui chevauche les pièces en haut de l'image
- [x] (2026-11-25, 5 motifs) **P2** Arbre rose du calque proche trop gros et trop répété : au moins 3 motifs par biome
- [x] (2026-11-25, `AuditScenarios` en 1920×1080) Limites de l'audit revues : écrans, choix du héros et du monde, 8 mondes en partie, défaite et saisie du nom
- [ ] **P0** Tester sur la vraie borne : Firefox ESR, 60 fps, manettes, envoi du score

## Grand audit du 2026-11-25
Verticalité, visuels après les obstacles de Lou, contenu. Détail, captures et pistes : [audit-verticalite-2026-11-25.md](audit-verticalite-2026-11-25.md).
- [x] (2026-11-25) Brume du décor limitée à une colonne de 282 px au centre de l'écran → échelle divisée par la taille du sprite (`BiomeBackdrop`)
- [x] (2026-11-25, décision de Jeremy : 1 niveau assumé, « DÉCOR » gardé tout le morceau ; peut changer plus tard) **P0** Les 8 mondes jouent la même chart (seule la peau change, et l'Orage et les Échos reviennent partout) : 1 niveau assumé, 3 niveaux, ou une chart par monde (E1)
- [ ] **P0 — décision d'équipe** Choisir la musique finale **avant** de polir la chart : tout le level design est écrit sur Shovel Knight (E2)
- [x] (2026-11-25, profondeur des piliers en paliers transparents) **P0** Verticalité visible : terrain en couches qui s'allège vers le bas, paliers flottants au lieu de colonnes de 20 à 60 u (A1)
- [x] (2026-11-25, calques qui descendent, mer de nuages, ciel plus froid ; le gardien reste immobile, ADR-028) **P0** Décor qui change avec l'altitude : bandes (mer de nuages, ciel sombre), parallaxe verticale, gardien qui suit le héros des yeux (A2)
- [x] (2026-11-25, vue 480, plans larges 640 posés par le générateur) **P0** Héros à ~60 px sur 1080 en vue 640 : vue 480 par défaut, 640 seulement sur les tambours et les plongeons ; contour sombre (B1)
- [x] (2026-11-25, 2×2 au plus, neige ≤ 30 % dégagée autour du héros, plus d'image masquée, FIN nette) **P0** Refrain 2 : 5 écrans de 640×360 et une image toute en neige, illisible à 2 m. 2 écrans au plus, neige ≤ 30 % et jamais sur le héros, retirer le glitch de la FIN (D1)
- [ ] **P1** Gestes de montée et de descente variés (double marche, mur, plateformes sur le temps, dévalée avec frappes), une grande ascension par section ; couplets 1 et 2 identiques aujourd'hui (A3)
- [x] (2026-11-25, cadrage selon la pente, plans larges) **P1** Caméra : héros collé en bas en descente, en haut sur les tambours ; rail asymétrique selon la pente, repère d'arrivée hors champ (A4)
- [ ] **P1** Couleurs : héros et ennemis perdus sur l'Orage et les Échos, pièces sur les arbres jaunes ; le fond ne doit jamais reprendre la couleur des pièces (B2)
- [x] (2026-11-25, vérifié : le niveau est toujours dessiné devant le décor ; la brume pleine largeur détache le jeu) **P1** Calque proche devant les ennemis et les pièces : le garder sous la ligne de sol (B3)
- [x] (2026-11-25) **P1** Lianes en trait de 1 px : corde de 2 à 3 px, nœud, halo sur le temps du lâcher (C2)
- [x] (2026-11-25, vraie porte ; plafond inchangé) **P1** Portail de gravité (deux traits cyan) et plafond (bande sombre) : arche, plafond dessiné comme un sol (C3)
- [x] (2026-11-25, dessous en îlot ; les barres de glissade ont disparu avec la PR #22) **P1** Plateformes flottantes et poteaux de barre trop fins (C4)
- [x] (2026-11-25, annonce en haut une mesure avant) **P1** Télégraphie des surprises (portail, arrière-plan, split) une mesure avant (D2)
- [ ] **P1** Passage à l'arrière-plan peu visible : sprite réduit, premier plan en silhouette, son étouffé ; l'image du héros se règle dans `PlayerController.SetVisualPlane` (D3 ; l'annonce une mesure avant est faite)
- [ ] **P1** Le jeu se joue tout seul (139 PARFAIT, 0 BIEN) : contretemps, doubles croches, deux chemins pour le score (E4)
- [x] (2026-11-25, `ResetMood` remet le sol à zéro) **P1** Démo de l'accueil → partie sans écran de fin : le décor glisse 0,6 s depuis la hauteur de la démo (remettre `groundValid` à faux au lancement)
- [x] **P1** L'autoplay du jeu ne ramasse que 372 pièces sur 395 alors que le simulateur du générateur les prend toutes (déjà vrai sur main après la PR #18). Les ratées sont presque toutes dans l'enchaînement de tambours du refrain 2 (beats 225 à 234, y 62 à 71). Cause (2026-11-27) : `LevelBuilder.MaxHeight` = 60 u aplatissait le sol à 60 u, les tambours ne montaient plus (la caméra, elle, suivait le vrai relief). Garde-fou relevé à 200 u, et le générateur refuse un niveau qui le dépasse (`MAX_HEIGHT`)
- [ ] **P2** Contour sombre du héros et règle de couleur de la DA (le fond ne reprend jamais la couleur des pièces) (B2)
- [ ] **P2** Rampes en escalier de colonnes de 8 px : tuiles de pente 1:1 et 1:2 par biome (A5)
- [ ] **P2** Poteau planté au pied d'une falaise (Sylve, beat ~28) : à vérifier dans la chart (C5)
- [ ] **P2** Calque proche : 8 à 10 motifs, ou des motifs par section (C6)
- [x] (2026-11-25, `ScreenFx` plafonné à 50 %) **P2** Flash blanc plein écran à l'entrée du final : 50 % au plus (photosensibilité) (D4)
- [x] (2026-11-25) **P2** Étiquette de section : ne nommer le biome que s'il change (F)

## Outils
- [ ] Outil pour écrire une chart plus vite (tap au rythme → JSON, ou visualiseur de chart)
- [ ] Tests EditMode : `ChartData.Parse`, faisabilité d'une chart en autoplay
- [ ] Gizmos de la grille de beats dans la vue Scene

## Site web (`web/`)
- [ ] **P0 — Landing v1** (exigée d'abord par l'enseignant) : noms (Lou Calmes, Lucas Corrieras, Jeremy Hordé), **rôles**, type de jeu
- [ ] P0 — Serveur Node : Express + ws + API du [contrat](api-contract.md), stockage JSON/SQLite
- [ ] P0 — Bloc « live borne » (WebSocket) : dernières parties, meilleur score du jour
- [ ] P0 — Mini-blog : articles d'avancement en Markdown (`web/content/blog/`)
- [ ] P1 — Concept, personnages, lore, dans l'esthétique du jeu
- [ ] P1 — Déploiement sur le VPS (nginx + pm2 + HTTPS) : **plus tard**, on reste en local pour l'instant (ADR-006)

## Équipe / organisation
- [ ] Définir les **rôles** des 3 membres ([decisions.md](decisions.md) + landing)
- [ ] Tous sur Unity **6000.3.23f1** + MCP Unity v10.2.0

## Bugs / dettes connus
- [x] (2026-11-27, retour de Lucas) Lianes : « quand je saute, ça m'obéit pas ». Un appui plus tôt que la fenêtre du lâcher (±0,35 temps) était ignoré : la main glissait et le héros tombait au lieu de sauter (il retombait parfois sur l'autre rive, ce qui masquait le problème au simulateur). L'appui fait après la prise est maintenant gardé : on lâche à l'ouverture de la fenêtre, et la main se resserre tout de suite (jeu et simulateur). À faire tester à la manette
- [x] (2026-11-27) Unity qui sature et plante : scripts recompilés pendant le Play mode, `NullReferenceException` à chaque image dans `FxPool`, `BiomeBackdrop`, `RunManager` (88 000 dans le journal). Garde-fous ajoutés ; régler Preferences > General > Script Changes While Playing sur Recompile After Finished Playing
- [x] (2026-11-27, retour de Lucas) Pièces mal placées et pas raccord avec la musique : la note sonnait toujours une double-croche trop tôt (ramassage ~0,3 temps avant + quantification) → jouée sur le temps de la pièce ; pièces sur la grille des croches, une au sommet de chaque saut, jamais collées à un ennemi ; rayon de ramassage 1 u (jeu et simulateur) et petite animation (`FX/CoinPickup`). Niveaux 1, 2, 3, *Main Theme* et 8 régénérés (seules les pièces changent)
- [x] (2026-11-27, retour de Lucas) Repères de rythme : la grille de debug (F9) restait à hauteur fixe et n'existait pas en build → un trait par temps qui suit la caméra en hauteur, toujours affiché (`FX/BeatGuides`)
- [x] (2026-11-27) Niveau 8, beat 192 : un portail de gravité pile à l'atterrissage faisait traverser le sol au héros (1,5 u) quand la bascule tombait une image avant le contact ; le simulateur, lui, s'était posé. Maintenant, si on touche le sol dans les 2 images, on se pose d'abord (`PlayerController.LandBeforeFlip`, `FLIP_LAND_SECONDS`). Vérifié dans le jeu (autoplay, Kenta) : toutes les pièces des niveaux 1, 2, 3, *Main Theme* (287/287) et 8 (344/344), sans mort (une mort sur une passe de *Main Theme* faite pendant que la machine était chargée, aucune sur les deux passes au repos)
- [x] (2026-11-27) *Main Theme* devient le niveau 5 (`Level_05_echine`, 4 étoiles) ; le niveau 4 est en cours chez Jeremy
- [x] (2026-11-26, retour de Lucas, sur la borne et le build de Lou) En fin de niveau, pas de « BRAVO » : le héros courait dans le vide et mourait. En build Web, la source audio peut rester « en lecture » après la fin du clip : la fin, attendue sur `!isPlaying`, n'était jamais vue. La fin se décide maintenant sur le temps du morceau (`Conductor.SongEnded`, 7 tests). **À vérifier sur la borne** avec un nouveau build
- [ ] L'atlas `LiberationSans SDF - Fallback.asset` change tout seul : ne pas le commiter (`git restore`). Idéalement, activer « Clear Dynamic Data On Build ».
- [ ] Tant que le classement de la borne a moins de 10 scores, tout score > 0 déclenche la saisie du nom (comportement du toolkit)
- [ ] À très bas FPS (onglet en arrière-plan), le perso tombe : le balayage de collision est limité à 3 unités. Sans impact attendu à 60 fps sur la borne.
