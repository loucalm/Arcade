"""Fiche d'un niveau : son morceau, son tempo, ses sections et ce qu'il a le droit de demander au joueur.

La fiche est la règle de difficulté du niveau (ADR-034) : le moteur refuse une chart qui utilise une mécanique
absente de `mechanics`, ou dont une action laisse moins de `margin_ms` de marge pour ne pas mourir.
"""
from dataclasses import dataclass, field
from typing import Callable, Optional

# Les mécaniques, dans l'ordre où le joueur les découvre (plan des 6 niveaux, ADR-034).
MECHANICS = {
    "gap": "trou à sauter",
    "step": "marche à monter (ou descente)",
    "wall": "souche ou mur à sauter",
    "enemy": "ennemi au sol à frapper",
    "platform": "plateforme au-dessus du vide",
    "vine": "liane (lâcher au saut)",
    "stomp": "cuirassé (sauter dessus, appui au rebond)",
    "flyer": "chauve-souris à frapper en vol",
    "orb": "note de saut (appui en passant)",
    "gravity": "gravité inversée (portail, plafond, ennemis pendus)",
    "bounce": "tambour-tremplin",
    "ramp": "rampe, course sur le mur, dévalée",
    "back": "passage dans l'arrière-plan",
    "screen_split": "écran dédoublé",
    "screen_flip": "écran en miroir ou tête en bas",
    "glitch": "grésillement de l'image",
}
ALL = frozenset(MECHANICS)

# Distance parcourue en un temps (8 u/s à 150 BPM, le niveau 01). Toutes les tolérances du jeu ont été réglées et
# vérifiées pour cette distance : la portée de la frappe (1,9 u), la place d'un ennemi (0,6 temps après son coup),
# les murs et les marches. Garder 3,2 u par temps garde ces tolérances en temps, donc plus larges en ms à un tempo
# lent. À 117 BPM et 8 u/s, une frappe 150 ms en avance rate l'ennemi (test du 2026-11-26).
UNITS_PER_BEAT = 3.2


def mechanics_of(events):
    """Mécaniques demandées par une liste d'événements (avant ou après `engine.resolve`). Les pièces, les sections, la
    caméra, les sauts sans obstacle et les falaises atteintes en vol (conséquence d'une autre mécanique) n'en sont pas."""
    used = set()
    for e in events:
        t, lane, v = e["type"], e.get("lane", 0), e.get("params", {}).get("variant", "")
        if t in ("gap", "wall", "platform", "vine", "orb", "ramp", "screen_split", "screen_flip", "glitch"):
            used.add(t)
        elif t == "step" and lane == 0:
            used.add("step")
        elif t in ("gravity", "ceiling"):
            used.add("gravity")
        elif t == "back":
            used.add("back")
        elif t == "bounce":
            used.add("back" if v in ("to_back", "to_front") else "bounce")
        elif t == "jump" and v == "back":
            used.add("back")
        elif t == "enemy_air":
            used.add("flyer")
        elif t in ("enemy", "block"):
            if v == "stomp":
                used.add("stomp")
            elif v == "back":
                used.add("back")
            elif lane == 1:
                used.add("flyer")
            elif lane == 2:
                used.add("gravity")
            else:
                used.add("enemy")
    return used


@dataclass
class Fiche:
    id: str                                  # nom de la chart : game/Assets/_Project/Charts/<id>.json
    title: str                               # titre du morceau (en-tête de la chart)
    bpm: float                               # tempo constant
    offset: float                            # secondes entre le début du fichier audio et le beat 0
    beats: int                               # longueur du niveau (beats)
    sections: list                           # [(nom, beat de début)] ; « refrain… » = plan large
    analysis: str                            # carte du morceau (tools/data/…, écrite par tools/analyse-song.py)
    build: Callable[[], list]                # écrit les scènes : renvoie la liste des événements
    run_speed: Optional[float] = None        # u/s ; None = UNITS_PER_BEAT par temps (la géométrie en temps reste celle
                                             # réglée sur le niveau 01 : 6,24 u/s à 117 BPM, 9,28 u/s à 174 BPM)
    zoom: int = 480                          # vue par défaut (px de jeu de large)
    zoom_wide: int = 640                     # plan large ; = zoom pour une vue fixe (aucun changement de plan)
    wide_on_refrains: bool = True            # plan large sur chaque section « refrain… » ; sinon, seulement là où un
                                             # ennemi se verrait trop tard en vue serrée
    scale: tuple = (9, 0, 2, 4, 7)           # gamme des notes des pièces (classes de hauteur ; défaut : la mineur pentatonique)
    mechanics: frozenset = ALL               # mécaniques autorisées
    margin_ms: float = 120.0                 # marge de survie minimale de chaque action, des deux côtés (ADR-024)
    margin_ranges: Optional[list] = None     # passages vérifiés à cette marge [(de, à)] ; None = tout le niveau
    checkpoint_every: int = 8                # un point de reprise par phrase de N temps (ADR-033)
    biomes: dict = field(default_factory=dict)   # section → asset de biome (changement de monde ; vide = un thème)
