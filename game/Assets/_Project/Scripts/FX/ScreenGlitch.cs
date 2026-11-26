using System.Collections.Generic;
using Kenta.Rhythm;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.FX
{
    /// <summary>
    /// Effets d'écran pilotés par la chart : l'image du jeu se dédouble en une grille qui remplit tout l'écran, sans
    /// bande noire (4 copies en 2×2, agrandies ×1,5 : seule exception aux facteurs entiers, assumée pour l'effet ; ou
    /// 9 copies en 3×3 à ×1) et grésille (neige, image qui se décale, couleurs qui sautent, coupures d'une image) au
    /// rythme des temps, et peut se retourner (tête en bas), passer en miroir (on court vers la gauche) ou les deux
    /// (demi-tour) : un miroir entier, pas de rotation, le pixel art reste net. Ils gênent la lecture sans la rendre
    /// impossible : la chart les cale sur des débuts de phrase et des passages déjà joués. L'interface (score,
    /// multiplicateur) reste à l'endroit, par-dessus.
    /// Créé par PixelScreen ; aucun réglage dans la scène.
    /// </summary>
    [DefaultExecutionOrder(100)]   // après CameraRig, qui repositionne les calques à chaque image
    public class ScreenGlitch : MonoBehaviour
    {
        public static ScreenGlitch Instance { get; private set; }

        PixelScreen screen;
        PixelLayer[] layers;                              // monde et acteurs (tableau gardé : rien d'alloué par image)
        readonly List<RectTransform> copies = new();
        readonly List<RawImage> copyImages = new();
        RawImage noise;
        Texture2D noiseTex;
        int copyCount = 1;
        bool flipX, flipY;
        float glitchLevel;
        double glitchUntilBeat = -1;
        int frame;

        public int Copies => copyCount;
        /// <summary>Image en miroir gauche-droite (X) et/ou retournée haut-bas (Y).</summary>
        public bool FlipX => flipX;
        public bool FlipY => flipY;
        /// <summary>Intensité du grésillement en ce moment (0 = image nette).</summary>
        public float Level { get; private set; }

        void Awake()
        {
            Instance = this;
            screen = GetComponent<PixelScreen>();
            layers = new[] { screen != null ? screen.WorldLayer : null, screen != null ? screen.ActorsLayer : null };
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (noiseTex != null) Destroy(noiseTex);
        }

        /// <summary>Retour à l'image normale (début et fin de partie).</summary>
        public void ResetAll()
        {
            glitchLevel = 0f;
            glitchUntilBeat = -1;
            SetCopies(1);
            SetFlip(false, false);
            Level = 0f;
            if (noise != null) noise.enabled = false;
            ApplyMain(Vector2.zero, Color.white, true);
        }

        /// <summary>
        /// Nombre de copies de l'image : 1 (normal), 4 (grille 2×2) ou 9 (grille 3×3) ; 2 et 5 (anciennes charts) donnent
        /// 4 et 9. Chaque copie est l'image entière, en plus petit ; la grille remplit tout l'écran.
        /// </summary>
        public void SetCopies(int count)
        {
            count = count >= 5 ? 9 : count >= 2 ? 4 : 1;
            foreach (var c in copies) if (c != null) Destroy(c.gameObject);
            copies.Clear();
            copyImages.Clear();
            copyCount = count;
            if (screen == null || screen.Frame == null) return;
            bool split = count > 1;
            ApplyMain(Vector2.zero, Color.white, !split);
            if (!split) return;

            var frameRect = screen.Frame.rect;
            int n = count == 9 ? 3 : 2;
            Vector2 cell = new(frameRect.width / n, frameRect.height / n);
            float k = cell.x / screen.ViewWidth;                         // ×1 en 3×3 sur la borne, ×1,5 en 2×2
            int insertAt = screen.ActorsLayer.Display.transform.GetSiblingIndex() + 1;
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    var holder = new GameObject("Copy", typeof(RectTransform)).GetComponent<RectTransform>();
                    holder.SetParent(screen.Frame, false);
                    holder.SetSiblingIndex(insertAt++);
                    holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0.5f);
                    holder.anchoredPosition = new Vector2(Mathf.Round((i - (n - 1) * 0.5f) * cell.x), Mathf.Round(((n - 1) * 0.5f - j) * cell.y));
                    AddCopyImage(holder, screen.WorldLayer, k);
                    AddCopyImage(holder, screen.ActorsLayer, k);
                    copies.Add(holder);
                }
        }

        void AddCopyImage(RectTransform holder, PixelLayer layer, float k)
        {
            var img = new GameObject(layer.name, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            img.transform.SetParent(holder, false);
            img.texture = layer.Texture;
            img.raycastTarget = false;
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(Mathf.Round(layer.Texture.width * k), Mathf.Round(layer.Texture.height * k));
            img.uvRect = new Rect(flipX ? 1f : 0f, flipY ? 1f : 0f, flipX ? -1f : 1f, flipY ? -1f : 1f);
            copyImages.Add(img);
        }

        /// <summary>
        /// Retourne l'image du jeu : miroir gauche-droite (on court vers la gauche), haut-bas (tête en bas), ou les deux.
        /// Les copies suivent. L'interface ne bouge pas.
        /// </summary>
        public void SetFlip(bool mirror, bool upsideDown)
        {
            flipX = mirror;
            flipY = upsideDown;
            var uv = new Rect(flipX ? 1f : 0f, flipY ? 1f : 0f, flipX ? -1f : 1f, flipY ? -1f : 1f);
            if (screen != null)
                foreach (var layer in layers)
                    if (layer != null && layer.Display != null) layer.Display.uvRect = uv;
            foreach (var img in copyImages)
                if (img != null) img.uvRect = uv;
        }

        /// <summary>Grésillement d'intensité 0..1 pendant `beats` beats (plus fort sur chaque temps, puis il retombe).</summary>
        public void Glitch(float intensity, float beats)
        {
            var c = Conductor.Instance;
            double now = c != null ? c.SongBeat : 0;
            glitchLevel = Mathf.Clamp01(intensity);
            glitchUntilBeat = now + beats;
        }

        void LateUpdate()
        {
            var c = Conductor.Instance;
            double beat = c != null ? c.SongBeat : 0;
            float target = beat < glitchUntilBeat ? glitchLevel : 0f;
            // pulsé sur le temps : fort sur le temps, la moitié entre deux
            float phase = (float)(beat - System.Math.Floor(beat));
            Level = target * (0.45f + 0.55f * Mathf.Exp(-5f * phase));
            frame++;
            // image retournée : le décalage sous-pixel des calques (posé par CameraRig) doit aller dans l'autre sens
            if ((flipX || flipY) && screen != null)
                foreach (var layer in layers)
                {
                    if (layer == null || layer.Display == null) continue;
                    var pos = layer.Display.rectTransform.anchoredPosition;
                    layer.Display.rectTransform.anchoredPosition = new Vector2(flipX ? -pos.x : pos.x, flipY ? -pos.y : pos.y);
                }
            if (Level <= 0.01f)
            {
                if (noise != null && noise.enabled) noise.enabled = false;
                if (copyCount == 1) ApplyMain(Vector2.zero, Color.white, true);
                else ApplyCopies(Vector2.zero, Color.white, true);
                return;
            }

            // décalage horizontal par pixels d'écran entiers, couleurs qui sautent, coupures d'une image
            float r = Random.value;
            Vector2 offset = r < Level * 0.6f ? new Vector2(Mathf.Round((Random.value - 0.5f) * 2f * 24f * Level), 0f) : Vector2.zero;
            Color tint = Random.value < Level * 0.35f ? PickTint() : Color.white;
            bool visible = Random.value > Level * 0.12f;
            if (copyCount == 1) ApplyMain(offset, tint, visible);
            else ApplyCopies(offset, tint, visible);
            UpdateNoise();
        }

        static Color PickTint()
        {
            switch (Random.Range(0, 3))
            {
                case 0: return new Color(1f, 0.45f, 0.55f);
                case 1: return new Color(0.45f, 1f, 0.85f);
                default: return new Color(0.6f, 0.6f, 1f);
            }
        }

        void ApplyMain(Vector2 offset, Color tint, bool visible)
        {
            if (screen == null) return;
            foreach (var layer in layers)
            {
                if (layer == null || layer.Display == null) continue;
                var d = layer.Display;
                d.enabled = visible && copyCount == 1;
                d.color = tint;
                if (offset != Vector2.zero) d.rectTransform.anchoredPosition += offset;
            }
        }

        void ApplyCopies(Vector2 offset, Color tint, bool visible)
        {
            if (screen != null)
                foreach (var layer in layers)
                    if (layer != null && layer.Display != null) layer.Display.enabled = false;
            for (int i = 0; i < copies.Count; i++)
            {
                // chaque copie tremble un peu différemment : on ne sait plus laquelle regarder
                copies[i].gameObject.SetActive(visible || i % 2 == 0);
                foreach (var img in copies[i].GetComponentsInChildren<RawImage>())
                {
                    img.color = tint;
                    img.rectTransform.anchoredPosition = i % 2 == 0 ? offset : -offset;
                }
            }
        }

        /// <summary>Neige : une texture 160×90 de pixels clairs et sombres, renouvelée une image sur deux, par-dessus le jeu.</summary>
        void UpdateNoise()
        {
            if (screen == null || screen.Frame == null) return;
            if (noise == null)
            {
                noiseTex = new Texture2D(160, 90, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
                noise = new GameObject("Noise", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                noise.transform.SetParent(screen.Frame, false);
                noise.raycastTarget = false;
                noise.texture = noiseTex;
                var rt = noise.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            // juste sous l'interface
            int ui = screen.Frame.childCount - 1;
            noise.transform.SetSiblingIndex(Mathf.Max(0, ui - 1));
            noise.enabled = true;
            if (frame % 2 == 0)
            {
                var px = noiseTex.GetPixels32();
                float density = 0.08f + 0.35f * Level;
                int band = Random.Range(0, 90), bandH = Random.Range(2, 10);
                for (int i = 0; i < px.Length; i++)
                {
                    int row = i / 160;
                    bool inBand = row >= band && row < band + bandH;
                    float v = Random.value;
                    if (v < density || (inBand && v < 0.7f))
                    {
                        byte g = (byte)(Random.value < 0.5f ? 20 : 235);
                        px[i] = new Color32(g, g, g, (byte)(inBand ? 200 : 150));
                    }
                    else px[i] = new Color32(0, 0, 0, 0);
                }
                noiseTex.SetPixels32(px);
                noiseTex.Apply(false);
            }
            noise.color = new Color(1f, 1f, 1f, Mathf.Clamp01(0.35f + Level * 0.5f));
        }
    }
}
