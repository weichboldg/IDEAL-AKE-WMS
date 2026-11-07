using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Services.Sdata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

public class LagerplatzSyncService : ILagerplatzSyncService
{
    // Sage-Codes duerfen den vollen DB-Platz nutzen (NVARCHAR(50)). Manuelle
    // Codes sind via Model-Validation auf 12 Zeichen begrenzt.
    private const int MaxCodeLength = 50;
    private const int MaxDescriptionLength = 200;
    private const string SyncUser = "system:sync";
    // StorageAreas-Laengen laut SData-$schema (P-S1): Lagerkennung(10), Bezeichnung(40), Lagertechnik(10).
    private const int MaxLagerkennungLength = 10;
    private const int MaxLagerortBezeichnungLength = 40;
    private const int MaxLagertechnikLength = 10;

    private readonly ApplicationDbContext _ctx;
    private readonly ISageLagerplatzReader _reader;
    private readonly ISyncErrorNotifier _errorNotifier;
    private readonly ISyncLogger _syncLogger;
    private readonly ILogger<LagerplatzSyncService> _logger;

    public LagerplatzSyncService(
        ApplicationDbContext ctx,
        ISageLagerplatzReader reader,
        ILogger<LagerplatzSyncService> logger,
        ISyncErrorNotifier errorNotifier,
        ISyncLogger syncLogger)
    {
        _ctx = ctx;
        _reader = reader;
        _errorNotifier = errorNotifier;
        _syncLogger = syncLogger;
        _logger = logger;
    }

    public async Task<LagerplatzSyncResult> RunAsync(CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.Lagerplatz, ct);
        int inserted = 0, updated = 0, conflicts = 0, deactivated = 0, skipped = 0, errors = 0;

