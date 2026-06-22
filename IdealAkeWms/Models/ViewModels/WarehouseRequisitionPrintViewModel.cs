using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>Eine sichtbare Druck-Spalte in finaler Reihenfolge.</summary>
public record PrintColumn(string Key, string Label);

/// <summary>
/// Druck-ViewModel fuer WarehousePicking/Print. Items sind bereits gefiltert + sortiert,
/// Columns sind die sichtbaren Spalten in finaler Reihenfolge (aus User-Preferences).
/// </summary>
public class WarehouseRequisitionPrintViewModel
{
    public int Id { get; set; }
    public string WorkplaceName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public WarehouseRequisitionStatus Status { get; set; }
    public List<PrintColumn> Columns { get; set; } = new();
    public List<WarehouseRequisitionDetailItemViewModel> Items { get; set; } = new();
}
