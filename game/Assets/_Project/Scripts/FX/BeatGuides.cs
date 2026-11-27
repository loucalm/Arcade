using Kenta.Level;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Repères de rythme (2026-11-27, retour de Lucas) : un trait fin sur chaque temps, plus marqué sur le premier temps
    /// de la mesure, sur toute la hauteur de l'image. C'était la grille de debug (F9), posée à hauteur fixe : elle
    /// disparaissait dès qu'on montait, et n'existait pas en build. Ici, une petite réserve de traits suit la caméra :
    /// à chaque image, chaque trait se place sur un temps visible, au pixel près, centré sur la caméra en hauteur.
    /// Aucune allocation par image.
    /// </summary>
    public class BeatGuides : MonoBehaviour
    {
        static readonly Color BeatColor = new(1f, 1f, 1f, 0.12f);
        static readonly Color BarColor = new(1f, 1f, 1f, 0.28f);

        SpriteRenderer[] lines;
        LevelBuilder level;
        int beatsPerBar = 4, lastBeat;

        public void Setup(LevelBuilder builder, SpriteRenderer[] pool, int perBar, int last)
        {
            level = builder;
            lines = pool;
            beatsPerBar = Mathf.Max(1, perBar);
            lastBeat = last;
        }

        /// <summary>Premier temps dont le trait est dans l'image (bord gauche de la vue, en u, et largeur d'un temps).</summary>
        public static int FirstBeat(float viewLeft, float unitsPerBeat) =>
            Mathf.Max(0, Mathf.CeilToInt(viewLeft / Mathf.Max(0.001f, unitsPerBeat)));

        void LateUpdate()
        {
            if (lines == null || level == null) return;
            var rig = CameraRig.Instance;
            var cam = rig != null && rig.Cam != null ? rig.Cam : Camera.main;
            if (cam == null) return;
            float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
            Vector3 c = cam.transform.position;
            float unitsPerBeat = level.BeatToX(1);
            float left = c.x - halfW - 0.5f, right = c.x + halfW + 0.5f;
            int first = FirstBeat(left, unitsPerBeat);
            float y = PixelScreen.Snap(c.y);
            float height = (halfH * 2f + 2f) * PixelScreen.Ppu;          // la vue entière, et un peu plus
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                int beat = first + i;
                float x = level.BeatToX(beat);
                bool on = beat <= lastBeat && x <= right;
                if (line.enabled != on) line.enabled = on;
                if (!on) continue;
                line.transform.position = new Vector3(PixelScreen.Snap(x), y, 0f);
                line.transform.localScale = new Vector3(1f, height, 1f);
                line.color = beat % beatsPerBar == 0 ? BarColor : BeatColor;
            }
        }
    }
}
