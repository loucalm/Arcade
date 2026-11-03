using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>Fait sautiller un objet d'1 pixel sur chaque temps (ennemis qui dansent, pièces qui pulsent).</summary>
    public class BeatHop : MonoBehaviour
    {
        [SerializeField] int pixels = 1;
        [SerializeField, Min(1)] int everyNBeats = 1;

        Conductor subscribed;
        Vector3 basePos;
        bool up;

        public void Configure(int hopPixels, int everyN = 1)
        {
            pixels = hopPixels;
            everyNBeats = Mathf.Max(1, everyN);
        }

        void Awake() => basePos = transform.localPosition;
        void OnEnable() => TrySubscribe();

        void OnDisable()
        {
            if (subscribed != null) subscribed.OnBeat -= Hop;
            subscribed = null;
            transform.localPosition = basePos;
        }

        void Update()
        {
            if (subscribed == null) TrySubscribe();
        }

        void TrySubscribe()
        {
            if (subscribed != null || Conductor.Instance == null) return;
            subscribed = Conductor.Instance;
            subscribed.OnBeat += Hop;
        }

        void Hop(int beat)
        {
            if (beat % everyNBeats != 0) return;
            up = !up;
            transform.localPosition = basePos + (up ? Vector3.up * pixels / PixelScreen.Ppu : Vector3.zero);
        }
    }
}
