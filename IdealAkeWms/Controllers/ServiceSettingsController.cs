using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.Settings;
using IdealAkeWms.Services.HierarchischeStruktur;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.AspNetCore.DataProtection;

namespace IdealAkeWms.Controllers;

[RequireAdminAccess]
public class ServiceSettingsController : Controller
{
    private readonly IServiceSettingRepository _repository;
    private readonly ISyncLogger _syncLogger;
    private readonly ICurrentUserService _currentUser;
    private readonly IDataProtectionProvider _schutz;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServiceSettingsController> _logger;

    public ServiceSettingsController(
        IServiceSettingRepository repository,
        ISyncLogger syncLogger,
        ICurrentUserService currentUser,
        IDataProtectionProvider schutz,
        IConfiguration configuration,
        ILogger<ServiceSettingsController> logger)
    {
        _repository = repository;
        _syncLogger = syncLogger;
        _currentUser = currentUser;
        _schutz = schutz;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>v1.99.14: Geheim-Keys (SData-Kennwort) laufen nie ueber den Freitext-Fallback (Klartext in die DB bzw. Schutztext ins Formular).</summary>
    private static bool IstGeheim(string? key) => key != null && ServiceSettingDefinitions.TryGet(key, out var d) && d.Type == SettingType.Geheim;

    /// <summary>v1.99.92 SD-3: Key weicht nur in der Gross-/Kleinschreibung von einem Katalog-Key ab. Bei CI-Kollation traefe er die echte Zeile
    /// an den Waechtern vorbei (sdata:password, produktionsauftraghierarchisch). Der exakte Key laeuft weiter ueber die bestehenden Waechter.</summary>
    private static string? KatalogKeyAndereSchreibweise(string? key) => key == null ? null
        : ServiceSettingDefinitions.All.Select(d => d.Key)
            .FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase) && !string.Equals(k, key, StringComparison.Ordinal));

    private IActionResult SchreibweiseAbgelehnt(string key, string katalogKey)
    {
        TempData["WarningMessage"] = $"Key „{key}“ weicht nur in der Groß-/Kleinschreibung von „{katalogKey}“ ab – Katalog-Einstellungen nur über die Maske pflegen.";
        return RedirectToAction(nameof(Index));
    }

    private IActionResult GeheimAbgelehnt()
    {
        TempData["WarningMessage"] = "Das Kennwort wird nur über das Kennwortfeld der Service-Einstellungen gepflegt (verschlüsselt).";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Index()
    {
        var vm = await BuildViewModelAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSettings(Dictionary<string, string> settings, string[]? geheimEntfernen = null)
    {
        settings ??= new Dictionary<string, string>();
        var entfernen = (geheimEntfernen ?? Array.Empty<string>()).ToHashSet(StringComparer.Ordinal);
        var geheimEingegeben = false;
        var geheimGeaendert = new List<(SettingDefinition Def, bool Entfernt)>();

        // v1.77.0 E4a (F5, ADR 0020): ALLE Katalog-Keys pruefen, bevor geschrieben wird — bei einem Fehler wird nichts gespeichert
        // (vorher wurden die uebrigen Werte trotzdem geschrieben). Choice exakt (der Dienst liest ordinal). Speichern ohne Nebenwirkung (F4).
        var bestand = (await _repository.GetAllAsync() ?? new List<ServiceSetting>()).ToDictionary(r => r.Key, StringComparer.Ordinal);
        var schreiben = new List<(SettingDefinition Def, string Wert)>();
        foreach (var (key, rawValue) in settings)
        {
            if (!ServiceSettingDefinitions.TryGet(key, out var def))
                continue; // unbekannte Keys ignorieren (Orphans laufen ueber Edit/Delete)

            var roh = rawValue ?? string.Empty;
            // v1.99.14 Geheim (F6): leer = unveraendert; „entfernen“ = leerer Wert; Eingabe = pruefen, verschluesseln. Wert nie in Meldungen.
            if (def.Type == SettingType.Geheim)
            {
                var vorhanden = bestand.TryGetValue(key, out var alt) && !string.IsNullOrEmpty(alt.Value);
                if (entfernen.Contains(key))
                {
                    if (vorhanden) { schreiben.Add((def, "")); geheimGeaendert.Add((def, true)); }
                    continue;
                }
                if (roh.Length == 0) continue;
                geheimEingegeben = true;
                var g = SettingValidator.Validate(def, roh, exakt: true);
                if (!g.Gueltig) { ModelState.AddModelError(key, g.Fehler!); continue; }
                schreiben.Add((def, SdataZugangsdaten.Schuetzen(roh, _schutz)));
                geheimGeaendert.Add((def, false));
                continue;
            }
            // Checkbox-Altformen wie bisher als „an“ lesen.
            if (def.Type == SettingType.Bool && (roh == "1" || roh.Equals("on", StringComparison.OrdinalIgnoreCase))) roh = "true";
            // Unveraendert = weder pruefen noch schreiben (F4, QA B2).
            if (bestand.TryGetValue(key, out var vorher) ? string.Equals(vorher.Value, roh, StringComparison.Ordinal) : SettingItem.EntsprichtDefault(def, roh))
                continue;
            var pruefung = SettingValidator.Validate(def, roh, exakt: true);
            if (!pruefung.Gueltig)
            {
                ModelState.AddModelError(key, pruefung.Fehler!);
                continue;
            }
            // QA v1.99.9: String mit Muster (System-Kuerzel) getrimmt speichern, wie geprueft
            var wert = def.Type == SettingType.String && def.Muster is null ? roh : pruefung.Kanonisch!;
            if (bestand.TryGetValue(key, out var row) && string.Equals(row.Value, wert, StringComparison.Ordinal)) continue;
            if (!bestand.ContainsKey(key) && SettingItem.EntsprichtDefault(def, wert)) continue;
            schreiben.Add((def, wert));
        }

        if (ModelState.IsValid)
        {
            foreach (var (def, wert) in schreiben)
            {
                try
                {
                    await _repository.UpsertAsync(def.Key, wert, def.Category, def.Description);
                }
                catch (HierarchischeStrukturGuardException ex)
                {
                    // Technischer Schreibversuch auf einen geschützten Einwegtor-Key über die generische
                    // Maske: abgelehnt (Choke-Point-Decorator) + protokolliert (AK 2).
                    ModelState.AddModelError(def.Key, ex.Message);
                    await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.HierarchieUmstellung);
                    await run.LogWarningAsync(
                        $"Technischer Schreibversuch auf geschützten Key '{def.Key}' über die generische Maske abgelehnt (durch {_currentUser.GetWindowsUserName()}): {ex.Message}");
                    await run.FinishSuccessAsync();
                }
            }
        }

        if (ModelState.IsValid)
            foreach (var (def, entfernt) in geheimGeaendert)
                _logger.LogInformation("{Einstellung} {Aktion} durch {Benutzer}.", def.Anzeigename ?? def.Key,
                    entfernt ? "entfernt" : "geändert", _currentUser.GetWindowsUserName());

        if (!ModelState.IsValid)
        {
            // Fehler sichtbar machen: View mit gemergtem Stand zurueckgeben.
            // F5 / QA B1: mit den eingegebenen Werten zurueck - v1.99.14: Geheimnisse NIE (Feld bleibt leer, Hinweis).
            if (geheimEingegeben)
                ViewData["GeheimHinweis"] = "Kennwort nicht gespeichert, bitte erneut eingeben.";
            var vm = await BuildViewModelAsync(settings);
            return View(nameof(Index), vm);
        }

        TempData["SuccessMessage"] = "Einstellungen gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Eingabe des Sage-Verbindungstests (aktuelle Formularwerte, ggf. noch ungespeichert).</summary>
    public class SageConnectionTestRequest
    {
        public string? BaseUrl { get; set; }
        public string? Application { get; set; }
        public string? ServiceContract { get; set; }
        public string? Dataset { get; set; }
        public bool VerifyCertificate { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        /// <summary>Relative SData-Resource, Default <c>$schema</c> (read-only Discovery).</summary>
        public string? Resource { get; set; }
    }

    /// <summary>
    /// Diagnose-Aufruf gegen die Sage-SData-API: baut die URL aus den (aktuellen) Konfig-Segmenten,
    /// sendet einen <b>GET</b> (nebenwirkungsfrei — nie eine echte Buchung) mit Basic-Auth und dem
    /// konfigurierten TLS-Verhalten, und liefert Status, Response-Header und Body als JSON zurueck.
    /// Nur Admin (Controller-Attribut). Antwortet immer mit 200 + JSON, damit die UI Fehler anzeigen kann.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestSageConnection([FromBody] SageConnectionTestRequest req, CancellationToken ct)
    {
        req ??= new SageConnectionTestRequest();
        var resource = string.IsNullOrWhiteSpace(req.Resource) ? "$schema" : req.Resource.Trim();
        var url = SdataUrlBuilder.BuildResourceUrl(req.BaseUrl, req.Application, req.ServiceContract, req.Dataset, resource);
        var zugangsQuelle = "Eingabe im Fenster";
        if (string.IsNullOrEmpty(req.Password))
        {
            // v1.99.14 F2: leeres Kennwort = gespeicherte Zugangsdaten (F6, nur serverseitig); nicht entschluesselbar = kein Aufruf, kein Rueckfall
            var stand = await SdataZugangsdaten.WebStandAsync(_repository, _configuration, _schutz);
            var z = stand.Zugang;
            if (z.Fehler is not null)
                return Json(new { ok = false, requestUrl = url, sslVerify = req.VerifyCertificate, status = 0, reason = (string?)null,
                    headers = Array.Empty<object>(), body = (string?)null, error = z.Fehler, zugang = z.Quelle });
            // v1.99.92 SD-1: gespeicherte Zugangsdaten nur an die gespeicherte Adresse, nie mit schwaecherer TLS-Pruefung - sonst kein Aufruf
            if (z.Quelle != "keine"
                && SdataZugangsdaten.SperreGespeicherterZugang(url, req.VerifyCertificate, stand.BaseUrl, stand.ZertifikatPruefen) is { } sperre)
                return Json(new { ok = false, requestUrl = url, sslVerify = req.VerifyCertificate, status = 0, reason = (string?)null,
                    headers = Array.Empty<object>(), body = (string?)null, error = sperre, zugang = "gespeichert – nicht verwendet" });
            req.Username = z.Benutzer;
            req.Password = z.Kennwort;
            zugangsQuelle = z.Quelle == "keine" ? "keine gespeicherten Zugangsdaten" : $"gespeichert ({z.Quelle})";
        }

        using var handler = new HttpClientHandler();
        if (!req.VerifyCertificate)
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        using var message = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrEmpty(req.Username) || !string.IsNullOrEmpty(req.Password))
        {
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{req.Username}:{req.Password}"));
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        }

        try
        {
            // v1.99.92 SD-2a: nur die Kopfzeilen abwarten, Body hoechstens 50.000 Zeichen lesen (Dispose bricht den Rest ab)
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, ct);
            var body = await SdataZugangsdaten.LeseHoechstensAsync(await response.Content.ReadAsStreamAsync(ct), 50000, ct);
            var headers = response.Headers.Concat(response.Content.Headers)
                .Select(h => new { name = h.Key, value = string.Join(", ", h.Value) })
                .ToList();

            return Json(new
            {
                ok = response.IsSuccessStatusCode,
                requestUrl = url,
                sslVerify = req.VerifyCertificate,
                status = (int)response.StatusCode,
                reason = response.ReasonPhrase,
                headers,
                body,
                error = (string?)null,
                zugang = zugangsQuelle
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                ok = false,
                requestUrl = url,
                sslVerify = req.VerifyCertificate,
                status = 0,
                reason = (string?)null,
                headers = Array.Empty<object>(),
                body = (string?)null,
                error = ex.Message,
                zugang = zugangsQuelle
            });
        }
    }

    private async Task<ServiceSettingsViewModel> BuildViewModelAsync(IReadOnlyDictionary<string, string>? eingaben = null)
    {
        var dbRows = await _repository.GetAllAsync() ?? new List<ServiceSetting>();
        var dbByKey = dbRows.ToDictionary(s => s.Key, s => (string?)s.Value, StringComparer.Ordinal);
        if (eingaben != null)
            foreach (var (k, v) in eingaben.Where(e => ServiceSettingDefinitions.TryGet(e.Key, out _)))
                dbByKey[k] = v ?? "";

        // v1.77.0 E3: SettingItem nach Katalog (Bool „leer = Default“ zeigt den wirksamen Zustand, AK 11; Choice exakt wie der Dienst liest).
        var echt = dbRows.ToDictionary(s => s.Key, s => (string?)s.Value, StringComparer.Ordinal);
        var items = ServiceSettingDefinitions.All
            .Select(def => def.Type == SettingType.Geheim
                // v1.99.14: Status aus dem GESPEICHERTEN Wert, nie aus der Eingabe; der Wert selbst erreicht die View nie
                ? new SettingItem { Definition = def, Wirksam = "", GeheimStatus = SdataZugangsdaten.Status(echt.GetValueOrDefault(def.Key), _schutz) }
                : SettingItem.Aus(def, dbByKey.GetValueOrDefault(def.Key), exakt: true))
            .ToList();
        var catalogKeys = ServiceSettingDefinitions.All.Select(d => d.Key).ToHashSet(StringComparer.Ordinal);
        return new ServiceSettingsViewModel
        {
            Groups = SettingGroup.Bilden(items),
            OrphanEntries = dbRows.Where(r => !catalogKeys.Contains(r.Key)).ToList(),
        };
    }

    // --- Freitext-Fallback fuer Orphan-/Ad-hoc-Keys (nicht mehr Hauptpfad) ---

    public IActionResult Create()
    {
        return View(new ServiceSetting());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceSetting setting)
    {
        if (KatalogKeyAndereSchreibweise(setting.Key) is { } kk) return SchreibweiseAbgelehnt(setting.Key!, kk);
        if (IstGeheim(setting.Key)) return GeheimAbgelehnt();
        if (!ModelState.IsValid)
            return View(setting);

        await _repository.UpsertAsync(setting.Key, setting.Value, setting.Category, setting.Description);
        TempData["SuccessMessage"] = "Einstellung gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        if (IstGeheim(id)) return GeheimAbgelehnt();
        var settings = await _repository.GetAllAsync();
        var item = settings.FirstOrDefault(s => s.Key == id);
        if (item == null)
            return NotFound();

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ServiceSetting setting)
    {
        if (id != setting.Key)
            return NotFound();
        if (KatalogKeyAndereSchreibweise(id) is { } kk) return SchreibweiseAbgelehnt(id, kk);
        if (IstGeheim(id)) return GeheimAbgelehnt();

        if (!ModelState.IsValid)
            return View(setting);

        await _repository.UpsertAsync(setting.Key, setting.Value, setting.Category, setting.Description);
        TempData["SuccessMessage"] = "Einstellung gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (KatalogKeyAndereSchreibweise(id) is { } kk) return SchreibweiseAbgelehnt(id, kk);
        if (IstGeheim(id)) return GeheimAbgelehnt();
        await _repository.DeleteAsync(id);
        TempData["SuccessMessage"] = "Einstellung gelöscht.";
        return RedirectToAction(nameof(Index));
    }
}
