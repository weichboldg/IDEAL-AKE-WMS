using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class StockMovement : AuditableEntity
{
    [Required(ErrorMessage = "Artikel ist erforderlich")]
    [Display(Name = "Artikel")]
    public int ArticleId { get; set; }

    [Required(ErrorMessage = "Menge ist erforderlich")]
    [Range(0.001, double.MaxValue, ErrorMessage = "Menge muss größer als 0 sein")]
    [Display(Name = "Menge")]
    public decimal Quantity { get; set; }

    [Required(ErrorMessage = "Lagerplatz ist erforderlich")]
    [Display(Name = "Lagerplatz")]
    public int StorageLocationId { get; set; }

    [StringLength(500)]
    [Display(Name = "Fertigungsauftrag")]
    public string? ProductionOrder { get; set; }

    /// <summary>v1.87.0 (Spec 2026-11-08-lagerbuchungen-fa-feld-und-verlinkung): verknuepfter FA, vom Server aus dem Text
    /// <see cref="ProductionOrder"/> aufgeloest (<c>IProductionOrderRepository.FindForBookingAsync</c>); null = leer, Liste, mehrdeutig
    /// oder unbekannt. Der Text bleibt unveraendert (Eingabe, Altbestand, Listen, Sage-Entscheidung <c>hatFaBezug</c>).</summary>
    [Display(Name = "Fertigungsauftrag (verknüpft)")]
    public int? ProductionOrderId { get; set; }

    [Display(Name = "Bewegungsart")]
    public MovementType MovementType { get; set; }

    [Display(Name = "Zeitpunkt")]
    public DateTime Timestamp { get; set; }

    [Display(Name = "Benutzer")]
    public int? UserId { get; set; }

    [StringLength(200)]
    [Display(Name = "Windows-Benutzer")]
    public string WindowsUser { get; set; } = string.Empty;

    [Display(Name = "Quell-Lagerplatz")]
    public int? SourceStorageLocationId { get; set; }

    [StringLength(500)]
    [Display(Name = "Notiz")]
    public string? Note { get; set; }

    /// <summary>v1.99.68 (Spec 2026-11-24-sage-buchung-referenz-benutzer-abteilung F4): Werkbank-Name (<c>ProductionWorkplace.Name</c>) als
    /// Schnappschuss, nur bei Ausbuchung/Lagerplatz ausbuchen waehlbar; null = keine. Geht nur in die Sage-Referenz (Lagerbuchung).</summary>
    [StringLength(200)]
    [Display(Name = "Abteilung")]
    public string? Abteilung { get; set; }

    public Article Article { get; set; } = null!;
    public StorageLocation StorageLocation { get; set; } = null!;
    public StorageLocation? SourceStorageLocation { get; set; }
    public User? User { get; set; }

    /// <summary>Navigation zu <see cref="ProductionOrderId"/> (Name <c>ProductionOrder</c> ist durch den Text belegt).</summary>
    public ProductionOrder? LinkedProductionOrder { get; set; }

    /// <summary>v1.84.0: Wareneingang zu einer Sage-Bestellposition (nur bei Einbuchung ueber „Wareneingang zu Bestellung“), sonst null.</summary>
    public PurchaseOrderReceipt? PurchaseOrderReceipt { get; set; }

    /// <summary>v1.99.53 (Spec 2026-11-23-lagerbestellung-verbrauchsbuchung-bei-lieferung): Lieferung einer Lager-/Glasbestellungsposition
    /// (Ausbuchung beim Abschliessen), sonst null. Summe je Position = schon ausgebucht (Schutz gegen Doppelbuchung).</summary>
    [Display(Name = "Lagerbestellposition")]
    public int? WarehouseRequisitionItemId { get; set; }

    public WarehouseRequisitionItem? WarehouseRequisitionItem { get; set; }
}
