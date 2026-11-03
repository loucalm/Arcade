# Kenta

**Kenta** : runner/plateformeur rythmique en pixel art pour la borne d'arcade MMI (**Anatidae**), inspiré des niveaux musicaux de Rayman Legends et de Geometry Dash. Huit mondes, un morceau, une Sourdine qui avale le son. Projet BUT MMI 3, S5 : Lou Calmes, Lucas Corrieras, Jeremy Hordé.

- `game/` : jeu Unity **6000.3.23f1** (base [anatidae-toolkit](https://github.com/XariusExcl/anatidae-toolkit)). Ouvrir ce dossier dans Unity Hub.
- `web/` : site one-page + serveur (Node, Express, ws) déployé sur le VPS.
- `docs/` : référence borne, DA Kenta (PDF + mise en œuvre), game design, format des charts, contrat d'API, décisions, roadmap.

## Démarrage
1. Installer Unity **6000.3.23f1** (exactement cette version) avec le module **Web Build Support**.
2. Ouvrir `game/`, puis la scène `Assets/_Project/Scenes/Main.unity`. Le package MCP Unity (CoplayDev, v10.2.0) est déjà dans le manifest : pour s'en servir avec Claude Code, lancer le bridge depuis Window > MCP for Unity.
3. Borne locale pour les tests : voir [docs/anatidae.md](docs/anatidae.md#tester-en-local).

Conventions d'équipe et règles pour Claude Code : [CLAUDE.md](CLAUDE.md).
