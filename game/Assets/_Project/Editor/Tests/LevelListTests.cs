using Kenta.UI;
using NUnit.Framework;
using UnityEngine;

namespace Kenta.EditorTests
{
    public class LevelListTests
    {
        [TestCase(-1, 8, 7)]
        [TestCase(8, 8, 0)]
        [TestCase(3, 8, 3)]
        [TestCase(-9, 8, 7)]
        public void Wrap(int index, int count, int expected) => Assert.AreEqual(expected, LevelListLayout.Wrap(index, count));

        [TestCase(7, 0f, 8, -1f)]
        [TestCase(1, 0f, 8, 1f)]
        [TestCase(4, 0f, 8, -4f)]
        [TestCase(0, 7.5f, 8, 0.5f)]
        [TestCase(2, 10f, 8, 0f)]
        public void Offset_IsShortestCircularDistance(int index, float scroll, int count, float expected) =>
            Assert.AreEqual(expected, LevelListLayout.Offset(index, scroll, count), 1e-4f);

        [Test]
        public void CardRect_Selected() => Assert.AreEqual(new RectInt(-18, 0, 174, 28), LevelListLayout.CardRect(0f));

        [Test]
        public void CardRect_Next() => Assert.AreEqual(new RectInt(-12, -29, 168, 26), LevelListLayout.CardRect(1f));

        [Test]
        public void CardRect_TwoAbove() => Assert.AreEqual(58, LevelListLayout.CardRect(-2f).y);

        [TestCase(150f, 103.4f, "150 BPM  1:43")]
        [TestCase(120f, 59.9f, "120 BPM  0:59")]
        public void FormatInfo(float bpm, float seconds, string expected) =>
            Assert.AreEqual(expected, LevelListLayout.FormatInfo(bpm, seconds));

        [Test]
        public void StickRepeat_FirstPushThenDelayThenInterval()
        {
            var r = new StickRepeat(0.4f, 0.15f);
            Assert.AreEqual(1, r.Step(1f, 0.016f));
            int steps = 0;
            for (int i = 0; i < 23; i++) steps += r.Step(1f, 0.016f);   // ~0,37 s : pas encore de répétition
            Assert.AreEqual(0, steps);
            for (int i = 0; i < 4; i++) steps += r.Step(1f, 0.016f);    // passe 0,4 s
            Assert.AreEqual(1, steps);
            for (int i = 0; i < 10; i++) steps += r.Step(1f, 0.016f);   // +0,16 s : une répétition de plus
            Assert.AreEqual(2, steps);
        }

        [Test]
        public void StickRepeat_ReleaseRearms()
        {
            var r = new StickRepeat();
            Assert.AreEqual(1, r.Step(1f, 0.016f));
            Assert.AreEqual(0, r.Step(0f, 0.016f));
            Assert.AreEqual(1, r.Step(1f, 0.016f));
        }

        [Test]
        public void StickRepeat_DownAndDeadZone()
        {
            Assert.AreEqual(-1, new StickRepeat().Step(-1f, 0.016f));
            Assert.AreEqual(0, new StickRepeat().Step(0.3f, 0.016f));
        }
    }
}
