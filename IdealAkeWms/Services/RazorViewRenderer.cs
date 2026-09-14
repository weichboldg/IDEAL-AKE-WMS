using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace IdealAkeWms.Services;

/// <summary>Rendert eine Razor-View mit Model zu einem HTML-String ausserhalb der Response — damit
/// dieselbe <c>Print.cshtml</c> an den Browser (Print-Action) UND an den PdfRenderService (Pdf-Action)
/// geht. Kein zweites Layout.</summary>
public interface IRazorViewRenderer
{
    Task<string> RenderToStringAsync(ControllerContext controllerContext, string viewName, object model);
}

public sealed class RazorViewRenderer : IRazorViewRenderer
{
    private readonly ICompositeViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;

    public RazorViewRenderer(ICompositeViewEngine viewEngine, ITempDataProvider tempDataProvider)
    {
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
    }

    public async Task<string> RenderToStringAsync(ControllerContext controllerContext, string viewName, object model)
    {
        var viewResult = _viewEngine.FindView(controllerContext, viewName, isMainPage: true);
        if (!viewResult.Success)
            throw new InvalidOperationException($"View '{viewName}' nicht gefunden. Gesucht in: {string.Join(", ", viewResult.SearchedLocations)}");

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model };
        var tempData = new TempDataDictionary(controllerContext.HttpContext, _tempDataProvider);
        await using var writer = new StringWriter();
        var viewContext = new ViewContext(controllerContext, viewResult.View, viewData, tempData, writer, new HtmlHelperOptions());
        await viewResult.View.RenderAsync(viewContext);
        return writer.ToString();
    }
}
