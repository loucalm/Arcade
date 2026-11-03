using UnityEngine;
using UnityEngine.UI;

namespace Kenta.FX
{
    /// <summary>
    /// Tout le jeu est rendu en 240×135 px puis agrandi ×8 (entier) sur l'écran de 1920×1080 de la borne :
    /// la caméra de jeu écrit dans une RenderTexture au filtre Point, affichée en plein écran par une RawImage.
    /// Le tour REMIX réduit encore la définition (SetDivider).
    /// </summary>
    public class PixelScreen : MonoBehaviour
    {
        public const int Width = 240, Height = 135, Ppu = 40;
        public static PixelScreen Instance { get; private set; }

        [SerializeField] Camera gameCamera;
        [SerializeField] RawImage display;

        RenderTexture rt;
        public int Divider { get; private set; } = 1;

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
            if (rt != null) rt.Release();
        }

        void Build()
        {
            if (rt != null) rt.Release();
            rt = new RenderTexture(Width / Divider, Height / Divider, 24, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                antiAliasing = 1,
                useMipMap = false,
                wrapMode = TextureWrapMode.Clamp,
                name = "PixelScreen",
            };
            rt.Create();
            gameCamera.targetTexture = rt;
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = Height / (2f * Ppu);
            display.texture = rt;
        }

        /// <summary>1 = 240×135 ; 3 = 80×45 (tour REMIX) ; 4 = 60×33.</summary>
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
