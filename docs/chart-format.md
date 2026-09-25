# Format de chart (JSON)

Un fichier par niveau dans `game/Assets/_Project/Charts/<id>.json`, chargé comme `TextAsset` et parsé avec `JsonUtility` (donc **pas de dictionnaires ni de champs polymorphes** : `params` est un objet à champs fixes et optionnels).

```json
{
  "id": "level01",
  "title": "Nom du morceau",
  "bpm": 128,
  "offsetMs": 45,
  "beatsPerBar": 4,
  "runSpeed": 9.0,
  "audio": "level01_main",
  "remixAudio": "level01_8bit",
  "sections": [
    { "name": "intro",   "startBeat": 0 },
    { "name": "couplet", "startBeat": 16 },
    { "name": "refrain", "startBeat": 48 }
  ],
  "events": [
    { "beat": 8.0,  "type": "gap",       "lane": 0, "params": { "length": 2 } },
    { "beat": 10.0, "type": "lum_line",  "lane": 1, "params": { "count": 4, "step": 0.5 } },
    { "beat": 12.0, "type": "enemy",     "lane": 0, "params": { "variant": "drum" } },
    { "beat": 16.0, "type": "fx_flash",  "lane": 0, "params": { "intensity": 1 } }
  ]
}
```

## Champs
- `bpm` : tempo **constant** (pas de changement de tempo en cours de morceau).
- `offsetMs` : délai entre le début du fichier audio et le beat 0.
- `runSpeed` : unités Unity par seconde. Position : `x = BeatToSeconds(beat) * runSpeed`, avec `BeatToSeconds(b) = b * 60 / bpm + offsetMs / 1000`.
- `lane` : hauteur/étage (0 = sol, 1 = hauteur de saut, 2 = haut/plafond).
- `beat` : en temps (float). Granularité minimale : **0,25** (double-croche).
- Les `sections` servent aussi de **checkpoints**.

## Types d'événements
| type | Action attendue | params utiles |
|---|---|---|
| `gap` | sauter | `length` (en beats) |
| `enemy` | frapper | `variant` |
| `block` | frapper (cassable) | `variant` |
| `slide_bar` | glisser | `length` |
| `wall` | sauter (mur à franchir) | `height` |
| `wall_run` | automatique | `length` |
| `hook` | automatique (liane/crochet) | `length` |
| `lum_line` | collecter | `count`, `step` (beats entre lums) |
| `fx_flash` | — (visuel) | `intensity` |
| `camera_shake` | — | `intensity`, `length` |
| `camera_zoom` | — | `intensity`, `length` |
| `section_change` | — (mise en scène) | `variant` |

Ajouter un type = mettre à jour **ce tableau**, `LevelBuilder` et l'autoplay de debug dans la même PR.

## Règles de level design
- Télégraphier chaque obstacle **1 mesure avant** (`beatsPerBar` beats).
- Au moins **1 beat** entre deux actions dans l'intro. Les croches (0,5) seulement à partir du refrain, les syncopes au final.
- Valider chaque chart avec l'**autoplay** + la grille de beats avant de merger.
