using Kenta.FX;
using NUnit.Framework;
using UnityEngine;

public class CrtMathTests
{
    const float Fish = 0.06f, Curve = 0.12f, Corner = 0.05f, Aspect = 16f / 9f;

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

    /// <summary>Les coins du HUD (score, compteur : ~18 × 12 px du bord en 1920×1080) restent visibles.</summary>
    [Test]
    public void Hud_CornersStayVisible()
    {
        float mx = 18f / 1920f, my = 12f / 1080f;
        foreach (var src in new[] { new Vector2(mx, my), new Vector2(1 - mx, my), new Vector2(mx, 1 - my), new Vector2(1 - mx, 1 - my) })
        {
            Vector2 p = src;
            for (int i = 0; i < 50; i++) p += src - CrtMath.SourceUv(p, Fish, Curve);
            Near(src, CrtMath.SourceUv(p, Fish, Curve), 1e-4f);
            Assert.That(p.x, Is.InRange(0f, 1f));
            Assert.That(p.y, Is.InRange(0f, 1f));
            Assert.Greater(CrtMath.CornerMask(p, Fish, Curve, Corner, Aspect), 0.5f, $"coin du HUD {src} mangé");
        }
    }
}
