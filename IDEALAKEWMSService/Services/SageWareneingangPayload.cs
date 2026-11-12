using System.Text.Json.Serialization;
using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IDEALAKEWMSService.Services;

// v1.99.4 (Spec 2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe F2/E4): Request an $service/StandardBelegService (Vertrag
// CommonWawiServices, Brain sage-sdata-commonwawiservices "StandardBelegService"). Belegübernahme Bestellung (EBB) -> Wareneingang (ELL).
// LESART DER SCHEMA-DOKU: welche Positions-Handles bei einer Übernahme zu belegen sind und ob Kurzbezeichnung die Lagerkennung oder die
// Platz-Kurzbezeichnung erwartet, ist unbelegt (Spec "Prüfschritte", Schreibtest). Der Schreibtest legt das fest; Felder bewusst minimal.

public sealed class SageWareneingangRequest
{
    public int Erfassungsart { get; init; } = 4100;                // ExtErfassungsart Einkauf
    public int BelegTransformArt { get; init; } = 1;                // Transformart Beleguebernahme
    public bool IsBelegTransform { get; init; } = true;
    public string BelegKennzeichen { get; init; } = SageWareneingang.BelegKennzeichen;
    public List<SageWeTransformSetting> TransformSettings { get; init; } = new();
    public string Belegdatum { get; init; } = string.Empty;         // yyyy-MM-dd; Belegjahr bewusst NICHT gesendet (optional; Sage bestimmt es, Geschaeftsjahr != Kalenderjahr moeglich)
    public string A0Konto { get; init; } = string.Empty;            // Lieferant der Bestellung
    public bool InteressentAdresse { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EKLieferschein { get; init; }                    // F6: leer = Feld nicht senden
    public string Memo { get; init; } = string.Empty;               // Korrelation WE#<id>#
    public bool MemoSpecified { get; init; } = true;
    public List<SageWeBelegPosition> StandardBelegPositionen { get; init; } = new();
}

public sealed class SageWeTransformSetting
{
    public int SourceBelegHandle { get; init; }                     // BelId der Bestellung
}

public sealed class SageWeBelegPosition
{
    public int PositionHandle { get; init; }                        // BelPosId der Bestellposition (Lesart, Schreibtest)
    public int Positionstyp { get; init; } = 1;                     // ExtPositionstyp Artikel
    public string Artikelnummer { get; init; } = string.Empty;
    public int AuspraegungsHandle { get; init; }
    public int Mengenart { get; init; } = 1;                        // ExtArtikelMengenart Basis (E0 B-2)
    public decimal MengeBasis { get; init; }
    /// <summary>v1.99.5 (Freigabe-Nachtrag 2026-11-12): Memo je Position - WE#&lt;Receipt-Id&gt;# [LS &lt;Lieferschein&gt;] Platz &lt;Lagerkennung&gt;.</summary>
    public string Artikelmemo { get; init; } = string.Empty;
    public List<SageWeLagermenge> BelegPositionLagermengen { get; init; } = new();
}

public sealed class SageWeLagermenge
{
    public int PlatzId { get; init; }
    public string Kurzbezeichnung { get; init; } = string.Empty;
    public decimal MengeBasis { get; init; }
}

public sealed class SageWareneingangPayloadException(string message) : Exception(message);

/// <summary>Rein und testbar: EINE Gruppe (ein Absenden = ein SageWeVorgang) -> EIN Beleg. Receipts mit StockMovement.Article und
/// StockMovement.StorageLocation geladen.</summary>
public static class SageWareneingangPayloadBuilder
{
    /// <summary>v1.99.5: ordnet jede Sage-Position genau einem WMS-Wareneingang zu (Muster Kopf-Memo WE#&lt;id&gt;#).</summary>
    public static string PositionsMemo(int receiptId, string? lieferschein, string lagerkennung) =>
        $"{SageWareneingang.Memo(receiptId)}{(string.IsNullOrWhiteSpace(lieferschein) ? "" : $" LS {lieferschein.Trim()}")} Platz {lagerkennung}";

    public static SageWareneingangRequest Build(IReadOnlyList<PurchaseOrderReceipt> gruppe)
    {
        if (gruppe.Count == 0) throw new SageWareneingangPayloadException("Leere Beleg-Gruppe.");
        var belIds = gruppe.Select(r => r.BelId).Distinct().ToList();
        if (belIds.Count != 1) throw new SageWareneingangPayloadException($"Beleg-Gruppe umfasst {belIds.Count} Bestellungen statt einer.");
        var erste = gruppe.MinBy(r => r.Id)!;
        if (string.IsNullOrWhiteSpace(erste.LieferantKto)) throw new SageWareneingangPayloadException($"Bestellung {erste.Belegnummer}: Lieferant (A0Konto) fehlt.");
        var positionen = new List<SageWeBelegPosition>();
        foreach (var r in gruppe.OrderBy(r => r.Id))
        {
            var platz = r.StockMovement?.StorageLocation;
            if (!SageWareneingang.IstSageWePlatz(platz))
                throw new SageWareneingangPayloadException(SageWareneingang.PlatzFehler(platz?.Code ?? $"#{r.StockMovement?.StorageLocationId}"));
            var artikel = r.StockMovement!.Article?.ArticleNumber;
            if (string.IsNullOrWhiteSpace(artikel)) throw new SageWareneingangPayloadException($"Wareneingang #{r.Id}: Artikelnummer fehlt.");
            positionen.Add(new SageWeBelegPosition
            {
                PositionHandle = r.BelPosId, Artikelnummer = artikel.Trim(), MengeBasis = r.Quantity,
                Artikelmemo = PositionsMemo(r.Id, r.Lieferschein, platz!.SageLagerkennung!),
                BelegPositionLagermengen = { new SageWeLagermenge { PlatzId = platz!.SageLagerplatzId!.Value, Kurzbezeichnung = platz.SageLagerkennung!, MengeBasis = r.Quantity } },
            });
        }
        var lieferschein = gruppe.Select(r => r.Lieferschein).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();
        return new SageWareneingangRequest
        {
            TransformSettings = { new SageWeTransformSetting { SourceBelegHandle = belIds[0] } },
            Belegdatum = erste.CreatedAt.ToString("yyyy-MM-dd"),
            A0Konto = erste.LieferantKto!.Trim(),
            EKLieferschein = lieferschein,
            Memo = SageWareneingang.Memo(erste.Id),
            StandardBelegPositionen = positionen,
        };
    }
}
