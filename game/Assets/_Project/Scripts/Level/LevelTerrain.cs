using System.Collections.Generic;
using UnityEngine;

namespace Kenta.Level
{
    /// <summary>
    /// Segment de sol : de x0 à x1, dessus à `top` en x0 et `top1` en x1 (pente : rampe ou course sur un mur).
    /// Une plateforme (`oneWay`) se traverse par en dessous.
    /// </summary>
    public struct GroundSpan
    {
        public float x0, x1, top, top1;
        public bool oneWay;

        public GroundSpan(float x0, float x1, float top, bool oneWay) : this(x0, x1, top, top, oneWay) { }

        public GroundSpan(float x0, float x1, float top, float top1, bool oneWay)
        {
            this.x0 = x0;
            this.x1 = x1;
            this.top = top;
            this.top1 = top1;
            this.oneWay = oneWay;
        }

        public bool Sloped => Mathf.Abs(top1 - top) > 0.0001f;
        public float Slope => x1 > x0 ? (top1 - top) / (x1 - x0) : 0f;

        /// <summary>Dessus du sol en x (borné au segment).</summary>
        public float TopAt(float x) => Sloped ? Mathf.Lerp(top, top1, Mathf.InverseLerp(x0, x1, x)) : top;
        public float MaxTop => Mathf.Max(top, top1);
        public float MinTop => Mathf.Min(top, top1);
    }

    /// <summary>
    /// Sol du niveau à hauteur libre (marches, trous, plateformes, rampes), lu par le joueur et la caméra.
    /// Collisions à la main (ADR-009) : atterrissage balayé entre deux images, appui sous les pieds, chocs de côté.
    /// </summary>
    public class LevelTerrain
    {
        const float SupportTolerance = 0.02f;
        /// <summary>Sur une pente, les pieds suivent le sol s'il est à moins de cette marge (montée ou descente d'une image).</summary>
        const float SlopeTolerance = 0.45f;
        /// <summary>Sous le dessus d'un sol de plus de cette marge, on le cogne de côté (sinon on remonte encore dessus).</summary>
        const float SideTolerance = 0.4f;
        /// <summary>Tomber de plus que ça sous le sol le plus bas autour d'un trou = mort.</summary>
        public const float FallDeath = 2.5f;

        readonly List<GroundSpan> spans = new();
        public IReadOnlyList<GroundSpan> Spans => spans;
        public float LowestTop { get; private set; }
        public float HighestTop { get; private set; }

        public void Clear()
        {
            spans.Clear();
            LowestTop = HighestTop = 0f;
        }

        public void Add(GroundSpan s)
        {
            if (s.x1 > s.x0) spans.Add(s);
        }

        public void Build()
        {
            spans.Sort((a, b) => a.x0.CompareTo(b.x0));
            LowestTop = float.PositiveInfinity;
            HighestTop = float.NegativeInfinity;
            foreach (var s in spans)
            {
                LowestTop = Mathf.Min(LowestTop, s.MinTop);
                HighestTop = Mathf.Max(HighestTop, s.MaxTop);
            }
            if (float.IsInfinity(LowestTop)) LowestTop = HighestTop = 0f;
        }

        static bool Overlaps(in GroundSpan s, float x, float halfWidth) => x + halfWidth > s.x0 && x - halfWidth < s.x1;

        /// <summary>Dessus du segment sous les pieds (centre en x ; sinon le bord le plus proche couvert par le corps).</summary>
        static float TopUnder(in GroundSpan s, float x) => s.TopAt(Mathf.Clamp(x, s.x0, s.x1));

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
                float t = TopUnder(s, x);
                float tol = s.Sloped ? SlopeTolerance : SupportTolerance;
                if (yPrev >= t - tol && yNow <= t && t > top)
                {
                    top = t;
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
                if (!Overlaps(s, x, halfWidth)) continue;
                float t = TopUnder(s, x);
                // une pente se suit (monte ou descend d'une image) ; un sol plat doit être pile sous les pieds
                bool sloped = s.Sloped || IsSlopeJoint(i, x, halfWidth);
                float tol = sloped ? SlopeTolerance : SupportTolerance;
                if (Mathf.Abs(y - t) <= tol && t > best) best = t;
            }
            return best;
        }

        /// <summary>Vrai si un segment voisin (qui touche celui-ci) est en pente : on suit la jonction rampe/plat.</summary>
        bool IsSlopeJoint(int i, float x, float halfWidth)
        {
            if (i > 0 && spans[i - 1].Sloped && Overlaps(spans[i - 1], x, halfWidth + 0.3f)) return true;
            if (i + 1 < spans.Count && spans[i + 1].Sloped && Overlaps(spans[i + 1], x, halfWidth + 0.3f)) return true;
            return false;
        }

        /// <summary>Vrai si le corps est entré dans le flanc d'un sol plein (tombé dans un trou contre le bord opposé).</summary>
        public bool HitsSide(float x, float halfWidth, float y)
        {
            for (int i = 0; i < spans.Count; i++)
            {
                var s = spans[i];
                if (s.x0 > x + halfWidth) break;
                if (!s.oneWay && Overlaps(s, x, halfWidth) && y < TopUnder(s, x) - SideTolerance) return true;
            }
            return false;
        }

        /// <summary>
        /// Hauteur sous laquelle on meurt en x : sous un sol plein, son dessus − FallDeath ; au-dessus d'un trou, le plus bas
        /// des deux bords − FallDeath. Une grande chute vers un sol plus bas (marche descendante) n'est donc pas mortelle.
        /// </summary>
        public float KillY(float x)
        {
            float left = float.NaN, right = float.NaN, under = float.PositiveInfinity;
            for (int i = 0; i < spans.Count; i++)
            {
                var s = spans[i];
                if (s.oneWay) continue;
                if (s.x1 <= x) left = s.top1;
                else if (s.x0 >= x) { right = s.top; break; }
                else under = Mathf.Min(under, s.TopAt(x));
            }
            if (!float.IsPositiveInfinity(under)) return under - FallDeath;
            float lowest = float.PositiveInfinity;
            if (!float.IsNaN(left)) lowest = Mathf.Min(lowest, left);
            if (!float.IsNaN(right)) lowest = Mathf.Min(lowest, right);
            return (float.IsPositiveInfinity(lowest) ? LowestTop : lowest) - FallDeath;
        }
    }
}
