using RythmeRunner.Core;
using RythmeRunner.FX;
using RythmeRunner.Level;
using RythmeRunner.Rhythm;
using UnityEngine;

namespace RythmeRunner.Player
{
    /// <summary>
    /// Personnage du runner. X vient TOUJOURS du Conductor (x = BeatToX(SongBeat)) : le joueur ne ralentit
    /// jamais et reste synchro avec la musique. Y = physique cinématique maison, réglée en beats :
    /// un saut dure jumpAirBeats beats, quel que soit le BPM. Collisions AABB à la main (ADR-009).
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Visuel (hiérarchie : Visual (squash) > Spinner (rotation) > Body/Glow)")]
        [SerializeField] Transform visual;
        [SerializeField] Transform spinner;
        [SerializeField] SpriteRenderer body;
        [SerializeField] SpriteRenderer slash;

        [Header("Corps")]
        [SerializeField] float width = 0.7f;
        [SerializeField] float standHeight = 0.85f;
        [SerializeField] float slideHeight = 0.4f;
        [SerializeField] float pickupRadius = 0.7f;
        [SerializeField] float fallDeathY = -2.5f;
        [Tooltip("Collisions balayées entre deux frames (à-coup du navigateur) jusqu'à cette distance max.")]
        [SerializeField] float maxSweep = 3f;

        [Header("Saut (hauteur fixe, durée en beats)")]
        [SerializeField] float jumpHeight = 2.2f;
        [SerializeField] float jumpAirBeats = 1f;
        [SerializeField] float coyoteBeats = 0.12f;
        [SerializeField] float jumpBufferBeats = 0.2f;

        [Header("Frappe")]
        [SerializeField] float hitReach = 1.9f;
        [SerializeField] float hitHeight = 2.1f;
        [SerializeField] float hitActiveBeats = 0.3f;

        public bool IsSliding => sliding;
        public bool IsGrounded => grounded;

        LevelBuilder level;
        RunManager run;
        IPlayerInput input;

