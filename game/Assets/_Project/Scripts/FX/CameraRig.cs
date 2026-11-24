using System;
using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Caméra du runner en pixel art, metteuse en scène. Elle cadre le héros selon un « plan » (zoom + place du héros à
    /// l'écran) que la chart change en cours de niveau (événement `camera`), regarde le terrain devant elle pour
    /// anticiper montées et chutes, et garde toujours le héros dans l'image (envolées des tambours, grandes chutes).
    /// Elle calcule une position exacte, se place sur la grille de pixels et passe la fraction restante à PixelScreen :
    /// le décor glisse au pixel d'écran près. Le héros (calque acteurs) est recalé pour rester immobile à l'écran.
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

        [Header("Plan par défaut")]
        [Tooltip("Largeur de vue (px de jeu) : 320 = ×6 (serré) … 640 = ×3 (très large).")]
        [SerializeField] int defaultZoom = 640;
        [Tooltip("Place du héros de gauche (0) à droite (1) : assez à gauche pour voir arriver les obstacles.")]
        [SerializeField, Range(0.1f, 0.6f)] float defaultFrameX = 0.22f;
        [Tooltip("Hauteur du sol sous le héros, du bas (0) au haut (1) de l'écran.")]
        [SerializeField, Range(0.1f, 0.7f)] float defaultFrameY = 0.3f;

        [Header("Suivi")]
        [Tooltip("Distance (fraction de la largeur de vue) à laquelle on regarde le terrain devant le héros.")]
        [SerializeField] float lookAhead = 0.4f;
        [Tooltip("Part du terrain devant dans le cadrage vertical (0 = on ne regarde que le sol sous le héros).")]
        [SerializeField, Range(0f, 1f)] float aheadWeight = 0.5f;
        [SerializeField] float smoothTime = 0.22f;
        [Tooltip("Lissage quand la caméra suit le rail précalculé de la chart (déjà lisse et en avance).")]
        [SerializeField] float railSmoothTime = 0.06f;
        [Tooltip("Durée (s) du glissement quand le plan change de cadrage.")]
        [SerializeField] float frameBlendTime = 0.45f;
        [Tooltip("Marges (fraction de la hauteur de vue) que le héros ne quitte jamais : bas, haut.")]
        [SerializeField] Vector2 safeMargins = new(0.08f, 0.1f);

        float shakeAmplitude, shakeDuration, shakeTime;
        float bump;
        float freeX, anchorY, camY, camYVelocity;
        float frameX, frameY, frameXTarget, frameYTarget, frameXVel, frameYVel;

        public Transform Target { get => target; set => target = value; }
        /// <summary>Défilement automatique hors partie (accueil, sélection) : unités par seconde.</summary>
        public float AutoScroll { get; set; }
        /// <summary>Hauteur du terrain en x (sans les trous) : sert à anticiper les montées et les chutes.</summary>
        public Func<float, float> HeightProbe { get; set; }
        /// <summary>Rail précalculé : centre de la caméra au beat donné (NaN = pas de rail, on suit le terrain).</summary>
        public Func<double, float> Rail { get; set; }
        /// <summary>Position exacte (avant calage sur la grille et sans secousse).</summary>
        public Vector2 ExactPosition { get; private set; }
        /// <summary>Fraction de pixel (−0,5 … 0,5) rattrapée par le décalage de l'image agrandie.</summary>
        public Vector2 Fraction { get; private set; }
        /// <summary>Décalage (unités) à appliquer aux éléments du calque acteurs pour qu'ils tombent sur la grille de l'écran.</summary>
        public Vector2 ActorShift { get; private set; }

        /// <summary>Place actuelle du héros à l'écran, de gauche (0) à droite (1).</summary>
        public float HeroFrameX => frameX;

        /// <summary>Sol sur lequel le héros s'est posé en dernier : la caméra s'y ancre verticalement.</summary>
        public void SetGroundAnchor(float top) => anchorY = top;

        static Vector2 View => PixelScreen.Instance != null ? PixelScreen.Instance.ViewUnits : new Vector2(8f, 4.5f);

        void Awake()
        {
            Instance = this;
            var cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            frameX = frameXTarget = defaultFrameX;
            frameY = frameYTarget = defaultFrameY;
        }

        void Start() => PixelScreen.Instance?.SetZoom(defaultZoom);

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Nouveau plan : zoom (largeur de vue en px de jeu, 0 = inchangé) et place du héros (0 = inchangé).
        /// Le zoom change net (coupe), le cadrage glisse.
        /// </summary>
        public void SetShot(int zoom, float heroX, float groundY)
        {
            if (zoom > 0) PixelScreen.Instance?.SetZoom(zoom);
            if (heroX > 0f) frameXTarget = Mathf.Clamp(heroX, 0.08f, 0.8f);
            if (groundY > 0f) frameYTarget = Mathf.Clamp(groundY, 0.08f, 0.85f);
        }

        /// <summary>Retour au plan par défaut, immédiat (début de partie). zoom > 0 : largeur de vue imposée par la chart.</summary>
        public void ResetShot(int zoom = 0)
        {
            PixelScreen.Instance?.SetZoom(zoom > 0 ? zoom : defaultZoom);
            frameX = frameXTarget = defaultFrameX;
            frameY = frameYTarget = defaultFrameY;
            frameXVel = frameYVel = 0f;
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
            var view = View;
            anchorY = target != null ? target.position.y : 0f;
            if (target != null) freeX = target.position.x + view.x * (0.5f - frameX);
            float rail = Rail != null && Conductor.Instance != null ? Rail(Conductor.Instance.SongBeat) : float.NaN;
            camY = !float.IsNaN(rail) ? rail : DesiredY(view, target != null ? target.position.x : 0f, target != null);
            camYVelocity = 0f;
        }

        float DesiredY(Vector2 view, float heroX, bool following)
        {
            float reference = anchorY;
            if (following && HeightProbe != null)
                reference = Mathf.Lerp(anchorY, HeightProbe(heroX + view.x * lookAhead), aheadWeight);
            return reference + view.y * (0.5f - frameY);
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            var view = View;
            frameX = Mathf.SmoothDamp(frameX, frameXTarget, ref frameXVel, frameBlendTime, Mathf.Infinity, dt);
            frameY = Mathf.SmoothDamp(frameY, frameYTarget, ref frameYVel, frameBlendTime, Mathf.Infinity, dt);

            bool following = target != null && target.gameObject.activeInHierarchy;
            float desired;
            float smooth = smoothTime;
            if (following)
            {
                freeX = target.position.x + view.x * (0.5f - frameX);
                float rail = Rail != null && Conductor.Instance != null ? Rail(Conductor.Instance.SongBeat) : float.NaN;
                if (!float.IsNaN(rail)) { desired = rail; smooth = railSmoothTime; }
                else desired = DesiredY(view, target.position.x, true);
            }
            else
            {
                freeX += AutoScroll * dt;
                anchorY = 0f;          // hors partie (accueil, menus) : retour à la hauteur de référence
                desired = DesiredY(view, 0f, false);
            }

            camY = Mathf.SmoothDamp(camY, desired, ref camYVelocity, smooth, Mathf.Infinity, dt);
            if (following)
            {
                // le héros ne sort jamais de l'image (envolée d'un tambour, grande chute)
                float ty = target.position.y;
                float minY = ty + 1.4f - view.y * (0.5f - safeMargins.y);   // la tête reste sous la marge du haut
                float maxY = ty + view.y * (0.5f - safeMargins.x);          // les pieds restent au-dessus de la marge du bas
                if (camY < minY) { camY = minY; camYVelocity = Mathf.Max(0f, camYVelocity); }
                if (camY > maxY) { camY = maxY; camYVelocity = Mathf.Min(0f, camYVelocity); }
            }
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
