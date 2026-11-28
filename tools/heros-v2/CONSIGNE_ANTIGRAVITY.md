# Consigne pour Antigravity : héros v2, corrections et nouvelles animations

À lancer depuis la racine du dépôt (branche des héros v2) :
`agy -p "$(cat tools/heros-v2/CONSIGNE_ANTIGRAVITY.md)" --mode accept-edits --settings <réglages de session>`
Claude relance les scripts, les tests et Unity, puis relit tout. Faire la partie 1 tout de suite ; la partie 2 quand
les planches de `PROMPT.md` sont dans `tools/heros-v2/raw/v3/`.

---

Tu es le sous-agent de Claude sur le jeu Kenta. Réponds et commente en français ; identifiants en anglais. La relecture
sera intransigeante : même qualité que si Claude le faisait.

## Règles strictes
- N'utilise **aucune commande de terminal** : seulement tes outils de lecture et d'écriture de fichiers. Ne lance ni le script, ni les tests, ni Unity.
- Ne modifie que les fichiers cités dans la partie demandée. Jamais d'image à la main, jamais de `.asset`, `.meta`, `.unity` ou `.prefab`.
- Réutilise l'existant (lis `tools/heros-v2/import.py`, `KentaBuilder.cs`, `HeroSpriteSet.cs`, `HeroAnimator.cs`, `tools/tests/test_heroes_v2.py`) : aucun doublon, changements minimaux, dans le style des fichiers.
- Mets à jour `docs/decisions.md` (ADR-045, une phrase par changement), `game/CLAUDE.md` si une règle change, et `docs/roadmap.md`.

## Partie 1 : corrections sans nouvelle image (à faire maintenant)

### 1. Les mains sur la liane (fait le 28/11 par Claude : toile 96 × 96, pivot à 24 px, poing à 54 px)
Le jeu dessine la corde jusqu'à la main du héros, en `(x, y + VineView.HandHeight)`, soit **44 px au-dessus du pivot** (les pieds). Les images v2 `hero_<h>_vine.png` et `vine_0..1` ont les mains à 70 px (Kenta, Ryo) et 54 px (Aiko) : la corde arrive à la poitrine.
- Dans `tools/heros-v2/import.py`, sors les images de liane (`vine`, `vine_*`) sur une toile plus haute. Le point de prise (le haut des mains jointes, centre horizontal des mains) y tombe **exactement à 44 px au-dessus du pivot**, et le pivot reste au centre horizontal des mains. Le héros pend donc en dessous : ses pieds descendent sous le pivot.
- Pivot de ces images : `KentaSpritePostprocessor` met `(0.5, 0)` à tout `Gen/heroes/`. Ajoute un cas pour les noms qui contiennent `_vine`, avec le pivot vertical qui correspond à la toile choisie (une constante commune au script et au post-processeur, commentée des deux côtés : « changer les deux ensemble »).
- Test dans `test_heroes_v2.py` : sur chaque image de liane, le haut des mains est à 44 ± 2 px au-dessus du pivot.

### 2. La traînée du bandeau
`FX/BandanaTrail` s'attache à `anchor = (-0.18, 0.95)` u des pieds : la tête de la marionnette. Celle des héros v2 est plus haut (haut de la tête à 53 px pour Kenta, 48 pour Aiko, 51 pour Ryo).
- `import.py` mesure sur l'image `idle` de chaque héros le point d'attache, c'est-à-dire l'arrière du bandeau (le nœud) ou, à défaut, l'arrière de la tête à la hauteur du bandeau, et l'écrit dans `tools/heros-v2/anchors.json` (en unités, 40 px = 1 u, depuis le pivot).
- `HeroSpriteSet` : un champ `Vector2 bandanaAnchor`, rempli par `KentaBuilder` depuis ce fichier.
- `BandanaTrail` : une propriété `Anchor`, comme `Color`.
- `RunManager.ApplyHero` : `bandana.Anchor = hero.sprites.bandanaAnchor`, si elle n'est pas nulle.
- Test Python : le point est dans la moitié haute du sprite et sur sa moitié arrière (gauche).

Réponds en 6 lignes au plus : les fichiers touchés et tes incertitudes.

## Partie 2 : nouvelles planches (quand `tools/heros-v2/raw/v3/` est rempli)

Planches `<héros>_<id>.png` (ids `R`, `J`, `L`, `A`, `T`, décrits dans `tools/heros-v2/PROMPT.md`), fond gris `#808080`, grille de 4 × 2 cases égales, ordre de lecture gauche → droite, puis la 2e rangée.

### 1. Import
Étends `import.py` avec le même détourage, la même réduction nette et le même contour qu'aujourd'hui.
- Mêmes règles qu'en v2 :
  - **une échelle par héros**, celle de son `idle` actuel (ne pas recalculer par planche) ;
  - la ligne de sol de chaque rangée posée sur la dernière rangée de pixels ;
  - le centre de la case au pivot ;
  - aucune image qui saute.
- Familles produites :
  - `R` → `run_0..7` (remplace l'actuelle ; Ryo passe à 8, supprime `run_8`) ;
  - `J` → `jump_0..7`, le salto : centre du corps à la même place sur les images 3 à 7 ;
  - `L` → `vine_0..3` (mains au point de prise, comme en partie 1), `fall_0..2` et `land_0` ;
  - `A` → `airhit_0..5`, sur une toile de 160 × 72 avec la traînée jusqu'à `HIT_REACH_PX`, comme `hit_2` et `hit_3` ;
  - `T` → `drum_0..2` et `victory_0..4`.
- Les cases vides sont ignorées. Une planche absente laisse la famille actuelle en place.

### 2. Branchement
`KentaBuilder` charge déjà `victory_`, `land_`, `vine_`, `drum_` et `death_` par `Frames(...)`. Ajoute `airhit_` → `airHitFrames` s'il n'y est pas. Ne touche pas à `HeroAnimator` sauf nécessité, et justifie-la.

### 3. Tests (`test_heroes_v2.py`)
- **Course** : sur `run_0..7`, la tête suit le motif bas / moyen / haut (écart de 3 px au plus entre images voisines), et au moins une image par pas a les deux pieds au-dessus de la ligne de sol (phase en l'air).
- **Salto** : `jump_3..6` contiennent au moins une image où la tête est sous le bassin (héros à l'envers). On le mesure par la position des pixels de la couleur des cheveux.
- **Liane** : mains à 44 ± 2 px du pivot sur `vine_0..3`.
- **Coup en l'air** : `airhit_2` atteint la colonne `80 + 76` (± 1).
- **Tailles** : stables d'une image à l'autre dans chaque famille (hauteur du corps à ± 3 px, hors salto).

Réponds en 8 lignes au plus : les fichiers touchés et tes incertitudes.
