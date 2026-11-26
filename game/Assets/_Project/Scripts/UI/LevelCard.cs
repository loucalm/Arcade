using Kenta.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>
    /// Une carte de la liste des niveaux : cadre d'1 px, fond, vignette du décor, « N NOM », titre du morceau, étoiles.
    /// Posée au pixel entier par <see cref="LevelSelectScreen"/> (pivot gauche-milieu).
    /// </summary>
    public class LevelCard : MonoBehaviour
    {
        [SerializeField] Image frame;
        [SerializeField] Image fill;
        [SerializeField] Image thumb;
        [SerializeField] PixelText title;
        [SerializeField] PixelText song;
        [SerializeField] Image[] stars;

        static readonly Color Gold = new(1f, 0.82f, 0.23f);
        static readonly Color FrameDim = new(80 / 255f, 74 / 255f, 120 / 255f);
        static readonly Color FillOn = new(29 / 255f, 24 / 255f, 48 / 255f, 235 / 255f);
        static readonly Color FillOff = new(29 / 255f, 24 / 255f, 48 / 255f, 200 / 255f);
        static readonly Color StarOff = new(70 / 255f, 64 / 255f, 100 / 255f);
        static readonly Color TextDim = new(0.6f, 0.58f, 0.75f);
        static readonly Color TextDimmer = new(0.43f, 0.41f, 0.57f);
        static readonly Color ThumbDim = new(0.62f, 0.6f, 0.68f);

        // le titre du morceau s'arrête avant les étoiles (à 38 px du bord droit, voir KentaBuilder) ; texte à x = 46
        const int SongLeft = 46, StarsFromRight = 38, SongGap = 3;

        LevelDef shownLevel;
        bool shownSelected, shown;
        RectInt placed = new(int.MinValue, 0, 0, 0);
        string songFull = "";

        /// <summary>Place laissée au titre du morceau sur une carte de cette largeur (px de l'interface 320×180).</summary>
        public static int SongMaxWidth(int cardWidth) => cardWidth - SongLeft - StarsFromRight - SongGap;

        /// <summary>Le texte tel quel s'il tient, sinon coupé et fini par un point (« WE'RE THE RESIS. »).</summary>
        public static string FitText(PixelFontData font, string text, int maxWidth)
        {
            if (font == null || string.IsNullOrEmpty(text) || PixelText.MeasureWidth(font, text, 1) <= maxWidth) return text;
            for (int n = text.Length - 1; n > 0; n--)
            {
                string cut = text.Substring(0, n).TrimEnd() + ".";
                if (PixelText.MeasureWidth(font, cut, 1) <= maxWidth) return cut;
            }
            return "";
        }

        void FitSong() => song.Text = FitText(song.Font, songFull, SongMaxWidth(placed.width));

        public void Show(LevelDef level, bool selected)
        {
            if (shown && level == shownLevel && selected == shownSelected) return;
            shown = true;
            shownLevel = level;
            shownSelected = selected;

            frame.color = selected ? Gold : FrameDim;
            fill.color = selected ? FillOn : FillOff;
            thumb.sprite = level != null ? level.thumbnail : null;
            thumb.enabled = thumb.sprite != null;
            thumb.color = selected ? Color.white : ThumbDim;
            title.Text = level != null ? $"{level.number} {level.Label}".ToUpperInvariant() : "";
            title.Color = selected ? Color.white : TextDim;
            songFull = level != null && level.song != null ? level.song.title.ToUpperInvariant() : "";
            FitSong();
            song.Color = selected ? TextDim : TextDimmer;
            int n = level != null ? level.stars : 0;
            for (int i = 0; i < stars.Length; i++) stars[i].color = i < n ? Gold : StarOff;
        }

        public void Place(RectInt r)
        {
            if (r.Equals(placed)) return;
            bool widthChanged = r.width != placed.width;
            placed = r;
            if (widthChanged && shown) FitSong();         // les cartes rétrécissent en s'éloignant de la sélection
            var rt = (RectTransform)transform;
            rt.anchoredPosition = new Vector2(r.x, r.y);
            rt.sizeDelta = new Vector2(r.width, r.height);
        }
    }
}
