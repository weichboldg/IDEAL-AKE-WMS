using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// Mehrfachartikel-Einbuchung: ein gemeinsamer Kopf (Lagerplatz + FA), darunter beliebig viele
/// Artikel-Zeilen. Jede gültige Zeile wird zu einer eigenen <see cref="StockMovement"/> vom Typ
/// <see cref="MovementType.Einbuchung"/> gebucht. Siehe
/// [[2026-08-05-wms-bugs-improvements-teil-2-spec]].
/// </summary>
public class StockMovementBulkInboundViewModel
{
    [Required(ErrorMessage = "Lagerplatz ist erforderlich")]
    [Display(Name = "Lagerplatz")]
    public int StorageLocationId { get; set; }

    [StringLength(100)]
    [Display(Name = "Fertigungsauftrag")]
    public string? ProductionOrder { get; set; }

    /// <summary>Artikel-Zeilen. Validierung erfolgt zeilenweise im Controller (nicht per
    /// DataAnnotation), damit die Fehler-Markierung unabhängig von den dynamischen
    /// Model-Binding-Schlüsseln (<c>Lines.Index</c>) funktioniert.</summary>
    public List<StockMovementBulkInboundLine> Lines { get; set; } = new();

    public List<StorageLocation> StorageLocations { get; set; } = new();
}

public class StockMovementBulkInboundLine
{
    public int ArticleId { get; set; }

    public decimal Quantity { get; set; } = 1m;

    /// <summary>Anzeigetext für das Select2-Feld beim Re-Render nach Validierungsfehler.</summary>
    [BindNever]
    public string? ArticleDisplay { get; set; }

    /// <summary>Zeilen-Fehlermeldung; gesetzt im Controller, markiert die fehlerhafte Zeile beim
    /// Re-Render. Nie aus dem Request gebunden.</summary>
    [BindNever]
    public string? Error { get; set; }
}
