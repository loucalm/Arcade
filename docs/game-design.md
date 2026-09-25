# Game design — Rythme Runner

Inspiration : les « music levels » de Rayman Legends. Un plateformeur 2D en scrolling horizontal où **toute la traversée est synchronisée sur la musique**. Bien joué, chaque saut, coup, glissade ou collecte tombe sur un temps fort : le joueur a l'impression de **jouer la musique en courant**.

## Pitch arcade (lisible en 10 s)
« Cours, saute et frappe en rythme. Ne te fais pas toucher. Ramasse les lums pour faire monter le score. »

## Commandes (3 boutons max)
| Action | Entrée | Son |
|---|---|---|
| Sauter (hauteur fixe, le saut dure **1 beat**, avec une vrille) | **B1** | accent/percussion quantifié |
| Frapper (casse blocs et ennemis) | **B2** | caisse claire/impact |
| Glisser (sous les obstacles) ; en l'air = chute rapide | **joystick bas** | swoosh |
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
- **3 cœurs.** Être touché fait perdre un cœur et respawn au **checkpoint de section** (audio resynchronisé). À 0 cœur, c'est **game over**.
- Morceau d'environ 1 min 30 à 2 min, découpé en sections (intro, montée, refrain, break, final), avec une densité d'actions croissante (noires → croches → syncopes).
- Fin du morceau → **tour REMIX 8-bit** : même chart, clip chiptune séparé, perturbations visuelles croissantes (brouillage, zoom, tremblements, zones sombres, image inversée). On boucle en ajoutant une perturbation à chaque tour, jusqu'à la mort.
- Durée cible d'une partie : **2 à 4 min**.
- **Score** (implémenté dans `RunManager`, réglable dans l'inspecteur) : lum 10, ennemi/bloc cassé 50, obstacle franchi 20, **× multiplicateur** = 1 + combo/8 (max ×8). Le combo gagne +1 par action réussie et retombe à 0 quand on est touché. Bonus : section sans chute +500, niveau terminé +1000 par cœur restant. Au respawn, le score et les lums reviennent à leur valeur du checkpoint (pas de farm en mourant).
- **Médailles** (ratio de lums) : OR ≥ 90 %, ARGENT ≥ 60 %, sinon BRONZE si le niveau est terminé ; en cas d'échec, BRONZE à partir de 30 %.
- État actuel de la démo : fin du morceau = fin de partie. Le tour REMIX est dans [roadmap.md](roadmap.md) (P0).
- **2 joueurs** : hot-seat en alternance (P1 puis P2 sur le même morceau, comparaison à la fin). À faire **après le MVP**.

## Écrans (flow imposé par l'enseignant)
Schéma de référence : [img/flow-ecrans.webp](img/flow-ecrans.webp), avec des exemples tirés de Metal Slug.

```
Démarrage → [1 Attract + HighScore] ⟲ (tourne en boucle)
               │ appui sur un bouton
               ▼
           [2 Explain] → [3 Config (facultatif)] → [4 Game] → perdu ?
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

| # | Écran | Contenu Rythme Runner | Toolkit |
|---|---|---|---|
| 1 | **Attract + HighScore** | Alternance toutes les ~8 s : démo en **autoplay** du niveau (musique basse, implémentée : `RunManager.BeginRun(song, demo: true)`) ↔ classement top 10. Logo + « APPUIE SUR UN BOUTON » qui pulse sur le beat. N'importe quel bouton → écran 2. Cet appui débloque aussi l'audio du navigateur. | `HighscoreUI` |
| 2 | **Explain (How to play)** | Une planche animée : joystick bas = glisser, B1 = sauter, B2 = frapper, « suis la musique ». B1/Start pour passer, sinon passage auto après ~6 s. | — |
| 3 | **Config** *(facultatif)* | 1 joueur / 2 joueurs (hot-seat), plus tard le choix du morceau. Si on ne le fait pas, on passe directement en 1P. | — |
| 4 | **Game** | HUD : score, multiplicateur, cœurs, progression du morceau, section, tour de remix. | — |
| 5 | **Game Over + score + liste** | Animation « GAME OVER » sur le beat, puis score final, lums, médaille, tours de remix et classement actuel avec la place du joueur mise en évidence. | `HighscoreUI` |
| 6 | **Enregistrement** | Seulement si `HighscoreManager.IsHighscore(score)` : saisie du nom sur 3 caractères, puis retour à l'écran 1. | `HighscoreNameInput` |

Partout : bouton blanc (1,5 s) ou 60 s sans input → retour au menu de la borne (`MenuManager`).
Implémentation : une machine à états `GameFlow` (`Attract, Explain, Config, Playing, GameOver, NameEntry`) dans une scène unique `Main`. On évite de charger des scènes en WebGL, pour garder l'audio et le `Conductor` actifs.

## Lisibilité et ressenti
- Chaque obstacle est **télégraphié une mesure avant** (couleur, forme, animation).
- Fluidité totale : le joueur est « porté » par la musique.
- Humour et énergie : les ennemis et le décor « jouent » avec le morceau.
- Satisfaction du run parfait : les sons d'action et de lums complètent la musique.

## Musique
Morceaux **originaux ou libres de droits** (noter la licence dans `game/Assets/_Project/Audio/LICENSES.md`), BPM stable entre **110 et 160**, sections bien marquées. Chaque morceau a une version normale et une version 8-bit **au même BPM et avec la même structure**.
