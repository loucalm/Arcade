# Kenta — un niveau par monde, difficulté croissante (proposition)

Date : 2026-11-26. Statut : **retenue par Lucas (ADR-035)**, à partager avec Lou et Jeremy. Branche : `feat/niveaux-par-monde`.
Demande : « un die and retry en rythme ; régler d'abord le level design et la DA ; puis un niveau par monde (aujourd'hui seulement cosmétiques), choisis dès le lancement, avec une difficulté croissante : niveau 1 facile, presque pas de pièges, pas d'écran qui bouge, peu de monstres, juste les mécaniques du jeu ; niveau 2 plus de mécaniques et plus de vitesse ; puis 3, 4, 5 (écran qui se retourne, etc.) ; pourquoi pas un niveau 6 qui cumule tout. Chaque niveau a sa musique. » Pas d'idée de musique : la short-list est plus bas.

## 1. Constat : pourquoi le niveau actuel ne marche pas encore
Le niveau 01 (« Main Theme », 256 temps) contient **toutes** les mécaniques du jeu : marches, trous, frappes, cuirassés, notes de saut, lianes, tambours, rampes, gravité inversée, arrière-plan, puis écrans en miroir, retournés et dédoublés. Les 8 mondes rejouent ce même niveau ; seule la peau change.
- **Pas de progression.** Un nouveau joueur reçoit tout en 1 min 40. Le niveau ne peut pas être à la fois le tutoriel et le final : c'est la source du « brouillon ».
- **Gestes répétitifs** : les couplets 1 et 2 sont identiques, avec peu de variété pour monter et descendre (grand audit, A3).
- **Tout tombe sur le temps** : 139 PARFAIT, 0 BIEN en autoplay, sans contretemps ni double-croche. Le rythme ne demande jamais rien (E4).
- **Tout le level design est écrit sur une musique protégée** (Shovel Knight), qu'il faudra de toute façon remplacer (E2) : polir ce niveau maintenant serait du travail perdu.
- **Le simulateur et le jeu divergent** : l'autoplay de Unity ramasse 372 pièces sur 395, le simulateur 395. Or c'est le simulateur qui garantit qu'un niveau est faisable, à 120 ms de marge.
- **DA** : héros et ennemis perdus sur l'Orage et les Échos, pièces de la couleur des arbres jaunes (B2) ; arrière-plan peu lisible (D3) ; tambours lus comme des tonneaux ; rampes en escalier de colonnes ; calque proche répétitif (C6) ; sprites provisoires (note de saut, portail, nœud de liane) ; obstacles de l'Orage, de la Cité et de l'Échine à refaire dans Gemini.

**Idée directrice** : on étale les mécaniques sur les niveaux. Chaque niveau enseigne **une** famille de mécaniques et réutilise les précédentes. Le niveau 01 actuel devient le brouillon du **niveau 6** (le cumul), qu'il est déjà.

