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

        [Header("Textes (provisoire)")]
        [SerializeField] TMP_Text hudScoreText;
        [SerializeField] TMP_Text gameOverScoreText;

        public GameState State { get; private set; }
        public int Score { get; private set; }
        public event Action<GameState> OnStateChanged;

        float stateTimer;
        bool showingHighscores;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // Abonnement dans Start (et pas OnEnable) : tous les Awake, dont celui du Conductor, sont passés.
        void Start()
        {
            if (Conductor.Instance != null)
            {
                Conductor.Instance.OnBeat += HandleBeat;
                Conductor.Instance.OnSongEnd += HandleSongEnd;
            }
            StartCoroutine(HighscoreManager.FetchHighscores());
            Enter(GameState.Attract);
        }

        void OnDestroy()
        {
            if (Conductor.Instance != null)
            {
                Conductor.Instance.OnBeat -= HandleBeat;
                Conductor.Instance.OnSongEnd -= HandleSongEnd;
            }
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
                    // DEBUG provisoire tant que le gameplay n'existe pas : Start = fin de partie.
                    if (guardPassed && ArcadeInput.StartDown()) EndRun();
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
            switch (next)
            {
                case GameState.Attract:
                    if (conductor != null && attractSong != null)
                    {
                        conductor.SetVolume(attractVolume);
                        conductor.Play(attractSong, 0, loop: true);
                    }
                    break;

                case GameState.Explain:
                    if (conductor != null) conductor.Stop();
                    break;

                case GameState.Playing:
                    Score = 0;
                    UpdateScoreTexts();
                    if (conductor != null && levelSong != null)
                    {
                        conductor.SetVolume(1f);
                        conductor.Play(levelSong, 0, loop: false);
                    }
                    break;

                case GameState.GameOver:
                    if (conductor != null) conductor.Stop();
                    UpdateScoreTexts();
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

        public void AddScore(int amount)
        {
            Score += amount;
            UpdateScoreTexts();
        }

        void HandleBeat(int beat)
        {
            // Provisoire : 10 points par beat pour valider le branchement Conductor → score.
            if (State == GameState.Playing) AddScore(10);
        }

        void HandleSongEnd()
        {
            // Provisoire : fin du morceau = fin de partie. Plus tard : tour REMIX 8-bit.
            EndRun();
        }

        void ToggleAttractHighscores()
        {
            showingHighscores = !showingHighscores;
            stateTimer = 0f;
            if (showingHighscores) HighscoreManager.ShowHighscores();
            else HighscoreManager.HideHighscores();
        }

        void UpdateScoreTexts()
        {
            if (hudScoreText != null) hudScoreText.text = Score.ToString("N0");
            if (gameOverScoreText != null) gameOverScoreText.text = $"SCORE  {Score:N0}";
        }

        static bool IsTopTen(int score) =>
            score > 0 && HighscoreManager.HasFetchedHighscores && HighscoreManager.IsHighscore(score);

        static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }
    }
}
