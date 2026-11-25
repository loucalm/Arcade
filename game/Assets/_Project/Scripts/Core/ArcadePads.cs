using System.Runtime.InteropServices;
using UnityEngine;

namespace Kenta.Core
{
    /// <summary>
    /// Manettes de la borne lues dans l'API Gamepad du navigateur (Plugins/WebGL/KentaGamepad.jslib), en plus de l'Input
    /// Manager. Pourquoi : en Web, Unity 6.3 ne voit que la manette à l'index 0 de navigator.getGamepads() ; sur la
    /// borne, la manette du joueur 1 peut être ailleurs, et alors plus rien ne répond (testé avec une fausse manette).
    /// Joueur 1 = première manette branchée, joueur 2 = deuxième, quel que soit leur index. Lu une fois par image.
    /// Hors build Web (éditeur) : aucune manette ici, l'Input Manager suffit.
    /// </summary>
    public static class ArcadePads
    {
        public const int Buttons = 9;              // 0..6 = actions, 8 = bouton blanc
        const int Slots = 2;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int KentaPad_Poll();
        [DllImport("__Internal")] static extern int KentaPad_Button(int slot, int button);
        [DllImport("__Internal")] static extern float KentaPad_Axis(int slot, int axis);
        [DllImport("__Internal")] static extern int KentaPad_Index(int slot);
        [DllImport("__Internal")] static extern string KentaPad_Id(int slot);
#else
        static int KentaPad_Poll() => 0;
        static int KentaPad_Button(int slot, int button) => 0;
        static float KentaPad_Axis(int slot, int axis) => 0f;
        static int KentaPad_Index(int slot) => -1;
        static string KentaPad_Id(int slot) => "";
#endif

        static int frame = -1;
        static int count;
        static readonly bool[,] now = new bool[Slots, Buttons], before = new bool[Slots, Buttons];
        static readonly Vector2[] sticks = new Vector2[Slots];

        /// <summary>Nombre de manettes vues par le navigateur (0 en éditeur).</summary>
        public static int Count { get { Refresh(); return count; } }

        static void Refresh()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            count = KentaPad_Poll();
            for (int s = 0; s < Slots; s++)
            {
                for (int b = 0; b < Buttons; b++)
                {
                    before[s, b] = now[s, b];
                    now[s, b] = s < count && KentaPad_Button(s, b) != 0;
                }
                sticks[s] = s < count ? new Vector2(KentaPad_Axis(s, 0), KentaPad_Axis(s, 1)) : Vector2.zero;
            }
        }

        static int Slot(int player) => player == 2 ? 1 : 0;

        public static bool Held(int player, int button) { Refresh(); return now[Slot(player), button]; }
        public static bool Down(int player, int button) { Refresh(); int s = Slot(player); return now[s, button] && !before[s, button]; }

        /// <summary>Joystick brut (haut = +y, comme le menu de la borne), sans dead zone.</summary>
        public static Vector2 Stick(int player) { Refresh(); return sticks[Slot(player)]; }

        /// <summary>Un bouton (0..6) de n'importe quelle manette vient d'être pressé.</summary>
        public static bool AnyActionDown()
        {
            Refresh();
            for (int s = 0; s < Slots; s++)
                for (int b = 0; b <= 6; b++)
                    if (now[s, b] && !before[s, b]) return true;
            return false;
        }

        /// <summary>Bouton blanc (8) tenu sur n'importe quelle manette.</summary>
        public static bool CoinHeld()
        {
            Refresh();
            for (int s = 0; s < Slots; s++)
                if (now[s, 8]) return true;
            return false;
        }

        /// <summary>Description des manettes vues (diagnostic sur la borne).</summary>
        public static string Describe()
        {
            Refresh();
            if (count == 0) return "aucune manette vue par le navigateur";
            var sb = new System.Text.StringBuilder();
            for (int s = 0; s < Mathf.Min(count, Slots); s++)
            {
                sb.Append($"J{s + 1} = index {KentaPad_Index(s)} « {KentaPad_Id(s)} » boutons ");
                for (int b = 0; b < Buttons; b++) sb.Append(now[s, b] ? b.ToString() : "·");
                sb.Append($" axes {sticks[s].x:0.00} {sticks[s].y:0.00}\n");
            }
            return sb.ToString();
        }
    }
}
