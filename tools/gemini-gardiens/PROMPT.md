# Kenta : gardiens (statues géantes du fond), dossier pour Gemini

Les gardiens sont **immobiles** et placés **derrière les montagnes lointaines** (ADR-028). Il n'y a qu'un état par statue, endormie.

## Mode d'emploi (pour Lou)

- **Gemini web** : un message par biome. Joindre :
  - `refs/gardiens/gardien_<n>_<biome>.png` (le gardien dans sa maquette, ×4) ;
  - `refs/maquettes/maquette_<n>_<biome>.png` (la maquette entière) ;
  - `refs/gardiens_dans_maquettes.png` (les 8 gardiens, pour garder une famille cohérente).

  Coller la partie « Prompt » ci-dessous, puis ajouter : « Do only <biome> now ». Enregistrer l'image dans `raw/sheets/<n>_<biome>.jpg` (ou `.png`), en remplaçant l'ancienne.
- **Gemini agent** (Antigravity, CLI) : ouvrir ce dossier et lui donner ce fichier (« Fais ce que dit PROMPT.md »).
- **Ensuite** (Lou ou Claude) : lancer `python3 tools/gemini-gardiens/clean.py` (dépendances : `pillow`, `numpy`, `scipy`). Il écrit dans `out/` les PNG, `guardians.json` et l'aperçu `preview.png` (chaque gardien sur le ciel de son biome, à côté de celui de la maquette). Si l'aperçu est bon, lancer `clean.py --install` : il copie dans `game/Assets/_Project/Art/Kenta/Gen/biomes/` et met à jour `guardians.json`. Puis, dans Unity, lancer le menu **Kenta > Reconstruire thèmes, prefabs et scène** (la position vient de `guardians.json`).

Contenu : `refs/` (références), `palettes.json` (couleurs, taille et position de chaque gardien), `raw/sheets/` (images Gemini, versionnées), `out/` (sorties), `clean.py`.
`refs/actuels/` et `refs/gardiens_actuels.png` montrent les anciens gardiens dessinés par code (aplats provisoires, avec un état éveillé qui n'existe plus). Ils ne servent que pour la taille.

---

## Prompt

You are drawing the **guardians** of **Kenta**, a 2D side-scrolling rhythm runner in pixel art for an arcade cabinet (1920×1080). Each of the 8 biomes has one guardian: a **giant statue sleeping in the far background, behind the distant mountains**. It never moves. Work only inside this folder. Do not modify any other file.

### References (in `refs/`)
- `gardiens/gardien_<n>_<biome>.png` and `gardiens_dans_maquettes.png`: the guardians in the official mockups of the art-direction PDF. **This is the design and style to match.**
- `maquettes/maquette_<n>_<biome>.png`: the full mockup of each biome (240×135 pixel art, shown ×4).
- `palettes.json`: colours of each guardian (`body`, `line`, `accent`) and of the sky behind it (`sky`).

### Style
- Pixel art like the mockups: clean square pixels all of the same size. No anti-aliasing, no gradients, no blur, no dithering noise, no text.
- Cute, round, soft statue shapes, as in the mockups. A big round head (about 60 % of the statue width, in the upper two thirds) sits on a rounded body that rests on the ground. Front view, symmetric, sitting still.
- Sleeping face as in the mockups: heavy half-closed eyelids (a dark lid line over a slightly lighter eye band), a tiny "o" mouth, a calm, sleepy expression. The eyes stay closed.
- A few big details only: moss patches on the head, cracks, glassy highlight streaks, horns, ears, antlers or a crest. No detail smaller than about 1/60 of the statue height: in the game the statue is about 150–200 px wide and 190–270 px high on a 640×360 view.
- 3–4 flat tones per material (shadow, base, light), light from the top-left.
- **No black outline**: the silhouette edge is a slightly darker tone of the body (the `line` colour). It is a far background element, behind mountains: colours are muted and close to the sky, but the statue must still be clearly readable.
- No background, no ground, no trees, no mountains, no fog, no shadow on the ground. The bottom of the body is flat; it will be hidden behind mountains and forests.

### Guardians (from the art-direction PDF)
| Biome (`id`, n) | Guardian | Look (see its mockup) | body / line / accent | sky behind |
|---|---|---|---|---|
| La Sylve (`sylve`, 1) | Cerf de pierre (stone deer) | grey stone deer; tall branching antlers like dead branches; spiky cheek tufts; moss on the head | #AEB6BC / #8C949C / #6CC24A | #BCDFD3 |
| Le Bayou (`bayou`, 2) | Grenouille-lune (moon frog) | mossy green frog; two eye bumps on top covered in moss; wide face | #8E9A8C / #6E7A6A / #7A9A5A | #8F6A78 |
| Les Échos (`echos`, 3) | Chauve-souris de cristal (crystal bat) | crystal bat; big pointed ears; wings folded around the body; glassy diagonal highlight streaks; two small fangs | #3E4488 / #2A2F66 / #4FC8C0 | #262A5A |
| La Forge (`forge`, 4) | Taureau-enclume (anvil bull) | dark basalt bull; curved horns; a glowing lava crack across the head; flat anvil-like snout | #4A2A24 / #2D0C08 / #FF7A1A | #4B190B |
| L'Échine (`echine`, 5) | Chat-montagne (mountain cat) | grey mountain cat; pointed ears; moss patches on the head and shoulders | #9AA0B2 / #7C8296 / #79BC7A | #68ADED |
| Le Givre (`givre`, 6) | Renard de glace (ice fox) | pale ice fox; tall pointed ears; glassy highlights; bushy tail curled at its side | #9CBEE6 / #7C9ECC / #FFFFFF | #5388C1 |
| La Cité Sourde (`cite`, 7) | Lapin de grès (sandstone rabbit) | sandstone rabbit; long upright ears with a little snow on the tips; a crack across the face | #D2A07A / #B8845A / #E8C8B0 | #82A6C7 |
| Le Grand Orage (`orage`, 8) | Oiseau-tonnerre (thunderbird) | storm-blue bird; crest of spikes; small triangular beak; a lightning bolt near the crest | #6A78C0 / #4A5598 / #E0B040 | #2E3570 |

### Output
One image per guardian: `raw/sheets/<n>_<biome>.png`, the statue alone, centred on a flat #FF00FF magenta background, nothing touching the edges, no text. Any resolution is fine if the pixel grid stays clean and square.

Then run `python3 clean.py` (needs `pillow`, `numpy` and `scipy`). It scales the statue to the in-game size, maps colours to the palette and writes `out/preview.png`. Do **not** run `clean.py --install`.

### Checklist (on `out/preview.png`)
- It reads instantly as the guardian of the table, in the style of its mockup, and the 8 look like one family.
- Eyes closed, round soft shapes, big details, no black outline, no noise or stray pixels.
- Readable on its sky colour, without stealing the show from the level in front.

When done, list the files of `out/` and report which guardians you are not happy with.
