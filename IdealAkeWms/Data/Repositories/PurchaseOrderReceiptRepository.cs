using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

/// <summary>
/// v1.99.4 (Wareneingang Teil 2, F5): im WMS erfasste Menge einer Bestellposition plus die an Sage uebergebenen Teilmengen (Zeitpunkt der
/// Uebergabe). <see cref="Abzuziehen"/> ist die EINE Stelle der Rest-Regel: abgezogen wird alles, was zum Abgleich-Stand noch nicht in Sage
/// enthalten war - alles ausser Uebergeben mit SageWeAm &lt;= SyncedAt (sonst zoege das WMS eine Uebergabe zweimal ab).
/// </summary>
public sealed record ErfasstJeBelPos(decimal Gesamt, IReadOnlyList<(DateTime Am, decimal Menge)> Uebergeben,
    IReadOnlyList<SageWareneingangStatus>? SageWe = null)
{
    public static readonly ErfasstJeBelPos Leer = new(0m, Array.Empty<(DateTime, decimal)>());

    /// <summary>v1.99.4 (E3): Zeile „Sage-Wareneingang: …“ der Maske, Anzahl je Status (ohne NichtNoetig); null = keiner.</summary>
    public string? SageWeText(Func<SageWareneingangStatus, string> text) => SageWe is { Count: > 0 } s
        ? "Sage-Wareneingang: " + string.Join(", ", s.GroupBy(x => x).OrderBy(g => g.Key).Select(g => $"{g.Count()}× {text(g.Key)}"))
        : null;
    public decimal Abzuziehen(DateTime syncedAt) => Gesamt - Uebergeben.Where(u => u.Am <= syncedAt).Sum(u => u.Menge);

    /// <summary>v1.99.10: Rest = offen laut Sage − im WMS noch abzuziehen, nie negativ. EINE Funktion fuer die Maske „Wareneingang zu
    /// Bestellung“ und die Lieferverzug-Mail (sonst laufen beide auseinander).</summary>
    public decimal Rest(decimal mengeOffen, DateTime syncedAt) => RestAus(mengeOffen, Abzuziehen(syncedAt));

    /// <summary>Rest aus einem bereits berechneten <see cref="Abzuziehen"/> (Zeilenmodell der Maske).</summary>
    public static decimal RestAus(decimal mengeOffen, decimal abzuziehen) => Math.Max(0m, mengeOffen - abzuziehen);
}

/// <summary>v1.84.0 (Spec 2026-11-08-wareneingang-aus-sage-bestellungen E3): Lesen der Wareneingaenge zu Bestellpositionen. Geschrieben
/// werden sie nur als Navigation an der <c>StockMovement</c> ueber <c>IStockMovementRepository.AddAsync</c> (Sage-Queue-Decorator).</summary>
public interface IPurchaseOrderReceiptRepository
{
    /// <summary>Im WMS erfasst je <c>BelPosId</c>, mit den an Sage uebergebenen Teilmengen (eine Abfrage fuer alle sichtbaren Zeilen, kein N+1).</summary>
    Task<Dictionary<int, ErfasstJeBelPos>> GetErfasstJeBelPosAsync(IReadOnlyCollection<int> belPosIds);

    /// <summary>v1.99.4 (E3): Wareneingaenge mit Sage-WE-Status (ohne NichtNoetig), neueste zuerst, mit Artikel und Lagerplatz.</summary>
    Task<List<PurchaseOrderReceipt>> GetSageWareneingaengeAsync(SageWareneingangStatus? status, string? bestellung, DateTime? von, DateTime? bis);

    /// <summary>v1.99.4 (E3): „Erneut senden“ - alle Fehler-Receipts der Gruppe auf Offen, Fehlertext leer, Versuche bleiben, Modified* gesetzt.
    /// Liefert die Anzahl; 0 = nichts im Status Fehler.</summary>
    Task<int> SageWeErneutSendenAsync(Guid vorgang, string user, string windowsUser);

