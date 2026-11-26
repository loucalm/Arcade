using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>Halo qui s'illumine sur chaque temps, puis redescend (deux paliers : pixel art, pas de dégradé continu).</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BeatGlow : MonoBehaviour
    {
        SpriteRenderer sr;
        float baseAlpha;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            baseAlpha = sr.color.a;
        }

        void LateUpdate()
        {
            var c = Conductor.Instance;
            if (c == null || !c.IsPlaying) return;
            float phase = (float)(c.SongBeat - System.Math.Floor(c.SongBeat));
            var col = sr.color;
            col.a = phase < 0.25f ? Mathf.Min(1f, baseAlpha + 0.35f) : baseAlpha;
            sr.color = col;
        }
    }
}
