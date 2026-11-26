using Kenta.Core;
using UnityEngine;

namespace Kenta.UI
{
    /// <summary>
    /// Grille du HUD en partie (pixels de l'interface 320×180, origine au centre) et calculs purs, testés
    /// (Editor/Tests/HudLayoutTests). Une seule grille pour tout le HUD :
    /// marge au bord de l'écran 4 px (haut, gauche, droite) ; plaque = cadre d'1 px + 2 px de marge intérieure autour du
    /// contenu (8 px : capitales de 7 px + ombre) = 14 px de haut ; 4 px entre deux éléments (plaques, score et
    /// multiplicateur, vies et pièces) ; 2 px entre une icône et son nombre.
    /// Les trois plaques du haut se suivent à 4 px d'écart : score | frise | vies et pièces.
    /// </summary>
    public static class HudLayout
    {
        public const int ScreenWidth = 320, ScreenHeight = 180;
        /// <summary>Bord de l'écran → plaque (haut, gauche, droite).</summary>
        public const int Margin = 4;
        /// <summary>Cadre de la plaque.</summary>
        public const int Frame = 1;
        /// <summary>Cadre → contenu, sur les quatre côtés.</summary>
        public const int Pad = 2;
        /// <summary>Entre deux éléments : plaques, score et multiplicateur, vies et pièces.</summary>
        public const int Gap = 4;
        /// <summary>Entre une icône et son nombre.</summary>
        public const int IconGap = 2;
        /// <summary>Hauteur du contenu : capitales de 7 px + 1 px d'ombre.</summary>
        public const int ContentHeight = 8;
        /// <summary>Cadre + marge intérieure.</summary>
        public const int Inset = Frame + Pad;
        public const int PlateHeight = ContentHeight + 2 * Inset;
        /// <summary>Corps d'une icône (cœur, pièce) : la hauteur et la largeur des capitales.</summary>
        public const int IconSize = 7;
        /// <summary>Chiffres en colonnes fixes (la police est proportionnelle : le « 1 » est plus étroit).</summary>
        public const int DigitAdvance = 6;
        /// <summary>Hauteur des segments de la frise, centrés dans la plaque.</summary>
        public const int FriezeHeight = 4;

        /// <summary>Haut de l'écran (origine au centre).</summary>
        public const int Top = ScreenHeight / 2;
        /// <summary>Bas de la bande du HUD (haut des plaques − leur hauteur).</summary>
        public const int BandBottom = Top - Margin - PlateHeight;

        /// <summary>Largeur de n chiffres en colonnes (sans l'espace après le dernier).</summary>
        public static int DigitsWidth(int n) => n <= 0 ? 0 : n * DigitAdvance - 1;

        /// <summary>Nombre de chiffres d'un entier positif (au moins 1).</summary>
        public static int DigitCount(int value)
        {
            int n = 1;
            for (value = Mathf.Abs(value); value >= 10; value /= 10) n++;
            return n;
        }

        /// <summary>Chiffre de rang <paramref name="fromRight"/> (0 = unités).</summary>
        public static int DigitAt(int value, int fromRight)
        {
            value = Mathf.Abs(value);
            for (int i = 0; i < fromRight; i++) value /= 10;
            return value % 10;
        }

        /// <summary>Largeur d'une plaque autour d'un contenu.</summary>
        public static int PlateWidth(int content) => content + 2 * Inset;

        /// <summary>
        /// Plaque de gauche : score, écart, « × » et le multiplicateur ; avec <paramref name="best"/> (reprise au début, à
        /// partir du 2ᵉ essai) : écart, couronne et meilleur score.
        /// </summary>
        public static int ScoreContentWidth(int scoreDigits, int multiplierDigits, bool best) =>
            DigitsWidth(scoreDigits) + Gap + DigitAdvance + DigitsWidth(multiplierDigits)
            + (best ? Gap + IconSize + IconGap + DigitsWidth(scoreDigits) : 0);

        /// <summary>
        /// Largeur des vies selon la difficulté du héros (ADR-036) : un cœur et le nombre de vies (reprise au point), un
        /// cœur seul (une seule vie), ou le texte « ESSAI 2/3 » de largeur <paramref name="triesTextWidth"/> (reprise au début).
        /// </summary>
        public static int LivesWidth(RetryMode mode, int lives, int triesTextWidth) => mode switch
        {
            RetryMode.OneLife => IconSize,
            RetryMode.Restart => triesTextWidth,
            _ => IconSize + IconGap + DigitsWidth(DigitCount(lives)),
        };

