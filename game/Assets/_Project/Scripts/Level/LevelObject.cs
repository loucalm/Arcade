using UnityEngine;

namespace Kenta.Level
{
    public enum LevelObjectKind { Gap, Wall, Enemy, Block, Lum, StepFace, Platform, Bounce, Vine, Orb }

    /// <summary>
    /// Donnée runtime d'un élément du niveau (collision gérée à la main, pas de moteur physique : ADR-009).
    /// `beat` = le temps sur lequel le joueur doit agir.
    /// </summary>
    public class LevelObject
    {
        public LevelObjectKind kind;
        public float beat;
        /// <summary>Hitbox monde (x, y = coin bas-gauche).</summary>
        public Rect bounds;
        /// <summary>Rang dans une ligne de lums (pour la note jouée).</summary>
        public int sequenceIndex;
        public bool alive = true;
        public bool passed;
        /// <summary>Déjà jugé (PARFAIT/BIEN/BOF) : un obstacle ne compte qu'une fois.</summary>
        public bool judged;
        public GameObject view;
        /// <summary>Tremplin : durée du vol (beats).</summary>
        public float airBeats;
        /// <summary>Jamais jugé (vide sous des plateformes, franchi sans action propre).</summary>
        public bool silent;
        /// <summary>Pièce : note jouée (demi-tons par rapport au la du son de pièce), si hasNote.</summary>
        public bool hasNote;
        public int note;
        /// <summary>Plan : 0 = premier plan, 1 = arrière-plan (on n'y touche que ce qui est sur le même plan que le héros).</summary>
        public int plane;
        /// <summary>Tremplin : plan où il envoie (−1 = le même).</summary>
        public int targetPlane = -1;
        /// <summary>Liane : beat où on l'attrape (le beat jugé est celui du lâcher), hauteur des pieds à la prise et au lâcher,
        /// creux du balancier.</summary>
        public float grabBeat, grabY, releaseY, sag;
        /// <summary>Ennemi cuirassé : on ne le frappe pas, on lui saute dessus (appui au rebond = grand rebond).</summary>
        public bool stomp;

        public bool IsHittable => kind == LevelObjectKind.Enemy || kind == LevelObjectKind.Block;

        public void ResetState()
        {
            alive = true;
            passed = false;
            judged = false;
            if (view != null)
            {
                view.SetActive(true);
                view.transform.localScale = Vector3.one;
            }
        }
    }
}
