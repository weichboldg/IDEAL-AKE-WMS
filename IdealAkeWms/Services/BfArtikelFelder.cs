namespace IdealAkeWms.Services;

/// <summary>
/// Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv: Sage-Artikelfelder der Blechteile (Lagerteile BG/FT). Der Dienst liest sie
/// im View-Modus per SData nach (V-B, Schalter <see cref="NachlesenKey"/>) und schreibt sie als Text-Merkmale (Definition ueber
/// <c>SyncSource = "Sage"</c> + <c>SyncFieldName</c>); der Warenkorb liest dieselben Merkmale. Eine Stelle fuer die Feldnamen (Web + Dienst).
/// </summary>
public static class BfArtikelFelder
{
    /// <summary>ServiceSetting (Default aus): Artikel-Abgleich liest im View-Modus die Blechteile-Felder per SData nach (nur GET).</summary>
    public const string NachlesenKey = "Sync:BlechteileFelderNachlesen";

    public const string IstLagerartikel = "USER_OSIstLagerartikel";
    public const string Artikeltyp = "USER_OSArtikeltyp";
    public const string Material = "USER_OSMaterial";
    public const string Materialnummer = "USER_OSMaterialnummerPN";
    public const string FertigungsInfoPN = "USER_OSFertigungsInfoPN";
    public const string Arbeitsschritte = "USER_OSArbeitsschritte";
    public const string Abteilung = "USER_OSAbteilung";
    /// <summary>v1.99.72 (Spec 2026-11-20-bde-liste-isolierungsfraese-pfad-material, Antwort 1): Blechdicke fuer die BDE-Teileliste.</summary>
    public const string Blechdicke = "USER_OSBlechdicke";

    /// <summary>Sage-Bestellmenge (Grundvariante) als Merkmal; kein USER-Feld, sondern <c>ArtikelVariantenExtended.Bestellmenge</c>.</summary>
    public const string Bestellmenge = "Bestellmenge";

    /// <summary>USER-Felder am Sage-Artikel (Namen belegt L10, sage-sdata-benutzerfelder-lesetest). Arbeitsschritte/Abteilung fuer spaetere BDE.</summary>
    public static readonly string[] SageFelder =
        { IstLagerartikel, Artikeltyp, Material, Materialnummer, FertigungsInfoPN, Arbeitsschritte, Abteilung, Blechdicke };

    /// <summary>Bestellbare Teilearten (Backlog Antwort 3: ZK nie).</summary>
    public static readonly string[] Teilearten = { "BG", "FT" };
}
