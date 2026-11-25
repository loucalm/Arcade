# Direction artistique : Kenta (pixel art) — mise en œuvre

La DA est décrite dans [da-kenta.md](da-kenta.md) (texte) et [DA_KENTA_JEUX_BORNE.pdf](DA_KENTA_JEUX_BORNE.pdf) (**le PDF fait foi** : visuels, palettes, maquettes).
Ce document explique **comment elle est construite dans le projet** et comment la faire évoluer.

## Le principe : tout est en 320 × 180 px, agrandi ×6
- Trois calques (`Scripts/FX/PixelLayer.cs`), chacun une caméra qui écrit dans sa `RenderTexture` au filtre Point, affichée par une `RawImage` agrandie d'un facteur **entier** (×6 sur les 1920 × 1080 de la borne ; sinon le plus grand entier qui tient, avec bandes noires) :
  - **monde** (décor, obstacles, pièces, particules) : 322 × 182 px (1 px de marge) ;
  - **acteurs** (héros et bandeau, couche Unity `Actors`) : même taille ;
  - **interface** (canvas `PixelUI`, couche `UI`) : 320 × 180 px, fixe.
- Monde : **40 px par unité** → l'écran montre 8 × 4,5 unités. Le héros est à 56 px du bord gauche, le sol de référence à 48 px du bas.
- **Défilement au sous-pixel** : `CameraRig` calcule une position exacte, place les caméras sur la grille (`PixelMath.Split`) et passe la fraction restante à `PixelScreen`, qui décale les images agrandies du monde (X et Y) et des acteurs (Y seulement). Le décor glisse donc au pixel d'écran près, sans à-coups de 6 px.
- Le héros et son bandeau sont recalés sur la grille de la caméra (`CameraRig.ActorShift`) : ils restent parfaitement immobiles horizontalement.
- La caméra suit le héros en hauteur : ancrage sur le dernier sol touché, zone morte, amorti (`SmoothDamp`).
- L'affichage utilise le shader `Kenta/PixelDisplay` (textures déjà prémultipliées : pas de double prémultiplication des éléments semi-transparents).
- Seule l'UI du toolkit Anatidae (saisie des 3 lettres, classement) reste en pleine définition par-dessus.
- Tour REMIX : `PixelScreen.SetDivider(4)` (80 × 45), pas encore branché.

## D'où viennent les sprites
| Source | Contenu | Où |
|---|---|---|
| **PDF de la DA** (`tools/extract-kenta-pdf.py`) | 3 héros × 8 poses + pose d'attente, 8 ennemis, djembé écrasé / étiré / cassé | `Art/Kenta/Pdf/` |
| **Générateur** (`tools/gen-kenta-assets.py`) | 8 biomes (ciel 640×360, 3 calques 1280×360, gardien endormi / éveillé + `guardians.json`, sol, trou, flanc et flanc profond, 4 détails de pilier), pièces, totem, logo, cœurs, cadres, police 5×7 | `Art/Kenta/Gen/` |
| **Gemini** (`tools/gemini-obstacles/`, ADR-026) | obstacles des 8 biomes : mur 38×34, bloc 40×40, plafond 16×16, poteau 8×16, plateforme 32×10. Planches Gemini (`raw/sheets/`) → `cut_sheets.py` → `clean.py --install` (palette du biome, plus proche voisin, contour 1 px, tuiles raccordées) | `Art/Kenta/Gen/biomes/` |

