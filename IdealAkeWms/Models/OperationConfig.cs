using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

/// <summary>
/// Arbeitsgang-Stammdaten (bis v1.53.0 Klasse und Tabelle unter OSEON-Namen, umbenannt mit
/// Migration <c>RenameOperationConfigsAndAddAudit</c> / <c>SQL/95</c>): je Arbeitsgang-Kuerzel ein Termin-Offset und das Flag
/// „Fremdsystem-relevant". Gemeinsamer Schluessel fuer OSEON-AG-Namen und WMS-<c>WorkOperation.OperationNumber</c> (IDEAL).
/// Spec 2026-10-09-arbeitsgang-stammdaten-vereinheitlichen.
/// </summary>
public class OperationConfig : AuditableEntity
{
    /// <summary>
    /// Arbeitsgang-Kuerzel (OSEON-AG-Name wie "B", "ST" oder IDEAL-Kuerzel wie "KA"). OSEON matcht es mit
    /// <c>OseonWorkOperation.Name</c>, FA-Struktur/BDE mit <c>WorkOperation.OperationNumber</c>.
    /// </summary>
    [Required, MaxLength(100)]
    public string OperationName { get; set; } = string.Empty;

    /// <summary>Optionaler Anzeigename (z.B. "Belegen", "Stanzen").</summary>
    [MaxLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Offset in Arbeitstagen zum Bezugstermin. Negativ = davor, positiv = danach. Bezugstermin je Fremdsystem:
    /// OSEON Stanztermin (OSEON-Endtermin), IDEAL FE-Termin, AKE-BDE Fertigungstermin (ProductionDate).
    /// </summary>
    public int DueDateOffsetDays { get; set; }

    /// <summary>
    /// „Fremdsystem-relevant" (UI-Begriff). Wirkt derzeit nur in der OSEON-Teileverfolgung (Fertig-/Ampel-Berechnung) und
    /// im OSEON-Reporting: false = AG wird dort ignoriert. Fuer IDEAL und AKE-BDE ohne Wirkung.
    /// </summary>
    public bool IsExternalSystemRelevant { get; set; } = true;

    /// <summary>
    /// Herkunft des Eintrags (<see cref="OperationConfigSource"/>, UI „Quelle"): Sage-Abgleich, Fremdsystem (OSEON-Seed/Altbestand)
    /// oder manuell im Reiter. Geschrieben nur bei der Anlage und vom Arbeitsgang-Abgleich (Wechsel Manual -> Sage), nie vom
    /// Bearbeiten im Reiter. Spec 2026-10-16-arbeitsgang-stammdaten-abgleich-aus-sage-view.
    /// </summary>
    [StringLength(20)]
    public string Source { get; set; } = OperationConfigSource.Manual;

    /// <summary>
    /// Zustand „nicht in Sage seit" (Arbeitsgang-Abgleich): gesetzt, wenn das Kuerzel im letzten Lauf nicht in der Sage-View stand;
    /// NULL, sobald es wieder auftaucht. Nie fuer <see cref="OperationConfigSource.ExternalSystem"/>.
    /// </summary>
    public DateTime? SageMissingSince { get; set; }
}

/// <summary>Werte fuer <see cref="OperationConfig.Source"/> (DB-Klartext, UI uebersetzt im Controller).</summary>
public static class OperationConfigSource
{
    public const string Sage = "Sage";
    public const string ExternalSystem = "ExternalSystem";
    public const string Manual = "Manual";
}
