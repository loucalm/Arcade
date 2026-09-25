using RythmeRunner.Core;
using RythmeRunner.Level;

namespace RythmeRunner.Player
{
    public struct PlayerCommands
    {
        public bool jumpDown;
        public bool hitDown;
        public bool slideHeld;
    }

    /// <summary>Source des commandes du joueur : manette/clavier ou autoplay.</summary>
    public interface IPlayerInput
    {
        PlayerCommands Sample(double beat);
        /// <summary>Appelé au (re)spawn sur un beat donné.</summary>
        void ResetAt(double beat);
    }

    public class ArcadePlayerInput : IPlayerInput
    {
        readonly int player;
        public ArcadePlayerInput(int player = 1) => this.player = player;

        public PlayerCommands Sample(double beat) => new()
        {
            jumpDown = ArcadeInput.JumpDown(player),
            hitDown = ArcadeInput.HitDown(player),
            slideHeld = ArcadeInput.SlideHeld(player),
        };

        public void ResetAt(double beat) { }
    }

    /// <summary>
    /// Joue la chart parfaitement : saute/frappe pile sur le beat de chaque événement, glisse sur les barres.
    /// Sert à valider une chart (aucun dégât attendu) et à la démo de l'écran Attract.
    /// </summary>
    public class AutoPlayerInput : IPlayerInput
    {
        readonly LevelBuilder level;
        int jumpIndex, hitIndex;

        public AutoPlayerInput(LevelBuilder level) => this.level = level;

        public PlayerCommands Sample(double beat)
        {
            var c = new PlayerCommands();
            while (jumpIndex < level.AutoJumpBeats.Count && level.AutoJumpBeats[jumpIndex] <= beat)
            {
                c.jumpDown = true;
                jumpIndex++;
            }
            while (hitIndex < level.AutoHitBeats.Count && level.AutoHitBeats[hitIndex] <= beat)
            {
                c.hitDown = true;
                hitIndex++;
            }
            foreach (var span in level.AutoSlideSpans)
                if (beat >= span.x && beat <= span.y) { c.slideHeld = true; break; }
            return c;
        }

        public void ResetAt(double beat)
        {
            jumpIndex = 0;
            while (jumpIndex < level.AutoJumpBeats.Count && level.AutoJumpBeats[jumpIndex] < beat) jumpIndex++;
            hitIndex = 0;
            while (hitIndex < level.AutoHitBeats.Count && level.AutoHitBeats[hitIndex] < beat) hitIndex++;
        }
    }
}
