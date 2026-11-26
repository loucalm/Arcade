using Kenta.Rhythm;
using NUnit.Framework;

public class ConductorEndTests
{
    [Test] public void SmoothedAtEnd_Ends() => Assert.IsTrue(Conductor.ReachedEnd(103.97, 103.9, 104.0));

    /// <summary>Le bug du niveau 2 : temps lissé 80 ms en retard quand la source s'arrête.</summary>
    [Test] public void SmoothedLagging_StillEnds() => Assert.IsTrue(Conductor.ReachedEnd(103.92, 103.95, 104.0));

    /// <summary>WebGL avant l'autorisation du son : source arrêtée au début, ce n'est pas une fin.</summary>
    [Test] public void NotStartedYet_DoesNotEnd() => Assert.IsFalse(Conductor.ReachedEnd(0.2, 0.0, 104.0));

    [Test] public void NoClip_DoesNotEnd() => Assert.IsFalse(Conductor.ReachedEnd(0, 0, 0));
}
