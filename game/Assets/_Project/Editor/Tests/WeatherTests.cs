using Kenta.FX;
using Kenta.Level;
using NUnit.Framework;

namespace Kenta.EditorTests
{
    /// <summary>Météo du décor (ADR-047) : apparitions calées sur le temps, courbes lues sur le morceau, éruption.</summary>
    public class WeatherTests
    {
        static ChartEvent Weather(float beat, string kind, float intensity, float length) =>
            new() { beat = beat, type = "weather", @params = new ChartParams { variant = kind, intensity = intensity, length = length } };

        [Test]
        public void Ticks_CountsSixteenths()
        {
            Assert.AreEqual(4, WeatherMath.Ticks(0.0, 1.0, 0.25, 99));
            Assert.AreEqual(0, WeatherMath.Ticks(0.1, 0.2, 0.25, 99));
            Assert.AreEqual(1, WeatherMath.Ticks(0.2, 0.25, 0.25, 99));     // le pas 0,25 est franchi
        }

        [Test]
        public void Ticks_CapsAfterSeek()
        {
            // reprise à un checkpoint ou image très longue : pas de déluge de gouttes d'un coup
            Assert.AreEqual(2, WeatherMath.Ticks(10, 200, 0.25, 2));
            Assert.AreEqual(0, WeatherMath.Ticks(50, 10, 0.25, 2));          // retour en arrière
        }

        [Test]
        public void SpawnCount_FollowsIntensity()
        {
            Assert.AreEqual(0, WeatherMath.SpawnCount(0f, 6));
            Assert.AreEqual(3, WeatherMath.SpawnCount(0.5f, 6));
            Assert.AreEqual(6, WeatherMath.SpawnCount(1f, 6));
        }

        [Test]
        public void WeatherTracks_FromChart()
        {
            var w = new WeatherTracks();
            Assert.IsTrue(w.AddEvent(Weather(8, "rain", 0.5f, 0)));
            Assert.AreEqual(0f, w.Rain.Evaluate(7.9), 1e-5f);
            Assert.AreEqual(0.5f, w.Rain.Evaluate(8.1), 1e-5f);
            Assert.AreEqual(0f, w.Snow.Evaluate(9), 1e-5f);
            Assert.AreSame(w.Rain, w.For("rain"));
            Assert.IsNull(w.For("fog"));
            Assert.IsFalse(w.AddEvent(Weather(9, "fog", 1f, 0)));             // météo inconnue : ignorée
        }

        [Test]
        public void WeatherTracks_SurviveSeek()
        {
            // la reprise relit la courbe au temps du morceau : même valeur, qu'on y arrive en jouant ou par un saut
            var w = new WeatherTracks();
            w.AddEvent(Weather(128, "rain", 0.35f, 8));
            w.AddEvent(Weather(176, "rain", 1f, 4));
            Assert.AreEqual(0.35f, WeatherFx.IntensityOf(w, "rain", 150), 1e-5f);
            Assert.AreEqual(1f, WeatherFx.IntensityOf(w, "rain", 190), 1e-5f);
        }

        [Test]
        public void WeatherTracks_Null_IsZero()
        {
            Assert.AreEqual(0f, WeatherFx.IntensityOf(null, "rain", 10));
            var w = new WeatherTracks();
            w.AddEvent(Weather(0, "snow", 1f, 0));
            w.Clear();
            Assert.AreEqual(0f, WeatherFx.IntensityOf(w, "snow", 10));
        }

        [Test]
        public void Wrap_KeepsFlakesOnScreen()
        {
            // la caméra court vite : un flocon sorti à gauche revient à droite au lieu de disparaître
            Assert.AreEqual(7f, WeatherMath.Wrap(-9f, 8f), 1e-5f);
            Assert.AreEqual(-7f, WeatherMath.Wrap(9f, 8f), 1e-5f);
            Assert.AreEqual(3f, WeatherMath.Wrap(3f, 8f), 1e-5f);
        }

        [Test]
        public void Weather_MovesAfterTheCamera()
        {
            // CameraRig bouge la caméra dans LateUpdate (ordre -50) : placées dans Update, les particules avaient une
            // image de retard (lave à gauche du cratère, neige qui tremble)
            var t = typeof(WeatherFx);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            Assert.IsNotNull(t.GetMethod("LateUpdate", flags));
            Assert.IsNull(t.GetMethod("Update", flags));
        }

        [Test]
        public void Lava_IsDrawnWithTheFarLayer()
        {
            // la lave du volcan lointain passe derrière le calque du milieu (+20) et le proche (+30)
            int order = BiomeBackdrop.FarFxOrder(-100);
            Assert.Greater(order, -100 + 10);
            Assert.Less(order, -100 + 20);
        }

        [Test]
        public void Snow_FitsThePool_AtFullStorm()
        {
            // avalanche du Givre : 168 BPM, intensité 1, flocons vivant jusqu'à 11,7 s en plan large 640
            float perSecond = WeatherMath.SnowPerSecond(168f, 4, 1f);
            Assert.LessOrEqual(perSecond * 11.7f, 120f, $"{perSecond} flocons/s");
        }

        [Test]
        public void Pool_Recycles_Oldest()
        {
            int slot = 0;
            for (int i = 0; i < 120; i++) slot = WeatherMath.NextSlot(slot, 120);
            Assert.AreEqual(0, slot);
            Assert.AreEqual(1, WeatherMath.NextSlot(0, 120));
        }

        [Test]
        public void EruptionGlow_NoVents_Zero()
        {
            Assert.AreEqual(0f, BiomeBackdrop.EruptionGlow(0.2f, 1f, false));
        }

        [Test]
        public void EruptionGlow_Stepped()
        {
            const float max = 0.35f, step = max / 4f;
            float peak = 0f;
            for (float t = 0f; t <= 1.2f; t += 0.01f)
            {
                float g = BiomeBackdrop.EruptionGlow(t, 1f, true);
                float k = g / step;
                Assert.AreEqual(UnityEngine.Mathf.Round(k), k, 1e-3f, $"t = {t} : pas un palier ({g})");
                peak = UnityEngine.Mathf.Max(peak, g);
            }
            Assert.LessOrEqual(peak, max + 1e-5f);
            Assert.Greater(peak, 0f);
            Assert.AreEqual(0f, BiomeBackdrop.EruptionGlow(1.1f, 1f, true));
        }
    }
}
