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
- `lane` : hauteur/étage (0 = sol, 1 = hauteur de saut, 2 = haut/plafond). Sens particulier pour `step` (falaise), `gap` (vide) et `enemy` (en vol), voir plus bas.
- `zoom` (optionnel) : largeur de vue de la caméra pour ce niveau (320, 384, 480 ou 640 px de jeu). Le niveau 01 est en 640 (×3) : une mesure d'avance devant le héros.
- `rail`, `railStep` (optionnels) : **rail de caméra** précalculé par le générateur, hauteur du centre de la caméra tous les `railStep` beats (trajectoire idéale lissée dans les deux sens : la caméra monte avant une envolée et descend avant un plongeon). Sans rail, la caméra suit le terrain.
- `beat` : en temps (float). Granularité minimale : **0,25** (double-croche).
- Les `sections` servent aussi de **checkpoints** et de **biomes** : la section n° i charge le `BiomeTheme` n° i de `Theme_Kenta` (8 sections de 32 beats pour le niveau 01).

## Placement (important)
Le `beat` d'un événement d'obstacle = **le temps où le joueur doit agir** (appuyer). L'obstacle est placé juste après par `LevelBuilder` (constantes en tête de fichier) :
| type | géométrie | fenêtre de réussite (≈, en beats autour de `beat`) |
|---|---|---|
| `gap` | trou de `beat+0,2` à `beat+0,2+length` (défaut 0,6). **`lane 1` = vide** sous une suite de plateformes : ni jugé ni sauté par l'autoplay | −0,18 … +0,42 |
| `wall` | bloc de 0,9 u à `beat+0,3`, hauteur `height` (défaut 0,8) | −0,29 … +0,11 |
| `enemy` / `block` | centre à `beat+0,4`. **`enemy` en `lane 1` = ennemi en vol** (chauve-souris), posé à la hauteur absolue `height` : à frapper en plein saut ou en pleine envolée | −0,46 … +0,22 |
| `slide_bar` | plafond de `beat+0,1` à `beat+0,1+length` (défaut 1), bas à 0,62 u | tenir le bas tout du long |
| `lum_line` | lums aux beats `beat + i×step`, hauteur selon `lane` **au-dessus du sol en x** (ou d'une plateforme) : 0 → 0,45 u, 1 → 1,9 u, 2 → 2,4 u | — |
| `step` | à partir de `beat+0,5`, le sol principal passe à `height` (absolue, libre : −60 … 60 u). Montée de plus de 0,25 u = face de marche mortelle : sauter sur `beat`. Descente = on tombe (une grande chute vers un sol plus bas n'est pas mortelle). **`lane 1` = falaise** : montée atteinte en vol (tambour), pas de saut attendu ni jugé | comme `wall` |
| `ramp` | de `beat` à `beat+length`, le sol monte ou descend en ligne droite jusqu'à `height` (automatique). Raide en montée = course sur le mur (pose « mur »). Dessinée en escalier de 8 px | — |
| `bounce` | tambour-tremplin (sprite `block` du biome, enfoncé dans le sol) centré sur `beat` : en passant dessus, envol de `length` beats (défaut 2), atterrissage pile sur le sol prévu à `beat+length`. Sauter sur `beat` est jugé ; sauter trop tôt fait rater le rebond | appui à ±0,5 beat |
| `lum_at` | une pièce à la hauteur absolue `height` (pièces posées sur la trajectoire par le générateur). Avec `hasNote: true`, elle joue la note `note` (demi-tons par rapport au la du son de pièce) : les pièces jouent la mélodie | — |
| `camera` | nouveau plan : `zoom` = largeur de vue en px de jeu (320, 384, 480, 640 → ×6, ×5, ×4, ×3 : changement net, petit flash), `frameX` = place du héros (0 gauche … 1 droite), `frameY` = hauteur du sol sous le héros (0 bas … 1 haut). 0 = inchangé | — |
| `platform` | plateforme de `beat+0,3` à `beat+0,3+length` (défaut 1) à `height`, traversable par en dessous ; sauter sur `beat` | sommet du saut au-dessus de `height` avant `beat+0,3` |

Tous les obstacles (mur, ennemi, bloc, plafond, pièces) se posent **sur le sol principal à leur x** : un mur après une marche est posé en haut de la marche.

Contraintes de faisabilité (le saut dure 1 beat, sommet à **1,5 u** au-dessus du point de départ) :
- une montée (`step` ou `platform`) ne dépasse jamais **+1,0 u** par rapport au sol d'où l'on saute ;
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
| `step` | sauter si ça monte (marche) ; rien si ça descend | `height` |
| `platform` | sauter dessus | `length`, `height` |
| `ramp` | rien (automatique) | `length`, `height` |
| `bounce` | rien (automatique) ; sauter sur le temps = jugé | `length` (durée du vol) |
| `camera` | — (mise en scène) | `zoom`, `frameX`, `frameY` |
| `lum_at` | collecter | `height` |
| `hook` | automatique, liane/crochet (**pas encore implémenté**) | `length` |
| `lum_line` | collecter | `count`, `step` (beats entre lums) |
| `fx_flash` | — (visuel) | `intensity` |
| `camera_shake` | — | `intensity`, `length` |
| `camera_zoom` | — | `intensity`, `length` |
| `section_change` | — (mise en scène) | `variant` |

Ajouter un type = mettre à jour **ce tableau**, `LevelBuilder` et l'autoplay de debug dans la même PR.

## Règles de level design
- Télégraphier chaque obstacle **1 mesure avant** (`beatsPerBar` beats).
- Au moins **1 beat** entre deux actions dans l'intro. Les croches (0,5) seulement à partir du refrain, les syncopes au final.
- Valider chaque chart avec l'**autoplay** avant de merger : F9 (overlay), puis F10 (autoplay) en partie. L'overlay doit afficher **0 chute** en fin de morceau.
- Exemples : `game/Assets/_Project/Charts/test_level_120.json` (test à la main, 128 beats), `test_vertical.json` (marches et plateformes, à la main) et `level01_shovel_knight.json` (**générée** par `tools/gen-level-vertical.py`, scène par scène : ne pas l'éditer à la main, modifier les scènes du script). Le script simule l'autoplay (même physique que le jeu) à 60 et 30 i/s et refuse d'écrire une chart où l'on meurt ; il pose les pièces sur la trajectoire simulée. L'ancien `tools/gen-level-chart.py` (librosa, niveau plat) n'est plus utilisé pour le niveau 01.
