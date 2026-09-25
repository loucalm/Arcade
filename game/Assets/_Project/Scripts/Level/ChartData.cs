using System;
using UnityEngine;

namespace RythmeRunner.Level
{
    /// <summary>Paramètres optionnels d'un événement (JsonUtility : champs fixes, 0/null si absents).</summary>
    [Serializable]
    public class ChartParams
    {
        public float length;
        public int count;
        public float step;
        public string variant;
        public float intensity;
        public float height;
    }

    [Serializable]
    public class ChartEvent
    {
        public float beat;
        public string type;
        public int lane;
        public ChartParams @params;
    }

    [Serializable]
    public class ChartSection
    {
        public string name;
        public float startBeat;
    }

    /// <summary>Chart d'un niveau. Format : docs/chart-format.md.</summary>
    [Serializable]
    public class ChartData
    {
        public string id;
        public string title;
        public float bpm;
        public float offsetMs;
        public int beatsPerBar = 4;
        public float runSpeed = 8f;
        public ChartSection[] sections;
        public ChartEvent[] events;

        public static ChartData Parse(TextAsset asset)
        {
            var chart = JsonUtility.FromJson<ChartData>(asset.text);
            chart.sections ??= new[] { new ChartSection { name = "début", startBeat = 0 } };
            chart.events ??= Array.Empty<ChartEvent>();
            foreach (var e in chart.events) e.@params ??= new ChartParams();
            Array.Sort(chart.sections, (a, b) => a.startBeat.CompareTo(b.startBeat));
            Array.Sort(chart.events, (a, b) => a.beat.CompareTo(b.beat));
            if (chart.runSpeed <= 0) chart.runSpeed = 8f;
            return chart;
        }

        /// <summary>Index de la section contenant ce beat (checkpoint courant).</summary>
        public int SectionIndexAt(double beat)
        {
            int index = 0;
            for (int i = 0; i < sections.Length; i++)
                if (beat >= sections[i].startBeat) index = i;
            return index;
        }
    }
}
