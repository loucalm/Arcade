# Contrat API — jeu ↔ VPS ↔ site

```
[Jeu Unity sur la borne] --REST via localhost:3000/proxy--> [Serveur VPS Express] --WebSocket--> [Navigateurs du site]
                         <--GET live-config---------------                     <--POST admin (site)--
```
Le jeu ne peut faire que du **REST** (via `AnatidaeProxyWebRequest`). Tout le temps réel côté site passe par le WebSocket du VPS.
Contraintes du proxy : les en-têtes ne sont transmis que sur les POST, et le statut HTTP n'est pas relayé. Voir [anatidae.md](anatidae.md).

Base : `https://<VPS>/api` (à renseigner dans le ScriptableObject `NetConfig` du jeu et dans `web/.env`).
Toutes les réponses sont en JSON : `{ ok: true, ... }` ou `{ ok: false, error: "..." }`.

## Jeu → VPS
| Méthode | Route | Corps | Quand |
|---|---|---|---|
| POST | `/api/runs/start` | `{ sessionId, players, level }` | début de partie |
| POST | `/api/runs/event` | `{ sessionId, type: "section"\|"death"\|"remix", section, score }` | pendant la partie (throttle ≥ 2 s) |
| POST | `/api/runs/end` | `{ sessionId, name?, score, lums, deaths, remixLaps, medal }` | fin de partie |
| GET | `/api/live-config` | — | au titre et au début de chaque partie |

- En-tête `X-RR-Key: <secret>` sur tous les POST. Le serveur rejette les requêtes sans ce secret.
- `sessionId` : GUID généré par le jeu à chaque partie.
- **Le jeu ne doit jamais bloquer** si le VPS ne répond pas (timeout de 3 s, on ignore l'échec).

`live-config` : `{ ok, message?: string, challenge?: { label, targetScore }, theme?: string }`.

## VPS → site (WebSocket `wss://<VPS>/ws`)
Messages `{ type, data }` :
- `run:start`, `run:event`, `run:end` : relais des événements du jeu.
- `stats` : `{ playsToday, bestToday: {name, score}, lastRuns: [...] }`, envoyé à la connexion puis à chaque changement.

## Site → VPS (admin léger)
| POST | `/api/live-config` | `{ message?, challenge? }` | protégé par `X-RR-Key` |

## Évolutions
Toute modification de ce contrat se fait **dans la même PR** côté jeu (`Scripts/Net/`) et côté web (`server/routes/`), avec une mise à jour de ce fichier.
