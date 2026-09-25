using RythmeRunner.Rhythm;
using UnityEngine;

namespace RythmeRunner.FX
{
    /// <summary>Affiche une alerte déterministe avant le beat d'action d'un obstacle.</summary>
    public class Telegraph : MonoBehaviour
    {
        float actionBeat;
        int beatsPerBar;
        SpriteRenderer[] renderers;
        Color[] originalColors;
        Transform pop;
        Vector3 popScale;
        SpriteRenderer spriteTarget;
        Sprite activeSprite;
        Sprite normalSprite;
        bool configured;
        bool active = true; // vrai pour forcer une première remise à l'état normal

        public void Configure(float actionBeat, int beatsPerBar, SpriteRenderer[] renderers, Transform pop)
        {
            this.actionBeat = actionBeat;
            this.beatsPerBar = Mathf.Max(1, beatsPerBar);
            this.renderers = renderers;
            originalColors = new Color[renderers != null ? renderers.Length : 0];
            for (int i = 0; i < originalColors.Length; i++)
                originalColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
            this.pop = pop;
            popScale = pop != null ? pop.localScale : Vector3.one;
            configured = true;
        }

        public void ConfigureActiveSprite(SpriteRenderer target, Sprite active)
        {
            spriteTarget = target;
            activeSprite = active;
            normalSprite = target != null ? target.sprite : null;
        }

        void Update()
        {
            if (!configured || Conductor.Instance == null) return;
            // Hors écran, rien à montrer : on évite de réécrire des centaines de couleurs par frame.
            if (renderers.Length > 0 && renderers[0] != null && !renderers[0].isVisible) return;

            float beat = (float)Conductor.Instance.SongBeat;
            float start = actionBeat - beatsPerBar;
            if (beat < start || beat >= actionBeat)
            {
                SetNormal();
                return;
            }

            float progress = Mathf.InverseLerp(start, actionBeat, beat);
            float pulse = 1f - Mathf.Clamp01(Mathf.Repeat(beat, 1f));
            float amount = progress * pulse;
            active = true;
            if (spriteTarget != null && activeSprite != null) spriteTarget.sprite = activeSprite;
            for (int i = 0; i < originalColors.Length; i++)
                if (renderers[i] != null) renderers[i].color = Color.Lerp(originalColors[i], Color.white, amount);
            if (pop != null)
                pop.localScale = popScale * (1f + amount * 0.12f);
        }

        void SetNormal()
        {
            if (!active) return;
            active = false;
            for (int i = 0; i < originalColors.Length; i++)
                if (renderers[i] != null) renderers[i].color = originalColors[i];
            if (spriteTarget != null) spriteTarget.sprite = normalSprite;
            if (pop != null) pop.localScale = popScale;
        }
    }
}
