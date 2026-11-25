using System;
using System.Collections;
using System.Collections.Generic;
using Anatidae;
using UnityEngine;

namespace Kenta.Core
{
    [Serializable]
    public struct LevelScoreEntry
    {
        public string name;
        public int score;
    }

    /// <summary>
    /// Classement de chaque niveau (top 10, une entrée par nom), stocké dans l'Extradata de la borne : une clé
    /// « top_&lt;id du niveau&gt; » par niveau, valeur JSON (ADR-030). La liste officielle de la borne
    /// (HighscoreManager) reste remplie à part. Sans la borne (éditeur, serveur absent), tout reste en mémoire et
    /// aucun appel réseau n'est fait : pas d'erreur en console.
    /// </summary>
    public static class LevelScores
    {
        public const int Max = 10;
        public const string KeyPrefix = "top_";

        [Serializable]
        class Wrapper
        {
            public List<LevelScoreEntry> entries;
        }

        static readonly Dictionary<string, List<LevelScoreEntry>> cache = new();
        static readonly List<LevelScoreEntry> Empty = new();

        /// <summary>Vrai une fois l'Extradata de la borne lue : les nouveaux scores y sont alors envoyés.</summary>
        public static bool Online { get; private set; }

        /// <summary>Incrémenté à chaque changement d'un classement (l'affichage se rafraîchit).</summary>
        public static int Version { get; private set; }

        public static IReadOnlyList<LevelScoreEntry> Top(string levelId) =>
            levelId != null && cache.TryGetValue(levelId, out var l) ? l : Empty;

        public static bool IsTopTen(string levelId, int score)
        {
            if (score <= 0) return false;
            var l = Top(levelId);
            return l.Count < Max || score > l[l.Count - 1].score;
        }

        /// <summary>Ajoute un score en mémoire (l'envoi à la borne se fait par <see cref="Upload"/>).</summary>
        public static void Record(string levelId, string name, int score)
        {
            if (string.IsNullOrEmpty(levelId)) return;
            cache.TryGetValue(levelId, out var l);
            cache[levelId] = Insert(l, name, score);
            Version++;
        }

        /// <summary>Envoie le classement d'un niveau à la borne ; ne fait rien hors ligne.</summary>
        public static IEnumerator Upload(string levelId)
        {
            if (!Online || string.IsNullOrEmpty(levelId)) yield break;
            cache.TryGetValue(levelId, out var l);
            yield return ExtradataManager.SetExtraData(KeyPrefix + levelId, ToJson(l));
        }

        /// <summary>Lit l'Extradata de la borne et charge tous les classements « top_* ». À lancer seulement si la borne répond.</summary>
        public static IEnumerator FetchFromBorne()
        {
            yield return ExtradataManager.FetchExtraData();
            if (!ExtradataManager.HasFetchedExtraData) yield break;
            foreach (var e in ExtradataManager.ExtraData)
            {
                if (e.key == null || !e.key.StartsWith(KeyPrefix)) continue;
                cache[e.key.Substring(KeyPrefix.Length)] = FromJson(e.value);
            }
            Online = true;
            Version++;
        }

        /// <summary>Nouvelle liste : tri décroissant, une entrée par nom (la meilleure), <see cref="Max"/> au plus.</summary>
        public static List<LevelScoreEntry> Insert(List<LevelScoreEntry> list, string name, int score)
        {
            if (string.IsNullOrEmpty(name)) name = "---";
            var result = list != null ? new List<LevelScoreEntry>(list) : new List<LevelScoreEntry>();
            int i = result.FindIndex(e => e.name == name);
            if (i >= 0)
            {
                if (result[i].score >= score) return result;
                result.RemoveAt(i);
            }
            result.Add(new LevelScoreEntry { name = name, score = score });
            result.Sort((a, b) => b.score.CompareTo(a.score));
            if (result.Count > Max) result.RemoveRange(Max, result.Count - Max);
            return result;
        }

        public static string ToJson(List<LevelScoreEntry> list) =>
            JsonUtility.ToJson(new Wrapper { entries = list ?? new List<LevelScoreEntry>() });

        public static List<LevelScoreEntry> FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return new List<LevelScoreEntry>();
            try
            {
                var w = JsonUtility.FromJson<Wrapper>(json);
                return w?.entries != null ? new List<LevelScoreEntry>(w.entries) : new List<LevelScoreEntry>();
            }
            catch (Exception)
            {
                return new List<LevelScoreEntry>();
            }
        }

        public static void ResetForTests()
        {
            cache.Clear();
            Online = false;
            Version = 0;
        }
    }
}
