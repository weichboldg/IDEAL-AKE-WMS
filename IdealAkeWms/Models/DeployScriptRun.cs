namespace IdealAkeWms.Models;

/// <summary>
/// v1.99.0 (Spec 2026-11-11-update-skript-prueft-und-fuehrt-aus-mit-bestaetigung, ADR 0023): Marker je Daten-/Einmal-Eintrag des
/// Nachlauf-Manifests (<c>scripts/deploy/nachlauf.psd1</c>). Schreibt NUR das Update-Skript, in derselben Transaktion wie die
/// Datenaenderung; die Anwendung liest und schreibt die Tabelle nicht (kein Repository). Kein AuditableEntity: Protokoll, nur angehaengt,
/// nie geaendert (Muster SyncLog). Ein Eintrag mit Marker wird nie wieder angeboten (UNIQUE ManifestId).
/// </summary>
public class DeployScriptRun
{
    public int Id { get; set; }
    public string ManifestId { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? FileSha256 { get; set; }
    /// <summary>Scharf | Transaktion | Entfaellt | Spur | VonHand</summary>
    public string Mode { get; set; } = string.Empty;
    public int? Hits { get; set; }
    public string? Output { get; set; }
    public string PackageVersion { get; set; } = string.Empty;
    public string? PackageCommit { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string ExecutedBy { get; set; } = string.Empty;
    public string? Note { get; set; }
}
