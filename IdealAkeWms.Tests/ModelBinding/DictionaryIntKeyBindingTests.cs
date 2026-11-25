using System.Globalization;
using FluentAssertions;
using IdealAkeWms.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace IdealAkeWms.Tests.ModelBinding;

/// <summary>
/// v1.99.69 (QA-Befund N-1): echte Formularbindung eines <c>Dictionary&lt;int, string&gt;</c>-Action-Parameters, wenn das
/// Formular KEIN Feld <c>name[..]</c> enthaelt (leerer Beschichtungsauftrag-Entwurf). Ohne festes Praefix faellt der Binder
/// auf das leere Praefix zurueck, liest die uebrigen Formularschluessel (<c>rowVersion</c>, <c>ziel</c>, …) als int-Schluessel
/// und setzt einen ModelState-Fehler unter dem Schluessel "" — daher „ungueltiger Wert in .“. Gleicher Pfad wie
/// <see cref="NullableIntArrayBindingTests"/> (ParameterBinder + FormValueProvider, ohne TestServer).
/// </summary>
public class DictionaryIntKeyBindingTests
{
    private static async Task<(object? Model, ModelStateDictionary State)> BindAsync(
        Type controller, string action, string parameter, Dictionary<string, StringValues> formular)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        await using var sp = services.BuildServiceProvider();

        var info = controller.GetMethod(action)!.GetParameters().Single(p => p.Name == parameter);
        var metadata = ((ModelMetadataProvider)sp.GetRequiredService<IModelMetadataProvider>()).GetMetadataForParameter(info);
        var descriptor = new ControllerParameterDescriptor
        {
            Name = info.Name!, ParameterType = info.ParameterType, ParameterInfo = info,
            BindingInfo = BindingInfo.GetBindingInfo(info.GetCustomAttributes(true), metadata)
        };
        var binder = sp.GetRequiredService<IModelBinderFactory>().CreateBinder(new ModelBinderFactoryContext
        {
            Metadata = metadata, BindingInfo = descriptor.BindingInfo, CacheToken = descriptor
        });
        var valueProvider = new FormValueProvider(BindingSource.Form, new FormCollection(formular), CultureInfo.InvariantCulture);
        var actionContext = new ActionContext(
            new DefaultHttpContext { RequestServices = sp }, new RouteData(), new ControllerActionDescriptor());

        var result = await sp.GetRequiredService<ParameterBinder>().BindModelAsync(
            actionContext, binder, valueProvider, descriptor, metadata, value: null, container: null);
        return (result.Model, actionContext.ModelState);
    }

    public static TheoryData<Type, string, string, Dictionary<string, StringValues>> FormulareOhneDictionaryFelder => new()
    {
        // Leerer Entwurf: Kandidat angehakt + Speichern bzw. „Auf Übergeben setzen“
        { typeof(BeschichtungsauftraegeController), nameof(BeschichtungsauftraegeController.Bearbeiten), "mengen",
            new() { ["id"] = "11", ["rowVersion"] = "AAAAAAAAB9E=", ["hakenGesendet"] = "true",
                    ["schluessel"] = "FA|50002326|1", ["ziel"] = "Uebergeben" } },
        // Speichern ohne Kandidat (nur Kopf)
        { typeof(BeschichtungsauftraegeController), nameof(BeschichtungsauftraegeController.Bearbeiten), "mengen",
            new() { ["id"] = "11", ["rowVersion"] = "AAAAAAAAB9E=", ["dienstleister"] = "X" } },
    };

    [Theory]
    [MemberData(nameof(FormulareOhneDictionaryFelder))]
    public async Task OhneDictionaryFelder_KeinModelStateFehler_LeeresOderKeinDictionary(
        Type controller, string action, string parameter, Dictionary<string, StringValues> formular)
    {
        var (model, state) = await BindAsync(controller, action, parameter, formular);

        state.Where(kv => kv.Value!.Errors.Count > 0).Select(kv => kv.Key).Should()
            .BeEmpty("ohne {0}[..]-Felder darf der Binder keine fremden Formularschluessel lesen", parameter);
        ((model as Dictionary<int, string>) ?? new()).Should().BeEmpty();
    }

    [Theory]
    [InlineData(typeof(BeschichtungsauftraegeController), nameof(BeschichtungsauftraegeController.Bearbeiten), "mengen")]
    public async Task MitDictionaryFeldern_BindetWeiter(Type controller, string action, string parameter)
    {
        var (model, state) = await BindAsync(controller, action, parameter,
            new() { [$"{parameter}[7]"] = "2,5", [$"{parameter}[9]"] = "3", ["rowVersion"] = "x" });

        state.IsValid.Should().BeTrue();
        model.Should().BeEquivalentTo(new Dictionary<int, string> { [7] = "2,5", [9] = "3" });
    }
}
