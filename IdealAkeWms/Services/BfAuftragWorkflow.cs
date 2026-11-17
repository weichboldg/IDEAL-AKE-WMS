using System.Globalization;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Services;

/// <summary>Ergebnis einer BF-Aktion: ein Text fuer TempData (Success bei <see cref="Ok"/>, sonst Warning), optional die Auftrags-Id.</summary>
public sealed record BfErgebnis(bool Ok, string Text, int? AuftragId = null);

/// <summary>
/// Fachliche Regeln der BF-Auftraege (Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv). Ein Aufruf je Request (nach einem
/// gescheiterten Speichern verwirft der Workflow den Change-Tracker). Rollenpruefung macht der Controller per Filter; hier nur
/// Eigentuemer (Besteller) und Ausgangsstatus (ADR 0026: der Server prueft am Schreibort).
/// </summary>
public class BfAuftragWorkflow
{
    public const int MaxNummerVersuche = 5;
    public const string Praefix = "BF";
    /// <summary>Default von <see cref="AppSettingKeys.BfStatuswerte"/>, wenn die Zeile fehlt.</summary>
    public const string StatuswerteDefault = "EILT";

    private readonly IBfAuftragRepository _repo;
    private readonly IAppSettingRepository _settings;
    private readonly IOrderRecipientRepository _groups;
    private readonly IStorageLocationRepository _lagerplaetze;
    private readonly ICurrentUserService _user;
    private readonly IHolidayRepository _holidays;
    private readonly IBusinessDayService _arbeitstage;
    private readonly TimeProvider _time;
    private readonly ILogger<BfAuftragWorkflow> _logger;

    public BfAuftragWorkflow(IBfAuftragRepository repo, IAppSettingRepository settings, IOrderRecipientRepository groups,
        IStorageLocationRepository lagerplaetze, ICurrentUserService user, IHolidayRepository holidays, IBusinessDayService arbeitstage,
        TimeProvider time, ILogger<BfAuftragWorkflow> logger)
    {
        _repo = repo;
        _settings = settings;
        _groups = groups;
        _lagerplaetze = lagerplaetze;
        _user = user;
        _holidays = holidays;
        _arbeitstage = arbeitstage;
        _time = time;
        _logger = logger;
    }

    private DateTime Jetzt => _time.GetLocalNow().DateTime;

    // ===================== Regeln (statisch, testbar) =====================

    public static string Nummer(int laufend) => $"{Praefix}{laufend:D8}";

    /// <summary>Mengenregel: Menge &gt; 0; ist die Sage-Bestellmenge &gt; 0, nur ganzes Vielfaches. Null = ok.
    /// Die Bestellmenge ist nur bei gleicher Basis-/Lagermengeneinheit gesetzt (V-B-Leser), sonst frei.</summary>
    public static string? MengenFehler(decimal menge, decimal? bestellmenge)
    {
        if (menge <= 0) return "Menge muss größer als 0 sein.";
        if (bestellmenge is > 0 && menge % bestellmenge.Value != 0)
            return $"Menge muss ein ganzes Vielfaches der Bestellmenge {bestellmenge.Value.ToString("0.###", CultureInfo.GetCultureInfo("de-AT"))} sein.";
        return null;
    }

    /// <summary>Werte der Einstellung <see cref="AppSettingKeys.BfStatuswerte"/> (Zeile fehlt = <see cref="StatuswerteDefault"/>, leer = keine).</summary>
    public static List<string> Statuswerte(string? setting) =>
        (setting ?? StatuswerteDefault).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToList();

    public async Task<List<string>> GetStatuswerteAsync() => Statuswerte(await _settings.GetValueAsync(AppSettingKeys.BfStatuswerte));

    public static bool IstEigentuemer(BfAuftrag a, int? userId) => a.BestellerUserId != null && a.BestellerUserId == userId;

    // ===================== Warenkorb (Besteller, Entwurf) =====================

