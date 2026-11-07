using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>Zeile aus <c>Kontokorrent</c> (Namen exakt wie in der SData-Antwort, Typen P-S1). Je Lauf werden nur die Felder der
/// jeweiligen Tabelle abgefragt (<see cref="KontokorrentSyncService.SelectKreditoren"/> / <see cref="KontokorrentSyncService.SelectDebitoren"/>).</summary>
public sealed record SdataKontoZeile(string? Kto, string? Matchcode, string? EUUStID, string? Zahlungskond, string? Gruppe, bool? Aktiv,
    string? Vertreter = null, decimal? USER_EKMindestbestellwert = null, int? Adresse = null);

/// <summary>Zeile aus <c>Adressen</c> (= Sage-Tabelle KHKAdressen; Spaltennamen und Laengen am AKE-Test belegt, Spec P-K1-Nachtrag).
/// Liefer* ist die Hausadresse (2.204 von 2.287 Kreditoren gefuellt), Post* ist das Postfach (6) und wird nicht gelesen.</summary>
public sealed record SdataAdresseZeile(int? Adresse, string? Name1, string? Name2, string? LieferStrasse, string? LieferPLZ, string? LieferOrt,
    string? LieferLand, string? Telefon, string? EMail, string? USER_KundenNameFA = null);

