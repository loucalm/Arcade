using UnityEngine;

namespace Kenta.UI
{
    /// <summary>
    /// Nombre du HUD en colonnes fixes de 6 px (un PixelText par chiffre, centré dans sa colonne) : la police est
    /// proportionnelle, et un score qui défile ne doit pas faire bouger ses voisins. Les colonnes visibles sont les
    /// premières (de gauche à droite) ; aucune chaîne créée à l'affichage (chiffres en cache).
    /// </summary>
    public class HudNumber : MonoBehaviour
    {
        [SerializeField] PixelText[] cells;
        [Tooltip("Zéros en tête (000120) ; sinon le nombre occupe juste ses chiffres.")]
        [SerializeField] bool zeroPad = true;

        static readonly string[] DigitText = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        int digits = -1;
        int shown = int.MinValue;
        Color color = Color.white;

        public int Capacity => cells != null ? cells.Length : 0;

        /// <summary>Nombre de colonnes affichées (1 → Capacity).</summary>
        public int Digits
        {
            get => digits < 0 ? Capacity : digits;
            set
            {
                value = Mathf.Clamp(value, 1, Capacity);
                if (value == digits) return;
                digits = value;
                Arrange();
                shown = int.MinValue;
            }
        }

        public int Width => HudLayout.DigitsWidth(Digits);

        public Color Color
        {
            get => color;
            set
            {
                if (value == color) return;
                color = value;
                for (int i = 0; i < cells.Length; i++) cells[i].Color = value;
            }
        }

        /// <summary>Place les colonnes : le chiffre (5 px, « 1 » 3 px) est centré dans sa colonne de 6 px.</summary>
        public void Arrange()
        {
            if (cells == null) return;
            int n = Digits;
            for (int i = 0; i < cells.Length; i++)
            {
                var rt = (RectTransform)cells[i].transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(i * HudLayout.DigitAdvance + 2, 0f);
                cells[i].gameObject.SetActive(i < n);
            }
        }

        public void SetValue(int value)
        {
            if (value == shown || cells == null) return;
            shown = value;
            int n = Digits;
            int max = 1;
            for (int i = 0; i < n; i++) max *= 10;
            value = Mathf.Clamp(value, 0, max - 1);
            int used = HudLayout.DigitCount(value);
            for (int i = 0; i < n; i++)
            {
                int fromRight = n - 1 - i;
                bool blank = !zeroPad && fromRight >= used;
                cells[i].Text = blank ? "" : DigitText[HudLayout.DigitAt(value, fromRight)];
            }
        }
    }
}
