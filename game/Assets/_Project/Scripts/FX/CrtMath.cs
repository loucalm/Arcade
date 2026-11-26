using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Géométrie de l'effet CRT (même calcul que le shader Kenta/CrtDisplay) : pour un point de l'écran, le point de
    /// l'image du jeu qu'on y voit (fisheye puis bombé), et le masque des coins arrondis. Sert aux tests.
    /// </summary>
    public static class CrtMath
    {
        /// <summary>Coordonnées (0..1) déformées, encore centrées sur 0 (−1..1).</summary>
        static Vector2 Warp(Vector2 screenUv, float fish, float curve, float zoom)
        {
            Vector2 c = (screenUv * 2f - Vector2.one) * zoom;                   // zoom > 1 : léger dézoom, rien n'est coupé
            c *= 1f - fish + fish * c.sqrMagnitude * 0.5f;                       // fisheye : centre grossi, coins fixes
            return new Vector2(c.x * (1f + c.y * curve * c.y * curve),        // bombé : bords repoussés
                               c.y * (1f + c.x * curve * c.x * curve));
        }

        /// <summary>Point de l'image du jeu (0..1) vu au point d'écran screenUv (0..1).</summary>
        public static Vector2 SourceUv(Vector2 screenUv, float fish, float curve, float zoom = 1f)
            => Warp(screenUv, fish, curve, zoom) * 0.5f + new Vector2(0.5f, 0.5f);

        /// <summary>
        /// 1 dans l'image, 0 hors de l'image ou dans un coin arrondi (corner : rayon en fraction de la hauteur ;
        /// aspect = largeur / hauteur ; border : le cadre arrondi dépasse l'image d'autant, pour ne pas en couper les coins).
        /// </summary>
        public static float CornerMask(Vector2 screenUv, float fish, float curve, float corner, float aspect, float zoom = 1f, float border = 0f)
        {
            Vector2 c = Warp(screenUv, fish, curve, zoom);
            float ax = Mathf.Abs(c.x), ay = Mathf.Abs(c.y);
            if (ax > 1f || ay > 1f) return 0f;
            ax /= 1f + border;
            ay /= 1f + border;
            float rx = corner / aspect;
            float dx = Mathf.Max(ax - (1f - rx), 0f) / rx;
            float dy = Mathf.Max(ay - (1f - corner), 0f) / corner;
            return Mathf.Clamp01((1f - Mathf.Sqrt(dx * dx + dy * dy)) * 25f);
        }
    }
}
