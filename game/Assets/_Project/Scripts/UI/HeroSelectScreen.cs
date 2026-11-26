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
    /// </summary>
    public class HeroSelectScreen : MonoBehaviour
    {
        [SerializeField] LevelTheme theme;
        [SerializeField] Image[] portraits;
        [SerializeField] Image[] frames;
        [SerializeField] PixelText[] names;
        [SerializeField] PixelText timerText;
        [SerializeField] float seconds = 12f;

        int index, startIndex;
        float timer;
        Vector2 stickPrev;

        public bool Done { get; private set; }

        /// <summary>Héros présélectionné, avant d'afficher l'écran.</summary>
        public void Setup(int heroIndex) => startIndex = heroIndex;

        void OnEnable()
        {
            index = Mathf.Clamp(startIndex, 0, theme.heroes.Length - 1);
            timer = seconds;
            Done = false;
            stickPrev = ArcadeInput.Stick(1);
            Refresh();
        }

        void Update()
        {
            if (Done) return;
            timer -= Time.unscaledDeltaTime;
            timerText.Text = Mathf.Max(0, Mathf.CeilToInt(timer)).ToString("D2");

            var stick = ArcadeInput.Stick(1);
            if (Flick(stick.x, stickPrev.x)) MoveHero(stick.x > 0 ? 1 : -1);
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
        }
    }
}
