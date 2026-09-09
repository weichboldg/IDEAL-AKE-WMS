using FluentAssertions;
using IDEALAKEWMSService.Common;
using Xunit;

namespace IDEALAKEWMSService.Tests.Common;

/// <summary>
/// Entprellung der Sammelmail „unbekannte Arbeitsbereiche" (S1). Der Materialisierungs-Sync laeuft
/// alle 15 Minuten; ohne diese Klasse gingen bis zur Nachpflege 96 identische Mails pro Tag raus,
/// und dann wird auch die eine wichtige uebersehen. Die Klasse ist DB-frei und deshalb hier
/// vollstaendig testbar — im Sync-Test selbst wird sie gestubbt, weil der Versandpfad dahinter
/// eine echte SQL-Verbindung braucht.
/// </summary>
public class UnknownWorkplaceStateTests
{
    [Fact]
    public void HasChanged_FirstReport_IsChange()
    {
        var sut = new UnknownWorkplaceState();
        sut.HasChanged(new[] { "S-01" }).Should().BeTrue();
    }

    [Fact]
    public void HasChanged_SameSetTwice_SecondIsNoChange()
    {
        var sut = new UnknownWorkplaceState();
        sut.HasChanged(new[] { "S-01", "K-02" });
        sut.HasChanged(new[] { "S-01", "K-02" }).Should().BeFalse();
    }

    [Fact]
    public void HasChanged_IgnoresOrder()
    {
        var sut = new UnknownWorkplaceState();
        sut.HasChanged(new[] { "S-01", "K-02" });
        sut.HasChanged(new[] { "K-02", "S-01" }).Should().BeFalse();
    }

    [Fact]
    public void HasChanged_IsCaseInsensitive()
    {
        var sut = new UnknownWorkplaceState();
        sut.HasChanged(new[] { "S-01" });
        sut.HasChanged(new[] { "s-01" }).Should().BeFalse();
    }

    [Fact]
    public void HasChanged_AddedName_IsChange()
    {
        var sut = new UnknownWorkplaceState();
        sut.HasChanged(new[] { "S-01" });
        sut.HasChanged(new[] { "S-01", "H4-04" }).Should().BeTrue();
    }

    [Fact]
    public void HasChanged_RemovedName_IsChange()
    {
        var sut = new UnknownWorkplaceState();
        sut.HasChanged(new[] { "S-01", "H4-04" });
        sut.HasChanged(new[] { "S-01" }).Should().BeTrue();
    }

    [Fact]
    public void HasChanged_EmptyOnEmpty_IsNoChange()
    {
        var sut = new UnknownWorkplaceState();
        sut.HasChanged(System.Array.Empty<string>()).Should().BeFalse();
    }

    /// <summary>
    /// Der fachlich wichtigste Fall: Ein Arbeitsbereich wird gemeldet, jemand legt die Werkbank an
    /// (Menge leer), spaeter taucht derselbe Name wieder auf. Das MUSS erneut gemeldet werden —
    /// sonst bleibt ein wiederkehrender Datenfehler dauerhaft unsichtbar. Deshalb schreibt der
    /// Sync den Zustand auch bei leerer Menge fort.
    /// </summary>
    [Fact]
    public void HasChanged_NameReappearsAfterBeingResolved_IsChangeAgain()
    {
        var sut = new UnknownWorkplaceState();
        sut.HasChanged(new[] { "S-01" }).Should().BeTrue();
        sut.HasChanged(System.Array.Empty<string>()).Should().BeTrue();   // behoben
        sut.HasChanged(new[] { "S-01" }).Should().BeTrue();               // wieder da -> erneut melden
    }
}
