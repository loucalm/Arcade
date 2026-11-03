using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Décor façon Rider : rochers « low poly » bleu-vert en parallaxe infinie (deux calques) qui
    /// s'éclaircissent sur les temps forts, et un voile (vignette + lueur d'horizon) collé à la caméra.
    /// Généré au démarrage depuis les sprites du LevelTheme.
    /// </summary>
    public class Backdrop : MonoBehaviour
    {
        [System.Serializable]
        public class Layer
        {
            [Tooltip("0 = fixe à l'écran (très loin), 1 = défile comme le niveau.")]
            public float parallax = 0.3f;
            public Color tint = new(0.5f, 0.5f, 0.5f);
            public int count = 10;
            [Tooltip("Échelle des rochers (1 = 2,56 × 3,84 unités).")]
            public Vector2 scaleRange = new(1f, 1.6f);
            public float baseY = -1f;
            public int sortingOrder = -50;
            [HideInInspector] public Transform root;
            [HideInInspector] public SpriteRenderer[] pieces;
        }

        [SerializeField] Sprite[] rocks;
        [SerializeField] Sprite veil;
        [SerializeField] Camera cam;
        [SerializeField] float span = 64f;
        [SerializeField] int veilSortingOrder = -40;
        [Tooltip("Éclaircissement des rochers sur le temps fort (0 = aucun).")]
        [SerializeField, Range(0f, 2f)] float beatGlow = 1f;
        [SerializeField] Layer[] layers =
        {
            new() { parallax = 0.12f, tint = new Color(0.30f, 0.52f, 0.60f), count = 9, scaleRange = new Vector2(1.7f, 2.5f), baseY = -2.2f, sortingOrder = -60 },
            new() { parallax = 0.35f, tint = new Color(0.46f, 0.74f, 0.84f), count = 11, scaleRange = new Vector2(0.9f, 1.5f), baseY = -1.0f, sortingOrder = -50 },
        };

        float glow;
        Conductor subscribed;
        Transform veilTransform;

        void Start()
        {
            if (cam == null) cam = Camera.main;
            var rng = new System.Random(4242);
            foreach (var layer in layers)
            {
                layer.root = new GameObject($"Layer {layer.parallax}").transform;
                layer.root.SetParent(transform, false);
                layer.pieces = new SpriteRenderer[layer.count];
                for (int i = 0; i < layer.count; i++)
                {
                    var go = new GameObject("Rock");
                    go.transform.SetParent(layer.root, false);
                    float s = Mathf.Lerp(layer.scaleRange.x, layer.scaleRange.y, (float)rng.NextDouble());
                    bool flip = rng.NextDouble() < 0.5;
                    go.transform.localPosition = new Vector3(i * span / layer.count + (float)rng.NextDouble() * 2f, layer.baseY, 0f);
                    go.transform.localScale = new Vector3(flip ? -s : s, s, 1f);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = rocks[rng.Next(rocks.Length)];
                    sr.color = layer.tint;
                    sr.sortingOrder = layer.sortingOrder;
                    layer.pieces[i] = sr;
                }
            }

            if (veil != null && cam != null)
            {
                var go = new GameObject("Veil");
                veilTransform = go.transform;
                veilTransform.SetParent(cam.transform, false);
                veilTransform.localPosition = new Vector3(0f, 0f, 10f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = veil;
                sr.sortingOrder = veilSortingOrder;
            }
        }

        void OnDestroy()
        {
            if (subscribed != null) subscribed.OnBeat -= OnBeat;
        }

        void OnBeat(int beat) => glow = beat % subscribed.BeatsPerBar == 0 ? 1f : 0.35f;

        void LateUpdate()
        {
            if (subscribed == null && Conductor.Instance != null)
            {
                subscribed = Conductor.Instance;
                subscribed.OnBeat += OnBeat;
            }
            if (cam == null) return;

            var camPos = cam.transform.position;
            glow = Mathf.Lerp(glow, 0f, 1f - Mathf.Exp(-5f * Time.unscaledDeltaTime));
            foreach (var layer in layers)
            {
                if (layer.root == null) continue;
                // Le calque suit la caméra à (1 - parallax) : il semble défiler plus lentement.
                float layerX = camPos.x * (1f - layer.parallax);
                layer.root.position = new Vector3(layerX, camPos.y * 0.25f, 0f);
                float left = camPos.x - span * 0.5f;
                var c = Color.Lerp(layer.tint, Color.white, glow * beatGlow * layer.parallax * 0.9f);
                foreach (var piece in layer.pieces)
                {
                    var t = piece.transform;
                    float worldX = layerX + t.localPosition.x;
                    // Recyclage : un rocher sorti à gauche repasse à droite.
                    if (worldX < left) t.localPosition += new Vector3(span, 0f, 0f);
                    else if (worldX > left + span) t.localPosition -= new Vector3(span, 0f, 0f);
                    piece.color = c;
                }
            }

            if (veilTransform != null)
            {
                // Le voile recouvre toujours l'écran, même pendant un zoom de caméra.
                float h = cam.orthographicSize * 2f + 1f, w = h * cam.aspect + 1f;
                var size = veil.bounds.size;
                veilTransform.localScale = new Vector3(w / size.x, h / size.y, 1f);
            }
        }
    }
}
