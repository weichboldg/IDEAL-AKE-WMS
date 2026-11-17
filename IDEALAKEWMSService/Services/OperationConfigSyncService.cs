using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Common;
using IDEALAKEWMSService.Services.Sdata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

/// <summary>Einstellungen eines Laufs (eigener Typ, damit die Logik ohne Settings-DB testbar ist).</summary>
internal sealed record OperationConfigSyncSettings(string? ViewName, string? Ausschlussliste, bool FremdsystemRelevant, int Mandant);

/// <summary>Bestandszeile, wie <see cref="OperationConfigSyncService.Plan"/> sie sieht.</summary>
internal sealed record ExistingOperationConfig(int Id, string OperationName, string Source, DateTime? SageMissingSince,
    string? SageArbeitsgangnummer = null, string? SageArbeitsgangnummerManuell = null);

/// <summary>Reine Entscheidung eines Laufs (keine DB, keine Zeit).</summary>
internal sealed class OperationConfigSyncPlan
{
    public List<(string OperationName, string? DisplayName, string? Nummer)> New { get; } = new();
    public List<(int Id, string? Nummer)> NummerSetzen { get; } = new(); // Sage-Arbeitsgangnummer aendern (auch auf NULL spiegeln)
    public List<string> OhneKuerzel { get; } = new();  // View-Zeilen mit leerem AG: "Nummer Bezeichnung" (Pruefliste c)
    public int NummerMehrdeutig { get; set; }
    public int AbweichungHand { get; set; }          // nach dem Lauf: Handwert und Sage-Wert gesetzt und verschieden (FA-4)
    public List<int> Mark { get; } = new();          // neu nicht in Sage
    public List<int> Unmark { get; } = new();        // wieder in Sage (mit Meldung)
    public List<int> Clear { get; } = new();         // Fremdsystem/Ausschluss: Kennzeichnung still entfernen
    public List<int> SourceToSage { get; } = new();  // Quelle Manual -> Sage (Antwort 5)
    public List<string> Warnings { get; } = new();
    public int Vorhanden { get; set; }
    public int Uebersprungen { get; set; }
    public int NameMehrdeutig { get; set; }
    public int NichtInSage { get; set; }             // nach dem Lauf gekennzeichnet (neu + schon vorher)
}

/// <inheritdoc cref="IOperationConfigSyncService"/>
public class OperationConfigSyncService : IOperationConfigSyncService
{
    private const string ServiceUser = "OperationConfigSync";
    private const int MaxDetailLines = 100;
    private const int MaxMessageLength = 1000; // SyncLogs.Message NVARCHAR(1000), laengere Zeilen gingen still verloren

    /// <summary>Default/Fallback der Quelle = Katalog-Default von Sync:OperationConfigSyncViewName.</summary>
    public const string DefaultViewName = "[vw_IDEAL_AKE_WMS_Arbeitsgaenge]";

    /// <summary>Fallback der Ausschlussliste, wenn der Settings-Read null liefert (FA 3: dieser Sync loescht nie).</summary>
    public const string DefaultAusschlussliste = "STO,XXX,x01,BS,BS2,FRE,AKE";

