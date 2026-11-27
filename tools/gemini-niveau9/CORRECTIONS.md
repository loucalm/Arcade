# Niveau 9 : corrections des planches (relecture de Claude, 2026-11-27)

**Fait (Claude, 2026-11-27)** : les 5 corrections ci-dessous sont dans `gen_sheets.py` ; `python3 gen_sheets.py a1 b1 b2 b3 d1 d2` régénère ces planches, et les autres n'ont pas bougé. La souche et la bûche sont maintenant le négatif exact des sprites de la Sylve (les traits sombres d'origine deviennent les traits blancs). L'œil grand ouvert est l'œil **gauche** ; l'œil droit est le même sprite retourné (`flipX`).

Les planches de `raw/sheets/` viennent de `gen_sheets.py` (copié ici depuis le brouillon d'Antigravity). Elles sont à la bonne échelle (×4 pour les sprites, ×2 pour les décors), avec les bonnes couleurs.

**Bonnes, à garder telles quelles** : `a2_terrain`, `b4_vide_far`, `b5_vide_near`, `c_yeux`, `e_attaques`.

**À corriger** (en modifiant `gen_sheets.py`, puis `python3 gen_sheets.py`, sans toucher aux planches bonnes) :

1. `a1_obstacles`, `wall` et `block` : l'intérieur est un semis de points blancs qui se lit comme du bruit. À la place, des traits blancs **continus**. Pour la souche, 2 anneaux concentriques sur le dessus et 3 ou 4 rainures verticales d'écorce, sur la moitié droite seulement (lumière en haut à gauche). Pour la bûche, le creux noir avec un bord blanc net et les mêmes rainures. On garde la déchirure de glitch (2 ou 3 rangées décalées de 2 px). `ceil`, `post` et `platform` sont bons.
2. `b1_far` : les montagnes n'ont pas de contour. Sur le ciel noir du jeu, elles seraient invisibles. Ajouter un contour continu d'1 px gris foncé (#3A3842) sur toute la silhouette, et **retirer toutes les bandes de glitch** (pas de glitch dans les décors : le code s'en charge).
3. `b2_mid` et `b3_near` : retirer les bandes de glitch et les petits rectangles flottants (pixels perdus). Dans `b3_near`, fermer le contour blanc autour des couronnes, des troncs et des buissons.
4. `d1_sourdine_corps` : le bord gauche fait une rangée de triangles en dents de scie, on dirait du papier déchiré. Il faut une encre vivante : des **coulures arrondies** qui pendent, de longueurs variées, et des **traînées horizontales** (barres noires qui dépassent vers la gauche de 4 à 30 px, hautes de 1 à 3 px), comme un signal vidéo déchiré. Contour blanc d'1 px le long de tout le bord gauche, sans trou magenta dans la masse. Bords droit, haut et bas toujours droits ; taille finale 220×360.
5. `d2_sourdine_visage` : retirer les pixels isolés (petits points aux coins des yeux ouvert et grand ouvert, point en haut à droite de l'œil fêlé). L'œil grand ouvert ressemble à un rectangle arrondi : le faire plus rond et plus haut, avec la paupière du haut en V (colère), toujours dans la boîte de 48×28.

Ensuite : vérifier les 5 planches corrigées une à une, puis lister ce qui a changé.
