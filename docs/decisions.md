# Journal des décisions

Format : `ADR-NNN — Titre (AAAA-MM-JJ)` : décision, puis pourquoi. Ajouter les nouvelles entrées **en bas**. Ne pas réécrire une ADR : en ajouter une nouvelle qui la remplace.

## Équipe & propriétaires
| Membre | Rôle | Scènes/zones possédées |
|---|---|---|
| Lou Calmes | _rôle à définir_ | |
| Lucas Corrieras | _rôle à définir_ | |
| Jeremy Hordé | _rôle à définir_ | |

## ADR-001 — Unity via le toolkit Anatidae, Built-in RP (2026-09-25)
On part du repo `anatidae-toolkit` (Unity 6000.0.40f1, Built-in RP, ancien Input Manager). Raisons : configuration borne déjà faite (input, WebGL, highscores, retour menu), le plus léger pour le GPU intégré. Le juice sera fait à la main (sprites additifs, shaders simples), sans URP.

## ADR-002 — Monorepo (2026-09-25)
`game/` (Unity) + `web/` (site + serveur) + `docs/`, un CLAUDE.md racine et un CLAUDE.md par dossier. Raison : 3 devs, un seul endroit pour les contrats et les décisions.

## ADR-003 — Web : Node + Express + ws (2026-09-25)
Un seul serveur Node sur le VPS sert le site statique, l'API REST et le WebSocket. Raison : même techno que la borne, WebSocket natif pour le live.

## ADR-004 — Jeu → VPS en REST via le proxy de la borne (2026-09-25)
Le jeu passe par `AnatidaeProxyWebRequest` (`localhost:3000/proxy`). Pas de WebSocket depuis Unity WebGL (il faudrait un plugin jslib, sans garantie que ça marche avec le réseau de la borne). Le temps réel est géré côté serveur, entre le VPS et le site.

## ADR-005 — Pas de Git LFS (2026-09-25)
Assets légers (audio en Vorbis, textures ≤ 2048). Raison : quotas LFS gratuits limités, setup plus simple pour 3 personnes. À revoir si le repo dépasse ~500 Mo.

## ADR-006 — Web en local d'abord, VPS plus tard (2026-09-25)
Le serveur `web/` tourne en local (`http://localhost:8080`, le port 3000 étant pris par la borne locale). Le jeu cible cette URL via `NetConfig`. Passer sur le VPS ne demandera que de changer l'URL et d'ajouter nginx/pm2.

## ADR-007 — Flow d'écrans imposé, scène unique (2026-09-25)
Flow de l'enseignant ([img/flow-ecrans.webp](img/flow-ecrans.webp)) : Attract+HighScore → Explain → Config (facultatif) → Game → Game Over + scores → enregistrement si top 10 → Attract. Implémenté comme une machine à états `GameFlow` dans **une seule scène `Main`**, pour ne pas couper l'audio ni le `Conductor`. Chaque écran est un prefab, ce qui limite les conflits à 3.

## ADR-008 — Migration vers Unity 6000.3.23f1 + MCP Unity (2026-09-25)
Le projet du toolkit (6000.0.40f1) a été ouvert et migré en **6000.3.23f1**. La 6000.0.40f1 bloquait sur les Mac Apple Silicon (Package Manager x86_64 uniquement). Après la migration : console vide, réglages borne intacts (Built-in RP, ancien Input Manager, Web Minimal sans compression, 1920×1080). Plateforme passée en Web, scène `Main` créée avec `AnatidaeInterface`. Package MCP CoplayDev figé en v10.2.0. **Les 3 postes doivent utiliser exactement 6000.3.23f1.** À vérifier tôt : un build Web de cette version tourne bien dans le Firefox ESR de la borne.

## ADR-009 — Collisions à la main, pas de moteur physique (2026-09-25)
Le joueur avance à x = BeatToX(SongBeat) (pas de vélocité physique). Collisions AABB maison contre les `LevelObject`, balayées entre deux frames, Y en physique cinématique réglée en beats. Raisons : déterminisme, synchro parfaite avec l'audio, coût quasi nul sur la borne, autoplay fiable pour valider les charts.

## ADR-010 — Démo en autoplay sur l'écran Attract (2026-09-25)
L'Attract lance le vrai niveau en autoplay (sans perte de cœur), sous le titre. Ça montre le jeu aux passants, et ça réutilise l'autoplay de debug. La boucle de menu `attractSong` ne sert que si le niveau n'a pas de chart.

