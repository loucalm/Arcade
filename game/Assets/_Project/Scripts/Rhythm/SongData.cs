using UnityEngine;

namespace RythmeRunner.Rhythm
{
    /// <summary>
    /// Données d'un morceau : clip, tempo, offset. Un asset par morceau dans _Project/ScriptableObjects/Songs.
    /// Le BPM doit être CONSTANT sur tout le morceau.
    /// </summary>
    [CreateAssetMenu(fileName = "Song_", menuName = "Rythme Runner/Song Data")]
    public class SongData : ScriptableObject
    {
        public string title = "Sans titre";
        public AudioClip clip;
        [Tooltip("Version 8-bit / remix : même BPM, même structure.")]
        public AudioClip remixClip;
        [Min(1f)] public float bpm = 120f;
        [Tooltip("Délai (s) entre le début du fichier audio et le beat 0.")]
        public float offsetSeconds;
        [Min(1)] public int beatsPerBar = 4;
        [Tooltip("Chart JSON du niveau (voir docs/chart-format.md). Optionnel pour un morceau de menu.")]
        public TextAsset chart;
    }
}
