using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace IdealAkeWms.Tests.Integration;

/// <summary>
/// Spec 11.3 / Plan Task 8 Step 7.
///
/// Verifiziert die Eager-Create-Folge-MERGEs aus
/// <c>SQL/AgentJobs/01_Import_Produktionsauftraege.sql</c>: nach Lauf gilt fuer
/// jeden ProductionOrder genau 1 PickingStatus, 1 BdeStatus, 5 AssemblyGroups
/// (VK/VL/VE/VT/VA). Re-Run ist idempotent (keine Duplikate).
///
/// Marked <see cref="Xunit.TraitAttribute"/> "Category" = "SqlServerOnly" — InMemory-DB
/// unterstuetzt kein <c>MERGE</c>. Lokal mit echter Stage-DB ausfuehren via
/// <c>dotnet test --filter "Category=SqlServerOnly"</c> + Environment-Variable
/// <c>WMS_STAGE_CONN</c> (Connection-String zur Stage-DB).
///
/// In CI per Default ausgeschlossen (Filter <c>Category!=SqlServerOnly</c>).
/// Plan Step 17 "Offene Entscheidung": dieser Test deckt 12.6 (AgentJob-Idempotenz-Bug)
/// + 12.9 (InMemory-DB-Coverage-Luecke fuer MERGE) ab.
/// </summary>
[Trait("Category", "SqlServerOnly")]
public class ProductionOrderEagerCreateAgentJobTests
{
    private const string EnvVarName = "WMS_STAGE_CONN";

    private const string TestOrderNumber = "FA-TEST-EAGERCREATE-001";

    /// <summary>
    /// Die 3 Folge-MERGEs aus <c>SQL/AgentJobs/01_Import_Produktionsauftraege.sql</c>.
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

MERGE [dbo].[ProductionOrderAssemblyGroups] AS s
USING (
    SELECT p.Id AS ProductionOrderId, k.GroupKey
    FROM [dbo].[ProductionOrders] p
    CROSS JOIN (VALUES ('VK'),('VL'),('VE'),('VT'),('VA')) k(GroupKey)
) AS src
    ON s.ProductionOrderId = src.ProductionOrderId AND s.GroupKey = src.GroupKey
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ProductionOrderId, GroupKey, IsApplicable, IsCompleted,
            CreatedAt, CreatedBy, CreatedByWindows)
    VALUES (src.ProductionOrderId, src.GroupKey, 0, 0,
            GETDATE(), 'Sage_Schnittstelle', SYSTEM_USER);
";

    [SkippableFact]
    public async Task EagerCreate_ThreeMergesProduce7Rows_PerNewOrder_AndAreIdempotent()
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
                DELETE FROM dbo.ProductionOrderAssemblyGroups
                WHERE ProductionOrderId IN (SELECT Id FROM dbo.ProductionOrders WHERE OrderNumber = @num);
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

            // 3. First run of the 3 Eager-Create-MERGEs
            await ExecAsync(conn, EagerCreateMerges);

            // Assert: 1×PS, 1×BDE, 5×AG (VK/VL/VE/VT/VA, all IsApplicable=0)
            (await CountAsync(conn,
                "SELECT COUNT(*) FROM dbo.ProductionOrderPickingStatus WHERE ProductionOrderId = @id",
                ("@id", (int)poId))).Should().Be(1);
            (await CountAsync(conn,
                "SELECT COUNT(*) FROM dbo.ProductionOrderBdeStatus WHERE ProductionOrderId = @id",
                ("@id", (int)poId))).Should().Be(1);
            (await CountAsync(conn,
                "SELECT COUNT(*) FROM dbo.ProductionOrderAssemblyGroups WHERE ProductionOrderId = @id",
                ("@id", (int)poId))).Should().Be(5);
            (await CountAsync(conn,
                "SELECT COUNT(*) FROM dbo.ProductionOrderAssemblyGroups " +
                "WHERE ProductionOrderId = @id AND IsApplicable = 0",
                ("@id", (int)poId))).Should().Be(5,
                "all 5 groups must default to IsApplicable=0");
            (await CountAsync(conn,
                "SELECT COUNT(DISTINCT GroupKey) FROM dbo.ProductionOrderAssemblyGroups " +
                "WHERE ProductionOrderId = @id AND GroupKey IN ('VK','VL','VE','VT','VA')",
                ("@id", (int)poId))).Should().Be(5);

            // 4. Re-run: idempotent — no duplicates
            await ExecAsync(conn, EagerCreateMerges);

            (await CountAsync(conn,
                "SELECT COUNT(*) FROM dbo.ProductionOrderPickingStatus WHERE ProductionOrderId = @id",
                ("@id", (int)poId))).Should().Be(1);
            (await CountAsync(conn,
                "SELECT COUNT(*) FROM dbo.ProductionOrderBdeStatus WHERE ProductionOrderId = @id",
                ("@id", (int)poId))).Should().Be(1);
            (await CountAsync(conn,
                "SELECT COUNT(*) FROM dbo.ProductionOrderAssemblyGroups WHERE ProductionOrderId = @id",
                ("@id", (int)poId))).Should().Be(5);
        }
        finally
        {
            // Final cleanup — leave the Stage-DB tidy regardless of test outcome
            await ExecAsync(conn, $@"
                DELETE FROM dbo.ProductionOrderAssemblyGroups
                WHERE ProductionOrderId IN (SELECT Id FROM dbo.ProductionOrders WHERE OrderNumber = @num);
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
