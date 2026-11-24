using Kenta.Core;
using Kenta.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>
    /// Saisie des trois lettres (docs/da-kenta.md § 8.6), en pixel art : joystick haut/bas pour la lettre, bouton 1 pour
    /// passer à la suivante, bouton 2 pour revenir. La lettre active pulse en or ; le héros choisi est à droite.
    /// Validation automatique au bout de 20 s. GameFlow envoie ensuite le score par Anatidae.HighscoreManager.SetHighscore.
    /// </summary>
    public class NameEntryScreen : MonoBehaviour
    {
        public const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        [SerializeField] LevelTheme theme;
        [SerializeField] PixelText[] letters;          // 3 lettres
        [SerializeField] PixelText scoreText;
        [SerializeField] PixelText timerText;
        [SerializeField] Image hero;
        [SerializeField] float seconds = 20f;
        [SerializeField] float repeatDelay = 0.35f, repeatRate = 0.12f;

        static readonly string[] Glyphs = BuildGlyphs();        // une chaîne par lettre : pas d'allocation par image
        static string[] BuildGlyphs()
        {
            var g = new string[Alphabet.Length];
            for (int i = 0; i < g.Length; i++) g[i] = Alphabet[i].ToString();
            return g;
        }

        static readonly Color Gold = new(1f, 0.82f, 0.23f);
        static readonly Color Done_ = new(0.86f, 0.84f, 0.96f);

        readonly int[] chars = new int[3];
        int cursor;
        float timer, guard, holdTime, nextRepeat;
        int holdDir;

        public bool Done { get; private set; }
        public string PlayerName { get; private set; } = "AAA";
        public int Score { get; private set; }

        /// <summary>Ouvre la saisie pour ce score ; reprend le dernier nom tapé sur la borne, s'il existe.</summary>
        public void Begin(int score, string lastName)
        {
            Score = score;
            for (int i = 0; i < 3; i++)
            {
                int k = lastName != null && i < lastName.Length ? Alphabet.IndexOf(char.ToUpperInvariant(lastName[i])) : 0;
                chars[i] = k < 0 ? 0 : k;
            }
            cursor = 0;
            timer = seconds;
            guard = 0.4f;
            holdDir = 0;
            Done = false;
            if (scoreText != null) scoreText.Text = $"SCORE {score:D6}";
            var profile = GameSession.Hero != null ? GameSession.Hero : theme.heroes[0];
            if (hero != null)
            {
                hero.sprite = profile.sprites.jump;           // bondit de joie (pas de pose de victoire dans le PDF)
                ((RectTransform)hero.transform).sizeDelta = hero.sprite.rect.size;
            }
            Paint();
        }

        void Update()
        {
            if (Done) return;
            float dt = Time.unscaledDeltaTime;
            timer -= dt;
            guard -= dt;
            if (timerText != null) timerText.Text = Mathf.Max(0, Mathf.CeilToInt(timer)).ToString("D2");
            if (timer <= 0f) { Submit(); return; }
            if (guard > 0f) { Paint(); return; }

            float y = ArcadeInput.Stick(1).y;
            int dir = y > 0.5f ? -1 : y < -0.5f ? 1 : 0;                   // haut = lettre précédente (A … Z, puis 0 … 9)
            if (dir != holdDir) { holdDir = dir; holdTime = 0f; nextRepeat = repeatDelay; if (dir != 0) Step(dir); }
            else if (dir != 0)
            {
                holdTime += dt;
                if (holdTime >= nextRepeat) { nextRepeat += repeatRate; Step(dir); }
            }

            if (ArcadeInput.ConfirmDown(1))
            {
                if (cursor < 2) cursor++;
                else { Submit(); return; }
            }
            else if (ArcadeInput.HitDown(1) && cursor > 0) cursor--;
            Paint();
        }

        void Step(int dir) => chars[cursor] = (chars[cursor] + dir + Alphabet.Length) % Alphabet.Length;

        void Submit()
        {
            PlayerName = new string(new[] { Alphabet[chars[0]], Alphabet[chars[1]], Alphabet[chars[2]] });
            Done = true;
            Paint();
        }

        void Paint()
        {
            float k = 0.5f + 0.5f * Mathf.Cos(Time.unscaledTime * 6f);
            for (int i = 0; i < letters.Length; i++)
            {
                letters[i].Text = Glyphs[chars[i]];
                letters[i].Color = Done ? Gold : i == cursor ? Color.Lerp(Color.white, Gold, k) : i < cursor ? Done_ : new Color(0.6f, 0.58f, 0.75f);
            }
        }
    }
}
