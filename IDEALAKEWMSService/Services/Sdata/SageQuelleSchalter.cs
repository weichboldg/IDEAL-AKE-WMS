using IDEALAKEWMSService.Common;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SageSdata;

namespace IDEALAKEWMSService.Services.Sdata;

// Spec 2026-11-06-sage-sdata-stammdaten-sync (E0, F1): Quellenschalter je Abgleich. Default View = Verhalten unveraendert.

public enum SageQuelle { View, SData, Vergleich }

/// <summary>Gewaehlte Quelle; <paramref name="Warnung"/> != null = ins Protokoll schreiben (unbekannter Wert, SData gesperrt).</summary>
public sealed record SageQuellenWahl(SageQuelle Quelle, string? Warnung);

public static class SageQuelleSchalter
{
    public const string KeyPrefix = "Sync:Quelle:";

    // Bereiche = Key-Suffix (ServiceSettingDefinitions + Referenz im Protokoll SageQuellenvergleich)
    public const string Lagerplaetze = "Lagerplaetze";
    public const string Lagerbestand = "Lagerbestand";
    public const string Artikel = "Artikel";
    public const string FaStruktur = "FaStruktur";
    public const string Arbeitsplaetze = "Arbeitsplaetze";
    public const string LagerbuchungLookup = "LagerbuchungLookup";

    public static string Key(string bereich) => KeyPrefix + bereich;

    /// <summary>
    /// Leer/fehlend = <c>View</c> (still). Erlaubt sind genau <c>View</c>, <c>SData</c>, <c>Vergleich</c> (ordinal, ohne Trim) —
    /// alles andere (z. B. <c>"sdata "</c>, <c>"Neu"</c>) wirkt wie <c>View</c> mit Warnung (fail-safe, nie still nach SData).
    /// <paramref name="sdataGesperrtBis"/> gesetzt (offene Pruefabfrage): <c>SData</c> wirkt wie <c>View</c> mit Warnung.
    /// </summary>
    public static SageQuellenWahl Parse(string bereich, string? wert, string? sdataGesperrtBis = null) => wert switch
    {
        null or "" or "View" => new(SageQuelle.View, null),
        "Vergleich" => new(SageQuelle.Vergleich, null),
        "SData" when sdataGesperrtBis is null => new(SageQuelle.SData, null),
        "SData" => new(SageQuelle.View, $"{Key(bereich)} = SData ist gesperrt bis {sdataGesperrtBis} – Quelle View."),
        _ => new(SageQuelle.View, $"Unbekannter Wert '{wert}' für {Key(bereich)} (erlaubt: View, SData, Vergleich) – Quelle View."),
    };
}

/// <summary>
/// Baut das SData-Ziel eines Abgleich-Laufs: Endpunkt aus ServiceSettings (DB-first, wie <c>SageBookingWorker</c>), Zugangsdaten aus
/// appsettings <c>SageLagerbuchung:*</c>, TLS-Schalter <see cref="SageTlsPolicy"/> (reist je Anfrage in den Request-Options).
/// Wirft bei fehlender Konfiguration — ein Abgleich mit Quelle SData darf nie still leer laufen.
/// </summary>
public static class SdataLeseZielLeser
{
    public static async Task<SdataLeseZiel> LesenAsync(IConfiguration configuration, CancellationToken ct)
    {
        var baseUrl = (await ServiceSettings.GetValueSafeAsync(configuration, "SData:BaseUrl", ct))?.Trim();
        var application = (await ServiceSettings.GetValueSafeAsync(configuration, "SData:Application", ct))?.Trim();
        var dataset = (await ServiceSettings.GetValueSafeAsync(configuration, "SData:Dataset", ct))?.Trim();
        var pruefen = SageTlsPolicy.ShouldVerifyCertificate(await ServiceSettings.GetValueSafeAsync(configuration, SageTlsPolicy.SettingKey, ct));
        var zugang = await ZugangAsync(configuration, ct);
        if (zugang.Fehler is { } f)
            throw new InvalidOperationException(f + ".");   // kein Sage-Aufruf, kein Rueckfall auf appsettings
        return Bauen(baseUrl, application, dataset, zugang.Benutzer, zugang.Kennwort, pruefen) with { Warnung = zugang.Warnung };
    }

    /// <summary>
    /// v1.99.14 (Spec 2026-11-13-sdata-teil-2 F6): Zugangsdaten an EINER Stelle (<see cref="SdataZugangsdaten"/>) — DB first, Kennwort mit dem
    /// Schluesselring der Web-App entschluesselt (nur lesend). Bewusst NICHT die Safe-Variante: ein DB-Lesefehler darf nicht still auf
    /// appsettings zurueckfallen.
    /// </summary>
    public static async Task<SdataZugang> ZugangAsync(IConfiguration configuration, CancellationToken ct) =>
        SdataZugangsdaten.Aufloesen(
            await ServiceSettings.GetValueAsync(configuration, SdataZugangsdaten.BenutzerKey, ct),
            await ServiceSettings.GetValueAsync(configuration, SdataZugangsdaten.KennwortKey, ct),
            configuration, SdataZugangsdaten.DienstProvider(configuration));

    public static SdataLeseZiel Bauen(string? baseUrl, string? application, string? dataset, string? username, string? password, bool zertifikatPruefen)
    {
        var fehlt = new List<string>();
        if (string.IsNullOrWhiteSpace(baseUrl)) fehlt.Add("SData:BaseUrl");
        else if (baseUrl.Contains('@')) fehlt.Add("SData:BaseUrl ohne Zugangsdaten");
        if (string.IsNullOrWhiteSpace(dataset)) fehlt.Add("SData:Dataset");
        if (string.IsNullOrWhiteSpace(username)) fehlt.Add("SData:Username (bzw. appsettings SageLagerbuchung:Username)");
        if (fehlt.Count > 0)
            throw new InvalidOperationException("SData nicht konfiguriert: " + string.Join(", ", fehlt) + ".");
        return new SdataLeseZiel(baseUrl!, string.IsNullOrWhiteSpace(application) ? "ol" : application, dataset!, username!, password ?? string.Empty, zertifikatPruefen);
    }
}
