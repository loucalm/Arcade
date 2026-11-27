using Kenta.FX;
using NUnit.Framework;

namespace Kenta.EditorTests
{
    public class CorruptionCurveTests
    {
        [Test]
        public void Empty_IsZero()
        {
            Assert.AreEqual(0f, new CorruptionCurve().Evaluate(100));
        }

        [Test]
        public void Ramp_GoesFromZeroToTarget()
        {
            var c = new CorruptionCurve();
            c.Add(48, 0.6f, 16);
            Assert.AreEqual(0f, c.Evaluate(40), 1e-5f);
            Assert.AreEqual(0.3f, c.Evaluate(56), 1e-5f);
            Assert.AreEqual(0.6f, c.Evaluate(64), 1e-5f);
            Assert.AreEqual(0.6f, c.Evaluate(200), 1e-5f);
        }

        [Test]
        public void InterruptedRamp_StartsFromTheCurrentValue()
        {
            var c = new CorruptionCurve();
            c.Add(64, 1f, 4);       // ajoutée avant l'autre : l'ordre d'ajout ne compte pas
            c.Add(48, 0.8f, 32);    // à 64, la rampe en est à 0,4
            Assert.AreEqual(0.4f, c.Evaluate(63.999), 1e-3f);
            Assert.AreEqual(0.7f, c.Evaluate(66), 1e-4f);
            Assert.AreEqual(1f, c.Evaluate(68), 1e-5f);
        }

        [Test]
        public void ZeroLength_IsACut()
        {
            var c = new CorruptionCurve();
            c.Add(10, 1f, 0);
            c.Add(20, 0f, 0);
            Assert.AreEqual(1f, c.Evaluate(10));
            Assert.AreEqual(1f, c.Evaluate(19.9));
            Assert.AreEqual(0f, c.Evaluate(20));
        }
    }
}
