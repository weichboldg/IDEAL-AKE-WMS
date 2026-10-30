using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SageSdata;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

/// <summary>
/// Spec 2026-10-30-sage-sdata-fa-ansicht (v1.71.0): FA-Struktur und Stueckliste LIVE aus Sage per SData RealTimeData —
/// Machbarkeitsnachweis, nur lesen. Keine Persistenz, kein Schreiben an Sage, kein Bezug zu <c>FaHierarchy*</c>.
/// Zugriff: derselbe Class-Level-Read-Filter wie <see cref="FaHierarchyController"/>; Gate <c>SageSdataFaAnsichtAktiv</c>
/// inline (Default aus, nur exakt "true"; Semantik wie RequireFaHierarchyKommissionierlistenAktivFilter).
/// <para>Seitenaufbau = 5 Aufrufe, sequenziell (Sage-Last): BelID → StrukturID, Struktur, dann je Quelle ein or-Aufruf
/// ueber alle BelIDs (Bloecke zu 50). Nach dem ersten Verbindungsfehler werden die uebrigen Aufrufe uebersprungen;
/// HTTP-Fehler einer Quelle brechen nicht ab.</para>
/// </summary>
[RequirePickingOrTrackingOrLeitstandAccess]
public class SageSdataFaController : Controller
{
    private readonly ISageSdataReadClient _client;
    private readonly IAppSettingRepository _appSettings;
    private readonly IServiceSettingRepository _serviceSettings;
    private readonly IConfiguration _configuration;

    public SageSdataFaController(ISageSdataReadClient client, IAppSettingRepository appSettings,
        IServiceSettingRepository serviceSettings, IConfiguration configuration)
    {
        _client = client;
        _appSettings = appSettings;
        _serviceSettings = serviceSettings;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? belId, CancellationToken ct)
    {
        var aktiv = string.Equals(await _appSettings.GetValueAsync(AppSettingKeys.SageSdataFaAnsichtAktiv), "true", StringComparison.OrdinalIgnoreCase);
        if (!aktiv)
        {
            TempData["WarningMessage"] = "Die Sage-Live-Ansicht der FA-Struktur ist deaktiviert.";
            return RedirectToAction("Index", "Home");
        }

        var vm = new SageSdataFaViewModel { BelId = belId };
        var baseUrl = (await _serviceSettings.GetValueAsync("SData:BaseUrl"))?.Trim();
        var application = (await _serviceSettings.GetValueAsync("SData:Application"))?.Trim();
        var dataset = (await _serviceSettings.GetValueAsync("SData:Dataset"))?.Trim();
        var username = _configuration["SageLagerbuchung:Username"];
        var password = _configuration["SageLagerbuchung:Password"] ?? string.Empty;
        var pruefen = SageTlsPolicy.ShouldVerifyCertificate(await _serviceSettings.GetValueAsync(SageTlsPolicy.SettingKey));
        vm.ZertifikatPruefungAus = !pruefen;

        if (string.IsNullOrEmpty(baseUrl)) vm.NichtKonfiguriert.Add("SData:BaseUrl");
        else if (baseUrl.Contains('@')) // user:pw@host — Zugangsdaten nie in URL/Diagnose (Freigabe-Nachtrag 6)
            vm.NichtKonfiguriert.Add("SData:BaseUrl enthält Zugangsdaten (die gehören nicht in die URL)");
        if (string.IsNullOrEmpty(dataset)) vm.NichtKonfiguriert.Add("SData:Dataset");
        if (string.IsNullOrWhiteSpace(username)) vm.NichtKonfiguriert.Add("SageLagerbuchung:Username");
        if (vm.NichtKonfiguriert.Count > 0)
            return View(vm);

        if (!ModelState.IsValid || belId is <= 0)
        {
            ModelState.Clear();
            ModelState.AddModelError(nameof(belId), "Bitte eine FA-Nummer (ganze Zahl größer 0) eingeben.");
            vm.BelId = null;
            return View(vm);
        }
        if (belId is null)
            return View(vm);

        var ziel = new SdataLeseZiel(baseUrl!, string.IsNullOrEmpty(application) ? "ol" : application, dataset!, username!, password, pruefen);
        await LadeAsync(vm, ziel, belId.Value, ct);
        return View(vm);
    }

