using Kenta.Player;

namespace Kenta.Core
{
    /// <summary>Choix du joueur pour la partie en cours (héros). Réinitialisé à Kenta quand on retourne à l'accueil.</summary>
    public static class GameSession
    {
        public static int HeroIndex;
        public static HeroProfile Hero;
    }
}
