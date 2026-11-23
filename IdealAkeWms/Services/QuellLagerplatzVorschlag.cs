using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Services;

/// <summary>EINE Vorschlagsregel fuer den Quell-/Entnahme-Lagerplatz: Kommissionierung (Auftragsseite und Auftragsdruck, v1.99.13) und
/// Lager-/Glasbestellung (Entnahme beim Abschliessen, v1.99.53). Inhalt unveraendert aus <c>PickingController.QuellVorschlag</c> verschoben.</summary>
public static class QuellLagerplatzVorschlag
{
    /// <summary>Hauptlagerplatz mit buchbarem Bestand > 0, sonst buchbarer Platz mit der hoechsten Menge, sonst NAN. Nur buchbare Plaetze — Sage-synchronisierte sind standardmaessig
    /// IstBuchbar=false und fehlen im Dropdown; als Vorschlag blieb das Select sonst leer („--“).</summary>
    public static int? Waehle(IEnumerable<StockLocationInfo> locations, ISet<int> buchbarIds, int? nanId)
    {
        var buchbar = locations.Where(sl => sl.Quantity > 0 && buchbarIds.Contains(sl.StorageLocationId)).ToList();
        return (buchbar.FirstOrDefault(sl => sl.IsPrimaryStorageLocation)
                ?? buchbar.OrderByDescending(sl => sl.Quantity).FirstOrDefault())?.StorageLocationId
            ?? nanId;
    }
}
