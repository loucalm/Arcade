# Niveau 4 refait : un volcan d'ambiance, la marche entre les Échos et le Main Theme (plan, 27/11)

## Pourquoi
Retour de Lucas après la fusion du niveau 4 de Jeremy (*Fire Maze*, PR #46) : on ne l'a pas fait nous-mêmes, la musique
ne colle pas, et on ne sent pas la difficulté monter. Les niveaux 1, 2 et 3 sont bons. Le 4 doit être la marche entre le 3
(Échos) et le Main Theme (niveau 5), et c'est lui qui doit **commencer à amener les effets d'écran**.

Mesures du simulateur (charts actuelles) :

| Niveau | Morceau | BPM | Marge | Actions/s | Mécaniques | Effets d'écran |
|---|---|---|---|---|---|---|
| 1 Sylve | We're the Resistors | 117 | 160 ms | 0,70 | 4 | aucun |
| 2 Bayou | Arpanauts | 120 | 150 ms | 0,95 | 6 | aucun |
| 3 Échos | Stage 1 | 140 | 120 ms | 1,20 | 9 (+ gravité) | aucun |
| **4 Forge (Jeremy)** | Fire Maze | 151 | 120 ms | **1,32** | 12 (+ tambours, rampes, arrière-plan) | **aucun** |
| 5 à 7 | Main Theme (chart de base) | 150 | 120 ms | 1,42 | 16 | miroir ×7, dédoublé ×4, grésillement ×2 |
| 8 Orage | Orage | 180 | 100 ms | 2,40 | 17 | miroir ×20, dédoublé ×10, éclairs, zooms |
| 9 Sourdine | BIKE | 280 (grille) | 45 ms | 3,07 | 17 | tout |

Le 4 actuel est à 1,32 action/s, presque le 3 (1,20), sans effet d'écran. Puis le niveau 5 sort d'un coup 13 effets
d'écran. La marche manque là.

## Cibles du nouveau niveau 4
- **Tempo** : 140 à 150 BPM (le test de progression impose BPM du 3 ≤ BPM du 4 ≤ BPM du suivant). Un morceau d'ambiance
  plus lent compte en double temps (ex. 72 BPM joué sur une grille de 144).
- **Marge** : 120 ms (comme le 3 et le Main Theme).
- **Densité** : ≈ 1,30 action/s en moyenne, pic d'une phrase entre celui du 3 et celui du Main Theme.
- **Mécaniques** : celles du 3 (ennemis, trous, marches, murs, cuirassés, lianes, chauves-souris, notes de saut, gravité)
  + **tambours** et **rampes** (le volcan monte). L'arrière-plan et les plateformes restent pour le Main Theme.
- **Effets d'écran, introduits comme une mécanique** (seul, répété, varié, combiné) et toujours annoncés par le bandeau
  d'avertissement :
  1. un **grésillement** court sur un drop (sans gêner la lecture) ;
  2. le premier **miroir** (gauche-droite) sur une phrase calme, sans trou ni ennemi pendant le retournement ;
  3. l'écran **dédoublé** (la grille 2×2, le seul dédoublé du jeu) sur une phrase simple ;
  4. au final, un miroir puis un dédoublé sur des gestes déjà connus. Pas de tête en bas ni de demi-tour (Main Theme et plus).

  Total visé : 2 ou 3 miroirs, 1 ou 2 dédoublés, 2 grésillements (le Main Theme en a 7, 4 et 2).
- **Durée** : 1 min 40 à 1 min 50, comme les autres.

## La musique : « volcan d'ambiance » qui garde un battement
Critères :
- sombre et chaud (tambours graves, drones, braises), pas du métal ;
- **mais des attaques nettes** (grosse caisse, taiko, toms) : le générateur pose chaque action sur une attaque du morceau,
  et une nappe sans percussion ne lui donne rien ;
- tempo stable entre 140 et 150 (ou 70 à 75 en double temps) ;
- **licence libre compatible** : CC0, CC BY ou OGA-BY. Si le morceau est en CC BY-NC-ND, il faut l'accord de l'artiste,
  comme pour BIKE, et on ne le coupe pas.

Pistes trouvées (à écouter : rien n'est encore téléchargé) :

| Morceau | Où | Licence | Ce qu'on en sait |
|---|---|---|---|
| *Another Volcano* (Marty Opsahl, Mega Pixel Music Lab) | [OpenGameArt](https://opengameart.org/content/another-volcano) | CC BY 3.0 (et autres) | exploration d'un volcan : cordes, chœur, cuivres, caisse claire, grosse caisse ; boucle |
| *Lava Area Theme* (Wolfgang_) | [OpenGameArt](https://opengameart.org/content/lava-area-theme) | CC BY 4.0 | thème d'une zone de lave ; batterie non confirmée |
| *Chaos ID* (Soularflair) | [Free Music Archive](https://freemusicarchive.org/music/Soularflair/cc-by-free-to-use-for-anything-1/chaos-id-tribal-instrumental-metal) | CC BY | tribal sombre (penche vers le métal) |
| *Mad Totems* | [OpenGameArt](https://opengameart.org/content/mad-totems) | à vérifier | tribal |
| *Tower of Lava* | [OpenGameArt](https://opengameart.org/content/tower-of-lava) | à vérifier | lave |
| *Ambience of a Fallen Age* (Umplix) | OpenGameArt | CC0 | ambiance de volcan, **sans batterie** : à garder pour un menu, pas pour un niveau |

Une proposition de l'équipe passe avant cette liste.

**Choix de Lucas (27/11) : *Chaos ID (tribal, instrumental, metal)*, de Soularflair.**
- Licence : CC BY 4.0 (album « CC-BY FREE TO USE FOR ANYTHING », sorti le 04/10/2025) : modification permise, en
  créditant l'artiste.
- Crédit demandé : « Music by Soularflair and @canmastersmash (IG) - Chaos ID ». **Condition posée par l'artiste sur
  sa page : le prévenir** quand on utilise sa musique (adresse sur la page FMA). C'est à faire par l'équipe avant la
  sortie, en lui disant ce qu'on en fait (jeu d'école sur borne, version 8-bit).
- 1 min 55 (pas besoin de couper), MP3. Le téléchargement demande un compte FMA.
- Tempo inconnu : le genre est indiqué « progressive », donc il faut vérifier qu'il ne change pas en cours de route.

Lucas aimait l'idée d'une **version 8-bit** : l'essai (étape 2 bis) sonnait faux sur le morceau entier, **on garde
l'original** à 125 BPM (ADR-044). Plus lent que les Échos : la difficulté monte par la densité et les effets d'écran.

## Étapes
1. **Choisir le morceau** (Lucas, à l'oreille). Ensuite, avec l'accord de télécharger : mesure du tempo et du premier
   temps, et carte des attaques (`tools/analyse-song.py`, `tools/accents-song.py`). On vérifie qu'il y a des attaques sur
   au moins 80 % des temps forts.
2. **Monter le morceau** à 1 min 45 environ (`tools/cut-song.py`, mesures entières), seulement si la licence permet de
   le modifier.
2 bis. **Version 8-bit (essai)** : un clip à part, construit à partir de l'analyse du morceau :
   - batterie rejouée au bruit et aux impulsions sur les coups détectés ;
   - basse et riff suivis à la hauteur, rejoués en carré ou en triangle ;
   - sous le tout, le morceau d'origine réduit en 8 bits et 11 kHz, pour la texture.

   D'abord un extrait de 30 s à écouter. Une transcription automatique d'un mix de métal dense ne sera pas parfaite :
   si l'essai ne convainc pas, on garde l'original. **Résultat (27/11) : sonnait faux sur le morceau entier, abandonné.**
3. **Réécrire `tools/levels/n4_forge.py`** : même nom de fichier (le test de progression range les niveaux par nom),
   nouvelle fiche (morceau, BPM, marge 120 ms, mécaniques ci-dessus) et scènes calées sur la forme du morceau : intro,
   montée, couplet, refrain, pont, refrain, final. Chaque nouveauté (tambours, rampes, effets d'écran) est apprise en
   4 temps.
4. **Générer et vérifier** :
   - `python3 tools/gen-level.py n4_forge`, puis `pytest tools/tests` (progression, jouabilité, marges, pièces) ;
   - calage des actions sur la musique (écart médian visé : moins de 10 ms) ;
   - autoplay complet en jeu, sans vie perdue ;
   - partie à la main par Lucas, en normal puis en difficile.
5. **Brancher dans le jeu** : `Song_04_forge` (clip, BPM, premier temps), crédits du morceau. Le biome Forge et ses
   sprites restent. Vignette de sélection : `python3 tools/gen-kenta-assets.py --only thumbs`.
6. **Prévenir Jeremy** : son *Fire Maze* reste dans l'historique de `main`. Lui demander s'il veut le garder ailleurs
   (tour REMIX, niveau bonus).
7. **Doc** : ADR (nouveau niveau 4, la place des effets d'écran dans la progression), roadmap, `game/CLAUDE.md`.

## Ensuite : le niveau 8 descend en 6 ou en 7
Lucas finit l'Orage en difficile du premier coup : il n'est pas assez dur pour être l'avant-dernier. À trancher avant
de le bouger :
- **6 ou 7** ? Le 6 et le 7 jouent encore la chart de base (Main Theme) : celui qu'on ne remplit pas reste à faire.
- **Biome** : la DA place l'Orage en 8e et dernier biome (le sommet). Soit on change l'ordre des biomes (le Givre ou la
  Cité passe après), soit on garde son morceau et on le fait jouer dans le décor du 6 ou du 7.
- **Ce qu'il faut toucher** :
  - l'ordre du `LevelCatalog` ;
  - les fichiers `Level_0X` et `Song_0X` ;
  - le nom de la fiche (`n8_orage.py` devient `n6_…` ou `n7_…`) : le test de progression range les niveaux par nom ;
  - sa marge (100 ms) et son tempo, qui doivent rester entre ceux de ses voisins ;
  - les vignettes et le classement par niveau (les scores enregistrés sont rangés par `id`).