## 2. La progression : 6 niveaux
Tout reste débloqué dès le lancement (c'est déjà le cas, ADR-032). 10 vies et un point de reprise par phrase de 8 temps partout (ADR-033).

| N° | Monde | BPM visé | Nouveau dans ce niveau | Effets d'écran | Marge de survie | Étoiles |
|---|---|---|---|---|---|---|
| 1 | **La Sylve** | 110-120 | sauter (trous, marches, souches), frapper (ennemi au sol), suivre les pièces | aucun ; vue fixe 480 px | ≥ 160 ms (180 visé : un saut en retard plafonne vers 170 ms à 117 BPM) | ★ |
| 2 | **Le Bayou** | 120-130 | **lianes** au-dessus du marais, **cuirassés** (écraser, appui au rebond) | aucun | ≥ 150 ms | ★★ |
| 3 | **Les Échos** | 130-140 | **chauves-souris** à frapper en vol (sur les lianes et entre deux notes : sur un rebond de cuirassé, elles passaient sous le héros ou hors de l'image), **notes de saut** au-dessus des gouffres, **gravité inversée** (plafond de la grotte) | aucun ; plan large seulement si un ennemi se verrait trop tard (`wide_on_refrains=False`) | ≥ 120 ms | ★★★ |
| 4 | **La Forge** | 140-150 | **tambours** en chaîne, **rampes et course sur le mur**, grands plongeons, passage dans l'**arrière-plan** | plans larges (640 px) sur les envolées | ≥ 120 ms | ★★★★ |
| 5 | **Le Givre** | 145-155 | l'**écran se retourne** : miroir (le reflet de la glace), tête en bas, deux écrans | miroir, retourné, 2 écrans | ≥ 120 ms | ★★★★ |
| 6 | **Le Grand Orage** | 155-170 | **tout**, enchaîné, avec contretemps et doubles croches | tout, plus le grésillement des éclairs | ≥ 120 ms | ★★★★★ |

En réserve : **L'Échine** et **La Cité sourde**. On les retire de la liste pour l'instant ; elles pourront servir de niveaux bonus plus tard. Avec 5 niveaux seulement, on fusionne 4 et 5 (La Forge garde les écrans qui se retournent).

### Ce qui rend chaque niveau plus dur que le précédent
1. **Le tempo** (le levier principal) : plus de temps par seconde, donc plus d'actions par seconde.
2. **La densité** (actions par mesure, en moyenne) : environ 1 au niveau 1, 1,5 au 2, 2 au 3, 2,5 aux 4 et 5, 3 au 6.
3. **Les valeurs rythmiques** : niveau 1, des noires sur les temps forts ; niveau 2, sur tous les temps ; niveau 3, des croches ; niveau 4, des croches enchaînées ; niveau 5, des contretemps ; niveau 6, des doubles croches ponctuelles. C'est ce qui fait apparaître les BIEN et les BOF (E4).
4. **La lecture** : niveaux 1 à 3 à plat ou presque, en vue fixe ; la verticalité, l'arrière-plan et les écrans détraqués arrivent ensuite.
5. **La vitesse de course suit le tempo** : 3,2 u par temps (8 u/s à 150 BPM), donc de 6,24 u/s au niveau 1 à 9,28 u/s au niveau 6. Les tolérances du jeu ont été réglées pour cette distance par temps : à 117 BPM et 8 u/s, une frappe 150 ms en avance rate l'ennemi (simulateur, 2026-11-26). La vue montre toujours la même part de mesure devant le héros.

La difficulté monte aussi **dans** chaque niveau, section par section : c'est la « difficulté croissante » exigée par l'enseignant. Le tour REMIX 8-bit devient un bonus (P2), à décider.

## 3. Règles d'écriture d'un niveau (gabarit commun)
- **Une fiche par niveau** (en tête de son fichier de génération) : morceau, BPM, offset, sections, mécaniques autorisées, marge minimale en ms, densité visée, vue, effets autorisés. Le générateur **refuse** une mécanique hors de la fiche.
- **Une mécanique s'enseigne en 4 temps** : 1) seule, avec de l'air autour ; 2) répétée ; 3) variée (plus haut, plus vite, à contretemps) ; 4) combinée avec un geste déjà appris, au refrain : c'est le moment signature du niveau.
- **Le niveau suit le morceau** (docs/level-design-musical.md) : grosse caisse → sauts, caisse claire → frappes, notes tenues → lianes et envolées ; les pièces jouent la mélodie et dessinent le chemin. Le passage 2 du morceau rejoue le passage 1 en plus dur.
- **Durée** : 1 min 15 à 1 min 30 pour le niveau 1, 1 min 30 à 2 min pour les niveaux 2 à 5, 2 min à 2 min 30 pour le 6. Le morceau est coupé sur une fin de phrase, avec un fondu.
- **Vérifié par le simulateur** : le niveau est fini en autoplay, la marge de la fiche est tenue sur chaque action et chaque point de reprise est rejoué.
- **Testé à la main** par quelqu'un qui ne connaît pas le niveau. Niveau 1 : compris en 10 s et fini à la première partie ; niveau 6 : fini en 3 à 5 parties.

