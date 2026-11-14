using FluentAssertions;
using IdealAkeWms.Models.ViewModels;
using Xunit;

namespace IdealAkeWms.Tests.Models;

/// <summary>v1.99.23 (N-1, AK-13): Vater-Position nur, wenn sie in der Liste steht (Kommissionierauftrag ohne Vater-Zeilen).</summary>
public class BomViewModelVaterPositionTests
{
    private static (BomViewModel Vm, Dictionary<string, BomItemViewModel> Zeilen) Liste(params string[] positionen)
    {
        var zeilen = positionen.ToDictionary(p => p, p => new BomItemViewModel { Position = p });
        return (new BomViewModel { Items = zeilen.Values.ToList() }, zeilen);
    }

    [Fact]
    public void OhneVaterZeile_Null_ZeileSichtbar()
    {
        var (vm, z) = Liste("3.1");
        vm.VaterPosition(z["3.1"]).Should().BeNull();
    }

    [Fact]
    public void DirekterVaterFehlt_Grossvater()
    {
        var (vm, z) = Liste("3", "3.1.2");
        vm.VaterPosition(z["3.1.2"]).Should().Be("3");
    }

    [Fact]
    public void VolleStueckliste_DirekterVater_WieBisher()
    {
        var (vm, z) = Liste("3", "3.1", "3.1.2", "4");
        vm.VaterPosition(z["3.1"]).Should().Be("3");
        vm.VaterPosition(z["3.1.2"]).Should().Be("3.1");
        vm.VaterPosition(z["3"]).Should().BeNull();
        vm.VaterPosition(z["4"]).Should().BeNull();
        vm.VaterPosition(new BomItemViewModel { Position = null }).Should().BeNull();
    }
}
