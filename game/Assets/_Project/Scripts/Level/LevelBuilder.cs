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
                if (theme.groundFill != null)
                    SpriteTiled(g, "Fill", theme.groundFill, new Vector2(mid, -5f), new Vector2(w, 8f), 0).color = theme.groundFillTint;
                else
                    Sprite(g, "Body", theme.square, theme.groundBody, new Vector2(mid, -4f), new Vector2(w, 8f), 0);
                if (theme.groundTop != null)
                    SpriteTiled(g, "Top", theme.groundTop, new Vector2(mid, -0.5f), new Vector2(w, 1f), 2);
                else
                    Sprite(g, "Top", theme.square, theme.groundTopColor, new Vector2(mid, -0.07f), new Vector2(w, 0.14f), 2);
                if (theme.groundTopLeft != null)
                    SpriteFit(g, "TopLeft", theme.groundTopLeft, new Vector2(x0 + 0.5f, -0.5f), 1f, 3);
                if (theme.groundTopRight != null)
                    SpriteFit(g, "TopRight", theme.groundTopRight, new Vector2(x1 - 0.5f, -0.5f), 1f, 3);
                Sprite(g, "TopGlow", theme.glow, new Color(theme.groundTopColor.r, theme.groundTopColor.g, theme.groundTopColor.b, 0.25f), new Vector2(mid, -0.07f), new Vector2(w + 1f, 0.9f), 1, true);
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
                    float h = bar ? 0.55f : 0.25f;
                    Sprite(ticks, "Tick", theme.square, theme.beatTick, new Vector2(x, -0.14f - h * 0.5f), new Vector2(bar ? 0.12f : 0.07f, h), 3);
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
                if (theme.checkpointFrames != null && theme.checkpointFrames.Length > 0 && theme.checkpointFrames[0] != null)
                {
                    var pole = SpriteFit(g, "Flag", theme.checkpointFrames[0], new Vector2(x, 0.8f), 1.6f, 4);
                    var flipbook = g.gameObject.AddComponent<BeatFlipbook>();
                    flipbook.Configure(pole, theme.checkpointFrames, 0.5f, 0f, null);
                }
                else
                {
                    Sprite(g, "Pole", theme.square, new Color(c.r, c.g, c.b, 0.6f), new Vector2(x, 2.5f), new Vector2(0.08f, 5f), 4);
                    Sprite(g, "Flag", theme.square, c, new Vector2(x + 0.3f, 4.8f), new Vector2(0.6f, 0.4f), 4);
                }
                Sprite(g, "Glow", theme.glow, new Color(c.r, c.g, c.b, 0.35f), new Vector2(x, 2.5f), new Vector2(1.2f, 6f), 3, true);
            }
        }

        GameObject MakeWall(Rect r)
        {
            var t = Group("Wall", new Vector2(r.center.x, 0f));
            if (theme.wallTile != null)
                SpriteTiled(t, "Body", theme.wallTile, new Vector2(0f, r.height * 0.5f), new Vector2(r.width, r.height), 10).color = theme.wall;
            else
                Sprite(t, "Body", theme.square, theme.wall, new Vector2(0f, r.height * 0.5f), new Vector2(r.width, r.height), 10);
            Sprite(t, "Top", theme.square, Color.white, new Vector2(0f, r.height - 0.05f), new Vector2(r.width, 0.1f), 11);
            Sprite(t, "Glow", theme.glow, new Color(theme.wall.r, theme.wall.g, theme.wall.b, 0.35f), new Vector2(0f, r.height * 0.5f), new Vector2(r.width + 1.2f, r.height + 1.2f), 9, true);
            return t.gameObject;
        }

        GameObject MakeEnemy(float cx, float actionBeat)
        {
            var t = Group("Enemy", new Vector2(cx, 0f));
            Sprite(t, "Glow", theme.glow, new Color(theme.enemy.r, theme.enemy.g, theme.enemy.b, 0.4f), new Vector2(0f, 0.45f), new Vector2(1.8f, 1.8f), 9, true);
            if (theme.enemyFrames != null && theme.enemyFrames.Length > 0 && theme.enemyFrames[0] != null)
            {
                var pop = new GameObject("Pop").transform;
                pop.SetParent(t, false);
                var dancer = new GameObject("Dancer").transform;
                dancer.SetParent(pop, false);
                var body = SpriteFit(dancer, "Body", theme.enemyFrames[0], new Vector2(0f, 0.45f), 0.95f, 10);
                var flipbook = t.gameObject.AddComponent<BeatFlipbook>();
                flipbook.Configure(body, theme.enemyFrames, 0.5f, 0.15f, dancer);
                var telegraph = t.gameObject.AddComponent<Telegraph>();
                telegraph.Configure(actionBeat, Chart.beatsPerBar, new[] { body }, pop);
                return t.gameObject;
            }
            Sprite(t, "Body", theme.square, theme.enemy, new Vector2(0f, 0.45f), new Vector2(0.9f, 0.9f), 10);
            Sprite(t, "EyeL", theme.square, Color.white, new Vector2(-0.2f, 0.58f), new Vector2(0.18f, 0.22f), 11);
            Sprite(t, "EyeR", theme.square, Color.white, new Vector2(0.2f, 0.58f), new Vector2(0.18f, 0.22f), 11);
            Sprite(t, "PupilL", theme.square, Color.black, new Vector2(-0.24f, 0.56f), new Vector2(0.08f, 0.12f), 12);
            Sprite(t, "PupilR", theme.square, Color.black, new Vector2(0.16f, 0.56f), new Vector2(0.08f, 0.12f), 12);
            var pulse = t.gameObject.AddComponent<BeatPulse>();
            pulse.Configure(0.12f, 0.25f);
            return t.gameObject;
        }

        GameObject MakeBlock(float cx, float actionBeat)
        {
            var t = Group("Block", new Vector2(cx, 0f));
            if (theme.blockFrames != null && theme.blockFrames.Length > 0 && theme.blockFrames[0] != null)
            {
                var pop = new GameObject("Pop").transform;
                pop.SetParent(t, false);
                var body = SpriteFit(pop, "Body", theme.blockFrames[0], new Vector2(0f, 0.47f), 0.95f, 10);
                var telegraph = t.gameObject.AddComponent<Telegraph>();
                telegraph.Configure(actionBeat, Chart.beatsPerBar, new[] { body }, pop);
                if (theme.blockFrames.Length > 1 && theme.blockFrames[1] != null)
                    telegraph.ConfigureActiveSprite(body, theme.blockFrames[1]);
                return t.gameObject;
            }
            Sprite(t, "Body", theme.square, theme.block, new Vector2(0f, 0.47f), new Vector2(0.95f, 0.95f), 10);
            Sprite(t, "Inner", theme.square, new Color(0f, 0f, 0f, 0.25f), new Vector2(0f, 0.47f), new Vector2(0.6f, 0.6f), 11);
            Sprite(t, "Glow", theme.glow, new Color(theme.block.r, theme.block.g, theme.block.b, 0.3f), new Vector2(0f, 0.47f), new Vector2(1.8f, 1.8f), 9, true);
            return t.gameObject;
        }

        GameObject MakeSlideBar(Rect r, float actionBeat)
        {
            var t = Group("SlideBar", new Vector2(r.center.x, 0f));
            if (theme.slideBarTile != null)
            {
                var bar = SpriteTiled(t, "Body", theme.slideBarTile, new Vector2(0f, r.yMin + 0.5f), new Vector2(r.width, 1f), 11);
                if (theme.chainTile != null)
                    SpriteTiled(t, "Chain", theme.chainTile, new Vector2(0f, r.yMin + 1f + (r.yMax - r.yMin - 1f) * 0.5f), new Vector2(1f, r.yMax - r.yMin - 1f), 10);
                Sprite(t, "Glow", theme.glow, new Color(theme.slideBar.r, theme.slideBar.g, theme.slideBar.b, 0.45f), new Vector2(0f, r.yMin), new Vector2(r.width + 1f, 1f), 12, true);
                var telegraph = t.gameObject.AddComponent<Telegraph>();
                telegraph.Configure(actionBeat, Chart.beatsPerBar, new[] { bar }, null);
                return t.gameObject;
            }
            Sprite(t, "Body", theme.square, new Color(theme.slideBar.r * 0.55f, theme.slideBar.g * 0.45f, theme.slideBar.b * 0.2f, 1f), new Vector2(0f, r.center.y), new Vector2(r.width, r.height), 10);
            Sprite(t, "Edge", theme.square, theme.slideBar, new Vector2(0f, r.yMin + 0.08f), new Vector2(r.width, 0.16f), 11);
            Sprite(t, "Glow", theme.glow, new Color(theme.slideBar.r, theme.slideBar.g, theme.slideBar.b, 0.45f), new Vector2(0f, r.yMin), new Vector2(r.width + 1f, 1f), 12, true);
            // Hachures d'avertissement
            for (float x = r.xMin + 0.3f; x < r.xMax - 0.2f; x += 0.7f)
                Sprite(t, "Stripe", theme.square, new Color(0f, 0f, 0f, 0.35f), new Vector2(x - r.center.x, r.yMin + 0.08f), new Vector2(0.25f, 0.16f), 12);
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
            SpriteFit(t, "Anchor", theme.hookAnchor != null ? theme.hookAnchor : theme.circle, new Vector2(x, 6f), 0.7f, 12);
            float ropeHeight = 5.5f;
            if (theme.hookRope != null)
                SpriteTiled(t, "Rope", theme.hookRope, new Vector2(x, 6f - ropeHeight * 0.5f), new Vector2(0.12f, ropeHeight), 11);
            else
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
                if (theme.wallTile != null)
                    SpriteTiled(t, "Tile", theme.wallTile, new Vector2(x, height * 0.5f), new Vector2(1f, height), 5);
                else
                    Sprite(t, "Tile", theme.square, theme.wall, new Vector2(x, height * 0.5f), new Vector2(1f, height), 5);
                if (theme.groundTop != null)
                    SpriteFit(t, "Top", theme.groundTop, new Vector2(x, height), 1f, 6);
            }
            return t.gameObject;
        }

        GameObject MakeLum(Vector2 center)
        {
            var t = Group("Lum", center);
            Sprite(t, "Glow", theme.glow, new Color(theme.lum.r, theme.lum.g, theme.lum.b, 0.55f), Vector2.zero, new Vector2(1.2f, 1.2f), 14, true);
            if (theme.lumFrames != null && theme.lumFrames.Length > 0 && theme.lumFrames[0] != null)
            {
                var core = SpriteFit(t, "Core", theme.lumFrames[0], Vector2.zero, 0.55f, 15);
                var flipbook = t.gameObject.AddComponent<BeatFlipbook>();
                flipbook.Configure(core, theme.lumFrames, 0.25f, 0f, null);
            }
            else
                Sprite(t, "Core", theme.circle, theme.lum, Vector2.zero, new Vector2(0.42f, 0.42f), 15);
            var pulse = t.gameObject.AddComponent<BeatPulse>();
            pulse.Configure(0.25f, 0.4f);
            return t.gameObject;
        }
    }
}
