using Kenta.FX;
using Kenta.Rhythm;
using NUnit.Framework;
using UnityEngine;

namespace Kenta.EditorTests
{
    /// <summary>Pièces : la note sur le temps de la pièce, et l'animation de ramassage au pixel près (2026-11-27).</summary>
    public class CoinPickupTests
    {
        [Test]
        public void Note_WaitsForTheCoinsOwnBeat()
        {
            // ramassée 0,3 temps avant son temps, à 117 BPM : la note attend 0,3 temps
            Assert.AreEqual(0.3 * 60.0 / 117.0, ActionSfx.DelayUntil(40.7, 41.0, 60.0 / 117.0), 1e-9);
        }

        [Test]
        public void Note_PlaysNow_WhenTheBeatIsPastOrFar()
        {
            Assert.AreEqual(0.0, ActionSfx.DelayUntil(41.1, 41.0, 0.5));          // déjà passé : tout de suite
            Assert.AreEqual(0.0, ActionSfx.DelayUntil(30.0, 41.0, 0.5));          // trop loin (reprise, saut) : tout de suite
        }

        [Test]
        public void Pop_RisesOnThePixelGrid_ThenBlinks()
        {
            for (float t = 0f; t < 0.16f; t += 0.007f)
            {
                float px = CoinPickup.RiseAt(t) * PixelScreen.Ppu;
                Assert.AreEqual(Mathf.Round(px), px, 1e-4f, $"t = {t}");
                Assert.LessOrEqual(px, 6f + 1e-4f);
            }
            Assert.IsTrue(CoinPickup.VisibleAt(0.01f));
            Assert.IsFalse(CoinPickup.VisibleAt(0.12f) && CoinPickup.VisibleAt(0.085f) && CoinPickup.VisibleAt(0.125f) && CoinPickup.VisibleAt(0.15f));
        }

        [Test]
        public void Play_PopsThenHidesTheCoin_WhichComesBackWhole()
        {
            var coin = new GameObject("Coin");
            try
            {
                var sprite = new GameObject("Sprite").AddComponent<SpriteRenderer>();
                sprite.transform.SetParent(coin.transform, false);
                coin.transform.localPosition = new Vector3(2f, 3f, 0f);
                var pop = coin.AddComponent<CoinPickup>();

                pop.Play();
                Assert.IsTrue(pop.Playing && pop.enabled, "Play ne doit pas être annulé");
                pop.Tick(0.05f);
                Assert.Greater(coin.transform.localPosition.y, 3f, "la pièce saute");
                for (int i = 0; i < 4; i++) pop.Tick(0.05f);

                Assert.IsFalse(coin.activeSelf, "ramassée, la pièce disparaît");
                Assert.AreEqual(new Vector3(2f, 3f, 0f), coin.transform.localPosition);
                Assert.IsTrue(sprite.enabled);
                Assert.IsFalse(pop.Playing || pop.enabled, "au repos : pas d'Update");
            }
            finally
            {
                Object.DestroyImmediate(coin);
            }
        }
    }
}
