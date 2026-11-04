using UnityEngine;
using UnityEngine.UI;

namespace Kenta.FX
{
    /// <summary>
    /// Un calque pixel : une caméra qui écrit dans sa RenderTexture (filtre Point), affichée agrandie par une RawImage.
    /// Le calque peut suivre la fraction de pixel de la caméra (décalage de l'image agrandie) : le décor glisse
    /// alors par pas d'1 px d'écran au lieu d'un pixel de jeu entier.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class PixelLayer : MonoBehaviour
    {
        [SerializeField] Camera layerCamera;
        [SerializeField] bool followX;
        [SerializeField] bool followY;
        [Tooltip("Pixels de marge autour de l'image (révélés par le décalage sous-pixel). 0 pour l'interface.")]
        [SerializeField] int margin = 1;

        RawImage display;
        RenderTexture rt;
        int scale = 1;
        Vector2 fraction;

        public Camera LayerCamera => layerCamera;

        public void Bind(int width, int height, int newScale)
        {
            display ??= GetComponent<RawImage>();
            int w = width + 2 * margin, h = height + 2 * margin;
            if (rt == null || rt.width != w || rt.height != h)
            {
                if (rt != null) rt.Release();
                rt = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32)
                {
                    filterMode = FilterMode.Point,
                    antiAliasing = 1,
                    useMipMap = false,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "Pixel_" + name,
                };
                rt.Create();
            }
            layerCamera.targetTexture = rt;
            layerCamera.orthographic = true;
            layerCamera.orthographicSize = h / (2f * PixelScreen.Ppu);
            display.texture = rt;
            scale = newScale;
            var r = display.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(w * scale, h * scale);
            Apply();
        }

        /// <summary>Fraction de pixel de jeu (−0,5 … 0,5) que l'image doit rattraper.</summary>
        public void SetFraction(Vector2 fracPx)
        {
            fraction = fracPx;
            Apply();
        }

        void Apply()
        {
            if (display == null) return;
            float x = followX ? -fraction.x * scale : 0f;
            float y = followY ? -fraction.y * scale : 0f;
            display.rectTransform.anchoredPosition = new Vector2(Mathf.Round(x), Mathf.Round(y));
        }

        void OnDestroy()
        {
            if (rt != null) rt.Release();
        }
    }
}
