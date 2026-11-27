using System;
using UnityEngine;

namespace Kenta.Rhythm
{
    /// <summary>
    /// Sons d'action quantifiés à la double-croche (invariant n°4) : un son déclenché un peu avant
    /// la double-croche suivante est retardé pour tomber pile dessus. Le joueur « joue la musique ».
    /// Pool de sources pour les sons qui se chevauchent.
    /// </summary>
    public class ActionSfx : MonoBehaviour
    {
        public static ActionSfx Instance { get; private set; }

        [SerializeField] AudioClip jump, hit, kill, lum, land, hurt, checkpoint;
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
        /// <summary>Son de frappe du héros choisi (null : son commun).</summary>
        public AudioClip HeroHit { get; set; }

        public void Hit() => Play(HeroHit != null ? HeroHit : hit, 1f, 0.6f);
        public void Kill() => Play(kill, 1f, 0.9f);
        public void Land() => Play(land, 1f, 0.5f, quantized: false);
        public void Hurt() => Play(hurt, 1f, 0.9f, quantized: false);
        public void Checkpoint() => Play(checkpoint, 1f, 0.8f);

        /// <summary>Lum n°index d'une ligne : la note monte dans la gamme.</summary>
        public void Lum(int index) =>
            Play(lum, Mathf.Pow(2f, Pentatonic[Mathf.Clamp(index, 0, Pentatonic.Length - 1)] / 12f), 0.55f);

        /// <summary>Pièce qui joue une note de la mélodie : `semitones` par rapport au la du son de pièce.</summary>
        public void LumNote(int semitones) => Play(lum, Mathf.Pow(2f, Mathf.Clamp(semitones, -24, 24) / 12f), 0.55f);

        /// <summary>
        /// Note de la pièce posée au temps `beat` de la chart, jouée sur ce temps-là (2026-11-27). On ramasse une pièce
        /// dès qu'on passe à moins du rayon de ramassage, soit environ 0,3 temps avant elle : la quantification à la
        /// double-croche la plus proche la faisait sonner une double-croche trop tôt, à chaque pièce.
        /// </summary>
        public void LumNote(int semitones, double beat) =>
            PlayAtBeat(lum, Mathf.Pow(2f, Mathf.Clamp(semitones, -24, 24) / 12f), 0.55f, beat);

        /// <summary>Délai (s) jusqu'au temps `beat` (0 s'il est passé ou trop loin : plus d'une demi-mesure).</summary>
        public static double DelayUntil(double nowBeat, double beat, double secondsPerBeat, double maxBeats = 2.0)
        {
            double ahead = beat - nowBeat;
            return ahead > 0 && ahead <= maxBeats ? ahead * secondsPerBeat : 0;
        }

        /// <summary>Joue un son sur un temps précis de la chart ; s'il est déjà passé, tout de suite (quantifié).</summary>
        public void PlayAtBeat(AudioClip clip, float pitch, float gain, double beat)
        {
            var conductor = Conductor.Instance;
            if (conductor == null || !conductor.IsPlaying) { Play(clip, pitch, gain); return; }
            double delay = DelayUntil(conductor.SongBeat, beat, conductor.SecondsPerBeat);
            if (delay <= 0) { Play(clip, pitch, gain); return; }
            if (clip == null || pool == null) return;
            var source = pool[next];
            next = (next + 1) % pool.Length;
            source.Stop();
            source.clip = clip;
            source.pitch = pitch;
            source.volume = gain * volume;
            source.PlayDelayed((float)delay);
        }

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
