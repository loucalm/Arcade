using UnityEngine;

namespace Kenta.Player
{
    /// <summary>
    /// Choisit l'image du héros (images de tools/gen-heroes.py). La course est un cycle de 8 images qui avance avec la
    /// distance parcourue : la foulée suit la vitesse, sans à-coup sur le temps. La frappe suit le temps écoulé depuis
    /// l'appui, le saut suit son avancement, la glissade boucle avec la distance ; on se jette au sol et on se relève
    /// en quelques images, et une pose accroupie amortit la réception d'un saut. Sans ces images, on retombe sur une
    /// pose fixe.
    /// </summary>
    public class HeroAnimator : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] SpriteRenderer target;
        [SerializeField] HeroSpriteSet sprites;
        [Tooltip("Distance (unités) d'un cycle de course complet (deux pas, les 8 images). Doit valoir STRIDE de tools/gen-heroes.py.")]
        [SerializeField] float strideUnits = 2.4f;
        [Tooltip("Durée de l'animation de frappe (beats). Plus longue que la fenêtre active pour que le geste se lise.")]
        [SerializeField] float hitAnimBeats = 0.45f;
        [Tooltip("Distance (unités) d'un cycle des images de glissade.")]
        [SerializeField] float slideLoopUnits = 1f;        // 8 u/s : une image toutes les ~60 ms
        [Tooltip("Durée (s) de la pose accroupie à la réception d'un saut.")]
        [SerializeField] float crouchSeconds = 0.07f;
        [Tooltip("Durée (s) des images « se jeter au sol » (slideIn). Doit valoir 3 × SLIDE_IN_STEP de tools/gen-heroes.py.")]
        [SerializeField] float slideInSeconds = 0.12f;
        [Tooltip("Durée (s) des images « se relever » (slideOut). Doit valoir 3 × SLIDE_OUT_STEP de tools/gen-heroes.py.")]
        [SerializeField] float slideOutSeconds = 0.15f;

        public HeroSpriteSet Sprites { get => sprites; set => sprites = value; }

        bool wasSliding;
        float slideChangedAt = -99f;

        /// <summary>Image du cycle de course pour une distance donnée (boucle, jamais négative).</summary>
        public static int RunFrameIndex(float distance, float stride, int frames)
        {
            if (frames <= 0 || stride <= 0f) return 0;
            float t = distance / stride;
            t -= Mathf.Floor(t);
            return Mathf.Min(frames - 1, (int)(t * frames));
        }

        /// <summary>
        /// Image de la frappe, `age` beats après l'appui ; -1 hors de l'animation. Avec 4 images : début de l'arc,
        /// arc complet (l'image gelée, la plus longue), fin de l'arc, bras tendu.
        /// </summary>
        public static int HitFrameIndex(double age, float duration, int frames)
        {
            if (frames <= 0 || duration <= 0f || age < 0 || age >= duration) return -1;
            float t = (float)(age / duration);
            if (frames < 4) return Mathf.Min(frames - 1, (int)(t * frames));
            if (t < 0.12f) return 0;
            if (t < 0.55f) return 1;
            if (t < 0.78f) return 2;
            return 3;
        }

        /// <summary>Image d'une transition (se jeter au sol, se relever), `elapsed` s après son début ; -1 si elle est finie.</summary>
        public static int TransitionFrameIndex(float elapsed, float duration, int frames)
        {
            if (frames <= 0 || duration <= 0f || elapsed < 0f || elapsed >= duration) return -1;
            return Mathf.Min(frames - 1, (int)(elapsed / duration * frames));
        }

        /// <summary>
        /// Image du saut pour son avancement (0 → 1) : les images sont réparties à égalité sur la durée du saut
        /// (impulsion, genoux remontés, vrille, ouverture, réception). -1 hors d'un saut.
        /// </summary>
        public static int JumpFrameIndex(float progress, int frames)
        {
            if (frames <= 0 || progress < 0f) return -1;
            return Mathf.Min(frames - 1, (int)(progress * frames));
        }

        void LateUpdate()
        {
            if (player == null || target == null || sprites == null) return;
            if (player.IsSliding != wasSliding)
            {
                wasSliding = player.IsSliding;
                slideChangedAt = Time.time;
            }
            target.sprite = Pick();
        }

        Sprite Pick()
        {
            if (player.IsDead) return sprites.death;

            int hit = HitFrameIndex(player.HitAgeBeats, hitAnimBeats, Count(sprites.hitFrames));
            if (hit >= 0) return sprites.hitFrames[hit];
            if (player.IsPunching) return sprites.hit;

            float sinceSlideChange = Time.time - slideChangedAt;
            if (player.IsSliding)
            {
                int turn = TransitionFrameIndex(sinceSlideChange, slideInSeconds, Count(sprites.slideIn));
                if (turn >= 0) return sprites.slideIn[turn];
                int n = Count(sprites.slideLoop);
                return n > 0 ? sprites.slideLoop[RunFrameIndex(player.transform.position.x, slideLoopUnits, n)] : sprites.slide;
            }
            if (player.IsClimbing && sprites.wall != null) return sprites.wall;

            if (!player.IsGrounded)
            {
                int n = Count(sprites.jumpFrames);
                int k = JumpFrameIndex(player.JumpProgress, n);
                if (k >= 0) return sprites.jumpFrames[k];
                return n > 0 ? sprites.jumpFrames[n - 2] : sprites.jump;   // chute depuis un bord : bras ouverts, jambes vers le sol
            }

            int getUp = TransitionFrameIndex(sinceSlideChange, slideOutSeconds, Count(sprites.slideOut));
            if (getUp >= 0) return sprites.slideOut[getUp];
            if (player.SinceLanded < crouchSeconds)
                return sprites.crouch != null ? sprites.crouch : sprites.idle;
            var cycle = sprites.runCycle;
            if (cycle == null || cycle.Length == 0) return sprites.run;
            return cycle[RunFrameIndex(player.transform.position.x, strideUnits, cycle.Length)];
        }

        static int Count(Sprite[] frames) => frames == null ? 0 : frames.Length;
    }
}
