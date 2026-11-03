# Direction artistique « KENTA » (proposition, octobre 2026)

Version texte de [DA_KENTA_JEUX_BORNE.pdf](DA_KENTA_JEUX_BORNE.pdf) (34 pages). **C'est le PDF qui fait foi** : il contient les visuels, les maquettes d'écrans et les palettes de chaque biome (codes hex sous chaque maquette).
Statut : **proposition à valider en équipe**, pas encore une décision (voir § 10). À relire avant tout travail sur les visuels, l'UI, le niveau ou le site.

## 1. Trois partis pris
1. **Rayman × Geometry Dash** : le décor vit et joue la musique, comme dans les niveaux musicaux de Rayman Legends. Les obstacles arrivent sans prévenir, au rythme de la musique : c'est au joueur de les lire, comme dans Geometry Dash.
2. **Pixel art, puis 8-bit** : tout le jeu est en pixel art. Décors en **240 × 135 px** (×8 = 1920×1080, mise à l'échelle entière). Personnages d'environ **50 px** de haut, avec un **contour noir d'un pixel**. Le tour REMIX divise encore la résolution et réduit le décor à 4 tons.
3. **Un monde par section** : huit biomes enchaînés dans un seul run. Chaque drop de la musique est une scène de transition : effondrement, chute, éruption, avalanche.

## 2. L'univers : la Partition vivante
Dans ce monde, la nature fait la musique : les arbres sont des tambours, les cristaux des xylophones, les volcans des amplis. Une tache de silence, **la Sourdine**, avale le son et la couleur de tout ce qu'elle touche.
Kenta porte le **Bandeau de la Partition**, où est gravée la dernière mélodie intacte. Il court la jouer au sommet du monde pour relancer l'orage, le morceau du monde. S'arrêter, c'est se faire avaler.

| Rôle | Dans l'univers |
|---|---|
| Héros | Un aventurier musicien, bandeau au front. Ses actions jouent des sons qui complètent le morceau. |
| Menace | La Sourdine, au bord gauche. Elle justifie la course forcée vers la droite. |
| Pièces (= les « lums » du code) | Pièces d'or frappées d'une croche, tombées de la partition. Chacune joue une note. Une grosse pièce rare est cachée dans chaque biome. |
| Ennemis | Des instruments rendus fous par la Sourdine. Les frapper les réaccorde. |
| Gardiens | Huit statues géantes endormies, une par biome. À chaque drop, la statue s'éveille, frappe le temps fort et ouvre le passage : c'est le checkpoint. |
| Remix | La Sourdine contre-attaque : elle compresse le monde en pixels, un peu plus à chaque tour. |

## 3. Le voyage : du sous-bois au sommet de l'orage
8 biomes × 8 mesures à environ 128 BPM ≈ 2 min. Un biome = un thème chargé au checkpoint.
Obstacles dans l'ordre : trou / mur bas / bloc / plafond / liane-crochet.

| # | Biome | Section | Lieu et gardien | Musique | Obstacles | Ennemi local | Drop vers le biome suivant |
|---|---|---|---|---|---|---|---|
| 1 | **La Sylve** | intro, m. 1–8 | Forêt-tambour · Cerf de pierre | folk percussif : bois, shakers, flûte | racines arrachées / souche / tronc creux / branche basse / liane fleurie | Djembé-champignon | Un tronc géant frappe le sol sur le dernier temps : le sol cède, chute verticale vers le marais. |
| 2 | **Le Bayou** | couplet A, m. 9–16 | Marais brumeux · Grenouille-lune | swamp blues : banjo, contrebasse, washboard | eau noire / souche moussue / tonneau / racines pendantes / mousse suspendue | Crapaud-grosse caisse | Un siphon s'ouvre au milieu de l'eau : glissade dans un tunnel de racines jusqu'à la grotte. |
| 3 | **Les Échos** | couplet B, m. 17–24 | Grotte de cristaux · Chauve-souris de cristal | électro : arpèges, basse synthé, réverb | gouffre / cristal / géode / stalactites / cristal suspendu | Chauve-souris maracas | Les cristaux virent au rouge mesure après mesure et éclatent ensemble : la paroi s'ouvre sur la lave. |
| 4 | **La Forge** | refrain, m. 25–32 | Volcan, fleuve de lave · Taureau-enclume | métal : guitare saturée, double pédale | lave / basalte / roche en fusion / poutre de forge / chaîne | Ampli-crabe | Éruption : un geyser projette le héros vers les sommets. Arrêt sur image en haut de l'arc, la musique coupe. |
| 5 | **L'Échine** | break, m. 33–40 | Montagne, rochers · Chat-montagne | épique : taiko, cuivres | crevasse / rocher / pierre fendue / arche rocheuse / corde de pont, falaise | Golem-gong | Avalanche : le héros court sur une paroi de rochers qui tombent et débouche sur le glacier. |
| 6 | **Le Givre** | couplet C, m. 41–48 | Glacier, aurores · Renard de glace | synthwave froide : glockenspiel, nappes | glace brisée / bloc de glace / glaçon fêlé / pics de glace / stalactite | Morse-xylophone | La banquise se fend sur un temps fort : glissade sur la pente jusqu'aux portes de la cité. |
| 7 | **La Cité Sourde** | pré-final, m. 49–56 | Ruines de la cité musicale · Lapin de grès | funk : cuivres, basse slap | dalle effondrée / colonne tombée / statue / arche / cloche | Gargouille-trompette | Le clocher s'effondre sur le temps : le héros escalade les débris jusqu'aux nuages. |
| 8 | **Le Grand Orage** | final, m. 57–64 | Sommet dans les nuages · Oiseau-tonnerre | rock orchestral : tout le groupe, chœurs | vide entre deux nuages / nuage dense / nuage chargé / éclair horizontal / cerf-volant | Nuage-timbale | Dernière note : l'orage se change en feu d'artifice. Écran blanc, puis le monde repart en 8-bit pour le tour REMIX. |

**Sur le temps**, chaque biome a son décor qui joue :
- **Sylve** : champignons-cymbales qui s'ouvrent, lucioles sur les croches, tronc creux qui lâche une onde de feuilles.
- **Bayou** : bulles qui éclatent, nénuphars qui montent et descendent, grenouilles qui coassent sur les contretemps.
- **Échos** : cristaux en arpège de gauche à droite, chaque goutte d'une stalactite joue une note.
- **Forge** : geysers et braises sur les temps forts, enclumes géantes au loin.
- **Échine** : rochers qui roulent au loin, rubans des cairns dans le vent, caméra qui s'ouvre en grand.
- **Givre** : stalactites qui tintent comme un glockenspiel, aurore qui ondule sur la mesure, sol qui craque sur les temps forts.
- **Cité Sourde** : colonnes-orgues qui soufflent, statues qui hochent la tête, cloches en fin de mesure. Les zones grises ont déjà été avalées.
- **Orage** : éclairs sur les temps forts (flash du fond), nuages-timbales qui rebondissent, pluie en double-croches.

Pendant un drop, le joueur ne lâche jamais la manette.

## 4. Les héros : les porteurs du bandeau
Aventuriers musiciens chibi en pixel art (environ 50 px, contour noir d'un pixel). Leur bandeau porte la partition : **ses deux pans s'allongent avec le multiplicateur et forment la traînée de combo**.

| Héros | Description | Plus / moins |
|---|---|---|
| **Kenta**, le batteur (recommandé en joueur 1) | Pics roux, bandeau blanc gravé d'une croche, écharpe orange, deux baguettes dans le dos. Sa frappe claque comme une caisse claire. | + énergique, lisible de loin, héros évident · − frappe très typée percussion |
| **Aiko**, la flûtiste | Longue queue de cheval turquoise, bandeau violet, veste blanche croisée. Sa flûte de bambou sert de sabre : chaque coup joue une note. | + silhouette la plus élégante, cheveux qui dansent · − cheveux longs = plus d'images à animer |
| **Ryo**, le bassiste | Crinière blanche, bandeau rouge, basse portée dans le dos comme une épée. Plus calme, plus âgé. | + rival idéal du mode 2 joueurs face à Kenta · − moins « héros » au premier regard |

**Huit poses, un seul gabarit** (identiques pour les trois héros), 2 à 4 images par action, calées sur le temps :

| Pose | Détail |
|---|---|
| Course | bras en arrière, bandeau qui claque |
| Saut + vrille | tour complet sur 1 temps |
| Glissade | sous les branches basses |
| Frappe | arc de frappe + gel de 3 images |
| Liane | suspendu, jambes qui balancent |
| Course sur mur | le monde pivote de 90° |
| Touché | flash blanc 3 temps, « AÏE ! » |
| Mort | se couche, ses notes s'envolent |

## 5. La Sourdine (la menace)
Un mur de silence au bord gauche de l'écran (masse noire, deux yeux blancs à pupille rouge, un sourire). Tout ce qu'il couvre perd sa couleur et son son : **un simple calque gris, sans shader**.
Elle se rapproche quand le joueur est touché et recule quand le multiplicateur monte : **la distance montre l'état du combo**.
Sa forme par biome : Sylve brume grise · Bayou vase noire · Échos ombre · Forge cendre froide · Échine avalanche grise · Givre blizzard · Cité Sourde poussière · Orage nuage noir.

## 6. Les ennemis : l'orchestre détraqué
**Règle commune** : yeux blancs, une danse sur 2 temps, un état « cassé » qui libère des pièces.

Une variante-instrument par biome :
- **Djembé-champignon** (Sylve) : sautille, son chapeau rebondit. C'est l'ennemi de base, variante « drum ».
- **Crapaud-grosse caisse** (Bayou) : sa gorge gonfle sur le temps fort.
- **Chauve-souris maracas** (Échos) : ailes en double-croches.
- **Ampli-crabe** (Forge) : pinces sur la caisse claire.
- **Golem-gong** (Échine) : dodeline, résonne quand on le frappe.
- **Morse-xylophone** (Givre) : frappe ses défenses.
- **Gargouille-trompette** (Cité Sourde) : souffle sur les temps forts.
- **Nuage-timbale** (Orage) : gonfle sur la mesure.

Le Djembé-champignon détaillé :
- **Temps 1, écrasé** : le chapeau s'aplatit comme une peau de tambour frappée.
- **Temps 2, étiré** : il saute sur place, pieds décollés. Boucle de 2 temps.
- **Cassé** : le chapeau se fend en deux, 3 pièces s'échappent, arrêt sur image de 3 images.

## 7. HUD : tout en haut, rien au milieu
- **Gauche** : score et multiplicateur.
- **Centre** : la frise des 8 biomes sert de barre de progression du morceau (« COUPLET B · LES ÉCHOS · TOUR 1 »).
- **Droite** : 3 cœurs et pièces ramassées (ex. 48 / 120).
- **Dans le monde** : +50, PARFAIT !, CHECKPOINT. Ils pulsent sur le temps puis s'effacent en 1 mesure.
- **Traînée** : elle s'allonge avec le multiplicateur, de ×1 à ×8.

## 8. Les 7 écrans de la borne
Police pixel pour l'UI, cadres sombres à contour clair, accent jaune-or. Bandeau d'arcade en haut de l'accueil : 1UP, HI-SCORE, 2UP, CRÉDIT.

1. **Accueil** : décor de la Sylve seul, sans personnage ni élément de jeu. Logo KENTA, sous-titre « LA COURSE EN RYTHME ». Le bouton « APPUIE SUR START » rebondit sur le temps et les pièces tournent autour du logo. Alterne toutes les 10 s avec le classement.
2. **Choix du héros** : joystick gauche/droite, bouton 1 pour valider. Le héros choisi monte d'un cran et joue sa pose de victoire. 12 s pour choisir, sinon Kenta. Le joueur 2 rejoint avec son START (cadre turquoise).
3. **Consignes** (« COMMENT JOUER ») : Kenta mime chaque geste en boucle, sur le temps. Les boutons sont dessinés comme ceux de la borne.
   - Textes : « 1 SAUTE : trous, souches, rochers », « 2 FRAPPE : ennemis-instruments », « ▼ GLISSE : branches et poutres basses », « RAMASSE LES PIÈCES », « NE T'ARRÊTE JAMAIS, LA SOURDINE TE SUIT ».
   - Passe seul après 6 s, ou dès qu'un bouton est pressé.
4. **Début de partie** : « PRÊT ? 1 2 3 GO! », le décompte suit les 4 temps de la mesure d'intro. La Sourdine sort du bord gauche sur « GO ! ». Rien à presser : le run démarre sur la mesure.
5. **Partie perdue** : la Sourdine recouvre l'écran et tout passe en gris. « GAME OVER » tombe sur le temps fort suivant. Panneau : score, pièces, biome atteint, rang. « CONTINUER ? » 10 s avec un crédit. Si le score entre dans le top 10 : saisie du nom.
6. **Trois lettres** (« NOUVEAU RECORD ! ») : joystick haut/bas pour la lettre, bouton 1 pour passer à la suivante. La lettre active clignote en or sur le temps et le héros choisi fait sa pose de victoire à droite. Validation automatique après 20 s.
7. **Classement** (« MEILLEURS SCORES ») : top 10 sauvegardé sur la borne. Trois pièces or, argent et bronze devant les trois premiers, et le dernier score entré clignote en or. Retour à l'accueil après 8 s.

## 9. Tour REMIX 8-bit : le même monde, compressé
Mêmes formes, seul le rendu change : le joueur reconnaît tout immédiatement. Tour normal en pixel art 240 × 135 ; tour REMIX en **80 × 45**, palette de 4 tons (vert façon console portable), pièces en or.

| Tour | Ce qui s'ajoute |
|---|---|
| 1 | Chiptune, résolution ÷ 3, décor en 4 tons |
| 2 | Brouillage : lignes de balayage, bruit |
| 3 | Zoom qui pulse sur les temps forts |
| 4 | Zones sombres : la Sourdine mange l'écran |
| 5 | Image inversée gauche/droite |
| 6 et plus | Résolution ÷ 4, décor en 2 tons |

## 10. Logo et décisions à trancher
**Nom retenu : KENTA.** Trois traitements du logo (lettres pixel épaisses) :
- **A, orange écharpe** (recommandé) : couleur de l'écharpe de Kenta, ombre rouge, contour noir épais. Se lit de loin, même sur un décor clair.
- **B, blanc bandeau** : plus sobre, idéal sur les décors sombres (grotte, orage).
- **C, or pièce** : rappelle le score et l'arcade. Moins lié au héros.

À trancher ensemble :
1. Le traitement du logo, et le joueur 2 (Ryo ou Aiko).
2. Tester la lisibilité des obstacles sur la borne, vu debout à 2 m.
3. Le morceau : tempo fixe vers 128 BPM, 8 sections. Option courte : 4 biomes de 16 mesures.
4. Dessiner la Sylve en entier d'abord : elle sert de référence aux sept autres thèmes.

## 11. Points à confronter aux contraintes du projet
Repérés en comparant le PDF à [anatidae.md](anatidae.md), [game-design.md](game-design.md) et au code. Ils sont à régler avant d'implémenter.
- **« CONTINUER ? » avec un crédit** : la borne n'a pas de système de crédit. Le bouton `Coin` est le bouton blanc, qui ramène au menu de la borne. Le flow imposé va directement du game over à l'enregistrement, puis à l'accueil.
- **Nom du jeu** : le nom affiché (`info.json` → `name`) peut devenir « Kenta ». En revanche, `HighscoreManager.GameName` et le dossier de build sont aujourd'hui `RythmeRunner`. Les changer change aussi la clé des scores sur la borne, et plusieurs docs.
- **Vocabulaire** : les « pièces » du PDF sont les `lum` / `lum_line` du code et des charts. Il faut choisir un seul terme.
- **Musique** : 8 biomes × 8 mesures ≈ 2 min à 128 BPM, ce qui colle à la durée visée. Les 8 genres musicaux supposent un morceau composé exprès, avec sa version 8-bit au même tempo.
- **Remix** : la baisse de résolution passe par un rendu en basse définition agrandi. C'est peu coûteux et compatible avec le GPU intégré, à valider sur la borne. Le gris de la Sourdine est un calque, sans shader : c'est conforme.
- **Thèmes** : un thème par biome correspond au `LevelTheme` actuel (ScriptableObject), à charger au checkpoint.
