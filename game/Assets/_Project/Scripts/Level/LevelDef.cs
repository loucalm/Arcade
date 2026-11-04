using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.Level
{
    /// <summary>
    /// Un niveau = un monde (biome) + un morceau + une chart. Pour l'instant les 8 niveaux partagent le même morceau et
    /// la même chart ; plus tard chacun aura les siens (sans code : changer les références ici).
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

        public TextAsset Chart => chart != null ? chart : (song != null ? song.chart : null);
        public string Label => biome != null ? biome.label : id;
    }
}
