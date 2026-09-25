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

## ADR-011 — Direction artistique néon procédurale, façon Castle Rock (2026-09-25)
Tous les visuels sont construits en code à partir des formes de `Theme_Neon` (carré, cercle, halo, triangle, anneau, étincelle, arrondi, dégradé), sans sprites externes : héros « sans membres » (`PlayerRig`), éléments du niveau (`LevelBuilder`), décor (`Backdrop`). Palette de Castle Rock : horizon en feu et château en silhouettes (chaud, derrière), gameplay néon cyan/jaune/rose (froid, devant). Un essai avec le pack Kenney (CC0) a été abandonné : trop éloigné du style. Les champs de sprites du thème restent optionnels pour de futurs dessins maison.

## ADR-012 — Lianes et wall run automatiques (2026-09-25)
`hook` et `wall_run` sont des trajectoires imposées (y = fonction du beat), sans bouton, comme dans les niveaux musicaux de Rayman Legends. Le joueur garde x = BeatToX(SongBeat). Contraintes de chart dans [chart-format.md](chart-format.md) et vérifiées par `tools/check-chart.py`.

