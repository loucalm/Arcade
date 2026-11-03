using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>Alterne les images d'un SpriteRenderer à chaque beat (ennemis qui « dansent »).</summary>
    public class BeatFrames : MonoBehaviour
    {
        [SerializeField] SpriteRenderer target;
        [SerializeField] Sprite[] frames;
        [SerializeField, Min(1)] int everyNBeats = 1;

        Conductor subscribed;

        public void Setup(SpriteRenderer sr, Sprite[] frameList, int everyN = 1)
        {
            target = sr;
            frames = frameList;
            everyNBeats = Mathf.Max(1, everyN);
        }

        void OnEnable() => TrySubscribe();

        void OnDisable()
        {
            if (subscribed != null) subscribed.OnBeat -= Step;
            subscribed = null;
        }

        void Update()
        {
            if (subscribed == null) TrySubscribe();
        }

        void TrySubscribe()
        {
            if (subscribed != null || Conductor.Instance == null) return;
            subscribed = Conductor.Instance;
            subscribed.OnBeat += Step;
        }

        void Step(int beat)
        {
            if (frames == null || frames.Length == 0 || target == null) return;
            target.sprite = frames[(beat / everyNBeats) % frames.Length];
        }
    }
}
