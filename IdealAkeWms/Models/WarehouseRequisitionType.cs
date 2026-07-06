namespace IdealAkeWms.Models;

/// <summary>
/// Bestelltyp einer Lagerbestellung: normale Lager-Bestellung oder Glas-Bestellung.
/// Steuert Empfaenger-Gruppe (Mail) und erlaubte Artikelgruppen (v1.25.0).
/// </summary>
public enum WarehouseRequisitionType
{
    Lager = 1,
    Glas = 2
}
