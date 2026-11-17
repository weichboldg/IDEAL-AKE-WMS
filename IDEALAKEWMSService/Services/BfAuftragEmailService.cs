using System.Globalization;
using System.Net;
using System.Text;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// v1.99.35 (Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv F2, Freigabe-Antwort 13): genau eine Mail je abgeschicktem BF-Auftrag an die
/// aktiven Empfaenger seiner Gruppe (Muster <see cref="WarehouseRequisitionEmailService"/>). Keine Mail bei Freigabe/Fertig/Storno; ein vor dem
/// Versand stornierter Auftrag bekommt keine. Schalter <see cref="BfCsv.EmailEnabledKey"/> prueft der <c>SyncWorker</c>.
/// Kein Protokoll-Eintrag ohne Arbeit (ADR 0010).
/// </summary>
public class BfAuftragEmailService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IMailService _mail;
    private readonly IConfiguration _config;
    private readonly ILogger<BfAuftragEmailService> _logger;
    private readonly ISyncLogger _syncLogger;

    public BfAuftragEmailService(ApplicationDbContext ctx, IMailService mail, IConfiguration config, ILogger<BfAuftragEmailService> logger,
        ISyncLogger syncLogger)
    {
        _ctx = ctx; _mail = mail; _config = config; _logger = logger; _syncLogger = syncLogger;
    }

    public async Task<int> SendPendingEmailsAsync(bool dryRun, CancellationToken ct = default)
    {
        var offen = await _ctx.BfAuftraege.Include(a => a.Positionen).Include(a => a.OrderRecipientGroup!).ThenInclude(g => g.Recipients)
            .Where(a => a.EmailSentAt == null && a.Nummer != null && a.OrderRecipientGroupId != null
                        && a.Status != BfAuftragStatus.Entwurf && a.Status != BfAuftragStatus.Storniert)
            .OrderBy(a => a.Id).ToListAsync(ct);
        if (offen.Count == 0) return 0;

        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.BfAuftragEmail, ct);
        int gesendet = 0, fehler = 0;
        try
        {
            var baseUrl = await BaseUrlAsync(ct);
            foreach (var a in offen)
            {
                try
                {
                    var emails = a.OrderRecipientGroup!.Recipients.Where(r => r.IsActive).Select(r => r.Email).Distinct().ToList();
                    if (emails.Count == 0)
                    {
                        fehler++;
                        await run.LogWarningAsync($"{a.Nummer}: Empfängergruppe „{a.OrderRecipientGroup.Name}“ hat keine aktiven Empfänger – keine Mail.",
                            reference: a.Nummer, ct: ct);
                        continue;
                    }
                    if (!dryRun)
                    {
                        await _mail.SendAsync($"BF-Auftrag {a.Nummer} – {a.BestellerName}", Html(a, baseUrl), emails, Text(a, baseUrl), ct);
                        var jetzt = DateTime.Now;
                        a.EmailSentAt = jetzt;
                        a.ModifiedAt = jetzt; a.ModifiedBy = BfCsv.MailActor; a.ModifiedByWindows = Environment.UserName;
                        await _ctx.SaveChangesAsync(ct);
                    }
                    gesendet++;
                    await run.LogInfoAsync($"{a.Nummer}: Mail an {emails.Count} Empfänger.", reference: a.Nummer, ct: ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    fehler++;
                    _logger.LogError(ex, "BF-Auftrag {Nummer}: Mail fehlgeschlagen.", a.Nummer);
                    await run.LogWarningAsync($"{a.Nummer}: Mail fehlgeschlagen: {ex.Message}", reference: a.Nummer, ct: ct);
                }
            }
            await run.FinishSuccessAsync(new Dictionary<string, int> { ["gesendet"] = gesendet, ["fehler"] = fehler },
                messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
            return gesendet;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, counts: new Dictionary<string, int> { ["gesendet"] = gesendet, ["fehler"] = fehler }, ct: ct);
            throw;
        }
    }

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
    private static string M(decimal m) => m.ToString("0.###", CultureInfo.GetCultureInfo("de-AT"));
    private static string Link(BfAuftrag a, string baseUrl) => string.IsNullOrWhiteSpace(baseUrl) ? "" : $"{baseUrl.TrimEnd('/')}/BfAuftraege/Details/{a.Id}";

    internal static string Html(BfAuftrag a, string baseUrl)
    {
        var sb = new StringBuilder();
        sb.Append("<html><body style='font-family:Segoe UI, Arial; color:#000;'>");
        sb.Append($"<h2 style='color:#053153;'>BF-Auftrag {E(a.Nummer)}</h2>");
        sb.Append($"<p>Besteller: <b>{E(a.BestellerName)}</b>, bestellt am {a.BestelltAm:dd.MM.yyyy HH:mm}</p>");
        sb.Append("<table cellpadding='4' cellspacing='0' border='1' style='border-collapse:collapse;'>");
        sb.Append("<tr><th>Pos</th><th>Artikel</th><th>Matchcode</th><th>Teileart</th><th>Material</th><th>Menge</th><th>Status</th><th>Lagerplatz</th></tr>");
        foreach (var p in a.Positionen.OrderBy(p => p.Position))
            sb.Append($"<tr><td>{p.Position}</td><td>{E(p.ArticleNumber)}</td><td>{E(p.Matchcode)}</td><td>{E(p.Teileart)}</td>"
                      + $"<td>{E(p.Material)}{(string.IsNullOrWhiteSpace(p.Materialnummer) ? " (keine Materialnummer)" : "")}</td>"
                      + $"<td style='text-align:right;'>{M(p.Menge)}</td><td>{E(p.Status)}</td><td>{E(p.Hauptlagerplatz)}</td></tr>");
        sb.Append("</table>");
        var link = Link(a, baseUrl);
        if (link.Length > 0) sb.Append($"<p><a href='{E(link)}'>Im WMS öffnen</a></p>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    internal static string Text(BfAuftrag a, string baseUrl)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"BF-Auftrag {a.Nummer}");
        sb.AppendLine($"Besteller: {a.BestellerName}, bestellt am {a.BestelltAm:dd.MM.yyyy HH:mm}");
        foreach (var p in a.Positionen.OrderBy(p => p.Position))
            sb.AppendLine($"{p.Position}: {p.ArticleNumber} {p.Matchcode} | {p.Teileart} | {p.Material} | Menge {M(p.Menge)} | {p.Status} | {p.Hauptlagerplatz}");
        var link = Link(a, baseUrl);
        if (link.Length > 0) sb.AppendLine(link);
        return sb.ToString();
    }

    /// <summary>Wie <see cref="WarehouseRequisitionEmailService"/>: <c>Notifications:AppBaseUrl</c> aus den ServiceSettings (leer = ohne Link).</summary>
    private async Task<string> BaseUrlAsync(CancellationToken ct) =>
        await IDEALAKEWMSService.Common.ServiceSettings.GetValueSafeAsync(_config, "Notifications:AppBaseUrl", ct) ?? "";
}
