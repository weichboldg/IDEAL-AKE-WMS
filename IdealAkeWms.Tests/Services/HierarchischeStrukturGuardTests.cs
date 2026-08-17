using FluentAssertions;
using IdealAkeWms.Services.HierarchischeStruktur;
using Xunit;

namespace IdealAkeWms.Tests;

public class HierarchischeStrukturGuardTests
{
    // Kein Datensatz -> alles frei (Einwegtor noch nicht geschlossen).
    [Theory]
    [InlineData(GuardedSetting.Master, false, true)]
    [InlineData(GuardedSetting.Master, true, false)]
    [InlineData(GuardedSetting.AutoErledigt, false, true)]
    public void OhneHierarchischeDaten_JedeAenderungErlaubt(GuardedSetting s, bool cur, bool req)
    {
        HierarchischeStrukturGuard.Evaluate(s, cur, req, hierarchicalDataExists: false)
            .Allowed.Should().BeTrue();
    }

    // No-op-Schreibvorgang (Ist == Soll) ist immer erlaubt, auch gesperrt.
    [Fact]
    public void GleicherWert_ImmerErlaubt_AuchGesperrt()
    {
        HierarchischeStrukturGuard.Evaluate(GuardedSetting.Master, true, true, true)
            .Allowed.Should().BeTrue();
    }

    // Gesperrt: Master lässt sich nicht mehr deaktivieren, Runbook referenziert.
    [Fact]
    public void MasterDeaktivieren_Gesperrt_Abgelehnt_MitRunbook()
    {
        var d = HierarchischeStrukturGuard.Evaluate(GuardedSetting.Master, true, false, true);
        d.Allowed.Should().BeFalse();
        d.RejectionReason.Should().Contain("RUNBOOK-FA-HIERARCHIE-RUECKBAU.md");
    }

    // Gesperrt: Auto-Erledigt lässt sich nicht einschalten...
    [Fact]
    public void AutoErledigtEinschalten_Gesperrt_Abgelehnt()
    {
        HierarchischeStrukturGuard.Evaluate(GuardedSetting.AutoErledigt, false, true, true)
            .Allowed.Should().BeFalse();
    }

    // ...aber ausschalten bleibt erlaubt (Master-Flip schaltet ab).
    [Fact]
    public void AutoErledigtAusschalten_Gesperrt_Erlaubt()
    {
        HierarchischeStrukturGuard.Evaluate(GuardedSetting.AutoErledigt, true, false, true)
            .Allowed.Should().BeTrue();
    }
}
