using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services.HierarchischeStruktur;

namespace IdealAkeWms.Data.Repositories;

/// <summary>
/// IDEAL Minimal-Fix (Spec 2026-08-18): äußerster <see cref="IBomRepository"/>-Decorator. Im
/// hierarchischen Modus (Master <c>ProduktionsauftragHierarchisch</c> = true) existiert die fest
/// verdrahtete AKE-Stücklisten-View nicht → ein Live-Zugriff endete in HTTP 500. Der Guard fängt das
/// ab, BEVOR Cache oder DB berührt werden, und gibt ein leeres Ergebnis mit eigener Quellen-Kennung
/// zurück. Die Stückliste kommt im hierarchischen Modus aus der FA-Struktur (`/FaHierarchy`).
///
/// Bewusst: der Guard hängt am **Master-Schalter**, NICHT am Scheitern (kein try/catch — das würde
/// echte SQL-Fehler verschlucken, Spec-Fallstrick 1). Als äußerster Decorator deckt er **alle**
/// Aufrufer von <see cref="IBomRepository"/> ab (Picking-BOM, Druck, Read-only-Vorbau-BOM).
/// </summary>
public class HierarchicalBomGuardRepository : IBomRepository
{
    private readonly IBomRepository _inner;
    private readonly IServiceSettingRepository _settings;

    public HierarchicalBomGuardRepository(IBomRepository inner, IServiceSettingRepository settings)
    {
        _inner = inner;
        _settings = settings;
    }

    public async Task<BomQueryResult> GetBomItemsAsync(string productionOrderArticleNumber)
    {
        // Master-Zustand über den bestehenden web-seitigen Leseweg (Teil 7) — kein zweiter Key,
        // keine eigene Caching-Logik.
        var master = await _settings.GetValueAsync(HierarchischeStrukturKeys.Master);
        if (string.Equals(master, "true", StringComparison.OrdinalIgnoreCase))
            return new BomQueryResult(new List<BomItem>(), BomDataSources.HierarchicalUnavailable);

        // AKE / flacher Modus: unverändert an den Cache-/Live-Pfad delegieren.
        return await _inner.GetBomItemsAsync(productionOrderArticleNumber);
    }
}
