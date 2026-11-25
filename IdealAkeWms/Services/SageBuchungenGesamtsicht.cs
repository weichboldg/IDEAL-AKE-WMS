using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Services;

/// <summary>v1.99.66 (Spec 2026-11-23-sage-buchungen-gesamtsicht F2): gemeinsamer Anzeige-Status beider Wege. Die Namen binden alte Lesezeichen
/// (<c>?status=Fehler|Gesendet|Klaeren|Uebergeben</c>); <c>Offen</c>/<c>Bestaetigt</c> binden bewusst nicht (= „Alle“). Nur Anzeige, keine DB-Spalte.</summary>
public enum SageBuchungAnzeige { Wartet, Gesendet, Uebergeben, Fehler, Klaeren }

/// <summary>v1.99.66 (F3): Herkunft einer Zeile, Reihenfolge der Regel = Vorrang (Wareneingang, Lagerbestellung, Bewegungsart).</summary>
public enum SageBuchungHerkunft { Einbuchung, Ausbuchung, Umbuchung, Lagerbestellung, Wareneingang }

/// <summary>Eine Zeile der Seite „Sage-Buchungen“: genau eine der beiden Quellen ist gesetzt.</summary>
public sealed class SageBuchungZeile
{
    public SageBookingQueueItem? Queue { get; init; }
    public SageWareneingangZeile? We { get; init; }
    public required SageBuchungHerkunft Herkunft { get; init; }
    public required SageBuchungAnzeige Anzeige { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string ErfasstVon { get; init; }
    /// <summary>Queue-Id bzw. Receipt-Id (nur Sortierung; die Spalte „#“ zeigt nur die Queue-Id).</summary>
    public int Id => Queue?.Id ?? We!.Receipt.Id;
    /// <summary>F5: erste Zeile ihres Sage-WE-Vorgangs auf der angezeigten Seite - nur dort stehen die Knöpfe.</summary>
    public bool ZeigtWeAktionen { get; set; }
}

/// <summary>
/// v1.99.66 (Spec 2026-11-23-sage-buchungen-gesamtsicht): reine Funktionen der Seite „Sage-Buchungen“ (Lagerbuchungs-Queue und
/// Sage-Wareneingänge in einer Liste), DB-frei. EINE Stelle für Status-Zuordnung, Herkunft, „Erfasst von“, Zeitraum-Regel und Zelltexte
/// (View und Spaltenfilter lesen dieselben <see cref="Spalten"/>).
/// </summary>
public static class SageBuchungenGesamtsicht
{
    /// <summary>F8: ohne „von“ gilt heute − 30 Tage.</summary>
    public const int StandardTage = 30; // ponytail: feste 30 Tage, Einstellung erst bei Bedarf

    public static SageBuchungAnzeige Anzeige(SageBookingQueueStatus s) => s switch
    {
        SageBookingQueueStatus.Offen => SageBuchungAnzeige.Wartet,
        SageBookingQueueStatus.Gesendet => SageBuchungAnzeige.Gesendet,
        SageBookingQueueStatus.Bestaetigt => SageBuchungAnzeige.Uebergeben,
        SageBookingQueueStatus.Fehler => SageBuchungAnzeige.Fehler,
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Status ohne Zuordnung (Spec F2)"),
    };

    /// <summary>null = nicht anzeigen (NichtNoetig steht als Einbuchung in der Queue, NurInSage nirgends, Antwort 8).</summary>
    public static SageBuchungAnzeige? Anzeige(SageWareneingangStatus s) => s switch
    {
        SageWareneingangStatus.Offen => SageBuchungAnzeige.Wartet,
        SageWareneingangStatus.Gesendet => SageBuchungAnzeige.Gesendet,
        SageWareneingangStatus.Uebergeben => SageBuchungAnzeige.Uebergeben,
        SageWareneingangStatus.Fehler => SageBuchungAnzeige.Fehler,
        SageWareneingangStatus.Klaeren => SageBuchungAnzeige.Klaeren,
        SageWareneingangStatus.NichtNoetig or SageWareneingangStatus.NurInSage => null,
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Status ohne Zuordnung (Spec F2)"),
    };

    public static bool IstOffen(SageBuchungAnzeige a) => a != SageBuchungAnzeige.Uebergeben;

    public static string Text(SageBuchungAnzeige a) => a switch
    {
        SageBuchungAnzeige.Wartet => "wartet",
        SageBuchungAnzeige.Gesendet => "gesendet",
        SageBuchungAnzeige.Uebergeben => "übergeben",
        SageBuchungAnzeige.Fehler => "Fehler",
        _ => "klären",
    };

    /// <summary>Bootstrap-Badge; <c>text-bg-*</c> setzt die Schriftfarbe passend (Kontrast), der Text steht immer dabei.</summary>
    public static string Badge(SageBuchungAnzeige a) => a switch
    {
        SageBuchungAnzeige.Wartet => "text-bg-secondary",
        SageBuchungAnzeige.Gesendet => "text-bg-info",
        SageBuchungAnzeige.Uebergeben => "text-bg-success",
        SageBuchungAnzeige.Fehler => "text-bg-danger",
        _ => "text-bg-warning",
    };

