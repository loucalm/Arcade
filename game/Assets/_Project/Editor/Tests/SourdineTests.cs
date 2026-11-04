using Kenta.FX;
using NUnit.Framework;

namespace Kenta.EditorTests
{
    public class SourdineTests
    {
        [Test]
        public void TargetEdge_NoComboIsClosest() => Assert.AreEqual(45f, Sourdine.TargetEdge(1));

        [Test]
        public void TargetEdge_MaxComboPushesItBack() => Assert.AreEqual(10f, Sourdine.TargetEdge(8));

        [Test]
        public void TargetEdge_IsClamped()
        {
            Assert.LessOrEqual(Sourdine.TargetEdge(0), 140f);
            Assert.GreaterOrEqual(Sourdine.TargetEdge(99), 4f);
        }
    }
}
