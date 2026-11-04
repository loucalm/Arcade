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
        const float StepStartBeats = 0.3f;          // marche et plateforme : comme un mur
        const float MaxHeight = 2.0f;
        const float StepFaceMin = 0.25f;            // une montée plus haute que ça se saute (face mortelle)
        static readonly float[] LumLaneY = { 0.45f, 1.9f, 2.4f };

        [SerializeField] LevelTheme theme;

        public ChartData Chart { get; private set; }
        public LevelTheme Theme => theme;
        public float RunSpeed => Chart != null ? Chart.runSpeed : 8f;

        /// <summary>Obstacles (tout sauf les lums), triés par bounds.xMin.</summary>
        public readonly List<LevelObject> Obstacles = new();
        public readonly List<LevelObject> Lums = new();
        /// <summary>Plateformes (jugées à l'atterrissage, pas de collision de côté).</summary>
        public readonly List<LevelObject> Platforms = new();
        /// <summary>Événements visuels de la chart (fx_flash, camera_shake, camera_zoom, section_change), triés par beat.</summary>
        public readonly List<ChartEvent> FxEvents = new();
        // Données d'autoplay (debug + démo de l'écran Attract).
        public readonly List<float> AutoJumpBeats = new();
        public readonly List<float> AutoHitBeats = new();
        public readonly List<Vector2> AutoSlideSpans = new();

        readonly List<Vector2> gaps = new();
        /// <summary>Profil du sol principal : (x de début, hauteur), trié par x.</summary>
        readonly List<Vector2> steps = new();
        readonly List<GroundSpan> platforms = new();

        /// <summary>Sol du niveau (marches, trous, plateformes) : lu par le joueur et la caméra.</summary>
        public LevelTerrain Ground { get; } = new LevelTerrain();
        float secondsPerBeat = 0.5f;
        Transform root;
        GameObject beatGrid;

        public float BeatToX(double beat) => (float)(beat * secondsPerBeat * RunSpeed);

        /// <summary>Biome du niveau (un niveau = un monde) ; null = un biome par section (ancien fonctionnement).</summary>
        public BiomeTheme LevelBiome { get; private set; }

        public void Build(ChartData chart, float bpm, BiomeTheme biome = null)
        {
            Clear();
            Chart = chart;
            LevelBiome = biome;
            secondsPerBeat = 60f / bpm;
            if (!Mathf.Approximately(chart.bpm, bpm))
                Debug.LogWarning($"LevelBuilder: BPM de la chart ({chart.bpm}) ≠ BPM du SongData ({bpm}). Le SongData fait foi.");

            root = new GameObject("Built").transform;
            root.SetParent(transform, false);

            // 1er passage : le profil du sol (marches), pour poser les obstacles à la bonne hauteur
            float h0 = 0f;
            foreach (var e in chart.events)
            {
                if (e.type != "step") continue;
                float h = Mathf.Clamp(e.@params.height, 0f, MaxHeight);
                float x = BeatToX(e.beat + StepStartBeats);
                steps.Add(new Vector2(x, h));
                if (h - h0 > StepFaceMin)
                {
                    Obstacles.Add(new LevelObject { kind = LevelObjectKind.StepFace, beat = e.beat, bounds = new Rect(x, h0, 0.05f, h - h0) });
                    AutoJumpBeats.Add(e.beat);
                }
                h0 = h;
            }

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
                        float gh = HeightAt(x0);
                        Obstacles.Add(new LevelObject { kind = LevelObjectKind.Gap, beat = e.beat, bounds = Rect.MinMaxRect(x0, gh - 50f, x1, gh - 1f) });
                        AutoJumpBeats.Add(e.beat);
                        lastX = Mathf.Max(lastX, x1);
                        break;
                    }
                    case "wall":
                    {
                        float x0 = BeatToX(e.beat + WallStartBeats);   // le sprite du mur fait 36×32 px : hauteur fixe de 0,8 u
                        var o = new LevelObject { kind = LevelObjectKind.Wall, beat = e.beat, bounds = new Rect(x0, HeightAt(x0 + 0.45f), 0.9f, 0.8f) };
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
                        float ey = HeightAt(cx);
                        var o = new LevelObject
                        {
                            kind = enemy ? LevelObjectKind.Enemy : LevelObjectKind.Block,
                            beat = e.beat,
                            bounds = enemy ? new Rect(cx - 0.4f, ey, 0.8f, 0.9f) : new Rect(cx - 0.45f, ey, 0.9f, 0.95f),
                        };
                        o.view = enemy ? MakeEnemy(new Vector2(cx, ey), BiomeAt(e.beat)) : MakeBlock(new Vector2(cx, ey), BiomeAt(e.beat));
                        Obstacles.Add(o);
                        AutoHitBeats.Add(e.beat);
                        lastX = Mathf.Max(lastX, o.bounds.xMax);
                        break;
                    }
                    case "slide_bar":
                    {
                        float len = p.length > 0 ? p.length : SlideDefaultLength;
                        float x0 = BeatToX(e.beat + SlideStartBeats), x1 = BeatToX(e.beat + SlideStartBeats + len);
                        float sh = HeightAt(x0);
                        var o = new LevelObject { kind = LevelObjectKind.SlideBar, beat = e.beat, bounds = Rect.MinMaxRect(x0, sh + SlideBarBottom, x1, sh + 8f) };
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
                            float ly = y + LumBaseAt(x);
                            var o = new LevelObject { kind = LevelObjectKind.Lum, beat = e.beat + i * step, sequenceIndex = i, bounds = new Rect(x - 0.3f, ly - 0.3f, 0.6f, 0.6f) };
                            o.view = MakeLum(new Vector2(x, ly));
                            Lums.Add(o);
                            lastX = Mathf.Max(lastX, x);
                        }
                        break;
                    }
                    case "step":
                        lastX = Mathf.Max(lastX, BeatToX(e.beat + StepStartBeats));
                        break;
                    case "platform":
                    {
                        float len = p.length > 0 ? p.length : 1f;
                        float x0 = BeatToX(e.beat + StepStartBeats), x1 = BeatToX(e.beat + StepStartBeats + len);
                        float top = Mathf.Clamp(p.height, 0f, MaxHeight);
                        platforms.Add(new GroundSpan(x0, x1, top, true));
                        Platforms.Add(new LevelObject { kind = LevelObjectKind.Platform, beat = e.beat, bounds = Rect.MinMaxRect(x0, top, x1, top) });
                        MakePlatform(x0, x1, top, BiomeAt(e.beat));
                        AutoJumpBeats.Add(e.beat);
                        lastX = Mathf.Max(lastX, x1);
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
            BuildTerrain(lastX + 60f);
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
            Platforms.Clear();
            FxEvents.Clear();
            AutoJumpBeats.Clear();
            AutoHitBeats.Clear();
            AutoSlideSpans.Clear();
            gaps.Clear();
            steps.Clear();
            platforms.Clear();
            Ground.Clear();
        }

        /// <summary>Respawn : réactive tout ce qui est après le checkpoint.</summary>
        public void ResetFrom(float beat)
        {
            foreach (var o in Obstacles) if (o.beat >= beat) o.ResetState();
            foreach (var o in Lums) if (o.beat >= beat) o.ResetState();
        }

        /// <summary>Hauteur du sol principal en x (marches de la chart ; 0 avant la première).</summary>
        public float HeightAt(float x)
        {
            float h = 0f;
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i].x > x) break;
                h = steps[i].y;
            }
            return h;
        }

        /// <summary>Base des pièces en x : le dessus d'une plateforme si elle est là, sinon le sol principal.</summary>
        float LumBaseAt(float x)
        {
            float h = HeightAt(x);
            foreach (var pl in platforms)
                if (x >= pl.x0 && x <= pl.x1) h = Mathf.Max(h, pl.top);
            return h;
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
            if (LevelBiome != null) return LevelBiome;
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

        /// <summary>Segments de sol principal (profil des marches, moins les trous) puis plateformes.</summary>
        void BuildTerrain(float endX)
        {
            foreach (var span in MainSpans(endX)) Ground.Add(span);
            foreach (var pl in platforms) Ground.Add(pl);
            Ground.Build();
        }

        IEnumerable<GroundSpan> MainSpans(float endX)
        {
            // intervalles de hauteur constante
            var bounds = new List<(float x0, float x1, float h)>();
            float from = -40f, h = 0f;
            foreach (var st in steps)
            {
                if (st.x > from) bounds.Add((from, st.x, h));
                from = Mathf.Max(from, st.x);
                h = st.y;
            }
            bounds.Add((from, endX, h));
            // on retire les trous
            foreach (var (x0, x1, top) in bounds)
            {
                float cursor = x0;
                foreach (var g in gaps)
                {
                    if (g.y <= cursor || g.x >= x1) continue;
                    if (g.x > cursor) yield return new GroundSpan(cursor, g.x, top, false);
                    cursor = Mathf.Max(cursor, g.y);
                }
                if (x1 > cursor) yield return new GroundSpan(cursor, x1, top, false);
            }
        }

        void BuildGround(float endX)
        {
            var g = Group("Ground", Vector2.zero);
            foreach (var s in MainSpans(endX))
            {
                // découpe aux frontières de section pour suivre le biome
                float from = s.x0;
                for (int i = 1; i < Chart.sections.Length; i++)
                {
                    float c = BeatToX(Chart.sections[i].startBeat);
                    if (c <= from || c >= s.x1) continue;
                    EmitGround(g, from, c, s.top);
                    from = c;
                }
                EmitGround(g, from, s.x1, s.top);
            }
            foreach (var gap in gaps)
            {
                var biome = BiomeAtX((gap.x + gap.y) * 0.5f);
                Tiled(g, "Hazard", biome.hazard, new Vector2((gap.x + gap.y) * 0.5f, -TileHeight * 0.5f), new Vector2(gap.y - gap.x, TileHeight), 0);
            }
        }

        void EmitGround(Transform g, float x0, float x1, float top)
        {
            if (x1 - x0 < 0.01f) return;
            var biome = BiomeAtX((x0 + x1) * 0.5f);
            float cx = (x0 + x1) * 0.5f, w = x1 - x0;
            if (top <= 0.001f || biome.fill == null || biome.cliff == null)
            {
                Tiled(g, "Ground", biome.ground, new Vector2(cx, top - TileHeight * 0.5f), new Vector2(w, TileHeight), 0);
                return;
            }
            // sol surélevé : la rivière continue en bas, flanc de terre, dessus herbu
            Tiled(g, "Ground", biome.ground, new Vector2(cx, -TileHeight * 0.5f), new Vector2(w, TileHeight), 0);
            const float cliff = 0.2f;                                   // 8 px
            float fillTop = top - cliff;
            Tiled(g, "Fill", biome.fill, new Vector2(cx, fillTop * 0.5f), new Vector2(w, fillTop), 1);
            Tiled(g, "Cliff", biome.cliff, new Vector2(cx, top - cliff * 0.5f), new Vector2(w, cliff), 2);
        }

        void MakePlatform(float x0, float x1, float top, BiomeTheme biome)
        {
            if (biome.platform == null) return;
            var g = Group("Platform", Vector2.zero);
            Tiled(g, "Slab", biome.platform, new Vector2((x0 + x1) * 0.5f, top - 0.125f), new Vector2(x1 - x0, 0.25f), 3);
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
            var t = Group("Wall", new Vector2(r.center.x, r.yMin));
            Spr(t, "Sprite", biome.wall, Vector2.zero, 10);
            return t.gameObject;
        }

        GameObject MakeEnemy(Vector2 pos, BiomeTheme biome)
        {
            var t = Group("Enemy", pos);
            var frames = biome.enemyFrames;
            var sr = Spr(t, "Sprite", frames[0], Vector2.zero, 10);
            if (frames.Length > 1) t.gameObject.AddComponent<BeatFrames>().Setup(sr, frames);   // djembé : écrasé / étiré
            return t.gameObject;
        }

        GameObject MakeBlock(Vector2 pos, BiomeTheme biome)
        {
            var t = Group("Block", pos);
            Spr(t, "Sprite", biome.block, Vector2.zero, 10);
            return t.gameObject;
        }

        GameObject MakeSlideBar(Rect r, BiomeTheme biome)
        {
            var t = Group("Ceiling", new Vector2(r.center.x, r.yMin - SlideBarBottom));
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
