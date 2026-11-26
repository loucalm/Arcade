using Kenta.FX;
using NUnit.Framework;
using UnityEngine;

namespace Kenta.EditorTests
{
    public class HoverTests
    {
        const float Amp = 4f / PixelScreen.Ppu, Period = 0.9f;

        [Test]
        public void Offset_StaysOnThePixelGrid_AndWithinTheAmplitude()
        {
            for (float t = 0f; t < 2f; t += 0.013f)
            {
                float o = Hover.Offset(Amp, Period, t);
                Assert.LessOrEqual(Mathf.Abs(o), Amp + 1e-5f, $"t = {t}");
                float px = o * PixelScreen.Ppu;
                Assert.AreEqual(Mathf.Round(px), px, 1e-4f, $"t = {t} : {px} px");
            }
        }

        [Test]
        public void Offset_ReallyMoves_UpAndDown()
        {
            Assert.AreEqual(Amp, Hover.Offset(Amp, Period, Period * 0.25f), 1e-5f);     // en haut
            Assert.AreEqual(-Amp, Hover.Offset(Amp, Period, Period * 0.75f), 1e-5f);    // en bas
        }
    }
}
