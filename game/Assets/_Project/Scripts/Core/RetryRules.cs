using UnityEngine;

namespace Kenta.Core
{
    /// <summary>Difficulté d'un héros : ce qui se passe quand on meurt (réglée dans HeroProfile).</summary>
    public enum RetryMode
    {
        /// <summary>Facile : on repart au dernier point de reprise de la chart (ADR-033). 10 vies par défaut.</summary>
        Checkpoint,
        /// <summary>Moyen : le niveau recommence au début, le meilleur essai compte. 3 essais par défaut.</summary>
        Restart,
        /// <summary>Dur : une seule vie, la première mort finit la partie.</summary>
        OneLife,
    }

    /// <summary>Suite d'une mort : reprendre au point, recommencer au début, ou finir la partie.</summary>
    public enum RetryAction { Resume, Restart, GameOver }

    /// <summary>Décision prise à la mort : quoi faire, et à quel beat repartir (Resume, Restart).</summary>
    public readonly struct RetryDecision
    {
        public readonly RetryAction action;
        public readonly double beat;

        public RetryDecision(RetryAction action, double beat)
        {
            this.action = action;
            this.beat = beat;
        }

        public static RetryDecision End => new(RetryAction.GameOver, 0);
    }

    /// <summary>
    /// Règles du die and retry selon le héros (fonctions pures, testées dans Editor/Tests/RetryRulesTests) : vies au
    /// départ, suite d'une mort, meilleur essai, et les mots affichés à l'écran du héros (police pixel : majuscules).
    /// </summary>
    public static class RetryRules
    {
        /// <summary>Vies par défaut en reprise au point (ADR-033).</summary>
        public const int CheckpointLives = 10;
        /// <summary>Essais par défaut en reprise au début.</summary>
        public const int RestartTries = 3;

        /// <summary>Vies (ou essais) d'un mode quand le héros n'en fixe pas.</summary>
        public static int DefaultLives(RetryMode mode) => mode switch
        {
            RetryMode.Checkpoint => CheckpointLives,
            RetryMode.Restart => RestartTries,
            _ => 1,
        };

        /// <summary>Vies au départ : celles du héros (0 ou moins = valeur du mode) ; toujours 1 en « une seule vie ».</summary>
        public static int StartLives(RetryMode mode, int heroLives)
        {
            if (mode == RetryMode.OneLife) return 1;
            return heroLives > 0 ? heroLives : DefaultLives(mode);
        }

        /// <summary>Dernier point de reprise de la chart au beat de la mort ou avant (0 s'il n'y en a pas).</summary>
        public static double CheckpointBefore(float[] checkpoints, double beat)
        {
            double best = 0;
            if (checkpoints != null)
                foreach (var c in checkpoints)
                    if (c <= beat && c > best) best = c;
            return best;
        }

        /// <summary>
        /// Suite d'une mort. livesLeft = vies restantes, la vie perdue déjà retirée. startBeat = beat de départ de la
        /// partie (0, sauf test d'un passage dans l'éditeur). Plus de vie, ou « une seule vie » : fin de partie.
        /// </summary>
        public static RetryDecision Decide(RetryMode mode, int livesLeft, double deathBeat, float[] checkpoints, double startBeat)
        {
            if (livesLeft <= 0 || mode == RetryMode.OneLife) return RetryDecision.End;
            if (mode == RetryMode.Restart) return new RetryDecision(RetryAction.Restart, startBeat);
            return new RetryDecision(RetryAction.Resume, CheckpointBefore(checkpoints, deathBeat));
        }

        /// <summary>Reprise au début : faux si l'essai en cours fait au moins aussi bien que le meilleur des précédents.</summary>
        public static bool KeepEarlier(bool hasEarlier, int earlierScore, int currentScore) =>
            hasEarlier && earlierScore > currentScore;

        /// <summary>
        /// Bonus de score de la difficulté (choix de Lucas, 2026-11-26) : chaque point gagné est multiplié, pour qu'un bon
        /// score en DUR passe devant un score FACILE dans un même classement au score. ×1 facile, ×1,5 moyen, ×2 dur.
        /// </summary>
        public static float ScoreFactor(RetryMode mode) => mode switch
        {
            RetryMode.Restart => 1.5f,
            RetryMode.OneLife => 2f,
            _ => 1f,
        };

        /// <summary>Points après le bonus de difficulté (arrondis au plus proche).</summary>
        public static int WithBonus(int points, float factor) =>
            (int)System.Math.Round(points * (double)factor, System.MidpointRounding.AwayFromZero);

        /// <summary>« ×1,5 » : le bonus tel qu'il s'affiche (rien en facile).</summary>
        public static string FactorLabel(float factor) =>
            factor <= 1f ? "" : factor == Mathf.Floor(factor) ? $"×{(int)factor}" : $"×{factor:0.#}".Replace('.', ',');

        /// <summary>
        /// Couleur de la difficulté, la même partout (cadre du héros, classement) : vert facile, ambre moyen (distinct de
        /// l'or des médailles et du score mis en avant), rouge franc dur (« on voit que le joueur est chaud »).
        /// </summary>
        public static Color DifficultyColor(RetryMode mode) => mode switch
        {
            RetryMode.Checkpoint => new Color(0.5f, 0.9f, 0.45f),
            RetryMode.Restart => new Color(1f, 0.62f, 0.18f),
            _ => new Color(1f, 0.2f, 0.22f),
        };

        /// <summary>Mot de la difficulté, écrit dans le cadre du héros.</summary>
        public static string DifficultyLabel(RetryMode mode) => mode switch
        {
            RetryMode.Checkpoint => "FACILE",
            RetryMode.Restart => "MOYEN",
            _ => "DUR",
        };

        /// <summary>La règle en une ligne, pour le héros sélectionné (lives = vies au départ, voir StartLives).</summary>
        public static string RuleLine(RetryMode mode, int lives)
        {
            switch (mode)
            {
                case RetryMode.Checkpoint:
                    return (lives == 1 ? "1 VIE" : $"{lives} VIES") + " · ON REPART AU DERNIER POINT";
                case RetryMode.Restart:
                    return (lives == 1 ? "1 ESSAI" : $"{lives} ESSAIS") + " · ON REPART DU DÉBUT · LE MEILLEUR COMPTE";
                default:
                    return "UNE SEULE VIE · UN SEUL ESSAI";
            }
        }
    }
}
