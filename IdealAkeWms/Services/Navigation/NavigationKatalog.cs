using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services.HierarchischeStruktur;

namespace IdealAkeWms.Services.Navigation;

/// <summary>v1.99.45 (ADR 0034): ein Menueziel. <see cref="Kategorie"/> null = nur im Menue, keine Kachel auf der Startseite.</summary>
public sealed record NavZiel(string Key, string Titel, string Controller, string Action, string? Kategorie = null, string? Beschreibung = null);

/// <summary>Katalog aller Menueziele. Reihenfolge von <see cref="Alle"/> = Kachelreihenfolge; <see cref="Kategorien"/> = Reihenfolge der
/// Startseiten-Abschnitte. Ein neuer Menuepunkt entsteht NUR hier plus eine Regel in <see cref="NavigationSicht"/> (Waechter-Tests).</summary>
public static class NavZiele
{
    public const string KatLager = "Lager", KatBestellungen = "Bestellungen", KatKommissionierung = "Kommissionierung", KatProduktionBde = "Produktion/BDE",
        KatBlechfertigung = "Blechfertigung", KatBeschichtung = "Beschichtung", KatEinkauf = "Einkauf/Wareneingang", KatStammdaten = "Stammdaten",
        KatAdministration = "Administration";

    public static readonly IReadOnlyList<string> Kategorien =
        [KatLager, KatBestellungen, KatKommissionierung, KatProduktionBde, KatBlechfertigung, KatBeschichtung, KatEinkauf, KatStammdaten, KatAdministration];

    // ---- Lager
    public static readonly NavZiel Einbuchung = new("Einbuchung", "Einbuchung", "StockMovements", "Inbound", KatLager, "Ware auf einen Lagerplatz einbuchen");
    public static readonly NavZiel MehrfachEinbuchung = new("MehrfachEinbuchung", "Mehrfach-Einbuchung", "StockMovements", "InboundBulk", KatLager, "Mehrere Artikel in einem Zug einbuchen");
    public static readonly NavZiel Ausbuchung = new("Ausbuchung", "Ausbuchung", "StockMovements", "Outbound", KatLager, "Ware von einem Lagerplatz ausbuchen");
    public static readonly NavZiel Umbuchung = new("Umbuchung", "Umbuchung", "StockMovements", "Transfer", KatLager, "Ware auf einen anderen Lagerplatz umbuchen");
    public static readonly NavZiel Bestaende = new("Bestaende", "Bestände", "StockOverview", "Index", KatLager, "Aktuelle Bestände je Artikel und Lagerplatz");
    public static readonly NavZiel Bewegungshistorie = new("Bewegungshistorie", "Bewegungshistorie", "StockMovements", "Index", KatLager, "Alle Lagerbewegungen");
    public static readonly NavZiel Artikelinfo = new("Artikelinfo", "Artikelinfo", "Articles", "Info", KatLager, "QR-Code scannen: Details und Bestand");
    public static readonly NavZiel LagerplatzAusbuchen = new("LagerplatzAusbuchen", "Lagerplatz ausbuchen", "StockMovements", "OutboundAll", KatLager, "Einen ganzen Lagerplatz leeren");
    public static readonly NavZiel LagerplatzUmbuchen = new("LagerplatzUmbuchen", "Lagerplatz umbuchen", "StockMovements", "LocationTransfer", KatLager, "Einen ganzen Lagerplatz umziehen");
    public static readonly NavZiel SageLagerbuchungen = new("SageLagerbuchungen", "Sage-Lagerbuchungen", "SageBookingQueue", "Index");
    public static readonly NavZiel OffeneBestellungenLager = new("OffeneBestellungenLager", "Offene Bestellungen", "PurchaseOrders", "Index", KatEinkauf, "Offene Sage-Bestellungen für den Wareneingang");
    public static readonly NavZiel WareneingangZuBestellung = new("WareneingangZuBestellung", "Wareneingang zu Bestellung", "PurchaseOrders", "Receive", KatEinkauf, "Gelieferte Ware zur Bestellung buchen");
    public static readonly NavZiel SageWareneingaengeLager = new("SageWareneingaengeLager", "Sage-Wareneingänge", "PurchaseOrders", "SageWareneingaenge");

