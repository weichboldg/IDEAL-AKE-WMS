namespace IDEALAKEWMSService.Services;

/// <summary>
/// IDEAL: Arbeitsgang-Erkennung aus der FA-Struktur (v1.41.0). Leitet <c>FaWorkStep</c>-Zeilen
/// explizit aus <c>FaHierarchyNode.Arbeitsschritte</c> (leerzeichen-getrennte Token je Position)
/// ab, statt sie wie die AKE-Heuristik aus Bezeichnungen zu raten.
///
/// Eigener Sync-Schritt NACH der FA-Materialisierung (braucht die materialisierten
/// <c>ProductionOrder</c>-Zeilen als Ziel). Match exakt ueber <c>WorkStep.Code</c>; unbekannte Token
/// werden gemeldet, nicht angelegt (Melden-statt-Anlegen, ADR 0014). Nur-hinzufuegen-Semantik
/// identisch zum AKE-Vorbild <see cref="IFaWorkStepDetectionService"/>.
/// </summary>
public interface IFaWorkStepStructureDetectionService
{
    Task<SyncResult> DetectAsync(bool dryRun, CancellationToken ct = default);
}
