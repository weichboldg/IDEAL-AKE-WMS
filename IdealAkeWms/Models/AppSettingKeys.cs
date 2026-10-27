namespace IdealAkeWms.Models;

/// <summary>
/// Typed access to well-known AppSettings keys. Prevents silent typo-fallbacks.
/// </summary>
public static class AppSettingKeys
{
    // BDE
    public const string BdeAktiv = "BdeAktiv";
    public const string BdeNurFaMeldung = "BdeNurFaMeldung";
    public const string BdeDefaultArbeitsgang = "BdeDefaultArbeitsgang";
    public const string BdeMehrfachBuchungProOperator = "BdeMehrfachBuchungProOperator";
    public const string BdeMehrfachBuchungProArbeitsgang = "BdeMehrfachBuchungProArbeitsgang";
    public const string BdeGleichzeitigerAbschlussBeiMehrfachStart = "BdeGleichzeitigerAbschlussBeiMehrfachStart";
    public const string BdeSchichtkalenderAktiv = "BdeSchichtkalenderAktiv";
    public const string BdeTerminalMaxHauptFa = "BdeTerminalMaxHauptFa";

    // Picking / Leitstand / Warehouse Requisitions
    public const string LeitstandAktiv = "LeitstandAktiv";
    public const string KommissionierungMitZuweisung = "KommissionierungMitZuweisung";
    public const string BestellungenAktiv = "BestellungenAktiv";
    public const string LagerbestellungAktiv = "LagerbestellungAktiv";
    public const string DefaultLagerbestellempfaengerId = "DefaultLagerbestellempfaengerId";
    public const string DefaultGlasbestellempfaengerId = "DefaultGlasbestellempfaengerId";
    public const string GlasArtikelgruppen = "GlasArtikelgruppen";
    public const string GemeinsameArtikelgruppen = "GemeinsameArtikelgruppen";
    public const string FaCompletionAktiv = "FaCompletionAktiv";

    // Stock / Buchung
    public const string NegativeBuchungErlaubt = "NegativeBuchungErlaubt";
    public const string NegativeBuchungLagerplatz = "NegativeBuchungLagerplatz";
    public const string QrMitFaNummer = "QrMitFaNummer";
    /// <summary>v1.68.0: erstes QR-Feld ist "Artikelnummer" (Standard) oder "Matchcode" (Uebersetzung per /api/articles/by-matchcode).</summary>
    public const string QrErstesFeld = "QrErstesFeld";
    /// <summary>v1.69.0: Trennzeichen der QR-Felder, ";" (Standard) oder ",". Genau ein Zeichen je Installation.</summary>
    public const string QrTrennzeichen = "QrTrennzeichen";

    /// <summary>v1.69.0: Einstellungen mit festen Werten (Wert, Anzeige). /Settings zeigt dafuer ein Dropdown,
    /// SettingsController.SaveSettings speichert nur diese Werte (kanonische Schreibweise).</summary>
    public static readonly IReadOnlyDictionary<string, (string Wert, string Text)[]> FesteWerte =
        new Dictionary<string, (string Wert, string Text)[]>(StringComparer.OrdinalIgnoreCase)
        {
            [QrErstesFeld] = new[] { ("Artikelnummer", "Artikelnummer"), ("Matchcode", "Matchcode") },
            [QrTrennzeichen] = new[] { (";", "; (Semikolon)"), (",", ", (Komma)") },
        };

    // Tracking / Beschichtung
    public const string TeileverfolgungAktiv = "TeileverfolgungAktiv";
    public const string LackierteilKategorieName = "LackierteilKategorieName";
    public const string BeschichtungAbholtage = "BeschichtungAbholtage";

    // OSEON / Reporting
    public const string OseonReportingHorizonDays = "OseonReportingHorizonDays";
    public const string OseonReportingOverdueLookbackDays = "OseonReportingOverdueLookbackDays";

    // Windows Auth / AD
    public const string WindowsAuthAktiv = "WindowsAuthAktiv";
    public const string WindowsAuthBerechtigungsgruppe = "WindowsAuthBerechtigungsgruppe";

    // IDEAL FA-Hierarchie (Teil 2 — Baumanzeige)
    // Tiefen-Cap (Reissleine) fuer den rekursiven Aufbau je Struktur im FaHierarchyTreeBuilder.
    // Reine Web-Anzeige-Einstellung (ADR 0011). Ohne DB-Zeile defaultet der Code auf 500 (kein Seed).
    public const string FaHierarchyMaxDepth = "FaHierarchyMaxTiefe";

