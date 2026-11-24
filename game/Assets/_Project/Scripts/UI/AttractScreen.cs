using Anatidae;
using UnityEngine;

namespace Kenta.UI
{
    /// <summary>
    /// Accueil (docs/da-kenta.md § 8.1). Deux visages, pilotés par GameFlow : le logo KENTA qui flotte sur la Sylve, ou la
    /// démo (une vraie partie en autoplay) avec seulement « DÉMO » et « APPUIE SUR START ». Le texte d'appel respire en
    /// restant lisible (jamais sous 85 % d'opacité) et passe de l'or au blanc. Aucune animation par crans sur le temps.
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
            float t = Time.unscaledTime;
            float k = 0.5f + 0.5f * Mathf.Cos(t * 3.2f);
            pressGroup.alpha = 0.85f + 0.15f * k;
            pressStart.Color = Color.Lerp(Color.white, Gold, k);
            if (logo != null) logo.anchoredPosition = new Vector2(0f, logoY + Mathf.Round(2f * Mathf.Sin(t * 1.6f)));
        }
    }
}
