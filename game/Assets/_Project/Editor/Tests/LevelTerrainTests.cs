using Kenta.Level;
using NUnit.Framework;

namespace Kenta.EditorTests
{
    public class LevelTerrainTests
    {
        static LevelTerrain Flat()
        {
            var t = new LevelTerrain();
            t.Add(new GroundSpan(-10f, 10f, 0f, false));
            t.Build();
            return t;
        }

        [Test]
        public void Terrain_FlatGround_LandsWhenCrossingTop()
        {
            Assert.IsTrue(Flat().TryLand(0f, 0.25f, 0.1f, -0.05f, out float top));
            Assert.AreEqual(0f, top, 1e-5f);
        }

        [Test]
        public void Terrain_Gap_NoSupportInTheMiddle()
        {
            var t = new LevelTerrain();
            t.Add(new GroundSpan(-10f, 0f, 0f, false));
            t.Add(new GroundSpan(2f, 10f, 0f, false));
            t.Build();
            Assert.AreEqual(float.NegativeInfinity, t.SupportTop(1f, 0.25f, 0f));
            Assert.IsFalse(t.TryLand(1f, 0.25f, 0.1f, -0.1f, out _));
        }

        [Test]
        public void Terrain_Gap_OneFootOnTheEdgeStillSupports()
        {
            var t = new LevelTerrain();
            t.Add(new GroundSpan(-10f, 0f, 0f, false));
            t.Add(new GroundSpan(2f, 10f, 0f, false));
            t.Build();
            Assert.AreEqual(0f, t.SupportTop(0.2f, 0.25f, 0f), 1e-5f);
        }

        [Test]
        public void Terrain_Step_LandsOnHigherGround()
        {
            var t = new LevelTerrain();
            t.Add(new GroundSpan(-10f, 5f, 0f, false));
            t.Add(new GroundSpan(5f, 20f, 1f, false));
            t.Build();
            Assert.IsTrue(t.TryLand(6f, 0.25f, 1.3f, 0.9f, out float top));
            Assert.AreEqual(1f, top, 1e-5f);
        }

        [Test]
        public void Terrain_OneWayPlatform_PassThroughFromBelow()
        {
            var t = new LevelTerrain();
            t.Add(new GroundSpan(0f, 4f, 1f, true));
            t.Build();
            Assert.IsFalse(t.TryLand(2f, 0.25f, 0.8f, 1.2f, out _));   // on monte à travers
            Assert.IsTrue(t.TryLand(2f, 0.25f, 1.2f, 0.9f, out float top));
            Assert.AreEqual(1f, top, 1e-5f);
        }

        [Test]
        public void Terrain_SweptLanding_BigDt()
        {
            // à-coup du navigateur : on passe de 2,4 à -0,6 en une image au-dessus d'une plateforme à 1,0
            var t = new LevelTerrain();
            t.Add(new GroundSpan(-10f, 10f, -2f, false));
            t.Add(new GroundSpan(0f, 4f, 1f, true));
            t.Build();
            Assert.IsTrue(t.TryLand(2f, 0.25f, 2.4f, -0.6f, out float top));
            Assert.AreEqual(1f, top, 1e-5f);
        }

        [Test]
        public void Terrain_Build_SortsSpans()
        {
            var t = new LevelTerrain();
            t.Add(new GroundSpan(5f, 8f, 0f, false));
            t.Add(new GroundSpan(-3f, 1f, 0.5f, false));
            t.Build();
            Assert.AreEqual(-3f, t.Spans[0].x0, 1e-5f);
            Assert.AreEqual(5f, t.Spans[1].x0, 1e-5f);
        }

        [Test]
        public void Terrain_LowestTop()
        {
            var t = new LevelTerrain();
            t.Add(new GroundSpan(0f, 1f, 0.5f, false));
            t.Add(new GroundSpan(1f, 2f, -0.5f, false));
            t.Build();
            Assert.AreEqual(-0.5f, t.LowestTop, 1e-5f);
        }

        [Test]
        public void Terrain_HitsSide_WhenFallenBelowANeighbourTop()
        {
            var t = new LevelTerrain();
            t.Add(new GroundSpan(-10f, 0f, 0f, false));
            t.Add(new GroundSpan(2f, 10f, 0f, false));
            t.Build();
            Assert.IsTrue(t.HitsSide(2.1f, 0.25f, -0.6f));    // tombé dans le trou, on cogne le bord opposé
            Assert.IsFalse(t.HitsSide(2.1f, 0.25f, -0.2f));   // juste sous le bord : on remonte encore (tolérance)
            Assert.IsFalse(t.HitsSide(1f, 0.25f, -0.6f));     // au milieu du trou : rien
        }

        [Test]
        public void Terrain_HitsSide_IgnoresOneWayPlatforms()
        {
            var t = new LevelTerrain();
            t.Add(new GroundSpan(0f, 4f, 1f, true));
            t.Build();
            Assert.IsFalse(t.HitsSide(2f, 0.25f, 0.2f));
        }
    }
}
