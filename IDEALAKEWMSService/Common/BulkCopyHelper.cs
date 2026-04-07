using System.Data;
using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Common;

/// <summary>
/// Thin wrappers around the SqlBulkCopy + MERGE pattern used by sync services.
/// Handles boilerplate (column mappings, timeouts, OUTPUT $action parsing)
/// without imposing a specific table schema.
/// </summary>
public static class BulkCopyHelper
{
    /// <summary>
    /// Streams a DataTable into a temp table via SqlBulkCopy with explicit column mappings.
    /// </summary>
    public static async Task BulkCopyToTempTableAsync(
        SqlConnection conn,
        string tempTableName,
        DataTable data,
        Dictionary<string, string> columnMappings,
        int timeoutSeconds,
        CancellationToken ct)
    {
        using var bulkCopy = new SqlBulkCopy(conn)
        {
            DestinationTableName = tempTableName,
            BulkCopyTimeout = timeoutSeconds
        };
        foreach (var (src, dest) in columnMappings)
            bulkCopy.ColumnMappings.Add(src, dest);
        await bulkCopy.WriteToServerAsync(data, ct);
    }

    /// <summary>
    /// Executes a MERGE (or other DML) statement that emits OUTPUT $action and
    /// returns counts of inserted vs updated rows.
    /// </summary>
    public static async Task<(int Inserted, int Updated)> ExecuteMergeWithOutputAsync(
        SqlConnection conn,
        string mergeSql,
        int timeoutSeconds,
        CancellationToken ct)
    {
        int inserted = 0, updated = 0;
        await using var cmd = new SqlCommand(mergeSql, conn) { CommandTimeout = timeoutSeconds };
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var action = reader.GetString(0);
            if (action == "INSERT") inserted++;
            else if (action == "UPDATE") updated++;
        }
        return (inserted, updated);
    }
}
