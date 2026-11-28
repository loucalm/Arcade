using Kenta.Core;
using Kenta.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>
    /// Une carte de la liste des niveaux : cadre d'1 px, fond, vignette du décor, « N NOM », titre du morceau, étoiles.
    /// Pour un niveau enchaîné (ex. « 10 ??? »), affiche des chaînes en fer et un cadenas pixel par-dessus la vignette.
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
        static readonly Color LockedFrameOn = new(0.88f, 0.28f, 0.36f);
        static readonly Color LockedFrameOff = new(58 / 255f, 52 / 255f, 78 / 255f);
        static readonly Color LockedFillOn = new(20 / 255f, 16 / 255f, 32 / 255f, 242 / 255f);
        static readonly Color LockedFillOff = new(16 / 255f, 14 / 255f, 26 / 255f, 215 / 255f);
        static readonly Color FrameDim = new(80 / 255f, 74 / 255f, 120 / 255f);
        static readonly Color FillOn = new(29 / 255f, 24 / 255f, 48 / 255f, 235 / 255f);
        static readonly Color FillOff = new(29 / 255f, 24 / 255f, 48 / 255f, 200 / 255f);
        static readonly Color StarOff = new(70 / 255f, 64 / 255f, 100 / 255f);
        static readonly Color TextDim = new(0.6f, 0.58f, 0.75f);
        static readonly Color TextDimmer = new(0.43f, 0.41f, 0.57f);
        static readonly Color ThumbDim = new(0.62f, 0.6f, 0.68f);
        static readonly Color ThumbLocked = new(0.18f, 0.16f, 0.24f);
        static readonly Color ChainSteel = new(0.62f, 0.64f, 0.76f);
        static readonly Color ChainShadow = new(0.12f, 0.10f, 0.18f, 0.92f);

        // le titre du morceau s'arrête avant les étoiles (à 38 px du bord droit, voir KentaBuilder) ; texte à x = 46
        const int SongLeft = 46, StarsFromRight = 38, SongGap = 3;

        LevelDef shownLevel;
        bool shownSelected, shownLocked, shown;
        RectInt placed = new(int.MinValue, 0, 0, 0);
        string songFull = "";
        GameObject lockOverlay;
        Image lockBody;

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

        void EnsureLockOverlay()
        {
            if (lockOverlay != null || thumb == null || frame == null) return;
            var px = frame.sprite;
            var root = new GameObject("LockChains", typeof(RectTransform));
            root.layer = gameObject.layer;
            root.transform.SetParent(transform, false);
            var rt = (RectTransform)root.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(2f, 0f);
            rt.sizeDelta = new Vector2(40f, 24f);

            // voile sombre sur la vignette + chaînes croisées + maillons sur la carte + cadenas central
            Bar(rt, px, "Veil", 20f, 0f, 40f, 24f, new Color(0.05f, 0.04f, 0.09f, 0.68f));
            // barres de chaînes horizontales et verticales sur la vignette (ombre + fer)
            Bar(rt, px, "ChainH0S", 20f, 5f, 40f, 3f, ChainShadow);
            Bar(rt, px, "ChainH0", 20f, 5f, 40f, 1f, ChainSteel);
            Bar(rt, px, "ChainH1S", 20f, -5f, 40f, 3f, ChainShadow);
            Bar(rt, px, "ChainH1", 20f, -5f, 40f, 1f, ChainSteel);
            Bar(rt, px, "ChainV0S", 11f, 0f, 3f, 24f, ChainShadow);
            Bar(rt, px, "ChainV0", 11f, 0f, 1f, 24f, ChainSteel);
            Bar(rt, px, "ChainV1S", 29f, 0f, 3f, 24f, ChainShadow);
            Bar(rt, px, "ChainV1", 29f, 0f, 1f, 24f, ChainSteel);
            // maillons d'attache
            for (int lx = 5; lx <= 35; lx += 6)
            {
                Bar(rt, px, "LinkT", lx, 5f, 3f, 2f, ChainSteel);
                Bar(rt, px, "LinkB", lx, -5f, 3f, 2f, ChainSteel);
            }
            // cadenas pixel au centre de la vignette (x = 20, y = 0)
            Bar(rt, px, "PadShadow", 20f, 0f, 12f, 15f, ChainShadow);
            Bar(rt, px, "Shackle", 20f, 4.5f, 6f, 5f, new Color(0.80f, 0.82f, 0.92f));
            Bar(rt, px, "ShackleHole", 20f, 4f, 2f, 3f, ChainShadow);
            lockBody = Bar(rt, px, "PadBody", 20f, -2f, 10f, 8f, Gold);
            Bar(rt, px, "KeyholeTop", 20f, -1f, 2f, 2f, ChainShadow);
            Bar(rt, px, "KeyholeBot", 20f, -3f, 1f, 2f, ChainShadow);

            lockOverlay = root;
        }

        static Image Bar(RectTransform parent, Sprite px, string name, float x, float y, float w, float h, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
            var img = go.GetComponent<Image>();
            img.sprite = px;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        public void Show(LevelDef level, bool selected)
        {
            bool locked = LevelScores.IsLocked(level);
            if (shown && level == shownLevel && selected == shownSelected && locked == shownLocked) return;
            shown = true;
            shownLevel = level;
            shownSelected = selected;
            shownLocked = locked;

            frame.color = locked ? (selected ? LockedFrameOn : LockedFrameOff) : (selected ? Gold : FrameDim);
            fill.color = locked ? (selected ? LockedFillOn : LockedFillOff) : (selected ? FillOn : FillOff);
            thumb.sprite = level != null ? level.thumbnail : null;
            thumb.enabled = thumb.sprite != null;
            thumb.color = locked ? ThumbLocked : (selected ? Color.white : ThumbDim);

            if (locked) EnsureLockOverlay();
            if (lockOverlay != null)
            {
                if (lockOverlay.activeSelf != locked) lockOverlay.SetActive(locked);
                if (locked && lockBody != null)
                    lockBody.color = selected ? LockedFrameOn : new Color(0.58f, 0.56f, 0.68f);
            }

            string label = level == null ? "" : (locked ? $"{level.number} ???" : $"{level.number} {level.Label}".ToUpperInvariant());
            title.Text = label;
            title.Color = locked ? (selected ? LockedFrameOn : TextDim) : (selected ? Color.white : TextDim);
            songFull = level == null ? "" : (locked ? "ENCHAÎNÉ - ???" : (level.song != null ? level.song.title.ToUpperInvariant() : "BIENTÔT..."));
            FitSong();
            song.Color = selected ? TextDim : TextDimmer;
            int n = level != null ? level.stars : 0;
            var starLit = locked ? (selected ? LockedFrameOn : TextDimmer) : Gold;
            for (int i = 0; i < stars.Length; i++) stars[i].color = i < n ? starLit : StarOff;
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
