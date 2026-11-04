using System;
using System.Collections;
using Kenta.FX;
using Kenta.Level;
using Kenta.Player;
using Kenta.Rhythm;
using UnityEngine;

namespace Kenta.Core
{
    /// <summary>
    /// Une partie : construit le niveau, lance la musique, gère score et combo. Mort subite : un coup
    /// (obstacle, chute) termine la partie.
    /// Mode démo = autoplay sans perte de cœur (écran Attract).
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        [SerializeField] LevelBuilder level;
        [SerializeField] PlayerController player;
        [SerializeField] HeroAnimator heroAnimator;
        [SerializeField] BandanaTrail bandana;
        [SerializeField] BiomeBackdrop backdrop;
        [Tooltip("Racine du monde de jeu (niveau + joueur), masquée hors partie.")]
        [SerializeField] GameObject world;

        [Header("Règles (mort subite : un coup suffit)")]
        [Tooltip("Temps (s) pendant lequel on voit la pose de mort avant l'écran de fin.")]
        [SerializeField] float deathDelay = 0.8f;
        [SerializeField] int comboPerMultiplier = 8;
        [SerializeField] int maxMultiplier = 8;

        [Header("Points (× multiplicateur)")]
        [SerializeField] int lumPoints = 10;
        [SerializeField] int enemyPoints = 50;
        [SerializeField] int obstaclePoints = 20;
        [SerializeField] int perfectSectionBonus = 500;
        [SerializeField] int completionBonus = 2000;

        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int Lums { get; private set; }
        public int TotalLums { get; private set; }
        public int Multiplier => Mathf.Min(1 + Combo / Mathf.Max(1, comboPerMultiplier), maxMultiplier);
        public bool IsRunning { get; private set; }
        public bool IsDemo { get; private set; }
        public bool Completed { get; private set; }
        public bool Autoplay { get; private set; }
        public double SongLengthSeconds => song != null && song.clip != null ? song.clip.length - song.offsetSeconds : 1.0;
        public ChartData Chart { get; private set; }
        /// <summary>Biome en cours (un par section de la chart).</summary>
        public BiomeTheme CurrentBiome => level.Theme != null && Chart != null ? level.BiomeAt(Chart.sections[SectionIndex].startBeat) : null;
        public int SectionIndex { get; private set; }
        public string SectionName => Chart != null ? Chart.sections[SectionIndex].name : "";

        /// <summary>Avancement 0..1 dans le morceau.</summary>
        public float Progress
        {
            get
            {
                var c = Conductor.Instance;
                if (c == null || song == null || song.clip == null) return 0f;
                double totalBeats = c.SecondsToBeat(song.clip.length - song.offsetSeconds);
                return Mathf.Clamp01((float)(c.SongBeat / totalBeats));
            }
        }

        public string Medal
        {
            get
            {
                float ratio = TotalLums > 0 ? (float)Lums / TotalLums : 0f;
                if (!Completed) return ratio >= 0.3f ? "BRONZE" : "AUCUNE";
                return ratio >= 0.9f ? "OR" : ratio >= 0.6f ? "ARGENT" : "BRONZE";
            }
        }

        public RunJudgeStats JudgeStats => judgeStats;
        /// <summary>Note de fin de niveau (S+ … D).</summary>
        public string Grade => GradeCalculator.Grade(judgeStats.perfect, judgeStats.good, judgeStats.meh, Lums, TotalLums, Completed);

        /// <summary>(jugement, position monde) : mot PARFAIT / BIEN / BOF près du héros.</summary>
        public event Action<Judgement, Vector2> OnJudged;
        public event Action OnRunStarted;
        public event Action OnStatsChanged;
        /// <summary>(section, parfaite, bonus)</summary>
        public event Action<ChartSection, bool, int> OnSectionChanged;
        public event Action OnDamaged;
        public event Action<LevelObject> OnLumPicked;
        /// <summary>vrai = niveau terminé, faux = mort.</summary>
        public event Action<bool> OnFinished;

