using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IdealAkeWms.Services.SageSdata;

/// <summary>Reiner Lese-Client (nur GET) fuer SData: <c>RealTimeData</c> (FA-Ansicht) und <c>CommonWawiServicesResources</c> (Abgleiche).</summary>
public interface ISageSdataReadClient
{
    /// <summary>
    /// <c>GET {Basis}/sdata/{App}/RealTimeData/{Dataset}/{quelle}?where=…&amp;select=…[&amp;orderBy=…]&amp;format=json</c>.
    /// Wirft nie bei HTTP- oder Verbindungsfehlern (leere Zeilen + Diagnose), nur bei leerem <paramref name="where"/>
    /// (<see cref="ArgumentException"/>) und bei Abbruch durch <paramref name="ct"/>.
    /// </summary>
    Task<SdataErgebnis<T>> GetAsync<T>(SdataLeseZiel ziel, string quelle, string where, string select, string? orderBy, CancellationToken ct);

    /// <summary>
    /// Liest ALLE Zeilen einer Ressource seitenweise (<c>count</c>/<c>startIndex</c> bis <c>$totalResults</c>) aus dem Vertrag
    /// <paramref name="vertrag"/> (<see cref="SageSdataReadClient.VertragResources"/> bzw. <see cref="SageSdataReadClient.VertragResourcesSpecific"/>).
    /// Fuer Abgleiche: <b>wirft</b> bei jedem Fehler (<see cref="SdataLeseException"/>), liefert nie leere Zeilen statt Fehler.
    /// <see cref="ArgumentException"/> ohne <paramref name="orderBy"/> (Sage: HTTP 500 ohne ORDER BY) und fuer
    /// <see cref="SageSdataReadClient.NurMitWhere"/>-Ressourcen ohne <paramref name="where"/>.
    /// </summary>
    Task<SdataSeiten<T>> QueryAllAsync<T>(SdataLeseZiel ziel, string vertrag, string ressource, string? where, string? select,
        string orderBy, CancellationToken ct, int count = SageSdataReadClient.Seitengroesse);
}

/// <summary>
/// Spec 2026-10-30-sage-sdata-fa-ansicht (v1.71.0). Muster wie der Lagerbuchungs-Client des Dienstes
/// (IDEALAKEWMSService/Services/SageLagerbuchungClient.cs: Basic-Auth, kein Throw bei HTTP-Fehler, Body-Cap 4000).
/// <para>
/// <b>Kodierung:</b> <c>where</c> wird NICHT mit <see cref="Uri.EscapeDataString"/> kodiert (aus <c>'</c> wuerde <c>%27</c>);
/// die <see cref="Uri"/>-Pipeline macht Leerzeichen zu <c>%20</c> und laesst <c>'</c> und <c>;</c> literal — so wie die im
/// Browser erprobte URL. <b>TLS:</b> Der Schalter <c>SageLagerbuchungSslZertifikatPruefen</c> reist je Anfrage in
/// <see cref="HttpRequestMessage.Options"/> (<see cref="ZertifikatPruefenOption"/>); der Zertifikats-Callback in
/// <c>Program.cs</c> liest nur diese Option — kein DB-Zugriff im Handshake (Freigabe-Nachtrag 4).
/// </para>
/// </summary>
public sealed class SageSdataReadClient : ISageSdataReadClient
{
    public const string ServiceContract = "RealTimeData";
    public const int MaxRohdaten = 4000;

    /// <summary>Fehlt die Option, wird geprueft (fail-safe).</summary>
    public static readonly HttpRequestOptionsKey<bool> ZertifikatPruefenOption = new("SageSdata.ZertifikatPruefen");

    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly ILogger<SageSdataReadClient> _logger;

