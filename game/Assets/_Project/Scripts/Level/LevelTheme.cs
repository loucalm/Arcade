using UnityEngine;

namespace RythmeRunner.Level
{
    /// <summary>
    /// Direction artistique provisoire : formes de base + palette. Les artistes remplacent les sprites
    /// ici sans toucher au code. Un asset par thème (ex. un thème « remix 8-bit » plus tard).
    /// </summary>
    [CreateAssetMenu(fileName = "Theme_", menuName = "Rythme Runner/Level Theme")]
    public class LevelTheme : ScriptableObject
    {
        [Header("Formes")]
        public Sprite square;
        public Sprite circle;
        public Sprite glow;
        [Tooltip("Matériau additif pour halos et particules.")]
        public Material additive;

        [Header("Palette")]
        public Color background = new Color(0.05f, 0.03f, 0.1f);
        public Color backgroundFlash = new Color(0.16f, 0.06f, 0.24f);
        public Color groundBody = new Color(0.12f, 0.07f, 0.22f);
        public Color groundTop = new Color(0.3f, 0.95f, 1f);
        public Color beatTick = new Color(0.3f, 0.95f, 1f, 0.35f);
        public Color player = new Color(0.3f, 0.95f, 1f);
        public Color enemy = new Color(1f, 0.3f, 0.35f);
        public Color block = new Color(1f, 0.6f, 0.2f);
        public Color wall = new Color(1f, 0.25f, 0.75f);
        public Color slideBar = new Color(1f, 0.85f, 0.2f);
        public Color lum = new Color(1f, 0.95f, 0.4f);
        public Color checkpoint = new Color(0.4f, 1f, 0.6f);
        public Color backdropNear = new Color(0.14f, 0.07f, 0.26f);
        public Color backdropFar = new Color(0.09f, 0.05f, 0.18f);
    }
}