        SongData song;
        RunJudgeStats judgeStats;
        bool sectionClean;
        int fxCursor;
        Conductor subscribed;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            if (Conductor.Instance != null)
            {
                subscribed = Conductor.Instance;
                subscribed.OnSongEnd += HandleSongEnd;
            }
            if (world != null && !IsRunning) world.SetActive(false);
        }

        void OnDestroy()
        {
            if (subscribed != null) subscribed.OnSongEnd -= HandleSongEnd;
            if (Instance == this) Instance = null;
        }

        public void BeginRun(SongData songData, bool demo)
        {
            StopAllCoroutines();
            if (songData == null || songData.chart == null)
            {
                Debug.LogError("RunManager: SongData sans chart.");
                return;
            }

            song = songData;
            IsDemo = demo;
            Chart = ChartData.Parse(song.chart);
            level.Build(Chart, song.bpm);

            TotalLums = level.Lums.Count;
            Score = Combo = Lums = 0;
            judgeStats = default;
            Completed = false;
            SectionIndex = 0;
            sectionClean = true;
            fxCursor = 0;

            world.SetActive(true);
            ApplyHero();
            ApplyBiome(0);
            player.Setup(level, this, CreateInput());
            player.Spawn(0);
            if (CameraRig.Instance != null)
            {
                CameraRig.Instance.Target = player.transform;
                CameraRig.Instance.Snap();
            }
            Conductor.Instance.Play(song, 0, loop: false);
            IsRunning = true;

            OnRunStarted?.Invoke();
            OnStatsChanged?.Invoke();
        }

        void ApplyHero()
        {
            var hero = GameSession.Hero != null ? GameSession.Hero : (level.Theme.heroes != null && level.Theme.heroes.Length > 0 ? level.Theme.heroes[0] : null);
            if (hero == null) return;
            if (heroAnimator != null) heroAnimator.Sprites = hero.sprites;
            if (bandana != null) bandana.Color = hero.accent;
        }

        void ApplyBiome(int index)
        {
            var biomes = level.Theme.biomes;
            if (backdrop != null && biomes != null && biomes.Length > 0)
                backdrop.SetBiome(biomes[Mathf.Clamp(index, 0, biomes.Length - 1)]);
        }

        /// <summary>Arrête la partie (changement d'écran). hideWorld = masquer et vider le niveau.</summary>
        public void StopRun(bool hideWorld, bool stopMusic = true)
        {
            StopAllCoroutines();
            IsRunning = false;
            if (stopMusic) Conductor.Instance?.Stop();
            if (hideWorld)
            {
                level.Clear();
                if (world != null) world.SetActive(false);
            }
        }

        /// <summary>Debug (F10) : l'IA joue à la place du joueur.</summary>
        public void SetAutoplay(bool on)
        {
            Autoplay = on;
            if (IsRunning && !IsDemo) player.SetInput(CreateInput(), Conductor.Instance.SongBeat);
        }

        IPlayerInput CreateInput() => IsDemo || Autoplay ? new AutoPlayerInput(level) : new ArcadePlayerInput(1);

        void Update()
        {
            if (!IsRunning || Chart == null) return;
            double beat = Conductor.Instance.SongBeat;

            int index = Chart.SectionIndexAt(beat);
            if (index > SectionIndex) EnterSection(index);

            var fx = level.FxEvents;
            while (fxCursor < fx.Count && fx[fxCursor].beat <= beat)
            {
                TriggerFx(fx[fxCursor]);
                fxCursor++;
            }
        }

        // ---------- Appelé par le PlayerController ----------

        public void OnLumCollected(LevelObject lum)
        {
            Lums++;
            Combo++;
            Score += lumPoints * Multiplier;
            ActionSfx.Instance?.Lum(lum.sequenceIndex);
            FxPool.Instance?.Burst(lum.bounds.center, new Color(1f, 0.82f, 0.23f), 8, 4f, 0.14f, 0.35f, 0f, 0f);
            if (lum.view != null) lum.view.SetActive(false);
            OnLumPicked?.Invoke(lum);
            OnStatsChanged?.Invoke();
        }

        public void OnEnemyKilled(LevelObject enemy, double actionBeat)
        {
            ScoreAction(enemy, actionBeat, enemyPoints);
            ActionSfx.Instance?.Kill();
            var biome = level.BiomeAt(enemy.beat);
            var color = biome != null ? biome.spark : Color.white;
            FxPool.Instance?.Burst(enemy.bounds.center, color, 26, 10f, 0.26f, 0.7f, 14f, 0.5f);
            FxPool.Instance?.Burst(enemy.bounds.center, Color.white, 10, 6f, 0.15f, 0.35f, 0f, 0f);
            CameraRig.Instance?.Shake(0.18f, 0.15f);
            CameraRig.Instance?.Punch(0.15f);
            if (enemy.view != null) enemy.view.SetActive(false);
            OnStatsChanged?.Invoke();
        }

        public void OnObstacleCleared(LevelObject obstacle, double actionBeat)
        {
            ScoreAction(obstacle, actionBeat, obstaclePoints);
            OnStatsChanged?.Invoke();
        }

        /// <summary>Juge l'action (écart appui ↔ beat de l'obstacle), ajoute les points et règle le combo.</summary>
        void ScoreAction(LevelObject o, double actionBeat, int basePoints)
        {
            double offset = (actionBeat - o.beat) * Conductor.Instance.SecondsPerBeat;
            var j = TimingJudge.Judge(offset);
            if (!ApplyJudgement(ref judgeStats, o, j)) return;
            if (j == Judgement.Meh) Combo = 0;
            else Combo++;
            Score += Mathf.RoundToInt(basePoints * TimingJudge.Factor(j) * Multiplier);
            OnJudged?.Invoke(j, new Vector2(player.transform.position.x, player.transform.position.y + 1.4f));
        }

        /// <summary>Compte un jugement ; faux si l'objet était déjà jugé (jamais deux fois).</summary>
        public static bool ApplyJudgement(ref RunJudgeStats stats, LevelObject o, Judgement j)
        {
            if (o.judged) return false;
            o.judged = true;
            switch (j)
            {
                case Judgement.Perfect: stats.perfect++; break;
                case Judgement.Good: stats.good++; break;
                default: stats.meh++; break;
            }
            return true;
        }

        /// <summary>Mort subite : pose de mort, la Sourdine se referme, puis l'écran de fin.</summary>
        public void Damage()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Combo = 0;
            sectionClean = false;

            player.Die();
            ActionSfx.Instance?.Hurt();
            CameraRig.Instance?.Shake(0.6f, 0.5f);
            CameraRig.Instance?.Flash(new Color(0.88f, 0.14f, 0.27f), 0.8f);
            Conductor.Instance.Stop();

            OnDamaged?.Invoke();
            OnStatsChanged?.Invoke();
            StartCoroutine(FinishAfterDeath());
        }

        IEnumerator FinishAfterDeath()
        {
            yield return new WaitForSecondsRealtime(deathDelay);
            Finish(false);
        }

        void EnterSection(int index)
        {
            bool perfect = sectionClean && !IsDemo;
            int bonus = perfect ? perfectSectionBonus : 0;
            Score += bonus;
            SectionIndex = index;
            sectionClean = true;

            ActionSfx.Instance?.Checkpoint();
            // Le drop : flash, secousse, et le décor change de monde pendant que l'écran est blanc.
            var biomeNow = level.BiomeAt(Chart.sections[index].startBeat);
            ApplyBiome(index);
            CameraRig.Instance?.Flash(biomeNow != null ? biomeNow.flash : Color.white, 1f);
            CameraRig.Instance?.Shake(0.35f, 0.45f);
            FxPool.Instance?.Burst(new Vector2(level.BeatToX(Chart.sections[index].startBeat), 1.2f), biomeNow != null ? biomeNow.spark : Color.white, 24, 7f, 0.2f, 0.6f, 8f, 0.5f);

            OnSectionChanged?.Invoke(Chart.sections[index], perfect, bonus);
            OnStatsChanged?.Invoke();
        }

        void TriggerFx(ChartEvent e)
        {
            var rig = CameraRig.Instance;
            if (rig == null) return;
            float intensity = e.@params.intensity > 0 ? e.@params.intensity : 1f;
            switch (e.type)
            {
                case "fx_flash":
                    rig.Flash(Color.white, 0.45f * intensity);
                    break;
                case "camera_shake":
                    float duration = (float)(Conductor.Instance.SecondsPerBeat * (e.@params.length > 0 ? e.@params.length : 0.5f));
                    rig.Shake(0.25f * intensity, duration);
                    break;
                case "camera_zoom":
                    rig.Punch(0.5f * intensity);
                    break;
                case "section_change":
                    rig.Shake(0.35f * intensity, 0.3f);
                    rig.Punch(0.4f * intensity);
                    rig.Flash(Color.white, 0.7f * intensity);
                    break;
            }
        }

        void HandleSongEnd()
        {
            if (!IsRunning) return;
            Completed = true;
            if (!IsDemo) Score += completionBonus;
            OnStatsChanged?.Invoke();
            Finish(true);
        }

        void Finish(bool completed)
        {
            IsRunning = false;
            OnFinished?.Invoke(completed);
            if (IsDemo) StartCoroutine(RestartDemo());
            else if (GameFlow.Instance != null) GameFlow.Instance.EndRun();
        }

        IEnumerator RestartDemo()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            BeginRun(song, demo: true);
        }
    }
}
