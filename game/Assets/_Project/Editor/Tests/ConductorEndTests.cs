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

    // SongEnded : la fin vue dans tous les cas (bug de la borne du 26/11 : le héros courait dans le vide après la fin)

    /// <summary>Build Web : la source reste « en lecture » après la fin, position figée ; le temps du morceau a dépassé le clip.</summary>
    [Test] public void Web_SourceStillPlayingPastTheEnd_Ends() =>
        Assert.IsTrue(Conductor.SongEnded(false, true, 104.2, 103.99, 103.99, 104.0));

    /// <summary>La source a fini et sa position est revenue à 0 alors qu'on était à la fin : c'est une fin, pas un retour au début.</summary>
    [Test] public void SourceWrappedToZeroAtTheEnd_Ends() =>
        Assert.IsTrue(Conductor.SongEnded(false, true, 103.7, 103.65, 0.0, 104.0));

    /// <summary>Le cas de Lou : temps lissé en retard quand la source s'arrête.</summary>
    [Test] public void StoppedWithSmoothedLagging_Ends() =>
        Assert.IsTrue(Conductor.SongEnded(false, false, 103.92, 103.95, 0.0, 104.0));

    [Test] public void InTheMiddle_DoesNotEnd() =>
        Assert.IsFalse(Conductor.SongEnded(false, true, 52.0, 51.98, 52.01, 104.0));

    /// <summary>WebGL avant l'autorisation du son : source arrêtée au début.</summary>
    [Test] public void WebBeforeAudioUnlock_DoesNotEnd() =>
        Assert.IsFalse(Conductor.SongEnded(false, false, 0.3, 0.0, 0.0, 104.0));

    /// <summary>Après une reprise au point (Seek en arrière), la position recule de peu : pas une fin.</summary>
    [Test] public void SeekBackAfterADeath_DoesNotEnd() =>
        Assert.IsFalse(Conductor.SongEnded(false, true, 60.0, 60.0, 59.99, 104.0));

    /// <summary>Musique des menus en boucle : jamais de fin.</summary>
    [Test] public void LoopingMenuMusic_NeverEnds() =>
        Assert.IsFalse(Conductor.SongEnded(true, true, 200.0, 103.9, 0.1, 104.0));
}
