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
- [ ] Pont 2 et roulement 2 (184-212) : passer à la règle des 120 ms et y mettre les nouvelles mécaniques
- [ ] Portail de gravité : son dédié et sprite (aujourd'hui deux colonnes de pixels)
- [ ] Décor qui joue avec la musique (éléments qui réagissent au temps, foule d'ennemis qui danse au fond)
- [ ] Musique libre de droits **écrite pour le niveau** : idéalement, les sons des actions manquent dans la piste et c'est le joueur qui les joue
- [ ] Tambour : animation d'écrasement au rebond et son dédié (aujourd'hui : son de saut)
- [ ] **Menace qui poursuit** le joueur (mur de son / créature) à l'écran
- [ ] **Télégraphie** : chaque obstacle s'annonce 1 mesure avant (couleur, clignotement, animation)
- [x] Ennemis en hauteur (`lane 1`) à frapper pendant un saut : chauves-souris du niveau 01 (2026-11-24, PR de Lucas)
- [ ] **Écran Config** : 1 ou 2 joueurs
- [ ] (Lou) **Sélection de niveau** (en cours, branche `feat/selection-niveau`) : un niveau = un thème + un morceau + une chart, extrait du morceau au survol, record par niveau ; **classement par niveau** (top 10 dans `ExtradataManager`, le score part aussi dans la liste officielle de la borne). Provisoire : les 8 niveaux gardent le même morceau et la même chart
- [ ] **Contenu unique par niveau** : un morceau et une chart propres à chacun des 8 niveaux (aujourd'hui tous sur « Main Theme » et `level01_shovel_knight.json`). Un thème par niveau, sans changement de monde en cours de partie
- [ ] **États du thème** au fil d'un niveau : pluie, nuit, lever du jour… Visuels par biome (particules, palette, ciel) + un champ par section dans la chart, appliqué au décor par `RunManager`. À écrire avec la chart propre à chaque niveau (un orage se cale sur un drop du morceau)
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
- [ ] **P2** Les 3 héros sont seulement cosmétiques (mêmes sons, même gameplay) : au minimum un son de frappe par héros (caisse claire, flûte, basse) comme dans le PDF

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
- [ ] **P2** Plateformes flottantes trop fines (2 à 3 px) : elles se lisent comme des lignes. Les épaissir, avec un dessous
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
- [ ] **P2** La frise du HUD a 8 segments (8 biomes) alors que le niveau a 11 sections et un seul monde : la caler sur les sections du niveau
- [x] (2026-11-24, les menus sont aussi en vue large) **P2** Retour à l'accueil après une partie : le zoom de la partie (640 px) n'est pas remis à 320, et l'accueil change d'échelle après la première partie (`GameFlow.Enter(Attract)`)
- [x] (2026-11-24, vérifié en 1920×1080) **P1** Faux problème : le HUD est bien dessiné au-dessus de la Sourdine ; l'impression venait du texte de debug (F9) qui le recouvre en éditeur. Constat initial : le score et le multiplicateur (HUD en haut à gauche) passent sous la Sourdine : la masse et le voile recouvrent la plaque du score, même à ×8. Dessiner le HUD au-dessus de la Sourdine, ou le décaler
- [ ] **P2** Feu d'artifice de victoire en particules simples : gerbes petites, sans fusée ni traînée
- [ ] **P2** Hors 1920×1080 (confirmé le 24/11 : en 1920×1080 dans l'éditeur, HUD et panneaux sont entiers) (fenêtre de l'éditeur), l'interface 320×180 est rognée au cadre du monde : le HUD disparaît et le panneau de fin est coupé. Sur 1920×1080 les deux cadres font la même taille (×3 et ×6) : à confirmer dans un build

**Nouveaux points (après les P1)**
- [ ] **P2** « SILENCE… » s'affiche encore au milieu de l'écran à la mort (`RunHud.HandleDamaged`), contre la règle « rien au milieu »
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
