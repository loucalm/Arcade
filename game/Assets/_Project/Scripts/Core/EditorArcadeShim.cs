#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Anatidae;
using UnityEngine;

namespace Kenta.Core
{
    /// <summary>
    /// Éditeur seulement : remplace ce que le toolkit Anatidae ne sait faire que sur la borne, sans le modifier
    /// (le CLAUDE.md l'interdit). Rien de tout ça n'existe dans le build.
    /// - Classement : sans la borne locale (localhost:3000), la récupération échoue, HighscoreManager.Highscores reste
    ///   null et HighscoreUI lève une NullReferenceException. On part d'une liste vide, marquée récupérée : le classement
    ///   s'affiche vide, la saisie du nom s'ouvre en fin de partie (le score n'est envoyé nulle part),
    ///   et l'erreur de connexion du toolkit devient un simple avertissement.
    /// - Retour au menu : MenuManager appelle BackToMenu() (jslib du build Web) après 60 s sans toucher aux commandes ou
    ///   le bouton blanc tenu 1,5 s (et en quittant le Play mode), et lève alors une exception. En éditeur, on le retire
    ///   et on revient à l'écran d'accueil à la place.
    /// </summary>
    public class EditorArcadeShim : MonoBehaviour
    {
        const float AfkTime = 60f;
        const float HeldQuitTime = 1.5f;

        float afk, held;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void StartWithEmptyHighscores()
        {
            if (!(Debug.unityLogger.logHandler is QuietArcadeLog))
                Debug.unityLogger.logHandler = new QuietArcadeLog(Debug.unityLogger.logHandler);
            if (HighscoreManager.Highscores != null) return;
            var setter = typeof(HighscoreManager).GetProperty(nameof(HighscoreManager.Highscores))?.GetSetMethod(true);
            setter?.Invoke(null, new object[] { new List<HighscoreManager.HighscoreEntry>() });
            // classement « récupéré » (vide) : la saisie du nom s'ouvre en fin de partie, comme sur la borne ; si la borne
            // locale répond, FetchHighscores remplace la liste
            typeof(HighscoreManager).GetProperty(nameof(HighscoreManager.HasFetchedHighscores))?.GetSetMethod(true)
                ?.Invoke(null, new object[] { true });
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ReplaceMenuManager()
        {
            var managers = FindObjectsByType<MenuManager>(FindObjectsSortMode.None);
            if (managers.Length == 0) return;
            var quitText = typeof(MenuManager).GetField("quitText", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var m in managers)
            {
                // son texte « Retour au menu » (Arial) restait affiché : c'est MenuManager qui le masquait à chaque image
                if (quitText?.GetValue(m) is Component text) text.gameObject.SetActive(false);
                Destroy(m);                                  // désactivé, il recevait encore OnApplicationQuit → BackToMenu()
            }
            var go = new GameObject(nameof(EditorArcadeShim));
            DontDestroyOnLoad(go);
            go.AddComponent<EditorArcadeShim>();
        }

        void Update()
        {
            held = Input.GetButton("Coin") ? held + Time.unscaledDeltaTime : 0f;
            // l'autoplay de debug (F10) n'appuie sur rien : ce n'est pas un joueur parti, on ne le renvoie pas à l'accueil
            bool autoplay = RunManager.Instance != null && RunManager.Instance.IsRunning && RunManager.Instance.Autoplay;
            afk = ArcadeInput.AnyActivity() || Input.anyKeyDown || autoplay ? 0f : afk + Time.unscaledDeltaTime;
            if (held < HeldQuitTime && afk < AfkTime) return;

            bool afkTimeout = afk >= AfkTime;
            held = afk = 0f;
            var flow = GameFlow.Instance;
            if (flow == null || (afkTimeout && flow.State == GameState.Attract)) return;   // déjà à l'accueil
            Debug.Log("[Éditeur] Retour au menu de la borne (simulé) : retour à l'écran d'accueil.");
            flow.Enter(GameState.Attract);
        }

        /// <summary>Rétrograde en avertissement l'erreur « borne locale injoignable » du toolkit (attendue en éditeur).</summary>
        sealed class QuietArcadeLog : ILogHandler
        {
            readonly ILogHandler inner;
            public QuietArcadeLog(ILogHandler inner) => this.inner = inner;

            public void LogFormat(LogType type, UnityEngine.Object context, string format, params object[] args)
            {
                if (type == LogType.Error && args != null && args.Length == 1 && args[0] is string msg && msg == "Cannot connect to destination host")
                {
                    inner.LogFormat(LogType.Warning, context, "Borne locale absente (localhost:3000) : classement vide en éditeur. Pour les scores, lancer tools/anatidae-arcade.");
                    return;
                }
                inner.LogFormat(type, context, format, args);
            }

            public void LogException(Exception exception, UnityEngine.Object context) => inner.LogException(exception, context);
        }

    }
}
#endif
