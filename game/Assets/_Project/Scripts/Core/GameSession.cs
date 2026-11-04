using Kenta.Level;
using Kenta.Player;

namespace Kenta.Core
{
    /// <summary>Choix du joueur pour la partie en cours (héros, niveau). Réinitialisé quand on retourne à l'accueil.</summary>
    public static class GameSession
    {
        public static int HeroIndex;
        public static HeroProfile Hero;
        public static LevelDef Level;
    }
}
