using Kenta.Level;

namespace Kenta.FX
{
    /// <summary>
    /// Météo d'un niveau (événement `weather` de la chart, ADR-047) : une courbe par effet, lue sur le temps du morceau.
    /// Une reprise à un checkpoint retrouve d'elle-même la bonne intensité.
    /// </summary>
    public class WeatherTracks
    {
        public readonly CorruptionCurve Rain = new(), Snow = new(), Embers = new();

        public void Clear()
        {
            Rain.Clear();
            Snow.Clear();
            Embers.Clear();
        }

        /// <summary>Courbe d'un effet (« rain », « snow », « embers »), null pour un nom inconnu.</summary>
        public CorruptionCurve For(string kind) => kind switch
        {
            "rain" => Rain,
            "snow" => Snow,
            "embers" => Embers,
            _ => null,
        };

        /// <summary>Range un événement `weather` ; faux (ignoré) si l'effet est inconnu.</summary>
        public bool AddEvent(ChartEvent e)
        {
            var curve = For(e.@params?.variant);
            if (curve == null) return false;
            curve.Add(e.beat, e.@params.intensity, e.@params.length);
            return true;
        }
    }
}
