using System.Collections.Generic;
using Kenta.Core;
using NUnit.Framework;

namespace Kenta.EditorTests
{
    /// <summary>Bonus de score de la difficulté et classement : un trigramme, une ligne, avec son héros (2026-11-26).</summary>
    public class ScoreBonusTests
    {
        [Test]
        public void HarderHeroes_EarnMorePoints()
        {
            Assert.AreEqual(1f, RetryRules.ScoreFactor(RetryMode.Checkpoint));
            Assert.AreEqual(1.5f, RetryRules.ScoreFactor(RetryMode.Restart));
            Assert.AreEqual(2f, RetryRules.ScoreFactor(RetryMode.OneLife));
            Assert.AreEqual(150, RetryRules.WithBonus(100, 1.5f));
            Assert.AreEqual(38, RetryRules.WithBonus(25, 1.5f));                 // 37,5 → 38
            Assert.AreEqual(200, RetryRules.WithBonus(100, 2f));
        }

        [Test]
        public void FactorLabel_ShowsOnlyARealBonus()
        {
            Assert.AreEqual("", RetryRules.FactorLabel(1f));
            Assert.AreEqual("×1,5", RetryRules.FactorLabel(1.5f));
            Assert.AreEqual("×2", RetryRules.FactorLabel(2f));
        }

        [Test]
        public void SameTrigram_KeepsOneLine_WithTheHeroOfItsBestScore()
        {
            var l = LevelScores.Insert(new List<LevelScoreEntry>(), "LCS", 8000, (int)RetryMode.Checkpoint, "KENTA");
            l = LevelScores.Insert(l, "LCS", 12000, (int)RetryMode.OneLife, "RYO");       // meilleur : remplace
            l = LevelScores.Insert(l, "LCS", 9000, (int)RetryMode.Restart, "AIKO");       // moins bon : ignoré
            Assert.AreEqual(1, l.Count);
            Assert.AreEqual(12000, l[0].score);
            Assert.AreEqual("RYO", l[0].hero);
            Assert.AreEqual((int)RetryMode.OneLife, l[0].difficulty);
        }

        [Test]
        public void EqualScores_HarderHeroFirst()
        {
            var l = LevelScores.Insert(new List<LevelScoreEntry>(), "AAA", 5000, (int)RetryMode.Checkpoint, "KENTA");
            l = LevelScores.Insert(l, "BBB", 5000, (int)RetryMode.OneLife, "RYO");
            Assert.AreEqual("BBB", l[0].name);
        }

        [Test]
        public void OldScores_WithoutHero_StillLoad()
        {
            var l = LevelScores.FromJson("{\"entries\":[{\"name\":\"LOU\",\"score\":300}]}");
            Assert.AreEqual(1, l.Count);
            Assert.AreEqual(0, l[0].difficulty);
            Assert.AreEqual("", l[0].hero);
        }

        [Test]
        public void HeroAndDifficulty_SurviveTheRoundTripThroughTheBorne()
        {
            var l = LevelScores.Insert(new List<LevelScoreEntry>(), "LCS", 12000, (int)RetryMode.OneLife, "RYO");
            var back = LevelScores.FromJson(LevelScores.ToJson(l));
            Assert.AreEqual("RYO", back[0].hero);
            Assert.AreEqual((int)RetryMode.OneLife, back[0].difficulty);
        }
    }
}