    public static SageBuchungHerkunft Herkunft(StockMovement m) =>
        m.WarehouseRequisitionItemId != null ? SageBuchungHerkunft.Lagerbestellung
        : m.MovementType switch
        {
            MovementType.Ausbuchung or MovementType.SageAusbuchung => SageBuchungHerkunft.Ausbuchung,
            MovementType.Umbuchung => SageBuchungHerkunft.Umbuchung,
            _ => SageBuchungHerkunft.Einbuchung,
        };

    /// <summary>F6: wer gebucht hat; von der Nacherfassung eingereiht = Zusatz „(nacherfasst)“; ohne Akteur an der Bewegung der Service-Akteur.</summary>
    public static string ErfasstVon(SageBookingQueueItem q)
    {
        var akteur = q.StockMovement.CreatedBy;
        if (string.IsNullOrEmpty(akteur)) return q.CreatedBy;
        return q.CreatedBy == SageBookingQueueRepository.ServiceActor ? akteur + " (nacherfasst)" : akteur;
    }

    /// <summary>F8: <paramref name="bis"/> zählt den ganzen Tag. Dieselbe Regel steht als LINQ in den beiden Repository-Abfragen.</summary>
    public static bool ImZeitraum(DateTime erstellt, DateTime von, DateTime? bis)
        => erstellt >= von.Date && (bis == null || erstellt < bis.Value.Date.AddDays(1));

    public static List<SageBuchungZeile> Build(IEnumerable<SageBookingQueueItem> queue, IEnumerable<SageWareneingangZeile> we)
    {
        var q = queue.Select(i => new SageBuchungZeile
        {
            Queue = i, Herkunft = Herkunft(i.StockMovement), Anzeige = Anzeige(i.Status), CreatedAt = i.CreatedAt, ErfasstVon = ErfasstVon(i),
        });
        var w = we.Select(z => (z, a: Anzeige(z.Receipt.SageWeStatus))).Where(x => x.a != null).Select(x => new SageBuchungZeile
        {
            We = x.z, Herkunft = SageBuchungHerkunft.Wareneingang, Anzeige = x.a!.Value, CreatedAt = x.z.Receipt.CreatedAt,
            ErfasstVon = x.z.Receipt.CreatedBy,
        });
        return q.Concat(w).OrderByDescending(z => z.CreatedAt).ThenBy(z => z.Id).ToList();
    }

    /// <summary>F5: nach dem Paging - je Vorgang die erste Zeile der Seite trägt die Knöpfe (die Aktion gilt ohnehin für den ganzen Vorgang).</summary>
    public static void MarkiereErsteJeVorgang(IEnumerable<SageBuchungZeile> seite)
    {
        var gesehen = new HashSet<Guid>();
        foreach (var z in seite)
            z.ZeigtWeAktionen = z.We?.Receipt.SageWeVorgang is { } v && gesehen.Add(v);
    }

    private static string? Datum(DateTime? d) => d?.ToString("dd.MM.yyyy HH:mm");

    public static string HerkunftText(SageBuchungHerkunft h) => h.ToString();

    /// <summary>Zelltexte je Spalten-Key (F4) - die View rendert sie, der Server-Spaltenfilter filtert über sie (ADR 0005).</summary>
    public static readonly Dictionary<string, Func<SageBuchungZeile, string?>> Spalten = new()
    {
        ["id"] = z => z.Queue?.Id.ToString(),
        ["herkunft"] = z => HerkunftText(z.Herkunft),
        ["status"] = z => Text(z.Anzeige),
        ["status-weg"] = z => z.Queue != null ? SageBookingQueueController.StatusText(z.Queue.Status) : PurchaseOrdersController.SageWeStatusText(z.We!.Receipt.SageWeStatus),
        ["created-at"] = z => Datum(z.CreatedAt),
        ["bestellung"] = z => z.We?.Receipt.Belegnummer,
        ["pos"] = z => z.We?.Receipt.BelPosId.ToString(),
        ["article"] = z => (z.Queue?.StockMovement ?? z.We!.Receipt.StockMovement).Article.ArticleNumber,
        ["quantity"] = z => (z.Queue?.StockMovement.Quantity ?? z.We!.Receipt.Quantity).ToString("0.###"),
        ["storage-location"] = z => (z.Queue?.StockMovement ?? z.We!.Receipt.StockMovement).StorageLocation.Code,
        ["sagebeleg"] = z => z.We?.Receipt.SageWeBelegnummer,
        ["sent-at"] = z => Datum(z.Queue?.SentAt),
        ["confirmed-at"] = z => Datum(z.Queue != null ? z.Queue.ConfirmedAt : z.We!.Receipt.SageWeAm),
        ["attempts"] = z => (z.Queue?.AttemptCount ?? z.We!.Receipt.SageWeVersuche).ToString(),
        ["error"] = z => z.Queue != null ? z.Queue.LastError : z.We!.Receipt.SageWeFehler,
        ["erfasst-von"] = z => z.ErfasstVon,
        ["abteilung"] = z => z.Queue?.StockMovement.Abteilung, // v1.99.68: nur Lagerbuchung, WE leer
        ["lieferschein"] = z => z.We?.Receipt.Lieferschein,
        ["barcode"] = z => z.We?.Receipt.Barcode,
        ["memo-position"] = z => z.We?.Receipt.StockMovement.Note,
        ["memo"] = z => z.We?.Memo,
        ["csv"] = z => z.We?.Csv,
    };
}