        try
        {
            List<SageLagerplatzDto> sageRecords;
            try
            {
                sageRecords = await _reader.GetAllActiveAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Sage-Connection fehlgeschlagen.");
                await LogQuelleAsync(run, ct);
                await run.LogErrorAsync($"Sage-Connection fehlgeschlagen: {ex.Message}", ct: ct);
                // G10: bei Quelle SData/Vergleich zusaetzlich Fehlermail; Quelle View wie bisher ohne Mail.
                if (_reader is SageQuelleWeiche { GenutzteQuelle: SageQuelle.SData or SageQuelle.Vergleich })
                    await _errorNotifier.NotifyAsync("Lagerplatz-Sync", ex, ct);
                errors++;
                await run.FinishFailedAsync($"Sage-Connection fehlgeschlagen: {ex.Message}", counts: new Dictionary<string, int>
                {
                    ["neu"] = inserted,
                    ["aktualisiert"] = updated,
                    ["konflikte"] = conflicts,
                    ["deaktiviert"] = deactivated,
                    ["uebersprungen"] = skipped,
                    ["fehler"] = errors,
                }, ct: ct);
                return new LagerplatzSyncResult(0, 0, 0, 0, 0, 1);
            }
            await LogQuelleAsync(run, ct);

            // E1: Sage-Lagerorte (nur wenn die Quelle sie liefert = SData; vor der Dubletten-Bereinigung, alle Zeilen zaehlen)
            var areaByKennung = await SyncStorageAreasAsync(sageRecords, run, ct);

            // Sage-Duplikate erkennen und entfernen
            var dupGroups = sageRecords
                .Where(r => !string.IsNullOrWhiteSpace(r.Kurzbezeichnung))
                .GroupBy(r => r.Kurzbezeichnung!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in dupGroups)
            {
                var bereiche = string.Join(", ", group.Select(g => g.Lagerkennung ?? "?"));
                await run.LogWarningAsync(
                    $"Sage liefert Lagerplatz '{group.Key}' mehrfach (Bereiche {bereiche}). Eintraege uebersprungen.",
                    reference: group.Key, ct: ct);
            }

            var dupCodes = dupGroups.Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            sageRecords = sageRecords
                .Where(r => string.IsNullOrWhiteSpace(r.Kurzbezeichnung)
                         || !dupCodes.Contains(r.Kurzbezeichnung!.Trim()))
                .ToList();

            var existing = await _ctx.StorageLocations.ToListAsync(ct);
            var byCode = existing.ToDictionary(x => x.Code, x => x, StringComparer.OrdinalIgnoreCase);

            foreach (var dto in sageRecords)
            {
                if (!string.IsNullOrWhiteSpace(dto.Kurzbezeichnung))
                {
                    var rawCode = dto.Kurzbezeichnung.Trim();
                    if (rawCode.Length > MaxCodeLength)
                    {
                        await run.LogWarningAsync(
                            $"Lagerplatz-Code '{rawCode}' ist zu lang ({rawCode.Length} > {MaxCodeLength}), uebersprungen.",
                            reference: rawCode, ct: ct);
                        skipped++;
                        continue;
                    }
                }

                if (string.IsNullOrWhiteSpace(dto.Kurzbezeichnung))
                {
                    skipped++;
                    continue;
                }

                var code = dto.Kurzbezeichnung.Trim();
                var zone = string.IsNullOrWhiteSpace(dto.Lagerkennung) ? null : dto.Lagerkennung.Trim();
                var description = string.IsNullOrWhiteSpace(dto.Platzbezeichnung) ? null : dto.Platzbezeichnung.Trim();

                if (description != null && description.Length > MaxDescriptionLength)
                {
                    await run.LogInfoAsync(
                        $"Beschreibung von '{code}' auf {MaxDescriptionLength} Zeichen gekuerzt.",
                        reference: code, ct: ct);
                    description = description.Substring(0, MaxDescriptionLength);
                }

                if (!byCode.ContainsKey(code))
                {
                    _ctx.StorageLocations.Add(new StorageLocation
                    {
                        Code = code,
                        Zone = zone,
                        Description = description,
                        BarcodeValue = code,
                        Source = StorageLocationSource.Sage,
                        IsActive = true,
                        IstBuchbar = false,                       // NEU: Sage-Plaetze sind by default nicht buchbar
                        // Sage-Lagerbuchung: dedizierte, vom frei editierbaren Zone entkoppelte Referenzfelder.
                        // SageLagerkennung = volle Kurzbezeichnung (= Code, z.B. "LL;1;4;0"), NICHT die bare
                        // Lagerkennung; SageLagerplatzId = KHKLagerplaetze.PlatzID.
                        SageLagerkennung = code,
                        SageLagerplatzId = dto.PlatzId,
                        StorageArea = Area(areaByKennung, dto),
                        SageIstGesperrt = dto.IstGesperrt,
                        SageIstGesperrtInventur = dto.IstGesperrtInventur,
                        SageBreite = dto.Breite,
                        SageHoehe = dto.Hoehe,
                        SageLaenge = dto.Laenge,
                        SageVolumen = dto.Volumen,
                        SageTragkraft = dto.Tragkraft,
                        Capacity = null,
                        IsPickingTransport = false,
                        CreatedAt = DateTime.Now,
                        CreatedBy = SyncUser,
                        CreatedByWindows = Environment.MachineName
                    });
                    inserted++;
                }
                else
                {
                    var existingLoc = byCode[code];
                    // E9/F11: ein im WMS angelegter Platz mit offener Uebergabe ist jetzt in Sage -> uebernehmen (Herkunft Sage), statt
                    // ihn als Konflikt zu melden. IstBuchbar/SageBuchungErlaubt bleiben (der Update-Zweig unten fasst sie nie an).
                    // Bestandsplaetze (NichtNoetig) bleiben Konflikt wie bisher.
                    if (existingLoc.Source != StorageLocationSource.Sage && SageUebergabeStatusText.IstOffen(existingLoc.SageUebergabeStatus))
                    {
                        existingLoc.Source = StorageLocationSource.Sage;
                        existingLoc.SageUebergabeStatus = SageUebergabeStatus.Uebergeben;
                        existingLoc.SageUebergabeAm = DateTime.Now;
                        existingLoc.SageUebergabeFehler = null;
                        existingLoc.ModifiedAt = DateTime.Now;
                        existingLoc.ModifiedBy = SyncUser;
                        existingLoc.ModifiedByWindows = Environment.MachineName;
                        await SageStammdatenOutbox.ErledigenAsync(_ctx, SageStammdatenOutbox.TypLagerplatz, existingLoc.Id, SyncUser,
                            Environment.MachineName, ct);
                        await run.LogInfoAsync(
                            $"Lagerplatz {code} (im WMS angelegt, Übergabe offen) in Sage gefunden -> übernommen, Quelle jetzt Sage. " +
                            "Ab dem nächsten Lauf gleicht der Lagerbestand-Abgleich seinen Bestand mit Sage ab.",
                            reference: code, ct: ct);
                    }

                    if (existingLoc.Source == StorageLocationSource.Sage)
                    {
                        // E1/F1: SData-only-Felder nur vergleichen/schreiben, wenn die Quelle sie liefert (null = nicht schreiben).
                        var area = Area(areaByKennung, dto);
                        var diff = existingLoc.Zone != zone
                                || (area is not null && existingLoc.StorageArea != area)
                                || Abweichend(existingLoc.SageIstGesperrt, dto.IstGesperrt)
                                || Abweichend(existingLoc.SageIstGesperrtInventur, dto.IstGesperrtInventur)
                                || Abweichend(existingLoc.SageBreite, dto.Breite)
                                || Abweichend(existingLoc.SageHoehe, dto.Hoehe)
                                || Abweichend(existingLoc.SageLaenge, dto.Laenge)
                                || Abweichend(existingLoc.SageVolumen, dto.Volumen)
                                || Abweichend(existingLoc.SageTragkraft, dto.Tragkraft)
                                || existingLoc.Description != description
                                || existingLoc.BarcodeValue != code
                                || existingLoc.SageLagerkennung != code
                                || existingLoc.SageLagerplatzId != dto.PlatzId
                                || !existingLoc.IsActive;

                        if (diff)
                        {
                            existingLoc.Zone = zone;
                            existingLoc.Description = description;
                            existingLoc.BarcodeValue = code;
                            existingLoc.SageLagerkennung = code;
                            existingLoc.SageLagerplatzId = dto.PlatzId;
                            existingLoc.StorageArea = area ?? existingLoc.StorageArea;
                            existingLoc.SageIstGesperrt = dto.IstGesperrt ?? existingLoc.SageIstGesperrt;
                            existingLoc.SageIstGesperrtInventur = dto.IstGesperrtInventur ?? existingLoc.SageIstGesperrtInventur;
                            existingLoc.SageBreite = dto.Breite ?? existingLoc.SageBreite;
                            existingLoc.SageHoehe = dto.Hoehe ?? existingLoc.SageHoehe;
                            existingLoc.SageLaenge = dto.Laenge ?? existingLoc.SageLaenge;
                            existingLoc.SageVolumen = dto.Volumen ?? existingLoc.SageVolumen;
                            existingLoc.SageTragkraft = dto.Tragkraft ?? existingLoc.SageTragkraft;
                            existingLoc.IsActive = true;
                            existingLoc.ModifiedAt = DateTime.Now;
                            existingLoc.ModifiedBy = SyncUser;
                            existingLoc.ModifiedByWindows = Environment.MachineName;
                            updated++;
                        }
                    }
                    else // Manual
                    {
                        await run.LogWarningAsync(
                            $"Konflikt: Lagerplatz {code} existiert manuell, Sage-Eintrag ignoriert.",
                            reference: code, ct: ct);
                        conflicts++;
                    }
                }
            }

            // Soft-deactivate Sage-Records, die nicht mehr in der Sage-Liste sind
            var sageCodesInFeed = sageRecords
                .Select(r => string.IsNullOrWhiteSpace(r.Kurzbezeichnung) ? null : r.Kurzbezeichnung.Trim())
                .Where(c => c != null)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)!;