Les PNG sont **commités** : inutile de relancer les outils sauf pour modifier un décor ou si le PDF change. Dépendances : `pip install -r tools/requirements.txt` (+ `pymupdf` pour l'extraction du PDF, `librosa` pour la chart).
- Les sprites du PDF sont recadrés pour que le pivot soit **milieu-bas** (les pieds), sans réglage dans Unity.
- Les biomes sont décrits dans `tools/kenta_biomes.py` (palettes issues du PDF) et dessinés par `tools/kenta_art.py`. Ajouter un biome = une entrée dans `BIOMES` + éventuellement un nouveau type de calque dans `kenta_art.py`.
- `tools/pixlib.py` : primitives de dessin sans anti-aliasing, contour noir d'1 px, dégradés en bandes.
- **Héros : marionnette pixel art** (ADR-022), plus les sprites du PDF (gardés dans `Art/Kenta/Pdf/` pour la vignette). `tools/hero_rig.py` dessine un héros à partir d'un squelette (bassin, buste, tête, bras et jambes articulés) : proportions d'**environ 3,5 têtes** (PDF : ~2), mêmes palettes et signes distinctifs que le PDF (bandeau, écharpe et baguette de Kenta, queue de cheval et flûte d'Aiko, crinière et basse de Ryo). Style : membres effilés (plus fins vers les mains et les pieds), cheveux en mèches courbes (`HAIR` dans `hero_rig.py`), visage avec menton et oreille, ombre en bas-arrière, reflet en haut-avant, membres du fond plus sombres ; **contour noir autour de la silhouette seulement**, et lignes intérieures dans un ton sombre de la pièce de devant (« sel-out ») : un trait noir autour de chaque pièce donnait un effet « pièces détachées ». `tools/gen-heroes.py` (appelé par `gen-kenta-assets.py --only heroes`) produit toutes les images, sur une toile de 96 × 72 px (pivot milieu-bas) : **course** 8 images (deux pas = 2,4 u, bras et jambes opposés, rebond naturel), **saut** 8 images réparties sur sa durée (impulsion, genoux remontés, vrille d'un tour, ouverture, réception), **frappe** 4 images (0,45 beat après l'appui : l'arc part du haut, arc complet gelé, fin de l'arc, retour), **glissade** 3 images en boucle (façon baseball : couché, jambe avant tendue, poussière et traits de vitesse ; ~28 px de haut), avec **3 images pour se jeter au sol** (genoux qui plient, bascule en arrière, jambe qui part devant ; 0,12 s) et **3 pour se relever** (on se redresse, accroupi, on repart ; 0,15 s), **accroupi** (réception d'un saut), attente, mur, liane, touché, mort.
- **Éléments souples simulés** à la génération (Verlet dans le repère du héros, avec le vent relatif de la course) : pans du bandeau, **pan de veste** dans le dos (les trois héros), écharpe de Kenta, queue de cheval d'Aiko (lourde : elle ondule et rebondit), mèches de Ryo. Ils traînent derrière en courant et ondulent, fouettent pendant la vrille, retombent au sommet du saut, partent vers l'avant quand le héros est touché. Régler une chaîne : `strands_for()` dans `gen-heroes.py` (frottement, gravité, flottement).
- **Aucune animation par crans sur le temps** (rebond d'1 px au beat, texte qui saute) : ça se lit comme un tremblement. Mouvements continus (sinus lents, fondus) ou cycles d'images.

## Import Unity
`Editor/KentaSpritePostprocessor.cs` règle tout automatiquement, par chemin : filtre Point, aucune compression, PPU **40** (monde) ou **100** (interface : un sprite fait sa taille en px dans le canvas), pivot, bordures du cadre 9-slice. **Si on change la taille d'un sprite (mur, bloc, totem), mettre à jour ses pivots dans ce fichier.**

## Reconstruire le projet après un changement d'assets
Menu **Kenta > Reconstruire thèmes, prefabs et scène** (`Editor/KentaBuilder.cs`) : recrée la police (`PixelFont`), les 3 héros, les 8 `BiomeTheme`, `Theme_Kenta`, les 5 prefabs d'écran et recâble la scène `Main` (affichage pixel, canvas, flash, héros, décor, particules). Reproductible : on le relance sans crainte.

## Dans le code
| Élément | Script |
|---|---|
| Sol à hauteur variable | `cliff_<biome>` (dessus herbu), `fill_<biome>` (flanc : bandeau clair, puis assombri), `fill_<biome>_deep` (sous 2,4 u, plus sombre), `deco_<biome>_0..3` (pierres, racines, fossiles semés sur le flanc), `platform_<biome>` (plateforme suspendue). Fond des trous : le liquide du biome (`hazard_<biome>`) juste au-dessus de la hauteur où l'on meurt |
| 8 mondes | `Level/BiomeTheme` (SO) ; un biome = une section de la chart ; `LevelBuilder.BiomeAt(beat)` |
| Décor | `FX/BiomeBackdrop` : ciel fixe 640×360, gardien (sprite à part, éveillé une mesure à chaque section et tout le refrain), 3 calques de 1280 px qui bouclent, brume. **Dessinés à la définition de la vue large** (`pixlib.Pix(scale=2)` : composition de l'écran 320×180, tracée en 640×360) : leurs pixels font la taille de ceux du niveau. Ambiance par section (`SetSection` : teinte par type de section, lumière qui baisse au fil du morceau) |
| Héros | `Player/HeroProfile` + `HeroSpriteSet` (SO : `runCycle` 8 images, `jumpFrames` 8, `hitFrames` 4, `slideLoop` 3, `slideIn` 3, `slideOut` 3, `crouch`, poses fixes) ; `HeroAnimator` choisit l'image : course et glissade selon la distance, frappe selon le temps depuis l'appui (`PlayerController.HitAgeBeats`), saut selon son avancement (`JumpProgress`) |
| Bandeau (traînée de combo) | `FX/BandanaTrail` : 8 à 48 px selon le multiplicateur ×1 → ×8 |
| Ennemis | djembé : 2 images alternées sur le temps (`BeatFrames`) ; les autres : immobiles (le sautillement au beat a été retiré) |
| Texte | `UI/PixelText` + `PixelFontData` : police 5×7 **proportionnelle**, ombre d'1 px vers le bas, une image par caractère ; `plate` = plaque sombre arrondie (9-slice) sous les textes posés sur le décor ; agrandissements entiers |
| Cadres | `Gen/ui/panel.png` (9-slice, coins arrondis, liseré clair) ; `Gen/ui/plate.png` (plaque de texte) |
| Flash des drops | `FX/ScreenFx` |

## Règles
- Pas de rotation ni d'échelle fractionnaire d'un sprite : tout reste à l'échelle 1:1 (les agrandissements d'interface sont des **entiers**).
- Le contour noir d'1 px est dans les sprites ; ne pas en ajouter côté Unity.
- Lisibilité d'abord : tout texte posé sur le décor a sa plaque (`plate`) ; sur la borne, vu debout à 2 m (point à tester, voir roadmap).
- Pendant le classement du toolkit (accueil, fin de partie), nos écrans sont masqués pour qu'il ne chevauche pas le logo.