    private readonly ApplicationDbContext _ctx;
    private readonly IConfiguration _config;
    private readonly ISageArbeitsplatzReader _reader;
    private readonly ILogger<OperationConfigSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public OperationConfigSyncService(
        ApplicationDbContext ctx,
        IConfiguration config,
        ISageArbeitsplatzReader reader,
        ILogger<OperationConfigSyncService> logger,
        ISyncLogger syncLogger) // ISyncLogger als LETZTER Ctor-Parameter (ADR 0010)
    {
        _ctx = ctx;
        _config = config;
        _reader = reader;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task<SyncResult> RunAsync(bool dryRun, CancellationToken ct = default)
    {
        // Safe-Varianten: ein transienter Settings-DB-Fehler faellt auf den Default zurueck.
        var settings = new OperationConfigSyncSettings(
            await ServiceSettings.GetValueSafeAsync(_config, "Sync:OperationConfigSyncViewName", ct),
            await ServiceSettings.GetValueSafeAsync(_config, "Sync:ProductionWorkplaceSyncAusschlussliste", ct),
            await ServiceSettings.GetBoolSafeAsync(_config, "Sync:OperationConfigSyncFremdsystemRelevant", false, ct),
            await ServiceSettings.GetIntSafeAsync(_config, "Sync:ProductionWorkplaceSyncMandant", 1, ct));
        return await RunAsync(dryRun, settings, ct);
    }

    internal async Task<SyncResult> RunAsync(bool dryRun, OperationConfigSyncSettings settings, CancellationToken ct)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.OperationConfigSync, ct);
        var details = 0;
        async Task Detail(bool warning, string message, string? reference = null)
        {
            if (details++ >= MaxDetailLines) return;
            if (message.Length > MaxMessageLength) message = message[..(MaxMessageLength - 1)] + "…";
            if (warning) await run.LogWarningAsync(message, reference, ct);
            else await run.LogInfoAsync(message, reference, ct);
        }

        string quotedSource;
        try
        {
            quotedSource = FaHierarchySql.ValidateAndQuote(string.IsNullOrWhiteSpace(settings.ViewName) ? DefaultViewName : settings.ViewName);
        }
        catch (FaHierarchyViewNameException ex)
        {
            // Kein SQL. Fehler + Fehlermail (throw -> RunResilientAsync).
            await run.LogErrorAsync($"Ungültiger View-Name: {ex.Message}", ct: ct);
            await run.FinishFailedAsync($"Ungültiger View-Name: {ex.Message}", ct: ct);
            throw new InvalidOperationException(
                $"Arbeitsgang-Abgleich abgebrochen — ungueltiger View-Name (kein SQL ausgefuehrt): {ex.Message}", ex);
        }

        var ausschluss = (settings.Ausschlussliste ?? DefaultAusschlussliste)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Bestand VOR dem Sage-Read laden (tracked; Naht fuer den Test "waehrend des Laufs geloescht").
        var configs = await _ctx.OperationConfigs.ToListAsync(ct);

