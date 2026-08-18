using System.Globalization;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.Standort;
using IdealAkeWms.Services.HierarchischeStruktur;
using IdealAkeWms.Services.Standort;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

/// <summary>
/// IDEAL Teil 6: kuratierte Standorteinstellungen-Maske. Admin-only (kein neuer Filter/keine neue
/// Rolle, wie ServiceSettings/Settings). Bündelt eine explizite Feldliste
/// (<see cref="StandortSettingsCatalog"/>) über AppSettings UND ServiceSettings — beide werden pro
/// Speichervorgang atomar geschrieben (<see cref="IStandortSettingsWriter"/>). Der Master-Einwegtor
/// <c>ProduktionsauftragHierarchisch</c> erscheint nur read-only; er ist NICHT im Katalog und wird
/// daher nie geschrieben (Allow-List schützt gegen manipulierte Requests, AK 2). Kein Guard-Aufruf.
/// </summary>
[RequireAdminAccess]
public class StandortEinstellungenController : Controller
{
    private readonly IAppSettingRepository _appSettings;
    private readonly IServiceSettingRepository _serviceSettings;
    private readonly IStandortSettingsWriter _writer;
    private readonly HierarchischeStrukturStatus _masterStatus;

    public StandortEinstellungenController(IAppSettingRepository appSettings,
        IServiceSettingRepository serviceSettings, IStandortSettingsWriter writer,
        HierarchischeStrukturStatus masterStatus)
    {
        _appSettings = appSettings;
        _serviceSettings = serviceSettings;
        _writer = writer;
        _masterStatus = masterStatus;
    }

    public async Task<IActionResult> Index()
        => View(await BuildViewModelAsync(null));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Dictionary<string, string> settings)
    {
        settings ??= new Dictionary<string, string>();

        var appKv = new Dictionary<string, string?>();
        var svc = new List<(string Key, string Value, string? Category, string? Description)>();

        // Nur Katalog-Keys verarbeiten (Allow-List). Ein nicht gelisteter Key — insbesondere der
        // Master — wird ignoriert und daher nie geschrieben.
        foreach (var f in StandortSettingsCatalog.Fields)
        {
            // Feld nicht im POST -> unverändert lassen (die Maske sendet normalerweise ALLE Felder;
            // ein fehlendes Feld ist ein Teil-Request und soll weder erzwungen validiert noch
            // überschrieben werden).
            if (!settings.TryGetValue(f.Key, out var raw))
                continue;
            raw ??= string.Empty;

            string val;
            switch (f.Type)
            {
                case StandortFieldType.Bool:
                    val = IsTrue(raw) ? "true" : "false"; // Checkbox+Hidden -> "true"/"false"
                    break;
                case StandortFieldType.Int:
                    if (!int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    {
                        ModelState.AddModelError(f.Key, $"'{f.Label}' erwartet eine ganze Zahl (war: '{raw}').");
                        continue; // diesen Key NICHT übernehmen
                    }
                    val = i.ToString(CultureInfo.InvariantCulture);
                    break;
                default:
                    val = raw;
                    break;
            }

            if (f.Backend == StandortBackend.App)
            {
                appKv[f.Key] = val;
            }
            else
            {
                ServiceSettingDefinitions.TryGet(f.Key, out var def);
                svc.Add((f.Key, val, def?.Category, def?.Description));
            }
        }

        // Validierung VOR dem Schreiben: schlägt ein Int fehl, wird NICHTS geschrieben (kein
        // Partial-Save) und die Maske zeigt den Fehler mit den eingegebenen Werten (AK 4).
        if (!ModelState.IsValid)
            return View(nameof(Index), await BuildViewModelAsync(settings));

        await _writer.SaveAtomicAsync(appKv, svc);
        TempData["SuccessMessage"] = "Standorteinstellungen gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<StandortEinstellungenViewModel> BuildViewModelAsync(IDictionary<string, string>? posted)
    {
        var vm = new StandortEinstellungenViewModel();

        foreach (var groupName in StandortSettingsCatalog.GroupOrder)
        {
            var group = new StandortFieldGroup { Name = groupName };
            foreach (var f in StandortSettingsCatalog.Fields.Where(x => x.Group == groupName))
            {
                string? current;
                if (posted != null && posted.TryGetValue(f.Key, out var p))
                    current = p; // eingegebener Wert bei Fehler-Redisplay
                else
                    current = f.Backend == StandortBackend.App
                        ? await _appSettings.GetValueAsync(f.Key)
                        : await _serviceSettings.GetValueAsync(f.Key);

                group.Fields.Add(new StandortFieldView
                {
                    Key = f.Key,
                    Label = f.Label,
                    Type = f.Type,
                    Hint = f.Hint,
                    Value = current,
                    BoolValue = f.Type == StandortFieldType.Bool && IsTrue(current)
                });
            }
            vm.Groups.Add(group);
        }

        var masterVal = await _serviceSettings.GetValueAsync(StandortSettingsCatalog.MasterKey);
        vm.Master = new MasterStatusView
        {
            Enabled = IsTrue(masterVal),
            Locked = _masterStatus.HierarchicalDataExists,
            AsOfUtc = _masterStatus.LastRefreshedUtc
        };
        return vm;
    }

    private static bool IsTrue(string? v) =>
        v != null && (v.Equals("true", StringComparison.OrdinalIgnoreCase)
                   || v.Equals("on", StringComparison.OrdinalIgnoreCase)
                   || v == "1");
}
