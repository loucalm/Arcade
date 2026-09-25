using RythmeRunner.Core;
using RythmeRunner.Level;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RythmeRunner.UI
{
    /// <summary>
    /// HUD de la partie : score (compteur animé), multiplicateur, lums, cœurs, progression du morceau,
    /// bannière de section et messages courts (PARFAIT !, AÏE !…).
    /// </summary>
    public class RunHud : MonoBehaviour
    {
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text multiplierText;
        [SerializeField] TMP_Text lumsText;
        [SerializeField] TMP_Text bannerText;
        [SerializeField] TMP_Text feedbackText;
        [SerializeField] Image[] hearts;
        [SerializeField] RectTransform progressFill;
        [SerializeField] Color heartOn = new(1f, 0.25f, 0.75f);
        [SerializeField] Color heartOff = new(1f, 1f, 1f, 0.15f);

        RunManager run;
        float shownScore;
        int lastMultiplier = 1;
        float multiplierPulse, lumsPulse, bannerTimer, feedbackTimer;
        const float BannerDuration = 1.6f, FeedbackDuration = 1.2f;

        void OnEnable()
        {
            run = RunManager.Instance;
            if (run == null) return;
            run.OnRunStarted += HandleRunStarted;
            run.OnStatsChanged += HandleStats;
            run.OnSectionChanged += HandleSection;
            run.OnDamaged += HandleDamaged;
            run.OnRespawned += HandleRespawned;
            run.OnLumPicked += HandleLum;
            shownScore = run.Score;
            HandleStats();
        }

        void OnDisable()
        {
            if (run == null) return;
            run.OnRunStarted -= HandleRunStarted;
            run.OnStatsChanged -= HandleStats;
            run.OnSectionChanged -= HandleSection;
            run.OnDamaged -= HandleDamaged;
            run.OnRespawned -= HandleRespawned;
            run.OnLumPicked -= HandleLum;
        }

        void HandleRunStarted()
        {
            shownScore = 0;
            ShowBanner("C'EST PARTI !");
        }

        void HandleStats()
        {
            if (run == null) return;
            int multiplier = run.Multiplier;
            if (multiplier > lastMultiplier) multiplierPulse = 1f;
            lastMultiplier = multiplier;
            multiplierText.text = $"x{multiplier}";
            lumsText.text = $"LUMS {run.Lums}/{run.TotalLums}";
            for (int i = 0; i < hearts.Length; i++)
                hearts[i].color = i < run.Hearts ? heartOn : heartOff;
        }

        void HandleSection(ChartSection section, bool perfect, int bonus)
        {
            ShowBanner(section.name.ToUpperInvariant());
            if (perfect) ShowFeedback($"PARFAIT !  +{bonus}", new Color(0.4f, 1f, 0.6f));
        }

        void HandleDamaged() => ShowFeedback(run.Hearts > 0 ? "AÏE !" : "PLUS DE CŒURS…", new Color(1f, 0.3f, 0.35f));
        void HandleRespawned() => ShowBanner("CHECKPOINT");
        void HandleLum(LevelObject lum) => lumsPulse = 1f;

        void ShowBanner(string text)
        {
            bannerText.text = text;
            bannerTimer = BannerDuration;
        }

        void ShowFeedback(string text, Color color)
        {
            feedbackText.text = text;
            feedbackText.color = color;
            feedbackTimer = FeedbackDuration;
        }

        void Update()
        {
            if (run == null) return;
            float dt = Time.unscaledDeltaTime;

            // Le score « compte » jusqu'à sa valeur : plus satisfaisant qu'un saut sec.
            shownScore = Mathf.MoveTowards(shownScore, run.Score, Mathf.Max(60f, Mathf.Abs(run.Score - shownScore) * 8f) * dt);
            scoreText.text = Mathf.RoundToInt(shownScore).ToString("N0");

            multiplierPulse = Mathf.MoveTowards(multiplierPulse, 0f, dt * 3f);
            multiplierText.transform.localScale = Vector3.one * (1f + 0.5f * multiplierPulse);
            lumsPulse = Mathf.MoveTowards(lumsPulse, 0f, dt * 5f);
            lumsText.transform.localScale = Vector3.one * (1f + 0.15f * lumsPulse);

            progressFill.anchorMax = new Vector2(run.Progress, 1f);

            bannerTimer = Mathf.Max(0f, bannerTimer - dt);
            float bannerT = bannerTimer / BannerDuration;
            bannerText.alpha = Mathf.Clamp01(bannerT * 3f);
            bannerText.transform.localScale = Vector3.one * (1f + 0.4f * Mathf.Pow(bannerT, 6f));

            feedbackTimer = Mathf.Max(0f, feedbackTimer - dt);
            feedbackText.alpha = Mathf.Clamp01(feedbackTimer / FeedbackDuration * 2f);
        }
    }
}