    // ---- Bestellungen
    public static readonly NavZiel Bedarfsmeldungen = new("Bedarfsmeldungen", "Bedarfsmeldungen", "PartRequisitions", "Index", KatBestellungen, "Bedarf an die Empfänger melden");
    public static readonly NavZiel Lagerbestellungen = new("Lagerbestellungen", "Lagerbestellungen", "WarehouseRequisitions", "Index", KatBestellungen, "Ware beim Lager bestellen");
    public static readonly NavZiel MeineFehlteile = new("MeineFehlteile", "Meine Fehlteile", "MissingParts", "Index", KatBestellungen, "Offene Fehlteile eigener Bestellungen");
    public static readonly NavZiel LagerEingehendeListen = new("LagerEingehendeListen", "Lager: Eingehende Listen", "WarehousePicking", "Index", KatBestellungen, "Bestellungen an das Lager bearbeiten");
    public static readonly NavZiel LagerFehlteile = new("LagerFehlteile", "Lager: Fehlteile", "MissingPartsLager", "Index", KatBestellungen, "Fehlteile aller Bestellungen nachliefern");

    // ---- Kommissionierung
    public static readonly NavZiel Kommissionierung = new("Kommissionierung", "Kommissionierung", "Picking", "Index", KatKommissionierung, "Fertigungsaufträge kommissionieren");
    public static readonly NavZiel Kommissionierlisten = new("Kommissionierlisten", "Kommissionierlisten", "FaHierarchyKommissionierListen", "Summiert", KatKommissionierung, "Material je Haupt-FA summiert");
    public static readonly NavZiel LeitstandKommissionierung = new("LeitstandKommissionierung", "Leitstand Kommissionierung", "PickingLeitstand", "Index", KatKommissionierung, "Aufträge freigeben und priorisieren");

    // ---- Produktion/BDE
    public static readonly NavZiel FaListe = new("FaListe", "FA-Liste", "ProductionOrders", "Index", KatProduktionBde, "Fertigungsaufträge mit Terminen");
    public static readonly NavZiel FaStruktur = new("FaStruktur", "FA-Struktur", "FaHierarchy", "Index", KatProduktionBde, "Haupt- und Unter-FAs als Baum");
    public static readonly NavZiel FaVervollstaendigen = new("FaVervollstaendigen", "FA-Vervollständigen", "FaCompletion", "Index", KatProduktionBde, "Fehlende FA-Angaben ergänzen");
    public static readonly NavZiel FaAbarbeitungsliste = new("FaAbarbeitungsliste", "FA-Abarbeitungsliste", "FaWorklist", "Index", KatProduktionBde, "Arbeitsschritte je Werkbank abarbeiten");
    public static readonly NavZiel Vormontage = new("Vormontage", "Vormontage", "FaHierarchyVormontage", "Index", KatProduktionBde, "Vormontage-Listen je Abteilung");
    public static readonly NavZiel BdeTerminal = new("BdeTerminal", "Terminal", "BdeTerminal", "Index", KatProduktionBde, "BDE: Arbeitsgänge buchen");
    public static readonly NavZiel BdeCockpit = new("BdeCockpit", "Cockpit", "BdeCockpit", "Index", KatProduktionBde, "BDE: laufende Arbeit im Überblick");
    public static readonly NavZiel BdeBuchungen = new("BdeBuchungen", "Buchungen", "BdeBookings", "Index", KatProduktionBde, "BDE: Buchungen prüfen und korrigieren");
    public static readonly NavZiel BdeStammdaten = new("BdeStammdaten", "Stammdaten", "BdeMasterData", "Index");
    public static readonly NavZiel BdeSchichtkalender = new("BdeSchichtkalender", "Schichtkalender", "BdeShiftCalendar", "Index");
    public static readonly NavZiel TeileverfolgungRueckmeldungen = new("TeileverfolgungRueckmeldungen", "Rückmeldungen", "Tracking", "Index");
    public static readonly NavZiel TeileverfolgungOseon = new("TeileverfolgungOseon", "OSEON Aufträge", "Tracking", "OseonIndex", KatProduktionBde, "Teileverfolgung: OSEON-Aufträge");
    public static readonly NavZiel OseonAgUebersicht = new("OseonAgUebersicht", "OSEON AG-Übersicht", "OseonReporting", "OperationsOverview", KatProduktionBde, "Reporting: Arbeitsgänge je Zeitraum");

