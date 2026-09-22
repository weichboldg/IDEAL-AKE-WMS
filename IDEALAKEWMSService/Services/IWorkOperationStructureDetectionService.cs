namespace IDEALAKEWMSService.Services;

/// <summary>
/// IDEAL: Arbeitsgang-Erkennung aus der FA-Struktur. Leitet <c>WorkOperation</c>-Zeilen explizit aus
/// <c>FaHierarchyNode.Arbeitsschritte</c> (leerzeichen-getrennte Token je Position) ab und matcht sie
/// ueber <c>ProductionWorkplace.ArbeitsschrittCode</c> (Sage-Arbeitsplatz-Kuerzel, Baustein a). Ziel ist
/// die Tabelle, gegen die das BDE-Terminal tatsaechlich bucht (<c>WorkOperation</c>), nicht die
/// FA-Abarbeitungsliste (<c>FaWorkStep</c>).
///
/// Eigener Sync-Schritt NACH der FA-Materialisierung (braucht die materialisierten
/// <c>ProductionOrder</c>-Zeilen als Ziel) und NACH der Werkbank-Anlage (braucht deren Kuerzel-Katalog).
/// Unbekannte Token werden gemeldet, nicht angelegt (Melden-statt-Anlegen, ADR 0014); Kuerzel auf der
/// Ausschlussliste erzeugen weder Arbeitsgang noch Meldung; ein Kuerzel, das mehrere Werkbaenke trifft,
/// wird als „mehrdeutig" gemeldet. (Bis zum WorkOperation-Umbau hiess dieser Service
/// <c>FaWorkStepStructureDetectionService</c>, v1.41.0 — reines Rename, nie deployt.)
/// </summary>
public interface IWorkOperationStructureDetectionService
{
    Task<SyncResult> DetectAsync(bool dryRun, CancellationToken ct = default);
}
