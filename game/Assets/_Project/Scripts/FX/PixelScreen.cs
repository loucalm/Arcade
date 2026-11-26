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

        /// <summary>Largeurs de vue possibles (px de jeu) : sur 1920×1080, agrandies ×6, ×5, ×4 et ×3 (toujours entier).</summary>
        public static readonly int[] ZoomWidths = { 320, 384, 480, 640 };

        /// <summary>Largeur de la vue du monde en px de jeu (zoom de la caméra). L'interface reste en 320×180.</summary>
        public int ViewWidth { get; private set; } = Width;
        public int ViewHeight => ViewWidth * 9 / 16;
        /// <summary>Taille de la vue du monde en unités.</summary>
        public Vector2 ViewUnits => new(ViewWidth / (float)Ppu, ViewHeight / (float)Ppu);

        public int Divider { get; private set; } = 1;
        /// <summary>Agrandissement non entier du cadre entier quand l'agrandissement entier laisserait trop de noir (1 sur la borne).</summary>
        public float FitScale { get; private set; } = 1f;
        public RectTransform Frame => frame;
        public PixelLayer WorldLayer => world;
        public PixelLayer ActorsLayer => actors;
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
            if (GetComponent<ScreenGlitch>() == null) gameObject.AddComponent<ScreenGlitch>();   // copies et grésillement (chart)
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
            int w = ViewWidth / Divider, h = ViewHeight / Divider;
            Scale = PixelMath.IntegerScale(Screen.width, Screen.height, w, h);
            if (frame != null)
            {
                frame.sizeDelta = new Vector2(w * Scale, h * Scale);
                // fenêtre de taille impaire : le cadre centré tomberait sur un demi-pixel d'écran (colonnes perdues)
                frame.anchoredPosition = PixelMath.CenterCorrection(Screen.width, Screen.height, w * Scale, h * Scale);
                // plein cadre (ADR-032) : hors 1920×1080 (Game view de l'éditeur…), tout le cadre est agrandi au plus
                // grand qui rentre ; les pixels sont alors un peu inégaux, mais le jeu remplit l'écran, quel que soit le
                // plan de caméra (sinon le cadre changerait de taille à chaque zoom). Sur la borne (1920×1080), le
                // facteur vaut 1 pour toutes les largeurs de vue : rien ne change.
                float fit = Mathf.Min(Screen.width / (float)(w * Scale), Screen.height / (float)(h * Scale));
                FitScale = fit > 1.01f ? fit : 1f;
                frame.localScale = new Vector3(FitScale, FitScale, 1f);
            }
            world.Bind(w, h, Scale);
            actors.Bind(w, h, Scale);
            // L'interface est dans le cadre : son agrandissement se calcule sur le cadre (pas sur l'écran), sinon elle
            // déborde et se fait rogner (HUD absent, consignes coupées). S'il reste un écart, elle est étirée au cadre.
            int uiScale = PixelMath.IntegerScale(w * Scale, h * Scale, Width, Height);
            ui.Bind(Width, Height, uiScale);
            float uiFit = Mathf.Max(1f, Mathf.Min(w * Scale / (float)(Width * uiScale), h * Scale / (float)(Height * uiScale)));
            ui.Display.rectTransform.localScale = new Vector3(uiFit, uiFit, 1f);
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

        /// <summary>
        /// Change le zoom (largeur de vue la plus proche parmi ZoomWidths). Changement net, comme une coupe de caméra :
        /// la définition change mais chaque pixel de jeu reste un carré entier de pixels d'écran.
        /// </summary>
        public void SetZoom(int viewWidth)
        {
            int best = ZoomWidths[0];
            foreach (int z in ZoomWidths)
                if (Mathf.Abs(z - viewWidth) < Mathf.Abs(best - viewWidth)) best = z;
            if (best == ViewWidth) return;
            ViewWidth = best;
            Build();
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
