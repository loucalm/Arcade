using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Pièce ramassée (2026-11-27, « plus satisfaisant à ramasser ») : au lieu de disparaître d'un coup, elle saute de
    /// quelques pixels en clignotant, puis s'éteint. Positions recalées sur la grille de pixels, aucune échelle. La pièce
    /// revient intacte quand le niveau la réactive (reprise après une mort : OnEnable).
    /// </summary>
    public class CoinPickup : MonoBehaviour
    {
        const float Duration = 0.16f;              // s
        const float Rise = 6f / PixelScreen.Ppu;   // u : 6 px du monde
        const float BlinkStep = 0.04f;             // s : clignote dans la seconde moitié

        SpriteRenderer[] renderers;
        Vector3 basePosition;
        float t;
        bool playing;

        void Awake() => renderers = GetComponentsInChildren<SpriteRenderer>(true);

        void OnEnable()
        {
            playing = false;
            enabled = false;
            if (renderers != null) foreach (var r in renderers) r.enabled = true;
        }

        public void Play()
        {
            if (playing) return;
            basePosition = transform.localPosition;
            t = 0f;
            playing = true;
            enabled = true;
        }

        /// <summary>Hauteur (u, sur la grille) et visibilité à l'instant `time` de l'animation.</summary>
        public static float RiseAt(float time) => PixelScreen.Snap(Rise * Mathf.Clamp01(time / Duration));
        public static bool VisibleAt(float time) => time < Duration * 0.5f || ((int)(time / BlinkStep) & 1) == 0;

        void Update()
        {
            if (!playing) { enabled = false; return; }
            t += Time.deltaTime;
            if (t >= Duration)
            {
                playing = false;
                transform.localPosition = basePosition;
                gameObject.SetActive(false);
                return;
            }
            transform.localPosition = basePosition + new Vector3(0f, RiseAt(t), 0f);
            bool visible = VisibleAt(t);
            foreach (var r in renderers) r.enabled = visible;
        }
    }
}
