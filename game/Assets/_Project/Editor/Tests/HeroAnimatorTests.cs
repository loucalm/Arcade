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

        [TestCase(-0.01, -1)]      // pas encore frappé
        [TestCase(0.0, 0)]         // début de l'arc
        [TestCase(0.1, 1)]         // arc complet, l'image gelée
        [TestCase(0.24, 1)]
        [TestCase(0.3, 2)]         // fin de l'arc
        [TestCase(0.4, 3)]         // bras tendu
        [TestCase(0.45, -1)]       // fini : retour à la course
        [TestCase(99.0, -1)]       // aucun appui depuis le départ
        public void HitFrameIndex_FollowsTimeSincePress(double age, int expected)
        {
            Assert.AreEqual(expected, HeroAnimator.HitFrameIndex(age, 0.45f, 4));
        }

        [Test]
        public void HitFrameIndex_NoFramesGivesMinusOne()
        {
            Assert.AreEqual(-1, HeroAnimator.HitFrameIndex(0.1, 0.45f, 0));
        }

        [TestCase(-1f, -1)]        // au sol ou en chute
        [TestCase(0f, 0)]          // impulsion
        [TestCase(0.13f, 1)]       // genoux remontés
        [TestCase(0.5f, 4)]        // vrille, tête en bas au sommet
        [TestCase(0.99f, 7)]       // réception
        [TestCase(1f, 7)]
        public void JumpFrameIndex_SpreadsFramesOverTheJump(float progress, int expected)
        {
            Assert.AreEqual(expected, HeroAnimator.JumpFrameIndex(progress, 8));
        }

        [TestCase(-0.01f, -1)]     // pas encore commencée
        [TestCase(0f, 0)]
        [TestCase(0.05f, 1)]       // 3 images en 0,12 s : 0,04 s chacune
        [TestCase(0.11f, 2)]
        [TestCase(0.12f, -1)]      // finie : on passe à la boucle (ou à la course)
        public void TransitionFrameIndex_PlaysOnceThenStops(float elapsed, int expected)
        {
            Assert.AreEqual(expected, HeroAnimator.TransitionFrameIndex(elapsed, 0.12f, 3));
        }

        [Test]
        public void JumpFrameIndex_NoFramesGivesMinusOne()
        {
            Assert.AreEqual(-1, HeroAnimator.JumpFrameIndex(0.5f, 0));
        }
    }
}
