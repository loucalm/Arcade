using UnityEngine;

namespace Kenta.FX
{
    /// <summary>Anime un SpriteRenderer en boucle à vitesse fixe (pièce qui tourne), phase décalée par objet.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteCycle : MonoBehaviour
    {
        [SerializeField] Sprite[] frames;
        [SerializeField, Min(0.5f)] float fps = 8f;

        SpriteRenderer sr;
        float phase;

        public void Setup(Sprite[] list, float framesPerSecond = 8f)
        {
            frames = list;
            fps = framesPerSecond;
            phase = Random.value * 4f;
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (frames != null && frames.Length > 0) sr.sprite = frames[0];
        }

        void Awake() => sr = GetComponent<SpriteRenderer>();

        // optimisation (près de 400 pièces dans le niveau 01) : on n'anime que ce qui est à l'écran
        void Start() => enabled = sr != null && sr.isVisible;
        void OnBecameVisible() => enabled = true;
        void OnBecameInvisible() => enabled = false;

        void Update()
        {
            if (frames == null || frames.Length == 0) return;
            sr.sprite = frames[(int)((Time.unscaledTime * fps + phase) % frames.Length)];
        }
    }
}
