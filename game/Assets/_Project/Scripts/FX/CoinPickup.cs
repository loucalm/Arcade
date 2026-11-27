using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Pièce ramassée (2026-11-27, « plus satisfaisant à ramasser ») : au lieu de disparaître d'un coup, elle saute de
    /// quelques pixels en clignotant, puis s'éteint. Positions recalées sur la grille de pixels, aucune échelle.
    /// Au repos, le composant est désactivé (pas d'Update sur des centaines de pièces) ; Play() l'active.
    /// ⚠️ Piège : ne pas remettre l'animation à zéro dans OnEnable. Play() active le composant, ce qui appelle OnEnable
    /// aussitôt : l'animation était annulée dès son départ et les pièces ramassées restaient affichées. La remise en état
    /// se fait à la fin de l'animation (ou si la pièce est cachée en route) : le niveau peut la réactiver intacte.
    /// </summary>
    public class CoinPickup : MonoBehaviour
    {
        const float Duration = 0.16f;              // s
        const float Rise = 6f / PixelScreen.Ppu;   // u : 6 px du monde
        const float BlinkStep = 0.04f;             // s : clignote dans la seconde moitié

        SpriteRenderer[] renderers;
        Vector3 basePosition;
        float t;

        /// <summary>Vrai pendant l'animation de ramassage.</summary>
        public bool Playing { get; private set; }

        void Awake()
        {
            if (!Playing) enabled = false;
        }

        /// <summary>Lance l'animation ; la pièce se désactive toute seule à la fin.</summary>
        public void Play()
        {
            if (Playing) return;
            renderers ??= GetComponentsInChildren<SpriteRenderer>(true);
            basePosition = transform.localPosition;
            t = 0f;
            Playing = true;
            enabled = true;
        }

        /// <summary>Hauteur (u, sur la grille) et visibilité à l'instant `time` de l'animation.</summary>
        public static float RiseAt(float time) => PixelScreen.Snap(Rise * Mathf.Clamp01(time / Duration));
        public static bool VisibleAt(float time) => time < Duration * 0.5f || ((int)(time / BlinkStep) & 1) == 0;

        void Update() => Tick(Time.deltaTime);

        /// <summary>Avance l'animation de `dt` secondes (appelé par Update ; public pour les tests).</summary>
        public void Tick(float dt)
        {
            if (!Playing) { enabled = false; return; }
            t += dt;
            if (t >= Duration)
            {
                Restore();
                gameObject.SetActive(false);
                return;
            }
            transform.localPosition = basePosition + new Vector3(0f, RiseAt(t), 0f);
            bool visible = VisibleAt(t);
            foreach (var r in renderers) r.enabled = visible;
        }

        // cachée en pleine animation (tranche du niveau éteinte, reprise) : on la remet en état
        void OnDisable()
        {
            if (Playing) Restore();
        }

        void Restore()
        {
            Playing = false;
            transform.localPosition = basePosition;
            if (renderers != null) foreach (var r in renderers) r.enabled = true;
            enabled = false;
        }
    }
}
