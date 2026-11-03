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
    /// Attract+HighScore → Explain → Config (choix du héros) → Playing → GameOver → NameEntry si top 10 → Attract.
    /// Un GameObject « écran » par état, dans le canvas pixel 240×135. Scène unique Main (ADR-007).
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [Header("Écrans (un GameObject racine par état)")]
        [SerializeField] GameObject attractScreen;
        [SerializeField] GameObject explainScreen;
        [SerializeField] GameObject configScreen;
        [SerializeField] GameObject playingScreen;
        [SerializeField] GameObject gameOverScreen;
        [SerializeField] HeroSelectScreen heroSelect;

        [Header("Monde et effets")]
        [SerializeField] LevelTheme theme;
        [SerializeField] BiomeBackdrop backdrop;
        [SerializeField] Sourdine sourdine;

        [Header("Musique")]
        [SerializeField] SongData levelSong;
        [Tooltip("Boucle de menu ; si vide, le morceau du niveau tourne en boucle, plus bas.")]
        [SerializeField] SongData attractSong;
        [SerializeField, Range(0f, 1f)] float attractVolume = 0.5f;

        [Header("Durées (s)")]
        [SerializeField] float attractHighscoreSwap = 10f;
        [SerializeField] float explainDuration = 6f;
        [SerializeField] float gameOverDuration = 5.5f;
        [SerializeField] float gameOverHighscoreDuration = 4f;
        [Tooltip("Délai avant d'accepter une validation, pour éviter qu'un appui en rafale saute un écran.")]
        [SerializeField] float inputGuard = 0.5f;
        [SerializeField] float attractScroll = 1.4f;

        public GameState State { get; private set; }
        public int Score => RunManager.Instance != null ? RunManager.Instance.Score : 0;
        public event Action<GameState> OnStateChanged;

        float stateTimer;
        bool showingHighscores;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // Start (et pas Awake) : tous les Awake, dont ceux du Conductor et du RunManager, sont passés.
        void Start()
        {
            StartCoroutine(HighscoreManager.FetchHighscores());
            Enter(GameState.Attract);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            stateTimer += Time.unscaledDeltaTime;
            bool guardPassed = stateTimer >= inputGuard;

            switch (State)
            {
                case GameState.Attract:
                    if (stateTimer >= attractHighscoreSwap) ToggleAttractHighscores();
                    if (ArcadeInput.AnyButtonDown()) Enter(GameState.Explain);
                    break;

                case GameState.Explain:
                    if (stateTimer >= explainDuration || (guardPassed && ArcadeInput.ConfirmDown()))
                        Enter(GameState.Config);
                    break;

                case GameState.Config:
                    if (heroSelect == null || heroSelect.Done) Enter(GameState.Playing);
                    break;

                case GameState.Playing:
                    // La fin de partie vient du RunManager (plus de cœurs ou fin du morceau) → EndRun().
                    break;

                case GameState.GameOver:
                    if (!showingHighscores && stateTimer >= gameOverDuration)
                    {
                        showingHighscores = true;
                        HighscoreManager.ShowHighscores();
                    }
                    if (stateTimer >= gameOverDuration + gameOverHighscoreDuration)
                        Enter(IsTopTen(Score) ? GameState.NameEntry : GameState.Attract);
                    break;

                case GameState.NameEntry:
                    if (!HighscoreManager.IsHighscoreInputScreenShown) Enter(GameState.Attract);
                    break;
            }
        }

        public void Enter(GameState next)
        {
            State = next;
            stateTimer = 0f;
            if (showingHighscores) HighscoreManager.HideHighscores();
            showingHighscores = false;

            SetActive(attractScreen, next == GameState.Attract);
            SetActive(explainScreen, next == GameState.Explain);
            SetActive(configScreen, next == GameState.Config);
            SetActive(playingScreen, next == GameState.Playing);
            SetActive(gameOverScreen, next == GameState.GameOver);

            var conductor = Conductor.Instance;
            var run = RunManager.Instance;
            var rig = CameraRig.Instance;
            switch (next)
            {
                case GameState.Attract:
                    GameSession.HeroIndex = 0;
                    GameSession.Hero = theme != null && theme.heroes.Length > 0 ? theme.heroes[0] : null;
                    run?.StopRun(hideWorld: true, stopMusic: false);
                    if (sourdine != null) sourdine.ResetPosition();
                    if (PixelScreen.Instance != null) PixelScreen.Instance.SetDivider(1);
                    if (backdrop != null && theme != null) { backdrop.SetBiome(theme.biomes[0]); backdrop.SetGroundVisible(true); }
                    if (rig != null) { rig.Target = null; rig.AutoScroll = attractScroll; }
                    PlayMenuMusic();
                    break;

                case GameState.Explain:
                case GameState.Config:
                    run?.StopRun(hideWorld: true, stopMusic: false);
                    break;

                case GameState.Playing:
                    if (rig != null) rig.AutoScroll = 0f;
                    if (backdrop != null) backdrop.SetGroundVisible(false);
                    conductor?.SetVolume(1f);
                    if (sourdine != null) sourdine.ResetPosition();
                    run?.BeginRun(levelSong, demo: false);
                    break;

                case GameState.GameOver:
                    conductor?.Stop();
                    if (sourdine != null) sourdine.Cover();
                    break;

                case GameState.NameEntry:
                    HighscoreManager.ShowHighscoreInput(Score);
                    break;
            }

            OnStateChanged?.Invoke(next);
        }

        void PlayMenuMusic()
        {
            var conductor = Conductor.Instance;
            if (conductor == null) return;
            var song = attractSong != null ? attractSong : levelSong;
            if (song == null) return;
            conductor.SetVolume(attractVolume);
            if (!(conductor.IsPlaying && conductor.Song == song)) conductor.Play(song, 0, loop: true);
        }

        /// <summary>Fin de partie (plus de cœurs, ou niveau terminé).</summary>
        public void EndRun()
        {
            if (State == GameState.Playing) Enter(GameState.GameOver);
        }

        void ToggleAttractHighscores()
        {
            showingHighscores = !showingHighscores;
            stateTimer = 0f;
            if (showingHighscores) HighscoreManager.ShowHighscores();
            else HighscoreManager.HideHighscores();
        }

        static bool IsTopTen(int score) =>
            score > 0 && HighscoreManager.HasFetchedHighscores && HighscoreManager.IsHighscore(score);

        static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }
    }
}
