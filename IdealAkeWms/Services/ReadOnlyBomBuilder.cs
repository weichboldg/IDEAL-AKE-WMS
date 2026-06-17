using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Services;

/// <summary>
/// Baut das <see cref="BomViewModel"/> fuer die <b>read-only Stueckliste</b> (Spec §7,
/// FA-Abarbeitungsliste/FA-Vervollstaendigung): reine BOM-Anzeige mit
/// <c>ReadOnly=true</c> — KEINE Picking-Initialisierung, keine Lagerplatz-Suggests
/// /Dropdowns, kein Umbuchen, keine Fotos, keine Bedarfsmeldungen.
/// Vorher in <c>FaWorklistController.Bom</c> dupliziert; jetzt gemeinsam genutzt von
/// FaWorklist + FaCompletion (DRY, Plan Task 1).
/// </summary>
public class ReadOnlyBomBuilder
{
    private readonly IProductionOrderRepository _productionOrderRepository;
    private readonly IBomRepository _bomRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IArticleAttributeRepository _articleAttributeRepository;
    private readonly IUserRepository _userRepository;

    public ReadOnlyBomBuilder(
        IProductionOrderRepository productionOrderRepository,
        IBomRepository bomRepository,
        IStockMovementRepository stockMovementRepository,
        IArticleAttributeRepository articleAttributeRepository,
        IUserRepository userRepository)
    {
        _productionOrderRepository = productionOrderRepository;
        _bomRepository = bomRepository;
        _stockMovementRepository = stockMovementRepository;
        _articleAttributeRepository = articleAttributeRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Laedt FA + BOM und erzeugt das read-only ViewModel.
    /// Gibt <c>null</c> zurueck, wenn der FA fehlt oder keine Artikelnummer hat
    /// (Aufrufer entscheidet ueber NotFound/Warnung+Redirect).
    /// </summary>
    public async Task<BomViewModel?> BuildAsync(int productionOrderId, string? filterText, int? currentAppUserId)
    {
        var order = await _productionOrderRepository.GetByIdAsync(productionOrderId);
        if (order == null || string.IsNullOrEmpty(order.ArticleNumber))
        {
            return null;
        }

        // User-Defaults fuer client-seitige Spaltenfilter (wie PickingController.Bom).
        string? defaultFilterBeschaffung = null;
        string? defaultFilterArtikelgruppe = null;
        var recursiveFilterSearch = false;
        if (currentAppUserId.HasValue)
        {
            var currentUser = await _userRepository.GetByIdAsync(currentAppUserId.Value);
            if (currentUser != null)
            {
                defaultFilterBeschaffung = currentUser.DefaultFilterBeschaffung;
                defaultFilterArtikelgruppe = currentUser.DefaultFilterArtikelgruppe;
                recursiveFilterSearch = currentUser.RecursiveFilterSearch;
            }
        }

        var bomResult = await _bomRepository.GetBomItemsAsync(order.ArticleNumber);
        var bomItems = bomResult.Items;

        var articleNumbers = bomItems
            .Select(b => b.Ressourcenummer)
            .Where(r => !string.IsNullOrEmpty(r))
            .Select(r => r!)
            .Distinct()
            .ToList();
        var stockByArticle = await _stockMovementRepository.GetStockByArticleNumbersAsync(articleNumbers);
        var categoryByArticle = await _articleAttributeRepository.GetCategoryNamesByArticleNumbersAsync(articleNumbers);

        // Baugruppen-Hierarchie: sammle alle Baugruppen-Werte (fuer Baum-Navigation).
        var baugruppen = bomItems
            .Where(b => !string.IsNullOrEmpty(b.Baugruppe))
            .Select(b => b.Baugruppe!)
            .Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var viewItems = bomItems.Select(bom =>
        {
            stockByArticle.TryGetValue(bom.Ressourcenummer ?? "", out var stockLocations);

            // TreeLevel aus Position ableiten: Anzahl Punkte = Ebene ("15.1" = 1).
            var treeLevel = string.IsNullOrEmpty(bom.Position) ? 0 : bom.Position.Count(c => c == '.');

            return new BomItemViewModel
            {
                Artikelnummer = bom.Artikelnummer,
                Position = bom.Position,
                Baugruppe = bom.Baugruppe,
                Ressourcenummer = bom.Ressourcenummer,
                Bezeichnung1 = bom.Bezeichnung1,
                Bezeichnung2 = bom.Bezeichnung2,
                Menge = bom.Menge * order.Quantity,
                Beschaffungsartikel = bom.Beschaffungsartikel,
                Artikelgruppe = bom.Artikelgruppe,
                KategorieName = categoryByArticle.TryGetValue(bom.Ressourcenummer ?? "", out var catName) ? catName : null,
                StockLocations = stockLocations ?? new List<StockLocationInfo>(),
                TreeLevel = treeLevel,
                IsBaugruppe = !string.IsNullOrEmpty(bom.Ressourcenummer) && baugruppen.Contains(bom.Ressourcenummer)
            };
        })
        .OrderBy(v => v.Position, new NaturalPositionComparer())
        .ToList();

        if (!string.IsNullOrWhiteSpace(filterText))
        {
            viewItems = viewItems.Where(i =>
                (i.Ressourcenummer != null && i.Ressourcenummer.Contains(filterText, StringComparison.OrdinalIgnoreCase)) ||
                (i.Bezeichnung1 != null && i.Bezeichnung1.Contains(filterText, StringComparison.OrdinalIgnoreCase)) ||
                (i.Bezeichnung2 != null && i.Bezeichnung2.Contains(filterText, StringComparison.OrdinalIgnoreCase)) ||
                (i.Baugruppe != null && i.Baugruppe.Contains(filterText, StringComparison.OrdinalIgnoreCase)) ||
                (i.Position != null && i.Position.Contains(filterText, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        return new BomViewModel
        {
            ProductionOrderId = productionOrderId,
            OrderNumber = order.OrderNumber,
            ArticleNumber = order.ArticleNumber,
            Description1 = order.Description1,
            Items = viewItems,
            FilterText = filterText,
            DefaultFilterBeschaffung = defaultFilterBeschaffung,
            DefaultFilterArtikelgruppe = defaultFilterArtikelgruppe,
            DataSource = bomResult.DataSource,
            RecursiveFilterSearch = recursiveFilterSearch,
            ReadOnly = true
        };
    }
}
