using Kenta.FX;
using NUnit.Framework;

namespace Kenta.EditorTests
{
    public class PixelMathTests
    {
        [TestCase(1920, 1080, 6)]
        [TestCase(1366, 768, 4)]
        [TestCase(2560, 1440, 8)]
        [TestCase(300, 170, 1)]
        public void PixelMath_IntegerScale_LargestIntegerThatFits(int w, int h, int expected)
        {
            Assert.AreEqual(expected, PixelMath.IntegerScale(w, h, 320, 180));
        }

        [TestCase(991, 682, 960, 540, 0.5f, 0f)]
        [TestCase(1920, 1080, 1920, 1080, 0f, 0f)]
        [TestCase(1366, 767, 1280, 720, 0f, 0.5f)]
        public void PixelMath_CenterCorrection_AlignsOddWindows(int sw, int sh, int w, int h, float ex, float ey)
        {
            var c = PixelMath.CenterCorrection(sw, sh, w, h);
            Assert.AreEqual(ex, c.x, 1e-6f);
            Assert.AreEqual(ey, c.y, 1e-6f);
        }

        [Test]
        public void PixelMath_Split_RoundsToNearestPixel()
        {
            var (snapped, frac) = PixelMath.Split(1.013f, 40);   // 40,52 px
            Assert.AreEqual(1.025f, snapped, 1e-5f);              // 41 px
            Assert.AreEqual(-0.48f, frac, 1e-3f);
        }

        [Test]
        public void PixelMath_Split_ExactPixelHasNoFraction()
        {
            var (snapped, frac) = PixelMath.Split(-0.3f, 40);
            Assert.AreEqual(-0.3f, snapped, 1e-5f);
            Assert.AreEqual(0f, frac, 1e-4f);
        }

        [Test]
        public void PixelMath_Split_FractionInHalfOpenRange()
        {
            for (float x = -3f; x < 3f; x += 0.0071f)
            {
                var (snapped, frac) = PixelMath.Split(x, 40);
                Assert.GreaterOrEqual(frac, -0.5f - 1e-4f);
                Assert.Less(frac, 0.5f + 1e-4f);
                Assert.AreEqual(x, snapped + frac / 40f, 1e-4f);
            }
        }
    }
}
