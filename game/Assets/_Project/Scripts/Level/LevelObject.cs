using UnityEngine;

namespace RythmeRunner.Level
{
    public enum LevelObjectKind { Gap, Wall, Enemy, Block, SlideBar, Lum }

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
        public GameObject view;

        public bool IsHittable => kind == LevelObjectKind.Enemy || kind == LevelObjectKind.Block;

        public void ResetState()
        {
            alive = true;
            passed = false;
            if (view != null)
            {
                view.SetActive(true);
                view.transform.localScale = Vector3.one;
            }
        }
    }
}
