using Kenta.Core;
using Kenta.UI;
using NUnit.Framework;
using UnityEngine;

namespace Kenta.EditorTests
{
    /// <summary>Grille et calculs du HUD en partie (UI/HudLayout).</summary>
    public class HudLayoutTests
    {
        [Test]
        public void Grid_PlateIs14PixelsAndFitsTheTopBand()
        {
            Assert.AreEqual(14, HudLayout.PlateHeight);                      // 1 + 2 + 8 + 2 + 1
            Assert.AreEqual(90 - 4 - 14, HudLayout.BandBottom);
            Assert.AreEqual(HudLayout.Frame + HudLayout.Pad, HudLayout.Inset);
        }

        [Test]
        public void DigitsWidth_ColumnsOf6WithoutTrailingGap()
        {
            Assert.AreEqual(0, HudLayout.DigitsWidth(0));
            Assert.AreEqual(5, HudLayout.DigitsWidth(1));
            Assert.AreEqual(35, HudLayout.DigitsWidth(6));
        }

        [TestCase(0, 1)]
        [TestCase(9, 1)]
        [TestCase(10, 2)]
        [TestCase(122, 3)]
        [TestCase(1000, 4)]
        public void DigitCount(int value, int expected) => Assert.AreEqual(expected, HudLayout.DigitCount(value));

        [Test]
        public void DigitAt_ReadsFromTheRight()
        {
            Assert.AreEqual(0, HudLayout.DigitAt(120, 0));
            Assert.AreEqual(2, HudLayout.DigitAt(120, 1));
            Assert.AreEqual(1, HudLayout.DigitAt(120, 2));
            Assert.AreEqual(0, HudLayout.DigitAt(120, 3));
        }

        [Test]
        public void ScorePlate_ScoreGapMultiplier_AndBestAddsAGroup()
        {
            // 000000 (35) + 4 + « × » (6) + 1 chiffre (5) = 50 ; plaque = 50 + 2 × 3
            Assert.AreEqual(50, HudLayout.ScoreContentWidth(6, 1, false));
            Assert.AreEqual(56, HudLayout.PlateWidth(HudLayout.ScoreContentWidth(6, 1, false)));
            // + 4 + couronne (7) + 2 + 000000 (35)
            Assert.AreEqual(50 + 4 + 7 + 2 + 35, HudLayout.ScoreContentWidth(6, 1, true));
        }

        [Test]
        public void Lives_DependOnTheHeroDifficulty()
        {
            Assert.AreEqual(7 + 2 + 11, HudLayout.LivesWidth(RetryMode.Checkpoint, 10, 0));    // cœur, 2 px, « 10 »
            Assert.AreEqual(7 + 2 + 5, HudLayout.LivesWidth(RetryMode.Checkpoint, 9, 0));
            Assert.AreEqual(7, HudLayout.LivesWidth(RetryMode.OneLife, 1, 0));                 // un cœur seul
            Assert.AreEqual(48, HudLayout.LivesWidth(RetryMode.Restart, 3, 48));               // « ESSAI 2/3 »
        }

        [Test]
        public void StatsPlate_LivesGapCoins()
        {
            // pièces : icône 7 + 2 + « 000/000 » (7 colonnes = 41)
            Assert.AreEqual(50, HudLayout.CoinsWidth(3));
            Assert.AreEqual(20 + 4 + 50, HudLayout.StatsContentWidth(RetryMode.Checkpoint, 10, 0, 3));
        }

        [Test]
        public void CenterPlate_FillsTheSpaceWith4PixelGaps()
        {
            int left = 56, right = 80;
            int center = HudLayout.CenterPlateWidth(left, right);
            Assert.AreEqual(320, HudLayout.Margin + left + HudLayout.Gap + center + HudLayout.Gap + right + HudLayout.Margin);
        }

