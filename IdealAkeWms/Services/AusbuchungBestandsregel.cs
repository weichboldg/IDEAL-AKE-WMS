using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

/// <summary>EINE Bestandsregel fuer Ausbuchungen (Warenausgang und Lieferung einer Lager-/Glasbestellung, v1.99.53): reicht der Bestand am
/// Platz nicht, wird ohne <see cref="AppSettingKeys.NegativeBuchungErlaubt"/> abgewiesen; mit dem Schalter wird vom Ausweichplatz
/// (<see cref="AppSettingKeys.NegativeBuchungLagerplatz"/>, Default NAN) gebucht. Inhalt unveraendert aus <c>StockMovementsController.Outbound</c>.</summary>
public static class AusbuchungBestandsregel
{
    /// <param name="Abgewiesen">Bestand reicht nicht und Minus-Buchungen sind aus.</param>
    /// <param name="StorageLocationId">Platz, von dem gebucht wird (gewaehlter oder Ausweichplatz).</param>
    /// <param name="Verfuegbar">Bestand am gewaehlten Platz.</param>
    /// <param name="AusweichCode">Gesetzt, wenn vom Ausweichplatz gebucht wird.</param>
    public sealed record Ergebnis(bool Abgewiesen, int StorageLocationId, decimal Verfuegbar, string? AusweichCode)
    {
        public string? Warnung => AusweichCode == null ? null
            : $"Lagerstand nicht verfügbar (Bestand: {Verfuegbar:N3}), buche vom Lagerplatz {AusweichCode} ab.";
    }

    public static async Task<Ergebnis> PruefeAsync(IStockMovementRepository stock, IAppSettingRepository settings,
        IStorageLocationRepository locations, int articleId, int storageLocationId, decimal menge)
    {
        var verfuegbar = await stock.GetCurrentStockAtLocationAsync(articleId, storageLocationId);
        if (verfuegbar >= menge) return new(false, storageLocationId, verfuegbar, null);

        var negativErlaubt = (await settings.GetValueAsync(AppSettingKeys.NegativeBuchungErlaubt))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        if (!negativErlaubt) return new(true, storageLocationId, verfuegbar, null);

        var code = await settings.GetValueAsync(AppSettingKeys.NegativeBuchungLagerplatz) ?? "NAN";
        var ausweich = await locations.GetByCodeAsync(code);
        // Bestehendes Verhalten (Warenausgang): fehlt der Ausweichplatz, wird am gewaehlten Platz ins Minus gebucht.
        return ausweich == null ? new(false, storageLocationId, verfuegbar, null) : new(false, ausweich.Id, verfuegbar, code);
    }
}
