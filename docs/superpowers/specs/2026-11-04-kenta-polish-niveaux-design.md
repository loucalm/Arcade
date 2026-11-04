# Kenta — polish graphique, niveaux par biome, mort subite, verticalité (design)

Date : 2026-11-04. Base : branche `feat/da-kenta` (PR #6), DA Kenta déjà intégrée (ADR-016).
Retour de Lou après le premier essai : « l'interface est assez moche, tout tremble, les personnages ne se déplacent pas fluidement, ça fait cheap », « un niveau par biome », « mort subite », « on ne voit pas assez arriver les obstacles (dézoomer) », « pas assez de verticalité ». Ajout : « un écran de sélection de niveau, à la osu!, en liste ».

## Intention
Faire passer Kenta de « prototype qui marche » à « jeu d'arcade qui donne envie » : image stable et fluide, interface propre, lecture des obstacles à l'avance, parcours en hauteur, et une structure en niveaux (un monde = un niveau) qu'on choisit comme un morceau dans osu!.

## Objectifs
1. **Fluidité** : plus aucun tremblement. Le décor défile sans à-coups, le héros est parfaitement stable à l'écran, aucune animation par saccades sur le beat.
2. **Dézoom** : on voit nettement plus loin devant le héros.
3. **Interface** : lisible, sobre, cohérente pixel art ; plus d'aspect « cheap ».
4. **Un niveau par biome** + **écran de sélection de niveau** façon osu!.
5. **Mort subite** : un coup = mort, pas de cœurs.
6. **Verticalité** : le parcours monte et descend (marches, plateformes, chemins hauts).
7. **Score qui départage les joueurs** : chaque action est jugée sur sa précision rythmique (PARFAIT / BIEN / BOF), et chaque niveau fini reçoit une note (S+ à D).

## Non-objectifs (pour l'instant)
- Une musique et une chart **différentes par niveau** : prévu plus tard. Pour l'instant **les 8 niveaux utilisent le même morceau et la même chart** ; seul le thème (biome) change. L'architecture doit permettre de donner à chaque niveau sa musique et sa chart sans code (décision de Lou).
- Tour REMIX, 2 joueurs, scènes de drop, courses sur mur / lianes : inchangés (roadmap).
- **Mode marathon** (les 8 niveaux à la suite, une seule partie) : plus tard. L'architecture doit le permettre (une partie = une liste de niveaux ; pour l'instant une liste d'un seul niveau).
- **Éditeur de niveau** (poser les obstacles sur le rythme de la musique, avec lecture/pause/défilement) : plus tard. Le format de chart et les données de niveau doivent rester simples à écrire par un outil.
- Sauvegarde de progression : tout est débloqué.

## Anti-objectifs
- Une image floue ou des pixels de tailles inégales (on reste en pixel art net, agrandissement entier).
- Des tremblements remplacés par d'autres (ex. héros qui ondule d'un pixel quand le décor défile).
- Une baisse de perfs sur la borne (GPU intégré, 60 fps visés).
- Casser le flow imposé par l'enseignant ou les contraintes borne (CLAUDE.md racine).

## Ce qui est décidé

