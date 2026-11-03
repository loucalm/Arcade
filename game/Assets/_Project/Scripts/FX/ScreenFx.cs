using UnityEngine;
using UnityEngine.UI;

namespace Kenta.FX
{
    /// <summary>Flash plein écran (calque d'interface dans le canvas pixel) : drops, dégâts, éclairs.</summary>
    public class ScreenFx : MonoBehaviour
    {
        public static ScreenFx Instance { get; private set; }

        [SerializeField] Image overlay;
        [SerializeField] float decay = 3f;

        float alpha;
        Color tint = Color.white;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Intensité 0..1 ; la couleur est celle du flash.</summary>
        public void Flash(Color color, float intensity)
        {
            tint = color;
            alpha = Mathf.Max(alpha, Mathf.Clamp01(intensity));
        }

        void Update()
        {
            if (overlay == null) return;
            alpha = Mathf.MoveTowards(alpha, 0f, decay * Time.unscaledDeltaTime);
            // paliers de 1/6 : l'écran reste « pixel art » (pas de dégradé continu)
            float a = Mathf.Round(alpha * 6f) / 6f * 0.85f;
            overlay.color = new Color(tint.r, tint.g, tint.b, a);
            overlay.enabled = a > 0.01f;
        }
    }
}