        float y, vy;
        bool grounded, sliding, dead;
        double lastGroundedBeat, jumpBufferBeat = -99, hitUntilBeat = -99, jumpStartBeat;
        bool spinning;
        int obstacleCursor, lumCursor;
        float prevX, prevCenterY;
        Vector3 squash = Vector3.one;
        float slashTimer, slashDuration = 0.1f;

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
        }

        public void Spawn(double beat)
        {
            dead = false;
            y = vy = 0f;
            grounded = true;
            sliding = false;
            spinning = false;
            jumpBufferBeat = hitUntilBeat = -99;
            lastGroundedBeat = beat;
            float x = level.BeatToX(beat);
            transform.position = new Vector3(x, 0f, 0f);
            prevX = x;
            prevCenterY = standHeight * 0.5f;
            obstacleCursor = LevelBuilder.FirstIndexAfter(level.Obstacles, x - 4f);
            lumCursor = LevelBuilder.FirstIndexAfter(level.Lums, x - 4f);
            input?.ResetAt(beat);
            gameObject.SetActive(true);
            visual.gameObject.SetActive(true);
            squash = new Vector3(0.6f, 1.5f, 1f);
            spinner.localRotation = Quaternion.identity;
            slash.enabled = false;
            FxPool.Instance?.Burst(new Vector2(x, 0.5f), body.color, 14, 5f, 0.18f, 0.45f, 4f);
        }

        public void Die()
        {
            dead = true;
            visual.gameObject.SetActive(false);
            slash.enabled = false;
            var p = (Vector2)transform.position + new Vector2(0f, 0.45f);
            FxPool.Instance?.Burst(p, body.color, 40, 11f, 0.28f, 0.9f, 10f, 0.6f);
            FxPool.Instance?.Burst(p, Color.white, 16, 7f, 0.18f, 0.5f, 6f);
        }

        void Update()
        {
            if (dead || level == null || run == null || !run.IsRunning) return;
            var conductor = Conductor.Instance;
            if (conductor == null) return;

            double beat = conductor.SongBeat;
            float spb = (float)conductor.SecondsPerBeat;
            float dt = Time.deltaTime;
            float x = level.BeatToX(beat);
            var cmd = input.Sample(beat);

            // Physique réglée en beats : sommet à jumpHeight, retour au sol après jumpAirBeats.
            float airTime = jumpAirBeats * spb;
            float gravity = 8f * jumpHeight / (airTime * airTime);
            float jumpVelocity = 4f * jumpHeight / airTime;
            bool overGap = level.IsOverGap(x, width * 0.5f);

            if (grounded && overGap) { grounded = false; vy = 0f; }
            if (grounded) lastGroundedBeat = beat;

            // Saut (avec buffer et coyote time)
            if (cmd.jumpDown) jumpBufferBeat = beat;
            bool coyote = !grounded && vy <= 0f && y > -0.2f && beat - lastGroundedBeat <= coyoteBeats;
            if (beat - jumpBufferBeat <= jumpBufferBeats && (grounded || coyote)) Jump(jumpVelocity, beat, x);

            // Frappe
            if (cmd.hitDown)
            {
                hitUntilBeat = beat + hitActiveBeats;
                slashDuration = slashTimer = hitActiveBeats * spb;
                ActionSfx.Instance?.Hit();
                squash = new Vector3(1.3f, 0.85f, 1f);
            }

            // Glissade au sol, chute rapide en l'air
            if (!grounded && cmd.slideHeld && vy > -jumpVelocity) vy = -jumpVelocity;
            bool nowSliding = cmd.slideHeld && grounded;
            if (nowSliding && !sliding)
            {
                ActionSfx.Instance?.Slide();
                FxPool.Instance?.Burst(new Vector2(x - 0.3f, 0.05f), Color.white, 6, 3f, 0.12f, 0.3f, 2f, -0.2f);
            }
            sliding = nowSliding;

            // Intégration verticale
            if (!grounded)
            {
                vy -= gravity * dt;
                y += vy * dt;
                if (vy <= 0f && y <= 0f && !overGap)
                {
                    if (y > -0.4f) Land(x);
                    else { run.Damage(); return; } // tombé dans le trou, cogne le bord opposé
                }
            }
            if (y < fallDeathY) { run.Damage(); return; }

            transform.position = new Vector3(x, y, 0f);

            // Balayage depuis la frame précédente : un à-coup ne doit pas faire traverser un lum ou un obstacle.
            float sweepFrom = Mathf.Max(prevX, x - maxSweep);
            if (CheckCollisions(x, sweepFrom, beat)) return;
            CollectLums(x, sweepFrom);
            UpdateVisual(beat, dt);
            prevX = x;
            prevCenterY = y + (sliding ? slideHeight : standHeight) * 0.5f;
        }

        void Jump(float velocity, double beat, float x)
        {
            vy = velocity;
            grounded = false;
            jumpBufferBeat = -99;
            jumpStartBeat = beat;
            spinning = true;
            squash = new Vector3(0.7f, 1.35f, 1f);
            ActionSfx.Instance?.Jump();
            FxPool.Instance?.Burst(new Vector2(x, 0.05f), Color.white, 6, 3f, 0.12f, 0.3f, 2f, 0f);
        }

        void Land(float x)
        {
            y = 0f;
            vy = 0f;
            grounded = true;
            spinning = false;
            spinner.localRotation = Quaternion.identity;
            squash = new Vector3(1.35f, 0.7f, 1f);
            ActionSfx.Instance?.Land();
            FxPool.Instance?.Burst(new Vector2(x, 0.05f), Color.white, 8, 3.5f, 0.12f, 0.35f, 3f, 0f);
        }

        /// <summary>Retourne vrai si le joueur a été touché (la frame s'arrête là).</summary>
        bool CheckCollisions(float x, float sweepFrom, double beat)
        {
            float h = sliding ? slideHeight : standHeight;
            var bodyRect = Rect.MinMaxRect(sweepFrom - width * 0.5f, y, x + width * 0.5f, y + h);
            bool hitting = beat <= hitUntilBeat;
            var hitRect = Rect.MinMaxRect(sweepFrom, y - 0.5f, x + hitReach, y - 0.5f + hitHeight);

            var obstacles = level.Obstacles;
            while (obstacleCursor < obstacles.Count && obstacles[obstacleCursor].bounds.xMax < x - 4f) obstacleCursor++;
            for (int i = obstacleCursor; i < obstacles.Count; i++)
            {
                var o = obstacles[i];
                if (o.bounds.xMin > x + hitReach + 0.5f) break;
                if (!o.alive) continue;

                if (o.kind == LevelObjectKind.Gap)
                {
                    if (!o.passed && x - width * 0.5f > o.bounds.xMax && y > -0.4f)
                    {
                        o.passed = true;
                        run.OnObstacleCleared(o);
                    }
                    continue;
                }
                if (o.IsHittable && hitting && hitRect.Overlaps(o.bounds))
                {
                    o.alive = false;
                    run.OnEnemyKilled(o);
                    continue;
                }
                if (bodyRect.Overlaps(o.bounds))
                {
                    run.Damage();
                    return true;
                }
                if (!o.passed && x - width * 0.5f > o.bounds.xMax)
                {
                    o.passed = true;
                    run.OnObstacleCleared(o);
                }
            }
            return false;
        }

        void CollectLums(float x, float sweepFrom)
        {
            float h = sliding ? slideHeight : standHeight;
            var center = new Vector2(x, y + h * 0.5f);
            var from = new Vector2(sweepFrom, prevCenterY);
            var lums = level.Lums;
            while (lumCursor < lums.Count && lums[lumCursor].bounds.xMax < x - 4f) lumCursor++;
            for (int i = lumCursor; i < lums.Count; i++)
            {
                var o = lums[i];
                if (o.bounds.xMin > x + 1f) break;
                if (!o.alive) continue;
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

        void UpdateVisual(double beat, float dt)
        {
            var target = sliding ? new Vector3(1.3f, 0.47f, 1f) : Vector3.one;
            squash = Vector3.Lerp(squash, target, 1f - Mathf.Exp(-14f * dt));
            visual.localScale = squash;

            // Vrille pendant le saut : un tour complet par saut, calé sur sa durée.
            if (spinning)
            {
                float t = Mathf.Clamp01((float)((beat - jumpStartBeat) / jumpAirBeats));
                spinner.localRotation = Quaternion.Euler(0f, 0f, -360f * Mathf.SmoothStep(0f, 1f, t));
            }

            if (slashTimer > 0f)
            {
                slashTimer -= dt;
                float k = Mathf.Clamp01(slashTimer / slashDuration);
                slash.enabled = true;
                slash.transform.localScale = new Vector3(hitReach * (1.2f - 0.4f * k), 1.4f * k + 0.2f, 1f);
                var c = slash.color;
                c.a = k;
                slash.color = c;
            }
            else if (slash.enabled) slash.enabled = false;
        }
    }
}
