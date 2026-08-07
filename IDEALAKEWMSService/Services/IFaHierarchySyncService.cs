namespace IDEALAKEWMSService.Services;

/// <summary>Ergebnis eines FaHierarchy-Full-Refresh-Laufs.</summary>
public record FaHierarchySyncResult(
    int Nodes,
    int OrderInfos,
    int DeletedNodes,
    int DeletedOrderInfos,
    bool Skipped,
    bool DryRun);

/// <summary>
/// Mehrstufiger FA-Struktur-/Stuecklisten-Import (IDEAL) aus den Sage-Views FAListe/FAInfos in die
/// lokalen Cache-Tabellen FaHierarchyNode/FaHierarchyOrderInfo (Full-Refresh).
/// </summary>
public interface IFaHierarchySyncService
{
    Task<FaHierarchySyncResult> RunAsync(bool dryRun, CancellationToken ct = default);
}