## ADR-013 — [remplacée par ADR-016] Direction artistique néon filaire façon Rider (2026-10-13)
Remplace le style provisoire en carrés teintés. Tous les sprites (héros filaire à 19 images, ennemis, caisse, mur, laser, lums, sol, rochers, cœurs, logo) sont **générés par `tools/gen-neon-sprites.py`** (Pillow + numpy) : traits turquoise et magenta avec cœur blanc et halo, sur fond bleu-vert sombre. Raisons : cohérence totale du style, aucune compétence de dessin requise, modifiable en changeant des paramètres, pas d'asset externe à licencier. Le héros est animé par images (`HeroAnimator`), la foulée calée sur le beat. Sprites et palette référencés par `Theme_Neon`, donc remplaçables par des dessins maison sans toucher au code. Si la branche `feat/niveau-musical-neon` (DA « Castle Rock », ADR-011) est mergée, cette ADR **remplace** l'ADR-011 pour les visuels : `PlayerRig`, `BeatDance`, `GroundWave` et le décor en code de cette branche sont à retirer ou à adapter.

## ADR-014 — Musique provisoire du niveau : « Main Theme » (Shovel Knight) (2026-11-03)
Demande de l'équipe : le niveau joue le thème principal de Shovel Knight (`Audio/Music/level01_shovel_knight.ogg`, 150 BPM, offset 0,18 s détectés par analyse, tronqué à 103 s = 256 beats). `Song_Level01` + chart `level01_shovel_knight.json` (chart de test jouée en deux passes, 215 événements). **Morceau protégé** : usage provisoire, à remplacer avant diffusion publique (voir `Audio/LICENSES.md`). Le fichier est en OGG (2,6 Mo) : ne jamais commiter le WAV d'origine (31 Mo).

## ADR-015 — Chart générée depuis l'analyse de la musique (2026-11-03)
La chart du niveau 01 est produite par `tools/gen-level-chart.py` (librosa) : attaques mesurées par croche dans trois bandes (grave, médium, aigu), plan de sections calé sur la structure réelle du morceau (introduction, silence, montée, couplet, pont, roulement de grosse caisse, refrain, retombée, final), obstacles posés sur les attaques les plus fortes avec espacement minimal garanti (1 beat, plus après un laser). Le morceau se répétant (beats 32-127 = 152-247), le second passage reprend le même dessin. Les lums suivent la mélodie, avec des arcs au-dessus des sauts. Remplace la chart de test jouée deux fois (ADR-014). À relancer après tout changement de morceau ; le plan (`PLAN`, `SECTIONS`) est à réécrire pour un autre morceau.

## ADR-016 — Direction artistique Kenta : pixel art 240×135, huit biomes, nom du jeu « Kenta » (2026-11-03)
La proposition « Kenta » (PDF de la DA, mergée sur `main`) devient la direction artistique du jeu, **en remplacement de la DA néon Rider** (ADR-013) et de la DA procédurale Castle Rock (ADR-011, branche non mergée). Le jeu s'appelle **Kenta** : namespace `Kenta`, `HighscoreManager.GameName = "Kenta"` (donc nouvelle clé de scores sur la borne), dossier de build `dist/Kenta`, `info.json`, docs.
Choix techniques : (1) **rendu 240×135 px agrandi ×8** par une RenderTexture au filtre Point (`PixelScreen`), monde à 40 px/unité, caméra et parallaxe sur la grille de pixels ; interface en canvas « Screen Space – Camera » dans la même texture ; (2) **les sprites des héros et des ennemis sont ceux du PDF** (extraits par `tools/extract-kenta-pdf.py`, pivot milieu-bas) ; décors, obstacles, pièces, Sourdine, logo et police pixel sont **générés** (`tools/gen-kenta-assets.py`, palettes du PDF) ; (3) **un biome = une section de la chart** (8 × 32 beats), `BiomeTheme` chargé au checkpoint, avec un drop (flash, secousse, changement de décor) ; (4) la physique est réglée pour l'écran réduit : saut 1,5 u en 1 beat, vitesse 8 u/s, hitbox héros 0,5 × 1,0 u, de sorte que franchir un mur de 0,8 u laisse environ 85 ms de marge ; (5) menus Anatidae : le choix du héros prend la place de l'écran « Config » du flow imposé ; la saisie des 3 lettres et le classement restent ceux du toolkit.
Le morceau (150 BPM) ne correspond pas aux « 128 BPM » du PDF : 8 sections de 32 beats = 256 beats = 1 min 43, au lieu de 2 min.

