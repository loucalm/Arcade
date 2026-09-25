using System.Collections.Generic;
using RythmeRunner.DebugTools;
using RythmeRunner.FX;
using UnityEngine;

namespace RythmeRunner.Level
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
        const float HookDefaultLength = 2f;
        const float WallRunDefaultLength = 4f;
        static readonly float[] LumLaneY = { 0.45f, 2.0f, 3.1f };

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
                        float h = p.height > 0 ? p.height : 0.8f;
                        float x0 = BeatToX(e.beat + WallStartBeats);
                        var o = new LevelObject { kind = LevelObjectKind.Wall, beat = e.beat, bounds = new Rect(x0, 0f, 0.9f, h) };
                        o.view = MakeWall(o.bounds);
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
                            bounds = new Rect(cx - 0.35f, 0f, 0.7f, 0.8f),
                        };
                        o.view = enemy ? MakeEnemy(cx, e.beat) : MakeBlock(cx, e.beat);
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
                        o.view = MakeSlideBar(o.bounds, e.beat);
                        Obstacles.Add(o);
                        AutoSlideSpans.Add(new Vector2(e.beat - 0.05f, e.beat + SlideStartBeats + len + 0.35f));
                        lastX = Mathf.Max(lastX, x1);
                        break;
                    }
                    case "hook":
                    {
                        float len = Mathf.Max(1.5f, p.length > 0 ? p.length : HookDefaultLength);
                        float x0 = BeatToX(e.beat), x1 = BeatToX(e.beat + len);
                        gaps.Add(new Vector2(BeatToX(e.beat + 0.2f), BeatToX(e.beat + len + 0.5f)));
                        var o = new LevelObject { kind = LevelObjectKind.Hook, beat = e.beat, startBeat = e.beat, endBeat = e.beat + len, trajectoryY0 = 0f, bounds = Rect.MinMaxRect(x0, -50f, x1, 8f) };
                        o.view = MakeHook(o);
                        Obstacles.Add(o);
                        AddTrajectoryLums(o, p.count, true);
                        lastX = Mathf.Max(lastX, x1);
                        break;
                    }
                    case "wall_run":
                    {
                        float len = Mathf.Max(2f, p.length > 0 ? p.length : WallRunDefaultLength);
                        float x0 = BeatToX(e.beat), x1 = BeatToX(e.beat + len);
                        var o = new LevelObject { kind = LevelObjectKind.WallRun, beat = e.beat, startBeat = e.beat, endBeat = e.beat + len, bounds = Rect.MinMaxRect(x0, 0f, x1, 8f) };
                        o.view = MakeWallRun(o);
                        Obstacles.Add(o);
                        AddTrajectoryLums(o, p.count, false);
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
            BuildBeatTicks(Mathf.CeilToInt(lastBeat) + 16);
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

        // ---------- Visuels ----------

        SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Color color, Vector2 center, Vector2 size, int order, bool additive = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            if (additive && theme.additive != null) sr.sharedMaterial = theme.additive;
            // Les sprites de forme font 1×1 unité : l'échelle = la taille voulue.
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            return sr;
        }

        SpriteRenderer SpriteFit(Transform parent, string name, Sprite sprite, Vector2 center, float targetHeight, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = theme.spriteTint;
            sr.sortingOrder = order;
            go.transform.localScale = Vector3.one * (targetHeight / sprite.bounds.size.y);
            return sr;
        }

        SpriteRenderer SpriteTiled(Transform parent, string name, Sprite sprite, Vector2 center, Vector2 size, int order)
        {
            // En mode Tiled, la taille est en unités locales : échelle 1, sinon elle se multiplie (tuiles de 1 unité).
            var sr = SpriteFit(parent, name, sprite, center, 1f, order);
            sr.transform.localScale = Vector3.one;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
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
            float cursor = -40f;
            void Segment(float x0, float x1)
            {
                if (x1 - x0 < 0.01f) return;
                float w = x1 - x0, mid = (x0 + x1) * 0.5f;
                Sprite(g, "Body", theme.square, theme.groundBody, new Vector2(mid, -4f), new Vector2(w, 8f), 0);
                for (int row = 0; row < 4; row++)
                {
                    float y = -1.1f - row * 1.55f;
                    float brickWidth = 1.25f;
                    float offset = row % 2 == 0 ? 0f : brickWidth * 0.5f;
                    for (float x = x0 - offset; x < x1; x += brickWidth)
                        Sprite(g, "Stone", theme.square, new Color(0.55f, 0.42f, 0.75f, 0.12f), new Vector2(x + brickWidth * 0.5f, y), new Vector2(brickWidth - 0.06f, 0.07f), 1);
                }
                SpriteRenderer top = Sprite(g, "Top", theme.square, theme.groundTopColor, new Vector2(mid, -0.07f), new Vector2(w, 0.14f), 2);
                Sprite(g, "TopGlow", theme.glow, new Color(theme.groundTopColor.r, theme.groundTopColor.g, theme.groundTopColor.b, 0.25f), new Vector2(mid, -0.07f), new Vector2(w + 1f, 0.9f), 1, true);
                var wave = g.gameObject.AddComponent<GroundWave>();
                wave.Configure(top, x0, x1, theme.additive);
                if (x0 > -39f)
                    Sprite(g, "Crenel", theme.square, new Color(theme.groundTopColor.r, theme.groundTopColor.g, theme.groundTopColor.b, 0.65f), new Vector2(x0 + 0.18f, 0.08f), new Vector2(0.28f, 0.16f), 3);
                if (x1 < endX)
                    Sprite(g, "Crenel", theme.square, new Color(theme.groundTopColor.r, theme.groundTopColor.g, theme.groundTopColor.b, 0.65f), new Vector2(x1 - 0.18f, 0.08f), new Vector2(0.28f, 0.16f), 3);
            }
            foreach (var gap in gaps)
            {
                Segment(cursor, gap.x);
                cursor = Mathf.Max(cursor, gap.y);
            }
            Segment(cursor, endX);
        }

        void BuildBeatTicks(int lastBeat)
        {
            var ticks = Group("BeatTicks", Vector2.zero);
            beatGrid = Group("BeatGrid (debug)", Vector2.zero).gameObject;
            int perBar = Chart.beatsPerBar > 0 ? Chart.beatsPerBar : 4;
            for (int b = 0; b <= lastBeat; b++)
            {
                float x = BeatToX(b);
                bool bar = b % perBar == 0;
                if (!IsOverGap(x, 0.05f))
                {
                    float h = bar ? 0.42f : 0.18f;
                    Sprite(ticks, "Tick", theme.square, new Color(theme.beatTick.r, theme.beatTick.g, theme.beatTick.b, bar ? 0.24f : 0.12f), new Vector2(x, -0.14f - h * 0.5f), new Vector2(bar ? 0.1f : 0.05f, h), 3);
                }
                Sprite(beatGrid.transform, "Line", theme.square, new Color(1f, 1f, 1f, bar ? 0.12f : 0.05f), new Vector2(x, 3f), new Vector2(0.03f, 10f), 50);
            }
            beatGrid.SetActive(RhythmDebugOverlay.Visible);
        }

        void BuildCheckpoints()
        {
            var g = Group("Checkpoints", Vector2.zero);
            for (int i = 1; i < Chart.sections.Length; i++)
            {
                float x = BeatToX(Chart.sections[i].startBeat);
                var c = theme.checkpoint;
                Sprite(g, "Pole", theme.square, new Color(c.r, c.g, c.b, 0.7f), new Vector2(x, 2.5f), new Vector2(0.08f, 5f), 4);
                var flag = Sprite(g, "Flag", theme.triangle, c, new Vector2(x + 0.3f, 4.8f), new Vector2(0.75f, 0.55f), 5);
                var flagDance = flag.gameObject.AddComponent<BeatDance>();
                flagDance.Configure(flag, 0f, 0.04f, 8f, 0.25f);
                Sprite(g, "Glow", theme.glow, new Color(c.r, c.g, c.b, 0.35f), new Vector2(x, 2.5f), new Vector2(1.2f, 6f), 3, true);
            }
        }

        GameObject MakeWall(Rect r)
        {
            var t = Group("Wall", new Vector2(r.center.x, 0f));
            Sprite(t, "Body", theme.square, theme.wall, new Vector2(0f, r.height * 0.5f), new Vector2(r.width, r.height), 10);
            for (float y = 0.25f; y < r.height; y += 0.42f)
                Sprite(t, "Brick", theme.square, new Color(1f, 0.65f, 0.9f, 0.3f), new Vector2(0f, y), new Vector2(r.width + 0.01f, 0.035f), 11);
            Sprite(t, "Top", theme.square, Color.white, new Vector2(0f, r.height - 0.05f), new Vector2(r.width, 0.1f), 12);
            Sprite(t, "Crenel", theme.square, theme.wall, new Vector2(-0.28f, r.height + 0.08f), new Vector2(0.22f, 0.16f), 12);
            Sprite(t, "Crenel", theme.square, theme.wall, new Vector2(0.28f, r.height + 0.08f), new Vector2(0.22f, 0.16f), 12);
            Sprite(t, "Glow", theme.glow, new Color(theme.wall.r, theme.wall.g, theme.wall.b, 0.35f), new Vector2(0f, r.height * 0.5f), new Vector2(r.width + 1.2f, r.height + 1.2f), 9, true);
            return t.gameObject;
        }

        GameObject MakeEnemy(float cx, float actionBeat)
        {
            var t = Group("Enemy", new Vector2(cx, 0f));
            Sprite(t, "Glow", theme.glow, new Color(theme.enemy.r, theme.enemy.g, theme.enemy.b, 0.4f), new Vector2(0f, 0.45f), new Vector2(1.8f, 1.8f), 9, true);
            var body = Sprite(t, "Body", theme.circle, theme.enemy, new Vector2(0f, 0.45f), new Vector2(0.9f, 0.9f), 10);
            Sprite(t, "HornL", theme.triangle, theme.enemy, new Vector2(-0.28f, 0.93f), new Vector2(0.24f, 0.32f), 11);
            Sprite(t, "HornR", theme.triangle, theme.enemy, new Vector2(0.28f, 0.93f), new Vector2(0.24f, 0.32f), 11);
            Sprite(t, "EyeL", theme.circle, Color.white, new Vector2(-0.2f, 0.58f), new Vector2(0.2f, 0.23f), 12);
            Sprite(t, "EyeR", theme.circle, Color.white, new Vector2(0.2f, 0.58f), new Vector2(0.2f, 0.23f), 12);
            var browL = Sprite(t, "BrowL", theme.square, Color.black, new Vector2(-0.2f, 0.72f), new Vector2(0.28f, 0.06f), 13);
            browL.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            var browR = Sprite(t, "BrowR", theme.square, Color.black, new Vector2(0.2f, 0.72f), new Vector2(0.28f, 0.06f), 13);
            browR.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            Sprite(t, "Mouth", theme.square, new Color(0.18f, 0f, 0.08f, 1f), new Vector2(0f, 0.26f), new Vector2(0.3f, 0.08f), 13);
            var dancer = t.gameObject.AddComponent<BeatDance>();
            dancer.Configure(body, 0.11f, 0.13f, 5f, 0f);
            var telegraph = t.gameObject.AddComponent<Telegraph>();
            telegraph.Configure(actionBeat, Chart.beatsPerBar, new[] { body }, null);
            return t.gameObject;
        }

        GameObject MakeBlock(float cx, float actionBeat)
        {
            var t = Group("Block", new Vector2(cx, 0f));
            var body = Sprite(t, "Body", theme.rounded, theme.block, new Vector2(0f, 0.47f), new Vector2(0.95f, 0.95f), 10);
            Sprite(t, "Frame", theme.square, new Color(1f, 0.85f, 0.5f, 0.8f), new Vector2(0f, 0.47f), new Vector2(0.78f, 0.07f), 11);
            Sprite(t, "CrossH", theme.square, new Color(0.55f, 0.2f, 0.05f, 0.7f), new Vector2(0f, 0.47f), new Vector2(0.72f, 0.09f), 12);
            Sprite(t, "CrossV", theme.square, new Color(0.55f, 0.2f, 0.05f, 0.7f), new Vector2(0f, 0.47f), new Vector2(0.09f, 0.72f), 12);
            Sprite(t, "RivetL", theme.circle, Color.white, new Vector2(-0.32f, 0.78f), new Vector2(0.08f, 0.08f), 13);
            Sprite(t, "RivetR", theme.circle, Color.white, new Vector2(0.32f, 0.78f), new Vector2(0.08f, 0.08f), 13);
            Sprite(t, "Glow", theme.glow, new Color(theme.block.r, theme.block.g, theme.block.b, 0.3f), new Vector2(0f, 0.47f), new Vector2(1.8f, 1.8f), 9, true);
            var telegraph = t.gameObject.AddComponent<Telegraph>();
            telegraph.Configure(actionBeat, Chart.beatsPerBar, new[] { body }, null);
            var dance = t.gameObject.AddComponent<BeatDance>();
            dance.Configure(body, 0.025f, 0.02f, 1.5f);
            return t.gameObject;
        }

        GameObject MakeSlideBar(Rect r, float actionBeat)
        {
            var t = Group("SlideBar", new Vector2(r.center.x, 0f));
            // Herse : poutre de fer au bord bas + chaînes jusqu'en haut, sur un voile sombre (on ne peut pas sauter par-dessus).
            Sprite(t, "Veil", theme.square, new Color(0.05f, 0.02f, 0.06f, 0.55f), new Vector2(0f, r.center.y), new Vector2(r.width, r.height), 9);
            Sprite(t, "Beam", theme.square, new Color(0.2f, 0.12f, 0.16f, 1f), new Vector2(0f, r.yMin + 0.45f), new Vector2(r.width, 0.9f), 10);
            for (float x = r.xMin + 0.35f; x < r.xMax; x += 0.9f)
                Sprite(t, "Chain", theme.square, new Color(0.45f, 0.35f, 0.4f, 0.9f), new Vector2(x - r.center.x, r.yMin + 0.9f + (r.yMax - r.yMin - 0.9f) * 0.5f), new Vector2(0.07f, r.yMax - r.yMin - 0.9f), 10);
            var bar = Sprite(t, "Edge", theme.square, theme.slideBar, new Vector2(0f, r.yMin + 0.08f), new Vector2(r.width, 0.16f), 11);
            for (float x = r.xMin + 0.3f; x < r.xMax - 0.2f; x += 0.7f)
                Sprite(t, "Spike", theme.triangle, theme.slideBar, new Vector2(x - r.center.x, r.yMin - 0.1f), new Vector2(0.22f, 0.32f), 12).transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            Sprite(t, "Glow", theme.glow, new Color(theme.slideBar.r, theme.slideBar.g, theme.slideBar.b, 0.45f), new Vector2(0f, r.yMin), new Vector2(r.width + 1f, 1f), 12, true);
            // Hachures d'avertissement
            for (float x = r.xMin + 0.3f; x < r.xMax - 0.2f; x += 0.7f)
                Sprite(t, "Stripe", theme.square, new Color(0f, 0f, 0f, 0.35f), new Vector2(x - r.center.x, r.yMin + 0.08f), new Vector2(0.25f, 0.16f), 12);
            var telegraph = t.gameObject.AddComponent<Telegraph>();
            telegraph.Configure(actionBeat, Chart.beatsPerBar, new[] { bar }, null);
            return t.gameObject;
        }

        void AddTrajectoryLums(LevelObject trajectory, int count, bool hook)
        {
            if (count <= 0) return;
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : (hook ? Mathf.Lerp(0.15f, 0.85f, i / (float)(count - 1)) : Mathf.Lerp(0.1f, 0.9f, i / (float)(count - 1)));
                double beat = Mathf.Lerp(trajectory.startBeat, trajectory.endBeat, t);
                float x = BeatToX(beat);
                float y = trajectory.EvaluateY(beat) + (hook ? 0f : 0.9f);
                var o = new LevelObject { kind = LevelObjectKind.Lum, beat = (float)beat, sequenceIndex = i, bounds = new Rect(x - 0.3f, y - 0.3f, 0.6f, 0.6f) };
                o.view = MakeLum(new Vector2(x, y));
                Lums.Add(o);
            }
        }

        GameObject MakeHook(LevelObject hook)
        {
            var t = Group("Hook", Vector2.zero);
            float x = BeatToX((hook.startBeat + hook.endBeat) * 0.5f);
            Sprite(t, "AnchorGlow", theme.glow, new Color(theme.lum.r, theme.lum.g, theme.lum.b, 0.45f), new Vector2(x, 6f), new Vector2(1.2f, 1.2f), 11, true);
            Sprite(t, "Anchor", theme.ring, theme.lum, new Vector2(x, 6f), new Vector2(0.72f, 0.72f), 12);
            var sparkle = Sprite(t, "Sparkle", theme.sparkle, Color.white, new Vector2(x, 6f), new Vector2(0.42f, 0.42f), 13, true);
            var sparkleDance = sparkle.gameObject.AddComponent<BeatDance>();
            sparkleDance.Configure(sparkle, 0f, 0f, 180f);
            float ropeHeight = 5.5f;
            Sprite(t, "Rope", theme.square, theme.wall, new Vector2(x, 6f - ropeHeight * 0.5f), new Vector2(0.08f, ropeHeight), 11);
            return t.gameObject;
        }

        GameObject MakeWallRun(LevelObject wallRun)
        {
            var t = Group("WallRun", Vector2.zero);
            float startX = BeatToX(wallRun.startBeat);
            float width = Mathf.Max(1f, BeatToX(wallRun.endBeat) - startX);
            int columns = Mathf.CeilToInt(width);
            for (int i = 0; i < columns; i++)
            {
                float x = startX + i + 0.5f;
                float beat = wallRun.startBeat + (x - startX) / width * (wallRun.endBeat - wallRun.startBeat);
                float height = Mathf.Max(0.1f, wallRun.EvaluateY(beat));
                Sprite(t, "Tile", theme.square, theme.wall, new Vector2(x, height * 0.5f), new Vector2(1f, height), 5);
                Sprite(t, "Top", theme.square, new Color(1f, 0.65f, 0.9f, 0.75f), new Vector2(x, height), new Vector2(1f, 0.08f), 6);
                float previousHeight = i > 0 ? Mathf.Max(0.1f, wallRun.EvaluateY(wallRun.startBeat + (x - 0.5f - startX) / width * (wallRun.endBeat - wallRun.startBeat))) : height;
                var edge = Sprite(t, "Edge", theme.square, theme.wall, new Vector2(x, height + 0.04f), new Vector2(1.05f, 0.06f), 7);
                edge.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(height - previousHeight, 1f) * Mathf.Rad2Deg);
            }
            return t.gameObject;
        }

        GameObject MakeLum(Vector2 center)
        {
            var t = Group("Lum", center);
            Sprite(t, "Glow", theme.glow, new Color(theme.lum.r, theme.lum.g, theme.lum.b, 0.55f), Vector2.zero, new Vector2(1.2f, 1.2f), 14, true);
            var core = Sprite(t, "Core", theme.circle, theme.lum, Vector2.zero, new Vector2(0.42f, 0.42f), 15);
            var leftWing = Sprite(t, "WingL", theme.triangle, new Color(1f, 0.8f, 0.3f, 0.9f), new Vector2(-0.28f, 0.02f), new Vector2(0.32f, 0.18f), 15);
            leftWing.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            var rightWing = Sprite(t, "WingR", theme.triangle, new Color(1f, 0.8f, 0.3f, 0.9f), new Vector2(0.28f, 0.02f), new Vector2(0.32f, 0.18f), 15);
            rightWing.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            var dance = core.gameObject.AddComponent<BeatDance>();
            dance.Configure(core, 0.05f, 0.1f, 0f);
            var wingDance = leftWing.gameObject.AddComponent<BeatDance>();
            wingDance.Configure(core, 0f, 0.18f, 22f, 0.25f);
            wingDance = rightWing.gameObject.AddComponent<BeatDance>();
            wingDance.Configure(core, 0f, 0.18f, -22f, 0.25f);
            var pulse = t.gameObject.AddComponent<BeatPulse>();
            pulse.Configure(0.25f, 0.4f);
            return t.gameObject;
        }
    }
}
