using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Cuirassé écrasé quand on lui saute dessus : ses images « écrasé » puis « se relève » remplacent un instant sa danse
    /// (BeatFrames), puis il repart. Appelé par RunManager.OnEnemyStomped.
    /// </summary>
    public class StompFrames : MonoBehaviour
    {
        [SerializeField] SpriteRenderer target;
        [SerializeField] Sprite[] frames;
        [Tooltip("Durée (s) d'une image.")]
        [SerializeField] float frameSeconds = 0.08f;

        float startedAt = -99f;

        public void Setup(SpriteRenderer sr, Sprite[] list)
        {
            target = sr;
            frames = list;
        }

        public void Press() => startedAt = Time.time;

        // après BeatFrames (Update) : l'image écrasée l'emporte pendant l'animation
        void LateUpdate()
        {
            if (target == null || frames == null || frames.Length == 0) return;
            int f = (int)((Time.time - startedAt) / frameSeconds);
            if (f >= 0 && f < frames.Length && frames[f] != null) target.sprite = frames[f];
        }
    }
}
