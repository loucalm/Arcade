using UnityEngine;

namespace RythmeRunner.Core
{
    /// <summary>
    /// Point d'entrée UNIQUE pour lire la borne (manettes + clavier de dev).
    /// Ne jamais appeler Input.GetAxis / GetButton ailleurs dans le projet.
    /// Mapping : B1 = saut, B2 = frappe, joystick bas = glissade, Start = valider.
    /// </summary>
    public static class ArcadeInput
    {
        /// <summary>Repos des joysticks de la borne ≈ -0.00392 : tout ce qui est sous ce seuil est ignoré.</summary>
        public const float DeadZone = 0.3f;

        /// <summary>Seuil (après dead zone) pour considérer le joystick « poussé » dans une direction.</summary>
        public const float DirectionThreshold = 0.5f;

        // Noms d'axes/boutons pré-calculés pour éviter les allocations de string à chaque frame.
        static readonly string[] Horizontal = { "P1_Horizontal", "P2_Horizontal" };
        static readonly string[] Vertical = { "P1_Vertical", "P2_Vertical" };
        static readonly string[] B1 = { "P1_B1", "P2_B1" };
        static readonly string[] B2 = { "P1_B2", "P2_B2" };
        static readonly string[] Start = { "P1_Start", "P2_Start" };
        static readonly string[] AllButtons =
        {
            "P1_B1", "P1_B2", "P1_B3", "P1_B4", "P1_B5", "P1_B6", "P1_Start",
            "P2_B1", "P2_B2", "P2_B3", "P2_B4", "P2_B5", "P2_B6", "P2_Start",
        };

        static int Index(int player) => player == 2 ? 1 : 0;

        /// <summary>Joystick avec dead zone radiale, remis à l'échelle 0..1 au-delà du seuil. Haut = +y.</summary>
        public static Vector2 Stick(int player = 1)
        {
            int i = Index(player);
            var raw = new Vector2(Input.GetAxisRaw(Horizontal[i]), Input.GetAxisRaw(Vertical[i]));
            float magnitude = raw.magnitude;
            if (magnitude < DeadZone) return Vector2.zero;
            float scaled = Mathf.Clamp01((magnitude - DeadZone) / (1f - DeadZone));
            return raw / magnitude * scaled;
        }

        public static bool JumpDown(int player = 1) => Input.GetButtonDown(B1[Index(player)]);
        public static bool JumpHeld(int player = 1) => Input.GetButton(B1[Index(player)]);
        public static bool JumpUp(int player = 1) => Input.GetButtonUp(B1[Index(player)]);

        public static bool HitDown(int player = 1) => Input.GetButtonDown(B2[Index(player)]);

        /// <summary>Glissade = joystick tenu vers le bas.</summary>
        public static bool SlideHeld(int player = 1) => Stick(player).y < -DirectionThreshold;

        public static bool StartDown(int player = 1) => Input.GetButtonDown(Start[Index(player)]);

        /// <summary>Valider dans les menus : Start ou B1.</summary>
        public static bool ConfirmDown(int player = 1) => StartDown(player) || JumpDown(player);

        /// <summary>N'importe quel bouton de jeu (P1 ou P2) vient d'être pressé. Exclut le bouton blanc (Coin).</summary>
        public static bool AnyButtonDown()
        {
            for (int i = 0; i < AllButtons.Length; i++)
                if (Input.GetButtonDown(AllButtons[i])) return true;
            return false;
        }
    }
}
