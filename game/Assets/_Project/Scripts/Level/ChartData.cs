using System;
using UnityEngine;

namespace Kenta.Level
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
        /// <summary>camera : largeur de la vue en px de jeu (320, 384, 480 ou 640 : agrandissement entier ×6, ×5, ×4, ×3).</summary>
        public int zoom;
        /// <summary>camera : position du héros à l'écran, de gauche (0) à droite (1).</summary>
        public float frameX;
        /// <summary>camera : hauteur du sol sous le héros à l'écran, du bas (0) au haut (1).</summary>
        public float frameY;
        /// <summary>lum_at : la pièce joue une note (demi-tons par rapport au son de pièce, un la).</summary>
        public bool hasNote;
        public int note;
        /// <summary>screen_split : nombre de copies de l'image (1, 2, 5 ou 9).</summary>
        public int copies;
        /// <summary>vine : hauteur des pieds au lâcher (la prise est à `height`) et creux du balancier (u).</summary>
        public float releaseHeight;
        public float sag;
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
        /// <summary>Optionnel : nom de l'asset du biome de cette section (ex. « Biome_08_orage ») ; vide = biome du niveau.</summary>
        public string biome;
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
        /// <summary>
        /// Rail de caméra (optionnel) : hauteur du centre de la caméra tous les `railStep` beats, précalculée par le
        /// générateur (trajectoire idéale lissée dans les deux sens, héros toujours dans l'image).
        /// </summary>
        public float[] rail;
        public float railStep = 0.25f;
        /// <summary>Largeur de vue (px de jeu) pour laquelle le rail a été calculé (0 = défaut de la caméra).</summary>
        public int zoom;
        /// <summary>
        /// Points de reprise (beats) après une mort (die and retry, ADR-031) : choisis par le générateur là où le héros est
        /// au sol, sur le premier plan, gravité normale, au moins 2 temps avant la prochaine action. Triés.
        /// </summary>
        public float[] checkpoints;

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
