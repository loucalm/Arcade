using System.IO;
using UnityEditor;
using UnityEngine;

namespace Kenta.EditorTools
{
    /// <summary>
    /// Règle automatiquement l'import des sprites pixel art de Art/Kenta (générés par tools/gen-kenta-assets.py
    /// ou extraits du PDF par tools/extract-kenta-pdf.py) : filtre Point, aucune compression, PPU, pivot, 9-slice.
    /// Monde : 40 px par unité (l'écran de 240×135 px montre 6 × 3,375 unités). Interface : 100 px par unité,
    /// donc un sprite fait sa taille en pixels dans le canvas de 240×135.
    /// </summary>
    public class KentaSpritePostprocessor : AssetPostprocessor
    {
        const string Root = "Assets/_Project/Art/Kenta/";
        public const float WorldPpu = 40f;
        public const float UiPpu = 100f;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            string rel = assetPath.Substring(Root.Length);
            string name = Path.GetFileNameWithoutExtension(assetPath);
            bool ui = rel.StartsWith("Gen/ui/") || rel.StartsWith("Gen/font/");

            var pivot = new Vector2(0.5f, 0.5f);
            var border = Vector4.zero;

            if (rel.StartsWith("Pdf/")) pivot = new Vector2(0.5f, 0f);                         // pieds en bas
            else if (name.StartsWith("wall_")) pivot = new Vector2(0.5f, 1f / 34f);              // 36×32 + contour d'1 px
            else if (name.StartsWith("block_")) pivot = new Vector2(0.5f, 1f / 40f);             // 38×38 + contour
            else if (name == "totem") pivot = new Vector2(0.5f, 1f / 48f);                       // 20×46 + contour
            else if (name == "panel") border = new Vector4(4, 4, 4, 4);

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ui ? UiPpu : WorldPpu;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.spriteBorder = border;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.spritePivot = pivot;
        }
    }
}
