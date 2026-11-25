using Kenta.Level;
using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Décor du biome courant : un ciel fixe (640×360), le gardien (sprite à part, toujours endormi : il ne bouge pas) et trois calques
    /// de 1280×360 en parallaxe qui bouclent sans couture. Dessinés à la définition de la vue large : leurs pixels ont la
    /// même taille que ceux du niveau. Tout est placé sur la grille de pixels. SetBiome change de monde.
    /// Ambiance par section (SetSection) : teinte et luminosité qui glissent, pour que le décor change au fil du morceau.
    /// </summary>
    public class BiomeBackdrop : MonoBehaviour
    {
        [SerializeField] Camera cam;
        [Tooltip("Vitesse relative des calques (0 = fixe à l'écran, 1 = défile comme le niveau) : loin, milieu, proche.")]
        [SerializeField] float[] parallax = { 0.12f, 0.35f, 0.7f };
        [SerializeField] int firstSortingOrder = -100;
        [Tooltip("Brume entre le décor et le niveau : éclaircit le fond pour que ce qu'on touche ressorte.")]
        [SerializeField] Color fog = new(0.93f, 0.96f, 1f, 0.32f);

        SpriteRenderer sky;
        SpriteRenderer guardian;
        SpriteRenderer ground;
        SpriteRenderer groundFill;
        SpriteRenderer fogLayer;
        static readonly Color GroundFillTint = new(0.58f, 0.54f, 0.7f);    // comme le flanc des piliers du niveau
        const float GroundFillDepth = 8f;
        readonly SpriteRenderer[][] layers = new SpriteRenderer[3][];
        BiomeTheme current;
        Color mood = Color.white, moodFrom = Color.white, moodTo = Color.white;
        float moodT = 1f, moodBlend = 1f;

        public BiomeTheme Current => current;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            sky = Make("Sky", firstSortingOrder);
            guardian = Make("Guardian", firstSortingOrder + 15);      // comme dans les maquettes : devant le lointain, derrière les arbres
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
            }
            fogLayer = Make("Fog", firstSortingOrder + 40);       // au-dessus des 3 calques, sous le niveau
            var tex = Texture2D.whiteTexture;
            fogLayer.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            fogLayer.color = fog;
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
                foreach (var sr in layers[i]) sr.sprite = strips[i];
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
            Vector2 go = current.guardianOffset * (k / PixelScreen.Ppu);
            guardian.transform.position = new Vector3(PixelScreen.Snap(c.x + go.x), PixelScreen.Snap(c.y + go.y), 0f);
            float fogH = cam.orthographicSize * 2f + 1f;
            fogLayer.transform.localScale = new Vector3(fogH * cam.aspect + 1f, fogH, 1f);
            fogLayer.transform.position = new Vector3(c.x, c.y, 0f);
            float half = cam.orthographicSize * cam.aspect;
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
                float offset = Mathf.Repeat(c.x * parallax[i], width);
                float left = c.x - half - offset;
                for (int j = 0; j < 2; j++)
                {
                    layers[i][j].transform.localScale = scale;
                    layers[i][j].color = mood;
                    layers[i][j].transform.position = new Vector3(PixelScreen.Snap(left + width * (j + 0.5f)), c.y, 0f);
                }
            }
        }
    }
}
