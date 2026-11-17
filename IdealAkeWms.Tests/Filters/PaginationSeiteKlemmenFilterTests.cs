using FluentAssertions;
using IdealAkeWms.Filters;
using IdealAkeWms.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace IdealAkeWms.Tests.Filters;

/// <summary>v1.99.39: Seite hinter der letzten („Eintraege 26–8 von 8“, leere Tabelle) -> Redirect auf die letzte Seite, Filter bleiben.</summary>
public class PaginationSeiteKlemmenFilterTests
{
    private sealed class Vm { public PaginationState Pagination { get; set; } = new(); }

    private static ResultExecutingContext Ctx(string query, object? model = null, PaginationState? viewBag = null, string method = "GET")
    {
        var http = new DefaultHttpContext();
        http.Request.Method = method;
        http.Request.Path = "/SageFaArbeitsgaenge";
        http.Request.QueryString = new QueryString(query);
        var vd = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model };
        if (viewBag is not null) vd["Pagination"] = viewBag;
        return new ResultExecutingContext(new ActionContext(http, new RouteData(), new ActionDescriptor()), new List<IFilterMetadata>(),
            new ViewResult { ViewData = vd }, new object());
    }

    private static PaginationState P(int page, int total) => new() { CurrentPage = page, PageSize = 25, TotalCount = total };

    [Fact]
    public void SeiteHinterDemEnde_RedirectAufLetzteSeite_FilterBleiben()
    {
        var ctx = Ctx("?page=2&search=abc&cf_Status=Fehler", new Vm { Pagination = P(2, 8) });
        new PaginationSeiteKlemmenFilter().OnResultExecuting(ctx);
        ctx.Result.Should().BeOfType<RedirectResult>().Which.Url.Should().Be("/SageFaArbeitsgaenge?page=1&search=abc&cf_Status=Fehler");
    }

    [Fact]
    public void ViewBag_Und_NullTreffer_AufSeite1()
    {
        var ctx = Ctx("?page=5", viewBag: P(5, 0));
        new PaginationSeiteKlemmenFilter().OnResultExecuting(ctx);
        ctx.Result.Should().BeOfType<RedirectResult>().Which.Url.Should().Be("/SageFaArbeitsgaenge?page=1");

        var ctx2 = Ctx("?page=9", viewBag: P(9, 60));          // 3 Seiten
        new PaginationSeiteKlemmenFilter().OnResultExecuting(ctx2);
        ((RedirectResult)ctx2.Result).Url.Should().Be("/SageFaArbeitsgaenge?page=3");
    }

    [Theory]
    [InlineData("?page=1", 1, 8, "GET")]
    [InlineData("?page=3", 3, 60, "GET")]      // letzte Seite gueltig
    [InlineData("?page=2", 2, 8, "POST")]      // nur GET wird umgeleitet
    public void GueltigeSeite_OderPost_KeinRedirect(string query, int page, int total, string method)
    {
        var ctx = Ctx(query, new Vm { Pagination = P(page, total) }, method: method);
        new PaginationSeiteKlemmenFilter().OnResultExecuting(ctx);
        ctx.Result.Should().BeOfType<ViewResult>();
    }
}