    public async Task<BfErgebnis> HinzufuegenAsync(string articleNumber, decimal menge, string? status, string? hauptlagerplatz)
    {
        var userId = _user.GetCurrentAppUserId();
        if (userId is null) return new(false, "Kein WMS-Benutzer angemeldet – Bestellen nicht möglich.");
        var artikel = await _repo.GetArtikelAsync(articleNumber?.Trim() ?? "");
        if (artikel is null) return new(false, $"Artikel {articleNumber} ist kein bestellbares Blechteil (Lagerartikel BG/FT).");
        if (MengenFehler(menge, artikel.Bestellmenge) is { } mf) return new(false, $"{artikel.ArticleNumber}: {mf}");
        var (stOk, st) = await StatusPruefenAsync(status);
        if (!stOk) return new(false, st!);
        var (lpOk, lp) = await LagerplatzPruefenAsync(hauptlagerplatz, artikel.Hauptlagerplatz);
        if (!lpOk) return new(false, lp!);

        var a = await _repo.GetEntwurfAsync(userId.Value);
        var jetzt = Jetzt;
        if (a is null)
        {
            a = new BfAuftrag { Status = BfAuftragStatus.Entwurf, BestellerUserId = userId, BestellerName = _user.GetDisplayName() };
            Angelegt(a, jetzt);
            _repo.Add(a);
        }
        var p = new BfAuftragPosition
        {
            Position = a.Positionen.Count == 0 ? 1 : a.Positionen.Max(x => x.Position) + 1,
            ArticleNumber = artikel.ArticleNumber, Matchcode = artikel.Matchcode, Teileart = artikel.Teileart, Material = artikel.Material,
            Materialnummer = artikel.Materialnummer, FertigungsInfoPN = artikel.FertigungsInfoPN, Hauptlagerplatz = lp,
            Menge = menge, SageBestellmenge = artikel.Bestellmenge, Status = st,
        };
        Angelegt(p, jetzt);
        a.Positionen.Add(p);
        if (a.Id != 0) Geaendert(a, jetzt);
        return await SpeichernAsync("Warenkorb", async () =>
        {
            await _repo.SaveChangesAsync();
            var hinweis = string.IsNullOrWhiteSpace(p.Materialnummer) ? " Hinweis: keine Materialnummer – die Position wird nicht an WiCAM übergeben." : "";
            return new BfErgebnis(true, $"{p.ArticleNumber} hinzugefügt.{hinweis}", a.Id);
        });
    }

    /// <summary>Menge/Status/Lagerplatz einer Position: Besteller im Entwurf (eigener Auftrag), Schachtler im Status Bestellt (ohne Status).</summary>
    public async Task<BfErgebnis> PositionAendernAsync(int auftragId, int positionId, decimal menge, string? status, string? hauptlagerplatz,
        bool alsSchachtler, byte[]? rowVersion)
    {
        var a = await _repo.GetMitPositionenAsync(auftragId);
        if (a is null) return new(false, "BF-Auftrag nicht gefunden.");
        if (PruefeBearbeitung(a, alsSchachtler) is { } f) return new(false, f, a.Id);
        var p = a.Positionen.FirstOrDefault(x => x.Id == positionId);
        if (p is null) return new(false, "Position nicht gefunden.", a.Id);
        if (MengenFehler(menge, p.SageBestellmenge) is { } mf) return new(false, $"Pos {p.Position}: {mf}", a.Id);
        var (lpOk, lp) = await LagerplatzPruefenAsync(hauptlagerplatz, p.Hauptlagerplatz);
        if (!lpOk) return new(false, lp!, a.Id);
        if (!alsSchachtler)
        {
            var (stOk, st) = await StatusPruefenAsync(status, p.Status);
            if (!stOk) return new(false, st!, a.Id);
            p.Status = st;
        }
        p.Menge = menge;
        p.Hauptlagerplatz = lp;
        var jetzt = Jetzt;
        Geaendert(p, jetzt);
        Geaendert(a, jetzt);
        return await MitRowVersionAsync(a, rowVersion, $"Pos {p.Position} gespeichert.");
    }

