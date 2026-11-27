using UnityEngine;

namespace Kenta.Level
{
    /// <summary>
    /// Un des 8 mondes de Kenta (docs/da-kenta.md § 3) : décor en 4 calques, sol, trou, obstacles et ennemi local.
    /// Un biome = une section de la chart, chargé au checkpoint. Assets créés par le menu « Kenta > Reconstruire ».
    /// </summary>
    [CreateAssetMenu(fileName = "Biome_", menuName = "Kenta/Biome")]
    public class BiomeTheme : ScriptableObject
    {
        [Header("Noms affichés")]
        public string label = "LA SYLVE";
        [Tooltip("Nom de la section musicale (INTRO, COUPLET A…).")]
        public string sectionName = "INTRO";
        public string guardianName = "CERF DE PIERRE";

        [Header("Décor (calques en parallaxe)")]
        [Tooltip("640×360, fixe à l'écran : ciel, astre.")]
        public Sprite sky;
        [Tooltip("1280×360, boucle sans couture.")]
        public Sprite far, mid, near;

        [Header("Gardien (statue immobile, placée comme le ciel, derrière les montagnes)")]
        public Sprite guardian;
        [Tooltip("Centre du gardien par rapport au centre de l'écran 640×360, en px (y vers le haut). Écrit par tools/gemini-gardiens/clean.py (guardians.json).")]
        public Vector2 guardianOffset;

        [Header("Niveau")]
        [Tooltip("Tuile 32×20 : herbe, terre, reflet de l'eau.")]
        public Sprite ground;
        [Tooltip("Tuile 32×20 posée dans les trous.")]
        public Sprite hazard;
        [Tooltip("36×32, pivot aux pieds.")]
        public Sprite wall;
        [Tooltip("38×38, pivot aux pieds.")]
        public Sprite block;
        [Tooltip("Tuile 16×16 de la poutre du plafond.")]
        public Sprite ceiling;
        [Tooltip("Tuile 8×16 des poteaux du plafond.")]
        public Sprite post;
        [Tooltip("Flanc d'un sol surélevé (marche), 32×32, répété.")]
        public Sprite fill;
        [Tooltip("Flanc en profondeur, plus sombre (32×32) : les piliers fonçent vers le bas.")]
        public Sprite fillDeep;
        [Tooltip("Détails semés sur le flanc des piliers : pierre, racine, fossile, cailloux.")]
        public Sprite[] pillarDecor;
        [Tooltip("Dessus d'un sol surélevé, 32×8.")]
        public Sprite cliff;
        [Tooltip("Plateforme suspendue (traversable par en dessous), 32×10.")]
        public Sprite platform;

        [Header("Ennemi local (instrument détraqué)")]
        [Tooltip("Une image = il sautille d'un pixel sur le temps ; deux images = elles alternent sur le temps (djembé).")]
        public Sprite[] enemyFrames;
        [Tooltip("Image « cassé » : montrée une fraction de seconde quand on le frappe.")]
        public Sprite enemyBroken;
        [Tooltip("Touché : l'image du coup (tools/gemini-ennemis/clean.py).")]
        public Sprite enemyHurt;
        [Tooltip("Cassé, en quelques images : la fêlure, puis les morceaux. Jouées là où l'ennemi a été frappé.")]
        public Sprite[] enemyBrokenFrames;

        [Header("Couleurs")]
        [Tooltip("Étincelles et particules du biome.")]
        public Color spark = Color.white;
        [Tooltip("Couleur du flash du drop vers ce biome.")]
        public Color flash = Color.white;
        [Tooltip("Monde en noir et blanc (le monde sourd du niveau 9) : pas de teinte de section ni de jour qui baisse.")]
        public bool monochrome;
    }
}