    /// <summary>v1.99.7 (E5): Fehler/Klären-Gruppe von Hand auf Uebergeben (Belegnummer, SageWeAm = jetzt, Fehlertext leer). Liefert die Anzahl.</summary>
    Task<int> SageWeAlsUebergebenAsync(Guid vorgang, string belegnummer, string user, string windowsUser);

    /// <summary>v1.99.7 (Q-2): Anzahl Wareneingänge, die noch nicht an Sage übergeben sind (Offen, Fehler, Klären).</summary>
    Task<int> SageWeNichtUebergebenAsync();

    // ---- v1.99.11 Wareneingang-CSV (Spec 2026-11-12-wareneingang-barcode-lieferschein-csv-export) ----

    /// <summary>F1: erster Wareneingang mit diesem Barcode (ohne Gross/Klein) oder null.</summary>
    Task<PurchaseOrderReceipt?> BarcodeVergebenAsync(string barcode);

    /// <summary>F4: Ausgangszeilen der angezeigten Vorgaenge (eine Abfrage, kein N+1).</summary>
    Task<Dictionary<Guid, WareneingangCsvExport>> GetCsvExporteAsync(IReadOnlyCollection<Guid> vorgaenge);

    /// <summary>F4: Anzahl Vorgaenge mit Barcode ohne Ausgangszeile (wartende CSV-Uebergaben).</summary>
    Task<int> CsvWartendAsync();

    /// <summary>F4: „CSV erneut schreiben“ - Ausgangszeile im Status Fehler auf Offen, Fehlertext leer, Modified* gesetzt. 0 = nicht (mehr) Fehler.</summary>
    Task<int> CsvErneutSchreibenAsync(Guid vorgang, string user, string windowsUser);
}

public class PurchaseOrderReceiptRepository : IPurchaseOrderReceiptRepository
{
    private readonly ApplicationDbContext _db;

    public PurchaseOrderReceiptRepository(ApplicationDbContext db) => _db = db;

    public async Task<Dictionary<int, ErfasstJeBelPos>> GetErfasstJeBelPosAsync(IReadOnlyCollection<int> belPosIds)
    {
        if (belPosIds.Count == 0) return new Dictionary<int, ErfasstJeBelPos>();
        var zeilen = await _db.PurchaseOrderReceipts.AsNoTracking().Where(r => belPosIds.Contains(r.BelPosId))
            .Select(r => new { r.BelPosId, r.Quantity, r.SageWeStatus, r.SageWeAm }).ToListAsync();
        return zeilen.GroupBy(r => r.BelPosId).ToDictionary(g => g.Key, g => new ErfasstJeBelPos(g.Sum(r => r.Quantity),
            g.Where(r => r.SageWeStatus == SageWareneingangStatus.Uebergeben && r.SageWeAm != null).Select(r => (r.SageWeAm!.Value, r.Quantity)).ToList(),
            g.Where(r => r.SageWeStatus != SageWareneingangStatus.NichtNoetig).Select(r => r.SageWeStatus).ToList()));
    }

