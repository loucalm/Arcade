using Kenta.FX;
using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.Player
{
    /// <summary>
    /// Choisit la pose du héros (course, saut, glissade, frappe, mort) parmi les 8 du PDF de la DA. Chaque pose n'a qu'une
    /// image : la course rebondit d'un pixel sur chaque demi-temps (un pas par temps), calée sur la musique.
    /// </summary>
    public class HeroAnimator : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] SpriteRenderer target;
        [SerializeField] HeroSpriteSet sprites;

        public HeroSpriteSet Sprites { get => sprites; set => sprites = value; }

        void LateUpdate()
        {
            if (player == null || target == null || sprites == null) return;
            target.sprite = Pick(out int bobPixels);
            target.transform.localPosition = new Vector3(0f, bobPixels / (float)PixelScreen.Ppu, 0f);
        }

        Sprite Pick(out int bob)
        {
            bob = 0;
            if (player.IsDead) return sprites.death;
            if (player.IsPunching) return sprites.hit;
            if (player.IsSliding) return sprites.slide;
            if (!player.IsGrounded) return sprites.jump;
            if (player.SinceLanded < 0.08f) return sprites.idle;
            var conductor = Conductor.Instance;
            double beat = conductor != null && conductor.IsPlaying ? conductor.SongBeat : Time.unscaledTime * 2.0;
            bob = ((int)(beat * 2.0) & 1) == 0 ? 0 : 1;
            return sprites.run;
        }
    }
}
