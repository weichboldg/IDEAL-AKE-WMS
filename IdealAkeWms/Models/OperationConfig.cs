using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

/// <summary>
/// Arbeitsgang-Stammdaten (bis v1.53.0 <c>OseonOperationConfig</c> / Tabelle <c>OseonOperationConfigs</c>, umbenannt mit
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
}
