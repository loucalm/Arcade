using Kenta.Player;
using NUnit.Framework;

namespace Kenta.EditorTests
{
    public class HeroAnimatorTests
    {
        [TestCase(0f, 0)]
        [TestCase(0.27f, 1)]       // 1,6 u / 6 images = 0,2667 u par image
        [TestCase(1.59f, 5)]
        [TestCase(1.6f, 0)]        // une foulée complète
        [TestCase(3.5f, 1)]        // 2 foulées + 0,3 u
        [TestCase(-0.1f, 5)]       // avant le beat 0 : on boucle sans index négatif
        public void RunFrameIndex_FollowsDistance(float distance, int expected)
        {
            Assert.AreEqual(expected, HeroAnimator.RunFrameIndex(distance, 1.6f, 6));
        }

        [Test]
        public void RunFrameIndex_NoFramesGivesZero()
        {
            Assert.AreEqual(0, HeroAnimator.RunFrameIndex(12f, 1.6f, 0));
        }
    }
}
