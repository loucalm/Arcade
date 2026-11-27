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
        const float WallStartBeats = 0.4f;          // mur juste après le sommet du saut (+ coup de pouce du PlayerController) : ≥ 120 ms des deux côtés
        const float EnemyCenterBeats = 0.6f;        // ennemi 0,6 beat après son temps : ~165 ms de marge des deux côtés
        /// <summary>Ennemi cuirassé : on le touche 0,15 beat avant son temps (fin d'un saut d'un beat), l'appui du rebond tombe sur le temps.</summary>
        public const float StompTouchBeats = 0.15f;
        const float StepStartBeats = 0.3f;          // plateforme : comme un mur
        const float StepFaceBeats = 0.5f;           // marche : face à beat+0,5 (+ coup de pouce : ~130 ms de marge sur +1 u)
        const float MaxHeight = 60f;                // hauteur libre : le niveau grimpe et plonge (garde-fou seulement)
        const float StepFaceMin = 0.25f;            // une montée plus haute que ça se saute (face mortelle)
        const float BounceDefaultBeats = 2f;
        const float PillarDepth = 14f;              // les piliers descendent jusque sous le sol le plus bas
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
        /// <summary>Tremplins (tambours) : lancent le héros très haut, triés par x.</summary>
        public readonly List<LevelObject> Bounces = new();
        /// <summary>Lianes : on les attrape en passant, le balancier est automatique, on est lâché au beat de l'objet. Triées par x.</summary>
        public readonly List<LevelObject> Vines = new();
        /// <summary>Notes de saut : en l'air près d'une note, un appui relance le saut vers la suivante. Triées par x.</summary>
        public readonly List<LevelObject> Orbs = new();
        /// <summary>Changements de gravité (x = beat, y = 1 : on tombe vers le plafond, 0 : retour au sol), triés par beat.</summary>
        public readonly List<Vector2> GravityFlips = new();
        /// <summary>Événements visuels de la chart (fx_flash, camera_shake, camera_zoom, camera, section_change, screen_split,
        /// glitch), triés par beat.</summary>
        public readonly List<ChartEvent> FxEvents = new();
        // Données d'autoplay (debug + démo de l'écran Attract).
        public readonly List<float> AutoJumpBeats = new();
        public readonly List<float> AutoHitBeats = new();

        readonly List<Vector2> gaps = new();
        /// <summary>
        /// Profil du sol principal : nœuds (x, hauteur, z = 1 si le sol monte/descend en ligne droite jusqu'au nœud suivant,
        /// 0 s'il reste plat), triés par x. Marche = nœud plat ; rampe = nœud en pente puis nœud plat.
        /// </summary>
        readonly List<Vector3> steps = new();
        readonly List<GroundSpan> platforms = new();

        /// <summary>Sol du niveau (marches, trous, plateformes) : lu par le joueur et la caméra.</summary>
        public LevelTerrain Ground { get; } = new LevelTerrain();
        /// <summary>
        /// Plafond, en miroir (dessus = −dessous du plafond) : en gravité inversée, le joueur calcule dans ce repère
        /// retourné (y' = −y) et réutilise toute la logique du sol.
        /// </summary>
        public LevelTerrain CeilingMirror { get; } = new LevelTerrain();
        /// <summary>Chemin de l'arrière-plan (plan 1), derrière le niveau : on y saute par un tambour.</summary>
        public LevelTerrain Back { get; } = new LevelTerrain();
        readonly List<GroundSpan> ceilingSpans = new();       // dessous réel du plafond (pas en miroir)
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

            // 1er passage : le profil du sol (marches, rampes), pour poser les obstacles à la bonne hauteur
            float h0 = 0f;
            foreach (var e in chart.events)
            {
                if (e.type == "step")
                {
                    float h = Mathf.Clamp(e.@params.height, -MaxHeight, MaxHeight);
                    float x = BeatToX(e.beat + StepFaceBeats);
                    steps.Add(new Vector3(x, h, 0f));
                    // lane 1 = falaise : on l'atteint en vol (tambour) ; pas de saut attendu, s'y cogner au sol reste mortel
                    if (h - h0 > StepFaceMin && e.lane != 1)
                    {
                        Obstacles.Add(new LevelObject { kind = LevelObjectKind.StepFace, beat = e.beat, bounds = new Rect(x, h0, 0.05f, h - h0) });
                        AutoJumpBeats.Add(e.beat);
                    }
                    h0 = h;
                }
                else if (e.type == "ramp")
                {
                    // rampe automatique (course sur un mur si elle est raide) : de la hauteur actuelle à `height` en `length` beats
                    float h = Mathf.Clamp(e.@params.height, -MaxHeight, MaxHeight);
                    float len = e.@params.length > 0 ? e.@params.length : 1f;
                    steps.Add(new Vector3(BeatToX(e.beat), h0, 1f));
                    steps.Add(new Vector3(BeatToX(e.beat + len), h, 0f));
                    h0 = h;
                }
            }

            float lastX = 0f;
            foreach (var e in chart.events)
                if (e.type == "back")
                {
                    float len = e.@params.length > 0 ? e.@params.length : 1f;
                    backProfile.Add(new GroundSpan(BeatToX(e.beat), BeatToX(e.beat + len), e.@params.height, false));
                }
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
                        // lane 1 = vide (sous une suite de plateformes) : pas jugé, pas de saut de l'autoplay
                        bool silent = e.lane == 1;
                        Obstacles.Add(new LevelObject { kind = LevelObjectKind.Gap, beat = e.beat, silent = silent, bounds = Rect.MinMaxRect(x0, gh - 50f, x1, gh - 1f) });
                        if (!silent) AutoJumpBeats.Add(e.beat);
                        lastX = Mathf.Max(lastX, x1);
                        break;
                    }
                    case "wall":
                    {
                        float x0 = BeatToX(e.beat + WallStartBeats);   // le sprite du mur fait 36×32 px : hauteur fixe de 0,8 u
                        // hitbox indulgente (0,7 × 0,75 u) dans le sprite de 0,9 × 0,8 u, centrée pareil
                        var o = new LevelObject { kind = LevelObjectKind.Wall, beat = e.beat, bounds = new Rect(x0 + 0.1f, HeightAt(x0 + 0.45f), 0.7f, 0.75f) };
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
                        bool flying = enemy && e.lane == 1;         // ennemi en vol : à la hauteur absolue `height`
                        bool hanging = enemy && e.lane == 2;        // pendu au plafond (dessous du plafond à `height`)
                        bool stomp = enemy && p.variant == "stomp"; // cuirassé : on lui saute dessus (appui au rebond, sur `beat`)
                        int plane = PlaneOf(e);
                        float cx = BeatToX(e.beat + (stomp ? -StompTouchBeats : EnemyCenterBeats));
                        float ey = flying ? p.height : hanging ? p.height - 0.9f : plane == 1 ? BackHeightAt(cx) : HeightAt(cx);
                        var o = new LevelObject
                        {
                            kind = enemy ? LevelObjectKind.Enemy : LevelObjectKind.Block,
                            beat = e.beat,
                            plane = plane,
                            stomp = stomp,
                            airBeats = stomp ? (p.length > 0 ? p.length : 2f) : 0f,
                            bounds = enemy ? new Rect(cx - 0.4f, ey, 0.8f, 0.9f) : new Rect(cx - 0.45f, ey, 0.9f, 0.95f),
                        };
                        o.view = flying ? MakeFlyer(new Vector2(cx, ey), BiomeAt(e.beat))
                            : hanging ? MakeHanging(new Vector2(cx, p.height), BiomeAt(e.beat))
                            : stomp ? MakeStomper(new Vector2(cx, ey), BiomeAt(e.beat))
                            : enemy ? MakeEnemy(new Vector2(cx, ey), BiomeAt(e.beat)) : MakeBlock(new Vector2(cx, ey), BiomeAt(e.beat));
                        if (plane == 1) SendToBack(o.view);
                        Obstacles.Add(o);
                        if (stomp) AutoJumpBeats.Add(e.beat);          // l'appui du grand rebond
                        else AutoHitBeats.Add(e.beat);
                        lastX = Mathf.Max(lastX, o.bounds.xMax);
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
                        lastX = Mathf.Max(lastX, BeatToX(e.beat + StepFaceBeats));
                        break;
                    case "ramp":
                        lastX = Mathf.Max(lastX, BeatToX(e.beat + (p.length > 0 ? p.length : 1f)));
                        break;
                    case "bounce":
                    {
                        // tambour-tremplin : posé sur le sol au beat ; le héros qui passe dessus s'envole (sauter pile dessus est jugé)
                        // variant : « to_back » (envoie dans l'arrière-plan), « to_front » (posé au fond, ramène devant), « back »
                        float cx = BeatToX(e.beat);
                        int plane = PlaneOf(e);
                        float top = plane == 1 ? BackHeightAt(cx) : HeightAt(cx);
                        var o = new LevelObject
                        {
                            kind = LevelObjectKind.Bounce,
                            beat = e.beat,
                            plane = plane,
                            targetPlane = p.variant == "to_back" ? 1 : p.variant == "to_front" ? 0 : plane,
                            airBeats = p.length > 0 ? p.length : BounceDefaultBeats,
                            bounds = new Rect(cx - 0.45f, top, 0.9f, 0.35f),   // tambour enfoncé dans le sol : seule la peau dépasse
                        };
                        o.view = MakeBounce(new Vector2(cx, top - 0.6f), BiomeAt(e.beat));
                        if (plane == 1) SendToBack(o.view);
                        Bounces.Add(o);
                        AutoJumpBeats.Add(e.beat);
                        lastX = Mathf.Max(lastX, cx);
                        break;
                    }
                    case "lum_at":
                    {
                        // pièce à une hauteur absolue (arcs des tremplins, chutes)
                        float x = BeatToX(e.beat);
                        float ly = p.height;
                        var o = new LevelObject { kind = LevelObjectKind.Lum, beat = e.beat, sequenceIndex = e.lane, hasNote = p.hasNote, note = p.note, plane = PlaneOf(e), bounds = new Rect(x - 0.3f, ly - 0.3f, 0.6f, 0.6f) };
                        o.view = MakeLum(new Vector2(x, ly));
                        if (o.plane == 1) SendToBack(o.view, 0.75f);
                        Lums.Add(o);
                        lastX = Mathf.Max(lastX, x);
                        break;
                    }
                    case "platform":
                    {
                        float len = p.length > 0 ? p.length : 1f;
                        float x0 = BeatToX(e.beat + StepStartBeats), x1 = BeatToX(e.beat + StepStartBeats + len);
                        float top = Mathf.Clamp(p.height, -MaxHeight, MaxHeight);
                        platforms.Add(new GroundSpan(x0, x1, top, true));
                        Platforms.Add(new LevelObject { kind = LevelObjectKind.Platform, beat = e.beat, bounds = Rect.MinMaxRect(x0, top, x1, top) });
                        MakePlatform(x0, x1, top, BiomeAt(e.beat));
                        AutoJumpBeats.Add(e.beat);
                        lastX = Mathf.Max(lastX, x1);
                        break;
                    }
                    case "ceiling":
                    {
                        // plafond : dessous à `height`, de beat à beat + length (en gravité inversée, on court dessous)
                        float x0 = BeatToX(e.beat), x1 = BeatToX(e.beat + (p.length > 0 ? p.length : 1f));
                        ceilingSpans.Add(new GroundSpan(x0, x1, p.height, false));
                        lastX = Mathf.Max(lastX, x1);
                        break;
                    }
                    case "back":
                        // chemin de l'arrière-plan (lu au 1er passage, dans backProfile)
                        lastX = Mathf.Max(lastX, BeatToX(e.beat + (p.length > 0 ? p.length : 1f)));
                        break;
                    case "jump":
                    {
                        // saut sans trou dans le sol du niveau (trou du plafond, du chemin du fond) : jugé quand on le passe
                        float x0 = BeatToX(e.beat + GapStartBeats), x1 = BeatToX(e.beat + GapStartBeats + GapDefaultLength);
                        Obstacles.Add(new LevelObject { kind = LevelObjectKind.Gap, beat = e.beat, plane = PlaneOf(e), bounds = Rect.MinMaxRect(x0, -1000f, x1, -999f) });
                        AutoJumpBeats.Add(e.beat);
                        lastX = Mathf.Max(lastX, x1);
                        break;
                    }
                    case "orb":
                    {
                        // note de saut : centre à (x du beat, `height`) ; un appui près d'elle, en l'air, relance le saut
                        // pour `step` beats (défaut 1) vers la note suivante ou le sol
                        float ox = BeatToX(e.beat);
                        var o = new LevelObject
                        {
                            kind = LevelObjectKind.Orb,
                            beat = e.beat,
                            plane = PlaneOf(e),
                            airBeats = p.step > 0 ? p.step : 1f,
                            bounds = new Rect(ox - 0.35f, p.height - 0.35f, 0.7f, 0.7f),
                        };
                        o.view = MakeOrb(new Vector2(ox, p.height));
                        Orbs.Add(o);
                        AutoJumpBeats.Add(e.beat);
                        lastX = Mathf.Max(lastX, ox);
                        break;
                    }
                    case "gravity":
                        GravityFlips.Add(new Vector2(e.beat, e.lane == 1 ? 1f : 0f));
                        MakeGravityPortal(BeatToX(e.beat), e.lane == 1, BiomeAt(e.beat));
                        break;
                    case "vine":
                    {
                        // liane : prise au beat, balancier de `length` beats (pieds de `height` à `releaseHeight`, creux `sag`),
                        // lâcher jugé, puis `step` beats de vol (vers la liane suivante ou le sol)
                        float len = p.length > 0 ? p.length : 2f;
                        var o = new LevelObject
                        {
                            kind = LevelObjectKind.Vine,
                            beat = e.beat + len,
                            grabBeat = e.beat,
                            grabY = p.height,
                            releaseY = p.releaseHeight,
                            sag = p.sag,
                            airBeats = p.step > 0 ? p.step : 1f,
                            bounds = Rect.MinMaxRect(BeatToX(e.beat), Mathf.Min(p.height, p.releaseHeight) - p.sag, BeatToX(e.beat + len), Mathf.Max(p.height, p.releaseHeight)),
                        };
                        o.view = MakeVine(o, BiomeAt(e.beat));
                        Vines.Add(o);
                        AutoJumpBeats.Add(o.beat);
                        lastX = Mathf.Max(lastX, o.bounds.xMax);
                        break;
                    }
                    case "fx_flash":
                    case "camera_shake":
                    case "camera_zoom":
                    case "camera":
                    case "section_change":
                    case "screen_split":
                    case "screen_flip":
                    case "glitch":
                        FxEvents.Add(e);
                        break;
                    default:
                        Debug.LogWarning($"LevelBuilder: type d'événement non géré « {e.type} » (beat {e.beat}).");
                        break;
                }
            }

            Obstacles.Sort((a, b) => a.bounds.xMin.CompareTo(b.bounds.xMin));
            Lums.Sort((a, b) => a.bounds.xMin.CompareTo(b.bounds.xMin));
            Bounces.Sort((a, b) => a.bounds.xMin.CompareTo(b.bounds.xMin));
            Vines.Sort((a, b) => a.bounds.xMin.CompareTo(b.bounds.xMin));
            Orbs.Sort((a, b) => a.bounds.xMin.CompareTo(b.bounds.xMin));
            GravityFlips.Sort((a, b) => a.x.CompareTo(b.x));
            gaps.Sort((a, b) => a.x.CompareTo(b.x));
            AutoJumpBeats.Sort();
            AutoHitBeats.Sort();

            float lastBeat = chart.events.Length > 0 ? chart.events[^1].beat : 0f;
            BuildTerrain(lastX + 60f);
            BuildGround(lastX + 60f);
            BuildCeiling();
            BuildBack();
            BuildBeatGrid(Mathf.CeilToInt(lastBeat) + 16);
            BuildCheckpoints();
            PrepareFrontFade();
            BuildChunks();
        }

        // ---------- Tranches : seul le morceau de niveau autour de la caméra est actif ----------
        // Le niveau entier (≈ 6 000 sprites, 400 pièces animées) restait actif toute la partie : sur la borne
        // (i5-7200U, Firefox), le tri et les scripts de tout le niveau coûtaient chaque image. On range les objets
        // par tranches de ChunkSize u et on n'active que celles proches de la caméra. Un objet tué ou ramassé
        // (activeSelf faux) le reste quand sa tranche revient.
        const float ChunkSize = 8f;
        readonly List<GameObject> chunks = new();
        int chunkLo = -1, chunkHi = -2;
        float chunkOrigin;

        void BuildChunks()
        {
            chunks.Clear();
            chunkLo = -1; chunkHi = -2;
            if (root == null) return;
            var candidates = new List<Transform>();
            for (int i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                if (beatGrid != null && c.gameObject == beatGrid) continue;
                // conteneur (sol, trous, plafond, arrière-plan…) : ses enfants ; sinon l'objet lui-même
                bool container = c.GetComponent<Renderer>() == null && c.localPosition == Vector3.zero && c.childCount > 0
                                 && c.GetComponent<MonoBehaviour>() == null;
                if (container) for (int j = 0; j < c.childCount; j++) candidates.Add(c.GetChild(j));
                else candidates.Add(c);
            }
            float min = float.MaxValue;
            var placed = new List<(Transform t, int index)>();
            foreach (var t in candidates)
            {
                var rs = t.GetComponentsInChildren<Renderer>(true);
                if (rs.Length == 0) continue;
                var b = rs[0].bounds;
                for (int k = 1; k < rs.Length; k++) b.Encapsulate(rs[k].bounds);
                if (b.size.x > ChunkSize * 1.5f) continue;                 // très large : reste toujours actif
                min = Mathf.Min(min, b.center.x);
                placed.Add((t, Mathf.FloorToInt(b.center.x / ChunkSize)));
            }
            if (placed.Count == 0) return;
            int first = Mathf.FloorToInt(min / ChunkSize);
            chunkOrigin = first * ChunkSize;
            foreach (var (t, index) in placed)
            {
                int i = index - first;
                while (chunks.Count <= i)
                {
                    var go = new GameObject($"Chunk{chunks.Count}");
                    go.transform.SetParent(root, false);
                    go.SetActive(false);
                    chunks.Add(go);
                }
                t.SetParent(chunks[i].transform, true);
            }
            UpdateChunks();
        }

        void UpdateChunks()
        {
            if (chunks.Count == 0) return;
            var rig = CameraRig.Instance;
            float cx = rig != null ? rig.ExactPosition.x : 0f;
            float half = PixelScreen.Instance != null ? PixelScreen.Instance.ViewUnits.x * 0.5f : 8f;
            // une tranche de marge de chaque côté : un objet déborde d'au plus ¾ de tranche de la sienne
            int lo = Mathf.Max(0, Mathf.FloorToInt((cx - half - chunkOrigin) / ChunkSize) - 1);
            int hi = Mathf.Min(chunks.Count - 1, Mathf.FloorToInt((cx + half - chunkOrigin) / ChunkSize) + 1);
            if (lo == chunkLo && hi == chunkHi) return;
            for (int i = Mathf.Max(0, Mathf.Min(lo, chunkLo)); i <= Mathf.Min(chunks.Count - 1, Mathf.Max(hi, chunkHi)); i++)
            {
                bool on = i >= lo && i <= hi;
                if (chunks[i].activeSelf != on) chunks[i].SetActive(on);
            }
            chunkLo = lo; chunkHi = hi;
        }

        void LateUpdate() => UpdateChunks();

        public void Clear()
        {
            if (root != null) Destroy(root.gameObject);
            root = null;
            beatGrid = null;
            Obstacles.Clear();
            Lums.Clear();
            Platforms.Clear();
            Bounces.Clear();
            Vines.Clear();
            Orbs.Clear();
            GravityFlips.Clear();
            FxEvents.Clear();
            AutoJumpBeats.Clear();
            AutoHitBeats.Clear();
            gaps.Clear();
            steps.Clear();
            platforms.Clear();
            Ground.Clear();
            CeilingMirror.Clear();
            Back.Clear();
            frontFaded.Clear();
            frontGround = null;
            chunks.Clear();
            chunkLo = -1; chunkHi = -2;
            ceilingSpans.Clear();
            backProfile.Clear();
        }

        /// <summary>
        /// Reprise après une mort (die and retry) : réactive les obstacles, ennemis, tambours, lianes et notes après le point
        /// de reprise ; les pièces déjà prises restent prises (le compteur ne compte pas deux fois).
        /// </summary>
        public void ResetObstaclesFrom(float beat)
        {
            foreach (var o in Obstacles) if (o.beat >= beat) o.ResetState();
            foreach (var o in Bounces) if (o.beat >= beat) o.ResetState();
            foreach (var o in Orbs) if (o.beat >= beat) o.ResetState();
            foreach (var o in Vines)
            {
                if (o.grabBeat >= beat) o.ResetState();
                if (o.view != null) o.view.GetComponent<VineView>()?.Release();
            }
        }

        /// <summary>Respawn : réactive tout ce qui est après le checkpoint.</summary>
        public void ResetFrom(float beat)
        {
            foreach (var o in Obstacles) if (o.beat >= beat) o.ResetState();
            foreach (var o in Lums) if (o.beat >= beat) o.ResetState();
            foreach (var o in Bounces) if (o.beat >= beat) o.ResetState();
            foreach (var o in Vines) if (o.grabBeat >= beat) o.ResetState();
            foreach (var o in Orbs) if (o.beat >= beat) o.ResetState();
        }

        /// <summary>Centre de la caméra au beat donné, d'après le rail de la chart (NaN s'il n'y en a pas).</summary>
        public float RailAt(double beat)
        {
            var rail = Chart != null ? Chart.rail : null;
            if (rail == null || rail.Length == 0) return float.NaN;
            float step = Chart.railStep > 0f ? Chart.railStep : 0.25f;
            float f = Mathf.Clamp((float)(beat / step), 0f, rail.Length - 1);
            int i = Mathf.Min((int)f, rail.Length - 2);
            return i < 0 ? rail[0] : Mathf.Lerp(rail[i], rail[i + 1], f - i);
        }

        /// <summary>Hauteur du sol principal en x (marches et rampes de la chart, trous ignorés ; 0 avant la première).</summary>
        public float HeightAt(float x)
        {
            float h = 0f;
            for (int i = 0; i < steps.Count; i++)
            {
                var k = steps[i];
                if (k.x > x) break;
                h = k.y;
                if (k.z > 0.5f && i + 1 < steps.Count && steps[i + 1].x > x)
                    return Mathf.Lerp(k.y, steps[i + 1].y, Mathf.InverseLerp(k.x, steps[i + 1].x, x));
            }
            return h;
        }

        readonly List<GroundSpan> backProfile = new();

        /// <summary>Hauteur du chemin de l'arrière-plan en x (le segment qui couvre x, sinon le plus proche).</summary>
        public float BackHeightAt(float x)
        {
            float best = 0f, dist = float.PositiveInfinity;
            foreach (var s in backProfile)
            {
                float d = x < s.x0 ? s.x0 - x : x > s.x1 ? x - s.x1 : 0f;
                if (d < dist) { dist = d; best = s.top; }
            }
            return best;
        }

        /// <summary>Hauteur d'arrivée sur un plan donné (0 = sol du niveau, 1 = arrière-plan).</summary>
        public float HeightOnPlane(float x, int plane) => plane == 1 ? BackHeightAt(x) : HeightAt(x);

        static int PlaneOf(ChartEvent e) => e.@params.variant == "back" || e.@params.variant == "to_front" ? 1 : 0;

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

        /// <summary>Biome de la section qui contient ce beat (un biome par section de la chart).</summary>
        public BiomeTheme BiomeAt(double beat)
        {
            // le décor choisi par le joueur vaut pour tout le morceau (décision du 25/11 : les 8 « mondes » sont le
            // même niveau, le choix est celui du décor). Les biomes nommés par les sections ne servent que sans choix.
            if (LevelBiome != null) return LevelBiome;
            int i = Chart != null ? Chart.SectionIndexAt(beat) : 0;
            // une section peut changer de monde (ex. le refrain monte dans l'orage) : elle nomme l'asset du biome
            string named = Chart != null && Chart.sections.Length > 0 ? Chart.sections[Mathf.Clamp(i, 0, Chart.sections.Length - 1)].biome : null;
            if (!string.IsNullOrEmpty(named))
                foreach (var b in theme.biomes)
                    if (b != null && b.name == named) return b;
            if (LevelBiome != null) return LevelBiome;
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
            foreach (var c in ceilingSpans) CeilingMirror.Add(new GroundSpan(c.x0, c.x1, -c.top, false));
            CeilingMirror.Build();
            foreach (var b in backProfile) Back.Add(b);
            Back.Build();
        }

        IEnumerable<GroundSpan> MainSpans(float endX)
        {
            // intervalles du profil : plats (marches) ou en pente (rampes)
            var bounds = new List<(float x0, float x1, float h0, float h1)>();
            float from = -40f, h = 0f;
            bool linear = false;
            foreach (var st in steps)
            {
                if (st.x > from) bounds.Add((from, st.x, h, linear ? st.y : h));
                from = Mathf.Max(from, st.x);
                h = st.y;
                linear = st.z > 0.5f;
            }
            bounds.Add((from, endX, h, h));
            // on retire les trous
            foreach (var (x0, x1, h0, h1) in bounds)
            {
                float cursor = x0;
                foreach (var g in gaps)
                {
                    if (g.y <= cursor || g.x >= x1) continue;
                    if (g.x > cursor) yield return Cut(x0, x1, h0, h1, cursor, g.x);
                    cursor = Mathf.Max(cursor, g.y);
                }
                if (x1 > cursor) yield return Cut(x0, x1, h0, h1, cursor, x1);
            }
        }

        static GroundSpan Cut(float x0, float x1, float h0, float h1, float a, float b)
        {
            float ta = Mathf.Lerp(h0, h1, Mathf.InverseLerp(x0, x1, a));
            float tb = Mathf.Lerp(h0, h1, Mathf.InverseLerp(x0, x1, b));
            return new GroundSpan(a, b, ta, tb, false);
        }

        /// <summary>Le sol en piliers : dessus herbu, flanc de terre jusque sous le point le plus bas. Les trous restent vides.</summary>
        void BuildGround(float endX)
        {
            var g = Group("Ground", Vector2.zero);
            frontGround = g;
            float floor = Ground.LowestTop - PillarDepth;
            foreach (var s in MainSpans(endX))
            {
                if (s.Sloped) { EmitRamp(g, s, floor); continue; }
                // découpe aux frontières de section pour suivre le biome
                float from = s.x0;
                for (int i = 1; i < Chart.sections.Length; i++)
                {
                    float c = BeatToX(Chart.sections[i].startBeat);
                    if (c <= from || c >= s.x1) continue;
                    EmitGround(g, from, c, s.top, floor);
                    from = c;
                }
                EmitGround(g, from, s.x1, s.top, floor);
            }
            BuildPits(floor);
        }

        void EmitGround(Transform g, float x0, float x1, float top, float floor)
        {
            if (x1 - x0 < 0.01f) return;
            var biome = BiomeAtX((x0 + x1) * 0.5f);
            float cx = (x0 + x1) * 0.5f, w = x1 - x0;
            if (biome.fill == null || biome.cliff == null)
            {
                Tiled(g, "Ground", biome.ground, new Vector2(cx, top - TileHeight * 0.5f), new Vector2(w, TileHeight), 0);
                return;
            }
            const float cliff = 0.2f;                                   // 8 px
            float fillTop = top - cliff;
            EmitFill(g, biome, cx, w, fillTop, floor);
            Tiled(g, "Cliff", biome.cliff, new Vector2(cx, top - cliff * 0.5f), new Vector2(w, cliff), 2);
            EmitPillarDecor(g, biome, x0, x1, fillTop);
        }

        // Flanc d'un pilier : un bandeau clair sous le bord herbu, puis la terre assombrie, puis la profondeur encore plus
        // sombre (le dessus, où l'on court, ressort ; les piliers ne sont plus un aplat uniforme).
        static readonly Color DeepFill = new(0.58f, 0.54f, 0.7f);
        static readonly Color DeeperFill = new(0.46f, 0.42f, 0.58f);
        const float LipDepth = 0.4f;                                   // 16 px
        const float MidDepth = 2.4f;                                   // sous 2,4 u : la profondeur
        const float PitSurfaceDepth = 1f;                              // liquide des trous : 1 u sous le bord le plus bas

        void EmitFill(Transform g, BiomeTheme biome, float cx, float w, float fillTop, float floor)
        {
            float lipBottom = Mathf.Max(floor, PixelScreen.Snap(fillTop - LipDepth));
            Tiled(g, "Lip", biome.fill, new Vector2(cx, (fillTop + lipBottom) * 0.5f), new Vector2(w, fillTop - lipBottom), 1);
            if (lipBottom - floor <= 0.01f) return;
            float midBottom = biome.fillDeep != null ? Mathf.Max(floor, PixelScreen.Snap(fillTop - MidDepth)) : floor;
            Tiled(g, "Fill", biome.fill, new Vector2(cx, (lipBottom + midBottom) * 0.5f), new Vector2(w, lipBottom - midBottom), 1).color = DeepFill;
            if (midBottom - floor <= 0.01f) return;
            // la profondeur s'efface par paliers (pas de dégradé continu) : les grands piliers deviennent des îlots qui
            // se fondent dans le décor, et l'on voit le paysage sous soi quand on monte (audit du 25/11, verticalité)
            float y = midBottom;
            for (int i = 0; i <= DeepFade.Length && y - floor > 0.01f; i++)
            {
                float bottom = i < DeepFade.Length ? Mathf.Max(floor, PixelScreen.Snap(y - DeepBand)) : floor;
                float alpha = i == 0 ? 1f : DeepFade[i - 1];
                if (i == DeepFade.Length) alpha = DeepFade[DeepFade.Length - 1];
                var c = DeeperFill;
                c.a = alpha;
                if (i == 0) bottom = Mathf.Max(floor, PixelScreen.Snap(y - DeepSolid));
                Tiled(g, "Deep", biome.fillDeep, new Vector2(cx, (y + bottom) * 0.5f), new Vector2(w, y - bottom), 1).color = c;
                y = bottom;
            }
        }

        const float DeepSolid = 0.8f;                                  // sous la terre : 32 px encore pleins
        const float DeepBand = 1.2f;                                   // puis des paliers de 48 px…
        static readonly float[] DeepFade = { 0.78f, 0.56f, 0.36f, 0.2f };   // … de plus en plus transparents

        /// <summary>
        /// Détails semés sur le flanc (pierres, racines, fossiles) : environ un toutes les 1,6 u, placés au hasard mais
        /// toujours au même endroit (graine = position), sur la grille de pixels, sous le bord herbu.
        /// </summary>
        void EmitPillarDecor(Transform g, BiomeTheme biome, float x0, float x1, float fillTop)
        {
            var decor = biome.pillarDecor;
            if (decor == null || decor.Length == 0) return;
            const float spacing = 1.6f;
            for (float x = Mathf.Ceil(x0 / spacing) * spacing; x < x1 - 0.4f; x += spacing)
            {
                uint h = Hash(Mathf.RoundToInt(x * 40f));
                if ((h & 3u) == 0u) continue;                                      // un emplacement sur quatre reste nu
                var sprite = decor[(h >> 2) % (uint)decor.Length];
                if (sprite == null) continue;
                float dx = ((h >> 5) % 24u) / 40f;                                 // 0 … 0,6 u
                float depth = 0.75f + ((h >> 10) % 80u) / 40f;                     // 0,75 … 2,7 u sous le bord
                float px = x + dx;
                if (px < x0 + 0.3f || px > x1 - 0.3f) continue;
                var sr = Spr(g, "Decor", sprite, new Vector2(PixelScreen.Snap(px), PixelScreen.Snap(fillTop - depth)), 1);
                sr.color = depth > MidDepth - 0.4f ? DeeperFill : DeepFill;
                sr.sortingOrder = 2;
            }
        }

        static uint Hash(int v)
        {
            uint h = (uint)v * 2654435761u;
            h ^= h >> 15;
            h *= 2246822519u;
            h ^= h >> 13;
            return h;
        }

        /// <summary>
        /// Le fond d'un trou montre le danger : le liquide du biome (eau, lave, vase…) 1 u sous le bord le plus bas, donc
        /// dans l'image, puis plus sombre en profondeur. On y plonge avant de mourir (KillY : 2,5 u sous le bord).
        /// Avant, le trou restait vide, couleur de la brume.
        /// </summary>
        void BuildPits(float floor)
        {
            if (gaps.Count == 0) return;
            var g = Group("Pits", Vector2.zero);
            foreach (var gap in gaps)
            {
                var biome = BiomeAtX((gap.x + gap.y) * 0.5f);
                if (biome.hazard == null) continue;
                float edge = Mathf.Min(HeightAt(gap.x - 0.05f), HeightAt(gap.y + 0.05f));
                float top = PixelScreen.Snap(edge - PitSurfaceDepth);
                float cx = (gap.x + gap.y) * 0.5f, w = gap.y - gap.x;
                Tiled(g, "Surface", biome.hazard, new Vector2(cx, top - TileHeight * 0.5f), new Vector2(w, TileHeight), 0);
                if (top - TileHeight - floor > 0.01f)
                    Tiled(g, "Depth", biome.hazard, new Vector2(cx, (top - TileHeight + floor) * 0.5f), new Vector2(w, top - TileHeight - floor), 0).color = DeeperFill;
            }
        }

        /// <summary>Rampe : colonnes de 8 px en escalier (pixel art, pas de rotation), le héros glisse sur la pente exacte.</summary>
        void EmitRamp(Transform g, GroundSpan s, float floor)
        {
            var biome = BiomeAtX((s.x0 + s.x1) * 0.5f);
            if (biome.fill == null || biome.cliff == null) return;
            const float col = 0.2f, cliff = 0.2f;
            int n = Mathf.Max(1, Mathf.CeilToInt((s.x1 - s.x0) / col));
            float w = (s.x1 - s.x0) / n;
            for (int i = 0; i < n; i++)
            {
                float a = s.x0 + i * w;
                float top = PixelScreen.Snap(s.TopAt(a + w * 0.5f));
                float cx = a + w * 0.5f;
                EmitFill(g, biome, cx, w, top - cliff, floor);
                Tiled(g, "RampTop", biome.cliff, new Vector2(cx, top - cliff * 0.5f), new Vector2(w, cliff), 2);
            }
        }

        void MakePlatform(float x0, float x1, float top, BiomeTheme biome)
        {
            if (biome.platform == null) return;
            var g = Group("Platform", Vector2.zero);
            float cx = (x0 + x1) * 0.5f, w = x1 - x0;
            Tiled(g, "Slab", biome.platform, new Vector2(cx, top - 0.125f), new Vector2(w, 0.25f), 3);
            // dessous en îlot (deux paliers de terre en retrait) : la plateforme a du volume et ne se lit plus comme
            // une ligne de 2 à 3 px (audit du 25/11)
            if (biome.fill == null) return;
            float y = top - 0.25f;
            float[] inset = { 0.1f, 0.3f }, depth = { 0.2f, 0.15f };   // 14 px en tout : on passe encore dessous
            for (int i = 0; i < inset.Length; i++)
            {
                float ww = PixelScreen.Snap(w - inset[i] * 2f);
                if (ww <= 0.2f) break;
                Tiled(g, "Under", biome.fill, new Vector2(PixelScreen.Snap(cx), y - depth[i] * 0.5f), new Vector2(ww, depth[i]), 2).color = i == 0 ? DeepFill : DeeperFill;
                y -= depth[i];
            }
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
            {
                float x = TotemX(BeatToX(Chart.sections[i].startBeat) - 0.6f);
                // posé sur le sol réel (marches, rampes) : avant, toujours à 0, il flottait ou s'enfonçait
                float y = Mathf.Min(HeightAt(x - TotemHalfWidth), HeightAt(x + TotemHalfWidth));
                Spr(g, "Totem", theme.totem, new Vector2(PixelScreen.Snap(x), PixelScreen.Snap(y)), 4);
            }
        }

        const float TotemHalfWidth = 0.3f;              // 20 px + contour, en unités (40 px/u), avec un peu de marge

        /// <summary>Place du totem en x : recule avant un trou s'il tombait dessus (il flottait dans le vide).</summary>
        float TotemX(float x)
        {
            for (int k = 0; k < 4; k++)
            {
                bool moved = false;
                foreach (var gap in gaps)
                    if (x + TotemHalfWidth > gap.x && x - TotemHalfWidth < gap.y)
                    {
                        x = gap.x - TotemHalfWidth - 0.1f;
                        moved = true;
                    }
                if (!moved) break;
            }
            return x;
        }

        // ---------- Plafond, arrière-plan, lianes, portails de gravité ----------

        static readonly Color BackTint = new(0.5f, 0.5f, 0.66f);         // l'arrière-plan : plus sombre, bleuté
        const int BackOrder = -50;                                       // derrière le niveau, devant la brume du décor

        /// <summary>Plafond : le sol retourné (herbe en dessous, terre au-dessus), suspendu au-dessus du chemin.</summary>
        void BuildCeiling()
        {
            if (ceilingSpans.Count == 0) return;
            var g = Group("Ceiling", Vector2.zero);
            foreach (var c in ceilingSpans)
            {
                var biome = BiomeAtX((c.x0 + c.x1) * 0.5f);
                if (biome.fill == null || biome.cliff == null) continue;
                float cx = (c.x0 + c.x1) * 0.5f, w = c.x1 - c.x0, h = c.top;
                Tiled(g, "Grass", biome.cliff, new Vector2(cx, h + 0.1f), new Vector2(w, 0.2f), 2).flipY = true;
                Tiled(g, "Lip", biome.fill, new Vector2(cx, h + 0.2f + LipDepth * 0.5f), new Vector2(w, LipDepth), 1).flipY = true;
                var deep = Tiled(g, "Fill", biome.fill, new Vector2(cx, h + 0.2f + LipDepth + 5f), new Vector2(w, 10f), 1);
                deep.flipY = true;
                deep.color = DeepFill;
            }
        }

        /// <summary>Chemin de l'arrière-plan : piliers plus sombres, derrière le niveau (le héros y passe derrière le premier plan).</summary>
        void BuildBack()
        {
            if (backProfile.Count == 0) return;
            var g = Group("Back", Vector2.zero);
            foreach (var s in backProfile)
            {
                var biome = BiomeAtX((s.x0 + s.x1) * 0.5f);
                if (biome.fill == null || biome.cliff == null) continue;
                float cx = (s.x0 + s.x1) * 0.5f, w = s.x1 - s.x0;
                Tiled(g, "Top", biome.cliff, new Vector2(cx, s.top - 0.1f), new Vector2(w, 0.2f), BackOrder + 1).color = BackTint;
                Tiled(g, "Fill", biome.fill, new Vector2(cx, s.top - 0.2f - 6f), new Vector2(w, 12f), BackOrder).color = BackTint * DeepFill;
            }
        }

        Transform frontGround;
        readonly List<SpriteRenderer> frontFaded = new();

        /// <summary>
        /// Pendant que le héros court dans l'arrière-plan, le premier plan (sol, piliers, objets du plan 0) devient
        /// semi-transparent : on le voit passer derrière. Avant, il n'était qu'un peu assombri et se perdait.
        /// </summary>
        /// <summary>Liste du premier plan à pâlir, faite à la construction (ensuite, le sol est rangé par tranches).</summary>
        void PrepareFrontFade()
        {
            frontFaded.Clear();
            if (frontGround != null) frontFaded.AddRange(frontGround.GetComponentsInChildren<SpriteRenderer>(true));
            foreach (var o in Obstacles)
                if (o.plane == 0 && o.view != null) frontFaded.AddRange(o.view.GetComponentsInChildren<SpriteRenderer>(true));
        }

        public void SetFrontFaded(bool faded)
        {
            foreach (var sr in frontFaded)
            {
                if (sr == null) continue;
                var c = sr.color;
                c.a = faded ? 0.45f : 1f;
                sr.color = c;
            }
        }

        /// <summary>Objet de l'arrière-plan : assombri et dessiné derrière le niveau.</summary>
        static void SendToBack(GameObject view, float tint = 1f)
        {
            if (view == null) return;
            foreach (var sr in view.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.sortingOrder = BackOrder + 4;
                sr.color *= Color.Lerp(Color.white, BackTint, tint);
            }
        }

        /// <summary>
        /// Portail de gravité : une grande porte (deux montants à contour sombre, linteau en haut et en bas, voile coloré
        /// rayé entre les deux) et de grosses flèches qui montrent le sens de la chute. On la traverse et le haut devient
        /// le bas. Lisible à 2 m : l'ancien portail (deux traits cyan) se lisait comme un bug d'affichage.
        /// </summary>
        void MakeGravityPortal(float x, bool up, BiomeTheme biome)
        {
            var g = Group(up ? "GravityUp" : "GravityDown", Vector2.zero);
            float y0 = PixelScreen.Snap(HeightAt(x) - 1f), y1 = y0 + 9f, ym = (y0 + y1) * 0.5f, h = y1 - y0;
            var c = biome.spark;
            var ink = new Color(0.08f, 0.06f, 0.14f);
            const float half = 0.8f, post = 0.3f, rim = 0.05f;
            // voile : bandes horizontales alternées (pixel art, pas de dégradé)
            for (float y = y0; y < y1 - 0.01f; y += 0.4f)
            {
                bool odd = Mathf.RoundToInt((y - y0) / 0.4f) % 2 == 1;
                Box(g, "Veil", new Color(c.r, c.g, c.b, odd ? 0.22f : 0.36f), new Vector2(PixelScreen.Snap(x), y + 0.2f), new Vector2(half * 2f, 0.4f), 11);
            }
            // montants et linteaux : contour sombre, puis la couleur vive du biome
            foreach (float side in new[] { -1f, 1f })
            {
                float px = PixelScreen.Snap(x + side * (half + post * 0.5f));
                Box(g, "PostRim", ink, new Vector2(px, ym), new Vector2(post + rim * 2f, h + rim * 2f), 12);
                Box(g, "Post", c, new Vector2(px, ym), new Vector2(post, h), 13);
                Box(g, "PostLight", Color.Lerp(c, Color.white, 0.5f), new Vector2(PixelScreen.Snap(px - post * 0.25f), ym), new Vector2(0.05f, h), 14);
            }
            foreach (float yy in new[] { y0, y1 })
            {
                Box(g, "LintelRim", ink, new Vector2(PixelScreen.Snap(x), yy), new Vector2(half * 2f + post * 2f + rim * 2f, 0.3f + rim * 2f), 12);
                Box(g, "Lintel", c, new Vector2(PixelScreen.Snap(x), yy), new Vector2(half * 2f + post * 2f, 0.3f), 13);
            }
            // grosses flèches (chevrons de 3 carrés de 4 px, contour sombre) dans le sens de la chute
            const float sq = 0.1f;
            for (int k = 0; k < 4; k++)
            {
                float ay = PixelScreen.Snap(y0 + 1.5f + k * 2f);
                for (int j = -2; j <= 2; j++)
                {
                    var p = new Vector2(PixelScreen.Snap(x + j * sq * 1.5f), PixelScreen.Snap(ay + (up ? 1 : -1) * (2 - Mathf.Abs(j)) * sq * 1.5f));
                    Box(g, "ArrowRim", ink, p, new Vector2(sq * 2.5f, sq * 2.5f), 14);
                    Box(g, "Arrow", Color.white, p, new Vector2(sq * 1.5f, sq * 1.5f), 15);
                }
            }
        }

        /// <summary>
        /// Cuirassé : carapace de métal et tremplin jaune (sprite commun à tous les biomes, comme la chauve-souris), le
        /// tremplin s'écrase sur le temps, et une double flèche jaune au-dessus dit « saute dessus ». Rien à voir avec
        /// l'ennemi du biome, qu'on frappe. Sans sprite dédié : l'ennemi du biome teinté métal.
        /// </summary>
        GameObject MakeStomper(Vector2 pos, BiomeTheme biome)
        {
            var t = Group("Stomper", pos);
            var frames = theme.stomper != null && theme.stomper.Length > 0 && theme.stomper[0] != null ? theme.stomper : biome.enemyFrames;
            var sr = Spr(t, "Sprite", frames[0], Vector2.zero, 10);
            if (frames.Length > 1) t.gameObject.AddComponent<BeatFrames>().Setup(sr, frames);
            if (frames == biome.enemyFrames) sr.color = new Color(0.6f, 0.75f, 1f);
            var arrow = new GameObject("Arrow").transform;
            arrow.SetParent(t, false);
            for (int k = 0; k < 2; k++)
                for (int j = 0; j < 5; j++)
                {
                    float dx = (j - 2) * 0.1f, dy = 1.25f + k * 0.3f + Mathf.Abs(j - 2) * 0.1f;     // chevron pointe en bas
                    Box(arrow, "Px", new Color(1f, 0.85f, 0.25f, 1f), Vector2.zero, new Vector2(0.1f, 0.1f), 13).transform.localPosition =
                        new Vector3(PixelScreen.Snap(dx), PixelScreen.Snap(dy), 0f);
                }
            arrow.gameObject.AddComponent<BeatBob>();
            return t.gameObject;
        }

        /// <summary>Note de saut : une pièce agrandie ×2 (facteur entier), teintée, qui tourne.</summary>
        GameObject MakeOrb(Vector2 center)
        {
            var t = Group("Orb", center);
            var sr = Spr(t, "Sprite", theme.coinFrames[0], Vector2.zero, 14, 2f);
            sr.color = new Color(0.55f, 1f, 0.95f);
            sr.gameObject.AddComponent<SpriteCycle>().Setup(theme.coinFrames);
            // halo : quatre pixels autour
            foreach (var d in new[] { new Vector2(0.55f, 0f), new Vector2(-0.55f, 0f), new Vector2(0f, 0.55f), new Vector2(0f, -0.55f) })
                Box(t, "Halo", new Color(0.55f, 1f, 0.95f, 0.8f), d, new Vector2(0.1f, 0.1f), 13).transform.localPosition = new Vector3(PixelScreen.Snap(d.x), PixelScreen.Snap(d.y), 0f);
            return t.gameObject;
        }

        /// <summary>Ennemi pendu au plafond (dessous du plafond à `underside`) : l'ennemi du biome, la tête en bas.</summary>
        GameObject MakeHanging(Vector2 underside, BiomeTheme biome)
        {
            var t = Group("Hanging", underside);
            var frames = biome.enemyFrames;
            var sr = Spr(t, "Sprite", frames[0], Vector2.zero, 10);
            sr.flipY = true;
            if (IsBat(frames)) Flap(sr);
            else if (frames.Length > 1) t.gameObject.AddComponent<BeatFrames>().Setup(sr, frames);
            return t.gameObject;
        }

        /// <summary>Liane : un nœud accroché en haut, la corde (pixels) qui pend vers l'endroit où on l'attrape.</summary>
        GameObject MakeVine(LevelObject o, BiomeTheme biome)
        {
            float xg = BeatToX(o.grabBeat), xr = o.bounds.xMax;
            var anchor = new Vector2(PixelScreen.Snap((xg + xr) * 0.5f), PixelScreen.Snap(Mathf.Max(o.grabY, o.releaseY) + VineView.HandHeight + 2.2f));
            var t = Group("Vine", anchor);
            if (biome.post != null) Spr(t, "Knot", biome.post, Vector2.zero, 9);
            var view = t.gameObject.AddComponent<VineView>();
            // au repos, la corde pend droit sous son nœud (jusqu'à la hauteur de la main) ; on l'attrape en passant
            view.Setup(theme.pixel, new Color(0.3f, 0.45f, 0.18f), new Vector2(anchor.x, o.grabY + VineView.HandHeight));
            return t.gameObject;
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
            if (IsBat(frames)) Flap(sr);                                                       // les Échos : chauve-souris au sol
            else if (frames.Length > 1) t.gameObject.AddComponent<BeatFrames>().Setup(sr, frames);   // djembé : écrasé / étiré
            return t.gameObject;
        }

        GameObject MakeBlock(Vector2 pos, BiomeTheme biome)
        {
            var t = Group("Block", pos);
            Spr(t, "Sprite", biome.block, Vector2.zero, 10);
            return t.gameObject;
        }

        /// <summary>Tambour-tremplin : le tambour du biome (sprite `block`). Dans les niveaux verticaux, tambour = rebond.</summary>
        GameObject MakeBounce(Vector2 pos, BiomeTheme biome)
        {
            var t = Group("Bounce", pos);
            var sr = Spr(t, "Sprite", biome.block, Vector2.zero, 10);
            sr.gameObject.AddComponent<PressBump>();
            return t.gameObject;
        }

        /// <summary>
        /// Ennemi volant (chauve-souris du PDF, commune à tous les biomes ; l'ennemi du biome sinon). Elle bat des ailes
        /// (4 images, tools/gen-bat.py) et un liseré clair la détache des décors sombres. Plus de halo derrière elle : il
        /// se lisait comme un bouclier alors qu'on doit la frapper (retour de Lucas, 2026-11-26).
        /// </summary>
        GameObject MakeFlyer(Vector2 pos, BiomeTheme biome)
        {
            var t = Group("Flyer", pos);
            bool animated = IsBat(theme.flyerFrames);
            var sr = Spr(t, "Sprite", animated ? theme.flyerFrames[0] : theme.flyer != null ? theme.flyer : biome.enemyFrames[0], Vector2.zero, 10);
            if (animated) Flap(sr);
            return t.gameObject;
        }

        /// <summary>Images par seconde du battement d'ailes : 4 images, soit 3 battements par seconde.</summary>
        const float FlapFps = 12f;
        /// <summary>Vol stationnaire des chauves-souris : ±4 px du monde, un aller-retour en 0,9 s (visuel seulement).</summary>
        const float HoverAmplitude = 4f / PixelScreen.Ppu, HoverPeriod = 0.9f;

        /// <summary>Ces images sont-elles la chauve-souris animée (tools/gen-bat.py) ? Volante, au sol (les Échos) ou pendue.</summary>
        bool IsBat(Sprite[] frames) =>
            frames != null && frames.Length > 1 && frames[0] != null && theme.flyerFrames != null &&
            theme.flyerFrames.Length > 0 && frames[0] == theme.flyerFrames[0];

        /// <summary>La chauve-souris bat des ailes en continu (pas par crans sur le temps) et plane de haut en bas.</summary>
        void Flap(SpriteRenderer sr)
        {
            sr.gameObject.AddComponent<SpriteCycle>().Setup(theme.flyerFrames, FlapFps);
            sr.gameObject.AddComponent<Hover>().Setup(HoverAmplitude, HoverPeriod);
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
