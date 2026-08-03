using IDEALAKEWMSService.Common;
using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Services;

public class SageBuchungLookupReader : ISageBuchungLookupReader
{
    private readonly IConfiguration _configuration;

    // DEV-LAUF / MANUAL-UAT: Die Korrelationsspalte ist am Sage-Testsystem zu bestaetigen — das
    // Postman-Sample dokumentiert das Antwortformat des LagerbuchungService nicht. Angenommen wird
    // die frei setzbare Spalte [Memo] in [KHKLagerplatzbuchungen]. Traegt Sage die Korrelation statt
    // dessen in [Referenz], hier auf Referenz umstellen (nur diese eine Zeile).
    private const string LookupSql =
        "SELECT TOP 1 1 FROM KHKLagerplatzbuchungen WHERE Memo LIKE @pattern";

    public SageBuchungLookupReader(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> ExistsAsync(int stockMovementId, CancellationToken ct = default)
    {
        var sageConnection = ConnectionStrings.Sage(_configuration);

        await using var conn = new SqlConnection(sageConnection);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(LookupSql, conn) { CommandTimeout = 30 };
        cmd.Parameters.AddWithValue("@pattern", SageBookingCorrelation.LikePattern(stockMovementId));

        var result = await cmd.ExecuteScalarAsync(ct);
        return result != null && result != DBNull.Value;
    }
}
