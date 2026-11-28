using System;
using System.Collections.Generic;
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

        // 9e monde : le monde sourd du niveau 9 (la Sylve décolorée par la Sourdine, tools/gemini-niveau9, ADR-040)
        static readonly string[] BiomeIds = { "sylve", "bayou", "echos", "forge", "echine", "givre", "cite", "orage", "sourd" };
        static readonly string[] Labels = { "LA SYLVE", "LE BAYOU", "LES ÉCHOS", "LA FORGE", "L'ÉCHINE", "LE GIVRE", "LA CITÉ SOURDE", "LE GRAND ORAGE", "LA SOURDINE" };
        static readonly string[] Sections = { "INTRO", "COUPLET A", "COUPLET B", "REFRAIN", "BREAK", "COUPLET C", "PRÉ-FINAL", "FINAL", "SILENCE" };
        static readonly string[] Guardians = { "CERF DE PIERRE", "GRENOUILLE-LUNE", "CHAUVE-SOURIS DE CRISTAL", "TAUREAU-ENCLUME", "CHAT-MONTAGNE", "RENARD DE GLACE", "LAPIN DE GRÈS", "OISEAU-TONNERRE", "LA SOURDINE" };
        static readonly string[] Enemies = { "djembe", "crapaud", "chauve", "crabe", "golem", "morse", "gargouille", "nuage", "djembe_sourd" };
        static readonly string[] Sparks = { "#FF6A82", "#FFD2A0", "#4FC8C0", "#FFA040", "#FFD060", "#FFFFFF", "#EDB88A", "#E0B040", "#FF2A4A" };
        static readonly string[] Flashes = { "#FFFFFF", "#FFE6D0", "#CFE8FF", "#FFD8A8", "#FFFFFF", "#FFFFFF", "#FFF2DC", "#FFFFFF", "#FFFFFF" };

        static readonly Color Gold = new(1f, 0.82f, 0.23f);
        static readonly Color Dim = new(0.6f, 0.58f, 0.75f);

        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        // ------------------------------------------------------------------ utilitaires
        /// <summary>Images numérotées `prefix0.png`, `prefix1.png`… tant qu'elles existent (vide si aucune).</summary>
        static Sprite[] Frames(string prefix, int max = 16)
        {
            var list = new List<Sprite>();
            for (int k = 0; k < max; k++)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(Art + prefix + k + ".png");
                if (s == null) break;
                list.Add(s);
            }
            return list.ToArray();
        }

        /// <summary>Sprite optionnel : null s'il n'existe pas.</summary>
        static Sprite SprOrNull(string rel) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + rel);

        /// <summary>
        /// Après tools/heros-v2/import.py et tools/gemini-ennemis/clean.py : met les nouveaux sprites dans les jeux
        /// d'images des héros, les biomes et le thème (ScriptableObjects), sans toucher à la scène ni aux prefabs.
        /// </summary>
        [MenuItem("Kenta/Installer les sprites des héros et des ennemis")]
        public static void InstallSpritesMenu()
        {
            AssetDatabase.Refresh();
            var heroes = BuildHeroes();
            var biomes = BuildBiomes();
            BuildTheme(biomes, heroes);
            AssetDatabase.SaveAssets();
            Debug.Log($"[KentaBuilder] sprites installés : {heroes.Length} héros, {biomes.Length} biomes, thème.");
        }

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
            // difficulté dans l'ordre de l'écran du héros : reprise au point, reprise au début, une seule vie.
            // `lives` n'est pas touché : 0 = valeur du mode (10 vies, 3 essais), à régler dans l'inspecteur si besoin.
            RetryMode[] modes = { RetryMode.Checkpoint, RetryMode.Restart, RetryMode.OneLife };
            var result = new HeroProfile[3];
            for (int i = 0; i < 3; i++)
            {
                var set = Asset<HeroSpriteSet>(SoDir + $"Heroes/HeroSprites_{names[i]}.asset");
                string n = ids[i];
                // héros v2 dessinés (tools/heros-v2/import.py, ADR-045) ; les sprites du PDF restent dans Pdf/
                string g = $"Gen/heroes/hero_{n}_";
                set.idle = Spr(g + "idle.png");
                set.idleCycle = Frames(g + "idle_");                          // 6 images en v2
                set.runCycle = Frames(g + "run_");                            // 8 images (Kenta, Aiko) ou 9 (Ryo)
                set.run = set.runCycle[0];
                set.jumpFrames = Frames(g + "jump_");                         // 8 images
                set.jump = set.jumpFrames[1];
                set.hitFrames = Frames(g + "hit_");                           // katana (6 images)
                set.hit = set.hitFrames[2];
                set.crouch = Spr(g + "crouch.png");
                set.climbCycle = Frames(g + "climb_");                        // 8 images
                set.descendCycle = Frames(g + "descend_");                    // 8 images
                set.fallFrames = Frames(g + "fall_");                         // 3 images
                set.vine = Spr(g + "vine.png");
                set.wall = Spr(g + "wall.png");
                set.hurt = Spr(g + "hurt.png");
                set.death = Spr(g + "death.png");
                // animations optionnelles : vine_ et hurt_ (2 images) sont fournies en v2 ; victory, land, wall_,
                // drum, death_, airhit et portrait restent vides (le code retombe sur les poses de base)
                set.victoryFrames = Frames(g + "victory_");
                set.landFrames = Frames(g + "land_");
                set.vineFrames = Frames(g + "vine_");
                set.wallCycle = Frames(g + "wall_");
                set.drumFrames = Frames(g + "drum_");
                set.hurtFrames = Frames(g + "hurt_");
                set.deathFrames = Frames(g + "death_");
                set.airHitFrames = Frames(g + "airhit_");
                set.portraits = Frames(g + "portrait_");
                EditorUtility.SetDirty(set);
                var p = Asset<HeroProfile>(SoDir + $"Heroes/Hero_{names[i]}.asset");
                p.displayName = names[i];
                p.role = roles[i];
                p.sprites = set;
                p.accent = Hex(accents[i]);
                p.retryMode = modes[i];
                // son de frappe du héros (tools/gen-sfx.py) : caisse claire, flûte, basse
                p.hitSound = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Project/Audio/Sfx/sfx_hit_{names[i].ToLowerInvariant()}.wav");
                EditorUtility.SetDirty(p);
                result[i] = p;
            }
            return result;
        }

        [Serializable] class GuardianPos { public string id; public float x, y; }
        [Serializable] class GuardianList { public GuardianPos[] items; }
        [Serializable] class SunPos { public string id; public float x, y, set; }
        [Serializable] class SunList { public SunPos[] items; }

        static BiomeTheme[] BuildBiomes()
        {
            var result = new BiomeTheme[BiomeIds.Length];
            var guardianFile = Art + "Gen/biomes/guardians.json";
            var positions = File.Exists(guardianFile) ? JsonUtility.FromJson<GuardianList>(File.ReadAllText(guardianFile)).items : new GuardianPos[0];
            var sunFile = Art + "Gen/biomes/suns.json";
            var suns = File.Exists(sunFile) ? JsonUtility.FromJson<SunList>(File.ReadAllText(sunFile)).items : new SunPos[0];
            for (int i = 0; i < BiomeIds.Length; i++)
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
                b.fillDeep = Spr($"Gen/biomes/fill_{id}_deep.png");
                b.pillarDecor = Enumerable.Range(0, 4).Select(k => Spr($"Gen/biomes/deco_{id}_{k}.png")).ToArray();
                b.guardian = Spr($"Gen/biomes/guardian_{id}.png");
                var pos = positions.FirstOrDefault(g => g.id == id);
                b.guardianOffset = pos != null ? new Vector2(pos.x, pos.y) : Vector2.zero;
                // astres (ADR-041) : le soleil est un sprite à part qui se couche ; lune et étoiles à la nuit
                var sun = suns.FirstOrDefault(g => g.id == id);
                b.sun = sun != null ? Spr($"Gen/biomes/sun_{id}.png") : null;
                b.sunOffset = sun != null ? new Vector2(sun.x, sun.y) : Vector2.zero;
                b.sunSetY = sun != null ? sun.set : -110f;
                b.moon = sun != null ? Spr("Gen/biomes/moon.png") : null;
                b.nightStars = sun != null ? Spr("Gen/biomes/night_stars.png") : null;
                b.bolts = id == "orage" ? Enumerable.Range(0, 3).Select(k => Spr($"Gen/biomes/bolt_{k}.png")).ToArray() : new Sprite[0];
                if (File.Exists(Art + $"Pdf/enemy_{Enemies[i]}_hurt.png"))
                {
                    // ennemis dessinés par Gemini (tools/gemini-ennemis/clean.py) : danse sur 2 temps, touché, cassé
                    string e = $"Pdf/enemy_{Enemies[i]}";
                    b.enemyFrames = new[] { Spr(e + "_squash.png"), Spr(e + "_stretch.png") };
                    b.enemyBroken = SprOrNull(e + "_broken.png");
                    b.enemyHurt = SprOrNull(e + "_hurt.png");
                    b.enemyBrokenFrames = Frames(e + "_broken_");
                }
                else if (Enemies[i].StartsWith("djembe"))         // le djembé du PDF, ou son négatif (monde sourd)
                {
                    b.enemyFrames = new[] { Spr($"Pdf/enemy_{Enemies[i]}_squash.png"), Spr($"Pdf/enemy_{Enemies[i]}_stretch.png") };
                    b.enemyBroken = Spr($"Pdf/enemy_{Enemies[i]}_broken.png");
                }
                else if (Enemies[i] == "chauve")
                {
                    // les Échos : la chauve-souris au sol bat des ailes comme celle qui vole (tools/gen-bat.py)
                    b.enemyFrames = Enumerable.Range(0, 4).Select(k => Spr($"Gen/world/bat_{k}.png")).ToArray();
                    b.enemyBroken = null;
                }
                else
                {
                    // danse sur 2 temps, comme le djembé : écrasé puis étiré (tools/gen-enemies.py)
                    string e = $"Pdf/enemy_{Enemies[i]}";
                    b.enemyFrames = AssetDatabase.LoadAssetAtPath<Sprite>(Art + e + "_squash.png") != null
                        ? new[] { Spr(e + "_squash.png"), Spr(e + "_stretch.png") }
                        : new[] { Spr(e + ".png") };
                    b.enemyBroken = null;
                }
                b.spark = Hex(Sparks[i]);
                b.monochrome = id == "sourd";
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
            t.coinFrames = Frames("Gen/world/coin_");                       // 4 (anciennes) ou 5 images (Gemini)
            t.totem = Spr("Gen/world/totem.png");
            // la Sourdine, boss du niveau 9 (Gen/boss : tools/gemini-niveau9/gen_sheets.py --install)
            t.sourdine = new SourdineSprites
            {
                body = Spr("Gen/boss/sourdine_body.png"),
                floatBody = Spr("Gen/boss/sourdine_float.png"),
                halfTop = Spr("Gen/boss/sourdine_half_top.png"),
                halfBottom = Spr("Gen/boss/sourdine_half_bottom.png"),
                eyes = new[] { "open", "half", "closed", "wide", "cracked" }.Select(m => Spr($"Gen/boss/sourdine_eye_{m}.png")).ToArray(),
                pupils = new[] { "big", "mid", "pin" }.Select(m => Spr($"Gen/boss/sourdine_pupil_{m}.png")).ToArray(),
                mouths = new[] { "smile", "half", "scream" }.Select(m => Spr($"Gen/boss/sourdine_mouth_{m}.png")).ToArray(),
                beam = Spr("Gen/boss/beam.png"),
                beamWarning = Spr("Gen/boss/beam_warning.png"),
                muzzle = Spr("Gen/boss/beam_muzzle.png"),
                noteBlack = new[] { Spr("Gen/boss/note_black_0.png"), Spr("Gen/boss/note_black_1.png") },
                noteGold = Spr("Gen/boss/note_gold.png"),
            };
            t.flyerFrames = Enumerable.Range(0, 4).Select(i => Spr($"Gen/world/bat_{i}.png")).ToArray();   // tools/gen-bat.py
            t.flyer = t.flyerFrames[0] != null ? t.flyerFrames[0] : Spr("Pdf/enemy_chauve.png");   // ennemi volant commun
            t.stomper = new[] { Spr("Gen/world/stomper_0.png"), Spr("Gen/world/stomper_1.png") };   // cuirassé commun
            // dessinés par Gemini (tools/gemini-ennemis/clean.py) ; vides sinon (le code garde alors les anciens visuels)
            t.flyerHang = Frames("Gen/world/bat_hang_");
            var batHurt = SprOrNull("Gen/world/bat_hurt.png");
            var batBroken = SprOrNull("Gen/world/bat_broken.png");
            t.flyerBroken = batHurt != null && batBroken != null ? new[] { batHurt, batBroken } : new Sprite[0];
            var stomped = Frames("Gen/world/stomper_stomped_").ToList();
            var recover = SprOrNull("Gen/world/stomper_recover.png");
            if (recover != null) stomped.Add(recover);
            t.stomperStomped = stomped.ToArray();
            t.drumFrames = Frames("Gen/world/drum_");
            t.orbFrames = Frames("Gen/world/orb_");
            t.vineKnot = SprOrNull("Gen/world/vine_knot.png");
            t.pixel = Spr("Gen/world/px.png");
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
            var subtitle = AddText(tr, font, "Subtitle", "LA COURSE EN RYTHME", new Vector2(0, -2), Color.white, 1, PixelText.Align.Center, true);
            var press = AddText(tr, font, "PressStart", "APPUIE SUR START", new Vector2(0, -46), Gold, 2, PixelText.Align.Center, true);
            var demo = AddText(tr, font, "DemoLabel", "DÉMO", new Vector2(0, 76), Gold, 1, PixelText.Align.Center, true);
            demo.gameObject.SetActive(false);
            var comp = root.AddComponent<AttractScreen>();
            Set(comp, "pressStart", press);
            Set(comp, "hiScore", hi);
            Set(comp, "logo", (RectTransform)logo.transform);
            Set(comp, "logoY", 26f);
            SetArray(comp, "logoOnly", new UnityEngine.Object[] { logo.gameObject, subtitle.gameObject, hi.gameObject });
            Set(comp, "demoLabel", demo.gameObject);
            // entrée façon arcade : lettres, éclairs, reflet (tools/gen-kenta-assets.py --only ui) et sons
            Set(comp, "subtitle", subtitle);
            SetArray(comp, "letters", Enumerable.Range(0, 5).Select(i => (UnityEngine.Object)Spr($"Gen/ui/logo_letter_{i}.png")).ToArray());
            SetArray(comp, "letterWhites", Enumerable.Range(0, 5).Select(i => (UnityEngine.Object)Spr($"Gen/ui/logo_letter_{i}_white.png")).ToArray());
            Set(comp, "logoWhite", Spr("Gen/ui/logo_white.png"));
            SetArray(comp, "shine", Enumerable.Range(0, 10).Select(i => (UnityEngine.Object)Spr($"Gen/ui/logo_shine_{i}.png")).ToArray());
            Set(comp, "letterSfx", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Sfx/sfx_land.wav"));
            Set(comp, "impactSfx", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Sfx/sfx_kill.wav"));
            return SavePrefab(root, "Screen_Attract");
        }

        static GameObject BuildExplain(PixelFontData font, LevelTheme theme)
        {
            // 2 gestes (ADR-030 : plus de glissade), les boutons dessinés comme ceux de la borne ; le héros mime le geste en cours
            var root = ScreenRoot("Screen_Explain");
            var tr = root.transform;
            var panel = AddImage(tr, "Panel", Spr("Gen/ui/panel.png"), new Vector2(24, -6), null, new Vector2(224, 112), false);
            panel.type = Image.Type.Sliced;
            AddText(tr, font, "Title", "COMMENT JOUER", new Vector2(0, 70), Gold, 2, PixelText.Align.Center, true);
            var hero = AddImage(tr, "Hero", theme.heroes[0].sprites.run, new Vector2(-122, -50), null, null, true, new Vector2(0.5f, 0f));
            string[] verbs = { "SAUTE", "FRAPPE" };
            string[] details = { "TROUS, CUIRASSÉS, LIANES", "LES INSTRUMENTS" };
            var texts = new PixelText[2];
            var icons = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                float y = 26 - i * 34;
                texts[i] = AddText(tr, font, $"Line{i}", verbs[i], new Vector2(-46, y + 4), Color.white, 2, PixelText.Align.Left);
                AddText(tr, font, $"Detail{i}", details[i], new Vector2(-46, y - 10), Dim, 1, PixelText.Align.Left);
            }
            icons[0] = AddImage(tr, "Btn1", Spr("Gen/ui/button_red.png"), new Vector2(-64, 26), null, null, true);
            AddText(tr, font, "Btn1Label", "1", new Vector2(-64, 26), Color.white);
            icons[1] = AddImage(tr, "Btn2", Spr("Gen/ui/button_blue.png"), new Vector2(-64, -8), null, null, true);
            AddText(tr, font, "Btn2Label", "2", new Vector2(-64, -8), Color.white);
            // la difficulté dépend du héros (choisi à l'écran suivant) : un pied de page vrai pour les trois
            var footer = AddText(tr, font, "Footer", ExplainScreen.FooterText, new Vector2(24, -50), Gold, 1, PixelText.Align.Center);
            AddText(tr, font, "Hint", "BOUTON 1 POUR CONTINUER", new Vector2(0, -80), Color.white, 1, PixelText.Align.Center, true);
            var comp = root.AddComponent<ExplainScreen>();
            Set(comp, "theme", theme);
            Set(comp, "hero", hero);
            Set(comp, "footer", footer);
            SetArray(comp, "lines", texts);
            SetArray(comp, "icons", icons);
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
                frames[i] = AddImage(tr, $"Frame{i}", Spr("Gen/ui/panel.png"), new Vector2(x, -12), Dim, new Vector2(84, 92), false);
                frames[i].type = Image.Type.Sliced;
                portraits[i] = AddImage(tr, $"Portrait{i}", theme.heroes[i].sprites.idle, new Vector2(x, -42), null, null, true, new Vector2(0.5f, 0f));
                names[i] = AddText(tr, font, $"Name{i}", theme.heroes[i].displayName, new Vector2(x, -68), Color.white, 1, PixelText.Align.Center, true);
            }
            AddText(tr, font, "Hint", "◄ ► HÉROS   1 VALIDER", new Vector2(0, -84), Color.white, 1, PixelText.Align.Center, true);
            var comp = root.AddComponent<HeroSelectScreen>();
            Set(comp, "theme", theme);
            Set(comp, "timerText", timer);
            SetArray(comp, "portraits", portraits);
            SetArray(comp, "frames", frames);
            SetArray(comp, "names", names);
            return SavePrefab(root, "Screen_HeroSelect");
        }

        static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        static void Anchor(Component c, float x, float y)
        {
            var rt = (RectTransform)c.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(x, y);
        }

        /// <summary>
        /// Choix du niveau (ADR-032), maquette docs/img/selection-niveau-maquette.png : liste façon osu! à droite (cartes
        /// créées en jeu depuis la carte modèle), infos et top 5 du niveau à gauche, bandeaux en haut et en bas.
        /// Coordonnées : pixels d'interface, origine au centre de l'écran 320×180.
        /// </summary>
        static GameObject BuildLevelSelect(PixelFontData font, LevelTheme theme)
        {
            var root = ScreenRoot("Screen_LevelSelect");
            var tr = root.transform;
            var px = Spr("Gen/ui/pixel.png");
            var star = Spr("Gen/ui/star.png");
            var ink = new Color(20 / 255f, 16 / 255f, 28 / 255f);
            var rowDim = new Color(90 / 255f, 84 / 255f, 130 / 255f);
            AddImage(tr, "Veil", px, Vector2.zero, new Color(ink.r, ink.g, ink.b, 90 / 255f), new Vector2(PixelScreen.Width, PixelScreen.Height), false);

            // liste : la carte modèle (inactive) est copiée en jeu, une par niveau du catalogue
            var list = NewRect("List", tr, Vector2.zero);
            var card = NewRect("CardTemplate", list, Vector2.zero, new Vector2(174, 28));
            card.pivot = new Vector2(0f, 0.5f);
            var frame = AddImage(card, "Frame", px, Vector2.zero, Gold, null, false);
            Stretch(frame.rectTransform, 0f);
            var fill = AddImage(card, "Fill", px, Vector2.zero, new Color(29 / 255f, 24 / 255f, 48 / 255f, 235 / 255f), null, false);
            Stretch(fill.rectTransform, 1f);
            var thumb = AddImage(card, "Thumb", Spr("Gen/ui/thumb_sylve.png"), new Vector2(2, 0), null, new Vector2(40, 24), false, new Vector2(0f, 0.5f));
            Anchor(thumb, 0f, 0.5f);
            var title = AddText(card, font, "Title", "1 LA SYLVE", new Vector2(46, 5), Color.white, 1, PixelText.Align.Left);
            Anchor(title, 0f, 0.5f);
            var song = AddText(card, font, "Song", "MAIN THEME", new Vector2(46, -6), Dim, 1, PixelText.Align.Left);
            Anchor(song, 0f, 0.5f);
            var cardStars = new Image[5];
            for (int i = 0; i < cardStars.Length; i++)
            {
                cardStars[i] = AddImage(card, $"Star{i}", star, new Vector2(-38 + 7 * i, -9), Gold, null, true, Vector2.zero);
                Anchor(cardStars[i], 1f, 0.5f);
            }
            var lc = card.gameObject.AddComponent<LevelCard>();
            Set(lc, "frame", frame);
            Set(lc, "fill", fill);
            Set(lc, "thumb", thumb);
            Set(lc, "title", title);
            Set(lc, "song", song);
            SetArray(lc, "stars", cardStars);
            card.gameObject.SetActive(false);

            // bandeau du haut (au-dessus de la liste)
            AddImage(tr, "TopBar", px, new Vector2(0, 82), new Color(ink.r, ink.g, ink.b, 230 / 255f), new Vector2(PixelScreen.Width, 16), false);
            AddText(tr, font, "Header", "CHOISIS TON NIVEAU", new Vector2(-154, 82), Gold, 1, PixelText.Align.Left);
            var timer = AddText(tr, font, "Timer", "15", new Vector2(148, 82), Color.white);

            // panneau d'infos à gauche
            var panel = AddImage(tr, "Panel", Spr("Gen/ui/panel.png"), new Vector2(-87, -1), null, new Vector2(134, 142), false);
            panel.type = Image.Type.Sliced;
            var name = AddText(tr, font, "Name", "LA SYLVE", new Vector2(-146, 50), Color.white, 2, PixelText.Align.Left);
            var songText = AddText(tr, font, "SongTitle", "MAIN THEME", new Vector2(-146, 30), Dim, 1, PixelText.Align.Left);
            var info = AddText(tr, font, "Info", "150 BPM  1:43", new Vector2(-146, 18), Dim, 1, PixelText.Align.Left);
            var stars = new Image[5];
            for (int i = 0; i < stars.Length; i++) stars[i] = AddImage(tr, $"Star{i}", star, new Vector2(-146 + 7 * i, 5), Gold, null, true, Vector2.zero);
            AddImage(tr, "Rule", px, new Vector2(-146, -3), rowDim, new Vector2(118, 1), false, Vector2.zero);
            AddText(tr, font, "BoardTitle", "CLASSEMENT DU NIVEAU", new Vector2(-146, -12), Gold, 1, PixelText.Align.Left);
            var boardNames = new PixelText[5];
            var boardScores = new PixelText[5];
            for (int k = 0; k < boardNames.Length; k++)
            {
                float y = -23 - 10 * k;
                boardNames[k] = AddText(tr, font, $"BoardName{k}", $"{k + 1} ---", new Vector2(-146, y), rowDim, 1, PixelText.Align.Left);
                boardScores[k] = AddText(tr, font, $"BoardScore{k}", "", new Vector2(-30, y), rowDim, 1, PixelText.Align.Right);
            }

            // bandeau du bas
            AddImage(tr, "BottomBar", px, new Vector2(0, -83), new Color(ink.r, ink.g, ink.b, 200 / 255f), new Vector2(PixelScreen.Width, 14), false);
            AddText(tr, font, "Hint", "▲▼ NIVEAU   1 JOUER   2 RETOUR", new Vector2(0, -83), Color.white);

            var comp = root.AddComponent<LevelSelectScreen>();
            Set(comp, "listRoot", list);
            Set(comp, "cardTemplate", lc);
            Set(comp, "nameText", name);
            Set(comp, "songText", songText);
            Set(comp, "infoText", info);
            Set(comp, "timerText", timer);
            SetArray(comp, "stars", stars);
            SetArray(comp, "boardNames", boardNames);
            SetArray(comp, "boardScores", boardScores);
            Set(comp, "font", font);
            return SavePrefab(root, "Screen_LevelSelect");
        }

        static GameObject BuildHud(PixelFontData font, LevelTheme theme)
        {
            var root = ScreenRoot("Screen_Playing");
            FillHud(root, font);
            return SavePrefab(root, "Screen_Playing");
        }

        /// <summary>
        /// Refait le HUD dans le prefab existant (Screen_Playing) sans le remplacer : la racine, ses composants et donc
        /// l'instance de la scène Main et ses références restent les mêmes ; seuls les enfants sont recréés. Ne touche ni
        /// à la scène ni aux autres écrans (la reconstruction complète ajoute un objet « Bandana » à la scène).
        /// </summary>
        [MenuItem("Kenta/Construire le HUD")]
        public static void BuildHudMenu() => Debug.Log(BuildHudInPlace());

        public static string BuildHudInPlace()
        {
            AssetDatabase.Refresh();
            var font = AssetDatabase.LoadAssetAtPath<PixelFontData>(SoDir + "PixelFont.asset");
            if (font == null) throw new Exception("PixelFont absent : lancer d'abord « Reconstruire thèmes, prefabs et scène ».");
            string path = PrefabDir + "Screen_Playing.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                FillHud(root, font);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return "HUD reconstruit dans " + path;
        }

        /// <summary>
        /// HUD en partie (RunHud) : trois plaques en haut, collées à 4 px d'écart (score | frise | vies et pièces), un
        /// intitulé sous la frise, le mot PARFAIT / BIEN / BOF au-dessus du héros (JudgePopup). Grille : UI/HudLayout.
        /// Les positions fines sont posées par RunHud.Layout au départ de chaque partie (vies, nombre de pièces).
        /// </summary>
        static void FillHud(GameObject root, PixelFontData font)
        {
            var tr = root.transform;
            var px = Spr("Gen/ui/pixel.png");
            var group = root.GetComponent<CanvasGroup>();
            if (group == null) group = root.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            var scorePlate = HudPlateNode(tr, "ScorePlate", px);
            var score = HudNumberNode(scorePlate.transform, font, "Score", 6, true, Color.white);
            var multGroup = NewRect("Multiplier", scorePlate.transform, Vector2.zero);
            var multSign = AddText(multGroup, font, "Sign", "×", Vector2.zero, Gold, 1, PixelText.Align.Left);
            var multNumber = HudNumberNode(multGroup, font, "Digits", 1, false, Gold);
            var bestIcon = AddImage(scorePlate.transform, "BestIcon", Spr("Gen/ui/hud_best.png"), Vector2.zero, null, null, true, Vector2.zero);
            var bestNumber = HudNumberNode(scorePlate.transform, font, "Best", 6, true, Dim);

            var friezePlate = HudPlateNode(tr, "FriezePlate", px);
            var friezeBack = new Image[12];
            var friezeFill = new Image[12];
            for (int i = 0; i < friezeBack.Length; i++)
            {
                var back = AddImage(friezePlate.transform, $"Seg{i}", px, Vector2.zero, null, new Vector2(8, HudLayout.FriezeHeight), false, Vector2.zero);
                var fill = AddImage(back.transform, "Fill", px, Vector2.zero, Gold, null, false, new Vector2(0f, 0.5f));
                var frt = fill.rectTransform;
                frt.anchorMin = new Vector2(0, 0);
                frt.anchorMax = new Vector2(0, 1);       // RunHud règle sa largeur en pixels entiers
                frt.sizeDelta = Vector2.zero;
                friezeBack[i] = back;
                friezeFill[i] = fill;
            }

            var statsPlate = HudPlateNode(tr, "StatsPlate", px);
            var heart = AddImage(statsPlate.transform, "Heart", Spr("Gen/ui/hud_heart.png"), Vector2.zero, null, null, true, Vector2.zero);
            var lives = HudNumberNode(statsPlate.transform, font, "Lives", 2, false, Color.white);
            var triesWord = AddText(statsPlate.transform, font, "TriesWord", "ESSAI", Vector2.zero, Dim, 1, PixelText.Align.Left);
            var tries = AddText(statsPlate.transform, font, "Tries", "1/3", Vector2.zero, Color.white, 1, PixelText.Align.Left);
            var coinIcon = AddImage(statsPlate.transform, "CoinIcon", Spr("Gen/ui/hud_coin.png"), Vector2.zero, null, null, true, Vector2.zero);
            var coins = HudNumberNode(statsPlate.transform, font, "Coins", 4, true, Color.white);
            var slash = AddText(statsPlate.transform, font, "Slash", "/", Vector2.zero, Dim, 1, PixelText.Align.Left);
            var total = HudNumberNode(statsPlate.transform, font, "CoinsTotal", 4, true, Dim);

            var labelPlate = HudPlateNode(tr, "LabelPlate", px);
            var label = AddText(labelPlate.transform, font, "Label", "", Vector2.zero, Color.white, 1, PixelText.Align.Left);

            var judgeLabel = AddText(tr, font, "Judge", "", Vector2.zero, Gold, 1, PixelText.Align.Center, false);
            judgeLabel.Outline = true;
            var popup = root.GetComponent<JudgePopup>();
            if (popup == null) popup = root.AddComponent<JudgePopup>();
            Set(popup, "label", judgeLabel);

            var comp = root.GetComponent<RunHud>();
            if (comp == null) comp = root.AddComponent<RunHud>();
            Set(comp, "scorePlate", scorePlate);
            Set(comp, "friezePlate", friezePlate);
            Set(comp, "statsPlate", statsPlate);
            Set(comp, "labelPlate", labelPlate);
            Set(comp, "scoreNumber", score);
            Set(comp, "multiplierGroup", multGroup);
            Set(comp, "multiplierSign", multSign);
            Set(comp, "multiplierNumber", multNumber);
            Set(comp, "bestIcon", bestIcon);
            Set(comp, "bestNumber", bestNumber);
            SetArray(comp, "friezeBack", friezeBack);
            SetArray(comp, "friezeFill", friezeFill);
            Set(comp, "heart", heart);
            Set(comp, "heartFull", Spr("Gen/ui/hud_heart.png"));
            Set(comp, "heartEmpty", Spr("Gen/ui/hud_heart_empty.png"));
            Set(comp, "livesNumber", lives);
            Set(comp, "triesWord", triesWord);
            Set(comp, "triesText", tries);
            Set(comp, "coinIcon", coinIcon);
            Set(comp, "coinsNumber", coins);
            Set(comp, "coinsSlash", slash);
            Set(comp, "coinsTotal", total);
            Set(comp, "labelText", label);
            comp.Layout(RetryMode.Checkpoint, 10, 1, 122, false);   // aperçu du prefab : Kenta (10 vies), niveau 1 (122 pièces)
        }

        /// <summary>Plaque du HUD : fond translucide, cadre d'1 px aux coins coupés (4 bords), ancrée en haut à gauche.</summary>
        static HudPlate HudPlateNode(Transform parent, string name, Sprite px)
        {
            var rt = NewRect(name, parent, new Vector2(HudLayout.Margin, -HudLayout.Margin), new Vector2(40, HudLayout.PlateHeight));
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.gameObject.AddComponent<CanvasGroup>();
            var fill = AddImage(rt, "Fill", px, Vector2.zero, new Color(29 / 255f, 24 / 255f, 48 / 255f, 220 / 255f), null, false);
            Stretch(fill.rectTransform, 1f);
            var frame = new Image[4];
            string[] names = { "Top", "Bottom", "Left", "Right" };
            for (int i = 0; i < 4; i++)
            {
                frame[i] = AddImage(rt, names[i], px, Vector2.zero, new Color(90 / 255f, 80 / 255f, 136 / 255f), null, false);
                var f = frame[i].rectTransform;
                bool horizontal = i < 2;
                // bords d'1 px, raccourcis d'1 px à chaque bout : coins coupés, comme panel.png
                f.anchorMin = horizontal ? new Vector2(0f, i == 0 ? 1f : 0f) : new Vector2(i == 2 ? 0f : 1f, 0f);
                f.anchorMax = horizontal ? new Vector2(1f, i == 0 ? 1f : 0f) : new Vector2(i == 2 ? 0f : 1f, 1f);
                f.pivot = horizontal ? new Vector2(0.5f, i == 0 ? 1f : 0f) : new Vector2(i == 2 ? 0f : 1f, 0.5f);
                f.anchoredPosition = Vector2.zero;
                f.sizeDelta = horizontal ? new Vector2(-2f, 1f) : new Vector2(1f, -2f);
            }
            var plate = rt.gameObject.AddComponent<HudPlate>();
            Set(plate, "fill", fill);
            SetArray(plate, "frame", frame);
            return plate;
        }

        /// <summary>Nombre en colonnes fixes de 6 px : un PixelText par chiffre (voir UI/HudNumber).</summary>
        static HudNumber HudNumberNode(Transform parent, PixelFontData font, string name, int cells, bool zeroPad, Color color)
        {
            var rt = NewRect(name, parent, Vector2.zero);
            var texts = new UnityEngine.Object[cells];
            for (int i = 0; i < cells; i++) texts[i] = AddText(rt, font, $"D{i}", "0", Vector2.zero, color, 1, PixelText.Align.Center);
            var n = rt.gameObject.AddComponent<HudNumber>();
            SetArray(n, "cells", texts);
            Set(n, "zeroPad", zeroPad);
            return n;
        }

        static GameObject BuildGameOver(PixelFontData font)
        {
            var root = ScreenRoot("Screen_GameOver");
            var tr = root.transform;
            // titre en haut, note en grand au centre, panneau en dessous (la note était coincée à gauche du panneau)
            var titleT = AddText(tr, font, "Title", "GAME OVER", new Vector2(0, 62), Color.white, 3, PixelText.Align.Center, true);
            var grade = AddText(tr, font, "Grade", "S+", new Vector2(0, 19), Gold, 4, PixelText.Align.Center, true);   // ×4 : de l'air entre titre, note et panneau
            var panel = NewRect("Panel", tr, new Vector2(0, -50), new Vector2(196, 76));
            var bg = panel.gameObject.AddComponent<Image>();
            bg.sprite = Spr("Gen/ui/panel.png");
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;
            var score = AddText(panel, font, "Score", "SCORE 000000", new Vector2(0, 24), Gold, 1);
            var judge = AddText(panel, font, "Judge", "PARFAIT 0  BIEN 0  BOF 0", new Vector2(0, 8), Color.white);
            var coins = AddText(panel, font, "Coins", "PIÈCES 0/0", new Vector2(0, -8), Color.white);
            var biome = AddText(panel, font, "Biome", "DÉCOR LA SYLVE", new Vector2(0, -24), Dim);
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

        static GameObject BuildHighscores(PixelFontData font, LevelTheme theme)
        {
            var root = ScreenRoot("Screen_Highscores");
            root.AddComponent<HighscoreBoard>();
            FillHighscores(root, font);
            return SavePrefab(root, "Screen_Highscores");
        }

        /// <summary>
        /// Refait le classement dans le prefab existant (Screen_Highscores) sans le remplacer : racine et composant gardés,
        /// donc la scène Main et ses références ne bougent pas ; seuls les enfants sont recréés (comme Construire le HUD).
        /// </summary>
        [MenuItem("Kenta/Construire le classement")]
        public static void BuildHighscoresMenu() => Debug.Log(BuildHighscoresInPlace());

        public static string BuildHighscoresInPlace()
        {
            AssetDatabase.Refresh();
            var font = AssetDatabase.LoadAssetAtPath<PixelFontData>(SoDir + "PixelFont.asset");
            if (font == null) throw new Exception("PixelFont absent : lancer d'abord « Reconstruire thèmes, prefabs et scène ».");
            string path = PrefabDir + "Screen_Highscores.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                FillHighscores(root, font);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return "Classement reconstruit dans " + path;
        }

        /// <summary>
        /// Classement en pixel art (UI/HighscoreBoard) : titre, plaque, en-têtes RANG / NOM / HÉROS / SCORE, dix lignes à
        /// 10 px (bande une sur deux, rouge sur toute la ligne pour un score en DUR, liseré à gauche), petites pièces or /
        /// argent / bronze devant les trois premiers, héros à la couleur de sa difficulté et son bonus, légende en bas.
        /// Colonnes (px de l'interface 320×180, origine au centre) : pièce −88, rang −80, nom −62, héros −38, bonus −6,
        /// score 92 (calé à droite).
        /// </summary>
        static void FillHighscores(GameObject root, PixelFontData font)
        {
            var tr = root.transform;
            var px = Spr("Gen/ui/pixel.png");
            var coin = Spr("Gen/ui/hud_coin.png");
            var title = AddText(tr, font, "Title", "MEILLEURS SCORES", new Vector2(0, 72), Gold, 2, PixelText.Align.Center, true);
            var panel = AddImage(tr, "Panel", Spr("Gen/ui/panel.png"), new Vector2(0, -6), null, new Vector2(204, 140), false);
            panel.type = Image.Type.Sliced;

            var header = NewRect("Header", tr, Vector2.zero);
            AddText(header, font, "Rank", "RANG", new Vector2(-92, 54), Dim, 1, PixelText.Align.Left);   // au-dessus des pièces : « NOM » commence à −62
            AddText(header, font, "Name", "NOM", new Vector2(-62, 54), Dim, 1, PixelText.Align.Left);
            AddText(header, font, "Hero", "HÉROS", new Vector2(-38, 54), Dim, 1, PixelText.Align.Left);
            AddText(header, font, "Score", "SCORE", new Vector2(92, 54), Dim, 1, PixelText.Align.Right);
            AddImage(header, "Rule", px, new Vector2(0, 48), new Color(Dim.r, Dim.g, Dim.b, 0.5f), new Vector2(192, 1), false);

            const int rows = 10;
            var bands = new Image[rows];
            var accents = new Image[rows];
            var medals = new Image[3];
            var ranks = new PixelText[rows];
            var names = new PixelText[rows];
            var heroes = new PixelText[rows];
            var bonuses = new PixelText[rows];
            var scores = new PixelText[rows];
            for (int i = 0; i < rows; i++)
            {
                float y = 41 - i * 10;
                bands[i] = AddImage(tr, $"Band{i}", px, new Vector2(0, y), new Color(1f, 1f, 1f, 0.05f), new Vector2(192, 9), false);
                accents[i] = AddImage(tr, $"Accent{i}", px, new Vector2(-95, y), Color.red, new Vector2(2, 9), false);
                if (i < 3) medals[i] = AddImage(tr, $"Medal{i}", coin, new Vector2(-88, y));
                ranks[i] = AddText(tr, font, $"Rank{i}", "01", new Vector2(-80, y), Color.white, 1, PixelText.Align.Left);
                names[i] = AddText(tr, font, $"Name{i}", "AAA", new Vector2(-62, y), Color.white, 1, PixelText.Align.Left);
                heroes[i] = AddText(tr, font, $"Hero{i}", "KENTA", new Vector2(-38, y), Color.white, 1, PixelText.Align.Left);
                bonuses[i] = AddText(tr, font, $"Bonus{i}", "×2", new Vector2(-6, y), Color.white, 1, PixelText.Align.Left);
                scores[i] = AddText(tr, font, $"Score{i}", "000000", new Vector2(92, y), Color.white, 1, PixelText.Align.Right);
            }

            // légende : la couleur de chaque difficulté et son bonus de score
            var legend = NewRect("Legend", tr, Vector2.zero);
            AddImage(legend, "RuleBottom", px, new Vector2(0, -60), new Color(Dim.r, Dim.g, Dim.b, 0.5f), new Vector2(192, 1), false);
            (string label, RetryMode mode, float x)[] items =
                { ("FACILE", RetryMode.Checkpoint, -90), ("MOYEN ×1,5", RetryMode.Restart, -36), ("DUR ×2", RetryMode.OneLife, 36) };
            foreach (var (label, mode, x) in items)
            {
                var c = RetryRules.DifficultyColor(mode);
                AddImage(legend, "Swatch" + mode, px, new Vector2(x, -67), c, new Vector2(5, 5), false);
                AddText(legend, font, "Label" + mode, label, new Vector2(x + 6, -67), c, 1, PixelText.Align.Left);
            }

            var empty = AddText(tr, font, "Empty", "AUCUN SCORE : À TOI DE JOUER !", new Vector2(0, -6), Color.white);
            var comp = root.GetComponent<HighscoreBoard>();
            SetArray(comp, "ranks", ranks);
            SetArray(comp, "namesText", names);
            SetArray(comp, "heroes", heroes);
            SetArray(comp, "bonuses", bonuses);
            SetArray(comp, "scores", scores);
            SetArray(comp, "bands", bands);
            SetArray(comp, "accents", accents);
            SetArray(comp, "medals", medals);
            SetArray(comp, "decorations", new UnityEngine.Object[] { header.gameObject, legend.gameObject });
            Set(comp, "emptyText", empty);
            Set(comp, "title", title);
            Set(comp, "font", font);
        }

        static GameObject BuildNameEntry(PixelFontData font, LevelTheme theme)
        {
            // trois lettres : joystick haut/bas, bouton 1 suivante, bouton 2 retour ; le héros bondit de joie à droite
            var root = ScreenRoot("Screen_NameEntry");
            var tr = root.transform;
            var panel = AddImage(tr, "Panel", Spr("Gen/ui/panel.png"), new Vector2(-20, -2), null, new Vector2(176, 100), false);
            panel.type = Image.Type.Sliced;
            AddText(tr, font, "Title", "NOUVEAU RECORD !", new Vector2(0, 72), Gold, 2, PixelText.Align.Center, true);
            var timer = AddText(tr, font, "Timer", "20", new Vector2(146, 72), Color.white, 1, PixelText.Align.Center, true);
            var score = AddText(tr, font, "Score", "SCORE 000000", new Vector2(-20, 34), Gold, 1);
            AddText(tr, font, "Prompt", "TON NOM", new Vector2(-20, 20), Dim, 1);
            var letters = new PixelText[3];
            for (int i = 0; i < 3; i++)
            {
                float x = -20 + (i - 1) * 26;
                AddText(tr, font, $"Up{i}", "▲", new Vector2(x, 8), Dim);
                letters[i] = AddText(tr, font, $"Letter{i}", "A", new Vector2(x, -8), Color.white, 3);
                AddText(tr, font, $"Down{i}", "▼", new Vector2(x, -26), Dim);
            }
            var hero = AddImage(tr, "Hero", theme.heroes[0].sprites.jump, new Vector2(108, -44), null, null, true, new Vector2(0.5f, 0f));
            AddText(tr, font, "Hint", "▲ ▼ LETTRE   1 SUIVANTE   2 RETOUR", new Vector2(0, -80), Color.white, 1, PixelText.Align.Center, true);
            var comp = root.AddComponent<NameEntryScreen>();
            Set(comp, "theme", theme);
            SetArray(comp, "letters", letters);
            Set(comp, "scoreText", score);
            Set(comp, "timerText", timer);
            Set(comp, "hero", hero);
            return SavePrefab(root, "Screen_NameEntry");
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

        /// <summary>
        /// Un niveau par biome, chacun avec son SongData (Song_0N_&lt;biome&gt;), créé s'il manque en recopiant Song_Level01
        /// (ADR-032). Ne remplace jamais le morceau, la chart ou la vignette qu'un niveau a déjà reçus.
        /// </summary>
        static LevelCatalog BuildLevels(BiomeTheme[] biomes)
        {
            int[] stars = { 1, 2, 3, 4, 4, 4, 5, 5, 5 };     // les valeurs par défaut (Orage et Cité échangés dans le catalogue : 7 puis 8)
            var shared = AssetDatabase.LoadAssetAtPath<SongData>(SoDir + "Songs/Song_Level01.asset");
            EnsureFolder(SoDir + "Levels");
            var byBiome = new LevelDef[biomes.Length];
            for (int i = 0; i < biomes.Length; i++)
            {
                string path = SoDir + $"Levels/Level_{i + 1:00}_{BiomeIds[i]}.asset";
                bool isNew = AssetDatabase.LoadAssetAtPath<LevelDef>(path) == null;
                var l = Asset<LevelDef>(path);
                l.id = BiomeIds[i];
                l.biome = biomes[i];
                if (l.song == null || l.song == shared) l.song = LevelSong(i, shared);
                if (l.thumbnail == null) l.thumbnail = AssetDatabase.LoadAssetAtPath<Sprite>(Art + $"Gen/ui/thumb_{BiomeIds[i]}.png");
                if (isNew) l.stars = stars[Mathf.Min(i, stars.Length - 1)];
                byBiome[i] = l;
            }
            // Ordre de difficulté (ADR-044) : 1..6, puis 7 = Orage (index biome 7), 8 = Cité (index biome 6), 9 = Sourdine (8), 10 = Secret
            int[] order = { 0, 1, 2, 3, 4, 5, 7, 6, 8 };
            var levels = new LevelDef[10];
            for (int k = 0; k < order.Length; k++)
            {
                var l = byBiome[order[k]];
                l.number = k + 1;
                EditorUtility.SetDirty(l);
                levels[k] = l;
            }
            var secret = Asset<LevelDef>(SoDir + "Levels/Level_10_secret.asset");
            secret.id = "secret";
            secret.number = 10;
            secret.stars = 5;
            secret.lockedBySourdineHard = true;
            if (secret.biome == null) secret.biome = biomes[8];
            if (secret.thumbnail == null) secret.thumbnail = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Gen/ui/thumb_sourd.png");
            EditorUtility.SetDirty(secret);
            levels[9] = secret;

            var catalog = Asset<LevelCatalog>(SoDir + "LevelCatalog.asset");
            catalog.levels = levels;
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        /// <summary>Morceau propre au niveau i : créé s'il n'existe pas (copie du morceau commun), jamais réécrit.</summary>
        static SongData LevelSong(int i, SongData shared)
        {
            string path = SoDir + $"Songs/Song_{i + 1:00}_{BiomeIds[i]}.asset";
            var song = AssetDatabase.LoadAssetAtPath<SongData>(path);
            if (song != null) return song;
            song = ScriptableObject.CreateInstance<SongData>();
            if (shared != null)
            {
                song.clip = shared.clip;
                song.remixClip = shared.remixClip;
                song.bpm = shared.bpm;
                song.offsetSeconds = shared.offsetSeconds;
                song.beatsPerBar = shared.beatsPerBar;
                song.chart = shared.chart;
            }
            song.title = "MAIN THEME";
            song.previewBeat = 96f;                    // début du refrain de « Main Theme »
            AssetDatabase.CreateAsset(song, path);
            return song;
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
            int displayLayer = LayerMask.NameToLayer("Display");
            if (displayLayer < 0) throw new System.Exception("Couche « Display » absente (Project Settings > Tags and Layers).");
            cam.cullingMask = ~((1 << actorsLayer) | (1 << uiLayer) | (1 << displayLayer));
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

            // effet CRT (ADR-034) : l'image composée passe par une texture puis par Kenta/CrtDisplay
            var crtMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/CrtDisplay.mat");
            if (crtMat == null)
            {
                crtMat = new Material(Shader.Find("Kenta/CrtDisplay"));
                AssetDatabase.CreateAsset(crtMat, "Assets/_Project/Materials/CrtDisplay.mat");
            }
            var crt = psGo.AddComponent<CrtScreen>();
            Set(crt, "displayCanvas", dc);
            Set(crt, "material", crtMat);

            var screens = prefabs.Select(p => (GameObject)PrefabUtility.InstantiatePrefab(p, uiGo.transform)).ToArray();
            foreach (var s in screens) { var r = (RectTransform)s.transform; r.anchoredPosition = Vector2.zero; s.SetActive(false); }
            GameObject attract = screens[0], explain = screens[1], hero = screens[2], hud = screens[3], over = screens[4],
                       board = screens[5], nameEntry = screens[6], levelSel = screens[7];
            levelSel.transform.SetSiblingIndex(hero.transform.GetSiblingIndex() + 1);   // juste après le héros

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

            // un seul bandeau : chaque reconstruction en ajoutait un de plus (30 dans la scène, tous actifs : jusqu'à
            // 1 400 sprites de traînée en fin de partie, une grosse part du lag de la borne)
            for (int i = world.transform.childCount - 1; i >= 0; i--)
                if (world.transform.GetChild(i).name == "Bandana") UnityEngine.Object.DestroyImmediate(world.transform.GetChild(i).gameObject);
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
            Set(rig, "defaultZoom", 480);       // vue de 12 × 6,75 u (×4) : héros lisible sur la borne (audit du 25/11)
            Set(rig, "defaultFrameX", 0.22f);   // héros au quart gauche de l'écran
            Set(rig, "defaultFrameY", 0.3f);    // sol sous le héros à 30 % du bas
            Set(player, "visual", visual);
            Set(player, "spinner", spinner);
            Set(player, "slash", null);
            Set(player, "trail", bandana);
            Set(player, "burstColor", theme.heroes[0].accent);
            Set(player, "squashAmount", 0f);
            Set(player, "spinOnJump", false);
            Set(player, "width", 0.5f);
            Set(player, "standHeight", 1.0f);
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
            Set(flow, "playingScreen", hud);
            Set(flow, "gameOverScreen", over);
            WireScreens(flow, hero, levelSel, board);
            Set(flow, "theme", theme);
            Set(flow, "backdrop", backdrop);
            Set(flow, "nameEntry", nameEntry.GetComponent<NameEntryScreen>());
            Set(flow, "gameOverDuration", 4.5f);    // titre, note puis panneau : le temps de lire (bouton 1 pour passer)
            Set(flow, "gameOverHighscoreDuration", 3.5f);
            Set(flow, "attractLogoSeconds", 15f);
            Set(flow, "attractDemoSeconds", 20f);
            Set(flow, "attractBoardSeconds", 8f);
            Set(flow, "catalog", catalog);
            Set(flow, "attractSong", null);

            cam.backgroundColor = Color.black;
            SetLayerRecursive(uiGo, uiLayer);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "scène câblée";
        }

        /// <summary>Écrans câblés à GameFlow : héros (Config), niveaux (LevelSelect), classement.</summary>
        static void WireScreens(GameFlow flow, GameObject hero, GameObject levelSelect, GameObject board)
        {
            Set(flow, "configScreen", hero);
            Set(flow, "heroSelect", hero.GetComponent<HeroSelectScreen>());
            Set(flow, "levelSelectScreen", levelSelect);
            Set(flow, "levelSelect", levelSelect.GetComponent<LevelSelectScreen>());
            Set(flow, "highscoreBoard", board.GetComponent<HighscoreBoard>());
        }

        /// <summary>Remplace (ou ajoute) l'instance d'un écran sous le canvas, au même rang (ou au rang donné).</summary>
        static GameObject ReplaceScreen(Transform canvas, GameObject prefab, int index = -1)
        {
            var old = canvas.Find(prefab.name);
            int at = old != null ? old.GetSiblingIndex() : canvas.childCount;
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
            ((RectTransform)go.transform).anchoredPosition = Vector2.zero;
            go.SetActive(false);
            go.transform.SetSiblingIndex(Mathf.Clamp(index >= 0 ? index : at, 0, canvas.childCount - 1));
            return go;
        }

        /// <summary>
        /// Construit seulement l'écran des niveaux (ADR-032) : morceaux et vignettes des niveaux, prefabs du héros, des
        /// niveaux et du classement, puis les branche dans la scène ouverte. Ne touche à rien d'autre (la reconstruction
        /// complète ajoute un objet « Bandana » à la scène).
        /// </summary>
        [MenuItem("Kenta/Construire l'écran de niveaux")]
        public static void BuildLevelSelectMenu() => Debug.Log(BuildLevelSelectOnly());

        public static string BuildLevelSelectOnly()
        {
            AssetDatabase.Refresh();
            var font = AssetDatabase.LoadAssetAtPath<PixelFontData>(SoDir + "PixelFont.asset");
            var theme = AssetDatabase.LoadAssetAtPath<LevelTheme>(SoDir + "Theme_Kenta.asset");
            if (font == null || theme == null) throw new Exception("PixelFont ou Theme_Kenta absent : lancer d'abord « Reconstruire thèmes, prefabs et scène ».");
            var catalog = BuildLevels(theme.biomes);
            AssetDatabase.SaveAssets();
            var heroPrefab = BuildHeroSelect(font, theme);
            var boardPrefab = BuildHighscores(font, theme);
            var levelPrefab = BuildLevelSelect(font, theme);
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/_Project/Scenes/Main.unity")
                scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
            var ui = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(c => c.name == "PixelUI" && c.transform.parent == null);
            if (ui == null) throw new Exception("Canvas « PixelUI » absent de la scène : lancer « Reconstruire thèmes, prefabs et scène ».");
            var hero = ReplaceScreen(ui.transform, heroPrefab);
            var board = ReplaceScreen(ui.transform, boardPrefab);
            var levelSel = ReplaceScreen(ui.transform, levelPrefab, hero.transform.GetSiblingIndex() + 1);
            int uiLayer = LayerMask.NameToLayer("UI");
            foreach (var go in new[] { hero, board, levelSel }) SetLayerRecursive(go, uiLayer);

            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>(FindObjectsInactive.Include);
            WireScreens(flow, hero, levelSel, board);
            Set(flow, "catalog", catalog);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return $"écran des niveaux construit ; {catalog.levels.Length} niveaux";
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
                BuildHighscores(font, theme), BuildNameEntry(font, theme), BuildLevelSelect(font, theme),
            };
            AssetDatabase.SaveAssets();
            string result = BuildScene(theme, font, prefabs, catalog);
            AssetDatabase.SaveAssets();
            return $"{result} ; {catalog.levels.Length} niveaux, {biomes.Length} biomes, {heroes.Length} héros, {font.sprites.Length} glyphes";
        }
    }
}