## 4. Musique : où chercher, comment choisir
**Critères** :
- licence CC0 ou CC BY : pas de NC ni de SA, puisque le site sera public ;
- **tempo constant** : chiptune ou musique faite au tracker, idéal ;
- batterie bien audible (grosse caisse, caisse claire) pour `analyse-song.py` ;
- 1 min 30 à 2 min 30, ou une boucle jouable deux fois ;
- sections claires (intro, couplet, refrain, break) ;
- ambiance du monde, BPM dans la case du niveau.

**Short-list (licences vérifiées le 2026-11-26)** :
| Source | Licence | Pourquoi |
|---|---|---|
| **Eric Skiff — *Resistor Anthems*** (18 titres, composés sur Game Boy : tempo parfaitement constant), [ericskiff.com/music](https://ericskiff.com/music/) | **CC BY 4.0** (crédit : « Music: Eric Skiff - Titre - Resistor Anthems - Available at http://EricSkiff.com/music ») | Un seul album pour plusieurs niveaux, donc un son cohérent ; à écouter en premier : *A Night Of Dizzy Spells*, *Underclocked*, *Chibi Ninja*, *Come and Find Me*, *Ascending*, *Arpanauts*, *We're the Resistors*, *Jumpshot*, *Digital Native* |
| **Juhani Junkala — *4 Chiptunes (Adventure)*** et ***5 Chiptunes (Action)***, [OpenGameArt](https://opengameart.org/content/4-chiptunes-adventure), [Action](https://opengameart.org/content/5-chiptunes-action) | **CC0** | Boucles parfaites, WAV ; *Adventure* est décrit « pour les plateformeurs légers » (niveaux 1 et 2), *Action* pour les niveaux 4 à 6 |
| **Abstraction — *Three Red Hearts*** (Tallbeard Studios), [itch.io](https://tallbeard.itch.io/three-red-hearts-prepare-to-dev) | **CC0** (crédit demandé, pas exigé) | Chiptune moderne, plusieurs ambiances |
| **HoliznaCC0 — *ChipTunes***, [Free Music Archive](https://freemusicarchive.org/music/holiznacc0/chiptunes) | **CC0** | Réserve |
| **BoxCat Games — *Nameless: The Hackers RPG***, [Free Music Archive](https://freemusicarchive.org/music/BoxCat_Games/Nameless_the_Hackers_RPG_Soundtrack) | CC BY (à vérifier titre par titre) | 29 chiptunes « funky » |
| Kevin MacLeod (incompetech), Pixabay Music | CC BY 4.0 ; licence Pixabay (sans crédit, autorisée dans un jeu, pas en fichier seul) | Réserve |
Écartés : les bandes originales de jeux (protégées, comme Shovel Knight aujourd'hui) et la musique générée par IA (droits flous pour une diffusion publique).
**À terme, l'idéal** : composer nos morceaux avec BeepBox ou JummBox. Le morceau appartient à son auteur, et l'export canal par canal permet de retirer de la piste les sons des actions, pour que ce soit le joueur qui les joue.

**Méthode de choix** :
1. Télécharger la short-list dans `tools/music-candidates/` (gitignoré).
2. Mesurer avec `analyse-song.py` le BPM, la stabilité du tempo, la durée et la netteté de la batterie : on obtient un tableau.
3. Écouter en équipe et placer un morceau par niveau, du plus lent au plus rapide.
4. Noter chaque licence dans `Audio/LICENSES.md`, afficher le crédit sur la carte du niveau (écran de sélection) et sur le site.

### Mesures du 2026-11-26 (étapes 1 et 2 faites)
Téléchargés dans `tools/music-candidates/` (gitignoré) : *Resistor Anthems* (17 titres, version 2018 de l'archive), *4 Chiptunes (Adventure)* et *5 Chiptunes (Action)*. Le fichier `INFO.txt` de chaque pack Junkala confirme la licence CC0. Abstraction n'a pas pu être récupéré : itch.io ne donne pas de lien direct, il faut le télécharger à la main.
Mesure : BPM au centième par balayage du tempo et de la phase sur tout le morceau. « Dérive » = décalage du temps entre les deux moitiés du morceau : moins de 15 ms veut dire un tempo fixe et un BPM juste.
| Morceau | BPM | Durée | Dérive | Remarque |
|---|---|---|---|---|
| Skiff — *A Night Of Dizzy Spells* | 101,0 | 1:54 | 8 ms | |
| Skiff — *We're all under the stars* / *Behind the waterfall…* | 101,0 / 99,8 | 4:33 / 3:51 | < 13 ms | longs, peu de batterie |
| Junkala — *Level 2* / *Level 3* (Action) | 105,5 | 1:13 / 1:22 | < 10 ms | boucles |
| Skiff — *Searching* / *Come and Find Me* (+ B mix) | 110,5 / 110,9 | 2:20 / 3:20 | < 7 ms | *Come and Find Me* : très peu de batterie |
| **Skiff — *We're the Resistors*** | **117,0** | 2:22 | 3 ms | grosse caisse la plus nette de l'album |
| Skiff — *Arpanauts*, *HHavok (intro + main)*, *Digital Native*, *Prologue* | ≈ 120 | 1:08 à 3:25 | < 9 ms | sans doute 120 exactement |
| Skiff — *Underclocked*, *All of Us*, *Ascending* ; Junkala — *Level 1*, *Ending* | ≈ 114-121 | | > 100 ms | tempo à revérifier à l'oreille |
| **Junkala — *Stage 1*** (Adventure) | **140,0** | 0:41 (boucle) | 3 ms | attaque très nette ; à jouer 3 fois |
| **Junkala — *Stage 2*** (Adventure) | **154,0** | 0:56 (boucle) | 0 ms | à jouer 2 fois |
| **Skiff — *Chibi Ninja*** | **161,7** | 2:03 | 11 ms | une partie en demi-temps |
| **Junkala — *Boss Fight*** (Adventure) | **174,0** | 1:12 (boucle) | 1 ms | une partie en demi-temps ; à jouer 2 fois |
| Junkala — *Stage Select* | 180,0 | 0:21 | 3 ms | trop court |
| Skiff — *Jumpshot* | 182,8 (ou 91,4 ressenti) | 2:03 | 3 ms | réserve pour le niveau 6 |

**Échelle proposée** (à valider **à l'écoute** : je n'ai mesuré que le tempo, pas l'ambiance) :
| Niveau | Morceau | BPM |
|---|---|---|
| 1 Sylve | *We're the Resistors* (Skiff) | 117 |
| 2 Bayou | *Arpanauts* ou *HHavok-main* (Skiff) | 120 |
| 3 Échos | *Stage 1* (Junkala) | 140 |
| 4 Forge | *Stage 2* (Junkala) | 154 |
| 5 Givre | *Chibi Ninja* (Skiff) | 162 |
| 6 Grand Orage | *Boss Fight* (Junkala), ou *Jumpshot* (Skiff) | 174 (183) |
Il n'y a rien entre 121 et 140 BPM. Si le niveau 2 doit aller plus vite que le 1, on peut prendre pour le niveau 1 un morceau à ≈ 110 (*Searching*).

## 5. Plan de travail
**Étape 0 — Décider (une réunion)** : 6 ou 5 niveaux, monde de chaque niveau, musiques, cette grille de difficulté, sort du tour REMIX. Les choix deviennent un ADR.

**Étape 1 — Outils, avant d'écrire un niveau** (une PR par point) :
1. Découper `tools/gen-level-vertical.py` (1 355 lignes, écrit pour un seul morceau) en un **moteur commun** (physique, monde, simulateur, rail, pièces, points de reprise, contrôles) et **un fichier par niveau** (`tools/levels/n1_sylve.py` …) qui porte sa fiche et ses scènes. Le niveau 01 actuel est porté tel quel, et sa chart doit ressortir à l'identique.
2. **Marges et fenêtres en ms**, et non plus en temps : avec un BPM qui change, 0,3 temps ne fait plus 120 ms.
3. `analyse-song.py` mesure aussi le BPM et l'offset et propose des sections ; une carte par morceau (`tools/data/<niveau>_analysis.json`).
4. **Corriger l'écart entre l'autoplay du jeu et le simulateur** (372 pièces sur 395) : sans ça, la garantie « faisable à 120 ms » ne vaut rien sur 6 niveaux.
5. Unity (MCP ou `KentaBuilder`, jamais à la main) : catalogue de 6 niveaux, étoiles, crédit du morceau sur la carte, `SongData` et chart propres à chaque niveau.

**Étape 2 — Niveau 1 étalon (La Sylve)** : l'écrire, le tester à la main (clavier, puis borne), le faire tester par 2 ou 3 personnes extérieures, puis ajuster la grille et le gabarit. Sa DA doit être irréprochable : c'est la vitrine.

**Étape 3 — Niveaux 2 à 5**, un par PR et dans l'ordre (chacun s'appuie sur ce que le précédent a appris), avec le même cycle : écrire, simuler, tester à la main.

**Mise à jour du 2026-11-27 (Lucas)** : le niveau 4 est écrit par Jeremy ; *Main Theme* (toutes les mécaniques) passe en niveau 5.

**Mise à jour du 2026-11-26 (Lucas)** : en attendant, le niveau 4 joue le niveau de base (toutes les mécaniques, *Main Theme*) pour monter tout de suite en difficulté après les Échos ; il sera réécrit sur *Stage 2*.
**Mise à jour du 2026-11-26 (Jeremy)** : niveau 4 écrit sur *Fire Maze* de JadeWizard (CC BY 4.0, 151,3 BPM : *Stage 2* ne sonnait pas assez volcan ; `tools/levels/n4_forge.py`) ; le niveau de base ne sert plus qu'aux niveaux 5 à 8.

**Étape 4 — Niveau 6 (Grand Orage)** : réécrire le niveau 01 actuel sur sa nouvelle musique, avec les contretemps et les doubles croches.

**En parallèle — la DA par monde, dans l'ordre des niveaux** :
- transversal : règle de couleur (héros, ennemis et pièces jamais dans les couleurs du fond), vérifiée par un script sur les palettes ; tambours qui disent « rebond » ; tuiles de pente ; calque proche plus varié ;
- niveau 2 : nœud de liane, eau du marais ;
- niveau 3 : contraste dans la grotte, plafond dessiné comme un sol, note de saut, portail de gravité ;
- niveau 4 : arrière-plan lisible (D3), lave ;
- niveau 6 : obstacles de l'Orage à refaire dans Gemini, contraste.

**Étape 5 — Borne** : test complet, réglage des vies et des étoiles, choix du score envoyé à la liste officielle.

Pendant la transition, le jeu reste jouable : un niveau qui n'est pas encore écrit garde la chart actuelle.

## 6. Risques
- **Le temps** : 6 niveaux, c'est 6 fois écrire, simuler et tester. D'où l'outillage d'abord et le niveau 1 comme étalon. Si ça coince, 4 niveaux bien faits valent mieux que 6 moyens.
- **Une musique mal coupée ou trop libre** (tempo qui flotte, longue intro) : on la mesure avant de la choisir.
- **CC BY** : le crédit doit apparaître dans le jeu et sur le site.
- **La difficulté réelle** se mesure seulement avec des joueurs extérieurs, pas avec l'autoplay.
