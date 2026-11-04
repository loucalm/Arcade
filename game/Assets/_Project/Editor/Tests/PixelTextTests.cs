using Kenta.UI;
using NUnit.Framework;
using UnityEngine;

namespace Kenta.EditorTests
{
    public class PixelTextTests
    {
        static PixelFontData FakeFont()
        {
            var font = ScriptableObject.CreateInstance<PixelFontData>();
            var tex = new Texture2D(16, 16);
            Sprite S(int w) => Sprite.Create(tex, new Rect(0, 0, w, 10), new Vector2(0.5f, 0.5f), 100f);
            font.chars = "IM ";
            font.sprites = new[] { S(4), S(6), S(4) };   // largeur du glyphe en pixels
            return font;
        }

        [Test]
        public void AdvanceOf_IsSpriteWidthPlusOne()
        {
            var f = FakeFont();
            Assert.AreEqual(5, f.AdvanceOf('I'));
            Assert.AreEqual(7, f.AdvanceOf('M'));
        }

        [Test]
        public void AdvanceOf_UnknownCharFallsBackToSix()
        {
            var f = FakeFont();
            f.chars = "I";
            f.sprites = new[] { f.sprites[0] };
            Assert.AreEqual(6, f.AdvanceOf('#'));
        }

        [Test]
        public void MeasureWidth_NarrowTextIsNarrower()
        {
            var f = FakeFont();
            Assert.Less(PixelText.MeasureWidth(f, "II", 1), PixelText.MeasureWidth(f, "MM", 1));
        }

        [Test]
        public void MeasureWidth_EmptyIsZero()
        {
            Assert.AreEqual(0, PixelText.MeasureWidth(FakeFont(), "", 2));
        }

        [Test]
        public void MeasureWidth_ScalesAndDropsLastGap()
        {
            var f = FakeFont();
            // « IM » = 5 + 6 (pas d'espace après la dernière lettre), ×2
            Assert.AreEqual(22, PixelText.MeasureWidth(f, "IM", 2));
        }
    
        [Test]
        public void PlateSize_PadsTextBy3x2Pixels()
        {
            var f = FakeFont();
            // « IM » : 11 px de large, glyphes de 10 px de haut ; marge 3 px de chaque côté, 2 px en haut et en bas
            Assert.AreEqual(new Vector2(17, 14), PixelText.PlateSize(f, "IM", 1));
            Assert.AreEqual(new Vector2(28, 24), PixelText.PlateSize(f, "IM", 2));
        }

        [Test]
        public void PlateSize_EmptyTextHasNoPlate()
        {
            Assert.AreEqual(Vector2.zero, PixelText.PlateSize(FakeFont(), "", 1));
        }
    }
}
