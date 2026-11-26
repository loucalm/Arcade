using Kenta.Level;
using Kenta.UI;
using NUnit.Framework;
using UnityEditor;

namespace Kenta.EditorTests
{
    public class LevelSelectTests
    {
        const int PanelWidth = 118;

        static PixelFontData Font => AssetDatabase.LoadAssetAtPath<PixelFontData>("Assets/_Project/ScriptableObjects/PixelFont.asset");

        [Test]
        public void AllLevelNamesFitInfoPanel()
        {
            var font = Font;
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>("Assets/_Project/ScriptableObjects/LevelCatalog.asset");
            Assert.IsNotNull(font);
            Assert.IsNotNull(catalog);
            foreach (var level in catalog.levels)
            {
                string label = level.Label.ToUpperInvariant();
                int scale = LevelSelectScreen.NameScale(font, label, PanelWidth);
                Assert.LessOrEqual(PixelText.MeasureWidth(font, label, scale), PanelWidth, label);
            }
        }

        [Test]
        public void SongTitleStopsBeforeTheStars()
        {
            var font = Font;
            int max = LevelCard.SongMaxWidth(LevelListLayout.Right - LevelListLayout.SelectedLeft);   // carte sélectionnée
            string fit = LevelCard.FitText(font, "WE'RE THE RESISTORS", max);
            Assert.LessOrEqual(PixelText.MeasureWidth(font, fit, 1), max);
            StringAssert.EndsWith(".", fit);
            Assert.AreEqual("MAIN THEME", LevelCard.FitText(font, "MAIN THEME", max));             // un titre court reste entier
        }

        [Test]
        public void NameScale_ShortNameIsBig() => Assert.AreEqual(2, LevelSelectScreen.NameScale(Font, "LA SYLVE", PanelWidth));

        [Test]
        public void NameScale_LongNameFallsBack() => Assert.AreEqual(1, LevelSelectScreen.NameScale(Font, "LE GRAND ORAGE", PanelWidth));
    }
}
