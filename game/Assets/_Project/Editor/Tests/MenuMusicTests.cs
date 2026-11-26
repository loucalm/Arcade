using Kenta.Core;
using Kenta.Rhythm;
using NUnit.Framework;
using UnityEngine;

namespace Kenta.EditorTests
{
    /// <summary>Musique des menus : reprendre au même endroit quand le morceau en cours est le même clip (revue du 25/11).</summary>
    public class MenuMusicTests
    {
        static SongData Song(AudioClip clip)
        {
            var s = ScriptableObject.CreateInstance<SongData>();
            s.clip = clip;
            s.bpm = 150f;
            s.offsetSeconds = 0.18f;
            return s;
        }

        [Test]
        public void SameClip_DifferentSongData_Resumes()
        {
            var clip = AudioClip.Create("main", 44100, 1, 44100, false);
            Assert.AreEqual(42.5, GameFlow.MenuResumeBeat(Song(clip), true, 42.5, Song(clip)), 1e-9);
        }

        [Test]
        public void OtherClip_StartsOver()
        {
            var a = AudioClip.Create("a", 44100, 1, 44100, false);
            var b = AudioClip.Create("b", 44100, 1, 44100, false);
            Assert.AreEqual(0.0, GameFlow.MenuResumeBeat(Song(a), true, 42.5, Song(b)), 1e-9);
        }

        [Test]
        public void Stopped_StartsOver()
        {
            var clip = AudioClip.Create("main", 44100, 1, 44100, false);
            Assert.AreEqual(0.0, GameFlow.MenuResumeBeat(Song(clip), false, 42.5, Song(clip)), 1e-9);
        }

        [Test]
        public void NegativeBeat_ClampsToZero()
        {
            var clip = AudioClip.Create("main", 44100, 1, 44100, false);
            Assert.AreEqual(0.0, GameFlow.MenuResumeBeat(Song(clip), true, -0.5, Song(clip)), 1e-9);
        }
    }
}