## ADR-018 — Niveau vertical et caméra metteuse en scène (2026-11-24)
(ADR-017 est laissée au « niveau par biome » en cours sur `main`.) Retour de l'équipe : le niveau est « plat et vide, mou et fade ». On vise le ressenti des niveaux musicaux d'un plateformeur moderne, en **réutilisant les visuels existants**.
Choix : (1) **hauteur du sol libre** (−60 … 60 u, avant 0 … 2 u), sol dessiné en piliers (dessus + flanc jusque sous le point le plus bas), trous laissés vides ; mort par chute calculée **localement** (`LevelTerrain.KillY` : sous le sol en dessous, ou sous le plus bas des deux bords d'un trou) ; (2) **rampes** automatiques (`ramp`, sol en pente, raide = course sur le mur) et **tambours-tremplins** (`bounce`, envol de N beats, atterrissage calculé pour tomber pile sur le sol prévu) ; (3) **caméra par plans** pilotée par la chart (`camera`) : zoom par paliers entiers 320/384/480/640 px de large (×6/×5/×4/×3 sur 1920×1080, donc toujours du pixel art net, changement net comme une coupe), place du héros à l'écran, regard vers le terrain devant (montées, chutes), héros toujours dans l'image ; vue par défaut 480 px (×4) au lieu de 320 ; (4) le niveau 01 est **écrit scène par scène** (`tools/gen-level-vertical.py`) sur la structure du morceau, avec un **simulateur** de l'autoplay qui garantit qu'il est faisable et pose les pièces sur la trajectoire. Les niveaux d'autres jeux ne sont pas recopiés : on reprend les mécaniques du genre, pas leurs niveaux.
Réglages de tolérance découverts par le simulateur : (a) le saut était intégré par Euler et perdait ~0,13 u à 60 i/s (plus à 30 i/s) : il est maintenant **intégré exactement** (`y += vy·dt − ½·g·dt²`), donc 1,5 u en 1 beat quel que soit le framerate ; (b) la face d'une marche passe de `beat+0,3` à `beat+0,5` et le mur de `beat+0,3` à `beat+0,36` avec une hitbox indulgente de 0,7 × 0,75 u : un appui 40 ms en avance ou en retard passe partout (vérifié par le simulateur).
Conséquence : l'invariant n° 8 devient « pixel art agrandi d'un facteur entier (×3 à ×6 selon le plan) ». Le décor (320×180) est agrandi ×2 quand la vue dépasse 320 px.

## ADR-019 — Niveau musical : rail de caméra, pièces-mélodie, carte du morceau (2026-11-24)
Suite d'ADR-018 après essai (« mieux mais encore loin, parfois brouillon »). Principes écrits dans [level-design-musical.md](level-design-musical.md). Choix : (1) **plus de coupes de caméra** : une vue large fixe (640 px, ×3), et un **rail** précalculé par le générateur (trajectoire idéale lissée dans les deux sens, contrainte à garder le héros dans l'image) que la caméra suit ; (2) `tools/analyse-song.py` (librosa, lancé une fois) écrit la **carte du morceau** (`tools/data/level01_analysis.json`, versionnée) : le générateur pose les sauts sur la grosse caisse, les coups sur la caisse claire, et donne à chaque pièce la **note de la mélodie** ramenée dans la pentatonique de la tonique ; (3) **ennemis en vol** (chauve-souris du PDF, `LevelTheme.flyer`) à frapper en sautant ; tambours en chaîne ; (4) lisibilité : brume entre le décor et le niveau, piliers assombris sous leur bord, pas de texte au milieu de l'écran, jugement en petit.


## ADR-020 — Corrections P0 de l'audit : multiplicateur à paliers, Sourdine derrière le héros, sections sans flash (2026-11-24)
Suite à l'audit du 24/11 (roadmap). (1) **Multiplicateur à paliers croissants** (×8 à 112 actions de combo, au lieu de 1 + combo/8) et **pièces hors combo** : avant, ×8 arrivait au premier quart du niveau et plus rien ne bougeait ensuite. Calcul sans champ sérialisé nouveau (`RunManager.MultiplierFor`), donc sans toucher la scène. (2) La **Sourdine** se cale sur la place du héros à l'écran (`CameraRig.HeroFrameX`) et ne le recouvre jamais ; elle recule de 8 px par palier. (3) Changement de **section** : un petit coup de caméra (événement `section_change`), plus de flash plein écran ; flash, secousse et son seulement quand le monde change vraiment. (4) **Victoire** distincte de la défaite : pas de Sourdine, feu d'artifice en notes montantes. (5) Niveau 01 : **6 glissades** au lieu d'une.

