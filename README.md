# Rythme Runner

Runner/plateformeur rythmique pour la borne d'arcade MMI (**Anatidae**), inspiré des niveaux musicaux de Rayman Legends. Projet BUT MMI 3, S5.

- `game/` : jeu Unity **6000.3.23f1** (base [anatidae-toolkit](https://github.com/XariusExcl/anatidae-toolkit)). Ouvrir ce dossier dans Unity Hub.
- `web/` : site one-page + serveur (Node, Express, ws) déployé sur le VPS.
- `docs/` : référence borne, game design, format des charts, contrat d'API, décisions.

## Démarrage
1. Installer Unity **6000.3.23f1** (exactement cette version) avec le module **Web Build Support**.
2. Ouvrir `game/`, puis la scène `Assets/_Project/Scenes/Main.unity`. Le package MCP Unity (CoplayDev, v10.2.0) est déjà dans le manifest : pour s'en servir avec Claude Code, lancer le bridge depuis Window > MCP for Unity.
3. Borne locale pour les tests : voir [docs/anatidae.md](docs/anatidae.md#tester-en-local).

Conventions d'équipe et règles pour Claude Code : [CLAUDE.md](CLAUDE.md).
