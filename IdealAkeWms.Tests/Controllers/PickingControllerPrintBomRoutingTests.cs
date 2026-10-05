using System.Reflection;
using FluentAssertions;
using IdealAkeWms.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>
/// Hotfix v1.30.1 (HTTP 404.15): PrintBom gibt es als GET (Lesezeichen, druckt alles) und als POST
/// (Druck-Button, Positionsliste im Formular-Body). Beide heissen "PrintBom" — ohne [HttpGet] am GET waere
/// ein POST zur Laufzeit mehrdeutig (AmbiguousMatchException), und ohne Antiforgery-Pruefung waere der POST
/// ungeschuetzt. Beides laesst sich nur per Attribut absichern.
/// </summary>
public class PickingControllerPrintBomRoutingTests
{
    private static MethodInfo Method(string name) =>
        typeof(PickingController).GetMethod(name, BindingFlags.Public | BindingFlags.Instance)!;

    [Fact]
    public void Get_IsGetOnly_AndHasNoPositionList()
    {
        var get = Method(nameof(PickingController.PrintBom));
        get.GetCustomAttribute<HttpGetAttribute>().Should().NotBeNull();
        get.GetParameters().Select(p => p.Name).Should().NotContain("visiblePositions");
    }

    [Fact]
    public void Post_IsPostWithAntiforgery_UnderActionNamePrintBom()
    {
        var post = Method(nameof(PickingController.PrintBomPost));
        post.GetCustomAttribute<HttpPostAttribute>().Should().NotBeNull();
        post.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>().Should().NotBeNull();
        post.GetCustomAttribute<ActionNameAttribute>()!.Name.Should().Be("PrintBom");
        post.GetParameters().Single(p => p.Name == "visiblePositions")
            .GetCustomAttribute<FromFormAttribute>().Should().NotBeNull();
    }
}
