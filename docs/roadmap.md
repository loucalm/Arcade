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
- [ ] **Tester sur la vraie borne** : 60 fps dans Firefox ESR, manettes et dead zone, bouton blanc, AFK 60 s, et surtout **l'audio démarre-t-il après un appui manette ?** (politique d'autoplay du navigateur)
- [ ] **Tester le ressenti dans Firefox** : durée et hauteur du saut, fenêtres de tolérance, latence des sons quantifiés (jusqu'à ~60 ms)
- [ ] **Réduire le build** (`Build.wasm` ≈ 31 Mo) : Managed Stripping Level = High, IL2CPP « Optimize Size », retirer les packages inutiles (navigation, multiplayer center, timeline, modules VR/XR…)
- [ ] **Musique libre de droits** : le niveau utilise provisoirement « Main Theme » de Shovel Knight (protégé, voir `Audio/LICENSES.md`) ; la remplacer par un morceau original ou libre (110–160 BPM, sections marquées), avec sa **version 8-bit** au même BPM + licence dans `Audio/LICENSES.md`
- [x] Chart recalée sur la structure réelle de « Main Theme » : obstacles posés sur les attaques de la musique, sections alignées, `tools/gen-level-chart.py` (2026-11-03)
- [ ] **Vérifier à l'oreille** que le niveau colle à la musique (offset 0,18 s : sinon essayer 0,38 s dans `SongData` et `tools/gen-level-chart.py`)
- [ ] **Tour REMIX 8-bit** : à la fin du morceau, on reboucle sur la même chart avec `remixClip` et des perturbations visuelles croissantes, jusqu'à la mort. C'est la « difficulté croissante » exigée. Aujourd'hui, fin du morceau = fin de partie.
- [ ] **Communication jeu → site** (exigée par le sujet) : `Scripts/Net/` + `NetConfig`, `POST /api/runs/*` et `GET /api/live-config` via `AnatidaeProxyWebRequest`. Contrat : [api-contract.md](api-contract.md)
- [x] `thumbnail.png` Kenta (la Sylve, le héros et le logo ; `tools/gen-kenta-assets.py --only thumbnail`) (2026-11-03)
- [ ] `attract.mp4` (≤ 20 s, muet) en option : trailer pour le mode attract de la borne

## Jeu — P1 (gameplay « music level »)
- [x] **Niveau vertical** (ADR-018) : hauteur libre, rampes et course sur le mur (`ramp`), tambours-tremplins (`bounce`), caméra par plans (zoom ×3 à ×6, cadrage), niveau 01 réécrit scène par scène avec simulateur de l'autoplay (2026-11-24)
- [ ] (Lucas) **Tester le niveau vertical à la main** (clavier puis borne) : lisibilité des tambours, des chutes et des changements de zoom ; régler `gen-level-vertical.py`
- [ ] Chutes de plus de ~6 u : en vue 640 px, le sol d'arrivée n'apparaît qu'en cours de chute (la vue fait 9 u de haut). Option : vue encore plus large pour les plongeons, ou chutes en deux paliers
- [x] **Niveau musical, 2ᵉ passe** (docs/level-design-musical.md) : carte du morceau (`tools/analyse-song.py`), actions sur les attaques (grosse caisse → sauts, caisse claire → coups), pièces qui jouent la mélodie et dessinent le chemin, ennemis en vol, tambours en chaîne, caméra large sur rail précalculé sans coupes, brume de fond et piliers ombrés, HUD sans texte au milieu (2026-11-24)
- [ ] `hook` : lianes / crochets (automatique ; pose `vine` déjà extraite)
- [ ] Décor qui joue avec la musique (éléments qui réagissent au temps, foule d'ennemis qui danse au fond)
- [ ] Musique libre de droits **écrite pour le niveau** : idéalement, les sons des actions manquent dans la piste et c'est le joueur qui les joue
- [ ] Tambour : animation d'écrasement au rebond et son dédié (aujourd'hui : son de saut)
- [ ] **Menace qui poursuit** le joueur (mur de son / créature) à l'écran
- [ ] **Télégraphie** : chaque obstacle s'annonce 1 mesure avant (couleur, clignotement, animation)
- [x] Ennemis en hauteur (`lane 1`) à frapper pendant un saut : chauves-souris du niveau 01 (2026-11-24, PR de Lucas)
- [ ] **Écran Config** : 1 ou 2 joueurs
- [ ] **Mode 2 joueurs** hot-seat (P1 puis P2 sur le même morceau, comparaison à la fin)
- [ ] Stems audio : couches (batterie, mélodie) qui montent selon le combo ou la section

## Jeu — P2 (juice / direction artistique)
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
  - [ ] **P2** Obstacles à refaire dans Gemini : `wall_orage` (socle plat), `block_orage` et `post_orage` (fond rectangulaire retiré, contour haché), `block_cite` (copie de l'ancien bloc), `post_echine` (corde en rayures jaunes et noires)
  - [ ] Faire valider le nouveau gabarit par l'équipe, puis **vérifier sur la borne** : lisibilité à 2 m, rendu des héros à ×3 (vue large)
  - [ ] Animations restantes : attente animée (menus), pose de victoire, mort animée ; liane et course sur mur à brancher avec leurs mécaniques
  - [ ] Vignette (`thumbnail.png`) et pages du site : encore avec le Kenta du PDF
  - décors des biomes plus riches (aujourd'hui générés : arbres, rochers, cristaux, volcan, ruines, nuages) : gardiens animés, éléments qui jouent sur le temps (champignons-cymbales, bulles, cristaux en arpège, geysers, aurore, éclairs…)
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
- [ ] **P2** Les 3 héros sont seulement cosmétiques (mêmes sons, même gameplay) : au minimum un son de frappe par héros (caisse claire, flûte, basse) comme dans le PDF

**Visuel**
- [x] (2026-11-24, ADR-020) **P0** Flash blanc plein écran + secousse + son de « checkpoint » à chacune des 10 sections, en double (`section_change` de la chart **et** `RunManager.EnterSection`), alors qu'il n'y a plus ni checkpoint ni changement de monde : ça masque les obstacles en début de section. Garder un seul effet discret, ou le réserver aux vrais drops
- [x] (2026-11-24, ADR-020) **P0** Victoire et défaite ont le même écran : la Sourdine recouvre tout et grise l'image, même sur « BRAVO ! ». Faire une fin heureuse (feu d'artifice, pose de victoire, couleurs)
- [x] (2026-11-24, ADR-021) **P1** Densités de pixels mélangées → décors régénérés en 640×360 (`pixlib.Pix(scale=2)`). Avant : en vue 640 px : le décor (320×180) est agrandi ×2, donc ses pixels font 6 px d'écran contre 3 px pour le niveau et le héros. L'arbre rose du fond est plus gros et plus « cubique » que le premier plan. Dessiner le décor en 640×360, ou revenir à une vue plus serrée
- [x] (2026-11-24, ADR-021) **P1** Piliers de terre → trois tons (bandeau, terre, profondeur `fill_deep`) et détails semés (`deco_*`). Avant : : de grands aplats de briques brun foncé (jusqu'à la moitié de l'écran), tous identiques. Ajouter racines, pierres, mousse, variations de tuiles, dégradé vers le bas
- [x] (2026-11-24, ADR-021) **P1** Trous et chutes → liquide du biome 1 u sous le bord, plus sombre en profondeur. Avant : : le fond d'un trou est vide (couleur de la brume). Montrer le danger : eau, ronces, obscurité ou la Sourdine en bas
- [x] (2026-11-24, ADR-021) **P1** Un seul décor pendant 1 min 43 → ambiance par section (teinte, lumière qui baisse) ; la démo de l'accueil change de monde. Avant : : même arbre rose, même gardien, même ciel. Varier par section (heure, météo, couleurs) même sans changer de biome
- [x] (2026-11-24, ADR-021) **P1** Le gardien (Cerf de pierre) → sprite à part, plus contrasté, yeux ouverts une mesure à chaque section et tout le refrain. Avant : est une tache grise floue derrière la brume, jamais animé : le rendre lisible et le faire réagir aux sections
- [ ] **P2** Pièces qui tournent : les images de profil (1 à 2 px de large) se lisent comme des traits orange parasites dans les arcs. Garder des images plus larges, ou ne pas les faire tourner
- [ ] **P2** `pytest tools/tests` : `test_generated_chart_is_feasible_and_vertical` échoue (`validate` de l'ancien `tools/gen-level-chart.py` refuse des hauteurs, ex. « hauteur 6.0 … beat 66 ») ; déjà le cas sur `main` le 2026-11-25. Mettre le test à jour ou le retirer avec l'ancien générateur
- [ ] **P2** Plateformes flottantes trop fines (2 à 3 px) : elles se lisent comme des lignes. Les épaissir, avec un dessous
- [ ] **P2** Tambours-tremplins dessinés en tonneaux : rien ne dit « rebond ». Peau tendue, couleur vive, flèche ou animation d'appel
- [ ] **P2** Héros petit en vue 640 px (~50 px sur 360, soit 150 px d'écran) : vérifier la lisibilité à 2 m sur la borne
- [ ] **P2** Bandeau (traînée de combo) : une ligne pointillée fine, presque invisible même à ×8
- [ ] **P2** PARFAIT reste à l'endroit où l'action a été jugée : sur un tremplin, le mot apparaît sous le héros qui s'envole. Le faire suivre le héros

**Écrans et interface**
- [x] (2026-11-24, ADR-021) **P1** Accueil → cycle logo, démo jouée (un monde différent à chaque passage), classement ; sol et terre sous le décor des menus. Avant : décor seul et texte. Ni démo jouée (pourtant décrite dans `game-design.md` et `game/CLAUDE.md`), ni héros, ni pièces autour du logo. Pour attirer les passants : alterner logo, démo en autoplay et classement. Mettre la doc à jour selon le choix
- [x] (2026-11-24, ADR-021) **P1** « APPUIE SUR START » → jamais sous 85 %, passe de l'or au blanc ; en bas de l'écran pendant la démo. Avant : il descendait à 55 % d'opacité : jaune sale sur plaque grise, peu lisible au creux de la pulsation
- [x] (2026-11-24, ADR-021) **P1** Classement et saisie du nom → `HighscoreBoard` et `NameEntryScreen` en pixel art (données et envoi par le toolkit), vérifiés avec la borne locale. Avant : : style par défaut du toolkit (police Arial, panneau gris), en rupture avec le pixel art. Le restyler (autorisé par le CLAUDE.md) : police pixel, pièces or / argent / bronze
- [x] (2026-11-24, ADR-021) **P1** Consignes → 3 gestes avec les boutons de la borne, le héros mime sur le temps. Avant : 6 lignes de texte et une image fixe qui change toutes les 1,1 s, sans lien avec le tempo. Trop long pour « compris en 10 s ». Garder 3 gestes avec des pictos de la borne, et le héros qui mime en boucle sur le temps
- [ ] **P2** Choix du héros : grands cadres sombres presque vides, sélection signalée par la seule couleur du cadre, pas de pose de victoire ni de son au déplacement
- [ ] **P2** La frise du HUD a 8 segments (8 biomes) alors que le niveau a 11 sections et un seul monde : la caler sur les sections du niveau
- [x] (2026-11-24, les menus sont aussi en vue large) **P2** Retour à l'accueil après une partie : le zoom de la partie (640 px) n'est pas remis à 320, et l'accueil change d'échelle après la première partie (`GameFlow.Enter(Attract)`)
- [x] (2026-11-24, vérifié en 1920×1080) **P1** Faux problème : le HUD est bien dessiné au-dessus de la Sourdine ; l'impression venait du texte de debug (F9) qui le recouvre en éditeur. Constat initial : le score et le multiplicateur (HUD en haut à gauche) passent sous la Sourdine : la masse et le voile recouvrent la plaque du score, même à ×8. Dessiner le HUD au-dessus de la Sourdine, ou le décaler
- [ ] **P2** Feu d'artifice de victoire en particules simples : gerbes petites, sans fusée ni traînée
- [ ] **P2** Hors 1920×1080 (confirmé le 24/11 : en 1920×1080 dans l'éditeur, HUD et panneaux sont entiers) (fenêtre de l'éditeur), l'interface 320×180 est rognée au cadre du monde : le HUD disparaît et le panneau de fin est coupé. Sur 1920×1080 les deux cadres font la même taille (×3 et ×6) : à confirmer dans un build

**Nouveaux points (après les P1)**
- [ ] **P2** « SILENCE… » s'affiche encore au milieu de l'écran à la mort (`RunHud.HandleDamaged`), contre la règle « rien au milieu »
- [ ] **P2** Liquide des trous parfois hors de l'image quand la caméra anticipe une montée (le sol est alors en bas de l'écran)
- [ ] **P2** En éditeur, l'AFK du toolkit (60 s sans appui) affiche « Retour au menu… » pendant une partie en autoplay : sans effet sur la borne (on y joue), mais l'accueil en cycle (logo, démo, classement) renvoie au menu de la borne après 60 s d'inactivité, comme le veut le toolkit

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
- [ ] L'atlas `LiberationSans SDF - Fallback.asset` change tout seul : ne pas le commiter (`git restore`). Idéalement, activer « Clear Dynamic Data On Build ».
- [ ] Tant que le classement de la borne a moins de 10 scores, tout score > 0 déclenche la saisie du nom (comportement du toolkit)
- [ ] À très bas FPS (onglet en arrière-plan), le perso tombe : le balayage de collision est limité à 3 unités. Sans impact attendu à 60 fps sur la borne.
