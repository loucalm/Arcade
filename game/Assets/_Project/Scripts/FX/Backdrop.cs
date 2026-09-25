using RythmeRunner.Level;
using RythmeRunner.Rhythm;
using UnityEngine;

namespace RythmeRunner.FX
{
    /// <summary>Décor de château nocturne en parallaxe, construit une seule fois au démarrage.</summary>
    public class Backdrop : MonoBehaviour
    {
        [System.Serializable]
        public class Layer
        {
            public float parallax = 0.3f;
            public Color color = new Color(0.14f, 0.07f, 0.26f);
            public int count = 14;
            public Vector2 widthRange = new Vector2(1f, 3f);
            public Vector2 heightRange = new Vector2(3f, 9f);
            public float baseY = -1f;
            public int sortingOrder = -50;
            [HideInInspector] public Transform root;
            [HideInInspector] public SpriteRenderer[] pieces;
            [HideInInspector] public float[] collapseBaseY;
            [HideInInspector] public Quaternion[] collapseBaseRotation;
        }

        sealed class BeatSprite
        {
            public SpriteRenderer renderer;
            public int phase;
            public bool downbeat;
            public Color baseColor;
        }

        [Header("Ressources")]
        [SerializeField] LevelTheme theme;
        [SerializeField] Sprite square;
        [SerializeField] Transform cameraTransform;
        [Header("Parallaxe")]
        [SerializeField] float span = 44f;
        [SerializeField] float horizonY = 1.6f;
        [SerializeField] Layer[] layers =
        {
            new Layer { parallax = 0.15f, color = new Color(0.17f, 0.10f, 0.28f), count = 10, baseY = -1.8f, sortingOrder = -60 },
            new Layer { parallax = 0.42f, color = new Color(0.10f, 0.045f, 0.16f), count = 12, baseY = -1.6f, sortingOrder = -50 },
            new Layer { parallax = 0.78f, color = new Color(0.035f, 0.018f, 0.065f), count = 10, baseY = -1.4f, sortingOrder = -40 }
        };

        readonly BeatSprite[] stars = new BeatSprite[24];
        readonly BeatSprite[] windows = new BeatSprite[22];
        readonly SpriteRenderer[] atmosphere = new SpriteRenderer[3];
        SpriteRenderer[] foreground;
        Transform foregroundRoot;
        Transform skyRoot;
        Transform celestialRoot;
        SpriteRenderer moon;
        SpriteRenderer moonGlow;
        Conductor subscribed;
        int downbeatBeat = int.MinValue;
        int lastBeat = int.MinValue;
        int collapseLayer = -1;
        float collapseElapsed;
        float collapseDuration;
        bool collapsing;

        Sprite Shape(Sprite fallback, System.Func<LevelTheme, Sprite> selector)
        {
            if (theme != null)
            {
                Sprite result = selector(theme);
                if (result != null) return result;
            }
            return fallback != null ? fallback : square;
        }

        SpriteRenderer AddSprite(string name, Transform parent, Sprite sprite, Color color, int order, Vector3 position, Vector3 scale, Material material = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            if (material != null) renderer.sharedMaterial = material;
            return renderer;
        }

        void Start()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            if (layers == null || layers.Length == 0)
                layers = new[] { new Layer { parallax = 0.2f, color = new Color(0.08f, 0.04f, 0.14f), count = 12, baseY = -1.7f, sortingOrder = -50 } };

            System.Random random = new System.Random(12345);
            skyRoot = new GameObject("Sky").transform;
            skyRoot.SetParent(transform, false);
            atmosphere[0] = AddSprite("Gradient", skyRoot, Shape(null, value => value.gradient), new Color(0.62f, 0.17f, 0.1f), -100, new Vector3(0f, 1.2f, 0f), new Vector3(40f, 9f, 1f));
            atmosphere[1] = AddSprite("HorizonMist", skyRoot, Shape(null, value => value.gradient), new Color(1f, 0.35f, 0.1f, 0.18f), -20, new Vector3(0f, horizonY, 0f), new Vector3(40f, 2.6f, 1f), theme != null ? theme.additive : null);
            atmosphere[2] = AddSprite("HorizonMistWide", skyRoot, Shape(null, value => value.gradient), new Color(1f, 0.62f, 0.25f, 0.08f), -19, new Vector3(4f, horizonY + 0.35f, 0f), new Vector3(40f, 1.5f, 1f), theme != null ? theme.additive : null);

