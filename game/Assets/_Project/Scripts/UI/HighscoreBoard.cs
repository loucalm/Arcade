using Anatidae;
using Kenta.Core;
using Kenta.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>
    /// Classement en pixel art (docs/da-kenta.md § 8.7) : top 10, pièces or, argent et bronze devant les trois premiers,
    /// et le score qu'on vient d'entrer qui pulse en or. Deux sources : la liste officielle de la borne
    /// (« MEILLEURS SCORES », Anatidae.HighscoreManager) ou le classement d'un niveau (« CLASSEMENT : … »,
    /// LevelScores, ADR-030). Remplace l'affichage du toolkit (police Arial) ; les données et l'envoi restent ceux du toolkit.
    /// </summary>
    public class HighscoreBoard : MonoBehaviour
    {
        [SerializeField] PixelText[] ranks;
        [SerializeField] PixelText[] namesText;
        [SerializeField] PixelText[] scores;
        [Tooltip("Pièces devant les 3 premiers (teintées or, argent, bronze).")]
        [SerializeField] Image[] medals;
        [SerializeField] PixelText emptyText;
        [SerializeField] PixelText title;
        [SerializeField] PixelFontData font;

        static readonly Color Gold = new(1f, 0.82f, 0.23f);
        static readonly Color Row = new(0.86f, 0.84f, 0.96f);
        static readonly Color[] MedalColors = { new(1f, 0.85f, 0.3f), new(0.82f, 0.86f, 0.92f), new(0.86f, 0.55f, 0.32f) };

        string highlightName;
        int highlightScore = -1, highlightRow = -1;
        object shownList;
        int shownCount = -1, shownVersion = -1;
        LevelDef level;                     // null = liste officielle

        const string OfficialTitle = "MEILLEURS SCORES";
        const int TitleMaxWidth = 304;

        /// <summary>Liste officielle de la borne.</summary>
        public void ShowOfficial()
        {
            level = null;
            SetTitle(OfficialTitle);
            shownList = null;
            shownCount = -1;
        }

        /// <summary>Classement d'un niveau (repli : liste officielle si aucun niveau).</summary>
        public void ShowLevel(LevelDef def)
        {
            if (def == null) { ShowOfficial(); return; }
            level = def;
            SetTitle("CLASSEMENT : " + def.Label.ToUpperInvariant());
            shownList = null;
            shownCount = -1;
        }

        void SetTitle(string text)
        {
            if (title == null) return;
            title.Scale = PixelText.MeasureWidth(font, text, 2) <= TitleMaxWidth ? 2 : 1;
            title.Text = text;
        }

        int EntryCount
        {
            get
            {
                if (level != null) return LevelScores.Top(level.id).Count;
                var list = HighscoreManager.Highscores;
                return list != null ? list.Count : 0;
            }
        }

        void Entry(int i, out string name, out int score)
        {
            if (level != null)
            {
                var e = LevelScores.Top(level.id)[i];
                name = e.name;
                score = e.score;
            }
            else
            {
                var e = HighscoreManager.Highscores[i];
                name = e.name;
                score = e.score;
            }
        }

        /// <summary>Le score à faire pulser (celui qu'on vient d'entrer), ou rien.</summary>
        public void Highlight(string name, int score)
        {
            highlightName = name;
            highlightScore = score;
            shownList = null;
            shownCount = -1;
        }

        public void ClearHighlight() => Highlight(null, -1);

        void OnEnable()
        {
            shownList = null;
            shownCount = -1;
            Refresh();
        }

        void Refresh()
        {
            shownList = level != null ? null : HighscoreManager.Highscores;
            shownCount = EntryCount;
            shownVersion = LevelScores.Version;
            highlightRow = -1;
            int n = Mathf.Min(shownCount, ranks.Length);
            for (int i = 0; i < ranks.Length; i++)
            {
                bool on = i < n;
                ranks[i].gameObject.SetActive(on);
                namesText[i].gameObject.SetActive(on);
                scores[i].gameObject.SetActive(on);
                if (i < medals.Length) medals[i].gameObject.SetActive(on);
                if (!on) continue;
                Entry(i, out string name, out int score);
                ranks[i].Text = (i + 1).ToString("D2");
                namesText[i].Text = string.IsNullOrEmpty(name) ? "---" : name.ToUpperInvariant();
                scores[i].Text = score.ToString("D6");
                if (i < medals.Length) medals[i].color = MedalColors[i];
                if (highlightRow < 0 && score == highlightScore && (highlightName == null || name == highlightName)) highlightRow = i;
                Paint(i, Row);
            }
            if (emptyText != null) emptyText.gameObject.SetActive(n == 0);
        }

        void Paint(int i, Color c)
        {
            ranks[i].Color = c;
            namesText[i].Color = c;
            scores[i].Color = c;
        }

        void Update()
        {
            bool changed = level != null
                ? shownVersion != LevelScores.Version || shownCount < 0
                : !ReferenceEquals(HighscoreManager.Highscores, shownList) || EntryCount != shownCount;   // le classement arrive du serveur
            if (changed) Refresh();
            if (highlightRow >= 0)
            {
                float k = 0.5f + 0.5f * Mathf.Cos(Time.unscaledTime * 6f);
                Paint(highlightRow, Color.Lerp(Color.white, Gold, k));
            }
        }
    }
}
