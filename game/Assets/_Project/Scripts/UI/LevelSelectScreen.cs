using System;
using System.Collections.Generic;
using Kenta.Core;
using Kenta.Level;
using Kenta.Rhythm;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>
    /// Choix du niveau (ADR-032), après le héros : liste façon osu! à droite (joystick haut/bas, la liste boucle et
    /// glisse), infos et top 5 du niveau à gauche, décor du niveau derrière (GameFlow, via OnLevelChanged) et extrait
    /// de son morceau. Bouton 1 = jouer, bouton 2 = retour au héros ; 15 s pour choisir, puis on part sur le niveau
    /// en surbrillance.
    /// </summary>
    public class LevelSelectScreen : MonoBehaviour
    {
        [SerializeField] RectTransform listRoot;
        [Tooltip("Carte modèle, inactive dans le prefab : une copie par niveau du catalogue.")]
        [SerializeField] LevelCard cardTemplate;
        [SerializeField] PixelText nameText;
        [SerializeField] PixelText songText;
        [SerializeField] PixelText infoText;
        [SerializeField] PixelText timerText;
        [SerializeField] Image[] stars;
        [SerializeField] PixelText[] boardNames;
        [SerializeField] PixelText[] boardScores;
        [SerializeField] PixelFontData font;
        [SerializeField] float seconds = 15f;
        [Tooltip("Délai avant d'accepter une validation : l'appui qui a validé le héros ne lance pas la partie.")]
        [SerializeField] float guard = 0.3f;
        [Tooltip("La sélection doit rester stable ce temps-là avant de lancer l'extrait (défilement rapide).")]
        [SerializeField] float previewDelay = 0.25f;
        [SerializeField] float scrollSharpness = 18f;
        [SerializeField] int nameMaxWidth = 118;

        static readonly Color Gold = new(1f, 0.82f, 0.23f);
        static readonly Color StarOff = new(70 / 255f, 64 / 255f, 100 / 255f);
        static readonly Color EmptyRow = new(90 / 255f, 84 / 255f, 130 / 255f);

        readonly List<LevelCard> cards = new();
        readonly StickRepeat repeat = new();
        LevelCatalog catalog;
        int cursor, shownIndex = -1, shownVersion = -1, shownSeconds = -1;
        float scroll, timer, elapsed, stableTime;
        bool previewed;

        public bool Done { get; private set; }
        public bool Back { get; private set; }
        public int SelectedIndex => LevelListLayout.Wrap(cursor, Count);
        public LevelDef Selected => Count > 0 ? catalog.levels[SelectedIndex] : null;

        /// <summary>Le niveau en surbrillance change (GameFlow met le décor à jour).</summary>
        public event Action<LevelDef> OnLevelChanged;

        int Count => catalog != null && catalog.levels != null ? catalog.levels.Length : 0;

        /// <summary>Niveaux proposés et niveau présélectionné, avant d'afficher l'écran.</summary>
        public void Setup(LevelCatalog levels, int startIndex)
        {
            catalog = levels;
            cursor = Count > 0 ? LevelListLayout.Wrap(startIndex, Count) : 0;
            scroll = cursor;
        }

        /// <summary>Nom du niveau en ×2 s'il tient dans la largeur du panneau, sinon en ×1.</summary>
        public static int NameScale(PixelFontData font, string label, int maxWidth) =>
            PixelText.MeasureWidth(font, label, 2) <= maxWidth ? 2 : 1;

        /// <summary>Aucune action pendant `seconds` : GameFlow revient à l'écran d'accueil.</summary>
        public bool TimedOut { get; private set; }

        void OnEnable()
        {
            EnsureCards();
            timer = seconds;
            elapsed = stableTime = 0f;
            Done = Back = TimedOut = false;
            previewed = false;
            shownIndex = shownVersion = shownSeconds = -1;
            repeat.Hold(ArcadeInput.Stick(1).y);
            scroll = cursor;
            Layout();
            RefreshPanel();
            OnLevelChanged?.Invoke(Selected);
        }

        void EnsureCards()
        {
            if (cardTemplate == null) return;
            cardTemplate.gameObject.SetActive(false);
            while (cards.Count < Count)
            {
                var card = Instantiate(cardTemplate, listRoot);
                card.name = $"Card{cards.Count + 1}";
                card.gameObject.SetActive(true);
                cards.Add(card);
            }
        }

        void Update()
        {
            if (Done || Back || TimedOut || Count == 0) return;
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            timer -= dt;
            if (ArcadeInput.AnyActivity()) timer = seconds;    // on touche : le délai repart
            int left = Mathf.Max(0, Mathf.CeilToInt(timer));
            if (left != shownSeconds) { shownSeconds = left; timerText.Text = left.ToString("D2"); }

            int step = repeat.Step(ArcadeInput.Stick(1).y, dt);
            if (step != 0) Move(-step);                  // haut = niveau précédent

            scroll = Mathf.Lerp(scroll, cursor, 1f - Mathf.Exp(-dt * scrollSharpness));
            if (Mathf.Abs(scroll - cursor) < 0.01f) scroll = cursor;
            Layout();

            if (shownIndex != SelectedIndex || shownVersion != LevelScores.Version) RefreshPanel();

            stableTime += dt;
            if (!previewed && stableTime >= previewDelay) PlayPreview();

            bool ready = elapsed >= guard;
            if (ready && ArcadeInput.ConfirmDown(1)) Confirm();
            else if (ready && ArcadeInput.HitDown(1)) Back = true;
            else if (timer <= 0f) TimedOut = true;           // personne : retour à l'accueil (GameFlow)
        }

        void Move(int d)
        {
            cursor += d;
            stableTime = 0f;
            previewed = false;
            OnLevelChanged?.Invoke(Selected);
        }

        void Layout()
        {
            int n = Count;
            int sel = SelectedIndex;
            for (int i = 0; i < cards.Count; i++)
            {
                // les cartes restent actives : les réactiver recréerait les glyphes de leurs textes (allocations à chaque
                // cran) ; celles qui sont loin de la sélection sont simplement posées hors de l'écran 320×180
                var card = cards[i];
                bool exists = i < n;
                if (card.gameObject.activeSelf != exists) card.gameObject.SetActive(exists);
                if (!exists) continue;
                var r = LevelListLayout.CardRect(LevelListLayout.Offset(i, scroll, n));
                card.Place(r);
                card.Show(catalog.levels[i], i == sel);
            }
        }

        void RefreshPanel()
        {
            var level = Selected;
            shownIndex = SelectedIndex;
            shownVersion = LevelScores.Version;
            if (level == null) return;

            string label = level.Label.ToUpperInvariant();
            nameText.Scale = NameScale(font, label, nameMaxWidth);
            nameText.Text = label;
            var song = level.song;
            songText.Text = song != null ? song.title.ToUpperInvariant() : "";
            infoText.Text = song != null ? LevelListLayout.FormatInfo(song.bpm, song.clip != null ? song.clip.length : 0f) : "";
            for (int i = 0; i < stars.Length; i++) stars[i].color = i < level.stars ? Gold : StarOff;

            var top = LevelScores.Top(level.id);
            for (int k = 0; k < boardNames.Length; k++)
            {
                bool filled = k < top.Count;
                var color = !filled ? EmptyRow : k == 0 ? Gold : Color.white;
                boardNames[k].Text = filled ? $"{k + 1} {top[k].name.ToUpperInvariant()}" : $"{k + 1} ---";
                boardNames[k].Color = color;
                if (k < boardScores.Length)
                {
                    boardScores[k].Text = filled ? top[k].score.ToString() : "";
                    boardScores[k].Color = color;
                }
            }
        }

        void PlayPreview()
        {
            previewed = true;
            var level = Selected;
            var conductor = Conductor.Instance;
            if (level == null || level.song == null || level.song.clip == null || conductor == null) return;
            if (conductor.IsPlaying && conductor.Song == level.song) return;
            conductor.Play(level.song, level.song.previewBeat, loop: true);
        }

        void Confirm()
        {
            GameSession.LevelIndex = SelectedIndex;
            GameSession.Level = Selected;
            Done = true;
        }
    }
}
