using UnityEngine;

namespace RythmeRunner.FX
{
    /// <summary>
    /// Mur de son néon qui poursuit le joueur (purement visuel, aucune collision). Il reste au bord gauche de
    /// l'écran, avale le joueur quand il est touché, puis recule sur quelques beats après le respawn.
    /// </summary>
    public class Chaser : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Sprite square;
        [SerializeField] Material additive;
        [Tooltip("Mur de feu façon Castle Rock : sprite triangle = flammes.")]
        [SerializeField] Color barColor = new(1f, 0.45f, 0.1f, 0.9f);
        [SerializeField] Color haloColor = new(1f, 0.2f, 0.05f, 0.35f);
        [SerializeField, Min(1)] int barCount = 12;
        [SerializeField] float wallHeight = 12f;
        [Tooltip("Distance entre le joueur et le front du mur (le bord gauche de l'écran est à ~2,1 u du joueur).")]
        [SerializeField] float normalDistance = 1.7f;
        [Tooltip("Distance au respawn : le mur repart de là et recule jusqu'à normalDistance.")]
        [SerializeField] float lungeDistance = 0.7f;
        [SerializeField] float lungeBeats = 4f;
        [Tooltip("Durée (s, temps réel) pendant laquelle le mur avale le joueur touché. L'audio est coupé à ce moment-là.")]
        [SerializeField] float catchSeconds = 0.35f;

        Transform wall;
        Transform[] bars;
        float[] heights;
        float[] targetHeights;
        Transform halo;
        Transform edge;
        RythmeRunner.Core.RunManager runManager;
        RythmeRunner.Rhythm.Conductor conductor;
        float lungeStartBeat;
        float lungeEndBeat;
        bool lunging;
        bool catching;
        float catchElapsed;

        void Start()
        {
            barCount = Mathf.Max(1, barCount);
            wall = new GameObject("Chaser Wall").transform;
            wall.SetParent(transform, false);
            bars = new Transform[barCount];
            heights = new float[barCount];
            targetHeights = new float[barCount];
            for (int i = 0; i < barCount; i++)
            {
                GameObject go = new GameObject("Equalizer Bar");
                go.transform.SetParent(wall, false);
                SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = square;
                renderer.color = barColor;
                renderer.sortingOrder = 25;
                if (additive != null) renderer.sharedMaterial = additive;
                bars[i] = go.transform;
                heights[i] = targetHeights[i] = wallHeight * 0.5f;
            }
            halo = CreatePart("Halo", haloColor, 0.65f, wallHeight * 0.9f, 20);
            edge = CreatePart("Player Edge", new Color(1f, 0.85f, 0.3f, 0.9f), 0.16f, wallHeight * 1.05f, 28);
            TrySubscribeRunManager();
            TrySubscribeConductor();
        }

        Transform CreatePart(string name, Color color, float width, float height, int sortingOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(wall, false);
            go.transform.localScale = new Vector3(width, height, 1f);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            if (additive != null) renderer.sharedMaterial = additive;
            return go.transform;
        }

        void OnDestroy()
        {
            if (conductor != null) conductor.OnBeat -= OnBeat;
            if (runManager != null)
            {
                runManager.OnDamaged -= OnDamaged;
                runManager.OnRespawned -= OnRespawned;
                runManager.OnRunStarted -= OnRunStarted;
            }
        }

        void TrySubscribeRunManager()
        {
            if (runManager != null || RythmeRunner.Core.RunManager.Instance == null) return;
            runManager = RythmeRunner.Core.RunManager.Instance;
            runManager.OnDamaged += OnDamaged;
            runManager.OnRespawned += OnRespawned;
            runManager.OnRunStarted += OnRunStarted;
        }

        void TrySubscribeConductor()
        {
            if (conductor != null || RythmeRunner.Rhythm.Conductor.Instance == null) return;
            conductor = RythmeRunner.Rhythm.Conductor.Instance;
            conductor.OnBeat += OnBeat;
        }

        void OnRunStarted() => lunging = catching = false;

        // Pendant un dégât, RunManager.IsRunning est faux et l'audio est coupé : l'avancée se fait en temps réel.
        void OnDamaged()
        {
            catching = true;
            catchElapsed = 0f;
            lunging = false;
        }

        void OnRespawned()
        {
            catching = false;
            lungeStartBeat = conductor != null ? (float)conductor.SongBeat : 0f;
            lungeEndBeat = lungeStartBeat + lungeBeats;
            lunging = true;
        }

        void OnBeat(int beat)
        {
            for (int i = 0; i < barCount; i++)
            {
                int value = Hash(beat * 97 + i * 53);
                float normalized = (value & 1023) / 1023f;
                targetHeights[i] = Mathf.Lerp(wallHeight * 0.25f, wallHeight, normalized);
            }
            if (wall != null && wall.gameObject.activeSelf && beat % conductor.BeatsPerBar == 0)
                FxPool.Instance?.Burst(wall.position, barColor, 8, 5f, 0.12f, 0.3f, 8f, 0.4f);
        }

        void LateUpdate()
        {
            TrySubscribeRunManager();
            TrySubscribeConductor();
            bool visible = runManager != null && (runManager.IsRunning || catching) && target != null;
            if (wall == null) return;
            wall.gameObject.SetActive(visible);
            if (!visible) return;

            float distance = normalDistance;
            if (catching)
            {
                catchElapsed += Time.unscaledDeltaTime;
                distance = Mathf.Lerp(normalDistance, -0.6f, Mathf.SmoothStep(0f, 1f, catchElapsed / Mathf.Max(0.01f, catchSeconds)));
            }
            else if (lunging && conductor != null)
            {
                float t = Mathf.Clamp01(((float)conductor.SongBeat - lungeStartBeat) / Mathf.Max(0.01f, lungeEndBeat - lungeStartBeat));
                distance = Mathf.Lerp(lungeDistance, normalDistance, t);
                if (t >= 1f) lunging = false;
            }
            wall.position = new Vector3(target.position.x - distance, 0f, target.position.z + 0.2f);
            for (int i = 0; i < barCount; i++)
            {
                heights[i] = Mathf.Lerp(heights[i], targetHeights[i], 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
                // Les barres s'étendent vers la gauche à partir du front du mur (x = 0).
                float x = -0.3f - i * 0.85f;
                bars[i].localPosition = new Vector3(x, heights[i] * 0.5f, 0f);
                bars[i].localScale = new Vector3(0.48f, heights[i], 1f);
            }
            halo.localPosition = new Vector3(-0.2f, wallHeight * 0.5f, 0f);
            edge.localPosition = new Vector3(0f, wallHeight * 0.5f, -0.01f);
        }

        static int Hash(int value)
        {
            value ^= value << 13;
            value ^= value >> 17;
            return value ^ (value << 5);
        }
    }
}
