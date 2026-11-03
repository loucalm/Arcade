using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>Anime une Image en boucle (pièces qui tournent autour du logo).</summary>
    [RequireComponent(typeof(Image))]
    public class UiSpriteCycle : MonoBehaviour
    {
        [SerializeField] Sprite[] frames;
        [SerializeField, Min(0.5f)] float fps = 8f;
        [SerializeField] float phase;

        Image img;
        void Awake() => img = GetComponent<Image>();

        public void Setup(Sprite[] list, float framesPerSecond, float startPhase)
        {
            frames = list; fps = framesPerSecond; phase = startPhase;
        }

        void Update()
        {
            if (frames == null || frames.Length == 0) return;
            if (img == null) img = GetComponent<Image>();
            img.sprite = frames[(int)((Time.unscaledTime * fps + phase) % frames.Length)];
        }
    }
}
