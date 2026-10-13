using UnityEngine;

namespace RythmeRunner.Player
{
    /// <summary>Les images du héros (générées par tools/gen-neon-sprites.py). Un asset par apparence.</summary>
    [CreateAssetMenu(fileName = "HeroSprites_", menuName = "Rythme Runner/Hero Sprite Set")]
    public class HeroSpriteSet : ScriptableObject
    {
        [Tooltip("Cycle de course : une foulée complète (jambe gauche + droite). Calé sur le beat par HeroAnimator.")]
        public Sprite[] run;
        public Sprite jumpUp;
        public Sprite jumpApex;
        public Sprite jumpFall;
        public Sprite land;
        public Sprite slide;
        public Sprite hit;
        public Sprite hurt;
    }
}
