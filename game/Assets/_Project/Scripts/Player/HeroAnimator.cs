using UnityEngine;

namespace Kenta.Player
{
    /// <summary>
    /// Choisit la pose du héros (course, saut, glissade, frappe, mort) parmi celles du PDF de la DA. La course est un
    /// cycle de 6 images (tools/gen-hero-run.py) qui avance avec la distance parcourue : la foulée suit la vitesse,
    /// sans à-coup sur le temps.
    /// </summary>
    public class HeroAnimator : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] SpriteRenderer target;
        [SerializeField] HeroSpriteSet sprites;
        [Tooltip("Distance (unités) d'une foulée complète (les 6 images).")]
        [SerializeField] float strideUnits = 1.6f;

        public HeroSpriteSet Sprites { get => sprites; set => sprites = value; }

        /// <summary>Image du cycle de course pour une distance donnée (boucle, jamais négative).</summary>
        public static int RunFrameIndex(float distance, float stride, int frames)
        {
            if (frames <= 0 || stride <= 0f) return 0;
            float t = distance / stride;
            t -= Mathf.Floor(t);
            return Mathf.Min(frames - 1, (int)(t * frames));
        }

        void LateUpdate()
        {
            if (player == null || target == null || sprites == null) return;
            target.sprite = Pick();
        }

        Sprite Pick()
        {
            if (player.IsDead) return sprites.death;
            if (player.IsPunching) return sprites.hit;
            if (player.IsSliding) return sprites.slide;
            if (!player.IsGrounded) return sprites.jump;
            if (player.SinceLanded < 0.08f) return sprites.idle;
            var cycle = sprites.runCycle;
            if (cycle == null || cycle.Length == 0) return sprites.run;
            return cycle[RunFrameIndex(player.transform.position.x, strideUnits, cycle.Length)];
        }
    }
}
