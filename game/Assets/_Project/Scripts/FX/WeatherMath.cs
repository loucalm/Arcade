namespace Kenta.FX
{
    /// <summary>
    /// Calculs purs de la météo du décor (ADR-047) : combien de pas rythmiques (double-croches, temps, mesures) ont été
    /// franchis depuis l'image d'avant, combien de particules lâcher, quel emplacement du pool reprendre.
    /// </summary>
    public static class WeatherMath
    {
        /// <summary>
        /// Nombre de multiples de `step` franchis dans (prevBeat, beat], plafonné à `max` : après une reprise à un
        /// checkpoint ou une image très longue, pas de déluge de particules d'un coup. 0 si le temps recule.
        /// </summary>
        public static int Ticks(double prevBeat, double beat, double step, int max)
        {
            if (beat <= prevBeat || step <= 0) return 0;
            double n = System.Math.Floor(beat / step) - System.Math.Floor(prevBeat / step);
            return (int)System.Math.Min(max, System.Math.Max(0, n));
        }

        /// <summary>Particules lâchées sur un pas, selon l'intensité (0..1).</summary>
        public static int SpawnCount(float intensity, int perTickAtFull) =>
            (int)System.Math.Round(UnityEngine.Mathf.Clamp01(intensity) * perTickAtFull, System.MidpointRounding.AwayFromZero);

        /// <summary>Emplacement suivant du pool : on reprend toujours le plus ancien (aucune allocation).</summary>
        public static int NextSlot(int next, int capacity) => (next + 1) % capacity;
    }
}