            celestialRoot = new GameObject("MoonAndStars").transform;
            celestialRoot.SetParent(transform, false);
            moonGlow = AddSprite("MoonGlow", celestialRoot, Shape(null, value => value.glow), new Color(1f, 0.3f, 0.1f, 0.35f), -85, new Vector3(9f, 4.3f, 0f), new Vector3(4.2f, 4.2f, 1f), theme != null ? theme.additive : null);
            moon = AddSprite("Moon", celestialRoot, Shape(null, value => value.circle), new Color(1f, 0.6f, 0.38f, 0.9f), -84, new Vector3(9f, 4.3f, 0f), new Vector3(2.2f, 2.2f, 1f));
            for (int i = 0; i < stars.Length; i++)
            {
                float x = Mathf.Lerp(-4f, 38f, (float)random.NextDouble());
                float y = Mathf.Lerp(4.4f, 8.8f, (float)random.NextDouble());
                BeatSprite star = new BeatSprite();
                star.renderer = AddSprite("Star", celestialRoot, Shape(null, value => value.sparkle), new Color(1f, 0.85f, 0.7f, 0.45f), -82, new Vector3(x, y, 0f), Vector3.one * Mathf.Lerp(0.08f, 0.18f, (float)random.NextDouble()));
                star.phase = i % 8;
                star.baseColor = star.renderer.color;
                stars[i] = star;
            }
            BuildFarHills(random);
            BuildCastle(random);
            BuildNearArchitecture(random);
            BuildForeground(random);
        }

        void BuildFarHills(System.Random random)
        {
            Layer layer = layers[0];
            PrepareLayer(layer);
            for (int i = 0; i < layer.pieces.Length; i++)
            {
                float height = Mathf.Lerp(2.2f, 4.8f, (float)random.NextDouble());
                Transform item = layer.pieces[i].transform;
                item.localPosition = new Vector3(i * span / layer.pieces.Length, layer.baseY + height * 0.5f, 0f);
                item.localScale = new Vector3(Mathf.Lerp(2.2f, 5f, (float)random.NextDouble()), height, 1f);
                layer.pieces[i].sprite = Shape(null, value => value.rounded);
            }
        }

        void BuildCastle(System.Random random)
        {
            Layer layer = layers[Mathf.Min(1, layers.Length - 1)];
            PrepareLayer(layer);
            for (int i = 0; i < layer.pieces.Length; i++)
            {
                float height = Mathf.Lerp(3f, 7f, (float)random.NextDouble());
                Transform item = layer.pieces[i].transform;
                item.localPosition = new Vector3(i * span / layer.pieces.Length, layer.baseY + height * 0.5f, 0f);
                item.localScale = new Vector3(Mathf.Lerp(1.2f, 2.8f, (float)random.NextDouble()), height, 1f);
                layer.pieces[i].sprite = Shape(null, value => value.square);
            }
            Transform root = layer.root;
            Sprite roof = Shape(null, value => value.triangle);
            Sprite crenel = Shape(null, value => value.square);
            Sprite window = Shape(null, value => value.rounded);
            for (int i = 0; i < 7; i++)
            {
                float x = 2f + i * 5.5f;
                // Une tour sur deux a un toit pointu, l'autre des créneaux : le corps de tour descend jusqu'au sol.
                float top = 3.6f + (i % 3) * 0.9f;
                AddSprite("Tower", root, crenel, layer.color, layer.sortingOrder + 1, new Vector3(x, (layer.baseY + top) * 0.5f, 0f), new Vector3(1.8f, top - layer.baseY, 1f));
                if (i % 2 == 0)
                    AddSprite("Roof", root, roof, layer.color, layer.sortingOrder + 1, new Vector3(x, top + 0.9f, 0f), new Vector3(2.3f, 1.8f, 1f));
                else
                    for (int j = 0; j < 3; j++)
                        AddSprite("Crenel", root, crenel, layer.color, layer.sortingOrder + 1, new Vector3(x - 0.65f + j * 0.65f, top + 0.2f, 0f), new Vector3(0.4f, 0.4f, 1f));
                for (int j = 0; j < 2; j++)
                {
                    BeatSprite litWindow = new BeatSprite();
                    litWindow.renderer = AddSprite("Window", root, window, new Color(1f, 0.42f, 0.08f, 0.9f), layer.sortingOrder + 2, new Vector3(x - 0.35f + j * 0.7f, 1.6f + (i % 3) * 0.9f, 0f), new Vector3(0.18f, 0.38f, 1f));
                    litWindow.phase = (i * 2 + j) % 8;
                    litWindow.baseColor = litWindow.renderer.color;
                    litWindow.downbeat = (i + j) % 3 == 0;
                    windows[i * 2 + j] = litWindow;
                }
            }
        }

