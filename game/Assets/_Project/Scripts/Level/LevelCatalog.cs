using UnityEngine;

namespace Kenta.Level
{
    /// <summary>Liste ordonnée des niveaux proposés à l'écran de sélection (et, plus tard, au mode marathon).</summary>
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Kenta/Level Catalog")]
    public class LevelCatalog : ScriptableObject
    {
        public LevelDef[] levels;
    }
}