            foreach (var loc in existing.Where(l => l.Source == StorageLocationSource.Sage && l.IsActive))
            {
                if (!sageCodesInFeed.Contains(loc.Code))
                {
                    loc.IsActive = false;
                    loc.ModifiedAt = DateTime.Now;
                    loc.ModifiedBy = SyncUser;
                    loc.ModifiedByWindows = Environment.MachineName;
                    deactivated++;

                    await run.LogInfoAsync(
                        $"Lagerplatz {loc.Code} aus Sage entfernt -> deaktiviert.",
                        reference: loc.Code, ct: ct);
                }
            }

            await _ctx.SaveChangesAsync(ct);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["neu"] = inserted,
                ["aktualisiert"] = updated,
                ["konflikte"] = conflicts,
                ["deaktiviert"] = deactivated,
                ["uebersprungen"] = skipped,
                ["fehler"] = errors,
            }, ct: ct);

            return new LagerplatzSyncResult(inserted, updated, conflicts, deactivated, skipped, errors);
        }
        catch (Exception ex)
        {
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, counts: new Dictionary<string, int>
            {
                ["neu"] = inserted,
                ["aktualisiert"] = updated,
                ["konflikte"] = conflicts,
                ["deaktiviert"] = deactivated,
                ["uebersprungen"] = skipped,
                ["fehler"] = errors,
            }, ct: ct);
            throw;
        }
    }

    /// <summary>Info-Zeile „Quelle: View|SData|Vergleich“ (F1), nur wenn der Reader die Quellen-Weiche ist.</summary>
    private async Task LogQuelleAsync(ISyncRun run, CancellationToken ct)
    {
        if (_reader is SageQuelleWeiche { GenutzteQuelle: not null } weiche)
            await weiche.LogQuelleAsync(run, ct);
    }

    private static bool Abweichend<T>(T? alt, T? neu) where T : struct => neu.HasValue && !Equals(alt, neu);

    private static StorageArea? Area(Dictionary<string, StorageArea>? areaByKennung, SageLagerplatzDto dto) =>
        dto.Lagerort is null || areaByKennung is null ? null : areaByKennung.GetValueOrDefault(dto.Lagerort.Lagerkennung.Trim());

    /// <summary>
    /// E1/F3: Sage-Lagerorte upserten (Schluessel Lagerkennung, Gross/Klein egal). Liefert die Quelle keine Lagerort-Daten
    /// (View-Reader), bleibt die Tabelle unberuehrt (null). Lagerorte werden nie geloescht: fehlt einer (oder ist er in Sage
    /// inaktiv — der Reader liefert nur aktive), bekommt er SageMissingSince und IsActive = false.
    /// ponytail: liefert SData gar keinen aktiven Lagerort, gilt das wie "keine Lagerort-Daten" (nichts wird als fehlend markiert);
    /// der Platz-Abgleich deaktiviert dann ohnehin alle Sage-Plaetze und meldet das je Platz.
    /// </summary>
    private async Task<Dictionary<string, StorageArea>?> SyncStorageAreasAsync(List<SageLagerplatzDto> records, ISyncRun run, CancellationToken ct)
    {
        var orte = records.Where(r => r.Lagerort is not null).Select(r => r.Lagerort!)
            .GroupBy(o => o.Lagerkennung.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        if (orte.Count == 0)
            return null;

        int neu = 0, aktualisiert = 0, fehlend = 0;
        var now = DateTime.Now;
        var areas = await _ctx.StorageAreas.ToListAsync(ct);
        var byKennung = areas.ToDictionary(a => a.Lagerkennung, StringComparer.OrdinalIgnoreCase);
        foreach (var o in orte)
        {
            var kennung = o.Lagerkennung.Trim();
            if (kennung.Length > MaxLagerkennungLength)
            {
                await run.LogWarningAsync($"Lagerkennung '{kennung}' ist zu lang ({kennung.Length} > {MaxLagerkennungLength}), Lagerort uebersprungen.",
                    reference: kennung, ct: ct);
                continue;
            }
            var bezeichnung = Kappe(o.Bezeichnung, MaxLagerortBezeichnungLength);
            var technik = Kappe(o.Lagertechnik, MaxLagertechnikLength);

            if (!byKennung.TryGetValue(kennung, out var area))
            {
                area = new StorageArea
                {
                    Lagerkennung = kennung, Bezeichnung = bezeichnung, Lagertechnik = technik, IstDispoLager = o.IstDispoLager,
                    IsActive = true, CreatedAt = now, CreatedBy = SyncUser, CreatedByWindows = Environment.MachineName,
                };
                _ctx.StorageAreas.Add(area);
                byKennung[kennung] = area;
                neu++;
            }
            else if (area.Bezeichnung != bezeichnung || area.Lagertechnik != technik || area.IstDispoLager != o.IstDispoLager
                     || !area.IsActive || area.SageMissingSince is not null)
            {
                area.Bezeichnung = bezeichnung;
                area.Lagertechnik = technik;
                area.IstDispoLager = o.IstDispoLager;
                area.IsActive = true;
                area.SageMissingSince = null;
                area.ModifiedAt = now;
                area.ModifiedBy = SyncUser;
                area.ModifiedByWindows = Environment.MachineName;
                aktualisiert++;
            }
        }

        var imFeed = orte.Select(o => o.Lagerkennung.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var area in areas.Where(a => a.SageMissingSince is null && !imFeed.Contains(a.Lagerkennung)))
        {
            area.SageMissingSince = now;
            area.IsActive = false;
            area.ModifiedAt = now;
            area.ModifiedBy = SyncUser;
            area.ModifiedByWindows = Environment.MachineName;
            fehlend++;
            await run.LogInfoAsync($"Lagerort {area.Lagerkennung} nicht (mehr) aktiv in Sage -> 'nicht in Sage seit' gesetzt.",
                reference: area.Lagerkennung, ct: ct);
        }

        await run.LogInfoAsync($"Lagerorte: {neu} neu, {aktualisiert} aktualisiert, {fehlend} nicht mehr in Sage.", ct: ct);
        return byKennung;
    }

    private static string? Kappe(string? s, int max)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim();
        return t.Length > max ? t[..max] : t;
    }
}