        void BuildNearArchitecture(System.Random random)
        {
            Layer layer = layers[layers.Length - 1];
            PrepareLayer(layer);
            for (int i = 0; i < layer.pieces.Length; i++)
            {
                float height = Mathf.Lerp(2.5f, 6f, (float)random.NextDouble());
                Transform item = layer.pieces[i].transform;
                item.localPosition = new Vector3(i * span / layer.pieces.Length, layer.baseY + height * 0.5f, 0f);
                item.localScale = new Vector3(Mathf.Lerp(0.8f, 1.8f, (float)random.NextDouble()), height, 1f);
                layer.pieces[i].sprite = Shape(null, value => value.square);
            }
            // Incendie au pied des tours : lueurs basses et discrètes (la zone de jeu reste lisible).
            Sprite glowSprite = Shape(null, value => value.glow);
            for (int i = 0; i < 5; i++)
                AddSprite("FireGlow", layer.root, glowSprite, new Color(1f, 0.38f, 0.08f, 0.28f), layer.sortingOrder - 1, new Vector3(3f + i * 8.8f, -0.6f, 0f), new Vector3(6f, 2.6f, 1f), theme != null ? theme.additive : null);
        }

        void BuildForeground(System.Random random)
        {
            foregroundRoot = new GameObject("ForegroundSilhouettes").transform;
            foregroundRoot.SetParent(transform, false);
            foreground = new SpriteRenderer[14];
            Sprite shape = Shape(null, value => value.square);
            for (int i = 0; i < foreground.Length; i++)
            {
                float y = i % 2 == 0 ? -1.1f : 7.1f;
                foreground[i] = AddSprite(i % 2 == 0 ? "Grass" : "Banner", foregroundRoot, shape, new Color(0.012f, 0.008f, 0.025f, 0.9f), -5, new Vector3(i * span / foreground.Length, y, 0f), new Vector3(Mathf.Lerp(0.15f, 0.4f, (float)random.NextDouble()), i % 2 == 0 ? 1.4f : 1.1f, 1f));
            }
        }

        void PrepareLayer(Layer layer)
        {
            layer.root = new GameObject("BackdropLayer").transform;
            layer.root.SetParent(transform, false);
            layer.pieces = new SpriteRenderer[Mathf.Max(1, layer.count)];
            layer.collapseBaseY = new float[layer.pieces.Length];
            layer.collapseBaseRotation = new Quaternion[layer.pieces.Length];
            for (int i = 0; i < layer.pieces.Length; i++)
            {
                layer.pieces[i] = AddSprite("Silhouette", layer.root, square, layer.color, layer.sortingOrder, Vector3.zero, Vector3.one);
                layer.collapseBaseY[i] = layer.pieces[i].transform.localPosition.y;
                layer.collapseBaseRotation[i] = layer.pieces[i].transform.localRotation;
            }
        }

