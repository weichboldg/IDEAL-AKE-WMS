using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public record WarehouseRequisitionDetailItemViewModel(
    int Id,
    int Position,
    string ArticleNumber,
    string ArticleDescription,
    string? Unit,
    decimal QuantityRequested,
    decimal? QuantityPicked,
    string StorageLocations,
    string? Note = null,
    ShortageStatus ShortageStatus = ShortageStatus.None,
    string? NoteEinkauf = null,
    // v1.99.53 Verbrauchsbuchung (nur bei Schalter LagerbestellungVerbrauchsbuchungAktiv gefuellt):
    bool IstLagerartikel = true,
    int? EntnahmeVorschlagId = null,
    string? EntnahmeHinweis = null,
    decimal BereitsAusgebucht = 0);

public class WarehouseRequisitionDetailViewModel
{
    public int Id { get; set; }
    public string WorkplaceName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? Comment { get; set; }
    public WarehouseRequisitionStatus Status { get; set; }
    public WarehouseRequisitionType Type { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public List<WarehouseRequisitionDetailItemViewModel> Items { get; set; } = new();

    /// <summary>v1.99.53: Schalter <c>LagerbestellungVerbrauchsbuchungAktiv</c>; aus = Seite wie bisher.</summary>
    public bool VerbrauchsbuchungAktiv { get; set; }

    /// <summary>v1.99.53: waehlbare Entnahme-Plaetze (buchbar, ohne Sage-Plaetze ohne Sage-Uebergabe); nur im Bearbeitungsmodus gefuellt.</summary>
    public List<StorageLocation> EntnahmePlaetze { get; set; } = new();
}
