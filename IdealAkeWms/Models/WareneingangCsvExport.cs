namespace IdealAkeWms.Models;

/// <summary>v1.99.11 (Spec 2026-11-12-wareneingang-barcode-lieferschein-csv-export F2): Status der CSV-Datei je Sage-WE-Vorgang, als int, ohne 0.
/// „Wartet“ ist kein Status, sondern wird angezeigt, solange es noch keine Zeile gibt.</summary>
public enum WareneingangCsvStatus
{
    Offen = 1,
    Geschrieben = 2,
    Fehler = 3,
}

/// <summary>
/// v1.99.11 (F2): Ausgangstabelle der CSV-Übergabe – eine Zeile je <see cref="PurchaseOrderReceipt.SageWeVorgang"/> (UNIQUE). Angelegt vom
/// Dienst, sobald alle Receipts des Vorgangs mit Barcode „an Sage übergeben“ sind (auch nach „Als übergeben markieren“).
/// </summary>
public class WareneingangCsvExport : AuditableEntity
{
    public Guid SageWeVorgang { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public WareneingangCsvStatus Status { get; set; }
    /// <summary>Schreibversuche mit Fehler (DEFAULT 0 nur im SQL, kein HasDefaultValue – fallstricke Sentinel).</summary>
    public int Versuche { get; set; }
    public string? Fehler { get; set; }
    /// <summary>Voller Pfad der geschriebenen <c>.csv</c>.</summary>
    public string? Datei { get; set; }
    public DateTime? GeschriebenAm { get; set; }
}
