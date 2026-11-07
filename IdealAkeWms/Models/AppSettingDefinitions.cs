using System.Diagnostics.CodeAnalysis;

namespace IdealAkeWms.Models;

/// <summary>
/// Typisierter Katalog aller AppSettings (/Settings) — v1.77.0, Spec 2026-11-07-einstellungen-auswahlfelder-und-beschreibung (E1, ADR 0020).
/// Ein Eintrag je Key; <see cref="SettingDefinition.DefaultValue"/> = Wert des LESERS bei fehlender Zeile, nie ein neuer Default.
/// <see cref="SettingDefinition.Seed"/> = der Web-Start legt die Zeile an, wenn sie fehlt (dieselben 38 Keys und Werte wie die frueheren
/// Seed-Bloecke in Program.cs; FreshInstall-Keys und Keys ohne Seed bleiben ohne Laufzeit-Anlage — fuer manche wirkt "" anders als „Zeile
/// fehlt“, F4). Neue AppSettings kommen HIER hinein (Drift-Guard <c>AppSettingDefinitionsTests</c>).
/// </summary>
public static class AppSettingDefinitions
{
    private const string Beide = "beide";
    private const string GTermine = "Termine und Beschichtung";
    private const string GLager = "Lager und Buchungen";
    private const string GQr = "QR-Code und Scanner";
    private const string GBestellung = "Bestellungen";
    private const string GLeitstand = "Leitstand und FA-Vervollständigung";
    private const string GBde = "BDE";
    private const string GOseon = "Teileverfolgung und OSEON (AKE)";
    private const string GIdeal = "IDEAL FA-Hierarchie";
    private const string GSage = "Sage";
    private const string GAnmeldung = "Anmeldung";
    private const string GFirma = "Firma und Druck";
    private const string GLinks = "Links (Vault, enaio)";

    private static readonly (string, string)[] Wochentage =
    {
        ("Montag", "Montag"), ("Dienstag", "Dienstag"), ("Mittwoch", "Mittwoch"), ("Donnerstag", "Donnerstag"),
        ("Freitag", "Freitag"), ("Samstag", "Samstag"), ("Sonntag", "Sonntag"),
    };

    private static SettingDefinition B(string key, string def, string gruppe, string name, string bewirkt, string verwendung, string standort,
        bool seed, SettingBoolLesart lesart = SettingBoolLesart.NurTrueAktiviert, string? warn = null, string? abhaengig = null) =>
        new(key, SettingType.Bool, def, gruppe, bewirkt, Anzeigename: name, Verwendung: verwendung, Standort: standort, Warnhinweis: warn,
            AbhaengigVon: abhaengig, BoolLesart: lesart, Seed: seed);

    private static SettingDefinition I(string key, string def, string gruppe, string name, string bewirkt, string verwendung, string standort,
        bool seed, string einheit, int? min = null, int? max = null, bool leerErlaubt = false) =>
        new(key, SettingType.Int, def, gruppe, bewirkt, Anzeigename: name, Verwendung: verwendung, Standort: standort, Einheit: einheit,
            Min: min, Max: max, LeerErlaubt: leerErlaubt, Seed: seed);

    private static SettingDefinition S(string key, string def, string gruppe, string name, string bewirkt, string verwendung, string standort,
        bool seed, bool multiline = false, string? abhaengig = null, string? warn = null) =>
        new(key, SettingType.String, def, gruppe, bewirkt, multiline, Anzeigename: name, Verwendung: verwendung, Standort: standort,
            AbhaengigVon: abhaengig, Warnhinweis: warn, Seed: seed);

    private static SettingDefinition M(string key, string def, string gruppe, string name, string bewirkt, string verwendung, string standort,
        bool seed, SettingStammdaten quelle, bool leerErlaubt, string? abhaengig = null) =>
        new(key, SettingType.MasterData, def, gruppe, bewirkt, Anzeigename: name, Verwendung: verwendung, Standort: standort,
            Stammdaten: quelle, LeerErlaubt: leerErlaubt, AbhaengigVon: abhaengig, Seed: seed);

