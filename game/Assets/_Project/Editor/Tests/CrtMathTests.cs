using Kenta.FX;
using NUnit.Framework;
using UnityEngine;

public class CrtMathTests
{
    const float Fish = 0.06f, Curve = 0.12f, Corner = 0.05f, Aspect = 16f / 9f, Zoom = 1.04f, Border = 0.02f;

    static void Near(Vector2 expected, Vector2 actual, float eps = 1e-5f)
    {
        Assert.AreEqual(expected.x, actual.x, eps);
        Assert.AreEqual(expected.y, actual.y, eps);
    }

    [Test] public void Center_IsFixed() => Near(new Vector2(0.5f, 0.5f), CrtMath.SourceUv(new Vector2(0.5f, 0.5f), Fish, Curve));

    [Test]
    public void FisheyeOnly_KeepsCorners()
    {
        Near(Vector2.zero, CrtMath.SourceUv(Vector2.zero, Fish, 0f));
        Near(Vector2.one, CrtMath.SourceUv(Vector2.one, Fish, 0f));
    }

    [Test] public void Fisheye_MagnifiesCenter() => Assert.Less(CrtMath.SourceUv(new Vector2(0.6f, 0.5f), Fish, Curve).x, 0.6f);

    [Test] public void ScreenCorner_IsBlack() => Assert.AreEqual(0f, CrtMath.CornerMask(new Vector2(0.002f, 0.002f), Fish, Curve, Corner, Aspect));

    [Test] public void ScreenCenter_IsVisible() => Assert.AreEqual(1f, CrtMath.CornerMask(new Vector2(0.5f, 0.5f), Fish, Curve, Corner, Aspect));

    /// <summary>Avec le dézoom, toute l'image reste visible : bords et coins (HUD compris), à 2 px près.</summary>
    [Test]
    public void WholeImage_StaysVisible()
    {
        const float m = 0.0015f;
        for (int i = 0; i <= 40; i++)
        {
            float t = m + i / 40f * (1 - 2 * m);
            foreach (var src in new[] { new Vector2(t, m), new Vector2(t, 1 - m), new Vector2(m, t), new Vector2(1 - m, t) })
            {
                Vector2 p = src;
                for (int k = 0; k < 300; k++) p += (src - CrtMath.SourceUv(p, Fish, Curve, Zoom)) / Zoom;
                Near(src, CrtMath.SourceUv(p, Fish, Curve, Zoom), 1e-4f);
                Assert.That(p.x, Is.InRange(0f, 1f), $"{src} hors écran");
                Assert.That(p.y, Is.InRange(0f, 1f), $"{src} hors écran");
                Assert.Greater(CrtMath.CornerMask(p, Fish, Curve, Corner, Aspect, Zoom, Border), 0.5f, $"{src} mangé par un coin");
            }
        }
    }
}
