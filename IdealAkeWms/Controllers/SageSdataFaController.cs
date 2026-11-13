using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SageSdata;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

/// <summary>
/// Spec 2026-10-30-sage-sdata-fa-ansicht (v1.71.0): FA-Struktur und Stueckliste LIVE aus Sage per SData RealTimeData —
/// Machbarkeitsnachweis, nur lesen. Keine Persistenz, kein Schreiben an Sage, kein Bezug zu <c>FaHierarchy*</c>.
/// v1.99.14 (Spec 2026-11-13-sdata-teil-2-userfelder-stueckliste F5): Testwerkzeug im Register Sage-Test, nur Admin; Gate
/// <c>SageSdataFaAnsichtAktiv</c> entfaellt; Endpunkt + Zugangsdaten ueber <see cref="SdataZugangsdaten.WebStandAsync"/> (F6).
/// <para>Seitenaufbau = 5 Aufrufe, sequenziell (Sage-Last): BelID → StrukturID, Struktur, dann je Quelle ein or-Aufruf
/// ueber alle BelIDs (Bloecke zu 50). Nach dem ersten Verbindungsfehler werden die uebrigen Aufrufe uebersprungen;
/// HTTP-Fehler einer Quelle brechen nicht ab.</para>
/// </summary>
[RequireAdminAccess]
public class SageSdataFaController : Controller
{
    private readonly ISageSdataReadClient _client;
    private readonly IServiceSettingRepository _serviceSettings;
    private readonly IConfiguration _configuration;
    private readonly IDataProtectionProvider _schutz;

    public SageSdataFaController(ISageSdataReadClient client, IServiceSettingRepository serviceSettings, IConfiguration configuration,
        IDataProtectionProvider schutz)
    {
        _client = client;
        _serviceSettings = serviceSettings;
        _configuration = configuration;
        _schutz = schutz;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? belId, CancellationToken ct)
    {
        var vm = new SageSdataFaViewModel { BelId = belId };
        var stand = await SdataZugangsdaten.WebStandAsync(_serviceSettings, _configuration, _schutz);
        vm.ZertifikatPruefungAus = !stand.ZertifikatPruefen;
        vm.NichtKonfiguriert.AddRange(stand.NichtKonfiguriert);
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

        await LadeAsync(vm, stand.Ziel!, belId.Value, ct);
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
