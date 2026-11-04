using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Le jeu est rendu en 320×180 px puis agrandi d'un facteur entier (×6 sur les 1920×1080 de la borne),
    /// par trois calques : monde (suit la fraction de pixel de la caméra en X et Y), acteurs (héros et bandeau :
    /// suivent seulement Y, pour rester immobiles à l'écran) et interface (fixe). Si la fenêtre n'est pas un
    /// multiple, bandes noires (jamais d'agrandissement non entier). Le tour REMIX réduit la définition (SetDivider).
    /// </summary>
    public class PixelScreen : MonoBehaviour
    {
        public const int Width = 320, Height = 180, Ppu = 40;
        public static PixelScreen Instance { get; private set; }

        [SerializeField] PixelLayer world;
        [SerializeField] PixelLayer actors;
        [SerializeField] PixelLayer ui;
        [Tooltip("Cadre masqué (RectMask2D) de la taille de l'écran agrandi.")]
        [SerializeField] RectTransform frame;
        [Tooltip("Canvas de l'interface (Screen Space - Camera sur la caméra du calque UI).")]
        [SerializeField] Canvas uiCanvas;

        public int Divider { get; private set; } = 1;
        public int Scale { get; private set; } = 1;
        int lastW, lastH;

        void Awake()
        {
            Instance = this;
            // Les caméras qui écrivent dans une texture ne nettoient pas l'écran : une caméra vide s'en charge (bandes noires).
            var clear = new GameObject("ScreenClear").AddComponent<Camera>();
            clear.transform.SetParent(transform, false);
            clear.clearFlags = CameraClearFlags.SolidColor;
            clear.backgroundColor = Color.black;
            clear.cullingMask = 0;
            clear.depth = -100;
            Build();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Screen.width != lastW || Screen.height != lastH) Build();
        }

        void Build()
        {
            lastW = Screen.width;
            lastH = Screen.height;
            int w = Width / Divider, h = Height / Divider;
            Scale = PixelMath.IntegerScale(Screen.width, Screen.height, w, h);
            if (frame != null)
            {
                frame.sizeDelta = new Vector2(w * Scale, h * Scale);
                // fenêtre de taille impaire : le cadre centré tomberait sur un demi-pixel d'écran (colonnes perdues)
                frame.anchoredPosition = PixelMath.CenterCorrection(Screen.width, Screen.height, w * Scale, h * Scale);
            }
            world.Bind(w, h, Scale);
            actors.Bind(w, h, Scale);
            ui.Bind(Width, Height, PixelMath.IntegerScale(Screen.width, Screen.height, Width, Height));
            // piège : un canvas « Screen Space - Camera » ne recalcule pas sa taille quand la caméra change de texture
            if (uiCanvas != null)
            {
                uiCanvas.worldCamera = null;
                uiCanvas.worldCamera = ui.LayerCamera;
            }
        }

        /// <summary>Fraction de pixel (−0,5 … 0,5) de la caméra, rattrapée par le décalage des calques monde et acteurs.</summary>
        public void SetFraction(Vector2 fracPx)
        {
            world.SetFraction(fracPx);
            actors.SetFraction(fracPx);
        }

        /// <summary>1 = 320×180 ; 4 = 80×45 (tour REMIX).</summary>
        public void SetDivider(int divider)
        {
            divider = Mathf.Max(1, divider);
            if (divider == Divider) return;
            Divider = divider;
            Build();
        }

        public static float Snap(float worldUnits) => Mathf.Round(worldUnits * Ppu) / Ppu;
    }
}
