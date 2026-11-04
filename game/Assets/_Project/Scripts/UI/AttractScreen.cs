using Anatidae;
using UnityEngine;

namespace Kenta.UI
{
    /// <summary>
    /// Accueil (docs/da-kenta.md § 8.1) : la Sylve qui défile, le logo KENTA qui flotte doucement, et « APPUIE SUR START »
    /// qui respire (fondu continu). Aucune animation par crans sur le temps.
    /// </summary>
    public class AttractScreen : MonoBehaviour
    {
        [SerializeField] PixelText pressStart;
        [SerializeField] PixelText hiScore;
        [SerializeField] RectTransform logo;
        [SerializeField] float logoY = 16f;

        CanvasGroup pressGroup;

        void Awake()
        {
            pressGroup = pressStart.GetComponent<CanvasGroup>();
            if (pressGroup == null) pressGroup = pressStart.gameObject.AddComponent<CanvasGroup>();
        }

        void OnEnable() => RefreshHiScore();

        void RefreshHiScore()
        {
            int best = 0;
            if (HighscoreManager.HasFetchedHighscores && HighscoreManager.Highscores != null && HighscoreManager.Highscores.Count > 0)
                best = HighscoreManager.Highscores[0].score;
            if (hiScore != null) hiScore.Text = $"HI-SCORE {best:D6}";
        }

        void Update()
        {
            float t = Time.unscaledTime;
            pressGroup.alpha = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Cos(t * 3.2f));
            if (logo != null) logo.anchoredPosition = new Vector2(0f, logoY + Mathf.Round(2f * Mathf.Sin(t * 1.6f)));
        }
    }
}
