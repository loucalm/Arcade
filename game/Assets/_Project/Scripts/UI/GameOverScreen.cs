using Kenta.Core;
using UnityEngine;

namespace Kenta.UI
{
    /// <summary>
    /// Partie perdue (docs/da-kenta.md § 8.5) : la Sourdine recouvre l'écran, tout passe en gris, « GAME OVER » tombe sur le
    /// temps fort suivant, puis la note (S+ … D) et le panneau : score, pièces, PARFAIT / BIEN / BOF, monde.
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

        float t;

        void OnEnable()
        {
            t = 0f;
            var run = RunManager.Instance;
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
    }
}