    // IDEAL FA-Hierarchie (Teil 3 — Kommissionierlisten)
    // Feature-Toggle fuer das Kommissionierlisten-Modul. INVERTIERTE Default-aus-Semantik
    // (im Unterschied zu LagerbestellungAktiv, dessen Default-ein aus Bestandsschutz kommt):
    // fehlend/nicht "true" => inaktiv. Kein Seed; RequireFaHierarchyKommissionierlistenAktivAttribute.
    public const string FaHierarchyKommissionierlistenAktiv = "FaHierarchyKommissionierlistenAktiv";

    // IDEAL FA-Hierarchie (Teil 4 — Beschichtungsauftrag)
    // Feature-Toggle fuer das Beschichtungsauftrag-Modul. INVERTIERTE Default-aus-Semantik
    // (wie FaHierarchyKommissionierlistenAktiv, im Unterschied zu LagerbestellungAktiv):
    // fehlend/nicht "true" => inaktiv. Kein Seed; RequireFaHierarchyBeschichtungAktivAttribute.
    public const string FaHierarchyBeschichtungAktiv = "FaHierarchyBeschichtungAktiv";

    // IDEAL FA-Hierarchie (Teil 5 — Vormontage-Listen)
    // Feature-Toggle fuer das Vormontage-Modul (zwei Sichten: Einzelne Teile + Summiert).
    // INVERTIERTE Default-aus-Semantik (wie FaHierarchyKommissionierlistenAktiv/Beschichtung,
    // im Unterschied zu LagerbestellungAktiv): fehlend/nicht "true" => inaktiv. Kein Seed;
    // RequireFaHierarchyVormontageAktivAttribute.
    public const string FaHierarchyVormontageAktiv = "FaHierarchyVormontageAktiv";

    // IDEAL FA-Hierarchie (Teil 6 — Standorteinstellungen-Maske): Firmendaten fuer Druckkopf/Anzeige.
    // Reine AppSettings-Werte OHNE Katalog/Seed: Teil 6 rendert sie kuratiert (eigene Feldliste) und
    // legt die DB-Zeile beim ersten Speichern an. Definitionsort ist HIER (Fallback-Regel: Teil 3 hat
    // die Keys bis zum Teil-6-Dev-Lauf nicht angelegt). Kein Seed, keine Migration.
    public const string Firmenname = "Firmenname";
    public const string Firmenanschrift = "Firmenanschrift";

    // Beschichtungsauftrag-Ausbau Teil 2 (v1.49.0): voller Pfad zur briefkopf.html der IT im Ordner Branding\
    // ausserhalb des Publish-Ordners (BriefkopfVorlageService). Kein Seed, keine Migration — die Zeile entsteht beim
    // ersten Speichern der Standorteinstellungen (nur Admin). Leer/fehlt => Ersatz-Kopf aus Firmenname + Anschrift.
    public const string BriefkopfVorlagePfad = "BriefkopfVorlagePfad";

    // Hardware-Barcodescanner (v1.66.0): Standort-Hauptschalter (nur exakt "true" aktiviert, Default aus), Praefix der Scanner-
    // Ausgabe (leer => "~09") und groesster Zeichenabstand in ms (leer/ungueltig => 50). Kein Seed, keine Migration — die Zeilen
    // entstehen beim ersten Speichern der Standorteinstellungen. Gelesen nur im _Layout (body-data-Attribute fuer hardware-scanner.js).
    public const string HardwareScannerAktiv = "HardwareScannerAktiv";
    public const string HardwareScannerPraefix = "HardwareScannerPraefix";
    public const string HardwareScannerMaxZeichenabstandMs = "HardwareScannerMaxZeichenabstandMs";

    // Vault- und enaio-Links (v1.67.0): Schalter mit DEFAULT-AN-Lesart wie LagerbestellungAktiv (nur "false" sperrt) und Link-
    // Vorlagen mit Platzhalter ({Artikelnummer} / {ObjektId}, leer = keine Links). Laufzeit-SEED in Program.cs mit den AKE-Werten —
    // Pflicht, weil die Standorteinstellungen ein Bool ohne Zeile als "aus" zeigen und beim naechsten Speichern "false" schreiben.
    // Regel an EINEM Ort: Helpers/LinkVorlage.Build (gelesen in _VaultLink und _EnaioDmsBadges).
    public const string VaultLinksAktiv = "VaultLinksAktiv";
    public const string VaultLinkVorlage = "VaultLinkVorlage";
    public const string EnaioLinksAktiv = "EnaioLinksAktiv";
    public const string EnaioLinkVorlage = "EnaioLinkVorlage";
}