    // ---- Blechfertigung
    public static readonly NavZiel BfWarenkorb = new("BfWarenkorb", "BF-Warenkorb", "BfAuftraege", "Warenkorb", KatBlechfertigung, "Blechteile bestellen");
    public static readonly NavZiel BfAuftraege = new("BfAuftraege", "BF-Aufträge", "BfAuftraege", "Index", KatBlechfertigung, "Bestellte Blechteile verfolgen");

    // ---- Beschichtung
    public static readonly NavZiel BeschichtungVorschlagsliste = new("BeschichtungVorschlagsliste", "Vorschlagsliste", "FaHierarchyBeschichtung", "Summiert", KatBeschichtung, "Zu beschichtende Teile summiert");
    public static readonly NavZiel Beschichtungsauftraege = new("Beschichtungsauftraege", "Aufträge", "Beschichtungsauftraege", "Index", KatBeschichtung, "Beschichtungsaufträge an den Beschichter");

    // ---- Einkauf/Wareneingang
    public static readonly NavZiel Bestellvorschlaege = new("Bestellvorschlaege", "Bestellvorschläge", "PurchaseProposals", "Index", KatEinkauf, "Bestellvorschläge prüfen und bestellen");
    public static readonly NavZiel OffeneBestellungenEinkauf = new("OffeneBestellungenEinkauf", "Offene Bestellungen (Einkauf)", "EinkaufPurchaseOrders", "Index", KatEinkauf, "Offene Sage-Bestellungen des Einkaufs");
    public static readonly NavZiel SageWareneingaengeEinkauf = new("SageWareneingaengeEinkauf", "Sage-Wareneingänge (offen)", "EinkaufPurchaseOrders", "SageWareneingaenge");

    // ---- Stammdaten
    public static readonly NavZiel Artikel = new("Artikel", "Artikel", "Articles", "Index", KatStammdaten, "Artikelstamm aus Sage");
    public static readonly NavZiel Lagerplaetze = new("Lagerplaetze", "Lagerplätze", "StorageLocations", "Index", KatStammdaten, "Lagerplätze pflegen");
    public static readonly NavZiel Werkbaenke = new("Werkbaenke", "Werkbänke", "ProductionWorkplaces", "Index", KatStammdaten, "Werkbänke der Fertigung");
    public static readonly NavZiel Artikelkategorien = new("Artikelkategorien", "Artikelkategorien", "ArticleCategories", "Index");
    public static readonly NavZiel Artikelmerkmale = new("Artikelmerkmale", "Artikelmerkmale", "ArticleAttributes", "Index");
    public static readonly NavZiel FaVorbauAg = new("FaVorbauAg", "FA-Vorbau-AG", "WorkSteps", "Index");
    public static readonly NavZiel Kommissionierbereiche = new("Kommissionierbereiche", "Kommissionierbereiche", "PickingAreas", "Index");
    public static readonly NavZiel FaMerkmale = new("FaMerkmale", "FA-Merkmale", "FaAttributes", "Index");
    public static readonly NavZiel Kreditoren = new("Kreditoren", "Kreditoren", "Creditors", "Index");
    public static readonly NavZiel Debitoren = new("Debitoren", "Debitoren", "Debitors", "Index");
    public static readonly NavZiel Empfaenger = new("Empfaenger", "Empfänger", "OrderRecipients", "Index");
    public static readonly NavZiel ArtikelgruppenZuordnung = new("ArtikelgruppenZuordnung", "Artikelgruppen-Zuordnung", "OrderRecipients", "ArticleGroupMappings");

