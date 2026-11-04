using Kenta.Core;
using Kenta.Level;
using Kenta.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>
    /// Choix du héros (docs/da-kenta.md § 8.2) : joystick gauche/droite, bouton 1 pour valider. Le héros choisi monte d'un cran
    /// et prend sa pose de course. 12 s pour choisir, sinon Kenta.
    /// </summary>
    public class HeroSelectScreen : MonoBehaviour
    {
        [SerializeField] LevelTheme theme;
        [SerializeField] Image[] portraits;
        [SerializeField] Image[] frames;
        [SerializeField] PixelText[] names;
        [SerializeField] PixelText timerText;
        [SerializeField] float seconds = 12f;

        int index;
        float timer;
        float stickPrev;

        public bool Done { get; private set; }

        void OnEnable()
        {
            index = 0;
            timer = seconds;
            Done = false;
            stickPrev = 0f;
            Refresh();
        }

        void Update()
        {
            if (Done) return;
            timer -= Time.unscaledDeltaTime;
            timerText.Text = Mathf.Max(0, Mathf.CeilToInt(timer)).ToString("D2");

            float x = ArcadeInput.Stick(1).x;
            bool flick = Mathf.Abs(x) > 0.5f && Mathf.Abs(stickPrev) <= 0.5f;
            stickPrev = x;
            if (flick) Move(x > 0 ? 1 : -1);
            if (ArcadeInput.ConfirmDown(1)) Confirm();
            else if (timer <= 0f) { index = 0; Confirm(); }
        }

        void Move(int d)
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
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, sel ? -42f : -46f);
                frames[i].color = sel ? hero.accent : new Color(0.35f, 0.3f, 0.5f);
                names[i].Color = sel ? Color.white : new Color(0.6f, 0.58f, 0.75f);
            }
        }
    }
}