    public async Task<List<PurchaseOrderReceipt>> GetSageWareneingaengeAsync(SageWareneingangStatus? status, string? bestellung, DateTime? von, DateTime? bis)
    {
        var q = _db.PurchaseOrderReceipts.AsNoTracking()
            .Include(r => r.StockMovement).ThenInclude(m => m.Article)
            .Include(r => r.StockMovement).ThenInclude(m => m.StorageLocation)
            .Where(r => r.SageWeStatus != SageWareneingangStatus.NichtNoetig);
        if (status != null) q = q.Where(r => r.SageWeStatus == status);
        if (!string.IsNullOrWhiteSpace(bestellung)) { var b = bestellung.Trim(); q = q.Where(r => r.Belegnummer.Contains(b)); }
        if (von != null) q = q.Where(r => r.CreatedAt >= von.Value.Date);
        if (bis != null) q = q.Where(r => r.CreatedAt < bis.Value.Date.AddDays(1));
        return await q.OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id).ToListAsync();
    }

    public async Task<int> SageWeErneutSendenAsync(Guid vorgang, string user, string windowsUser)
    {
        var gruppe = await _db.PurchaseOrderReceipts.Where(r => r.SageWeVorgang == vorgang
            && (r.SageWeStatus == SageWareneingangStatus.Fehler || r.SageWeStatus == SageWareneingangStatus.Klaeren)).ToListAsync();
        var now = DateTime.Now;
        foreach (var r in gruppe)
        {
            r.SageWeStatus = SageWareneingangStatus.Offen;
            r.SageWeFehler = null;
            r.ModifiedAt = now; r.ModifiedBy = user; r.ModifiedByWindows = windowsUser;
        }
        await _db.SaveChangesAsync();
        return gruppe.Count;
    }

    public async Task<int> SageWeAlsUebergebenAsync(Guid vorgang, string belegnummer, string user, string windowsUser)
    {
        var gruppe = await _db.PurchaseOrderReceipts.Where(r => r.SageWeVorgang == vorgang
            && (r.SageWeStatus == SageWareneingangStatus.Fehler || r.SageWeStatus == SageWareneingangStatus.Klaeren)).ToListAsync();
        var now = DateTime.Now;
        foreach (var r in gruppe)
        {
            r.SageWeStatus = SageWareneingangStatus.Uebergeben;
            r.SageWeBelegnummer = belegnummer;
            r.SageWeAm = now;
            r.SageWeFehler = null;
            r.ModifiedAt = now; r.ModifiedBy = user; r.ModifiedByWindows = windowsUser;
        }
        await _db.SaveChangesAsync();
        return gruppe.Count;
    }

    public Task<int> SageWeNichtUebergebenAsync() =>
        _db.PurchaseOrderReceipts.CountAsync(r => r.SageWeStatus == SageWareneingangStatus.Offen
            || r.SageWeStatus == SageWareneingangStatus.Fehler || r.SageWeStatus == SageWareneingangStatus.Klaeren);

    // ponytail: ToUpper auf beiden Seiten (InMemory kennt keine Kollation, fallstricke §26); nutzt den Index nicht, reicht fuer die Tabellengroesse.
    public Task<PurchaseOrderReceipt?> BarcodeVergebenAsync(string barcode)
    {
        var b = barcode.Trim().ToUpper();
        return _db.PurchaseOrderReceipts.AsNoTracking().Where(r => r.Barcode != null && r.Barcode.ToUpper() == b)
            .OrderBy(r => r.Id).FirstOrDefaultAsync();
    }

    public async Task<Dictionary<Guid, WareneingangCsvExport>> GetCsvExporteAsync(IReadOnlyCollection<Guid> vorgaenge)
    {
        if (vorgaenge.Count == 0) return new Dictionary<Guid, WareneingangCsvExport>();
        return await _db.WareneingangCsvExporte.AsNoTracking().Where(e => vorgaenge.Contains(e.SageWeVorgang))
            .ToDictionaryAsync(e => e.SageWeVorgang);
    }

    public Task<int> CsvWartendAsync() =>
        _db.PurchaseOrderReceipts.Where(r => r.Barcode != null && r.SageWeVorgang != null
                && !_db.WareneingangCsvExporte.Any(e => e.SageWeVorgang == r.SageWeVorgang))
            .Select(r => r.SageWeVorgang).Distinct().CountAsync();

    public async Task<int> CsvErneutSchreibenAsync(Guid vorgang, string user, string windowsUser)
    {
        var e = await _db.WareneingangCsvExporte.FirstOrDefaultAsync(x => x.SageWeVorgang == vorgang && x.Status == WareneingangCsvStatus.Fehler);
        if (e == null) return 0;
        e.Status = WareneingangCsvStatus.Offen;
        e.Fehler = null;
        e.ModifiedAt = DateTime.Now; e.ModifiedBy = user; e.ModifiedByWindows = windowsUser;
        await _db.SaveChangesAsync();
        return 1;
    }
}
