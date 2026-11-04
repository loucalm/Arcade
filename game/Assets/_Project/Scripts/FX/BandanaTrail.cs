using Kenta.Core;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Les deux pans du bandeau (docs/da-kenta.md § 4) : ils s'allongent avec le multiplicateur de combo (×1 à ×8) et
    /// forment la traînée. Chaîne de petits carrés qui suivent l'historique des positions du héros, sur la grille de pixels.
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

        SpriteRenderer[] segs;
        Vector2[] history;
        float[] times;
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
            times = new float[Capacity];
        }

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
            history[head] = target.position;
            times[head] = Time.time;
            head = (head + 1) % Capacity;
            count = Mathf.Min(count + 1, Capacity);

            int mult = RunManager.Instance != null ? RunManager.Instance.Multiplier : 1;
            Vector2 shift = CameraRig.Instance != null ? CameraRig.Instance.ActorShift : Vector2.zero;
            int length = Mathf.Min(maxSegments, 8 + mult * 5);
            for (int pan = 0; pan < 2; pan++)
            {
                for (int i = 0; i < maxSegments; i++)
                {
                    var sr = segs[pan * maxSegments + i];
                    if (i >= length || i >= count) { sr.gameObject.SetActive(false); continue; }
                    float wanted = Time.time - (i + 1) * segmentSpacing;
                    Vector2 p = SampleAt(wanted);
                    float wave = Mathf.Sin(Time.time * 14f - i * 0.18f) * 0.035f * (i / (float)maxSegments);
                    float tilt = pan == 0 ? 0.045f : -0.02f;
                    // calque acteurs : recalé sur la grille de l'écran comme le héros (CameraRig.ActorShift)
                    var pos = new Vector3(PixelScreen.Snap(p.x + anchor.x + shift.x), PixelScreen.Snap(p.y + anchor.y + tilt + wave), 0f);
                    sr.transform.position = pos;
                    float fade = 1f - i / (float)maxSegments * 0.7f;
                    sr.color = new Color(color.r, color.g, color.b, fade);
                    sr.transform.localScale = Vector3.one * (i < 10 ? 2f : 1f);
                    sr.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>Position du héros à l'instant `time` (interpolée entre deux images), pour une traînée continue.</summary>
        Vector2 SampleAt(float time)
        {
            for (int k = 1; k < count; k++)
            {
                int newer = (head - k + Capacity * 2) % Capacity;
                int older = (head - 1 - k + Capacity * 2) % Capacity;
                if (times[older] <= time)
                {
                    float span = Mathf.Max(0.0001f, times[newer] - times[older]);
                    return Vector2.Lerp(history[older], history[newer], Mathf.Clamp01((time - times[older]) / span));
                }
            }
            return history[(head - count + Capacity) % Capacity];
        }
    }
}
