using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

/// <summary>
/// Position eines <see cref="Beschichtungsauftrag"/>: Herkunftsschluessel (<see cref="HauptFA"/>, <see cref="VaterFA"/>,
/// <see cref="SubFA"/>, <see cref="Position"/> — die Struktur-Felder von <see cref="FaHierarchyNode"/>) und ein bei der
/// Anlage eingefrorener Snapshot. Bewusst KEIN Fremdschluessel auf <see cref="FaHierarchyNode"/>: deren Ids wechseln
/// mit jedem Sync, und ein erledigter Auftrag muss lesbar bleiben, auch wenn der HauptFA aus Sage verschwindet.
/// Eigene Id je Zeile (Vorsorge Antwort 7).
/// </summary>
public class BeschichtungsauftragPosition : AuditableEntity
{
    public int BeschichtungsauftragId { get; set; }
    public Beschichtungsauftrag Beschichtungsauftrag { get; set; } = null!;

    public int HauptFA { get; set; }
    public int? VaterFA { get; set; }
    public int SubFA { get; set; }
    public int? Position { get; set; }

    [StringLength(200)] public string? HauptArtnr { get; set; }
    [StringLength(200)] public string? Artnr { get; set; }
    [StringLength(200)] public string? Matchcode { get; set; }

    /// <summary>Menge — bei Anlage = <c>Sollmenge</c> (Antwort 8), im Entwurf aenderbar, keine Nachfuehrung.</summary>
    public decimal Menge { get; set; }
    public decimal Breite { get; set; }
    public decimal Hoehe { get; set; }
    public decimal Tiefe { get; set; }
}
