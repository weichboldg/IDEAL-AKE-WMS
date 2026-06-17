using IdealAkeWms.Data.Repositories;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// Einheitliche Dokument-Buttons fuer die FA-Vorbau-Views (FA-Vervollstaendigung +
/// FA-Abarbeitung). Rendert ueber das shared Partial <c>_FaDocumentLinks.cshtml</c>
/// in fester Reihenfolge: (1) read-only BOM-Button, (2) enaio-Badges, (3) Vault-Link.
/// </summary>
public class FaDocumentLinksViewModel
{
    /// <summary>Artikelnummer fuer den Vault-Link. NULL/leer = kein Vault-Link.</summary>
    public string? ArticleNumber { get; set; }

    /// <summary>FA-Nummer (Anzeige/Identifikation).</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>FA-Id fuer den BOM-Button (<c>asp-route-id</c>).</summary>
    public int ProductionOrderId { get; set; }

    /// <summary>Ziel-Controller des read-only BOM-Buttons ("FaCompletion" bzw. "FaWorklist").</summary>
    public string BomController { get; set; } = string.Empty;

    /// <summary>enaio-DMS-Links der FA (bereits sortiert). NULL/leer = keine enaio-Badges.</summary>
    public List<EnaioDmsDocumentLink>? EnaioLinks { get; set; }

    /// <summary>
    /// True -> enaio-Badges mit Text-Label (Kopf-Bereich der Edit-View);
    /// false -> reine Icon-Links (Listen-Zellen).
    /// </summary>
    public bool ShowEnaioLabel { get; set; }
}
