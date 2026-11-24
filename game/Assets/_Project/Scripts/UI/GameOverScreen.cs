using Kenta.Core;
using Kenta.FX;
using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.UI
{
    /// <summary>
    /// Partie perdue (docs/da-kenta.md § 8.5) : « GAME OVER » tombe sur le temps fort suivant, puis la note (S+ … D) et le panneau : score, pièces, PARFAIT / BIEN / BOF, monde.
    /// </summary>
    public class GameOverScreen : MonoBehaviour
    {
        [SerializeField] RectTransform title;
        [SerializeField] PixelText titleText;
        [SerializeField] GameObject panel;
        [SerializeField] PixelText scoreText;
        [SerializeField] PixelText coinsText;
        [SerializeField] PixelText biomeText;
        [Tooltip("Grande lettre de note (S+ … D).")]
        [SerializeField] PixelText gradeText;
        [SerializeField] PixelText judgeText;
        [SerializeField] float dropDelay = 0.9f;
        [SerializeField] float panelDelay = 1.7f;

        [Header("Victoire : feu d'artifice dans le monde")]
        [SerializeField] float fireworkInterval = 0.4f;
        [SerializeField] int fireworkCount = 12;

        static readonly Color Gold = new(1f, 0.82f, 0.23f);
        static readonly Color[] FireworkColors =
        {
            new(1f, 0.82f, 0.23f), new(1f, 0.42f, 0.19f), new(0.44f, 0.84f, 1f), new(1f, 0.45f, 0.7f), Color.white,
        };
        static readonly int[] FireworkNotes = { -12, -9, -7, -5, -2, 0, 3, 5, 7, 10, 12, 15 };   // la mineur pentatonique, qui monte

        float t, nextFirework;
        int fireworks;
        bool victory;
        Color titleDefault = Color.white;
        bool titleDefaultRead;

        void OnEnable()
        {
            t = 0f;
            var run = RunManager.Instance;
            victory = run != null && run.Completed;
            fireworks = 0;
            nextFirework = 0.15f;
            if (!titleDefaultRead) { titleDefault = titleText.Color; titleDefaultRead = true; }
            titleText.Color = victory ? Gold : titleDefault;
            if (victory)
            {
                CameraRig.Instance?.Flash(Gold, 0.35f);
                ActionSfx.Instance?.Checkpoint();
            }
            if (run != null)
            {
                titleText.Text = run.Completed ? "BRAVO !" : "GAME OVER";
                scoreText.Text = $"SCORE  {run.Score:D6}";
                coinsText.Text = $"PIÈCES  {run.Lums}/{run.TotalLums}";
                var b = run.CurrentBiome;
                biomeText.Text = b != null ? $"MONDE  {b.label}" : "";
                var j = run.JudgeStats;
                if (judgeText != null) judgeText.Text = $"PARFAIT {j.perfect}  BIEN {j.good}  BOF {j.meh}";
                if (gradeText != null) gradeText.Text = run.Grade;
            }
            title.gameObject.SetActive(false);
            panel.SetActive(false);
            if (gradeText != null) gradeText.gameObject.SetActive(false);
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            if (victory && fireworks < fireworkCount && t >= nextFirework) LaunchFirework();
            if (t >= dropDelay)
            {
                title.gameObject.SetActive(true);
                float k = Mathf.Clamp01((t - dropDelay) / 0.35f);
                title.anchoredPosition = new Vector2(0f, Mathf.Round(Mathf.Lerp(110f, 48f, k * k)));
            }
            if (t >= panelDelay)
            {
                panel.SetActive(true);
                if (gradeText != null) gradeText.gameObject.SetActive(true);
            }
        }

        /// <summary>Une gerbe dans le haut de la vue (au-dessus du panneau), une note qui monte à chaque fois.</summary>
        void LaunchFirework()
        {
            nextFirework = t + fireworkInterval;
            var rig = CameraRig.Instance;
            var screen = PixelScreen.Instance;
            if (rig == null || screen == null || FxPool.Instance == null) { fireworks = fireworkCount; return; }
            Vector2 view = screen.ViewUnits;
            // positions réparties de gauche à droite en zigzag, dans le tiers haut de la vue
            float u = ((fireworks * 7) % 10 + 0.5f) / 10f;
            float v = 0.55f + 0.3f * ((fireworks * 3) % 4) / 3f;
            var pos = rig.ExactPosition + new Vector2((u - 0.5f) * view.x, (v - 0.5f) * view.y);
            var color = FireworkColors[fireworks % FireworkColors.Length];
            FxPool.Instance.Burst(pos, color, 40, 13f, 0.3f, 1.3f, 4f, 0f);
            FxPool.Instance.Burst(pos, Color.white, 12, 6f, 0.25f, 0.6f, 0f, 0f);
            ActionSfx.Instance?.LumNote(FireworkNotes[fireworks % FireworkNotes.Length]);
            fireworks++;
        }
    }
}