    // ---- Administration (im Menue unter Stammdaten)
    public static readonly NavZiel Benutzer = new("Benutzer", "Benutzer", "Users", "Index", KatAdministration, "Benutzer und Rollen zuweisen");
    public static readonly NavZiel Arbeitsplaetze = new("Arbeitsplaetze", "Arbeitsplätze", "Workstations", "Index", KatAdministration, "Rechner und Drucker zuordnen");
    public static readonly NavZiel Rollen = new("Rollen", "Rollen", "Roles", "Index", KatAdministration, "Rollen und ihre Bedeutung");
    public static readonly NavZiel Arbeitsgaenge = new("Arbeitsgaenge", "Arbeitsgänge", "OperationConfigs", "Index", KatAdministration, "Arbeitsgang-Stammdaten");
    public static readonly NavZiel ArbeitsgaengeSage = new("ArbeitsgaengeSage", "Arbeitsgänge → Sage", "SageFaArbeitsgaenge", "Index", KatAdministration, "Arbeitsgänge in Sage-FAs anlegen");
    public static readonly NavZiel Einstellungen = new("Einstellungen", "Einstellungen", "Settings", "Index", KatAdministration, "Module und fachliche Einstellungen");
    public static readonly NavZiel ServiceEinstellungen = new("ServiceEinstellungen", "Service-Einstellungen", "ServiceSettings", "Index");
    public static readonly NavZiel Standorteinstellungen = new("Standorteinstellungen", "Standorteinstellungen", "StandortEinstellungen", "Index", KatAdministration, "Einstellungen je Standort");
    public static readonly NavZiel AktivitaetsProtokoll = new("AktivitaetsProtokoll", "Aktivitäts-Protokoll", "SyncLog", "Index", KatAdministration, "Läufe der Hintergrunddienste");

    /// <summary>Alle Ziele in Kachelreihenfolge (innerhalb der Kategorie).</summary>
    public static readonly IReadOnlyList<NavZiel> Alle =
    [
        Einbuchung, MehrfachEinbuchung, Ausbuchung, Umbuchung, Bestaende, Bewegungshistorie, Artikelinfo, LagerplatzAusbuchen, LagerplatzUmbuchen,
        SageLagerbuchungen, OffeneBestellungenLager, WareneingangZuBestellung, SageWareneingaengeLager,
        Bedarfsmeldungen, Lagerbestellungen, MeineFehlteile, LagerEingehendeListen, LagerFehlteile,
        Kommissionierung, Kommissionierlisten, LeitstandKommissionierung,
        FaListe, FaStruktur, FaVervollstaendigen, FaAbarbeitungsliste, Vormontage, BdeTerminal, BdeCockpit, BdeBuchungen, BdeStammdaten,
        BdeSchichtkalender, TeileverfolgungRueckmeldungen, TeileverfolgungOseon, OseonAgUebersicht,
        BfWarenkorb, BfAuftraege, BeschichtungVorschlagsliste, Beschichtungsauftraege,
        Bestellvorschlaege, OffeneBestellungenEinkauf, SageWareneingaengeEinkauf,
        Artikel, Lagerplaetze, Werkbaenke, Artikelkategorien, Artikelmerkmale, FaVorbauAg, Kommissionierbereiche, FaMerkmale, Kreditoren, Debitoren,
        Empfaenger, ArtikelgruppenZuordnung,
        Benutzer, Arbeitsplaetze, Rollen, Arbeitsgaenge, ArbeitsgaengeSage, Einstellungen, ServiceEinstellungen, Standorteinstellungen, AktivitaetsProtokoll,
    ];
}

