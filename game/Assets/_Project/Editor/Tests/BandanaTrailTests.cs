using Kenta.FX;
using NUnit.Framework;
using UnityEngine;

namespace Kenta.EditorTests
{
    public class BandanaTrailTests
    {
        static readonly Vector2 Anchor = new(-0.18f, 0.95f);

        [Test]
        public void Anchor_IsAboveTheFeet_InNormalGravity()
        {
            var p = BandanaTrail.AnchorPoint(new Vector2(10f, 2f), Anchor, false);
            Assert.AreEqual(9.82f, p.x, 1e-4f);
            Assert.AreEqual(2.95f, p.y, 1e-4f);
        }

        [Test]
        public void Anchor_IsBelowTheFeet_WhenUpsideDown()
        {
            // au plafond, les pieds touchent le plafond et la tête pend en dessous : la traînée part de la tête
            var p = BandanaTrail.AnchorPoint(new Vector2(10f, 6.5f), Anchor, true);
            Assert.AreEqual(9.82f, p.x, 1e-4f);
            Assert.AreEqual(5.55f, p.y, 1e-4f);
        }
    }
}
