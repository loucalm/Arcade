using Kenta.Core;
using Kenta.Level;
using Kenta.Rhythm;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>
    /// HUD de la partie (docs/da-kenta.md § 7) : tout en haut, rien au milieu.
    /// Gauche : score et multiplicateur. Centre : frise des 8 biomes (barre de progression) et son intitulé.
    /// Droite : pièces ramassées. Mort subite : pas de cœurs. Dans le monde : « PRÊT ? 1 2 3 GO ! », PARFAIT !
    /// </summary>
    public class RunHud : MonoBehaviour
    {
        [SerializeField] PixelText scoreText;
        [SerializeField] PixelText multiplierText;
        [SerializeField] PixelText coinsText;
        [SerializeField] PixelText biomeText;
        [Tooltip("Durée (s) d'affichage de l'intitulé de section en haut de l'écran : ensuite il s'efface pour ne pas masquer les pièces.")]
        [SerializeField] float sectionLabelSeconds = 3f;
        string sectionLabel = "";
        float sectionLabelTimer;
        [SerializeField] PixelText bannerText;
        [SerializeField] PixelText feedbackText;
        [Tooltip("8 segments de la frise (un par biome) : fond ; le remplissage est un enfant nommé « Fill ».")]
        [SerializeField] Image[] friezeBack;
        [SerializeField] Image[] friezeFill;

        RunManager run;
        float shownScore;
        int lastMultiplier = 1;
        float multiplierPulse, bannerTimer, feedbackTimer;
        const float FeedbackDuration = 1.2f;
        Color feedbackColor = Color.white;

        void OnEnable()
        {
            run = RunManager.Instance;
            if (run == null) return;
            run.OnRunStarted += HandleRunStarted;
            run.OnStatsChanged += HandleStats;
            run.OnSectionChanged += HandleSection;
            run.OnDamaged += HandleDamaged;
            shownScore = run.Score;
            bannerTimer = feedbackTimer = 0f;
            HandleStats();
        }

        void OnDisable()
        {
            if (run == null) return;
            run.OnRunStarted -= HandleRunStarted;
            run.OnStatsChanged -= HandleStats;
            run.OnSectionChanged -= HandleSection;
            run.OnDamaged -= HandleDamaged;
        }

        void HandleRunStarted()
        {
            shownScore = 0;
            sectionLabelTimer = sectionLabelSeconds;
        }

        void HandleStats()
        {
            if (run == null) return;
            int m = run.Multiplier;
            if (m > lastMultiplier) multiplierPulse = 1f;
            lastMultiplier = m;
            multiplierText.Text = $"×{m}";
            coinsText.Text = $"{run.Lums}/{run.TotalLums}";
            var biome = run.CurrentBiome;
            string section = run.SectionName.ToUpperInvariant();
            if (biome != null) sectionLabel = string.IsNullOrEmpty(section) ? biome.label : $"{section} · {biome.label}";
        }

        void HandleSection(ChartSection section, bool perfect, int bonus)
        {
            // un niveau = un monde : pas de grand bandeau au milieu de l'action (il masquait les obstacles), l'intitulé du haut suffit
            HandleStats();
            sectionLabelTimer = sectionLabelSeconds;
            // pas de texte au milieu de l'action : le bonus de section se voit au score (docs/level-design-musical.md § 4)
        }

        void HandleDamaged() => ShowFeedback("SILENCE…", new Color(1f, 0.35f, 0.4f));

        void ShowFeedback(string t, Color c)
        {
            feedbackText.Text = t;
            feedbackText.Color = c;
            feedbackColor = c;
            feedbackTimer = FeedbackDuration;
        }

        void Update()
        {
            if (run == null) return;
            float dt = Time.unscaledDeltaTime;
            shownScore = Mathf.MoveTowards(shownScore, run.Score, Mathf.Max(60f, Mathf.Abs(run.Score - shownScore) * 8f) * dt);
            scoreText.Text = Mathf.RoundToInt(shownScore).ToString("D6");

            multiplierPulse = Mathf.MoveTowards(multiplierPulse, 0f, dt * 3f);
            multiplierText.Scale = multiplierPulse > 0.3f ? 3 : 2;

            UpdateFrieze();
            UpdateCountdown();
            // intitulé de section : visible quelques secondes à chaque nouvelle section, puis effacé (il chevauchait les pièces)
            sectionLabelTimer = Mathf.Max(0f, sectionLabelTimer - dt);
            string label = sectionLabelTimer > 0f ? sectionLabel : "";
            if (biomeText != null && biomeText.Text != label) biomeText.Text = label;

            bannerTimer = Mathf.Max(0f, bannerTimer - dt);
            if (bannerTimer <= 0f && !countdownActive) bannerText.Text = "";
            feedbackTimer = Mathf.Max(0f, feedbackTimer - dt);
            if (feedbackTimer <= 0f) feedbackText.Text = "";
            else if (feedbackTimer < 0.3f) feedbackText.Color = new Color(feedbackColor.r, feedbackColor.g, feedbackColor.b, 0.6f);
        }

        bool countdownActive;

        /// <summary>« PRÊT ? 1 2 3 GO ! » : le décompte suit les 4 temps de la mesure d'intro.</summary>
        void UpdateCountdown()
        {
            var c = Conductor.Instance;
            countdownActive = false;
            if (run == null || c == null || !run.IsRunning || run.IsDemo || run.SectionIndex != 0) return;
            double beat = c.SongBeat;
            if (beat < 0 || beat >= 5.2) return;
            string[] steps = { "PRÊT ?", "1", "2", "3", "GO !" };
            countdownActive = true;
            bannerText.Text = steps[Mathf.Clamp((int)beat, 0, 4)];
            bannerText.Scale = 3;
            bannerTimer = 0.05f;
        }

        /// <summary>Barre de progression du niveau en 8 segments égaux (le segment en cours est doré).</summary>
        void UpdateFrieze()
        {
            if (friezeBack == null || friezeBack.Length == 0) return;
            float progress = run.Progress * friezeBack.Length;
            for (int i = 0; i < friezeBack.Length; i++)
            {
                float f = Mathf.Clamp01(progress - i);
                var fill = (RectTransform)friezeFill[i].transform;
                fill.anchorMax = new Vector2(f, 1f);
                bool current = f > 0f && f < 1f;
                friezeFill[i].color = current ? new Color(1f, 0.82f, 0.23f) : new Color(0.75f, 0.72f, 0.9f);
                friezeBack[i].color = current ? new Color(0.35f, 0.3f, 0.5f) : new Color(0.18f, 0.15f, 0.28f);
            }
        }
    }
}
