using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using IdealAkeWms.Services;

namespace IdealAkeWms.Models;

/// <summary>
/// Vollständiger, typisierter Katalog aller Service-Einstellungen.
/// Quelle: jeder Eintrag entspricht einem realen ServiceSettings-Read im
/// Service (Common/ServiceSettings.Get*Async oder direkter ServiceSettings-
/// Tabellen-Read) bzw. dem bisherigen Program.cs-Seed-Block.
/// Ausgenommen (bewusst NICHT im Katalog): ConnectionStrings:* + MailSettings:*.
/// </summary>
public static class ServiceSettingDefinitions
{
    /// <summary>Katalog mit Metadaten (v1.77.0, Spec 2026-11-07-einstellungen-auswahlfelder-und-beschreibung E3, ADR 0020): Basisliste unten,
    /// ergaenzt um Anzeigename, Verwendung, Standort, Warnhinweis (<see cref="Meta"/>), Bool-Lesart „leer = Default“
    /// (<c>ServiceSettings.GetBoolAsync</c>) und die Quellenschalter als Auswahl.</summary>
    public static IReadOnlyList<SettingDefinition> All => _all ??= Basis().Select(Ergaenzen).ToList();
    private static IReadOnlyList<SettingDefinition>? _all;

    /// <summary>Erlaubte Werte der Quellenschalter <c>Sync:Quelle:*</c> — exakt wie <c>SageQuelle</c> im Dienst (ordinal, ohne Trim).</summary>
    public static readonly (string Wert, string Text)[] QuellenWerte =
    {
        ("View", "View – bisheriger SQL-Weg"),
        ("SData", "SData – Sage-Schnittstelle"),
        ("Vergleich", "Vergleich – liest beide, schreibt aus der View"),
    };

    private const string WarnSData = "SData schreibt die Stammdaten dieses Abgleichs aus der Sage-Schnittstelle.";

