using UnityEngine;

namespace Kenta.Player
{
    /// <summary>Les images d'un héros, identiques pour les trois (docs/da-kenta.md § 4), dessinées par la marionnette
    /// de tools/hero_rig.py (tools/gen-heroes.py).</summary>
    [CreateAssetMenu(fileName = "HeroSprites_", menuName = "Kenta/Hero Sprite Set")]
    public class HeroSpriteSet : ScriptableObject
    {
        public Sprite idle;
        public Sprite run;
        [Tooltip("Cycle de course (8 images, deux pas).")]
        public Sprite[] runCycle;
        public Sprite jump;
        public Sprite slide;
        public Sprite hit;
        public Sprite vine;
        public Sprite wall;
        public Sprite hurt;
        public Sprite death;

        [Header("Images d'action")]
        [Tooltip("Frappe en 4 images : début de l'arc, arc complet (gelé), fin de l'arc, retour.")]
        public Sprite[] hitFrames;
        [Tooltip("Saut en 8 images réparties sur sa durée : impulsion, genoux remontés, vrille d'un tour, ouverture, réception.")]
        public Sprite[] jumpFrames;
        [Tooltip("Glissade en boucle (poussière et traits de vitesse).")]
        public Sprite[] slideLoop;
        [Tooltip("Se jeter au sol : de la course à la glissade.")]
        public Sprite[] slideIn;
        [Tooltip("Se relever : de la glissade à la course.")]
        public Sprite[] slideOut;
        [Tooltip("Accroupi : réception d'un saut.")]
        public Sprite crouch;
        [Tooltip("Montée d'une pente : course penchée en avant, genoux hauts (8 images).")]
        public Sprite[] climbCycle;
        [Tooltip("Descente d'une pente : course penchée en arrière, foulée retenue (8 images).")]
        public Sprite[] descendCycle;
        [Tooltip("Chute depuis un bord : le pas dans le vide, puis la chute en boucle.")]
        public Sprite[] fallFrames;
    }
}
