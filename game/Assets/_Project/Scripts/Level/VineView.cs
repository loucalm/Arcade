using Kenta.FX;
using UnityEngine;

namespace Kenta.Level
{
    /// <summary>
    /// Corde d'une liane en pixel art : des carrés de 4 px de jeu à contour sombre posés sur la grille, avec des feuilles,
    /// du nœud (ce transform, dessiné) jusqu'à la main du héros pendant le balancier, sinon jusqu'à l'endroit où on
    /// l'attrape. Pas de rotation (pixel art). Lisible à 2 m : l'ancienne corde de 2 px se perdait dans le décor.
    /// </summary>
    public class VineView : MonoBehaviour
    {
        /// <summary>Hauteur de la main au-dessus des pieds (u) : 54 px, le poing des images de liane des héros v2
        /// (tools/heros-v2/import.py, VINE_GRIP_PX ; changer les deux ensemble).</summary>
        public const float HandHeight = 1.35f;
        const int MaxDots = 120;
        const float Dot = 2f / PixelScreen.Ppu;                 // pas de 2 px de jeu entre deux carrés
        const int LeafEvery = 7;
        static readonly Color Outline = new(0.1f, 0.12f, 0.08f);
        static readonly Color Leaf = new(0.62f, 0.86f, 0.32f);
        static readonly Color KnotColor = new(0.42f, 0.27f, 0.14f);

        SpriteRenderer[] dots, rims, leaves;
        Vector2 restEnd;

        public void Setup(Sprite pixel, Color color, Vector2 restWorldEnd)
        {
            restEnd = restWorldEnd;
            // corde plus claire que l'ancienne (elle se lit sur les fonds sombres), contour sombre (sur les fonds clairs)
            Color rope = Color.Lerp(color, new Color(0.5f, 0.75f, 0.28f), 0.6f);
            dots = new SpriteRenderer[MaxDots];
            rims = new SpriteRenderer[MaxDots];
            leaves = new SpriteRenderer[MaxDots / LeafEvery + 1];
            for (int i = 0; i < MaxDots; i++)
            {
                rims[i] = Square(pixel, Outline, 6f, 6f, 8);   // tailles paires : centrées sur la grille
                dots[i] = Square(pixel, rope, 4f, 4f, 9);
            }
            for (int i = 0; i < leaves.Length; i++) leaves[i] = Square(pixel, Leaf, 4f, 2f, 10);
            // nœud d'accroche : un carré de 8 px à contour, bien visible
            var knotRim = Square(pixel, Outline, 10f, 10f, 8);
            var knot = Square(pixel, KnotColor, 8f, 8f, 9);
            knotRim.transform.position = knot.transform.position =
                new Vector3(PixelScreen.Snap(transform.position.x), PixelScreen.Snap(transform.position.y), 0f);
            Draw(restEnd);
        }

        SpriteRenderer Square(Sprite pixel, Color color, float w, float h, int order)
        {
            var go = new GameObject("Rope");
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(w, h, 1f);    // le sprite pixel fait 1 px de jeu
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = pixel;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
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
                dots[i].enabled = rims[i].enabled = on;
                if (i % LeafEvery == LeafEvery - 1)
                {
                    var leaf = leaves[i / LeafEvery];
                    leaf.enabled = on;
                    if (on)
                    {
                        var q = Vector2.Lerp(a, end, (i + 0.5f) / n);
                        float side = (i / LeafEvery) % 2 == 0 ? 3f : -3f;     // feuilles en quinconce
                        leaf.transform.position = new Vector3(PixelScreen.Snap(q.x + side / PixelScreen.Ppu), PixelScreen.Snap(q.y), 0f);
                    }
                }
                if (!on) continue;
                var p = Vector2.Lerp(a, end, (i + 0.5f) / n);
                var pos = new Vector3(PixelScreen.Snap(p.x), PixelScreen.Snap(p.y), 0f);
                dots[i].transform.position = rims[i].transform.position = pos;
            }
        }
    }
}