    /// <summary>Anzeigename, Verwendung, Standort, Warnhinweis je Key (Standort leer = beide).</summary>
    private static readonly Dictionary<string, (string Name, string Verwendung, string? Standort, string? Warn)> Meta = new()
    {
        ["Sync:ProductionOrdersEnabled"] = ("FA-Abgleich aus Sage", "Dienst: Sync-Lauf Produktionsaufträge", null, null),
        ["Sync:ArticlesEnabled"] = ("Artikel-Abgleich aus Sage", "Dienst: Sync-Lauf Artikel", null, null),
        ["Sync:ArticlesViewName"] = ("Artikel-View (Sage)", "Dienst: Artikel-Abgleich; Sperre SData für Artikel", "IDEAL", null),
        ["Sync:OseonArticleCategoryEnabled"] = ("OSEON-Artikelkategorien", "Dienst: nach dem Artikel-Abgleich", "AKE", null),
        ["Sync:OseonTrackingEnabled"] = ("OSEON-Teileverfolgung", "Dienst: OSEON-Tracking- und Werkbank-Abgleich", "AKE", null),
        ["Sync:EnaioDmsEnabled"] = ("enaio-DMS-Abgleich", "Dienst: enaio-Lauf; Links an FA-Nummern", null, null),
        ["Sync:EnaioDmsViewName"] = ("enaio-View", "Dienst: enaio-Lauf", null, null),
        ["Sync:PartRequisitionEmailEnabled"] = ("Mails für Bedarfsmeldungen", "Dienst: Mailversand Bedarfsmeldungen", null, "Versendet Mails."),
        ["Sync:WarehouseRequisitionEmailEnabled"] = ("Mails für Lagerbestellungen", "Dienst: Mailversand Lagerbestellungen", null, "Versendet Mails."),
        ["Sync:LagerplaetzeEnabled"] = ("Lagerplatz-Abgleich aus Sage", "Dienst: Lauf „Lagerplatz“", null, null),
        ["Sync:LagerbestandEnabled"] = ("Lagerbestand-Abgleich aus Sage", "Dienst: Lauf „Lagerbestand“", null, "Korrigiert bzw. nullt Bestände im WMS."),
        ["Sync:LagerbestandIntervalMinutes"] = ("Lagerbestand: eigenes Intervall", "Dienst: Lauf „Lagerbestand“", null, null),
        ["Sync:LagerbestandNullsetzenMaxPerRun"] = ("Lagerbestand: max. Nullsetzungen je Lauf", "Dienst: Lauf „Lagerbestand“ (Sicherheits-Grenze)", null, null),
        ["Sync:ProductionOrderReconcileEnabled"] = ("Verwaiste FAs stornieren", "Dienst: FA-Abgleich", null, "Storniert FAs, die in Sage gelöscht wurden."),
        ["Sync:ReconcileMaxCancelPerRun"] = ("Storno: max. FAs je Lauf", "Dienst: FA-Abgleich (Sicherheits-Grenze)", null, null),
        ["Sync:FaZusatzinfoEnabled"] = ("FA-Zusatzinfos aus Sage", "Dienst: Lauf „FA-Zusatzinfos“", null, "Setzt FAs mit Sage-Status verpackt/abgeholt auf Komm-Erledigt."),
        ["Sync:FaZusatzinfoAutoDoneMaxPerRun"] = ("Auto-Erledigt: max. FAs je Lauf", "Dienst: Lauf „FA-Zusatzinfos“ (Sicherheits-Grenze)", null, null),
        ["Sync:FaZusatzinfoAutoErledigtEnabled"] = ("Auto-Erledigt aus Sage-Status", "Dienst: Lauf „FA-Zusatzinfos“", null, "Setzt FAs automatisch auf Komm-Erledigt."),
        ["Sync:HierarchicalFaEnabled"] = ("FA-Struktur-Import (IDEAL)", "Dienst: Lauf „FA-Hierarchie“", "IDEAL", null),
        ["Sync:FaHierarchyListeViewName"] = ("FA-Struktur: View Stückliste", "Dienst: Lauf „FA-Hierarchie“", "IDEAL", null),
        ["Sync:FaHierarchyInfosViewName"] = ("FA-Struktur: View Auftragsdaten", "Dienst: Lauf „FA-Hierarchie“", "IDEAL", null),
        ["ProduktionsauftragHierarchisch"] = ("Hierarchische Produktionsaufträge (Einwegtor)", "Dienst: Materialisierung; Umschalten nur über die eigene Seite", "IDEAL",
            "Einwegtor – nach dem ersten Lauf nicht zurückschaltbar."),
        ["Sync:WorkOperationStructureDetectionEnabled"] = ("Arbeitsgang-Erkennung aus der Struktur", "Dienst: nach dem FA-Struktur-Import", "IDEAL", null),
        ["Sync:ProductionWorkplaceSyncEnabled"] = ("Werkbänke aus Sage anlegen", "Dienst: Lauf „Werkbank-Anlage“", null, null),
        ["Sync:ProductionWorkplaceSyncMandant"] = ("Werkbank-Anlage: Mandant", "Dienst: Lauf „Werkbank-Anlage“", null, null),
        ["Sync:ProductionWorkplaceSyncViewName"] = ("Werkbank-Anlage: Sage-Quelle", "Dienst: Lauf „Werkbank-Anlage“", null, null),
        ["Sync:ProductionWorkplaceSyncAusschlussliste"] = ("Werkbank-Anlage: ausgeschlossene Kürzel", "Dienst: Lauf „Werkbank-Anlage“", null, null),
        ["Sync:OperationConfigSyncEnabled"] = ("Arbeitsgang-Abgleich", "Dienst: Lauf „Arbeitsgang-Abgleich“", null, null),
        ["Sync:OperationConfigSyncViewName"] = ("Arbeitsgang-Abgleich: View", "Dienst: Lauf „Arbeitsgang-Abgleich“", null, null),
        ["Sync:OperationConfigSyncFremdsystemRelevant"] = ("Neue Arbeitsgänge: Fremdsystem-relevant", "Dienst: Lauf „Arbeitsgang-Abgleich“ (nur Neuanlage)", null, null),
        ["SageLagerbuchungAktiv"] = ("Lagerbuchungen an Sage senden", "Dienst: Sage-Buchungs-Worker; Warteschlange", null, "Sendet Buchungen an Sage."),
        ["SageLagerbuchungUmbuchungAktiv"] = ("Umbuchungen an Sage senden", "Dienst: Sage-Buchungs-Worker", null, "Sendet Buchungen an Sage."),
        ["SageWareneingangBelegAktiv"] = ("Wareneingänge als Sage-Wareneingangsbeleg buchen",
            "Lager → Wareneingang zu Bestellung (Status und Platzregel beim Buchen); Dienst: Sage-Buchungs-Worker, Lauf „Sage-Wareneingang“; Liste „Sage-Wareneingänge“",
            "AKE (IDEAL aus, kein SData-Endpunkt)", "Schreibt Wareneingangsbelege nach Sage: Bestellung wird beliefert, Bestand in Sage gebucht. Nur nach erfolgreichem Schreibtest am AKE-Test einschalten. Der Einkauf darf diese Positionen danach nicht mehr von Hand schließen. Wareneingänge auf Plätze ohne Sage-Buchung sind gesperrt."),
        ["Sync:SageWareneingangMaxWiederholungen"] = ("Sage-Wareneingang: automatische Wiederholungen",
            "Dienst: Sage-Buchungs-Worker, Lauf „Sage-Wareneingang“ (Wiederholen nach Fehler); Liste „Sage-Wareneingänge“",
            "AKE (IDEAL ohne Wirkung, Sage-Wareneingang aus)", "Wiederholt das Anlegen von Wareneingangsbelegen in Sage. Vor jedem Versuch sucht der Dienst den Beleg in Sage; 0 = nur „Erneut senden“ von Hand."),
        ["Sync:SageWareneingangWiederholAbstandMinuten"] = ("Sage-Wareneingang: Abstand der Wiederholungen",
            "Dienst: Sage-Buchungs-Worker, Lauf „Sage-Wareneingang“", "AKE (IDEAL ohne Wirkung)", null),
        ["Sync:SageSystemKuerzel"] = ("System-Kürzel für Sage-Kennungen",
            "Dienst: Sage-Buchungs-Worker (Lagerbuchungen SM#…#, Lauf „Sage-Wareneingang“ WE#…#) – Memo am Sage-Beleg und Suche vor dem erneuten Senden; Liste „Sage-Wareneingänge“ (Spalte Memo)",
            null, "In der Produktion LEER lassen. Nur Testsysteme, die mit anderen WMS in dieselbe Sage-Datenbank schreiben, bekommen ein eigenes Kürzel (DEV, AT, ATI, IT). Ändern wirkt nur auf neue Sendungen; bereits gesendete Einträge behalten ihre Kennung."),
        // v1.99.11 Wareneingang-CSV (Spec 2026-11-12-wareneingang-barcode-lieferschein-csv-export)
        ["WareneingangCsvAktiv"] = ("Wareneingang: CSV-Übergabe auf Netzlaufwerk",
            "Dienst: Sage-Buchungs-Worker, Lauf „Wareneingang-CSV“; Liste „Sage-Wareneingänge“ (Spalte CSV, Hinweis); /ServiceSettings (Hinweis)",
            "AKE (IDEAL aus)", "Schreibt Dateien auf die Netzfreigabe aus „Wareneingang-CSV: Pfad“; ein anderes System (enaio) liest sie. Das Dienstkonto braucht dort Schreib- und Änderungsrecht. Wirkt nur bei eingeschaltetem Sage-Wareneingang. Beim Einschalten werden alle wartenden Vorgänge sofort geschrieben."),
        ["WareneingangCsvPfad"] = ("Wareneingang-CSV: Pfad", "Dienst: Lauf „Wareneingang-CSV“", "AKE",
            "Ein falscher Pfad legt Dateien an einer Stelle ab, die der Leser nicht sieht. Pfad mit dem Betreiber des Lesers (enaio) abstimmen."),
        ["Sync:WareneingangCsvMaxWiederholungen"] = ("Wareneingang-CSV: automatische Wiederholungen",
            "Dienst: Lauf „Wareneingang-CSV“ (Wiederholen nach Schreibfehler); Liste „Sage-Wareneingänge“", "AKE (IDEAL ohne Wirkung)", null),
        ["Sync:WareneingangCsvWiederholAbstandMinuten"] = ("Wareneingang-CSV: Abstand der Wiederholungen",
            "Dienst: Lauf „Wareneingang-CSV“", "AKE (IDEAL ohne Wirkung)", null),
        ["SData:BaseUrl"] = ("SData: Adresse", "Sage-Buchung, Sage-Abgleich, FA-Struktur live", null, null),
        ["SData:Application"] = ("SData: Application", "Sage-Buchung, Sage-Abgleich, FA-Struktur live", null, null),
        ["SData:ServiceContract"] = ("SData: ServiceContract", "Sage-Buchung", null, null),
        ["SData:Dataset"] = ("SData: Dataset (Mandant)", "Sage-Buchung, Sage-Abgleich, FA-Struktur live", null, null),
        ["Sync:SageLagerbuchungIntervalSeconds"] = ("Sage-Buchung: Intervall", "Dienst: Sage-Buchungs-Worker", null, null),
        ["Sync:SageLagerbuchungBatchSize"] = ("Sage-Buchung: Einträge je Durchlauf", "Dienst: Sage-Buchungs-Worker", null, null),
        ["Sync:SageLagerbuchungMaxRetries"] = ("Sage-Buchung: max. Versuche", "Dienst: Sage-Buchungs-Worker", null, null),
        ["Sync:SageLagerbuchungMaxErrorsPerRun"] = ("Sage-Buchung: Fehlermail ab Fehlern je Lauf", "Dienst: Sage-Buchungs-Worker", null, null),
        ["Sync:SageLagerbuchungStuckMinutes"] = ("Sage-Buchung: hängend nach", "Dienst: Sage-Buchungs-Worker; Sage-Wareneingang (hängend → Fehler „Ergebnis unklar“)", null, null),
        ["SageLagerbuchungSslZertifikatPruefen"] = ("SData: TLS-Zertifikat prüfen", "Sage-Buchung, Sage-Abgleich, FA-Struktur live", null, null),
        ["Sync:Quelle:Lagerplaetze"] = ("Quelle Lagerplatz-Abgleich", "Dienst: Lauf „Lagerplatz“", null, WarnSData),
        ["Sync:Quelle:Lagerbestand"] = ("Quelle Lagerbestand-Abgleich", "Dienst: Lauf „Lagerbestand“", null, WarnSData),
        ["Sync:Quelle:Artikel"] = ("Quelle Artikel-Abgleich", "Dienst: Lauf „Artikel“", "AKE frei; IDEAL gesperrt bis P-R1", WarnSData),
        ["Sync:Quelle:FaStruktur"] = ("Quelle FA-Struktur", "Dienst: Lauf „FA-Hierarchie“", "gesperrt bis P-F2", WarnSData),
        ["Sync:Quelle:Arbeitsplaetze"] = ("Quelle Werkbänke und Arbeitsgänge", "Dienst: Läufe „Werkbank-Anlage“, „Arbeitsgang-Abgleich“", null, WarnSData),
        ["Sync:Quelle:LagerbuchungLookup"] = ("Quelle Lagerbuchungs-Lookup", "Dienst: Sage-Buchungs-Worker (nur Lesen)", null, null),
        ["Sync:ArtikelBenutzerfelder"] = ("Artikel-Benutzerfelder aus Sage", "Dienst: Artikel-Abgleich über SData", null, null),
        ["Sync:PpsMitarbeiterEnabled"] = ("PPS-Mitarbeiter aus Sage", "Dienst: Lauf „PPS-Mitarbeiter“; BDE-Personalpflege", null, null),
        ["Sync:StillstandsgruendeEnabled"] = ("Stillstandsgründe aus Sage", "Dienst: Lauf „Stillstandsgründe“", null, null),
        ["Sync:KreditorenEnabled"] = ("Kreditoren aus Sage", "Dienst: Lauf „Kreditoren“; Stammdaten → Kreditoren", null, "Überschreibt WMS-Werte (Sage ist führend)."),
        ["Sync:DebitorenEnabled"] = ("Debitoren aus Sage", "Dienst: Lauf „Debitoren“; Stammdaten → Debitoren", null, "Überschreibt WMS-Werte (Sage ist führend)."),
        ["Sync:ArtikelLieferantEnabled"] = ("Artikel-Lieferanten aus Sage", "Dienst: Lauf „Artikel-Lieferanten“", null, null),
        ["Sync:EinkaufBestellungenEnabled"] = ("Offene Bestellungen aus Sage", "Dienst: Lauf „Offene Bestellungen (Sage)“; Lager → Offene Bestellungen, Wareneingang zu Bestellung", "AKE (IDEAL ohne SData-Endpunkt aus lassen)", null),
        ["Sync:BomCacheEnabled"] = ("Stücklisten-Cache", "Dienst: Lauf „BOM-Cache“; Stückliste", "AKE", null),
        ["Sync:BomCacheWeeks"] = ("Stücklisten-Cache: Wochen voraus", "Dienst: Lauf „BOM-Cache“", "AKE", null),
        ["Sync:BomCacheMaxOrders"] = ("Stücklisten-Cache: max. Aufträge", "Dienst: Lauf „BOM-Cache“", "AKE", null),
        ["Sync:BomCacheMaxAgeHours"] = ("Stücklisten-Cache: max. Alter", "Dienst: Lauf „BOM-Cache“", "AKE", null),
        ["Sync:FaWorkStepDetectionEnabled"] = ("FA-Arbeitsgang-Erkennung", "Dienst: nach dem BOM-Cache; FA-Vervollständigung", "AKE", null),
        ["Sync:CoatingDetectionEnabled"] = ("Lackierteil-Erkennung", "Dienst: Lauf „Lackierteile“", null, null),
        ["Sync:BdeAutoPauseIntervalMinutes"] = ("BDE-Auto-Pause: Intervall", "Dienst: BDE-Auto-Pause am Schichtende", null, null),
        ["Sync:FeiertagSyncEnabled"] = ("Feiertage abgleichen", "Dienst: Lauf „Feiertage“", null, null),
        ["Sync:FeiertagCountryCode"] = ("Feiertage: Land", "Dienst: Lauf „Feiertage“", null, null),
        ["Sync:FeiertagRegion"] = ("Feiertage: Region", "Dienst: Lauf „Feiertage“", null, null),
        ["Sync:FeiertagJahreVoraus"] = ("Feiertage: Jahre voraus", "Dienst: Lauf „Feiertage“", null, null),
        ["Cleanup:AktivitaetsprotokollAufbewahrungTage"] = ("Aktivitäts-Protokoll: Aufbewahrung", "Dienst: tägliche Bereinigung", null, null),
        ["WorkerSettings:SyncIntervalMinutes"] = ("Sync-Intervall", "Dienst: alle Sync-Läufe", null, null),
        ["WorkerSettings:NotificationCheckIntervalMinutes"] = ("Meldebestand-Prüfung: Intervall", "Dienst: Meldebestand-Prüfung", null, null),
        ["WorkerSettings:SyncDryRun"] = ("Probelauf (nichts schreiben)", "Dienst: alle Sync-Läufe", null, null),
        ["ErrorNotification:Enabled"] = ("Fehlermails", "Dienst: Fehlermail bei Sync-Fehlern", null, "Versendet Mails."),
        ["ErrorNotification:Recipients"] = ("Fehlermail: Empfänger", "Dienst: Fehlermail", null, null),
        ["Notifications:MeldebestandEnabled"] = ("Meldebestand-Mail", "Dienst: Meldebestand-Prüfung", null, "Versendet Mails."),
        ["Notifications:MeldebestandSubject"] = ("Meldebestand-Mail: Betreff", "Dienst: Meldebestand-Prüfung", null, null),
        ["Notifications:Recipients"] = ("Meldebestand-Mail: Empfänger", "Dienst: Meldebestand-Prüfung", null, null),
        ["Notifications:AppBaseUrl"] = ("App-Adresse für Links in Mails", "Dienst: Mails mit Links", null, null),
        // v1.99.10 Lieferverzug-Mail (Spec 2026-11-12-offene-bestellungen-mahnmail-lieferverzug): beide Standorte, Werte je Installation;
        // wirkt nur mit Daten aus Sync:EinkaufBestellungenEnabled (IDEAL bis zur SData-Schnittstelle aus).
        ["Lieferverzug:MailAktiv"] = ("Lieferverzug-Mails", LvVerwendung, null, "Versendet Mails, bei abgeschaltetem Testbetrieb auch an Lieferanten."),
        ["Lieferverzug:TageNachTermin"] = ("Tage nach Liefertermin", LvVerwendung, null, null),
        ["Lieferverzug:Uhrzeit"] = ("Versand ab (Uhr)", LvVerwendung, null, null),
        ["Lieferverzug:EinkaufAktiv"] = ("Sammelliste an den Einkauf", LvVerwendung, null, "Versendet Mails."),
        ["Lieferverzug:EinkaufEmpfaenger"] = ("Einkauf: Empfänger", LvVerwendung, null, null),
        ["Lieferverzug:KreditorAktiv"] = ("Erinnerung an Kreditoren", LvVerwendung, null, "Versendet Mails an externe Lieferanten (nicht rückholbar), sofern der Testbetrieb aus ist."),
        ["Lieferverzug:Testbetrieb"] = ("Testbetrieb (Kreditor-Mails an den Einkauf)", LvVerwendung, null, "Ausschalten sendet ab dem nächsten Versandtag an externe Lieferanten. Vorher Mail-Relay für externe Empfänger und Absender mit der IT klären."),
        ["Lieferverzug:KreditorWiederholTage"] = ("Kreditor: erneut erinnern nach", LvVerwendung, null, null),
        ["Lieferverzug:KreditorMaxJeLauf"] = ("Kreditor: max. Mails je Lauf", LvVerwendung + " (Sicherheits-Grenze)", null, null),
        ["Lieferverzug:KreditorAbsender"] = ("Absenderadresse Kreditor-Mail", LvVerwendung, null, "Absender externer Mails: der SMTP-Relay muss diese Adresse für externe Empfänger zulassen (IT)."),
        ["Lieferverzug:AntwortAdresse"] = ("Antwortadresse Kreditor-Mail", LvVerwendung, null, null),
        ["Lieferverzug:Standort"] = ("Absender-Firma (CI)", LvVerwendung, null, null),
    };