    private async Task LadeAsync(SageSdataFaViewModel vm, SdataLeseZiel ziel, int belId, CancellationToken ct)
    {
        var abgebrochen = false;

        async Task<SdataErgebnis<T>?> Aufruf<T>(string quelle, string where, string select, string? orderBy)
        {
            if (abgebrochen)
            {
                vm.Aufrufe.Add(SdataCall.Uebersprungene(quelle));
                return null;
            }
            var e = await _client.GetAsync<T>(ziel, quelle, where, select, orderBy, ct);
            vm.Aufrufe.Add(e.Call);
            if (e.Call.Verbindungsfehler)
            {
                abgebrochen = true;
                vm.Fehler.Add($"Sage nicht erreichbar: {e.Call.Fehler}. Weitere Aufrufe wurden übersprungen.");
            }
            return e;
        }

        void RestUeberspringen(params string[] quellen)
        {
            foreach (var q in quellen) vm.Aufrufe.Add(SdataCall.Uebersprungene(q));
        }

        // 1. BelID → StrukturID (Haupt- und Sub-FA ueber denselben Weg).
        var such = await Aufruf<SageFaKopf>(SageSdataQuellen.FaBelegsuche, $"BelID eq {belId}", "BelID,StrukturID", null);
        if (such is null || !such.Call.Erfolgreich)
        {
            if (such is not null && !such.Call.Verbindungsfehler)
                vm.Fehler.Add($"FA-Suche fehlgeschlagen ({such.Call.Fehler}).");
            RestUeberspringen(SageSdataQuellen.FaBelegsuche, SageSdataQuellen.Teileverwendungsnachweis, SageSdataQuellen.RueckKorrektur, SageSdataQuellen.Fertigmeldungen);
            return;
        }
        if (such.Zeilen.Count == 0)
        {
            vm.Fehler.Add($"BelID {belId} in Sage nicht gefunden.");
            return;
        }
        var strukturId = such.Zeilen[0].StrukturID;
        if (strukturId <= 0)
        {
            vm.Fehler.Add($"BelID {belId}: Struktur ohne gültige StrukturID ({strukturId}) – keine weiteren Aufrufe.");
            return;
        }
        if (strukturId != belId)
            vm.Hinweis = $"BelID {belId} gehört zur Struktur {strukturId} – angezeigt wird die ganze Struktur.";

        // 2. Struktur.
        var struktur = await Aufruf<SageFaKopf>(SageSdataQuellen.FaBelegsuche, $"StrukturID eq {strukturId}", SageSdataQuellen.FaSelect, "BelID");
        if (struktur is null || !struktur.Call.Erfolgreich)
        {
            if (struktur is not null && !struktur.Call.Verbindungsfehler)
                vm.Fehler.Add($"Struktur {strukturId} nicht geladen ({struktur.Call.Fehler}).");
            RestUeberspringen(SageSdataQuellen.Teileverwendungsnachweis, SageSdataQuellen.RueckKorrektur, SageSdataQuellen.Fertigmeldungen);
            return;
        }
        if (struktur.Zeilen.Count == 0)
        {
            vm.Fehler.Add($"Struktur {strukturId} in Sage nicht gefunden.");
            return;
        }

        var ids = struktur.Zeilen.Select(f => f.BelID).ToList();
        var bloecke = SageSdataQuellen.OrBloecke("BelID", ids)
            .Zip(ids.Distinct().Chunk(SageSdataQuellen.MaxBelIdsJeAufruf), (where, blockIds) => (where, blockIds)).ToList();

        // 3.–5. Je Quelle ein or-Aufruf je Block.
        async Task<List<T>> LadeBloecke<T>(string quelle, string select, Dictionary<int, string> fehlend, string text)
        {
            var alle = new List<T>();
            foreach (var (where, blockIds) in bloecke)
            {
                var e = await Aufruf<T>(quelle, where, select, null);
                if (e is not null) alle.AddRange(e.Zeilen);
                var grund = e is null ? "übersprungen" : e.Call.Erfolgreich ? null : e.Call.Fehler;
                if (grund is null) continue;
                foreach (var id in blockIds)
                    fehlend[id] = fehlend.TryGetValue(id, out var alt) ? $"{alt}; {text} nicht geladen ({grund})" : $"{text} nicht geladen ({grund})";
                if (e is { Call.Verbindungsfehler: false })
                    vm.Fehler.Add($"{text}: {e.Call.Fehler} – Details in der Diagnose.");
            }
            return alle;
        }

        var positionen = await LadeBloecke<SageFaPosition>(SageSdataQuellen.Teileverwendungsnachweis, SageSdataQuellen.PositionSelect, vm.PositionenNichtGeladen, "Positionen");
        var rueck = await LadeBloecke<SageRueckmeldung>(SageSdataQuellen.RueckKorrektur, SageSdataQuellen.RueckmeldungSelect, vm.RueckmeldungenNichtGeladen, "Rückmeldungen");
        var fertig = await LadeBloecke<SageRueckmeldung>(SageSdataQuellen.Fertigmeldungen, SageSdataQuellen.RueckmeldungSelect, vm.RueckmeldungenNichtGeladen, "Fertigmeldungen");

        vm.Struktur = SageSdataFaTreeBuilder.Build(struktur.Zeilen, positionen);
        vm.Rueckmeldungen = SageSdataFaTreeBuilder.Summiere(rueck);
        vm.Fertigmeldungen = SageSdataFaTreeBuilder.Summiere(fertig);
    }
}
