using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Vol stationnaire : l'image monte et descend en continu (sinus), recalée sur la grille de pixels. Seulement
    /// visuel : la boîte de l'objet (LevelObject.bounds) ne bouge pas, le jeu reste le même. Pour les chauves-souris
    /// (retour de Lucas : « fais-les bouger en hauteur pour vraiment voir que ça bouge »).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Hover : MonoBehaviour
    {
        [SerializeField] float amplitude = 0.1f;     // u (4 px du monde)
        [SerializeField, Min(0.1f)] float period = 0.9f;
        float baseY, phase;
        SpriteRenderer sr;

        public void Setup(float amplitudeUnits, float periodSeconds)
        {
            amplitude = amplitudeUnits;
            period = Mathf.Max(0.1f, periodSeconds);
            baseY = transform.localPosition.y;
            phase = Random.value * period;
        }

        /// <summary>Décalage vertical (u) à l'instant t, sur la grille de pixels.</summary>
        public static float Offset(float amplitudeUnits, float periodSeconds, float t) =>
            PixelScreen.Snap(amplitudeUnits * Mathf.Sin(t * 2f * Mathf.PI / Mathf.Max(0.1f, periodSeconds)));

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            baseY = transform.localPosition.y;
        }

        // comme SpriteCycle : on n'anime que ce qui est à l'écran
        void Start() => enabled = sr != null && sr.isVisible;
        void OnBecameVisible() => enabled = true;
        void OnBecameInvisible() => enabled = false;

        void Update()
        {
            var p = transform.localPosition;
            float y = baseY + Offset(amplitude, period, Time.unscaledTime + phase);
            if (p.y != y) transform.localPosition = new Vector3(p.x, y, p.z);
        }
    }
}
