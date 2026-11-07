using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
    public static IReadOnlyList<SettingDefinition> All { get; } = new List<SettingDefinition>
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
    };

    /// <summary>Findet eine Definition per Key (case-sensitiv, wie DB-Key).</summary>
    public static bool TryGet(string key, [NotNullWhen(true)] out SettingDefinition? def)
    {
        def = All.FirstOrDefault(d => d.Key == key);
        return def is not null;
    }
}
