using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Common;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// v1.99.10 (Spec 2026-11-12-offene-bestellungen-mahnmail-lieferverzug): Lauf „Lieferverzug-Mails“ im SyncWorker nach „Offene Bestellungen
/// (Sage)“. Quelle nur der Cache <see cref="SagePurchaseOrderLine"/> (kein Sage-Read, schreibt nie nach Sage). Ablauf: Zeitfenster → heute
/// schon gesendet (Protokolltabelle, F6) → Planer → Kreditor-Mails (je Mail eigener Fehlerfang) → Einkauf-Sammelliste mit den Ergebnissen →
/// Protokollzeilen nur nach <c>true</c> vom Mailservice. Ohne Arbeit kein Lauf-Eintrag.
/// </summary>
public class LieferverzugMailService
{
    public const string LogoRessource = "IdealAkeLogo.jpg";

    private readonly ApplicationDbContext _ctx;
    private readonly IPurchaseOrderReceiptRepository _receipts;
    private readonly IMailService _mail;
    private readonly ISyncErrorNotifier _errorNotifier;
    private readonly ILogger<LieferverzugMailService> _logger;
    private readonly ISyncLogger _syncLogger;

    public LieferverzugMailService(ApplicationDbContext ctx, IPurchaseOrderReceiptRepository receipts, IMailService mail,
        ISyncErrorNotifier errorNotifier, ILogger<LieferverzugMailService> logger, ISyncLogger syncLogger) // ISyncLogger LETZTER Parameter (ADR 0010)
    {
        _ctx = ctx;
        _receipts = receipts;
        _mail = mail;
        _errorNotifier = errorNotifier;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    /// <summary>Liest die Einstellungen DB-first (ServiceSettings je Standort); Grenzen wie im Katalog, Standort exakt „IDEAL“ sonst AKE.</summary>
    public static async Task<LieferverzugEinstellungen> LesenAsync(IConfiguration c, CancellationToken ct)
    {
        async Task<int> Int(string key, int def, int min, int max) => Math.Clamp(await ServiceSettings.GetIntSafeAsync(c, key, def, ct), min, max);
        async Task<string?> Str(string key) => (await ServiceSettings.GetValueSafeAsync(c, key, ct))?.Trim() is { Length: > 0 } s ? s : null;
        return new LieferverzugEinstellungen(
            EinkaufAktiv: await ServiceSettings.GetBoolSafeAsync(c, "Lieferverzug:EinkaufAktiv", true, ct),
            EinkaufEmpfaenger: MailAdressen.Liste(await Str("Lieferverzug:EinkaufEmpfaenger")),
            KreditorAktiv: await ServiceSettings.GetBoolSafeAsync(c, "Lieferverzug:KreditorAktiv", false, ct),
            Testbetrieb: await ServiceSettings.GetBoolSafeAsync(c, "Lieferverzug:Testbetrieb", true, ct),
            TageNachTermin: await Int("Lieferverzug:TageNachTermin", 3, 0, 60),
            Uhrzeit: await Int("Lieferverzug:Uhrzeit", 7, 0, 23),
            WiederholTage: await Int("Lieferverzug:KreditorWiederholTage", 7, 1, 60),
            MaxJeLauf: await Int("Lieferverzug:KreditorMaxJeLauf", 30, 1, 500),
            Absender: await Str("Lieferverzug:KreditorAbsender"),
            AntwortAdresse: await Str("Lieferverzug:AntwortAdresse"),
            Standort: await Str("Lieferverzug:Standort") == "IDEAL" ? "IDEAL" : "AKE",
            AppBaseUrl: await Str("Notifications:AppBaseUrl"));
    }

    public async Task RunAsync(LieferverzugEinstellungen e, bool dryRun, DateTime jetzt, CancellationToken ct = default)
    {
        if (!LieferverzugPlaner.ImZeitfenster(jetzt, e.Uhrzeit) || (!e.EinkaufAktiv && !e.KreditorAktiv)) return;
        var heute = jetzt.Date;
        // F6: einmal am Tag – steht in der Protokolltabelle, nicht im Arbeitsspeicher (Dienst-Neustart loest keine zweite Mail aus)
        if (await _ctx.PurchaseOrderReminders.AnyAsync(r => r.SentAt >= heute && r.SentAt < heute.AddDays(1), ct)) return;

        var zeilen = await _ctx.SagePurchaseOrderLines.AsNoTracking().ToListAsync(ct);
        DateTime? stand = zeilen.Count == 0 ? null : zeilen.Max(z => z.SyncedAt);
        var kandidaten = zeilen.Where(l => LieferverzugPlaner.TerminUeberschritten(l, heute, e.TageNachTermin)).ToList();
        if (kandidaten.Count == 0) return;

        var ids = kandidaten.Select(l => l.BelPosId).ToList();
        var erfasst = await _receipts.GetErfasstJeBelPosAsync(ids);
        var rest = kandidaten.ToDictionary(l => l.BelPosId, l => erfasst.GetValueOrDefault(l.BelPosId, ErfasstJeBelPos.Leer).Rest(l));

        var creditorIds = kandidaten.Where(l => l.CreditorId != null).Select(l => l.CreditorId!.Value).Distinct().ToList();
        var kreditoren = await _ctx.Creditors.AsNoTracking().Where(c => creditorIds.Contains(c.Id))
            .Select(c => new LieferverzugKreditor(c.Id, c.Name, c.Email, c.ReminderEmail, c.ReminderDisabled, c.IsActive))
            .ToDictionaryAsync(k => k.Id, ct);
        // Wiederholung: nur Zeilen des aktuellen Modus (Testbetrieb-Zeilen zaehlen nicht fuer echte Kreditor-Mails, AK 9)
        var letzteMail = (await _ctx.PurchaseOrderReminders.AsNoTracking()
                .Where(r => r.Art == PurchaseOrderReminderArt.Kreditor && r.Testbetrieb == e.Testbetrieb && ids.Contains(r.BelPosId))
                .Select(r => new { r.BelPosId, r.Liefertermin, r.SentAt }).ToListAsync(ct))
            .GroupBy(r => (r.BelPosId, r.Liefertermin?.Date))
            .ToDictionary(g => ((int, DateTime?))g.Key, g => g.Max(r => r.SentAt));

        var plan = LieferverzugPlaner.Planen(kandidaten, rest, kreditoren, letzteMail, e, jetzt, stand);
        if (plan.AnzahlFaellig == 0 || (!e.EinkaufAktiv && !plan.MitMail.Any() && !plan.GrenzeUeberschritten)) return;

        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.LieferverzugMail, ct);
        int einkaufGesendet = 0, kreditorGesendet = 0, umgeleitet = 0, fehler = 0, nichtGesendet = 0;

        if (e.EinkaufEmpfaenger.Count == 0)
            await run.LogWarningAsync("Keine Einkauf-Empfänger (Lieferverzug:EinkaufEmpfaenger) – keine Sammelliste" +
                (e.Testbetrieb && e.KreditorAktiv ? " und keine Testbetrieb-Mail." : "."), ct: ct);
        if (plan.StandAlt && e.KreditorAktiv)
            await run.LogWarningAsync($"Bestelldaten von {(stand?.ToString("dd.MM.yyyy HH:mm") ?? "(Cache leer)")} älter als 24 h – Kreditor-Mails ausgesetzt.", ct: ct);
        if (plan.GrenzeUeberschritten)
        {
            var msg = $"Lieferverzug-Mails: {plan.Gruppen.Count(g => g.Grund == LieferverzugGrund.Grenze)} Kreditor-Mails fällig > Grenze " +
                $"{e.MaxJeLauf} (Lieferverzug:KreditorMaxJeLauf) – keine Kreditor-Mail versendet.";
            await run.LogWarningAsync(msg, ct: ct);
            if (!dryRun) await _errorNotifier.NotifyAsync("Lieferverzug-Mails", new InvalidOperationException(msg), ct);
        }

        var bilder = LogoLaden();
        var antwortAn = e.AntwortAdresse ?? e.EinkaufEmpfaenger.FirstOrDefault();
        foreach (var g in plan.MitMail.ToList())
        {
            var test = g.Grund == LieferverzugGrund.Testbetrieb;
            IReadOnlyList<string> empfaenger = test ? e.EinkaufEmpfaenger : new[] { g.Adresse! };
            if (dryRun || empfaenger.Count == 0)
            {
                if (empfaenger.Count == 0) Ergebnis(g, LieferverzugGrund.NichtGesendet, "nicht gesendet – keine Einkauf-Empfänger");
                continue;
            }
            var inhalt = LieferverzugMailHtml.Kreditor(g, g.MailPositionen!, e.Standort, test);
            try
            {
                var ok = await _mail.SendMitOptionenAsync(new MailNachricht(inhalt.Betreff, inhalt.Html, inhalt.Text, empfaenger, antwortAn, e.Absender, bilder), ct);
                if (!ok)
                {
                    nichtGesendet++;
                    Ergebnis(g, LieferverzugGrund.NichtGesendet, "nicht gesendet – Mailversand nicht konfiguriert");
                    await run.LogWarningAsync($"Kreditor-Mail an {g.Name} nicht gesendet: Mailversand übersprungen (MailSettings:SmtpHost?).", g.Kto, ct);
                    continue;
                }
                await ProtokollAsync(PurchaseOrderReminderArt.Kreditor, g.MailPositionen!, empfaenger, test, g.CreditorId, jetzt, ct);
                if (test) umgeleitet++; else kreditorGesendet++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                fehler++;
                _logger.LogError(ex, "Lieferverzug-Mail an Kreditor {Kto} fehlgeschlagen.", g.Kto);
                Ergebnis(g, LieferverzugGrund.Fehler, $"Fehler beim Versand: {ex.Message}");
                await run.LogWarningAsync($"Kreditor-Mail an {g.Name} fehlgeschlagen: {ex.Message}", g.Kto, ct);
            }
        }

        if (e.EinkaufAktiv && e.EinkaufEmpfaenger.Count > 0 && !dryRun)
        {
            var inhalt = LieferverzugMailHtml.Einkauf(plan, e.Standort, stand, e.AppBaseUrl, heute, e.TageNachTermin);
            try
            {
                if (await _mail.SendMitOptionenAsync(new MailNachricht(inhalt.Betreff, inhalt.Html, inhalt.Text, e.EinkaufEmpfaenger, Bilder: bilder), ct))
                {
                    await ProtokollAsync(PurchaseOrderReminderArt.Einkauf, plan.Gruppen.SelectMany(g => g.Positionen).ToList(), e.EinkaufEmpfaenger, false, null, jetzt, ct);
                    einkaufGesendet = 1;
                }
                else
                {
                    nichtGesendet++;
                    await run.LogWarningAsync("Sammelliste an den Einkauf nicht gesendet: Mailversand übersprungen (MailSettings:SmtpHost?).", ct: ct);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                fehler++;
                _logger.LogError(ex, "Lieferverzug-Sammelliste an den Einkauf fehlgeschlagen.");
                await run.LogWarningAsync($"Sammelliste an den Einkauf fehlgeschlagen: {ex.Message}", ct: ct);
            }
        }

        int Anzahl(LieferverzugGrund gr) => plan.Gruppen.Count(g => g.Grund == gr);
        await run.FinishSuccessAsync(new Dictionary<string, int>
        {
            ["faellig"] = plan.AnzahlFaellig,
            ["kreditoren"] = plan.Gruppen.Count,
            ["einkauf_gesendet"] = einkaufGesendet,
            ["kreditor_gesendet"] = kreditorGesendet,
            ["testbetrieb_umgeleitet"] = dryRun ? Anzahl(LieferverzugGrund.Testbetrieb) : umgeleitet,
            ["kreditor_geplant"] = dryRun ? Anzahl(LieferverzugGrund.Gesendet) : 0,
            ["ohne_adresse"] = Anzahl(LieferverzugGrund.OhneAdresse),
            ["abgeschaltet"] = Anzahl(LieferverzugGrund.Abgeschaltet),
            ["gesperrt"] = Anzahl(LieferverzugGrund.Gesperrt),
            ["kein_kreditor"] = Anzahl(LieferverzugGrund.KeinKreditor),
            ["inaktiv"] = Anzahl(LieferverzugGrund.Inaktiv),
            ["nicht_dran"] = Anzahl(LieferverzugGrund.NichtDran),
            ["ausgesetzt"] = Anzahl(LieferverzugGrund.StandAlt) + Anzahl(LieferverzugGrund.Grenze),
            ["nicht_gesendet"] = nichtGesendet,
            ["fehler"] = fehler,
        }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
    }

    private static void Ergebnis(LieferverzugGruppe g, LieferverzugGrund grund, string status)
    {
        g.Grund = grund;
        g.Status = status;
    }

    /// <summary>F7: eine Zeile je Mail und Position, sofort gespeichert – ein spaeterer Fehler im Lauf darf eine gesendete Mail nicht vergessen.</summary>
    private async Task ProtokollAsync(PurchaseOrderReminderArt art, IReadOnlyList<LieferverzugPosition> positionen, IReadOnlyList<string> empfaenger,
        bool testbetrieb, int? creditorId, DateTime now, CancellationToken ct)
    {
        var an = string.Join(",", empfaenger);
        if (an.Length > 1000) an = an[..1000];
        _ctx.PurchaseOrderReminders.AddRange(positionen.Select(p => new PurchaseOrderReminder
        {
            Art = art, BelPosId = p.Line.BelPosId, Belegnummer = p.Line.Belegnummer, LieferantKto = p.Line.LieferantKto,
            CreditorId = creditorId ?? p.Line.CreditorId, Liefertermin = p.Line.Liefertermin, Rest = p.Rest, Empfaenger = an,
            Testbetrieb = testbetrieb, SentAt = now,
        }));
        await _ctx.SaveChangesAsync(ct);
    }

    /// <summary>Logo als EmbeddedResource (Branding/IDEAL_AKE_Logo_rgb_klein.jpg); fehlt es, geht die Mail ohne Bild (Alt-Text).</summary>
    private IReadOnlyList<MailBild>? LogoLaden()
    {
        using var s = typeof(LieferverzugMailService).Assembly.GetManifestResourceStream(LogoRessource);
        if (s == null)
        {
            _logger.LogWarning("Logo {Ressource} nicht eingebettet – Lieferverzug-Mails ohne Logo.", LogoRessource);
            return null;
        }
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return new[] { new MailBild(LieferverzugMailHtml.LogoCid, ms.ToArray(), "image/jpeg") };
    }
}
