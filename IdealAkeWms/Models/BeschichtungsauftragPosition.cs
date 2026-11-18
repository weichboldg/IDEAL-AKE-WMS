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

    /// <summary>v1.99.34 (Spec 2026-11-16-beschichtungslogik-je-standort E5, Freigabe-Antwort 20): Stuecklisten-Position als Text
    /// (flache AKE-Quelle, z. B. <c>51.5.2</c>), mit <see cref="Artnr"/> der Positionsschluessel. NULL = IDEAL-Semantik
    /// (Schluessel ueber <see cref="VaterFA"/>/<see cref="SubFA"/>/<see cref="Position"/>). Nur bei der Anlage gesetzt.</summary>
    [StringLength(50)] public string? PositionText { get; set; }

    [StringLength(200)] public string? HauptArtnr { get; set; }
    [StringLength(200)] public string? Artnr { get; set; }
    [StringLength(200)] public string? Matchcode { get; set; }

    /// <summary>Menge — bei Anlage = <c>Sollmenge</c> (Antwort 8), im Entwurf aenderbar, keine Nachfuehrung.</summary>
    public decimal Menge { get; set; }
    public decimal Breite { get; set; }
    public decimal Hoehe { get; set; }
    public decimal Tiefe { get; set; }

    /// <summary>v1.99.41 (Spec 2026-11-18-beschichtungsauftrag-ausbau, A1/A8): Haken „verpackt“. Anlage aus der Vorschlagsauswahl
    /// = true, hinzugefuegte und Reklamations-Positionen = false. Versendet nur, wenn alle Positionen verpackt sind (A6).</summary>
    public bool Verpackt { get; set; }

    /// <summary>v1.99.41 (A7): freie Position ohne FA-Bezug (Artikel aus dem Stamm). Zaehlt nie als Vergabe
    /// (<c>GetVergabenAsync</c>), darf mehrfach vorkommen; ihr Schluessel (VaterFA null, SubFA 0, Position null) ist bedeutungslos.</summary>
    public bool OhneFa { get; set; }
}
