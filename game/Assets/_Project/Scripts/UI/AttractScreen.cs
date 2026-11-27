using Anatidae;
using Kenta.FX;
using Kenta.Rhythm;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>
    /// Accueil (docs/da-kenta.md § 8.1). Deux visages, pilotés par GameFlow : le logo KENTA sur la Sylve, ou la démo
    /// (une vraie partie en autoplay) avec le logo en haut de l'écran et « APPUIE SUR START » en bas.
    /// Entrée façon borne d'arcade à chaque retour du logo (PlayIntro, environ 2 s) :
    /// écran noir ; les lettres K-E-N-T-A tombent une à une et frappent (éclair blanc sur la lettre, petite secousse,
    /// son) ; toutes posées, GROS impact : flash blanc, le décor apparaît, le logo s'illumine et tremble ; le
    /// sous-titre s'écrit lettre par lettre, le meilleur score glisse de la droite, puis « APPUIE SUR START » clignote
    /// comme sur une borne. Ensuite, un reflet balaie le logo toutes les 3 s. Positions toujours en pixels entiers ;
    /// rien n'est alloué image par image (textes du sous-titre préparés à l'avance).
    /// </summary>
    public class AttractScreen : MonoBehaviour
    {
        [SerializeField] PixelText pressStart;
        [SerializeField] PixelText hiScore;
        [SerializeField] RectTransform logo;
        [SerializeField] float logoY = 16f;
        [Tooltip("Hauteur du logo pendant la démo (en haut de l'écran, au-dessus du héros).")]
        [SerializeField] float demoLogoY = 64f;
        [Tooltip("Éléments masqués pendant la démo (logo, sous-titre, meilleur score).")]
        [SerializeField] GameObject[] logoOnly;
        [Tooltip("« DÉMO », visible seulement pendant la démo.")]
        [SerializeField] GameObject demoLabel;
        [SerializeField] PixelText subtitle;

        [Header("Entrée du titre (Gen/ui/logo_*)")]
        [SerializeField] Sprite[] letters;           // K E N T A
        [SerializeField] Sprite[] letterWhites;      // silhouettes blanches (éclair à l'impact)
        [SerializeField] Sprite logoWhite;
        [SerializeField] Sprite[] shine;             // reflet qui balaie le logo
        [SerializeField] AudioClip letterSfx;
        [SerializeField] AudioClip impactSfx;

        static readonly Color Gold = new(1f, 0.82f, 0.23f);
        const int LetterStep = 23;                   // kenta_ui.LOGO_LETTER_STEP

        // chronologie (secondes depuis PlayIntro)
        const float LetterStart = 0.03f, LetterGap = 0.1f, LetterFall = 0.15f, WhiteFade = 0.18f;
        const float BigImpact = LetterStart + 4 * LetterGap + LetterFall + 0.05f;      // ≈ 0,6 s d'écran noir
        const float LogoGlow = 0.5f, JitterEnd = BigImpact + 0.35f;
        const float TypeStart = BigImpact + 0.2f, TypeStep = 0.035f;
        const float HiStart = BigImpact + 0.35f, HiEnd = BigImpact + 0.75f;
        const float PressAt = BigImpact + 0.9f;
        const float ShineEvery = 3f, ShineFrame = 0.035f;

        CanvasGroup pressGroup;
        Image cover, logoImage, logoGlow, shineImage;
        Image[] letterImages, letterGlows;
        string[] typed;
        string subtitleText;
        Vector2 hiBase;
        float intro = -1f, shineClock, baseY;
        int lettersLanded, steadyFrames;
        bool impactDone, pressDone, built, demoMode;

        void Awake()
        {
            pressGroup = pressStart.GetComponent<CanvasGroup>();
            if (pressGroup == null) pressGroup = pressStart.gameObject.AddComponent<CanvasGroup>();
            baseY = logoY;
        }

        void OnEnable() => RefreshHiScore();

        /// <summary>Vrai : la démo tourne derrière (logo masqué). Faux : le logo.</summary>
        public void SetDemo(bool demo)
        {
            // démo : le logo reste (en haut), le sous-titre et le meilleur score disparaissent, plus d'étiquette « DÉMO »
            if (logoOnly != null)
                foreach (var go in logoOnly) if (go != null) go.SetActive(!demo || (logo != null && go == logo.gameObject));
            if (demoLabel != null) demoLabel.SetActive(false);
            baseY = demo ? demoLogoY : logoY;
            demoMode = demo;
            if (logo != null) logo.anchoredPosition = new Vector2(0f, baseY);
            // pendant la démo, l'appel descend en bas de l'écran : il ne cache plus le héros
            var rt = (RectTransform)pressStart.transform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, demo ? -76f : -46f);
            RefreshHiScore();
            if (demo) StopIntro();
            else PlayIntro();
        }

        /// <summary>Crée une fois les images de l'entrée (lettres, éclairs, voile noir, reflet).</summary>
        void Build()
        {
            if (built) return;
            built = true;
            logoImage = logo != null ? logo.GetComponent<Image>() : null;

            cover = NewImage("IntroCover", transform, null, Color.black);
            var cr = cover.rectTransform;
            cr.anchorMin = Vector2.zero;
            cr.anchorMax = Vector2.one;
            cr.sizeDelta = Vector2.zero;
            cover.transform.SetAsFirstSibling();             // sous les textes, par-dessus le décor

            if (logo != null)
            {
                logoGlow = NewImage("Glow", logo, logoWhite, new Color(1f, 1f, 1f, 0f));
                shineImage = NewImage("Shine", logo, null, Color.white);
                shineImage.enabled = false;
            }
            int n = letters != null ? letters.Length : 0;
            letterImages = new Image[n];
            letterGlows = new Image[n];
            float logoW = logoImage != null && logoImage.sprite != null ? logoImage.sprite.rect.width : 120f;
            for (int i = 0; i < n; i++)
            {
                letterImages[i] = NewImage("Letter" + i, logo != null ? logo.parent : transform, letters[i], Color.white);
                var w = letters[i].rect.width;
                letterImages[i].rectTransform.anchoredPosition = new Vector2(Mathf.Round(-logoW / 2f + i * LetterStep + w / 2f), logoY);
                letterGlows[i] = NewImage("Glow", letterImages[i].transform,
                    letterWhites != null && i < letterWhites.Length ? letterWhites[i] : null, new Color(1f, 1f, 1f, 0f));
                letterImages[i].gameObject.SetActive(false);
            }
            if (subtitle != null)
            {
                subtitleText = subtitle.Text;
                typed = new string[subtitleText.Length + 1];
                for (int i = 0; i <= subtitleText.Length; i++) typed[i] = subtitleText.Substring(0, i);
            }
            if (hiScore != null) hiBase = ((RectTransform)hiScore.transform).anchoredPosition;
        }

        static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            if (sprite != null) rt.sizeDelta = sprite.rect.size;   // pas de SetNativeSize (PPU du monde)
            else if (parent is RectTransform prt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero; }
            return img;
        }

        /// <summary>Rejoue l'entrée du titre.</summary>
        public void PlayIntro()
        {
            Build();
            intro = 0f;
            lettersLanded = steadyFrames = 0;
            impactDone = pressDone = false;
            shineClock = 0f;
            if (logoImage != null) logoImage.enabled = false;
            if (logoGlow != null) logoGlow.color = new Color(1f, 1f, 1f, 0f);
            for (int i = 0; i < letterImages.Length; i++) letterImages[i].gameObject.SetActive(false);
            cover.enabled = true;
            cover.color = Color.black;
            if (subtitle != null && typed != null) subtitle.Text = typed[0];
            if (hiScore != null) ((RectTransform)hiScore.transform).anchoredPosition = hiBase + new Vector2(340f, 0f);
            pressGroup.alpha = 0f;
        }

        /// <summary>Coupe l'entrée (passage à la démo) : tout est remis à sa place.</summary>
        void StopIntro()
        {
            if (!built) return;
            intro = -1f;
            cover.enabled = false;
            if (logoImage != null) logoImage.enabled = true;
            for (int i = 0; i < letterImages.Length; i++) letterImages[i].gameObject.SetActive(false);
            if (subtitle != null && subtitleText != null) subtitle.Text = subtitleText;
            if (hiScore != null) ((RectTransform)hiScore.transform).anchoredPosition = hiBase;
        }

        void RefreshHiScore()
        {
            int best = 0;
            if (HighscoreManager.HasFetchedHighscores && HighscoreManager.Highscores != null && HighscoreManager.Highscores.Count > 0)
                best = HighscoreManager.Highscores[0].score;
            if (hiScore != null)
            {
                hiScore.Text = best > 0 ? $"HI-SCORE {best:D6}" : "";
                hiScore.gameObject.SetActive(best > 0 && !demoMode && (logo == null || logo.gameObject.activeSelf));
            }
        }

        static void Sfx(AudioClip clip, float pitch, float gain)
        {
            if (clip != null) ActionSfx.Instance?.Play(clip, pitch, gain, quantized: false);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool logoPhase = logo != null && logo.gameObject.activeInHierarchy;
            if (intro >= 0f) RunIntro(dt);

            if (pressDone || intro < 0f)
            {
                if (logoPhase && !demoMode)
                {
                    // appel façon borne : allumé, puis un bref creux, en boucle (jamais tout à fait éteint : lisible)
                    float c = Time.unscaledTime % 0.9f;
                    pressGroup.alpha = c < 0.65f ? 1f : 0.25f;
                    pressStart.Color = Gold;
                }
                else
                {
                    float k = 0.5f + 0.5f * Mathf.Cos(Time.unscaledTime * 3.2f);
                    pressGroup.alpha = 0.85f + 0.15f * k;
                    pressStart.Color = Color.Lerp(Color.white, Gold, k);
                }
            }

            if (!logoPhase || (intro >= 0f && intro < JitterEnd)) return;
            float t = Time.unscaledTime;
            logo.anchoredPosition = new Vector2(0f, baseY + Mathf.Round(2f * Mathf.Sin(t * 1.6f)));
            // reflet qui balaie le logo toutes les 3 s
            if (shineImage != null && shine != null && shine.Length > 0)
            {
                shineClock += dt;
                int f = (int)((shineClock - ShineEvery) / ShineFrame);
                if (shineClock >= ShineEvery && f < shine.Length)
                {
                    shineImage.enabled = true;
                    shineImage.sprite = shine[f];
                    shineImage.rectTransform.sizeDelta = shine[f].rect.size;
                }
                else
                {
                    shineImage.enabled = false;
                    if (f >= shine.Length) shineClock = 0f;
                }
            }
        }

        void RunIntro(float dt)
        {
            // au lancement, les premières images sont très longues (chargement, audio) : sans ce garde, une image
            // d'une seconde sautait toute la chute des lettres. On attend que le jeu tourne régulièrement, et le
            // temps de l'entrée avance d'au plus 1/30 s par image.
            if (dt > 0.1f) { steadyFrames = 0; return; }
            if (steadyFrames < 3) { steadyFrames++; return; }
            intro += Mathf.Min(dt, 1f / 30f);
            float t = intro;
            // les lettres tombent une à une (accélèrent) et frappent
            for (int i = 0; i < letterImages.Length; i++)
            {
                float s = t - (LetterStart + i * LetterGap);
                var img = letterImages[i];
                if (s < 0f || impactDone) continue;
                if (!img.gameObject.activeSelf) img.gameObject.SetActive(true);
                float k = Mathf.Clamp01(s / LetterFall);
                var p = img.rectTransform.anchoredPosition;
                img.rectTransform.anchoredPosition = new Vector2(p.x, Mathf.Round(Mathf.Lerp(logoY + 110f, logoY, k * k)));
                if (k >= 1f && lettersLanded <= i)
                {
                    lettersLanded = i + 1;
                    CameraRig.Instance?.Shake(0.06f, 0.12f);
                    Sfx(letterSfx, 0.9f + 0.08f * i, 0.8f);
                }
                float since = s - LetterFall;
                letterGlows[i].color = new Color(1f, 1f, 1f, since >= 0f ? Mathf.Clamp01(1f - since / WhiteFade) : 0f);
            }
            // le gros impact : flash, le décor apparaît, le logo complet s'illumine
            if (!impactDone && t >= BigImpact)
            {
                impactDone = true;
                for (int i = 0; i < letterImages.Length; i++) letterImages[i].gameObject.SetActive(false);
                if (logoImage != null) logoImage.enabled = true;
                cover.enabled = false;
                ScreenFx.Instance?.Flash(Color.white, 1f);
                CameraRig.Instance?.Shake(0.3f, 0.45f);
                CameraRig.Instance?.Punch(1f);
                Sfx(impactSfx, 0.8f, 1f);
            }
            if (impactDone && logo != null)
            {
                float since = t - BigImpact;
                if (logoGlow != null) logoGlow.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - since / LogoGlow));
                float a = t < JitterEnd ? 4f * (1f - since / (JitterEnd - BigImpact)) : 0f;
                logo.anchoredPosition = new Vector2(Mathf.Round(Mathf.Sin(t * 90f) * a), logoY + Mathf.Round(Mathf.Cos(t * 77f) * a * 0.5f));
            }
            // le sous-titre s'écrit, le meilleur score glisse de la droite
            if (subtitle != null && typed != null && t >= TypeStart)
            {
                int n = Mathf.Min(typed.Length - 1, (int)((t - TypeStart) / TypeStep));
                if (subtitle.Text != typed[n]) subtitle.Text = typed[n];
            }
            if (hiScore != null && t >= HiStart)
            {
                float k = Mathf.Clamp01((t - HiStart) / (HiEnd - HiStart));
                k = 1f - (1f - k) * (1f - k) * (1f - k);
                ((RectTransform)hiScore.transform).anchoredPosition = hiBase + new Vector2(Mathf.Round(340f * (1f - k)), 0f);
            }
            if (!pressDone && t >= PressAt)
            {
                pressDone = true;
                ScreenFx.Instance?.Flash(Gold, 0.35f);
            }
            if (!pressDone) pressGroup.alpha = 0f;
            if (pressDone && t >= PressAt + 0.3f && t >= JitterEnd) intro = -1f;
        }
    }
}
