using UnityEngine;
using UnityEngine.UI;

namespace Kenta.FX
{
    /// <summary>
    /// Effet d'écran cathodique (ADR-034) : le canvas PixelDisplay (les 3 calques pixel, effets de ScreenGlitch compris)
    /// est rendu par une caméra dédiée dans une texture de la taille de l'écran, puis affiché en plein écran par une
    /// RawImage avec le shader Kenta/CrtDisplay (bombé, coins, lignes, lueur). Le jeu reste dessiné en pixels entiers :
    /// seule l'image finale est déformée. Effet coupé : le canvas repasse en Overlay, comme avant.
    /// F8 bascule l'effet (éditeur et Development Build). Câblé par KentaBuilder.
    /// </summary>
    [DefaultExecutionOrder(-40)]   // après PixelScreen (Awake), qui dimensionne le cadre
    public class CrtScreen : MonoBehaviour
    {
        public static CrtScreen Instance { get; private set; }

        [Tooltip("Canvas PixelDisplay (calques pixel agrandis).")]
        [SerializeField] Canvas displayCanvas;
        [Tooltip("Matériau Kenta/CrtDisplay (réglages de l'effet).")]
        [SerializeField] Material material;
        [SerializeField] bool effectOn = true;

        Camera crtCamera;
        RawImage output;
        RenderTexture rt;
        int displayLayer;
        int lastW, lastH;

        public bool EffectOn
        {
            get => effectOn;
            set { effectOn = value; Apply(); }
        }

        void Awake()
        {
            Instance = this;
            displayLayer = LayerMask.NameToLayer("Display");
            if (displayCanvas == null || material == null || displayLayer < 0)
            {
                Debug.LogWarning("CrtScreen : canvas, matériau ou couche « Display » manquant, effet coupé.");
                enabled = false;
                return;
            }

            var camGo = new GameObject("CrtCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.position = new Vector3(0f, 2000f, -10f);   // loin des caméras du jeu
            crtCamera = camGo.AddComponent<Camera>();
            crtCamera.orthographic = true;
            crtCamera.clearFlags = CameraClearFlags.SolidColor;
            crtCamera.backgroundColor = Color.black;
            crtCamera.cullingMask = 1 << displayLayer;
            crtCamera.depth = 50;
            crtCamera.allowHDR = false;
            crtCamera.allowMSAA = false;
            crtCamera.nearClipPlane = 0.1f;
            crtCamera.farClipPlane = 10f;

            var outGo = new GameObject("CrtOutput", typeof(Canvas));
            outGo.transform.SetParent(transform, false);
            var canvas = outGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -90;
            var imgGo = new GameObject("Image", typeof(RectTransform), typeof(RawImage));
            imgGo.transform.SetParent(outGo.transform, false);
            var r = (RectTransform)imgGo.transform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            output = imgGo.GetComponent<RawImage>();
            output.material = material;
            output.raycastTarget = false;

            Resize();
            Apply();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (rt != null) rt.Release();
        }

        void Update()
        {
            if (crtCamera == null || output == null || displayCanvas == null) return;   // scripts rechargés en pleine partie (Play mode) : objets créés à l'exécution perdus, pas d'erreur à chaque image
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.F8)) EffectOn = !EffectOn;
#endif
            if (Screen.width != lastW || Screen.height != lastH)
            {
                Resize();
                Apply();
            }
        }

        void Resize()
        {
            lastW = Screen.width;
            lastH = Screen.height;
            if (rt != null) rt.Release();
            rt = new RenderTexture(Mathf.Max(1, lastW), Mathf.Max(1, lastH), 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                antiAliasing = 1,
                name = "CrtScreen",
            };
            rt.Create();
            crtCamera.targetTexture = rt;
            output.texture = rt;
        }

        void Apply()
        {
            if (crtCamera == null) return;
            crtCamera.enabled = effectOn;
            output.enabled = effectOn;
            if (effectOn)
            {
                SetLayer(displayCanvas.transform, displayLayer);
                displayCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                displayCanvas.planeDistance = 1f;
                // piège : un canvas « Screen Space - Camera » ne recalcule pas sa taille quand la caméra change de texture
                displayCanvas.worldCamera = null;
                displayCanvas.worldCamera = crtCamera;
            }
            else
            {
                displayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                displayCanvas.sortingOrder = -100;
            }
        }

        static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayer(t.GetChild(i), layer);
        }
    }
}
