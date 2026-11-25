using System.Collections.Generic;
using Kenta.Rhythm;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.FX
{
    /// <summary>
    /// Effets d'écran pilotés par la chart, pour la fin du niveau : l'image du jeu se dédouble (2, 5 ou 9 copies plus
    /// petites, agrandies d'un facteur entier) et grésille (neige, image qui se décale, couleurs qui sautent, coupures
    /// d'une image) au rythme des temps. Ils gênent la lecture sans la rendre impossible : la chart les cale sur des
    /// passages simples. L'interface (score, multiplicateur) reste nette par-dessus.
    /// Créé par PixelScreen ; aucun réglage dans la scène.
    /// </summary>
    [DefaultExecutionOrder(100)]   // après CameraRig, qui repositionne les calques à chaque image
    public class ScreenGlitch : MonoBehaviour
    {
        public static ScreenGlitch Instance { get; private set; }

        PixelScreen screen;
        readonly List<RectTransform> copies = new();
        readonly List<RawImage> copyImages = new();
        RawImage noise;
        Texture2D noiseTex;
        int copyCount = 1;
        float glitchLevel;
        double glitchUntilBeat = -1;
        int frame;

        public int Copies => copyCount;
        /// <summary>Intensité du grésillement en ce moment (0 = image nette).</summary>
        public float Level { get; private set; }

        void Awake()
        {
            Instance = this;
            screen = GetComponent<PixelScreen>();
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
            Level = 0f;
            if (noise != null) noise.enabled = false;
            ApplyMain(Vector2.zero, Color.white, true);
        }

        /// <summary>
        /// Nombre de copies de l'image : 1 (normal), 2 (côte à côte), 5 (centre et coins), 9 (grille 3×3). Chaque copie est
        /// l'image entière, en plus petit (agrandissement entier, un tiers de l'écran sur la borne).
        /// </summary>
        public void SetCopies(int count)
        {
            count = count >= 9 ? 9 : count >= 5 ? 5 : count >= 2 ? 2 : 1;
            foreach (var c in copies) if (c != null) Destroy(c.gameObject);
            copies.Clear();
            copyImages.Clear();
            copyCount = count;
            if (screen == null || screen.Frame == null) return;
            bool split = count > 1;
            ApplyMain(Vector2.zero, Color.white, !split);
            if (!split) return;

            var frameRect = screen.Frame.rect;
            Vector2 cell = new(frameRect.width / 3f, frameRect.height / 3f);
            int insertAt = screen.ActorsLayer.Display.transform.GetSiblingIndex() + 1;
            foreach (var g in Cells(count))
            {
                var holder = new GameObject("Copy", typeof(RectTransform)).GetComponent<RectTransform>();
                holder.SetParent(screen.Frame, false);
                holder.SetSiblingIndex(insertAt++);
                holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0.5f);
                holder.anchoredPosition = new Vector2(Mathf.Round((g.x - 1) * cell.x), Mathf.Round((1 - g.y) * cell.y));
                AddCopyImage(holder, screen.WorldLayer);
                AddCopyImage(holder, screen.ActorsLayer);
                copies.Add(holder);
            }
        }

        static IEnumerable<Vector2Int> Cells(int count)
        {
            if (count == 2) { yield return new Vector2Int(0, 1); yield return new Vector2Int(2, 1); yield break; }
            if (count == 5)
            {
                yield return new Vector2Int(1, 1);
                yield return new Vector2Int(0, 0); yield return new Vector2Int(2, 0);
                yield return new Vector2Int(0, 2); yield return new Vector2Int(2, 2);
                yield break;
            }
            for (int j = 0; j < 3; j++)
                for (int i = 0; i < 3; i++)
                    yield return new Vector2Int(i, j);
        }

        void AddCopyImage(RectTransform holder, PixelLayer layer)
        {
            var img = new GameObject(layer.name, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            img.transform.SetParent(holder, false);
            img.texture = layer.Texture;
            img.raycastTarget = false;
            int k = Mathf.Max(1, screen.Scale / 3);                       // ×1 sur la borne (×3 en temps normal)
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(layer.Texture.width * k, layer.Texture.height * k);
            copyImages.Add(img);
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
            foreach (var layer in new[] { screen.WorldLayer, screen.ActorsLayer })
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
                foreach (var layer in new[] { screen.WorldLayer, screen.ActorsLayer })
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
