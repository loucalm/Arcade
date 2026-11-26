using UnityEngine;

namespace Kenta.Player
{
    /// <summary>Un porteur du bandeau : Kenta, Aiko ou Ryo.</summary>
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
    }
}
