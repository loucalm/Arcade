using Kenta.Core;
using Kenta.FX;
using UnityEngine;

namespace Kenta.UI
{
    /// <summary>
    /// Mot PARFAIT / BIEN / BOF qui apparaît au-dessus du héros à chaque action jugée, monte doucement et s'efface.
    /// </summary>
    public class JudgePopup : MonoBehaviour
    {
        [SerializeField] PixelText label;
        [SerializeField] float duration = 0.5f;
        [SerializeField] float risePixels = 8f;
        [SerializeField] Color perfectColor = new(1f, 0.82f, 0.23f);
        [SerializeField] Color goodColor = new(0.44f, 0.84f, 1f);
        [SerializeField] Color mehColor = new(0.66f, 0.63f, 0.75f);

        RunManager run;
        CanvasGroup group;
        Vector2 anchor;
        float t = 99f;

        void Awake()
        {
            group = label.GetComponent<CanvasGroup>();
            if (group == null) group = label.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
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
            var rig = CameraRig.Instance;
            Vector2 cam = rig != null ? rig.ExactPosition : Vector2.zero;
            anchor = new Vector2(Mathf.Round((world.x - cam.x) * PixelScreen.Ppu), Mathf.Round((world.y - cam.y) * PixelScreen.Ppu));
            t = 0f;
        }

        void Update()
        {
            if (t > duration) return;
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            ((RectTransform)label.transform).anchoredPosition = anchor + new Vector2(0f, Mathf.Round(risePixels * k));
            group.alpha = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
        }
    }
}
