using System.Text.RegularExpressions;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SageSdata;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>Zeile aus <c>EinkaufBelegeInfo</c> (Namen exakt wie in der SData-Antwort, P-W9 (b)).</summary>
public sealed record SageWeBelegTreffer(int BelID, int? Belegjahr, int? Belegnummer, string? Memo)
{
    /// <summary>Wie <c>BelegnummerFormatiert</c> der Schreib-Antwort (z. B. 2027-398675).</summary>
    public string Anzeige => Belegjahr is { } j ? $"{j}-{Belegnummer}" : $"{Belegnummer}";
}

/// <summary>v1.99.7 E5 (Variante A): Sage-Prüfung vor jedem erneuten Senden eines Sage-Wareneingangs. <b>Wirft bei jedem Fehler</b> -
/// der Sender sendet dann nicht (ein „nicht gefunden“ wegen eines HTTP-Fehlers wäre eine Doppelbuchung).</summary>
public interface ISageWareneingangLookup
{
    /// <summary>ELL-Belege mit der Memo-Kennung <c>WE#&lt;id&gt;#</c>.</summary>
    Task<IReadOnlyList<SageWeBelegTreffer>> NachMemoAsync(int ersteReceiptId, CancellationToken ct);

    /// <summary>Sicherungsnetz bei 0 Memo-Treffern: ELL-Belege aus derselben Bestellung ab dem Buchungsdatum, mit Lieferschein-Nr. falls erfasst.</summary>
    Task<IReadOnlyList<SageWeBelegTreffer>> KandidatenAsync(int bestellBelId, DateTime abDatum, string? lieferschein, CancellationToken ct);
}

/// <summary>SData-Lookup über <c>EinkaufBelegeInfo</c> (nur GET). Filter immer eng (Memo bzw. ReferenzBelID + Datum): ein breiter Filter wie nur
/// <c>Belegkennzeichen eq 'ELL'</c> blättert ~90 000 Belege (P-W9 (b): über 10 Minuten). Datum nur als <c>@yyyy-MM-dd@</c> - ein Datum in
/// Hochkommas liefert ohne Fehler 0 Treffer (Live-Test 2026-11-12).</summary>
public sealed class SdataWareneingangLookupReader : ISageWareneingangLookup
{
    public const string Ressource = "EinkaufBelegeInfo";
    public const string Select = "BelID,Belegjahr,Belegnummer,Memo";

    private readonly ISageSdataReadClient _client;
    private readonly IConfiguration _configuration;

    public SdataWareneingangLookupReader(ISageSdataReadClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
    }

    public static string WhereMemo(int ersteReceiptId) =>
        $"Belegkennzeichen eq '{SageWareneingang.BelegKennzeichen}' and Memo like '%{SageWareneingang.Memo(ersteReceiptId)}%'";

    public static string WhereKandidaten(int bestellBelId, DateTime abDatum, string? lieferschein) =>
        $"Belegkennzeichen eq '{SageWareneingang.BelegKennzeichen}' and ReferenzBelID eq {bestellBelId} and Belegdatum ge @{abDatum:yyyy-MM-dd}@"
        + (string.IsNullOrWhiteSpace(lieferschein) ? "" : $" and Lieferschein eq '{lieferschein.Trim().Replace("'", "''")}'");

    /// <summary>Treffer, deren Memo eine ANDERE WMS-Kennung trägt, sind andere WMS-Wareneingänge, keine Kandidaten.</summary>
    public static bool FremdeKennung(string? memo, int ersteReceiptId) =>
        Regex.Matches(memo ?? "", @"WE#(\d+)#").Any(m => m.Groups[1].Value != ersteReceiptId.ToString());

    public Task<IReadOnlyList<SageWeBelegTreffer>> NachMemoAsync(int ersteReceiptId, CancellationToken ct) => Lesen(WhereMemo(ersteReceiptId), ct);

    public Task<IReadOnlyList<SageWeBelegTreffer>> KandidatenAsync(int bestellBelId, DateTime abDatum, string? lieferschein, CancellationToken ct) =>
        Lesen(WhereKandidaten(bestellBelId, abDatum, lieferschein), ct);

    private async Task<IReadOnlyList<SageWeBelegTreffer>> Lesen(string where, CancellationToken ct)
    {
        var ziel = await SdataLeseZielLeser.LesenAsync(_configuration, ct);
        var r = await _client.QueryAllAsync<SageWeBelegTreffer>(ziel, SageSdataReadClient.VertragResources, Ressource, where, Select, "BelID", ct, count: 20);
        return r.Zeilen;
    }
}