        /// <summary>Largeur des pièces : icône, puis « 000/000 » en colonnes.</summary>
        public static int CoinsWidth(int coinDigits) => IconSize + IconGap + DigitsWidth(2 * coinDigits + 1);

        /// <summary>Plaque de droite : vies, écart, pièces.</summary>
        public static int StatsContentWidth(RetryMode mode, int lives, int triesTextWidth, int coinDigits) =>
            LivesWidth(mode, lives, triesTextWidth) + Gap + CoinsWidth(coinDigits);

        /// <summary>Mot affiché sous la frise à la mort (jamais au milieu de l'écran).</summary>
        public static string DeathMessage(RetryMode mode, int livesLeft)
        {
            if (livesLeft <= 0 || mode == RetryMode.OneLife) return "SILENCE...";
            return mode == RetryMode.Restart ? "DU DÉBUT !" : "ENCORE !";
        }

        /// <summary>Plaque du milieu (frise) : la place entre les deux autres, à <see cref="Gap"/> de chacune.</summary>
        public static int CenterPlateWidth(int leftPlate, int rightPlate) =>
            Mathf.Max(2 * Inset + 8, ScreenWidth - 2 * Margin - 2 * Gap - leftPlate - rightPlate);

        /// <summary>
        /// Segments de la frise en pixels entiers : un par section, largeur proportionnelle à sa durée, 1 px d'écart.
        /// starts = début de chaque section en fraction du morceau (0 → 1, croissant). Les segments couvrent exactement
        /// [0, width) ; chacun fait au moins 1 px.
        /// </summary>
        public static void FriezeSegments(float[] starts, int count, int width, int gap, int[] x, int[] w)
        {
            int prevEnd = 0;
            for (int i = 0; i < count; i++)
            {
                int a = i == 0 ? 0 : Mathf.Max(prevEnd + gap, Mathf.RoundToInt(Mathf.Clamp01(starts[i]) * width));
                int maxEnd = width - (count - 1 - i) * (1 + gap);        // place gardée pour les segments suivants
                a = Mathf.Min(a, maxEnd - 1);
                int b = i + 1 < count ? Mathf.RoundToInt(Mathf.Clamp01(starts[i + 1]) * width) - gap : width;
                b = Mathf.Clamp(b, a + 1, maxEnd);
                x[i] = a;
                w[i] = b - a;
                prevEnd = b;
            }
        }

        /// <summary>Remplissage (px entiers) d'un segment [from, to) pour une progression donnée.</summary>
        public static int FillPixels(float progress, float from, float to, int width)
        {
            if (progress >= to) return width;
            if (progress <= from || to <= from) return 0;
            return Mathf.Clamp(Mathf.FloorToInt((progress - from) / (to - from) * width), 0, width);
        }

        /// <summary>Score affiché qui défile vers le vrai score : vite pour un gros écart, jamais au-delà.</summary>
        public static float RollScore(float shown, float target, float dt) =>
            Mathf.MoveTowards(shown, target, Mathf.Max(60f, Mathf.Abs(target - shown) * 8f) * dt);

        /// <summary>Rebond en pixels entiers : 0 → height → 0 sur la durée (demi-sinus), 0 en dehors.</summary>
        public static int Bounce(float t, float duration, int height)
        {
            if (t <= 0f || t >= duration || duration <= 0f) return 0;
            return Mathf.RoundToInt(height * Mathf.Sin(Mathf.PI * t / duration));
        }

        /// <summary>Monde → interface 320×180 (origine au centre), la vue du monde pouvant être plus large (zoom).</summary>
        public static Vector2 WorldToUi(Vector2 world, Vector2 cam, int viewWidth, int ppu)
        {
            float k = ScreenWidth / (float)Mathf.Max(1, viewWidth);
            return (world - cam) * (ppu * k);
        }

        /// <summary>Rectangle d'interface (origine au centre) d'une plaque collée en haut, de bord gauche <paramref name="left"/>.</summary>
        public static Rect TopPlateRect(int left, int width) => new(left - ScreenWidth / 2, BandBottom, width, PlateHeight);
    }
}
