namespace IdealAkeWms.Services;

/// <summary>
/// v1.99.35 (Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv): gemeinsame Keys und Regeln der BF-Auftraege fuer Web und Dienst
/// (Muster <see cref="WareneingangCsv"/>).
/// </summary>
public static class BfCsv
{
    /// <summary>ServiceSetting (Default aus): Dienst sendet die Bestell-Mail je abgeschicktem BF-Auftrag.</summary>
    public const string EmailEnabledKey = "Sync:BfAuftragEmailEnabled";

    /// <summary>Audit-Kennung des Mail-Schritts.</summary>
    public const string MailActor = "system:bf-mail";
}
