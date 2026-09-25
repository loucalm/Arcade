using RythmeRunner.Rhythm;
using UnityEngine;

namespace RythmeRunner.FX
{
    /// <summary>
    /// Décor de fond en parallaxe infinie : colonnes qui défilent plus lentement que la caméra
    /// et s'illuminent sur les temps forts. Généré au démarrage (aucun asset requis).
    /// </summary>
    public class Backdrop : MonoBehaviour
    {
        [System.Serializable]
        public class Layer
        {
            public float parallax = 0.3f;
            public Color color = new(0.14f, 0.07f, 0.26f);
            public int count = 14;
            public Vector2 widthRange = new(1f, 3f);
            public Vector2 heightRange = new(3f, 9f);
            public float baseY = -1f;
            public int sortingOrder = -50;
            [HideInInspector] public Transform root;
            [HideInInspector] public SpriteRenderer[] pieces;
            [HideInInspector] public float[] collapseBaseY;
            [HideInInspector] public Quaternion[] collapseBaseRotation;
        }

        [SerializeField] Sprite square;
        [SerializeField] Transform cameraTransform;
        [SerializeField] float span = 44f;
        [SerializeField] Layer[] layers =
        {
            new() { parallax = 0.15f, color = new Color(0.09f, 0.05f, 0.18f), count = 12, widthRange = new Vector2(2f, 4f), heightRange = new Vector2(5f, 11f), baseY = -2f, sortingOrder = -60 },
            new() { parallax = 0.4f, color = new Color(0.14f, 0.07f, 0.26f), count = 16, widthRange = new Vector2(0.8f, 2.2f), heightRange = new Vector2(2f, 7f), baseY = -1f, sortingOrder = -50 },
        };

        float glow;
        int collapseLayer = -1;
        float collapseElapsed;
        float collapseDuration;
        bool collapsing;
        Conductor subscribed;

        void Start()
        {
            var rng = new System.Random(12345);
            foreach (var layer in layers)
            {
                layer.root = new GameObject($"Layer {layer.parallax}").transform;
                layer.root.SetParent(transform, false);
                layer.pieces = new SpriteRenderer[layer.count];
                layer.collapseBaseY = new float[layer.count];
                layer.collapseBaseRotation = new Quaternion[layer.count];
                for (int i = 0; i < layer.count; i++)
                {
                    var go = new GameObject("Column");
                    go.transform.SetParent(layer.root, false);
                    float w = Mathf.Lerp(layer.widthRange.x, layer.widthRange.y, (float)rng.NextDouble());
                    float h = Mathf.Lerp(layer.heightRange.x, layer.heightRange.y, (float)rng.NextDouble());
                    go.transform.localPosition = new Vector3(i * span / layer.count + (float)rng.NextDouble(), layer.baseY + h * 0.5f, 0f);
                    go.transform.localScale = new Vector3(w, h, 1f);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = square;
                    sr.color = layer.color;
                    sr.sortingOrder = layer.sortingOrder;
                    layer.pieces[i] = sr;
                    layer.collapseBaseY[i] = go.transform.localPosition.y;
                    layer.collapseBaseRotation[i] = go.transform.localRotation;
                }
            }
        }

        /// <summary>Fait tomber puis remonter les colonnes du calque le plus proche.</summary>
        public void Collapse(float seconds)
        {
            if (collapsing || layers == null || layers.Length == 0) return;
            collapseLayer = 0;
            for (int i = 1; i < layers.Length; i++)
                if (layers[i].parallax > layers[collapseLayer].parallax) collapseLayer = i;
            if (layers[collapseLayer].pieces == null) return;

            Layer layer = layers[collapseLayer];
            for (int i = 0; i < layer.pieces.Length; i++)
            {
                Transform piece = layer.pieces[i].transform;
                layer.collapseBaseY[i] = piece.localPosition.y;
                layer.collapseBaseRotation[i] = piece.localRotation;
            }
            collapseElapsed = 0f;
            collapseDuration = Mathf.Max(0.01f, seconds);
            collapsing = true;
        }

        /// <summary>Réinitialise immédiatement l'animation du calque effondré.</summary>
        public void ResetCollapse()
        {
            if (collapseLayer >= 0 && collapseLayer < layers.Length)
            {
                Layer layer = layers[collapseLayer];
                if (layer.pieces != null)
                    for (int i = 0; i < layer.pieces.Length; i++)
                    {
                        Transform piece = layer.pieces[i].transform;
                        piece.localPosition = new Vector3(piece.localPosition.x, layer.collapseBaseY[i], piece.localPosition.z);
                        piece.localRotation = layer.collapseBaseRotation[i];
                    }
            }
            collapsing = false;
            collapseElapsed = 0f;
        }

        void OnDestroy()
        {
            if (subscribed != null) subscribed.OnBeat -= OnBeat;
        }

        void OnBeat(int beat) => glow = beat % subscribed.BeatsPerBar == 0 ? 1f : 0.4f;

        void LateUpdate()
        {
            if (subscribed == null && Conductor.Instance != null)
            {
                subscribed = Conductor.Instance;
                subscribed.OnBeat += OnBeat;
            }
            if (cameraTransform == null) return;

            if (collapsing)
            {
                collapseElapsed += Time.unscaledDeltaTime;
                if (collapseElapsed >= collapseDuration) ResetCollapse();
            }

            float camX = cameraTransform.position.x;
            glow = Mathf.Lerp(glow, 0f, 1f - Mathf.Exp(-5f * Time.unscaledDeltaTime));
            for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
            {
                Layer layer = layers[layerIndex];
                if (layer.root == null) continue;
                // Le calque suit la caméra à (1 - parallax) : il semble défiler plus lentement.
                float layerX = camX * (1f - layer.parallax);
                layer.root.position = new Vector3(layerX, cameraTransform.position.y * 0.2f, 0f);
                float left = camX - span * 0.5f;
                var c = Color.Lerp(layer.color, layer.color * 1.9f, glow * layer.parallax * 1.5f);
                foreach (var piece in layer.pieces)
                {
                    var t = piece.transform;
                    float worldX = layerX + t.localPosition.x;
                    // Recyclage : une colonne sortie à gauche repasse à droite.
                    if (worldX < left) t.localPosition += new Vector3(span, 0f, 0f);
                    else if (worldX > left + span) t.localPosition -= new Vector3(span, 0f, 0f);
                    piece.color = c;
                }
                if (collapsing && collapseLayer == layerIndex)
                {
                    float maxDelay = Mathf.Min(0.18f, collapseDuration * 0.12f);
                    float duration = Mathf.Max(0.01f, collapseDuration - maxDelay);
                    for (int i = 0; i < layer.pieces.Length; i++)
                    {
                        Transform piece = layer.pieces[i].transform;
                        float delay = (Hash(i * 31 + 7) & 1023) / 1023f * maxDelay;
                        float t = Mathf.Clamp01((collapseElapsed - delay) / duration);
                        float amount = t < 0.5f ? t * 2f : (1f - t) * 2f;
                        float direction = (Hash(i * 17 + 3) & 1) == 0 ? -1f : 1f;
                        piece.localPosition = new Vector3(piece.localPosition.x, layer.collapseBaseY[i] - amount * 2.2f, piece.localPosition.z);
                        piece.localRotation = layer.collapseBaseRotation[i] * Quaternion.Euler(0f, 0f, direction * amount * 18f);
                    }
                }
            }
        }

        static int Hash(int value)
        {
            value ^= value << 13;
            value ^= value >> 17;
            return value ^ (value << 5);
        }
    }
}