        List<SageArbeitsplatzDto> rows;
        try
        {
            var read = await _reader.GetActiveAsync(quotedSource, settings.Mandant, ct);
            if (!read.SourceExists)
            {
                await run.LogWarningAsync($"{FaHierarchySql.SourceMissingMessage(quotedSource)} — Lauf übersprungen.", ct: ct);
                await run.FinishSuccessAsync(EmptyCounts(), messageSuffix: dryRun ? "Quelle nicht vorhanden [DryRun]" : "Quelle nicht vorhanden", ct: ct);
                return new SyncResult(0, 0, 0, "Quelle nicht vorhanden.");
            }
            rows = read.Rows;
            if (_reader is SageQuelleWeiche { GenutzteQuelle: not null } weiche)
                await weiche.LogQuelleAsync(run, ct); // F1
            if (!read.ArbeitsschrittGeliefert)
            {
                // E6a/G12, TODO(P-A1): SData liefert USER_ArbeitsSchritt (noch) nicht. Ohne Kuerzel waere jeder Eintrag "nicht in Sage" —
                // daher kein Abgleich (keine Anlage, keine Kennzeichnung), sichtbar als Warnung.
                await run.LogWarningAsync("Quelle liefert kein Arbeitsschritt-Kuerzel (USER_ArbeitsSchritt, Pruefabfrage P-A1) — " +
                    "keine Anlage, keine Kennzeichnung.", ct: ct);
                await run.FinishSuccessAsync(EmptyCounts(), messageSuffix: dryRun ? "Kuerzel nicht geliefert [DryRun]" : "Kuerzel nicht geliefert", ct: ct);
                return new SyncResult(0, 0, 0, "Kuerzel nicht geliefert.");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Sage-Connection (Arbeitsgang-Abgleich) fehlgeschlagen.");
            await run.LogErrorAsync($"Sage-Connection fehlgeschlagen: {ex.Message}", ct: ct);
            await run.FinishFailedAsync($"Sage-Connection fehlgeschlagen: {ex.Message}", ct: ct);
            throw;
        }

        if (rows.Count == 0)
        {
            // Leere Quelle: sonst waeren alle Eintraege "nicht in Sage".
            await run.LogWarningAsync($"Sage-Quelle {quotedSource} liefert keine Zeilen — keine Anlage, keine Kennzeichnung.", ct: ct);
            await run.FinishSuccessAsync(EmptyCounts(), messageSuffix: dryRun ? "Quelle leer [DryRun]" : "Quelle leer", ct: ct);
            return new SyncResult(0, 0, 0, "Quelle leer.");
        }

        try
        {
            // Sage-Arbeitsgangnummer nur aus der Arbeitsgang-View (Spalte Arbeitsgangnummer = KHKPpsArbeitsgaenge, P-AG1). Die Tabelle
            // KHKPpsArbeitsplaetze und SData (PpsArbeitsplaetzeInfo) liefern in Spalte 0 die ARBEITSPLATZnummer -> dann nichts aendern.
            var nummerLesen = !SageArbeitsplatzSql.IsTable(quotedSource)
                && _reader is not SageQuelleWeiche { GenutzteQuelle: SageQuelle.SData };
            var plan = Plan(rows,
                configs.Select(c => new ExistingOperationConfig(c.Id, c.OperationName, c.Source, c.SageMissingSince,
                    c.SageArbeitsgangnummer, c.SageArbeitsgangnummerManuell)).ToList(),
                ausschluss, nummerLesen);
            foreach (var w in plan.Warnings) await Detail(true, w);

            int neu = 0, fehler = 0, quelleSage = 0, wiederInSage = 0, nummerGesetzt = 0;
            var nichtInSage = plan.NichtInSage;
            var angelegt = new List<string>();
            var neuNichtInSage = new List<string>();
            var wieder = new List<string>();
            var gewechselt = new List<string>();
            var nummern = new List<string>();
            var byId = configs.ToDictionary(c => c.Id);
            var now = DateTime.Now;

            foreach (var (name, display, nummer) in plan.New)
            {
                ct.ThrowIfCancellationRequested();
                if (dryRun) { neu++; angelegt.Add(Label(name, display, nummer)); continue; }
                var entity = new OperationConfig
                {
                    OperationName = name,
                    DisplayName = display,
                    DueDateOffsetDays = 0,
                    IsExternalSystemRelevant = settings.FremdsystemRelevant, // immer explizit: Model-Default ist true
                    Source = OperationConfigSource.Sage,
                    SageMissingSince = null,
                    SageArbeitsgangnummer = nummer, // Handwert bleibt NULL (FA-3)
                    CreatedAt = now, CreatedBy = ServiceUser, CreatedByWindows = ServiceUser,
                };
                _ctx.OperationConfigs.Add(entity);
                if (await TrySaveAsync(entity, $"Anlage von Kürzel {name} fehlgeschlagen", name))
                {
                    neu++;
                    angelegt.Add(Label(name, display, nummer));
                }
                else fehler++;
            }

            // Bestandszeilen je Eintrag speichern (FA 2.7): erst Quelle-Wechsel, dann Kennzeichnung.
            async Task<bool> Change(int id, Action<OperationConfig> apply)
            {
                if (dryRun) return true;
                var e = byId[id];
                apply(e);
                e.ModifiedAt = now; e.ModifiedBy = ServiceUser; e.ModifiedByWindows = ServiceUser;
                return await TrySaveAsync(e, $"Eintrag {e.OperationName}", e.OperationName);
            }

            foreach (var id in plan.SourceToSage)
            {
                if (await Change(id, e => e.Source = OperationConfigSource.Sage)) { quelleSage++; gewechselt.Add(byId[id].OperationName); }
                else fehler++;
            }
            // Sage-Wert der Arbeitsgangnummer (FA-2); der Handwert wird nie angefasst (FA-3). Abgehaengte (geloeschte) Eintraege nicht.
            foreach (var (id, nummer) in plan.NummerSetzen.Where(x => _ctx.Entry(byId[x.Id]).State != EntityState.Detached))
            {
                var e = byId[id];
                var alt = e.SageArbeitsgangnummer;
                if (!await Change(id, x => x.SageArbeitsgangnummer = nummer)) { fehler++; continue; }
                nummerGesetzt++;
                nummern.Add($"{e.OperationName} {nummer ?? "(leer)"}");
                var hand = e.SageArbeitsgangnummerManuell;
                if (hand != null && nummer != null && !string.Equals(hand.Trim(), nummer, StringComparison.Ordinal))
                    await Detail(true, $"Sage-Arbeitsgangnummer geändert, Handwert weicht ab: {e.OperationName} Sage {alt ?? "(leer)"} → {nummer} / Hand {hand}",
                        e.OperationName);
            }
            foreach (var id in plan.Mark)
            {
                if (await Change(id, e => e.SageMissingSince = now)) neuNichtInSage.Add(byId[id].OperationName);
                else { fehler++; nichtInSage--; }
            }
            // Ein Eintrag kann in SourceToSage UND Unmark stehen; ist er beim Wechsel schon als geloescht abgehaengt (fehler gezaehlt),
            // wuerde SaveChanges auf dem Detached-Objekt nichts schreiben und "wieder in Sage" falsch melden.
            foreach (var id in plan.Unmark.Where(id => _ctx.Entry(byId[id]).State != EntityState.Detached))
            {
                if (await Change(id, e => e.SageMissingSince = null)) { wiederInSage++; wieder.Add(byId[id].OperationName); }
                else fehler++;
            }
            foreach (var id in plan.Clear)
                if (!await Change(id, e => e.SageMissingSince = null)) fehler++;

            if (angelegt.Count > 0) await Detail(false, "angelegt: " + string.Join(", ", angelegt));
            if (gewechselt.Count > 0) await Detail(false, "Quelle Manuell → Sage: " + string.Join(", ", gewechselt));
            if (neuNichtInSage.Count > 0) await Detail(true, "neu nicht in Sage: " + string.Join(", ", neuNichtInSage));
            if (wieder.Count > 0) await Detail(false, "wieder in Sage: " + string.Join(", ", wieder));
            if (nummern.Count > 0) await Detail(false, "Sage-Arbeitsgangnummer gesetzt: " + string.Join(", ", nummern));
            if (plan.OhneKuerzel.Count > 0) await Detail(false, "Sage-Arbeitsgänge ohne Kürzel: " + string.Join(", ", plan.OhneKuerzel));

            _logger.LogInformation(
                "Arbeitsgang-Abgleich: {Neu} neu, {Vorh} vorhanden, {Nicht} nicht in Sage, {Err} Fehler{Dry}.",
                neu, plan.Vorhanden, nichtInSage, fehler, dryRun ? " [DryRun]" : "");

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["neu"] = neu,
                ["vorhanden"] = plan.Vorhanden,
                ["uebersprungen"] = plan.Uebersprungen,
                ["name_mehrdeutig"] = plan.NameMehrdeutig,
                ["nicht_in_sage"] = nichtInSage,
                ["wieder_in_sage"] = wiederInSage,
                ["quelle_sage"] = quelleSage,
                ["nummer_gesetzt"] = nummerGesetzt,
                ["nummer_mehrdeutig"] = plan.NummerMehrdeutig,
                ["abweichung_hand"] = plan.AbweichungHand,
                ["ohne_kuerzel"] = plan.OhneKuerzel.Count,
                ["fehler"] = fehler,
            }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);

