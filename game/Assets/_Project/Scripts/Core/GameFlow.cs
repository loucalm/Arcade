using System;
using Anatidae;
using Kenta.FX;
using Kenta.Level;
using Kenta.Rhythm;
using Kenta.UI;
using UnityEngine;

namespace Kenta.Core
{
    public enum GameState { Attract, Explain, Config, Playing, GameOver, NameEntry }

    /// <summary>
    /// Machine à états du flow d'écrans imposé (docs/game-design.md) :
    /// Attract+HighScore → Explain → Config (héros et monde) → Playing → GameOver (+ classement) → NameEntry si top 10 → Attract.
    /// L'accueil tourne en trois temps : logo, démo jouée en autoplay (un monde différent à chaque fois), classement.
    /// Rejouer dans la minute saute les consignes, et chaque écran de fin se passe avec le bouton 1.
    /// Un GameObject « écran » par état, dans le canvas pixel 320×180. Scène unique Main (ADR-007).
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        enum AttractPhase { Logo, Demo, Board }

        [Header("Écrans (un GameObject racine par état)")]
        [SerializeField] GameObject attractScreen;
        [SerializeField] GameObject explainScreen;
        [SerializeField] GameObject configScreen;
        [SerializeField] GameObject playingScreen;
        [SerializeField] GameObject gameOverScreen;
        [SerializeField] HeroSelectScreen heroSelect;
        [Tooltip("Classement en pixel art (remplace l'affichage du toolkit).")]
        [SerializeField] HighscoreBoard highscoreBoard;
        [Tooltip("Saisie des 3 lettres en pixel art (l'envoi passe par HighscoreManager.SetHighscore).")]
        [SerializeField] NameEntryScreen nameEntry;

        [Header("Monde et effets")]
        [SerializeField] LevelTheme theme;
        [SerializeField] BiomeBackdrop backdrop;

        [Header("Musique")]
        [SerializeField] SongData levelSong;
        [Tooltip("Mondes proposés : choisis à l'écran du héros, et tour à tour dans la démo de l'accueil.")]
        [SerializeField] LevelCatalog catalog;
        [Tooltip("Boucle de menu ; si vide, le morceau du niveau tourne en boucle, plus bas.")]
        [SerializeField] SongData attractSong;
        [SerializeField, Range(0f, 1f)] float attractVolume = 0.5f;

        [Header("Durées (s)")]
        [SerializeField] float attractLogoSeconds = 8f;
        [SerializeField] float attractDemoSeconds = 20f;
        [SerializeField] float attractBoardSeconds = 8f;
        [SerializeField] float explainDuration = 6f;
        [SerializeField] float gameOverDuration = 4.5f;
        [SerializeField] float gameOverHighscoreDuration = 3.5f;
        [Tooltip("Délai avant d'accepter une validation, pour éviter qu'un appui en rafale saute un écran.")]
        [SerializeField] float inputGuard = 0.5f;
        [Tooltip("Délai avant de pouvoir passer l'écran de fin au bouton 1 (le temps de voir la note).")]
        [SerializeField] float skipGuard = 1.5f;
        [SerializeField] float attractScroll = 1.4f;

        public GameState State { get; private set; }
        public int Score => RunManager.Instance != null ? RunManager.Instance.Score : 0;
        public event Action<GameState> OnStateChanged;

