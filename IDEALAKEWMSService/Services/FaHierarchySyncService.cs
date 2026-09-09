using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Full-Refresh-Import der IDEAL-FA-Hierarchie aus den Sage-Views in die lokalen Cache-Tabellen.
/// Primaerweg (RCSI AN, Standard-Scope Teil 1): DELETE FROM beider Zieltabellen + Neubefuellung in
/// EINER Transaktion — kein Staging, kein sp_rename, kein GRANT ALTER.
///
/// Absicherung: View-Namen aus ServiceSettings, Whitelist + QUOTENAME (FaHierarchySql). Empty-Guard
/// auf den ungefilterten Roh-Zeilenzahlen BEIDER Views VOR jeder Mutation. Fehlermail
/// (ISyncErrorNotifier) explizit bei ungueltigem View-Namen UND bei leerem View-Read, da
/// RunResilientAsync nur bei geworfener Exception mailt (ADR 0010, S-4).
/// </summary>
public class FaHierarchySyncService : IFaHierarchySyncService
{
    private const string SyncStep = "FaHierarchy-Sync";

    private readonly ApplicationDbContext _ctx;
    private readonly IConfiguration _config;
    private readonly ISyncErrorNotifier _errorNotifier;
    private readonly ILogger<FaHierarchySyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public FaHierarchySyncService(
        ApplicationDbContext ctx,
        IConfiguration config,
        ISyncErrorNotifier errorNotifier,
        ILogger<FaHierarchySyncService> logger,
        ISyncLogger syncLogger) // ISyncLogger als LETZTER Ctor-Parameter (ADR 0010)
    {
        _ctx = ctx;
        _config = config;
        _errorNotifier = errorNotifier;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task<FaHierarchySyncResult> RunAsync(bool dryRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.FaHierarchy, ct);

        // 1) View-Namen aus ServiceSettings lesen + validieren (Whitelist + QUOTENAME).
        var listeRaw = await ServiceSettings.GetValueAsync(_config, "Sync:FaHierarchyListeViewName", ct);
        if (string.IsNullOrWhiteSpace(listeRaw))
            listeRaw = "[vw_IDEAL-AKE_Kommissionierung_FAListe]";
        var infosRaw = await ServiceSettings.GetValueAsync(_config, "Sync:FaHierarchyInfosViewName", ct);
        if (string.IsNullOrWhiteSpace(infosRaw))
            infosRaw = "[vw_IDEAL-AKE_Kommissionierung_FAInfos]";

        string quotedListe, quotedInfos;
        try
        {
            quotedListe = FaHierarchySql.ValidateAndQuote(listeRaw);
            quotedInfos = FaHierarchySql.ValidateAndQuote(infosRaw);
        }
        catch (FaHierarchyViewNameException ex)
        {
            // KEIN SQL wird ausgefuehrt. FinishFailedAsync + explizite Fehlermail (S-4).
            _logger.LogError(ex, "FaHierarchy-Sync: ungueltiger View-Name.");
            await run.LogErrorAsync($"Ungueltiger View-Name: {ex.Message}", ct: ct);
            await run.FinishFailedAsync($"Ungueltiger View-Name: {ex.Message}", ct: ct);
            await _errorNotifier.NotifyAsync(
                "FaHierarchy-Sync: ungueltiger View-Name",
                new InvalidOperationException(
                    $"FaHierarchy-Sync abgebrochen — ungueltiger View-Name (kein SQL ausgefuehrt): {ex.Message}"),
                ct);
            return new FaHierarchySyncResult(0, 0, 0, 0, Skipped: true, dryRun);
        }

        try
        {
            var sageConnStr = _config.GetConnectionString("SageConnection")
                ?? throw new InvalidOperationException("SageConnection nicht konfiguriert.");

            await using var conn = new SqlConnection(sageConnStr);
            await conn.OpenAsync(ct);

            // 2) Empty-Guard: ungefilterte Roh-Zeilenzahlen BEIDER Views VOR jeder Mutation
            //    (sonst maskiert die Existenzpruefung einen leeren FAInfos-Read als "0 Struktur-Zeilen").
            var listeCount = await ScalarCountAsync(conn, FaHierarchySql.BuildCountSql(quotedListe), ct);
            var infosCount = await ScalarCountAsync(conn, FaHierarchySql.BuildCountSql(quotedInfos), ct);

            if (listeCount == 0 || infosCount == 0)
            {
                var msg = $"Leerer View-Read (FAListe={listeCount}, FAInfos={infosCount}) — " +
                          "kein Replace, bestehende Zieltabellen bleiben unangetastet.";
                _logger.LogWarning("FaHierarchy-Sync: {Msg}", msg);
                await run.LogWarningAsync(msg, ct: ct);
                await _errorNotifier.NotifyAsync(
                    "FaHierarchy-Sync: leerer View-Read",
                    new InvalidOperationException($"FaHierarchy-Sync uebersprungen: {msg}"),
                    ct);
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["neu"] = 0,
                    ["geloescht"] = 0,
                }, messageSuffix: "[uebersprungen: leerer View-Read]", ct: ct);
                return new FaHierarchySyncResult(0, 0, 0, 0, Skipped: true, dryRun);
            }

