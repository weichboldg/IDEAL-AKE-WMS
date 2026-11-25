using System.Globalization;
using System.Text.Json;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data;

/// <summary>
/// F11/E9: Vormerken und Erledigen von Outbox-Eintraegen WMS → Sage. Beide Methoden speichern NICHT – der Aufrufer
/// schreibt sie mit seinem SaveChanges (Repository bzw. Lagerplatz-Abgleich). Es wird nichts an Sage gesendet.
/// </summary>
public static class SageStammdatenOutbox
{
    public const string TypLagerplatz = "StorageLocation";
    public const string TypBdeMitarbeiter = "BdeOperator";
    /// <summary>v1.99.71: im WMS erfasstes Lager (StorageArea), das der Dienst in Sage anlegt (Spec 2026-11-24-lagerplatz-anlage-wms-nach-sage F1a).</summary>
    public const string TypLagerort = "StorageArea";

    /// <summary>Neuen offenen Eintrag anlegen; Audit-Werte vom angelegten Datensatz (gleicher Benutzer, gleicher Zeitpunkt).</summary>
    public static void Vormerken(ApplicationDbContext ctx, string typ, AuditableEntity quelle, object payload) =>
        ctx.SageStammdatenOutbox.Add(new SageStammdatenOutboxItem
        {
            EntityType = typ,
            EntityKey = quelle.Id.ToString(CultureInfo.InvariantCulture),
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAt = quelle.CreatedAt,
            CreatedBy = quelle.CreatedBy,
            CreatedByWindows = quelle.CreatedByWindows,
        });

    /// <summary>Datensatz ist in Sage gefunden/verknuepft: offene, gesendete bzw. fehlerhafte Eintraege auf Bestaetigt setzen (nichts mehr zu
    /// senden). v1.99.71: auch <c>Gesendet</c> (Sende-Lauf der Lagerplatz-Anlage setzt vor dem Sage-Aufruf Gesendet, AK 38).</summary>
    public static async Task ErledigenAsync(ApplicationDbContext ctx, string typ, int id, string modifiedBy, string modifiedByWindows,
        CancellationToken ct = default)
    {
        var key = id.ToString(CultureInfo.InvariantCulture);
        var offen = await ctx.SageStammdatenOutbox
            .Where(o => o.EntityType == typ && o.EntityKey == key
                     && (o.Status == SageBookingQueueStatus.Offen || o.Status == SageBookingQueueStatus.Gesendet
                         || o.Status == SageBookingQueueStatus.Fehler))
            .ToListAsync(ct);
        foreach (var o in offen)
        {
            o.Status = SageBookingQueueStatus.Bestaetigt;
            o.ModifiedAt = DateTime.Now;
            o.ModifiedBy = modifiedBy;
            o.ModifiedByWindows = modifiedByWindows;
        }
    }
}
