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
