using Kenta.Level;
using Kenta.Rhythm;
using UnityEditor;
using UnityEngine;

namespace Kenta.EditorTools
{
    /// <summary>
    /// Branche les morceaux, les charts et l'échelle complète des 10 niveaux (ADR-035, ADR-046) :
    /// N1 Sylve (117 BPM) → N2 Bayou (120) → N3 Échos (140) → N4 Forge (125, Chaos ID) → N5 Échine (150, L'Ascension) →
    /// N6 Givre (168, Boss Battle #2) → N7 Grand Orage (180) → N8 Cité Sourde (120→240, Atama no Taisou) →
    /// N9 La Sourdine (280, BIKE) → N10 « ??? » (secret enchaîné, débloqué en finissant N9 en DUR).
    /// Menu : Kenta &gt; Brancher les morceaux des niveaux. Rejouable sans risque.
    /// </summary>
    public static class KentaLevelSongs
    {
        const string SoDir = "Assets/_Project/ScriptableObjects/";
        const string Songs = SoDir + "Songs/";
        const string LevelsDir = SoDir + "Levels/";
        const string BiomesDir = SoDir + "Biomes/";
        const string Music = "Assets/_Project/Audio/Music/";
        const string Charts = "Assets/_Project/Charts/";
        const string Art = "Assets/_Project/Art/Kenta/";

        struct SongEntry
        {
            public string song, clip, chart, title;
            public float bpm, offset, preview;
        }

        struct SlotEntry
        {
            public string levelAsset, id, biomeAsset, songAsset, thumb;
            public int number, stars;
            public bool lockedSecret;
        }

        static readonly SongEntry[] SongEntries =
        {
            new() { song = "Song_01_sylve",  clip = "n1_sylve_resistors.ogg",   chart = "n1_sylve.json",             title = "WE'RE THE RESISTORS", bpm = 117f,    offset = 0.054f, preview = 64f },
            new() { song = "Song_02_bayou",  clip = "n2_bayou_arpanauts.ogg",   chart = "n2_bayou.json",             title = "ARPANAUTS",           bpm = 120f,    offset = 0.048f, preview = 64f },
            new() { song = "Song_03_echos",  clip = "n3_echos_stage1.ogg",      chart = "n3_echos.json",             title = "STAGE 1",             bpm = 140f,    offset = 0.020f, preview = 64f },
            new() { song = "Song_04_forge",  clip = "n4_forge_chaos_id.ogg",    chart = "n4_forge.json",             title = "CHAOS ID",            bpm = 125f,    offset = 0.052f, preview = 96f },
            new() { song = "Song_05_echine", clip = "level01_shovel_knight.ogg",chart = "level01_shovel_knight.json",title = "L'ASCENSION",         bpm = 150f,    offset = 0.180f, preview = 96f },
            new() { song = "Song_06_givre",  clip = "n6_givre_boss2.wav",       chart = "n6_givre.json",             title = "BOSS BATTLE #2",      bpm = 168f,    offset = 0.021f, preview = 48f },
            new() { song = "Song_08_orage",  clip = "n8_orage.ogg",             chart = "n7_orage.json",             title = "LE GRAND ORAGE",      bpm = 180f,    offset = 0.035f, preview = 96f },
            new() { song = "Song_07_cite",   clip = "n8_cite_miku.wav",         chart = "n8_cite.json",              title = "ATAMA NO TAISOU",     bpm = 240f,    offset = 0.461f, preview = 16f },
            new() { song = "Song_09_sourd",  clip = "n9_sourdine.ogg",          chart = "n9_sourdine.json",          title = "LA SOURDINE",         bpm = 280f,    offset = 0.028f, preview = 72f },
        };

