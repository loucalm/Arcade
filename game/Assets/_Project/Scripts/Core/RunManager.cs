using System;
using System.Collections;
using RythmeRunner.FX;
using RythmeRunner.Level;
using RythmeRunner.Player;
using RythmeRunner.Rhythm;
using UnityEngine;

namespace RythmeRunner.Core
{
    /// <summary>
    /// Une partie : construit le niveau, lance la musique, gère cœurs, score, combo, checkpoints
    /// (une section de la chart = un checkpoint) et respawn resynchronisé sur l'audio.
    /// Mode démo = autoplay sans perte de cœur (écran Attract).
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        [SerializeField] LevelBuilder level;
        [SerializeField] PlayerController player;
        [Tooltip("Racine du monde de jeu (niveau + joueur), masquée hors partie.")]
        [SerializeField] GameObject world;

        [Header("Règles")]
        [SerializeField] int startHearts = 3;
        [SerializeField] float respawnDelay = 1.3f;
        [SerializeField] int comboPerMultiplier = 8;
        [SerializeField] int maxMultiplier = 8;

        [Header("Points (× multiplicateur)")]
        [SerializeField] int lumPoints = 10;
        [SerializeField] int enemyPoints = 50;
        [SerializeField] int obstaclePoints = 20;
        [SerializeField] int perfectSectionBonus = 500;
        [SerializeField] int completionBonusPerHeart = 1000;

        public int Score { get; private set; }
        public int Hearts { get; private set; }
        public int Combo { get; private set; }
        public int Lums { get; private set; }
        public int TotalLums { get; private set; }
        public int Deaths { get; private set; }
        public int Multiplier => Mathf.Min(1 + Combo / Mathf.Max(1, comboPerMultiplier), maxMultiplier);
        public int StartHearts => startHearts;
        public bool IsRunning { get; private set; }
        public bool IsDemo { get; private set; }
        public bool Completed { get; private set; }
        public bool Autoplay { get; private set; }
        public ChartData Chart { get; private set; }
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

        public event Action OnRunStarted;
        public event Action OnStatsChanged;
        /// <summary>(section, parfaite, bonus)</summary>
        public event Action<ChartSection, bool, int> OnSectionChanged;
        public event Action OnDamaged;
        public event Action OnRespawned;
        public event Action<LevelObject> OnLumPicked;
        /// <summary>vrai = niveau terminé, faux = plus de cœurs.</summary>
        public event Action<bool> OnFinished;

        SongData song;
        int checkpointScore, checkpointLums;
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
            Score = Combo = Lums = Deaths = 0;
            Hearts = startHearts;
            Completed = false;
            SectionIndex = 0;
            checkpointScore = checkpointLums = 0;
            sectionClean = true;
            fxCursor = 0;

            world.SetActive(true);
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

        /// <summary>Arrête la partie (changement d'écran). hideWorld = masquer et vider le niveau.</summary>
        public void StopRun(bool hideWorld)
        {
            StopAllCoroutines();
            IsRunning = false;
            Conductor.Instance?.Stop();
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
            FxPool.Instance?.Burst(lum.bounds.center, level.Theme.lum, 8, 4f, 0.14f, 0.35f, 0f, 0f);
            if (lum.view != null) lum.view.SetActive(false);
            OnLumPicked?.Invoke(lum);
            OnStatsChanged?.Invoke();
        }

        public void OnEnemyKilled(LevelObject enemy)
        {
            Combo++;
            Score += enemyPoints * Multiplier;
            ActionSfx.Instance?.Kill();
            var color = enemy.kind == LevelObjectKind.Enemy ? level.Theme.enemy : level.Theme.block;
            FxPool.Instance?.Burst(enemy.bounds.center, color, 26, 10f, 0.26f, 0.7f, 14f, 0.5f);
            FxPool.Instance?.Burst(enemy.bounds.center, Color.white, 10, 6f, 0.15f, 0.35f, 0f, 0f);
            CameraRig.Instance?.Shake(0.18f, 0.15f);
            CameraRig.Instance?.Punch(0.15f);
            if (enemy.view != null) enemy.view.SetActive(false);
            OnStatsChanged?.Invoke();
        }

        public void OnObstacleCleared(LevelObject obstacle)
        {
            Combo++;
            Score += obstaclePoints * Multiplier;
            OnStatsChanged?.Invoke();
        }

        public void Damage()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Deaths++;
            if (!IsDemo) Hearts--;
            Combo = 0;
            sectionClean = false;

            player.Die();
            ActionSfx.Instance?.Hurt();
            CameraRig.Instance?.Shake(0.6f, 0.5f);
            CameraRig.Instance?.Flash(level.Theme.enemy, 1f);
            Conductor.Instance.Stop();

            OnDamaged?.Invoke();
            OnStatsChanged?.Invoke();
            StartCoroutine(RespawnAfterDelay());
        }

        IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSecondsRealtime(respawnDelay);
            if (Hearts <= 0)
            {
                Finish(false);
                yield break;
            }

            float checkpointBeat = Chart.sections[SectionIndex].startBeat;
            Score = checkpointScore;
            Lums = checkpointLums;
            level.ResetFrom(checkpointBeat);
            player.Spawn(checkpointBeat);
            fxCursor = 0;
            while (fxCursor < level.FxEvents.Count && level.FxEvents[fxCursor].beat < checkpointBeat) fxCursor++;
            Conductor.Instance.Play(song, checkpointBeat, loop: false);
            CameraRig.Instance?.Snap();
            IsRunning = true;

            OnRespawned?.Invoke();
            OnStatsChanged?.Invoke();
        }

        void EnterSection(int index)
        {
            bool perfect = sectionClean && !IsDemo;
            int bonus = perfect ? perfectSectionBonus : 0;
            Score += bonus;
            SectionIndex = index;
            checkpointScore = Score;
            checkpointLums = Lums;
            sectionClean = true;

            ActionSfx.Instance?.Checkpoint();
            CameraRig.Instance?.Flash(level.Theme.checkpoint, 0.8f);
            FxPool.Instance?.Burst(new Vector2(level.BeatToX(Chart.sections[index].startBeat), 4.8f), level.Theme.checkpoint, 20, 7f, 0.2f, 0.6f, 8f, 0.5f);

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
                    rig.Flash(level.Theme.backgroundFlash * 2f, intensity);
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
                    rig.Flash(level.Theme.wall, intensity);
                    break;
            }
        }

        void HandleSongEnd()
        {
            if (!IsRunning) return;
            Completed = true;
            if (!IsDemo) Score += Hearts * completionBonusPerHeart;
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
