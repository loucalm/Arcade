using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.Level
{
    /// <summary>
    /// Un niveau = un thème (biome) + un morceau + une chart, choisi à l'écran de sélection (ADR-032). Chaque niveau a
    /// son SongData ; provisoirement, tous pointent vers le même clip et la même chart. Donner à un niveau sa musique et
    /// sa chart = changer les références de son SongData, sans code (la reconstruction ne les écrase pas).
    /// </summary>
    [CreateAssetMenu(fileName = "Level_", menuName = "Kenta/Level")]
    public class LevelDef : ScriptableObject
    {
        public string id = "level";
        [Min(1)] public int number = 1;
        public BiomeTheme biome;
        public SongData song;
        [Tooltip("Chart du niveau ; vide = celle du SongData.")]
        public TextAsset chart;
        [Range(1, 5)] public int stars = 1;
        [Tooltip("Vignette 40×24 de la liste des niveaux (Gen/ui/thumb_<biome>.png).")]
        public Sprite thumbnail;

        public TextAsset Chart => chart != null ? chart : (song != null ? song.chart : null);
        public string Label => biome != null ? biome.label : id;
    }
}
