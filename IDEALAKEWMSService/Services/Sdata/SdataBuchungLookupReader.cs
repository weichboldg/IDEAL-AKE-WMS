using IdealAkeWms.Services.SageSdata;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>Zeile aus <c>LagerplatzbuchungenInfo</c> (nur der Schluessel; Name exakt wie in der SData-Antwort).</summary>
public sealed record SdataBuchungZeile(int? BuchungsID);

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E7/F9): Idempotenz-Lookup ueber SData statt Direkt-SQL —
/// <c>LagerplatzbuchungenInfo where Memo like '%SM#&lt;id&gt;#%'</c> (Vertrag CommonWawiServicesResources, nur GET),
/// Muster <see cref="SageBookingCorrelation.LikePattern"/> wie <see cref="SageBuchungLookupReader"/>.
/// <b>Wirft bei jedem Fehler</b> (<see cref="SdataLeseException"/>, fehlende Konfiguration), liefert nie <c>false</c> statt
/// Fehler: der <c>SageBookingWorker</c> sendet bei geworfenem Lookup bewusst nicht neu — ein „nicht gebucht“ wegen eines
/// HTTP-Fehlers wuerde eine Doppelbuchung ausloesen.
/// </summary>
public sealed class SdataBuchungLookupReader : ISageBuchungLookupReader
{
    public const string Ressource = "LagerplatzbuchungenInfo";
    public const string Select = "BuchungsID";
    public const string OrderBy = "BuchungsID";

    private readonly ISageSdataReadClient _client;
    private readonly IConfiguration _configuration;

    public SdataBuchungLookupReader(ISageSdataReadClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
    }

    // TODO(UAT E7): Am AKE-Test belegt ist nur ein Praefix-like (`Memo like 'IdealAkeWms%'`). Ungeprueft: fuehrendes `%` und das von
    // SageSdataReadClient.BuildQueryUrl als %23 kodierte `#` (Sage muss es zu `#` dekodieren). Pruefung: TS-106.9 Schritt „Lookup SData“
    // mit einer gebuchten Bewegung muss `true` liefern; liefert er `false`, Schalter zurueck auf View (Doppelbuchungsgefahr bei Requeue).
    public static string Where(string marker) => $"Memo like '{SageBookingCorrelation.LikePattern(marker)}'";

    public async Task<bool> ExistsAsync(string marker, CancellationToken ct = default) =>
        await ExistsAsync(await SdataLeseZielLeser.LesenAsync(_configuration, ct), marker, ct);

    public async Task<bool> ExistsAsync(SdataLeseZiel ziel, string marker, CancellationToken ct)
    {
        // count=1: eine Seite reicht fuer "gibt es"; QueryAllAsync liest bis $totalResults (eine IU-Umbuchung = wenige Zeilen).
        var treffer = await _client.QueryAllAsync<SdataBuchungZeile>(ziel, SageSdataReadClient.VertragResources, Ressource,
            Where(marker), Select, OrderBy, ct, count: 1);
        return treffer.Zeilen.Count > 0;
    }
}
