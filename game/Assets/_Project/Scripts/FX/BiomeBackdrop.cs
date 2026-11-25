using System.Collections.Generic;
using Kenta.Core;
using Kenta.Level;
using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Décor du biome courant : un ciel fixe (640×360), le gardien (sprite à part, endormi ou éveillé) et trois calques
    /// de 1280×360 en parallaxe qui bouclent sans couture. Dessinés à la définition de la vue large : leurs pixels ont la
    /// même taille que ceux du niveau. Tout est placé sur la grille de pixels. SetBiome change de monde.
    /// Ambiance par section (SetSection) : teinte et luminosité qui glissent, pour que le décor change au fil du morceau.
    /// En partie, les calques suivent aussi le sol du niveau à la verticale (parallaxe verticale) : le pied des arbres
    /// reste au niveau du sol le plus bas de la vue au lieu de rester collé à l'écran pendant que le niveau monte et
    /// descend (audit du 2026-11-25). Sous chaque calque, sa dernière rangée (le pied, tools/kenta_art.py) est étirée
    /// jusqu'au bas de la vue. Le sol est lu sur CameraRig.HeightProbe, sans rien changer à la caméra.
    /// </summary>
    public class BiomeBackdrop : MonoBehaviour
    {
        [SerializeField] Camera cam;
        [Tooltip("Vitesse relative des calques (0 = fixe à l'écran, 1 = défile comme le niveau) : loin, milieu, proche.")]
        [SerializeField] float[] parallax = { 0.12f, 0.35f, 0.7f };
        [SerializeField] int firstSortingOrder = -100;
        [Tooltip("Couleur de la brume entre le décor et le niveau (l'opacité vient de fogTop / fogBottom).")]
        [SerializeField] Color fog = new(0.93f, 0.96f, 1f, 1f);
        [Tooltip("Opacité de la brume en haut et en bas de la vue : plus dense vers l'horizon, le ciel reste net.")]
        [SerializeField, Range(0f, 1f)] float fogTop = 0.04f;
        [SerializeField, Range(0f, 1f)] float fogBottom = 0.2f;
        [Tooltip("Part du suivi vertical du sol par calque (0 = collé à l'écran, 1 = posé sur le sol) : loin, milieu, proche.")]
        [SerializeField] float[] groundFollow = { 0.35f, 0.7f, 1f };
        [Tooltip("Hauteur (px, image du calque) de sa ligne de sol au-dessus du bas de l'image : 360 − 2 × GROUND_Y de kenta_art.py.")]
        [SerializeField] float layerGroundPx = 96f;
        [Tooltip("Lissage (s) de la hauteur du sol suivie par le décor : pas de saut à chaque marche.")]
        [SerializeField] float groundSmoothTime = 0.6f;
        [Tooltip("Enfoncement du gardien (px) sous sa place d'origine. 0 : il est en entier ; c'est l'élément principal du décor.")]
        [SerializeField] float guardianSink = 0f;

        SpriteRenderer sky;
        SpriteRenderer guardian;
        SpriteRenderer ground;
        SpriteRenderer groundFill;
        SpriteRenderer fogLayer;
        static readonly Color GroundFillTint = new(0.58f, 0.54f, 0.7f);    // comme le flanc des piliers du niveau
        const float GroundFillDepth = 8f;
        readonly SpriteRenderer[][] layers = new SpriteRenderer[3][];
        readonly SpriteRenderer[] feet = new SpriteRenderer[3];
        readonly Dictionary<Sprite, Sprite> footSprites = new();
        float groundSmooth, groundVel;
        bool groundValid;
        BiomeTheme current;
        Color mood = Color.white, moodFrom = Color.white, moodTo = Color.white;
        float moodT = 1f, moodBlend = 1f;

        public BiomeTheme Current => current;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            sky = Make("Sky", firstSortingOrder);
            guardian = Make("Guardian", firstSortingOrder + 5);    // derrière les montagnes lointaines (ADR-028)
            ground = Make("Ground", -5);
            ground.drawMode = SpriteDrawMode.Tiled;
            ground.tileMode = SpriteTileMode.Continuous;
            ground.size = new Vector2(8f, 0.5f);
            ground.enabled = false;
            groundFill = Make("GroundFill", -6);
            groundFill.drawMode = SpriteDrawMode.Tiled;
            groundFill.tileMode = SpriteTileMode.Continuous;
            groundFill.color = GroundFillTint;
            groundFill.enabled = false;
            for (int i = 0; i < 3; i++)
            {
                layers[i] = new[] { Make($"Layer{i}A", firstSortingOrder + 10 * (i + 1)), Make($"Layer{i}B", firstSortingOrder + 10 * (i + 1)) };
                feet[i] = Make($"Layer{i}Foot", firstSortingOrder + 10 * (i + 1));
            }
            fogLayer = Make("Fog", firstSortingOrder + 40);       // au-dessus des 3 calques, sous le niveau
            fogLayer.sprite = FogSprite(fogTop, fogBottom);
            fogLayer.color = new Color(fog.r, fog.g, fog.b, 1f);
        }

        /// <summary>Brume en dégradé vertical (8 bandes, pixel art) : sprite de 1 × 1 unité.</summary>
        static Sprite FogSprite(float top, float bottom)
        {
            const int bands = 8;
            var tex = new Texture2D(1, bands, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < bands; y++)                   // y = 0 en bas
                tex.SetPixel(0, y, new Color(1f, 1f, 1f, Mathf.Lerp(bottom, top, y / (bands - 1f))));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, bands), new Vector2(0.5f, 0.5f), bands);
        }

        /// <summary>Dernière rangée d'un calque (son pied, couleur unie), à étirer sous lui jusqu'au bas de la vue.</summary>
        Sprite FootOf(Sprite strip)
        {
            if (strip == null) return null;
            if (footSprites.TryGetValue(strip, out var foot)) return foot;
            var r = strip.textureRect;
            foot = Sprite.Create(strip.texture, new Rect(r.x, r.y, r.width, 1f), new Vector2(0.5f, 1f), strip.pixelsPerUnit);
            footSprites[strip] = foot;
            return foot;
        }

        SpriteRenderer Make(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            return sr;
        }

        public void SetBiome(BiomeTheme biome)
        {
            if (biome == null) return;
            current = biome;
            sky.sprite = biome.sky;
            guardian.sprite = biome.guardian;
            ground.sprite = biome.ground;
            ground.size = new Vector2(8f, 0.5f);          // le sprite remet la taille à celle d'une tuile : on la rétablit
            groundFill.sprite = biome.fill;
            groundFill.size = new Vector2(8f, GroundFillDepth);
            var strips = new[] { biome.far, biome.mid, biome.near };
            for (int i = 0; i < 3; i++)
            {
                foreach (var sr in layers[i]) sr.sprite = strips[i];
                feet[i].sprite = FootOf(strips[i]);
            }
        }

        /// <summary>Retour à l'ambiance neutre (début de partie, accueil).</summary>
        public void ResetMood()
        {
            mood = moodFrom = moodTo = Color.white;
            moodT = moodBlend = 1f;
        }

        /// <summary>
        /// Ambiance d'une section : une teinte par type de section (couplet chaud, pont frais, refrain doré, final au
        /// couchant…) et une lumière qui baisse doucement au fil du morceau (progress 0 → 1), en `blendSeconds`.
        /// </summary>
        public void SetSection(string sectionName, float progress, float blendSeconds = 1.2f)
        {
            moodFrom = mood;
            moodTo = MoodFor(sectionName, progress);
            moodT = 0f;
            moodBlend = Mathf.Max(0.01f, blendSeconds);
        }

        public static Color MoodFor(string sectionName, float progress)
        {
            string n = (sectionName ?? "").ToLowerInvariant();
            Color tint = n.StartsWith("couplet") ? new Color(1f, 0.96f, 0.88f)
                       : n.StartsWith("pont") ? new Color(0.9f, 0.95f, 1f)
                       : n.StartsWith("roulement") ? new Color(0.96f, 0.86f, 0.82f)
                       : n.StartsWith("refrain") ? new Color(1f, 0.9f, 0.72f)
                       : n.StartsWith("final") ? new Color(1f, 0.8f, 0.7f)
                       : n.StartsWith("fin") ? new Color(0.78f, 0.76f, 0.95f)
                       : Color.white;
            float light = Mathf.Lerp(1f, 0.86f, Mathf.Clamp01(progress));      // le jour baisse au fil du morceau
            return new Color(tint.r * light, tint.g * light, tint.b * light, 1f);
        }

        /// <summary>Sol décoratif sous le ciel (accueil, menus) : en partie, le niveau pose son propre sol.</summary>
        public void SetGroundVisible(bool visible)
        {
            ground.enabled = visible;
            groundFill.enabled = visible && groundFill.sprite != null;
        }

        void LateUpdate()
        {
            if (cam == null || current == null) return;
            var c = cam.transform.position;
            // décor dessiné pour la vue large (640×360) : agrandi seulement si un jour la vue dépasse sa hauteur
            float skyPx = current.sky != null ? current.sky.rect.height : 360f;
            int k = PixelScreen.Instance != null ? Mathf.Max(1, Mathf.CeilToInt(PixelScreen.Instance.ViewHeight / skyPx)) : 1;
            var scale = new Vector3(k, k, 1f);
            sky.transform.localScale = scale;
            sky.transform.position = new Vector3(PixelScreen.Snap(c.x), PixelScreen.Snap(c.y), 0f);

            if (moodT < 1f)
            {
                moodT = Mathf.Min(1f, moodT + Time.deltaTime / moodBlend);
                mood = Color.Lerp(moodFrom, moodTo, moodT * moodT * (3f - 2f * moodT));
            }
            sky.color = mood;
            guardian.sprite = current.guardian;
            guardian.color = mood;
            guardian.transform.localScale = scale;
            float half = cam.orthographicSize * cam.aspect;
            float halfH = cam.orthographicSize;
            // sol suivi par le décor : le plus bas de la vue, lissé (en partie seulement ; menus : décor collé à l'écran)
            float lift = GroundLift(c, half, halfH, k);
            Vector2 go = current.guardianOffset * (k / PixelScreen.Ppu);
            float sink = guardianSink * k / PixelScreen.Ppu;
            guardian.transform.position = new Vector3(PixelScreen.Snap(c.x + go.x), PixelScreen.Snap(c.y + go.y - sink + lift * Follow(0)), 0f);
            float fogH = halfH * 2f + 1f;
            // le sprite fait 1 × 8 px à 8 px/u (0,125 × 1 u) : la largeur se divise par celle du sprite, sinon la brume
            // ne couvre qu'une colonne au centre de l'écran
            Vector2 fogSize = fogLayer.sprite != null ? (Vector2)fogLayer.sprite.bounds.size : Vector2.one;
            fogLayer.transform.localScale = new Vector3((fogH * cam.aspect + 1f) / fogSize.x, fogH / fogSize.y, 1f);
            fogLayer.transform.position = new Vector3(c.x, c.y, 0f);
            if (ground.enabled && ground.sprite != null)
            {
                // sol des menus : toute la largeur de la vue, motif accroché au monde (il défile avec le décor), terre dessous
                float tile = ground.sprite.bounds.size.x;
                float w = Mathf.Ceil((half * 2f + 2f) / tile) * tile;
                float gx = Mathf.Floor(c.x / tile) * tile;
                ground.size = new Vector2(w, 0.5f);
                ground.transform.position = new Vector3(gx, -0.25f, 0f);
                if (groundFill.enabled)
                {
                    groundFill.size = new Vector2(w, GroundFillDepth);
                    groundFill.transform.position = new Vector3(gx, -0.5f - GroundFillDepth * 0.5f, 0f);
                }
            }
            for (int i = 0; i < 3; i++)
            {
                var strip = layers[i][0].sprite;
                if (strip == null) continue;
                float width = strip.bounds.size.x * k;
                float height = strip.bounds.size.y * k;
                float offset = Mathf.Repeat(c.x * parallax[i], width);
                float left = c.x - half - offset;
                float y = PixelScreen.Snap(c.y + lift * Follow(i));
                for (int j = 0; j < 2; j++)
                {
                    layers[i][j].transform.localScale = scale;
                    layers[i][j].color = mood;
                    layers[i][j].transform.position = new Vector3(PixelScreen.Snap(left + width * (j + 0.5f)), y, 0f);
                }
                // pied : dernière rangée du calque, étirée de son bas jusque sous la vue
                var foot = feet[i];
                float bottom = y - height * 0.5f;
                float rows = Mathf.Max(0f, Mathf.Ceil((bottom - (c.y - halfH) + 1f) * PixelScreen.Ppu / k));
                foot.enabled = foot.sprite != null && rows > 0f;
                foot.color = mood;
                foot.transform.localScale = new Vector3(k * Mathf.Ceil((half * 2f + 2f) / (foot.sprite != null ? foot.sprite.bounds.size.x : 1f)), k * rows, 1f);
                foot.transform.position = new Vector3(PixelScreen.Snap(c.x), bottom, 0f);
            }
        }

        const float LayerHalfPx = 180f;                     // calques de 1280 × 360 px

        float Follow(int layer) => groundFollow != null && layer < groundFollow.Length ? groundFollow[layer] : 0f;

        /// <summary>
        /// Décalage vertical (u) à appliquer au décor pour poser sa ligne de sol sur le sol le plus bas de la vue (lissé),
        /// par rapport à sa place collée à l'écran. 0 hors partie ou sans terrain.
        /// </summary>
        float GroundLift(Vector3 c, float half, float halfH, int k)
        {
            var run = RunManager.Instance;
            var probe = CameraRig.Instance != null ? CameraRig.Instance.HeightProbe : null;
            if (run == null || !run.IsRunning || probe == null)
            {
                groundValid = false;
                return 0f;
            }
            float low = float.MaxValue;
            for (int s = 0; s <= 8; s++)
                low = Mathf.Min(low, probe(c.x - half + half * 2f * s / 8f));
            if (!groundValid || Mathf.Abs(low - groundSmooth) > halfH * 3f)
            {
                groundSmooth = low;                         // début de partie ou grand saut : on se pose sans glisser
                groundVel = 0f;
                groundValid = true;
            }
            else groundSmooth = Mathf.SmoothDamp(groundSmooth, low, ref groundVel, groundSmoothTime, Mathf.Infinity, Time.deltaTime);
            // ligne de sol d'un calque collé à l'écran (centré sur la caméra, 360 px de haut × k)
            float lockedGround = c.y + k * (layerGroundPx - LayerHalfPx) / PixelScreen.Ppu;
            // jamais au-dessus des 6/10 de la vue : le décor ne doit pas remplir l'écran de terre
            float target = Mathf.Min(groundSmooth, c.y + halfH * 0.2f);
            return target - lockedGround;
        }
    }
}
