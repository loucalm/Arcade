using System.Collections.Generic;
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
    /// Une partie : construit le niveau, lance la musique, gère score et combo. Die and retry (ADR-033) : un coup
    /// (obstacle, chute) coûte une vie ; la suite dépend de la difficulté du héros (HeroProfile.retryMode, règles dans
    /// RetryRules) : reprise au dernier point de reprise (facile), le niveau qui recommence au début, le meilleur essai
    /// comptant (moyen), ou fin de partie à la première mort (dur). À 0 vie, la partie se termine.
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

        [Header("Règles")]
        [Tooltip("Temps (s) pendant lequel on voit la pose de mort avant l'écran de fin.")]
        [SerializeField] float deathDelay = 0.8f;
        [Header("Die and retry (ADR-033) : vies et difficulté dans le HeroProfile du héros choisi")]
        [Tooltip("Pause (s) entre la mort et la reprise (au point ou au début) : on voit la pose de mort, puis on repart.")]
        [SerializeField] float respawnDelay = 0.7f;
        [Tooltip("Bonus par vie restante quand on finit le niveau.")]
        [SerializeField] int lifeBonus = 500;
        [SerializeField] int comboPerMultiplier = 8;
        [SerializeField] int maxMultiplier = 8;

        [Header("Points (× multiplicateur)")]
        [SerializeField] int lumPoints = 10;
        [SerializeField] int enemyPoints = 50;
        [SerializeField] int obstaclePoints = 20;
        [SerializeField] int perfectSectionBonus = 500;
        [SerializeField] int completionBonus = 2000;

        /// <summary>Score de l'essai en cours ; en fin de partie, celui du meilleur essai (reprise au début).</summary>
        public int Score { get; private set; }
        /// <summary>Vies restantes (die and retry) : vies en reprise au point, essais en reprise au début, 1 en une seule vie.</summary>
        public int Lives { get; private set; }
        /// <summary>Difficulté de la partie : celle du héros choisi.</summary>
        public RetryMode Difficulty { get; private set; }
        /// <summary>Numéro de l'essai (1 au départ, +1 à chaque reprise au début).</summary>
        public int Attempt { get; private set; }
        /// <summary>Meilleur score des essais, celui en cours compris (reprise au début ; sinon = Score).</summary>
        public int BestScore => hasBestAttempt && bestAttempt.score > Score ? bestAttempt.score : Score;
        public int Combo { get; private set; }
        public int Lums { get; private set; }
        public int TotalLums { get; private set; }
        public int Multiplier => MultiplierFor(Combo, comboPerMultiplier, maxMultiplier);

        /// <summary>
        /// Paliers croissants : passer de ×m à ×(m+1) demande m × comboPerMultiplier / 2 actions de plus
        /// (avec 8 : ×2 à 4 actions, ×3 à 12, ×4 à 24 … ×8 à 112). Le ×8 arrive vers les 4/5 du niveau 01
        /// au lieu du premier quart : le multiplicateur reste un enjeu tout le morceau.
        /// </summary>
        public static int MultiplierFor(int combo, int comboPerMultiplier, int maxMultiplier)
        {
            int per = Mathf.Max(1, comboPerMultiplier);
            int m = 1;
            while (m < maxMultiplier && combo >= per * m * (m + 1) / 4) m++;
            return m;
        }
        public bool IsRunning { get; private set; }
        public bool IsDemo { get; private set; }
        public bool Completed { get; private set; }
        public bool Autoplay { get; private set; }
        public double SongLengthSeconds => song != null && song.clip != null ? song.clip.length - song.offsetSeconds : 1.0;
        public ChartData Chart { get; private set; }
        /// <summary>Biome en cours : celui du niveau (ou de la section, sans niveau).</summary>
        public BiomeTheme CurrentBiome => level.Theme != null && Chart != null ? level.BiomeAt(Chart.sections[SectionIndex].startBeat) : null;
        public int SectionIndex { get; private set; }
        public string SectionName => Chart != null ? Chart.sections[SectionIndex].name : "";

        /// <summary>Avancement 0..1 dans le morceau.</summary>
        /// <summary>Part du morceau (0..1) atteinte au beat donné (frise du HUD).</summary>
        public float ProgressAt(double beat)
        {
            var c = Conductor.Instance;
            if (c == null || song == null || song.clip == null) return 0f;
            double totalBeats = c.SecondsToBeat(song.clip.length - song.offsetSeconds);
            return totalBeats > 0 ? Mathf.Clamp01((float)(beat / totalBeats)) : 0f;
        }

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
                if (!ResultCompleted) return ratio >= 0.3f ? "BRONZE" : "AUCUNE";
                return ratio >= 0.9f ? "OR" : ratio >= 0.6f ? "ARGENT" : "BRONZE";
            }
        }

        public RunJudgeStats JudgeStats => judgeStats;
        /// <summary>Note de fin de niveau (S+ … D), celle de l'essai retenu.</summary>
        public string Grade => GradeCalculator.Grade(judgeStats.perfect, judgeStats.good, judgeStats.meh, Lums, TotalLums, ResultCompleted);

        /// <summary>L'essai retenu a fini le niveau (faux si c'est un essai précédent, mort en route, qui a fait mieux).</summary>
        bool ResultCompleted => Completed && !keptEarlierAttempt;

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
        /// <summary>Une surprise arrive dans une mesure (portail, arrière-plan, écrans, lianes) : texte d'annonce.</summary>
        public event Action<string> OnWarning;

        // annonces des surprises (télégraphie, docs/level-design-musical.md § 6) : (beat d'affichage, texte)
        readonly List<(double beat, string text)> warnings = new();
        int warnCursor;

        SongData song;
        RunJudgeStats judgeStats;
        bool sectionClean;
        int fxCursor;
        Conductor subscribed;

        /// <summary>Un essai fini par une mort (reprise au début) : le meilleur est retenu en fin de partie.</summary>
        struct AttemptResult
        {
            public int score, lums;
            public RunJudgeStats judges;
        }
        AttemptResult bestAttempt;
        bool hasBestAttempt;
        bool keptEarlierAttempt;      // fin de partie : un essai précédent a fait mieux que le dernier
        double runStartBeat;          // beat de départ de la partie (0, sauf test d'un passage dans l'éditeur)

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

        /// <summary>Niveau en cours (monde, morceau, chart).</summary>
        public LevelDef Level { get; private set; }

        /// <summary>Lance un niveau : son monde, son morceau, sa chart.</summary>
        public void BeginRun(LevelDef def, bool demo)
        {
            if (def == null || def.song == null || def.Chart == null)
            {
                Debug.LogError("RunManager: niveau sans morceau ou sans chart.");
                return;
            }
            Level = def;
            StartRun(def.song, def.Chart, def.biome, demo);
        }

        /// <summary>Lance un morceau seul (tests, ancienne démo) : un biome par section.</summary>
        public void BeginRun(SongData songData, bool demo)
        {
            if (songData == null || songData.chart == null)
            {
                Debug.LogError("RunManager: SongData sans chart.");
                return;
            }
            Level = null;
            StartRun(songData, songData.chart, null, demo);
        }

        void StartRun(SongData songData, TextAsset chart, BiomeTheme biome, bool demo)
        {
            StopAllCoroutines();
            song = songData;
            IsDemo = demo;
            Chart = ChartData.Parse(chart);
            level.Build(Chart, song.bpm, biome);

            TotalLums = level.Lums.Count;
            ResetAttemptStats();
            BuildWarnings();

            // difficulté du héros choisi : vies (ou essais) au départ et suite d'une mort (RetryRules)
            var hero = CurrentHero();
            Difficulty = hero != null ? hero.retryMode : RetryMode.Checkpoint;
            Lives = hero != null ? hero.StartLives : RetryRules.DefaultLives(Difficulty);
            Attempt = 1;
            bestAttempt = default;
            hasBestAttempt = keptEarlierAttempt = false;

            world.SetActive(true);
            ApplyHero(hero);
            ApplyBiome(0);
            if (backdrop != null)
            {
                backdrop.ResetMood();
                backdrop.SetSection(Chart.sections.Length > 0 ? Chart.sections[0].name : "", 0f, 0.01f);
            }
            // test d'un passage dans l'éditeur (menu Kenta > Tester le refrain) : on démarre au beat demandé
            ScreenGlitch.Instance?.ResetAll();
            double start = demo ? 0 : DevStart.Beat;
            runStartBeat = start;
            if (start > 0) PrepareStartAt(start);
            player.Setup(level, this, CreateInput());
            player.Spawn(start);
            if (CameraRig.Instance != null)
            {
                CameraRig.Instance.Target = player.transform;
                CameraRig.Instance.HeightProbe = level.HeightAt;
                CameraRig.Instance.Rail = level.RailAt;
                CameraRig.Instance.ResetShot(Chart.zoom);
                CameraRig.Instance.Snap();
            }
            Conductor.Instance.Play(song, start, loop: false);
            IsRunning = true;

            OnRunStarted?.Invoke();
            OnStatsChanged?.Invoke();
        }

        /// <summary>Score, combo, pièces, jugements, section et curseurs remis à zéro : départ de la partie ou d'un nouvel essai.</summary>
        void ResetAttemptStats()
        {
            Score = Combo = Lums = 0;
            judgeStats = default;
            Completed = false;
            SectionIndex = 0;
            sectionClean = true;
            fxCursor = 0;
            warnCursor = 0;
        }

        /// <summary>
        /// Met le niveau dans l'état d'un beat donné (départ de test ou reprise après une mort) : section et biome, effets
        /// d'écran en cours (copies, image retournée), curseur des effets, et rien qui chevauche le point de départ.
        /// </summary>
        void PrepareStartAt(double start)
        {
            int section = Mathf.Max(0, Chart.SectionIndexAt(start));
            if (section != SectionIndex || start <= 0)
            {
                SectionIndex = section;
                ApplyBiome(SectionIndex);
                if (backdrop != null && Chart.sections.Length > 0) backdrop.SetSection(Chart.sections[SectionIndex].name, Progress, 0.01f);
            }
            float sx = level.BeatToX(start);
            foreach (var o in level.Obstacles)
                if (o.bounds.xMin < sx + 0.5f && o.bounds.xMax > sx - 0.5f && o.kind != LevelObjectKind.Gap)
                {
                    o.alive = false;
                    if (o.view != null) o.view.SetActive(false);
                }
            var glitch = ScreenGlitch.Instance;
            glitch?.ResetAll();
            fxCursor = 0;
            while (fxCursor < level.FxEvents.Count && level.FxEvents[fxCursor].beat < start)
            {
                var e = level.FxEvents[fxCursor++];
                if (e.type == "screen_split") glitch?.SetCopies(e.@params.copies);
                if (e.type == "screen_flip")
                    glitch?.SetFlip(e.@params.variant == "mirror" || e.@params.variant == "both", e.@params.variant == "up" || e.@params.variant == "both");
            }
            // annonces des surprises : on reprend à partir de ce beat (une reprise rejoue celles qui suivent le point)
            warnCursor = 0;
            while (warnCursor < warnings.Count && warnings[warnCursor].beat < start) warnCursor++;
        }

        /// <summary>Reprise au point (héros facile) : cp = le dernier point sûr de la chart avant la mort (RetryRules.CheckpointBefore).</summary>
        IEnumerator RespawnAfterDeath(double cp)
        {
            yield return new WaitForSecondsRealtime(respawnDelay);
            // on rejoue depuis le point de reprise : obstacles, ennemis, tambours, lianes et notes reviennent
            // (les pièces déjà prises restent prises)
            level.ResetObstaclesFrom((float)cp);
            PrepareStartAt(cp);
            player.Spawn(cp);
            // entrée recalée sur le point de reprise : sinon l'autoplay (démo, F10) garde son curseur au beat de la mort
            // et ne rejoue pas les actions d'après le point (il meurt en boucle)
            player.SetInput(CreateInput(), cp);
            CameraRig.Instance?.Snap();
            Conductor.Instance.Play(song, cp, loop: false);
            Combo = 0;
            IsRunning = true;
            CameraRig.Instance?.Flash(Color.white, 0.5f);
            OnStatsChanged?.Invoke();
        }

        /// <summary>
        /// Reprise au début (héros moyen) : le niveau recommence à son beat de départ comme une partie neuve, sans repasser
        /// par les écrans. Tout le niveau revient (pièces comprises) ; musique, effets d'écran, décor, section, score,
        /// combo et jugements de l'essai repartent de zéro. Les vies (essais) et le meilleur essai sont gardés.
        /// </summary>
        IEnumerator RestartAfterDeath(double start)
        {
            yield return new WaitForSecondsRealtime(respawnDelay);
            level.ResetObstaclesFrom(float.MinValue);         // obstacles, ennemis, cuirassés, tambours, lianes, notes
            var lums = level.Lums;
            for (int i = 0; i < lums.Count; i++) lums[i].ResetState();
            var platforms = level.Platforms;                  // atterrissages : rejugés (sans vue, rien d'autre à rallumer)
            for (int i = 0; i < platforms.Count; i++) platforms[i].ResetState();
            Attempt++;
            ResetAttemptStats();
            ApplyBiome(0);
            if (backdrop != null)
            {
                backdrop.ResetMood();
                backdrop.SetSection(Chart.sections.Length > 0 ? Chart.sections[0].name : "", 0f, 0.01f);
            }
            ScreenGlitch.Instance?.ResetAll();
            if (start > 0) PrepareStartAt(start);
            player.Spawn(start);
            player.SetInput(CreateInput(), start);           // l'autoplay (F10) repart aussi du début
            if (CameraRig.Instance != null)
            {
                CameraRig.Instance.ResetShot(Chart.zoom);
                CameraRig.Instance.Snap();
            }
            Conductor.Instance.Play(song, start, loop: false);
            IsRunning = true;
            CameraRig.Instance?.Flash(Color.white, 0.5f);
            OnRunStarted?.Invoke();                           // le HUD repart de zéro (score, intitulé, frise)
            OnStatsChanged?.Invoke();
        }

        /// <summary>Essai fini par une mort (reprise au début) : on le garde s'il fait au moins aussi bien que le meilleur.</summary>
        void RecordAttempt()
        {
            if (!RetryRules.KeepEarlier(hasBestAttempt, bestAttempt.score, Score))
                bestAttempt = new AttemptResult { score = Score, lums = Lums, judges = judgeStats };
            hasBestAttempt = true;
        }

        /// <summary>
        /// Fin de partie : si un essai précédent a fait mieux que le dernier (reprise au début), c'est lui qui compte :
        /// son score (envoyé au classement), ses pièces, ses jugements et sa note.
        /// </summary>
        void KeepBestAttempt()
        {
            keptEarlierAttempt = !IsDemo && RetryRules.KeepEarlier(hasBestAttempt, bestAttempt.score, Score);
            if (!keptEarlierAttempt) return;
            Score = bestAttempt.score;
            Lums = bestAttempt.lums;
            judgeStats = bestAttempt.judges;
        }

        /// <summary>Héros de la partie : celui choisi, sinon le premier du thème (démo, test d'un passage).</summary>
        HeroProfile CurrentHero()
        {
            if (GameSession.Hero != null) return GameSession.Hero;
            var heroes = level.Theme != null ? level.Theme.heroes : null;
            return heroes != null && heroes.Length > 0 ? heroes[0] : null;
        }

        void ApplyHero(HeroProfile hero)
        {
            if (hero == null) return;
            if (heroAnimator != null) heroAnimator.Sprites = hero.sprites;
            if (bandana != null) bandana.Color = hero.accent;
            if (ActionSfx.Instance != null) ActionSfx.Instance.HeroHit = hero.hitSound;
        }

        void ApplyBiome(int index)
        {
            if (level.LevelBiome != null || !string.IsNullOrEmpty(Chart.sections[Mathf.Clamp(index, 0, Chart.sections.Length - 1)].biome))
            {
                // un niveau = un monde, sauf les sections qui en changent (BiomeAt lit le biome de la section)
                if (backdrop != null) backdrop.SetBiome(level.BiomeAt(Chart.sections[Mathf.Clamp(index, 0, Chart.sections.Length - 1)].startBeat));
                return;
            }
            var biomes = level.Theme.biomes;
            if (backdrop != null && biomes != null && biomes.Length > 0)
                backdrop.SetBiome(biomes[Mathf.Clamp(index, 0, biomes.Length - 1)]);
        }

        /// <summary>Arrête la partie (changement d'écran). hideWorld = masquer et vider le niveau.</summary>
        public void StopRun(bool hideWorld, bool stopMusic = true)
        {
            StopAllCoroutines();
            IsRunning = false;
            ScreenGlitch.Instance?.ResetAll();
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

            while (warnCursor < warnings.Count && warnings[warnCursor].beat <= beat)
                OnWarning?.Invoke(warnings[warnCursor++].text);

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
            Lums++;                            // les pièces rapportent des points, mais seul le timing des actions fait le combo
            Score += lumPoints * Multiplier;
            if (lum.hasNote) ActionSfx.Instance?.LumNote(lum.note);
            else ActionSfx.Instance?.Lum(lum.sequenceIndex);
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

        /// <summary>Ennemi cuirassé écrasé : effets seulement (le jugement vient de l'appui du grand rebond).</summary>
        public void OnEnemyStomped(LevelObject enemy)
        {
            ActionSfx.Instance?.Land();                 // un tremplin, pas une mort
            var biome = level.BiomeAt(enemy.beat);
            FxPool.Instance?.Burst(new Vector2(enemy.bounds.center.x, enemy.bounds.yMax), biome != null ? biome.spark : Color.white, 18, 7f, 0.2f, 0.5f, 10f, 0.3f);
            CameraRig.Instance?.Punch(0.25f);
            // le cuirassé reste en place (tremplin) : son ressort s'écrase
            if (enemy.view != null)
                foreach (var bump in enemy.view.GetComponentsInChildren<PressBump>()) bump.Press();
        }

        public void OnObstacleCleared(LevelObject obstacle, double actionBeat)
        {
            ScoreAction(obstacle, actionBeat, obstaclePoints);
            OnStatsChanged?.Invoke();
        }

        /// <summary>Juge l'action (écart appui ↔ beat de l'obstacle), ajoute les points et règle le combo.</summary>
        void ScoreAction(LevelObject o, double actionBeat, int basePoints)
        {
            if (o.silent) return;
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

        /// <summary>
        /// Mort : pose de mort, une vie de moins, puis selon la difficulté du héros (RetryRules.Decide) : reprise au
        /// dernier point (ADR-033), reprise au début du niveau, ou écran de fin.
        /// </summary>
        public void Damage()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Combo = 0;
            sectionClean = false;

            double deathBeat = Conductor.Instance.SongBeat;
            player.Die();
            ActionSfx.Instance?.Hurt();
            CameraRig.Instance?.Shake(0.6f, 0.5f);
            CameraRig.Instance?.Flash(new Color(0.88f, 0.14f, 0.27f), 0.8f);
            Conductor.Instance.Stop();

            // die and retry : une vie de moins ; la difficulté du héros dit la suite. La démo ne perd jamais de vie.
            if (!IsDemo) Lives = Mathf.Max(0, Lives - 1);
            var next = IsDemo ? RetryDecision.End : RetryRules.Decide(Difficulty, Lives, deathBeat, Chart.checkpoints, runStartBeat);
            if (next.action == RetryAction.Restart) RecordAttempt();      // cet essai est fini : on garde le meilleur
            OnDamaged?.Invoke();
            OnStatsChanged?.Invoke();
            switch (next.action)
            {
                case RetryAction.Resume: StartCoroutine(RespawnAfterDeath(next.beat)); break;
                case RetryAction.Restart: StartCoroutine(RestartAfterDeath(next.beat)); break;
                default: StartCoroutine(FinishAfterDeath()); break;
            }
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

            // Le drop (flash, secousse, son, gerbe) seulement quand le monde change : un niveau = un monde, donc ses
            // sections ne font que l'accent discret de l'événement `section_change` de la chart (TriggerFx).
            var biomeNow = level.BiomeAt(Chart.sections[index].startBeat);
            bool newWorld = biomeNow != null && backdrop != null && biomeNow != backdrop.Current;
            ApplyBiome(index);
            UpdateScenery(index);
            if (newWorld)
            {
                ActionSfx.Instance?.Checkpoint();
                CameraRig.Instance?.Flash(biomeNow.flash, 1f);
                CameraRig.Instance?.Shake(0.35f, 0.45f);
                FxPool.Instance?.Burst(new Vector2(level.BeatToX(Chart.sections[index].startBeat), 1.2f), biomeNow.spark, 24, 7f, 0.2f, 0.6f, 8f, 0.5f);
            }

            OnSectionChanged?.Invoke(Chart.sections[index], perfect, bonus);
            OnStatsChanged?.Invoke();
        }

        /// <summary>
        /// Le décor suit le morceau : ambiance de la section (teinte, lumière qui baisse). Le gardien reste immobile (ADR-028).
        /// </summary>
        void UpdateScenery(int index)
        {
            if (backdrop == null) return;
            var section = Chart.sections[index];
            backdrop.SetSection(section.name, Progress);
        }

        /// <summary>
        /// Chaque surprise s'annonce une mesure avant (audit du 25/11, D2) : la première de chaque série seulement
        /// (portail de gravité, passage à l'arrière-plan, écrans dédoublés ou retournés, lianes).
        /// </summary>
        void BuildWarnings()
        {
            warnings.Clear();
            warnCursor = 0;
            if (Chart?.events == null) return;
            int perBar = Chart.beatsPerBar > 0 ? Chart.beatsPerBar : 4;
            double lastBack = -99, lastVine = -99;
            foreach (var e in Chart.events)
            {
                string text = null;
                switch (e.type)
                {
                    case "gravity": text = e.lane == 1 ? "▲ LE MONDE SE RETOURNE" : "▼ RETOUR AU SOL"; break;
                    case "back":
                        if (e.beat - lastBack > 8) text = "► VERS L'ARRIÈRE-PLAN";
                        lastBack = e.beat;
                        break;
                    case "vine":
                        if (e.beat - lastVine > 8) text = "LIANES : SAUTE POUR LÂCHER";
                        lastVine = e.beat;
                        break;
                    case "screen_split":
                        if (e.@params != null && e.@params.copies > 1) text = "L'ÉCRAN SE DÉDOUBLE !";
                        break;
                    case "screen_flip":
                        string mode = e.@params != null ? e.@params.variant : null;
                        if (mode == "mirror") text = "◄ L'ÉCRAN EN MIROIR";
                        else if (mode == "up") text = "▼ L'ÉCRAN SE RETOURNE";
                        else if (mode == "both") text = "L'ÉCRAN FAIT DEMI-TOUR !";
                        break;
                }
                if (text != null) warnings.Add((e.beat - perBar, text));
            }
            warnings.Sort((a, b) => a.beat.CompareTo(b.beat));
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
                case "camera":
                    // nouveau plan : zoom (coupe nette, sur le temps) et place du héros à l'écran ; plus de flash :
                    // le niveau change de plan une vingtaine de fois (vue 480, plan large 640 sur les tambours)
                    rig.SetShot(e.@params.zoom, e.@params.frameX, e.@params.frameY);
                    break;
                case "section_change":
                    // accent sur le premier temps de la section, sans flash plein écran (il masquait les obstacles)
                    rig.Shake(0.12f * intensity, 0.2f);
                    rig.Punch(0.35f * intensity);
                    break;
                case "screen_split":
                    // l'image se dédouble (2, 5 ou 9 copies plus petites) jusqu'au prochain screen_split
                    ScreenGlitch.Instance?.SetCopies(e.@params.copies);
                    rig.Flash(Color.white, 0.3f);
                    break;
                case "screen_flip":
                    // l'image se retourne sur le début de phrase : « mirror » (gauche-droite), « up » (tête en bas),
                    // « both » (demi-tour), autre chose = retour à l'endroit
                    ScreenGlitch.Instance?.SetFlip(e.@params.variant == "mirror" || e.@params.variant == "both",
                                                   e.@params.variant == "up" || e.@params.variant == "both");
                    rig.Flash(Color.white, 0.35f);
                    rig.Shake(0.15f, 0.2f);
                    break;
                case "glitch":
                    // grésillement calé sur les temps, pendant `length` beats
                    ScreenGlitch.Instance?.Glitch(e.@params.intensity > 0 ? e.@params.intensity : 0.5f, e.@params.length > 0 ? e.@params.length : 4f);
                    break;
            }
        }

        void HandleSongEnd()
        {
            if (!IsRunning) return;
            Completed = true;
            if (!IsDemo) Score += completionBonus + Lives * lifeBonus;
            OnStatsChanged?.Invoke();
            Finish(true);
        }

        void Finish(bool completed)
        {
            IsRunning = false;
            KeepBestAttempt();                       // reprise au début : le meilleur essai est celui qui compte
            if (keptEarlierAttempt) OnStatsChanged?.Invoke();
            OnFinished?.Invoke(completed);
            if (IsDemo) StartCoroutine(RestartDemo());
            else if (GameFlow.Instance != null) GameFlow.Instance.EndRun();
        }

        IEnumerator RestartDemo()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            if (Level != null) BeginRun(Level, demo: true);
            else BeginRun(song, demo: true);
        }
    }
}
