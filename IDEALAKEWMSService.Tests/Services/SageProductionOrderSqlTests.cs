using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using IDEALAKEWMSService.Services;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class SageProductionOrderSqlTests
{
    // Extrahiert den Inhalt der ersten Klammer nach dem gegebenen Anker (z.B. "INSERT INTO ... (")
    // und liefert die durch Komma getrennten Top-Level-Elemente. Nested-Parens (z.B. GETUTCDATE())
    // werden korrekt behandelt: die schliessende Klammer wird per Tiefen-Tracking bestimmt und
    // Kommas innerhalb von Nested-Parens NICHT als Trenner gewertet.
    private static string[] ExtractParenList(string sql, string anchor)
    {
        var anchorIdx = sql.IndexOf(anchor, StringComparison.Ordinal);
        anchorIdx.Should().BeGreaterThanOrEqualTo(0, $"Anker '{anchor}' muss im SQL vorkommen");
        var open = sql.IndexOf('(', anchorIdx);
        open.Should().BeGreaterThanOrEqualTo(0);

        // Passende schliessende Klammer per Tiefen-Tracking finden.
        int depth = 0, close = -1;
        for (int i = open; i < sql.Length; i++)
        {
            if (sql[i] == '(') depth++;
            else if (sql[i] == ')')
            {
                depth--;
                if (depth == 0) { close = i; break; }
            }
        }
        close.Should().BeGreaterThan(open, "passende schliessende Klammer muss gefunden werden");

        var inner = sql.Substring(open + 1, close - open - 1);

        // Nur auf Top-Level-Kommas splitten (Tiefe 0), damit GETUTCDATE() ein Element bleibt.
        var items = new List<string>();
        var current = new StringBuilder();
        depth = 0;
        foreach (var c in inner)
        {
            if (c == '(') { depth++; current.Append(c); }
            else if (c == ')') { depth--; current.Append(c); }
            else if (c == ',' && depth == 0)
            {
                items.Add(current.ToString().Trim());
                current.Clear();
            }
            else current.Append(c);
        }
        if (current.ToString().Trim().Length > 0)
            items.Add(current.ToString().Trim());

        return items.Where(s => s.Length > 0).ToArray();
    }

    private static string InsertClause(string sql)
    {
        // Alles ab "INSERT INTO" bis "SELECT SCOPE_IDENTITY" — der reine Insert-Zweig.
        var start = sql.IndexOf("INSERT INTO", StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);
        var end = sql.IndexOf("SELECT SCOPE_IDENTITY", start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start);
        return sql.Substring(start, end - start);
    }

    private static string UpdateClause(string sql)
    {
        // Alles ab "UPDATE [dbo].[ProductionOrders]" bis "SELECT NULL AS InsertedId".
        var start = sql.IndexOf("UPDATE [dbo].[ProductionOrders]", StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);
        var end = sql.IndexOf("SELECT NULL AS InsertedId", start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start);
        return sql.Substring(start, end - start);
    }

    [Fact]
    public void BuildUpsert_False_InsertOhneSubOrderNumber_SpaltenGleichWerte()
    {
        var sql = SageProductionOrderSql.BuildUpsert(false);
        var insert = InsertClause(sql);

        insert.Should().NotContain("[SubOrderNumber]");

        var cols = ExtractParenList(insert, "INSERT INTO");
        var valsAnchor = insert.IndexOf("VALUES", StringComparison.Ordinal);
        valsAnchor.Should().BeGreaterThan(0);
        var vals = ExtractParenList(insert.Substring(valsAnchor), "VALUES");

        cols.Should().HaveCount(12);
        vals.Should().HaveCount(cols.Length, "Insert-Spalten- und Werte-Anzahl muessen uebereinstimmen");
        // @OrderNumber darf nur einmal in den VALUES vorkommen (ohne SubOrderNumber).
        vals.Count(v => v == "@OrderNumber").Should().Be(1);
    }

    [Fact]
    public void BuildUpsert_True_InsertMitSubOrderNumber_OrderNumberDoppelt_SpaltenGleichWerte()
    {
        var sql = SageProductionOrderSql.BuildUpsert(true);
        var insert = InsertClause(sql);

        var cols = ExtractParenList(insert, "INSERT INTO");
        cols.Should().Contain("[SubOrderNumber]");
        cols[0].Should().Be("[OrderNumber]");
        cols[1].Should().Be("[SubOrderNumber]");

        var valsAnchor = insert.IndexOf("VALUES", StringComparison.Ordinal);
        valsAnchor.Should().BeGreaterThan(0);
        var vals = ExtractParenList(insert.Substring(valsAnchor), "VALUES");

        // Genau EINE Spalte mehr als der false-Fall (die zusaetzliche SubOrderNumber).
        var falseCols = ExtractParenList(InsertClause(SageProductionOrderSql.BuildUpsert(false)), "INSERT INTO");
        cols.Should().HaveCount(falseCols.Length + 1);
        vals.Should().HaveCount(cols.Length, "Insert-Spalten- und Werte-Anzahl muessen uebereinstimmen");
        // @OrderNumber wird fuer OrderNumber UND SubOrderNumber verwendet -> genau zweimal.
        vals.Count(v => v == "@OrderNumber").Should().Be(2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BuildUpsert_UpdateZweigLaesstSubOrderNumberUnangetastet(bool includeSubOrderNumber)
    {
        var sql = SageProductionOrderSql.BuildUpsert(includeSubOrderNumber);
        var update = UpdateClause(sql);

        update.Should().NotContain("[SubOrderNumber]");
        update.Should().NotContain("SubOrderNumber");
    }
}
