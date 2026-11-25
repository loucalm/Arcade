# Brief direction artistique — Rythme Runner

Récap complet du jeu pour travailler une nouvelle direction artistique (DA). Le document se lit seul, sans connaître le repo : il peut être collé tel quel dans un outil de design.
Sources : [game-design.md](game-design.md), [chart-format.md](chart-format.md), [anatidae.md](anatidae.md), [roadmap.md](roadmap.md), [decisions.md](decisions.md). État au 2026-10-13.

## 1. Le jeu en bref
**Rythme Runner** est un runner/plateformeur 2D en défilement horizontal où toute la traversée est synchronisée sur la musique. L'inspiration directe, ce sont les niveaux musicaux de Rayman Legends (Castle Rock, etc.).

- **Pitch joueur** : « Cours, saute et frappe en rythme. Ne te fais pas toucher. Ramasse les lums pour faire monter le score. »
- **Promesse** : bien joué, chaque saut, coup, glissade et ramassage tombe sur un temps fort. Le joueur a l'impression de **jouer la musique en courant**.
- **Phrase d'accroche actuelle** (fiche de la borne) : « Joue la musique en courant ! »
- **Ton visé** : fluidité, énergie, humour. Le décor et les ennemis « jouent » avec le morceau.

## 2. Contexte
- Projet de BUT MMI 3 (S5), à 3 : Lou Calmes, Lucas Corrieras, Jeremy Hordé.
- Le jeu tourne sur **Anatidae**, la borne d'arcade du couloir MMI : écran 24 pouces en 1920×1080, 2 joueurs, chacun un joystick et des boutons.
- Public : des passants, debout, qui découvrent le jeu sans explication. Il doit être **compris en 10 secondes** et une partie dure **2 à 4 minutes**.
- Deux livrables partagent la même identité visuelle : le **jeu** et un **site one-page** (présentation, personnages, lore, mini-blog, bloc « en direct de la borne »).

## 3. Ce que fait le joueur
Trois actions seulement, plus une automatique.

| Action | Commande | Sert à | Son joué |
|---|---|---|---|
| Sauter | bouton 1 | franchir un trou ou un mur | accent / percussion |
| Frapper | bouton 2 | casser un ennemi ou un bloc | caisse claire / impact |
| Glisser | joystick vers le bas | passer sous un plafond bas ; en l'air, chute rapide | swoosh |
| Liane, crochet, course sur mur | automatique | moments de mise en scène | note |

- Le personnage court **tout seul vers la droite, à vitesse constante**. Il ne s'arrête jamais et ne recule jamais.
- Le saut a une hauteur fixe, dure exactement **1 temps** et comporte une vrille.
- Les sons d'action sont recalés sur la double-croche la plus proche : ils complètent toujours la musique.

## 4. Ce qu'on croise dans un niveau
Le niveau n'est pas dessiné à la main : il est généré à partir d'une partition (« chart ») où chaque élément est posé sur un temps de la musique.

