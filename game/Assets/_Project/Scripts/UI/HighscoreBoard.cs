using Anatidae;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>
    /// Classement en pixel art (docs/da-kenta.md § 8.7) : « MEILLEURS SCORES », top 10 lu dans Anatidae.HighscoreManager,
    /// pièces or, argent et bronze devant les trois premiers, et le score qu'on vient d'entrer qui pulse en or.
    /// Remplace l'affichage du toolkit (police Arial) ; les données et l'envoi restent ceux du toolkit.
    /// </summary>
    public class HighscoreBoard : MonoBehaviour
    {
        [SerializeField] PixelText[] ranks;
        [SerializeField] PixelText[] namesText;
        [SerializeField] PixelText[] scores;
        [Tooltip("Pièces devant les 3 premiers (teintées or, argent, bronze).")]
        [SerializeField] Image[] medals;
        [SerializeField] PixelText emptyText;

        static readonly Color Gold = new(1f, 0.82f, 0.23f);
        static readonly Color Row = new(0.86f, 0.84f, 0.96f);
        static readonly Color[] MedalColors = { new(1f, 0.85f, 0.3f), new(0.82f, 0.86f, 0.92f), new(0.86f, 0.55f, 0.32f) };

        string highlightName;
        int highlightScore = -1, highlightRow = -1;
        object shownList;
        int shownCount = -1;

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
            var list = HighscoreManager.Highscores;
            shownList = list;
            shownCount = list != null ? list.Count : 0;
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
                var e = list[i];
                ranks[i].Text = (i + 1).ToString("D2");
                namesText[i].Text = string.IsNullOrEmpty(e.name) ? "---" : e.name.ToUpperInvariant();
                scores[i].Text = e.score.ToString("D6");
                if (i < medals.Length) medals[i].color = MedalColors[i];
                if (highlightRow < 0 && e.score == highlightScore && (highlightName == null || e.name == highlightName)) highlightRow = i;
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
            var list = HighscoreManager.Highscores;
            if (!ReferenceEquals(list, shownList) || (list != null ? list.Count : 0) != shownCount) Refresh();   // le classement arrive du serveur
            if (highlightRow >= 0)
            {
                float k = 0.5f + 0.5f * Mathf.Cos(Time.unscaledTime * 6f);
                Paint(highlightRow, Color.Lerp(Color.white, Gold, k));
            }
        }
    }
}
