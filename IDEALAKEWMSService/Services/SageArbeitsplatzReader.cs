using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Services;

public class SageArbeitsplatzReader : ISageArbeitsplatzReader
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SageArbeitsplatzReader> _logger;

    public SageArbeitsplatzReader(IConfiguration configuration, ILogger<SageArbeitsplatzReader> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SageArbeitsplatzReadResult> GetActiveAsync(string quotedSource, int mandant, CancellationToken ct = default)
    {
        var sageConnection = _configuration.GetConnectionString("SageConnection")
            ?? throw new InvalidOperationException("SageConnection nicht konfiguriert.");

        // Baustein a: Arbeitsplaetze. Arbeitsplatznummer bewusst typunabhaengig gelesen (GetValue().ToString(),
        // nie int.Parse — 0000 != 0); getrimmt wird im ProductionWorkplaceSyncService.
        var isTable = SageArbeitsplatzSql.IsTable(quotedSource);
        var sql = SageArbeitsplatzSql.Build(quotedSource);

        var result = new List<SageArbeitsplatzDto>();

        await using var conn = new SqlConnection(sageConnection);
        await conn.OpenAsync(ct);

        if (!await FaHierarchySql.SourceExistsAsync(conn, quotedSource, ct))
        {
            _logger.LogWarning("{Msg} — Werkbank-Anlage wird uebersprungen.", FaHierarchySql.SourceMissingMessage(quotedSource));
            return new SageArbeitsplatzReadResult(false, result);
        }
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
        if (isTable)
            cmd.Parameters.AddWithValue("@mandant", mandant);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            result.Add(new SageArbeitsplatzDto(
                Arbeitsplatznummer: reader.IsDBNull(0) ? null : reader.GetValue(0)?.ToString(),
                Bezeichnung1: reader.IsDBNull(1) ? null : reader.GetString(1),
                Arbeitsschritt: reader.IsDBNull(2) ? null : reader.GetString(2)
            ));
        }

        _logger.LogInformation(isTable
                ? "Sage liefert {Count} aktive Arbeitsplaetze aus {Source} (Mandant {Mandant})."
                : "Sage liefert {Count} Arbeitsplaetze aus der View {Source} (Mandant-Einstellung {Mandant} wirkt hier nicht).",
            result.Count, quotedSource, mandant);
        return new SageArbeitsplatzReadResult(true, result);
    }
}

/// <summary>
/// Reiner, unit-testbarer SQL-Baustein des Arbeitsplatz-Reads (Spec 2026-10-14-ideal-sage-views-umstellung, v1.60.2).
/// Zwei Lesewege nach Quelle: die Sage-Tabelle <c>KHKPpsArbeitsplaetze</c> (Default) mit dem bisherigen SQL inkl.
/// Mandant/Aktiv-Filter, jede andere Quelle als WMS-View <c>vw_IDEAL_AKE_WMS_Arbeitsgaenge</c> mit deren Spalten
/// (sys.columns, Gerald 2026-10-15: Arbeitsgangnummer, Bezeichnung, AG — kein Mandant/Aktiv; die View liefert bereits die
/// aktiven Arbeitsplaetze des Mandanten, Pruefabfrage P3b). Beide Wege fuellen dieselben DTO-Felder in derselben
/// Spaltenreihenfolge.
/// </summary>
public static class SageArbeitsplatzSql
{
    public const string TableName = "KHKPpsArbeitsplaetze";

    /// <summary>Bisheriger Select auf die Tabelle, zeichengleich mit dem Stand vor v1.60.0 (unqualifiziert, Default-Schema).</summary>
    public const string TableSql = """
        SELECT Arbeitsplatznummer, Bezeichnung1, USER_ArbeitsSchritt
        FROM KHKPpsArbeitsplaetze
        WHERE Mandant = @mandant
          AND Aktiv = -1
        """;

    /// <summary>
    /// Tabelle oder View? Entscheidet der Objektname (letztes Segment, ohne Klammern, Gross/Klein egal), nicht das Schema:
    /// <c>KHKPpsArbeitsplaetze</c>, <c>[KHKPpsArbeitsplaetze]</c>, <c>[dbo].[KHKPpsArbeitsplaetze]</c> = Tabelle. Der Name ist
    /// das einzige, was der Sync sicher weiss (die Spalten der Quelle kennt er nicht); eine Spaltenpruefung per sys.columns
    /// waere ein weiterer Roundtrip fuer einen Fall, den genau ein Name beschreibt.
    /// </summary>
    public static bool IsTable(string quotedSource)
    {
        var last = quotedSource.Split('.')[^1].Trim('[', ']');
        return string.Equals(last, TableName, StringComparison.OrdinalIgnoreCase);
    }

    public static string Build(string quotedSource) => IsTable(quotedSource) ? TableSql : $"""
        SELECT Arbeitsgangnummer, Bezeichnung, AG
        FROM {quotedSource}
        """;
}
