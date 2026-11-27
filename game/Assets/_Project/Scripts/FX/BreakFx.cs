using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Casse d'un ennemi frappé (tools/gemini-ennemis/clean.py) : là où il était, ses images « touché », « fêlé » et « en
    /// morceaux » défilent, puis plus rien (les pièces et les étincelles viennent de FxPool). Un petit stock de sprites
    /// créé une fois au démarrage (pas d'Instantiate pendant la partie, pas d'allocation par image).
    /// </summary>
    public class BreakFx : MonoBehaviour
    {
        public static BreakFx Instance { get; private set; }

        [SerializeField, Min(1)] int poolSize = 6;
        [Tooltip("Durée (s) d'une image de la casse.")]
        [SerializeField] float frameSeconds = 0.07f;

        SpriteRenderer[] renderers;
        Sprite[][] frames;
        float[] startedAt;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            renderers = new SpriteRenderer[poolSize];
            frames = new Sprite[poolSize][];
            startedAt = new float[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject("BreakFx");
                go.transform.SetParent(transform, false);
                renderers[i] = go.AddComponent<SpriteRenderer>();
                go.SetActive(false);
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Joue `list` à la place de `source` (même position, même sens, même couche).</summary>
        public void Play(SpriteRenderer source, Sprite[] list)
        {
            if (source == null || list == null || list.Length == 0 || list[0] == null) return;
            int k = 0;
            for (int i = 0; i < renderers.Length; i++)
                if (!renderers[i].gameObject.activeSelf) { k = i; break; }
                else if (startedAt[i] < startedAt[k]) k = i;            // tout est pris : on reprend la plus ancienne
            var r = renderers[k];
            r.transform.position = source.transform.position;
            r.flipX = source.flipX;
            r.flipY = source.flipY;
            r.sortingLayerID = source.sortingLayerID;
            r.sortingOrder = source.sortingOrder + 1;
            r.gameObject.layer = source.gameObject.layer;
            r.color = Color.white;
            r.sprite = list[0];
            frames[k] = list;
            startedAt[k] = Time.time;
            r.gameObject.SetActive(true);
        }

        void Update()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (!r.gameObject.activeSelf) continue;
                int f = (int)((Time.time - startedAt[i]) / frameSeconds);
                if (f >= frames[i].Length || frames[i][f] == null) { r.gameObject.SetActive(false); continue; }
                r.sprite = frames[i][f];
            }
        }
    }
}
