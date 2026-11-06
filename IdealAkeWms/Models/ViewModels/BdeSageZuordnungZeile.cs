namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// Zeile der Sage-Zuordnung in den BDE-Stammdaten (Spec 2026-11-06-sage-sdata-stammdaten-sync, E6b/G14):
/// Sage-PPS-Mitarbeiter, der ihm zugeordnete BDE-Mitarbeiter (falls vorhanden) und der Vorschlag — nur bei exakter Gleichheit
/// <c>PersonnelNumber = Sage-Nummer</c> und nur unter noch nicht zugeordneten BDE-Mitarbeitern, nie ueber den Namen.
/// </summary>
public sealed record BdeSageZuordnungZeile(SagePpsMitarbeiter Sage, BdeOperator? Zugeordnet, BdeOperator? Vorschlag)
{
    public string Name => $"{Sage.Nachname} {Sage.Vorname}".Trim();
    public string AktivText => Sage.Aktiv switch { true => "Aktiv", false => "Inaktiv", null => "-" };
    public string StatusText => Zugeordnet is null ? "Nicht zugeordnet" : "Zugeordnet";
    public string ZugeordnetText => Zugeordnet is null ? "-" : $"{Zugeordnet.PersonnelNumber} {Zugeordnet.LastName} {Zugeordnet.FirstName}";
}
