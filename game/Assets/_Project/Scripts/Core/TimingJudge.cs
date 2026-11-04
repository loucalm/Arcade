using System;

namespace Kenta.Core
{
    public enum Judgement { Perfect, Good, Meh }

    /// <summary>Compteurs de jugements d'une partie.</summary>
    [Serializable]
    public struct RunJudgeStats
    {
        public int perfect, good, meh;
        public int Total => perfect + good + meh;
    }

    /// <summary>
    /// Précision rythmique d'une action : écart entre l'appui et le beat de l'obstacle dans la chart.
    /// N'influe que sur le score et le combo : la réussite reste celle d'un plateformeur (invariant n°4).
    /// </summary>
    public static class TimingJudge
    {
        public const double PerfectWindow = 0.045, GoodWindow = 0.090;

        public static Judgement Judge(double offsetSeconds)
        {
            double a = Math.Abs(offsetSeconds);
            if (a <= PerfectWindow + 1e-9) return Judgement.Perfect;
            if (a <= GoodWindow + 1e-9) return Judgement.Good;
            return Judgement.Meh;
        }

        /// <summary>Part des points de base gagnée : 100 % / 60 % / 20 %.</summary>
        public static float Factor(Judgement j) => j switch
        {
            Judgement.Perfect => 1f,
            Judgement.Good => 0.6f,
            _ => 0.2f,
        };

        public static string Label(Judgement j) => j switch
        {
            Judgement.Perfect => "PARFAIT",
            Judgement.Good => "BIEN",
            _ => "BOF",
        };
    }
}
