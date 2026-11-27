using Kenta.Rhythm;
using UnityEditor;
using UnityEngine;

namespace Kenta.EditorTools
{
    /// <summary>
    /// Branche le morceau et la chart de chaque niveau écrit (ADR-035) dans son SongData (`Song_0N_&lt;biome&gt;`),
    /// sans passer par l'inspecteur : clip, chart, tempo, offset, titre, extrait de l'écran des niveaux, et réglages
    /// d'import du clip (Vorbis, compressé en mémoire, préchargé). Les valeurs viennent de la fiche du niveau
    /// (`tools/levels/&lt;niveau&gt;.py`). Menu : Kenta &gt; Brancher les morceaux des niveaux. Rejouable sans risque.
    /// </summary>
    public static class KentaLevelSongs
    {
        const string Songs = "Assets/_Project/ScriptableObjects/Songs/";
        const string Music = "Assets/_Project/Audio/Music/";
        const string Charts = "Assets/_Project/Charts/";

        struct Entry
        {
            public string song, clip, chart, title;
            public float bpm, offset, preview;
        }

        // Un niveau par ligne, dans l'ordre. Ajouter une ligne quand un niveau reçoit son morceau.
        static readonly Entry[] Levels =
        {
            new Entry { song = "Song_04_forge", clip = "n4_forge_fire_maze.ogg", chart = "n4_forge.json",
                        title = "FIRE MAZE", bpm = 151.29f, offset = 0.026f, preview = 80f },
        };

        [MenuItem("Kenta/Brancher les morceaux des niveaux")]
        public static void Apply()
        {
            int done = 0;
            foreach (var e in Levels)
            {
                var song = AssetDatabase.LoadAssetAtPath<SongData>(Songs + e.song + ".asset");
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Music + e.clip);
                var chart = AssetDatabase.LoadAssetAtPath<TextAsset>(Charts + e.chart);
                if (song == null || clip == null || chart == null)
                {
                    Debug.LogError($"[KentaLevelSongs] {e.song} : SongData, clip ou chart introuvable " +
                                   $"({Music + e.clip}, {Charts + e.chart}).");
                    continue;
                }
                SetMusicImport(Music + e.clip);
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
                done++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[KentaLevelSongs] {done}/{Levels.Length} morceau(x) de niveau branché(s).");
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
