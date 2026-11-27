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
        [Tooltip("Durée (s) de la pose accroupie à la réception d'un saut.")]
        [SerializeField] float crouchSeconds = 0.07f;

        public HeroSpriteSet Sprites { get => sprites; set => sprites = value; }

        [Header("Pentes et chutes")]
        [Tooltip("Pente (dy/dx) à partir de laquelle on passe au cycle de montée ou de descente.")]
        [SerializeField] float slopeThreshold = 0.3f;
        [Tooltip("Distance (u, le long de la pente) d'un cycle de montée. Doit valoir CLIMB_STRIDE de tools/gen-heroes.py.")]
        [SerializeField] float climbStrideUnits = 2f;
        [Tooltip("Distance (u, le long de la pente) d'un cycle de descente. Doit valoir DESCEND_STRIDE de tools/gen-heroes.py.")]
        [SerializeField] float descendStrideUnits = 2.2f;
        [Tooltip("Durée (s) du pas dans le vide quand on quitte un bord sans sauter.")]
        [SerializeField] float fallStepSeconds = 0.1f;
        [Tooltip("Durée (s) d'une image de la boucle de chute.")]
        [SerializeField] float fallLoopSeconds = 0.12f;

        [Header("Animations Gemini (si le jeu d'images les a)")]
        [Tooltip("Durée (s) d'une image de la victoire, de la liane, du touché et de la mort.")]
        [SerializeField] float victoryFrameSeconds = 0.12f, vineFrameSeconds = 0.14f, hurtFrameSeconds = 0.05f, deathFrameSeconds = 0.09f;
        [Tooltip("Durée (s) de l'atterrissage (toutes ses images).")]
        [SerializeField] float landSeconds = 0.12f;
        [Tooltip("Part du vol d'un tambour jouée par l'envol (le reste : le saut normal, vrille comprise).")]
        [SerializeField] float drumLaunchPart = 0.45f;
        [Tooltip("Distance (u) d'un cycle de course sur le mur.")]
        [SerializeField] float wallStrideUnits = 2f;

        // lus sur la position du héros (rien n'est changé à sa physique)
        Vector3 lastPos;
        bool hasLastPos, wasGrounded;
        float slope, pathDistance, airborneAt = -99f, deadAt = -1f, doneAt = -1f;

        /// <summary>
        /// Image d'une chute depuis un bord, `elapsed` s après avoir quitté le sol : image 0 (le pas dans le vide) pendant
        /// `step` s, puis les autres en boucle, `loop` s chacune. -1 sans images.
        /// </summary>
        public static int FallFrameIndex(float elapsed, float step, float loop, int frames)
        {
            if (frames <= 0) return -1;
            if (frames == 1 || elapsed < step) return 0;
            int k = loop > 0f ? (int)((elapsed - step) / loop) : 0;
            return 1 + k % (frames - 1);
        }

        /// <summary>Image d'une séquence jouée une fois, `elapsed` s après son début : la dernière image est tenue.</summary>
        public static int OnceFrameIndex(float elapsed, float frameSeconds, int frames)
        {
            if (frames <= 0) return -1;
            if (elapsed <= 0f || frameSeconds <= 0f) return 0;
            return Mathf.Min(frames - 1, (int)(elapsed / frameSeconds));
        }

        /// <summary>Image du cycle de course pour une distance donnée (boucle, jamais négative).</summary>
        public static int RunFrameIndex(float distance, float stride, int frames)
        {
            if (frames <= 0 || stride <= 0f) return 0;
            float t = distance / stride;
            t -= Mathf.Floor(t);
            return Mathf.Min(frames - 1, (int)(t * frames));
        }

        // coup de katana en 6 images (tools/gen-heroes.py, HIT_EDGES) : la coupe pleine dure le plus longtemps
        static readonly float[] KatanaEdges = { 0.08f, 0.30f, 0.55f, 0.72f, 0.86f };

        /// <summary>
        /// Image de la frappe, `age` beats après l'appui ; -1 hors de l'animation. Avec 6 images (katana) : dégainé,
        /// la coupe part, coupe pleine (la plus longue), la traînée s'efface, suivi, retour en garde. Avec 4 images
        /// (ancienne frappe) : début de l'arc, arc complet, fin de l'arc, bras tendu.
        /// </summary>
        public static int HitFrameIndex(double age, float duration, int frames)
        {
            if (frames <= 0 || duration <= 0f || age < 0 || age >= duration) return -1;
            float t = (float)(age / duration);
            if (frames >= 6)
            {
                for (int i = 0; i < KatanaEdges.Length; i++)
                    if (t < KatanaEdges[i]) return i;
                return KatanaEdges.Length;
            }
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
            Track();
            target.sprite = Pick();
        }

        /// <summary>Pente du sol et distance parcourue le long de lui, à partir du déplacement du héros.</summary>
        void Track()
        {
            Vector3 p = player.transform.position;
            bool grounded = player.IsGrounded;
            if (!hasLastPos || Mathf.Abs(p.x - lastPos.x) > 4f)      // départ ou réapparition : on repart de là
            {
                hasLastPos = true;
                lastPos = p;
                pathDistance = p.x;
                slope = 0f;
                wasGrounded = grounded;
                return;
            }
            float dx = p.x - lastPos.x, dy = p.y - lastPos.y;
            if (player.IsInverted) dy = -dy;                         // au plafond, « monter » se lit à l'envers
            if (grounded && dx > 1e-4f)
            {
                float s = dy / dx;
                slope = Mathf.Lerp(slope, s, 0.5f);                  // lissé : pas de bascule d'une image à l'autre
                pathDistance += Mathf.Sqrt(dx * dx + dy * dy);
            }
            else
            {
                if (!grounded) slope = 0f;
                pathDistance += Mathf.Max(0f, dx);
            }
            if (wasGrounded && !grounded) airborneAt = Time.time;
            wasGrounded = grounded;
            lastPos = p;
        }

        Sprite Pick()
        {
            if (player.IsDead)
            {
                // touché (recul, grimace), puis la mort (projeté, tombe, rebondit, couché : la dernière image est tenue)
                if (deadAt < 0f) deadAt = Time.time;
                float t = Time.time - deadAt;
                int nh = Count(sprites.hurtFrames), nd = Count(sprites.deathFrames);
                if (nh > 0 && t < nh * hurtFrameSeconds) return sprites.hurtFrames[OnceFrameIndex(t, hurtFrameSeconds, nh)];
                if (nd > 0) return sprites.deathFrames[OnceFrameIndex(t - nh * hurtFrameSeconds, deathFrameSeconds, nd)];
                return sprites.death;
            }
            deadAt = -1f;
            var run = Kenta.Core.RunManager.Instance;
            if (run != null && run.Completed && !run.IsRunning)          // niveau fini : il s'arrête et savoure sa victoire
            {
                if (doneAt < 0f) doneAt = Time.time;
                int nv = Count(sprites.victoryFrames);
                if (nv > 0) return sprites.victoryFrames[OnceFrameIndex(Time.time - doneAt, victoryFrameSeconds, nv)];
                return sprites.idle != null ? sprites.idle : sprites.run;
            }
            doneAt = -1f;

            // coup de katana : en l'air, le coup en l'air s'il existe
            var hitSet = !player.IsGrounded && Count(sprites.airHitFrames) > 0 ? sprites.airHitFrames : sprites.hitFrames;
            int hit = HitFrameIndex(player.HitAgeBeats, hitAnimBeats, Count(hitSet));
            if (hit >= 0) return hitSet[hit];
            if (player.IsPunching) return sprites.hit;
            if (player.IsSwinging)
            {
                int nv = Count(sprites.vineFrames);
                if (nv > 0) return sprites.vineFrames[(int)(Time.time / vineFrameSeconds) % nv];
                if (sprites.vine != null) return sprites.vine;
            }

            if (!player.IsGrounded)
            {
                // envol d'un tambour : accroupi, détente, étoile, puis la vrille du saut
                int nb = Count(sprites.drumFrames);
                if (player.IsBouncing && nb > 0 && player.JumpProgress >= 0f && player.JumpProgress < drumLaunchPart)
                    return sprites.drumFrames[Mathf.Min(nb - 1, (int)(player.JumpProgress / drumLaunchPart * nb))];
                int n = Count(sprites.jumpFrames);
                int k = JumpFrameIndex(player.JumpProgress, n);
                if (k >= 0) return sprites.jumpFrames[k];
                // chute depuis un bord (sans saut) : un pas dans le vide, puis la chute bras levés, en boucle
                int f = FallFrameIndex(Time.time - airborneAt, fallStepSeconds, fallLoopSeconds, Count(sprites.fallFrames));
                if (f >= 0) return sprites.fallFrames[f];
                return n > 0 ? sprites.jumpFrames[n - 2] : sprites.jump;
            }

            // course sur le mur (rampe raide) : sa propre boucle, si elle existe
            if (player.IsClimbing && Count(sprites.wallCycle) > 0)
                return sprites.wallCycle[RunFrameIndex(pathDistance, wallStrideUnits, sprites.wallCycle.Length)];
            // pentes : on grimpe penché en avant, on descend penché en arrière ; le cycle suit la distance le long du sol
            if (slope > slopeThreshold && Count(sprites.climbCycle) > 0)
                return sprites.climbCycle[RunFrameIndex(pathDistance, climbStrideUnits, sprites.climbCycle.Length)];
            if (player.IsClimbing && sprites.wall != null) return sprites.wall;
            if (slope < -slopeThreshold && Count(sprites.descendCycle) > 0)
                return sprites.descendCycle[RunFrameIndex(pathDistance, descendStrideUnits, sprites.descendCycle.Length)];

            int nl = Count(sprites.landFrames);
            if (nl > 0 && player.SinceLanded < landSeconds)
                return sprites.landFrames[TransitionFrameIndex(player.SinceLanded, landSeconds, nl) is int li && li >= 0 ? li : nl - 1];
            if (player.SinceLanded < crouchSeconds)
                return sprites.crouch != null ? sprites.crouch : sprites.idle;
            var cycle = sprites.runCycle;
            if (cycle == null || cycle.Length == 0) return sprites.run;
            return cycle[RunFrameIndex(pathDistance, strideUnits, cycle.Length)];
        }

        static int Count(Sprite[] frames) => frames == null ? 0 : frames.Length;
    }
}
