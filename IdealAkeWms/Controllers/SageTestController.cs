using System.Text.Json;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services.SageSdata;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

/// <summary>
/// Spec 2026-11-13-sdata-teil-2-userfelder-stueckliste F1–F4: Register „Sage-Test“ in den Einstellungen — Konfigurationsstand,
/// Verbindungstest (Logik bleibt <see cref="ServiceSettingsController.TestSageConnection"/>), Feldliste, Stueckliste live; FA-Struktur live
/// ist <see cref="SageSdataFaController"/>. Nur Admin (Freigabe-Antwort 3), nur GET an Sage, nichts wird gespeichert. Ohne Adresse/Dataset bzw.
/// mit nicht entschluesselbarem Kennwort geht keine Anfrage an Sage.
/// </summary>
[RequireAdminAccess]
public class SageTestController : Controller
{
    private readonly ISageSdataReadClient _client;
    private readonly IServiceSettingRepository _settings;
    private readonly IConfiguration _configuration;
    private readonly IDataProtectionProvider _schutz;

    public SageTestController(ISageSdataReadClient client, IServiceSettingRepository settings, IConfiguration configuration,
        IDataProtectionProvider schutz)
    {
        _client = client;
        _settings = settings;
        _configuration = configuration;
        _schutz = schutz;
    }

    private Task<SdataWebStand> StandAsync() => SdataZugangsdaten.WebStandAsync(_settings, _configuration, _schutz);

    [HttpGet]
    public async Task<IActionResult> Index() => View(await StandAsync());

