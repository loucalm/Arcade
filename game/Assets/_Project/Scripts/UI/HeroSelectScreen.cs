using System;
using Kenta.Core;
using Kenta.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>
    /// Choix du héros et du monde (docs/da-kenta.md § 8.2) : joystick gauche/droite pour le héros, haut/bas pour le monde,
    /// bouton 1 pour valider. Le décor derrière montre le monde choisi. 12 s pour choisir : à la fin, on part avec la
    /// sélection en cours. Si le joueur rejoue dans la minute, ses derniers choix sont repris.
    /// </summary>
    public class HeroSelectScreen : MonoBehaviour
    {
        [SerializeField] LevelTheme theme;
        [SerializeField] Image[] portraits;
        [SerializeField] Image[] frames;
        [SerializeField] PixelText[] names;
        [SerializeField] PixelText timerText;
        [Tooltip("« ▲ DÉCOR 1/8 : LA SYLVE ▼ »")]
        [SerializeField] PixelText worldText;
        [SerializeField] float seconds = 12f;

        int index, level;
        float timer;
        Vector2 stickPrev;
        LevelCatalog catalog;

        public bool Done { get; private set; }
        /// <summary>Le monde affiché change (GameFlow met le décor à jour).</summary>
        public event Action<LevelDef> OnLevelChanged;

        /// <summary>Mondes proposés (le catalogue des niveaux), avant d'afficher l'écran.</summary>
        public void Setup(LevelCatalog levels) => catalog = levels;

        int LevelCount => catalog != null && catalog.levels != null ? catalog.levels.Length : 0;
        LevelDef CurrentLevel => LevelCount > 0 ? catalog.levels[Mathf.Clamp(level, 0, LevelCount - 1)] : null;

        void OnEnable()
        {
            bool retry = GameSession.IsQuickRetry;
            index = retry ? Mathf.Clamp(GameSession.HeroIndex, 0, theme.heroes.Length - 1) : 0;
            level = retry ? Mathf.Clamp(GameSession.LevelIndex, 0, Mathf.Max(0, LevelCount - 1)) : 0;
            timer = seconds;
            Done = false;
            stickPrev = ArcadeInput.Stick(1);
            Refresh();
            OnLevelChanged?.Invoke(CurrentLevel);
        }

        void Update()
        {
            if (Done) return;
            timer -= Time.unscaledDeltaTime;
            timerText.Text = Mathf.Max(0, Mathf.CeilToInt(timer)).ToString("D2");

            var stick = ArcadeInput.Stick(1);
            if (Flick(stick.x, stickPrev.x)) MoveHero(stick.x > 0 ? 1 : -1);
            if (Flick(stick.y, stickPrev.y)) MoveLevel(stick.y > 0 ? -1 : 1);     // haut = monde précédent
            stickPrev = stick;

            // garde de 0,3 s : l'appui qui a passé les consignes ne doit pas valider le héros dans la même image
            bool guard = seconds - timer < 0.3f;
            if ((!guard && ArcadeInput.ConfirmDown(1)) || timer <= 0f) Confirm();
        }

        static bool Flick(float now, float before) => Mathf.Abs(now) > 0.5f && Mathf.Abs(before) <= 0.5f;

        void MoveHero(int d)
        {
            index = (index + d + theme.heroes.Length) % theme.heroes.Length;
            Refresh();
        }

        void MoveLevel(int d)
        {
            if (LevelCount <= 1) return;
            level = (level + d + LevelCount) % LevelCount;
            Refresh();
            OnLevelChanged?.Invoke(CurrentLevel);
        }

        void Confirm()
        {
            GameSession.HeroIndex = index;
            GameSession.Hero = theme.heroes[index];
            GameSession.LevelIndex = level;
            GameSession.Level = CurrentLevel;
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
            if (worldText != null)
            {
                var l = CurrentLevel;
                worldText.Text = l != null ? $"▲  DÉCOR {level + 1}/{LevelCount} : {l.Label}  ▼" : "";
            }
        }
    }
}