            // 3) Zielzeilen im Speicher aufbauen (Existenzpruefung im SQL, Anforderung 2).
            var syncedAt = DateTime.Now;
            var nodes = await ReadNodesAsync(conn, FaHierarchySql.BuildNodeSelectSql(quotedListe, quotedInfos), syncedAt, ct);
            var infos = await ReadOrderInfosAsync(conn, FaHierarchySql.BuildInfosSelectSql(quotedInfos), syncedAt, ct);

            if (dryRun)
            {
                _logger.LogInformation(
                    "FaHierarchy-Sync [DryRun]: wuerde {Nodes} Knoten + {Infos} OrderInfos ersetzen.",
                    nodes.Count, infos.Count);
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["neu"] = nodes.Count + infos.Count,
                    ["geloescht"] = 0,
                }, messageSuffix: "[DryRun]", ct: ct);
                return new FaHierarchySyncResult(nodes.Count, infos.Count, 0, 0, Skipped: false, DryRun: true);
            }

            // 4) Full-Refresh: beide Zieltabellen in EINER Transaktion ersetzen (RCSI-Primaerweg).
            //    Nie steht FaHierarchyNode (neu) neben altem FaHierarchyOrderInfo oder umgekehrt.
            await using var tx = await _ctx.Database.BeginTransactionAsync(ct);

            var deletedNodes = await _ctx.Database.ExecuteSqlRawAsync("DELETE FROM dbo.FaHierarchyNodes", ct);
            var deletedInfos = await _ctx.Database.ExecuteSqlRawAsync("DELETE FROM dbo.FaHierarchyOrderInfos", ct);

            await _ctx.FaHierarchyNodes.AddRangeAsync(nodes, ct);
            await _ctx.FaHierarchyOrderInfos.AddRangeAsync(infos, ct);
            await _ctx.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "FaHierarchy-Sync: {Nodes} Knoten + {Infos} OrderInfos neu ({DelN}/{DelI} geloescht).",
                nodes.Count, infos.Count, deletedNodes, deletedInfos);

            var ambiguousCount = await LogAmbiguousHauptFasAsync(run, infos, ct);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["neu"] = nodes.Count + infos.Count,
                ["geloescht"] = deletedNodes + deletedInfos,
                ["hauptfa_mehrdeutig"] = ambiguousCount,
            }, ct: ct);

            return new FaHierarchySyncResult(nodes.Count, infos.Count, deletedNodes, deletedInfos, Skipped: false, DryRun: false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Unerwarteter Fehler: FinishFailedAsync + rethrow. Die Fehlermail schickt der
            // umgebende RunResilientAsync-Block im SyncWorker (mailt bei geworfener Exception).
            _logger.LogError(ex, "FaHierarchy-Sync fehlgeschlagen.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }

    /// <summary>
    /// Kombigeraete: derselbe HauptFA mit mehreren Montage-Abteilungen. Eine Eigenschaft der
    /// DATEN, nicht der Anzeige — deshalb einmal je Lauf gemeldet statt je Seitenaufruf
    /// (Rueckfrage 7). Die Kennzeichnung in der Oberflaeche bleibt davon unberuehrt.
    /// Internal statt private: <see cref="RunAsync"/> braucht eine echte SqlConnection nach
    /// Sage und ist deshalb ohne Live-DB nicht InMemory-testbar — die reine Meldelogik wird
    /// hier separat gehalten, damit sie ueber InternalsVisibleTo direkt getestet werden kann.
    /// </summary>
    internal static async Task<int> LogAmbiguousHauptFasAsync(
        ISyncRun run, IReadOnlyCollection<FaHierarchyOrderInfo> infos, CancellationToken ct)
    {
        var ambiguousHauptFas = infos.GroupBy(i => i.HauptFA)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .OrderBy(k => k)
            .ToList();
        if (ambiguousHauptFas.Count > 0)
            await run.LogWarningAsync(
                $"Kombigeraete: {ambiguousHauptFas.Count} HauptFA mit mehreren Auftragskoepfen " +
                $"({string.Join(", ", ambiguousHauptFas)}). Kopfdaten werden in der FA-Liste als " +
                "mehrdeutig gekennzeichnet, es wird keine Variante ausgewaehlt.", ct: ct);
        return ambiguousHauptFas.Count;
    }

    private static async Task<long> ScalarCountAsync(SqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
        var v = await cmd.ExecuteScalarAsync(ct);
        return v is null or DBNull ? 0L : Convert.ToInt64(v);
    }

    private static async Task<List<FaHierarchyNode>> ReadNodesAsync(
        SqlConnection conn, string sql, DateTime syncedAt, CancellationToken ct)
    {
        var result = new List<FaHierarchyNode>();
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
        await using var r = await cmd.ExecuteReaderAsync(ct);

        int oHauptFA = r.GetOrdinal("HauptFA"), oVaterFA = r.GetOrdinal("VaterFA"),
            oSubFA = r.GetOrdinal("SubFA"), oPosition = r.GetOrdinal("Position"),
            oHauptArtnr = r.GetOrdinal("HauptArtnr"), oArtnr = r.GetOrdinal("Artnr"),
            oMatchcode = r.GetOrdinal("Matchcode"), oBez1 = r.GetOrdinal("Bezeichnung1"),
            oBez2 = r.GetOrdinal("Bezeichnung2"), oSoll = r.GetOrdinal("Sollmenge"),
            oFert = r.GetOrdinal("Fertigungmenge"), oBesch = r.GetOrdinal("Beschaffungsartikel"),
            oArtGrp = r.GetOrdinal("Artikelgruppe"), oHlp = r.GetOrdinal("Hauptlagerplatz"),
            oBemPN = r.GetOrdinal("BemerkungPN"), oFInfoPN = r.GetOrdinal("FertigungsInfoPN"),
            oKomm = r.GetOrdinal("Kommissionieren"), oArbBer = r.GetOrdinal("Arbeitsbereich"),
            oArbSchr = r.GetOrdinal("Arbeitsschritte"), oArtTyp = r.GetOrdinal("Artikeltyp"),
            oBeschichtet = r.GetOrdinal("Beschichtet"), oMaterial = r.GetOrdinal("Material"),
            oBreite = r.GetOrdinal("Breite"), oHoehe = r.GetOrdinal("Hoehe"),
            oTiefe = r.GetOrdinal("Tiefe"), oEK = r.GetOrdinal("EKBedarf"),
            oVM = r.GetOrdinal("VMBedarf");

        while (await r.ReadAsync(ct))
        {
            result.Add(new FaHierarchyNode
            {
                HauptFA = Int32(r, oHauptFA),
                VaterFA = IntN(r, oVaterFA),
                SubFA = Int32(r, oSubFA),
                Position = IntN(r, oPosition),
                HauptArtnr = Str(r, oHauptArtnr),
                Artnr = Str(r, oArtnr),
                Matchcode = Str(r, oMatchcode),
                Bezeichnung1 = Str(r, oBez1),
                Bezeichnung2 = Str(r, oBez2),
                Sollmenge = Dec(r, oSoll),
                Fertigungmenge = Dec(r, oFert),
                Beschaffungsartikel = Bool(r, oBesch),
                Artikelgruppe = Str(r, oArtGrp),
                Hauptlagerplatz = Str(r, oHlp),
                BemerkungPN = Str(r, oBemPN),
                FertigungsInfoPN = Str(r, oFInfoPN),
                Kommissionieren = Str(r, oKomm),
                Arbeitsbereich = Str(r, oArbBer),
                Arbeitsschritte = Str(r, oArbSchr),
                Artikeltyp = Str(r, oArtTyp),
                Beschichtet = Bool(r, oBeschichtet),
                Material = Str(r, oMaterial),
                Breite = Dec(r, oBreite),
                Hoehe = Dec(r, oHoehe),
                Tiefe = Dec(r, oTiefe),
                EKBedarf = Bool(r, oEK),
                VMBedarf = Str(r, oVM),
                SyncedAt = syncedAt,
            });
        }
        return result;
    }

    private static async Task<List<FaHierarchyOrderInfo>> ReadOrderInfosAsync(
        SqlConnection conn, string sql, DateTime syncedAt, CancellationToken ct)
    {
        var result = new List<FaHierarchyOrderInfo>();
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
        await using var r = await cmd.ExecuteReaderAsync(ct);

        int oHauptFA = r.GetOrdinal("HauptFA"), oMontage = r.GetOrdinal("MontageAbteilung"),
            oABNr = r.GetOrdinal("ABNr"), oPos = r.GetOrdinal("Pos"), oKunde = r.GetOrdinal("Kunde"),
            oStatus = r.GetOrdinal("Status"), oDienst = r.GetOrdinal("Dienstleister"),
            oRAL = r.GetOrdinal("RAL"), oBemUhr = r.GetOrdinal("Bemerkung_Uhrzeit"),
            oKO = r.GetOrdinal("KO_Termin"), oFE = r.GetOrdinal("FE_Termin"),
            oStartB = r.GetOrdinal("Start_Beschichtung"), oRetour = r.GetOrdinal("Beschichten_Retour"),
            oNeuPT = r.GetOrdinal("Neuer_PT_PPS"), oVerlade = r.GetOrdinal("Verladetermin_Vsl"),
            oMStd = r.GetOrdinal("Montagestunden"), oPrio = r.GetOrdinal("Prio");

        while (await r.ReadAsync(ct))
        {
            result.Add(new FaHierarchyOrderInfo
            {
                HauptFA = Int32(r, oHauptFA),
                MontageAbteilung = Str(r, oMontage),
                ABNr = Str(r, oABNr),
                Pos = Str(r, oPos),
                Kunde = Str(r, oKunde),
                Status = Str(r, oStatus),
                Dienstleister = Str(r, oDienst),
                RAL = Str(r, oRAL),
                Bemerkung_Uhrzeit = Str(r, oBemUhr),
                KO_Termin = DateN(r, oKO),
                FE_Termin = DateN(r, oFE),
                Start_Beschichtung = DateN(r, oStartB),
                Beschichten_Retour = DateN(r, oRetour),
                Neuer_PT_PPS = DateN(r, oNeuPT),
                Verladetermin_Vsl = DateN(r, oVerlade),
                Montagestunden = DecN(r, oMStd),
                Prio = IntN(r, oPrio),
                SyncedAt = syncedAt,
            });
        }
        return result;
    }

    private static string? Str(SqlDataReader r, int o) => r.IsDBNull(o) ? null : Convert.ToString(r.GetValue(o));
    private static int Int32(SqlDataReader r, int o) => r.IsDBNull(o) ? 0 : Convert.ToInt32(r.GetValue(o));
    private static int? IntN(SqlDataReader r, int o) => r.IsDBNull(o) ? (int?)null : Convert.ToInt32(r.GetValue(o));
    private static decimal Dec(SqlDataReader r, int o) => r.IsDBNull(o) ? 0m : Convert.ToDecimal(r.GetValue(o));
    private static decimal? DecN(SqlDataReader r, int o) => r.IsDBNull(o) ? (decimal?)null : Convert.ToDecimal(r.GetValue(o));
    private static DateTime? DateN(SqlDataReader r, int o) => r.IsDBNull(o) ? (DateTime?)null : Convert.ToDateTime(r.GetValue(o));

    /// <summary>Sage-VB6-Boolean-Mapping: 'Ja'/'-1'/'1'/true =&gt; true; 'Nein'/'0'/NULL =&gt; false.</summary>
    private static bool Bool(SqlDataReader r, int o)
    {
        if (r.IsDBNull(o)) return false;
        var v = r.GetValue(o);
        if (v is bool b) return b;
        if (v is string s)
        {
            s = s.Trim();
            return s.Equals("Ja", StringComparison.OrdinalIgnoreCase)
                || s.Equals("true", StringComparison.OrdinalIgnoreCase)
                || s == "-1" || s == "1";
        }
        try { return Convert.ToDecimal(v) != 0m; } // -1 (VB6) und 1 (bit) => true, 0 => false
        catch { return false; }
    }
}
