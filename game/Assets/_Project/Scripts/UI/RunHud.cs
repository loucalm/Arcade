using Kenta.Core;
using Kenta.Level;
using Kenta.Rhythm;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>
    /// HUD de la partie (docs/da-kenta.md § 7) : tout en haut, rien au milieu.
    /// Gauche : score et multiplicateur. Centre : frise des sections du niveau (barre de progression) et son intitulé, ou l'annonce d'une surprise.
    /// Droite : pièces ramassées et vies (die and retry, ADR-033 : un cœur et le nombre de vies). Pas de décompte au
    /// départ (il tombait sur la première action) ; dans le monde : PARFAIT !
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
        [Tooltip("Segments de la frise (un par section du niveau, 12 au plus) : fond ; le remplissage est un enfant nommé « Fill ».")]
        [SerializeField] Image[] friezeBack;
        [SerializeField] Image[] friezeFill;
        [Tooltip("Cœur affiché à côté du nombre de vies (créés à l'exécution sous le compteur de pièces).")]
        [SerializeField] Sprite heartSprite;
        PixelText livesText;
        Image heart;
        int shownLives = -1;
        float livesPulse;

        RunManager run;
        float shownScore;
        int lastShownScore = -1;
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
            EnsureLivesDisplay();
            run.OnWarning += HandleWarning;
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
            run.OnWarning -= HandleWarning;
        }

        // annonce d'une surprise : à la place de l'intitulé, en or, qui clignote sur les temps pendant une mesure
        string warning = "";
        float warningTimer;
        static readonly Color WarningColor = new(1f, 0.82f, 0.23f);

        void HandleWarning(string text)
        {
            warning = text;
            var c = Conductor.Instance;
            warningTimer = (float)(c != null ? c.SecondsPerBeat * 4.0 : 1.6);
        }

        void HandleRunStarted()
        {
            shownScore = 0;
            sectionLabelTimer = sectionLabelSeconds;
            warningTimer = 0f;
            LayoutFrieze();
        }

        // frise calée sur les sections du niveau : un segment par section, de largeur proportionnelle à sa durée
        const float FriezeLeft = -72f, FriezeWidth = 144f, FriezeGap = 2f;
        readonly float[] segFrom = new float[16], segTo = new float[16];
        int segCount;

        void LayoutFrieze()
        {
            segCount = 0;
            if (friezeBack == null || friezeBack.Length == 0 || run == null || run.Chart == null) return;
            var sections = run.Chart.sections;
            int n = Mathf.Clamp(sections != null ? sections.Length : 0, 1, Mathf.Min(friezeBack.Length, segFrom.Length));
            for (int i = 0; i < n; i++)
            {
                segFrom[i] = sections != null && sections.Length > 0 ? run.ProgressAt(sections[i].startBeat) : 0f;
                segTo[i] = i + 1 < n && sections != null ? run.ProgressAt(sections[i + 1].startBeat) : 1f;
            }
            segCount = n;
            for (int i = 0; i < friezeBack.Length; i++)
            {
                bool on = i < n;
                friezeBack[i].gameObject.SetActive(on);
                if (!on) continue;
                float x0 = Mathf.Round(FriezeLeft + segFrom[i] * FriezeWidth);
                float x1 = Mathf.Round(FriezeLeft + segTo[i] * FriezeWidth) - FriezeGap;
                var rt = (RectTransform)friezeBack[i].transform;
                rt.sizeDelta = new Vector2(Mathf.Max(2f, x1 - x0), rt.sizeDelta.y);
                rt.anchoredPosition = new Vector2(Mathf.Round((x0 + x1) * 0.5f), rt.anchoredPosition.y);
            }
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
            // le décor ne change plus pendant le morceau : on ne le nomme qu'au départ, ensuite la section seule
            if (biome != null) sectionLabel = string.IsNullOrEmpty(section) ? biome.label
                                             : run.SectionIndex <= 0 ? $"{section} · {biome.label}" : section;
        }

        void HandleSection(ChartSection section, bool perfect, int bonus)
        {
            // un niveau = un monde : pas de grand bandeau au milieu de l'action (il masquait les obstacles), l'intitulé du haut suffit
            HandleStats();
            sectionLabelTimer = sectionLabelSeconds;
            // pas de texte au milieu de l'action : le bonus de section se voit au score (docs/level-design-musical.md § 4)
        }

        void HandleDamaged() => ShowFeedback(run != null && run.Lives > 0 ? "ENCORE !" : "SILENCE…", new Color(1f, 0.35f, 0.4f));

        /// <summary>Le cœur et le nombre de vies, sous le compteur de pièces (copie de son texte : même police, même style).</summary>
        void EnsureLivesDisplay()
        {
            if (livesText != null || coinsText == null) return;
            var go = Instantiate(coinsText.gameObject, coinsText.transform.parent);
            go.name = "Lives";
            var rt = (RectTransform)go.transform;
            var crt = (RectTransform)coinsText.transform;
            rt.anchoredPosition = crt.anchoredPosition + new Vector2(0f, -16f);
            livesText = go.GetComponent<PixelText>();
            if (heartSprite != null)
            {
                heart = new GameObject("Heart", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                heart.transform.SetParent(coinsText.transform.parent, false);
                heart.sprite = heartSprite;
                heart.raycastTarget = false;
                var hrt = heart.rectTransform;
                hrt.anchorMin = crt.anchorMin;
                hrt.anchorMax = crt.anchorMax;
                hrt.pivot = new Vector2(0.5f, 0.5f);
                hrt.sizeDelta = heartSprite.rect.size;                       // pixels du sprite (canvas 320×180)
                hrt.anchoredPosition = rt.anchoredPosition + new Vector2(-40f, 0f);
            }
        }

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
            int shown = Mathf.RoundToInt(shownScore);
            if (shown != lastShownScore)                         // une chaîne par changement, pas par image (GC WebGL)
            {
                lastShownScore = shown;
                scoreText.Text = shown.ToString("D6");
            }

            multiplierPulse = Mathf.MoveTowards(multiplierPulse, 0f, dt * 3f);
            multiplierText.Scale = multiplierPulse > 0.3f ? 3 : 2;

            UpdateFrieze();
            UpdateLives(dt);
            // intitulé de section : visible quelques secondes à chaque nouvelle section, puis effacé (il chevauchait les pièces)
            sectionLabelTimer = Mathf.Max(0f, sectionLabelTimer - dt);
            warningTimer = Mathf.Max(0f, warningTimer - dt);
            string label = sectionLabelTimer > 0f ? sectionLabel : "";
            Color labelColor = Color.white;
            if (warningTimer > 0f)
            {
                var cd = Conductor.Instance;
                double b = cd != null ? cd.SongBeat : 0;
                label = b - System.Math.Floor(b) < 0.7 ? warning : "";      // clignote sur les temps
                labelColor = WarningColor;
            }
            if (biomeText != null)
            {
                if (biomeText.Text != label) biomeText.Text = label;
                if (biomeText.Color != labelColor) biomeText.Color = labelColor;
            }

            bannerTimer = Mathf.Max(0f, bannerTimer - dt);
            if (bannerTimer <= 0f) bannerText.Text = "";
            feedbackTimer = Mathf.Max(0f, feedbackTimer - dt);
            if (feedbackTimer <= 0f) feedbackText.Text = "";
            else if (feedbackTimer < 0.3f) feedbackText.Color = new Color(feedbackColor.r, feedbackColor.g, feedbackColor.b, 0.6f);
        }

        /// <summary>Vies restantes : le nombre grossit un instant quand on en perd une. Rien en démo.</summary>
        void UpdateLives(float dt)
        {
            if (livesText == null) return;
            bool show = !run.IsDemo;
            livesText.gameObject.SetActive(show);
            if (heart != null) heart.gameObject.SetActive(show);
            if (!show) return;
            if (run.Lives != shownLives)
            {
                if (shownLives >= 0 && run.Lives < shownLives) livesPulse = 1f;
                shownLives = run.Lives;
                livesText.Text = $"×{run.Lives}";
            }
            livesPulse = Mathf.MoveTowards(livesPulse, 0f, dt * 2f);
            livesText.Color = livesPulse > 0.01f ? new Color(1f, 0.35f, 0.4f) : Color.white;
            livesText.Scale = livesPulse > 0.5f ? 2 : 1;
        }

        /// <summary>Barre de progression du niveau en 8 segments égaux (le segment en cours est doré).</summary>
        void UpdateFrieze()
        {
            if (friezeBack == null || friezeBack.Length == 0) return;
            if (segCount == 0) LayoutFrieze();
            float progress = run.Progress;
            for (int i = 0; i < segCount; i++)
            {
                float f = Mathf.Clamp01((progress - segFrom[i]) / Mathf.Max(0.0001f, segTo[i] - segFrom[i]));
                var fill = (RectTransform)friezeFill[i].transform;
                fill.anchorMax = new Vector2(f, 1f);
                bool current = f > 0f && f < 1f;
                friezeFill[i].color = current ? new Color(1f, 0.82f, 0.23f) : new Color(0.75f, 0.72f, 0.9f);
                friezeBack[i].color = current ? new Color(0.35f, 0.3f, 0.5f) : new Color(0.18f, 0.15f, 0.28f);
            }
        }
    }
}
