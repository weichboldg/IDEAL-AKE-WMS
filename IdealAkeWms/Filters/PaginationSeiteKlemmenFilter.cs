using IdealAkeWms.Models.ViewModels;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;

namespace IdealAkeWms.Filters;

/// <summary>
/// v1.99.39: Liegt die angeforderte Seite (<c>?page=</c>) hinter der letzten, leitet ein GET auf die letzte Seite um (0 Treffer: Seite 1),
/// alle anderen Query-Parameter bleiben. Grund: Die Controller rechnen Skip/Take aus <c>page</c>, bevor die Trefferzahl bekannt ist —
/// ohne Umleitung zeigte das <c>_Pagination</c>-Partial „Eintraege 26–8 von 8“ ueber einer leeren Tabelle. Global registriert, damit
/// keiner der Listen-Controller angefasst werden muss; erkennt den <see cref="PaginationState"/> im Model (Property) und im ViewBag.
/// </summary>
public sealed class PaginationSeiteKlemmenFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        var request = context.HttpContext.Request;
        if (!HttpMethods.IsGet(request.Method)) return;
        var viewData = context.Result switch
        {
            ViewResult v => v.ViewData,
            PartialViewResult p => p.ViewData,
            _ => null,
        };
        if (viewData is null) return;

        var states = viewData.Values.OfType<PaginationState>()
            .Concat(viewData.Model?.GetType().GetProperties()
                .Where(p => p.PropertyType == typeof(PaginationState) && p.GetIndexParameters().Length == 0)
                .Select(p => p.GetValue(viewData.Model) as PaginationState) ?? [])
            .OfType<PaginationState>();
        var zuWeit = states.FirstOrDefault(s => s.CurrentPage > Math.Max(1, s.TotalPages));
        if (zuWeit is null) return;

        var query = QueryHelpers.ParseQuery(request.QueryString.Value);
        query["page"] = Math.Max(1, zuWeit.TotalPages).ToString();
        var qb = new QueryBuilder();
        foreach (var (key, values) in query)
            foreach (var v in values) qb.Add(key, v ?? "");
        context.Result = new RedirectResult(request.PathBase + request.Path + qb.ToQueryString());
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
