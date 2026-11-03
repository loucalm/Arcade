using Kenta.Player;
using UnityEngine;

namespace Kenta.Level
{
    /// <summary>
    /// Éléments communs à tous les biomes (pièces, totem, pixel blanc) et liste des 8 biomes et des héros.
    /// Direction artistique : docs/da-kenta.md. Les sprites sont générés ou extraits (voir docs/art-direction.md).
    /// </summary>
    [CreateAssetMenu(fileName = "Theme_", menuName = "Kenta/Level Theme")]
    public class LevelTheme : ScriptableObject
    {
        [Tooltip("Ordre du voyage : un biome par section de la chart.")]
        public BiomeTheme[] biomes;
        public HeroProfile[] heroes;

        [Header("Communs")]
        [Tooltip("4 images de la pièce qui tourne.")]
        public Sprite[] coinFrames;
        [Tooltip("Gardien miniature posé au début de chaque biome (checkpoint).")]
        public Sprite totem;
        [Tooltip("Carré blanc de 1 px (40 px/unité) : traînée du bandeau, particules.")]
        public Sprite pixel;
        public Sprite sourdine;
        public Sprite sourdineVeil;
    }
}
