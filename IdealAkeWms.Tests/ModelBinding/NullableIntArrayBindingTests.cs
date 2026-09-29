using System.Globalization;
using FluentAssertions;
using IdealAkeWms.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace IdealAkeWms.Tests.ModelBinding;

/// <summary>
/// Hotfix v1.30.1 (Spec 2026-09-28, Abschnitt F): belegt die ECHTE ASP.NET-Formularbindung der parallelen
/// Arrays <c>itemIds</c>/<c>quantitiesPicked</c> — nicht per Annahme. Laeuft ueber denselben Pfad wie ein
/// Formular-POST (<see cref="ParameterBinder"/> + <see cref="FormValueProvider"/> + registrierte Binder aus
/// <c>AddControllers()</c>), gegen die Parameter-Metadaten der realen Controller-Actions.
/// Bewusst ohne TestServer: <c>Microsoft.AspNetCore.TestHost</c> ist NICHT Teil von
/// <c>Microsoft.AspNetCore.App</c> und waere ein neues NuGet-Paket.
/// </summary>
public class NullableIntArrayBindingTests
{
    private static async Task<object?> BindAsync(string action, string parameter, params string[] values)
        => (await BindWithStateAsync(action, parameter, values)).Model;

    private static async Task<(object? Model, ModelStateDictionary State)> BindWithStateAsync(
        string action, string parameter, params string[] values)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        await using var sp = services.BuildServiceProvider();

        var method = typeof(WarehousePickingController).GetMethod(action)!;
        var info = method.GetParameters().Single(p => p.Name == parameter);
        var metadataProvider = sp.GetRequiredService<IModelMetadataProvider>();
        var metadata = ((ModelMetadataProvider)metadataProvider).GetMetadataForParameter(info);
        var descriptor = new ControllerParameterDescriptor
        {
            Name = info.Name!, ParameterType = info.ParameterType, ParameterInfo = info,
            BindingInfo = BindingInfo.GetBindingInfo(info.GetCustomAttributes(true), metadata)
        };

        var binder = sp.GetRequiredService<IModelBinderFactory>().CreateBinder(new ModelBinderFactoryContext
        {
            Metadata = metadata, BindingInfo = descriptor.BindingInfo, CacheToken = descriptor
        });

        var form = new FormCollection(new Dictionary<string, StringValues> { [parameter] = new(values) });
        var valueProvider = new FormValueProvider(BindingSource.Form, form, CultureInfo.InvariantCulture);
        var actionContext = new ActionContext(
            new DefaultHttpContext { RequestServices = sp }, new RouteData(), new ControllerActionDescriptor());

        var result = await sp.GetRequiredService<ParameterBinder>().BindModelAsync(
            actionContext, binder, valueProvider, descriptor, metadata, value: null, container: null);
        return (result.Model, actionContext.ModelState);
    }

    [Theory]
    [InlineData(nameof(WarehousePickingController.SaveProgress))]
    [InlineData(nameof(WarehousePickingController.Close))]
    [InlineData(nameof(WarehousePickingController.PrintAndClose))]
    public async Task QuantitiesPicked_EmptyString_BindsNullAtSameIndex(string action)
    {
        var bound = await BindAsync(action, "quantitiesPicked", "5", "", "7");

        bound.Should().BeOfType<int?[]>()
            .Which.Should().Equal(5, null, 7);
    }

    /// <summary>
    /// Gegenprobe (Code-Review-Befund 1): Ein NICHT ganzzahliger Wert wird vom Binder VERWORFEN, nicht als
    /// null gebunden — das Array wird kuerzer und alle folgenden Werte rutschen eine Zeile nach oben.
    /// Gemessen: ["5","1.5","7"] -> [5, 7] + ModelState-Fehler. Deshalb weisen Close/PrintAndClose ab, wenn
    /// ModelState ungueltig ist oder die Laengen von itemIds/quantitiesPicked nicht passen.
    /// </summary>
    [Fact]
    public async Task QuantitiesPicked_NonInteger_IsDroppedNotNull_AndInvalidatesModelState()
    {
        var (model, state) = await BindWithStateAsync("Close", "quantitiesPicked", "5", "1.5", "7");

        ((int?[])model!).Should().Equal(5, 7);
        state.IsValid.Should().BeFalse();
    }
}
