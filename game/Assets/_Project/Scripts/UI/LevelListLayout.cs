using UnityEngine;

namespace Kenta.UI
{
    /// <summary>
    /// Calcul de la liste des niveaux façon osu! (écran de sélection, ADR-030), en pixels d'interface (320×180, repère
    /// centré). La carte sélectionnée est au milieu, sortie vers la gauche ; les autres s'écartent vers la droite.
    /// La liste boucle. Tout est arrondi au pixel entier.
    /// </summary>
    public static class LevelListLayout
    {
        public const int Spacing = 29, SelectedHeight = 28, Height = 26, SelectedLeft = -18, Shift = 6, Right = 156;

        public static int Wrap(int index, int count) => count <= 0 ? 0 : ((index % count) + count) % count;

        /// <summary>Écart circulaire le plus court entre la carte <paramref name="index"/> et la position de défilement, dans [−count/2, count/2).</summary>
        public static float Offset(int index, float scroll, int count)
        {
            if (count <= 0) return 0f;
            float half = count * 0.5f;
            return Mathf.Repeat(index - scroll + half, count) - half;
        }

        /// <summary>Rectangle d'une carte : x = bord gauche, y = centre vertical (pivot gauche-milieu).</summary>
        public static RectInt CardRect(float offset)
        {
            float a = Mathf.Abs(offset);
            int x = SelectedLeft + Mathf.RoundToInt(Shift * a);
            int y = Mathf.RoundToInt(-offset * Spacing);
            return new RectInt(x, y, Right - x, a < 0.5f ? SelectedHeight : Height);
        }

        /// <summary>« 150 BPM  1:43 » : BPM arrondi, durée en minutes:secondes (tronquée).</summary>
        public static string FormatInfo(float bpm, float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{Mathf.RoundToInt(bpm)} BPM  {total / 60}:{total % 60:D2}";
        }
    }
}
