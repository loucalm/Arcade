using Kenta.Core;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// La Sourdine (docs/da-kenta.md § 5) : un mur de silence au bord gauche de l'écran. Il avance quand le héros est touché
    /// et recule quand le multiplicateur monte : sa distance montre l'état du combo. Tout ce qu'elle couvre perd sa couleur
    /// (calque gris tramé, sans shader). En fin de partie elle recouvre l'écran.
    /// </summary>
    public class Sourdine : MonoBehaviour
    {
        [SerializeField] RectTransform body;     // 104 px de large, bord droit à x = 100
        [SerializeField] RectTransform veil;     // 56 px de large, collé à droite du corps
        [Tooltip("Aplat gris translucide qui prolonge le calque tramé jusqu'au bord droit, en fin de partie.")]
        [SerializeField] RectTransform veilFill;
        [SerializeField] float hiddenX = -60f;
        [SerializeField] float speed = 36f;
        [SerializeField] float coverSpeed = 220f;

        float edge;
        bool covering;

        public bool Covered => covering && edge >= 99f;

        void OnEnable() => ResetPosition();

        public void ResetPosition()
        {
            covering = false;
            edge = hiddenX;
            Apply();
        }

        /// <summary>Fin de partie : la Sourdine recouvre tout l'écran.</summary>
        public void Cover() => covering = true;

        float Target()
        {
            if (covering) return 100f;                 // la masse recouvre les 100 px de gauche, le gris prend tout le reste
            var run = RunManager.Instance;
            if (run == null || !run.IsRunning) return hiddenX;
            float combo = (8 - run.Multiplier) * 5f;
            float hits = (run.StartHearts - run.Hearts) * 16f;
            return Mathf.Clamp(10f + combo + hits, 4f, 140f);
        }

        void Update()
        {
            edge = Mathf.MoveTowards(edge, Target(), (covering ? coverSpeed : speed) * Time.unscaledDeltaTime);
            Apply();
        }

        void Apply()
        {
            float e = Mathf.Round(edge);
            if (body != null) body.anchoredPosition = new Vector2(e - 100f, 0f);
            if (veil != null) veil.anchoredPosition = new Vector2(e, 0f);
            if (veilFill != null)
            {
                float x = e + 56f;
                veilFill.anchoredPosition = new Vector2(x, 0f);
                veilFill.sizeDelta = new Vector2(covering ? Mathf.Max(0f, PixelScreen.Width - x) : 0f, PixelScreen.Height);
            }
        }
    }
}
