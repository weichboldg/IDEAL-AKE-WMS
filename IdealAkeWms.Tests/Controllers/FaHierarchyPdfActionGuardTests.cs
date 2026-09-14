using System.Reflection;
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Filters;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>Die Pdf-Actions haben keine Controller-Tests (Verbraucher-Services sind konkrete Klassen
/// ohne Testfabrik). Dieser Waechter sichert die zwei Dinge, die beim Kopieren still verloren gehen:
/// die Attribut-Route mit <c>{hauptFa}</c> als Segment (Freigabe-Antwort 7) und dass der PDF-Download
/// ueber die Class-Level-Filter geschuetzt bleibt (AK 9).</summary>
public class FaHierarchyPdfActionGuardTests
{
    [Theory]
    [InlineData(typeof(FaHierarchyKommissionierListenController), "FaHierarchyKommissionierListen/Pdf/{hauptFa:int}")]
    [InlineData(typeof(FaHierarchyBeschichtungController), "FaHierarchyBeschichtung/Pdf/{hauptFa:int}")]
    public void PdfAction_HasRouteWithHauptFaSegment(Type controller, string template)
    {
        var action = controller.GetMethod("Pdf", BindingFlags.Public | BindingFlags.Instance);
        action.Should().NotBeNull();
        action!.GetCustomAttribute<HttpGetAttribute>()!.Template.Should().Be(template);
    }

    [Fact]
    public void Controllers_KeepClassLevelAccessAndFeatureFilters()
    {
        typeof(FaHierarchyKommissionierListenController).GetCustomAttribute<RequireLagerProcessingAccessAttribute>().Should().NotBeNull();
        typeof(FaHierarchyKommissionierListenController).GetCustomAttribute<RequireFaHierarchyKommissionierlistenAktivAttribute>().Should().NotBeNull();
        typeof(FaHierarchyBeschichtungController).GetCustomAttribute<RequireBeschichtungsauftragAccessAttribute>().Should().NotBeNull();
        typeof(FaHierarchyBeschichtungController).GetCustomAttribute<RequireFaHierarchyBeschichtungAktivAttribute>().Should().NotBeNull();
    }
}
