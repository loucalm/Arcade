using Kenta.Core;
using Kenta.FX;
using Kenta.Level;
using Kenta.Rhythm;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>
    /// HUD de la partie (docs/da-kenta.md § 7) : trois plaques en haut, collées à 4 px d'écart, rien au milieu de l'action.
    /// Gauche : score (chiffres en colonnes, qui défilent) et multiplicateur (or, petit rebond quand il monte) ; en reprise
    /// au début, dès le 2ᵉ essai, une couronne et le meilleur score des essais, en discret.
    /// Milieu : frise des sections du niveau (une par section, largeur selon sa durée, section en cours en or) ; dessous,
    /// un intitulé en fondu (section, annonce d'une surprise, meilleur score au début d'un essai, mot de la mort).
    /// Droite : vies selon la difficulté du héros (ADR-036, lu sur RunManager) — cœur et nombre (reprise au point),
    /// « ESSAI 2/3 » (reprise au début), cœur seul (une seule vie) — puis les pièces.
    /// Grille commune (UI/HudLayout) : marge 4 px, plaque de 14 px (cadre 1 + marge 2 + texte 8), 4 px entre deux
    /// éléments, 2 px entre une icône et son nombre. Une plaque qui couvre le héros, une pièce ou un ennemi s'efface.
    /// Aucune allocation par image ; positions en pixels entiers ; pas d'animation calée sur le temps (sauf le pouls
    /// léger d'une annonce).
    /// </summary>
    public class RunHud : MonoBehaviour
    {
        [Header("Plaques")]
        [SerializeField] HudPlate scorePlate;
        [SerializeField] HudPlate friezePlate;
        [SerializeField] HudPlate statsPlate;
        [SerializeField] HudPlate labelPlate;

        [Header("Score")]
        [SerializeField] HudNumber scoreNumber;
        [Tooltip("« × » et son chiffre : rebondit quand le multiplicateur monte.")]
        [SerializeField] RectTransform multiplierGroup;
        [SerializeField] PixelText multiplierSign;
        [SerializeField] HudNumber multiplierNumber;
        [Tooltip("Couronne + meilleur score des essais (reprise au début, à partir du 2ᵉ essai).")]
        [SerializeField] Image bestIcon;
        [SerializeField] HudNumber bestNumber;

        [Header("Frise")]
        [Tooltip("Segments (un par section du niveau, au plus autant que d'images) : fond ; le remplissage est leur enfant « Fill ».")]
        [SerializeField] Image[] friezeBack;
        [SerializeField] Image[] friezeFill;

        [Header("Vies et pièces")]
        [SerializeField] Image heart;
        [SerializeField] Sprite heartFull;
        [SerializeField] Sprite heartEmpty;
        [SerializeField] HudNumber livesNumber;
        [Tooltip("Reprise au début : « ESSAI » (atténué) et « 2/3 ».")]
        [SerializeField] PixelText triesWord;
        [SerializeField] PixelText triesText;
        [SerializeField] Image coinIcon;
        [SerializeField] HudNumber coinsNumber;
        [SerializeField] PixelText coinsSlash;
        [SerializeField] HudNumber coinsTotal;

        [Header("Intitulé (sous la frise)")]
        [SerializeField] PixelText labelText;
        [Tooltip("Durée (s) de l'intitulé d'une nouvelle section, fondus compris.")]
        [SerializeField] float sectionLabelSeconds = 3f;
        [Tooltip("Opacité d'une plaque qui couvre le héros, une pièce ou un ennemi.")]
        [SerializeField, Range(0f, 1f)] float coveredAlpha = 0.3f;

        // couleurs : celles des menus (Screen_LevelSelect, panel.png)
        static readonly Color Gold = new(1f, 0.82f, 0.23f);
        static readonly Color Dim = new(0.6f, 0.58f, 0.75f);
        static readonly Color FrameColor = new(90 / 255f, 80 / 255f, 136 / 255f);
        static readonly Color SegTodo = new(90 / 255f, 80 / 255f, 136 / 255f);
        static readonly Color SegDone = new(154 / 255f, 144 / 255f, 200 / 255f);
        static readonly Color Hurt = new(1f, 0.35f, 0.4f);
        static readonly Color BestDim = new(0.6f, 0.58f, 0.75f, 0.85f);
        static readonly Color CrownDim = new(1f, 1f, 1f, 0.45f);        // couronne or, estompée tant qu'on ne bat pas

        const float FadeIn = 0.15f, FadeOut = 0.3f, CoverFadeSpeed = 5f, HudFadeIn = 0.35f;
        const float BounceTime = 0.25f, CoinBounceTime = 0.15f, FlashTime = 0.5f, DeathMessageTime = 1.4f;
        const int TextY = -(HudLayout.Inset + HudLayout.ContentHeight / 2);              // centre du texte dans une plaque
        const int IconBottom = TextY - HudLayout.ContentHeight / 2;                      // bas d'une icône 9×9 (contour)
        const int LabelTop = -(HudLayout.Margin + HudLayout.PlateHeight + HudLayout.Gap);

        RunManager run;
        LevelBuilder level;
        CanvasGroup rootGroup;
        Transform heroTransform;
        SpriteRenderer heroSprite;

        float hudAlpha;
        float shownScore;
        int lastMultiplier = 1, lastLums, maxLives = 1, shownLives = -1, coinDigits = 3;
        RetryMode mode;
        bool showBest;
        float multT = 99f, multFlashT = 99f, coinT = 99f, livesT = 99f, livesFlashT = 99f;
        Color multFlashColor = Color.white;
        int multX, coinIconX, triesWidth;

        // frise
        readonly float[] segFrom = new float[16], segTo = new float[16], starts = new float[16];
        readonly int[] segX = new int[16], segW = new int[16];
        int segCount, friezeLaidWidth = -1;

        // plaques : la largeur des vies et pièces suit le nombre de vies (10 → 9) en glissant
        int scoreWidth;
        float statsWidth;
        int statsTarget;

        // intitulé
        string sectionLabel = "", warning = "", deathMessage = "", attemptLabel = "";
        int labelSection = -1;
        float sectionLabelTimer, warningTimer, deathTimer, attemptTimer, warningDuration = 1f;
        string labelShown;

        void Awake() => rootGroup = GetComponent<CanvasGroup>();

        void OnEnable()
        {
            run = RunManager.Instance;
            if (run == null) return;
            if (level == null) level = FindFirstObjectByType<LevelBuilder>(FindObjectsInactive.Include);
            run.OnRunStarted += HandleRunStarted;
            run.OnStatsChanged += HandleStats;
            run.OnSectionChanged += HandleSection;
            run.OnDamaged += HandleDamaged;
            run.OnWarning += HandleWarning;
            run.OnLumPicked += HandleLum;
            HandleRunStarted();
        }

        void OnDisable()
        {
            if (run == null) return;
            run.OnRunStarted -= HandleRunStarted;
            run.OnStatsChanged -= HandleStats;
            run.OnSectionChanged -= HandleSection;
            run.OnDamaged -= HandleDamaged;
            run.OnWarning -= HandleWarning;
            run.OnLumPicked -= HandleLum;
        }

        /// <summary>
        /// Départ d'une partie, ou d'un nouvel essai en reprise au début (relancé par RunManager) : tout repart de zéro
        /// (score, frise, intitulé) ; les objets du HUD sont réutilisés, jamais recréés.
        /// </summary>
        void HandleRunStarted()
        {
            if (run == null) return;
            if (run.Attempt <= 1) maxLives = Mathf.Max(1, run.Lives);     // vies (ou essais) au départ de la partie
            mode = run.Difficulty;
            shownScore = run.Score;
            lastMultiplier = run.Multiplier;
            lastLums = run.Lums;
            shownLives = -1;
            multT = multFlashT = coinT = livesT = livesFlashT = 99f;
            warningTimer = deathTimer = 0f;
            labelSection = -1;
            sectionLabelTimer = sectionLabelSeconds;
            // nouvel essai : le meilleur score à battre, à la place du nom de la section
            showBest = mode == RetryMode.Restart && run.Attempt > 1 && !run.IsDemo;
            attemptTimer = 0f;
            if (showBest)
            {
                attemptLabel = $"MEILLEUR SCORE {run.BestScore:D6}";
                attemptTimer = sectionLabelSeconds;
                sectionLabelTimer = 0f;
            }
            hudAlpha = 0f;                                      // le HUD apparaît en fondu
            if (rootGroup != null) rootGroup.alpha = 0f;
            var target = CameraRig.Instance != null ? CameraRig.Instance.Target : null;
            heroTransform = target;
            heroSprite = target != null ? target.GetComponentInChildren<SpriteRenderer>(true) : null;
            Layout(mode, maxLives, run.Attempt, run.TotalLums, showBest);
            HandleStats();
            friezeLaidWidth = -1;
            scorePlate.SnapAlpha(1f);
            friezePlate.SnapAlpha(1f);
            statsPlate.SnapAlpha(1f);
            labelPlate.SnapAlpha(1f);
            labelPlate.Visibility = 0f;
        }

        /// <summary>
        /// Met en place les plaques pour une partie : difficulté (vies), essai en cours, nombre de pièces du niveau
        /// (colonnes du compteur), meilleur score affiché ou non. Appelé au départ de chaque partie ou essai (et par le
        /// builder pour l'aperçu du prefab).
        /// </summary>
        public void Layout(RetryMode retryMode, int startLives, int attempt, int totalCoins, bool best)
        {
            mode = retryMode;
            maxLives = Mathf.Max(1, startLives);
            showBest = best;
            coinDigits = Mathf.Clamp(HudLayout.DigitCount(totalCoins), 2, coinsNumber.Capacity);

            // gauche : score, multiplicateur, puis (2ᵉ essai et suivants) couronne et meilleur score
            scoreNumber.Digits = scoreNumber.Capacity;
            multiplierNumber.Digits = 1;
            bestNumber.Digits = scoreNumber.Digits;
            scoreWidth = HudLayout.PlateWidth(HudLayout.ScoreContentWidth(scoreNumber.Digits, multiplierNumber.Digits, best));
            scorePlate.Width = scoreWidth;
            scorePlate.Left = HudLayout.Margin;
            Place(scoreNumber.transform, HudLayout.Inset, TextY);
            multX = HudLayout.Inset + scoreNumber.Width + HudLayout.Gap;
            Place(multiplierGroup, multX, TextY);
            Place(multiplierSign.transform, 0, 0);
            Place(multiplierNumber.transform, HudLayout.DigitAdvance, 0);
            int bestX = multX + HudLayout.DigitAdvance + multiplierNumber.Width + HudLayout.Gap;
            Place(bestIcon.transform, bestX - 1, IconBottom);
            Place(bestNumber.transform, bestX + HudLayout.IconSize + HudLayout.IconGap, TextY);
            SetActive(bestIcon.gameObject, best);
            SetActive(bestNumber.gameObject, best);
            scoreNumber.Arrange();
            multiplierNumber.Arrange();
            bestNumber.Arrange();

            // droite : vies depuis le bord gauche de la plaque, pièces depuis son bord droit
            bool restart = mode == RetryMode.Restart;
            SetActive(heart.gameObject, !restart);
            SetActive(livesNumber.gameObject, mode == RetryMode.Checkpoint);
            SetActive(triesWord.gameObject, restart);
            SetActive(triesText.gameObject, restart);
            heart.sprite = heartFull;
            Place(heart.transform, HudLayout.Inset - 1, IconBottom);
            livesNumber.Digits = HudLayout.DigitCount(maxLives);
            Place(livesNumber.transform, HudLayout.Inset + HudLayout.IconSize + HudLayout.IconGap, TextY);
            triesWidth = 0;
            if (restart)
            {
                triesWord.Text = "ESSAI";
                triesText.Text = $"{Mathf.Clamp(attempt, 1, maxLives)}/{maxLives}";    // une chaîne par essai
                int wordW = PixelText.MeasureWidth(triesWord.Font, "ESSAI ", 1) + 1;  // espace de la police
                Place(triesWord.transform, HudLayout.Inset, TextY);
                Place(triesText.transform, HudLayout.Inset + wordW, TextY);
                triesWidth = wordW + PixelText.MeasureWidth(triesText.Font, triesText.Text, 1);
            }

            coinsNumber.Digits = coinsTotal.Digits = coinDigits;
            int totalX = -HudLayout.Inset - coinsTotal.Width;
            int slashX = totalX - HudLayout.DigitAdvance;
            int countX = slashX - coinDigits * HudLayout.DigitAdvance;
            PlaceRight(coinsTotal.transform, totalX, TextY);
            PlaceRight(coinsSlash.transform, slashX, TextY);
            PlaceRight(coinsNumber.transform, countX, TextY);
            coinIconX = countX - HudLayout.IconGap - HudLayout.IconSize - 1;
            PlaceRight(coinIcon.transform, coinIconX, IconBottom);
            coinsTotal.SetValue(totalCoins);

            statsTarget = HudLayout.PlateWidth(HudLayout.StatsContentWidth(mode, maxLives, triesWidth, coinDigits));
            statsWidth = statsTarget;
            ApplyPlateWidths();

            // intitulé : sous la frise
            Place(labelText.transform, HudLayout.Inset, TextY);
            labelPlate.Rect.anchoredPosition = new Vector2(labelPlate.Left, LabelTop);
            labelShown = null;
        }

        static void SetActive(GameObject go, bool on)
        {
            if (go.activeSelf != on) go.SetActive(on);
        }

        static void Place(Transform t, int x, int y)
        {
            var rt = (RectTransform)t;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
        }

        static void PlaceRight(Transform t, int x, int y)
        {
            var rt = (RectTransform)t;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>Déplace au pixel près, seulement si la position change (pas de reconstruction du canvas pour rien).</summary>
        static void Move(RectTransform rt, int x, int y)
        {
            var p = rt.anchoredPosition;
            if (Mathf.RoundToInt(p.x) != x || Mathf.RoundToInt(p.y) != y) rt.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>Plaques collées à 4 px d'écart : score | frise (la place restante) | vies et pièces.</summary>
        void ApplyPlateWidths()
        {
            int stats = Mathf.RoundToInt(statsWidth);
            statsPlate.Width = stats;
            statsPlate.Left = HudLayout.ScreenWidth - HudLayout.Margin - stats;
            friezePlate.Left = HudLayout.Margin + scoreWidth + HudLayout.Gap;
            friezePlate.Width = HudLayout.CenterPlateWidth(scoreWidth, stats);
        }

        void HandleStats()
        {
            if (run == null) return;
            int m = run.Multiplier;
            if (m > lastMultiplier) { multT = 0f; multFlashT = 0f; multFlashColor = Color.white; }
            else if (m < lastMultiplier) { multFlashT = 0f; multFlashColor = Dim; }
            lastMultiplier = m;
            multiplierNumber.SetValue(m);
            coinsNumber.SetValue(run.Lums);
            if (run.Lums > lastLums) coinT = 0f;
            lastLums = run.Lums;
            if (showBest) bestNumber.SetValue(run.BestScore);

            if (run.SectionIndex != labelSection)               // une chaîne par section, pas par événement
            {
                labelSection = run.SectionIndex;
                var biome = run.CurrentBiome;
                string section = run.SectionName.ToUpperInvariant();
                // le décor ne change pas pendant le morceau : on ne le nomme qu'au départ, ensuite la section seule
                sectionLabel = biome == null ? section
                             : string.IsNullOrEmpty(section) ? biome.label
                             : run.SectionIndex <= 0 ? $"{section} · {biome.label}" : section;
            }
            UpdateLives();
        }

        void HandleSection(ChartSection section, bool perfect, int bonus)
        {
            // pas de grand bandeau au milieu (il masquait les obstacles) : l'intitulé sous la frise suffit ; le bonus de
            // section se voit au score (docs/level-design-musical.md § 4)
            HandleStats();
            sectionLabelTimer = sectionLabelSeconds;
        }

        // la mort se lit sur les vies (cœur vide, nombre et cadre en rouge) ; le mot va sous la frise, jamais au milieu
        void HandleDamaged()
        {
            if (run == null || run.IsDemo) return;
            deathMessage = HudLayout.DeathMessage(run.Difficulty, run.Lives);
            deathTimer = DeathMessageTime;
        }

        // annonce d'une surprise (au moins 1,5 s à l'écran même à très haut BPM) : à la place de l'intitulé, en or, avec un pouls léger
        void HandleWarning(string text)
        {
            warning = text;
            var c = Conductor.Instance;
            warningDuration = warningTimer = Mathf.Max(1.5f, (float)(c != null ? c.SecondsPerBeat * 4.0 : 1.6));
        }

        void HandleLum(LevelObject lum) => coinT = 0f;

        /// <summary>Vies : le cœur se vide et tombe d'un cran, le nombre et le cadre passent au rouge puis reviennent.</summary>
        void UpdateLives()
        {
            int lives = run.Lives;
            if (lives == shownLives) return;
            if (shownLives >= 0 && lives < shownLives) livesT = livesFlashT = 0f;
            shownLives = lives;
            heart.sprite = lives > 0 ? heartFull : heartEmpty;
            if (mode == RetryMode.Checkpoint)
            {
                livesNumber.Digits = HudLayout.DigitCount(lives);
                livesNumber.SetValue(lives);
                statsTarget = HudLayout.PlateWidth(HudLayout.StatsContentWidth(mode, lives, triesWidth, coinDigits));
            }
        }

        void Update()
        {
            if (run == null) return;
            float dt = Time.unscaledDeltaTime;

            // apparition du HUD en fondu au départ (et à chaque nouvel essai)
            if (hudAlpha < 1f)
            {
                hudAlpha = Mathf.Min(1f, hudAlpha + dt / HudFadeIn);
                if (rootGroup != null) rootGroup.alpha = hudAlpha;
            }

            // score : défile vers sa valeur
            shownScore = HudLayout.RollScore(shownScore, run.Score, dt);
            scoreNumber.SetValue(Mathf.RoundToInt(shownScore));

            // multiplicateur : rebond de 2 px quand il monte, éclair blanc (ou terne s'il retombe) qui revient à l'or ;
            // le cadre de la plaque s'allume en or quand il monte
            multT += dt;
            multFlashT += dt;
            Move(multiplierGroup, multX, TextY + HudLayout.Bounce(multT, BounceTime, 2));
            float mk = multFlashT / FlashTime;
            var mc = mk >= 1f ? Gold : Color.Lerp(multFlashColor, Gold, mk);
            multiplierNumber.Color = mc;
            multiplierSign.Color = mc;
            bool rising = multFlashColor == Color.white && mk < 1f;
            scorePlate.SetFrameColor(rising ? Color.Lerp(Gold, FrameColor, mk) : FrameColor);
            if (showBest)
            {
                // couronne : or quand l'essai en cours égale ou bat le meilleur, atténuée sinon
                var bc = run.Score >= run.BestScore && run.Score > 0 ? Color.white : CrownDim;
                if (bestIcon.color != bc) bestIcon.color = bc;
                bestNumber.Color = BestDim;
            }

            // pièces : la pièce saute d'1 px à chaque prise
            coinT += dt;
            Move((RectTransform)coinIcon.transform, coinIconX, IconBottom + HudLayout.Bounce(coinT, CoinBounceTime, 1));

            UpdateLivesAnimation(dt);
            UpdateFrieze();
            UpdateLabel(dt);
            UpdateCover(dt);
        }

        void UpdateLivesAnimation(float dt)
        {
            SetActive(statsPlate.gameObject, !run.IsDemo);
            livesT += dt;
            livesFlashT += dt;
            // vie perdue : le cœur tombe de 2 px et remonte ; nombre et cadre en rouge, qui reviennent en fondu
            Move(heart.rectTransform, HudLayout.Inset - 1, IconBottom - HudLayout.Bounce(livesT, BounceTime * 1.4f, 2));
            float k = livesFlashT / (FlashTime * 1.2f);
            var text = k >= 1f ? Color.white : Color.Lerp(Hurt, Color.white, k);
            if (mode == RetryMode.Checkpoint) livesNumber.Color = text;
            else if (mode == RetryMode.Restart) triesText.Color = text;
            statsPlate.SetFrameColor(k >= 1f ? FrameColor : Color.Lerp(Hurt, FrameColor, k));

            // la plaque suit le nombre de vies (10 → 9) en glissant, la frise prend la place
            if (statsWidth != statsTarget)
            {
                statsWidth = Mathf.MoveTowards(statsWidth, statsTarget, 40f * dt);
                ApplyPlateWidths();
            }
        }

        /// <summary>Frise : un segment par section (largeur selon sa durée), section en cours en or, passées en clair.</summary>
        void UpdateFrieze()
        {
            int inner = friezePlate.Width - 2 * HudLayout.Inset;
            if (inner != friezeLaidWidth) LayoutFrieze(inner);
            float progress = run.Progress;
            for (int i = 0; i < segCount; i++)
            {
                int px = HudLayout.FillPixels(progress, segFrom[i], segTo[i], segW[i]);
                var fill = friezeFill[i].rectTransform;
                if (Mathf.RoundToInt(fill.sizeDelta.x) != px) fill.sizeDelta = new Vector2(px, 0f);
                bool current = progress >= segFrom[i] && progress < segTo[i];
                var c = current ? Gold : SegDone;
                if (friezeFill[i].color != c) friezeFill[i].color = c;
            }
        }

        void LayoutFrieze(int inner)
        {
            friezeLaidWidth = inner;
            segCount = 0;
            if (friezeBack == null || friezeBack.Length == 0 || run == null || run.Chart == null) return;
            var sections = run.Chart.sections;
            int n = Mathf.Clamp(sections != null ? sections.Length : 0, 1, Mathf.Min(friezeBack.Length, segFrom.Length));
            for (int i = 0; i < n; i++)
            {
                starts[i] = sections != null && sections.Length > 0 ? run.ProgressAt(sections[i].startBeat) : 0f;
                segFrom[i] = starts[i];
            }
            for (int i = 0; i < n; i++) segTo[i] = i + 1 < n ? segFrom[i + 1] : 1f;
            HudLayout.FriezeSegments(starts, n, inner, 1, segX, segW);
            segCount = n;
            const int barY = TextY - HudLayout.FriezeHeight / 2;
            for (int i = 0; i < friezeBack.Length; i++)
            {
                bool on = i < n;
                SetActive(friezeBack[i].gameObject, on);
                if (!on) continue;
                var rt = friezeBack[i].rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = Vector2.zero;
                rt.sizeDelta = new Vector2(segW[i], HudLayout.FriezeHeight);
                rt.anchoredPosition = new Vector2(HudLayout.Inset + segX[i], barY);
                friezeBack[i].color = SegTodo;
            }
        }

        /// <summary>
        /// Intitulé sous la frise, en fondu : mot de la mort &gt; annonce d'une surprise &gt; meilleur score d'un nouvel
        /// essai &gt; nom de la section.
        /// </summary>
        void UpdateLabel(float dt)
        {
            sectionLabelTimer = Mathf.Max(0f, sectionLabelTimer - dt);
            warningTimer = Mathf.Max(0f, warningTimer - dt);
            deathTimer = Mathf.Max(0f, deathTimer - dt);
            attemptTimer = Mathf.Max(0f, attemptTimer - dt);

            string text;
            Color color;
            float visibility;
            if (deathTimer > 0f)
            {
                text = deathMessage; color = Hurt;
                visibility = Envelope(DeathMessageTime - deathTimer, deathTimer);
            }
            else if (warningTimer > 0f)
            {
                text = warning; color = Gold;
                var cd = Conductor.Instance;
                double b = cd != null ? cd.SongBeat : 0;
                float pulse = 0.85f + 0.15f * Mathf.Cos((float)(b - System.Math.Floor(b)) * 2f * Mathf.PI);   // pouls léger
                visibility = Envelope(warningDuration - warningTimer, warningTimer) * pulse;
            }
            else if (attemptTimer > 0f)
            {
                text = attemptLabel; color = Gold;
                visibility = Envelope(sectionLabelSeconds - attemptTimer, attemptTimer);
            }
            else
            {
                text = sectionLabel; color = Color.white;
                visibility = Envelope(sectionLabelSeconds - sectionLabelTimer, sectionLabelTimer);
            }

            if (!ReferenceEquals(text, labelShown) && visibility > 0f)
            {
                labelShown = text;
                labelText.Text = text;
                labelPlate.Width = HudLayout.PlateWidth(PixelText.MeasureWidth(labelText.Font, text, 1));
            }
            labelText.Color = color;
            // centré sous la frise, au pixel près
            int left = friezePlate.Left + (friezePlate.Width - labelPlate.Width) / 2;
            Move(labelPlate.Rect, left, LabelTop);
            labelPlate.Visibility = visibility;
        }

        /// <summary>Fondu d'entrée puis de sortie (age : temps écoulé, left : temps restant).</summary>
        static float Envelope(float age, float left) => Mathf.Clamp01(Mathf.Min(age / FadeIn, left / FadeOut));

        /// <summary>
        /// Une plaque qui couvre le héros, une pièce ou un ennemi s'efface (le HUD ne cache jamais l'action) : test des
        /// rectangles en pixels d'interface, sans allocation.
        /// </summary>
        void UpdateCover(float dt)
        {
            var rig = CameraRig.Instance;
            var screen = PixelScreen.Instance;
            var glitch = ScreenGlitch.Instance;
            bool sCover = false, fCover = false, tCover = false, lCover = false;
            if (rig != null && screen != null)
            {
                Vector2 cam = rig.ExactPosition;
                int viewW = Mathf.Max(1, screen.ViewWidth);
                bool flipX = glitch != null && glitch.FlipX;
                bool flipY = glitch != null && glitch.FlipY;
                int copies = glitch != null ? glitch.Copies : 1;
                float unitsToUi = PixelScreen.Ppu * (HudLayout.ScreenWidth / (float)viewW);
                // Hors retournement ni division d'écran, élagage rapide des objets bas ; sinon le bas du monde peut être en haut !
                float lowest = (!flipY && copies <= 1)
                    ? cam.y + (HudLayout.Top + LabelTop - HudLayout.PlateHeight) / unitsToUi
                    : float.NegativeInfinity;
                float halfW = HudLayout.ScreenWidth * 0.5f / unitsToUi;
                bool label = labelPlate.Visibility > 0.01f;

                if (heroSprite != null && heroTransform != null && heroTransform.gameObject.activeInHierarchy)
                {
                    var b = heroSprite.bounds;
                    Cover(b.min, b.max, cam, viewW, flipX, flipY, copies, label, ref sCover, ref fCover, ref tCover, ref lCover);
                }
                if (level != null)
                {
                    CoverList(level.Lums, false, cam, viewW, flipX, flipY, copies, lowest, halfW, label, ref sCover, ref fCover, ref tCover, ref lCover);
                    CoverList(level.Obstacles, true, cam, viewW, flipX, flipY, copies, lowest, halfW, label, ref sCover, ref fCover, ref tCover, ref lCover);
                }
            }
            scorePlate.CoverTarget = sCover ? coveredAlpha : 1f;
            friezePlate.CoverTarget = fCover ? coveredAlpha : 1f;
            statsPlate.CoverTarget = tCover ? coveredAlpha : 1f;
            labelPlate.CoverTarget = lCover ? coveredAlpha : 1f;
            scorePlate.Tick(dt, CoverFadeSpeed);
            friezePlate.Tick(dt, CoverFadeSpeed);
            statsPlate.Tick(dt, CoverFadeSpeed);
            labelPlate.Tick(dt, CoverFadeSpeed);
        }

        void CoverList(System.Collections.Generic.List<LevelObject> list, bool enemiesOnly, Vector2 cam, int viewW,
                       bool flipX, bool flipY, int copies, float lowest, float halfW, bool label,
                       ref bool s, ref bool f, ref bool t, ref bool l)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var o = list[i];
                if (enemiesOnly && o.kind != LevelObjectKind.Enemy) continue;
                var b = o.bounds;
                if (b.yMax < lowest || b.xMax < cam.x - halfW || b.xMin > cam.x + halfW) continue;
                if (o.view == null || !o.view.activeInHierarchy) continue;
                Cover(b.min, b.max, cam, viewW, flipX, flipY, copies, label, ref s, ref f, ref t, ref l);
            }
        }

        void Cover(Vector2 min, Vector2 max, Vector2 cam, int viewW, bool flipX, bool flipY, int copies, bool label,
                   ref bool s, ref bool f, ref bool t, ref bool l)
        {
            // Seuls les cadrans du haut (0 en normal, 0 et 1 en grille 2×2) peuvent toucher les plaques du HUD
            int checkQuads = copies >= 2 ? 2 : 1;
            for (int q = 0; q < checkQuads; q++)
            {
                Vector2 p0 = HudLayout.WorldToUiQuad(min, cam, viewW, PixelScreen.Ppu, flipX, flipY, copies, q, out _);
                Vector2 p1 = HudLayout.WorldToUiQuad(max, cam, viewW, PixelScreen.Ppu, flipX, flipY, copies, q, out _);
                // interface : plaques ancrées en haut à gauche de l'écran 320×180 (x depuis la gauche, y négatif depuis le haut)
                float x0 = Mathf.Min(p0.x, p1.x) + HudLayout.ScreenWidth * 0.5f;
                float x1 = Mathf.Max(p0.x, p1.x) + HudLayout.ScreenWidth * 0.5f;
                float y0 = Mathf.Min(p0.y, p1.y) - HudLayout.Top;
                float y1 = Mathf.Max(p0.y, p1.y) - HudLayout.Top;
                s |= Hit(scorePlate, x0, x1, y0, y1);
                f |= Hit(friezePlate, x0, x1, y0, y1);
                t |= statsPlate.gameObject.activeSelf && Hit(statsPlate, x0, x1, y0, y1);
                l |= label && Hit(labelPlate, x0, x1, y0, y1);
            }
        }

        static bool Hit(HudPlate p, float x0, float x1, float y0, float y1)
        {
            var rt = p.Rect;
            float left = rt.anchoredPosition.x, top = rt.anchoredPosition.y;
            float right = left + rt.sizeDelta.x, bottom = top - rt.sizeDelta.y;
            return x1 > left && x0 < right && y1 > bottom && y0 < top;
        }
    }
}
