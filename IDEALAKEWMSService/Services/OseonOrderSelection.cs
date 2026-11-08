namespace IDEALAKEWMSService.Services;

/// <summary>
/// Reiner Entscheidungs-Helper fuer den OSEON-Tracking-Sync (Hotfix v1.30.3): je Auftragsnummer
/// genau eine OSEON-Auftragszeile, AGs nur aus dieser Zeile, je (Nummer, Position) einmal.
/// Hintergrund: OSEON schreibt Auftraege neu aus (gleiche AuftragsNr, neue ID); eindeutig im WMS ist
/// die Auftragsnummer (UNIQUE-Index), nicht die OSEON-ID.
/// </summary>
internal static class OseonOrderSelection
{
    internal sealed record Multiple(string OrderNumber, IReadOnlyList<long> OseonIds, long SelectedOseonId);

    internal sealed record Result(
        IReadOnlyList<OseonRawRow> Orders,
        IReadOnlyList<OseonRawRow> Operations,
        IReadOnlyList<Multiple> Multiples);

    public static Result Select(IReadOnlyList<OseonRawRow> rows)
    {
        // OrdinalIgnoreCase: die DB-Kollation ist CI (fallstricke "Gross/Klein in SQL macht die Kollation").
        // Restfaelle (z.B. Zeichen mit Gewicht 0) faengt der SQL-Waechter im MERGE ab.
        var groups = rows
            .Where(r => !string.IsNullOrEmpty(r.OseonOrderNumber))
            .GroupBy(r => r.OseonOrderNumber!, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var orders = new List<OseonRawRow>(groups.Count);
        var operations = new List<OseonRawRow>();
        var multiples = new List<Multiple>();

        foreach (var g in groups)
        {
            // Hoechste OSEON-ID gewinnt (Freigabe-Antwort 1): eine Neuausschreibung ist ein neuer Datensatz.
            var selectedId = g.Max(r => r.OseonId);
            var selected = g.First(r => r.OseonId == selectedId);
            orders.Add(selected);

            var ids = g.Select(r => r.OseonId).Distinct().OrderBy(id => id).ToList();
            if (ids.Count > 1)
                multiples.Add(new Multiple(selected.OseonOrderNumber!, ids, selectedId));

            operations.AddRange(g
                .Where(r => r.OseonId == selectedId && !string.IsNullOrEmpty(r.PositionNumber))
                .GroupBy(r => r.PositionNumber!, StringComparer.OrdinalIgnoreCase)
                .Select(pg => pg
                    .OrderByDescending(r => r.LastStatusReport ?? DateTime.MinValue)
                    .ThenByDescending(r => r.AgaStatus)
                    .First()));
        }

        return new Result(orders, operations, multiples);
    }
}

internal sealed class OseonRawRow
{
    public long OseonId { get; set; }
    public string? CustomerOrderNumber { get; set; }
    public string? OseonOrderNumber { get; set; }
    public int PaStatus { get; set; }
    public string? PositionNumber { get; set; }
    public string? ActivityName { get; set; }
    public string? ActivityDescription { get; set; }
    public int AgaStatus { get; set; }
    public bool IsFirstAga { get; set; }
    public bool IsLastAga { get; set; }
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public string? Description2 { get; set; }
    public string? WorkplaceName { get; set; }
    public decimal QuantityTarget { get; set; }
    public decimal QuantityActual { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? DateOfLastChange { get; set; }
    public DateTime? LastStatusReport { get; set; }
}
