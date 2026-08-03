using System.Text.Json.Serialization;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// SData-Request-Objekt fuer <c>POST .../$service/LagerbuchungService</c>. Property-Namen sind
/// per <see cref="JsonPropertyNameAttribute"/> exakt an das Postman-Sample gebunden (unabhaengig
/// von der JSON-Naming-Policy des Clients).
/// </summary>
public sealed class SageLagerbuchungRequest
{
    [JsonPropertyName("Memo")] public string Memo { get; set; } = string.Empty;
    [JsonPropertyName("Standardtext")] public string Standardtext { get; set; } = string.Empty;
    [JsonPropertyName("Lagerbuchungen")] public List<SageLagerbuchungZeile> Lagerbuchungen { get; set; } = new();
}

public sealed class SageLagerbuchungZeile
{
    [JsonPropertyName("Lagerbewegungsart")] public string Lagerbewegungsart { get; set; } = string.Empty;
    [JsonPropertyName("Artikelnummer")] public string Artikelnummer { get; set; } = string.Empty;
    [JsonPropertyName("AuspraegungHandle")] public int AuspraegungHandle { get; set; }
    [JsonPropertyName("HerkunftLagerkennung")] public string HerkunftLagerkennung { get; set; } = string.Empty;
    [JsonPropertyName("HerkunftLagerplatzId")] public int HerkunftLagerplatzId { get; set; }
    [JsonPropertyName("ZielLagerkennung")] public string ZielLagerkennung { get; set; } = string.Empty;
    [JsonPropertyName("ZielLagerplatzId")] public int ZielLagerplatzId { get; set; }
    [JsonPropertyName("MengeLager")] public decimal MengeLager { get; set; }
    [JsonPropertyName("Seriennummern")] public List<SageSeriennummer> Seriennummern { get; set; } = new();
    [JsonPropertyName("Chargen")] public List<SageCharge> Chargen { get; set; } = new();
}

public sealed class SageSeriennummer
{
    [JsonPropertyName("Seriennummer")] public string Seriennummer { get; set; } = string.Empty;
}

public sealed class SageCharge
{
    [JsonPropertyName("Charge")] public string Charge { get; set; } = string.Empty;
    [JsonPropertyName("Menge")] public decimal Menge { get; set; }
    [JsonPropertyName("Verfallsdatum")] public DateTime? Verfallsdatum { get; set; }
}

/// <summary>
/// Wird geworfen, wenn eine Buchung mangels Sage-Referenzdaten (Kennung/PlatzId) oder wegen einer
/// nicht buchbaren Bewegungsart nicht in einen gueltigen Payload uebersetzt werden kann (AK9).
/// Der Worker markiert den Queue-Eintrag daraufhin als Fehler — statt eine ungueltige Anfrage zu senden.
/// </summary>
public sealed class SageBookingPayloadException : Exception
{
    public SageBookingPayloadException(string message) : base(message) { }
}
