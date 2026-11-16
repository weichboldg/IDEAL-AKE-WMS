using IdealAkeWms.Models;
using IdealAkeWms.Services.Beschichtung;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace IDEALAKEWMSService.Common;

/// <summary>
/// Master-Read <c>ProduktionsauftragHierarchisch</c> fuer die Klasse-D-Gates (BOM-Bridge, Spec Design H).
/// Kapselt <see cref="ServiceSettings.GetBoolSafeAsync"/> (DB-first, ADR 0008), damit die Gates ohne
/// SQL Server testbar sind (Tests injizieren einen Stub). Safe-Reader: DB-Hickup → false (= AKE-Verhalten).
/// </summary>
public interface IHierarchicalModeReader
{
    Task<bool> IsHierarchicalAsync(CancellationToken ct);

    /// <summary>v1.99.25 (Spec 2026-11-16-beschichtungslogik-je-standort E3): Master + AppSettings <c>BeschichtungKennzeichenQuelle</c>/
    /// <c>BeschichtungTerminQuelle</c>, aufgeloest ueber <see cref="BeschichtungResolver.ResolveQuellen"/> — die EINE Stelle, an der der
    /// Dienst zwischen Kategorie- und Struktur-Weg entscheidet. Safe wie der Master: DB-Fehler = Werte fehlen = „Automatisch“.</summary>
    Task<BeschichtungQuellen> GetBeschichtungQuellenAsync(CancellationToken ct);
}

public sealed class HierarchicalModeReader : IHierarchicalModeReader
{
    private readonly IConfiguration _configuration;
    public HierarchicalModeReader(IConfiguration configuration) => _configuration = configuration;

    public Task<bool> IsHierarchicalAsync(CancellationToken ct) =>
        ServiceSettings.GetBoolSafeAsync(_configuration, "ProduktionsauftragHierarchisch", false, ct);

    public async Task<BeschichtungQuellen> GetBeschichtungQuellenAsync(CancellationToken ct)
    {
        var hierarchisch = await IsHierarchicalAsync(ct);
        string? kennzeichen = null, termin = null;
        try
        {
            await using var conn = new SqlConnection(ConnectionStrings.Wms(_configuration));
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand("SELECT [Key], [Value] FROM [dbo].[AppSettings] WHERE [Key] IN (@k, @t)", conn);
            cmd.Parameters.AddWithValue("@k", AppSettingKeys.BeschichtungKennzeichenQuelle);
            cmd.Parameters.AddWithValue("@t", AppSettingKeys.BeschichtungTerminQuelle);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var value = r.IsDBNull(1) ? null : r.GetString(1);
                if (r.GetString(0) == AppSettingKeys.BeschichtungKennzeichenQuelle) kennzeichen = value; else termin = value;
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { /* wie GetBoolSafeAsync: DB-Hickup = Automatisch */ }
        return BeschichtungResolver.ResolveQuellen(kennzeichen, termin, hierarchisch);
    }
}
