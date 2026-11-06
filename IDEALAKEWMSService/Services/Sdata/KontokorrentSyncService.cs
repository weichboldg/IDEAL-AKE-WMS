using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>Zeile aus <c>Kontokorrent</c> (Namen exakt wie in der SData-Antwort, Typen P-S1). Je Lauf werden nur die Felder der
/// jeweiligen Tabelle abgefragt (<see cref="KontokorrentSyncService.SelectKreditoren"/> / <see cref="KontokorrentSyncService.SelectDebitoren"/>).</summary>
public sealed record SdataKontoZeile(string? Kto, string? Matchcode, string? EUUStID, string? Zahlungskond, string? Gruppe, bool? Aktiv,
    string? Vertreter = null, decimal? USER_EKMindestbestellwert = null);

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E8c/F10, P-K1): liest Sage-Konten aus SData (<c>Kontokorrent</c>, Vertrag
/// CommonWawiServicesResources) — <c>KtoArt eq 'K'</c> in die <see cref="Creditor"/>-Tabelle, <c>KtoArt eq 'D'</c> in die
/// <see cref="Debitor"/>-Tabelle. Neuer Lese-Abgleich ohne View-Weg; Gates <c>Sync:KreditorenEnabled</c> / <c>Sync:DebitorenEnabled</c>
/// im SyncWorker (Default false).
/// <list type="bullet">
/// <item>Schluessel <c>SageKto</c> (Zeichenkette, nie numerisch). Neues Konto → <c>Source = Sage</c>, <c>ShortName</c> leer.</item>
/// <item>Treffer auf einen WMS-Datensatz (<c>Source = Manual</c>) mit gleichem <c>SageKto</c> → verknuepft: <c>Source</c> wechselt auf
///   <c>Sage</c> (die Sage-Felder werden ab jetzt ohnehin ueberschrieben, die Pflege sperrt sie), Uebergabe-Status Offen/Fehler →
///   Uebergeben (F11). Gleichnamige WMS-Datensaetze OHNE <c>SageKto</c> werden nie verknuepft, nur als „moegliche Dublette“ gemeldet.</item>
/// <item>Sage-Felder werden ueberschrieben (null = Sage liefert nichts = nicht schreiben, F1); WMS-Felder <c>ShortName</c>, <c>Matchcode</c>,
///   <c>Remark</c>, <c>IsCoater</c>, <c>IsActive</c> nie (IsActive nur bei der Neuanlage aus Sage <c>Aktiv</c>).</item>
/// <item>Fehlt ein Konto in Sage → <c>SageMissingSince</c>, nie loeschen; wieder da → zurueckgesetzt. Empty-Guard: leere Antwort
///   kennzeichnet nichts.</item>
/// </list>
/// Lesefehler: Lauf Fehler + throw (Mail ueber RunResilientAsync).
/// </summary>
public class KontokorrentSyncService
{
    public const string ServiceUser = "KontokorrentSync";
    public const string Ressource = "Kontokorrent";
    public const string WhereKreditoren = "KtoArt eq 'K'"; // P-K1 (AKE-Test 2026-11-06): K = 2.287 Lieferanten
    public const string WhereDebitoren = "KtoArt eq 'D'";  // P-K1: D = 534 Kunden
    // Nur Felder aus dem P-S1-Schema von Kontokorrent. Name/Adresse/Kontakt liegen in Adressen/Ansprechpartner — deren Schema ist
    // nicht belegt, ein unbekanntes Feld im select bricht die Abfrage (HTTP 500).
    // TODO(P-S1): Adressen (Name1, Strasse, PLZ, Ort, Land, EMail, Telefon, USER_KundenNameFA) per $schema pruefen, dann ueber
    // Kontokorrent.Adresse nachlesen. Bis dahin: Name = Sage-Matchcode bei der Neuanlage, Adressfelder bleiben unberuehrt.
    private const string SelectBasis = "Kto,Matchcode,EUUStID,Zahlungskond,Gruppe,Aktiv";
    public const string SelectKreditoren = SelectBasis + ",USER_EKMindestbestellwert";
    public const string SelectDebitoren = SelectBasis + ",Vertreter";

    private readonly ApplicationDbContext _ctx;
    private readonly ISageSdataReadClient _client;
    private readonly IConfiguration _config;
    private readonly ILogger<KontokorrentSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public KontokorrentSyncService(ApplicationDbContext ctx, ISageSdataReadClient client, IConfiguration config,
        ILogger<KontokorrentSyncService> logger, ISyncLogger syncLogger) // ISyncLogger als LETZTER Ctor-Parameter (ADR 0010)
    {
        _ctx = ctx;
        _client = client;
        _config = config;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public Task<SyncResult> RunKreditorenAsync(bool dryRun, CancellationToken ct = default, SdataLeseZiel? ziel = null) =>
        AbgleichAsync(_ctx.Creditors, SyncLogServices.Kreditoren, "Kreditoren", WhereKreditoren, SelectKreditoren, (c, z) =>
        {
            // TODO(P-S1): Nachkommastellen unbelegt, Spalte decimal(18,2) laut Spec F10.
            var wert = z.USER_EKMindestbestellwert is { } w ? Math.Round(w, 2) : (decimal?)null;
            if (wert == null || wert == c.SageEKMindestbestellwert) return false;
            c.SageEKMindestbestellwert = wert;
            return true;
        }, dryRun, ziel, ct);

    public Task<SyncResult> RunDebitorenAsync(bool dryRun, CancellationToken ct = default, SdataLeseZiel? ziel = null) =>
        AbgleichAsync(_ctx.Debitors, SyncLogServices.Debitoren, "Debitoren", WhereDebitoren, SelectDebitoren, (d, z) =>
            Setzen(Text(z.Vertreter), d.SageVertreter, v => d.SageVertreter = v), dryRun, ziel, ct);

    private async Task<SyncResult> AbgleichAsync<T>(DbSet<T> set, string lauf, string bezeichnung, string where, string select,
        Func<T, SdataKontoZeile, bool> zusatz, bool dryRun, SdataLeseZiel? ziel, CancellationToken ct)
        where T : AuditableEntity, ISageKonto, new()
    {
        await using var run = await _syncLogger.BeginRunAsync(lauf, ct);

        IReadOnlyList<SdataKontoZeile> zeilen;
        try
        {
            ziel ??= await SdataLeseZielLeser.LesenAsync(_config, ct);
            var seiten = await _client.QueryAllAsync<SdataKontoZeile>(ziel, SageSdataReadClient.VertragResources, Ressource, where, select, "Kto", ct);
            zeilen = seiten.Zeilen;
            await run.LogInfoAsync(seiten.Zusammenfassung + $" ({where})", ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "{Bereich} aus Sage (SData) nicht lesbar.", bezeichnung);
            await run.LogErrorAsync($"Sage (SData) nicht lesbar: {ex.Message}", ct: ct);
            await run.FinishFailedAsync($"Sage (SData) nicht lesbar: {ex.Message}", ct: ct);
            throw;
        }

        try
        {
            var (eindeutig, uebersprungen) = await SdataSchluessel.EindeutigAsync(zeilen, z => z.Kto, run, "Kto", ct);
            int neu = 0, aktualisiert = 0, unveraendert = 0, nichtInSage = 0, wiederDa = 0;
            var verknuepft = new List<string>();
            var neuMarkiert = new List<T>();
            var now = DateTime.Now;
            var bestand = await set.ToListAsync(ct);
            // App-Guard zum gefilterten UNIQUE-Index IX_*_SageKto (InMemory erzwingt ihn nicht): bei Dubletten gewinnt der erste.
            var byKto = bestand.Where(k => !string.IsNullOrWhiteSpace(k.SageKto))
                .GroupBy(k => k.SageKto!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            void Geaendert(T k) { k.ModifiedAt = now; k.ModifiedBy = ServiceUser; k.ModifiedByWindows = ServiceUser; }

            foreach (var z in eindeutig)
            {
                var kto = z.Kto!.Trim();
                var matchcode = Text(z.Matchcode);
                if (!byKto.TryGetValue(kto, out var k))
                {
                    neu++;
                    if (dryRun) continue;
                    var anlage = new T
                    {
                        Name = matchcode ?? kto, Source = StorageLocationSource.Sage, IsActive = z.Aktiv ?? true, SageKto = kto,
                        CreatedAt = now, CreatedBy = ServiceUser, CreatedByWindows = ServiceUser,
                    };
                    SageFelder(anlage, z);
                    zusatz(anlage, z);
                    set.Add(anlage);
                    continue;
                }

                var warSage = k.Source == StorageLocationSource.Sage;
                var anders = SageFelder(k, z) | zusatz(k, z);
                // Name nur, wenn er schon aus Sage stammt — den Namen eines verknuepften WMS-Datensatzes nicht durch den Matchcode ersetzen.
                if (warSage) anders |= Setzen(matchcode, k.Name, v => k.Name = v!);
                var warWeg = k.SageMissingSince != null;
                if (warWeg) { wiederDa++; k.SageMissingSince = null; }
                if (!warSage)
                {
                    verknuepft.Add($"{kto} → {k.Name}");
                    k.Source = StorageLocationSource.Sage;
                    if (k.SageUebergabeStatus is SageUebergabeStatus.Offen or SageUebergabeStatus.Fehler)
                    {
                        k.SageUebergabeStatus = SageUebergabeStatus.Uebergeben;
                        k.SageUebergabeAm = now;
                    }
                }
                if (anders) aktualisiert++;
                if (!anders && !warWeg && warSage) { unveraendert++; continue; }
                Geaendert(k);
            }

            if (verknuepft.Count > 0)
                await run.LogInfoAsync(SdataSchluessel.Kappen($"Mit Sage verknuepft (WMS-Datensatz mit gleicher Kontonummer, Herkunft jetzt Sage): " +
                    string.Join(", ", verknuepft.Take(20))), ct: ct);

            // „Moegliche Dublette“: aktiver WMS-Datensatz ohne Kontonummer, dessen Name dem Sage-Matchcode entspricht — nie automatisch verknuepfen.
            var sageNamen = eindeutig.Where(z => Text(z.Matchcode) != null)
                .GroupBy(z => Text(z.Matchcode)!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Kto!.Trim(), StringComparer.OrdinalIgnoreCase);
            var dubletten = bestand.Where(k => k.IsActive && string.IsNullOrWhiteSpace(k.SageKto) && sageNamen.ContainsKey(k.Name.Trim()))
                .Select(k => $"„{k.Name.Trim()}“ (WMS-Id {k.Id}) ↔ Sage {sageNamen[k.Name.Trim()]}").ToList();
            if (dubletten.Count > 0)
                await run.LogWarningAsync(SdataSchluessel.Kappen($"{dubletten.Count} moegliche Dublette(n), nicht verknuepft (das Sage-Konto " +
                    $"steht als eigener Datensatz daneben; WMS-Datensatz pruefen und ggf. deaktivieren): {string.Join("; ", dubletten.Take(20))}"), ct: ct);

            if (eindeutig.Count == 0)
                await run.LogWarningAsync($"Sage liefert keine {bezeichnung} — keine Kennzeichnung \"nicht in Sage\" (Empty-Guard).", ct: ct);
            else
            {
                var present = eindeutig.Select(z => z.Kto!.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var k in byKto.Values.Where(k => !present.Contains(k.SageKto!.Trim())))
                {
                    nichtInSage++;
                    if (k.SageMissingSince != null) continue;
                    neuMarkiert.Add(k);
                    k.SageMissingSince = now;
                    Geaendert(k);
                }
                if (neuMarkiert.Count > 0)
                    await run.LogWarningAsync(SdataSchluessel.Kappen($"Nicht mehr in Sage ({neuMarkiert.Count} neu gekennzeichnet): " +
                        string.Join(", ", neuMarkiert.Take(20).Select(k => $"{k.SageKto} {k.Name}"))), ct: ct);
            }

            if (!dryRun) await _ctx.SaveChangesAsync(ct);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["gelesen"] = eindeutig.Count,
                ["neu"] = neu,
                ["aktualisiert"] = aktualisiert,
                ["verknuepft"] = verknuepft.Count,
                ["unveraendert"] = unveraendert,
                ["uebersprungen"] = uebersprungen,
                ["nicht_in_sage"] = nichtInSage,
                ["nicht_in_sage_neu"] = neuMarkiert.Count,
                ["wieder_in_sage"] = wiederDa,
                ["moegliche_dubletten"] = dubletten.Count,
            }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
            return new SyncResult(neu, aktualisiert + verknuepft.Count, uebersprungen);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "{Bereich}-Abgleich fehlgeschlagen.", bezeichnung);
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }

    /// <summary>Gemeinsame Sage-Felder; true = mindestens eins geaendert.</summary>
    private static bool SageFelder(ISageKonto k, SdataKontoZeile z) =>
        Setzen(Text(z.Matchcode), k.SageMatchcode, v => k.SageMatchcode = v)
        | Setzen(Text(z.Gruppe), k.SageGruppe, v => k.SageGruppe = v)
        | Setzen(Text(z.EUUStID), k.SageEUUStID, v => k.SageEUUStID = v)
        | Setzen(Text(z.Zahlungskond), k.SageZahlungskondition, v => k.SageZahlungskondition = v);

    /// <summary>F1: null = Sage liefert den Wert nicht = nicht schreiben.</summary>
    private static bool Setzen(string? neu, string? alt, Action<string?> schreiben)
    {
        if (neu == null || string.Equals(neu, alt, StringComparison.Ordinal)) return false;
        schreiben(neu);
        return true;
    }

    private static string? Text(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