            return new SyncResult(neu, quelleSage + plan.Mark.Count + plan.Unmark.Count + nummerGesetzt, fehler);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Arbeitsgang-Abgleich fehlgeschlagen.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }

        // Speichert einen Eintrag; Unique-Verstoss (Insert) oder inzwischen geloeschte Zeile (Update) -> abhaengen, melden,
        // weiter (FA 2.6/2.7, keine Mail).
        async Task<bool> TrySaveAsync(OperationConfig entity, string what, string reference)
        {
            try
            {
                await _ctx.SaveChangesAsync(ct);
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                _ctx.Entry(entity).State = EntityState.Detached;
                await Detail(true, $"{what} inzwischen gelöscht — übersprungen.", reference);
                return false;
            }
            catch (DbUpdateException ex)
            {
                _ctx.Entry(entity).State = EntityState.Detached;
                await Detail(true, $"{what}: {ex.GetBaseException().Message} — übersprungen.", reference);
                return false;
            }
        }
    }

    /// <summary>
    /// Reine Entscheidung (FA 2–4, 6): gruppiert die View nach getrimmtem AG (OrdinalIgnoreCase), bestimmt Neuanlagen, Quelle-Wechsel
    /// Manual -> Sage und die Kennzeichnung. Keine DB, keine Zeit.
    /// </summary>
    internal static OperationConfigSyncPlan Plan(
        IReadOnlyList<SageArbeitsplatzDto> rows, IReadOnlyList<ExistingOperationConfig> existing, IReadOnlySet<string> ausschluss,
        bool nummerLesen = true)
    {
        var plan = new OperationConfigSyncPlan();
        var groups = new Dictionary<string, List<SageArbeitsplatzDto>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var ag = (row.Arbeitsschritt ?? string.Empty).Trim();
            if (ag.Length == 0)
            {
                plan.Uebersprungen++;
                var ohne = $"{row.Arbeitsplatznummer?.Trim()} {row.Bezeichnung1?.Trim()}".Trim();
                if (nummerLesen && ohne.Length > 0) plan.OhneKuerzel.Add(ohne); // Pruefliste (c), nur Info
                continue;
            }
            (groups.TryGetValue(ag, out var l) ? l : groups[ag] = new List<SageArbeitsplatzDto>()).Add(row);
        }

        var byName = new Dictionary<string, ExistingOperationConfig>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in existing) byName.TryAdd(e.OperationName.Trim(), e);

        foreach (var (key, group) in groups.OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            if (ausschluss.Contains(key)) { plan.Uebersprungen++; continue; }

            var spellings = group.Select(r => r.Arbeitsschritt!.Trim()).Distinct(StringComparer.Ordinal)
                .OrderBy(s => s, StringComparer.Ordinal).ToList();
            var name = spellings[0];
            if (spellings.Count > 1)
                plan.Warnings.Add($"Kürzel in Sage in mehreren Schreibweisen: {string.Join(" / ", spellings)} — verwendet: {name}");

            if (name.Length > 100)
            {
                plan.Uebersprungen++;
                plan.Warnings.Add($"Kürzel {name[..20]}… länger als 100 Zeichen — übersprungen.");
                continue;
            }

            var (nummerSetzen, nummer) = nummerLesen ? Nummer(name, group, plan) : (false, null);

            if (byName.TryGetValue(name, out var found))
            {
                plan.Vorhanden++;
                if (found.Source == OperationConfigSource.Manual) plan.SourceToSage.Add(found.Id);
                if (nummerSetzen && !string.Equals(found.SageArbeitsgangnummer, nummer, StringComparison.Ordinal))
                    plan.NummerSetzen.Add((found.Id, nummer));
                continue;
            }

            var names = group.Select(r => (r.Bezeichnung1 ?? string.Empty).Trim()).Where(b => b.Length > 0)
                .Distinct(StringComparer.Ordinal).ToList();
            string? display = null;
            if (names.Count == 1 && names[0].Length <= 200) display = names[0];
            else if (names.Count == 1)
                plan.Warnings.Add($"Kürzel {name}: Bezeichnung länger als 200 Zeichen — Anzeigename leer gelassen.");
            else if (names.Count > 1)
            {
                plan.NameMehrdeutig++;
                plan.Warnings.Add($"Kürzel {name}: mehrere Bezeichnungen ({string.Join(" / ", names)}) — Anzeigename leer gelassen.");
            }
            plan.New.Add((name, display, nummerSetzen ? nummer : null));
        }

        foreach (var e in existing)
        {
            var key = e.OperationName.Trim();
            if (e.Source == OperationConfigSource.ExternalSystem || ausschluss.Contains(key))
            {
                if (e.SageMissingSince != null) plan.Clear.Add(e.Id);
                continue;
            }
            if (!groups.ContainsKey(key))
            {
                if (e.SageMissingSince == null) plan.Mark.Add(e.Id);
                plan.NichtInSage++;
            }
            else if (e.SageMissingSince != null) plan.Unmark.Add(e.Id);
        }

        // Abweichung Sage <-> Hand nach dem Lauf (Zaehler je Lauf, FA-4)
        var neueNummer = plan.NummerSetzen.ToDictionary(x => x.Id, x => x.Nummer);
        plan.AbweichungHand = existing.Count(e =>
        {
            var sage = neueNummer.TryGetValue(e.Id, out var n) ? n : e.SageArbeitsgangnummer;
            return e.SageArbeitsgangnummerManuell != null && sage != null
                && !string.Equals(e.SageArbeitsgangnummerManuell.Trim(), sage.Trim(), StringComparison.Ordinal);
        });
        return plan;
    }

    /// <summary>Sage-Arbeitsgangnummer einer Kuerzel-Gruppe (FA-2): genau eine Nummer -> diese, keine -> NULL (spiegeln, Rueckfrage 6),
    /// mehrere oder laenger als 31 -> nicht aendern, mit Warnung.</summary>
    private static (bool Setzen, string? Nummer) Nummer(string name, List<SageArbeitsplatzDto> group, OperationConfigSyncPlan plan)
    {
        var nummern = group.Select(r => (r.Arbeitsplatznummer ?? string.Empty).Trim()).Where(n => n.Length > 0)
            .Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToList();
        if (nummern.Count == 0) return (true, null);
        if (nummern.Count > 1)
        {
            plan.NummerMehrdeutig++;
            plan.Warnings.Add($"Kürzel {name}: mehrere Sage-Arbeitsgangnummern ({string.Join(" / ", nummern)}) – Nummer nicht geändert.");
            return (false, null);
        }
        if (nummern[0].Length > 31)
        {
            plan.Warnings.Add($"Kürzel {name}: Sage-Arbeitsgangnummer länger als 31 Zeichen – Nummer nicht geändert.");
            return (false, null);
        }
        return (true, nummern[0]);
    }

    private static string Label(string name, string? display, string? nummer = null)
    {
        var info = string.Join(", ", new[] { display, nummer == null ? null : "Sage-AG " + nummer }.Where(x => x != null));
        return info.Length == 0 ? name : $"{name} ({info})";
    }

    private static Dictionary<string, int> EmptyCounts() => new()
    {
        ["neu"] = 0, ["vorhanden"] = 0, ["uebersprungen"] = 0, ["name_mehrdeutig"] = 0,
        ["nicht_in_sage"] = 0, ["wieder_in_sage"] = 0, ["quelle_sage"] = 0, ["nummer_gesetzt"] = 0, ["nummer_mehrdeutig"] = 0,
        ["abweichung_hand"] = 0, ["ohne_kuerzel"] = 0, ["fehler"] = 0,
    };
}
