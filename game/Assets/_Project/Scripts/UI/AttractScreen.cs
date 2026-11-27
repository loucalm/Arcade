using Anatidae;
using Kenta.FX;
using UnityEngine;

namespace Kenta.UI
{
    /// <summary>
    /// Accueil (docs/da-kenta.md § 8.1). Deux visages, pilotés par GameFlow : le logo KENTA qui flotte sur la Sylve, ou la
    /// démo (une vraie partie en autoplay) avec seulement « DÉMO » et « APPUIE SUR START ». Le texte d'appel respire en
    /// restant lisible (jamais sous 85 % d'opacité) et passe de l'or au blanc. Aucune animation par crans sur le temps.
    /// Entrée façon borne d'arcade à chaque retour du logo (PlayIntro) : le logo tombe du haut et rebondit, impact
    /// (flash blanc, secousse, logo qui tremble), le sous-titre et le meilleur score glissent des côtés, puis l'appel
    /// s'allume. Positions toujours en pixels entiers.
    /// </summary>
    public class AttractScreen : MonoBehaviour
    {
        [SerializeField] PixelText pressStart;
        [SerializeField] PixelText hiScore;
        [SerializeField] RectTransform logo;
        [SerializeField] float logoY = 16f;
        [Tooltip("Éléments masqués pendant la démo (logo, sous-titre, meilleur score).")]
        [SerializeField] GameObject[] logoOnly;
        [Tooltip("« DÉMO », visible seulement pendant la démo.")]
        [SerializeField] GameObject demoLabel;

        static readonly Color Gold = new(1f, 0.82f, 0.23f);
        CanvasGroup pressGroup;

        // entrée façon arcade (secondes depuis PlayIntro ; < 0 = pas d'entrée en cours)
        const float Drop = 0.5f, Impact = 0.5f, ShakeEnd = 0.85f, SlideStart = 0.75f, SlideEnd = 1.2f, PressAt = 1.35f, IntroEnd = 1.6f;
        float intro = -1f;
        RectTransform[] sliders;
        Vector2[] sliderBase;
        bool impactDone, pressDone;

        void Awake()
        {
            pressGroup = pressStart.GetComponent<CanvasGroup>();
            if (pressGroup == null) pressGroup = pressStart.gameObject.AddComponent<CanvasGroup>();
        }

        void OnEnable() => RefreshHiScore();

        /// <summary>Vrai : la démo tourne derrière (logo masqué). Faux : le logo.</summary>
        public void SetDemo(bool demo)
        {
            if (logoOnly != null)
                foreach (var go in logoOnly) if (go != null) go.SetActive(!demo);
            if (demoLabel != null) demoLabel.SetActive(demo);
            // pendant la démo, l'appel descend en bas de l'écran : il ne cache plus le héros
            var rt = (RectTransform)pressStart.transform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, demo ? -76f : -46f);
            RefreshHiScore();
            if (!demo) PlayIntro();
        }

        /// <summary>Rejoue l'entrée du titre (logo qui tombe, impact, textes qui glissent, appel qui s'allume).</summary>
        public void PlayIntro()
        {
            if (sliders == null && logoOnly != null)
            {
                var list = new System.Collections.Generic.List<RectTransform>();
                foreach (var go in logoOnly)
                    if (go != null && (logo == null || go != logo.gameObject)) list.Add((RectTransform)go.transform);
                sliders = list.ToArray();
                sliderBase = new Vector2[sliders.Length];
                for (int i = 0; i < sliders.Length; i++) sliderBase[i] = sliders[i].anchoredPosition;
            }
            intro = 0f;
            impactDone = pressDone = false;
            Apply(0f);
        }

        static float EaseOutBack(float x)
        {
            const float c = 1.9f;
            x -= 1f;
            return 1f + (c + 1f) * x * x * x + c * x * x;
        }

        static float EaseOutCubic(float x) => 1f - (1f - x) * (1f - x) * (1f - x);

        /// <summary>Place le logo, les textes et l'appel à l'instant t de l'entrée.</summary>
        void Apply(float t)
        {
            if (logo != null)
            {
                float k = EaseOutBack(Mathf.Clamp01(t / Drop));
                float y = Mathf.LerpUnclamped(logoY + 130f, logoY, k);
                float jx = 0f, jy = 0f;
                if (t > Impact && t < ShakeEnd)                     // le logo tremble à l'impact, de moins en moins
                {
                    float a = 3f * (1f - (t - Impact) / (ShakeEnd - Impact));
                    jx = Mathf.Round(Mathf.Sin(t * 90f) * a);
                    jy = Mathf.Round(Mathf.Cos(t * 77f) * a * 0.5f);
                }
                logo.anchoredPosition = new Vector2(jx, Mathf.Round(y) + jy);
            }
            if (sliders != null)
                for (int i = 0; i < sliders.Length; i++)
                {
                    float k = EaseOutCubic(Mathf.Clamp01((t - SlideStart - 0.08f * i) / (SlideEnd - SlideStart)));
                    float from = (i % 2 == 0 ? -1f : 1f) * 340f;        // un de chaque côté
                    sliders[i].anchoredPosition = new Vector2(Mathf.Round(Mathf.Lerp(from, 0f, k)) + sliderBase[i].x, sliderBase[i].y);
                }
            pressGroup.alpha = t < PressAt ? 0f : pressGroup.alpha;
        }

        void RefreshHiScore()
        {
            int best = 0;
            if (HighscoreManager.HasFetchedHighscores && HighscoreManager.Highscores != null && HighscoreManager.Highscores.Count > 0)
                best = HighscoreManager.Highscores[0].score;
            if (hiScore != null)
            {
                hiScore.Text = best > 0 ? $"HI-SCORE {best:D6}" : "";
                hiScore.gameObject.SetActive(best > 0 && (logo == null || logo.gameObject.activeSelf));
            }
        }

        void Update()
        {
            if (intro >= 0f)
            {
                intro += Time.unscaledDeltaTime;
                if (!impactDone && intro >= Impact)
                {
                    impactDone = true;
                    ScreenFx.Instance?.Flash(Color.white, 0.9f);
                    CameraRig.Instance?.Shake(0.2f, 0.35f);
                }
                if (!pressDone && intro >= PressAt)
                {
                    pressDone = true;
                    ScreenFx.Instance?.Flash(Gold, 0.35f);          // l'appel s'allume d'un éclat doré
                }
                Apply(intro);
                if (intro < PressAt) return;
                if (intro >= IntroEnd) intro = -1f;
            }
            float t = Time.unscaledTime;
            float k = 0.5f + 0.5f * Mathf.Cos(t * 3.2f);
            pressGroup.alpha = 0.85f + 0.15f * k;
            pressStart.Color = Color.Lerp(Color.white, Gold, k);
            if (logo != null && intro < 0f) logo.anchoredPosition = new Vector2(0f, logoY + Mathf.Round(2f * Mathf.Sin(t * 1.6f)));
        }
    }
}