/// <summary>Ergebnis fuer einen Request: welche Ziele sichtbar sind.</summary>
public sealed class NavigationErgebnis(IReadOnlySet<string> sichtbar, bool leitstandAktiv)
{
    /// <summary>Fuer das Badge an „Kommissionierung“ (_Layout).</summary>
    public bool LeitstandAktiv { get; } = leitstandAktiv;

    public bool Zeigt(NavZiel z) => sichtbar.Contains(z.Key);

    /// <summary>Dropdown nur, wenn mindestens ein Eintrag sichtbar ist.</summary>
    public bool ZeigtEines(params NavZiel[] ziele) => ziele.Any(Zeigt);

    /// <summary>Startseite: nur nicht-leere Kategorien, in fester Reihenfolge.</summary>
    public IReadOnlyList<(string Kategorie, IReadOnlyList<NavZiel> Kacheln)> Kacheln => NavZiele.Kategorien
        .Select(k => (k, (IReadOnlyList<NavZiel>)NavZiele.Alle.Where(z => z.Kategorie == k && Zeigt(z)).ToList()))
        .Where(x => x.Item2.Count > 0).ToList();
}

/// <summary>v1.99.45 (ADR 0034): EINE Sichtbarkeitsregel je Menueziel fuer Menue (_Layout) und Startseite (Home/Index).
/// Rechte ueber dieselben <see cref="ICurrentUserService"/>-Methoden wie die Filter am Ziel, Modulschalter mit derselben Lese-Semantik
/// wie vorher im Menue (ADR 0011). Scoped: einmal je Request berechnet.</summary>
public sealed class NavigationSicht(ICurrentUserService user, IAppSettingRepository appSettings, IServiceSettingRepository serviceSettings)
{
    private NavigationErgebnis? _ergebnis;

