# Direction artistique : Kenta (pixel art) — mise en œuvre

La DA est décrite dans [da-kenta.md](da-kenta.md) (texte) et [DA_KENTA_JEUX_BORNE.pdf](DA_KENTA_JEUX_BORNE.pdf) (**le PDF fait foi** : visuels, palettes, maquettes).
Ce document explique **comment elle est construite dans le projet** et comment la faire évoluer.

## Le principe : tout est en 240 × 135 px, agrandi ×8
- La caméra de jeu écrit dans une `RenderTexture` de **240 × 135 px** (filtre Point). Une `RawImage` plein écran l'agrandit **×8** sur les 1920 × 1080 de la borne (`Scripts/FX/PixelScreen.cs`). Bandes noires automatiques si la fenêtre n'est pas en 16:9.
- Monde : **40 px par unité** → l'écran montre 6 × 3,375 unités. Le sol est à 20 px du bas ; le héros est à 40 px du bord gauche ; les obstacles arrivent de la droite.
- La caméra suit le héros en X seulement et se place toujours sur la grille de pixels (`CameraRig`). Secousses en pixels entiers, parallaxe calée sur la grille.
- L'interface est un canvas « Screen Space – Camera » rendu dans la même texture (1 unité de canvas = 1 px), `pixelPerfect` activé : **tout** (HUD, menus) est en pixel art. Seule l'UI du toolkit Anatidae (saisie des 3 lettres, classement) reste en pleine définition par-dessus.
- Le tour REMIX réduira la définition (`PixelScreen.SetDivider(3)` → 80 × 45) : pas encore branché (voir roadmap).

## D'où viennent les sprites
| Source | Contenu | Où |
|---|---|---|
| **PDF de la DA** (`tools/extract-kenta-pdf.py`) | 3 héros × 8 poses + pose d'attente, 8 ennemis, djembé écrasé / étiré / cassé | `Art/Kenta/Pdf/` |
| **Générateur** (`tools/gen-kenta-assets.py`) | 8 biomes (ciel, 3 calques, sol, trou, mur, bloc, plafond, poteau), pièces, totem, Sourdine, logo, cœurs, cadres, police 5×7 | `Art/Kenta/Gen/` |

Les PNG sont **commités** : inutile de relancer les outils sauf pour modifier un décor ou si le PDF change. Dépendances : `pip install -r tools/requirements.txt` (+ `pymupdf` pour l'extraction du PDF, `librosa` pour la chart).
- Les sprites du PDF sont recadrés pour que le pivot soit **milieu-bas** (les pieds), sans réglage dans Unity.
- Les biomes sont décrits dans `tools/kenta_biomes.py` (palettes issues du PDF) et dessinés par `tools/kenta_art.py`. Ajouter un biome = une entrée dans `BIOMES` + éventuellement un nouveau type de calque dans `kenta_art.py`.
- `tools/pixlib.py` : primitives de dessin sans anti-aliasing, contour noir d'1 px, dégradés en bandes.
- Chaque héros n'a **qu'une image par pose** (le PDF n'en fournit pas plus). Le PDF prévoit 2 à 4 images par action : à dessiner (voir roadmap).

## Import Unity
`Editor/KentaSpritePostprocessor.cs` règle tout automatiquement, par chemin : filtre Point, aucune compression, PPU **40** (monde) ou **100** (interface : un sprite fait sa taille en px dans le canvas), pivot, bordures du cadre 9-slice. **Si on change la taille d'un sprite (mur, bloc, totem), mettre à jour ses pivots dans ce fichier.**

## Reconstruire le projet après un changement d'assets
Menu **Kenta > Reconstruire thèmes, prefabs et scène** (`Editor/KentaBuilder.cs`) : recrée la police (`PixelFont`), les 3 héros, les 8 `BiomeTheme`, `Theme_Kenta`, les 5 prefabs d'écran et recâble la scène `Main` (affichage pixel, canvas, Sourdine, flash, héros, décor, particules). Reproductible : on le relance sans crainte.

## Dans le code
| Élément | Script |
|---|---|
| 8 mondes | `Level/BiomeTheme` (SO) ; un biome = une section de la chart ; `LevelBuilder.BiomeAt(beat)` |
| Décor | `FX/BiomeBackdrop` : ciel fixe (avec le gardien) + 3 calques de 480 px qui bouclent |
| Héros | `Player/HeroProfile` + `HeroSpriteSet` (SO) ; `HeroAnimator` choisit la pose ; la course rebondit d'1 px sur chaque demi-temps |
| Bandeau (traînée de combo) | `FX/BandanaTrail` : 8 à 48 px selon le multiplicateur ×1 → ×8 |
| La Sourdine | `FX/Sourdine` (interface) : avance quand on est touché, recule avec le combo, recouvre l'écran au game over |
| Ennemis | djembé : 2 images alternées sur le temps (`BeatFrames`) ; les autres : saut d'1 px (`BeatHop`) |
| Texte | `UI/PixelText` + `PixelFontData` : une image par caractère, entiers seulement |
| Flash des drops | `FX/ScreenFx` |

## Règles
- Pas de rotation ni d'échelle fractionnaire d'un sprite : tout reste à l'échelle 1:1 (les agrandissements d'interface sont des **entiers**).
- Le contour noir d'1 px est dans les sprites ; ne pas en ajouter côté Unity.
- Lisibilité d'abord : sur la borne, vu debout à 2 m (point à tester, voir roadmap).
