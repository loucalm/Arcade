using System.Collections.Generic;
using Kenta.Core;
using NUnit.Framework;

namespace Kenta.EditorTests
{
    public class LevelScoresTests
    {
        [SetUp]
        public void SetUp() => LevelScores.ResetForTests();

        static List<LevelScoreEntry> Board(params int[] scores)
        {
            var l = new List<LevelScoreEntry>();
            for (int i = 0; i < scores.Length; i++) l = LevelScores.Insert(l, "P" + i.ToString("D2"), scores[i]);
            return l;
        }

        [Test]
        public void Insert_SortsDescending()
        {
            var l = Board(50, 300, 120);
            Assert.AreEqual(new[] { 300, 120, 50 }, l.ConvertAll(e => e.score).ToArray());
        }

        [Test]
        public void Insert_KeepsTen()
        {
            var l = Board(10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 5);
            Assert.AreEqual(LevelScores.Max, l.Count);
            Assert.AreEqual(20, l[l.Count - 1].score);
        }

        [Test]
        public void Insert_OneEntryPerName_KeepsBest()
        {
            var l = LevelScores.Insert(new List<LevelScoreEntry>(), "LOU", 100);
            l = LevelScores.Insert(l, "LOU", 50);
            Assert.AreEqual(1, l.Count);
            Assert.AreEqual(100, l[0].score);
            l = LevelScores.Insert(l, "LOU", 200);
            Assert.AreEqual(1, l.Count);
            Assert.AreEqual(200, l[0].score);
        }

        [Test]
        public void IsTopTen_EmptyBoard()
        {
            Assert.IsTrue(LevelScores.IsTopTen("sylve", 1));
            Assert.IsFalse(LevelScores.IsTopTen("sylve", 0));
        }

        [Test]
        public void IsTopTen_FullBoard()
        {
            for (int i = 1; i <= 10; i++) LevelScores.Record("sylve", "P" + i.ToString("D2"), i * 100);
            Assert.IsFalse(LevelScores.IsTopTen("sylve", 100));
            Assert.IsTrue(LevelScores.IsTopTen("sylve", 101));
        }

        [Test]
        public void Json_RoundTrip()
        {
            var l = Board(300, 120, 50);
            var back = LevelScores.FromJson(LevelScores.ToJson(l));
            Assert.AreEqual(l.Count, back.Count);
            for (int i = 0; i < l.Count; i++)
            {
                Assert.AreEqual(l[i].name, back[i].name);
                Assert.AreEqual(l[i].score, back[i].score);
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("{}")]
        [TestCase("[1,2")]
        [TestCase("{\"entries\":null}")]
        public void FromJson_GarbageGivesEmpty(string json) => Assert.AreEqual(0, LevelScores.FromJson(json).Count);

        [Test]
        public void FromJson_SortsCapsAndFixesNames()
        {
            // valeur écrite par une autre version : désordre, 12 entrées, un nom absent, un score nul
            var json = "{\"entries\":[{\"score\":500},{\"name\":\"ZER\",\"score\":0}";
            for (int i = 1; i <= 11; i++) json += $",{{\"name\":\"P{i:D2}\",\"score\":{i * 10}}}";
            json += "]}";
            var l = LevelScores.FromJson(json);
            Assert.AreEqual(LevelScores.Max, l.Count);
            for (int i = 0; i < l.Count; i++)
            {
                Assert.IsFalse(string.IsNullOrEmpty(l[i].name));
                Assert.Greater(l[i].score, 0);
                if (i > 0) Assert.GreaterOrEqual(l[i - 1].score, l[i].score);
            }
            Assert.AreEqual(500, l[0].score);
        }

        [Test]
        public void Record_IsPerLevel()
        {
            LevelScores.Record("sylve", "LOU", 500);
            Assert.AreEqual(1, LevelScores.Top("sylve").Count);
            Assert.AreEqual(0, LevelScores.Top("bayou").Count);
        }

        [Test]
        public void Record_BumpsVersion()
        {
            int v = LevelScores.Version;
            LevelScores.Record("sylve", "LOU", 500);
            Assert.Greater(LevelScores.Version, v);
        }
    }
}
