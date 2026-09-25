using RythmeRunner.Rhythm;
using UnityEngine;

namespace RythmeRunner.FX
{
    /// <summary>Anime un sprite et son enfant à partir de la position musicale courante.</summary>
    public class BeatFlipbook : MonoBehaviour
    {
        SpriteRenderer target;
        Sprite[] frames;
        float beatsPerFrame = 0.5f;
        [SerializeField] float phase;
        float hopHeight;
        Transform dancer;
        Vector3 dancerPosition;
        Vector3 dancerScale;
        int frame = -1;
        bool configured;

        public void Configure(SpriteRenderer target, Sprite[] frames, float beatsPerFrame, float hopHeight, Transform dancer)
        {
            this.target = target;
            this.frames = frames;
            this.beatsPerFrame = Mathf.Max(0.0001f, beatsPerFrame);
            this.hopHeight = hopHeight;
            this.dancer = dancer;
            if (dancer != null)
            {
                dancerPosition = dancer.localPosition;
                dancerScale = dancer.localScale;
            }
            frame = -1;
            configured = target != null && frames != null && frames.Length > 0;
        }

        void Update()
        {
            if (!configured || Conductor.Instance == null) return;
            if (!target.isVisible) return;

            double beat = Conductor.Instance.SongBeat;
            float phasedBeat = (float)beat + phase;
            int nextFrame = Mathf.FloorToInt(phasedBeat / beatsPerFrame) % frames.Length;
            if (nextFrame < 0) nextFrame += frames.Length;
            if (nextFrame != frame)
            {
                frame = nextFrame;
                target.sprite = frames[frame];
            }

            if (dancer == null) return;
            float fraction = Mathf.Repeat(phasedBeat, 1f);
            float hop = Mathf.Sin(fraction * Mathf.PI) * hopHeight;
            float squash = Mathf.Sin(fraction * Mathf.PI) * 0.12f;
            dancer.localPosition = dancerPosition + Vector3.up * hop;
            dancer.localScale = new Vector3(dancerScale.x * (1f + squash), dancerScale.y * (1f - squash), dancerScale.z);
        }
    }
}
