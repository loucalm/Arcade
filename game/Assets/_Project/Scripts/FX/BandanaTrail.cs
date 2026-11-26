using Kenta.Core;
using Kenta.Player;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Les deux pans du bandeau (docs/da-kenta.md § 4) : ils s'allongent avec le multiplicateur de combo et forment la
    /// traînée. Chaîne de carrés de 2 px (un ruban plein) qui suivent l'historique des positions du héros, sur la grille
    /// de pixels. Elle n'apparaît qu'à partir de ×minMultiplier : en dessous, le bandeau du héros (qui flotte déjà,
    /// ADR-022) suffit, et la traînée en pointillés faisait du bruit (audit du 2026-11-25).
    /// En gravité inversée, le héros est retourné (tête en bas) : la traînée part de sa tête, sous ses pieds. Le sens de
    /// la gravité est gardé avec chaque position de l'historique, pour que la traînée se retourne avec le héros au
    /// portail au lieu de sauter d'un coup.
    /// </summary>
    public class BandanaTrail : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Sprite pixel;
        [SerializeField] Vector2 anchor = new(-0.18f, 0.95f);
        [SerializeField] int maxSegments = 48;
        [Tooltip("Retard (s) entre deux pixels de la traînée : 1 px à la vitesse de course.")]
        [SerializeField] float segmentSpacing = 0.0032f;
        [SerializeField] Color color = Color.white;
        [Tooltip("Multiplicateur à partir duquel la traînée apparaît (récompense du combo).")]
        [SerializeField] int minMultiplier = 4;

        SpriteRenderer[] segs;
        Vector2[] history;            // point d'attache (tête) du héros, déjà retourné en gravité inversée
        float[] signs;                // +1 gravité normale, −1 inversée : sens des ondulations de ce point
        float[] times;
        PlayerController player;
        int head, count;
        const int Capacity = 128;

        public Color Color { get => color; set => color = value; }

        void Awake()
        {
            segs = new SpriteRenderer[maxSegments * 2];
            for (int i = 0; i < segs.Length; i++)
            {
                var go = new GameObject("B");
                go.layer = gameObject.layer;   // calque acteurs
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = pixel;
                sr.sortingOrder = 19;
                go.SetActive(false);
                segs[i] = sr;
            }
            history = new Vector2[Capacity];
            signs = new float[Capacity];
            times = new float[Capacity];
            if (target != null) player = target.GetComponent<PlayerController>();
        }

        /// <summary>Point d'attache de la traînée (la tête) : au-dessus des pieds, ou en dessous tête en bas.</summary>
        public static Vector2 AnchorPoint(Vector2 feet, Vector2 anchor, bool inverted) =>
            new(feet.x + anchor.x, feet.y + (inverted ? -anchor.y : anchor.y));

        public void Clear()
        {
            count = 0;
            foreach (var s in segs) s.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                Clear();
                return;
            }
            bool inverted = player != null && player.IsInverted;
            history[head] = AnchorPoint(target.position, anchor, inverted);
            signs[head] = inverted ? -1f : 1f;
            times[head] = Time.time;
            head = (head + 1) % Capacity;
            count = Mathf.Min(count + 1, Capacity);

            int mult = RunManager.Instance != null ? RunManager.Instance.Multiplier : 1;
            Vector2 shift = CameraRig.Instance != null ? CameraRig.Instance.ActorShift : Vector2.zero;
            int length = mult < minMultiplier ? 0 : Mathf.Min(maxSegments, 12 + (mult - minMultiplier) * 9);
            for (int pan = 0; pan < 2; pan++)
            {
                for (int i = 0; i < maxSegments; i++)
                {
                    var sr = segs[pan * maxSegments + i];
                    if (i >= length || i >= count) { sr.gameObject.SetActive(false); continue; }
                    float wanted = Time.time - (i + 1) * segmentSpacing;
                    Vector2 p = SampleAt(wanted, out float sign);
                    float wave = Mathf.Sin(Time.time * 14f - i * 0.18f) * 0.035f * (i / (float)maxSegments);
                    float tilt = pan == 0 ? 0.045f : -0.02f;
                    // calque acteurs : recalé sur la grille de l'écran comme le héros (CameraRig.ActorShift)
                    var pos = new Vector3(PixelScreen.Snap(p.x + shift.x), PixelScreen.Snap(p.y + sign * (tilt + wave)), 0f);
                    sr.transform.position = pos;
                    float fade = 1f - i / (float)maxSegments * 0.7f;
                    sr.color = new Color(color.r, color.g, color.b, fade);
                    sr.transform.localScale = Vector3.one * 2f;
                    sr.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>Point d'attache à l'instant `time` (interpolé entre deux images), pour une traînée continue ; `sign` :
        /// sens de la gravité à cet instant.</summary>
        Vector2 SampleAt(float time, out float sign)
        {
            for (int k = 1; k < count; k++)
            {
                int newer = (head - k + Capacity * 2) % Capacity;
                int older = (head - 1 - k + Capacity * 2) % Capacity;
                if (times[older] <= time)
                {
                    float span = Mathf.Max(0.0001f, times[newer] - times[older]);
                    sign = signs[newer];
                    return Vector2.Lerp(history[older], history[newer], Mathf.Clamp01((time - times[older]) / span));
                }
            }
            int oldest = (head - count + Capacity) % Capacity;
            sign = signs[oldest];
            return history[oldest];
        }
    }
}
