using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>
    /// Texte en police pixel : une Image par caractère, alignée sur la grille de pixels du canvas 240×135.
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
            float adv = PixelFontData.Advance * scale;
            for (int i = 0; i < Mathf.Max(n, glyphs.Count); i++)
            {
                if (i >= glyphs.Count) glyphs.Add(NewGlyph());
                var img = glyphs[i];
                if (i >= n) { img.gameObject.SetActive(false); continue; }
                var sprite = font.Get(text[i]);
                img.gameObject.SetActive(sprite != null);
                if (sprite == null) continue;
                img.sprite = sprite;
                img.color = color;
                var rt = (RectTransform)img.transform;
                rt.sizeDelta = sprite.rect.size * scale;
                float w = sprite.rect.width * scale;
                float x = align switch
                {
                    Align.Left => i * adv + w * 0.5f,
                    Align.Right => -((n - 1 - i) * adv + w * 0.5f),
                    _ => (i - (n - 1) * 0.5f) * adv,
                };
                rt.anchoredPosition = new Vector2(Mathf.Round(x), 0f);
            }
        }

        Image NewGlyph()
        {
            var go = new GameObject("g", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return img;
        }
    }
}
