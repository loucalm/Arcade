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
- **On voit venir chaque ennemi.** Le rail garde chaque ennemi dans l'image (sous le HUD) **1,5 temps avant son
  coup** quand c'est possible, et le générateur refuse un ennemi vu moins de 400 ms avant (avant : une chauve-souris
  du refrain apparaissait 40 ms avant le coup). Corollaire de level design : pas de chauve-souris en haut d'un grand
  envol (le héros est trop bas pour la voir venir) ; on la place au sommet, ou sur la redescente.
- **Le premier plan se détache du fond.** Une brume légère éclaircit le décor ; les piliers s'assombrissent sous
  leur bord herbu. Ce qu'on peut toucher est net et contrasté, le reste recule.
- **Un objet = un sens.** Tambour = rebond (jamais d'obstacle « tambour »). Champignon = à frapper au sol.
  Champignon cuirassé (teinte métal, flèche jaune au-dessus) = à écraser en sautant dessus. Note de saut (disque
  vert) = appuyer en passant dessus. Chauve-souris = à frapper en vol. Mur = à sauter. Barre = à glisser.
- **Interface discrète.** Pas de texte au milieu de l'écran pendant l'action ; le jugement (PARFAIT…) est petit,
  au-dessus du héros, et s'efface vite.
- **Tolérance de plateformeur : survivre est facile, être précis rapporte.** Chaque action laisse **au moins
  120 ms** de marge des deux côtés pour ne pas mourir ; seul le score (PARFAIT / BIEN) récompense la précision.
  Le générateur mesure la marge de chaque action seule (`--marges DE A`) et refuse une chart trop serrée sur les
  passages déjà passés à cette règle (`WIDE_MARGIN` : tout le niveau sauf le pont 2 et le roulement 2, encore
  vérifiés à ±40 ms). Leviers : ennemis posés 0,6 beat après leur temps, zone de frappe en l'air de 3,5 u au-dessus
  et au-dessous des pieds (on frappe ce qu'on croise, même en tombant vite), tampon de saut de 140 ms, coup de pouce
  sur les marches et les souches (pieds à moins de 0,4 u du haut : on passe dessus), trous de 0,5 beat sur les
  enchaînements, notes de saut qui retiennent le héros pendant leur fenêtre d'appui.

## 5. Prototype : le refrain « dans le ciel » (beats 96-128, test à partir de 78,5)
Test dans l'éditeur : menu **Kenta > Tester le refrain (prototype)** (la partie démarre au beat 78,5, sans les écrans ;
après une mort, un appui relance au même endroit). Scène `scene_refrain_ciel` de `tools/gen-level-vertical.py` :
tambour géant sur le drop (+10 u), coup en plein vol, course sur le mur, tambour de relance, **quatre tambours en
chaîne sur le roulement de grosse caisse** (on grimpe à chaque coup), sommet, **grand plongeon en deux dévalées raides** (−26 u, le sol reste à l'image),
rebond, glissade, coups sur les derniers temps. 33 u de dénivelé (près de 4 écrans), chaque action ≥ 130 ms de
marge. Le niveau entier monte maintenant jusqu'à 54 u ; objectif 10 à 12 écrans de dénivelé en tout.

## 6. Le niveau qui change : une surprise par section
Un niveau musical reste fun parce que **chaque section change les règles du jeu** (pas seulement les obstacles).
Chacune a un menu de test dans l'éditeur (**Kenta > Tester …**) :
| Section | Surprise | Mécanique (chart) |
|---|---|---|
| Intro (0-32) | une leçon toutes les deux mesures : sauter, frapper, monter, glisser, **écraser un cuirassé** (saut dessus, appui au rebond), **traverser un gouffre de note de saut en note de saut** | `enemy` `stomp`, `orb`, `jump` |
| Couplet (32-64) | coups par deux, **deux cuirassés enchaînés** (on rebondit de l'un à l'autre), course sur le mur, glissade, **deux notes de saut** au-dessus du vide, dévalée | idem |
| Pont (64-80) | **de liane en liane** au-dessus de l'eau ; **on saute pour lâcher** chaque liane, sur un temps (sinon la main glisse), on frappe une chauve-souris à chaque balancier | `vine` (lâcher au saut), `gap` long |
| Refrain (96-128) | **changement de monde** : on monte dans l'Orage ; tambours en chaîne, grand plongeon | section `"biome"`, `bounce`, `ramp` |
| Final (128-152) | **le monde se retourne** dans les Échos : portail, course au plafond la tête en bas (trous à sauter, ennemis pendus) ; puis un tambour envoie **dans l'arrière-plan** : on court derrière les piliers du premier plan, un autre tambour ramène devant | `gravity`, `ceiling`, `enemy` lane 2, `jump` ; `bounce` `to_back` / `to_front`, `back` |
| Refrain 2 (216-256) | **l'écran se détraque** : 2 écrans au drop, 5 écrans (en petit) sur le roulement, puis grésillement ; sur des gestes déjà appris au refrain 1 | `screen_split`, `glitch` |
Règles : la surprise s'annonce (portail visible une mesure avant, liane qui pend vers le point de prise), le premier
geste après elle est simple, et la règle des 120 ms vaut aussi pour elle (le simulateur gère gravité, plans et
lianes). Les effets d'écran gênent la lecture mais pas le rythme : ils tombent sur des passages déjà joués.

## 7. Ce qui reste à faire pour aller plus loin
Voir [roadmap.md](roadmap.md) : décor qui joue avec la musique (éléments qui
réagissent au temps), scènes de drop (décor qui s'effondre), son du tambour, et une **musique libre de droits
écrite pour le niveau** (l'idéal : les sons des actions *manquent* dans la piste, et c'est le joueur qui les joue).
