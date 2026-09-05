using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IProductionOrderBdeStatusRepository
{
    Task<ProductionOrderBdeStatus?> GetByProductionOrderIdAsync(int productionOrderId);
    Task SetIsDoneBdeAsync(int productionOrderId, bool value, string modifiedBy, string modifiedByWindows);

    /// <summary>
    /// IDEAL Teil 7/8 (Kaskade „Alle Sub-FAs fertigmelden"): setzt <c>IsDoneBde</c> fuer die BdeStatus-
    /// Zeilen ALLER <see cref="ProductionOrder"/> mit dieser HauptFA-<c>OrderNumber</c> (= alle
    /// materialisierten Nachfahren-Sub-FAs) in EINER Transaktion (atomar, AK C). Alle betroffenen Zeilen
    /// erhalten denselben <c>ModifiedAt</c>-Zeitstempel → im Audit als EINE Kaskade erkennbar (AK D).
    /// Setzt nur den uebergebenen Wert; die Gruppen-Aktion ruft ausschliesslich mit <c>true</c> auf
    /// (keine Gruppen-Ruecknahme). Gibt die Zahl der gesetzten Zeilen zurueck.
    /// </summary>
    Task<int> SetIsDoneBdeForOrderNumberAsync(string orderNumber, bool value, string modifiedBy, string modifiedByWindows);
}
