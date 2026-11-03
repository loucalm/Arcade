using UnityEditor;
using UnityEngine;

namespace Kenta.EditorTools
{
    /// <summary>
    /// Règle automatiquement l'import des sprites générés par tools/gen-neon-sprites.py
    /// (dossier Art/Neon) : PPU, pivot, bordures de 9-slice. Aucune action manuelle : régénérer les PNG suffit.
    /// Les valeurs doivent rester synchronisées avec le générateur (voir docs/art-direction.md).
    /// </summary>
    public class NeonSpritePostprocessor : AssetPostprocessor
    {
        const string Folder = "Assets/_Project/Art/Neon/";
        const float Ppu = 100f;

        struct Spec
        {
            public Vector2 pivot;       // en fraction de l'image (0,0 = bas gauche)
            public Vector4 border;      // gauche, bas, droite, haut (pixels)
            public bool sliced => border != Vector4.zero;
        }

        static Spec Center(Vector4 border = default) => new Spec { pivot = new Vector2(0.5f, 0.5f), border = border };

        static bool TryGetSpec(string name, out Spec spec)
        {
            // hero : image 224×224, hanche à x=108, pieds à y=176 (depuis le haut) → pivot aux pieds
            if (name.StartsWith("hero_")) { spec = new Spec { pivot = new Vector2(108f / 224f, 48f / 224f) }; return true; }
            // enemy : 160×160, bas de la gemme à y=142 ; block : bas de la caisse à y=140
            if (name.StartsWith("enemy_")) { spec = new Spec { pivot = new Vector2(0.5f, 18f / 160f) }; return true; }
            if (name == "block") { spec = new Spec { pivot = new Vector2(0.5f, 20f / 160f) }; return true; }
            // checkpoint : 128×512, pied du mât à (40, 496)
            if (name == "checkpoint") { spec = new Spec { pivot = new Vector2(40f / 128f, 16f / 512f) }; return true; }
            // rochers : posés sur leur base
            if (name.StartsWith("rock_")) { spec = new Spec { pivot = new Vector2(0.5f, 0f) }; return true; }
            // slash : centre de l'arc à (30, 96) sur 224×192
            if (name == "slash") { spec = new Spec { pivot = new Vector2(30f / 224f, 0.5f) }; return true; }
            // 9-slice
            if (name == "wall") { spec = Center(new Vector4(44, 44, 44, 44)); return true; }
            if (name == "laser_beam") { spec = Center(new Vector4(24, 0, 24, 0)); return true; }
            if (name == "laser_post") { spec = Center(new Vector4(0, 32, 0, 32)); return true; }
            if (name == "ground_top") { spec = Center(new Vector4(24, 0, 24, 0)); return true; }
            switch (name)
            {
                case "laser_node":
                case "lum":
                case "spark":
                case "heart_on":
                case "heart_off":
                case "logo":
                case "bg_gradient":
                    spec = Center();
                    return true;
            }
            spec = default;
            return false;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (!TryGetSpec(name, out var spec)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Ppu;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.spriteBorder = spec.border;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spriteMeshType = spec.sliced ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
            importer.SetTextureSettings(settings);
            importer.spritePivot = spec.pivot;
        }
    }
}
