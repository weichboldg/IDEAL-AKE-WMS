using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

/// <summary>
/// v1.84.0 (Spec 2026-11-08-wareneingang-aus-sage-bestellungen E2/E3): Lesezugriff auf den Cache offener Sage-Bestellpositionen.
/// Einzige Lese-Schnittstelle fuer offene Sage-Bestellungen im WMS (auch fuer das spaetere Einkaufs-Epic). Kein Caching-Decorator
/// (der Cache ist die Tabelle). Schreibt nur der Dienst-Abgleich.
/// </summary>
public interface ISagePurchaseOrderLineRepository
{
    /// <summary>Alle Positionen (ca. 1.000 am AKE-Test), ohne Tracking.</summary>
    Task<List<SagePurchaseOrderLine>> GetAllAsync();
    Task<List<SagePurchaseOrderLine>> GetByBelIdAsync(int belId);
    /// <summary>Distinct <c>BelId</c> (mit Belegnummer) zu einer Eingabe, getrimmt: nur Ziffern = Endung „-&lt;Eingabe&gt;“ (Bestellnummer ohne
    /// Belegjahr, v1.99.52), sonst exakt. Mehrere = sichtbare Meldung beim Aufrufer, nie still die erste.</summary>
    Task<List<(int BelId, string Belegnummer)>> GetBelIdsByBelegnummerAsync(string belegnummer);
    /// <summary>Zeitpunkt des letzten erfolgreichen Abgleichs (max <c>SyncedAt</c>); null = Cache leer.</summary>
    Task<DateTime?> GetStandAsync();
    /// <summary>v1.99.85: Positionen zu Artikelnummern (getrimmt; Gross/Klein macht die CI-Kollation der DB), EINE Abfrage fuer Artikelinfo
    /// und Lagerbestellung-Details. Leere Eingabe = leere Liste ohne Abfrage.</summary>
    Task<List<SagePurchaseOrderLine>> GetByArtikelnummernAsync(IReadOnlyCollection<string> artikelnummern);
}

public class SagePurchaseOrderLineRepository : ISagePurchaseOrderLineRepository
{
    private readonly ApplicationDbContext _db;

    public SagePurchaseOrderLineRepository(ApplicationDbContext db) => _db = db;

    public Task<List<SagePurchaseOrderLine>> GetAllAsync() => _db.SagePurchaseOrderLines.AsNoTracking().ToListAsync();

    public Task<List<SagePurchaseOrderLine>> GetByBelIdAsync(int belId) =>
        _db.SagePurchaseOrderLines.AsNoTracking().Where(l => l.BelId == belId).OrderBy(l => l.BelPosId).ToListAsync();

    public async Task<List<(int BelId, string Belegnummer)>> GetBelIdsByBelegnummerAsync(string belegnummer)
    {
        var key = belegnummer.Trim();
        // v1.99.52 (Spec 2026-11-23 F1): Belegjahr = Sage-Geschaeftsjahr, im WMS unbekannt -> kein Jahr ergaenzen, sondern Endung suchen.
        var endung = "-" + key;
        var q = _db.SagePurchaseOrderLines.AsNoTracking();
        q = key.Length > 0 && key.All(char.IsAsciiDigit) ? q.Where(l => l.Belegnummer.EndsWith(endung)) : q.Where(l => l.Belegnummer == key);
        var treffer = await q.Select(l => new { l.BelId, l.Belegnummer }).Distinct().ToListAsync();
        return treffer.GroupBy(t => t.BelId).Select(g => (g.Key, g.First().Belegnummer)).OrderBy(t => t.Belegnummer).ThenBy(t => t.Key).ToList();
    }

    public async Task<DateTime?> GetStandAsync() =>
        await _db.SagePurchaseOrderLines.AsNoTracking().Select(l => (DateTime?)l.SyncedAt).MaxAsync();

    public async Task<List<SagePurchaseOrderLine>> GetByArtikelnummernAsync(IReadOnlyCollection<string> artikelnummern)
    {
        var nummern = artikelnummern.Select(n => n.Trim()).Where(n => n.Length > 0).Distinct().ToList();
        if (nummern.Count == 0) return new();
        return await _db.SagePurchaseOrderLines.AsNoTracking().Where(l => nummern.Contains(l.Artikelnummer)).ToListAsync();
    }
}
