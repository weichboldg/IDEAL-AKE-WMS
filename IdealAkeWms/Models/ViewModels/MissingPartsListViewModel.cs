using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class MissingPartsListViewModel
{
    public List<MissingPartRow> Items { get; set; } = new();
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();
    public int? WorkplaceFilter { get; set; }
    public bool MineOnly { get; set; }

    /// <summary>v1.81.0: „Nur meine Werkbaenke“ mit fremder <c>workplaceId</c> in der URL -> sichtbarer Hinweis (Liste bleibt leer).</summary>
    public bool ForeignWorkplaceSelected { get; set; }
    public PaginationState Pagination { get; set; } = new();

    /// <summary>Welcher Tab ist gerade aktiv.</summary>
    public ShortageStatus ActiveTab { get; set; } = ShortageStatus.WillBeRestocked;

    /// <summary>Total-Count des WillBeRestocked-Tab (fuer Tab-Header-Badge).</summary>
    public int WaitingTotalCount { get; set; }

    /// <summary>Total-Count des NoRestock-Tab (fuer Tab-Header-Badge).</summary>
    public int NoRestockTotalCount { get; set; }

    /// <summary>
    /// True wenn mineOnly aktiv UND User hat keine Workplace-Zuordnung.
    /// Triggert Banner-Hinweis in der View.
    /// </summary>
    public bool HasNoWorkplaceMapping { get; set; }

    /// <summary>Aktiver Bestelltyp-Reiter (aeussere Ebene, v1.25.0).</summary>
    public WarehouseRequisitionType ActiveType { get; set; } = WarehouseRequisitionType.Lager;
    public bool CanOrderLager { get; set; } = true;
    public bool CanOrderGlas { get; set; } = true;
}