## ADR-021 — Corrections P1 de l'audit : choix du monde, décors 640×360, accueil en trois temps, classement en pixel art (2026-11-24)
Choix de l'équipe (Lou) sur les P1 de l'audit du 24/11. (1) **Choix du monde** à l'écran du héros (haut/bas), le décor change derrière ; les 8 niveaux restent sur la même chart en attendant le « niveau par biome ». (2) **Décors régénérés en 640×360** (calques 1280×360) : `pixlib.Pix(scale=2)` garde la composition de l'écran 320×180 mais trace à la définition de la vue large, donc les pixels du décor ont la taille de ceux du niveau (plus d'agrandissement ×2). Le **gardien** devient un sprite à part (endormi / éveillé, position dans `guardians.json`, lue par `KentaBuilder`), plus contrasté. (3) **Accueil** en trois temps : logo, démo jouée en autoplay (un monde différent à chaque passage), classement. (4) **Classement et saisie du nom en pixel art** (`HighscoreBoard`, `NameEntryScreen`) : l'interface Arial du toolkit n'est plus affichée, mais les données et l'envoi passent toujours par `Anatidae.HighscoreManager` (`Highscores`, `IsHighscore`, `SetHighscore`), comme l'exige la borne. (5) **Rejouer vite** : dans la minute qui suit une partie, consignes sautées, héros et monde repris ; écrans de fin passables au bouton 1. (6) Niveau : murs à sauter (6), piliers en trois tons avec détails, fond des trous qui montre le danger, ambiance du décor par section.

## ADR-022 — Héros redessinés en marionnette pixel art, proportions ~3,5 têtes (2026-11-24)
Retour de Jeremy : les héros du PDF ont une tête trop grosse pour le corps, la glissade est peu lisible, et on veut des mouvements plus réalistes (bras et jambes qui bougent, bandeau et cheveux qui flottent). Le PDF ne donne qu'une image par pose : les animations tirées de ces images restent figées.
Choix : les héros sont **dessinés par code** à partir d'un squelette (`tools/hero_rig.py`), comme le reste des assets générés. Proportions **~3,5 têtes** (le PDF : ~2), mêmes palettes et mêmes signes distinctifs ; toutes les animations sont des suites de poses (`tools/gen-heroes.py`), et les éléments souples (bandeau, écharpe, queue de cheval, mèches) sont **simulés à la génération** puis figés dans les images (aucun coût en jeu). Glissade refaite façon baseball (couché, jambe tendue, ~28 px de haut pour une boîte de 20 px), avec des images pour se jeter au sol et se relever. Style : contour noir autour de la silhouette seulement, lignes intérieures dans un ton sombre (« sel-out »), membres effilés, mèches courbes, pan de veste qui flotte.
Conséquences : écart assumé avec le PDF (`da-kenta.md` § 4 le signale) ; les sprites des héros du PDF restent dans `Art/Kenta/Pdf/` (vignette) ; `tools/gen-hero-run.py` est remplacé. **À valider par l'équipe** et sur la borne (lisibilité à 2 m).

## ADR-023 — Retrait de la Sourdine (2026-11-24)
La Sourdine (mur de silence au bord gauche, `FX/Sourdine`) servait à montrer l'état du combo et à recouvrir l'écran au game over. Avec la mort subite (plus de cœurs ni de checkpoints), elle n'a plus de rôle, elle grisait le héros (audit du 24/11) et Jeremy n'en veut plus.
Remplace la partie « Sourdine derrière le héros » d'ADR-020. Choix : retirée du jeu (script, test, champs `LevelTheme.sourdine` / `sourdineVeil`, construction dans `KentaBuilder`, images `Gen/ui/sourdine.png` et `veil.png` et leurs générateurs). L'image extraite du PDF (`Art/Kenta/Pdf/sourdine.png`) reste, comme matière première. La menace qui justifie la course forcée n'a plus de représentation à l'écran : à repenser si l'équipe le souhaite.

## ADR-024 — Gardiens repris du PDF, endormis ; décors mis au niveau des maquettes (2026-11-25)
Retour de Lou : décors en dessous de la DA, gardiens générés « pas beaux », animation des yeux à retirer. (1) Les **8 gardiens sont découpés dans les maquettes de biome du PDF** (`tools/extract-guardians.py` : silhouette tracée, pierre recoloriée sous les arbres et pièces, corps prolongé jusqu'au sol, ×2 pour avoir la taille de pixel du décor). (2) Le gardien **dort toujours** : plus de sprite « éveillé », plus de `BiomeBackdrop.Wake` ni de `BiomeTheme.guardianAwake`. (3) Il est dessiné **entre le lointain et le plan moyen** (comme dans les maquettes) et les forêts sont baissées pour dégager son visage. (4) Décors générés mis à niveau : ombrage par forme (`pixlib.Shape`, `shade_into`, `canopy`, `cloud`), nuages, forêts denses, flanc en terre et pierres. Les héros (retravaillés par Jérémy, ADR-022) ne sont pas touchés. Essai de génération par Gemini (Antigravity, sous-agent `image-generator`) le 25/11 : service en erreur HTTP 500, à retenter pour nettoyer les gardiens (parties cachées dans les maquettes).