### Flow d'écrans
Accueil (démo + classement) → Consignes → Choix du héros → **Sélection du niveau** → Partie → Game Over + score → saisie du nom si top 10 → Accueil. Le reste du comportement actuel est conservé (« pour l'instant le comportement me va »).
- Fin de niveau réussie : écran de fin « BRAVO ! » avec la **note** (S+…D), le détail PARFAIT / BIEN / BOF et le score, puis la suite normale (classement / saisie). On n'enchaîne pas le monde suivant (ce sera le mode marathon).

### Sélection du niveau (maquette validée : option A inversée)
- **Liste à gauche**, en arc comme osu! : l'entrée sélectionnée ressort vers la droite, plus grande et éclairée ; les autres s'écartent progressivement ; la liste boucle. Chaque entrée : vignette du ciel du biome, « N · NOM », étoiles de difficulté.
- **Infos à droite** : « MONDE N / 8 », nom du monde, gardien, morceau · BPM · durée, difficulté (étoiles), nombre d'obstacles ; en dessous, **top 3 du niveau**.
- Fond = décor complet du biome sélectionné (ciel + calques en parallaxe qui défilent lentement), assombri côté infos. La musique continue (même morceau pour tous pour l'instant).
- Joystick haut/bas = défiler (avec répétition si maintenu), bouton 1 = jouer. Délai : 15 s puis lancement du monde en surbrillance *(mon choix, comme le choix du héros)*.
- Mouvement de la liste interpolé (glisse, pas de saut d'une case).

### Niveaux
- 8 niveaux, un par biome, dans l'ordre de la DA (Sylve, Bayou, Échos, Forge, Échine, Givre, Cité, Orage).
- Un niveau = un asset (ScriptableObject) : biome, morceau, chart, difficulté affichée (étoiles), gardien. Pour l'instant tous pointent vers le même morceau et la même chart.
- Pendant tout le niveau, **un seul biome** (plus de changement de décor par section). Les sections de la chart restent des repères musicaux (drops : flash + secousse), plus des checkpoints.
- Difficulté affichée : 1 à 5 étoiles selon le rang du monde *(mon choix ; la chart étant identique, c'est indicatif tant que les charts ne sont pas différenciées)*.

### Scores
- Le classement de la borne (toolkit) reste **unique**, tous niveaux confondus.
- **Top 3 par niveau** stocké sur la borne via `ExtradataManager` (une clé par niveau) *(mon choix)*. Le top 3 utilise le nom saisi en fin de partie ; si le joueur n'entre pas dans le top 10 global, son score n'apparaît dans le top 3 du niveau que s'il le mérite, sous le nom « ??? » *(mon choix ; à revoir si gênant)*.

### Jugement du timing et note de fin
- Chaque action attendue (saut d'un trou / mur / marche, frappe d'un ennemi / bloc, glissade sous une barre) est jugée par l'écart entre l'appui et son beat dans la chart :
  - **PARFAIT** : |écart| ≤ 45 ms ; **BIEN** : ≤ 90 ms ; **BOF** : au-delà, mais l'action réussit quand même *(seuils : mon choix, à régler en jeu)*.
  - La réussite reste celle d'un plateformeur (invariant n°4 : on ne meurt pas parce qu'on est « hors temps », seulement si on touche l'obstacle). Le jugement ne change que le score et le combo.
- Points de base par action × multiplicateur : PARFAIT 100 %, BIEN 60 %, BOF 20 % *(mon choix)*. Le combo monte avec PARFAIT et BIEN ; un BOF le remet à zéro *(mon choix)*.
- Retour visuel immédiat : le mot (PARFAIT / BIEN / BOF) apparaît près du héros, en couleur.
- **Note de fin de niveau** calculée sur la précision (moyenne pondérée des jugements) et le ratio de pièces : **S+** (tout PARFAIT et toutes les pièces), **S**, **A**, **B**, **C**, **D** *(paliers exacts : mon choix, documentés dans game-design.md)*. Affichée en grand sur l'écran de fin et dans le top 3 du niveau.
- L'autoplay joue PARFAIT (sert à vérifier qu'un S+ est atteignable).

### Mort subite
- Toucher un obstacle ou tomber dans un trou = mort immédiate : pose de mort, la Sourdine avance et recouvre l'écran, puis Game Over.
- Plus de cœurs (HUD, RunManager, respawn, bonus « cœurs restants » retirés). Plus de checkpoints en partie.
- La Sourdine ne dépend plus que du combo (elle recule quand le multiplicateur monte).

### Rendu, fluidité, dézoom
- Définition interne **320 × 180 px, agrandie ×6** (= 1920 × 1080). Monde toujours à 40 px/unité : l'écran montre 8 × 4,5 u au lieu de 6 × 3,375 u. Le héros reste près du bord gauche pour maximiser la vue devant.
- Agrandissement **entier uniquement** ; si la fenêtre n'est pas un multiple (éditeur, autre écran), bandes noires plutôt qu'une mise à l'échelle non entière.
- **Défilement au sous-pixel** : la caméra est calée sur la grille de pixels, et la fraction restante est appliquée en décalant l'image agrandie (rendu avec une marge d'1 px). Le décor glisse donc par pas de 1 px *d'écran* et non de 6.
- **Héros et sa traînée sur un calque séparé**, sans ce décalage, pour qu'ils restent parfaitement immobiles horizontalement.
- **Caméra verticale** : suit le héros en hauteur avec une zone morte et un amorti, calée sur la grille (et lissée par le même décalage sous-pixel).
- Plus aucun sautillement sur le beat (héros, textes, pièces, ennemis) : remplacé par des animations continues (sinus doux, cycles d'images).
- **Course à 6 images** : générées à partir du sprite « course » du PDF en redessinant jambes (et bras si possible) à 6 phases, dans la palette du héros ; cadence liée à la vitesse de course. Idem pour les 3 héros.

### Interface
- Police pixel **proportionnelle** (largeur par caractère) avec **ombre portée d'1 px**, à la place du contour noir épais.
- HUD allégé : score, multiplicateur, progression du niveau (barre fine), compteur de pièces. Plus de cœurs, plus de fausse bande « 1UP / 2UP / CRÉD ».
- Nouveaux cadres (coins arrondis, liseré clair), espacements revus pour la nouvelle définition.
- Le titre « Highscores » du toolkit ne doit plus chevaucher le logo sur l'accueil (le style visuel des UI Highscore est modifiable, cf. CLAUDE.md).

### Verticalité
- **Passage obligé** : une seule route, pas de bifurcation pour l'instant *(mon choix, Lou n'a pas tranché ; plus simple et plus fiable à générer)*.
- Nouveaux éléments de chart : **marche montante** (mur qui devient le sol, à sauter), **marche descendante** (on tombe), **plateforme** au-dessus d'un trou (on saute dessus, on court, on redescend), **chemin haut** avec pièces.
- Hauteurs du sol entre 0 et environ 2 u ; une marche montante ne dépasse pas ce qu'un saut permet (saut actuel 1,5 u en 1 beat).
- Les **hauteurs sont écrites à la main** dans le plan du générateur (`PLAN` de `tools/gen-level-chart.py`, section par section) ; la musique ne fixe que le *moment* des actions (attaques), comme aujourd'hui *(mon choix : le suivi de mélodie sur un mix complet n'est pas fiable, et chaque futur morceau aura de toute façon son plan écrit)*. Mêmes garanties de faisabilité ; l'autoplay doit finir le niveau sans mort.
- Les plateformes sont traversables par en dessous (on ne se cogne pas la tête) ; seul le dessus est solide *(mon choix)*.
- `chart-format.md` est à corriger : il donne encore un sommet de saut à 2,2 u alors que le saut fait 1,5 u.
- Visuels des marches et plateformes par biome (générés comme le reste).

## Contraintes
- Toutes celles du CLAUDE.md racine (borne, rythme, pixel art, flow imposé, `ArcadeInput`, `MenuManager`, toolkit en lecture seule sauf `GameName` et style Highscore).
- Invariant n°8 du CLAUDE.md à mettre à jour (320×180 ×6), ADR à ajouter, docs `art-direction.md`, `chart-format.md`, `game-design.md`, `roadmap.md`, `game/CLAUDE.md` à mettre à jour dans les mêmes commits.
- Assets : générés (`tools/gen-kenta-assets.py`) ou extraits du PDF ; scène et prefabs reconstruits par le menu **Kenta > Reconstruire** (pas d'édition YAML à la main).
- Validation : autoplay complet sans mort, build Web testé dans la borne locale.

## Laissé au builder
- Détails techniques du rendu sous-pixel et du calque héros (caméras, RenderTextures, ordre de rendu).
- Forme exacte des données de niveau et des nouveaux événements de chart (en documentant `chart-format.md`).
- Dessin précis des 6 images de course, des cadres et de la police, tant que le style reste celui de la DA.
- Ordre de travail. *(mon choix proposé : rendu/fluidité → interface → mort subite → niveaux + sélection → verticalité.)*
- Branche : nouvelle branche `feat/kenta-polish` partie de `feat/da-kenta` *(mon choix)*.
