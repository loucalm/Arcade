# Direction artistique : néon filaire façon Rider

Référence : le jeu mobile *Rider* (fond bleu-vert sombre, traits lumineux turquoise et magenta, formes simples, halos).
Principe : **simple mais efficace**. Tout est fait de traits fins et brillants sur un fond sombre. Aucun aplat clair, aucune texture.

## Palette
| Rôle | Couleur | Hex | Sert à |
|---|---|---|---|
| Fond | bleu-vert très sombre | `#0C2531` | caméra ; s'éclaircit sur les temps forts (`#1A4B5E`) |
| Turquoise | néon principal | `#3FF0FF` | héros, sol, murs, balises : **le terrain et le joueur** |
| Magenta | néon d'action | `#FF2BB4` | écharpe et gants du héros, ennemis, caisses, coup de poing, logo : **ce qu'on frappe** |
| Ambre | danger | `#FFB12E` | lasers (barres à glisser) |
| Jaune pâle | collectible | `#FFF0A0` | lums |

Règle de lecture : **magenta = à frapper (B2)**, **turquoise = on saute par-dessus (B1)**, **ambre = on glisse dessous (joystick bas)**, **jaune = à ramasser**.

## Rendu d'un sprite néon
Chaque trait est rendu par `tools/neonlib.py` :
1. **corps** : le trait, de la couleur du néon ;
2. **cœur** : même trait, plus fin, presque blanc (effet tube) ;
3. **halo** : trait flouté à trois rayons (serré, moyen, large).
Les couches s'additionnent comme de la lumière : alpha = canal max, couleur = rgb / alpha. Sur fond sombre, c'est un néon. Les sprites sont donc **déjà colorés** : dans Unity ils restent blancs (`color = white`), on ne les teinte pas.

## Les sprites (`game/Assets/_Project/Art/Neon/`)
Tous générés par **`python3 tools/gen-neon-sprites.py`** (dépendances : `pip install -r tools/requirements.txt`). Ne jamais les retoucher à la main : modifier le générateur et relancer. Options : `--only hero,enemy` pour n'en regénérer que certains, `--sheet planche.png` pour une planche de contact.

| Fichier | Taille | Pivot / 9-slice | Usage |
|---|---|---|---|
| `hero_run_00…11` | 224² | pivot aux pieds (108, 176) | cycle de course, 1 foulée = 2 beats |
| `hero_jump_up / jump_apex / jump_fall / land / slide / hit / hurt` | 224² | idem | poses |
| `enemy_0`, `enemy_1` | 160² | pivot bas de la gemme | 2 images qui alternent à chaque beat |
| `block` | 160² | pivot bas de la caisse | caisse à frapper |
| `wall` | 128² | 9-slice, bordure 44 | mur ; marge de halo 0,30 u |
| `laser_beam` / `laser_node` / `laser_post` | 128×64 / 64² / 64×128 | 9-slice L-R 24 / centre / 9-slice H-B 32 | barre à glisser |
| `lum` | 96² | centre | collectible |
| `checkpoint` | 128×512 | pivot au pied du mât | balise de section |
| `ground_top` | 64×256 | 9-slice L-R 24 | trait de sol, dégradé, filets de fuite |
| `rock_0…3` | 256×384 | pivot bas | rochers du décor |
| `bg_gradient` | 4×256 | centre | voile : vignette + lueur d'horizon |
| `slash` | 224×192 | pivot au centre de l'arc | coup de poing |
| `spark` | 48² | centre | particules |
| `heart_on` / `heart_off` | 96² | centre | cœurs du HUD |
| `logo` | 1152×224 | centre | titre de l'écran Attract |

PPU = 100 partout (1 u = 100 px). Les pivots et bordures sont appliqués **automatiquement** à l'import par `Assets/_Project/Editor/NeonSpritePostprocessor.cs` (table par nom de fichier). **Si on change une taille ou un point d'ancrage dans le générateur, mettre à jour cette table dans le même commit.**

## Dans Unity
- `Theme_Neon` (`LevelTheme`) référence tous les sprites et la palette. Remplacer un sprite = changer la référence là, sans toucher au code.
- `HeroSprites_Neon` (`HeroSpriteSet`) : les images du héros ; `HeroAnimator` choisit l'image selon l'état et cale la foulée sur le beat (12 images par cycle de 2 beats, soit 12 images/s à 120 BPM).
- `NeonTrail` : traînée turquoise → magenta derrière le héros, matériau additif.
- `Backdrop` : deux calques de rochers en parallaxe + voile collé à la caméra.
- `LevelBuilder` : construit les obstacles à partir des sprites (9-slice pour les murs, lasers et sol).
- Particules : `FxPool` avec le sprite `spark` (additif).

## Ajouter ou modifier un élément
1. Ajouter une fonction `gen_xxx` dans `tools/gen-neon-sprites.py` et l'enregistrer dans `GENERATORS`.
2. Ajouter son entrée (pivot, bordure) dans `NeonSpritePostprocessor.TryGetSpec`.
3. Ajouter un champ dans `LevelTheme` (suffixe `Sprite` si le nom existe déjà comme couleur) et l'utiliser dans `LevelBuilder`.
4. Régénérer : `python3 tools/gen-neon-sprites.py`, puis vérifier dans Unity.

## Ce qui reste à faire (voir [roadmap.md](roadmap.md))
- Effets du tour REMIX (brouillage, image inversée…) dans le même style.
- Textes du HUD en police néon (aujourd'hui police TextMesh Pro par défaut).
- Animations supplémentaires du héros (mort, victoire).
