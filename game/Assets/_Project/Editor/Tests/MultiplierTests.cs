using Kenta.Core;
using NUnit.Framework;

namespace Kenta.EditorTests
{
    public class MultiplierTests
    {
        [TestCase(0, 1)]
        [TestCase(3, 1)]
        [TestCase(4, 2)]
        [TestCase(12, 3)]
        [TestCase(24, 4)]
        [TestCase(40, 5)]
        [TestCase(60, 6)]
        [TestCase(84, 7)]
        [TestCase(111, 7)]
        [TestCase(112, 8)]
        [TestCase(999, 8)]
        public void GrowingSteps(int combo, int expected) => Assert.AreEqual(expected, RunManager.MultiplierFor(combo, 8, 8));

        [Test]
        public void NeverAboveMax() => Assert.AreEqual(4, RunManager.MultiplierFor(10000, 8, 4));
    }
}
