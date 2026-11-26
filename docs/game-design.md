# Game design — Kenta

Inspiration : les « music levels » de Rayman Legends. Un plateformeur 2D en scrolling horizontal où **toute la traversée est synchronisée sur la musique**. Bien joué, chaque saut, coup, glissade ou collecte tombe sur un temps fort : le joueur a l'impression de **jouer la musique en courant**.

## Pitch arcade (lisible en 10 s)
« Cours, saute et frappe en rythme. Ne te fais pas toucher. Ramasse les pièces pour faire monter le score. »

**Univers, héros, huit mondes, ennemis, écrans** : voir [da-kenta.md](da-kenta.md). Les « pièces » du PDF sont les `lum` / `lum_line` du code et des charts.

## Commandes (deux boutons)
| Action | Entrée | Son |
|---|---|---|
| Sauter (hauteur fixe, le saut dure **1 beat**, avec une vrille) | **B1** | accent/percussion quantifié |
| Frapper (casse blocs et ennemis) | **B2** | caisse claire/impact |
| Course sur les murs, lianes, crochets | **contextuel, automatique** | note |
| Menus : valider | Start ou B1 | — |

## Boucle de jeu
- Course **constante** vers la droite, jamais d'arrêt ni de retour en arrière.
- Le rythme dicte l'espacement des obstacles (trous, ennemis, murs, plafonds bas, plateformes).
- Les **lums** sont posés en lignes rythmiques : chaque ramassage joue une note ou un tick, et les suites forment des mélodies ou des roulements.
- Le décor réagit à `OnBeat` : plateformes qui pulsent, ennemis qui dansent, fond qui flashe sur les temps forts.
- Les moments forts (drops, changements de section) déclenchent explosions, effondrements et changements de caméra.
- Une **menace poursuit le joueur** (mur de son / créature) pour justifier la vitesse imposée.