    public static IReadOnlyList<SettingDefinition> All { get; } = new List<SettingDefinition>
    {
        // ---- Termine und Beschichtung ----
        I("KommissionierTage", "4", GTermine, "Kommissionier-Vorlauf", "Arbeitstage vor dem FA-Termin, ab denen ein FA zum Kommissionieren fällig ist.",
            "FA-Liste, Kommissionierung, Leitstand, Werkbank-Arbeitsliste", Beide, seed: false, "Arbeitstage"),
        I("VorkommissionierTage", "1", GTermine, "Vorkommissionier-Vorlauf", "Arbeitstage vor dem Kommissionier-Termin für die Vorkommissionierung.",
            "FA-Liste, Leitstand, Werkbank-Arbeitsliste", Beide, seed: false, "Arbeitstage"),
        I("BeschichtungTage", "10", GTermine, "Beschichtungs-Vorlauf", "Arbeitstage vor dem FA-Termin, zu denen Lackierteile zur Beschichtung müssen.",
            "FA-Liste, Leitstand, Werkbank-Arbeitsliste", Beide, seed: false, "Arbeitstage"),
        new(AppSettingKeys.BeschichtungAbholtage, SettingType.MultiChoice, "Dienstag,Donnerstag", GTermine,
            "Wochentage, an denen der Beschichter abholt; der Beschichtungstermin fällt auf den letzten Abholtag davor. Leer = keine Abholtage (wirkt anders als „nicht gesetzt“ = Dienstag, Donnerstag).",
            Anzeigename: "Beschichtung – Abholtage", Verwendung: "FA-Liste, Leitstand, Werkbank-Arbeitsliste (Beschichtungstermin)", Standort: Beide,
            Werte: Wochentage, LeerErlaubt: true),
        M(AppSettingKeys.LackierteilKategorieName, "", GTermine, "Kategorie „Lackierteil“",
            "Artikelkategorie, deren Artikel als Lackierteil gelten (eigener Beschichtungstermin). Leer = Lackierteil-Erkennung aus.",
            "FA-Liste, Leitstand, Werkbank-Arbeitsliste; Dienst Lackierteil-Erkennung", Beide, seed: true, SettingStammdaten.Artikelkategorie, leerErlaubt: true),

        // ---- Lager und Buchungen ----
        I("WarningThresholdPercent", "150", GLager, "Bestand: Warnschwelle", "Bestand unter diesem Prozentsatz des Meldebestands wird gelb markiert.",
            "Bestandsübersicht", Beide, seed: false, "%"),
        I("CriticalThresholdPercent", "100", GLager, "Bestand: kritische Schwelle", "Bestand unter diesem Prozentsatz des Meldebestands wird rot markiert.",
            "Bestandsübersicht", Beide, seed: false, "%"),
        B(AppSettingKeys.NegativeBuchungErlaubt, "false", GLager, "Buchungen ins Minus erlauben",
            "Erlaubt Ausbuchungen über den Bestand hinaus; der Fehlbestand wird auf den Ausweich-Lagerplatz gebucht.",
            "Warenausgang, Umbuchung, Kommissionier-Umbuchung", Beide, seed: false, warn: "Erlaubt Buchungen ins Minus (Ausweich-Lagerplatz)."),
        M(AppSettingKeys.NegativeBuchungLagerplatz, "NAN", GLager, "Ausweich-Lagerplatz für Minus-Buchungen",
            "Lagerplatz-Code, auf den bei erlaubten Minus-Buchungen der Fehlbestand gebucht wird (nicht gesetzt = NAN).",
            "Warenausgang, Umbuchung, Kommissionier-Umbuchung", Beide, seed: false, SettingStammdaten.Lagerplatz, leerErlaubt: false,
            abhaengig: AppSettingKeys.NegativeBuchungErlaubt),
        B(AppSettingKeys.LagerplatzEtikettRolle93x55, "false", GLager, "Lagerplatz-Etikett 93 × 55 mm (Rolle)",
            "An = ein Etikett 93 × 55 mm je Seite ohne Rahmen; aus = A4 mit drei Etiketten je Seite.", "Lagerplätze → Etiketten drucken", Beide, seed: false),

        // ---- QR-Code und Scanner ----
        B(AppSettingKeys.QrMitFaNummer, "false", GQr, "QR-Code mit FA-Nummer",
            "Übernimmt die FA aus Feld 3 eines Artikel-QR-Codes in das FA-Feld.", "Wareneingang, Warenausgang, Umbuchung, Bestandsübersicht", Beide, seed: true),
        new(AppSettingKeys.QrErstesFeld, SettingType.Choice, "Artikelnummer", GQr, "Was im ersten Feld eines Artikel-QR-Codes steht.",
            Anzeigename: "QR-Code: erstes Feld", Verwendung: "alle Artikel-Scans (Kamera, Hardware-Scanner)", Standort: Beide,
            Werte: new[] { ("Artikelnummer", "Artikelnummer"), ("Matchcode", "Matchcode") }, Seed: true),
        new(AppSettingKeys.QrTrennzeichen, SettingType.Choice, ";", GQr, "Trennzeichen zwischen den Feldern eines Artikel-QR-Codes.",
            Anzeigename: "QR-Code: Trennzeichen", Verwendung: "alle Artikel-Scans (Kamera, Hardware-Scanner)", Standort: Beide,
            Werte: new[] { (";", "; (Semikolon)"), (",", ", (Komma)") }, Seed: true),
        B(AppSettingKeys.HardwareScannerAktiv, "false", GQr, "Hardware-Barcodescanner", "Schaltet die Erkennung von Hardware-Scanner-Eingaben ein.",
            "alle Seiten (Layout, hardware-scanner.js)", Beide, seed: false),
        S(AppSettingKeys.HardwareScannerPraefix, "", GQr, "Hardware-Scanner: Präfix", "Präfix, das der Scanner vor jede Ausgabe setzt. Leer = ~09.",
            "alle Seiten (Layout, hardware-scanner.js)", Beide, seed: false, abhaengig: AppSettingKeys.HardwareScannerAktiv),
        I(AppSettingKeys.HardwareScannerMaxZeichenabstandMs, "", GQr, "Hardware-Scanner: max. Zeichenabstand",
            "Größter Abstand zwischen zwei Zeichen, der noch als Scan gilt. Leer = 50 ms.", "alle Seiten (Layout, hardware-scanner.js)", Beide,
            seed: false, "ms", min: 10, max: 500, leerErlaubt: true),

        // ---- Bestellungen ----
        B(AppSettingKeys.BestellungenAktiv, "false", GBestellung, "Bedarfsmeldungen aus der Stückliste",
            "Blendet Bedarfsmeldungen (Bestellungen aus der Stückliste) ein.", "Menü, Kommissionierung, Wareneingang", Beide, seed: true),
        B(AppSettingKeys.LagerbestellungAktiv, "true", GBestellung, "Lagerbestellungen",
            "Lager- und Glasbestellungen, Meine Fehlteile, Lager-Arbeitslisten und Stücklisten-Knopf. Nur „false“ schaltet aus.",
            "Menü, Lagerbestellung, Fehlteile, Werkbank-Arbeitsliste, Kommissionierung", Beide, seed: true, SettingBoolLesart.NurFalseSperrt,
            warn: "Aus = Lager-/Glasbestellungen, Meine Fehlteile und Lager-Arbeitslisten sind gesperrt."),
        M(AppSettingKeys.DefaultLagerbestellempfaengerId, "", GBestellung, "Empfängergruppe Lagerbestellung",
            "Bestellempfänger-Gruppe, an die Lagerbestellungen gehen. Leer = Absenden wird blockiert.", "Lagerbestellung absenden", Beide, seed: true,
            SettingStammdaten.Bestellempfaengergruppe, leerErlaubt: true),
        M(AppSettingKeys.DefaultGlasbestellempfaengerId, "", GBestellung, "Empfängergruppe Glasbestellung",
            "Bestellempfänger-Gruppe, an die Glasbestellungen gehen. Leer = Absenden wird blockiert.", "Glasbestellung absenden", Beide, seed: true,
            SettingStammdaten.Bestellempfaengergruppe, leerErlaubt: true),
        S(AppSettingKeys.GlasArtikelgruppen, "", GBestellung, "Glas-Artikelgruppen",
            "Kommagetrennte Artikelgruppen der Glasbestellung; in der Lagerbestellung ausgenommen.", "Artikelsuche Lager-/Glasbestellung", Beide, seed: true),
        S(AppSettingKeys.GemeinsameArtikelgruppen, "EUZ", GBestellung, "Gemeinsame Artikelgruppen",
            "Kommagetrennte Artikelgruppen, die in Lager- und Glasbestellungen wählbar sind.", "Artikelsuche Lager-/Glasbestellung", Beide, seed: true),

        // ---- Leitstand und FA-Vervollständigung ----
        B(AppSettingKeys.LeitstandAktiv, "false", GLeitstand, "Leitstand", "Kommissionier-Freigabe und Priorisierung über den Leitstand.",
            "Menü, Startseite, Kommissionierung, Leitstand", Beide, seed: true),
        B(AppSettingKeys.KommissionierungMitZuweisung, "false", GLeitstand, "Kommissionierung mit Anwenderzuweisung",
            "Kommissionieraufträge werden Mitarbeitern zugewiesen.", "Kommissionierung, Leitstand", Beide, seed: true, abhaengig: AppSettingKeys.LeitstandAktiv),
        B(AppSettingKeys.FaCompletionAktiv, "false", GLeitstand, "FA-Vervollständigung", "Pflege fehlender FA-Daten.",
            "Menü, FA-Vervollständigung, Werkbank-Arbeitsliste", Beide, seed: true),

        // ---- BDE ----
        B(AppSettingKeys.BdeAktiv, "false", GBde, "BDE (Betriebsdatenerfassung)", "Schaltet das BDE-Modul ein.",
            "Menü, Startseite, BDE-Terminal, BDE-Stammdaten, FA-Struktur", Beide, seed: true),
        B(AppSettingKeys.BdeNurFaMeldung, "false", GBde, "BDE: nur FA-Meldung", "Vereinfachter Modus: gebucht wird auf den FA statt auf einzelne Arbeitsgänge.",
            "BDE-Terminal, BDE-Buchung, Werkbänke", Beide, seed: true, abhaengig: AppSettingKeys.BdeAktiv),
        S(AppSettingKeys.BdeDefaultArbeitsgang, "", GBde, "BDE: Default-Arbeitsgang",
            "Arbeitsgang, der bei „nur FA-Meldung“ ohne Werkbank-Default gebucht wird (z. B. PRODUKTION). Leer = solche Buchungen schlagen fehl.",
            "BDE-Buchung (nur FA-Meldung), Werkbänke", Beide, seed: true, abhaengig: AppSettingKeys.BdeNurFaMeldung),
        B(AppSettingKeys.BdeMehrfachBuchungProOperator, "false", GBde, "BDE: mehrere Buchungen je Mitarbeiter",
            "Ein Mitarbeiter darf mehrere parallele Buchungen auf verschiedenen Arbeitsgängen haben.", "BDE-Terminal, BDE-Buchung", Beide, seed: true),
        B(AppSettingKeys.BdeMehrfachBuchungProArbeitsgang, "false", GBde, "BDE: mehrere Buchungen je Arbeitsgang",
            "Ein Arbeitsgang darf mehrere parallele Buchungen verschiedener Mitarbeiter haben.", "BDE-Terminal, BDE-Buchung", Beide, seed: true),
        B(AppSettingKeys.BdeGleichzeitigerAbschlussBeiMehrfachStart, "false", GBde, "BDE: gemeinsamer Abschluss",
            "Parallel gestartete Buchungen eines Mitarbeiters müssen gemeinsam fertiggemeldet werden.", "BDE-Terminal", Beide, seed: true,
            abhaengig: AppSettingKeys.BdeMehrfachBuchungProOperator),
        B(AppSettingKeys.BdeSchichtkalenderAktiv, "false", GBde, "BDE: Schichtkalender", "Schichtkalender mit automatischer Pause am Schichtende.",
            "BDE-Schichtkalender; Dienst Auto-Pause", Beide, seed: true, warn: "Pausiert laufende Buchungen am Schichtende automatisch (Dienst)."),
        I(AppSettingKeys.BdeTerminalMaxHauptFa, "5", GBde, "BDE-Terminal: Haupt-FAs in der Liste",
            "Anzahl Haupt-FAs in der Arbeitsgang-Liste; der Rest über „Weitere anzeigen“.", "BDE-Terminal (hierarchisch)", "IDEAL", seed: true, "Haupt-FAs"),

        // ---- Teileverfolgung und OSEON (AKE) ----
        B("TeileverfolgungAktiv", "false", GOseon, "Teileverfolgung", "Schaltet das Teileverfolgungs-Modul (OSEON) ein.", "Menü, Startseite, Teileverfolgung",
            "AKE", seed: true),
        B("OseonRueckmeldungAktiv", "false", GOseon, "Rückmeldung an OSEON", "Derzeit ohne Wirkung (kein Leser im Code).", "– (derzeit ohne Wirkung)",
            "AKE", seed: true, warn: "Derzeit ohne Wirkung."),
        B("SageRueckmeldungAktiv", "false", GOseon, "Rückmeldung an Sage", "Derzeit ohne Wirkung (kein Leser im Code).", "– (derzeit ohne Wirkung)",
            Beide, seed: true, warn: "Derzeit ohne Wirkung."),
        I("OseonAmpelGelbTage", "1", GOseon, "OSEON-Ampel: gelb ab", "Gelb ab so vielen Tagen vor dem Termin.", "Teileverfolgung (Ampel)", "AKE", seed: true, "Tage"),
        I("OseonAmpelBlauTage", "2", GOseon, "OSEON-Ampel: blau ab", "Blau ab so vielen Tagen vor dem Termin.", "Teileverfolgung (Ampel)", "AKE", seed: true, "Tage"),
        I(AppSettingKeys.OseonReportingHorizonDays, "10", GOseon, "OSEON-Reporting: Horizont", "Tage in die Zukunft (Standard-Horizont des Reportings).",
            "OSEON-Reporting", "AKE", seed: true, "Tage", min: 1, max: 60),
        I(AppSettingKeys.OseonReportingOverdueLookbackDays, "90", GOseon, "OSEON-Reporting: Rückblick überfällig",
            "Tage in die Vergangenheit für den Bereich „Überfällig“.", "OSEON-Reporting", "AKE", seed: true, "Tage", min: 1, max: 365),

        // ---- IDEAL FA-Hierarchie ----
        B(AppSettingKeys.FaHierarchyKommissionierlistenAktiv, "false", GIdeal, "Kommissionierlisten", "Schaltet das Kommissionierlisten-Modul ein.",
            "Menü, Kommissionierlisten", "IDEAL", seed: true),
        B(AppSettingKeys.FaHierarchyBeschichtungAktiv, "false", GIdeal, "Beschichtungsaufträge", "Schaltet das Beschichtungsauftrag-Modul ein.",
            "Menü, Beschichtung", "IDEAL", seed: true),
        B(AppSettingKeys.FaHierarchyVormontageAktiv, "false", GIdeal, "Vormontage-Listen", "Schaltet die Vormontage-Listen (Einzelne Teile, Summiert) ein.",
            "Menü, Vormontage", "IDEAL", seed: true),
        I(AppSettingKeys.FaHierarchyMaxDepth, "500", GIdeal, "FA-Struktur: max. Tiefe", "Höchste Rekursionstiefe beim Aufbau der FA-Struktur (Reißleine).",
            "FA-Struktur", "IDEAL", seed: true, "Ebenen"),

        // ---- Sage ----
        B(AppSettingKeys.SageSdataFaAnsichtAktiv, "false", GSage, "FA-Struktur live aus Sage", "Zeigt die Ansicht „FA-Struktur (Sage live)“ (liest nur).",
            "Menü, FA-Struktur (Sage live)", Beide, seed: false),

        // ---- Anmeldung ----
        B(AppSettingKeys.WindowsAuthAktiv, "false", GAnmeldung, "Windows-Anmeldung", "Automatische Anmeldung mit dem Windows-Konto.",
            "Anmeldung, Auto-Login", Beide, seed: true, warn: "Ändert die Anmeldung aller Benutzer (Auto-Login)."),
        S(AppSettingKeys.WindowsAuthBerechtigungsgruppe, "", GAnmeldung, "AD-Berechtigungsgruppe", "AD-Gruppe (SAM-Name) für „AD-Benutzer anlegen“.",
            "Benutzer → AD-Benutzer anlegen", Beide, seed: true),

        // ---- Firma und Druck ----
        S(AppSettingKeys.Firmenname, "", GFirma, "Firmenname", "Firmenname im Ersatz-Briefkopf.", "Druck (Briefkopf)", Beide, seed: false),
        S(AppSettingKeys.Firmenanschrift, "", GFirma, "Firmenanschrift", "Anschrift im Ersatz-Briefkopf.", "Druck (Briefkopf)", Beide, seed: false, multiline: true),
        S(AppSettingKeys.BriefkopfVorlagePfad, "", GFirma, "Briefkopf-Vorlage (Pfad)", "Voller Pfad zur briefkopf.html. Leer = Ersatz-Kopf aus Firmenname und Anschrift.",
            "Druck (Briefkopf)", Beide, seed: false),

        // ---- Links (Vault, enaio) ----
        B(AppSettingKeys.VaultLinksAktiv, "true", GLinks, "Vault-Links", "Zeigt Vault-Zeichnungslinks an Artikelnummern. Nur „false“ blendet aus.",
            "Artikelnummern in Listen und Details", Beide, seed: true, SettingBoolLesart.NurFalseSperrt),
        S(AppSettingKeys.VaultLinkVorlage, "http://akevault24.ake.at/AutodeskTC/AKE-VAULT01/explore?search={Artikelnummer}&searchContext=0", GLinks,
            "Vault-Adresse", "Adresse mit Platzhalter {Artikelnummer}. Leer = keine Links.", "Artikelnummern in Listen und Details", Beide, seed: true,
            abhaengig: AppSettingKeys.VaultLinksAktiv),
        B(AppSettingKeys.EnaioLinksAktiv, "true", GLinks, "enaio-Links", "Zeigt enaio-Dokument-Links an FA-Nummern. Nur „false“ blendet aus.",
            "FA-Nummern in Listen und Details", Beide, seed: true, SettingBoolLesart.NurFalseSperrt),
        S(AppSettingKeys.EnaioLinkVorlage, "http://akeosapp.ake.at/oscontentviewer/viewer/{ObjektId}/?pagecount=1", GLinks, "enaio-Adresse",
            "Adresse mit Platzhalter {ObjektId}. Leer = keine Links.", "FA-Nummern in Listen und Details", Beide, seed: true,
            abhaengig: AppSettingKeys.EnaioLinksAktiv),
    };

    /// <summary>Findet eine Definition per Key (case-sensitiv, wie DB-Key).</summary>
    public static bool TryGet(string key, [NotNullWhen(true)] out SettingDefinition? def)
    {
        def = All.FirstOrDefault(d => d.Key == key);
        return def is not null;
    }

    /// <summary>Feste Werte eines <see cref="SettingType.Choice"/>-Keys (ein Wert, Dropdown) — ersetzt das fruehere <c>AppSettingKeys.FesteWerte</c>.</summary>
    public static bool TryGetWerte(string key, [NotNullWhen(true)] out (string Wert, string Text)[]? werte)
    {
        werte = TryGet(key, out var d) && d.Type == SettingType.Choice ? d.Werte : null;
        return werte is not null;
    }

    /// <summary>Was der Web-Start anlegt, wenn die Zeile fehlt (Key, Wert, Beschreibung).</summary>
    public static IEnumerable<(string Key, string Value, string Description)> SeedEintraege =>
        All.Where(d => d.Seed).Select(d => (d.Key, d.DefaultValue, d.Description));
}