    private const string LvVerwendung = "Dienst: Lauf „Lieferverzug-Mails“ nach „Offene Bestellungen (Sage)“";

    /// <summary>v1.99.10: Auswahl Absender-Firma der Lieferverzug-Mails (Hausfarbe, Firmenadresse); exakte Schreibweise wie im Dienst.</summary>
    public static readonly (string Wert, string Text)[] LieferverzugStandortWerte =
    {
        ("AKE", "AKE Ausseer Kälte- und Edelstahltechnik GmbH (AKE Blau)"),
        ("IDEAL", "IDEAL Kältetechnik GmbH (IDEAL Blau)"),
    };

    /// <summary>Grenzen, die der Leser belegt (F1: nur dort).</summary>
    private static readonly Dictionary<string, (int? Min, int? Max, string Einheit)> IntMeta = new()
    {
        ["Sync:SageLagerbuchungIntervalSeconds"] = (5, null, "s"),
        ["Sync:SageWareneingangMaxWiederholungen"] = (0, 10, ""),
        ["Sync:SageWareneingangWiederholAbstandMinuten"] = (5, 1440, "min"),
        ["Sync:WareneingangCsvMaxWiederholungen"] = (0, 10, ""),
        ["Sync:WareneingangCsvWiederholAbstandMinuten"] = (5, 1440, "min"),
        ["Sync:BdeAutoPauseIntervalMinutes"] = (0, null, "min"),
        ["Sync:LagerbestandIntervalMinutes"] = (0, null, "min"),
        ["Lieferverzug:TageNachTermin"] = (0, 60, "Tage"),
        ["Lieferverzug:Uhrzeit"] = (0, 23, "Uhr"),
        ["Lieferverzug:KreditorWiederholTage"] = (1, 60, "Tage"),
        ["Lieferverzug:KreditorMaxJeLauf"] = (1, 500, ""),
    };

