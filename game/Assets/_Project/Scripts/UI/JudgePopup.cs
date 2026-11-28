using Kenta.Core;
using Kenta.FX;
using UnityEngine;

namespace Kenta.UI
{
    /// <summary>
    /// Mot PARFAIT / BIEN / BOF qui accompagne le héros à chaque action jugée.
    /// - Exécuté en LateUpdate après CameraRig (-50) et ScreenGlitch (+100) : aucun décalage sur les tambours, les plongeons
    ///   ni les coupes de zoom.
    /// - Suit ScreenGlitch (miroir gauche/droite, retournement haut/bas, et grille 2×2 où chacun des 4 cadrans affiche son
    ///   propre mot, toujours écrit à l'endroit).
    /// - Style sans boîte rectangulaire : contour pixel d'1 px (lisible même sur le monde blanc/négatif du niveau 9),
    ///   2 vagues superposables pour encaisser les rafales de croches sans téléportation, flash d'impact sur PARFAIT et
    ///   légère chute + tremblement sur BOF.
    /// </summary>
    [DefaultExecutionOrder(120)]
    public class JudgePopup : MonoBehaviour
    {
        const int WaveCount = 2;
        const int MaxQuads = 4;

        [SerializeField] PixelText label;
        [SerializeField] float duration = 0.48f;
        [SerializeField] float risePixels = 10f;
        [Tooltip("Hauteur (u) du mot au-dessus des pieds du héros.")]
        [SerializeField] float aboveHero = 1.55f;
        [SerializeField] Color perfectColor = new(1f, 0.84f, 0.24f);
        [SerializeField] Color perfectFlashColor = new(1f, 0.98f, 0.86f);
        [SerializeField] Color goodColor = new(0.44f, 0.86f, 1f);
        [SerializeField] Color mehColor = new(0.80f, 0.49f, 0.56f);
        [SerializeField] Color outlineColor = new(20 / 255f, 16 / 255f, 36 / 255f, 1f);

        struct Wave
        {
            public Judgement judgement;
            public Vector2 fallbackWorld;
            public float elapsed;
            public float totalDuration;
            public PixelText[] texts;
            public CanvasGroup[] groups;
            public RectTransform[] rects;
        }

        Wave[] waves;
        int nextWave;
        RunManager run;
        Transform hero;
        Kenta.Player.PlayerController player;
        string perfectText, goodText, mehText;

        void Awake()
        {
            EnsurePool();
        }

        void EnsurePool()
        {
            if (waves != null || label == null) return;
            var font = label.Font;
            var parent = label.transform.parent;
            int sibling = label.transform.GetSiblingIndex();

            ConfigureText(label, font);
            waves = new Wave[WaveCount];
            for (int w = 0; w < WaveCount; w++)
            {
                var wave = new Wave
                {
                    elapsed = 99f,
                    totalDuration = duration,
                    texts = new PixelText[MaxQuads],
                    groups = new CanvasGroup[MaxQuads],
                    rects = new RectTransform[MaxQuads],
                };
                for (int q = 0; q < MaxQuads; q++)
                {
                    PixelText pt;
                    if (w == 0 && q == 0)
                    {
                        pt = label;
                    }
                    else
                    {
                        var go = new GameObject($"Judge_W{w}_Q{q}", typeof(RectTransform), typeof(CanvasGroup));
                        go.transform.SetParent(parent, false);
                        go.transform.SetSiblingIndex(sibling + w * MaxQuads + q);
                        var rt = (RectTransform)go.transform;
                        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.sizeDelta = Vector2.zero;
                        pt = go.AddComponent<PixelText>();
                        pt.Setup(font, "", perfectColor, PixelText.Align.Center, 1);
                        ConfigureText(pt, font);
                    }
                    var cg = pt.GetComponent<CanvasGroup>();
                    if (cg == null) cg = pt.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = 0f;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                    wave.texts[q] = pt;
                    wave.groups[q] = cg;
                    wave.rects[q] = (RectTransform)pt.transform;
                }
                waves[w] = wave;
            }
        }

        void ConfigureText(PixelText pt, PixelFontData font)
        {
            if (pt == null) return;
            if (pt.Font == null && font != null) pt.Setup(font, "", perfectColor, PixelText.Align.Center, 1);
            pt.Scale = 1;
            pt.Plate = false;
            pt.Outline = true;
            pt.OutlineColor = outlineColor;
        }

        void OnEnable()
        {
            EnsurePool();
            run = RunManager.Instance;
            if (run != null) run.OnJudged += Show;
        }

        void OnDisable()
        {
            if (run != null) run.OnJudged -= Show;
            HideAll();
        }

        void HideAll()
        {
            if (waves == null) return;
            for (int w = 0; w < waves.Length; w++)
            {
                waves[w].elapsed = 99f;
                for (int q = 0; q < MaxQuads; q++)
                    if (waves[w].groups[q] != null) waves[w].groups[q].alpha = 0f;
            }
        }

