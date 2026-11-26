using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class WarehouseRequisitionEditViewModel
{
    public int Id { get; set; }
    public string WorkplaceName { get; set; } = string.Empty;
    public WarehouseRequisitionStatus Status { get; set; }
    public WarehouseRequisitionType Type { get; set; } = WarehouseRequisitionType.Lager;
    public DateTime CreatedAt { get; set; }
    public string? Comment { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public List<WarehouseRequisitionEditItemViewModel> Items { get; set; } = new();

    /// <summary>v1.99.72 (F13, Antwort 24): Artikelnummer (getrimmt) → Fraesen-Pfad; null = Schalter FraesenPfadAktiv aus (keine Spalte).</summary>
    public Dictionary<string, string>? FraesenPfade { get; set; }

    /// <summary>v1.99.86 (Spec 2026-11-26-lagerbestellung-hinweis-fehlteil-bereits-bestellt): Artikelnummer (getrimmt, Gross/Klein egal)
    /// → Hinweis „bereits als Fehlteil bestellt“. null/leer = kein Hinweis; nur im Entwurf gesetzt.</summary>
    public Dictionary<string, string>? FehlteilHinweise { get; set; }
}