        public void Collapse(float seconds)
        {
            if (collapsing || layers == null || layers.Length == 0) return;
            collapseLayer = layers.Length - 1;
            Layer layer = layers[collapseLayer];
            if (layer.pieces == null) return;
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

        void OnBeat(int beat)
        {
            if (subscribed == null || beat == lastBeat) return;
            lastBeat = beat;
            bool downbeat = beat % subscribed.BeatsPerBar == 0;
            if (downbeat) downbeatBeat = beat;
            for (int i = 0; i < stars.Length; i++)
            {
                BeatSprite star = stars[i];
                if (star.renderer == null || !star.renderer.isVisible || (beat + star.phase) % 2 != 0) continue;
                star.renderer.color = new Color(star.baseColor.r, star.baseColor.g, star.baseColor.b, 1f);
                star.renderer.transform.localScale = Vector3.one * (0.13f + (i % 3) * 0.025f);
            }
            for (int i = 0; i < windows.Length; i++)
            {
                BeatSprite window = windows[i];
                if (window == null || window.renderer == null || !window.renderer.isVisible) continue;
                if ((beat + window.phase) % 4 == 0 || window.downbeat && downbeat)
                    window.renderer.color = window.renderer.color.a > 0.2f ? new Color(0.12f, 0.025f, 0.04f, 0.25f) : window.baseColor;
            }
        }

        void LateUpdate()
        {
            if (subscribed == null && Conductor.Instance != null)
            {
                subscribed = Conductor.Instance;
                subscribed.OnBeat += OnBeat;
            }
            if (cameraTransform == null) return;
            float dt = Time.unscaledDeltaTime;
            float camX = cameraTransform.position.x;
            float camY = cameraTransform.position.y;
            float measurePulse = 0f;
            if (subscribed != null && downbeatBeat != int.MinValue)
                measurePulse = Mathf.Clamp01(1f - (float)(subscribed.SongBeat - downbeatBeat));
            if (skyRoot != null) skyRoot.position = new Vector3(camX, camY, 0f);
            if (celestialRoot != null) celestialRoot.position = new Vector3(camX * 0.975f - 8f, camY * 0.9f, 0f);
            if (moon != null && moon.isVisible)
            {
                float moonScale = 2.2f + measurePulse * 0.12f;
                moon.transform.localScale = new Vector3(moonScale, moonScale, 1f);
                moonGlow.transform.localScale = new Vector3(moonScale * 1.9f, moonScale * 1.9f, 1f);
            }
            if (atmosphere[1] != null && atmosphere[1].isVisible)
                atmosphere[1].color = new Color(1f, 0.35f, 0.1f, 0.14f + measurePulse * 0.1f);
            if (atmosphere[2] != null && atmosphere[2].isVisible)
                atmosphere[2].color = new Color(1f, 0.62f, 0.25f, 0.06f + measurePulse * 0.04f);
            for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
            {
                Layer layer = layers[layerIndex];
                if (layer.root == null) continue;
                float layerX = camX * (1f - layer.parallax);
                layer.root.position = new Vector3(layerX, camY * 0.08f, 0f);
                float left = camX - span * 0.5f;
                for (int i = layer.pieces.Length; i < layer.root.childCount; i++)
                {
                    Transform extra = layer.root.GetChild(i);
                    float extraX = layerX + extra.localPosition.x;
                    if (extraX < left) extra.localPosition += new Vector3(span, 0f, 0f);
                    else if (extraX > left + span) extra.localPosition -= new Vector3(span, 0f, 0f);
                }
                for (int i = 0; i < layer.pieces.Length; i++)
                {
                    SpriteRenderer piece = layer.pieces[i];
                    Transform item = piece.transform;
                    float worldX = layerX + item.localPosition.x;
                    if (worldX < left) item.localPosition += new Vector3(span, 0f, 0f);
                    else if (worldX > left + span) item.localPosition -= new Vector3(span, 0f, 0f);
                    if (collapsing && collapseLayer == layerIndex && piece.isVisible)
                    {
                        float delay = (Hash(i * 31 + 7) & 1023) / 1023f * Mathf.Min(0.18f, collapseDuration * 0.12f);
                        float t = Mathf.Clamp01((collapseElapsed - delay) / Mathf.Max(0.01f, collapseDuration));
                        float amount = t < 0.5f ? t * 2f : (1f - t) * 2f;
                        float direction = (Hash(i * 17 + 3) & 1) == 0 ? -1f : 1f;
                        item.localPosition = new Vector3(item.localPosition.x, layer.collapseBaseY[i] - amount * 2.2f, item.localPosition.z);
                        item.localRotation = layer.collapseBaseRotation[i] * Quaternion.Euler(0f, 0f, direction * amount * 18f);
                    }
                    if (layerIndex == layers.Length - 1 && piece.isVisible)
                        piece.color = Color.Lerp(layer.color, new Color(1f, 0.42f, 0.12f), measurePulse * 0.3f);
                }
            }
            if (foreground != null)
            {
                float foregroundX = camX * 1.18f;
                if (foregroundRoot != null) foregroundRoot.position = new Vector3(foregroundX, camY * 0.04f, 0f);
                float left = camX - span * 0.5f;
                for (int i = 0; i < foreground.Length; i++)
                {
                    SpriteRenderer item = foreground[i];
                    if (item == null || !item.isVisible) continue;
                    float worldX = foregroundX + item.transform.localPosition.x;
                    if (worldX < left) item.transform.localPosition += new Vector3(span, 0f, 0f);
                    else if (worldX > left + span) item.transform.localPosition -= new Vector3(span, 0f, 0f);
                }
            }
            if (collapsing)
            {
                collapseElapsed += dt;
                if (collapseElapsed >= collapseDuration) ResetCollapse();
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
