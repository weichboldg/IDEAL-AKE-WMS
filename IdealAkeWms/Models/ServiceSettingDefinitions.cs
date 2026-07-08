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
    public static IReadOnlyList<ServiceSettingDefinition> All { get; } = new List<ServiceSettingDefinition>
    {
        // ----- Sync (Master-Schalter der einzelnen Sync-Bloecke) -----
        new("Sync:ProductionOrdersEnabled",          ServiceSettingType.Bool, "true",  "Sync", "Produktionsauftraege-Sync aus SAGE aktiv"),
        new("Sync:ArticlesEnabled",                  ServiceSettingType.Bool, "true",  "Sync", "Artikel-Sync aus SAGE aktiv"),
        new("Sync:OseonArticleCategoryEnabled",      ServiceSettingType.Bool, "false", "Sync", "OSEON-Artikelkategorie-Sync aktiv (laeuft nach Artikel-Import)"),
        new("Sync:OseonTrackingEnabled",             ServiceSettingType.Bool, "false", "Sync", "OSEON-Tracking-Sync + Werkbank-Sync aktiv"),
        new("Sync:EnaioDmsEnabled",                  ServiceSettingType.Bool, "false", "Sync", "enaio DMS-Sync aktiv"),
        new("Sync:PartRequisitionEmailEnabled",      ServiceSettingType.Bool, "false", "Sync", "Bedarfsmeldungs-E-Mail-Versand aktiv"),
        new("Sync:WarehouseRequisitionEmailEnabled", ServiceSettingType.Bool, "false", "Sync", "E-Mail-Versand fuer Lagerbestellungen aktiv"),
        new("Sync:LagerplaetzeEnabled",              ServiceSettingType.Bool, "false", "Sync", "Sage-Lagerplatz-Stammdaten-Sync aktiv"),
        new("Sync:LagerbestandEnabled",              ServiceSettingType.Bool, "false", "Sync", "Sage-Lagerbestand-Sync (Bestand-Korrektur) aktiv"),
        new("Sync:LagerbestandIntervalMinutes",      ServiceSettingType.Int,  "0",     "Sync", "Eigenes Intervall (Minuten) fuer Lagerbestand-Sync (0 = Worker-Standard)"),
        new("Sync:ProductionOrderReconcileEnabled",  ServiceSettingType.Bool, "false", "Sync", "Verwaiste (in Sage geloeschte) offene FAs automatisch stornieren (Opt-in)"),
        new("Sync:ReconcileMaxCancelPerRun",         ServiceSettingType.Int,  "100",   "Sync", "Sicherheits-Cap: mehr Storno-Kandidaten je Lauf -> kein Storno + Fehlermail"),

        // ----- BOM-Cache -----
        new("Sync:BomCacheEnabled",                  ServiceSettingType.Bool, "false", "BOM-Cache", "BOM-Cache-Sync aktiv (Top-N offene Auftraege werden gecacht)"),
        new("Sync:BomCacheWeeks",                    ServiceSettingType.Int,  "8",     "BOM-Cache", "Wieviele Wochen Fertigungstermin in die Zukunft cachen"),
        new("Sync:BomCacheMaxOrders",                ServiceSettingType.Int,  "200",   "BOM-Cache", "Maximalanzahl Auftraege im BOM-Cache"),
        new("Sync:BomCacheMaxAgeHours",              ServiceSettingType.Int,  "24",    "BOM-Cache", "Sicherheitsnetz: Re-Sync wenn Cache-Eintrag aelter als X Stunden"),

        // ----- FA-Vervollstaendigung -----
        new("Sync:FaWorkStepDetectionEnabled",       ServiceSettingType.Bool, "false", "FA-Vervollstaendigung", "Automatische FA-Arbeitsgang-Erkennung aus dem BOM-Cache (laeuft nach BomCache-Sync)"),

        // ----- Lackierteile -----
        new("Sync:CoatingDetectionEnabled",          ServiceSettingType.Bool, "false", "Lackierteile", "Lackierteil-Erkennung als separater Sync-Job aktiv"),

        // ----- BDE / Feiertage -----
        new("Sync:BdeAutoPauseIntervalMinutes",      ServiceSettingType.Int,  "60",    "BDE", "Intervall (Minuten) fuer BDE-Auto-Pause am Schichtende"),
        new("Sync:FeiertagSyncEnabled",              ServiceSettingType.Bool, "false", "Feiertage", "Feiertags-Sync aus Nager.Date aktiv"),
        new("Sync:FeiertagCountryCode",              ServiceSettingType.String, "AT",  "Feiertage", "Laendercode fuer Feiertags-Sync (ISO-3166 alpha-2, z.B. AT, DE)"),
        new("Sync:FeiertagRegion",                   ServiceSettingType.String, "",    "Feiertage", "Optionale Region fuer Feiertags-Sync (z.B. AT-3 fuer Niederoesterreich)"),
        new("Sync:FeiertagJahreVoraus",              ServiceSettingType.Int,  "2",     "Feiertage", "Anzahl Folgejahre, die Feiertage vorausgesynct werden"),

        // ----- Worker -----
        new("WorkerSettings:SyncIntervalMinutes",    ServiceSettingType.Int,  "15",    "Worker", "Sync-Intervall (Minuten) fuer den SyncWorker"),
        new("WorkerSettings:NotificationCheckIntervalMinutes", ServiceSettingType.Int, "60", "Worker", "Intervall (Minuten) fuer die Meldebestand-Pruefung (NotificationWorker)"),
        new("WorkerSettings:SyncDryRun",             ServiceSettingType.Bool, "false", "Worker", "DryRun-Modus: kein Schreiben, nur Simulation"),

        // ----- Fehlermail -----
        new("ErrorNotification:Enabled",             ServiceSettingType.Bool, "false", "Fehlermail", "Fehlermail-Versand bei Sync-Fehlern aktiv"),
        new("ErrorNotification:Recipients",          ServiceSettingType.String, "",    "Fehlermail", "Empfaenger-Liste (kommagetrennt) fuer Sync-Fehlermails", Multiline: true),

        // ----- Benachrichtigungen (Meldebestand-Mail) -----
        new("Notifications:MeldebestandEnabled",     ServiceSettingType.Bool, "true",  "Benachrichtigungen", "Meldebestand-Mail aktiv"),
        new("Notifications:MeldebestandSubject",     ServiceSettingType.String, "Meldebestand unterschritten — IDEAL AKE WMS", "Benachrichtigungen", "Betreff der Meldebestand-Mail"),
        new("Notifications:Recipients",              ServiceSettingType.String, "",    "Benachrichtigungen", "Feste Empfaenger fuer Meldebestand-Mail (kommagetrennt, z.B. lager@ake.at,leitung@ake.at)", Multiline: true),
        new("Notifications:AppBaseUrl",              ServiceSettingType.String, "",    "Benachrichtigungen", "Basis-URL der App fuer Links in Mails (z.B. https://wms.ake.at)"),
    };

    /// <summary>Findet eine Definition per Key (case-sensitiv, wie DB-Key).</summary>
    public static bool TryGet(string key, [NotNullWhen(true)] out ServiceSettingDefinition? def)
    {
        def = All.FirstOrDefault(d => d.Key == key);
        return def is not null;
    }
}
