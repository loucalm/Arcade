using Kenta.Core;
using UnityEngine;

namespace Kenta.Player
{
    /// <summary>Un porteur du bandeau : Kenta, Aiko ou Ryo. Chacun a sa difficulté (ce qui se passe à la mort).</summary>
    [CreateAssetMenu(fileName = "Hero_", menuName = "Kenta/Hero Profile")]
    public class HeroProfile : ScriptableObject
    {
        public string displayName = "KENTA";
        public string role = "LE BATTEUR";
        public HeroSpriteSet sprites;
        [Tooltip("Couleur du bandeau (traînée de combo) et du cadre de sélection.")]
        public Color accent = Color.white;
        [Tooltip("Son de frappe propre au héros (Kenta : caisse claire, Aiko : flûte, Ryo : basse). Vide : son commun.")]
        public AudioClip hitSound;

        [Header("Difficulté (die and retry)")]
        [Tooltip("À la mort : Checkpoint = on repart au dernier point de reprise (facile) ; Restart = le niveau recommence " +
                 "au début, le meilleur essai compte (moyen) ; One Life = la première mort finit la partie (dur).")]
        public RetryMode retryMode = RetryMode.Checkpoint;
        [Tooltip("Vies (Checkpoint) ou essais (Restart) au départ. 0 = valeur du mode : 10 vies, 3 essais. One Life : toujours 1.")]
        [Min(0)] public int lives;

        /// <summary>Vies (ou essais) au départ d'une partie avec ce héros.</summary>
        public int StartLives => RetryRules.StartLives(retryMode, lives);
    }
}
