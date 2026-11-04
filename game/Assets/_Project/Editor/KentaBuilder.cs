using System;
using System.IO;
using System.Linq;
using System.Text;
using Kenta.Core;
using Kenta.FX;
using Kenta.Level;
using Kenta.Player;
using Kenta.Rhythm;
using Kenta.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.EditorTools
{
    /// <summary>
    /// Reconstruit de façon reproductible tout ce qui dépend des sprites de Art/Kenta : police pixel, héros, 8 biomes, thème,
    /// prefabs des écrans et câblage de la scène Main. À relancer après avoir régénéré les sprites
    /// (tools/gen-kenta-assets.py) : menu « Kenta > Reconstruire thèmes, prefabs et scène ».
    /// </summary>
    public static class KentaBuilder
    {
        const string Art = "Assets/_Project/Art/Kenta/";
        const string SoDir = "Assets/_Project/ScriptableObjects/";
        const string PrefabDir = "Assets/_Project/Prefabs/Screens/";

        static readonly string[] BiomeIds = { "sylve", "bayou", "echos", "forge", "echine", "givre", "cite", "orage" };
        static readonly string[] Labels = { "LA SYLVE", "LE BAYOU", "LES ÉCHOS", "LA FORGE", "L'ÉCHINE", "LE GIVRE", "LA CITÉ SOURDE", "LE GRAND ORAGE" };
        static readonly string[] Sections = { "INTRO", "COUPLET A", "COUPLET B", "REFRAIN", "BREAK", "COUPLET C", "PRÉ-FINAL", "FINAL" };
        static readonly string[] Guardians = { "CERF DE PIERRE", "GRENOUILLE-LUNE", "CHAUVE-SOURIS DE CRISTAL", "TAUREAU-ENCLUME", "CHAT-MONTAGNE", "RENARD DE GLACE", "LAPIN DE GRÈS", "OISEAU-TONNERRE" };
        static readonly string[] Enemies = { "djembe", "crapaud", "chauve", "crabe", "golem", "morse", "gargouille", "nuage" };
        static readonly string[] Sparks = { "#FF6A82", "#FFD2A0", "#4FC8C0", "#FFA040", "#FFD060", "#FFFFFF", "#EDB88A", "#E0B040" };
        static readonly string[] Flashes = { "#FFFFFF", "#FFE6D0", "#CFE8FF", "#FFD8A8", "#FFFFFF", "#FFFFFF", "#FFF2DC", "#FFFFFF" };

        static readonly Color Gold = new(1f, 0.82f, 0.23f);
        static readonly Color Dim = new(0.6f, 0.58f, 0.75f);

        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        // ------------------------------------------------------------------ utilitaires
        static Sprite Spr(string rel)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(Art + rel);
            if (s == null) throw new Exception($"Sprite introuvable : {Art + rel} (lancer tools/gen-kenta-assets.py et tools/extract-kenta-pdf.py)");
            return s;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        static T Asset<T>(string path) where T : ScriptableObject
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null)
            {
                a = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(a, path);
            }
            return a;
        }

        static void Set(UnityEngine.Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) throw new Exception($"Champ introuvable : {target.GetType().Name}.{field}");
            switch (value)
            {
                case null: p.objectReferenceValue = null; break;
                case UnityEngine.Object o: p.objectReferenceValue = o; break;
                case int i: p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case bool b: p.boolValue = b; break;
                case string s: p.stringValue = s; break;
                case Color c: p.colorValue = c; break;
                case Vector2 v: p.vector2Value = v; break;
                case Enum e: p.enumValueIndex = Convert.ToInt32(e); break;
                default: throw new Exception("Type non géré : " + value.GetType());
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(UnityEngine.Object target, string field, UnityEngine.Object[] items)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ assets
        static PixelFontData BuildFont()
        {
            var font = Asset<PixelFontData>(SoDir + "PixelFont.asset");
            var files = Directory.GetFiles(Art + "Gen/font", "glyph_*.png").OrderBy(f => f).ToArray();
            var sb = new StringBuilder();
            var sprites = new Sprite[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                string hex = Path.GetFileNameWithoutExtension(files[i]).Substring(6);
                sb.Append((char)Convert.ToInt32(hex, 16));
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(files[i].Replace('\\', '/'));
            }
            font.chars = sb.ToString();
            font.sprites = sprites;
            font.plate = Spr("Gen/ui/plate.png");
            EditorUtility.SetDirty(font);
            return font;
        }

        static HeroProfile[] BuildHeroes()
        {
            string[] ids = { "kenta", "aiko", "ryo" };
            string[] names = { "KENTA", "AIKO", "RYO" };
            string[] roles = { "LE BATTEUR", "LA FLÛTISTE", "LE BASSISTE" };
            string[] accents = { "#FF6A30", "#8A5AE0", "#E03A3A" };
            var result = new HeroProfile[3];
            for (int i = 0; i < 3; i++)
            {
                var set = Asset<HeroSpriteSet>(SoDir + $"Heroes/HeroSprites_{names[i]}.asset");
                string n = ids[i];
                set.idle = Spr($"Pdf/hero_{n}_idle.png");
                set.run = Spr($"Pdf/hero_{n}_run.png");
                set.runCycle = Enumerable.Range(0, 6).Select(k => Spr($"Gen/heroes/hero_{n}_run_{k}.png")).ToArray();
                set.jump = Spr($"Pdf/hero_{n}_jump.png");
                set.slide = Spr($"Pdf/hero_{n}_slide.png");
                set.hit = Spr($"Pdf/hero_{n}_hit.png");
                set.vine = Spr($"Pdf/hero_{n}_vine.png");
                set.wall = Spr($"Pdf/hero_{n}_wall.png");
                set.hurt = Spr($"Pdf/hero_{n}_hurt.png");
                set.death = Spr($"Pdf/hero_{n}_death.png");
                EditorUtility.SetDirty(set);
                var p = Asset<HeroProfile>(SoDir + $"Heroes/Hero_{names[i]}.asset");
                p.displayName = names[i];
                p.role = roles[i];
                p.sprites = set;
                p.accent = Hex(accents[i]);
                EditorUtility.SetDirty(p);
                result[i] = p;
            }
            return result;
        }

        static BiomeTheme[] BuildBiomes()
        {
            var result = new BiomeTheme[8];
            for (int i = 0; i < 8; i++)
            {
                string id = BiomeIds[i];
                var b = Asset<BiomeTheme>(SoDir + $"Biomes/Biome_{i + 1:00}_{id}.asset");
                b.label = Labels[i];
                b.sectionName = Sections[i];
                b.guardianName = Guardians[i];
                b.sky = Spr($"Gen/biomes/bg_{id}_sky.png");
                b.far = Spr($"Gen/biomes/bg_{id}_far.png");
                b.mid = Spr($"Gen/biomes/bg_{id}_mid.png");
                b.near = Spr($"Gen/biomes/bg_{id}_near.png");
                b.ground = Spr($"Gen/biomes/ground_{id}.png");
                b.hazard = Spr($"Gen/biomes/hazard_{id}.png");
                b.wall = Spr($"Gen/biomes/wall_{id}.png");
                b.block = Spr($"Gen/biomes/block_{id}.png");
                b.ceiling = Spr($"Gen/biomes/ceil_{id}.png");
                b.post = Spr($"Gen/biomes/post_{id}.png");
                b.fill = Spr($"Gen/biomes/fill_{id}.png");
                b.cliff = Spr($"Gen/biomes/cliff_{id}.png");
                b.platform = Spr($"Gen/biomes/platform_{id}.png");
                if (Enemies[i] == "djembe")
                {
                    b.enemyFrames = new[] { Spr("Pdf/enemy_djembe_squash.png"), Spr("Pdf/enemy_djembe_stretch.png") };
                    b.enemyBroken = Spr("Pdf/enemy_djembe_broken.png");
                }
                else
                {
                    b.enemyFrames = new[] { Spr($"Pdf/enemy_{Enemies[i]}.png") };
                    b.enemyBroken = null;
                }
                b.spark = Hex(Sparks[i]);
                b.flash = Hex(Flashes[i]);
                EditorUtility.SetDirty(b);
                result[i] = b;
            }
            return result;
        }

        static LevelTheme BuildTheme(BiomeTheme[] biomes, HeroProfile[] heroes)
        {
            var t = Asset<LevelTheme>(SoDir + "Theme_Kenta.asset");
            t.biomes = biomes;
            t.heroes = heroes;
            t.coinFrames = Enumerable.Range(0, 4).Select(i => Spr($"Gen/world/coin_{i}.png")).ToArray();
            t.totem = Spr("Gen/world/totem.png");
            t.pixel = Spr("Gen/world/px.png");
            t.sourdine = Spr("Gen/ui/sourdine.png");
            t.sourdineVeil = Spr("Gen/ui/veil.png");
            EditorUtility.SetDirty(t);
            return t;
        }

        // ------------------------------------------------------------------ interface
        static RectTransform NewRect(string name, Transform parent, Vector2 pos, Vector2? size = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size ?? Vector2.zero;
            return rt;
        }

        static Image AddImage(Transform parent, string name, Sprite sprite, Vector2 pos, Color? color = null, Vector2? size = null,
                              bool native = true, Vector2? pivot = null)
        {
            var rt = NewRect(name, parent, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color ?? Color.white;
            img.raycastTarget = false;
            if (pivot.HasValue) rt.pivot = pivot.Value;
            if (native && size == null && sprite != null) rt.sizeDelta = sprite.rect.size;
            return img;
        }

        static PixelText AddText(Transform parent, PixelFontData font, string name, string text, Vector2 pos, Color color,
                                 int scale = 1, PixelText.Align align = PixelText.Align.Center, bool plate = false)
        {
            var rt = NewRect(name, parent, pos, new Vector2(2, 2));
            var pt = rt.gameObject.AddComponent<PixelText>();
            Set(pt, "font", font);
            Set(pt, "text", text);
            Set(pt, "color", color);
            Set(pt, "align", align);
            Set(pt, "scale", scale);
            Set(pt, "plate", plate);
            return pt;
        }

        static GameObject SavePrefab(GameObject root, string name)
        {
            string path = PrefabDir + name + ".prefab";
            Directory.CreateDirectory(PrefabDir);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject ScreenRoot(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(PixelScreen.Width, PixelScreen.Height);
            return go;
        }

        static GameObject BuildAttract(PixelFontData font, LevelTheme theme)
        {
            var root = ScreenRoot("Screen_Attract");
            var tr = root.transform;
            var hi = AddText(tr, font, "HiScore", "HI-SCORE 000000", new Vector2(0, 76), Gold, 1, PixelText.Align.Center, true);
            var logo = AddImage(tr, "Logo", Spr("Gen/ui/logo.png"), new Vector2(0, 26));
            AddText(tr, font, "Subtitle", "LA COURSE EN RYTHME", new Vector2(0, -2), Color.white, 1, PixelText.Align.Center, true);
            var press = AddText(tr, font, "PressStart", "APPUIE SUR START", new Vector2(0, -46), Gold, 2, PixelText.Align.Center, true);
            var comp = root.AddComponent<AttractScreen>();
            Set(comp, "pressStart", press);
            Set(comp, "hiScore", hi);
            Set(comp, "logo", (RectTransform)logo.transform);
            Set(comp, "logoY", 26f);
            return SavePrefab(root, "Screen_Attract");
        }

        static GameObject BuildExplain(PixelFontData font, LevelTheme theme)
        {
            var root = ScreenRoot("Screen_Explain");
            var tr = root.transform;
            var panel = AddImage(tr, "Panel", Spr("Gen/ui/panel.png"), new Vector2(14, -6), null, new Vector2(232, 118), false);
            panel.type = Image.Type.Sliced;
            AddText(tr, font, "Title", "COMMENT JOUER", new Vector2(0, 70), Gold, 2, PixelText.Align.Center, true);
            var hero = AddImage(tr, "Hero", theme.heroes[0].sprites.run, new Vector2(-122, -62), null, null, true, new Vector2(0.5f, 0f));
            string[] lines = { "SAUTE : TROUS, MURS, MARCHES", "FRAPPE : LES INSTRUMENTS", "GLISSE : SOUS LA BRANCHE", "RAMASSE LES PIÈCES", "SUIS LE RYTHME : PARFAIT !", "UN SEUL COUP ET C'EST FINI" };
            var texts = new PixelText[lines.Length];
            for (int i = 0; i < lines.Length; i++)
                texts[i] = AddText(tr, font, $"Line{i}", lines[i], new Vector2(-62, 38 - i * 16), Color.white, 1, PixelText.Align.Left);
            AddImage(tr, "Btn1", Spr("Gen/ui/button_red.png"), new Vector2(-76, 38));
            AddText(tr, font, "Btn1Label", "1", new Vector2(-76, 38), Color.white);
            AddImage(tr, "Btn2", Spr("Gen/ui/button_blue.png"), new Vector2(-76, 22));
            AddText(tr, font, "Btn2Label", "2", new Vector2(-76, 22), Color.white);
            AddImage(tr, "Stick", Spr("Gen/ui/stick.png"), new Vector2(-76, 6));
            AddImage(tr, "Coin", theme.coinFrames[0], new Vector2(-76, -10));
            AddText(tr, font, "Hint", "BOUTON 1 POUR CONTINUER", new Vector2(0, -80), Color.white, 1, PixelText.Align.Center, true);
            var comp = root.AddComponent<ExplainScreen>();
            Set(comp, "theme", theme);
            Set(comp, "hero", hero);
            var so = new SerializedObject(comp);
            var p = so.FindProperty("lines");
            p.arraySize = texts.Length;
            for (int i = 0; i < texts.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = texts[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return SavePrefab(root, "Screen_Explain");
        }

        static GameObject BuildHeroSelect(PixelFontData font, LevelTheme theme)
        {
            var root = ScreenRoot("Screen_HeroSelect");
            var tr = root.transform;
            AddText(tr, font, "Title", "CHOISIS TON HÉROS", new Vector2(0, 72), Gold, 2, PixelText.Align.Center, true);
            var timer = AddText(tr, font, "Timer", "12", new Vector2(146, 72), Color.white, 1, PixelText.Align.Center, true);
            var portraits = new Image[3];
            var frames = new Image[3];
            var names = new PixelText[3];
            for (int i = 0; i < 3; i++)
            {
                float x = -96 + i * 96;
                frames[i] = AddImage(tr, $"Frame{i}", Spr("Gen/ui/panel.png"), new Vector2(x, -2), Dim, new Vector2(84, 108), false);
                frames[i].type = Image.Type.Sliced;
                portraits[i] = AddImage(tr, $"Portrait{i}", theme.heroes[i].sprites.idle, new Vector2(x, -46), null, null, true, new Vector2(0.5f, 0f));
                names[i] = AddText(tr, font, $"Name{i}", theme.heroes[i].displayName, new Vector2(x, -66), Color.white, 1, PixelText.Align.Center, true);
            }
            AddText(tr, font, "Hint", "◄ ► CHOISIR    1 VALIDER", new Vector2(0, -82), Color.white, 1, PixelText.Align.Center, true);
            var comp = root.AddComponent<HeroSelectScreen>();
            Set(comp, "theme", theme);
            Set(comp, "timerText", timer);
            SetArray(comp, "portraits", portraits);
            SetArray(comp, "frames", frames);
            SetArray(comp, "names", names);
            return SavePrefab(root, "Screen_HeroSelect");
        }

        static GameObject BuildHud(PixelFontData font, LevelTheme theme)
        {
            var root = ScreenRoot("Screen_Playing");
            var tr = root.transform;
            var score = AddText(tr, font, "Score", "000000", new Vector2(-154, 80), Color.white, 1, PixelText.Align.Left, true);
            var mult = AddText(tr, font, "Multiplier", "×1", new Vector2(-154, 62), Gold, 2, PixelText.Align.Left, true);
            AddImage(tr, "CoinIcon", theme.coinFrames[0], new Vector2(96, 80));     // « 252/252 » commence vers x = 117
            var coins = AddText(tr, font, "Coins", "0/0", new Vector2(154, 80), Color.white, 1, PixelText.Align.Right, true);
            var friezeBack = new Image[8];
            var friezeFill = new Image[8];
            for (int i = 0; i < 8; i++)
            {
                float x = -72 + i * 18 + 8;
                var back = AddImage(tr, $"Seg{i}", Spr("Gen/ui/pixel.png"), new Vector2(x, 82), new Color(0.18f, 0.15f, 0.28f), new Vector2(16, 3), false);
                var fill = AddImage(back.transform, "Fill", Spr("Gen/ui/pixel.png"), Vector2.zero, Gold, null, false);
                var frt = (RectTransform)fill.transform;
                frt.anchorMin = new Vector2(0, 0);
                frt.anchorMax = new Vector2(1, 1);       // RunHud pilote anchorMax.x (0 → 1) pour remplir le segment
                frt.pivot = new Vector2(0, 0.5f);
                frt.offsetMin = frt.offsetMax = Vector2.zero;
                friezeBack[i] = back;
                friezeFill[i] = fill;
            }
            var biome = AddText(tr, font, "BiomeLabel", "INTRO · LA SYLVE", new Vector2(0, 70), Color.white, 1, PixelText.Align.Center, true);
            var banner = AddText(tr, font, "Banner", "", new Vector2(0, 24), Color.white, 2, PixelText.Align.Center, true);
            var feedback = AddText(tr, font, "Feedback", "", new Vector2(0, 4), Gold, 1, PixelText.Align.Center, true);
            var judgeLabel = AddText(tr, font, "Judge", "", Vector2.zero, Gold, 1, PixelText.Align.Center, true);
            var popup = root.AddComponent<JudgePopup>();
            Set(popup, "label", judgeLabel);
            var comp = root.AddComponent<RunHud>();
            Set(comp, "scoreText", score);
            Set(comp, "multiplierText", mult);
            Set(comp, "coinsText", coins);
            Set(comp, "biomeText", biome);
            Set(comp, "bannerText", banner);
            Set(comp, "feedbackText", feedback);
            SetArray(comp, "friezeBack", friezeBack);
            SetArray(comp, "friezeFill", friezeFill);
            return SavePrefab(root, "Screen_Playing");
        }

        static GameObject BuildGameOver(PixelFontData font)
        {
            var root = ScreenRoot("Screen_GameOver");
            var tr = root.transform;
            var titleT = AddText(tr, font, "Title", "GAME OVER", new Vector2(0, 48), Color.white, 3, PixelText.Align.Center, true);
            var grade = AddText(tr, font, "Grade", "S+", new Vector2(-98, -20), Gold, 5, PixelText.Align.Center, true);
            var panel = NewRect("Panel", tr, new Vector2(30, -20), new Vector2(196, 76));
            var bg = panel.gameObject.AddComponent<Image>();
            bg.sprite = Spr("Gen/ui/panel.png");
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;
            var score = AddText(panel, font, "Score", "SCORE 000000", new Vector2(0, 24), Gold, 1);
            var judge = AddText(panel, font, "Judge", "PARFAIT 0  BIEN 0  BOF 0", new Vector2(0, 8), Color.white);
            var coins = AddText(panel, font, "Coins", "PIÈCES 0/0", new Vector2(0, -8), Color.white);
            var biome = AddText(panel, font, "Biome", "MONDE LA SYLVE", new Vector2(0, -24), Dim);
            var comp = root.AddComponent<GameOverScreen>();
            Set(comp, "title", (RectTransform)titleT.transform);
            Set(comp, "titleText", titleT);
            Set(comp, "panel", panel.gameObject);
            Set(comp, "scoreText", score);
            Set(comp, "coinsText", coins);
            Set(comp, "biomeText", biome);
            Set(comp, "gradeText", grade);
            Set(comp, "judgeText", judge);
            return SavePrefab(root, "Screen_GameOver");
        }

        // ------------------------------------------------------------------ scène
        static void Destroy(string name)
        {
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(g => g.name == name && g.transform.parent == null).ToArray())
                UnityEngine.Object.DestroyImmediate(go);
        }

        static T Child<T>(Transform parent, string name) where T : Component
        {
            var t = parent.Find(name);
            return t != null ? t.GetComponent<T>() : null;
        }

        static Camera NewLayerCamera(string name, Transform parent, int mask, int depth)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var c = go.AddComponent<Camera>();
            c.orthographic = true;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0f, 0f, 0f, 0f);
            c.cullingMask = mask;
            c.depth = depth;
            c.allowHDR = false;
            c.allowMSAA = false;
            c.nearClipPlane = 0.1f;
            c.farClipPlane = 100f;
            return c;
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
        }

        /// <summary>Un niveau par biome ; pour l'instant tous sur le même morceau et la même chart (le thème change).</summary>
        static LevelCatalog BuildLevels(BiomeTheme[] biomes)
        {
            int[] stars = { 1, 1, 2, 3, 3, 4, 4, 5 };
            var song = AssetDatabase.LoadAssetAtPath<SongData>(SoDir + "Songs/Song_Level01.asset");
            EnsureFolder(SoDir + "Levels");
            var levels = new LevelDef[biomes.Length];
            for (int i = 0; i < biomes.Length; i++)
            {
                var l = Asset<LevelDef>(SoDir + $"Levels/Level_{i + 1:00}_{BiomeIds[i]}.asset");
                l.id = BiomeIds[i];
                l.number = i + 1;
                l.biome = biomes[i];
                l.song = song;
                l.chart = null;                        // celle du morceau
                l.stars = stars[Mathf.Min(i, stars.Length - 1)];
                EditorUtility.SetDirty(l);
                levels[i] = l;
            }
            var catalog = Asset<LevelCatalog>(SoDir + "LevelCatalog.asset");
            catalog.levels = levels;
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        static string BuildScene(LevelTheme theme, PixelFontData font, GameObject[] prefabs, LevelCatalog catalog)
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/_Project/Scenes/Main.unity")
                scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");

            foreach (var n in new[] { "Screens", "Backdrop", "PixelScreen", "PixelUI", "PixelDisplay", "Bandana", "PixelUICamera" }) Destroy(n);

            int actorsLayer = LayerMask.NameToLayer("Actors");
            int uiLayer = LayerMask.NameToLayer("UI");
            if (actorsLayer < 0) throw new System.Exception("Couche « Actors » absente (Project Settings > Tags and Layers).");

            var cam = Camera.main;
            if (cam.GetComponent<CameraRig>() == null) cam.gameObject.AddComponent<CameraRig>();
            cam.cullingMask = ~((1 << actorsLayer) | (1 << uiLayer));
            cam.depth = 0;
            for (int i = cam.transform.childCount - 1; i >= 0; i--)
                if (cam.transform.GetChild(i).name == "ActorCamera") UnityEngine.Object.DestroyImmediate(cam.transform.GetChild(i).gameObject);
            var actorCam = NewLayerCamera("ActorCamera", cam.transform, 1 << actorsLayer, 1);
            var uiCam = NewLayerCamera("PixelUICamera", null, 1 << uiLayer, 2);
            uiCam.transform.position = new Vector3(0f, -500f, -10f);

            // affichage : 3 calques 320×180 agrandis d'un facteur entier, dans un cadre masqué
            var displayMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/PixelDisplay.mat");
            if (displayMat == null)
            {
                EnsureFolder("Assets/_Project/Materials");
                displayMat = new Material(Shader.Find("Kenta/PixelDisplay"));
                AssetDatabase.CreateAsset(displayMat, "Assets/_Project/Materials/PixelDisplay.mat");
            }
            var psGo = new GameObject("PixelScreen");
            var ps = psGo.AddComponent<PixelScreen>();
            var dispGo = new GameObject("PixelDisplay", typeof(Canvas), typeof(CanvasScaler));
            var dc = dispGo.GetComponent<Canvas>();
            dc.renderMode = RenderMode.ScreenSpaceOverlay;
            dc.sortingOrder = -100;
            dc.pixelPerfect = true;
            var frame = NewRect("Frame", dispGo.transform, Vector2.zero);
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.gameObject.AddComponent<RectMask2D>();
            PixelLayer Layer(string n, Camera c, bool fx, bool fy, int margin)
            {
                var go = new GameObject(n, typeof(RectTransform), typeof(RawImage), typeof(PixelLayer));
                go.transform.SetParent(frame, false);
                var raw = go.GetComponent<RawImage>();
                raw.material = displayMat;
                raw.raycastTarget = false;
                var l = go.GetComponent<PixelLayer>();
                Set(l, "layerCamera", c);
                Set(l, "followX", fx);
                Set(l, "followY", fy);
                Set(l, "margin", margin);
                return l;
            }
            Set(ps, "world", Layer("World", cam, true, true, 1));
            Set(ps, "actors", Layer("Actors", actorCam, false, true, 1));
            Set(ps, "ui", Layer("UI", uiCam, false, false, 0));
            Set(ps, "frame", frame);

            // interface en pixels, rendue par sa propre caméra (fixe) dans son calque
            var uiGo = new GameObject("PixelUI", typeof(Canvas), typeof(CanvasScaler));
            uiGo.layer = uiLayer;
            var canvas = uiGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCam;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 0;
            canvas.pixelPerfect = true;               // les éléments d'interface tombent toujours sur des pixels entiers
            var scaler = uiGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.referencePixelsPerUnit = 100f;
            Set(ps, "uiCanvas", canvas);

            // la Sourdine est sous les écrans
            var sourdineRoot = NewRect("Sourdine", uiGo.transform, Vector2.zero);
            sourdineRoot.anchorMin = Vector2.zero;
            sourdineRoot.anchorMax = Vector2.one;
            sourdineRoot.offsetMin = sourdineRoot.offsetMax = Vector2.zero;
            var body = AddImage(sourdineRoot, "Body", theme.sourdine, Vector2.zero, null, null, true, new Vector2(0f, 0.5f));
            var veil = AddImage(sourdineRoot, "Veil", theme.sourdineVeil, Vector2.zero, null, null, true, new Vector2(0f, 0.5f));
            foreach (var im in new[] { body, veil })
            {
                var r = (RectTransform)im.transform;
                r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
            }
            var fill = AddImage(sourdineRoot, "VeilFill", Spr("Gen/ui/pixel.png"), Vector2.zero, new Color(92f / 255f, 98f / 255f, 108f / 255f, 150f / 255f), new Vector2(0, PixelScreen.Height), false, new Vector2(0f, 0.5f));
            var fillRt = (RectTransform)fill.transform;
            fillRt.anchorMin = fillRt.anchorMax = new Vector2(0f, 0.5f);
            var sourdine = sourdineRoot.gameObject.AddComponent<Sourdine>();
            Set(sourdine, "body", (RectTransform)body.transform);
            Set(sourdine, "veil", (RectTransform)veil.transform);
            Set(sourdine, "veilFill", fillRt);

            var screens = prefabs.Select(p => (GameObject)PrefabUtility.InstantiatePrefab(p, uiGo.transform)).ToArray();
            foreach (var s in screens) { var r = (RectTransform)s.transform; r.anchoredPosition = Vector2.zero; s.SetActive(false); }
            GameObject attract = screens[0], explain = screens[1], hero = screens[2], hud = screens[3], over = screens[4];

            var flashRt = NewRect("ScreenFx", uiGo.transform, Vector2.zero);
            flashRt.anchorMin = Vector2.zero;
            flashRt.anchorMax = Vector2.one;
            flashRt.offsetMin = flashRt.offsetMax = Vector2.zero;
            var flashImg = flashRt.gameObject.AddComponent<Image>();
            flashImg.sprite = Spr("Gen/ui/pixel.png");
            flashImg.color = new Color(1, 1, 1, 0);
            flashImg.raycastTarget = false;
            flashImg.enabled = false;
            var fx = flashRt.gameObject.AddComponent<ScreenFx>();
            Set(fx, "overlay", flashImg);

            // monde
            var world = GameObject.Find("World");
            var level = world.transform.Find("Level").GetComponent<LevelBuilder>();
            Set(level, "theme", theme);

            var playerT = world.transform.Find("Player");
            for (int i = playerT.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(playerT.GetChild(i).gameObject);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(playerT, false);
            var spinner = new GameObject("Spinner").transform;
            spinner.SetParent(visual, false);
            var spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(spinner, false);
            var sr = spriteGo.AddComponent<SpriteRenderer>();
            sr.sprite = theme.heroes[0].sprites.idle;
            sr.sortingOrder = 20;
            var player = playerT.GetComponent<PlayerController>();
            var animator = visual.gameObject.AddComponent<HeroAnimator>();
            Set(animator, "player", player);
            Set(animator, "target", sr);
            Set(animator, "sprites", theme.heroes[0].sprites);

            var bandanaGo = new GameObject("Bandana");
            bandanaGo.transform.SetParent(world.transform, false);
            var bandana = bandanaGo.AddComponent<BandanaTrail>();
            Set(bandana, "target", playerT);
            Set(bandana, "pixel", theme.pixel);
            Set(bandana, "color", theme.heroes[0].accent);

            SetLayerRecursive(visual.gameObject, actorsLayer);
            bandanaGo.layer = actorsLayer;
            var rig = cam.GetComponent<CameraRig>();
            Set(rig, "actorVisual", visual);
            Set(rig, "offsetX", 2.6f);          // héros à 56 px du bord gauche (sur 320)
            Set(rig, "baseY", 1.05f);           // sol de référence à 48 px du bas
            Set(player, "visual", visual);
            Set(player, "spinner", spinner);
            Set(player, "slash", null);
            Set(player, "trail", bandana);
            Set(player, "burstColor", theme.heroes[0].accent);
            Set(player, "squashAmount", 0f);
            Set(player, "spinOnJump", false);
            Set(player, "width", 0.5f);
            Set(player, "standHeight", 1.0f);
            Set(player, "slideHeight", 0.5f);
            Set(player, "jumpHeight", 1.5f);

            var backdropGo = new GameObject("Backdrop");
            var backdrop = backdropGo.AddComponent<BiomeBackdrop>();
            Set(backdrop, "cam", cam);

            var fxPool = UnityEngine.Object.FindFirstObjectByType<FxPool>();
            Set(fxPool, "square", theme.pixel);
            Set(fxPool, "additive", null);
            Set(fxPool, "sizeMultiplier", 0.4f);

            var run = UnityEngine.Object.FindFirstObjectByType<RunManager>();
            Set(run, "level", level);
            Set(run, "player", player);
            Set(run, "world", world);
            Set(run, "heroAnimator", animator);
            Set(run, "bandana", bandana);
            Set(run, "backdrop", backdrop);

            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
            Set(flow, "attractScreen", attract);
            Set(flow, "explainScreen", explain);
            Set(flow, "configScreen", hero);
            Set(flow, "playingScreen", hud);
            Set(flow, "gameOverScreen", over);
            Set(flow, "heroSelect", hero.GetComponent<HeroSelectScreen>());
            Set(flow, "theme", theme);
            Set(flow, "backdrop", backdrop);
            Set(flow, "sourdine", sourdine);
            Set(flow, "gameOverDuration", 6f);
            Set(flow, "catalog", catalog);          // titre, note puis panneau : le temps de lire
            Set(flow, "attractSong", null);

            cam.backgroundColor = Color.black;
            SetLayerRecursive(uiGo, uiLayer);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "scène câblée";
        }

        // ------------------------------------------------------------------ point d'entrée
        [MenuItem("Kenta/Reconstruire thèmes, prefabs et scène")]
        public static void BuildMenu() => Debug.Log(BuildAll());

        public static string BuildAll()
        {
            AssetDatabase.Refresh();
            var font = BuildFont();
            var heroes = BuildHeroes();
            var biomes = BuildBiomes();
            var theme = BuildTheme(biomes, heroes);
            var catalog = BuildLevels(biomes);
            AssetDatabase.SaveAssets();
            var prefabs = new[]
            {
                BuildAttract(font, theme), BuildExplain(font, theme), BuildHeroSelect(font, theme), BuildHud(font, theme), BuildGameOver(font),
            };
            AssetDatabase.SaveAssets();
            string result = BuildScene(theme, font, prefabs, catalog);
            AssetDatabase.SaveAssets();
            return $"{result} ; {catalog.levels.Length} niveaux, {biomes.Length} biomes, {heroes.Length} héros, {font.sprites.Length} glyphes";
        }
    }
}
