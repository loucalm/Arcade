# Level design « niveau musical » — principes de Kenta

Ce document dit **ce qui fait un niveau musical réussi** (le genre des niveaux musicaux des plateformeurs modernes)
et **comment Kenta l'applique**. On reprend les mécaniques et les principes du genre, jamais les niveaux d'un autre
jeu. À relire avant d'écrire ou de modifier un niveau (`tools/gen-level-vertical.py`). Format : [chart-format.md](chart-format.md).

## 1. Le principe : le joueur joue la musique
Un niveau musical n'est pas un niveau « avec de la musique » : c'est **une partition qu'on joue en courant**.
- **Chaque appui tombe sur un son du morceau.** Grosse caisse → sauts (trous, marches, tambours). Caisse claire →
  coups de poing (ennemis). Notes tenues et silences → glissades, envolées, courses sur le mur.
- **Les pièces sont la mélodie.** Chaque pièce ramassée joue une note (`lum_at` + `note`) prise dans le contour
  mélodique du morceau, ramenée dans la gamme pentatonique de la tonique (la mineur pour le niveau 01) : même
  approximative, elle sonne juste. Une ligne de pièces = une phrase jouée.
- **Les pièces dessinent le chemin.** Au-dessus de chaque saut, un arc de pièces ; sur chaque envolée, l'arc complet ;
  entre deux actions, une ligne. Le joueur n'a jamais à deviner où aller : il suit les pièces, et s'il les prend
  toutes, il est dans le rythme.
- Source : `tools/analyse-song.py` écrit la carte du morceau (`tools/data/level01_analysis.json` : attaques
  grave / médium / aigu et note dominante à chaque double-croche). Le générateur cale les actions sur ces attaques.

## 2. Le niveau suit la forme du morceau
- **Phrases de 4 mesures (16 beats).** Une phrase = une idée (un motif). On l'énonce, on la répète, on la varie, on
  la résout : A A A′ B. Le joueur reconnaît le motif dès la 2ᵉ fois : c'est ce qui rend le niveau lisible.
- **Une idée nouvelle à la fois.** L'intro présente chaque geste seul, avec de l'air autour (sauter, frapper,
  glisser, monter une marche, rebondir). Les combinaisons viennent ensuite.
- **La hauteur suit l'énergie.** Intro au sol ; couplet : on monte ; pont : on flotte ; roulement : la tension
  monte (escalier sur chaque coup de grosse caisse) ; refrain : on est dans le ciel (tambours en chaîne, ennemis en
  vol) ; retombée : on redescend et on souffle ; final : la tour, puis le grand plongeon.
- **Chaque section a son moment signature** (une seule chose dont on se souvient). Le passage 2 du morceau rejoue
  les mêmes idées, **variées** (ennemis en vol à la place d'ennemis au sol, plus de hauteur) : on reconnaît, mais
  c'est plus dur.

## 3. La verticalité a un sens
| Geste | Quand dans la musique | Mécanique |
|---|---|---|
| Monter | une montée, une tension | marches sautées sur la grosse caisse ; rampe raide = course sur le mur sur une note tenue |
| Plonger / dévaler | le relâchement, le premier temps d'un refrain | rampe descendante (fluide) ; chute franche seulement si la caméra a anticipé |
| S'envoler | le gros accent, le début d'un refrain | tambour (`bounce`) : 2 à 3 beats en l'air, arc de pièces, ennemis en vol à frapper |
| Flotter | les passages légers | plateformes au-dessus du vide, à la hauteur des notes de la mélodie |
On ne change pas de hauteur « pour décorer » : chaque changement de hauteur répond à la musique.

## 4. Lisibilité : rien de brouillon
- **Une seule caméra, large, qui ne coupe pas.** Vue de 640 px (×3) : on voit **une mesure à l'avance** devant le
  héros (placé au quart gauche). Pas de changements de zoom en cours de niveau (ils lisaient comme des sauts
  d'image). La caméra suit un **rail précalculé** (trajectoire idéale du héros, lissée dans les deux sens) : elle
  monte *avant* une envolée et descend *avant* un plongeon, sans à-coups. Si le joueur s'écarte du rail, elle
  le garde quand même dans l'image.
- **Le premier plan se détache du fond.** Une brume légère éclaircit le décor ; les piliers s'assombrissent sous
  leur bord herbu. Ce qu'on peut toucher est net et contrasté, le reste recule.
- **Un objet = un sens.** Tambour = rebond (jamais d'obstacle « tambour »). Champignon = à frapper au sol.
  Chauve-souris = à frapper en vol. Mur = à sauter. Barre = à glisser.
- **Interface discrète.** Pas de texte au milieu de l'écran pendant l'action ; le jugement (PARFAIT…) est petit,
  au-dessus du héros, et s'efface vite.
- **Tolérance de plateformeur.** Tout passe avec un appui 40 ms en avance ou en retard (vérifié par le simulateur).

## 5. Ce qui reste à faire pour aller plus loin
Voir [roadmap.md](roadmap.md) : lianes (balancier automatique), décor qui joue avec la musique (éléments qui
réagissent au temps), scènes de drop (décor qui s'effondre), son du tambour, et une **musique libre de droits
écrite pour le niveau** (l'idéal : les sons des actions *manquent* dans la piste, et c'est le joueur qui les joue).
