using System;
using UnityEngine;

namespace RythmeRunner.Rhythm
{
    /// <summary>
    /// Sons d'action quantifiés à la double-croche (invariant n°4) : un son déclenché un peu avant
    /// la double-croche suivante est retardé pour tomber pile dessus. Le joueur « joue la musique ».
    /// Pool de sources pour les sons qui se chevauchent.
    /// </summary>
    public class ActionSfx : MonoBehaviour
    {
        public static ActionSfx Instance { get; private set; }

        [SerializeField] AudioClip jump, hit, kill, slide, lum, land, hurt, checkpoint;
        [SerializeField, Min(1)] int voices = 12;
        [Tooltip("Grille de quantification en beats (0,25 = double-croche).")]
        [SerializeField] float quantizeBeats = 0.25f;
        [SerializeField, Range(0f, 1f)] float volume = 0.8f;

        // Gamme pentatonique (demi-tons) : les lignes de lums jouent une petite mélodie.
        static readonly int[] Pentatonic = { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21, 24 };

        AudioSource[] pool;
        int next;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            pool = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                pool[i] = gameObject.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Jump() => Play(jump, 1f, 0.7f);
        public void Hit() => Play(hit, 1f, 0.6f);
        public void Kill() => Play(kill, 1f, 0.9f);
        public void Slide() => Play(slide, 1f, 0.6f);
        public void Land() => Play(land, 1f, 0.5f, quantized: false);
        public void Hurt() => Play(hurt, 1f, 0.9f, quantized: false);
        public void Checkpoint() => Play(checkpoint, 1f, 0.8f);

        /// <summary>Lum n°index d'une ligne : la note monte dans la gamme.</summary>
        public void Lum(int index) =>
            Play(lum, Mathf.Pow(2f, Pentatonic[Mathf.Clamp(index, 0, Pentatonic.Length - 1)] / 12f), 0.55f);

        public void Play(AudioClip clip, float pitch = 1f, float gain = 1f, bool quantized = true)
        {
            if (clip == null || pool == null) return;
            var source = pool[next];
            next = (next + 1) % pool.Length;
            source.Stop();
            source.clip = clip;
            source.pitch = pitch;
            source.volume = gain * volume;

            double delay = 0;
            var conductor = Conductor.Instance;
            if (quantized && conductor != null && conductor.IsPlaying)
            {
                double beat = conductor.SongBeat;
                double target = Math.Round(beat / quantizeBeats) * quantizeBeats;
                if (target > beat) delay = (target - beat) * conductor.SecondsPerBeat;
            }

            if (delay > 0.004) source.PlayDelayed((float)delay);
            else source.Play();
        }
    }
}