| Élément | Ce que le joueur doit faire | Couleur provisoire |
|---|---|---|
| Trou dans le sol | sauter | — |
| Mur bas | sauter par-dessus | rose `#FF40BF` |
| Ennemi (une variante s'appelle déjà « drum ») | frapper | rouge `#FF4D59` |
| Bloc cassable | frapper | orange `#FF9933` |
| Plafond bas (barre) | glisser dessous | jaune `#FFD933` |
| Lums, en lignes rythmiques | ramasser | jaune clair `#FFF266` |
| Checkpoint (début de section) | — | vert `#66FF99` |
| Liane / crochet, course sur mur | automatique | — |

Trois hauteurs existent : le sol, la hauteur d'un saut, et le haut de l'écran. Les lums peuvent être sur les trois.

**Lums** : chaque ramassage joue une note (gamme pentatonique). Une ligne de lums forme une petite mélodie ou un roulement.

**Événements de mise en scène** déclenchés par la partition : flash du fond, tremblement de caméra, zoom, changement de section.

## 5. Structure d'une partie
- Un morceau de 1 min 30 à 2 min, découpé en sections : **intro, couplet, refrain, break, final**. La densité monte : une action par temps, puis des croches, puis des syncopes.
- **3 cœurs.** Touché : on perd un cœur et on repart au début de la section, musique resynchronisée. Zéro cœur : game over.
- **Score** : lum 10, obstacle franchi 20, ennemi ou bloc 50. Un **combo** fait monter un multiplicateur jusqu'à **×8** ; il retombe à zéro quand on est touché. Bonus de section sans chute (+500, affiché « PARFAIT ! ») et de fin de niveau.
- **Médailles** selon la part de lums ramassés : or (90 %), argent (60 %), bronze.
- **Tour REMIX 8-bit** (prévu, pas encore fait) : à la fin du morceau, on recommence le même niveau sur une version chiptune de la musique, avec des perturbations visuelles qui s'ajoutent à chaque tour (brouillage, zoom, tremblements, zones sombres, image inversée), jusqu'à la mort. C'est la difficulté croissante du jeu.
- **2 joueurs** (prévu) : chacun son tour sur le même morceau, comparaison à la fin.

## 6. Les écrans à habiller
L'enchaînement est imposé par l'enseignant.

| # | Écran | Contenu |
|---|---|---|
| 1 | **Attract + classement** | Le jeu se joue tout seul en démonstration, en alternance toutes les 8 s environ avec le top 10. Logo et « APPUIE SUR UN BOUTON » qui pulse sur le temps. |
| 2 | **Comment jouer** | Une planche : joystick bas = glisser, bouton 1 = sauter, bouton 2 = frapper, « suis la musique ». Passe seule après 6 s environ. Prévue animée, avec des pictos plutôt que du texte. |
| 3 | **Config** (facultatif) | 1 ou 2 joueurs, plus tard choix du morceau. |
| 4 | **Jeu** | HUD : score, multiplicateur, cœurs, lums ramassés, progression du morceau, section, tour de remix. Bandeaux (« C'EST PARTI ! », « CHECKPOINT ») et retours courts (« PARFAIT ! », « AÏE ! »). |
| 5 | **Game over** | « GAME OVER » animé sur le temps, puis score, lums, médaille, tours de remix et classement avec la place du joueur mise en avant. |
| 6 | **Saisie du nom** | 3 caractères, seulement si le score entre dans le top 10. |

## 7. Ce qui rend le jeu intéressant pour une DA
- **La musique est le niveau.** Tout ce qui est à l'écran est posé sur une grille de temps. Le visuel peut donc être musical de bout en bout : formes, espacements, motifs répétés.
- **Tout bouge sur le temps.** Les plateformes pulsent, les ennemis dansent, le fond flashe sur les temps forts. Chaque élément dessiné doit avoir une façon de « battre ».
- **Le joueur est un musicien.** Ses actions produisent des sons qui complètent le morceau. Les ennemis peuvent être des instruments ou des sons (la partition prévoit déjà une variante « drum »).
- **Une menace poursuit le joueur** pour justifier la vitesse imposée : mur de son ou créature. C'est un personnage ou une entité à inventer.
- **Les drops sont des scènes.** Aux changements de section : explosions, effondrement du décor, changement de caméra.
- **Deux identités à concevoir.** Le tour REMIX 8-bit est une seconde version du même monde, plus rétro et plus dégradée à chaque tour. Les deux styles doivent se répondre.
- **La montée du combo se voit.** Traînée derrière le personnage à fort multiplicateur, textes flottants (+50, PARFAIT) dans le monde.
- **Le mode Attract est une vitrine.** Il doit accrocher quelqu'un qui passe dans le couloir, de loin.
- **Le site reprend la DA** : palette, polices et formes du jeu, avec des animations possibles au tempo du morceau.

## 8. Tout ce qu'il faut dessiner
**Personnage** : course, saut (avec vrille), glissade, frappe, accroché à une liane, course sur mur, touché, mort.

**Niveau** : sol (dessus, bords gauche et droit, remplissage), trou, mur, plafond bas, bloc cassable, liane et point d'accroche, checkpoint, lum.

**Ennemis** : au moins une variante de base, avec une animation de danse sur le temps et un état « cassé ». Possibilité de variantes par instrument.

**Menace** qui poursuit le joueur, visible au bord gauche de l'écran.

**Décor** : plusieurs plans en parallaxe (proche, lointain), fond, et sa réaction sur les temps forts.

**Effets** : particules, halos, flash, traînée, annonce d'un obstacle une mesure à l'avance.

**Interface** : logo et titre, police, HUD, cœurs, médailles, bandeaux, pictos des commandes, les 6 écrans, style du classement et de la saisie du nom.

**Version REMIX 8-bit** : la déclinaison de tout ce qui précède, et les perturbations visuelles.

**Hors jeu** : vignette carrée pour le menu de la borne (aujourd'hui un simple dégradé), bande-annonce muette de 20 s maximum (facultative), et le site (section d'accueil, équipe, concept, personnages, lore, blog).

## 9. Contraintes qui pèsent sur la DA
**Lisibilité (la plus importante)**
- Compris en 10 secondes, sur un écran vu debout.
- Le joueur doit savoir **instantanément** quelle action un obstacle demande : sauter, frapper ou glisser. Un code couleur et de forme par action est indispensable.
- Chaque obstacle s'annonce **une mesure avant** (couleur, clignotement, animation).
- Le décor ne doit jamais gêner la lecture du premier plan.

**Machine**
- Processeur i5-7200U avec **carte graphique intégrée**, dans Firefox, à 60 images par seconde.
- Pas de post-traitement plein écran coûteux, pas d'ombres, peu de lumières.
- Le « juice » passe par : sprites en mode additif (halos), particules légères (moins de 300 à l'écran), tremblement d'écran, écrasement et étirement, courts arrêts sur image, flashs sur le temps, animation de l'interface.
- 2D en sprites, caméra sans perspective. Textures de 2048 px maximum, regroupées en atlas.
- Les effets du remix doivent rester des shaders simples.

**Format**
- 1920×1080, paysage.
- Le personnage reste dans la partie gauche de l'écran ; on voit loin devant lui.
- Proportions actuelles : le personnage fait environ 0,7 de large sur 0,85 de haut, un saut monte à 2,2 et un temps de musique vaut 4 unités de long (à 120 BPM).

**Musique**
- Morceau original ou libre de droits, tempo fixe entre 110 et 160 BPM, sections bien marquées. Il n'est pas encore choisi : la DA et le style musical peuvent se décider ensemble.

## 10. État visuel actuel
**Sur la branche principale** : tout est provisoire. Le jeu n'utilise que trois formes blanches teintées (carré, cercle, halo) dans une palette néon sur fond violet très sombre.

| Rôle | Couleur |
|---|---|
| Fond | `#0D081A`, flash `#290F3D` |
| Décor lointain / proche | `#170D2E` / `#241242` |
| Sol | corps `#1F1238`, arête `#4DF2FF` |
| Personnage | cyan `#4DF2FF` |

Le décor est une série de colonnes en parallaxe qui s'illuminent sur les temps forts. Musique et bruitages sont générés par script. Aucun personnage dessiné, aucun logo, aucune police choisie.

**Essai de Lucas, non fusionné** (branche `feat/niveau-musical-neon`, 2026-09-25) : une DA « néon façon Castle Rock » entièrement construite en code à partir de formes simples. Héros sans membres animé en code, ennemis qui dansent, lums ailés, château en silhouettes sur trois plans, horizon en feu (chaud derrière, néon froid devant), mur de flammes qui poursuit le joueur, effondrement du décor aux drops. Un essai avec le pack de sprites Kenney a été abandonné : trop éloigné du style voulu. Cette branche ajoute aussi les lianes, la course sur mur et un niveau complet de 200 temps.

Le code est prêt à recevoir de vrais dessins : un thème regroupe tous les sprites et toutes les couleurs, et se remplace sans toucher au code. On peut avoir plusieurs thèmes, par exemple un pour le tour remix.

## 11. Questions ouvertes pour la DA
- Quel **univers** ? Qui court, où, et pourquoi ?
- Quelle **menace** le poursuit ?
- Que sont les **lums** dans cet univers ?
- Les **ennemis** sont-ils des instruments, des sons, autre chose ?
- Quel **style de rendu** tient sur la borne : aplats, néon, papier découpé, pixel, trait dessiné ?
- Comment le **tour REMIX 8-bit** transforme-t-il le monde ?
- Quel **code couleur** pour sauter, frapper, glisser ?
- Quel **genre musical**, puisqu'il donne le ton visuel ?
- Garde-t-on le titre **Rythme Runner** ? Le nom affiché peut changer ; seul le nom technique du dossier (`RythmeRunner`) est fixé.
