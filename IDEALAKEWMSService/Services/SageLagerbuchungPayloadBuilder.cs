using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Reine, DB-/HTTP-freie Uebersetzung einer WMS-<see cref="StockMovement"/> in den SData-Payload.
/// Einbuchung → "Zugang" (Ziel gesetzt, Herkunft leer), Ausbuchung → "Entnahme" (Herkunft gesetzt,
/// Ziel leer) — exakt spiegelbildlich zu den beiden Postman-Beispielen. Umbuchung (v1.73.0 E7, Spec F9; nur
/// eingereiht, wenn <c>SageLagerbuchungUmbuchungAktiv</c> an ist): Art nach den Platz-Flags aus
/// <see cref="SageBookingEnqueueDecision.Buchungsart"/> — Sage → Sage "Umbuchung" (Herkunft + Ziel, am AKE-Test
/// belegt: Bewegungsart IU), Sage → WMS-only "Entnahme" an der Herkunft, WMS-only → Sage "Zugang" am Ziel.
/// Isoliert unit-testbar.
/// </summary>
public static class SageLagerbuchungPayloadBuilder
{
    public static SageLagerbuchungRequest Build(StockMovement movement)
    {
        // Typfilter ohne Schalter: was in der Queue steht, wurde nach der Regel eingereiht (Schalter wirkt beim Einreihen).
        if (!SageBookingEnqueueDecision.IsBookableType(movement.MovementType, umbuchungAktiv: true))
            throw new SageBookingPayloadException(
                $"Bewegungsart {movement.MovementType} ist keine ausgehende Sage-Buchung (nur Ein-/Aus-/Umbuchung).");

        var article = movement.Article
            ?? throw new SageBookingPayloadException("Artikel der Buchung ist nicht geladen.");
        var location = movement.StorageLocation
            ?? throw new SageBookingPayloadException("Lagerplatz der Buchung ist nicht geladen.");

        StorageLocation? herkunft, ziel;
        string art, text;
        if (movement.MovementType == MovementType.Umbuchung)
        {
            if (movement.SourceStorageLocationId is not null && movement.SourceStorageLocation is null)
                throw new SageBookingPayloadException("Herkunfts-Lagerplatz der Umbuchung ist nicht geladen.");
            var source = movement.SourceStorageLocation;
            switch (SageBookingEnqueueDecision.Buchungsart(MovementType.Umbuchung, location.SageBuchungErlaubt, source?.SageBuchungErlaubt ?? false))
            {
                case SageBuchungsart.Umbuchung: (herkunft, ziel, art, text) = (source, location, "Umbuchung", "Umbuchung Material"); break;
                case SageBuchungsart.Entnahme: (herkunft, ziel, art, text) = (source, null, "Entnahme", "Abgang / Entnahme Material"); break;
                case SageBuchungsart.Zugang: (herkunft, ziel, art, text) = (null, location, "Zugang", "Zugang Material"); break;
                default:
                    throw new SageBookingPayloadException(
                        $"Umbuchung zwischen zwei Plätzen ohne 'Sage-Buchung erlaubt' ('{source?.Code}' → '{location.Code}') — keine Sage-Buchung.");
            }
        }
        else
        {
            var isZugang = movement.MovementType == MovementType.Einbuchung;
            (herkunft, ziel) = isZugang ? (null, location) : (location, (StorageLocation?)null);
            (art, text) = isZugang ? ("Zugang", "Zugang Material") : ("Entnahme", "Abgang / Entnahme Material");
        }

        var zeile = new SageLagerbuchungZeile
        {
            Lagerbewegungsart = art,
            Artikelnummer = article.ArticleNumber,
            AuspraegungHandle = 0,
            HerkunftLagerkennung = herkunft is null ? string.Empty : Kennung(herkunft),
            HerkunftLagerplatzId = herkunft is null ? 0 : PlatzId(herkunft),
            ZielLagerkennung = ziel is null ? string.Empty : Kennung(ziel),
            ZielLagerplatzId = ziel is null ? 0 : PlatzId(ziel),
            MengeLager = movement.Quantity,
            // Step 1: Serien/Chargen leer mitschicken (Schema-Konformitaet ohne fachlichen Inhalt).
            Seriennummern = new List<SageSeriennummer> { new() },
            Chargen = new List<SageCharge> { new() }
        };

        return new SageLagerbuchungRequest
        {
            Memo = SageBookingCorrelation.Memo(movement.Id),
            Standardtext = text,
            Lagerbuchungen = new List<SageLagerbuchungZeile> { zeile }
        };
    }

    // AK9: ohne Sage-Referenzdaten keine gueltige Buchung (z.B. faelschlich auf manuellem Platz gesetzt).
    private static void PruefeReferenz(StorageLocation l)
    {
        if (string.IsNullOrWhiteSpace(l.SageLagerkennung) || l.SageLagerplatzId is null)
            throw new SageBookingPayloadException(
                $"Lagerplatz '{l.Code}' hat keine Sage-Referenz (SageLagerkennung/SageLagerplatzId) — " +
                "keine Sage-Buchung moeglich.");
    }

    private static string Kennung(StorageLocation l) { PruefeReferenz(l); return l.SageLagerkennung!; }

    private static int PlatzId(StorageLocation l) { PruefeReferenz(l); return l.SageLagerplatzId!.Value; }
}