    public async Task<BfErgebnis> PositionEntfernenAsync(int auftragId, int positionId, byte[]? rowVersion)
    {
        var a = await _repo.GetMitPositionenAsync(auftragId);
        if (a is null) return new(false, "BF-Auftrag nicht gefunden.");
        if (PruefeBearbeitung(a, alsSchachtler: false) is { } f) return new(false, f, a.Id);
        var p = a.Positionen.FirstOrDefault(x => x.Id == positionId);
        if (p is null) return new(false, "Position nicht gefunden.", a.Id);
        _repo.Remove(p);
        a.Positionen.Remove(p);
        Geaendert(a, Jetzt);
        return await MitRowVersionAsync(a, rowVersion, $"{p.ArticleNumber} entfernt.");
    }

    /// <summary>F2: Entwurf → Bestellt mit Nummer <c>BF</c> + 8 Ziffern und Empfaengergruppe (leer = gesperrt). Mail schickt der Dienst.</summary>
    public async Task<BfErgebnis> AbschickenAsync(int auftragId, byte[]? rowVersion)
    {
        var a0 = await _repo.GetMitPositionenAsync(auftragId);
        if (a0 is null) return new(false, "BF-Auftrag nicht gefunden.");
        if (PruefeBearbeitung(a0, alsSchachtler: false) is { } f) return new(false, f, a0.Id);
        if (a0.Positionen.Count == 0) return new(false, "Bitte mindestens eine Position hinzufügen.", a0.Id);
        var groupId = await _settings.GetIntValueAsync(AppSettingKeys.BfEmpfaengergruppeId, 0);
        if (groupId <= 0 || await _groups.GetGroupByIdAsync(groupId) is null)
            return new(false, "Abschicken gesperrt: keine gültige Empfängergruppe für BF-Aufträge eingestellt (Einstellungen > Blechfertigung).", a0.Id);

        var (ok, ergebnis) = await Nummernkreis.MitVersuchenAsync(MaxNummerVersuche, () => _repo.InSerializableTransactionAsync(async () =>
        {
            // Jeder Versuch liest neu (der vorige hat den Change-Tracker verworfen).
            var a = await _repo.GetMitPositionenAsync(auftragId)
                    ?? throw new InvalidOperationException($"BF-Auftrag {auftragId} verschwunden.");
            if (rowVersion is { Length: > 0 }) _repo.SetOriginalRowVersion(a, rowVersion);
            if (a.Status != BfAuftragStatus.Entwurf) return new BfErgebnis(false, "Der Auftrag ist schon abgeschickt.", a.Id);
            var jetzt = Jetzt;
            a.Nummer = Nummer(await _repo.GetMaxLaufendeNummerAsync() + 1);
            a.Status = BfAuftragStatus.Bestellt;
            a.BestelltAm = jetzt;
            a.BestellerName = _user.GetDisplayName();
            a.OrderRecipientGroupId = groupId;
            Geaendert(a, jetzt);
            await _repo.SaveChangesAsync();
            return new BfErgebnis(true, $"BF-Auftrag {a.Nummer} abgeschickt ({a.Positionen.Count} Position(en)). Die Mail an die Empfängergruppe sendet der Dienst.", a.Id);
        }), _repo.VerwerfeAenderungen, _logger, "BF-Auftrag abschicken");
        return ok ? ergebnis! : new(false, $"Konnte nach {MaxNummerVersuche} Versuchen keine freie BF-Nummer vergeben (gleichzeitiges Abschicken oder Datenbankfehler, Details im Log) – bitte erneut versuchen.", auftragId);
    }

    // ===================== Schachtler (Status Bestellt) =====================

