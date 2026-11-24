using Kenta.Level;
using NUnit.Framework;

namespace Kenta.EditorTests
{
    /// <summary>Rampes (sol en pente) et mort par chute calculée localement (LevelTerrain.KillY).</summary>
    public class LevelTerrainSlopeTests
    {
        static LevelTerrain Make(params GroundSpan[] spans)
        {
            var t = new LevelTerrain();
            foreach (var s in spans) t.Add(s);
            t.Build();
            return t;
        }

        [Test]
        public void Slope_TopAt_Interpolates()
        {
            var s = new GroundSpan(0f, 4f, 0f, 2f, false);
            Assert.AreEqual(1f, s.TopAt(2f), 1e-5f);
            Assert.AreEqual(2f, s.TopAt(9f), 1e-5f);     // borné au segment
            Assert.IsTrue(s.Sloped);
        }

        [Test]
        public void Slope_SupportFollowsRamp_UpAndDown()
        {
            var t = Make(new GroundSpan(-5f, 0f, 0f, false), new GroundSpan(0f, 2f, 0f, 3f, false), new GroundSpan(2f, 8f, 3f, false));
            // à mi-rampe, pieds 0,2 u sous la pente (une image de retard) : on suit quand même
            Assert.AreEqual(1.5f, t.SupportTop(1f, 0.25f, 1.3f), 1e-4f);
            // en haut, sur le plat : appui exact
            Assert.AreEqual(3f, t.SupportTop(4f, 0.25f, 3f), 1e-4f);
            // un sol plat loin de la rampe ne « happe » pas des pieds 0,3 u au-dessus
            Assert.IsTrue(float.IsNegativeInfinity(t.SupportTop(6f, 0.25f, 3.3f)));
        }

        [Test]
        public void KillY_DropToLowerGround_IsNotDeadly()
        {
            // plateau à 10, puis sol à 0 : tomber de 10 u vers le sol du dessous n'est pas mortel
            var t = Make(new GroundSpan(-5f, 0f, 10f, false), new GroundSpan(0f, 10f, 0f, false));
            Assert.AreEqual(-2.5f, t.KillY(3f), 1e-4f);
        }

        [Test]
        public void KillY_OverGap_UsesLowestEdge()
        {
            var t = Make(new GroundSpan(-5f, 0f, 4f, false), new GroundSpan(2f, 8f, 6f, false));
            Assert.AreEqual(4f - LevelTerrain.FallDeath, t.KillY(1f), 1e-4f);
        }

        [Test]
        public void KillY_IgnoresPlatforms()
        {
            var t = Make(new GroundSpan(-5f, 0f, 0f, false), new GroundSpan(0.5f, 3f, 1f, true), new GroundSpan(4f, 8f, 0f, false));
            Assert.AreEqual(-LevelTerrain.FallDeath, t.KillY(2f), 1e-4f);
        }
    }
}
