using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.92.0 (Spec 2026-11-08-oseon-relevanzfilter-schaltet-sich-ab): Checkbox + gleichnamiges Hidden-„false“ – der bool-Binder nimmt den
/// ERSTEN Wert, das Hidden-Feld muss also HINTER der Checkbox stehen (AK 1, 5, 6, 7, 10).</summary>
public class CheckboxHiddenFeldV192Tests
{
    [Theory]
    [InlineData(typeof(bool), new[] { "true", "false" }, true)]
    [InlineData(typeof(bool), new[] { "false", "true" }, false)]
    [InlineData(typeof(bool?), new[] { "true", "false" }, true)]
    [InlineData(typeof(bool?), new[] { "false", "true" }, false)]
    public async Task Binder_NimmtDenErstenWert_AK6(Type typ, string[] werte, bool erwartet)
    {
        var query = new QueryCollection(new Dictionary<string, StringValues> { ["flag"] = new StringValues(werte) });
        var provider = new QueryStringValueProvider(BindingSource.Query, query, System.Globalization.CultureInfo.InvariantCulture);
        var metadata = new EmptyModelMetadataProvider().GetMetadataForType(typ);
        var ctx = DefaultModelBindingContext.CreateBindingContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()), provider, metadata, new BindingInfo(), "flag");
        await new SimpleTypeModelBinder(typ, NullLoggerFactory.Instance).BindModelAsync(ctx);
        ctx.Result.IsModelSet.Should().BeTrue();
        ctx.Result.Model.Should().Be(erwartet);
    }

    private static string ViewText(params string[] teile)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Views"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine(new[] { dir!.FullName, "IdealAkeWms", "Views" }.Concat(teile).ToArray()));
    }

    /// <summary>Das Hidden-„false“ steht HINTER der Checkbox gleichen Namens: zwischen der letzten Checkbox davor und dem Hidden-Feld steht der Name.</summary>
    private static void HiddenHinterCheckbox(string text, string name)
    {
        var hidden = text.IndexOf($"<input type=\"hidden\" name=\"{name}\" value=\"false\" />", StringComparison.Ordinal);
        hidden.Should().BeGreaterThan(0, $"Hidden-Feld {name} fehlt");
        var checkbox = text.LastIndexOf("type=\"checkbox\"", hidden, StringComparison.Ordinal);
        checkbox.Should().BeGreaterThan(0, $"keine Checkbox vor dem Hidden-Feld {name}");
        text.Substring(checkbox, hidden - checkbox).Should().Contain($"name=\"{name}\"",
            $"das versteckte false-Feld {name} steht HINTER seiner Checkbox (Binder nimmt den ersten Wert)");
    }

    [Fact]
    public void Views_HiddenFalseHinterCheckbox_AK1_AK5_AK7()
    {
        HiddenHinterCheckbox(ViewText("Tracking", "OseonIndex.cshtml"), "useRelevanceFilter");
        var bde = ViewText("BdeBookings", "Edit.cshtml");
        HiddenHinterCheckbox(bde, "Quantities[@i].IsFinal");
        HiddenHinterCheckbox(bde, "Quantities[@i].Delete");
        bde.Should().Contain("asp-validation-summary=\"ModelOnly\"");
    }

    [Fact]
    public async Task BdeEdit_ZweiFinalZeilen_Formularfehler_Kein500_AK10()
    {
        using var db = TestDbContextFactory.Create();
        var ctrl = BdeBookingsControllerTests.CreateForTests(db);
        var vm = new BdeBookingEditViewModel
        {
            Id = 1,
            Quantities =
            {
                new BdeBookingQuantityEditRow { Id = 1, IsFinal = true },
                new BdeBookingQuantityEditRow { Id = 2, IsFinal = true },
                new BdeBookingQuantityEditRow { Id = 3, IsFinal = true, Delete = true },
            },
        };
        var r = await ctrl.Edit(1, vm);
        r.Should().BeOfType<ViewResult>();
        ctrl.ModelState[""]!.Errors.Should().ContainSingle(e => e.ErrorMessage == "Nur eine Mengenzeile je Buchung darf „Final“ sein.");

        // eine Final-Zeile (die zweite wird geloescht) ist erlaubt → kein Formularfehler (Buchung 1 fehlt → NotFound)
        vm.Quantities[1].Delete = true;
        var ok = BdeBookingsControllerTests.CreateForTests(db);
        (await ok.Edit(1, vm)).Should().BeOfType<NotFoundResult>();
    }
}
