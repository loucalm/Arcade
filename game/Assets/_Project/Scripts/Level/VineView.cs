using Kenta.FX;
using UnityEngine;

namespace Kenta.Level
{
    /// <summary>
    /// Corde d'une liane en pixel art : des carrés de 2 px de jeu posés sur la grille, du nœud (ce transform) jusqu'à la
    /// main du héros pendant le balancier, sinon jusqu'à l'endroit où on l'attrape. Pas de rotation (pixel art).
    /// </summary>
    public class VineView : MonoBehaviour
    {
        /// <summary>Hauteur de la main au-dessus des pieds (u).</summary>
        public const float HandHeight = 1.1f;
        const int MaxDots = 120;
        const float Dot = 2f / PixelScreen.Ppu;                 // carré de 2 px de jeu

        SpriteRenderer[] dots;
        Vector2 restEnd;

        public void Setup(Sprite pixel, Color color, Vector2 restWorldEnd)
        {
            restEnd = restWorldEnd;
            dots = new SpriteRenderer[MaxDots];
            for (int i = 0; i < MaxDots; i++)
            {
                var go = new GameObject("Rope");
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(2f, 2f, 1f);   // le sprite pixel fait 1 px de jeu
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = pixel;
                sr.color = color;
                sr.sortingOrder = 9;
                dots[i] = sr;
            }
            Draw(restEnd);
        }

        /// <summary>La main du héros est en `hand` (coordonnées monde).</summary>
        public void Hold(Vector2 hand) => Draw(hand);

        /// <summary>Lâchée : la corde revient pendre vers son point de prise.</summary>
        public void Release() => Draw(restEnd);

        void Draw(Vector2 end)
        {
            if (dots == null) return;
            Vector2 a = transform.position;
            float len = Vector2.Distance(a, end);
            int n = Mathf.Clamp(Mathf.CeilToInt(len / Dot), 1, MaxDots);
            for (int i = 0; i < MaxDots; i++)
            {
                bool on = i < n;
                dots[i].enabled = on;
                if (!on) continue;
                var p = Vector2.Lerp(a, end, (i + 0.5f) / n);
                dots[i].transform.position = new Vector3(PixelScreen.Snap(p.x), PixelScreen.Snap(p.y), 0f);
            }
        }
    }
}
