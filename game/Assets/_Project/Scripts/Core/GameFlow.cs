using System;
using Anatidae;
using RythmeRunner.Rhythm;
using TMPro;
using UnityEngine;

namespace RythmeRunner.Core
{
    public enum GameState { Attract, Explain, Config, Playing, GameOver, NameEntry }

    /// <summary>
    /// Machine à états du flow d'écrans imposé (docs/game-design.md) :
    /// Attract+HighScore → Explain → (Config) → Playing → GameOver → NameEntry si top 10 → Attract.
    /// Un GameObject « écran » par état, activé/désactivé ici. Scène unique Main (ADR-007).
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

        [Header("Musique")]
        [Tooltip("Boucle de menu, utilisée seulement si le niveau n'a pas de chart (sinon : démo en autoplay).")]
        [SerializeField] SongData attractSong;
        [SerializeField] SongData levelSong;
        [SerializeField, Range(0f, 1f)] float attractVolume = 0.5f;

        [Header("Durées (s)")]
        [SerializeField] float attractHighscoreSwap = 8f;
        [SerializeField] float explainDuration = 6f;
        [SerializeField] float gameOverDuration = 3f;
        [SerializeField] float gameOverHighscoreDuration = 4f;
        [Tooltip("Délai avant d'accepter une validation, pour éviter qu'un appui en rafale saute un écran.")]
        [SerializeField] float inputGuard = 0.5f;

        [Header("Écran Game Over")]
        [SerializeField] TMP_Text gameOverTitleText;
        [SerializeField] TMP_Text gameOverScoreText;
        [SerializeField] TMP_Text gameOverDetailText;

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
                    if (stateTimer >= attractHighscoreSwap)
                        ToggleAttractHighscores();
                    if (ArcadeInput.AnyButtonDown()) Enter(GameState.Explain);
                    break;

                case GameState.Explain:
                    if (stateTimer >= explainDuration || (guardPassed && ArcadeInput.ConfirmDown()))
                        Enter(configScreen != null ? GameState.Config : GameState.Playing);
                    break;

                case GameState.Config:
                    // TODO : choix 1P / 2P (hot-seat). Pour l'instant : validation → 1 joueur.
                    if (guardPassed && ArcadeInput.ConfirmDown()) Enter(GameState.Playing);
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
            switch (next)
            {
                case GameState.Attract:
                    if (conductor != null) conductor.SetVolume(attractVolume);
                    if (run != null && levelSong != null && levelSong.chart != null)
                        run.BeginRun(levelSong, demo: true); // démo en autoplay derrière le titre
                    else if (conductor != null && attractSong != null)
                        conductor.Play(attractSong, 0, loop: true);
                    break;

                case GameState.Explain:
                    run?.StopRun(hideWorld: true);
                    conductor?.Stop();
                    break;

                case GameState.Config:
                    break;

                case GameState.Playing:
                    conductor?.SetVolume(1f);
                    run?.BeginRun(levelSong, demo: false);
                    break;

                case GameState.GameOver:
                    // Le monde reste affiché, figé, derrière le récapitulatif.
                    conductor?.Stop();
                    UpdateGameOverTexts();
                    break;

                case GameState.NameEntry:
                    HighscoreManager.ShowHighscoreInput(Score);
                    break;
            }

            OnStateChanged?.Invoke(next);
        }

        /// <summary>Fin de partie (mort ou fin des tours de remix).</summary>
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

        void UpdateGameOverTexts()
        {
            var run = RunManager.Instance;
            if (run == null) return;
            if (gameOverTitleText != null) gameOverTitleText.text = run.Completed ? "NIVEAU TERMINÉ !" : "GAME OVER";
            if (gameOverScoreText != null) gameOverScoreText.text = $"SCORE  {run.Score:N0}";
            if (gameOverDetailText != null)
                gameOverDetailText.text = $"LUMS {run.Lums}/{run.TotalLums}     MÉDAILLE {run.Medal}     CHUTES {run.Deaths}";
        }

        static bool IsTopTen(int score) =>
            score > 0 && HighscoreManager.HasFetchedHighscores && HighscoreManager.IsHighscore(score);

        static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }
    }
}