        // Ordre de progression dans LevelCatalog (1 → 10) : Orage passe en #7, Cité Sourde en #8, Secret « ??? » en #10.
        static readonly SlotEntry[] CatalogSlots =
        {
            new() { levelAsset = "Level_01_sylve",  id = "sylve",  number = 1,  stars = 1, biomeAsset = "Biome_01_sylve",  songAsset = "Song_01_sylve",  thumb = "thumb_sylve" },
            new() { levelAsset = "Level_02_bayou",  id = "bayou",  number = 2,  stars = 2, biomeAsset = "Biome_02_bayou",  songAsset = "Song_02_bayou",  thumb = "thumb_bayou" },
            new() { levelAsset = "Level_03_echos",  id = "echos",  number = 3,  stars = 3, biomeAsset = "Biome_03_echos",  songAsset = "Song_03_echos",  thumb = "thumb_echos" },
            new() { levelAsset = "Level_04_forge",  id = "forge",  number = 4,  stars = 4, biomeAsset = "Biome_04_forge",  songAsset = "Song_04_forge",  thumb = "thumb_forge" },
            new() { levelAsset = "Level_05_echine", id = "echine", number = 5,  stars = 4, biomeAsset = "Biome_05_echine", songAsset = "Song_05_echine", thumb = "thumb_echine" },
            new() { levelAsset = "Level_06_givre",  id = "givre",  number = 6,  stars = 4, biomeAsset = "Biome_06_givre",  songAsset = "Song_06_givre",  thumb = "thumb_givre" },
            new() { levelAsset = "Level_08_orage",  id = "orage",  number = 7,  stars = 5, biomeAsset = "Biome_08_orage",  songAsset = "Song_08_orage",  thumb = "thumb_orage" },
            new() { levelAsset = "Level_07_cite",   id = "cite",   number = 8,  stars = 5, biomeAsset = "Biome_07_cite",   songAsset = "Song_07_cite",   thumb = "thumb_cite" },
            new() { levelAsset = "Level_09_sourd",  id = "sourd",  number = 9,  stars = 5, biomeAsset = "Biome_09_sourd",  songAsset = "Song_09_sourd",  thumb = "thumb_sourd" },
            new() { levelAsset = "Level_10_secret", id = "secret", number = 10, stars = 5, biomeAsset = "Biome_09_sourd",  songAsset = null,             thumb = "thumb_sourd", lockedSecret = true },
        };

        [MenuItem("Kenta/Brancher les morceaux des niveaux")]
        public static void Apply()
        {
            AssetDatabase.Refresh();
            if (AssetDatabase.LoadAssetAtPath<TextAsset>(Charts + "n8_orage.json") != null)
                AssetDatabase.DeleteAsset(Charts + "n8_orage.json");

            int songsDone = 0;
            foreach (var e in SongEntries)
            {
                var song = LoadOrCreate<SongData>(Songs + e.song + ".asset");
                SetMusicImport(Music + e.clip);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Music + e.clip);
                var chart = AssetDatabase.LoadAssetAtPath<TextAsset>(Charts + e.chart);
                if (clip == null || chart == null)
                {
                    Debug.LogError($"[KentaLevelSongs] {e.song} : clip ou chart introuvable ({Music + e.clip}, {Charts + e.chart}).");
                    continue;
                }
                Undo.RecordObject(song, "Brancher le morceau du niveau");
                song.title = e.title;
                song.clip = clip;
                song.remixClip = null;
                song.bpm = e.bpm;
                song.offsetSeconds = e.offset;
                song.beatsPerBar = 4;
                song.chart = chart;
                song.previewBeat = e.preview;
                EditorUtility.SetDirty(song);
                songsDone++;
            }

            var catalogLevels = new LevelDef[CatalogSlots.Length];
            for (int i = 0; i < CatalogSlots.Length; i++)
            {
                var s = CatalogSlots[i];
                var lvl = LoadOrCreate<LevelDef>(LevelsDir + s.levelAsset + ".asset");
                Undo.RecordObject(lvl, "Configurer le niveau");
                lvl.id = s.id;
                lvl.number = s.number;
                lvl.stars = s.stars;
                lvl.biome = AssetDatabase.LoadAssetAtPath<BiomeTheme>(BiomesDir + s.biomeAsset + ".asset");
                lvl.song = !string.IsNullOrEmpty(s.songAsset) ? AssetDatabase.LoadAssetAtPath<SongData>(Songs + s.songAsset + ".asset") : null;
                lvl.chart = null;
                lvl.thumbnail = AssetDatabase.LoadAssetAtPath<Sprite>(Art + $"Gen/ui/{s.thumb}.png");
                lvl.lockedBySourdineHard = s.lockedSecret;
                EditorUtility.SetDirty(lvl);
                catalogLevels[i] = lvl;
            }

            var catalog = LoadOrCreate<LevelCatalog>(SoDir + "LevelCatalog.asset");
            Undo.RecordObject(catalog, "Mettre à jour LevelCatalog");
            catalog.levels = catalogLevels;
            EditorUtility.SetDirty(catalog);

            AssetDatabase.SaveAssets();
            Debug.Log($"[KentaLevelSongs] {songsDone}/{SongEntries.Length} morceaux et {catalogLevels.Length} niveaux branchés.");
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null)
            {
                a = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(a, path);
            }
            return a;
        }

        /// <summary>Réglages des musiques (game/CLAUDE.md) : Vorbis, compressé en mémoire, préchargé.</summary>
        static void SetMusicImport(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter imp)) return;
            var s = imp.defaultSampleSettings;
            if (s.loadType == AudioClipLoadType.CompressedInMemory && s.compressionFormat == AudioCompressionFormat.Vorbis
                && s.preloadAudioData) return;
            s.loadType = AudioClipLoadType.CompressedInMemory;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.7f;
            s.preloadAudioData = true;
            imp.defaultSampleSettings = s;
            imp.SaveAndReimport();
        }
    }
}
