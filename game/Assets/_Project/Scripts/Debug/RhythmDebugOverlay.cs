using RythmeRunner.Core;
using RythmeRunner.Rhythm;
using UnityEngine;

// Namespace « DebugTools » et pas « Debug » : sinon `Debug.Log` ne compile plus dans les autres namespaces RythmeRunner.*
namespace RythmeRunner.DebugTools
{
    /// <summary>
    /// Overlay de synchro : état du flow, temps/beat du Conductor, dérive audio, FPS, et un carré
    /// qui flashe sur chaque beat (à comparer à l'oreille avec le kick du morceau de test).
    /// Visible par défaut en éditeur et en Development Build. Touche F9 pour basculer.
    /// </summary>
    public class RhythmDebugOverlay : MonoBehaviour
    {
        [SerializeField] KeyCode toggleKey = KeyCode.F9;
        [Tooltip("Avec l'overlay affiché : l'IA joue à la place du joueur (valider une chart).")]
        [SerializeField] KeyCode autoplayKey = KeyCode.F10;

        /// <summary>Overlay affiché (la grille de beats du niveau suit cet état).</summary>
        public static bool Visible { get; private set; }
        float flash;
        float fpsTimer;
        int frames;
        string fpsText = "";
        string infoText = "";
        GUIStyle style;
        Texture2D white;
        Conductor subscribed;

        void Awake()
        {
            Visible = Application.isEditor || Debug.isDebugBuild;
            white = Texture2D.whiteTexture;
        }

        void OnDisable()
        {
            if (subscribed != null) subscribed.OnBeat -= OnBeat;
            subscribed = null;
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey)) Visible = !Visible;
            if (Visible && Input.GetKeyDown(autoplayKey) && RunManager.Instance != null)
                RunManager.Instance.SetAutoplay(!RunManager.Instance.Autoplay);
            if (subscribed == null && Conductor.Instance != null)
            {
                subscribed = Conductor.Instance;
                subscribed.OnBeat += OnBeat;
            }

            flash = Mathf.Max(0f, flash - Time.unscaledDeltaTime * 6f);

            frames++;
            fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.25f)
            {
                fpsText = $"{frames / fpsTimer:0} fps";
                frames = 0;
                fpsTimer = 0f;
                RefreshInfo();
            }
        }

        void OnBeat(int beat) => flash = beat % subscribed.BeatsPerBar == 0 ? 1f : 0.6f;

        void RefreshInfo()
        {
            var c = Conductor.Instance;
            string state = GameFlow.Instance != null ? GameFlow.Instance.State.ToString() : "-";
            var run = RunManager.Instance;
            if (run != null && run.Chart != null)
                state += $" [{run.SectionName} cœurs {run.Hearts} x{run.Multiplier} chutes {run.Deaths}{(run.Autoplay || run.IsDemo ? " AUTO" : "")}]";
            infoText = c == null
                ? $"{state} | pas de Conductor | {fpsText}"
                : $"{state} | {(c.IsPlaying ? "▶" : "■")} {c.Bpm:0} BPM | t {c.SongTime:0.000}s | beat {c.SongBeat:0.00} | dérive {c.LastDrift * 1000:0.0} ms | {fpsText}";
        }

        void OnGUI()
        {
            if (!Visible) return;
            style ??= new GUIStyle(GUI.skin.label) { fontSize = 22, normal = { textColor = Color.white } };

            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(10, 10, 1500, 44), white);
            GUI.color = new Color(1f, 0.2f, 0.8f, flash);
            GUI.DrawTexture(new Rect(18, 18, 28, 28), white);
            GUI.color = Color.white;
            GUI.Label(new Rect(56, 16, 1450, 36), infoText, style);
        }
    }
}
