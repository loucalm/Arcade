namespace Kenta.Core
{
    /// <summary>
    /// Note de fin de niveau (docs/game-design.md) : précision = (PARFAIT + 0,6 × BIEN + 0,2 × BOF) / actions.
    /// S+ = tout PARFAIT et toutes les pièces ; S ≥ 0,95 et pièces ≥ 90 % ; A ≥ 0,85 ; B ≥ 0,70 ; C ≥ 0,50 ; sinon D.
    /// Niveau non terminé : plafonné à C.
    /// </summary>
    public static class GradeCalculator
    {
        static readonly string[] Order = { "S+", "S", "A", "B", "C", "D" };

        public static string Grade(int perfect, int good, int meh, int lums, int totalLums, bool completed)
        {
            int n = perfect + good + meh;
            double acc = n > 0 ? (perfect + 0.6 * good + 0.2 * meh) / n : 1.0;
            double coins = totalLums > 0 ? (double)lums / totalLums : 1.0;
            string g;
            if (good == 0 && meh == 0 && lums >= totalLums) g = "S+";
            else if (acc >= 0.95 && coins >= 0.9) g = "S";
            else if (acc >= 0.85) g = "A";
            else if (acc >= 0.70) g = "B";
            else if (acc >= 0.50) g = "C";
            else g = "D";
            if (!completed && System.Array.IndexOf(Order, g) < System.Array.IndexOf(Order, "C")) g = "C";
            return g;
        }
    }
}
