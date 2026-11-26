using Kenta.Core;
using Kenta.UI;
using NUnit.Framework;
using UnityEditor;

namespace Kenta.EditorTests
{
    public class RetryRulesTests
    {
        static readonly float[] Checkpoints = { 8f, 16f, 24f };

        // ---------- vies au départ

        [TestCase(RetryMode.Checkpoint, 0, 10)]
        [TestCase(RetryMode.Checkpoint, 15, 15)]
        [TestCase(RetryMode.Restart, 0, 3)]
        [TestCase(RetryMode.Restart, 5, 5)]
        [TestCase(RetryMode.OneLife, 0, 1)]
        [TestCase(RetryMode.OneLife, 10, 1)]
        [TestCase(RetryMode.Checkpoint, -3, 10)]
        public void StartLives_HeroValueOrModeDefault(RetryMode mode, int heroLives, int expected) =>
            Assert.AreEqual(expected, RetryRules.StartLives(mode, heroLives));

        [Test]
        public void DefaultLives_TenThreeOne()
        {
            Assert.AreEqual(10, RetryRules.DefaultLives(RetryMode.Checkpoint));
            Assert.AreEqual(3, RetryRules.DefaultLives(RetryMode.Restart));
            Assert.AreEqual(1, RetryRules.DefaultLives(RetryMode.OneLife));
        }

        // ---------- point de reprise

        [TestCase(20.0, 16.0)]
        [TestCase(16.0, 16.0)]
        [TestCase(30.0, 24.0)]
        [TestCase(5.0, 0.0)]
        public void CheckpointBefore_LastPointAtOrBeforeDeath(double death, double expected) =>
            Assert.AreEqual(expected, RetryRules.CheckpointBefore(Checkpoints, death), 1e-9);

        [Test]
        public void CheckpointBefore_UnsortedAndMissing()
        {
            Assert.AreEqual(24.0, RetryRules.CheckpointBefore(new[] { 24f, 8f, 16f }, 30.0), 1e-9);
            Assert.AreEqual(0.0, RetryRules.CheckpointBefore(null, 30.0), 1e-9);
            Assert.AreEqual(0.0, RetryRules.CheckpointBefore(new float[0], 30.0), 1e-9);
        }

        // ---------- suite d'une mort

        [Test]
        public void Checkpoint_ResumesAtLastPoint()
        {
            var d = RetryRules.Decide(RetryMode.Checkpoint, 9, 20.0, Checkpoints, 0.0);
            Assert.AreEqual(RetryAction.Resume, d.action);
            Assert.AreEqual(16.0, d.beat, 1e-9);
        }

        [Test]
        public void Restart_GoesBackToStartBeat_IgnoringCheckpoints()
        {
            var d = RetryRules.Decide(RetryMode.Restart, 2, 50.0, Checkpoints, 0.0);
            Assert.AreEqual(RetryAction.Restart, d.action);
            Assert.AreEqual(0.0, d.beat, 1e-9);
        }

        [Test]
        public void Restart_UsesRunStartBeat_ForEditorPassageTests()
        {
            var d = RetryRules.Decide(RetryMode.Restart, 1, 50.0, Checkpoints, 32.0);
            Assert.AreEqual(RetryAction.Restart, d.action);
            Assert.AreEqual(32.0, d.beat, 1e-9);
        }

        [TestCase(RetryMode.Checkpoint)]
        [TestCase(RetryMode.Restart)]
        [TestCase(RetryMode.OneLife)]
        public void NoLifeLeft_IsGameOver(RetryMode mode) =>
            Assert.AreEqual(RetryAction.GameOver, RetryRules.Decide(mode, 0, 20.0, Checkpoints, 0.0).action);

        [Test]
        public void OneLife_FirstDeathEndsTheRun_EvenWithLivesLeft() =>
            Assert.AreEqual(RetryAction.GameOver, RetryRules.Decide(RetryMode.OneLife, 4, 20.0, Checkpoints, 0.0).action);

