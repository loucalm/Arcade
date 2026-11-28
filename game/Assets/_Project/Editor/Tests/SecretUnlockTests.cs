using Kenta.Core;
using Kenta.Level;
using Kenta.Rhythm;
using NUnit.Framework;
using UnityEngine;

namespace Kenta.Tests
{
    public class SecretUnlockTests
    {
        LevelDef sourd, secret, sylve;

        [SetUp]
        public void SetUp()
        {
            LevelScores.ResetForTests();
            sourd = ScriptableObject.CreateInstance<LevelDef>();
            sourd.id = "sourd";
            sourd.number = 9;

            secret = ScriptableObject.CreateInstance<LevelDef>();
            secret.id = "secret";
            secret.number = 10;
            secret.lockedBySourdineHard = true;

            sylve = ScriptableObject.CreateInstance<LevelDef>();
            sylve.id = "sylve";
            sylve.number = 1;
        }

        [TearDown]
        public void TearDown()
        {
            LevelScores.ResetForTests();
            Object.DestroyImmediate(sourd);
            Object.DestroyImmediate(secret);
            Object.DestroyImmediate(sylve);
        }

        [Test]
        public void SecretLevel_IsLockedInitially_NormalLevelIsNot()
        {
            Assert.IsTrue(LevelScores.IsLocked(secret));
            Assert.IsFalse(LevelScores.IsLocked(sourd));
            Assert.IsFalse(LevelScores.IsLocked(sylve));
            Assert.IsFalse(LevelScores.IsPlayable(secret));
        }

        [Test]
        public void DoesNotUnlock_OnEasyOrMediumOrDemoOrAutoplayOrDeathOrOtherLevel()
        {
            // Facile et moyen sur La Sourdine
            Assert.IsFalse(LevelScores.CheckSecretUnlock(sourd, true, RetryMode.Checkpoint, false, false, savePrefs: false));
            Assert.IsFalse(LevelScores.CheckSecretUnlock(sourd, true, RetryMode.Restart, false, false, savePrefs: false));
            // Mort en mode DUR
            Assert.IsFalse(LevelScores.CheckSecretUnlock(sourd, false, RetryMode.OneLife, false, false, savePrefs: false));
            // Démo ou Autoplay en mode DUR
            Assert.IsFalse(LevelScores.CheckSecretUnlock(sourd, true, RetryMode.OneLife, true, false, savePrefs: false));
            Assert.IsFalse(LevelScores.CheckSecretUnlock(sourd, true, RetryMode.OneLife, false, true, savePrefs: false));
            // Autre niveau terminé en mode DUR
            Assert.IsFalse(LevelScores.CheckSecretUnlock(sylve, true, RetryMode.OneLife, false, false, savePrefs: false));

            Assert.IsFalse(LevelScores.SecretUnlocked);
            Assert.IsTrue(LevelScores.IsLocked(secret));
        }

        [Test]
        public void Unlocks_OnlyWhenSourdineCompletedInHardMode()
        {
            int v0 = LevelScores.Version;
            bool unlocked = LevelScores.CheckSecretUnlock(sourd, true, RetryMode.OneLife, false, false, savePrefs: false);

            Assert.IsTrue(unlocked);
            Assert.IsTrue(LevelScores.SecretUnlocked);
            Assert.IsFalse(LevelScores.IsLocked(secret));
            Assert.Greater(LevelScores.Version, v0);

            // Idempotent : un second appel ne ré-émet pas de déblocage neuf
            Assert.IsFalse(LevelScores.CheckSecretUnlock(sourd, true, RetryMode.OneLife, false, false, savePrefs: false));
        }
    }
}
