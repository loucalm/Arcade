using System.Collections.Generic;
using UnityEngine;

namespace Kenta.UI
{
    /// <summary>
    /// Police pixel 5×7 (générée par tools/gen-kenta-assets.py) : un sprite par caractère, avec contour sombre intégré.
    /// Cellule de 7×12 px, avance de 6 px. Les minuscules sont affichées en majuscules.
    /// </summary>
    [CreateAssetMenu(fileName = "PixelFont", menuName = "Kenta/Pixel Font")]
    public class PixelFontData : ScriptableObject
    {
        public const int Advance = 6;
        public string chars = "";
        public Sprite[] sprites;

        Dictionary<char, Sprite> map;

        public Sprite Get(char c)
        {
            if (map == null)
            {
                map = new Dictionary<char, Sprite>();
                for (int i = 0; i < chars.Length && i < sprites.Length; i++) map[chars[i]] = sprites[i];
            }
            if (map.TryGetValue(c, out var s)) return s;
            c = char.ToUpperInvariant(c);
            if (c == 'œ' || c == 'Œ') c = 'Œ';
            return map.TryGetValue(c, out s) ? s : (map.TryGetValue(' ', out s) ? s : null);
        }
    }
}
