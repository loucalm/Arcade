using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>
    /// Plaque du HUD, dans le langage des menus : fond sombre translucide et cadre d'1 px aux coins coupés (4 bords
    /// étirés : la plaque suit sa largeur). Le cadre peut passer à l'or ou au rouge. Opacité = visibilité (fondu
    /// d'apparition, piloté par RunHud) × couverture (la plaque s'efface en douceur quand elle couvre le héros, une pièce
    /// ou un ennemi). Ancrée en haut à gauche de l'écran 320×180. Construite par Editor/KentaBuilder (BuildHud).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class HudPlate : MonoBehaviour
    {
        [SerializeField] Image fill;
        [Tooltip("Haut, bas, gauche, droite.")]
        [SerializeField] Image[] frame;

        CanvasGroup group;
        Color frameColor = new(-1f, 0f, 0f, 0f);
        float visibility = 1f, cover = 1f, coverTarget = 1f, applied = -1f;

        public RectTransform Rect => (RectTransform)transform;

        CanvasGroup Group => group != null ? group : group = GetComponent<CanvasGroup>();

        /// <summary>Largeur en pixels entiers (la hauteur est celle de la grille).</summary>
        public int Width
        {
            get => Mathf.RoundToInt(Rect.sizeDelta.x);
            set { if (Mathf.RoundToInt(Rect.sizeDelta.x) != value) Rect.sizeDelta = new Vector2(value, HudLayout.PlateHeight); }
        }

        /// <summary>Bord gauche, en pixels depuis le bord gauche de l'écran.</summary>
        public int Left
        {
            get => Mathf.RoundToInt(Rect.anchoredPosition.x);
            set { if (Mathf.RoundToInt(Rect.anchoredPosition.x) != value) Rect.anchoredPosition = new Vector2(value, Rect.anchoredPosition.y); }
        }

        public void SetFrameColor(Color c)
        {
            if (c == frameColor || frame == null) return;
            frameColor = c;
            for (int i = 0; i < frame.Length; i++) frame[i].color = c;
        }

        /// <summary>Visibilité (0 → 1), déjà lissée par l'appelant.</summary>
        public float Visibility
        {
            get => visibility;
            set { visibility = Mathf.Clamp01(value); Apply(); }
        }

        /// <summary>Opacité visée selon ce que la plaque couvre (le fondu vers elle se fait dans <see cref="Tick"/>).</summary>
        public float CoverTarget { set => coverTarget = value; }

        public void SnapAlpha(float a)
        {
            cover = coverTarget = a;
            Apply();
        }

        public void Tick(float dt, float speed)
        {
            if (cover == coverTarget) return;
            cover = Mathf.MoveTowards(cover, coverTarget, speed * dt);
            Apply();
        }

        void Apply()
        {
            float a = visibility * cover;
            if (a == applied) return;
            applied = a;
            Group.alpha = a;
        }
    }
}
