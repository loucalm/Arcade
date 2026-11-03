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
