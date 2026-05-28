using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Services.Oseon;

/// <summary>
/// Baut die OseonOrderGroupViewModel-Aggregate (inkl. SubOrders + Operations + Ampelfarben + Termine)
/// aus einer Liste von OseonProductionOrder-Entitaeten auf. Wird sowohl von TrackingController.OseonIndex
/// (im Prefetch-Pfad bei aktivem Artikel-Filter) als auch von TrackingController.OseonGroupDetails
/// (Lazy-Load eines einzelnen Customer-Order-Schluessels) verwendet.
/// </summary>
public interface IOseonGroupViewModelBuilder
{
    Task<OseonOrderGroupViewModel> BuildAsync(
        string customerOrderKey,
        IEnumerable<OseonProductionOrder> subOrders,
        bool useRelevanceFilter,
        string? filterArticle,
        CancellationToken ct = default);
}
