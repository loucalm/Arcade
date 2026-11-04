using System.Collections.Generic;
using UnityEngine;

namespace Kenta.Level
{
    /// <summary>Segment de sol : de x0 à x1, dessus à `top`. Une plateforme (`oneWay`) se traverse par en dessous.</summary>
    public struct GroundSpan
    {
        public float x0, x1, top;
        public bool oneWay;

        public GroundSpan(float x0, float x1, float top, bool oneWay)
        {
            this.x0 = x0;
            this.x1 = x1;
            this.top = top;
            this.oneWay = oneWay;
        }
    }

    /// <summary>
    /// Sol du niveau à hauteur variable (marches, trous, plateformes), lu par le joueur et la caméra.
    /// Collisions à la main (ADR-009) : atterrissage balayé entre deux images, appui sous les pieds, chocs de côté.
    /// </summary>
    public class LevelTerrain
    {
        const float SupportTolerance = 0.02f;
        /// <summary>Sous le dessus d'un sol de plus de cette marge, on le cogne de côté (sinon on remonte encore dessus).</summary>
        const float SideTolerance = 0.4f;

        readonly List<GroundSpan> spans = new();
        public IReadOnlyList<GroundSpan> Spans => spans;
        public float LowestTop { get; private set; }

        public void Clear()
        {
            spans.Clear();
            LowestTop = 0f;
        }

        public void Add(GroundSpan s)
        {
            if (s.x1 > s.x0) spans.Add(s);
        }

        public void Build()
        {
            spans.Sort((a, b) => a.x0.CompareTo(b.x0));
            LowestTop = float.PositiveInfinity;
            foreach (var s in spans) LowestTop = Mathf.Min(LowestTop, s.top);
            if (float.IsInfinity(LowestTop)) LowestTop = 0f;
        }

        static bool Overlaps(in GroundSpan s, float x, float halfWidth) => x + halfWidth > s.x0 && x - halfWidth < s.x1;

        /// <summary>Vrai si les pieds ont franchi le dessus d'un sol par le haut entre yPrev et yNow (le plus haut gagne).</summary>
        public bool TryLand(float x, float halfWidth, float yPrev, float yNow, out float top)
        {
            top = float.NegativeInfinity;
            if (yNow > yPrev) return false;            // on monte : on ne se pose pas
            bool found = false;
            for (int i = 0; i < spans.Count; i++)
            {
                var s = spans[i];
                if (s.x0 > x + halfWidth) break;
                if (!Overlaps(s, x, halfWidth)) continue;
                if (yPrev >= s.top - SupportTolerance && yNow <= s.top && s.top > top)
                {
                    top = s.top;
                    found = true;
                }
            }
            return found;
        }

        /// <summary>Dessus du sol sur lequel reposent les pieds à la hauteur y, sinon −∞ (le héros tombe).</summary>
        public float SupportTop(float x, float halfWidth, float y)
        {
            float best = float.NegativeInfinity;
            for (int i = 0; i < spans.Count; i++)
            {
                var s = spans[i];
                if (s.x0 > x + halfWidth) break;
                if (Overlaps(s, x, halfWidth) && Mathf.Abs(y - s.top) <= SupportTolerance && s.top > best) best = s.top;
            }
            return best;
        }

        /// <summary>Vrai si le corps est entré dans le flanc d'un sol plein (tombé dans un trou contre le bord opposé).</summary>
        public bool HitsSide(float x, float halfWidth, float y)
        {
            for (int i = 0; i < spans.Count; i++)
            {
                var s = spans[i];
                if (s.x0 > x + halfWidth) break;
                if (!s.oneWay && Overlaps(s, x, halfWidth) && y < s.top - SideTolerance) return true;
            }
            return false;
        }
    }
}
