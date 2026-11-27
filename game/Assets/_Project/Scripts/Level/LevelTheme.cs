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
        [Tooltip("Ennemi volant (à frapper en l'air), commun aux biomes : la chauve-souris du PDF.")]
        public Sprite flyer;
        [Tooltip("Battement d'ailes de la chauve-souris (haut, milieu, bas, milieu), avec un liseré clair : tools/gen-bat.py.")]
        public Sprite[] flyerFrames;
        [Tooltip("Cuirassé (on lui saute dessus) : carapace et tremplin jaune, deux images (repos, tremplin écrasé sur le temps). tools/gen-stomper.py.")]
        public Sprite[] stomper;
        [Tooltip("Carré blanc de 1 px (40 px/unité) : traînée du bandeau, particules.")]
        public Sprite pixel;

        [Header("La Sourdine (boss du niveau 9, Gen/boss : tools/gemini-niveau9/gen_sheets.py --install)")]
        public SourdineSprites sourdine;
    }

    /// <summary>Pièces de la Sourdine, assemblées et animées par FX/SourdineBoss.</summary>
    [System.Serializable]
    public class SourdineSprites
    {
        public Sprite body;
        [Tooltip("Corps flottant (176×168 : cornes, coulures) : la Sourdine monte et descend devant le héros (27/11).")]
        public Sprite floatBody;
        [Tooltip("Les deux moitiés tranchées par le dernier coup de katana (yeux fêlés, bouche qui hurle).")]
        public Sprite halfTop, halfBottom;
        [Tooltip("Œil : ouvert, mi-clos, fermé, grand ouvert (colère), fêlé.")]
        public Sprite[] eyes;
        [Tooltip("Pupille : dilatée, normale, tête d'épingle.")]
        public Sprite[] pupils;
        [Tooltip("Bouche : sourire, entrouverte, hurlement (charge le laser).")]
        public Sprite[] mouths;
        public Sprite beam, beamWarning, muzzle;
        [Tooltip("Note noire (œil ouvert, œil fermé) et note dorée (renvoyée).")]
        public Sprite[] noteBlack;
        public Sprite noteGold;
    }
}
