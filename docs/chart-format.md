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
- `runSpeed` : unités Unity par seconde. Position : `x = BeatToSeconds(beat) * runSpeed`, avec `BeatToSeconds(b) = b * 60 / bpm` (relatif au beat 0, donc x = 0 au beat 0). L'offset ne sert qu'à caler l'audio : position dans le fichier = `BeatToSeconds(b) + offsetMs / 1000`.
- `lane` : hauteur/étage (0 = sol, 1 = hauteur de saut, 2 = haut/plafond).
- `beat` : en temps (float). Granularité minimale : **0,25** (double-croche).
- Les `sections` servent aussi de **checkpoints**.

## Placement (important)
Le `beat` d'un événement d'obstacle = **le temps où le joueur doit agir** (appuyer). L'obstacle est placé juste après par `LevelBuilder` (constantes en tête de fichier) :
| type | géométrie | fenêtre de réussite (≈, en beats autour de `beat`) |
|---|---|---|
| `gap` | trou de `beat+0,2` à `beat+0,2+length` (défaut 0,6) | −0,18 … +0,42 |
| `wall` | bloc de 0,9 u à `beat+0,3`, hauteur `height` (défaut 0,8) | −0,29 … +0,11 |
| `enemy` / `block` | centre à `beat+0,4` | −0,46 … +0,22 |
| `slide_bar` | plafond de `beat+0,1` à `beat+0,1+length` (défaut 1), bas à 0,62 u | tenir le bas tout du long |
| `lum_line` | lums aux beats `beat + i×step`, hauteur selon `lane` : 0 → 0,45 u (au sol), 1 → 2,0 u (sommet d'un saut lancé 0,5 beat avant), 2 → 3,1 u | — |

Contraintes de faisabilité (le saut dure 1 beat, sommet à 2,2 u) :
- deux sauts (gap/wall) espacés d'**au moins 1 beat** ;
- pas d'ennemi pendant un saut (entre `j` et `j+1`), sauf pile à l'atterrissage `j+1` ;
- pas de saut entre `slide_bar.beat − 1,2` et la fin de la barre ;
- lums `lane 1` au-dessus d'un saut lancé en `j` : de `j+0,25` à `j+0,75`.

## Types d'événements
| type | Action attendue | params utiles |
|---|---|---|
| `gap` | sauter | `length` (en beats) |
| `enemy` | frapper | `variant` |
| `block` | frapper (cassable) | `variant` |
| `slide_bar` | glisser | `length` |
| `wall` | sauter (mur à franchir) | `height` |
| `wall_run` | automatique, course sur mur courbe | `length` (défaut 4, minimum 2), `count` |
| `hook` | automatique, liane/crochet | `length` (défaut 2, minimum 1,5), `count` |
| `lum_line` | collecter | `count`, `step` (beats entre lums) |
| `fx_flash` | — (visuel) | `intensity` |
| `camera_shake` | — | `intensity`, `length` |
| `camera_zoom` | — | `intensity`, `length` |
| `section_change` | — (mise en scène) | `variant` |

Ajouter un type = mettre à jour **ce tableau**, `LevelBuilder` et l'autoplay de debug dans la même PR.

## Règles de level design
- `hook` et `wall_run` ne peuvent pas chevaucher un début de section et gardent au moins 1 beat avant la fin de section.
- Aucun autre obstacle entre `hook.beat − 1` et `hook.beat + length + 1,5`, ni entre `wall_run.beat − 1` et `wall_run.beat + length + 1`.
- Une liane crée un trou de `beat + 0,2` à `beat + length + 0,5`; les lums de `hook` sont répartis entre `t=0,15` et `t=0,85`, ceux de `wall_run` entre `t=0,1` et `t=0,9`.
- Télégraphier chaque obstacle **1 mesure avant** (`beatsPerBar` beats).
- Au moins **1 beat** entre deux actions dans l'intro. Les croches (0,5) seulement à partir du refrain, les syncopes au final.
- Valider chaque chart avec l'**autoplay** avant de merger : F9 (overlay), puis F10 (autoplay) en partie. L'overlay doit afficher **0 chute** en fin de morceau.
- Exemple complet : `game/Assets/_Project/Charts/test_level_120.json` (128 beats, 5 sections, densité croissante).
