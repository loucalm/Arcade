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

        /// <summary>Flocons par croche et par rafale (sur le 1 de la mesure), à pleine intensité.</summary>
        public const int SnowPerEighth = 1, SnowGust = 6;

        /// <summary>
        /// Flocons lâchés par seconde : à l'avalanche du Givre (168 BPM), ils doivent tenir dans le pool de 120 pendant
        /// toute leur chute (jusqu'à 11,7 s en plan large), sinon des flocons disparaissent en plein écran.
        /// </summary>
        public static float SnowPerSecond(float bpm, int beatsPerBar, float intensity)
        {
            float beatsPerSecond = bpm / 60f;
            return beatsPerSecond * 2f * SpawnCount(intensity, SnowPerEighth)
                 + beatsPerSecond / UnityEngine.Mathf.Max(1, beatsPerBar) * SpawnCount(intensity, SnowGust);
        }

        /// <summary>Particules lâchées sur un pas, selon l'intensité (0..1).</summary>
        public static int SpawnCount(float intensity, int perTickAtFull) =>
            (int)System.Math.Round(UnityEngine.Mathf.Clamp01(intensity) * perTickAtFull, System.MidpointRounding.AwayFromZero);

        /// <summary>Position x (repère de la vue) ramenée dans [-span, span] : sortie à gauche, la particule revient à droite.</summary>
        public static float Wrap(float x, float span)
        {
            if (x < -span) return x + 2f * span;
            if (x > span) return x - 2f * span;
            return x;
        }

        /// <summary>Emplacement suivant du pool : on reprend toujours le plus ancien (aucune allocation).</summary>
        public static int NextSlot(int next, int capacity) => (next + 1) % capacity;
    }
}
