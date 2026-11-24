using Kenta.FX;
using NUnit.Framework;

namespace Kenta.EditorTests
{
    public class SourdineTests
    {
        const float HeroX = 70f;          // cadrage par défaut : 0,22 × 320 px

        [Test]
        public void TargetEdge_NoComboIsClosest() => Assert.AreEqual(44f, Sourdine.TargetEdge(1, HeroX));

        [Test]
        public void TargetEdge_MaxComboPushesItBack() => Assert.AreEqual(-12f, Sourdine.TargetEdge(8, HeroX));

        [Test]
        public void TargetEdge_RecedesAsMultiplierGrows()
        {
            for (int m = 1; m < 8; m++)
                Assert.Greater(Sourdine.TargetEdge(m, HeroX), Sourdine.TargetEdge(m + 1, HeroX));
        }

        [Test]
        public void TargetEdge_IsClamped()
        {
            Assert.LessOrEqual(Sourdine.TargetEdge(0, 400f), 140f);
            Assert.GreaterOrEqual(Sourdine.TargetEdge(99, 0f), -60f);
        }

        [Test]
        public void Veil_NeverCoversTheHero()
        {
            for (int m = 1; m <= 8; m++)
            {
                float edge = Sourdine.TargetEdge(m, HeroX);
                Assert.LessOrEqual(Sourdine.VeilLeft(edge, HeroX) + 56f, HeroX - 8f, $"×{m}");
                Assert.LessOrEqual(edge, HeroX - 8f, $"×{m}");
            }
        }
    }
}
