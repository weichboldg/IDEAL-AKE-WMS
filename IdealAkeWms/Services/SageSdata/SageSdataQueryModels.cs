using System.Text.Json;
using System.Text.Json.Serialization;

namespace IdealAkeWms.Services.SageSdata;

// Spec 2026-11-06-sage-sdata-stammdaten-sync (E0, F2): Modelle der Paging-Lesemethode QueryAllAsync
// (Vertrag CommonWawiServicesResources). Fuer Abgleiche gilt: werfen statt leer liefern.

/// <summary>
/// SData-Lesefehler eines Abgleich-Reads (HTTP-Fehler, Zeitueberschreitung, unlesbare Antwort, Sicherheitsgrenze).
/// Nie leere Zeilen statt Fehler: im Abgleich hiesse das „alles verschwunden“ (Deaktivieren/Nullsetzen).
/// <see cref="Exception.Message"/> ist auf <see cref="MaxMeldung"/> Zeichen gekappt (SyncLogs.Message = 1000).
/// </summary>
public sealed class SdataLeseException : Exception
{
    public const int MaxMeldung = 600;

    /// <summary>HTTP-Status; 0 = Verbindungsfehler, Zeitueberschreitung oder Abbruch durch eine Sicherheitsgrenze.</summary>
    public int Status { get; }
    public string Ressource { get; }
    /// <summary><c>$diagnoses[0].message</c> (enthaelt bei HTTP 500 das erzeugte SQL) bzw. Fehlertext; hoechstens 4000 Zeichen.</summary>
    public string Diagnose { get; }

    public SdataLeseException(string ressource, int status, string diagnose, Exception? inner = null)
        : base(Kappe($"SData {ressource}: {(status == 0 ? "" : $"HTTP {status} – ")}{diagnose}", MaxMeldung), inner)
    {
        Ressource = ressource;
        Status = status;
        Diagnose = Kappe(diagnose, SageSdataReadClient.MaxRohdaten);
    }

    private static string Kappe(string s, int max) => s.Length <= max ? s : s[..max];
}

/// <summary>Ergebnis von <see cref="ISageSdataReadClient.QueryAllAsync{T}"/>: alle Zeilen + Protokollangaben.</summary>
public sealed record SdataSeiten<T>(IReadOnlyList<T> Zeilen, string Ressource, int Seiten, int? Gesamt, long DauerMs)
{
    /// <summary>Info-Zeile fuer das Aktivitaets-Protokoll (Quelle, Ressource, Seiten/Zeilen, Dauer).</summary>
    public string Zusammenfassung => $"Quelle SData: {Ressource}, {Seiten} Seite(n), {Zeilen.Count} Zeile(n), {DauerMs} ms";
}

/// <summary>
/// Sage-Boolean robust lesen: Zahl (<c>-1</c>/<c>1</c> = wahr, <c>0</c> = falsch), JSON-<c>true</c>/<c>false</c> oder dieselben Werte als String.
/// Gleiche Regel wie <see cref="SageBool.IsTrue"/> (alles ausser 0 ist wahr).
/// </summary>
public sealed class SageBoolJsonConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch
    {
        JsonTokenType.True => true,
        JsonTokenType.False => false,
        JsonTokenType.Number => SageBool.IsTrue(reader.TryGetInt32(out var i) ? i : (int)Math.Sign(reader.GetDecimal())),
        JsonTokenType.String => reader.GetString()?.Trim() switch
        {
            var s when bool.TryParse(s, out var b) => b,
            var s when int.TryParse(s, out var n) => SageBool.IsTrue(n),
            var s => throw new JsonException($"Kein Sage-Boolean: '{s}'"),
        },
        _ => throw new JsonException($"Kein Sage-Boolean: {reader.TokenType}"),
    };

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteBooleanValue(value);
}