        void Show(Judgement j, Vector2 world)
        {
            EnsurePool();
            if (waves == null) return;

            perfectText ??= TimingJudge.Label(Judgement.Perfect);
            goodText ??= TimingJudge.Label(Judgement.Good);
            mehText ??= TimingJudge.Label(Judgement.Meh);
            string word = j == Judgement.Perfect ? perfectText : j == Judgement.Good ? goodText : mehText;
            Color initialColor = j == Judgement.Perfect ? perfectFlashColor : j == Judgement.Good ? goodColor : mehColor;

            var target = CameraRig.Instance != null ? CameraRig.Instance.Target : null;
            if (target != hero) player = target != null ? target.GetComponent<Kenta.Player.PlayerController>() : null;
            hero = target;

            ref var wave = ref waves[nextWave];
            nextWave = (nextWave + 1) % waves.Length;
            wave.judgement = j;
            wave.fallbackWorld = world;
            wave.elapsed = 0f;
            wave.totalDuration = j == Judgement.Good ? duration * 0.88f : duration;
            for (int q = 0; q < MaxQuads; q++)
            {
                wave.texts[q].Text = word;
                wave.texts[q].Color = initialColor;
            }
            PlaceWave(ref wave);
        }

        void LateUpdate()
        {
            if (waves == null) return;
            float dt = Time.unscaledDeltaTime;
            for (int w = 0; w < waves.Length; w++)
            {
                if (waves[w].elapsed > waves[w].totalDuration) continue;
                waves[w].elapsed += dt;
                PlaceWave(ref waves[w]);
            }
        }

        void PlaceWave(ref Wave wave)
        {
            if (wave.elapsed > wave.totalDuration)
            {
                for (int q = 0; q < MaxQuads; q++)
                    if (wave.groups[q].alpha != 0f) wave.groups[q].alpha = 0f;
                return;
            }

            var rig = CameraRig.Instance;
            var screen = PixelScreen.Instance;
            var glitch = ScreenGlitch.Instance;
            Vector2 cam = rig != null ? rig.ExactPosition : Vector2.zero;
            int viewW = screen != null ? screen.ViewWidth : PixelScreen.Width;
            bool flipX = glitch != null && glitch.FlipX;
            bool flipY = glitch != null && glitch.FlipY;
            int copies = glitch != null ? glitch.Copies : 1;
            int quadCount = HudLayout.ActiveQuadCount(copies);

            bool inv = player != null && player.IsInverted;
            // Position monde de référence : au-dessus de la tête du héros (ou sous ses pieds au plafond)
            Vector2 worldAnchor = hero != null && hero.gameObject.activeInHierarchy && (player == null || !player.IsDead)
                ? (Vector2)hero.position + new Vector2(0f, inv ? -aboveHero : aboveHero)
                : wave.fallbackWorld;

            // Sens visuel à l'écran : si gravité inversée OU écran retourné (mais pas les deux, qui s'annulent),
            // le héros a la tête vers le bas à l'écran → le texte s'écarte vers le bas.
            bool headDownOnScreen = inv ^ flipY;
            float dirY = headDownOnScreen ? -1f : 1f;

            float k = Mathf.Clamp01(wave.elapsed / Mathf.Max(0.01f, wave.totalDuration));
            float quadScale = quadCount > 1 ? 0.7f : 1f;

            // Mouvement distinct par jugement : pop rapide ease-out pour PARFAIT/BIEN, petite chute + frisson pour BOF
            float dy, dx = 0f;
            if (wave.judgement == Judgement.Meh)
            {
                float drop = 1f - (1f - k) * (1f - k);
                dy = -Mathf.Round(3f * quadScale * drop) * dirY;
                if (wave.elapsed < 0.12f)
                    dx = Mathf.Round(Mathf.Sin(wave.elapsed * 75f));
            }
            else
            {
                float easeOut = 1f - Mathf.Pow(1f - k, 2.6f);
                float amp = wave.judgement == Judgement.Perfect ? risePixels : risePixels * 0.7f;
                dy = Mathf.Round(amp * quadScale * easeOut) * dirY;
            }

            // Flash blanc d'impact sur PARFAIT pendant ~55 ms avant de prendre l'or
            if (wave.judgement == Judgement.Perfect)
            {
                Color c = wave.elapsed < 0.055f ? perfectFlashColor : perfectColor;
                for (int q = 0; q < quadCount; q++) wave.texts[q].Color = c;
            }

            float alpha = k < 0.08f ? k / 0.08f : k < 0.58f ? 1f : 1f - (k - 0.58f) / 0.42f;
            const float padX = 20f, padY = 6f;
            const float hudBottom = HudLayout.BandBottom - HudLayout.Gap - 6f;

            for (int q = 0; q < MaxQuads; q++)
            {
                if (q >= quadCount)
                {
                    if (wave.groups[q].alpha != 0f) wave.groups[q].alpha = 0f;
                    continue;
                }
                Vector2 uiPos = HudLayout.WorldToUiQuad(worldAnchor, cam, viewW, PixelScreen.Ppu, flipX, flipY, copies, q, out Rect bounds);
                float minX = bounds.xMin + padX;
                float maxX = bounds.xMax - padX;
                float minY = bounds.yMin + padY;
                // Les cadrans du haut (q = 0, 1 en 2×2 ; q = 0 en plein écran) restent sous la bande du HUD
                float topLimit = (quadCount == 1 || q < 2) ? Mathf.Min(bounds.yMax - padY, hudBottom) : bounds.yMax - padY;
                Vector2 p = new(
                    Mathf.Round(Mathf.Clamp(uiPos.x + dx, minX, maxX)),
                    Mathf.Round(Mathf.Clamp(uiPos.y + dy, minY, topLimit)));

                if (wave.rects[q].anchoredPosition != p) wave.rects[q].anchoredPosition = p;
                wave.groups[q].alpha = alpha;
            }
        }
    }
}
