# Référence borne Anatidae

Sources : [anatidae-arcade](https://github.com/XariusExcl/anatidae-arcade) (serveur/menu de la borne, v1.5.x) et [anatidae-toolkit](https://github.com/XariusExcl/anatidae-toolkit) (base Unity).
En cas de doute, **ces repos font foi**. Relire leur README si un comportement semble différent.

## Matériel
- i5-7200U, 8 Go de RAM, **GPU intégré**, Debian 12, **Firefox ESR**, écran 24" **1920×1080**.
- 2 joueurs, chacun avec 1 joystick 2 axes et 7 boutons (index 0–6). **Bouton blanc** = manette 1, bouton 8.
- Joystick au repos : `-0.00392 ; -0.00392` → **dead zone obligatoire**.
- Testeur de manette en ligne : https://hardwaretester.com/gamepad (scène Unity `Assets/Scenes/InputTester.unity`).

## Exigences
1. Dossier `public/<GameName>/` contenant `index.html`, `thumbnail.png` (carré, ≤ 1920×1920) et `info.json`.
2. Bouton blanc → `window.location.href = "http://localhost:3000"`, instantané ou après un appui long ≤ 1,5 s.
3. 60 s sans aucun input → même retour au menu.
4. Optionnel : `attract.mp4` (trailer muet, ≤ 20 s, affiché en mode attract) et `StreamingAssets/` (servi automatiquement).
5. Score + nom de 3 caractères (lettres/chiffres), validé par la borne (`/api/nameValid`, liste de noms interdits).

## info.json
```json
{
  "name": "Rythme Runner",
  "description": "Cours au rythme de la musique : chaque saut tombe sur le beat.",
  "creator": "Lou Calmes, Lucas Corrieras, Jeremy Hordé",
  "year": 2026,
  "type": "Runner rythmique",
  "players": "1-2",
  "catchphrase": "Joue la musique en courant !",
  "config": { "scoreType": "score", "scoreSort": "desc" }
}
```
`scoreType` : `score` | `time` | `distance` (+ `scoreUnit` pour distance). `scoreSort` : `asc` | `desc`.

## Entrées
### Unity (Input Manager du toolkit)
| Nom | Manette | Clavier (dev) |
|---|---|---|
| `P1_Horizontal` / `P1_Vertical` | joystick 1, axes 0/1 (vertical inversé → haut = +) | Q/D, Z/S (AZERTY ; touches physiques A/D, W/S) |
| `P1_B1`…`P1_B3` | boutons 0–2 | F, G, H |
| `P1_B4`…`P1_B6` | boutons 3–5 | R, T, Y |
| `P1_Start` | bouton 6 | X |
| `P2_Horizontal` / `P2_Vertical` | joystick 2 | ← → / ↑ ↓ |
| `P2_B1`…`P2_B6` | boutons 0–5 | pavé num. 1–6 |
| `P2_Start` | bouton 6 | pavé num. 0 |
| `Coin` (bouton blanc) | bouton 8 | **Échap** |

Schéma : [img/anatidae-clavier.png](img/anatidae-clavier.png). Attention : sur l'image, les libellés P2_Vertical et P2_Horizontal sont inversés. C'est l'InputManager qui fait foi.
### Web (Gamepad API)
`gamepad.axes[0]` = horizontal, `axes[1]` = vertical, `buttons[0..6]` = actions, `buttons[8]` = bouton blanc.

## API de la borne (`http://localhost:3000`)
Toutes les routes prennent `?game=<GameName>`.
| Méthode | Route | Corps | Réponse |
|---|---|---|---|
| GET | `/api/?game=` | — | highscores `[{name, score, timestamp}]` |
| POST | `/api/?game=` | `{name, score}` | `{success: true\|false}` (false si le score existant est meilleur) |
| GET | `/api/playcount?game=` | — | `{playcount}` |
| GET | `/api/extradata?game=` | — | paires clé/valeur |
| POST | `/api/extradata?game=` | `{key, value}` | `{success: true}` |
| POST | `/api/nameValid` | `{name}` | `{valid: true\|false}` |
Les erreurs renvoient `400 {error}`.

### Proxy vers l'extérieur
- `GET /proxy?url=<URL>` → `fetch(url)` côté serveur ; **aucun en-tête transmis**, et on reçoit le texte de la réponse.
- `POST /proxy?url=<URL>` → le corps JSON est relayé et **les en-têtes aussi** (sauf host, connection, content-length).
- Dans Unity : `Anatidae.AnatidaeProxyWebRequest.Get/Post` (même signature que `UnityWebRequest`).
- Le statut HTTP distant n'est pas relayé (`res.send(text)`) : mettre les erreurs dans le JSON.

## Toolkit Unity (fourni en 6000.0.40f1, projet migré en 6000.3.23f1 : voir ADR-008)
- Prefab `AnatidaeInterface` (dans chaque scène) : `MenuManager`, `HighscoreNameInput`, `HighscoreUI` (entrées affichées avec le prefab `HighscoreEntry`). Le style visuel est modifiable librement.
- `HighscoreManager` : `GameName`, `FetchHighscores()`, `IsHighscore(score)` (top 10), `IsHighscore(name, score)`, `ShowHighscoreInput(score)` (envoi auto après « END »), `SetHighscore(name, score)`, `PlayerName`, `IsHighscoreInputScreenShown`, `ShowHighscores()` / `HideHighscores()`.
- `ExtradataManager` : `FetchExtraData()`, `SetExtraData(key, value)`, `GetDataWithKey(key)`.
- `BackToMenu.jslib` : redirection vers `localhost:3000`.

## Tester en local
```bash
git clone https://github.com/XariusExcl/anatidae-arcade tools/anatidae-arcade
cd tools/anatidae-arcade && npm install
cp -R ../../dist/RythmeRunner public/
node server.js   # → http://localhost:3000
```
