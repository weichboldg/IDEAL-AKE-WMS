using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>Dienstleister-Meldung (E8a, Kreditoren-Spec AK 11–13, 17): Trim, Gross/Klein, leer, inaktiv, mehrere Zeilen je Text.</summary>
public class CreditorAssignmentTests
{
    private static FaHierarchyOrderInfo Info(string? d) => new() { HauptFA = 1, Dienstleister = d };
    private static Creditor Kred(string? shortName, bool active = true) => new() { ShortName = shortName, Name = "x", IsActive = active };

    [Fact]
    public void Leer_KeineMeldung()
    {
        CreditorAssignment.FindUnassigned([], [Kred("A")]).Should().BeEmpty();
        CreditorAssignment.FindUnassigned([Info(null), Info(""), Info("   ")], []).Should().BeEmpty();
    }

    [Fact]
    public void TrimUndGrossKlein_ZaehltAlsTreffer()
    {
        CreditorAssignment.FindUnassigned([Info("  PULVER "), Info("pulver")], [Kred(" Pulver ")]).Should().BeEmpty();
    }

    [Fact]
    public void NichtZugeordnet_MehrereZeilenJeText_Gruppiert_Sortiert()
    {
        var result = CreditorAssignment.FindUnassigned(
            [Info("Zink"), Info("glas "), Info("Glas"), Info("GLAS"), Info("Pulver")],
            [Kred("Pulver"), Kred(null)]);

        result.Should().Equal(
            new UnassignedServiceProvider("glas", 3, false),
            new UnassignedServiceProvider("Zink", 1, false));
    }

    [Fact]
    public void NurInaktiverKreditor_WirdAlsInaktivGemeldet_AktiverGewinnt()
    {
        CreditorAssignment.FindUnassigned([Info("Pulver")], [Kred("Pulver", active: false)])
            .Should().Equal(new UnassignedServiceProvider("Pulver", 1, true));
        CreditorAssignment.FindUnassigned([Info("Pulver")], [Kred("Pulver", active: false), Kred("PULVER")]).Should().BeEmpty();
    }
}