        float stateTimer, phaseTimer;
        AttractPhase phase;
        bool showingBoard;
        int demoIndex;
        AttractScreen attract;
        bool menuLooping;            // la musique de menu tourne en boucle (faux pendant une partie ou la démo)

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (attractScreen != null) attract = attractScreen.GetComponent<AttractScreen>();
        }

        // Start (et pas Awake) : tous les Awake, dont ceux du Conductor et du RunManager, sont passés.
        void Start()
        {
            StartCoroutine(HighscoreManager.FetchHighscores());
            if (heroSelect != null) heroSelect.OnLevelChanged += ShowWorld;
            if (DevStart.Active) StartCoroutine(DevPlay());   // test d'un passage : droit au jeu
            else Enter(GameState.Attract);
        }

        /// <summary>
        /// Test d'un passage (DevStart) : on laisse passer les premières images (longues, juste après l'entrée en Play)
        /// et le chargement de la musique, sinon l'horloge audio saute de plusieurs beats au départ.
        /// </summary>
        System.Collections.IEnumerator DevPlay()
        {
            var lvl = GameSession.Level != null ? GameSession.Level : FirstLevel();
            var clip = lvl != null && lvl.song != null ? lvl.song.clip : (levelSong != null ? levelSong.clip : null);
            if (clip != null) clip.LoadAudioData();
            while (Time.unscaledTime < 0.5f || (clip != null && clip.loadState == AudioDataLoadState.Loading)) yield return null;
            Enter(GameState.Playing);
        }

        void OnDestroy()
        {
            if (heroSelect != null) heroSelect.OnLevelChanged -= ShowWorld;
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            stateTimer += dt;
            phaseTimer += dt;
            bool guardPassed = stateTimer >= inputGuard;

            switch (State)
            {
                case GameState.Attract:
                    if (ArcadeInput.AnyButtonDown()) { Enter(GameState.Explain); break; }
                    float length = phase == AttractPhase.Logo ? attractLogoSeconds : phase == AttractPhase.Demo ? attractDemoSeconds : attractBoardSeconds;
                    if (phaseTimer >= length) EnterAttractPhase(phase == AttractPhase.Logo ? AttractPhase.Demo : phase == AttractPhase.Demo ? AttractPhase.Board : AttractPhase.Logo);
                    break;

                case GameState.Explain:
                    if (stateTimer >= explainDuration || (guardPassed && ArcadeInput.ConfirmDown()))
                        Enter(GameState.Config);
                    break;

                case GameState.Config:
                    if (heroSelect == null || heroSelect.Done) Enter(GameState.Playing);
                    break;

                case GameState.Playing:
                    // La fin de partie vient du RunManager (mort ou fin du morceau) → EndRun().
                    break;

                case GameState.GameOver:
                    bool skip = phaseTimer >= skipGuard && ArcadeInput.ConfirmDown();
                    if (DevStart.Active && skip) { Enter(GameState.Playing); break; }   // test : on relance au même passage
                    if (!showingBoard && (stateTimer >= gameOverDuration || skip))
                    {
                        showingBoard = true;
                        phaseTimer = 0f;
                        SetActive(gameOverScreen, false);
                        ShowBoard(true);
                    }
                    else if (showingBoard && (phaseTimer >= gameOverHighscoreDuration || (phaseTimer >= inputGuard * 2f && ArcadeInput.ConfirmDown())))
                        Enter(IsTopTen(Score) ? GameState.NameEntry : GameState.Attract);
                    break;

                case GameState.NameEntry:
                    if (nameEntry == null || nameEntry.Done) FinishNameEntry();
                    break;
            }
        }

        public void Enter(GameState next)
        {
            // rejouer dans la minute : les consignes sont déjà connues
            if (next == GameState.Explain && GameSession.IsQuickRetry) next = GameState.Config;

            var previous = State;
            State = next;
            stateTimer = phaseTimer = 0f;
            showingBoard = false;
            if (next == GameState.Config && heroSelect != null) heroSelect.Setup(catalog);   // avant son OnEnable

            SetActive(attractScreen, next == GameState.Attract);
            SetActive(explainScreen, next == GameState.Explain);
            SetActive(configScreen, next == GameState.Config);
            SetActive(playingScreen, next == GameState.Playing);
            SetActive(gameOverScreen, next == GameState.GameOver);
            SetActive(nameEntry != null ? nameEntry.gameObject : null, next == GameState.NameEntry);
            ShowBoard(false);

            var conductor = Conductor.Instance;
            var run = RunManager.Instance;
            var rig = CameraRig.Instance;
            switch (next)
            {
                case GameState.Attract:
                    // après la saisie du nom, on montre d'abord le classement (le nouveau score pulse en or)
                    EnterAttractPhase(previous == GameState.NameEntry ? AttractPhase.Board : AttractPhase.Logo);
                    break;

                case GameState.Explain:
                case GameState.Config:
                    run?.StopRun(hideWorld: true, stopMusic: false);
                    ShowMenuWorld(scroll: true);
                    PlayMenuMusic();
                    break;

                case GameState.Playing:
                    if (rig != null) rig.AutoScroll = 0f;
                    if (backdrop != null) backdrop.SetGroundVisible(false);
                    conductor?.SetVolume(1f);
                    menuLooping = false;
                    var lvl = GameSession.Level != null ? GameSession.Level : FirstLevel();
                    if (lvl != null) run?.BeginRun(lvl, demo: false);
                    else run?.BeginRun(levelSong, demo: false);
                    break;

                case GameState.GameOver:
                    conductor?.Stop();
                    GameSession.LastRunEnd = Time.unscaledTime;
                    if (highscoreBoard != null) highscoreBoard.ClearHighlight();
                    break;

                case GameState.NameEntry:
                    if (nameEntry != null) nameEntry.Begin(Score, HighscoreManager.PlayerName);
                    break;
            }

            OnStateChanged?.Invoke(next);
        }

        void EnterAttractPhase(AttractPhase p)
        {
            phase = p;
            phaseTimer = 0f;
            var run = RunManager.Instance;
            var rig = CameraRig.Instance;
            SetActive(attractScreen, p != AttractPhase.Board);
            ShowBoard(p == AttractPhase.Board);
            if (attract != null) attract.SetDemo(p == AttractPhase.Demo);

            if (p == AttractPhase.Demo && run != null && catalog != null && catalog.levels != null && catalog.levels.Length > 0)
            {
                // une vraie partie en autoplay, sur un monde différent à chaque passage
                var level = catalog.levels[demoIndex % catalog.levels.Length];
                demoIndex++;
                if (rig != null) rig.AutoScroll = 0f;
                if (backdrop != null) backdrop.SetGroundVisible(false);
                menuLooping = false;
                run.BeginRun(level, demo: true);
                Conductor.Instance?.SetVolume(attractVolume);
                return;
            }

            run?.StopRun(hideWorld: true, stopMusic: false);
            if (p == AttractPhase.Logo && highscoreBoard != null) highscoreBoard.ClearHighlight();
            ShowMenuWorld(scroll: true);
            PlayMenuMusic();
        }

        /// <summary>Décor des menus : le monde choisi (ou la Sylve) qui défile, en vue large comme en partie.</summary>
        void ShowMenuWorld(bool scroll)
        {
            var rig = CameraRig.Instance;
            if (PixelScreen.Instance != null) PixelScreen.Instance.SetDivider(1);
            if (rig != null)
            {
                rig.Target = null;
                rig.AutoScroll = scroll ? attractScroll : 0f;
                rig.ResetShot();
            }
            var level = GameSession.IsQuickRetry && GameSession.Level != null ? GameSession.Level : FirstLevel();
            ShowWorld(level);
        }

        void ShowWorld(LevelDef level)
        {
            if (backdrop == null) return;
            var biome = level != null && level.biome != null ? level.biome : theme != null && theme.biomes.Length > 0 ? theme.biomes[0] : null;
            if (biome != null) backdrop.SetBiome(biome);
            backdrop.ResetMood();
            backdrop.SetGroundVisible(true);
        }

        LevelDef FirstLevel() => catalog != null && catalog.levels != null && catalog.levels.Length > 0 ? catalog.levels[0] : null;

        void ShowBoard(bool on)
        {
            showingBoard = on;
            if (highscoreBoard != null) SetActive(highscoreBoard.gameObject, on);
        }

        void FinishNameEntry()
        {
            if (nameEntry != null)
            {
                HighscoreManager.PlayerName = nameEntry.PlayerName;
                StartCoroutine(HighscoreManager.SetHighscore(nameEntry.PlayerName, nameEntry.Score));
                if (highscoreBoard != null) highscoreBoard.Highlight(nameEntry.PlayerName, nameEntry.Score);
            }
            Enter(GameState.Attract);
        }

        void PlayMenuMusic()
        {
            var conductor = Conductor.Instance;
            if (conductor == null) return;
            var song = attractSong != null ? attractSong : levelSong;
            if (song == null) return;
            conductor.SetVolume(attractVolume);
            if (conductor.IsPlaying && conductor.Song == song && menuLooping) return;
            // sortie de démo ou de partie : le morceau continue là où il en est, mais en boucle
            double from = conductor.IsPlaying && conductor.Song == song ? Math.Max(0.0, conductor.SongBeat) : 0.0;
            conductor.Play(song, from, loop: true);
            menuLooping = true;
        }

        /// <summary>Fin de partie (mort, ou niveau terminé).</summary>
        public void EndRun()
        {
            if (State == GameState.Playing) Enter(GameState.GameOver);
        }

        static bool IsTopTen(int score) =>
            score > 0 && HighscoreManager.HasFetchedHighscores && HighscoreManager.IsHighscore(score);

        static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }
    }
}
