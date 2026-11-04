using UnityEngine;

namespace Kenta.FX
{
    /// <summary>Calculs de la grille de pixels (agrandissement entier, découpe pixel entier + fraction).</summary>
    public static class PixelMath
    {
        /// <summary>Plus grand agrandissement entier (≥ 1) de w×h qui tient dans l'écran.</summary>
        public static int IntegerScale(int screenW, int screenH, int w, int h)
            => Mathf.Max(1, Mathf.Min(screenW / w, screenH / h));

        /// <summary>
        /// Découpe une position monde en une position calée sur la grille (multiple de 1/ppu) et le reste,
        /// en pixels, dans [−0,5 ; 0,5). position = snapped + fracPx / ppu.
        /// </summary>
        public static (float snapped, float fracPx) Split(float worldUnits, int ppu)
        {
            float px = worldUnits * ppu;
            float whole = Mathf.Floor(px + 0.5f);
            return (whole / ppu, px - whole);
        }
    }
}