    public async Task<NavigationErgebnis> GetAsync()
    {
        if (_ergebnis != null) return _ergebnis;

        async Task<bool> An(string key) => string.Equals(await appSettings.GetValueAsync(key), "true", StringComparison.OrdinalIgnoreCase);

        var stock = await user.CanAccessStockAsync();
        var stockRead = await user.CanAccessStockReadAsync();
        var processLager = await user.CanProcessLagerAsync();
        var pick = await user.CanPickAsync();
        var lagerOderGlas = await user.CanAccessLagerbestellungAsync() || await user.CanAccessGlasbestellungAsync();
        var tracking = await user.CanViewTrackingAsync();
        var release = await user.CanManagePickingReleaseAsync();
        var vorbau = await user.HasVorbauAccessAsync();
        var useBde = await user.CanUseBdeAsync();
        var shiftlead = await user.CanManageBdeShiftleadAsync();
        var admin = await user.IsAdminAsync();
        var mdRead = await user.HasMasterDataReadAccessAsync();
        var faLesen = await user.CanViewProductionOrdersAsync();
        var transfer = await user.CanTransferStockAsync();
        var completion = await user.CanFaCompletionAsync();
        var reporting = await user.CanReportOperationsAsync();
        var beschichtungRecht = await user.HasBeschichtungsauftragAccessAsync();

        var teileverfolgung = await An(AppSettingKeys.TeileverfolgungAktiv);
        var bestellungen = await An(AppSettingKeys.BestellungenAktiv);
        // LagerbestellungAktiv: Default an, nur exakt "false" sperrt (ADR 0011)
        var lagerbestellung = !string.Equals(await appSettings.GetValueAsync(AppSettingKeys.LagerbestellungAktiv), "false", StringComparison.OrdinalIgnoreCase);
        var leitstand = await An(AppSettingKeys.LeitstandAktiv);
        var bde = await An(AppSettingKeys.BdeAktiv);
        var faCompletion = await An(AppSettingKeys.FaCompletionAktiv);
        var kommListen = await An(AppSettingKeys.FaHierarchyKommissionierlistenAktiv);
        var beschichtung = await An(AppSettingKeys.FaHierarchyBeschichtungAktiv);
        var vormontage = await An(AppSettingKeys.FaHierarchyVormontageAktiv);
        var wareneingang = await An(AppSettingKeys.WareneingangBestellungAktiv);
        var einkauf = await An(AppSettingKeys.EinkaufAktiv) && await user.CanAccessEinkaufAsync();
        var blechAn = await An(AppSettingKeys.BlechfertigungAktiv);
        var blech = blechAn && await user.CanBfBestellenAsync();
        // Master nur fuer Admins lesen (wie vorher im Menue)
        var master = admin && string.Equals(await serviceSettings.GetValueAsync(HierarchischeStrukturKeys.Master), "true", StringComparison.OrdinalIgnoreCase);
        var lagerbestellungSicht = lagerbestellung && (stock || lagerOderGlas);

        bool Regel(string key) => key switch
        {
            "Einbuchung" or "MehrfachEinbuchung" or "Ausbuchung" or "Umbuchung" => stock,
            "Bestaende" or "Bewegungshistorie" or "SageLagerbuchungen" => stockRead,
            "Artikelinfo" => mdRead,
            "LagerplatzAusbuchen" or "LagerplatzUmbuchen" => transfer,
            "OffeneBestellungenLager" or "SageWareneingaengeLager" => wareneingang && stockRead,
            "WareneingangZuBestellung" => wareneingang && stock,
            "Bedarfsmeldungen" => bestellungen && stock,
            "Lagerbestellungen" or "MeineFehlteile" => lagerbestellungSicht,
            "LagerEingehendeListen" or "LagerFehlteile" => lagerbestellungSicht && processLager,
            "Kommissionierung" => pick,
            "Kommissionierlisten" => kommListen && processLager,
            "LeitstandKommissionierung" => leitstand && (pick || release),
            "FaListe" or "FaStruktur" => faLesen,
            "FaVervollstaendigen" => faCompletion && completion,
            "FaAbarbeitungsliste" => faCompletion && vorbau,
            "Vormontage" => vormontage && vorbau,
            "BdeTerminal" => bde && useBde,
            "BdeCockpit" or "BdeBuchungen" or "BdeStammdaten" => bde && useBde && shiftlead,
            "BdeSchichtkalender" => bde && useBde && admin,
            "TeileverfolgungRueckmeldungen" or "TeileverfolgungOseon" => teileverfolgung && tracking,
            // Freigabe-Antwort 5 (2026-11-19): nur bei aktivem OSEON-Modul (TeileverfolgungAktiv, AppSettingDefinitions „Teileverfolgungs-Modul (OSEON)“)
            "OseonAgUebersicht" => teileverfolgung && reporting,
            "BfWarenkorb" or "BfAuftraege" => blech,
            "BeschichtungVorschlagsliste" or "Beschichtungsauftraege" => beschichtung && beschichtungRecht,
            "Bestellvorschlaege" or "OffeneBestellungenEinkauf" or "SageWareneingaengeEinkauf" => einkauf,
            "Artikel" or "Lagerplaetze" or "Werkbaenke" or "Artikelkategorien" or "Artikelmerkmale" or "FaVorbauAg" or "Kommissionierbereiche"
                or "FaMerkmale" or "Kreditoren" or "Debitoren" => mdRead,
            "Empfaenger" => mdRead && (bestellungen || lagerbestellung || blechAn),
            "ArtikelgruppenZuordnung" => mdRead && bestellungen,
            "Benutzer" or "Arbeitsplaetze" or "Rollen" or "Arbeitsgaenge" or "Einstellungen" or "ServiceEinstellungen" or "Standorteinstellungen"
                or "AktivitaetsProtokoll" => admin,
            "ArbeitsgaengeSage" => master,
            _ => throw new InvalidOperationException($"NavZiel '{key}' ohne Regel in NavigationSicht.")
        };

        return _ergebnis = new NavigationErgebnis(NavZiele.Alle.Where(z => Regel(z.Key)).Select(z => z.Key).ToHashSet(), leitstand);
    }
}
