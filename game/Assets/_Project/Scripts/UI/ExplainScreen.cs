using Kenta.Core;
using Kenta.Level;
using Kenta.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Kenta.UI
{
    /// <summary>Consignes (docs/da-kenta.md § 8.3) : le héros mime chaque geste en boucle, une consigne mise en avant à la fois.</summary>
    public class ExplainScreen : MonoBehaviour
    {
        [SerializeField] LevelTheme theme;
        [SerializeField] Image hero;
        [SerializeField] PixelText[] lines;       // 6 lignes de consignes
        [SerializeField] float stepSeconds = 1.1f;

        float t;

        void OnEnable() => t = 0f;

        void Update()
        {
            t += Time.unscaledDeltaTime;
            int step = (int)(t / stepSeconds) % lines.Length;
            var profile = GameSession.Hero != null ? GameSession.Hero : theme.heroes[0];
            var set = profile.sprites;
            Sprite[] poses = { set.jump, set.hit, set.slide, set.run, set.run, set.run };
            hero.sprite = poses[step];
            ((RectTransform)hero.transform).sizeDelta = hero.sprite.rect.size;     // taille en pixels (SetNativeSize tient compte du PPU monde)
            for (int i = 0; i < lines.Length; i++)
                lines[i].Color = i == step ? new Color(1f, 0.82f, 0.23f) : new Color(0.82f, 0.8f, 0.95f);
        }
    }
}
