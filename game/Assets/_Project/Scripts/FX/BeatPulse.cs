using RythmeRunner.Rhythm;
using UnityEngine;

namespace RythmeRunner.FX
{
    /// <summary>
    /// Fait « pulser » l'échelle d'un objet (sprite ou UI) sur le beat du Conductor.
    /// Accent plus fort sur le premier temps de chaque mesure.
    /// </summary>
    public class BeatPulse : MonoBehaviour
    {
        [SerializeField] float pulseAmount = 0.12f;
        [SerializeField] float downbeatAmount = 0.22f;
        [Tooltip("Vitesse de retour à l'échelle de base.")]
        [SerializeField] float decay = 10f;
        [Tooltip("1 = chaque beat, 2 = un beat sur deux, 4 = chaque mesure…")]
        [SerializeField, Min(1)] int everyNBeats = 1;

        Vector3 baseScale;
        float current;
        Conductor subscribed;

        void Awake() => baseScale = transform.localScale;

        /// <summary>Réglage depuis le code (objets générés par LevelBuilder).</summary>
        public void Configure(float pulse, float downbeat, int everyN = 1)
        {
            pulseAmount = pulse;
            downbeatAmount = downbeat;
            everyNBeats = Mathf.Max(1, everyN);
        }

        void OnEnable() => TrySubscribe();

        void OnDisable()
        {
            if (subscribed != null) subscribed.OnBeat -= Pulse;
            subscribed = null;
            current = 0f;
            transform.localScale = baseScale;
        }

        void Update()
        {
            if (subscribed == null) TrySubscribe();
            current = Mathf.Lerp(current, 0f, 1f - Mathf.Exp(-decay * Time.unscaledDeltaTime));
            transform.localScale = baseScale * (1f + current);
        }

        void TrySubscribe()
        {
            if (subscribed != null || Conductor.Instance == null) return;
            subscribed = Conductor.Instance;
            subscribed.OnBeat += Pulse;
        }

        void Pulse(int beat)
        {
            if (beat % everyNBeats != 0) return;
            current = beat % subscribed.BeatsPerBar == 0 ? downbeatAmount : pulseAmount;
        }
    }
}
