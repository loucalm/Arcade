using RythmeRunner.Rhythm;
using UnityEngine;

namespace RythmeRunner.Player
{
    /// <summary>
    /// Choisit l'image du héros selon son état. La foulée est calée sur la musique : un cycle complet
    /// dure runCycleBeats beats, donc un pied touche le sol à chaque beat.
    /// </summary>
    public class HeroAnimator : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] SpriteRenderer target;
        [SerializeField] HeroSpriteSet sprites;
        [Tooltip("Beats par foulée complète (2 = un pied par beat).")]
        [SerializeField, Min(0.5f)] float runCycleBeats = 2f;
        [Tooltip("Vitesse verticale (u/s) au-delà de laquelle on est « en montée » ou « en chute ».")]
        [SerializeField] float airThreshold = 6f;
        [SerializeField] float landPoseSeconds = 0.1f;

        void LateUpdate()
        {
            if (player == null || target == null || sprites == null || sprites.run == null || sprites.run.Length == 0) return;
            target.sprite = Pick();
        }

        Sprite Pick()
        {
            if (player.IsPunching) return sprites.hit;
            if (player.IsSliding) return sprites.slide;
            if (!player.IsGrounded)
            {
                float vy = player.VelocityY;
                if (vy > airThreshold) return sprites.jumpUp;
                if (vy < -airThreshold) return sprites.jumpFall;
                return sprites.jumpApex;
            }
            if (player.SinceLanded < landPoseSeconds) return sprites.land;

            var conductor = Conductor.Instance;
            double beat = conductor != null && conductor.IsPlaying ? conductor.SongBeat : Time.unscaledTime * 2.0;
            double phase = beat / runCycleBeats;
            phase -= System.Math.Floor(phase);
            int n = sprites.run.Length;
            return sprites.run[Mathf.Min(n - 1, (int)(phase * n))];
        }
    }
}
