using UnityEngine;

namespace RythmeRunner.Level
{
    public enum LevelObjectKind { Gap, Wall, Enemy, Block, SlideBar, Hook, WallRun, Lum }

    /// <summary>
    /// Donnée runtime d'un élément du niveau (collision gérée à la main, pas de moteur physique : ADR-009).
    /// `beat` = le temps sur lequel le joueur doit agir.
    /// </summary>
    public class LevelObject
    {
        public LevelObjectKind kind;
        public float beat;
        public float startBeat;
        public float endBeat;
        /// <summary>Hauteur du joueur au moment où il prend la liane (départ de l'arc).</summary>
        public float trajectoryY0;
        /// <summary>Hitbox monde (x, y = coin bas-gauche).</summary>
        public Rect bounds;
        /// <summary>Rang dans une ligne de lums (pour la note jouée).</summary>
        public int sequenceIndex;
        public bool alive = true;
        public bool passed;
        public GameObject view;

        public bool IsHittable => kind == LevelObjectKind.Enemy || kind == LevelObjectKind.Block;

        public bool IsTrajectory => kind == LevelObjectKind.Hook || kind == LevelObjectKind.WallRun;

        public float EvaluateY(double beat)
        {
            float t = Mathf.Clamp01((float)((beat - startBeat) / (endBeat - startBeat)));
            if (kind == LevelObjectKind.Hook)
                return Mathf.Lerp(trajectoryY0, 2.4f, Mathf.SmoothStep(0f, 1f, t)) + 0.8f * Mathf.Sin(Mathf.PI * t);
            return 2.6f * Mathf.Sin(Mathf.PI * t);
        }

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
