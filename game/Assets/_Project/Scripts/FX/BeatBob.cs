using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>Petit rebond sur le temps (2 px vers le bas sur le temps, puis retour) : la flèche « saute dessus ».</summary>
    public class BeatBob : MonoBehaviour
    {
        [SerializeField] float depthPx = 2f;
        Vector3 rest;

        void Awake() => rest = transform.localPosition;

        void LateUpdate()
        {
            var c = Conductor.Instance;
            if (c == null || !c.IsPlaying) return;
            float phase = (float)(c.SongBeat - System.Math.Floor(c.SongBeat));
            float down = phase < 0.25f ? depthPx : 0f;                  // pixel art : deux positions, pas d'interpolation
            transform.localPosition = rest - new Vector3(0f, down / PixelScreen.Ppu, 0f);
        }
    }
}
