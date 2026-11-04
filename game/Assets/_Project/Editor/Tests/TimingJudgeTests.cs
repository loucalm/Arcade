using Kenta.Core;
using Kenta.Level;
using NUnit.Framework;

namespace Kenta.EditorTests
{
    public class TimingJudgeTests
    {
        [TestCase(0.0, Judgement.Perfect)]
        [TestCase(0.045, Judgement.Perfect)]
        [TestCase(-0.045, Judgement.Perfect)]
        [TestCase(-0.046, Judgement.Good)]
        [TestCase(0.09, Judgement.Good)]
        [TestCase(0.2, Judgement.Meh)]
        [TestCase(-1.0, Judgement.Meh)]      // appui très en avance (buffer) : BOF, pas d'erreur
        public void TimingJudge_Thresholds(double offset, Judgement expected)
        {
            Assert.AreEqual(expected, TimingJudge.Judge(offset));
        }

        [Test]
        public void TimingJudge_Factors()
        {
            Assert.AreEqual(1f, TimingJudge.Factor(Judgement.Perfect));
            Assert.AreEqual(0.6f, TimingJudge.Factor(Judgement.Good));
            Assert.AreEqual(0.2f, TimingJudge.Factor(Judgement.Meh));
        }

        [Test]
        public void RunJudge_NoDoubleCount()
        {
            var stats = new RunJudgeStats();
            var o = new LevelObject();
            Assert.IsTrue(RunManager.ApplyJudgement(ref stats, o, Judgement.Perfect));
            Assert.IsFalse(RunManager.ApplyJudgement(ref stats, o, Judgement.Good));
            Assert.AreEqual(1, stats.perfect);
            Assert.AreEqual(0, stats.good);
        }

        [Test]
        public void RunJudge_CountsEachKind()
        {
            var stats = new RunJudgeStats();
            RunManager.ApplyJudgement(ref stats, new LevelObject(), Judgement.Perfect);
            RunManager.ApplyJudgement(ref stats, new LevelObject(), Judgement.Good);
            RunManager.ApplyJudgement(ref stats, new LevelObject(), Judgement.Meh);
            Assert.AreEqual(1, stats.perfect);
            Assert.AreEqual(1, stats.good);
            Assert.AreEqual(1, stats.meh);
            Assert.AreEqual(3, stats.Total);
        }
    }

    public class GradeCalculatorTests
    {
        [TestCase(10, 0, 0, 20, 20, true, "S+")]
        [TestCase(10, 0, 0, 19, 20, true, "S")]
        [TestCase(9, 1, 0, 20, 20, true, "S")]      // précision 0,96
        [TestCase(8, 2, 0, 20, 20, true, "A")]      // 0,92
        [TestCase(5, 5, 0, 20, 20, true, "B")]      // 0,80
        [TestCase(2, 5, 3, 20, 20, true, "C")]      // 0,56
        [TestCase(0, 0, 10, 20, 20, true, "D")]     // 0,20
        [TestCase(10, 0, 0, 20, 20, false, "C")]    // niveau non fini : plafonné à C
        [TestCase(0, 0, 0, 0, 0, true, "S+")]       // niveau vide
        public void Grade_Thresholds(int p, int g, int m, int lums, int total, bool completed, string expected)
        {
            Assert.AreEqual(expected, GradeCalculator.Grade(p, g, m, lums, total, completed));
        }

        [Test]
        public void Grade_AlwaysPerfectButMissingCoinsIsNotSPlus()
        {
            Assert.AreNotEqual("S+", GradeCalculator.Grade(10, 0, 0, 10, 20, true));
        }
    }
}