    [HttpGet]
    public async Task<IActionResult> Feldliste(SageTestFeldlisteViewModel eingabe, CancellationToken ct)
    {
        ModelState.Clear(); // Pruefung unten (ADR 0020: Server prueft, deutsche Meldungen)
        var vm = eingabe;
        vm.Stand = await StandAsync();
        if (!Request.Query.ContainsKey(nameof(SageTestFeldlisteViewModel.Ressource)))
            return View(vm); // erster Aufruf: nur Formular

        vm.Ressource = vm.Ressource?.Trim();
        vm.OrderBy = vm.OrderBy?.Trim();
        vm.Where = string.IsNullOrWhiteSpace(vm.Where) ? null : vm.Where.Trim();
        vm.Include = string.IsNullOrWhiteSpace(vm.Include) ? null : vm.Include.Trim();
        if (!SageTestFeldlisteViewModel.Vertraege.Contains(vm.Vertrag))
            vm.Fehler.Add("Vertrag nur aus der Liste.");
        if (string.IsNullOrEmpty(vm.Ressource) || vm.Ressource.IndexOfAny(new[] { '/', '?', '&', '#' }) >= 0)
            vm.Fehler.Add("Ressource angeben, ohne /, ?, & und #.");
        if (string.IsNullOrEmpty(vm.OrderBy))
            vm.Fehler.Add("orderBy ist Pflicht (Sage antwortet sonst mit HTTP 500).");
        if (vm.Vertrag == SageSdataReadClient.ServiceContract && vm.Where is null)
            vm.Fehler.Add("Bei RealTimeData ist where Pflicht (sonst liest Sage die ganze Quelle).");
        if (vm.Anzahl is < 1 or > 5)
            vm.Fehler.Add("Anzahl Beispielzeilen: 1 bis 5.");
        if (vm.Fehler.Count > 0 || vm.Stand.Ziel is not { } ziel)
            return View(vm); // keine Anfrage an Sage

        vm.Ausgefuehrt = true;
        // 1. Schema der Ressource; am AKE-Test antwortet {Ressource}/$schema mit 401 (Lesetest L1) -> Vertrags-Schema (nicht bei RealTimeData, ~6 MB)
        try
        {
            vm.Schema = SageTestAufbereitung.SchemaLesen(await _client.GetSchemaAsync(ziel, vm.Vertrag, vm.Ressource, ct), vm.Ressource!);
        }
        catch (SdataLeseException ex) when (ex.Status != 0)
        {
            vm.Hinweise.Add($"Schema der Ressource nicht lesbar (HTTP {ex.Status}).");
        }
        catch (SdataLeseException ex)
        {
            vm.Fehler.Add($"Sage nicht erreichbar: {ex.Diagnose}");
            return View(vm);
        }
        if (vm.Schema is null && vm.Vertrag != SageSdataReadClient.ServiceContract)
        {
            try
            {
                vm.Schema = SageTestAufbereitung.SchemaLesen(await _client.GetSchemaAsync(ziel, vm.Vertrag, null, ct), vm.Ressource!);
                vm.Hinweise.Add(vm.Schema is null ? $"Ressource {vm.Ressource} steht nicht im Schema des Vertrags." : "Felder aus dem Schema des Vertrags.");
            }
            catch (SdataLeseException ex)
            {
                vm.Hinweise.Add($"Schema nicht lesbar (HTTP {ex.Status}: {ex.Diagnose}).");
            }
        }

        // 2. Eine Seite Daten ohne select (alle Felder)
        try
        {
            var seite = await _client.QueryPageAsync(ziel, vm.Vertrag, vm.Ressource!, vm.Where, vm.OrderBy!, vm.Include, vm.Anzahl, ct);
            var zeilen = seite.Zeilen.Take(vm.Anzahl).ToList(); // RealTimeData ignoriert count
            vm.Zeilen = zeilen.Count;
            vm.Gesamt = seite.Gesamt;
            vm.DauerMs = seite.DauerMs;
            vm.Url = seite.Url;
            vm.Felder = SageTestAufbereitung.Feldliste(vm.Schema, zeilen);
            if (zeilen.Count == 0) vm.Hinweise.Add("Sage lieferte keine Datenzeile – Beispielwerte fehlen.");
        }
        catch (SdataLeseException ex)
        {
            vm.Fehler.Add(ex.Status == 0 ? $"Sage nicht erreichbar: {ex.Diagnose}" : $"HTTP {ex.Status}: {ex.Diagnose}");
            vm.Felder = SageTestAufbereitung.Feldliste(vm.Schema, Array.Empty<JsonElement>());
        }
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Stueckliste(SageTestStuecklisteViewModel eingabe, CancellationToken ct)
    {
        ModelState.Clear();
        var vm = eingabe;
        vm.Stand = await StandAsync();
        vm.Mandant ??= SageTestAufbereitung.MandantAusDataset(vm.Stand.Dataset);
        var artikel = vm.Artikelnummer?.Trim();
        vm.Artikelnummer = string.IsNullOrEmpty(artikel) ? null : artikel;
        if (vm.Artikelnummer is null && vm.FaNummer is null)
        {
            if (Request.Query.ContainsKey(nameof(SageTestStuecklisteViewModel.Artikelnummer)))
                vm.Fehler.Add("Artikelnummer oder FA-Nummer eingeben.");
            return View(vm);
        }
        if (vm.Artikelnummer is not null && vm.FaNummer is not null) vm.Fehler.Add("Entweder Artikelnummer oder FA-Nummer, nicht beides.");
        if (vm.Artikelnummer?.IndexOfAny(new[] { '\'', '&', '#' }) >= 0) vm.Fehler.Add("Artikelnummer ohne ', & und #.");
        if (vm.FaNummer is <= 0) vm.Fehler.Add("FA-Nummer: ganze Zahl größer 0.");
        if (vm.Mandant is null or < 0) vm.Fehler.Add("Mandant: ganze Zahl (steht im Dataset nach dem letzten ;).");
        if (vm.Fehler.Count > 0 || vm.Stand.Ziel is not { } ziel)
            return View(vm);

        vm.Ausgefuehrt = true;
        try
        {
            if (vm.FaNummer is { } fa) await LadeFaAsync(vm, ziel, fa, ct);
            else await LadeArtikelAsync(vm, ziel, vm.Artikelnummer!, vm.Mandant!.Value, ct);
        }
        catch (SdataLeseException ex)
        {
            vm.Fehler.Add(ex.Status == 0 ? $"Sage nicht erreichbar: {ex.Diagnose}" : $"HTTP {ex.Status}: {ex.Diagnose}");
        }
        return View(vm);
    }

    public const string RessourceListe = "PpsRessourcenListenInfo";
    public const string RessourcePositionen = "PpsRessourcenPositionenResolvedInfo";

    private async Task LadeArtikelAsync(SageTestStuecklisteViewModel vm, SdataLeseZiel ziel, string artikel, int mandant, CancellationToken ct)
    {
        var seite = await _client.QueryPageAsync(ziel, SageSdataReadClient.VertragResourcesSpecific, RessourceListe,
            $"Mandant eq {mandant} and Artikelnummer eq '{artikel}'", "Artikelnummer", $"{RessourcePositionen},SpecificUserfields", 1, ct);
        vm.Aufrufe.Add($"{seite.Url} – {seite.Zeilen.Count} Zeile(n), {seite.DauerMs} ms");
        if (seite.Zeilen.Count == 0)
        {
            vm.Fehler.Add($"Keine Ressourcenliste für Artikel {artikel} (Mandant {mandant}) in Sage gefunden.");
            return;
        }
        var kopf = seite.Zeilen[0];
        vm.Titel = $"Ressourcenliste {SageTestAufbereitung.Text(SageTestAufbereitung.Prop(kopf, "Artikelnummer"))} " +
                   $"{SageTestAufbereitung.Text(SageTestAufbereitung.Prop(kopf, "Matchcode"))}";
        vm.Kopf = SageTestAufbereitung.Flach(kopf, RessourcePositionen);
        var pos = SageTestAufbereitung.Prop(kopf, RessourcePositionen);
        vm.Zeilen = SageTestAufbereitung.RessourcenBaum(pos is { ValueKind: JsonValueKind.Array } p ? p.EnumerateArray().ToList() : new List<JsonElement>());
        if (vm.Zeilen.Count == 0) vm.Fehler.Add($"Ressourcenliste für Artikel {artikel} ohne Positionen.");
    }

    private async Task LadeFaAsync(SageTestStuecklisteViewModel vm, SdataLeseZiel ziel, int belId, CancellationToken ct)
    {
        var such = await _client.QueryPageAsync(ziel, SageSdataReadClient.VertragResources, "PpsFaBelegeInfo", $"BelID eq {belId}", "BelID", null, 1, ct);
        vm.Aufrufe.Add($"{such.Url} – {such.Zeilen.Count} Zeile(n), {such.DauerMs} ms");
        if (such.Zeilen.Count == 0)
        {
            vm.Fehler.Add($"FA {belId} in Sage nicht gefunden.");
            return;
        }
        var strukturId = SageTestAufbereitung.Zahl(SageTestAufbereitung.Prop(such.Zeilen[0], "StrukturID")) ?? belId;
        var fas = await _client.QueryAllAsync<JsonElement>(ziel, SageSdataReadClient.VertragResources, "PpsFaBelegeInfo",
            $"StrukturID eq {strukturId}", null, "BelID", ct);
        vm.Aufrufe.Add(fas.Zusammenfassung);
        var ids = fas.Zeilen.Select(f => SageTestAufbereitung.Zahl(SageTestAufbereitung.Prop(f, "BelID"))).OfType<int>().ToList();
        var positionen = new List<JsonElement>();
        foreach (var where in SageSdataQuellen.OrBloecke("BelID", ids))
        {
            var p = await _client.QueryAllAsync<JsonElement>(ziel, SageSdataReadClient.VertragResources, "PpsFaBelegePositionenInfo", where, null, "BelPosID", ct);
            vm.Aufrufe.Add(p.Zusammenfassung);
            positionen.AddRange(p.Zeilen);
        }
        vm.Titel = strukturId == belId ? $"FA-Struktur {strukturId}" : $"FA {belId} gehört zur Struktur {strukturId} – angezeigt wird die ganze Struktur";
        var wurzel = fas.Zeilen.FirstOrDefault(f => SageTestAufbereitung.Zahl(SageTestAufbereitung.Prop(f, "BelID")) == strukturId);
        vm.Kopf = wurzel.ValueKind == JsonValueKind.Object ? SageTestAufbereitung.Flach(wurzel) : new List<FlachesFeld>();
        vm.Zeilen = SageTestAufbereitung.FaBaum(fas.Zeilen, positionen, strukturId);
    }
}
