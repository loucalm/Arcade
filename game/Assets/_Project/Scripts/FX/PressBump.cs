using UnityEngine;

namespace Kenta.FX
{
    /// <summary>
    /// Enfoncement d'un objet quand on rebondit dessus (tambour) : il descend de quelques pixels et remonte,
    /// toujours sur la grille de pixels (pas d'échelle fractionnaire).
    /// </summary>
    public class PressBump : MonoBehaviour
    {
        [SerializeField] float depthPx = 4f;
        [SerializeField] float duration = 0.2f;

        Vector3 basePos;
        float t = 1f;
        bool init;

        public void Press()
        {
            if (!init) { basePos = transform.localPosition; init = true; }
            t = 0f;
        }

        void Update()
        {
            if (!init || t >= 1f) return;
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            float px = Mathf.Round(depthPx * (1f - t) * (1f - t));
            transform.localPosition = basePos + new Vector3(0f, -px / PixelScreen.Ppu, 0f);
        }
    }
}
