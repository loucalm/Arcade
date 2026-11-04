using System.Collections.Generic;
using UnityEngine;

namespace Kenta.UI
{
    /// <summary>
    /// Police pixel 5×7 proportionnelle (générée par tools/gen-kenta-assets.py) : un sprite par caractère, corps clair
    /// et ombre portée d'1 px vers le bas intégrée. Avance = largeur du sprite + 1 px. Les minuscules sont affichées en majuscules.
    /// </summary>
    [CreateAssetMenu(fileName = "PixelFont", menuName = "Kenta/Pixel Font")]
    public class PixelFontData : ScriptableObject
    {
        /// <summary>Avance par défaut (caractère inconnu).</summary>
        public const int Advance = 6;
        public string chars = "";
        public Sprite[] sprites;
        [Tooltip("Plaque sombre (9-slice) posée sous les textes qui sont sur le décor.")]
        public Sprite plate;

        /// <summary>Hauteur d'une ligne en pixels (tous les glyphes ont la même hauteur).</summary>
        public int LineHeight => sprites != null && sprites.Length > 0 && sprites[0] != null ? Mathf.RoundToInt(sprites[0].rect.height) : 11;

        Dictionary<char, Sprite> map;

        /// <summary>Avance en pixels (scale 1) : largeur du glyphe + 1 px d'espace.</summary>
        public int AdvanceOf(char c)
        {
            var s = Find(c);
            return s != null ? Mathf.RoundToInt(s.rect.width) + 1 : Advance;
        }

        public Sprite Get(char c) => Find(c) ?? Find(' ');

        Sprite Find(char c)
        {
            if (map == null)
            {
                map = new Dictionary<char, Sprite>();
                for (int i = 0; i < chars.Length && i < sprites.Length; i++) map[chars[i]] = sprites[i];
            }
            if (map.TryGetValue(c, out var s)) return s;
            c = char.ToUpperInvariant(c);
            if (c == 'œ' || c == 'Œ') c = 'Œ';
            return map.TryGetValue(c, out s) ? s : null;
        }
    }
}
