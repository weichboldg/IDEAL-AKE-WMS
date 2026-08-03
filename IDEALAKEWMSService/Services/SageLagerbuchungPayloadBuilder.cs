using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Reine, DB-/HTTP-freie Uebersetzung einer WMS-<see cref="StockMovement"/> in den SData-Payload.
/// Einbuchung → "Zugang" (Ziel gesetzt, Herkunft leer), Ausbuchung → "Entnahme" (Herkunft gesetzt,
/// Ziel leer) — exakt spiegelbildlich zu den beiden Postman-Beispielen. Isoliert unit-testbar.
/// </summary>
public static class SageLagerbuchungPayloadBuilder
{
    public static SageLagerbuchungRequest Build(StockMovement movement)
    {
        if (!SageBookingEnqueueDecision.IsBookableType(movement.MovementType))
            throw new SageBookingPayloadException(
                $"Bewegungsart {movement.MovementType} ist keine ausgehende Sage-Buchung (nur Ein-/Ausbuchung).");

        var article = movement.Article
            ?? throw new SageBookingPayloadException("Artikel der Buchung ist nicht geladen.");
        var location = movement.StorageLocation
            ?? throw new SageBookingPayloadException("Lagerplatz der Buchung ist nicht geladen.");

        // AK9: ohne Sage-Referenzdaten keine gueltige Buchung (z.B. faelschlich auf manuellem Platz gesetzt).
        if (string.IsNullOrWhiteSpace(location.SageLagerkennung) || location.SageLagerplatzId is null)
            throw new SageBookingPayloadException(
                $"Lagerplatz '{location.Code}' hat keine Sage-Referenz (SageLagerkennung/SageLagerplatzId) — " +
                "keine Sage-Buchung moeglich.");

        var kennung = location.SageLagerkennung!;
        var platzId = location.SageLagerplatzId!.Value;
        var isZugang = movement.MovementType == MovementType.Einbuchung;

        var zeile = new SageLagerbuchungZeile
        {
            Lagerbewegungsart = isZugang ? "Zugang" : "Entnahme",
            Artikelnummer = article.ArticleNumber,
            AuspraegungHandle = 0,
            HerkunftLagerkennung = isZugang ? string.Empty : kennung,
            HerkunftLagerplatzId = isZugang ? 0 : platzId,
            ZielLagerkennung = isZugang ? kennung : string.Empty,
            ZielLagerplatzId = isZugang ? platzId : 0,
            MengeLager = movement.Quantity,
            // Step 1: Serien/Chargen leer mitschicken (Schema-Konformitaet ohne fachlichen Inhalt).
            Seriennummern = new List<SageSeriennummer> { new() },
            Chargen = new List<SageCharge> { new() }
        };

        return new SageLagerbuchungRequest
        {
            Memo = SageBookingCorrelation.Memo(movement.Id),
            Standardtext = isZugang ? "Zugang Material" : "Abgang / Entnahme Material",
            Lagerbuchungen = new List<SageLagerbuchungZeile> { zeile }
        };
    }
}
