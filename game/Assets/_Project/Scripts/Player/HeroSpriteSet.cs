using UnityEngine;

namespace Kenta.Player
{
    /// <summary>Les images d'un héros, identiques pour les trois (docs/da-kenta.md § 4), dessinées par la marionnette
    /// de tools/hero_rig.py (tools/gen-heroes.py).</summary>
    [CreateAssetMenu(fileName = "HeroSprites_", menuName = "Kenta/Hero Sprite Set")]
    public class HeroSpriteSet : ScriptableObject
    {
        public Sprite idle;
        [Tooltip("Attente en 4 images (respiration, clignement d'yeux sur la dernière) : portraits de l'écran des héros.")]
        public Sprite[] idleCycle;
        public Sprite run;
        [Tooltip("Cycle de course (8 images, deux pas).")]
        public Sprite[] runCycle;
        public Sprite jump;
        public Sprite hit;
        public Sprite vine;
        public Sprite wall;
        public Sprite hurt;
        public Sprite death;

        [Header("Images d'action")]
        [Tooltip("Coup de katana en 6 images (160×72, héros centré) : dégainé, la coupe part, coupe pleine (la traînée " +
                 "s'arrête à la portée de frappe), la traînée s'efface, suivi, retour en garde.")]
        public Sprite[] hitFrames;
        [Tooltip("Saut en 8 images réparties sur sa durée : impulsion, genoux remontés, vrille d'un tour, ouverture, réception.")]
        public Sprite[] jumpFrames;
        [Tooltip("Accroupi : réception d'un saut.")]
        public Sprite crouch;
        [Tooltip("Montée d'une pente : course penchée en avant, genoux hauts (8 images).")]
        public Sprite[] climbCycle;
        [Tooltip("Descente d'une pente : course penchée en arrière, foulée retenue (8 images).")]
        public Sprite[] descendCycle;
        [Tooltip("Chute depuis un bord : le pas dans le vide, puis la chute en boucle.")]
        public Sprite[] fallFrames;

        [Header("Animations dessinées par Gemini (tools/gemini-heros/clean.py)")]
        [Tooltip("Victoire (fin de niveau) : moulinet du katana, rengainé, pose ; la dernière image est tenue.")]
        public Sprite[] victoryFrames;
        [Tooltip("Atterrissage : écrasé, se relève.")]
        public Sprite[] landFrames;
        [Tooltip("Liane : suspendu d'une main, les jambes balancent (boucle).")]
        public Sprite[] vineFrames;
        [Tooltip("Course sur le mur (boucle avec la distance).")]
        public Sprite[] wallCycle;
        [Tooltip("Envol de tambour : accroupi, détente, étoile, vrille.")]
        public Sprite[] drumFrames;
        [Tooltip("Touché : recul, grimace (début de la mort).")]
        public Sprite[] hurtFrames;
        [Tooltip("Mort : projeté, tombe, rebondit, couché ; la dernière image est tenue.")]
        public Sprite[] deathFrames;
        [Tooltip("Coup de katana en l'air, 4 images (160×72, héros centré).")]
        public Sprite[] airHitFrames;
        [Tooltip("Portraits 64×64 pour les menus : neutre, déterminé, victoire.")]
        public Sprite[] portraits;
    }
}
