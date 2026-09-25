using RythmeRunner.Rhythm;
using UnityEngine;

namespace RythmeRunner.FX
{
    /// <summary>Anime un élément directement depuis la position rythmique du morceau.</summary>
    public class BeatDance : MonoBehaviour
    {
        [SerializeField] float bounce = 0.08f;
        [SerializeField] float squash = 0.08f;
        [SerializeField] float rotation = 0f;
        [SerializeField] float phase = 0f;
        SpriteRenderer referenceRenderer;
        Vector3 basePosition;
        Vector3 baseScale;
        Quaternion baseRotation;

        public void Configure(SpriteRenderer reference, float bounceAmount, float squashAmount, float rotationAmount, float phaseOffset = 0f)
        {
            referenceRenderer = reference;
            bounce = bounceAmount;
            squash = squashAmount;
            rotation = rotationAmount;
            phase = phaseOffset;
            basePosition = transform.localPosition;
            baseScale = transform.localScale;
            baseRotation = transform.localRotation;
        }

        void Awake()
        {
            basePosition = transform.localPosition;
            baseScale = transform.localScale;
            baseRotation = transform.localRotation;
        }

        void Update()
        {
            if (Conductor.Instance == null || referenceRenderer == null || !referenceRenderer.isVisible) return;
            float beat = (float)Conductor.Instance.SongBeat + phase;
            float pulse = Mathf.Sin(beat * Mathf.PI * 2f);
            float lift = Mathf.Max(0f, pulse);
            float stretch = lift * squash;
            transform.localPosition = basePosition + Vector3.up * (lift * bounce);
            transform.localScale = new Vector3(baseScale.x * (1f - stretch), baseScale.y * (1f + stretch), baseScale.z);
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(beat * Mathf.PI) * rotation);
        }
    }
}
