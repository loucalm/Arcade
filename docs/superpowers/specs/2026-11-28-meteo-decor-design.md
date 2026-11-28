# Météo et événements du décor, calés sur la musique — design (2026-11-28)

Demande de Lou : des niveaux plus vivants. Pluie à un moment du Bayou (niveau 2), neige au Givre (niveau 6),
éruptions au loin à la Forge (niveau 4). **Décor pur** : rien ne change au jeu ni au générateur de niveaux.
Le décor **suit la musique**.

## Objectifs
- La météo **arrive et repart sur une section** du morceau, monte aux refrains et aux drops.
- Elle **bat sur le temps** : on lâche les gouttes en double-croches, les rafales de neige partent sur le 1 de la
  mesure, les braises sur les temps forts. Le mouvement reste continu : pas de déplacement par crans sur le beat
  (règle du projet, lu comme un tremblement). Seul le moment où les particules apparaissent suit le rythme.
- Les 3 niveaux cités d'abord ; le même modèle servira aux autres (lucioles de la Sylve, pluie de l'Orage…).

## Non-objectifs, anti-objectifs
- Aucun obstacle, aucune gêne de lecture : la météo reste **derrière le niveau**. Elle ne passe jamais devant le
  héros, les ennemis ou les pièces. *(mon choix : c'est ce que « décor pur » implique)*
- Pas de `ParticleSystem`, pas d'effet DSP, pas de flash blanc pour l'éruption.
- Échec, même si l'effet marche : une chute de fps sur la borne, ou une météo qui masque un obstacle.

## Contraintes
- GPU intégré : ≤ ~300 particules actives au total. `FxPool` en a déjà 256 → la météo en a **120 au plus**.
- Pixel art : sprites sur la grille, pas de rotation ni d'échelle fractionnaire.
- L'audio est l'horloge maître : une reprise à un checkpoint doit retrouver la bonne météo sans rien rejouer.
- Épilepsie photosensible : la lueur de l'éruption est une teinte qui monte et redescend par paliers, pas un flash.
  Au plus une éruption tous les 2 temps (comme les éclairs, ADR-041).

## Approches considérées
- **A. Événement de chart posé à la main dans chaque niveau.** Contrôle au temps près, mais tout à écrire.
- **B. Automatique par section** (`BiomeTheme` dit « pluie aux refrains »). Pas de travail par niveau, mais pas de
  moment unique : chaque refrain se ressemble.
- **C. Mélange — choisi par Lou.** Le *visuel* est décrit par biome dans `BiomeTheme`. Le *déclenchement* vient
  d'événements de chart, posés en une ligne par le script du niveau. Contrôle de A, coût proche de B, même chemin
  que les éclairs (ADR-041).

## Design
**Chart (deux événements nouveaux)**
- `weather` : `variant` = `rain` | `snow` | `embers`, `intensity` 0..1, `length` = durée de la transition en
  temps. C'est un **état** : une courbe par variante (`FX/CorruptionCurve`, réutilisée), lue sur `SongBeat`.
  Une reprise à un checkpoint retrouve donc la bonne intensité d'elle-même. `intensity 0` = la météo s'arrête.
- `eruption` : coup unique sur un temps (`intensity`), comme `lightning`. Le générateur vérifie l'écart de 2 temps.
- `levelgen/builder.py` : `L.weather(beat, kind, intensity, length)` et `L.eruption(beat, intensity)`. Le simulateur
  les ignore (visuel seulement). `LevelBuilder` : `weather` → courbes, `eruption` → `FxEvents`.

**Rendu : `FX/WeatherFx`** (nouveau, enfant du décor)
- Un pool de 120 `SpriteRenderer` créé au démarrage, sans allocation en jeu. Placé entre le calque proche et la brume
  (`firstSortingOrder + 38`), donc devant le décor et derrière tout le niveau.
- Les particules vivent dans le repère de la vue. Elles glissent un peu avec la parallaxe du calque proche.
- **Pluie** : traits de 1×4 à 1×6 px en biais. On en lâche à chaque double-croche (`SongBeat × 4`), nombre ∝ intensité.
  Le ciel s'assombrit avec l'intensité (multiplié à la teinte de section de `BiomeBackdrop`).
- **Neige** : flocons de 1 à 2 px, chute lente avec ondulation continue. Une rafale (plus de flocons, vent de côté)
  sur le 1 de chaque mesure. Le ciel bleuit un peu.
- **Braises** : points de 1 à 2 px orange/jaune qui montent, lâchés sur les temps 1 et 3.
- **Éruption** (`BiomeBackdrop.Erupt`) : une gerbe de lave jaillit d'un cratère du calque lointain, avec des blocs
  qui retombent et un panache. Le ciel rougit puis s'éteint par paliers en ~1 s. Petite secousse (`rig.Shake`).
  Les cratères sont lus sur le dessin : `gen-kenta-assets.py` écrit `Gen/biomes/vents.json` (x, y en px du calque
  1280×360), `KentaBuilder` le copie dans `BiomeTheme.vents`. La gerbe suit le calque quand il défile.

**Données du biome (`BiomeTheme`)** : `weatherSprites` (goutte, flocon, braise), `eruptionSprites` (gerbe, bloc,
fumée), `vents`. Sprites dessinés par code dans `tools/kenta_art.py` (`gen-kenta-assets.py --only biomes`), puis
menu **Kenta > Reconstruire**.

**Mise en scène des 3 niveaux** *(proposition, à ajuster à l'écoute)*
- **Bayou** (`n2_bayou.py`) : ciel clair jusqu'au `pont` (128). Pluie fine au `pont`, averse à la `montée` (176).
  Elle se calme et s'arrête pendant la `fin` (192).
- **Givre** (`n6_givre.py`) : neige légère dès l'intro, plus dense aux `refrain`, tempête à l'`avalanche` (240),
  presque rien pendant la `caverne` (112).
- **Forge** (`n4_forge.py`) : braises tout le niveau, plus nombreuses aux refrains. Une éruption sur le 1 de chaque
  `refrain` (96, 156, 216), puis une toutes les 2 mesures dans le `final`.

**Refonte du décor de la Forge (fidèle au PDF, page 9)**
Constat (comparaison `docs/img/forge-pdf-vs-jeu.png`) : le décor actuel ne correspond pas à la maquette. Lou a choisi
de le **redessiner par code** (`tools/kenta_art.py`, palette du PDF `#3A1A16 #5A3A30 #D8401A #FFA040 #FF7A1A
#8A1A0A`), comme tous les autres décors, plutôt que par une planche Gemini.
- **Calque lointain (`volcano`)** : un seul **grand volcan** par boucle de 1280 px, qui domine la vue (sommet vers le
  tiers haut de l'écran). Flancs ombrés en deux tons avec arêtes, **cratère qui brille** (lèvre `#FFA040`), **deux
  coulées sinueuses** au cœur clair et aux bords sombres, **gros nuage de cendre** en bandes. Autour, pics de basalte
  plus bas et silhouettes de **cheminées et piliers de forge**. Le cratère est la source de l'éruption
  (`vents.json`).
- **Calque du milieu (`lavarocks`)** : pics de basalte déchiquetés de tailles variées, sombres, avec quelques
  fissures de lave. On ne répète plus le même rocher en losange.
- **Calque proche** : les arbres orange restent (ils sont dans le PDF). On reprend leur forme (couronne ronde, reflets)
  et on ajoute une **chaîne qui pend** de temps en temps.
- **Ciel** : inchangé (bandes et braises déjà conformes).
- Après le dessin : `gen-kenta-assets.py --only biomes`, puis `--only thumbs` (vignette), puis menu
  **Kenta > Reconstruire**.
- **Hors de ce chantier** : le lac de lave qui reflète le héros, sous le sol. Le sol appartient au niveau, pas au
  décor. C'est à ajouter à la roadmap en P2.

**Tests et validation**
- EditMode : la courbe météo après un `Seek` (reprise) donne la même intensité que sans reprise.
- pytest : le générateur refuse deux `eruption` à moins de 2 temps.
- Menu **Kenta > Tester …** pour un passage de chaque niveau. Mesure des fps en build Web avant de valider.

**Docs (même commit)** : `chart-format.md` (2 types), ADR-047 dans `decisions.md`, roadmap (cocher les lignes
« États du thème » pour ces 3 biomes), `game/CLAUDE.md` (ligne `FX/WeatherFx`).

## Laissé au builder
Couleurs exactes, densités, vitesses de chute, forme des sprites : à régler à l'œil dans le jeu, en vue 480 et 640.
