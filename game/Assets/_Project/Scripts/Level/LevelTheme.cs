using UnityEngine;

namespace RythmeRunner.Level
{
    /// <summary>
    /// Direction artistique : sprites néon (générés par tools/gen-neon-sprites.py, voir docs/art-direction.md)
    /// et palette façon Rider. Les artistes peuvent remplacer n'importe quel sprite ici sans toucher au code.
    /// </summary>
    [CreateAssetMenu(fileName = "Theme_", menuName = "Rythme Runner/Level Theme")]
    public class LevelTheme : ScriptableObject
    {
        [Header("Formes de base")]
        public Sprite square;
        public Sprite circle;
        public Sprite glow;
        [Tooltip("Matériau additif : traînée, particules, coup de poing. Les sprites néon (alpha = intensité) s'y additionnent comme de la lumière.")]
        public Material additive;

        [Header("Sprites néon : niveau")]
        [Tooltip("2 images : elles alternent sur le beat (l'ennemi « danse »).")]
        public Sprite[] enemyFrames;
        public Sprite blockSprite;
        [Tooltip("9-slice, bordure 44 px.")]
        public Sprite wallSprite;
        [Tooltip("9-slice horizontal, bordures 24 px.")]
        public Sprite laserBeamSprite;
        public Sprite laserNodeSprite;
        [Tooltip("9-slice vertical, bordures 32 px.")]
        public Sprite laserPostSprite;
        public Sprite lumSprite;
        public Sprite checkpointSprite;
        [Tooltip("9-slice horizontal : trait de sol, dégradé et filets de fuite.")]
        public Sprite groundTopSprite;

        [Header("Sprites néon : décor, effets, UI")]
        public Sprite[] rocks;
        [Tooltip("Voile : assombrit le haut de l'écran, lueur turquoise vers l'horizon.")]
        public Sprite backgroundVeil;
        public Sprite slashSprite;
        public Sprite sparkSprite;
        public Sprite heartOn;
        public Sprite heartOff;
        public Sprite logoSprite;

        [Header("Palette (Rider : fond bleu-vert sombre, turquoise + magenta)")]
        public Color background = new Color(0.047f, 0.145f, 0.19f);
        public Color backgroundFlash = new Color(0.10f, 0.29f, 0.37f);
        public Color groundBody = new Color(0.03f, 0.10f, 0.14f);
        public Color groundTop = new Color(0.25f, 0.94f, 1f);
        public Color beatTick = new Color(0.25f, 0.94f, 1f, 0.28f);
        public Color player = new Color(0.25f, 0.94f, 1f);
        public Color enemy = new Color(1f, 0.17f, 0.71f);
        public Color block = new Color(1f, 0.17f, 0.71f);
        public Color wall = new Color(0.25f, 0.94f, 1f);
        public Color slideBar = new Color(1f, 0.69f, 0.18f);
        public Color lum = new Color(1f, 0.94f, 0.63f);
        public Color checkpoint = new Color(0.25f, 0.94f, 1f);
        public Color backdropNear = new Color(0.10f, 0.28f, 0.36f);
        public Color backdropFar = new Color(0.07f, 0.20f, 0.27f);
    }
}
