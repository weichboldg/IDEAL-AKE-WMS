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
    /// <summary>ELL-Belege mit der Memo-Kennung <c>WE#[K#]&lt;id&gt;#</c> (v1.99.9: die beim Senden gespeicherte Kennung, <see cref="SageWareneingang.Kennung"/>).</summary>
    Task<IReadOnlyList<SageWeBelegTreffer>> NachMemoAsync(string kennung, CancellationToken ct);

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

    /// <summary>v1.99.9: Kennung ohne LIKE-Metazeichen (Kuerzel nur A-Z/0-9, Buchstabe vorne). Ein <c>%WE#7#%</c> trifft <c>WE#DEV#7#</c> nicht
    /// und umgekehrt; das schliessende <c>#</c> trennt <c>WE#7#</c> von <c>WE#17#</c>/<c>WE#70#</c>.</summary>
    public static string WhereMemo(string kennung) =>
        $"Belegkennzeichen eq '{SageWareneingang.BelegKennzeichen}' and Memo like '%{kennung}%'";

    public static string WhereKandidaten(int bestellBelId, DateTime abDatum, string? lieferschein) =>
        $"Belegkennzeichen eq '{SageWareneingang.BelegKennzeichen}' and ReferenzBelID eq {bestellBelId} and Belegdatum ge @{abDatum:yyyy-MM-dd}@"
        + (string.IsNullOrWhiteSpace(lieferschein) ? "" : $" and Lieferschein eq '{lieferschein.Trim().Replace("'", "''")}'");

    /// <summary>Treffer, deren Memo eine ANDERE WMS-Kennung trägt, sind andere WMS-Wareneingänge, keine Kandidaten. v1.99.9: jede Kennung mit
    /// oder ohne System-Kuerzel (<c>WE#7#</c>, <c>WE#DEV#7#</c>), die nicht exakt die eigene ist, gilt als fremd - auch <c>WE#7#</c> bei eigener
    /// <c>WE#DEV#7#</c> (anderes WMS, z. B. Produktion oder ein Test-System ohne Kuerzel).</summary>
    public static bool FremdeKennung(string? memo, string eigeneKennung) =>
        Regex.Matches(memo ?? "", @"WE#(?:[A-Z][A-Z0-9]{0,4}#)?\d+#").Any(m => m.Value != eigeneKennung);

    public Task<IReadOnlyList<SageWeBelegTreffer>> NachMemoAsync(string kennung, CancellationToken ct) => Lesen(WhereMemo(kennung), ct);

    public Task<IReadOnlyList<SageWeBelegTreffer>> KandidatenAsync(int bestellBelId, DateTime abDatum, string? lieferschein, CancellationToken ct) =>
        Lesen(WhereKandidaten(bestellBelId, abDatum, lieferschein), ct);

    private async Task<IReadOnlyList<SageWeBelegTreffer>> Lesen(string where, CancellationToken ct)
    {
        var ziel = await SdataLeseZielLeser.LesenAsync(_configuration, ct);
        var r = await _client.QueryAllAsync<SageWeBelegTreffer>(ziel, SageSdataReadClient.VertragResources, Ressource, where, Select, "BelID", ct, count: 20);
        return r.Zeilen;
    }
}
