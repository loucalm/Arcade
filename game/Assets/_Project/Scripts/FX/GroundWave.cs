using RythmeRunner.Rhythm;
using UnityEngine;

namespace RythmeRunner.FX
{
    /// <summary>Fait parcourir un halo au liseré supérieur d'un segment de sol.</summary>
    public class GroundWave : MonoBehaviour
    {
        Transform wave;
        SpriteRenderer referenceRenderer;
        float startX;
        float endX;
        float width;

        public void Configure(SpriteRenderer reference, float x0, float x1, Material additive)
        {
            referenceRenderer = reference;
            startX = x0;
            endX = x1;
            width = Mathf.Max(0.2f, Mathf.Min(1.4f, (x1 - x0) * 0.08f));
            wave = new GameObject("BeatWave").transform;
            wave.SetParent(transform, false);
            var sr = wave.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = reference != null ? reference.sprite : null;
            sr.color = reference != null ? new Color(reference.color.r, reference.color.g, reference.color.b, 0.7f) : Color.white;
            sr.sortingOrder = 4;
            wave.localScale = new Vector3(width, 0.18f, 1f);
            if (additive != null) sr.sharedMaterial = additive;
        }

        void Update()
        {
            if (wave == null || referenceRenderer == null || !referenceRenderer.isVisible || Conductor.Instance == null) return;
            float beat = (float)Conductor.Instance.SongBeat;
            float normalized = Mathf.Repeat(beat * 0.5f, 1f);
            wave.localPosition = new Vector3(Mathf.Lerp(startX, endX, normalized) - transform.localPosition.x, -0.07f, 0f);
        }
    }
}
