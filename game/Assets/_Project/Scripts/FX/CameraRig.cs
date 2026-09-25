using RythmeRunner.Rhythm;
using UnityEngine;

namespace RythmeRunner.FX
{
    /// <summary>
    /// Caméra du runner : suit le joueur en X sans lissage (calée sur le rythme), lisse en Y,
    /// screen shake, « zoom punch » et flash du fond sur les temps forts.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig Instance { get; private set; }

        [SerializeField] Transform target;
        [Tooltip("Position du joueur à l'écran : il est à gauche pour voir arriver les obstacles.")]
        [SerializeField] Vector2 offset = new(6f, 2.4f);
        [SerializeField] float ySmoothing = 4f;
        [SerializeField] float yFollow = 0.35f;
        [SerializeField] float downbeatPunch = 0.08f;
        [SerializeField] Color background = new(0.05f, 0.03f, 0.1f);
        [SerializeField] Color backgroundFlash = new(0.16f, 0.06f, 0.24f);

        Camera cam;
        float baseSize;
        float punch;
        float shakeAmplitude, shakeDuration, shakeTime;
        float flash;
        Color flashColor;
        float smoothY;
        Conductor subscribed;

        public Transform Target { get => target; set => target = value; }

        void Awake()
        {
            Instance = this;
            cam = GetComponent<Camera>();
            baseSize = cam.orthographicSize;
            smoothY = offset.y;
            flashColor = backgroundFlash;
        }

        void OnDestroy()
        {
            if (subscribed != null) subscribed.OnBeat -= OnBeat;
            if (Instance == this) Instance = null;
        }

        public void Shake(float amplitude, float duration)
        {
            if (amplitude < shakeAmplitude * (1f - shakeTime / Mathf.Max(0.0001f, shakeDuration))) return;
            shakeAmplitude = amplitude;
            shakeDuration = duration;
            shakeTime = 0f;
        }

        public void Punch(float amount) => punch = Mathf.Max(punch, amount);

        public void Flash(Color color, float intensity = 1f)
        {
            flashColor = color;
            flash = Mathf.Max(flash, intensity);
        }

        /// <summary>Recentre immédiatement (respawn, début de partie).</summary>
        public void Snap()
        {
            if (target == null) return;
            smoothY = target.position.y * yFollow + offset.y;
        }

        void OnBeat(int beat)
        {
            if (beat % subscribed.BeatsPerBar != 0) return;
            Punch(downbeatPunch);
            Flash(backgroundFlash, 0.6f);
        }

        void LateUpdate()
        {
            if (subscribed == null && Conductor.Instance != null)
            {
                subscribed = Conductor.Instance;
                subscribed.OnBeat += OnBeat;
            }

            float dt = Time.unscaledDeltaTime;
            Vector3 pos = transform.position;
            if (target != null && target.gameObject.activeInHierarchy)
            {
                smoothY = Mathf.Lerp(smoothY, target.position.y * yFollow + offset.y, 1f - Mathf.Exp(-ySmoothing * dt));
                pos.x = target.position.x + offset.x;
                pos.y = smoothY;
            }

            Vector2 shake = Vector2.zero;
            if (shakeTime < shakeDuration)
            {
                shakeTime += dt;
                float k = shakeAmplitude * (1f - shakeTime / shakeDuration);
                shake = new Vector2(Mathf.PerlinNoise(Time.time * 40f, 0f) - 0.5f, Mathf.PerlinNoise(0f, Time.time * 40f) - 0.5f) * 2f * k;
            }
            transform.position = new Vector3(pos.x, pos.y, -10f) + (Vector3)shake;

            punch = Mathf.Lerp(punch, 0f, 1f - Mathf.Exp(-10f * dt));
            cam.orthographicSize = baseSize - punch;

            flash = Mathf.Lerp(flash, 0f, 1f - Mathf.Exp(-6f * dt));
            cam.backgroundColor = Color.Lerp(background, flashColor, flash);
        }
    }
}