    /// <summary>Position streichen bzw. wieder aufnehmen (bleibt sichtbar, geht nicht in die CSV).</summary>
    public async Task<BfErgebnis> StreichenAsync(int auftragId, int positionId, bool gestrichen, byte[]? rowVersion)
    {
        var a = await _repo.GetMitPositionenAsync(auftragId);
        if (a is null) return new(false, "BF-Auftrag nicht gefunden.");
        if (PruefeBearbeitung(a, alsSchachtler: true) is { } f) return new(false, f, a.Id);
        var p = a.Positionen.FirstOrDefault(x => x.Id == positionId);
        if (p is null) return new(false, "Position nicht gefunden.", a.Id);
        p.Gestrichen = gestrichen;
        var jetzt = Jetzt;
        Geaendert(p, jetzt);
        Geaendert(a, jetzt);
        return await MitRowVersionAsync(a, rowVersion, $"Pos {p.Position} {(gestrichen ? "gestrichen" : "wieder aufgenommen")}.");
    }

    /// <summary>F3 Freigabe (je Auftrag, Freigabe-Antwort 5): prueft jede nicht gestrichene Position (Mengenregel; bei Uebergabe Matchcode,
    /// Hauptlagerplatz, verbotene Zeichen), berechnet die Termine ab dem Freigabetag (Antwort 3, Arbeitstage ohne Feiertage), schreibt die
    /// Bemerkung fest und setzt CSV „offen“. Positionen ohne Materialnummer werden nicht uebergeben – nur Hinweis (Antwort 2); gesperrt nur,
    /// wenn keine Position uebergeben wuerde.</summary>
    public async Task<BfErgebnis> FreigebenAsync(int auftragId, byte[]? rowVersion)
    {
        var a = await _repo.GetMitPositionenAsync(auftragId);
        if (a is null) return new(false, "BF-Auftrag nicht gefunden.");
        if (PruefeBearbeitung(a, alsSchachtler: true) is { } f) return new(false, f, a.Id);

        var fehler = FreigabeFehler(a);
        if (fehler.Count > 0) return new(false, $"Freigabe von {Name(a)} abgelehnt: {string.Join("; ", fehler)}", a.Id);

        var jetzt = Jetzt;
        var bezug = jetzt.Date;
        var feiertage = await _holidays.GetHolidayDatesAsync();
        var schachtelTage = await _settings.GetIntValueAsync(AppSettingKeys.BfSchachtelterminArbeitstage, 1);
        var feTage = Math.Max(schachtelTage, await _settings.GetIntValueAsync(AppSettingKeys.BfFeTerminArbeitstage, 10));
        a.Schachteltermin = _arbeitstage.AddBusinessDays(bezug, schachtelTage, feiertage);
        a.FeTermin = _arbeitstage.AddBusinessDays(bezug, feTage, feiertage);
        foreach (var p in a.Positionen.Where(p => !p.Gestrichen))
        {
            p.Bemerkung = BfCsv.Bemerkung(p.Status, p.FertigungsInfoPN, bezug, p.Teileart, p.Position);
            Geaendert(p, jetzt);
        }
        a.Status = BfAuftragStatus.Freigegeben;
        a.FreigegebenAm = jetzt;
        a.FreigegebenVon = _user.GetDisplayName();
        a.CsvStatus = BfCsvStatus.Offen;
        a.CsvVersuche = 0;
        a.CsvFehler = null;
        Geaendert(a, jetzt);

        var ohne = a.Positionen.Where(p => !p.Gestrichen && !p.Uebergeben).OrderBy(p => p.Position).Select(p => p.Position).ToList();
        var hinweis = ohne.Count == 0 ? "" : $" Nicht an WiCAM übergeben (keine Materialnummer): Pos {string.Join(", ", ohne)}.";
        return await MitRowVersionAsync(a, rowVersion,
            $"{Name(a)} freigegeben: Schachteltermin {a.Schachteltermin:dd.MM.yyyy}, FE-Termin {a.FeTermin:dd.MM.yyyy}. Die CSV schreibt der Dienst.{hinweis}");
    }

