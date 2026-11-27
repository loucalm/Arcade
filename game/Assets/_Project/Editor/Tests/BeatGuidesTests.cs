using Kenta.FX;
using NUnit.Framework;

namespace Kenta.EditorTests
{
    public class BeatGuidesTests
    {
        [Test]
        public void FirstBeat_IsTheFirstTraitInsideTheView()
        {
            Assert.AreEqual(0, BeatGuides.FirstBeat(-5f, 3.2f));        // début du niveau : jamais de temps négatif
            Assert.AreEqual(4, BeatGuides.FirstBeat(10f, 3.2f));        // 10 u / 3,2 u par temps = 3,1 → temps 4
            Assert.AreEqual(4, BeatGuides.FirstBeat(12.8f, 3.2f));      // pile sur un temps : il est dans la vue
        }
    }
}
