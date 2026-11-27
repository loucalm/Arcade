using Kenta.Core;
using Kenta.FX;
using Kenta.Level;
using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.Player
{
    /// <summary>
    /// Personnage du runner. X vient TOUJOURS du Conductor (x = BeatToX(SongBeat)) : le joueur ne ralentit
    /// jamais et reste synchro avec la musique. Y = physique cinématique maison, réglée en beats :
    /// un saut dure jumpAirBeats beats, quel que soit le BPM. Collisions AABB à la main (ADR-009).
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Visuel (hiérarchie : Visual (squash) > Spinner > Sprite ; l'image vient de HeroAnimator)")]
        [SerializeField] Transform visual;
        [SerializeField] Transform spinner;
        [Tooltip("Optionnel : l'arc de frappe est déjà dessiné dans la pose « frappe » des héros.")]
        [SerializeField] SpriteRenderer slash;
        [SerializeField] BandanaTrail trail;
        [Tooltip("Couleur des gerbes de particules (apparition, mort).")]
        [SerializeField] Color burstColor = new(1f, 0.42f, 0.19f);
        [Tooltip("Vrille pendant le saut. Désactivée : la pose de saut contient déjà l'arc de la vrille.")]
        [SerializeField] bool spinOnJump;
        [Tooltip("Étirement (écrasement) aux moments forts : léger, car il épaissit les traits.")]
        [SerializeField, Range(0f, 1f)] float squashAmount = 0f;

        [Header("Corps")]
        [SerializeField] float width = 0.5f;
        [SerializeField] float standHeight = 1.0f;
        [Tooltip("Rayon de ramassage des pièces (u). Le même dans le simulateur (tools/levelgen/engine.py, PICKUP).")]
        [SerializeField] float pickupRadius = 1f;
        [Tooltip("Collisions balayées entre deux frames (à-coup du navigateur) jusqu'à cette distance max.")]
        [SerializeField] float maxSweep = 3f;

        [Header("Saut (hauteur fixe, durée en beats)")]
        [SerializeField] float jumpHeight = 1.5f;
        [SerializeField] float jumpAirBeats = 1f;
        [SerializeField] float coyoteBeats = 0.12f;
        [Tooltip("Appui en l'air juste avant d'atterrir : le saut part à l'atterrissage (0,35 beat = 140 ms). " +
                 "Nouveau nom (avant : jumpBufferBeats = 0,2) pour que la valeur de la scène ne l'écrase pas.")]
        [SerializeField] float jumpBufferWindowBeats = 0.35f;
        [Tooltip("Pieds à moins de cette hauteur sous le haut d'une marche ou d'un mur : on passe dessus au lieu de mourir.")]
        [SerializeField] float ledgeAssist = 0.4f;

        [Header("Frappe")]
        [SerializeField] float hitReach = 1.9f;
        [SerializeField] float hitHeight = 2.1f;
        [SerializeField] float hitActiveBeats = 0.3f;
        [Tooltip("En l'air, la zone de frappe s'étire : de airHitBelow sous les pieds à airHitAbove au-dessus " +
                 "(on frappe ce qu'on croise en montant ou en tombant, même vite).")]
        [SerializeField] float airHitBelow = 3.5f;
        [SerializeField] float airHitAbove = 3.5f;

        public bool IsDead => dead;
        /// <summary>Gravité inversée : il court au plafond, la tête en bas.</summary>
        public bool IsInverted => inverted;
        /// <summary>Plan du héros : 0 = premier plan, 1 = arrière-plan.</summary>
        public int Plane => plane;
        /// <summary>Pendu à une liane (balancier automatique).</summary>
        public bool IsSwinging => vine != null;
        public bool IsGrounded => grounded;
        /// <summary>Course sur une rampe raide (pose « mur »).</summary>
        public bool IsClimbing { get; private set; }
        /// <summary>En vol après un tambour-tremplin.</summary>
        public bool IsBouncing { get; private set; }
        public float VelocityY => vy;
        /// <summary>Le coup de poing est en cours (pose « hit » et croissant visibles).</summary>
        public bool IsPunching => slashTimer > 0f;
        /// <summary>Beat de l'appui qui a déclenché le dernier saut (buffer compris) : sert au jugement.</summary>
        public double LastJumpPressBeat { get; private set; } = -99;
        /// <summary>Beat du dernier appui sur « frappe ».</summary>
        public double LastHitPressBeat { get; private set; } = -99;
        /// <summary>Secondes depuis le dernier atterrissage.</summary>
        public float SinceLanded => Time.time - landedAt;
        /// <summary>Avancement du saut ou du rebond en cours (0 au décollage, 1 à l'atterrissage prévu) ; -1 au sol
        /// ou en simple chute (bord de plateforme). Sert à caler la vrille sur la durée du saut.</summary>
        public float JumpProgress => grounded || airBeats <= 0f ? -1f : Mathf.Clamp01((float)((nowBeat - airStartBeat) / airBeats));
        /// <summary>Beats écoulés depuis le dernier appui sur « frappe » (négatif ou très grand s'il n'y en a pas eu).</summary>
        public double HitAgeBeats => nowBeat - LastHitPressBeat;

        LevelBuilder level;
        RunManager run;
        IPlayerInput input;

        float y, vy;
        float groundTop;                      // dessus du dernier sol touché
        bool grounded, dead;
        double lastGroundedBeat, jumpBufferBeat = -99, hitUntilBeat = -99, jumpStartBeat;
        bool spinning;
        int obstacleCursor, lumCursor;
        float prevX, prevCenterY;
        Vector3 squash = Vector3.one;
        float slashTimer, slashDuration = 0.1f;
        float landedAt = -99f;
        double nowBeat, airStartBeat;
        float airBeats;                       // durée du saut ou du rebond en cours (0 : pas de saut)
        int bounceCursor;
        LevelObject pendingBounce;
        // Gravité inversée : y, vy, groundTop sont dans le repère retourné (y' = −y monde) et le sol est le plafond en miroir.
        bool inverted;
        int flipCursor;
        const float FlipLandSeconds = 1f / 30f;     // portail de gravité : atterrissage avancé de 2 images au plus
        // Plans : le héros ne touche que ce qui est sur son plan ; l'image change de calque au sommet du saut.
        int plane, visualPlane;
        // Liane en cours (null : aucune)
        LevelObject vine;
        double vineGrabBeat;                  // temps où l'on a attrapé la liane en cours
        int vineCursor;
        double lastJumpDownBeat = -99;
        // Ennemi cuirassé écrasé : on attend l'appui du grand rebond (sinon petit rebond)
        LevelObject pendingStomp;
        int orbCursor;
        double lastOrbBeat = -99;             // temps de la dernière note de saut prise : un appui ne sert qu'à une note
        float frameStartY, curSpb = 0.4f, curGravity = 75f;
        /// <summary>Fenêtre de l'appui d'une note de saut ou d'un rebond sur un cuirassé, autour de son temps (beats).</summary>
        const float PressEarlyBeats = 0.35f, PressLateBeats = 0.35f;
        /// <summary>Portée verticale d'une note de saut : on la prend même en arrivant vite, en fin de saut.</summary>
        const float OrbReach = 3f;
        /// <summary>Chute ralentie (u/s) près d'une note de saut, pendant sa fenêtre d'appui, une fois à sa hauteur
        /// (centre du héros à moins de OrbHoldAbove au-dessus d'elle). Avant, elle freinait dès l'ouverture de la fenêtre :
        /// à 280 BPM (niveau 9), le héros, qui tombe à plus de 20 u/s, restait figé 2 à 3 u au-dessus de la note et
        /// rebondissait dans le vide.</summary>
        const float OrbHover = 3f, OrbHoldAbove = 0.1f;
        /// <summary>Contact d'une note de saut : centre du héros à moins de 0,5 u du sien. Un appui en avance part là, ou
        /// sur le temps de la note.</summary>
        const float OrbTouch = 0.5f;
        /// <summary>Note de saut : un appui jusqu'à 0,3 temps avant sa fenêtre est gardé et part à l'ouverture (avant, il
        /// était ignoré : on appuyait un peu tôt, rien ne se passait, on tombait). Même règle dans levelgen/engine.py.</summary>
        const float OrbMemoryBeats = 0.3f;

        LevelTerrain Terrain => plane == 1 ? level.Back : inverted ? level.CeilingMirror : level.Ground;
        /// <summary>Hauteur des pieds dans le monde (y est dans le repère de la gravité).</summary>
        float WorldY => inverted ? -y : y;

        public void Setup(LevelBuilder level, RunManager run, IPlayerInput input)
        {
            this.level = level;
            this.run = run;
            this.input = input;
        }

        public void SetInput(IPlayerInput newInput, double beat)
        {
            input = newInput;
            input.ResetAt(beat);
            lastStepBeat = beat;
        }

        public void Spawn(double beat)
        {
            dead = false;
            vy = 0f;
            grounded = true;
            spinning = false;
            jumpBufferBeat = hitUntilBeat = -99;
            LastJumpPressBeat = LastHitPressBeat = -99;
            lastGroundedBeat = beat;
            nowBeat = lastStepBeat = beat;
            airBeats = 0f;
            float x = level.BeatToX(beat);
            y = groundTop = level.HeightAt(x);
            transform.position = new Vector3(x, y, 0f);
            CameraRig.Instance?.SetGroundAnchor(groundTop);
            prevX = x;
            prevCenterY = y + standHeight * 0.5f;
            inverted = false;
            plane = 0;
            SetVisualPlane(0);
            vine = null;
            vineCursor = LevelBuilder.FirstIndexAfter(level.Vines, x);
            orbCursor = LevelBuilder.FirstIndexAfter(level.Orbs, x - 2f);
            pendingStomp = null;
            flipCursor = 0;
            while (flipCursor < level.GravityFlips.Count && level.GravityFlips[flipCursor].x <= beat) flipCursor++;
            lastJumpDownBeat = -99;
            lastOrbBeat = -99;
            obstacleCursor = LevelBuilder.FirstIndexAfter(level.Obstacles, x - 4f);
            laserCursor = 0;
            while (laserCursor < level.Lasers.Count && level.Lasers[laserCursor].beat < beat - 1.0) laserCursor++;
            lumCursor = LevelBuilder.FirstIndexAfter(level.Lums, x - 4f);
            bounceCursor = LevelBuilder.FirstIndexAfter(level.Bounces, x - 4f);
            IsClimbing = IsBouncing = false;
            pendingBounce = null;
            input?.ResetAt(beat);
            gameObject.SetActive(true);
            visual.gameObject.SetActive(true);
            squash = Squash(0.75f, 1.3f);
            spinner.localRotation = Quaternion.identity;
            if (slash != null) slash.enabled = false;
            slashTimer = 0f;
            landedAt = -99f;
            if (trail != null) trail.Clear();
            FxPool.Instance?.Burst(new Vector2(x, 0.5f), burstColor, 14, 5f, 0.18f, 0.45f, 4f);
        }

        public void Die()
        {
            dead = true;                       // HeroAnimator montre la pose « mort »
            if (slash != null) slash.enabled = false;
            slashTimer = 0f;
            if (trail != null) trail.Clear();
            var p = (Vector2)transform.position + new Vector2(0f, 0.45f);
            FxPool.Instance?.Burst(p, burstColor, 40, 11f, 0.28f, 0.9f, 10f, 0.6f);
            FxPool.Instance?.Burst(p, new Color(1f, 0.17f, 0.71f), 24, 8f, 0.22f, 0.7f, 8f, 0.4f);
            FxPool.Instance?.Burst(p, Color.white, 16, 7f, 0.18f, 0.5f, 6f);
        }

        // Pas de physique d'au plus 1/60 s : sur la borne, une image longue (lag, ramasse-miettes du navigateur) faisait
        // avancer le héros de 0,5 à 1 u d'un coup. Il perdait alors le sol sur les rampes raides (mort « dans l'escalier »)
        // et passait d'« en dessous » à « dans » un cuirassé sans jamais être au-dessus (mort au lieu du rebond).
        // Le simulateur du générateur (tools/levelgen/engine.py) valide le niveau à 60 i/s : on rejoue ce pas-là.
        const float MaxStepSeconds = 1f / 60f;
        const int MaxSubSteps = 64;                       // jusqu'à ~1 s d'à-coup rattrapé en pas de 1/60 s
        double lastStepBeat = double.NaN;

        void Update()
        {
            if (dead || level == null || run == null || !run.IsRunning) return;
            var conductor = Conductor.Instance;
            if (conductor == null) return;

            double beat = conductor.SongBeat;
            float spb = (float)conductor.SecondsPerBeat;
            float dt = Time.deltaTime;
            // début de l'image : le beat du dernier pas (sinon, un seul pas)
            double from = !double.IsNaN(lastStepBeat) && lastStepBeat < beat && beat - lastStepBeat < 4.0 ? lastStepBeat : beat;
            int n = from < beat ? Mathf.Clamp(Mathf.CeilToInt((float)((beat - from) * spb) / MaxStepSeconds), 1, MaxSubSteps) : 1;
            // manette : un appui par image, appliqué au dernier pas (comme avant) ; autoplay : lu à chaque pas (seuils en beats)
            bool auto = input is AutoPlayerInput;
            var frameCmd = auto ? default : input.Sample(beat);
            for (int k = 1; k <= n; k++)
            {
                double b = n == 1 ? beat : from + (beat - from) * k / n;
                var cmd = auto ? input.Sample(b) : (k == n ? frameCmd : default);
                float stepDt = n == 1 ? dt : (float)((beat - from) * spb) / n;
                lastStepBeat = b;
                if (!Step(b, spb, stepDt, cmd)) return;
            }
            UpdateVisual(beat, dt);
        }

        /// <summary>Un pas de physique au beat donné. Faux si la partie s'arrête (mort).</summary>
        bool Step(double beat, float spb, float dt, PlayerCommands cmd)
        {
            nowBeat = beat;
            float x = level.BeatToX(beat);

            // Physique réglée en beats : sommet à jumpHeight, retour au sol après jumpAirBeats.
            float airTime = jumpAirBeats * spb;
            float gravity = 8f * jumpHeight / (airTime * airTime);
            float jumpVelocity = 4f * jumpHeight / airTime;
            float half = width * 0.5f;
            curSpb = spb;
            curGravity = gravity;

            // portails de gravité : le haut devient le bas (même position dans le monde, vitesse retournée)
            var flips = level.GravityFlips;
            while (flipCursor < flips.Count && flips[flipCursor].x <= beat)
            {
                LandBeforeFlip(x, half);
                SetGravity(flips[flipCursor].y > 0.5f, x);
                flipCursor++;
            }
            var ground = Terrain;

            // au sol : les pieds suivent le terrain (rampes) ; plus rien dessous (trou, bord) : on tombe
            float yBefore = y;
            if (grounded)
            {
                float support = ground.SupportTop(x, half, y);
                if (float.IsNegativeInfinity(support)) { grounded = false; vy = 0f; airBeats = 0f; }
                else if (support != y)
                {
                    y = groundTop = support;
                    CameraRig.Instance?.SetGroundAnchor(WorldY);
                }
            }
            float dx = x - prevX;
            IsClimbing = grounded && dx > 1e-4f && (y - yBefore) / dx > 0.7f;
            if (grounded) lastGroundedBeat = beat;

            // Saut (avec buffer et coyote time)
            if (cmd.jumpDown)
            {
                lastJumpDownBeat = beat;
                jumpBufferBeat = vine != null ? -99 : beat;     // pendu à une liane, l'appui sert seulement au lâcher
                // appui en avance sur une liane : il est gardé (lâcher à l'ouverture de la fenêtre) ; la main se resserre
                if (vine != null && beat < vine.beat - VineEarlyBeats) squash = Squash(0.85f, 1.15f);
                // appui juste après le décollage d'un tambour : il compte pour le rebond
                if (pendingBounce != null && beat - pendingBounce.beat <= 0.5) run.OnObstacleCleared(pendingBounce, beat);
                pendingBounce = null;
            }
            if (pendingBounce != null && beat - pendingBounce.beat > 0.5) pendingBounce = null;
            bool coyote = !grounded && vy <= 0f && y > groundTop - 0.2f && beat - lastGroundedBeat <= coyoteBeats;
            if (beat - jumpBufferBeat <= jumpBufferWindowBeats && (grounded || coyote)) Jump(jumpVelocity, beat, x);

            // Frappe
            if (cmd.hitDown)
            {
                LastHitPressBeat = beat;
                hitUntilBeat = beat + hitActiveBeats;
                slashDuration = slashTimer = hitActiveBeats * spb;
                ActionSfx.Instance?.Hit();
                squash = Squash(1.2f, 0.92f);
            }

            // Rebond sur un cuirassé : l'appui (en avance ou juste après l'avoir écrasé) donne le grand rebond
            if (pendingStomp != null)
            {
                if (lastJumpDownBeat >= pendingStomp.beat - PressEarlyBeats) BigBounce(pendingStomp, beat, x);
                else if (beat > pendingStomp.beat + PressLateBeats) pendingStomp = null;
            }
            // Notes de saut : en l'air près d'une note, l'appui relance le saut
            CheckOrbs(x, beat);

            // Liane : on l'attrape en passant, le balancier suit un arc calé sur les beats ; on saute pour lâcher
            if (vine == null) TryGrabVine(x);
            bool swinging = vine != null && Swing(x, beat, spb, gravity);
            if (swinging) ground = Terrain;

            // Intégration verticale
            frameStartY = y;
            if (!grounded && !swinging)
            {
                float yPrev = y;
                // intégration exacte pour une gravité constante : saut de 1,5 u en 1 beat quel que soit le framerate
                y += vy * dt - 0.5f * gravity * dt * dt;
                vy -= gravity * dt;
                // atterrissage balayé : même un à-coup ne fait pas traverser une plateforme
                if (vy <= 0f && ground.TryLand(x, half, yPrev, y, out float top)) Land(x, top);
                else if (ground.HitsSide(x, half, y)) { run.Damage(); return false; }   // tombé dans un trou, cogne le bord opposé
            }
            if (!grounded && !swinging && y < ground.KillY(x)) { run.Damage(); return false; }
            CheckBounce(x, beat, spb, gravity);

            transform.position = new Vector3(x, WorldY, 0f);

            // Balayage depuis la frame précédente : un à-coup ne doit pas faire traverser un lum ou un obstacle.
            float sweepFrom = Mathf.Max(prevX, x - maxSweep);
            if (CheckCollisions(x, sweepFrom, beat)) return false;
            if (CheckLasers(x, beat)) return false;
            CollectLums(x, sweepFrom);
            prevX = x;
            prevCenterY = CenterWorldY();
            return true;
        }

        float CenterWorldY() => WorldY + (inverted ? -1f : 1f) * standHeight * 0.5f;

        /// <summary>Hauteur dans le monde à `d` u des pieds, du côté du corps (en dessous des pieds tête en bas) : pour les effets.</summary>
        float BodySideY(float d) => WorldY + (inverted ? -d : d);

        /// <summary>
        /// Portail de gravité pile à l'atterrissage (niveau 8, beat 192) : si le héros allait se poser dans les 2 images
        /// qui viennent, il se pose d'abord, puis part vers le plafond depuis le sol. Sinon, à une image près, il gardait sa
        /// vitesse de chute, traversait le sol sur 1,5 u avant de remonter, et ratait les pièces posées sur l'arc du
        /// simulateur (qui, lui, s'était posé). Même règle dans levelgen/engine.py (FLIP_LAND_SECONDS).
        /// </summary>
        void LandBeforeFlip(float x, float half)
        {
            if (grounded || vy >= 0f || vine != null) return;
            float reach = -vy * FlipLandSeconds;
            if (Terrain.TryLand(x, half, y, y - reach, out float top)) Land(x, top);
        }

        /// <summary>Traverse un portail de gravité : on garde la position et la vitesse dans le monde, on tombe vers l'autre côté.</summary>
        void SetGravity(bool up, float x)
        {
            if (up == inverted) return;
            float w = WorldY;
            inverted = up;
            y = inverted ? -w : w;
            vy = -vy;
            grounded = false;
            airBeats = 0f;
            jumpBufferBeat = -99;
            if (vine != null) { vine.view?.GetComponent<VineView>()?.Release(); vine = null; }
            squash = Squash(0.8f, 1.25f);
            ActionSfx.Instance?.Jump();
            CameraRig.Instance?.Flash(Color.white, 0.35f);
            CameraRig.Instance?.Punch(0.3f);
            FxPool.Instance?.Burst(new Vector2(x, BodySideY(0.5f)), new Color(0.75f, 0.9f, 1f), 16, 5f, 0.15f, 0.4f, 0f, 0f);   // au centre du corps, déjà retourné
        }

        /// <summary>Attrape la liane suivante si on passe à son point de prise (en courant ou en l'air, à ±1,6 u).</summary>
        void TryGrabVine(float x)
        {
            var vines = level.Vines;
            while (vineCursor < vines.Count && (vines[vineCursor].passed || vines[vineCursor].bounds.xMax < x)) vineCursor++;
            if (vineCursor >= vines.Count || inverted) return;
            var o = vines[vineCursor];
            if (o.plane != plane || x < o.bounds.xMin - 0.05f || x > o.bounds.xMin + 0.8f || Mathf.Abs(y - o.grabY) > 1.6f) return;
            o.passed = true;
            vine = o;
            vineGrabBeat = nowBeat;
            grounded = false;
            IsBouncing = false;
            jumpBufferBeat = -99;
            ActionSfx.Instance?.Land();
            FxPool.Instance?.Burst(new Vector2(x, y + VineView.HandHeight), new Color(0.6f, 0.8f, 0.4f), 6, 2.5f, 0.12f, 0.3f, 0f, 0f);
        }

        /// <summary>
        /// Fenêtre du lâcher d'une liane autour de son temps (beats). Un appui plus tôt (après la prise) est gardé : on lâche
        /// à l'ouverture de la fenêtre (2026-11-27 : avant, il était ignoré, la main glissait et le héros tombait au lieu de
        /// sauter, « ça m'obéit pas »). Après la fenêtre, sans appui, on tombe.
        /// </summary>
        const float VineEarlyBeats = 0.35f, VineLateBeats = 0.35f;

        /// <summary>
        /// Balancier : pieds sur un arc (de la prise au lâcher prévu, creux `sag`) qui avance au rythme des beats. C'est au
        /// joueur de sauter pour lâcher, autour du temps de la liane ; le vol est alors calculé pour arriver pile sur la
        /// liane suivante (si elle suit) ou sur le sol, au temps prévu. Sans appui, la main glisse et on tombe.
        /// Renvoie vrai tant qu'on est pendu.
        /// </summary>
        bool Swing(float x, double beat, float spb, float gravity)
        {
            double len = vine.beat - vine.grabBeat;
            float u = (float)((beat - vine.grabBeat) / len);
            var view = vine.view != null ? vine.view.GetComponent<VineView>() : null;
            bool pressed = lastJumpDownBeat > vineGrabBeat;     // appui après la prise (en avance : gardé jusqu'à la fenêtre)
            float arcY = Mathf.Lerp(vine.grabY, vine.releaseY, u) - vine.sag * Mathf.Sin(Mathf.PI * u);
            float arcVy = (float)(((vine.releaseY - vine.grabY) - vine.sag * Mathf.PI * Mathf.Cos(Mathf.PI * u)) / (len * spb));
            if ((!pressed || beat < vine.beat - VineEarlyBeats) && beat <= vine.beat + VineLateBeats)
            {
                y = arcY;
                vy = arcVy;
                grounded = false;
                view?.Hold(new Vector2(x, y + VineView.HandHeight));
                return true;
            }
            view?.Release();
            var o = vine;
            vine = null;
            jumpBufferBeat = -99;
            if (!pressed)
            {
                // trop tard : la main glisse, on tombe (dans l'eau)
                y = arcY;
                vy = Mathf.Min(arcVy, 0f);
                airBeats = 0f;
                return false;
            }
            // lâcher : on arrive au temps prévu (lâcher + vol) sur la liane suivante si elle commence là, sinon sur le sol
            double arrive = o.beat + o.airBeats;
            float t = (float)System.Math.Max(0.3, arrive - beat) * spb;
            float xLand = x + t * level.RunSpeed;
            float target = level.HeightOnPlane(xLand, plane);
            foreach (var next in level.Vines)
                if (next != o && Mathf.Abs(next.grabBeat - (float)arrive) < 0.05f) { target = next.grabY; break; }
            y = arcY;
            vy = (target - y + 0.5f * gravity * t * t) / t;
            airStartBeat = beat;
            airBeats = (float)(arrive - beat);
            run.OnObstacleCleared(o, lastJumpDownBeat);
            squash = Squash(0.8f, 1.25f);
            ActionSfx.Instance?.Jump();
            FxPool.Instance?.Burst(new Vector2(x, y + VineView.HandHeight), new Color(0.6f, 0.8f, 0.4f), 8, 3f, 0.12f, 0.3f, 0f, 0f);
            return false;
        }

        /// <summary>Hauteur d'arrivée (pieds) au temps `arrive` : note de saut, cuirassé, liane qui commence là, sinon le sol.</summary>
        float ArrivalTarget(double arrive, float xLand, out double touch)
        {
            touch = arrive;
            foreach (var o in level.Orbs)
                if (!o.passed && Mathf.Abs(o.beat - (float)arrive) < 0.05f) return o.bounds.center.y - 0.5f;
            foreach (var o in level.Obstacles)
                if (o.stomp && o.alive && Mathf.Abs(o.beat - (float)arrive) < 0.05f)
                {
                    touch = arrive - LevelBuilder.StompTouchBeats;
                    return o.bounds.yMax;
                }
            foreach (var v in level.Vines)
                if (!v.passed && Mathf.Abs(v.grabBeat - (float)arrive) < 0.05f) return v.grabY;
            return level.HeightOnPlane(xLand, plane);
        }

        /// <summary>Envol calculé pour arriver au temps `arrive` sur la cible suivante (comme un tambour).</summary>
        void LaunchTo(double arrive, double beat, float x)
        {
            float xGuess = x + (float)((arrive - beat) * curSpb) * level.RunSpeed;
            float target = ArrivalTarget(arrive, xGuess, out double touch);
            float t = (float)System.Math.Max(0.3, touch - beat) * curSpb;
            vy = (target - y + 0.5f * curGravity * t * t) / t;
            grounded = false;
            airStartBeat = beat;
            airBeats = (float)(touch - beat);
            jumpBufferBeat = -99;
            squash = Squash(0.75f, 1.3f);
            ActionSfx.Instance?.Jump();
        }

        /// <summary>Note de saut : en l'air (ou au sol) près d'elle, un appui autour de son temps relance le saut vers la suivante.</summary>
        void CheckOrbs(float x, double beat)
        {
            var orbs = level.Orbs;
            while (orbCursor < orbs.Count && orbs[orbCursor].bounds.xMax < x - 1.5f) orbCursor++;
            for (int i = orbCursor; i < orbs.Count; i++)
            {
                var o = orbs[i];
                if (o.bounds.xMin > x + 1.5f) break;
                if (o.passed || o.plane != plane || inverted) continue;
                if (Mathf.Abs(o.bounds.center.x - x) > 1.2f || Mathf.Abs(o.bounds.center.y - CenterWorldY()) > OrbReach) continue;
                if (beat < o.beat - PressEarlyBeats || beat > o.beat + PressLateBeats) continue;
                float dy = CenterWorldY() - o.bounds.center.y;
                bool armed = lastJumpDownBeat >= o.beat - PressEarlyBeats - OrbMemoryBeats && lastJumpDownBeat > lastOrbBeat;
                if (!armed || (beat < o.beat && Mathf.Abs(dy) > OrbTouch))
                {
                    // pas d'appui : la note retient le héros à sa hauteur, le temps d'appuyer (au-dessus, il tombe vers elle).
                    // Appui en avance : le rebond part au contact de la note, ou sur son temps.
                    if (!armed && !grounded && dy <= OrbHoldAbove) vy = Mathf.Max(vy, -OrbHover);
                    continue;
                }
                o.passed = true;
                lastOrbBeat = beat;
                LaunchTo(o.beat + o.airBeats, beat, x);
                run.OnObstacleCleared(o, lastJumpDownBeat);
                if (o.view != null) o.view.SetActive(false);
                FxPool.Instance?.Burst(o.bounds.center, new Color(0.55f, 1f, 0.95f), 14, 5f, 0.15f, 0.4f, 0f, 0f);
                CameraRig.Instance?.Punch(0.2f);
                return;
            }
        }

        /// <summary>Écrase un cuirassé : petit rebond tout de suite ; l'appui (déjà fait ou juste après) donne le grand rebond.</summary>
        void Stomp(LevelObject o, double beat, float x)
        {
            o.bumped = true;                                       // il reste là : c'est un tremplin
            run.OnEnemyStomped(o);
            y = Mathf.Max(y, o.bounds.yMax);
            float t = 0.5f * curSpb;                               // petit rebond : un demi-temps, retombe à la même hauteur
            vy = 0.5f * curGravity * t;
            grounded = false;
            airStartBeat = beat;
            airBeats = 0.5f;
            squash = Squash(1.3f, 0.75f);
            pendingStomp = o;
            if (lastJumpDownBeat >= o.beat - PressEarlyBeats) BigBounce(o, beat, x);
        }

        void BigBounce(LevelObject o, double beat, float x)
        {
            pendingStomp = null;
            LaunchTo(o.beat + o.airBeats, beat, x);
            run.OnObstacleCleared(o, lastJumpDownBeat);
            CameraRig.Instance?.Shake(0.1f, 0.1f);
            FxPool.Instance?.Burst(new Vector2(x, WorldY), Color.white, 10, 5f, 0.12f, 0.3f, 3f, 0f);
        }

        /// <summary>Calque de l'image du héros : devant (calque acteurs) ou dans l'arrière-plan (calque monde, derrière le niveau).</summary>
        void SetVisualPlane(int p)
        {
            visualPlane = p;
            if (visual == null) return;
            int layer = p == 1 ? 0 : LayerMask.NameToLayer("Actors");
            if (layer < 0) layer = 0;
            SetLayer(visual, layer);
            if (trail != null) SetLayer(trail.transform, layer);
            foreach (var sr in visual.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!baseOrders.ContainsKey(sr)) baseOrders[sr] = sr.sortingOrder;
                sr.sortingOrder = p == 1 ? -45 : baseOrders[sr];
                sr.color = p == 1 ? new Color(0.62f, 0.62f, 0.78f) : Color.white;
            }
            if (CameraRig.Instance != null) CameraRig.Instance.ActorInWorld = p == 1;
            level?.SetFrontFaded(p == 1);              // le premier plan s'efface pour qu'on voie le héros passer derrière
        }

        readonly System.Collections.Generic.Dictionary<SpriteRenderer, int> baseOrders = new();

        static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayer(t.GetChild(i), layer);
        }

        void Jump(float velocity, double beat, float x)
        {
            LastJumpPressBeat = jumpBufferBeat;     // l'appui (éventuellement en avance, mis en mémoire)
            vy = velocity;
            grounded = false;
            jumpBufferBeat = -99;
            jumpStartBeat = airStartBeat = beat;
            airBeats = jumpAirBeats;
            spinning = spinOnJump;
            squash = Squash(0.82f, 1.2f);
            ActionSfx.Instance?.Jump();
            FxPool.Instance?.Burst(new Vector2(x, BodySideY(0.05f)), Color.white, 6, 3f, 0.12f, 0.3f, 2f, 0f);   // y est retourné au plafond : position monde
        }

        void Land(float x, float top)
        {
            // atterrir sur une plateforme valide le saut (jugé une seule fois)
            foreach (var p in level.Platforms)
            {
                if (p.passed || x < p.bounds.xMin || x > p.bounds.xMax || Mathf.Abs(top - p.bounds.yMin) > 0.02f) continue;
                p.passed = true;
                run.OnObstacleCleared(p, LastJumpPressBeat);
            }
            y = groundTop = top;
            IsBouncing = false;
            CameraRig.Instance?.SetGroundAnchor(WorldY);
            vy = 0f;
            grounded = true;
            airBeats = 0f;
            spinning = false;
            spinner.localRotation = Quaternion.identity;
            squash = Squash(1.2f, 0.82f);
            landedAt = Time.time;
            ActionSfx.Instance?.Land();
            FxPool.Instance?.Burst(new Vector2(x, BodySideY(0.05f)), Color.white, 8, 3.5f, 0.12f, 0.35f, 3f, 0f);
        }

        /// <summary>
        /// Tambour-tremplin : en passant dessus (au sol ou juste après un saut), le héros s'envole pour `airBeats` beats et
        /// retombe pile sur le sol prévu par la chart. Sauter sur le temps du tambour est jugé (PARFAIT…) ; ne pas sauter
        /// n'est pas puni, mais sauter trop tôt fait passer au-dessus (et rater le rebond).
        /// </summary>
        void CheckBounce(float x, double beat, float spb, float gravity)
        {
            var pads = level.Bounces;
            while (bounceCursor < pads.Count && pads[bounceCursor].bounds.xMax + 1f < x) bounceCursor++;
            for (int i = bounceCursor; i < pads.Count; i++)
            {
                var o = pads[i];
                if (o.bounds.xMin > x + 0.5f) break;
                if (o.passed || inverted || o.plane != plane || x < o.bounds.center.x - 0.15f || y > o.bounds.yMax + 1.2f || y < o.bounds.yMin - 0.4f) continue;
                o.passed = true;
                float t = o.airBeats * spb;
                if (o.targetPlane >= 0) plane = o.targetPlane;          // tambour de plan : on retombe sur l'autre chemin
                float landTop = level.HeightOnPlane(x + t * level.RunSpeed, plane);
                vy = (landTop - y + 0.5f * gravity * t * t) / t;
                grounded = false;
                IsBouncing = true;
                airStartBeat = beat;
                airBeats = o.airBeats;
                jumpBufferBeat = -99;
                if (System.Math.Abs(LastJumpPressBeat - o.beat) <= 0.5) run.OnObstacleCleared(o, LastJumpPressBeat);
                else pendingBounce = o;
                squash = Squash(0.7f, 1.35f);
                ActionSfx.Instance?.Jump();
                CameraRig.Instance?.Shake(0.12f, 0.12f);
                CameraRig.Instance?.Punch(0.3f);
                FxPool.Instance?.Burst(new Vector2(o.bounds.center.x, o.bounds.yMax), Color.white, 14, 6f, 0.15f, 0.4f, 4f, 0f);
                var press = o.view != null ? o.view.GetComponentInChildren<PressBump>() : null;
                if (press != null) press.Press();
                return;
            }
        }

        // Laser de la Sourdine (même règle que tools/levelgen/engine.py) : il brûle de beat + 0,3 à beat + 0,3 + durée,
        // au sommet d'un saut lancé sur le temps. Bas : les pieds doivent être à 0,35 u au moins au-dessus du sol ;
        // haut : à 0,6 u au plus (on reste au sol).
        public const float LaserStart = 0.3f, LaserLength = 0.4f, LaserLowClear = 0.35f, LaserHighClear = 0.6f;
        int laserCursor;
        readonly LevelObject laserJudge = new() { kind = LevelObjectKind.Gap };

        /// <summary>Retourne vrai si le joueur a été touché par un laser.</summary>
        bool CheckLasers(float x, double beat)
        {
            var lasers = level.Lasers;
            if (lasers.Count == 0 || inverted) return false;
            while (laserCursor < lasers.Count && beat > lasers[laserCursor].beat + LaserStart
                   + (lasers[laserCursor].@params != null && lasers[laserCursor].@params.length > 0 ? lasers[laserCursor].@params.length : LaserLength))
            {
                var done = lasers[laserCursor++];
                if (done.lane == 0)
                {
                    // laser bas franchi : le saut est jugé (PARFAIT / BIEN / BOF), comme un trou
                    laserJudge.beat = done.beat;
                    laserJudge.judged = false;
                    run.OnObstacleCleared(laserJudge, LastJumpPressBeat);
                }
            }
            for (int i = laserCursor; i < lasers.Count; i++)
            {
                var l = lasers[i];
                double from = l.beat + LaserStart;
                if (beat < from) break;
                float len = l.@params != null && l.@params.length > 0 ? l.@params.length : LaserLength;
                if (beat > from + len) continue;
                float above = y - level.HeightOnPlane(x, plane);
                if ((l.lane == 0 && above < LaserLowClear) || (l.lane == 1 && above > LaserHighClear))
                {
                    run.Damage();
                    return true;
                }
            }
            return false;
        }

        /// <summary>Retourne vrai si le joueur a été touché (la frame s'arrête là).</summary>
        bool CheckCollisions(float x, float sweepFrom, double beat)
        {
            float h = standHeight;
            float wy = WorldY;
            var bodyRect = BodyRect(sweepFrom, x, wy, h);
            bool hitting = beat <= hitUntilBeat;
            // zone de frappe dans le repère de la gravité (au-dessus des pieds = vers la tête), ramenée dans le monde
            float a = grounded ? -0.5f : -airHitBelow, b = grounded ? -0.5f + hitHeight : airHitAbove;
            var hitRect = inverted ? Rect.MinMaxRect(sweepFrom, wy - b, x + hitReach, wy - a) : Rect.MinMaxRect(sweepFrom, wy + a, x + hitReach, wy + b);

            var obstacles = level.Obstacles;
            while (obstacleCursor < obstacles.Count && obstacles[obstacleCursor].bounds.xMax < x - 4f) obstacleCursor++;
            for (int i = obstacleCursor; i < obstacles.Count; i++)
            {
                var o = obstacles[i];
                if (o.bounds.xMin > x + hitReach + 0.5f) break;
                if (!o.alive || o.plane != plane) continue;

                if (o.kind == LevelObjectKind.Gap)
                {
                    if (!o.passed && x - width * 0.5f > o.bounds.xMax && y > o.bounds.yMax + 0.6f)
                    {
                        o.passed = true;
                        run.OnObstacleCleared(o, LastJumpPressBeat);
                    }
                    continue;
                }
                if (o.stomp)
                {
                    // cuirassé = tremplin : il ne meurt pas et ne blesse plus une fois rebondi
                    if (o.bumped) continue;
                    // rebond si on arrive dessus en descendant, ou si on le touche les pieds à moins de 0,6 u sous son
                    // dessus (saut un peu court ou encore en montée) : avant, ces cas tuaient le héros au lieu du rebond
                    bool fromAbove = vy <= 0f && frameStartY >= o.bounds.yMax - 0.45f && y <= o.bounds.yMax + 0.8f;
                    bool nearTop = y >= o.bounds.yMax - 0.6f && bodyRect.Overlaps(o.bounds);
                    if (!inverted && (fromAbove || nearTop) && Mathf.Abs(x - o.bounds.center.x) <= 1.15f)
                    {
                        Stomp(o, beat, x);
                        continue;
                    }
                }
                if (o.IsHittable && !o.stomp && hitting && hitRect.Overlaps(o.bounds))
                {
                    o.alive = false;
                    run.OnEnemyKilled(o, LastHitPressBeat);
                    // un appui = un ennemi, le plus proche (la liste est triée de gauche à droite) : deux ennemis
                    // rapprochés demandent deux appuis, sur le rythme (avant, un coup les tuait tous les deux)
                    hitting = false;
                    hitUntilBeat = -99;
                    continue;
                }
                if (!inverted && bodyRect.Overlaps(o.bounds) && (o.kind == LevelObjectKind.StepFace || o.kind == LevelObjectKind.Wall)
                    && y >= o.bounds.yMax - ledgeAssist)
                {
                    // coup de pouce de plateformeur : un saut un peu court se hisse sur la marche ou passe la souche
                    y = o.bounds.yMax;
                    if (vy < 0f) vy = 0f;
                    transform.position = new Vector3(x, y, 0f);
                    bodyRect = BodyRect(sweepFrom, x, y, h);
                }
                if (bodyRect.Overlaps(o.bounds))
                {
                    run.Damage();
                    return true;
                }
                if (!o.passed && x - width * 0.5f > o.bounds.xMax)
                {
                    o.passed = true;
                    run.OnObstacleCleared(o, LastJumpPressBeat);
                }
            }
            return false;
        }

        /// <summary>Corps dans le monde : des pieds vers la tête (vers le bas en gravité inversée).</summary>
        Rect BodyRect(float fromX, float x, float feetWorldY, float h) => inverted
            ? Rect.MinMaxRect(fromX - width * 0.5f, feetWorldY - h, x + width * 0.5f, feetWorldY)
            : Rect.MinMaxRect(fromX - width * 0.5f, feetWorldY, x + width * 0.5f, feetWorldY + h);

        void CollectLums(float x, float sweepFrom)
        {
            var center = new Vector2(x, CenterWorldY());
            var from = new Vector2(sweepFrom, prevCenterY);
            var lums = level.Lums;
            while (lumCursor < lums.Count && lums[lumCursor].bounds.xMax < x - 4f) lumCursor++;
            for (int i = lumCursor; i < lums.Count; i++)
            {
                var o = lums[i];
                if (o.bounds.xMin > x + 1f) break;
                if (!o.alive || o.plane != plane) continue;
                if (SqrDistanceToSegment(o.bounds.center, from, center) <= pickupRadius * pickupRadius)
                {
                    o.alive = false;
                    run.OnLumCollected(o);
                }
            }
        }

        static float SqrDistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return (a + ab * t - p).sqrMagnitude;
        }

        // Écrasement atténué par squashAmount (1 = valeurs brutes, 0 = aucun).
        Vector3 Squash(float x, float y) => new Vector3(Mathf.Lerp(1f, x, squashAmount), Mathf.Lerp(1f, y, squashAmount), 1f);

        void UpdateVisual(double beat, float dt)
        {
            squash = Vector3.Lerp(squash, Vector3.one, 1f - Mathf.Exp(-14f * dt));
            visual.localScale = new Vector3(squash.x, inverted ? -squash.y : squash.y, 1f);   // tête en bas : miroir entier
            // changement de plan : l'image passe derrière (ou devant) le niveau au sommet du saut
            if (visualPlane != plane && (grounded || vy <= 0f)) SetVisualPlane(plane);

            // Vrille pendant le saut (désactivée par défaut) : un tour complet par saut, par crans de 45° pour rester pixel art.
            if (spinning)
            {
                float t = Mathf.Clamp01((float)((beat - jumpStartBeat) / jumpAirBeats));
                spinner.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Round(360f * t / 45f) * 45f);
            }

            if (slashTimer > 0f)
            {
                slashTimer -= dt;
                if (slash != null)
                {
                    float k = Mathf.Clamp01(slashTimer / slashDuration);
                    slash.enabled = true;
                    slash.transform.localScale = Vector3.one * (0.7f + 0.4f * (1f - k));
                    var c = slash.color;
                    c.a = k;
                    slash.color = c;
                }
            }
            else if (slash != null && slash.enabled) slash.enabled = false;
        }
    }
}
