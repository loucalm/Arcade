# Effet d'écran cathodique (CRT) « bombé léger » — conception

Date : 2026-11-26 · Branche : `feat/crt` · Taille : petit changement

## Intention
Donner au jeu le rendu d'une borne à tube (référence : photo d'un écran cathodique de borne, apportée par Lou), mais **léger**, sur tout l'écran. Variante choisie sur maquettes (capture réelle du jeu, mêmes calculs que le shader) : **G « bombé léger »**.

## Ce que fait la variante G
| Élément | Réglage |
|---|---|
| Fisheye (centre grossi, bords tassés, les coins restent aux coins) | 0,06 |
| Courbure des bords (bombé) | 0,12 |
| Coins arrondis noirs (rayon, en fraction de la hauteur) | 0,05 |
| Lignes de balayage : 1 ligne sombre toutes les 3 px de l'image (360 lignes), qui suivent la courbure | 15 % |
| Pixels adoucis à l'horizontale (3 prises à ±1,5 px) | 0,15 |
| Lueur (image floutée ajoutée) | 0,12 |
| Bords assombris (vignette) | 12 % |
| Luminosité remontée pour compenser | ×1,05 |
| Grille RGB | aucune |

## Objectifs, non-objectifs, anti-objectifs
- Objectif : l'aspect de G, sur tout ce qui s'affiche (jeu, interface, effets de `ScreenGlitch`), à 60 i/s sur la borne.
- Non-objectifs : réglage par le joueur dans l'écran Config ; effet sur l'interface Anatidae du toolkit (elle n'est plus montrée, ADR-021).
- Anti-objectifs : texte illisible (HUD, consignes) ; interface rognée sur les bords ; chute sous 60 i/s sur la borne.

## Contraintes
- Built-in RP, WebGL 2, GPU intégré (i5-7200U) : une seule passe plein écran en plus, pas de mipmaps régénérées à chaque image, pas d'allocation par image.
- Invariant 8 (pixels agrandis d'un facteur entier) : le jeu reste dessiné tel quel ; **seule l'image finale est déformée**. Exception assumée, à noter dans une ADR (comme celle de `ScreenGlitch`).
- Rendu toujours en 1920×1080 dans le gabarit Web (ADR-031). Dans l'éditeur, la taille de la Game view.

## Approches envisagées
1. **Image composée dans une texture, puis une passe CRT (retenue).** Le canvas `PixelDisplay` (aujourd'hui en Screen Space Overlay) passe en Screen Space Camera, rendu par une caméra dédiée dans une texture de la taille de l'écran ; un dernier canvas Overlay affiche cette texture en plein écran avec le shader CRT. C'est la seule façon d'avoir la courbure et la lueur sur l'image entière, effets de `ScreenGlitch` compris. Coût : une texture 1920×1080 (8 Mo) et une passe de ~7 lectures de texture par pixel.
2. **Déformer chaque calque dans `Kenta/PixelDisplay`.** Pas de texture en plus, mais la lueur et les coins sont à refaire calque par calque, et les copies et la neige de `ScreenGlitch` ne suivraient pas. Écarté.
3. **Calque plat en multiplication (variantes A à F).** Le moins cher, mais pas de fisheye ni de courbure : ce n'est pas ce qui est demandé. Écarté.

## Conception
- **Shader `Kenta/CrtDisplay`** (`Shaders/CrtDisplay.shader`) : pour chaque pixel de l'écran, calcule le point de l'image d'origine (fisheye puis courbure), lit l'image (3 prises bilinéaires pour l'adoucissement, 4 prises écartées pour la lueur), applique les lignes (sur la coordonnée d'origine), la vignette, les coins arrondis, le gain. Hors de l'image : noir. Réglages en propriétés du matériau (`Materials/CrtDisplay.mat`), valeurs de G par défaut.
- **Composant `FX/CrtScreen`** (sur `PixelScreen`) : crée à l'`Awake` la caméra d'affichage (couche **`Display`**, placée loin des autres caméras, après elles en profondeur) et la texture à la taille de l'écran (recréée si l'écran change de taille) ; passe le canvas `PixelDisplay` en Screen Space Camera sur cette caméra ; affiche la texture par une `RawImage` plein écran sur un canvas Overlay au-dessus. Désactivé : tout revient comme avant (canvas Overlay, sortie masquée).
- **Couche Unity `Display`**, exclue des masques des caméras monde, acteurs et interface.
- **`KentaBuilder`** : crée le matériau, ajoute `CrtScreen` et câble ses références ; la reconstruction reste reproductible.
- **F8** bascule l'effet (éditeur et Development Build, comme F9), pour comparer.

## Laissé au constructeur
- Formules exactes des maquettes (`crt_gen2.py`) à reporter dans le shader ; l'ordre : fisheye, courbure, lecture, lignes, lueur, vignette, coins, gain.
- Vérifier que le canvas Screen Space Camera ne recalcule pas mal sa taille (piège déjà noté dans `PixelScreen`).
- Mesurer les i/s dans un build release sur la borne locale, et comparer avec F8.

## Docs à mettre à jour
ADR-034 dans `docs/decisions.md` ; `docs/art-direction.md` (section affichage) ; `game/CLAUDE.md` (tableau des scripts, F8) ; `docs/roadmap.md`.
