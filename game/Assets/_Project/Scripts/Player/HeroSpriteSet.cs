using UnityEngine;

namespace Kenta.Player
{
    /// <summary>Les 8 poses d'un héros, identiques pour les trois (docs/da-kenta.md § 4), extraites du PDF de la DA.</summary>
    [CreateAssetMenu(fileName = "HeroSprites_", menuName = "Kenta/Hero Sprite Set")]
    public class HeroSpriteSet : ScriptableObject
    {
        public Sprite idle;
        public Sprite run;
        [Tooltip("Cycle de course (6 images générées par tools/gen-hero-run.py).")]
        public Sprite[] runCycle;
        public Sprite jump;
        public Sprite slide;
        public Sprite hit;
        public Sprite vine;
        public Sprite wall;
        public Sprite hurt;
        public Sprite death;
    }
}
