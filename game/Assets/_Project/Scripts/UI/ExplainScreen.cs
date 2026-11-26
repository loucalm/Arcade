using Kenta.Core;
using Kenta.Level;
using Kenta.Rhythm;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>
    /// Consignes (docs/da-kenta.md § 8.3), lisibles en quelques secondes : deux gestes avec les boutons dessinés comme
    /// ceux de la borne. Le héros mime le geste mis en avant, sur le temps de la musique : 2 temps par geste, la pose du
    /// geste sur le premier, la course sur le second. Sans musique, une horloge à 150 BPM prend le relais.
    /// Pied de page : la difficulté dépend du héros, choisi à l'écran suivant (HeroSelectScreen), d'où un texte commun.
    /// </summary>
    public class ExplainScreen : MonoBehaviour
    {
        /// <summary>Pied de page, vrai pour tous les héros (aussi écrit par KentaBuilder dans le prefab).</summary>
        public const string FooterText = "TON HÉROS FIXE LA DIFFICULTÉ";

        [SerializeField] LevelTheme theme;
        [SerializeField] Image hero;
        [Tooltip("Une ligne (texte du geste) par geste : saute, frappe.")]
        [SerializeField] PixelText[] lines;
        [Tooltip("Les icônes de bouton, dans le même ordre : elles s'allument avec leur geste.")]
        [SerializeField] Image[] icons;
        [Tooltip("Pied de page (FooterText). Vide : l'enfant « Footer » (prefab construit avant ce champ).")]
        [SerializeField] PixelText footer;
        [SerializeField] float beatsPerGesture = 2f;

        static readonly Color Gold = new(1f, 0.82f, 0.23f);
        static readonly Color Idle = new(0.82f, 0.8f, 0.95f);
        static readonly Color IconIdle = new(0.6f, 0.58f, 0.7f);
        float t;

        void OnEnable()
        {
            t = 0f;
            // l'ancien prefab disait « 10 VIES : ON REPART AU DERNIER POINT », faux pour les héros 2 et 3
            if (footer == null)
            {
                var child = transform.Find("Footer");
                if (child != null) footer = child.GetComponent<PixelText>();
            }
            if (footer != null) footer.Text = FooterText;
        }

        double Beat()
        {
            var c = Conductor.Instance;
            if (c != null && c.IsPlaying) return c.SongBeat;
            return t / 0.4;                                   // 150 BPM
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            double beat = Beat();
            int step = (int)(System.Math.Floor(beat / beatsPerGesture) % lines.Length);
            if (step < 0) step += lines.Length;
            bool posing = beat - System.Math.Floor(beat / beatsPerGesture) * beatsPerGesture < 1.0;

            var profile = GameSession.Hero != null ? GameSession.Hero : theme.heroes[0];
            var set = profile.sprites;
            Sprite pose = step == 0 ? set.jump : set.hit;
            Sprite sprite = posing ? pose
                : set.runCycle != null && set.runCycle.Length > 0 ? set.runCycle[(int)(t * 10f) % set.runCycle.Length] : set.run;
            if (hero.sprite != sprite)
            {
                hero.sprite = sprite;
                ((RectTransform)hero.transform).sizeDelta = sprite.rect.size;     // taille en pixels (SetNativeSize tient compte du PPU monde)
            }
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i].Color = i == step ? Gold : Idle;
                if (icons != null && i < icons.Length && icons[i] != null) icons[i].color = i == step ? Color.white : IconIdle;
            }
        }
    }
}
