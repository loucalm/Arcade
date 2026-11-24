using Kenta.Core;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// La Sourdine (docs/da-kenta.md § 5) : un mur de silence au bord gauche de l'écran. Il recule quand le multiplicateur
    /// monte et revient quand le combo casse : sa distance montre l'état du combo (mort subite : pas de cœurs). Tout ce qu'elle couvre perd sa couleur
    /// (calque gris tramé, sans shader), mais elle s'arrête toujours juste derrière le héros. En fin de partie perdue,
    /// elle recouvre l'écran.
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

        const float VeilWidth = 56f;
        const float HeroHalfWidth = 8f;     // demi-largeur du héros à l'écran (px d'interface), vue large comprise
        const float HeroMargin = 4f;
        const float StepPerMultiplier = 8f;

        float Target()
        {
            if (covering) return 100f;                 // la masse recouvre les 100 px de gauche, le gris prend tout le reste
            var run = RunManager.Instance;
            if (run == null || !run.IsRunning) return hiddenX;
            return TargetEdge(run.Multiplier, HeroScreenX());
        }

        /// <summary>Place du héros à l'écran, en px d'interface (la vue du monde couvre tout l'écran, comme l'interface).</summary>
        static float HeroScreenX() => CameraRig.Instance != null ? CameraRig.Instance.HeroFrameX * PixelScreen.Width : 70f;

        /// <summary>Limite (px depuis la gauche) que le voile ne dépasse jamais : juste derrière le héros.</summary>
        public static float HeroLimit(float heroScreenX) => heroScreenX - HeroHalfWidth - HeroMargin;

        /// <summary>
        /// Bord droit de la masse noire pour un multiplicateur ×1 … ×8 : au plus près à ×1 (14 px de voile visibles
        /// devant elle, puis le héros), elle recule de 8 px par palier. Le héros n'est jamais grisé.
        /// </summary>
        public static float TargetEdge(int multiplier, float heroScreenX) =>
            Mathf.Clamp(HeroLimit(heroScreenX) - 14f - (Mathf.Clamp(multiplier, 1, 8) - 1) * StepPerMultiplier, -60f, 140f);

        /// <summary>Bord gauche du voile : collé à la masse, mais tassé dessous quand elle est tout près du héros.</summary>
        public static float VeilLeft(float edge, float heroScreenX) => Mathf.Min(edge, HeroLimit(heroScreenX) - VeilWidth);

        void Update()
        {
            var run = RunManager.Instance;
            bool won = run != null && run.Completed && !run.IsRunning;      // victoire : elle s'enfuit d'un coup
            edge = Mathf.MoveTowards(edge, Target(), (covering || won ? coverSpeed : speed) * Time.unscaledDeltaTime);
            Apply();
        }

        void Apply()
        {
            float e = Mathf.Round(edge);
            float veilX = covering ? e : Mathf.Round(VeilLeft(e, HeroScreenX()));
            if (body != null) body.anchoredPosition = new Vector2(e - 100f, 0f);
            if (veil != null) veil.anchoredPosition = new Vector2(veilX, 0f);
            if (veilFill != null)
            {
                float x = e + 56f;
                veilFill.anchoredPosition = new Vector2(x, 0f);
                veilFill.sizeDelta = new Vector2(covering ? Mathf.Max(0f, PixelScreen.Width - x) : 0f, PixelScreen.Height);
            }
        }
    }
}
