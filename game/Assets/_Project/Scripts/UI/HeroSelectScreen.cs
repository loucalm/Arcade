using Kenta.Core;
using Kenta.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>
    /// Choix du héros (docs/da-kenta.md § 8.2) : joystick gauche/droite, bouton 1 pour valider ; le niveau se choisit
    /// à l'écran suivant (LevelSelectScreen, ADR-032). 12 s pour choisir : à la fin, on part avec la sélection en cours.
    /// GameFlow donne le héros présélectionné (rejouer dans la minute, ou retour depuis le choix du niveau).
    /// Chaque héros a sa difficulté (HeroProfile.retryMode) : FACILE / MOYEN / DUR en haut de son cadre, et la règle du
    /// héros sélectionné au-dessus des cadres. Ces textes sont créés à l'exécution (copies du nom : même police, même
    /// plaque), le prefab n'a pas à être reconstruit.
    /// </summary>
    public class HeroSelectScreen : MonoBehaviour
    {
        [SerializeField] LevelTheme theme;
        [SerializeField] Image[] portraits;
        [SerializeField] Image[] frames;
        [SerializeField] PixelText[] names;
        [SerializeField] PixelText timerText;
        [SerializeField] float seconds = 12f;

        // en pixels d'interface : le mot sous le bord haut du cadre (au-dessus de la tête du héros), la règle entre le titre et les cadres
        const float WordBelowFrameTop = 11f, RuleAboveFrameTop = 12f;

        int index, startIndex;
        float timer;
        Vector2 stickPrev;
        PixelText[] difficultyTexts;
        PixelText ruleText;
        string[] ruleLines;

        public bool Done { get; private set; }

        /// <summary>Héros présélectionné, avant d'afficher l'écran.</summary>
        public void Setup(int heroIndex) => startIndex = heroIndex;

        /// <summary>Aucune action pendant `seconds` : GameFlow revient à l'écran d'accueil.</summary>
        public bool TimedOut { get; private set; }

        void OnEnable()
        {
            index = Mathf.Clamp(startIndex, 0, theme.heroes.Length - 1);
            timer = seconds;
            Done = TimedOut = false;
            stickPrev = ArcadeInput.Stick(1);
            EnsureDifficultyTexts();
            Refresh();
        }

        void Update()
        {
            if (Done || TimedOut) return;
            timer -= Time.unscaledDeltaTime;
            if (ArcadeInput.AnyInput()) timer = Mathf.Max(timer, seconds - 0.3f);   // on touche : le délai repart (sans rejouer la garde)
            timerText.Text = Mathf.Max(0, Mathf.CeilToInt(timer)).ToString("D2");

            var stick = ArcadeInput.Stick(1);
            if (Flick(stick.x, stickPrev.x)) MoveHero(stick.x > 0 ? 1 : -1);
            stickPrev = stick;

            // garde de 0,3 s : l'appui qui a passé les consignes ne doit pas valider le héros dans la même image
            bool guard = seconds - timer < 0.3f;
            if (!guard && ArcadeInput.ConfirmDown(1)) Confirm();
            else if (timer <= 0f) TimedOut = true;           // personne : retour à l'accueil (GameFlow)
        }

        static bool Flick(float now, float before) => Mathf.Abs(now) > 0.5f && Mathf.Abs(before) <= 0.5f;

        void MoveHero(int d)
        {
            index = (index + d + theme.heroes.Length) % theme.heroes.Length;
            Refresh();
        }

        void Confirm()
        {
            GameSession.HeroIndex = index;
            GameSession.Hero = theme.heroes[index];
            Done = true;
        }

        void Refresh()
        {
            for (int i = 0; i < portraits.Length; i++)
            {
                bool sel = i == index;
                var hero = theme.heroes[i];
                portraits[i].sprite = sel ? hero.sprites.run : hero.sprites.idle;
                ((RectTransform)portraits[i].transform).sizeDelta = portraits[i].sprite.rect.size;
                var rt = (RectTransform)portraits[i].transform;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, sel ? -38f : -42f);
                frames[i].color = sel ? hero.accent : new Color(0.35f, 0.3f, 0.5f);
                names[i].Color = sel ? Color.white : new Color(0.6f, 0.58f, 0.75f);
            }
            if (ruleText != null && ruleLines != null && index < ruleLines.Length) ruleText.Text = ruleLines[index];
        }

        /// <summary>
        /// Textes de difficulté (une fois), puis leur contenu à chaque affichage de l'écran (les réglages des héros ont pu
        /// changer) : le mot de chaque héros dans sa couleur, et la règle de chacun, prête pour le héros sélectionné.
        /// </summary>
        void EnsureDifficultyTexts()
        {
            int n = Mathf.Min(theme.heroes.Length, Mathf.Min(frames.Length, names.Length));
            if (n <= 0) return;
            if (difficultyTexts == null)
            {
                difficultyTexts = new PixelText[n];
                for (int i = 0; i < n; i++)
                {
                    var frame = frames[i].rectTransform;
                    difficultyTexts[i] = CloneText(names[i], $"Difficulty{i}",
                        new Vector2(frame.anchoredPosition.x, FrameTop(frame) - WordBelowFrameTop));
                }
                ruleText = CloneText(names[0], "DifficultyRule", new Vector2(0f, FrameTop(frames[0].rectTransform) + RuleAboveFrameTop));
                ruleText.Color = Color.white;
                ruleLines = new string[n];
            }
            for (int i = 0; i < n; i++)
            {
                var hero = theme.heroes[i];
                difficultyTexts[i].Text = RetryRules.DifficultyLabel(hero.retryMode);
                difficultyTexts[i].Color = DifficultyColor(hero.retryMode);
                ruleLines[i] = RetryRules.RuleLine(hero.retryMode, hero.StartLives);
            }
        }

        /// <summary>Copie d'un texte de l'écran (police, plaque, échelle), posée ailleurs : comme les vies du HUD (RunHud).</summary>
        static PixelText CloneText(PixelText model, string name, Vector2 position)
        {
            var go = Instantiate(model.gameObject, model.transform.parent);
            go.name = name;
            ((RectTransform)go.transform).anchoredPosition = new Vector2(Mathf.Round(position.x), Mathf.Round(position.y));
            return go.GetComponent<PixelText>();
        }

        static float FrameTop(RectTransform frame) => frame.anchoredPosition.y + frame.sizeDelta.y * (1f - frame.pivot.y);

        static Color DifficultyColor(RetryMode mode) => RetryRules.DifficultyColor(mode);   // la même couleur au classement
    }
}