    /// <summary>Sperren der Freigabe je Position und Feld (Melden statt still ersetzen). Leer = Freigabe moeglich.</summary>
    public static List<string> FreigabeFehler(BfAuftrag a)
    {
        var fehler = new List<string>();
        foreach (var p in a.Positionen.Where(p => !p.Gestrichen).OrderBy(p => p.Position))
        {
            if (MengenFehler(p.Menge, p.SageBestellmenge) is { } mf) fehler.Add($"Pos {p.Position}: {mf}");
            if (!p.Uebergeben) continue;
            if (BfCsv.MatchcodeBereinigen(p.Matchcode).Length == 0) fehler.Add($"Pos {p.Position}: Matchcode fehlt");
            if (string.IsNullOrWhiteSpace(p.Hauptlagerplatz)) fehler.Add($"Pos {p.Position}: Hauptlagerplatz fehlt");
            foreach (var (feld, wert) in new[] { ("Matchcode", p.Matchcode), ("Materialnummer", p.Materialnummer), ("Status", p.Status),
                         ("FertigungsInfoPN", p.FertigungsInfoPN), ("Hauptlagerplatz", p.Hauptlagerplatz) })
                if (BfCsv.UnzulaessigesZeichen(wert) is { } z) fehler.Add($"Pos {p.Position} {feld}: {z}");
        }
        if (BfCsv.UnzulaessigesZeichen(a.BestellerName) is { } bz) fehler.Add($"Besteller: {bz}");
        if (!a.Positionen.Any(p => p.Uebergeben))
            fehler.Add("keine Position wird an WiCAM übergeben (alle gestrichen oder ohne Materialnummer)");
        return fehler;
    }

    /// <summary>F4: CSV erneut schreiben nach einem Fehler (z. B. Namenskollision, nachdem die Datei in WiCAM geloescht wurde) – nur Status Freigegeben.</summary>
    public async Task<BfErgebnis> CsvErneutSchreibenAsync(int auftragId, byte[]? rowVersion)
    {
        var a = await _repo.GetMitPositionenAsync(auftragId);
        if (a is null) return new(false, "BF-Auftrag nicht gefunden.");
        if (a.Status != BfAuftragStatus.Freigegeben) return new(false, $"{Name(a)} ist {StatusText(a.Status)} – CSV nur bei freigegebenen Aufträgen.", a.Id);
        // Nur nach Fehler: eine geschriebene Datei hat WiCAM ggf. schon als Auftrag angelegt – erneut schreiben gaebe einen doppelten Auftrag.
        if (a.CsvStatus != BfCsvStatus.Fehler) return new(false, $"Die CSV von {Name(a)} ist „{IdealAkeWms.Models.ViewModels.BfAuftragListRow.CsvText(a.CsvStatus)}“ – erneut schreiben nur nach einem Fehler.", a.Id);
        a.CsvStatus = BfCsvStatus.Offen;
        a.CsvVersuche = 0;
        a.CsvFehler = null;
        Geaendert(a, Jetzt);
        return await MitRowVersionAsync(a, rowVersion, $"CSV von {Name(a)} wird neu geschrieben (nächster Dienst-Takt).");
    }

    // ===================== Fertig / Storno (Schritt D) =====================

    /// <summary>F5 Fertig melden (je Auftrag, Mehrfachauswahl aus der Liste): nur Status <c>Freigegeben</c> mit geschriebener CSV wird
    /// <c>Fertig</c> – nur Status, keine Lagerbuchung (AK 14). Uebersprungene Auftraege werden einzeln gemeldet.</summary>
    public async Task<BfErgebnis> FertigMeldenAsync(IReadOnlyCollection<int> ids, byte[]? rowVersion = null)
    {
        ids = ids.Distinct().ToList();
        if (ids.Count == 0) return new(false, "Kein BF-Auftrag ausgewählt.");
        var auftraege = await _repo.GetMehrereAsync(ids);
        var jetzt = Jetzt;
        var fertig = new List<string>();
        var nicht = new List<string>();
        foreach (var a in auftraege.OrderBy(a => a.Nummer))
        {
            if (a.Status != BfAuftragStatus.Freigegeben) { nicht.Add($"{Name(a)} ist {StatusText(a.Status)}"); continue; }
            if (a.CsvStatus != BfCsvStatus.Geschrieben)
            {
                nicht.Add($"{Name(a)}: CSV ist „{IdealAkeWms.Models.ViewModels.BfAuftragListRow.CsvText(a.CsvStatus)}“, nicht geschrieben");
                continue;
            }
            a.Status = BfAuftragStatus.Fertig;
            a.FertigAm = jetzt;
            a.FertigVon = _user.GetDisplayName();
            Geaendert(a, jetzt);
            fertig.Add(Name(a));
        }
        var fehlend = ids.Count - auftraege.Count;
        if (fehlend > 0) nicht.Add($"{fehlend} Auftrag/Aufträge nicht gefunden");
        var rest = nicht.Count == 0 ? "" : $" Nicht fertig gemeldet: {string.Join("; ", nicht)}.";
        if (fertig.Count == 0) return new(false, $"Kein Auftrag fertig gemeldet.{rest}", ids.Count == 1 ? ids.First() : null);
        var text = $"Fertig gemeldet: {string.Join(", ", fertig)}.{rest}";
        if (ids.Count == 1) return await MitRowVersionAsync(auftraege[0], rowVersion, text);
        return await SpeichernAsync("BF-Aufträge", async () => { await _repo.SaveChangesAsync(); return new BfErgebnis(nicht.Count == 0, text); });
    }

