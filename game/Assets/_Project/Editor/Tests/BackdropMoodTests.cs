using Kenta.FX;
using NUnit.Framework;
using UnityEngine;

namespace Kenta.EditorTests
{
    public class BackdropMoodTests
    {
        [Test]
        public void IntroIsNeutralAtStart() => Assert.AreEqual(Color.white, BiomeBackdrop.MoodFor("intro", 0f));

        [Test]
        public void RefrainIsWarmerThanPont()
        {
            var refrain = BiomeBackdrop.MoodFor("refrain", 0f);
            var pont = BiomeBackdrop.MoodFor("pont", 0f);
            Assert.Greater(refrain.r - refrain.b, pont.r - pont.b);
        }

        [Test]
        public void LightFallsAlongTheSong()
        {
            var early = BiomeBackdrop.MoodFor("couplet", 0f);
            var late = BiomeBackdrop.MoodFor("couplet 2", 1f);
            Assert.Greater(early.grayscale, late.grayscale);
        }

        [Test]
        public void SunStartsHighAndSetsByNight()
        {
            Assert.AreEqual(0f, BiomeBackdrop.SunsetCurve(0f), 1e-5f);
            Assert.AreEqual(1f, BiomeBackdrop.SunsetCurve(1f), 1e-5f);
            Assert.AreEqual(1f, BiomeBackdrop.SunsetCurve(3f), 1e-5f);      // après la nuit : il reste couché
            float last = -1f;
            for (int i = 0; i <= 20; i++)                                     // il ne remonte jamais
            {
                float v = BiomeBackdrop.SunsetCurve(i / 20f);
                Assert.GreaterOrEqual(v, last);
                last = v;
            }
            Assert.Less(BiomeBackdrop.SunsetCurve(0.5f), 0.5f);               // lent au début, il plonge vers la fin
        }

        [Test]
        public void FinIsNight()
        {
            var fin = BiomeBackdrop.MoodFor("fin", 1f);
            var couplet = BiomeBackdrop.MoodFor("couplet", 1f);
            Assert.Less(fin.grayscale, couplet.grayscale);
            Assert.Greater(fin.b, fin.r);                                    // bleutée
        }

        [Test]
        public void NeverBrightensAboveTheSprite()
        {
            foreach (var n in new[] { "intro", "couplet", "pont", "roulement", "refrain", "final", "fin", "inconnu" })
            {
                var c = BiomeBackdrop.MoodFor(n, 0f);
                Assert.LessOrEqual(Mathf.Max(c.r, c.g, c.b), 1f, n);
            }
        }
    }
}
