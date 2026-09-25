# Rythme Runner — CLAUDE.md (racine)

Jeu d'arcade **runner/plateformeur rythmique** (façon « music levels » de Rayman Legends) pour la borne MMI **Anatidae**, plus un **site one-page** sur VPS qui communique avec le jeu.
Équipe : **Lou Calmes, Lucas Corrieras, Jeremy Hordé** (rôles : voir [docs/decisions.md](docs/decisions.md)), tous sous Claude Code. Langue du projet : **français** (docs, commits, UI). Code : identifiants en anglais.

Livrables : 1) le jeu Unity WebGL déposé sur la borne, 2) le site (présentation, lore, mini-blog, lien live avec la borne).

## Contraintes borne — NON NÉGOCIABLES
Toute PR qui casse un de ces points est refusée. Détails : [docs/anatidae.md](docs/anatidae.md).
- [ ] Résolution **1920×1080**, WebGL, **Firefox ESR**, i5-7200U + **GPU intégré** → sobriété graphique.
- [ ] **Bouton blanc** (`Coin`) → retour menu en ≤ 1,5 s. **60 s sans input** → retour menu. Géré par `MenuManager` du toolkit : ne pas le retirer des scènes.
- [ ] **Dead zone** sur les joysticks (repos ≈ `-0.00392`) : passer par `ArcadeInput`, jamais `Input.GetAxis` directement.
- [ ] Dossier de build = `index.html` + `thumbnail.png` (carré) + `info.json`. Nom du dossier = `HighscoreManager.GameName` = **`RythmeRunner`**.
- [ ] Score + saisie de nom (3 caractères) en fin de partie via `Anatidae.HighscoreManager`.
- [ ] 1 ou 2 joueurs, **3 boutons max** (on utilise B1 = saut, B2 = frappe, joystick bas = glissade).
- [ ] Jeu compréhensible en 10 s, difficulté croissante, partie de quelques minutes max.
- [ ] **Flow d'écrans imposé** : Attract+HighScore → Explain → Config (facultatif) → Game → Game Over + scores → enregistrement si top 10 → Attract. Voir [docs/game-design.md](docs/game-design.md#écrans-flow-imposé-par-lenseignant).

## Invariants du système rythmique
Détails : [docs/game-design.md](docs/game-design.md), [docs/chart-format.md](docs/chart-format.md).
1. **L'horloge maître est l'audio** (`Conductor.SongTime`). Aucune logique de rythme ne doit se baser uniquement sur `Time.deltaTime`.
2. **Vitesse de course constante** : `x = BeatToSeconds(beat) * runSpeed`. Le joueur ne ralentit jamais.
3. Le niveau est **généré à partir d'une chart JSON**. On ne place rien à la main en dur dans les scènes de niveau.
4. Les sons d'action sont **quantifiés à la double-croche** la plus proche. La tolérance est celle d'un plateformeur, pas d'un jeu de rythme.
5. Tout ce qui réagit au tempo s'abonne à `Conductor.OnBeat(int beat)`. Pas de minuteries locales.
6. Checkpoint = `{beat, x}`. Respawn = seek audio sur `BeatToSeconds(beat)`, puis reprise.
7. Sur WebGL, l'AudioMixer ne gère que le volume : **aucun effet DSP**. Le remix 8-bit est un clip séparé.

## Carte du repo
| Chemin | Contenu | Règles |
|---|---|---|
| `game/` | Projet Unity **6000.3.23f1** (base = anatidae-toolkit) | [game/CLAUDE.md](game/CLAUDE.md) |
| `web/` | Serveur Node (Express + ws) + site one-page + blog | [web/CLAUDE.md](web/CLAUDE.md) |
| `docs/` | Références : borne, game design, charts, API, décisions | à lire avant de coder une feature |
| `dist/RythmeRunner/` | Build prêt pour la borne (gitignoré) | généré, jamais édité à la main |
| `tools/anatidae-arcade/` | Clone local du serveur de la borne (gitignoré) | pour tester |

## Commandes clés
```bash
# Borne en local (une fois) : clone du serveur Anatidae
git clone https://github.com/XariusExcl/anatidae-arcade tools/anatidae-arcade && (cd tools/anatidae-arcade && npm install)
# Lancer la borne locale → http://localhost:3000 (copier dist/RythmeRunner dans tools/anatidae-arcade/public/)
cd tools/anatidae-arcade && node server.js
# Site + API en local → http://localhost:8080 (VPS plus tard, ADR-006)
cd web && npm run dev
```
Build Unity : File > Build Profiles > Web, sortie `game/Builds/WebGL`, puis copie vers `dist/RythmeRunner/` avec `game/BuildExtras/{info.json,thumbnail.png}`.

## Travail à 3
- Une branche par feature (`feat/conductor`, `feat/landing`, `fix/…`). PR courtes, merge dans `main` fréquent. Commits en français, à l'impératif.
- Une **scène unique `Main`** (ADR-007), avec **un seul propriétaire** à la fois (noté dans [docs/decisions.md](docs/decisions.md)). Chaque écran ou système est un **prefab** ou un ScriptableObject : c'est là qu'on travaille en parallèle, pour limiter les conflits YAML.
- Ne jamais modifier `game/Assets/Anatidae/**`, sauf `GameName` et le style visuel des UI Highscore. On l'étend depuis `Assets/_Project/`.
- Avant de coder : relire le doc concerné dans `docs/`. En cas de doute sur une règle de la borne, c'est [docs/anatidae.md](docs/anatidae.md) qui fait foi.

## Unity & MCP
- Si le **MCP Unity** est connecté : l'utiliser pour inspecter et modifier scènes, prefabs, composants et Build Settings, et pour lire la console.
- Sans MCP : **ne jamais éditer `.unity`, `.prefab`, `.asset` ou `.meta` à la main**. Écrire le C#, puis donner à l'humain les étapes éditeur, numérotées.
- Toujours créer les `.meta` via Unity (ne pas créer d'asset hors éditeur, sauf `.cs`, `.json` ou `.md`).

## Tenir ce fichier à jour (obligatoire)
Mettre à jour le CLAUDE.md concerné **dans le même commit** que le changement, dès qu'on a :
- une décision d'architecture ou de lib → ajouter aussi une entrée datée dans [docs/decisions.md](docs/decisions.md) ;
- une nouvelle convention, une commande ou un chemin → le CLAUDE.md du dossier ;
- un piège découvert (bug WebGL, borne, audio…) → la section concernée, en une ligne.
Garder ce fichier **court (< 150 lignes)** : le détail va dans `docs/`. Supprimer ce qui devient faux.
Préférences perso non partagées : `CLAUDE.local.md` (gitignoré).

## Infos manquantes (à compléter)
- Rôles des 3 membres → [docs/decisions.md](docs/decisions.md) et landing page.
- VPS (URL, accès) : **plus tard**. Tout tourne en local d'ici là → [web/CLAUDE.md](web/CLAUDE.md) § Déploiement.