    /// <summary>F6 Storno (Freigabe-Antwort 11): Besteller den eigenen abgeschickten Auftrag bis zur Freigabe (der Warenkorb wird ueber „Entfernen“ geleert), Schachtler bis zur
    /// Fertigmeldung (bestellt, freigegeben). Nach der Freigabe nur WMS-Status – die CSV liegt ggf. schon im Pool (Hinweis, kein Loeschen).
    /// Ein stornierter Auftrag wird nie als CSV geschrieben (der Dienst nimmt nur <c>Freigegeben</c>).</summary>
    public async Task<BfErgebnis> StornierenAsync(int auftragId, string? grund, bool alsSchachtler, byte[]? rowVersion)
    {
        var a = await _repo.GetMitPositionenAsync(auftragId);
        if (a is null) return new(false, "BF-Auftrag nicht gefunden.");
        var alsBesteller = IstEigentuemer(a, _user.GetCurrentAppUserId()) && a.Status == BfAuftragStatus.Bestellt;
        var alsS = alsSchachtler && a.Status is BfAuftragStatus.Bestellt or BfAuftragStatus.Freigegeben;
        if (!alsBesteller && !alsS)
            return new(false, a.Status == BfAuftragStatus.Entwurf ? "Der Warenkorb wird nicht storniert – Positionen entfernen."
                : a.Status is BfAuftragStatus.Fertig or BfAuftragStatus.Storniert
                ? $"{Name(a)} ist {StatusText(a.Status)} – Storno nicht mehr möglich."
                : $"{Name(a)} ist {StatusText(a.Status)} – stornieren darf der Besteller bis zur Freigabe, danach nur der Schachtler.", a.Id);
        grund = string.IsNullOrWhiteSpace(grund) ? null : grund.Trim();
        if (grund?.Length > 500) return new(false, "Storno-Grund: höchstens 500 Zeichen.", a.Id);

        var warFreigegeben = a.Status == BfAuftragStatus.Freigegeben;
        var csv = a.CsvStatus;
        var jetzt = Jetzt;
        a.Status = BfAuftragStatus.Storniert;
        a.StorniertAm = jetzt;
        a.StorniertVon = _user.GetDisplayName();
        a.StornoGrund = grund;
        Geaendert(a, jetzt);
        var hinweis = !warFreigegeben ? ""
            : csv == BfCsvStatus.Geschrieben ? $" Die CSV liegt bereits im Pool ({Path.GetFileName(a.CsvDatei)}) – den Auftrag in WiCAM löschen."
            : " Die CSV wird nicht mehr geschrieben; falls sie gerade im Pool angekommen ist, den Auftrag in WiCAM löschen.";
        return await MitRowVersionAsync(a, rowVersion, $"{Name(a)} storniert.{hinweis}");
    }

    // ===================== Hilfen =====================

