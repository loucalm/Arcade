using Anatidae;
using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.UI
{
    /// <summary>
    /// Accueil (docs/da-kenta.md § 8.1) : la Sylve seule, le logo KENTA, « APPUIE SUR START » qui clignote sur le temps,
    /// des pièces qui tournent autour du logo, et le bandeau d'arcade (1UP, HI-SCORE, 2UP, CRÉDIT).
    /// </summary>
    public class AttractScreen : MonoBehaviour
    {
        [SerializeField] PixelText pressStart;
        [SerializeField] PixelText hiScore;
        [SerializeField] RectTransform logo;
        [SerializeField] RectTransform[] orbitCoins;
        [SerializeField] Vector2 orbitCenter = new(0f, 14f);
        [SerializeField] Vector2 orbitRadius = new(78f, 22f);

        Conductor subscribed;
        bool visible = true;

        void OnEnable()
        {
            visible = true;
            pressStart.gameObject.SetActive(true);
            RefreshHiScore();
            TrySubscribe();
        }

        void OnDisable()
        {
            if (subscribed != null) subscribed.OnBeat -= OnBeat;
            subscribed = null;
        }

        void RefreshHiScore()
        {
            int best = 0;
            if (HighscoreManager.HasFetchedHighscores && HighscoreManager.Highscores != null && HighscoreManager.Highscores.Count > 0)
                best = HighscoreManager.Highscores[0].score;
            if (hiScore != null) hiScore.Text = $"HI-SCORE {best:D6}";
        }

        void TrySubscribe()
        {
            if (subscribed != null || Conductor.Instance == null) return;
            subscribed = Conductor.Instance;
            subscribed.OnBeat += OnBeat;
        }

        void OnBeat(int beat)
        {
            // « APPUIE SUR START » rebondit sur le temps : il bascule d'1 px à chaque temps et clignote à la mesure
            visible = beat % 4 != 3;
            pressStart.gameObject.SetActive(visible);
            var rt = (RectTransform)pressStart.transform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, (beat & 1) == 0 ? -42f : -41f);
        }

        void Update()
        {
            if (subscribed == null) TrySubscribe();
            if (orbitCoins == null) return;
            float t = Time.unscaledTime * 0.9f;
            for (int i = 0; i < orbitCoins.Length; i++)
            {
                float a = t + i * Mathf.PI * 2f / orbitCoins.Length;
                orbitCoins[i].anchoredPosition = new Vector2(Mathf.Round(orbitCenter.x + Mathf.Cos(a) * orbitRadius.x),
                                                             Mathf.Round(orbitCenter.y + Mathf.Sin(a) * orbitRadius.y));
            }
            if (logo != null) logo.anchoredPosition = new Vector2(0f, 16f + Mathf.Round(Mathf.Sin(Time.unscaledTime * 3f)));
        }
    }
}