    private static SettingDefinition Ergaenzen(SettingDefinition d)
    {
        var m = Meta.TryGetValue(d.Key, out var x) ? x : (Name: d.Key, Verwendung: "Dienst", Standort: (string?)null, Warn: (string?)null);
        d = d with { Anzeigename = m.Name, Verwendung = m.Verwendung, Standort = m.Standort ?? "beide", Warnhinweis = m.Warn };
        if (d.Type == SettingType.Bool) d = d with { BoolLesart = SettingBoolLesart.LeerIstDefault };
        if (d.Type == SettingType.Int && IntMeta.TryGetValue(d.Key, out var i)) d = d with { Min = i.Min, Max = i.Max, Einheit = i.Einheit };
        if (d.Key.StartsWith("Sync:Quelle:", StringComparison.Ordinal)) d = d with { Type = SettingType.Choice, Werte = QuellenWerte };
        return d;
    }

    private static List<SettingDefinition> Basis() => new()
    {
        // ----- Sync (Master-Schalter der einzelnen Sync-Bloecke) -----
        new("Sync:ProductionOrdersEnabled",          SettingType.Bool, "true",  "Sync", "Produktionsauftraege-Sync aus SAGE aktiv"),
        new("Sync:ArticlesEnabled",                  SettingType.Bool, "true",  "Sync", "Artikel-Sync aus SAGE aktiv"),
        new("Sync:ArticlesViewName",                 SettingType.String, "",    "Sync", "Sage-View als Quelle des Artikel-Syncs (IDEAL: [vw_IDEAL_AKE_WMS_Artikel], erst nach Pruefabfrage P4). Leer = bisheriger Select (AKE). Fehlt die View: Warnung + Skip, kein Rueckfall auf den alten Select, keine Mail. Nach der 00er-Bereinigung nicht mehr leeren (der alte Select holt die 00er zurueck)."),
        new("Sync:OseonArticleCategoryEnabled",      SettingType.Bool, "false", "Sync", "OSEON-Artikelkategorie-Sync aktiv (laeuft nach Artikel-Import)"),
        new("Sync:OseonTrackingEnabled",             SettingType.Bool, "false", "Sync", "OSEON-Tracking-Sync + Werkbank-Sync aktiv"),
        new("Sync:EnaioDmsEnabled",                  SettingType.Bool, "false", "Sync", "enaio DMS-Sync aktiv"),
        new("Sync:EnaioDmsViewName",                 SettingType.String, "[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]", "Sync", "Quelle des enaio DMS-Syncs (Whitelist + QUOTENAME, bis zu drei Teile [Datenbank].[Schema].[Name]; Spalten id, angelegt, Typ, WaNummer, WaNummerTrimmed). Leer = Default. Ungueltiger Name: kein SQL, Fehler + Fehlermail. Der Default wirkt nur, wo die Zeile fehlt."),
        new("Sync:PartRequisitionEmailEnabled",      SettingType.Bool, "false", "Sync", "Bedarfsmeldungs-E-Mail-Versand aktiv"),
        new("Sync:WarehouseRequisitionEmailEnabled", SettingType.Bool, "false", "Sync", "E-Mail-Versand fuer Lagerbestellungen aktiv"),
        new("Sync:LagerplaetzeEnabled",              SettingType.Bool, "false", "Sync", "Sage-Lagerplatz-Stammdaten-Sync aktiv"),
        new("Sync:LagerbestandEnabled",              SettingType.Bool, "false", "Sync", "Sage-Lagerbestand-Sync (Bestand-Korrektur) aktiv"),
        new("Sync:LagerbestandIntervalMinutes",      SettingType.Int,  "0",     "Sync", "Eigenes Intervall (Minuten) fuer Lagerbestand-Sync (0 = Worker-Standard)"),
        new("Sync:LagerbestandNullsetzenMaxPerRun",  SettingType.Int,  "100",   "Sync", "Sicherheits-Cap: mehr in Sage verschwundene Bestand-Paare je Lauf -> kein Nullsetzen + Fehlermail"),
        new("Sync:ProductionOrderReconcileEnabled",  SettingType.Bool, "false", "Sync", "Verwaiste (in Sage geloeschte) offene FAs automatisch stornieren (Opt-in)"),
        new("Sync:ReconcileMaxCancelPerRun",         SettingType.Int,  "100",   "Sync", "Sicherheits-Cap: mehr Storno-Kandidaten je Lauf -> kein Storno + Fehlermail"),
        new("Sync:FaZusatzinfoEnabled",              SettingType.Bool, "false", "Sync", "FA-Zusatzinfos (Sage): Kaeltemittel/Ventil/Ausfuehrung/Maschine/Status je FA synchronisieren. ACHTUNG: setzt FAs mit Sage-Status verpackt/abgeholt automatisch auf Komm-Erledigt (vorher DryRun pruefen)"),
        new("Sync:FaZusatzinfoAutoDoneMaxPerRun",    SettingType.Int,  "100",   "Sync", "Sicherheits-Cap: mehr Auto-Erledigt-Kandidaten (Sage-Status verpackt/abgeholt) je Lauf -> kein Erledigt-Setzen + Warnung + Fehlermail (Schutz vor View-Defekten)"),
        new("Sync:FaZusatzinfoAutoErledigtEnabled",  SettingType.Bool, "true",  "Sync", "Auto-Erledigt (Fold 2): FAs mit Sage-Status verpackt/abgeholt automatisch auf Komm-Erledigt setzen. Default an (AKE-Verhalten). Im hierarchischen IDEAL-Modus NICHT aktivierbar (HauptFA + Sub-FAs teilen die FA-Nummer); der Master-Flip 'ProduktionsauftragHierarchisch' schaltet ihn automatisch ab. Zusaetzlich greift eine datengetriebene Sperre: bei WA-Mehrfachtreffer wird Fold 2 uebersprungen."),

        // ----- FA-Hierarchie (IDEAL: mehrstufiger Struktur-/Stuecklisten-Import aus Sage-Views) -----
        new("Sync:HierarchicalFaEnabled",            SettingType.Bool,   "false", "FA-Hierarchie", "Mehrstufiger FA-Struktur-/Stuecklisten-Import (IDEAL) aus den Sage-Views FAListe/FAInfos aktiv (Full-Refresh in FaHierarchyNode/FaHierarchyOrderInfo)"),
        new("Sync:FaHierarchyListeViewName",         SettingType.String, "[vw_IDEAL_AKE_WMS_FAListe]", "FA-Hierarchie", "Name der Sage-View mit der Stueckliste je Struktur (Whitelist + QUOTENAME abgesichert). Format [Schema].[Name] oder [Name]. Fehlt die View: Fehler + Fehlermail. Der Default wirkt nur bei Neuinstallation, bestehende Werte von Hand umstellen."),
        new("Sync:FaHierarchyInfosViewName",         SettingType.String, "[vw_IDEAL_AKE_WMS_FAInfos]", "FA-Hierarchie", "Name der Sage-View mit den PPS-/Auftragsdaten je HauptFA (Whitelist + QUOTENAME abgesichert). Format [Schema].[Name] oder [Name]. Fehlt die View: Fehler + Fehlermail. Der Default wirkt nur bei Neuinstallation, bestehende Werte von Hand umstellen."),
        new("ProduktionsauftragHierarchisch",        SettingType.Bool,   "false", "FA-Hierarchie", "EINWEGTOR: schaltet die Materialisierung der Struktur nach ProductionOrders scharf (Sub-FAs werden rueckmeldefaehig). Nach dem ersten hierarchischen Import nicht mehr ueber die Anwendung deaktivierbar. Umschalten NUR ueber die Seite 'FA-Hierarchie-Umstellung' — hier read-only."),
        new("Sync:WorkOperationStructureDetectionEnabled", SettingType.Bool, "false", "FA-Hierarchie", "IDEAL: WorkOperation-Arbeitsgang-Erkennung aus der Struktur (FaHierarchyNode.Arbeitsschritte, exakt ueber ProductionWorkplace.ArbeitsschrittCode). Legt die WorkOperation-Zeilen an, gegen die das BDE-Terminal bucht. Nur bei Master 'ProduktionsauftragHierarchisch'=true, nach FA-Materialisierung und Werkbank-Anlage (Sync:ProductionWorkplaceSyncEnabled). Unbekannte Kuerzel gemeldet, ausgeschlossene still, mehrfache bevorzugt angelegt. (War Sync:FaWorkStepStructureDetectionEnabled, nie deployt.)"),
        new("Sync:ProductionWorkplaceSyncEnabled",   SettingType.Bool,   "false", "FA-Hierarchie", "IDEAL: Werkbank-Anlage aus den Sage-Arbeitsplaetzen (KHKPpsArbeitsplaetze) — legt fehlende ProductionWorkplaces an (Sage fuehrend fuer Nummer/Name/Kuerzel) und meldet Abweichungen. Laeuft NUR bei Master 'ProduktionsauftragHierarchisch'=true (Doppel-Gate). Voraussetzung fuer die Struktur-Arbeitsgang-Erkennung (Katalog der Arbeitsschritt-Kuerzel)."),
        new("Sync:ProductionWorkplaceSyncMandant",   SettingType.Int,    "1",     "FA-Hierarchie", "Mandant-Filter fuer die Sage-Arbeitsplatz-Anlage (KHKPpsArbeitsplaetze.Mandant). Nur Arbeitsplaetze dieses Mandanten werden angelegt. NICHT mit dem SData-Dataset der Sage-Lagerbuchung verwechseln — eigener, numerischer Wert."),
        new("Sync:ProductionWorkplaceSyncViewName",  SettingType.String, "KHKPpsArbeitsplaetze", "FA-Hierarchie", "IDEAL: Sage-Quelle der Werkbank-Anlage (Tabelle oder View, Whitelist + QUOTENAME). Default = bisherige Tabelle KHKPpsArbeitsplaetze; auf [vw_IDEAL_AKE_WMS_Arbeitsgaenge] erst nach Pruefabfrage P3 umstellen (gleiche Arbeitsplatznummern, sonst Neuanlagen). Die View liefert trotz ihres Namens ARBEITSPLAETZE. Fehlt die Quelle: Warnung + Skip, keine Mail."),
        new("Sync:ProductionWorkplaceSyncAusschlussliste", SettingType.String, "STO,XXX,x01,BS,BS2,FRE,AKE", "FA-Hierarchie", "Arbeitsschritt-Kuerzel, die NICHT als Werkbank angelegt werden (Statusmarker/externe Vorgaenge, kommagetrennt, getrimmt/case-insensitiv). Diese Kuerzel gelten als BEKANNT (keine Unbekannt-Meldung in der Struktur-Arbeitsgang-Erkennung)."),
        new("Sync:OperationConfigSyncEnabled",       SettingType.Bool,   "false", "FA-Hierarchie", "Arbeitsgang-Abgleich (beide Standorte) — legt fehlende Arbeitsgang-Kuerzel aus der Sage-View in den Arbeitsgang-Stammdaten an (Quelle Sage, Offset 0, nie ueberschreiben/loeschen) und kennzeichnet Eintraege der Quelle Sage/Manuell, die nicht in der View stehen (nur Hinweis). Ausschlussliste wie Werkbank-Anlage, je Standort pruefen."),
        new("Sync:OperationConfigSyncViewName",      SettingType.String, "[vw_IDEAL_AKE_WMS_Arbeitsgaenge]", "FA-Hierarchie", "Sage-View des Arbeitsgang-Abgleichs (Spalten Arbeitsgangnummer, Bezeichnung, AG; Whitelist + QUOTENAME). Default = IDEAL-View; AKE traegt die eigene View gleicher Spalten ein. Fehlt die View: Warnung + Skip, keine Mail."),
        new("Sync:OperationConfigSyncFremdsystemRelevant", SettingType.Bool, "false", "FA-Hierarchie", "Wert 'Fremdsystem-relevant' fuer Kuerzel, die der Arbeitsgang-Abgleich NEU anlegt (bestehende Eintraege nie geaendert). Wirkt nur in OSEON-Teileverfolgung/-Reporting: IDEAL false, AKE je nach Bedarf true."),

        // ----- Sage-Lagerbuchung (ausgehend: WMS -> Sage via SData) -----
        new("SageLagerbuchungAktiv",                 SettingType.Bool, "false", "Sage-Lagerbuchung", "Globaler Schalter: manuelle Ein-/Ausbuchungen an Sage melden (kumulativ zum Lagerplatz-Flag 'Sage-Buchung erlaubt')"),
        new("SageWareneingangBelegAktiv",            SettingType.Bool, "false", "Sage-Lagerbuchung", "Wareneingaenge zu Sage-Bestellungen werden beim Buchen fuer Sage vorgemerkt und vom Dienst als EIN Sage-Wareneingangsbeleg (ELL, Beleguebernahme aus der Bestellung, platzgenau, mit Lieferschein-Nr.) je WE-Buchung angelegt - NICHT zusaetzlich als Lagerbuchung-Zugang. Ziel nur Sage-Plaetze mit 'Sage-Buchung erlaubt'. Aus = Verhalten wie Teil 1 (Lagerbuchungs-Queue). Umschalten wirkt nur auf neue Buchungen. Unabhaengig von SageLagerbuchungAktiv (v1.99.4)."),
        new("Sync:SageWareneingangMaxWiederholungen", SettingType.Int, "3", "Sage-Lagerbuchung", "Sage-Wareneingang (v1.99.7): so oft wiederholt der Dienst einen Wareneingang mit Status 'Fehler' automatisch (0 = nie, nur 'Erneut senden'). Vor JEDEM Versuch sucht er den Beleg in Sage ueber die Memo-Kennung WE#<Id>#: gefunden = an Sage uebergeben ohne Senden; moeglicher Beleg ohne Kennung (Bestellung + Datum + Lieferschein) = 'Klaeren', kein Senden. Danach bleibt 'Fehler'."),
        new("Sync:SageWareneingangWiederholAbstandMinuten", SettingType.Int, "15", "Sage-Lagerbuchung", "Sage-Wareneingang (v1.99.7): Mindestabstand in Minuten zwischen einem Fehler und der automatischen Wiederholung (5 bis 1440)."),
        new("Sync:SageSystemKuerzel", SettingType.String, "", "Sage-Lagerbuchung", "System-Kuerzel in den Memo-Kennungen an Sage (v1.99.9): leer = WE#<Id># bzw. SM#<Id># (Produktion), sonst WE#<Kuerzel>#<Id># bzw. SM#<Kuerzel>#<Id>#. Damit findet die Suche vor dem erneuten Senden keine Belege anderer WMS-Datenbanken in derselben Sage-Testdatenbank. Gilt nur fuer neue Sendungen.",
            Muster: (IdealAkeWms.Services.SageSystemKuerzel.Muster, IdealAkeWms.Services.SageSystemKuerzel.MusterText)),

        // ----- Wareneingang-CSV (v1.99.11, Spec 2026-11-12-wareneingang-barcode-lieferschein-csv-export) -----
        new("WareneingangCsvAktiv", SettingType.Bool, "false", WeCsvKategorie, "An: Fuer jeden an Sage uebergebenen Wareneingang mit Barcode schreibt der Dienst <Barcode>.csv auf den Pfad 'Wareneingang-CSV: Pfad', zuerst als .tmp, dann umbenannt; nie ueberschrieben. Nur bei eingeschaltetem Sage-Wareneingang, sonst wartet die Uebergabe (Hinweis). Aus: nichts wird geschrieben, die Uebergabe wartet. Beim Einschalten werden wartende Vorgaenge nachgeholt (v1.99.11).",
            AbhaengigVon: SageWareneingang.ToggleKey),
        new("WareneingangCsvPfad", SettingType.String, "", WeCsvKategorie, "Zielordner der CSV-Dateien als UNC-Pfad (\\\\server\\freigabe\\ordner), Dateien direkt darin. Leer = nichts schreiben (Hinweis). Gespeichert wird nur die Form, die Erreichbarkeit prueft erst der Dienst (v1.99.11).",
            Muster: (WareneingangCsv.PfadMuster, WareneingangCsv.PfadMusterText), AbhaengigVon: "WareneingangCsvAktiv"),
        new("Sync:WareneingangCsvMaxWiederholungen", SettingType.Int, "3", WeCsvKategorie, "So oft schreibt der Dienst eine CSV nach einem Schreibfehler (Netz, Recht) automatisch erneut (0 = nie, nur 'CSV erneut schreiben'). Eine bereits vorhandene Datei wird nie automatisch wiederholt (v1.99.11)."),
        new("Sync:WareneingangCsvWiederholAbstandMinuten", SettingType.Int, "15", WeCsvKategorie, "Mindestabstand in Minuten zwischen einem Schreibfehler und der automatischen Wiederholung (5 bis 1440, v1.99.11)."),
        new("SageLagerbuchungUmbuchungAktiv",        SettingType.Bool, "false", "Sage-Lagerbuchung", "WMS-Umbuchungen an Sage melden (v1.73.0, kumulativ zu SageLagerbuchungAktiv): Sage-Platz -> Sage-Platz als Umbuchung, Sage -> WMS-only als Entnahme, WMS-only -> Sage als Zugang, WMS-only -> WMS-only nie. Default aus = nur Ein-/Ausbuchungen wie bisher."),
        new("SData:BaseUrl",                         SettingType.String, "",    "Sage-Lagerbuchung", "Host+Port der Sage-SData-API (z.B. https://sagetest01.ake.at:5493). Ein angehaengtes /sdata ist erlaubt (wird nicht verdoppelt). Ziel-URL = {BaseUrl}/sdata/{Application}/{ServiceContract}/{Dataset}/$service/LagerbuchungService"),
        new("SData:Application",                     SettingType.String, "ol",  "Sage-Lagerbuchung", "SData-Application-Segment (per SData-Discovery ermittelt, i.d.R. konstant 'ol')"),
        new("SData:ServiceContract",                 SettingType.String, "CommonWawiServices", "Sage-Lagerbuchung", "SData-ServiceContract-Segment (i.d.R. konstant 'CommonWawiServices')"),
        new("SData:Dataset",                         SettingType.String, "",    "Sage-Lagerbuchung", "Mandant/dataset-Segment der SData-URL (pro Standort/Instanz, z.B. ake_TEST2026;1 — Semikolon literal, NICHT kodieren)"),
        new("Sync:SageLagerbuchungIntervalSeconds",  SettingType.Int,  "20",    "Sage-Lagerbuchung", "Poll-Intervall (Sekunden) des SageBookingWorker fuer offene Queue-Eintraege ('fast live')"),
        new("Sync:SageLagerbuchungBatchSize",        SettingType.Int,  "50",    "Sage-Lagerbuchung", "Maximale Anzahl Queue-Eintraege, die je Worker-Tick verarbeitet werden"),
        new("Sync:SageLagerbuchungMaxRetries",       SettingType.Int,  "5",     "Sage-Lagerbuchung", "Maximale automatische Sende-Versuche je Eintrag; danach bleibt er auf 'Fehler' und braucht ein manuelles Requeue"),
        new("Sync:SageLagerbuchungMaxErrorsPerRun",  SettingType.Int,  "50",    "Sage-Lagerbuchung", "Sicherheits-Cap: mehr Fehler je Lauf -> Fehlermail (ISyncErrorNotifier)"),
        new("Sync:SageLagerbuchungStuckMinutes",     SettingType.Int,  "10",    "Sage-Lagerbuchung", "Ein 'Gesendet'-Eintrag aelter als X Minuten gilt als haengend und wird ueber den Sage-Memo-Lookup aufgeloest (nie blind neu gesendet)"),
        new("SageLagerbuchungSslZertifikatPruefen",  SettingType.Bool, "true",  "Sage-Lagerbuchung", "TLS-Zertifikat des Sage-SData-Servers pruefen. NUR fuer Testsysteme mit ungueltigem Zertifikat auf 'false' setzen (analog Postman 'Enable SSL certificate verification'). Wirkt nur auf die Sage-SData-Clients (Lagerbuchung + FA-Ansicht Sage live + Stammdaten-Abgleich). Fehlt/unparsebar -> geprueft (fail-safe)."),

        // ----- Sage-Abgleich ueber SData (Spec 2026-11-06-sage-sdata-stammdaten-sync, v1.73.0) -----
        // Default View bzw. false = Verhalten unveraendert. Endpunkt/Zugang wie Sage-Lagerbuchung (SData:*, appsettings SageLagerbuchung).
        new("Sync:Quelle:Lagerplaetze", SettingType.String, "View", "Sage-Abgleich (SData)", "Quelle des Lagerplatz-Abgleichs: View (Default, bisheriger SQL-Weg) | SData | Vergleich (liest beide, schreibt nur aus der View, Abweichungen im Protokoll \"Sage-Quellenvergleich\"). Unbekannter Wert = View + Warnung. Exakte Schreibweise."),
        new("Sync:Quelle:Lagerbestand", SettingType.String, "View", "Sage-Abgleich (SData)", "Quelle des Lagerbestand-Abgleichs: View (Default, bisheriger SQL-Weg) | SData | Vergleich (liest beide, schreibt nur aus der View, Abweichungen im Protokoll \"Sage-Quellenvergleich\"). Unbekannter Wert = View + Warnung. Exakte Schreibweise."),
        new("Sync:Quelle:Artikel", SettingType.String, "View", "Sage-Abgleich (SData)", "Quelle des Artikel-Abgleichs: View (Default, bisheriger SQL-Weg) | SData | Vergleich (liest beide, schreibt nur aus der View, Abweichungen im Protokoll \"Sage-Quellenvergleich\"). Unbekannter Wert = View + Warnung. Exakte Schreibweise. SData liest an AKE dieselbe Menge wie der AKE-SQL-Filter und fuellt Sage-Details + Benutzerfeld-Merkmale; ist Sync:ArticlesViewName gesetzt (IDEAL), ist SData bis Pruefabfrage P-R1 gesperrt und wirkt wie View (Warnung)."),
        new("Sync:Quelle:FaStruktur", SettingType.String, "View", "Sage-Abgleich (SData)", "Quelle der FA-Struktur (nur Kanten/Positionen): View (Default, bisheriger SQL-Weg) | SData | Vergleich (liest beide, schreibt nur aus der View, Abweichungen im Protokoll \"Sage-Quellenvergleich\"). Unbekannter Wert = View + Warnung. Exakte Schreibweise. SData ist bis Pruefabfrage P-F2 gesperrt und wirkt wie View (Warnung)."),
        new("Sync:Quelle:Arbeitsplaetze", SettingType.String, "View", "Sage-Abgleich (SData)", "Quelle der Werkbank-Anlage und des Arbeitsgang-Abgleichs: View (Default, bisheriger SQL-Weg) | SData | Vergleich (liest beide, schreibt nur aus der View, Abweichungen im Protokoll \"Sage-Quellenvergleich\"). Unbekannter Wert = View + Warnung. Exakte Schreibweise. SData liefert zusaetzlich BDE-Terminal, Kostenstelle, Schichtplan, aber (bis Pruefabfrage P-A1) KEIN Arbeitsschritt-Kuerzel: dann keine Werkbank-Neuanlage/kein Kuerzel-Abgleich, der Arbeitsgang-Abgleich setzt mit Warnung aus."),
        new("Sync:Quelle:LagerbuchungLookup", SettingType.String, "View", "Sage-Abgleich (SData)", "Quelle des Lagerbuchungs-Lookups (Memo-Suche): View (Default, bisheriger SQL-Weg) | SData | Vergleich (liest beide, schreibt nur aus der View, Abweichungen im Protokoll \"Sage-Quellenvergleich\"). Unbekannter Wert = View + Warnung. Exakte Schreibweise."),
        new("Sync:ArtikelBenutzerfelder", SettingType.String, "USER_Maschine,USER_Ausfuehrung,USER_VaultID,USER_MasterartikelNr,USER_TypenschildArtikel,USER_Artikeltyp,USER_Produktfamilie,USER_IstIDEAL,USER_DruckFertigungsauftrag,USER_AUSCAD,USER_DPSBestand,USER_Kaeltemittel,USER_Ventil,USER_PfadGlaszeichnung,USER_PfadSchaltplan", "Sage-Abgleich (SData)", "Sage-Artikel-Benutzerfelder (kommagetrennt), die der Artikel-Abgleich ueber SData als Text-Merkmal uebernimmt. Erweiterbar ohne Code; unbekannter Feldname = Warnung."),
        new("Sync:PpsMitarbeiterEnabled", SettingType.Bool, "false", "Sage-Abgleich (SData)", "PPS-Mitarbeiter aus Sage (SData) lesen; Zuordnung zu BDE-Mitarbeitern nur manuell."),
        new("Sync:StillstandsgruendeEnabled", SettingType.Bool, "false", "Sage-Abgleich (SData)", "Stillstandsbegruendungen aus Sage (SData) lesen."),
        new("Sync:KreditorenEnabled", SettingType.Bool, "false", "Sage-Abgleich (SData)", "Lieferanten aus Sage (SData, Kontokorrent + Adressen/Ansprechpartner) in die Kreditoren lesen. Sage ist fuehrend: ueberschreibt Name, Matchcode, Adresse, Kontakt und aktiv/inaktiv im WMS; Kurzname, Beschichter, Bemerkung bleiben. Liest nur, schreibt nie nach Sage."),
        new("Sync:DebitorenEnabled", SettingType.Bool, "false", "Sage-Abgleich (SData)", "Kunden aus Sage (SData, Kontokorrent + Adressen/Ansprechpartner) in die Debitoren lesen. Sage ist fuehrend: ueberschreibt Name, Matchcode, Adresse, Kontakt, aktiv/inaktiv, Vertreter und Kundenname FA im WMS; Kurzname und Bemerkung bleiben. Liest nur, schreibt nie nach Sage."),
        new("Sync:ArtikelLieferantEnabled", SettingType.Bool, "false", "Sage-Abgleich (SData)", "Artikel-Lieferanten aus Sage (SData) lesen (braucht Artikel und Kreditoren)."),
        new("Sync:EinkaufBestellungenEnabled", SettingType.Bool, "false", "Sage-Abgleich (SData)", "Offene Einkaufsbestellungen (nur normale Bestellungen EBB, Positionen) aus Sage (SData) in den Cache lesen; Zuordnung zu Artikel, Kreditor und Lagerplatz (braucht Kreditoren- und Lagerplatz-Abgleich). Liest nur, schreibt nie nach Sage."),

        // ----- BOM-Cache -----
        new("Sync:BomCacheEnabled",                  SettingType.Bool, "false", "BOM-Cache", "BOM-Cache-Sync aktiv (Top-N offene Auftraege werden gecacht)"),
        new("Sync:BomCacheWeeks",                    SettingType.Int,  "8",     "BOM-Cache", "Wieviele Wochen Fertigungstermin in die Zukunft cachen"),
        new("Sync:BomCacheMaxOrders",                SettingType.Int,  "200",   "BOM-Cache", "Maximalanzahl Auftraege im BOM-Cache"),
        new("Sync:BomCacheMaxAgeHours",              SettingType.Int,  "24",    "BOM-Cache", "Sicherheitsnetz: Re-Sync wenn Cache-Eintrag aelter als X Stunden"),

        // ----- FA-Vervollstaendigung -----
        new("Sync:FaWorkStepDetectionEnabled",       SettingType.Bool, "false", "FA-Vervollstaendigung", "Automatische FA-Arbeitsgang-Erkennung aus dem BOM-Cache (laeuft nach BomCache-Sync)"),

        // ----- Lackierteile -----
        new("Sync:CoatingDetectionEnabled",          SettingType.Bool, "false", "Lackierteile", "Lackierteil-Erkennung als separater Sync-Job aktiv"),

        // ----- BDE / Feiertage -----
        new("Sync:BdeAutoPauseIntervalMinutes",      SettingType.Int,  "60",    "BDE", "Intervall (Minuten) fuer BDE-Auto-Pause am Schichtende"),
        new("Sync:FeiertagSyncEnabled",              SettingType.Bool, "false", "Feiertage", "Feiertags-Sync aus Nager.Date aktiv"),
        new("Sync:FeiertagCountryCode",              SettingType.String, "AT",  "Feiertage", "Laendercode fuer Feiertags-Sync (ISO-3166 alpha-2, z.B. AT, DE)"),
        new("Sync:FeiertagRegion",                   SettingType.String, "",    "Feiertage", "Optionale Region fuer Feiertags-Sync (z.B. AT-3 fuer Niederoesterreich)"),
        new("Sync:FeiertagJahreVoraus",              SettingType.Int,  "2",     "Feiertage", "Anzahl Folgejahre, die Feiertage vorausgesynct werden"),

        // ----- Bereinigung (Cleanup-Jobs) -----
        new("Cleanup:AktivitaetsprotokollAufbewahrungTage", SettingType.Int, "180", "Bereinigung", "Aktivitaets-Protokoll: Eintraege aelter als X Tage werden taeglich geloescht (0 = nie loeschen)"),

        // ----- Worker -----
        new("WorkerSettings:SyncIntervalMinutes",    SettingType.Int,  "15",    "Worker", "Sync-Intervall (Minuten) fuer den SyncWorker"),
        new("WorkerSettings:NotificationCheckIntervalMinutes", SettingType.Int, "60", "Worker", "Intervall (Minuten) fuer die Meldebestand-Pruefung (NotificationWorker)"),
        new("WorkerSettings:SyncDryRun",             SettingType.Bool, "false", "Worker", "DryRun-Modus: kein Schreiben, nur Simulation"),

        // ----- Fehlermail -----
        new("ErrorNotification:Enabled",             SettingType.Bool, "false", "Fehlermail", "Fehlermail-Versand bei Sync-Fehlern aktiv"),
        new("ErrorNotification:Recipients",          SettingType.String, "",    "Fehlermail", "Empfaenger-Liste (kommagetrennt) fuer Sync-Fehlermails", Multiline: true),

        // ----- Benachrichtigungen (Meldebestand-Mail) -----
        new("Notifications:MeldebestandEnabled",     SettingType.Bool, "true",  "Benachrichtigungen", "Meldebestand-Mail aktiv"),
        new("Notifications:MeldebestandSubject",     SettingType.String, "Meldebestand unterschritten — IDEAL AKE WMS", "Benachrichtigungen", "Betreff der Meldebestand-Mail"),
        new("Notifications:Recipients",              SettingType.String, "",    "Benachrichtigungen", "Feste Empfaenger fuer Meldebestand-Mail (kommagetrennt, z.B. lager@ake.at,leitung@ake.at)", Multiline: true),
        new("Notifications:AppBaseUrl",              SettingType.String, "",    "Benachrichtigungen", "Basis-URL der App fuer Links in Mails (z.B. https://wms.ake.at)"),

        // ----- Lieferverzug-Mail (v1.99.10, Spec 2026-11-12-offene-bestellungen-mahnmail-lieferverzug) -----
        new("Lieferverzug:MailAktiv", SettingType.Bool, "false", LvKategorie, "Hauptschalter: Der Dienst prueft einmal am Tag (Mo-Fr ab der Uhrzeit unten) die offenen Bestellpositionen aus dem Cache 'Offene Bestellungen (Sage)' auf Lieferverzug und versendet die Mails nach den Einstellungen unten. Aus = kein Lauf. Ohne Bestell-Abgleich wirkungslos."),
        new("Lieferverzug:TageNachTermin", SettingType.Int, "3", LvKategorie, "Eine Position gilt als verzoegert, wenn heute mindestens so viele Kalendertage nach ihrem Liefertermin liegt und noch ein Rest offen ist (Rest wie in 'Wareneingang zu Bestellung'). Positionen ohne Liefertermin nie."),
        new("Lieferverzug:Uhrzeit", SettingType.Int, "7", LvKategorie, "Versand im ersten Sync-Zyklus ab dieser Stunde, nur Montag bis Freitag (Feiertage werden nicht beruecksichtigt). Hoechstens einmal am Tag, auch nach einem Dienst-Neustart."),
        new("Lieferverzug:EinkaufAktiv", SettingType.Bool, "true", LvKategorie, "Taegliche Sammelliste aller verzoegerten Positionen, gruppiert nach Kreditor mit Status der Kreditor-Mail, an die Einkauf-Empfaenger. Nur wenn mindestens eine Position verzoegert ist."),
        new("Lieferverzug:EinkaufEmpfaenger", SettingType.String, "", LvKategorie, "Empfaenger der Sammelliste UND im Testbetrieb aller umgeleiteten Kreditor-Mails (Adressen durch Komma, Semikolon oder Zeilenumbruch getrennt; jede wird geprueft). Leer = keine Mail, Warnung im Lauf.",
            Multiline: true, Mail: SettingMail.Liste),
        new("Lieferverzug:KreditorAktiv", SettingType.Bool, "false", LvKategorie, "Erinnerung je Kreditor mit seinen verzoegerten Positionen an die Adresse fuer Erinnerungen bzw. die E-Mail aus dem Kreditor-Stamm. Keine Mail bei gesperrtem Lieferanten, inaktivem/fehlendem Kreditor, 'keine Erinnerungsmails' oder ungueltiger Adresse (Grund in der Sammelliste)."),
        new("Lieferverzug:Testbetrieb", SettingType.Bool, "true", LvKategorie, "An: Jede Kreditor-Mail geht unveraendert an die Einkauf-Empfaenger, Betreff und Kopf 'TESTBETRIEB - waere an ... gegangen'. Aus: Mails gehen an die Kreditoren (Grenze je Lauf beachten)."),
        new("Lieferverzug:KreditorWiederholTage", SettingType.Int, "7", LvKategorie, "Abstand in Tagen, in dem eine weiterhin verzoegerte Position dem Kreditor erneut gemeldet wird. Ein geaenderter Liefertermin beginnt neu. Testbetrieb-Mails zaehlen nur fuer den Testbetrieb."),
        new("Lieferverzug:KreditorMaxJeLauf", SettingType.Int, "30", LvKategorie, "Sicherheitsgrenze: Waeren mehr echte Kreditor-Mails faellig, geht keine davon raus (Warnung und Fehlermail). Gilt nicht im Testbetrieb. Schuetzt vor der ersten Mailwelle nach dem Abschalten des Testbetriebs."),
        new("Lieferverzug:KreditorAbsender", SettingType.String, "", LvKategorie, "Absenderadresse der Kreditor-Mails (z. B. die Einkauf-Adresse). Leer = MailSettings:FromAddress aus appsettings. Die Einkauf-Sammelliste kommt immer von MailSettings:FromAddress.",
            Mail: SettingMail.Eine),
        new("Lieferverzug:AntwortAdresse", SettingType.String, "", LvKategorie, "Antwortadresse (Reply-To) der Kreditor-Mails, damit Antworten beim Einkauf landen. Leer = erste Einkauf-Empfaenger-Adresse.",
            Mail: SettingMail.Eine),
        new("Lieferverzug:Standort", SettingType.Choice, "AKE", LvKategorie, "Absender-Firma der Mails: bestimmt Hausfarbe, Firmenadresse und Signatur (AKE Blau bzw. IDEAL Blau). Je Installation setzen.",
            Werte: LieferverzugStandortWerte),
    };

    private const string LvKategorie = "Lieferverzug-Mail";
    private const string WeCsvKategorie = "Wareneingang-CSV";

    /// <summary>Findet eine Definition per Key (case-sensitiv, wie DB-Key).</summary>
    public static bool TryGet(string key, [NotNullWhen(true)] out SettingDefinition? def)
    {
        def = All.FirstOrDefault(d => d.Key == key);
        return def is not null;
    }
}