    public SageSdataReadClient(HttpClient http, ILogger<SageSdataReadClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>Zertifikatspruefung fuer eine Anfrage: ohne Fehler immer ok, sonst nur, wenn die Anfrage sie abschaltet.</summary>
    public static bool ZertifikatAkzeptieren(HttpRequestMessage? request, System.Net.Security.SslPolicyErrors errors) =>
        errors == System.Net.Security.SslPolicyErrors.None
        || (request is not null && request.Options.TryGetValue(ZertifikatPruefenOption, out var pruefen) && !pruefen);

    public static string BuildUrl(SdataLeseZiel ziel, string quelle, string where, string select, string? orderBy)
    {
        var query = $"{quelle}?where={where}&select={select}" + (orderBy is null ? "" : $"&orderBy={orderBy}") + "&format=json";
        return SdataUrlBuilder.BuildResourceUrl(ziel.BaseUrl, ziel.Application, ServiceContract, ziel.Dataset, query);
    }

    public async Task<SdataErgebnis<T>> GetAsync<T>(SdataLeseZiel ziel, string quelle, string where, string select, string? orderBy, CancellationToken ct)
    {
        // Code-Sperre: ohne where liefert z. B. dtsTeileverwendungsnachweis 237.122 Zeilen (count wird ignoriert).
        if (string.IsNullOrWhiteSpace(where))
            throw new ArgumentException("SData-Lesezugriff ohne where ist gesperrt.", nameof(where));

        var url = BuildUrl(ziel, quelle, where, select, orderBy);
        var uhr = Stopwatch.StartNew();
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, url);
            message.Options.Set(ZertifikatPruefenOption, ziel.ZertifikatPruefen);
            // format=json allein reicht nicht: ohne Accept-Header liefert Sage Atom-XML (UAT 2026-10-30).
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ziel.Username}:{ziel.Password}")));

            using var response = await _http.SendAsync(message, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            var status = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sage-SData antwortete {Status} auf {Url}.", status, url);
                return Leer<T>(new SdataCall(quelle, url, status, uhr.ElapsedMilliseconds, 0,
                    $"HTTP {status} {response.ReasonPhrase}", Cap(body)));
            }

