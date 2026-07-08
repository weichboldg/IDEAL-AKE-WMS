using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace IDEALAKEWMSService.Common;

/// <summary>
/// Reads service runtime settings from the WMS [ServiceSettings] table.
/// Each call hits the DB — settings are intentionally NOT cached so that
/// changes via the Web GUI take effect immediately on the next sync run.
/// </summary>
public static class ServiceSettings
{
    /// <summary>
    /// Reads a single setting value. Returns null when the key does not exist.
    /// </summary>
    public static async Task<string?> GetValueAsync(IConfiguration config, string key, CancellationToken ct = default)
    {
        var connStr = ConnectionStrings.Wms(config);
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(
            "SELECT [Value] FROM [dbo].[ServiceSettings] WHERE [Key] = @key", conn);
        cmd.Parameters.AddWithValue("@key", key);
        var v = await cmd.ExecuteScalarAsync(ct);
        return v as string;
    }

    public static async Task<bool> GetBoolAsync(IConfiguration config, string key, bool defaultValue, CancellationToken ct = default)
    {
        var v = await GetValueAsync(config, key, ct);
        if (string.IsNullOrWhiteSpace(v)) return defaultValue;
        return string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<int> GetIntAsync(IConfiguration config, string key, int defaultValue, CancellationToken ct = default)
    {
        var v = await GetValueAsync(config, key, ct);
        if (int.TryParse(v, out var i)) return i;
        return defaultValue;
    }

    /// <summary>
    /// Wie <see cref="GetBoolAsync"/>, faengt aber transiente DB-Fehler ab und
    /// liefert dann den Default (statt zu werfen). Fuer Takt-relevante Reads,
    /// die den Worker-Loop nicht abwuergen duerfen.
    /// </summary>
    public static async Task<bool> GetBoolSafeAsync(IConfiguration config, string key, bool defaultValue, CancellationToken ct = default)
    {
        try { return await GetBoolAsync(config, key, defaultValue, ct); }
        catch (OperationCanceledException) { throw; }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Wie <see cref="GetIntAsync"/>, faengt aber transiente DB-Fehler ab und
    /// liefert dann den Default (statt zu werfen).
    /// </summary>
    public static async Task<int> GetIntSafeAsync(IConfiguration config, string key, int defaultValue, CancellationToken ct = default)
    {
        try { return await GetIntAsync(config, key, defaultValue, ct); }
        catch (OperationCanceledException) { throw; }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Wie <see cref="GetValueAsync"/>, faengt aber transiente DB-Fehler ab und
    /// liefert dann null (statt zu werfen).
    /// </summary>
    public static async Task<string?> GetValueSafeAsync(IConfiguration config, string key, CancellationToken ct = default)
    {
        try { return await GetValueAsync(config, key, ct); }
        catch (OperationCanceledException) { throw; }
        catch { return null; }
    }
}