## Adaptation arcade (score, fin, difficulté)
- **La difficulté dépend du héros** (ADR-036) : **Kenta** 10 vies et reprise au dernier point de reprise (ci-dessous) ; **Aiko** 3 essais, le niveau recommence au début et le meilleur essai compte ; **Ryo** une seule vie.
- **Die and retry** (2026-11-25, ADR-033, remplace la mort subite ; vies réglées par héros depuis l'ADR-036) : **10 vies** (un cœur et le nombre dans le HUD). Toucher un obstacle ou tomber coûte une vie : pose de mort ~0,7 s, puis on repart au **dernier point de reprise** (un par phrase de 8 temps quand c'est possible, choisis par le générateur), musique recalée, obstacles réactivés, pièces gardées, combo remis à zéro. À 0 vie : Game Over, puis saisie du nom si le score est dans le top 10. Niveau fini : feu d'artifice, « BRAVO ! », bonus de 500 par vie restante. Pas de décompte « 3 2 1 GO » (il tombait sur la première action).
- Morceau d'environ 1 min 30 à 2 min, découpé en sections (intro, montée, refrain, break, final), avec une densité d'actions croissante (noires → croches → syncopes).
- Fin du morceau → **tour REMIX 8-bit** : même chart, clip chiptune séparé, perturbations visuelles croissantes (brouillage, zoom, tremblements, zones sombres, image inversée). On boucle en ajoutant une perturbation à chaque tour, jusqu'à la mort.
- Durée cible d'une partie : **2 à 4 min**.
- **Bonus de difficulté** (ADR-037) : chaque point est multiplié par la difficulté du héros, ×1 Kenta, ×1,5 Aiko, ×2 Ryo ; un seul classement au score, une ligne par trigramme, avec la couleur du héros (vert, ambre, rouge).
- **Score** (implémenté dans `RunManager`, réglable dans l'inspecteur) : lum 10, ennemi/bloc cassé 50, obstacle franchi 20 (× facteur du jugement), **× multiplicateur** à paliers croissants (2026-11-24, `RunManager.MultiplierFor`) : ×2 à 4 actions de combo, ×3 à 12, ×4 à 24, ×5 à 40, ×6 à 60, ×7 à 84, ×8 à 112 (passer de ×m à ×m+1 demande m × 4 actions de plus). Le combo gagne +1 par action PARFAIT/BIEN et retombe à 0 sur un BOF ; **les pièces ne comptent pas dans le combo** (elles rapportent seulement des points). Sur le niveau 01 joué parfaitement, ×8 arrive vers les 4/5 du morceau. Bonus : section sans faute +500, niveau terminé +2000.
- **Jugement du timing** (`Core/TimingJudge`) : chaque action attendue (saut d'un trou / mur / marche, frappe) est jugée sur l'écart entre l'appui (buffer compris) et son beat dans la chart : **PARFAIT ≤ 45 ms**, **BIEN ≤ 90 ms**, **BOF** au-delà. Points de base × 1,0 / 0,6 / 0,2 × multiplicateur ; PARFAIT et BIEN font monter le combo, **BOF le casse**. Le jugement ne fait jamais mourir (invariant n°4) ; un objet n'est jugé qu'une fois. Le mot s'affiche au-dessus du héros (`UI/JudgePopup`).
- **Note de fin** (`Core/GradeCalculator`) : précision = (PARFAIT + 0,6 × BIEN + 0,2 × BOF) / actions. **S+** = tout PARFAIT et toutes les pièces ; **S** ≥ 0,95 et pièces ≥ 90 % ; **A** ≥ 0,85 ; **B** ≥ 0,70 ; **C** ≥ 0,50 ; sinon **D**. Niveau non fini : note plafonnée à C. L'autoplay joue PARFAIT (vérifie qu'un S+ est atteignable).
- État actuel de la démo : fin du morceau = fin de partie. Le tour REMIX est dans [roadmap.md](roadmap.md) (P0).
- **2 joueurs** : hot-seat en alternance (P1 puis P2 sur le même morceau, comparaison à la fin). À faire **après le MVP**.

## Écrans (flow imposé par l'enseignant)
Schéma de référence : [img/flow-ecrans.webp](img/flow-ecrans.webp), avec des exemples tirés de Metal Slug.

```
Démarrage → [1 Attract + HighScore] ⟲ (tourne en boucle)
               │ appui sur un bouton
               ▼
           [2 Explain] → [3 Config (facultatif) : héros, puis niveau] → [4 Game] → perdu ?
                                                               │ oui
                                                               ▼
                                        [5 Game Over + score joueur + liste HighScore]
                                                               │
                                                  top 10 ? ─ non ─→ retour à 1
                                                               │ oui
                                                               ▼
                                              [6 Saisie du nom / enregistrement] → retour à 1
```
> Sur le schéma, les libellés « Oui/Non » du losange « score joueur highScore ? » sont inversés. L'intention est : **si le score entre dans le top 10 → enregistrement**, sinon retour direct à l'attract.

| # | Écran | Contenu Kenta | Toolkit |
|---|---|---|---|
| 1 | **Attract + HighScore** | Cycle en trois temps : logo KENTA sur la Sylve (8 s) → **démo jouée en autoplay** (20 s, un niveau différent à chaque passage, `RunManager.BeginRun(level, demo: true)`) → classement top 10 **du niveau de cette démo** (8 s). « APPUIE SUR START » respire entre l'or et le blanc. N'importe quel bouton → écran 2. Cet appui débloque aussi l'audio du navigateur. | `HighscoreBoard` (`LevelScores`) |
| 2 | **Explain (How to play)** | Deux gestes avec les boutons dessinés comme ceux de la borne (1 SAUTE, 2 FRAPPE ; plus de glissade, ADR-030) ; le héros mime le geste mis en avant, sur le temps (2 temps par geste). « TON HÉROS FIXE LA DIFFICULTÉ ». B1/Start pour passer, sinon passage auto après ~6 s. | — |
| 3 | **Config** : héros | Kenta, Aiko ou Ryo (joystick gauche/droite), avec leur difficulté : FACILE (10 vies, reprise au point), MOYEN (3 essais, reprise au début, le meilleur compte), DUR (une seule vie) ; la règle du héros sélectionné s'affiche au-dessus des cadres. Bouton 1. Au bout de 12 s, on part avec la sélection en cours. Le 2e joueur (hot-seat) viendra plus tard. | — |
| 3 bis | **Choix du niveau** (ADR-032) | Liste façon osu! à droite (joystick haut/bas, maintenu = défilement, la liste boucle) : une carte par niveau (vignette du décor, « N NOM », morceau, étoiles). À gauche : nom, morceau, BPM et durée, étoiles, top 5 du niveau. Derrière : le décor du niveau et un extrait de son morceau (depuis `SongData.previewBeat`). Bouton 1 = jouer, bouton 2 = retour au héros ; 15 s, puis départ sur le niveau en surbrillance. Un niveau = un thème du début à la fin. Maquette : [img/selection-niveau-maquette.png](img/selection-niveau-maquette.png), capture : [img/selection-niveau-capture.png](img/selection-niveau-capture.png). | `LevelScores` |
| 4 | **Game** | HUD : score, multiplicateur, cœurs, progression du morceau, section, tour de remix. | — |
| 5 | **Game Over + score + liste** | « GAME OVER » (ou « BRAVO ! » et feu d'artifice), note S+ … D, score, pièces, jugements, puis le **classement du niveau joué** (« CLASSEMENT : … »). Chaque panneau se passe au bouton 1. | `HighscoreBoard` |
| 6 | **Enregistrement** | Seulement si le score entre dans le top 10 du niveau (`LevelScores.IsTopTen`) ou dans la liste officielle (`HighscoreManager.IsHighscore`) : « NOUVEAU RECORD ! », 3 lettres (haut/bas, bouton 1 suivante, bouton 2 retour, validation auto à 20 s), envoi par `HighscoreManager.SetHighscore` (liste officielle de la borne) et `LevelScores` (classement du niveau, Extradata de la borne), puis retour à l'écran 1 sur le classement du niveau, le nouveau score en or. | `NameEntryScreen` |

Partout : bouton blanc (1,5 s) ou 60 s sans input → retour au menu de la borne (`MenuManager`).
**Rejouer vite** : si une partie recommence moins d'une minute après la précédente, les consignes sont sautées et le héros et le niveau choisis sont repris (`GameSession.IsQuickRetry`).
Implémentation : une machine à états `GameFlow` (`Attract, Explain, Config, LevelSelect, Playing, GameOver, NameEntry`) dans une scène unique `Main`. On évite de charger des scènes en WebGL, pour garder l'audio et le `Conductor` actifs.

## Lisibilité et ressenti
- Chaque obstacle est **télégraphié une mesure avant** (couleur, forme, animation).
- Fluidité totale : le joueur est « porté » par la musique.
- Humour et énergie : les ennemis et le décor « jouent » avec le morceau.
- Satisfaction du run parfait : les sons d'action et de lums complètent la musique.

## Musique
Morceaux **originaux ou libres de droits** (noter la licence dans `game/Assets/_Project/Audio/LICENSES.md`), BPM stable entre **110 et 160**, sections bien marquées. Chaque morceau a une version normale et une version 8-bit **au même BPM et avec la même structure**.
