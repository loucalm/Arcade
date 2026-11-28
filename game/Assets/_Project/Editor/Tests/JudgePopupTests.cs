using Kenta.UI;
using NUnit.Framework;
using UnityEngine;

namespace Kenta.Tests
{
    public class JudgePopupTests
    {
        [Test]
        public void SingleScreen_MatchesStandardWorldToUi()
        {
            var cam = new Vector2(10f, 2f);
            var world = new Vector2(7.6f, 3.2f);
            Vector2 expected = HudLayout.WorldToUi(world, cam, 480, 40);
            Vector2 actual = HudLayout.WorldToUiQuad(world, cam, 480, 40, false, false, 1, 0, out Rect bounds);
            Assert.AreEqual(expected.x, actual.x, 1e-4f);
            Assert.AreEqual(expected.y, actual.y, 1e-4f);
            Assert.AreEqual(320f, bounds.width, 1e-4f);
            Assert.AreEqual(180f, bounds.height, 1e-4f);
        }

        [Test]
        public void MirrorAndUpsideDown_FlipCoordinatesCleanly()
        {
            var cam = new Vector2(10f, 2f);
            var world = new Vector2(7f, 0.5f); // gauche et bas en vue normale
            Vector2 normal = HudLayout.WorldToUiQuad(world, cam, 480, 40, false, false, 1, 0, out _);
            Vector2 mirror = HudLayout.WorldToUiQuad(world, cam, 480, 40, true, false, 1, 0, out _);
            Vector2 upside = HudLayout.WorldToUiQuad(world, cam, 480, 40, false, true, 1, 0, out _);
            Vector2 both = HudLayout.WorldToUiQuad(world, cam, 480, 40, true, true, 1, 0, out _);

            Assert.Less(normal.x, 0f);
            Assert.Greater(mirror.x, 0f);
            Assert.AreEqual(-normal.x, mirror.x, 1e-4f);
            Assert.AreEqual(normal.y, mirror.y, 1e-4f);

            Assert.Less(normal.y, 0f);
            Assert.Greater(upside.y, 0f);
            Assert.AreEqual(-normal.y, upside.y, 1e-4f);

            Assert.AreEqual(-normal.x, both.x, 1e-4f);
            Assert.AreEqual(-normal.y, both.y, 1e-4f);
        }

        [Test]
        public void Split2x2_ProjectsIntoAllFourQuadrants()
        {
            Assert.AreEqual(4, HudLayout.ActiveQuadCount(4));
            var cam = new Vector2(20f, 5f);
            var world = new Vector2(20f, 5f); // pile au centre caméra -> au centre de chacun des 4 cadrans
            Vector2 q0 = HudLayout.WorldToUiQuad(world, cam, 480, 40, false, false, 4, 0, out Rect b0);
            Vector2 q1 = HudLayout.WorldToUiQuad(world, cam, 480, 40, false, false, 4, 1, out Rect b1);
            Vector2 q2 = HudLayout.WorldToUiQuad(world, cam, 480, 40, false, false, 4, 2, out Rect b2);
            Vector2 q3 = HudLayout.WorldToUiQuad(world, cam, 480, 40, false, false, 4, 3, out Rect b3);

            Assert.AreEqual(new Vector2(-80f, 45f), q0);
            Assert.AreEqual(new Vector2(80f, 45f), q1);
            Assert.AreEqual(new Vector2(-80f, -45f), q2);
            Assert.AreEqual(new Vector2(80f, -45f), q3);
            Assert.AreEqual(160f, b0.width, 1e-4f);
            Assert.AreEqual(90f, b0.height, 1e-4f);
            Assert.IsTrue(b1.Contains(q1));
            Assert.IsTrue(b2.Contains(q2));
            Assert.IsTrue(b3.Contains(q3));
        }
    }
}
