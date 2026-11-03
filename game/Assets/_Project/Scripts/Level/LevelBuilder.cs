using System.Collections.Generic;
using Kenta.DebugTools;
using Kenta.FX;
using UnityEngine;

namespace Kenta.Level
{
    /// <summary>
    /// Construit le niveau à partir d'une chart : visuels (sprites teintés du LevelTheme) + données de collision.
    /// Position monde : x = beat * secondesParBeat * runSpeed (invariant n°2). Le beat d'un événement est le
    /// moment où le joueur doit agir ; l'obstacle est placé juste après (voir constantes ci-dessous).
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        // Décalages (en beats) entre le beat d'action et l'obstacle : ils définissent la fenêtre de tolérance.
        const float GapStartBeats = 0.2f;
        const float GapDefaultLength = 0.6f;
        const float WallStartBeats = 0.3f;
        const float EnemyCenterBeats = 0.4f;
        const float SlideStartBeats = 0.1f;
        const float SlideDefaultLength = 1f;
        const float SlideBarBottom = 0.62f;
        static readonly float[] LumLaneY = { 0.45f, 1.9f, 2.4f };

        [SerializeField] LevelTheme theme;

        public ChartData Chart { get; private set; }
        public LevelTheme Theme => theme;
        public float RunSpeed => Chart != null ? Chart.runSpeed : 8f;

        /// <summary>Obstacles (tout sauf les lums), triés par bounds.xMin.</summary>
        public readonly List<LevelObject> Obstacles = new();
        public readonly List<LevelObject> Lums = new();
        /// <summary>Événements visuels de la chart (fx_flash, camera_shake, camera_zoom, section_change), triés par beat.</summary>
        public readonly List<ChartEvent> FxEvents = new();
        // Données d'autoplay (debug + démo de l'écran Attract).
        public readonly List<float> AutoJumpBeats = new();
        public readonly List<float> AutoHitBeats = new();
        public readonly List<Vector2> AutoSlideSpans = new();

        readonly List<Vector2> gaps = new();
        float secondsPerBeat = 0.5f;
        Transform root;
        GameObject beatGrid;

        public float BeatToX(double beat) => (float)(beat * secondsPerBeat * RunSpeed);

        public void Build(ChartData chart, float bpm)
        {
            Clear();
            Chart = chart;
            secondsPerBeat = 60f / bpm;
            if (!Mathf.Approximately(chart.bpm, bpm))
                Debug.LogWarning($"LevelBuilder: BPM de la chart ({chart.bpm}) ≠ BPM du SongData ({bpm}). Le SongData fait foi.");

            root = new GameObject("Built").transform;
            root.SetParent(transform, false);

            float lastX = 0f;
            foreach (var e in chart.events)
            {
                var p = e.@params;
                switch (e.type)
                {
                    case "gap":
                    {
                        float len = p.length > 0 ? p.length : GapDefaultLength;
                        float x0 = BeatToX(e.beat + GapStartBeats), x1 = BeatToX(e.beat + GapStartBeats + len);
                        gaps.Add(new Vector2(x0, x1));
                        Obstacles.Add(new LevelObject { kind = LevelObjectKind.Gap, beat = e.beat, bounds = Rect.MinMaxRect(x0, -50f, x1, -1f) });
                        AutoJumpBeats.Add(e.beat);
                        lastX = Mathf.Max(lastX, x1);
                        break;
                    }
                    case "wall":
                    {
                        float x0 = BeatToX(e.beat + WallStartBeats);   // le sprite du mur fait 36×32 px : hauteur fixe de 0,8 u
                        var o = new LevelObject { kind = LevelObjectKind.Wall, beat = e.beat, bounds = new Rect(x0, 0f, 0.9f, 0.8f) };
                        o.view = MakeWall(o.bounds, BiomeAt(e.beat));
                        Obstacles.Add(o);
                        AutoJumpBeats.Add(e.beat);
                        lastX = Mathf.Max(lastX, o.bounds.xMax);
                        break;
                    }
                    case "enemy":
                    case "block":
                    {
                        bool enemy = e.type == "enemy";
                        float cx = BeatToX(e.beat + EnemyCenterBeats);
                        var o = new LevelObject
                        {
                            kind = enemy ? LevelObjectKind.Enemy : LevelObjectKind.Block,
                            beat = e.beat,
                            bounds = enemy ? new Rect(cx - 0.4f, 0f, 0.8f, 0.9f) : new Rect(cx - 0.45f, 0f, 0.9f, 0.95f),
                        };
                        o.view = enemy ? MakeEnemy(cx, BiomeAt(e.beat)) : MakeBlock(cx, BiomeAt(e.beat));
                        Obstacles.Add(o);
                        AutoHitBeats.Add(e.beat);
                        lastX = Mathf.Max(lastX, o.bounds.xMax);
                        break;
                    }
                    case "slide_bar":
                    {
                        float len = p.length > 0 ? p.length : SlideDefaultLength;
                        float x0 = BeatToX(e.beat + SlideStartBeats), x1 = BeatToX(e.beat + SlideStartBeats + len);
                        var o = new LevelObject { kind = LevelObjectKind.SlideBar, beat = e.beat, bounds = Rect.MinMaxRect(x0, SlideBarBottom, x1, 8f) };
                        o.view = MakeSlideBar(o.bounds, BiomeAt(e.beat));
                        Obstacles.Add(o);
                        AutoSlideSpans.Add(new Vector2(e.beat - 0.05f, e.beat + SlideStartBeats + len + 0.35f));
                        lastX = Mathf.Max(lastX, x1);
                        break;
                    }
                    case "lum_line":
                    {
                        int count = p.count > 0 ? p.count : 4;
                        float step = p.step > 0 ? p.step : 0.5f;
                        float y = LumLaneY[Mathf.Clamp(e.lane, 0, LumLaneY.Length - 1)];
                        for (int i = 0; i < count; i++)
                        {
                            float x = BeatToX(e.beat + i * step);
                            var o = new LevelObject { kind = LevelObjectKind.Lum, beat = e.beat + i * step, sequenceIndex = i, bounds = new Rect(x - 0.3f, y - 0.3f, 0.6f, 0.6f) };
                            o.view = MakeLum(new Vector2(x, y));
                            Lums.Add(o);
                            lastX = Mathf.Max(lastX, x);
                        }
                        break;
                    }
                    case "fx_flash":
                    case "camera_shake":
                    case "camera_zoom":
                    case "section_change":
                        FxEvents.Add(e);
                        break;
                    default:
                        Debug.LogWarning($"LevelBuilder: type d'événement non géré « {e.type} » (beat {e.beat}).");
                        break;
                }
            }

            Obstacles.Sort((a, b) => a.bounds.xMin.CompareTo(b.bounds.xMin));
            Lums.Sort((a, b) => a.bounds.xMin.CompareTo(b.bounds.xMin));
            gaps.Sort((a, b) => a.x.CompareTo(b.x));
            AutoJumpBeats.Sort();
            AutoHitBeats.Sort();

            float lastBeat = chart.events.Length > 0 ? chart.events[^1].beat : 0f;
            BuildGround(lastX + 60f);
            BuildBeatGrid(Mathf.CeilToInt(lastBeat) + 16);
            BuildCheckpoints();
        }

