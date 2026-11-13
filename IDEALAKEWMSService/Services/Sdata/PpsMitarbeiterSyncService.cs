using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>Zeile aus <c>PpsMitarbeiterInfo</c> (Namen exakt wie in der SData-Antwort, Typen P-S1).</summary>
public sealed record SdataPpsMitarbeiterZeile(string? Nummer, string? Matchcode, string? Vorname, string? Nachname, string? Gruppe,
    string? Kartennummer, int? ExterneNummer, bool? Aktiv, string? Schichtplannummer, string? Kostenstelle, string? UserName);

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E6b/F8, G14): liest die Sage-PPS-Mitarbeiter aus SData (<c>PpsMitarbeiterInfo</c>,
/// Vertrag CommonWawiServicesResources) in den Cache <c>SagePpsMitarbeiter</c> (Vollersatz je Lauf). Neuer Lese-Abgleich ohne
/// View-Weg (keine Weiche), Gate <c>Sync:PpsMitarbeiterEnabled</c> im SyncWorker.
/// Legt NIE einen <see cref="BdeOperator"/> an und verknuepft nie (auch nicht ueber den Namen) — Zuordnung nur manuell in der
/// BDE-Personalpflege. Meldet aktive BDE-Mitarbeiter ohne Sage-Nummer und Zuordnungen, deren Nummer nicht (mehr) in Sage steht.
/// Empty-Guard: eine leere Antwort laesst den Cache unveraendert. Lesefehler: Lauf Fehler + throw (Mail ueber RunResilientAsync).
/// </summary>
public class PpsMitarbeiterSyncService
{
    public const string ServiceUser = "PpsMitarbeiterSync";
    public const string Ressource = "PpsMitarbeiterInfo";
    public const string Select = "Nummer,Matchcode,Vorname,Nachname,Gruppe,Kartennummer,ExterneNummer,Aktiv,Schichtplannummer,Kostenstelle,UserName";

    private readonly ApplicationDbContext _ctx;
    private readonly ISageSdataReadClient _client;
    private readonly IConfiguration _config;
    private readonly ILogger<PpsMitarbeiterSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public PpsMitarbeiterSyncService(ApplicationDbContext ctx, ISageSdataReadClient client, IConfiguration config,
        ILogger<PpsMitarbeiterSyncService> logger, ISyncLogger syncLogger) // ISyncLogger als LETZTER Ctor-Parameter (ADR 0010)
    {
        _ctx = ctx;
        _client = client;
        _config = config;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    /// <param name="ziel">Nur fuer Tests; im Betrieb aus ServiceSettings (<see cref="SdataLeseZielLeser"/>).</param>
    public async Task<SyncResult> RunAsync(bool dryRun, CancellationToken ct = default, SdataLeseZiel? ziel = null)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.PpsMitarbeiter, ct);

        IReadOnlyList<SdataPpsMitarbeiterZeile> zeilen;
        try
        {
            ziel ??= await SdataLeseZielLeser.LesenAsync(_config, ct);
            if (ziel.Warnung is { } zugangsWarnung) await run.LogWarningAsync(zugangsWarnung, ct: ct); // v1.99.14 F6: Rueckfall appsettings
            var seiten = await _client.QueryAllAsync<SdataPpsMitarbeiterZeile>(ziel, SageSdataReadClient.VertragResources, Ressource,
                null, Select, "Nummer", ct);
            zeilen = seiten.Zeilen;
            await run.LogInfoAsync(seiten.Zusammenfassung, ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "PPS-Mitarbeiter aus Sage (SData) nicht lesbar.");
            await run.LogErrorAsync($"Sage (SData) nicht lesbar: {ex.Message}", ct: ct);
            await run.FinishFailedAsync($"Sage (SData) nicht lesbar: {ex.Message}", ct: ct);
            throw;
        }

        try
        {
            var (eindeutig, uebersprungen) = await SdataSchluessel.EindeutigAsync(zeilen, z => z.Nummer, run, "Nummer", ct);
            var now = DateTime.Now;

            if (eindeutig.Count == 0)
                await run.LogWarningAsync("Sage liefert keine PPS-Mitarbeiter — Cache unveraendert (Empty-Guard).", ct: ct);
            else if (!dryRun)
            {
                _ctx.SagePpsMitarbeiter.RemoveRange(await _ctx.SagePpsMitarbeiter.ToListAsync(ct));
                _ctx.SagePpsMitarbeiter.AddRange(eindeutig.Select(z => new SagePpsMitarbeiter
                {
                    Nummer = z.Nummer!.Trim(),
                    Matchcode = z.Matchcode?.Trim(),
                    Vorname = z.Vorname?.Trim(),
                    Nachname = z.Nachname?.Trim(),
                    Gruppe = z.Gruppe?.Trim(),
                    Kartennummer = z.Kartennummer?.Trim(),
                    ExterneNummer = z.ExterneNummer,
                    Aktiv = z.Aktiv,
                    Schichtplannummer = z.Schichtplannummer?.Trim(),
                    Kostenstelle = z.Kostenstelle?.Trim(),
                    UserName = z.UserName?.Trim(),
                    SyncedAt = now,
                }));
                await _ctx.SaveChangesAsync(ct);
            }

            // Zuordnungs-Pruefung gegen die gelesene Menge (bei leerer Antwort gegen den unveraenderten Cache).
            var nummern = (eindeutig.Count > 0
                    ? eindeutig.Select(z => z.Nummer!.Trim()).ToList()
                    : await _ctx.SagePpsMitarbeiter.AsNoTracking().Select(m => m.Nummer).ToListAsync(ct))
                .ToHashSet(StringComparer.Ordinal);
            var operators = await _ctx.BdeOperators.AsNoTracking().ToListAsync(ct);
            var zugeordnet = operators.Where(o => o.SageMitarbeiternummer != null)
                .Select(o => o.SageMitarbeiternummer!).ToHashSet(StringComparer.Ordinal);
            var nichtZugeordnet = nummern.Count(n => !zugeordnet.Contains(n));

            // Befund §11/§14: ohne Sage-Mitarbeiter keine BDE-Rueckmeldung -> melden (Anzahl + erste 20).
            var ohneNummer = operators.Where(o => o.IsActive && string.IsNullOrWhiteSpace(o.SageMitarbeiternummer))
                .OrderBy(o => o.PersonnelNumber, StringComparer.Ordinal).ToList();
            if (ohneNummer.Count > 0)
                await run.LogWarningAsync(SdataSchluessel.Kappen($"{ohneNummer.Count} aktive BDE-Mitarbeiter ohne Sage-Mitarbeiternummer " +
                    $"(erste 20): {string.Join(", ", ohneNummer.Take(20).Select(o => $"{o.PersonnelNumber} {o.LastName} {o.FirstName}"))}"), ct: ct);

            var unbekannt = nummern.Count == 0 ? new List<BdeOperator>() : operators
                .Where(o => o.SageMitarbeiternummer != null && !nummern.Contains(o.SageMitarbeiternummer))
                .OrderBy(o => o.SageMitarbeiternummer, StringComparer.Ordinal).ToList();
            if (unbekannt.Count > 0)
                await run.LogWarningAsync(SdataSchluessel.Kappen($"{unbekannt.Count} zugeordnete Sage-Mitarbeiternummern stehen nicht (mehr) in Sage " +
                    $"(erste 20): {string.Join(", ", unbekannt.Take(20).Select(o => $"{o.SageMitarbeiternummer} → {o.PersonnelNumber} {o.LastName} {o.FirstName}"))}"),
                    ct: ct);

            if (nichtZugeordnet > 0)
                await run.LogInfoAsync($"{nichtZugeordnet} Sage-Mitarbeiter ohne Zuordnung — manuell in BDE-Stammdaten › Sage-Zuordnung.", ct: ct);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["gelesen"] = eindeutig.Count,
                ["uebersprungen"] = uebersprungen,
                ["nicht_zugeordnet"] = nichtZugeordnet,
                ["bde_ohne_sage_nummer"] = ohneNummer.Count,
                ["sage_nummer_unbekannt"] = unbekannt.Count,
            }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
            return new SyncResult(eindeutig.Count, 0, uebersprungen);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "PPS-Mitarbeiter-Abgleich fehlgeschlagen.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }
}

/// <summary>E6b: gemeinsame Schluessel-Bereinigung der neuen Lese-Abgleiche (leere Schluessel + Dubletten melden statt still verwerfen).</summary>
internal static class SdataSchluessel
{
    private const int MaxMessageLength = 1000; // SyncLogs.Message NVARCHAR(1000)

    public static string Kappen(string message) =>
        message.Length > MaxMessageLength ? message[..(MaxMessageLength - 1)] + "…" : message;

    /// <summary>Getrimmter Schluessel; Zeilen ohne Schluessel und jede weitere Zeile mit gleichem Schluessel werden uebersprungen und
    /// gemeldet (der UNIQUE-Index wuerde sonst den ganzen Lauf abbrechen).</summary>
    public static async Task<(List<T> Eindeutig, int Uebersprungen)> EindeutigAsync<T>(IReadOnlyList<T> zeilen, Func<T, string?> schluessel,
        ISyncRun run, string feld, CancellationToken ct)
    {
        var eindeutig = new List<T>();
        var gesehen = new HashSet<string>(StringComparer.Ordinal);
        var doppelt = new SortedSet<string>(StringComparer.Ordinal);
        int ohne = 0;
        foreach (var z in zeilen)
        {
            var k = schluessel(z)?.Trim();
            if (string.IsNullOrEmpty(k)) { ohne++; continue; }
            if (!gesehen.Add(k)) { doppelt.Add(k); continue; }
            eindeutig.Add(z);
        }
        if (ohne > 0)
            await run.LogWarningAsync($"{ohne} Zeile(n) ohne {feld} uebersprungen.", ct: ct);
        if (doppelt.Count > 0)
            await run.LogWarningAsync(Kappen($"{feld} mehrfach geliefert, nur die erste Zeile uebernommen: {string.Join(", ", doppelt.Take(20))}"), ct: ct);
        return (eindeutig, zeilen.Count - eindeutig.Count);
    }
}
