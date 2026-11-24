using Kenta.Level;
using Kenta.Player;
using UnityEngine;

namespace Kenta.Core
{
    /// <summary>
    /// Choix du joueur pour la partie en cours (héros, monde). Gardés une minute après la fin d'une partie : si le joueur
    /// rejoue tout de suite, il saute les consignes et retrouve son héros et son monde (audit du 24/11).
    /// </summary>
    public static class GameSession
    {
        public static int HeroIndex;
        public static HeroProfile Hero;
        public static int LevelIndex;
        public static LevelDef Level;

        /// <summary>Instant (temps réel) de la dernière fin de partie ; négatif = aucune.</summary>
        public static float LastRunEnd = -999f;

        /// <summary>Délai pendant lequel une nouvelle partie compte comme « rejouer » (même joueur).</summary>
        public const float QuickRetrySeconds = 60f;

        public static bool IsQuickRetry => Time.unscaledTime - LastRunEnd < QuickRetrySeconds;

        public static void Reset()
        {
            HeroIndex = 0;
            Hero = null;
            LevelIndex = 0;
            Level = null;
        }
    }
}
