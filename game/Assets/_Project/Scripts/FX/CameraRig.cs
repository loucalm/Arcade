using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Caméra du runner en pixel art. Elle calcule une position exacte (suivi du héros en X sans lissage, suivi amorti
    /// en Y avec zone morte), se place sur la grille de pixels et passe la fraction restante à PixelScreen : le décor
    /// glisse au pixel d'écran près. Le héros (calque acteurs) est recalé pour rester immobile à l'écran.
    /// Secousses en pixels entiers ; le flash d'écran est délégué à ScreenFx.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(-50)]   // avant le bandeau et les popups qui lisent ActorShift
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig Instance { get; private set; }

        [SerializeField] Transform target;
        [Tooltip("Visuel du héros (enfant de la cible) recalé sur la grille de pixels de l'écran.")]
        [SerializeField] Transform actorVisual;
        [Tooltip("Décalage du héros : à 56 px du bord gauche (sur 320), pour voir arriver les obstacles.")]
        [SerializeField] float offsetX = 2.6f;
        [Tooltip("Hauteur de la caméra quand le sol d'ancrage est à 0 : le sol est à 48 px du bas de l'écran.")]
        [SerializeField] float baseY = 1.05f;
        [SerializeField] float deadZoneUp = 0.6f;
        [SerializeField] float deadZoneDown = 0.25f;
        [SerializeField] float smoothTime = 0.18f;

        float shakeAmplitude, shakeDuration, shakeTime;
        float bump;
        float freeX, anchorY, camY, camYVelocity;

        public Transform Target { get => target; set => target = value; }
        /// <summary>Défilement automatique hors partie (accueil, sélection) : unités par seconde.</summary>
        public float AutoScroll { get; set; }
        /// <summary>Position exacte (avant calage sur la grille et sans secousse).</summary>
        public Vector2 ExactPosition { get; private set; }
        /// <summary>Fraction de pixel (−0,5 … 0,5) rattrapée par le décalage de l'image agrandie.</summary>
        public Vector2 Fraction { get; private set; }
        /// <summary>Décalage (unités) à appliquer aux éléments du calque acteurs pour qu'ils tombent sur la grille de l'écran.</summary>
        public Vector2 ActorShift { get; private set; }

        /// <summary>Sol le plus bas du niveau : la caméra ne descend pas plus bas (on ne suit pas une chute dans un trou).</summary>
        public float MinAnchor { get; set; }

        /// <summary>Sol sur lequel le héros s'est posé en dernier : la caméra s'y ancre verticalement.</summary>
        public void SetGroundAnchor(float top) => anchorY = top;

        void Awake()
        {
            Instance = this;
            var cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            camY = baseY;
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

        /// <summary>Petit coup vers le haut (en pixels).</summary>
        public void Punch(float amount) => bump = Mathf.Max(bump, amount);

        public void Flash(Color color, float intensity = 1f) => ScreenFx.Instance?.Flash(color, intensity);

        /// <summary>Recentre immédiatement (début de partie).</summary>
        public void Snap()
        {
            if (target != null) freeX = target.position.x + offsetX;
            anchorY = 0f;
            camY = baseY;
            camYVelocity = 0f;
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            bool following = target != null && target.gameObject.activeInHierarchy;
            if (following)
            {
                freeX = target.position.x + offsetX;
                // zone morte : on ne suit une chute ou une montée que si le héros sort franchement de son sol d'ancrage
                float ty = target.position.y;
                if (ty < anchorY - deadZoneDown) anchorY = Mathf.Max(MinAnchor, ty + deadZoneDown);
                else if (ty > anchorY + deadZoneUp + 1.5f) anchorY = ty - deadZoneUp - 1.5f;
            }
            else
            {
                freeX += AutoScroll * dt;
                anchorY = 0f;          // hors partie (accueil, menus) : retour à la hauteur de référence
            }

            camY = Mathf.SmoothDamp(camY, baseY + anchorY, ref camYVelocity, smoothTime, Mathf.Infinity, dt);
            bump = Mathf.MoveTowards(bump, 0f, dt * 3f);
            ExactPosition = new Vector2(freeX, camY + bump * 4f / PixelScreen.Ppu);

            var (sx, fx) = PixelMath.Split(ExactPosition.x, PixelScreen.Ppu);
            var (sy, fy) = PixelMath.Split(ExactPosition.y, PixelScreen.Ppu);

            Vector2 shake = Vector2.zero;
            if (shakeTime < shakeDuration)
            {
                shakeTime += dt;
                float k = shakeAmplitude * (1f - shakeTime / shakeDuration);
                shake = new Vector2(Mathf.Round((Mathf.PerlinNoise(Time.time * 40f, 0f) - 0.5f) * 2f * k * PixelScreen.Ppu),
                                    Mathf.Round((Mathf.PerlinNoise(0f, Time.time * 40f) - 0.5f) * 2f * k * PixelScreen.Ppu)) / PixelScreen.Ppu;
            }
            transform.position = new Vector3(sx + shake.x, sy + shake.y, -10f);
            Fraction = new Vector2(fx, fy);
            PixelScreen.Instance?.SetFraction(Fraction);

            // le calque acteurs ne suit pas la fraction en X : on recale le héros sur la grille de la caméra
            ActorShift = new Vector2(-fx / PixelScreen.Ppu, 0f);
            if (following && actorVisual != null)
            {
                float py = target.position.y;
                actorVisual.localPosition = new Vector3(ActorShift.x, PixelScreen.Snap(py) - py, 0f);
            }
        }
    }
}
