using Kenta.Level;
using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Décor du biome courant : un ciel fixe (240×135, avec le gardien) et trois calques de 480×135 en parallaxe
    /// qui bouclent sans couture. Tout est placé sur la grille de pixels. SetBiome change de monde au checkpoint.
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
        SpriteRenderer ground;
        SpriteRenderer fogLayer;
        readonly SpriteRenderer[][] layers = new SpriteRenderer[3][];
        BiomeTheme current;

        public BiomeTheme Current => current;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            sky = Make("Sky", firstSortingOrder);
            ground = Make("Ground", -5);
            ground.drawMode = SpriteDrawMode.Tiled;
            ground.tileMode = SpriteTileMode.Continuous;
            ground.size = new Vector2(8f, 0.5f);
            ground.enabled = false;
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
            ground.sprite = biome.ground;
            ground.size = new Vector2(8f, 0.5f);          // le sprite remet la taille à celle d'une tuile : on la rétablit
            var strips = new[] { biome.far, biome.mid, biome.near };
            for (int i = 0; i < 3; i++)
                foreach (var sr in layers[i]) sr.sprite = strips[i];
        }

        /// <summary>Sol décoratif sous le ciel (accueil, menus) : en partie, le niveau pose son propre sol.</summary>
        public void SetGroundVisible(bool visible) => ground.enabled = visible;

        void LateUpdate()
        {
            if (cam == null || current == null) return;
            var c = cam.transform.position;
            // vue dézoomée (jusqu'à 640×360) : le décor (320×180) est agrandi d'un facteur entier pour couvrir l'écran
            int k = PixelScreen.Instance != null ? Mathf.CeilToInt(PixelScreen.Instance.ViewHeight / (float)PixelScreen.Height) : 1;
            var scale = new Vector3(k, k, 1f);
            sky.transform.localScale = scale;
            sky.transform.position = new Vector3(c.x, c.y, 0f);
            float fogH = cam.orthographicSize * 2f + 1f;
            fogLayer.transform.localScale = new Vector3(fogH * cam.aspect + 1f, fogH, 1f);
            fogLayer.transform.position = new Vector3(c.x, c.y, 0f);
            ground.transform.position = new Vector3(PixelScreen.Snap(c.x), -0.25f, 0f);
            float half = cam.orthographicSize * cam.aspect;
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
                    layers[i][j].transform.position = new Vector3(PixelScreen.Snap(left + width * (j + 0.5f)), c.y, 0f);
                }
            }
        }
    }
}
