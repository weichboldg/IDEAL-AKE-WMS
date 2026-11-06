namespace IdealAkeWms.Models;

/// <summary>
/// Cache der Sage-PPS-Mitarbeiter (<c>PpsMitarbeiterInfo</c>, Spec 2026-11-06-sage-sdata-stammdaten-sync, E6b/F8). Reine Cache-Tabelle
/// je Lauf, KEIN AuditableEntity (Praezedenz <see cref="FaHierarchyNode"/>); Nachvollziehbarkeit ueber <see cref="SyncedAt"/> + Protokoll.
/// Schreibt nur der PPS-Mitarbeiter-Abgleich des Dienstes (Vollersatz je Lauf, leere Antwort aendert nichts).
/// Laengen laut SData-<c>$schema</c> (P-S1, 2026-11-06).
/// </summary>
public class SagePpsMitarbeiter
{
    public int Id { get; set; }
    public string Nummer { get; set; } = string.Empty; // string(20), Schluessel; Gegenstueck BdeOperator.SageMitarbeiternummer
    public string? Matchcode { get; set; }              // string(50)
    public string? Vorname { get; set; }                // string(35)
    public string? Nachname { get; set; }               // string(35)
    public string? Gruppe { get; set; }                 // string(10)
    public string? Kartennummer { get; set; }           // string(20), nur Anzeige (G14)
    public int? ExterneNummer { get; set; }             // int, nur Anzeige (G14)
    public bool? Aktiv { get; set; }
    public string? Schichtplannummer { get; set; }      // string(31)
    public string? Kostenstelle { get; set; }           // string(20)
    public string? UserName { get; set; }               // string(128)
    public DateTime SyncedAt { get; set; }
}
