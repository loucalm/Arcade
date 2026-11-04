using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>
    /// Texte en police pixel : une Image par caractère (police proportionnelle), alignée sur la grille de pixels du canvas 320×180.
    /// `scale` entier = texte agrandi sans flou. L'origine est le centre du RectTransform.
    /// </summary>
    public class PixelText : MonoBehaviour
    {
        public enum Align { Left, Center, Right }

        [SerializeField] PixelFontData font;
        [SerializeField] string text = "";
        [SerializeField] Color color = Color.white;
        [SerializeField] Align align = Align.Center;
        [SerializeField, Min(1)] int scale = 1;
        [Tooltip("Plaque sombre derrière le texte : lisible sur n'importe quel décor.")]
        [SerializeField] bool plate;

        const int PlatePadX = 3, PlatePadY = 2;
        Image plateImage;

        readonly List<Image> glyphs = new();
        string built;
        Color builtColor;
        Align builtAlign;
        int builtScale;

        public string Text
        {
            get => text;
            set { if (text != value) { text = value; Rebuild(); } }
        }

        public Color Color
        {
            get => color;
            set { if (color != value) { color = value; Rebuild(); } }
        }

        public int Scale { get => scale; set { scale = Mathf.Max(1, value); Rebuild(); } }

        void OnEnable()
        {
            // Les glyphs sont recréés à chaque activation : on retire d'abord ceux qu'un prefab aurait pu garder.
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            glyphs.Clear();
            plateImage = null;
            built = null;
            Rebuild();
        }

        public void Setup(PixelFontData data, string value, Color c, Align a = Align.Center, int s = 1)
        {
            font = data; text = value; color = c; align = a; scale = Mathf.Max(1, s);
            Rebuild();
        }

        void Rebuild()
        {
            if (font == null) return;
            if (text == built && color == builtColor && align == builtAlign && scale == builtScale) return;
            built = text; builtColor = color; builtAlign = align; builtScale = scale;

            int n = text.Length;
            int total = MeasureWidth(font, text, scale);
            float cursor = align switch
            {
                Align.Left => 0f,
                Align.Right => -total,
                _ => -Mathf.Floor(total * 0.5f),
            };
            UpdatePlate(cursor, total);
            for (int i = 0; i < Mathf.Max(n, glyphs.Count); i++)
            {
                if (i >= glyphs.Count) glyphs.Add(NewGlyph());
                var img = glyphs[i];
                if (i >= n) { img.gameObject.SetActive(false); continue; }
                var sprite = font.Get(text[i]);
                int adv = font.AdvanceOf(text[i]) * scale;
                img.gameObject.SetActive(sprite != null);
                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.color = color;
                    var rt = (RectTransform)img.transform;
                    rt.sizeDelta = sprite.rect.size * scale;
                    rt.anchoredPosition = new Vector2(cursor, -Mathf.Floor(sprite.rect.height * scale * 0.5f));   // pivot bas-gauche : pixels entiers
                }
                cursor += adv;
            }
        }

        void UpdatePlate(float left, int total)
        {
            if (!plate || font.plate == null)
            {
                if (plateImage != null) plateImage.gameObject.SetActive(false);
                return;
            }
            if (plateImage == null)
            {
                plateImage = NewGlyph();
                plateImage.name = "plate";
                plateImage.transform.SetAsFirstSibling();       // derrière les glyphes
                plateImage.sprite = font.plate;
                plateImage.type = Image.Type.Sliced;
            }
            var size = PlateSize(font, text, scale);
            plateImage.gameObject.SetActive(size.x > 0f);
            var rt = (RectTransform)plateImage.transform;
            rt.sizeDelta = size;
            rt.anchoredPosition = new Vector2(left - PlatePadX, -Mathf.Floor(font.LineHeight * scale * 0.5f) - PlatePadY);
        }

        /// <summary>Taille de la plaque : texte + 3 px de chaque côté, + 2 px en haut et en bas (marges non agrandies).</summary>
        public static Vector2 PlateSize(PixelFontData font, string value, int scale)
        {
            int w = MeasureWidth(font, value, scale);
            if (w <= 0) return Vector2.zero;
            return new Vector2(w + 2 * PlatePadX, font.LineHeight * Mathf.Max(1, scale) + 2 * PlatePadY);
        }

        /// <summary>Largeur en pixels d'un texte (sans l'espace après la dernière lettre).</summary>
        public static int MeasureWidth(PixelFontData font, string value, int scale)
        {
            if (font == null || string.IsNullOrEmpty(value)) return 0;
            int w = 0;
            for (int i = 0; i < value.Length; i++) w += font.AdvanceOf(value[i]);
            return (w - 1) * Mathf.Max(1, scale);
        }

        Image NewGlyph()
        {
            var go = new GameObject("g", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = Vector2.zero;
            return img;
        }
    }
}
