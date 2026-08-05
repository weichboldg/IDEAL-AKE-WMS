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

namespace IdealAkeWms.Controllers;

[RequireAdminAccess]
public class ServiceSettingsController : Controller
{
    private readonly IServiceSettingRepository _repository;

    public ServiceSettingsController(IServiceSettingRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index()
    {
        var vm = await BuildViewModelAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSettings(Dictionary<string, string> settings)
    {
        settings ??= new Dictionary<string, string>();

        foreach (var (key, rawValue) in settings)
        {
            if (!ServiceSettingDefinitions.TryGet(key, out var def))
                continue; // unbekannte Keys ignorieren (Orphans laufen ueber Edit/Delete)

            var value = rawValue ?? string.Empty;

            switch (def.Type)
            {
                case ServiceSettingType.Bool:
                    // Checkbox/Hidden liefert "true"/"false"; jeder truthy-Wert -> "true".
                    var isTrue = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                              || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase)
                              || value == "1";
                    await _repository.UpsertAsync(key, isTrue ? "true" : "false", def.Category, def.Description);
                    break;

                case ServiceSettingType.Int:
                    if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    {
                        ModelState.AddModelError(key, $"'{def.Key}' erwartet eine ganze Zahl (war: '{value}').");
                        continue; // diesen Key NICHT speichern
                    }
                    await _repository.UpsertAsync(key, i.ToString(CultureInfo.InvariantCulture), def.Category, def.Description);
                    break;

                default: // String
                    await _repository.UpsertAsync(key, value, def.Category, def.Description);
                    break;
            }
        }

        if (!ModelState.IsValid)
        {
            // Fehler sichtbar machen: View mit gemergtem Stand zurueckgeben.
            var vm = await BuildViewModelAsync();
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
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseContentRead, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
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
                body = body.Length > 50000 ? body.Substring(0, 50000) + "\n… (gekuerzt)" : body,
                error = (string?)null
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
                error = ex.Message
            });
        }
    }

    private async Task<ServiceSettingsViewModel> BuildViewModelAsync()
    {
        var dbRows = await _repository.GetAllAsync() ?? new List<ServiceSetting>();
        var dbByKey = dbRows.ToDictionary(s => s.Key, StringComparer.Ordinal);

        var vm = new ServiceSettingsViewModel();

        // Kategorie-Reihenfolge = Reihenfolge des ersten Vorkommens im Katalog (stabil).
        foreach (var def in ServiceSettingDefinitions.All)
        {
            var value = dbByKey.TryGetValue(def.Key, out var row) && row.Value != null
                ? row.Value
                : def.DefaultValue;

            var group = vm.Groups.FirstOrDefault(g => g.Category == def.Category);
            if (group == null)
            {
                group = new ServiceSettingGroup { Category = def.Category };
                vm.Groups.Add(group);
            }
            group.Items.Add(new ServiceSettingItem
            {
                Key = def.Key,
                Type = def.Type,
                Value = value,
                Description = def.Description,
                Multiline = def.Multiline
            });
        }

        // Orphans = DB-Keys ohne Katalog-Eintrag.
        var catalogKeys = ServiceSettingDefinitions.All.Select(d => d.Key).ToHashSet(StringComparer.Ordinal);
        vm.OrphanEntries = dbRows.Where(r => !catalogKeys.Contains(r.Key)).ToList();

        return vm;
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
        if (!ModelState.IsValid)
            return View(setting);

        await _repository.UpsertAsync(setting.Key, setting.Value, setting.Category, setting.Description);
        TempData["SuccessMessage"] = "Einstellung gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
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
        await _repository.DeleteAsync(id);
        TempData["SuccessMessage"] = "Einstellung gelöscht.";
        return RedirectToAction(nameof(Index));
    }
}
