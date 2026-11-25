using Kenta.Core;
using Kenta.FX;
using UnityEngine;

namespace Kenta.UI
{
    /// <summary>
    /// Mot PARFAIT / BIEN / BOF qui apparaît au-dessus du héros à chaque action jugée, monte doucement et s'efface.
    /// Il suit le héros (CameraRig.Target) : posé au point jugé, il restait en arrière pendant la course et passait sous
    /// le héros sur les tambours (audit du 2026-11-25). Sans héros, il reste au point jugé.
    /// </summary>
    public class JudgePopup : MonoBehaviour
    {
        [SerializeField] PixelText label;
        [SerializeField] float duration = 0.5f;
        [SerializeField] float risePixels = 8f;
        [Tooltip("Hauteur (u) du mot au-dessus des pieds du héros.")]
        [SerializeField] float aboveHero = 1.7f;
        [SerializeField] Color perfectColor = new(1f, 0.82f, 0.23f);
        [SerializeField] Color goodColor = new(0.44f, 0.84f, 1f);
        [SerializeField] Color mehColor = new(0.66f, 0.63f, 0.75f);

        RunManager run;
        CanvasGroup group;
        Vector2 world;
        Transform hero;
        float t = 99f;

        void Awake()
        {
            group = label.GetComponent<CanvasGroup>();
            if (group == null) group = label.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            label.Scale = 1;                     // petit et discret : il ne doit pas masquer l'action
        }

        void OnEnable()
        {
            run = RunManager.Instance;
            if (run != null) run.OnJudged += Show;
        }

        void OnDisable()
        {
            if (run != null) run.OnJudged -= Show;
            t = 99f;
            if (group != null) group.alpha = 0f;
        }

        void Show(Judgement j, Vector2 world)
        {
            label.Text = TimingJudge.Label(j) + (j == Judgement.Perfect ? " !" : "");
            label.Color = j == Judgement.Perfect ? perfectColor : j == Judgement.Good ? goodColor : mehColor;
            this.world = world;
            hero = CameraRig.Instance != null ? CameraRig.Instance.Target : null;
            t = 0f;
            Place();
        }

        /// <summary>Monde → interface (320×180) : la vue du monde peut être plus large que l'interface (zoom de la caméra).</summary>
        Vector2 Anchor()
        {
            var rig = CameraRig.Instance;
            Vector2 cam = rig != null ? rig.ExactPosition : Vector2.zero;
            Vector2 p = hero != null && hero.gameObject.activeInHierarchy ? (Vector2)hero.position + new Vector2(0f, aboveHero) : world;
            float k = PixelScreen.Instance != null ? PixelScreen.Width / (float)PixelScreen.Instance.ViewWidth : 1f;
            return new Vector2(Mathf.Round((p.x - cam.x) * PixelScreen.Ppu * k), Mathf.Round((p.y - cam.y) * PixelScreen.Ppu * k));
        }

        void Place()
        {
            float k = Mathf.Clamp01(t / duration);
            ((RectTransform)label.transform).anchoredPosition = Anchor() + new Vector2(0f, Mathf.Round(risePixels * k));
            group.alpha = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
        }

        void Update()
        {
            if (t > duration) return;
            t += Time.unscaledDeltaTime;
            Place();
        }
    }
}
