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
- `checkpoints` (optionnel) : beats des **points de reprise** après une mort (die and retry, ADR-033), triés ; le générateur les choisit (héros au sol sur le chemin principal, premier plan, gravité normale, aucune action dans le quart de temps avant ni 1,25 temps après) et rejoue la partie depuis chacun.
- `rail`, `railStep` (optionnels) : **rail de caméra** précalculé par le générateur, hauteur du centre de la caméra tous les `railStep` beats (trajectoire idéale lissée dans les deux sens : la caméra monte avant une envolée et descend avant un plongeon). Sans rail, la caméra suit le terrain.
- `beat` : en temps (float). Granularité minimale : **0,25** (double-croche).
- Les `sections` servent aussi de **checkpoints** et de **biomes** : la section n° i charge le `BiomeTheme` n° i de `Theme_Kenta` (8 sections de 32 beats pour le niveau 01). Un niveau a un monde (`LevelDef.biome`), gardé du début à la fin (ADR-032). Le champ `"biome": "Biome_08_orage"` (nom de l'asset) permet encore à une section d'en changer (flash et nouveau décor), mais aucune chart ne l'utilise : un thème par niveau.

## Placement (important)
Le `beat` d'un événement d'obstacle = **le temps où le joueur doit agir** (appuyer). L'obstacle est placé juste après par `LevelBuilder` (constantes en tête de fichier) :
| type | géométrie | fenêtre de réussite (≈, en beats autour de `beat`) |
|---|---|---|
| `gap` | trou de `beat+0,2` à `beat+0,2+length` (défaut 0,6). **`lane 1` = vide** sous une suite de plateformes : ni jugé ni sauté par l'autoplay | −0,18 … +0,42 |
| `wall` | souche de 0,9 u à `beat+0,4` (hitbox 0,7 × 0,75 u) ; pieds à moins de 0,4 u de son sommet : on passe dessus (coup de pouce) | ≥ ±0,3 (120 ms) |
| `enemy` / `block` | centre à `beat+0,6`. **`enemy` en `lane 1` = ennemi en vol** (chauve-souris), posé à la hauteur absolue `height` : à frapper en plein saut ou en pleine envolée (en l'air, la frappe porte de 1,2 u sous les pieds à 2,4 u au-dessus) | ≈ ±0,4 (165 ms) |
| `lum_line` | lums aux beats `beat + i×step`, hauteur selon `lane` **au-dessus du sol en x** (ou d'une plateforme) : 0 → 0,45 u, 1 → 1,9 u, 2 → 2,4 u | — |
| `step` | à partir de `beat+0,5`, le sol principal passe à `height` (absolue, libre : −60 … 60 u). Montée de plus de 0,25 u = face de marche mortelle : sauter sur `beat`. Descente = on tombe (une grande chute vers un sol plus bas n'est pas mortelle). **`lane 1` = falaise** : montée atteinte en vol (tambour), pas de saut attendu ni jugé | comme `wall` |
| `ramp` | de `beat` à `beat+length`, le sol monte ou descend en ligne droite jusqu'à `height` (automatique). Raide en montée = course sur le mur (pose « mur »). Dessinée en escalier de 8 px | — |
| `bounce` | tambour-tremplin (sprite `block` du biome, enfoncé dans le sol) centré sur `beat` : en passant dessus, envol de `length` beats (défaut 2), atterrissage pile sur le sol prévu à `beat+length`. Sauter sur `beat` est jugé ; sauter trop tôt fait rater le rebond | appui à ±0,5 beat |
| `lum_at` | une pièce à la hauteur absolue `height` (pièces posées sur la trajectoire par le générateur). Avec `hasNote: true`, elle joue la note `note` (demi-tons par rapport au la du son de pièce) : les pièces jouent la mélodie | — |
| `camera` | nouveau plan : `zoom` = largeur de vue en px de jeu (320, 384, 480, 640 → ×6, ×5, ×4, ×3 : changement net, petit flash), `frameX` = place du héros (0 gauche … 1 droite), `frameY` = hauteur du sol sous le héros (0 bas … 1 haut). 0 = inchangé | — |
| `platform` | plateforme de `beat+0,3` à `beat+0,3+length` (défaut 1) à `height`, traversable par en dessous ; sauter sur `beat` | sommet du saut au-dessus de `height` avant `beat+0,3` |
| `vine` | **liane** attrapée en passant à `beat` (pieds à ±1,6 u de `height`), balancier de `length` beats (pieds de `height` à `releaseHeight`, creux `sag`). **On saute pour lâcher**, autour de `beat+length` ; sans appui, la main glisse et on tombe. Le vol arrive au temps `beat+length+step` (défaut 1) sur la liane suivante si elle commence là, sinon sur le sol | lâcher à ±0,35 beat (140 ms) |
| `enemy` + `variant: "stomp"` | **cuirassé** (on ne le frappe pas) : centre à `beat−0,15`, on le touche en lui sautant dessus ; appuyer sur saut autour de `beat` = **grand rebond** de `length` beats (défaut 2) vers le cuirassé suivant, une note de saut ou le sol ; sinon petit rebond d'un demi-temps | appui à ±0,35 beat |
| `orb` | **note de saut** : centre à (x de `beat`, `height`). En l'air près d'elle (3 u en hauteur), un appui autour de `beat` relance le saut pour `step` beats (défaut 1) vers la note suivante, un cuirassé, une liane ou le sol. Pendant sa fenêtre, la note retient la chute (3 u/s) | appui à ±0,35 beat |
| `ceiling` | **plafond** : dessous à `height`, de `beat` à `beat+length` (les trous entre deux plafonds se sautent) | — |
| `gravity` | **portail de gravité** à `beat` : `lane 1` = on tombe vers le plafond et on y court la tête en bas, `lane 0` = retour au sol | — |
| `back` | **chemin de l'arrière-plan** : sol plat à `height`, de `beat` à `beat+length`, dessiné derrière le niveau | — |
| `bounce` + `variant` | `"to_back"` : tambour devant qui envoie sur le chemin du fond ; `"to_front"` : tambour au fond qui ramène devant ; `"back"` : tambour au fond | comme `bounce` |
| `enemy` + `variant: "boss"` | **le dernier coup du niveau 9** : un ennemi au sol sans dessin ; la Sourdine fond dessus juste avant, et le coup de katana qui le frappe la fend en deux (`FX/SourdineBoss`). Raté : on meurt, comme contre un ennemi | comme `enemy` |
| `enemy` + `lane 2` | **pendu au plafond** (dessous à `height`) ; avec `variant: "back"` : ennemi sur le chemin du fond | comme `enemy` |
| `jump` | saut sans trou dans le sol du niveau (trou du plafond, ou du chemin du fond avec `variant: "back"`) : jugé quand on passe le trou | comme `gap` |
| `screen_split` | l'image se dédouble en une grille qui remplit tout l'écran : `copies` = 1 (normal), 4 (2×2, agrandies ×1,5) ou 9 (3×3, ×1), jusqu'au prochain `screen_split` (2 et 5 sont lus comme 4 et 9) | — |
| `screen_flip` | l'image se retourne jusqu'au prochain `screen_flip` : `variant` = `"mirror"` (gauche-droite : on court vers la gauche), `"up"` (tête en bas), `"both"` (demi-tour), autre = à l'endroit. Miroir entier (pixel art net) ; l'interface reste à l'endroit | — |
| `glitch` | grésillement (neige, image qui saute, couleurs) d'intensité `intensity` (0…1) pendant `length` beats, plus fort sur chaque temps | — |
| `fx_invert` | le monde passe en négatif (`variant` = `"on"`) ou revient (`"off"`), jusqu'au prochain `fx_invert` ; seul le calque du monde s'inverse (le héros et l'interface gardent leurs couleurs). Un état sur un temps fort : au plus une bascule par temps (borne publique). Niveau 9 | — |
| `fx_corrupt` | le monde se corrompt (bandes décalées, couleurs séparées, blocs de sprite déplacés ou en aplats de couleur pure, pixels morts ; au-delà de 0,55 : barres R/V/B et trame) : va vers `intensity` (0…1) en `length` temps, depuis la valeur du moment ; lu sur le temps du morceau (juste après une reprise). Calque du monde seul, propre autour du héros. Niveau 9 (ADR-042) | — |
| `fx_gray` | le monde perd sa couleur : va vers `intensity` (0 = couleur, 1 = noir et blanc) en `length` temps ; le héros, les pièces et l'interface gardent la leur. Niveau 9 (ADR-042) | — |

Tous les obstacles (mur, ennemi, bloc, plafond, pièces) se posent **sur le sol principal à leur x** : un mur après une marche est posé en haut de la marche.

Contraintes de faisabilité (le saut dure 1 beat, sommet à **1,5 u** au-dessus du point de départ) :
- une montée (`step` ou `platform`) ne dépasse jamais **+1,0 u** par rapport au sol d'où l'on saute ;
- deux sauts (gap/wall) espacés d'**au moins 1 beat** ;
- pas d'ennemi pendant un saut (entre `j` et `j+1`), sauf pile à l'atterrissage `j+1` ;
- lums `lane 1` au-dessus d'un saut lancé en `j` : de `j+0,25` à `j+0,75`.

## Types d'événements
| type | Action attendue | params utiles |
|---|---|---|
| `gap` | sauter | `length` (en beats) |
| `enemy` | frapper | `variant` |
| `block` | frapper (cassable) | `variant` |
| `wall` | sauter (mur à franchir) | `height` |
| `step` | sauter si ça monte (marche) ; rien si ça descend | `height` |
| `platform` | sauter dessus | `length`, `height` |
| `ramp` | rien (automatique) | `length`, `height` |
| `bounce` | rien (automatique) ; sauter sur le temps = jugé | `length` (durée du vol) |
| `camera` | — (mise en scène) | `zoom`, `frameX`, `frameY` |
| `lum_at` | collecter | `height` |
| `vine` | sauter pour lâcher (obligatoire) | `length`, `height`, `releaseHeight`, `sag`, `step` |
| `enemy` `stomp` | sauter dessus, puis sauter au rebond | `variant`, `length` |
| `orb` | sauter en passant dessus | `height`, `step` |
| `ceiling`, `back`, `gravity` | rien (décor et portails) ; on saute les trous (`jump`) | `length`, `height` |
| `jump` | sauter (trou du plafond ou du chemin du fond) | `variant` |
| `screen_split`, `screen_flip`, `glitch`, `fx_invert`, `fx_corrupt`, `fx_gray` | — (effets d'écran) | `copies` ; `variant` ; `intensity`, `length` ; `variant` ; `intensity`, `length` ; `intensity`, `length` |
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
- Exemples : `game/Assets/_Project/Charts/test_level_120.json` (test à la main, 128 beats), `test_vertical.json` (marches et plateformes, à la main) et `level01_shovel_knight.json` (**générée** par `python3 tools/gen-level.py shovel_knight`, scène par scène dans `tools/levels/shovel_knight.py` : ne pas l'éditer à la main, modifier les scènes ; un fichier par niveau, ADR-035). Le moteur (`tools/levelgen/`) simule l'autoplay (même physique que le jeu) à 60 et 30 i/s et refuse d'écrire une chart où l'on meurt ; il pose les pièces sur la trajectoire simulée. L'ancien `tools/gen-level-chart.py` (librosa, niveau plat) n'est plus utilisé pour le niveau 01.
