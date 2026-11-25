using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

/// <summary>
/// v1.99.32 (Spec 2026-11-13-bde-vereinfacht-teileliste-blechfertigung, ADR 0030): Fertigmeldung EINER Strukturzeile der BDE-Teileliste
/// (Blechteil oder Baugruppe) fuer ein Kuerzel. Gehalten ueber den fachlichen Schluessel <c>(HauptFA, VaterFA, SubFA, Position, Artnr)</c>
/// + <see cref="Kuerzel"/>, NIE ueber <c>FaHierarchyNode.Id</c> (der Struktur-Sync loescht und legt die Knoten neu an).
/// Gefilterter UNIQUE <c>UX_BdeTeilMeldungen_Schluessel ... WHERE IsCancelled = 0</c> = Doppel-Schutz (ADR 0026).
/// Keine Meldung an Vater-Sub-FA oder Haupt-FA, nichts nach Sage.
/// </summary>
public class BdeTeilMeldung : AuditableEntity
{
    public int HauptFA { get; set; }
    public int? VaterFA { get; set; }
    public int SubFA { get; set; }
    public int? Position { get; set; }

    [StringLength(200)]
    public string? Artnr { get; set; }

    /// <summary>Anzeige-Schnappschuss zum Meldezeitpunkt (nicht Teil des Schluessels).</summary>
    [StringLength(200)]
    public string? Matchcode { get; set; }

    [Required]
    [StringLength(20)]
    public string Kuerzel { get; set; } = string.Empty;

    public int ProductionWorkplaceId { get; set; }
    public ProductionWorkplace ProductionWorkplace { get; set; } = null!;

    public int BdeOperatorId { get; set; }
    public BdeOperator BdeOperator { get; set; } = null!;

    public int BdeTerminalId { get; set; }
    public BdeTerminal BdeTerminal { get; set; } = null!;

    public decimal GoodQuantity { get; set; }
    public decimal ScrapQuantity { get; set; }
    public decimal SollmengeBeiMeldung { get; set; }

    /// <summary>Meldezeitpunkt in Lokalzeit (ADR 0025).</summary>
    public DateTime GemeldetAm { get; set; }

    /// <summary>Nur Baugruppen-Zeile: die zugleich erzeugte AG-Fertigmeldung (<see cref="BdeBooking"/>).</summary>
    public int? BdeBookingId { get; set; }
    public BdeBooking? BdeBooking { get; set; }

    /// <summary>v1.99.72 (Spec 2026-11-20-bde-liste-isolierungsfraese-pfad-material, ADR 0038): gesetzt = Meldung einer
    /// Lagerbestell-Position (HauptFA 0, SubFA 0, VaterFA/Position null, Artnr = Artikelnummer). Eigener gefilterter UNIQUE
    /// (<c>UX_BdeTeilMeldungen_Lagerbestellung</c>); alle FA-Lesewege filtern <c>WarehouseRequisitionItemId IS NULL</c>.</summary>
    public int? WarehouseRequisitionItemId { get; set; }
    public WarehouseRequisitionItem? WarehouseRequisitionItem { get; set; }

    public bool IsCancelled { get; set; }

    [StringLength(500)]
    public string? CancellationReason { get; set; }
}