        [Test]
        public void DeathMessage_NeverEmpty_AndSilenceAtTheEnd()
        {
            Assert.AreEqual("ENCORE !", HudLayout.DeathMessage(RetryMode.Checkpoint, 9));
            Assert.AreEqual("DU DÉBUT !", HudLayout.DeathMessage(RetryMode.Restart, 2));
            Assert.AreEqual("SILENCE...", HudLayout.DeathMessage(RetryMode.Restart, 0));
            Assert.AreEqual("SILENCE...", HudLayout.DeathMessage(RetryMode.OneLife, 0));
            Assert.AreEqual("SILENCE...", HudLayout.DeathMessage(RetryMode.Checkpoint, 0));
        }

        [Test]
        public void FriezeSegments_ProportionalContiguousAndExactWidth()
        {
            // niveau 1 : intro 32 temps, couplet 32, refrain 32, couplet 2 32, fin le reste (≈ 1/5 chacun)
            var starts = new[] { 0f, 0.2f, 0.4f, 0.6f, 0.8f };
            var x = new int[5];
            var w = new int[5];
            HudLayout.FriezeSegments(starts, 5, 100, 1, x, w);
            Assert.AreEqual(0, x[0]);
            Assert.AreEqual(100, x[4] + w[4]);                                // se termine au bord
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(x[i] + w[i] + 1, x[i + 1], $"écart d'1 px après le segment {i}");
                Assert.AreEqual(19, w[i]);
            }
        }

        [Test]
        public void FriezeSegments_TinySectionsKeepAtLeastOnePixel()
        {
            var starts = new[] { 0f, 0.001f, 0.002f, 0.003f, 0.5f, 0.999f };
            var x = new int[6];
            var w = new int[6];
            HudLayout.FriezeSegments(starts, 6, 40, 1, x, w);
            int end = -1;
            for (int i = 0; i < 6; i++)
            {
                Assert.GreaterOrEqual(w[i], 1);
                Assert.Greater(x[i], end, $"segment {i} chevauche le précédent");
                end = x[i] + w[i] - 1;
            }
            Assert.AreEqual(40, x[5] + w[5]);
        }

        [Test]
        public void FillPixels_IntegersFromEmptyToFull()
        {
            Assert.AreEqual(0, HudLayout.FillPixels(0.1f, 0.2f, 0.4f, 20));
            Assert.AreEqual(10, HudLayout.FillPixels(0.3f, 0.2f, 0.4f, 20));
            Assert.AreEqual(20, HudLayout.FillPixels(0.4f, 0.2f, 0.4f, 20));
            Assert.AreEqual(20, HudLayout.FillPixels(0.9f, 0.2f, 0.4f, 20));
        }

        [Test]
        public void RollScore_ReachesTheTargetWithoutOvershoot()
        {
            float shown = 0f;
            float last = 0f;
            for (int i = 0; i < 120; i++)                                       // 2 s à 60 i/s
            {
                shown = HudLayout.RollScore(shown, 5000f, 1f / 60f);
                Assert.GreaterOrEqual(shown, last);
                Assert.LessOrEqual(shown, 5000f);
                last = shown;
            }
            Assert.AreEqual(5000f, shown);
            Assert.AreEqual(0f, HudLayout.RollScore(40f, 0f, 1f));                // nouvel essai : retombe à 0
        }

        [Test]
        public void Bounce_WholePixelsUpAndBack()
        {
            Assert.AreEqual(0, HudLayout.Bounce(0f, 0.25f, 2));
            Assert.AreEqual(2, HudLayout.Bounce(0.125f, 0.25f, 2));
            Assert.AreEqual(0, HudLayout.Bounce(0.25f, 0.25f, 2));
            Assert.AreEqual(0, HudLayout.Bounce(99f, 0.25f, 2));
            for (float t = 0f; t < 0.25f; t += 0.01f)
            {
                int b = HudLayout.Bounce(t, 0.25f, 2);
                Assert.That(b, Is.InRange(0, 2));
            }
        }

        [Test]
        public void WorldToUi_ScalesWithTheCameraZoom()
        {
            // vue de 480 px (×4 sur la borne) : 1 u = 40 px de monde = 40 × 320/480 px d'interface
            var ui = HudLayout.WorldToUi(new Vector2(13f, 5f), new Vector2(10f, 5f), 480, 40);
            Assert.AreEqual(80f, ui.x, 1e-4f);
            Assert.AreEqual(0f, ui.y, 1e-4f);
        }
    }
}