/// <summary>Zeile aus <c>Ansprechpartner</c> (= KHKAnsprechpartner, n je Adresse, am AKE-Test bis 99).</summary>
public sealed record SdataAnsprechpartnerZeile(int? Nummer, int? Adresse, string? Vorname, string? Nachname, string? Ansprechpartner);

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E8c/F10, P-K1): liest Sage-Konten aus SData (<c>Kontokorrent</c>, Vertrag
/// CommonWawiServicesResources) — <c>KtoArt eq 'K'</c> in die <see cref="Creditor"/>-Tabelle, <c>KtoArt eq 'D'</c> in die
/// <see cref="Debitor"/>-Tabelle. Neuer Lese-Abgleich ohne View-Weg; Gates <c>Sync:KreditorenEnabled</c> / <c>Sync:DebitorenEnabled</c>
/// im SyncWorker (Default false).
/// <list type="bullet">
/// <item>Schluessel <c>SageKto</c> (Zeichenkette, nie numerisch). Neues Konto → <c>Source = Sage</c>, <c>ShortName</c> leer.</item>
/// <item>Treffer auf einen WMS-Datensatz (<c>Source = Manual</c>) mit gleichem <c>SageKto</c> → verknuepft: <c>Source</c> wechselt auf
///   <c>Sage</c> (die Sage-Felder werden ab jetzt ohnehin ueberschrieben, die Pflege sperrt sie), Uebergabe-Status Offen/Fehler →
///   Uebergeben (F11). Gleichnamige WMS-Datensaetze OHNE <c>SageKto</c> werden nie verknuepft, nur als „moegliche Dublette“ gemeldet.</item>
/// <item>G6 (Gerald 2026-11-07): Sage ist fuehrend und gewinnt bei jedem Feld, das Sage liefert (null = nicht schreiben, F1) —
///   Name, Adresse, Kontakt, <c>Matchcode</c> (= Sage-Matchcode), <c>IsActive</c> (= Sage <c>Aktiv</c>) und alle <c>Sage*</c>-Felder.
///   WMS-Felder nur <c>ShortName</c> (Zuordnung der Dienstleister-Texte; Sage-Matchcodes sind nicht eindeutig), <c>Remark</c>,
///   <c>IsCoater</c>. Datensaetze mit Herkunft WMS ohne Sage-Konto werden je Lauf gemeldet (Count <c>wms_eigen</c>), nie geloescht.</item>
/// <item>Fehlt ein Konto in Sage → <c>SageMissingSince</c>, nie loeschen; wieder da → zurueckgesetzt. Empty-Guard: leere Antwort
///   kennzeichnet nichts.</item>
/// </list>
/// Lesefehler: Lauf Fehler + throw (Mail ueber RunResilientAsync).
/// </summary>
public class KontokorrentSyncService
{
    public const string ServiceUser = "KontokorrentSync";
    public const string Ressource = "Kontokorrent";
    public const string WhereKreditoren = "KtoArt eq 'K'"; // P-K1 (AKE-Test 2026-11-06): K = 2.287 Lieferanten
    public const string WhereDebitoren = "KtoArt eq 'D'";  // P-K1: D = 534 Kunden
    // Felder aus dem P-S1-Schema von Kontokorrent; Adresse = Verweis auf Adressen.Adresse (KHKKontokorrent.Adresse, P-K1-Nachtrag).
    private const string SelectBasis = "Kto,Matchcode,EUUStID,Zahlungskond,Gruppe,Aktiv,Adresse";
    public const string SelectKreditoren = SelectBasis + ",USER_EKMindestbestellwert";
    public const string SelectDebitoren = SelectBasis + ",Vertreter";
    // Name/Adresse/Kontakt: Feldnamen = Spalten von KHKAdressen/KHKAnsprechpartner (am AKE-Test per SQL belegt; die SData-Namen von
    // Kontokorrent sind 1:1 die Spaltennamen). TODO(P-K1-Nachtrag): $schema beider Ressourcen bestaetigen. Nicht lesbar →
    // Warnung im Lauf, Name/Adresse/Kontakt bleiben unveraendert, der Konto-Abgleich laeuft weiter.
    public const string RessourceAdressen = "Adressen";
    public const string RessourceAnsprechpartner = "Ansprechpartner";
    public const string SelectAdressen = "Adresse,Name1,Name2,LieferStrasse,LieferPLZ,LieferOrt,LieferLand,Telefon,EMail";
    public const string SelectAdressenDebitoren = SelectAdressen + ",USER_KundenNameFA";
    public const string SelectAnsprechpartner = "Nummer,Adresse,Vorname,Nachname,Ansprechpartner";

    private readonly ApplicationDbContext _ctx;
    private readonly ISageSdataReadClient _client;
    private readonly IConfiguration _config;
    private readonly ILogger<KontokorrentSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public KontokorrentSyncService(ApplicationDbContext ctx, ISageSdataReadClient client, IConfiguration config,
        ILogger<KontokorrentSyncService> logger, ISyncLogger syncLogger) // ISyncLogger als LETZTER Ctor-Parameter (ADR 0010)
    {
        _ctx = ctx;
        _client = client;
        _config = config;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public Task<SyncResult> RunKreditorenAsync(bool dryRun, CancellationToken ct = default, SdataLeseZiel? ziel = null) =>
        AbgleichAsync(_ctx.Creditors, SyncLogServices.Kreditoren, "Kreditoren", WhereKreditoren, SelectKreditoren, SelectAdressen, (c, z, _) =>
        {
            // TODO(P-S1): Nachkommastellen unbelegt, Spalte decimal(18,2) laut Spec F10.
            var wert = z.USER_EKMindestbestellwert is { } w ? Math.Round(w, 2) : (decimal?)null;
            if (wert == null || wert == c.SageEKMindestbestellwert) return false;
            c.SageEKMindestbestellwert = wert;
            return true;
        }, dryRun, ziel, ct);

    public Task<SyncResult> RunDebitorenAsync(bool dryRun, CancellationToken ct = default, SdataLeseZiel? ziel = null) =>
        AbgleichAsync(_ctx.Debitors, SyncLogServices.Debitoren, "Debitoren", WhereDebitoren, SelectDebitoren, SelectAdressenDebitoren, (d, z, a) =>
            Setzen(Text(z.Vertreter), d.SageVertreter, v => d.SageVertreter = v)
            | Setzen(Text(a?.USER_KundenNameFA), d.SageKundenNameFA, v => d.SageKundenNameFA = v), dryRun, ziel, ct);

    private async Task<SyncResult> AbgleichAsync<T>(DbSet<T> set, string lauf, string bezeichnung, string where, string select,
        string selectAdressen, Func<T, SdataKontoZeile, SdataAdresseZeile?, bool> zusatz, bool dryRun, SdataLeseZiel? ziel, CancellationToken ct)
        where T : AuditableEntity, ISageKonto, new()
    {
        await using var run = await _syncLogger.BeginRunAsync(lauf, ct);

        IReadOnlyList<SdataKontoZeile> zeilen;
        try
        {
            ziel ??= await SdataLeseZielLeser.LesenAsync(_config, ct);
            var seiten = await _client.QueryAllAsync<SdataKontoZeile>(ziel, SageSdataReadClient.VertragResources, Ressource, where, select, "Kto", ct);
            zeilen = seiten.Zeilen;
            await run.LogInfoAsync(seiten.Zusammenfassung + $" ({where})", ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "{Bereich} aus Sage (SData) nicht lesbar.", bezeichnung);
            await run.LogErrorAsync($"Sage (SData) nicht lesbar: {ex.Message}", ct: ct);
            await run.FinishFailedAsync($"Sage (SData) nicht lesbar: {ex.Message}", ct: ct);
            throw;
        }

        // Adressen + Ansprechpartner (P-K1-Nachtrag): je eigener Lesevorgang; ein Fehler hier stoppt den Konto-Abgleich nicht.
        var adressen = await NachlesenAsync<SdataAdresseZeile>(run, ziel, RessourceAdressen, selectAdressen, "Adresse", ct);
        var adresseById = adressen?.Where(a => a.Adresse != null).GroupBy(a => a.Adresse!.Value).ToDictionary(g => g.Key, g => g.First());
        var partner = await NachlesenAsync<SdataAnsprechpartnerZeile>(run, ziel, RessourceAnsprechpartner, SelectAnsprechpartner, "Nummer", ct);
        var partnerById = partner?.Where(p => p.Adresse != null).GroupBy(p => p.Adresse!.Value)
            .ToDictionary(g => g.Key, g => KontaktText(g.OrderBy(p => p.Nummer).ToList()));

        try
        {
            var (eindeutig, uebersprungen) = await SdataSchluessel.EindeutigAsync(zeilen, z => z.Kto, run, "Kto", ct);
            int neu = 0, aktualisiert = 0, unveraendert = 0, nichtInSage = 0, wiederDa = 0;
            var verknuepft = new List<string>();
            var zuLang = new List<string>(); // QA Q1: Sage-Wert laenger als die WMS-Spalte
            var neuMarkiert = new List<T>();
            var now = DateTime.Now;
            var bestand = await set.ToListAsync(ct);
            // App-Guard zum gefilterten UNIQUE-Index IX_*_SageKto (InMemory erzwingt ihn nicht): bei Dubletten gewinnt der erste.
            var byKto = bestand.Where(k => !string.IsNullOrWhiteSpace(k.SageKto))
                .GroupBy(k => k.SageKto!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            void Geaendert(T k) { k.ModifiedAt = now; k.ModifiedBy = ServiceUser; k.ModifiedByWindows = ServiceUser; }

            foreach (var z in eindeutig)
            {
                var kto = z.Kto!.Trim();
                var matchcode = Text(z.Matchcode);
                SdataAdresseZeile? adr = null;
                string? kontakt = null;
                if (z.Adresse is { } adrId)
                {
                    adresseById?.TryGetValue(adrId, out adr);
                    partnerById?.TryGetValue(adrId, out kontakt);
                }
                if (!byKto.TryGetValue(kto, out var k))
                {
                    neu++;
                    if (dryRun) continue;
                    var anlage = new T
                    {
                        Name = NameText(adr) ?? matchcode ?? kto, Source = StorageLocationSource.Sage, IsActive = z.Aktiv ?? true, SageKto = kto,
                        CreatedAt = now, CreatedBy = ServiceUser, CreatedByWindows = ServiceUser,
                    };
                    SageFelder(anlage, z);
                    AdressFelder(anlage, adr, kontakt, zuLang);
                    zusatz(anlage, z, adr);
                    set.Add(anlage);
                    continue;
                }

                var warSage = k.Source == StorageLocationSource.Sage;
                var anders = SageFelder(k, z) | zusatz(k, z, adr) | AdressFelder(k, adr, kontakt, zuLang);
                if (z.Aktiv is bool aktiv && k.IsActive != aktiv) { k.IsActive = aktiv; anders = true; } // G6: Sage gewinnt
                // Name = Name1 (+Name2) aus Adressen (auch fuer verknuepfte WMS-Datensaetze, F10: Name ist Sage-Feld). Ohne Adresszeile nur
                // bei schon Sage-gefuehrten Konten der Matchcode (wie bisher); Adressen nicht lesbar → Name unveraendert (kein Hin und Her).
                if (NameText(adr) is { } name) anders |= Setzen(name, k.Name, v => k.Name = v!);
                else if (warSage && adresseById != null) anders |= Setzen(matchcode, k.Name, v => k.Name = v!);
                var warWeg = k.SageMissingSince != null;
                if (warWeg) { wiederDa++; k.SageMissingSince = null; }
                if (!warSage)
                {
                    verknuepft.Add($"{kto} → {k.Name}");
                    k.Source = StorageLocationSource.Sage;
                    if (k.SageUebergabeStatus is SageUebergabeStatus.Offen or SageUebergabeStatus.Fehler)
                    {
                        k.SageUebergabeStatus = SageUebergabeStatus.Uebergeben;
                        k.SageUebergabeAm = now;
                    }
                }
                if (anders) aktualisiert++;
                if (!anders && !warWeg && warSage) { unveraendert++; continue; }
                Geaendert(k);
            }

            if (zuLang.Count > 0)
                await run.LogWarningAsync(SdataSchluessel.Kappen($"{zuLang.Count} Sage-Wert(e) laenger als die WMS-Spalte, nicht uebernommen " +
                    $"(alter Wert bleibt): {string.Join("; ", zuLang.Take(20))}"), ct: ct);

            if (verknuepft.Count > 0)
                await run.LogInfoAsync(SdataSchluessel.Kappen($"Mit Sage verknuepft (WMS-Datensatz mit gleicher Kontonummer, Herkunft jetzt Sage): " +
                    string.Join(", ", verknuepft.Take(20))), ct: ct);

            // „Moegliche Dublette“: aktiver WMS-Datensatz ohne Kontonummer, dessen Name dem Sage-Namen entspricht — nie automatisch verknuepfen.
            // Sage-Name = Name1 (+Name2) aus Adressen, wie ihn der Abgleich schreibt (QA Q5); Rueckfall Matchcode, wenn die Adresse fehlt
            // oder Adressen nicht lesbar war.
            string? SageName(SdataKontoZeile z)
            {
                SdataAdresseZeile? a = null;
                if (z.Adresse is { } id) adresseById?.TryGetValue(id, out a);
                return NameText(a) ?? Text(z.Matchcode);
            }
            var sageNamen = eindeutig.Where(z => SageName(z) != null)
                .GroupBy(z => SageName(z)!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Kto!.Trim(), StringComparer.OrdinalIgnoreCase);
            var dubletten = bestand.Where(k => k.IsActive && string.IsNullOrWhiteSpace(k.SageKto) && sageNamen.ContainsKey(k.Name.Trim()))
                .Select(k => $"„{k.Name.Trim()}“ (WMS-Id {k.Id}) ↔ Sage {sageNamen[k.Name.Trim()]}").ToList();
            if (dubletten.Count > 0)
                await run.LogWarningAsync(SdataSchluessel.Kappen($"{dubletten.Count} moegliche Dublette(n), nicht verknuepft (das Sage-Konto " +
                    $"steht als eigener Datensatz daneben; WMS-Datensatz pruefen und ggf. deaktivieren): {string.Join("; ", dubletten.Take(20))}"), ct: ct);

            // G6: WMS-eigene Datensaetze (ohne Sage-Konto) melden, nicht loeschen.
            var wmsEigen = bestand.Where(k => k.Source != StorageLocationSource.Sage && string.IsNullOrWhiteSpace(k.SageKto)).ToList();
            if (wmsEigen.Count > 0)
                await run.LogWarningAsync(SdataSchluessel.Kappen($"{wmsEigen.Count} {bezeichnung} nur im WMS (ohne Sage-Konto; Sage ist fuehrend, " +
                    $"bitte in Sage anlegen und das Sage-Konto eintragen): {string.Join(", ", wmsEigen.Take(20).Select(k => $"{k.Name} (WMS-Id {k.Id})"))}"), ct: ct);

            if (eindeutig.Count == 0)
                await run.LogWarningAsync($"Sage liefert keine {bezeichnung} — keine Kennzeichnung \"nicht in Sage\" (Empty-Guard).", ct: ct);
            else
            {
                var present = eindeutig.Select(z => z.Kto!.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var k in byKto.Values.Where(k => !present.Contains(k.SageKto!.Trim())))
                {
                    nichtInSage++;
                    if (k.SageMissingSince != null) continue;
                    neuMarkiert.Add(k);
                    k.SageMissingSince = now;
                    Geaendert(k);
                }
                if (neuMarkiert.Count > 0)
                    await run.LogWarningAsync(SdataSchluessel.Kappen($"Nicht mehr in Sage ({neuMarkiert.Count} neu gekennzeichnet): " +
                        string.Join(", ", neuMarkiert.Take(20).Select(k => $"{k.SageKto} {k.Name}"))), ct: ct);
            }

            if (!dryRun) await _ctx.SaveChangesAsync(ct);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["gelesen"] = eindeutig.Count,
                ["neu"] = neu,
                ["aktualisiert"] = aktualisiert,
                ["verknuepft"] = verknuepft.Count,
                ["unveraendert"] = unveraendert,
                ["uebersprungen"] = uebersprungen,
                ["nicht_in_sage"] = nichtInSage,
                ["nicht_in_sage_neu"] = neuMarkiert.Count,
                ["wieder_in_sage"] = wiederDa,
                ["moegliche_dubletten"] = dubletten.Count,
                ["wms_eigen"] = wmsEigen.Count,
            }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
            return new SyncResult(neu, aktualisiert + verknuepft.Count, uebersprungen);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "{Bereich}-Abgleich fehlgeschlagen.", bezeichnung);
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }

    /// <summary>Gemeinsame Sage-Felder; true = mindestens eins geaendert.</summary>
    private static bool SageFelder(ISageKonto k, SdataKontoZeile z) =>
        Setzen(Text(z.Matchcode), k.SageMatchcode, v => k.SageMatchcode = v)
        | Setzen(Text(z.Gruppe), k.SageGruppe, v => k.SageGruppe = v)
        | Setzen(Text(z.EUUStID), k.SageEUUStID, v => k.SageEUUStID = v)
        | Setzen(Text(z.Zahlungskond), k.SageZahlungskondition, v => k.SageZahlungskondition = v);

    /// <summary>Adresse/Kontakt aus Sage (F1: null = nicht schreiben); <c>Matchcode</c> = Sage-Matchcode (G6, Sage gewinnt).
    /// QA Q1: Sage-Spalten sind teils laenger als die WMS-Spalten (LieferPLZ 24 → PostalCode 20, EMail 255 → Email 200, SQL/104/105);
    /// ein zu langer Wert wuerde das Speichern des ganzen Laufs kippen → nicht schreiben, in <paramref name="zuLang"/> melden. Nicht
    /// kuerzen: eine gekuerzte PLZ/E-Mail waere falsch. AKE-Test 2026-11-07: max. PLZ 20, E-Mail 60 → heute kein Fall.</summary>
    private static bool AdressFelder(ISageKonto k, SdataAdresseZeile? a, string? kontakt, List<string> zuLang)
    {
        string? Passt(string? wert, int max, string feld)
        {
            var t = Text(wert);
            if (t == null || t.Length <= max) return t;
            zuLang.Add($"{k.SageKto} {feld} ({t.Length} > {max})");
            return null;
        }
        return Setzen(Passt(a?.LieferStrasse, 200, "Strasse"), k.Street, v => k.Street = v)
            | Setzen(Passt(a?.LieferPLZ, 20, "PLZ"), k.PostalCode, v => k.PostalCode = v)
            | Setzen(Passt(a?.LieferOrt, 100, "Ort"), k.City, v => k.City = v)
            | Setzen(Passt(a?.LieferLand, 100, "Land"), k.Country, v => k.Country = v)
            | Setzen(Passt(a?.Telefon, 50, "Telefon"), k.Phone, v => k.Phone = v)
            | Setzen(Passt(a?.EMail, 200, "E-Mail"), k.Email, v => k.Email = v)
            | Setzen(kontakt, k.ContactPerson, v => k.ContactPerson = v)
            | Setzen(Passt(k.SageMatchcode, 50, "Matchcode"), k.Matchcode, v => k.Matchcode = v);
    }

    private static string? NameText(SdataAdresseZeile? a) => Text(string.Join(" ", new[] { Text(a?.Name1), Text(a?.Name2) }.Where(t => t != null)));

    /// <summary>Erster Ansprechpartner (kleinste Nummer) als „Vorname Nachname“, sonst sein Feld <c>Ansprechpartner</c>; weitere als
    /// „(+n weitere)“ — Sage kennt keinen Hauptansprechpartner (KHKAnsprechpartner ohne Kennzeichen, am AKE-Test bis 99 je Adresse).</summary>
    internal static string? KontaktText(IReadOnlyList<SdataAnsprechpartnerZeile> liste)
    {
        var namen = liste
            .Select(p => Text(string.Join(" ", new[] { Text(p.Vorname), Text(p.Nachname) }.Where(t => t != null))) ?? Text(p.Ansprechpartner))
            .Where(n => n != null).ToList();
        if (namen.Count == 0) return null;
        var text = namen.Count == 1 ? namen[0]! : $"{namen[0]} (+{namen.Count - 1} weitere)";
        return text.Length <= 200 ? text : text[..200];
    }

    private async Task<IReadOnlyList<TZeile>?> NachlesenAsync<TZeile>(ISyncRun run, SdataLeseZiel ziel, string ressource, string select,
        string orderBy, CancellationToken ct)
    {
        try
        {
            var seiten = await _client.QueryAllAsync<TZeile>(ziel, SageSdataReadClient.VertragResources, ressource, null, select, orderBy, ct);
            await run.LogInfoAsync(seiten.Zusammenfassung, ct: ct);
            return seiten.Zeilen;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "{Ressource} aus Sage (SData) nicht lesbar.", ressource);
            await run.LogWarningAsync($"{ressource} aus Sage (SData) nicht lesbar — Name, Adresse und Kontakt bleiben unveraendert: {ex.Message}", ct: ct);
            return null;
        }
    }

    /// <summary>F1: null = Sage liefert den Wert nicht = nicht schreiben.</summary>
    private static bool Setzen(string? neu, string? alt, Action<string?> schreiben)
    {
        if (neu == null || string.Equals(neu, alt, StringComparison.Ordinal)) return false;
        schreiben(neu);
        return true;
    }

    private static string? Text(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
