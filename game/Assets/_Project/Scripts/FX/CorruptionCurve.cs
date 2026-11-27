using System.Collections.Generic;
using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Une valeur 0..1 qui suit la chart (niveau 9 : corruption du monde, noir et blanc ; ADR-042). Chaque clé dit
    /// « à partir du temps `beat`, va vers `target` en `length` temps », en partant de la valeur à ce moment-là. Lue sur
    /// le temps du morceau (l'audio est l'horloge maître) : une reprise à un checkpoint retrouve d'elle-même la bonne
    /// valeur, sans rien rejouer. Pas d'allocation à la lecture.
    /// </summary>
    public class CorruptionCurve
    {
        struct Key
        {
            public double beat;
            public float target, length, from;
        }

        readonly List<Key> keys = new();

        public int Count => keys.Count;

        public void Clear() => keys.Clear();

        /// <summary>Ajoute une clé (dans n'importe quel ordre) ; `length` ≤ 0 = coupe nette.</summary>
        public void Add(double beat, float target, float length)
        {
            var k = new Key { beat = beat, target = Mathf.Clamp01(target), length = Mathf.Max(0f, length) };
            int i = keys.Count;
            while (i > 0 && keys[i - 1].beat > beat) i--;
            keys.Insert(i, k);
            // valeur de départ de chaque clé : celle que donnait la clé précédente à cet instant
            for (int j = 0; j < keys.Count; j++)
            {
                var kj = keys[j];
                kj.from = j == 0 ? 0f : ValueOf(keys[j - 1], kj.beat);
                keys[j] = kj;
            }
        }

        /// <summary>Valeur au temps `beat` (0 avant la première clé).</summary>
        public float Evaluate(double beat)
        {
            for (int i = keys.Count - 1; i >= 0; i--)
                if (keys[i].beat <= beat) return ValueOf(keys[i], beat);
            return 0f;
        }

        static float ValueOf(Key k, double beat)
        {
            if (k.length <= 0f) return k.target;
            float t = (float)((beat - k.beat) / k.length);
            return t >= 1f ? k.target : Mathf.Lerp(k.from, k.target, Mathf.Max(0f, t));
        }
    }
}
