# web/ — Site one-page + serveur (VPS)

Rôle : présenter le jeu (concept, personnages, lore), tenir un **mini-blog** de l'avancement, et **communiquer en direct avec la borne**.
Statut : dossier à initialiser (`npm init`, puis `express` et `ws`). Mettre à jour ce fichier dès que la structure existe.
**Mode actuel : 100 % local** (ADR-006). Serveur sur `http://localhost:8080` (`PORT=8080`, le port 3000 est pris par la borne locale). Pour le jeu, `NetConfig.baseUrl = "http://localhost:8080/api"`. Le proxy de la borne locale fait le `fetch` côté Node, donc pas de problème de CORS ni de mixed content.

## Stack (ADR-003)
- Node ≥ 18 (ES modules), **Express** (site statique + API REST), **ws** (WebSocket vers les navigateurs).
- Front : HTML/CSS/JS vanilla dans `public/`. Pas de framework sans ADR. Vite est envisageable plus tard.
- Blog : un fichier Markdown par article dans `content/blog/AAAA-MM-JJ-slug.md` (front-matter : `title`, `date`, `author`, `cover`), rendu côté serveur (`marked`).
- Stockage : fichier JSON ou SQLite (`better-sqlite3`). Pas de base externe.

## Structure cible
```
web/
├── server/  index.js  routes/api.js  ws.js  store.js
├── public/  index.html  css/  js/  assets/
├── content/blog/
└── package.json   (scripts: dev = node --watch server/index.js, start)
```

## Contenu de la one-page (ordre)
1. **Hero** dans l'esthétique du jeu + « Jouez sur la borne du couloir MMI ».
2. **Équipe** : Lou Calmes, Lucas Corrieras, Jeremy Hordé, avec le rôle de chacun et le type de jeu (« runner rythmique ») (**première version exigée par l'enseignant**).
3. Concept et gameplay (GIF ou vidéo), personnages, lore.
4. **Live borne** : dernières parties, meilleur score du jour, partie en cours (WebSocket).
5. Mini-blog (articles d'avancement).

## Communication avec le jeu
Contrat complet : [../docs/api-contract.md](../docs/api-contract.md). En résumé :
- Le jeu (sur la borne) appelle notre API via le proxy Anatidae : `http://localhost:3000/proxy?url=<notre URL>`. Donc **REST uniquement** côté jeu (GET/POST en JSON).
- Le serveur **diffuse en WebSocket** chaque événement reçu aux visiteurs du site.
- Du site vers le jeu : le site écrit une « live-config » (message, défi du jour) que le jeu lit en GET au début de chaque partie.
- Protéger les POST du jeu avec un en-tête `X-RR-Key` (secret partagé dans `.env`, jamais commité). ⚠️ Le proxy de la borne transmet les en-têtes des **POST** mais **pas ceux des GET**, et il renvoie toujours le corps de la réponse en texte (sans le statut HTTP). Les GET doivent donc rester publics et l'API doit mettre les erreurs dans le JSON.
- Valider et borner toutes les entrées (nom de 3 caractères `[A-Z0-9]`, score entier ≥ 0).

## Esthétique
Reprendre la palette, les polices et les formes du jeu. Tokens CSS dans `public/css/tokens.css` (`--rr-*`), synchronisés avec la direction artistique du jeu. Site responsive, animations sur le beat possibles (BPM du morceau).

## Déploiement VPS (plus tard : on reste en local pour l'instant)
- URL : _à renseigner_. Process : `pm2 start server/index.js --name rythme-runner`. Reverse proxy **nginx** avec HTTPS (Let's Encrypt) et upgrade WebSocket (`proxy_set_header Upgrade/Connection`).
- Variables : `.env` (`PORT`, `RR_KEY`), avec `.env.example` commité.