            try
            {
                var zeilen = JsonSerializer.Deserialize<SdataFeed<T>>(body, JsonOptions)?.Resources ?? new List<T>();
                return new SdataErgebnis<T>(zeilen, new SdataCall(quelle, url, status, uhr.ElapsedMilliseconds, zeilen.Count, null, Cap(body)));
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Sage-SData-Antwort von {Url} nicht lesbar.", url);
                return Leer<T>(new SdataCall(quelle, url, status, uhr.ElapsedMilliseconds, 0, $"Antwort nicht lesbar: {ex.Message}", Cap(body)));
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Status 0 = Verbindungsfehler (nicht erreichbar, Timeout, TLS). Meldungen nennen Host/Grund, nie Zugangsdaten
            // (die stehen nur im Authorization-Header).
            _logger.LogWarning(ex, "Sage-SData nicht erreichbar ({Url}).", url);
            var text = ex is TaskCanceledException
                ? $"Zeitüberschreitung nach {_http.Timeout.TotalSeconds:0} s"
                : ex.InnerException is null ? ex.Message : $"{ex.Message} ({ex.InnerException.Message})";
            return Leer<T>(new SdataCall(quelle, url, 0, uhr.ElapsedMilliseconds, 0, text, null));
        }
    }

    // ===== Paging-Lesen CommonWawiServicesResources (Spec 2026-11-06-sage-sdata-stammdaten-sync, E0/F2) =====

    public const string VertragResources = "CommonWawiServicesResources";
    public const string VertragResourcesSpecific = "CommonWawiServicesResourcesSpecific";
    public const int Seitengroesse = 500;
    /// <summary>Abfrage ohne where mit mehr Zeilen bricht nach der ersten Seite ab.</summary>
    public const int MaxZeilenOhneWhere = 100_000;
    public const int MaxWiederholungen = 2;
    /// <summary>Timeout je Seitenanfrage. Der Dienst registriert den HttpClient mit demselben Wert (die Web-App mit 20 s).</summary>
    public static readonly TimeSpan SeitenTimeout = TimeSpan.FromSeconds(60);

    /// <summary>Ressourcen, die nie ohne where gelesen werden (Millionen Belegpositionen).</summary>
    public static readonly IReadOnlySet<string> NurMitWhere =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "VerkaufPositionenAllInfo", "EinkaufPositionenAllInfo" };

    /// <summary>Wie <see cref="JsonOptions"/>, dazu Dezimal-/Zahlenwerte auch als String und Sage-Booleans (-1/0) als <c>bool</c>.</summary>
    public static readonly JsonSerializerOptions PagingJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
        Converters = { new SageBoolJsonConverter() },
    };

    /// <summary>Pause vor einer Wiederholung (Timeout/503). Tests setzen <see cref="TimeSpan.Zero"/>.</summary>
    public TimeSpan WiederholungsPause { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// URL einer Seite. <c>where</c> bleibt bis auf <c>% # &amp; +</c> literal (wie <see cref="BuildUrl"/>); diese vier wuerden die URL
    /// sonst zerschneiden (<c>#</c> = Fragment, z. B. Memo <c>SM#123#</c>) bzw. falsch dekodiert.
    /// </summary>
    public static string BuildQueryUrl(SdataLeseZiel ziel, string vertrag, string ressource, string? where, string? select,
        string orderBy, int count, int startIndex)
    {
        var query = $"{ressource}?format=json&orderBy={orderBy}&count={count}&startIndex={startIndex}"
            + (string.IsNullOrWhiteSpace(select) ? "" : $"&select={select}")
            + (string.IsNullOrWhiteSpace(where) ? "" : "&where=" + where.Replace("%", "%25").Replace("#", "%23").Replace("&", "%26").Replace("+", "%2B"));
        return SdataUrlBuilder.BuildResourceUrl(ziel.BaseUrl, ziel.Application, vertrag, ziel.Dataset, query);
    }

    public async Task<SdataSeiten<T>> QueryAllAsync<T>(SdataLeseZiel ziel, string vertrag, string ressource, string? where, string? select,
        string orderBy, CancellationToken ct, int count = Seitengroesse)
    {
        if (string.IsNullOrWhiteSpace(ressource))
            throw new ArgumentException("SData-Ressource fehlt.", nameof(ressource));
        if (string.IsNullOrWhiteSpace(orderBy))
            throw new ArgumentException("SData-Abfrage ohne orderBy ist gesperrt (Sage antwortet sonst mit HTTP 500).", nameof(orderBy));
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Seitengröße muss größer 0 sein.");
        var ohneWhere = string.IsNullOrWhiteSpace(where);
        if (ohneWhere && NurMitWhere.Contains(ressource))
            throw new ArgumentException($"SData-Ressource {ressource} darf nur mit where gelesen werden.", nameof(where));

        var uhr = Stopwatch.StartNew();
        var zeilen = new List<T>();
        int seiten = 0, startIndex = 1; // SData: startIndex ist 1-basiert
        int? gesamt = null;
        while (true)
        {
            var feed = await LeseSeiteAsync<T>(ziel, ressource, BuildQueryUrl(ziel, vertrag, ressource, where, select, orderBy, count, startIndex), ct);
            seiten++;
            gesamt = feed.TotalResults ?? gesamt;
            if (ohneWhere && feed.TotalResults > MaxZeilenOhneWhere)
                throw new SdataLeseException(ressource, 0,
                    $"Abbruch: {feed.TotalResults} Zeilen ohne where (Grenze {MaxZeilenOhneWhere}) – Abfrage braucht einen Filter.");

            var seite = feed.Resources!;
            zeilen.AddRange(seite);
            if (gesamt is not null)
            {
                if (zeilen.Count >= gesamt) break;
                // Leere Seite vor $totalResults = unvollstaendig gelesen: werfen, sonst wuerde der Abgleich Fehlendes deaktivieren.
                if (seite.Count == 0)
                    throw new SdataLeseException(ressource, 0, $"Unvollständig gelesen: {zeilen.Count} von {gesamt} Zeilen.");
            }
            else if (seite.Count != count)
                break; // ohne $totalResults: kurze (oder ungekappte) Seite = Ende
            startIndex += seite.Count;
        }

        _logger.LogInformation("Sage-SData {Ressource}: {Zeilen} Zeilen in {Seiten} Seite(n), {Ms} ms.", ressource, zeilen.Count, seiten, uhr.ElapsedMilliseconds);
        return new SdataSeiten<T>(zeilen, ressource, seiten, gesamt, uhr.ElapsedMilliseconds);
    }

    private async Task<SdataFeed<T>> LeseSeiteAsync<T>(SdataLeseZiel ziel, string ressource, string url, CancellationToken ct)
    {
        for (var versuch = 0; ; versuch++)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(SeitenTimeout);
            int status;
            string body;
            string? grund;
            try
            {
                using var message = new HttpRequestMessage(HttpMethod.Get, url);
                message.Options.Set(ZertifikatPruefenOption, ziel.ZertifikatPruefen);
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                message.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ziel.Username}:{ziel.Password}")));
                using var response = await _http.SendAsync(message, timeout.Token);
                body = await response.Content.ReadAsStringAsync(timeout.Token);
                status = (int)response.StatusCode;
                grund = response.ReasonPhrase;
            }
            catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
            {
                // Zeitueberschreitung (eigener 60-s-Timeout oder HttpClient.Timeout) -> wiederholen
                if (versuch < MaxWiederholungen)
                {
                    _logger.LogWarning("Sage-SData {Ressource}: Zeitüberschreitung, Wiederholung {Nr}.", ressource, versuch + 1);
                    await Task.Delay(WiederholungsPause, ct);
                    continue;
                }
                throw new SdataLeseException(ressource, 0, $"Zeitüberschreitung ({versuch + 1} Versuche à {SeitenTimeout.TotalSeconds:0} s).", ex);
            }
            catch (HttpRequestException ex)
            {
                // Verbindungsfehler (nicht erreichbar, TLS): keine Wiederholung. Meldung nennt nie Zugangsdaten.
                throw new SdataLeseException(ressource, 0,
                    ex.InnerException is null ? ex.Message : $"{ex.Message} ({ex.InnerException.Message})", ex);
            }

            if (status == 503 && versuch < MaxWiederholungen)
            {
                _logger.LogWarning("Sage-SData {Ressource}: HTTP 503, Wiederholung {Nr}.", ressource, versuch + 1);
                await Task.Delay(WiederholungsPause, ct);
                continue;
            }
            if (status is < 200 or >= 300)
                throw new SdataLeseException(ressource, status, DiagnoseText(body) ?? $"{grund} {(body.Length <= 300 ? body : body[..300])}".Trim());

            try
            {
                var feed = JsonSerializer.Deserialize<SdataFeed<T>>(body, PagingJsonOptions);
                if (feed?.Resources is null)
                    throw new SdataLeseException(ressource, status, "Antwort ist kein SData-Feed (ohne $resources).");
                return feed;
            }
            catch (JsonException ex)
            {
                // z. B. HTML-Loginseite eines Proxys mit 200: Fehler, nicht „0 Zeilen“
                throw new SdataLeseException(ressource, status, $"Antwort nicht lesbar: {ex.Message}", ex);
            }
        }
    }

    /// <summary><c>$diagnoses[0].message</c> (bzw. <c>$message</c>) aus einer Fehlerantwort; null, wenn nicht vorhanden/kein JSON.</summary>
    internal static string? DiagnoseText(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var liste = root.ValueKind == JsonValueKind.Array ? root
                : root.ValueKind == JsonValueKind.Object && root.TryGetProperty("$diagnoses", out var d) ? d : default;
            if (liste.ValueKind != JsonValueKind.Array || liste.GetArrayLength() == 0 || liste[0].ValueKind != JsonValueKind.Object)
                return null;
            foreach (var name in new[] { "message", "$message" })
                if (liste[0].TryGetProperty(name, out var m) && m.ValueKind == JsonValueKind.String)
                    return m.GetString();
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static SdataErgebnis<T> Leer<T>(SdataCall call) => new(Array.Empty<T>(), call);

    private static string? Cap(string? value) =>
        value is null ? null : (value.Length <= MaxRohdaten ? value : value[..MaxRohdaten]);
}
