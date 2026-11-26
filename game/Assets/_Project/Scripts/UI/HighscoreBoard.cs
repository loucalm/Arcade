using Anatidae;
using Kenta.Core;
using Kenta.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>
    /// Classement en pixel art (docs/da-kenta.md § 8.7) : top 10, une ligne par trigramme. Colonnes rang, nom, héros,
    /// score ; petites pièces or, argent et bronze devant les trois premiers ; le score qu'on vient d'entrer pulse en or.
    /// Chaque ligne porte la couleur de la difficulté de son héros (RetryRules.DifficultyColor) : vert facile, ambre
    /// moyen, rouge franc dur, avec une bande rouge et un liseré sur toute la ligne pour un score en DUR. La légende
    /// rappelle le bonus de score (×1,5 moyen, ×2 dur). Deux sources : le classement d'un niveau (« CLASSEMENT : … »,
    /// LevelScores, qui garde le héros de chaque score) ou la liste officielle de la borne (« MEILLEURS SCORES »,
    /// Anatidae.HighscoreManager : nom et score seulement, lignes neutres). Remplace l'affichage du toolkit (Arial).
    /// Construit par KentaBuilder (menu Kenta > Construire le classement).
    /// </summary>
    public class HighscoreBoard : MonoBehaviour
    {
        [SerializeField] PixelText[] ranks;
        [SerializeField] PixelText[] namesText;
        [SerializeField] PixelText[] scores;
        [Tooltip("Héros du score, à la couleur de sa difficulté.")]
        [SerializeField] PixelText[] heroes;
        [Tooltip("Bonus de la difficulté (×1,5, ×2), à côté du héros.")]
        [SerializeField] PixelText[] bonuses;
        [Tooltip("Bande derrière chaque ligne (une sur deux, rouge pour un score en DUR).")]
        [SerializeField] Image[] bands;
        [Tooltip("Liseré à gauche d'une ligne en DUR.")]
        [SerializeField] Image[] accents;
        [Tooltip("Pièces devant les 3 premiers (teintées or, argent, bronze).")]
        [SerializeField] Image[] medals;
        [Tooltip("En-têtes de colonnes et légende : cachés quand le classement est vide.")]
        [SerializeField] GameObject[] decorations;
        [SerializeField] PixelText emptyText;
        [SerializeField] PixelText title;
        [SerializeField] PixelFontData font;

        static readonly Color Gold = new(1f, 0.82f, 0.23f);
        static readonly Color Row = new(0.9f, 0.88f, 0.98f);
        static readonly Color Neutral = new(0.62f, 0.6f, 0.75f);
        static readonly Color[] MedalColors = { new(1f, 0.85f, 0.3f), new(0.82f, 0.86f, 0.92f), new(0.86f, 0.55f, 0.32f) };
        static readonly Color ZebraBand = new(1f, 1f, 1f, 0.05f);
        static readonly Color ClearBand = new(1f, 1f, 1f, 0f);

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

        /// <summary>Une ligne : nom, score, héros (vide pour la liste officielle) et difficulté (−1 : inconnue).</summary>
        void Entry(int i, out string name, out int score, out string hero, out int difficulty)
        {
            if (level != null)
            {
                var e = LevelScores.Top(level.id)[i];
                name = e.name;
                score = e.score;
                hero = e.hero;
                difficulty = string.IsNullOrEmpty(e.hero) && e.difficulty == 0 ? -1 : e.difficulty;   // ancien score : neutre
            }
            else
            {
                var e = HighscoreManager.Highscores[i];
                name = e.name;
                score = e.score;
                hero = null;
                difficulty = -1;
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

        static T At<T>(T[] list, int i) where T : class => list != null && i < list.Length ? list[i] : null;

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
                var heroText = At(heroes, i);
                var bonusText = At(bonuses, i);
                var band = At(bands, i);
                var accent = At(accents, i);
                var medal = At(medals, i);
                if (band != null) band.gameObject.SetActive(on);
                if (medal != null) medal.gameObject.SetActive(on);
                if (!on)
                {
                    if (heroText != null) heroText.gameObject.SetActive(false);
                    if (bonusText != null) bonusText.gameObject.SetActive(false);
                    if (accent != null) accent.gameObject.SetActive(false);
                    continue;
                }
                Entry(i, out string name, out int score, out string hero, out int difficulty);
                ranks[i].Text = (i + 1).ToString("D2");
                namesText[i].Text = string.IsNullOrEmpty(name) ? "---" : name.ToUpperInvariant();
                scores[i].Text = score.ToString("D6");
                if (medal != null) medal.color = MedalColors[i];

                var mode = (RetryMode)Mathf.Max(0, difficulty);
                bool known = difficulty >= 0;
                bool hard = known && mode == RetryMode.OneLife;
                Color tint = known ? RetryRules.DifficultyColor(mode) : Neutral;
                if (heroText != null)
                {
                    heroText.gameObject.SetActive(known);
                    heroText.Text = string.IsNullOrEmpty(hero) ? RetryRules.DifficultyLabel(mode) : hero.ToUpperInvariant();
                    heroText.Color = tint;
                }
                if (bonusText != null)
                {
                    string label = known ? RetryRules.FactorLabel(RetryRules.ScoreFactor(mode)) : "";
                    bonusText.gameObject.SetActive(label.Length > 0);
                    bonusText.Text = label;
                    bonusText.Color = tint;
                }
                if (band != null)
                    band.color = hard ? new Color(tint.r, tint.g, tint.b, 0.3f) : i % 2 == 0 ? ZebraBand : ClearBand;
                if (accent != null)
                {
                    accent.gameObject.SetActive(hard);
                    accent.color = tint;
                }
                if (highlightRow < 0 && score == highlightScore && (highlightName == null || name == highlightName)) highlightRow = i;
                Paint(i, hard ? Color.Lerp(Row, tint, 0.35f) : Row);
            }
            if (emptyText != null) emptyText.gameObject.SetActive(n == 0);
            if (decorations != null)
                foreach (var d in decorations)
                    if (d != null) d.SetActive(n > 0);
        }

        void Paint(int i, Color c)
        {
            ranks[i].Color = c;
            namesText[i].Color = c;
            scores[i].Color = i == 0 && i != highlightRow ? Gold : c;     // le meilleur score en or
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
                var c = Color.Lerp(Color.white, Gold, k);
                ranks[highlightRow].Color = c;
                namesText[highlightRow].Color = c;
                scores[highlightRow].Color = c;
            }
        }
    }
}
