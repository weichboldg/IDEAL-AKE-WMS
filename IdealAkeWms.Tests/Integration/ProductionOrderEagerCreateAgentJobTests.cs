using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace IdealAkeWms.Tests.Integration;

/// <summary>
/// Spec 11.3 / Plan Task 8 Step 7 (Phase 1) — angepasst in v1.22.0.
///
/// Verifiziert die Eager-Create-Folge-MERGEs aus
/// <c>SQL/AgentJobs/01_Import_Produktionsauftraege.sql</c>: nach Lauf gilt fuer
/// jeden ProductionOrder genau 1 PickingStatus, 1 BdeStatus. Re-Run ist
/// idempotent (keine Duplikate).
///
/// Der fruehere dritte MERGE (ProductionOrderAssemblyGroups, 5 Zeilen VK/VL/VE/VT/VA)
/// wurde in v1.22.0 ERSATZLOS entfernt — FaWorkSteps entstehen nur noch via
/// Detection-Sync oder manuell (Spec 2026-06-12, Migration FaWorkStepsAndAttributes).
///
/// Marked <see cref="Xunit.TraitAttribute"/> "Category" = "SqlServerOnly" — InMemory-DB
/// unterstuetzt kein <c>MERGE</c>. Lokal mit echter Stage-DB ausfuehren via
/// <c>dotnet test --filter "Category=SqlServerOnly"</c> + Environment-Variable
/// <c>WMS_STAGE_CONN</c> (Connection-String zur Stage-DB).
///
/// In CI per Default ausgeschlossen (Filter <c>Category!=SqlServerOnly</c>).
/// </summary>
[Trait("Category", "SqlServerOnly")]
public class ProductionOrderEagerCreateAgentJobTests
{
    private const string EnvVarName = "WMS_STAGE_CONN";

    private const string TestOrderNumber = "FA-TEST-EAGERCREATE-001";

    /// <summary>
    /// Die 2 Folge-MERGEs aus <c>SQL/AgentJobs/01_Import_Produktionsauftraege.sql</c>.
    /// Auch hier embedded — schaffen sonst eine Pfad-Abhaengigkeit zum SQL-Folder.
    /// </summary>
    private const string EagerCreateMerges = @"
MERGE [dbo].[ProductionOrderPickingStatus] AS s
USING (SELECT Id FROM [dbo].[ProductionOrders]) AS src
    ON s.ProductionOrderId = src.Id
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ProductionOrderId, IsReleasedForPicking,
            HasGlass, HasExternalPurchase, HasCoatingParts, IsCoatingDone, IsDonePicking,
            CreatedAt, CreatedBy, CreatedByWindows)
    VALUES (src.Id, 0, 0, 0, 0, 0, 0,
            GETDATE(), 'Sage_Schnittstelle', SYSTEM_USER);

MERGE [dbo].[ProductionOrderBdeStatus] AS s
USING (SELECT Id FROM [dbo].[ProductionOrders]) AS src
    ON s.ProductionOrderId = src.Id
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ProductionOrderId, IsDoneBde, CreatedAt, CreatedBy, CreatedByWindows)
    VALUES (src.Id, 0, GETDATE(), 'Sage_Schnittstelle', SYSTEM_USER);
";

    [SkippableFact]
    public async Task EagerCreate_TwoMergesProduce2Rows_PerNewOrder_AndAreIdempotent()
    {
        var connStr = Environment.GetEnvironmentVariable(EnvVarName);
        Skip.If(string.IsNullOrWhiteSpace(connStr),
            $"Stage DB not configured (env var '{EnvVarName}' is not set). " +
            "Set it to a connection string targeting a disposable Stage-DB to run this test.");

        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        try
        {
            // 1. Cleanup: drop any leftover test row
            await ExecAsync(conn, $@"
                DELETE FROM dbo.ProductionOrderBdeStatus
                WHERE ProductionOrderId IN (SELECT Id FROM dbo.ProductionOrders WHERE OrderNumber = @num);
                DELETE FROM dbo.ProductionOrderPickingStatus
                WHERE ProductionOrderId IN (SELECT Id FROM dbo.ProductionOrders WHERE OrderNumber = @num);
                DELETE FROM dbo.ProductionOrders WHERE OrderNumber = @num;",
                ("@num", TestOrderNumber));

            // 2. Insert a fresh FA
            await ExecAsync(conn, @"
                INSERT INTO dbo.ProductionOrders
                    (OrderNumber, Quantity, IsDone, CreatedAt, CreatedBy, CreatedByWindows)
                VALUES
                    (@num, 1, 0, GETDATE(), 'integration-test', SYSTEM_USER);",
                ("@num", TestOrderNumber));

            var poId = await ScalarLongAsync(conn,
                "SELECT Id FROM dbo.ProductionOrders WHERE OrderNumber = @num",
                ("@num", TestOrderNumber));
            poId.Should().BeGreaterThan(0, "the test FA must have been inserted");

            // 3. First run of the 2 Eager-Create-MERGEs
            await ExecAsync(conn, EagerCreateMerges);

            // Assert: 1×PS, 1×BDE
            (await CountAsync(conn,
                "SELECT COUNT(*) FROM dbo.ProductionOrderPickingStatus WHERE ProductionOrderId = @id",
                ("@id", (int)poId))).Should().Be(1);
            (await CountAsync(conn,
                "SELECT COUNT(*) FROM dbo.ProductionOrderBdeStatus WHERE ProductionOrderId = @id",
                ("@id", (int)poId))).Should().Be(1);

            // 4. Re-run: idempotent — no duplicates
            await ExecAsync(conn, EagerCreateMerges);

            (await CountAsync(conn,
                "SELECT COUNT(*) FROM dbo.ProductionOrderPickingStatus WHERE ProductionOrderId = @id",
                ("@id", (int)poId))).Should().Be(1);
            (await CountAsync(conn,
                "SELECT COUNT(*) FROM dbo.ProductionOrderBdeStatus WHERE ProductionOrderId = @id",
                ("@id", (int)poId))).Should().Be(1);
        }
        finally
        {
            // Final cleanup — leave the Stage-DB tidy regardless of test outcome
            await ExecAsync(conn, $@"
                DELETE FROM dbo.ProductionOrderBdeStatus
                WHERE ProductionOrderId IN (SELECT Id FROM dbo.ProductionOrders WHERE OrderNumber = @num);
                DELETE FROM dbo.ProductionOrderPickingStatus
                WHERE ProductionOrderId IN (SELECT Id FROM dbo.ProductionOrders WHERE OrderNumber = @num);
                DELETE FROM dbo.ProductionOrders WHERE OrderNumber = @num;",
                ("@num", TestOrderNumber));
        }
    }

    private static async Task ExecAsync(SqlConnection conn, string sql, params (string Name, object Value)[] parameters)
    {
        await using var cmd = new SqlCommand(sql, conn);
        foreach (var (n, v) in parameters)
            cmd.Parameters.AddWithValue(n, v);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<long> ScalarLongAsync(SqlConnection conn, string sql, params (string Name, object Value)[] parameters)
    {
        await using var cmd = new SqlCommand(sql, conn);
        foreach (var (n, v) in parameters)
            cmd.Parameters.AddWithValue(n, v);
        var result = await cmd.ExecuteScalarAsync();
        if (result == null || result is DBNull) return 0;
        return Convert.ToInt64(result);
    }

    private static async Task<int> CountAsync(SqlConnection conn, string sql, params (string Name, object Value)[] parameters)
    {
        await using var cmd = new SqlCommand(sql, conn);
        foreach (var (n, v) in parameters)
            cmd.Parameters.AddWithValue(n, v);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }
}
