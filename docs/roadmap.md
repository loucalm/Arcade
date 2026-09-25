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
- [x] (Lucas) Niveau complet « Neon Rock » : chart 200 beats / 8 sections, 0 chute en autoplay, `tools/check-chart.py` (2026-09-25)
- [x] (Lucas) Direction artistique néon façon Castle Rock : héros « sans membres » animé en code, ennemis qui dansent, lums ailés, château en 3 calques, horizon en feu (2026-09-25)

## Jeu — P0 (rendu / borne)
- [ ] **Tester sur la vraie borne** : 60 fps dans Firefox ESR, manettes et dead zone, bouton blanc, AFK 60 s, et surtout **l'audio démarre-t-il après un appui manette ?** (politique d'autoplay du navigateur)
- [ ] **Tester le ressenti dans Firefox** : durée et hauteur du saut, fenêtres de tolérance, latence des sons quantifiés (jusqu'à ~60 ms)
- [ ] **Réduire le build** (`Build.wasm` ≈ 31 Mo) : Managed Stripping Level = High, IL2CPP « Optimize Size », retirer les packages inutiles (navigation, multiplayer center, timeline, modules VR/XR…)
- [ ] **Vraie musique** (piste provisoire générée : `Audio/Music/placeholder_rock_120.wav`, 120 BPM) originale ou libre de droits (110–160 BPM, sections marquées), avec sa **version 8-bit** au même BPM + licence dans `Audio/LICENSES.md`
- [ ] **Chart du vrai niveau** : recaler `level01_neon_rock.json` (BPM, offset, sections) sur la vraie musique, puis `tools/check-chart.py` + autoplay (0 chute)
- [ ] **Tour REMIX 8-bit** : à la fin du morceau, on reboucle sur la même chart avec `remixClip` et des perturbations visuelles croissantes, jusqu'à la mort. C'est la « difficulté croissante » exigée. Aujourd'hui, fin du morceau = fin de partie.
- [ ] **Communication jeu → site** (exigée par le sujet) : `Scripts/Net/` + `NetConfig`, `POST /api/runs/*` et `GET /api/live-config` via `AnatidaeProxyWebRequest`. Contrat : [api-contract.md](api-contract.md)
- [ ] `thumbnail.png` définitive (carrée), + `attract.mp4` (≤ 20 s, muet) en option

## Jeu — P1 (gameplay « music level »)
- [x] (Lucas) `wall_run` : course automatique sur une bosse courbe (2026-09-25)
- [x] (Lucas) `hook` : lianes automatiques, arc puis lâcher (2026-09-25)
- [x] (Lucas) **Menace qui poursuit** : mur de flammes au bord gauche (`FX/Chaser`) (2026-09-25)
- [x] (Lucas) **Télégraphie** : flash et préparation des obstacles pendant la mesure qui précède (`FX/Telegraph`) (2026-09-25)
- [ ] Foule d'ennemis qui dansent en arrière-plan (comme les crapauds de Castle Rock)
- [ ] Rampe du wall run lisse (aujourd'hui en escalier de colonnes)
- [ ] Ennemis en hauteur (`lane 1`) à frapper pendant un saut
- [ ] **Écran Config** : 1 ou 2 joueurs
- [ ] **Mode 2 joueurs** hot-seat (P1 puis P2 sur le même morceau, comparaison à la fin)
- [ ] Stems audio : couches (batterie, mélodie) qui montent selon le combo ou la section

## Jeu — P2 (juice / direction artistique)
- [ ] Direction artistique : remplacer à terme les formes par des sprites maison (les champs de sprites de `Theme_Neon` sont prêts)
- [x] (Lucas) Animations du perso en code (`Player/PlayerRig`) : course, saut, glissade, frappe, liane (2026-09-25)
- [ ] Textes flottants (+50, PARFAIT) dans le monde, traînée du perso à fort multiplicateur
- [x] (Lucas) Mise en scène des drops : effondrement du décor, zoom caméra (`FX/DropDirector`) (2026-09-25)
- [ ] Effets remix : brouillage, image inversée, zones sombres (shaders simples, compatibles GPU intégré)
- [ ] Écran « Comment jouer » animé (pictos des boutons au lieu du texte)
- [ ] Police et identité visuelle du jeu (titre, HUD)

## Outils
- [ ] Outil pour écrire une chart plus vite (tap au rythme → JSON, ou visualiseur de chart)
- [x] (Lucas) Vérificateur de faisabilité d'une chart : `tools/check-chart.py` (2026-09-25)
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