        public void Clear()
        {
            if (root != null) Destroy(root.gameObject);
            root = null;
            beatGrid = null;
            Obstacles.Clear();
            Lums.Clear();
            FxEvents.Clear();
            AutoJumpBeats.Clear();
            AutoHitBeats.Clear();
            AutoSlideSpans.Clear();
            gaps.Clear();
        }

        /// <summary>Respawn : réactive tout ce qui est après le checkpoint.</summary>
        public void ResetFrom(float beat)
        {
            foreach (var o in Obstacles) if (o.beat >= beat) o.ResetState();
            foreach (var o in Lums) if (o.beat >= beat) o.ResetState();
        }

        /// <summary>Vrai si le corps du joueur (x ± halfWidth) est entièrement au-dessus d'un trou.</summary>
        public bool IsOverGap(float x, float halfWidth)
        {
            for (int i = 0; i < gaps.Count; i++)
            {
                var g = gaps[i];
                if (g.x > x) break;
                if (x - halfWidth >= g.x && x + halfWidth <= g.y) return true;
            }
            return false;
        }

        /// <summary>Premier index de la liste dont le bord droit est après x (pour repositionner les curseurs).</summary>
        public static int FirstIndexAfter(List<LevelObject> list, float x)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].bounds.xMax >= x) return i;
            return list.Count;
        }

        void Update()
        {
            if (beatGrid != null && beatGrid.activeSelf != RhythmDebugOverlay.Visible)
                beatGrid.SetActive(RhythmDebugOverlay.Visible);
        }

        // ---------- Visuels (pixel art : sprites des biomes, 40 px par unité) ----------

        const float TileHeight = 0.5f;          // tuile de sol de 32×20 px
        const float CeilingBottom = SlideBarBottom;

        /// <summary>Biome de la section qui contient ce beat (un biome par section de la chart).</summary>
        public BiomeTheme BiomeAt(double beat)
        {
            int i = Chart != null ? Chart.SectionIndexAt(beat) : 0;
            return theme.biomes[Mathf.Clamp(i, 0, theme.biomes.Length - 1)];
        }

        public int BiomeIndexAt(double beat) => Chart != null ? Mathf.Clamp(Chart.SectionIndexAt(beat), 0, theme.biomes.Length - 1) : 0;

        BiomeTheme BiomeAtX(float x) => BiomeAt(x / (secondsPerBeat * RunSpeed));

        SpriteRenderer Spr(Transform parent, string name, Sprite sprite, Vector2 pos, int order, float scale = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Sprite répété (tuile) sur `size` unités : sol, trous, poutres.</summary>
        SpriteRenderer Tiled(Transform parent, string name, Sprite sprite, Vector2 center, Vector2 size, int order)
        {
            var sr = Spr(parent, name, sprite, center, order);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = size;
            return sr;
        }

        SpriteRenderer Box(Transform parent, string name, Color color, Vector2 center, Vector2 size, int order)
        {
            var sr = Spr(parent, name, theme.pixel, center, order);
            sr.color = color;
            sr.transform.localScale = new Vector3(size.x * PixelScreen.Ppu, size.y * PixelScreen.Ppu, 1f);
            return sr;
        }

        Transform Group(string name, Vector2 position)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            t.localPosition = position;
            return t;
        }

        void BuildGround(float endX)
        {
            var g = Group("Ground", Vector2.zero);
            // intervalles de sol plein, coupés aux frontières de biome pour changer de tuile
            var cuts = new List<float>();
            for (int i = 1; i < Chart.sections.Length; i++) cuts.Add(BeatToX(Chart.sections[i].startBeat));
            void Piece(float x0, float x1)
            {
                var from = x0;
                foreach (float c in cuts)
                {
                    if (c <= from || c >= x1) continue;
                    Emit(from, c);
                    from = c;
                }
                Emit(from, x1);
            }
            void Emit(float x0, float x1)
            {
                if (x1 - x0 < 0.01f) return;
                var biome = BiomeAtX((x0 + x1) * 0.5f);
                Tiled(g, "Ground", biome.ground, new Vector2((x0 + x1) * 0.5f, -TileHeight * 0.5f), new Vector2(x1 - x0, TileHeight), 0);
            }
            float cursor = -40f;
            foreach (var gap in gaps)
            {
                Piece(cursor, gap.x);
                cursor = Mathf.Max(cursor, gap.y);
                var biome = BiomeAtX((gap.x + gap.y) * 0.5f);
                Tiled(g, "Hazard", biome.hazard, new Vector2((gap.x + gap.y) * 0.5f, -TileHeight * 0.5f), new Vector2(gap.y - gap.x, TileHeight), 0);
            }
            Piece(cursor, endX);
        }

        void BuildBeatGrid(int lastBeat)
        {
            // Grille de beats : uniquement pour le debug (F9), comme les repères de la maquette n'existent pas dans la DA.
            beatGrid = Group("BeatGrid (debug)", Vector2.zero).gameObject;
            int perBar = Chart.beatsPerBar > 0 ? Chart.beatsPerBar : 4;
            for (int b = 0; b <= lastBeat; b++)
            {
                bool bar = b % perBar == 0;
                Box(beatGrid.transform, "Line", new Color(1f, 1f, 1f, bar ? 0.35f : 0.15f), new Vector2(BeatToX(b), 1.2f), new Vector2(0.025f, 3.4f), 50);
            }
            beatGrid.SetActive(RhythmDebugOverlay.Visible);
        }

        void BuildCheckpoints()
        {
            // Au début de chaque biome : le gardien miniature, qui « s'éveille » à la frontière (drop).
            var g = Group("Totems", Vector2.zero);
            for (int i = 1; i < Chart.sections.Length; i++)
                Spr(g, "Totem", theme.totem, new Vector2(BeatToX(Chart.sections[i].startBeat) - 0.6f, 0f), 4);
        }

        GameObject MakeWall(Rect r, BiomeTheme biome)
        {
            var t = Group("Wall", new Vector2(r.center.x, 0f));
            Spr(t, "Sprite", biome.wall, Vector2.zero, 10);
            return t.gameObject;
        }

        GameObject MakeEnemy(float cx, BiomeTheme biome)
        {
            var t = Group("Enemy", new Vector2(cx, 0f));
            var frames = biome.enemyFrames;
            var sr = Spr(t, "Sprite", frames[0], Vector2.zero, 10);
            if (frames.Length > 1) t.gameObject.AddComponent<BeatFrames>().Setup(sr, frames);   // djembé : écrasé / étiré
            else sr.gameObject.AddComponent<BeatHop>().Configure(1);                           // les autres sautillent d'1 px
            return t.gameObject;
        }

        GameObject MakeBlock(float cx, BiomeTheme biome)
        {
            var t = Group("Block", new Vector2(cx, 0f));
            Spr(t, "Sprite", biome.block, Vector2.zero, 10);
            return t.gameObject;
        }

        GameObject MakeSlideBar(Rect r, BiomeTheme biome)
        {
            var t = Group("Ceiling", new Vector2(r.center.x, 0f));
            const float beam = 0.4f;                                  // tuile de 16 px
            float postTop = 3.0f;
            float postBottom = CeilingBottom + beam;
            foreach (float sign in new[] { -1f, 1f })
                Tiled(t, "Post", biome.post, new Vector2(sign * (r.width * 0.5f - 0.1f), (postTop + postBottom) * 0.5f), new Vector2(0.2f, postTop - postBottom), 9);
            Tiled(t, "Beam", biome.ceiling, new Vector2(0f, CeilingBottom + beam * 0.5f), new Vector2(r.width, beam), 11);
            return t.gameObject;
        }

        GameObject MakeLum(Vector2 center)
        {
            var t = Group("Coin", center);
            var sr = Spr(t, "Sprite", theme.coinFrames[0], Vector2.zero, 14);
            sr.gameObject.AddComponent<SpriteCycle>().Setup(theme.coinFrames);
            return t.gameObject;
        }
    }
}
