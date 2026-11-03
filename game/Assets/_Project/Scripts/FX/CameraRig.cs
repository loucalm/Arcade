using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Caméra du runner en pixel art : suit le héros en X sans lissage (calée sur le rythme), reste fixe en Y,
    /// et se place toujours sur la grille de pixels (1/40 u) pour que rien ne scintille. Secousses en pixels entiers.
    /// Le flash d'écran est délégué à ScreenFx (calque d'interface).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig Instance { get; private set; }

        [SerializeField] Transform target;
        [Tooltip("Décalage du héros : il est à 1 u du bord gauche (40 px sur 240), pour voir arriver les obstacles.")]
        [SerializeField] float offsetX = 2.0f;
        [Tooltip("Hauteur de la caméra : le haut du sol est à 20 px du bas de l'écran.")]
        [SerializeField] float cameraY = 1.1875f;

        float shakeAmplitude, shakeDuration, shakeTime;
        float bump;
        float freeX;

        public Transform Target { get => target; set => target = value; }
        /// <summary>Défilement automatique hors partie (accueil) : unités par seconde.</summary>
        public float AutoScroll { get; set; }

        void Awake()
        {
            Instance = this;
            var cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = PixelScreen.Height / (2f * PixelScreen.Ppu);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.allowHDR = false;
            cam.allowMSAA = false;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Shake(float amplitude, float duration)
        {
            if (amplitude < shakeAmplitude * (1f - shakeTime / Mathf.Max(0.0001f, shakeDuration))) return;
            shakeAmplitude = amplitude;
            shakeDuration = duration;
            shakeTime = 0f;
        }

        /// <summary>Petit coup vers le haut (en pixels), à la place du zoom de l'ancienne caméra.</summary>
        public void Punch(float amount) => bump = Mathf.Max(bump, amount);

        public void Flash(Color color, float intensity = 1f) => ScreenFx.Instance?.Flash(color, intensity);

        /// <summary>Recentre immédiatement (respawn, début de partie).</summary>
        public void Snap()
        {
            if (target != null) freeX = target.position.x + offsetX;
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (target != null && target.gameObject.activeInHierarchy) freeX = target.position.x + offsetX;
            else freeX += AutoScroll * dt;

            Vector2 shake = Vector2.zero;
            if (shakeTime < shakeDuration)
            {
                shakeTime += dt;
                float k = shakeAmplitude * (1f - shakeTime / shakeDuration);
                // amplitude en unités, ramenée à des pixels entiers : le tremblement reste « pixel art »
                shake = new Vector2(Mathf.Round((Mathf.PerlinNoise(Time.time * 40f, 0f) - 0.5f) * 2f * k * PixelScreen.Ppu),
                                    Mathf.Round((Mathf.PerlinNoise(0f, Time.time * 40f) - 0.5f) * 2f * k * PixelScreen.Ppu)) / PixelScreen.Ppu;
            }
            bump = Mathf.MoveTowards(bump, 0f, dt * 3f);
            float y = cameraY + Mathf.Round(bump * 4f) / PixelScreen.Ppu;
            transform.position = new Vector3(PixelScreen.Snap(freeX) + shake.x, PixelScreen.Snap(y) + shake.y, -10f);
        }
    }
}
