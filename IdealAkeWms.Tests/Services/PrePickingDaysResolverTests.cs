using FluentAssertions;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// Prioritaetsregel „Werkbank-Override gewinnt, sonst globaler Wert" (Spec
/// 2026-07-28-override-prepickingdays, Variante A). Die Regel steht bewusst an genau
/// EINER Codestelle und wird von FA-Liste, Leitstand und FA-Abarbeitungsliste je AG genutzt.
/// </summary>
public class PrePickingDaysResolverTests
{
    [Fact]
    public void Resolve_OverrideGesetzt_GewinntUeberGlobalenWert()
    {
        PrePickingDaysResolver.Resolve(3, 1).Should().Be(3);
    }

    [Fact]
    public void Resolve_OverrideNull_FaelltAufGlobalenWertZurueck()
    {
        PrePickingDaysResolver.Resolve(null, 1).Should().Be(1);
    }

    [Fact]
    public void Resolve_OverrideNull_UndAbweichenderGlobalerWert()
    {
        // Kein Override -> exakt der globale Settings-Wert, egal wie gross.
        PrePickingDaysResolver.Resolve(null, 7).Should().Be(7);
    }

    [Fact]
    public void Resolve_OverrideNull0_IstExpliziterWert_KeinSynonymFuerStandard()
    {
        // 0 = Vorkommissionierung am selben Tag wie der Kommissioniertermin.
        // Dieselbe Semantik wie die Werkbank-Liste (ColumnMap unterscheidet nach HasValue, nicht > 0).
        PrePickingDaysResolver.Resolve(0, 5).Should().Be(0);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(365, true)]
    public void IsOverrideActive_UnterscheidetStriktNachHasValue(int? overrideDays, bool expected)
    {
        PrePickingDaysResolver.IsOverrideActive(overrideDays).Should().Be(expected);
    }
}
