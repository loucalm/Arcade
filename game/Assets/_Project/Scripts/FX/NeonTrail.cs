using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Traînée lumineuse derrière le héros (signature de Rider) : turquoise → magenta → transparent.
    /// Construite au démarrage, aucun asset requis. Utilise le matériau additif du thème.
    /// </summary>
    [RequireComponent(typeof(TrailRenderer))]
    public class NeonTrail : MonoBehaviour
    {
        [SerializeField] Material material;
        [SerializeField] float time = 0.28f;
        [SerializeField] float startWidth = 0.16f;
        [SerializeField] int sortingOrder = 18;
        [SerializeField] Color head = new(0.25f, 0.94f, 1f, 0.6f);
        [SerializeField] Color mid = new(1f, 0.17f, 0.71f, 0.35f);
        [SerializeField] Color tail = new(1f, 0.17f, 0.71f, 0f);

        TrailRenderer trail;

        void Awake()
        {
            trail = GetComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = time;
            trail.minVertexDistance = 0.04f;
            trail.numCapVertices = 4;
            trail.numCornerVertices = 3;
            trail.sortingOrder = sortingOrder;
            trail.alignment = LineAlignment.View;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, startWidth), new Keyframe(0.35f, startWidth * 0.6f), new Keyframe(1f, 0f));
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(head, 0f), new GradientColorKey(mid, 0.4f), new GradientColorKey(tail, 1f) },
                new[] { new GradientAlphaKey(head.a, 0f), new GradientAlphaKey(mid.a, 0.4f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = g;
        }

        /// <summary>Respawn : efface la traînée (sinon elle relie l'ancien point au nouveau).</summary>
        public void Restart()
        {
            if (trail == null) trail = GetComponent<TrailRenderer>();
            trail.Clear();
            trail.emitting = true;
        }

        public void Stop()
        {
            if (trail == null) trail = GetComponent<TrailRenderer>();
            trail.emitting = false;
        }
    }
}