        /// <summary>Comme RunManager.Damage : une vie de moins à chaque mort, jusqu'à la fin de partie.</summary>
        static int DeathsUntilGameOver(RetryMode mode, int heroLives)
        {
            int lives = RetryRules.StartLives(mode, heroLives);
            for (int deaths = 1; deaths < 100; deaths++)
            {
                lives = System.Math.Max(0, lives - 1);
                if (RetryRules.Decide(mode, lives, 20.0, Checkpoints, 0.0).action == RetryAction.GameOver) return deaths;
            }
            return -1;
        }

        [TestCase(RetryMode.Checkpoint, 0, 10)]
        [TestCase(RetryMode.Restart, 0, 3)]
        [TestCase(RetryMode.OneLife, 0, 1)]
        [TestCase(RetryMode.Restart, 5, 5)]
        public void EachLifeIsOneTry(RetryMode mode, int heroLives, int expectedDeaths) =>
            Assert.AreEqual(expectedDeaths, DeathsUntilGameOver(mode, heroLives));

        // ---------- meilleur essai (reprise au début)

        [Test]
        public void KeepEarlier_OnlyWhenStrictlyBetter()
        {
            Assert.IsTrue(RetryRules.KeepEarlier(true, 5000, 3000));
            Assert.IsFalse(RetryRules.KeepEarlier(true, 3000, 5000));
            Assert.IsFalse(RetryRules.KeepEarlier(true, 4000, 4000));     // égalité : le dernier essai compte
            Assert.IsFalse(RetryRules.KeepEarlier(false, 9999, 0));        // aucun essai précédent
        }

        // ---------- textes de l'écran du héros

        [Test]
        public void Labels_EasyMediumHard()
        {
            Assert.AreEqual("FACILE", RetryRules.DifficultyLabel(RetryMode.Checkpoint));
            Assert.AreEqual("MOYEN", RetryRules.DifficultyLabel(RetryMode.Restart));
            Assert.AreEqual("DUR", RetryRules.DifficultyLabel(RetryMode.OneLife));
        }

        [Test]
        public void RuleLine_ShowsTheCount()
        {
            StringAssert.StartsWith("10 VIES ·", RetryRules.RuleLine(RetryMode.Checkpoint, 10));
            StringAssert.StartsWith("1 VIE ·", RetryRules.RuleLine(RetryMode.Checkpoint, 1));
            StringAssert.StartsWith("3 ESSAIS ·", RetryRules.RuleLine(RetryMode.Restart, 3));
            StringAssert.Contains("DÉBUT", RetryRules.RuleLine(RetryMode.Restart, 3));
            StringAssert.Contains("UNE SEULE VIE", RetryRules.RuleLine(RetryMode.OneLife, 1));
        }

        /// <summary>Tous les textes existent dans la police pixel (pas de minuscules) et tiennent à l'écran (320 px, cadre de 84 px).</summary>
        [Test]
        public void Texts_UseFontGlyphs_AndFit()
        {
            var font = AssetDatabase.LoadAssetAtPath<PixelFontData>("Assets/_Project/ScriptableObjects/PixelFont.asset");
            if (font == null || string.IsNullOrEmpty(font.chars)) Assert.Inconclusive("PixelFont.asset absent : menu Kenta > Reconstruire.");
            foreach (RetryMode mode in System.Enum.GetValues(typeof(RetryMode)))
            {
                string word = RetryRules.DifficultyLabel(mode);
                string rule = RetryRules.RuleLine(mode, RetryRules.StartLives(mode, 0));
                foreach (var text in new[] { word, rule })
                    foreach (char c in text)
                        Assert.IsTrue(c == ' ' || font.chars.IndexOf(c) >= 0, $"« {c} » manque dans la police ({text})");
                Assert.LessOrEqual(PixelText.PlateSize(font, word, 1).x, 80f, word);
                Assert.LessOrEqual(PixelText.PlateSize(font, rule, 1).x, 300f, rule);
            }
            foreach (char c in ExplainScreen.FooterText)
                Assert.IsTrue(c == ' ' || font.chars.IndexOf(c) >= 0, $"« {c} » manque dans la police (consignes)");
        }
    }
}