    /// <summary>Besteller: nur eigener Entwurf. Schachtler: nur Status Bestellt (Pruefphase).</summary>
    private string? PruefeBearbeitung(BfAuftrag a, bool alsSchachtler)
    {
        if (alsSchachtler)
            return a.Status == BfAuftragStatus.Bestellt ? null : $"{Name(a)} ist {StatusText(a.Status)} – nur bestellte Aufträge sind änderbar.";
        if (!IstEigentuemer(a, _user.GetCurrentAppUserId())) return "Nur der Besteller kann seinen Warenkorb ändern.";
        return a.Status == BfAuftragStatus.Entwurf ? null : $"{Name(a)} ist schon abgeschickt.";
    }

    private async Task<(bool Ok, string? WertOderFehler)> StatusPruefenAsync(string? status, string? bisher = null)
    {
        if (string.IsNullOrWhiteSpace(status)) return (true, null);
        var s = status.Trim();
        if ((await GetStatuswerteAsync()).Contains(s) || s == bisher) return (true, s);
        return (false, $"Status „{s}“ ist nicht in den Einstellungen (BF-Aufträge: Status-Werte).");
    }

    /// <summary>Leer = Vorbelegung behalten; sonst ein aktiver Lagerplatz oder der bisherige/Sage-Wert.</summary>
    private async Task<(bool Ok, string? WertOderFehler)> LagerplatzPruefenAsync(string? eingabe, string? bisher)
    {
        if (string.IsNullOrWhiteSpace(eingabe)) return (true, bisher);
        var lp = eingabe.Trim();
        if (string.Equals(lp, bisher, StringComparison.OrdinalIgnoreCase)) return (true, bisher);
        var platz = await _lagerplaetze.GetByCodeAsync(lp);
        return platz is { IsActive: true } ? (true, platz.Code) : (false, $"Lagerplatz „{lp}“ ist kein aktiver Lagerplatz.");
    }

    private async Task<BfErgebnis> MitRowVersionAsync(BfAuftrag a, byte[]? rowVersion, string text) =>
        await SpeichernAsync(Name(a), async () =>
        {
            if (rowVersion is { Length: > 0 }) _repo.SetOriginalRowVersion(a, rowVersion);
            await _repo.SaveChangesAsync();
            return new BfErgebnis(true, text, a.Id);
        });

    private async Task<BfErgebnis> SpeichernAsync(string was, Func<Task<BfErgebnis>> op)
    {
        try
        {
            return await op();
        }
        catch (DbUpdateConcurrencyException)
        {
            _repo.VerwerfeAenderungen();
            return new(false, $"{was} wurde inzwischen von jemand anderem geändert – bitte neu laden und erneut versuchen.");
        }
        catch (DbUpdateException ex)
        {
            _repo.VerwerfeAenderungen();
            _logger.LogError(ex, "BF-Auftrag {Was}: Speichern fehlgeschlagen.", was);
            return new(false, $"{was} konnte nicht gespeichert werden (Datenbankfehler, Details im Log) – bitte erneut versuchen.");
        }
    }

    public static string Name(BfAuftrag a) => a.Nummer ?? "Warenkorb";

    public static string StatusText(BfAuftragStatus s) => s switch
    {
        BfAuftragStatus.Entwurf => "Warenkorb",
        BfAuftragStatus.Bestellt => "bestellt",
        BfAuftragStatus.Freigegeben => "freigegeben",
        BfAuftragStatus.Fertig => "fertig",
        BfAuftragStatus.Storniert => "storniert",
        _ => s.ToString(),
    };

    private void Angelegt(AuditableEntity e, DateTime jetzt)
    {
        e.CreatedAt = jetzt;
        e.CreatedBy = _user.GetDisplayName();
        e.CreatedByWindows = _user.GetWindowsUserName();
    }

    private void Geaendert(AuditableEntity e, DateTime jetzt)
    {
        e.ModifiedAt = jetzt;
        e.ModifiedBy = _user.GetDisplayName();
        e.ModifiedByWindows = _user.GetWindowsUserName();
    }
}
