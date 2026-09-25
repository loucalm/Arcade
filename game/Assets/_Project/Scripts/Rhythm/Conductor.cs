using System;
using UnityEngine;

namespace RythmeRunner.Rhythm
{
    /// <summary>
    /// Horloge maître du jeu : tout ce qui dépend du rythme lit SongTime / SongBeat ou s'abonne à OnBeat.
    ///
    /// Source de vérité = position de lecture audio (timeSamples). Sur WebGL cette position avance par
    /// paquets : on avance donc une horloge lissée avec unscaledDeltaTime et on la recale sur l'audio
    /// à chaque nouveau paquet (correction douce, ou saut si la dérive dépasse resyncThreshold).
    /// Démarrage via Play() + timeSamples : PlayScheduled/dspTime ne sont pas garantis sur WebGL.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class Conductor : MonoBehaviour
    {
        public static Conductor Instance { get; private set; }

        [Tooltip("Au-delà de cette dérive (s), l'horloge lissée saute sur la position audio.")]
        [SerializeField] float resyncThreshold = 0.02f;
        [Tooltip("Part de l'erreur corrigée à chaque paquet audio sous le seuil (0..1).")]
        [SerializeField, Range(0f, 1f)] float softCorrection = 0.1f;

        /// <summary>Appelé une fois par beat entier franchi (beat 0, 1, 2…). beat % BeatsPerBar == 0 → début de mesure.</summary>
        public event Action<int> OnBeat;
        /// <summary>Appelé quand un morceau non bouclé se termine.</summary>
        public event Action OnSongEnd;

        public SongData Song { get; private set; }
        public bool IsPlaying { get; private set; }
        public float Bpm => Song != null ? Song.bpm : 120f;
        public int BeatsPerBar => Song != null ? Song.beatsPerBar : 4;
        public double SecondsPerBeat => 60.0 / Bpm;

        /// <summary>Temps du morceau en secondes, 0 = beat 0 (offset déjà retiré).</summary>
        public double SongTime => smoothedAudioTime + loopCount * clipLength - Offset;
        /// <summary>Position en beats (fractionnaire).</summary>
        public double SongBeat => SongTime / SecondsPerBeat;
        /// <summary>Dernière dérive mesurée entre horloge lissée et audio (s), pour le debug.</summary>
        public double LastDrift { get; private set; }

        AudioSource source;
        double smoothedAudioTime;
        double lastRawAudioTime = -1;
        double clipLength;
        int loopCount;
        int lastBeat = -1;
        // WebGL : le navigateur décode le clip en asynchrone. Tant que frequency/samples valent 0,
        // on ne peut ni seeker ni lire la position : on mémorise le seek et on l'applique plus tard.
        double pendingSeekBeat = double.NaN;

        double Offset => Song != null ? Song.offsetSeconds : 0.0;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Temps (s) d'un beat, relatif au beat 0. Position monde : x = BeatToSeconds(beat) * runSpeed.</summary>
        public double BeatToSeconds(double beat) => beat * SecondsPerBeat;
        public double SecondsToBeat(double seconds) => seconds / SecondsPerBeat;

        /// <summary>Lance un morceau à partir d'un beat donné (0 = début). loop = pour les musiques de menu.</summary>
        public void Play(SongData song, double startBeat = 0, bool loop = false, bool useRemix = false)
        {
            AudioClip clip = useRemix && song.remixClip != null ? song.remixClip : song.clip;
            if (clip == null) { Debug.LogError($"Conductor: pas de clip pour « {song.title} »."); return; }

            Song = song;
            source.clip = clip;
            source.loop = loop;
            clipLength = 0;
            source.Play();
            IsPlaying = true;
            Seek(startBeat);
        }

        /// <summary>Repositionne la lecture sur un beat (checkpoint / respawn).</summary>
        public void Seek(double beat)
        {
            if (source.clip == null) return;
            lastBeat = (int)Math.Floor(beat) - 1;
            if (!IsClipReady()) { pendingSeekBeat = beat; smoothedAudioTime = 0; loopCount = 0; return; }
            pendingSeekBeat = double.NaN;

            double audioTime = Math.Max(0.0, BeatToSeconds(beat) + Offset);
            loopCount = 0;
            if (source.loop && clipLength > 0)
            {
                loopCount = (int)(audioTime / clipLength);
                audioTime -= loopCount * clipLength;
            }
            source.timeSamples = Mathf.Clamp((int)(audioTime * source.clip.frequency), 0, source.clip.samples - 1);
            smoothedAudioTime = audioTime;
            lastRawAudioTime = audioTime;
        }

        bool IsClipReady()
        {
            var clip = source.clip;
            if (clip == null || clip.frequency <= 0 || clip.samples <= 0) return false;
            if (clipLength <= 0) clipLength = (double)clip.samples / clip.frequency;
            return true;
        }

        public void Stop()
        {
            source.Stop();
            IsPlaying = false;
        }

        public void SetVolume(float volume) => source.volume = volume;

        void Update()
        {
            if (!IsPlaying) return;
            if (!IsClipReady()) return;
            if (!double.IsNaN(pendingSeekBeat)) Seek(pendingSeekBeat);

            // Fin de morceau. On vérifie la position : sur WebGL, isPlaying peut être faux tant que
            // le navigateur n'a pas autorisé l'audio (aucune interaction encore).
            if (!source.isPlaying && !source.loop && smoothedAudioTime >= clipLength - 0.05)
            {
                IsPlaying = false;
                OnSongEnd?.Invoke();
                return;
            }

            double rawAudioTime = (double)source.timeSamples / source.clip.frequency;

            // Bouclage du clip : la position audio repart de 0.
            if (source.loop && rawAudioTime < lastRawAudioTime - clipLength * 0.5)
            {
                loopCount++;
                smoothedAudioTime -= clipLength;
            }

            smoothedAudioTime += Time.unscaledDeltaTime * source.pitch;

            // Nouveau paquet audio → on mesure la dérive et on corrige.
            if (rawAudioTime != lastRawAudioTime)
            {
                double drift = rawAudioTime - smoothedAudioTime;
                LastDrift = drift;
                if (Math.Abs(drift) > resyncThreshold) smoothedAudioTime = rawAudioTime;
                else smoothedAudioTime += drift * softCorrection;
                lastRawAudioTime = rawAudioTime;
            }

            int beat = (int)Math.Floor(SongBeat);
            while (lastBeat < beat)
            {
                lastBeat++;
                if (lastBeat >= 0) OnBeat?.Invoke(lastBeat);
            }
        }
    }
}
