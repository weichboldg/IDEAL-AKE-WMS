namespace IdealAkeWms.Models.Standort;

public enum StandortFieldType { String, Int, Bool }

/// <summary>Ein editierbares Feld der Standorteinstellungen-Maske (kuratiert, kein Master).</summary>
public sealed record StandortField(
    string Key,
    string Label,
    StandortFieldType Type,
    StandortBackend Backend,
    string Group,
    string? Hint = null,
    int? Min = null,
    int? Max = null,
    bool EmptyResetsToDefault = false);

public enum StandortBackend { App, Service }

/// <summary>
/// IDEAL Teil 6: die EINE kuratierte Feldliste der Standorteinstellungen-Maske (Spec: „eigene,
/// explizite Feldliste"). Bewusst OHNE den Master-Key <c>ProduktionsauftragHierarchisch</c> — der ist
/// read-only (nur Anzeige, siehe Controller) und wird nie über diese Maske geschrieben (AK 2). Ein
/// Key, der hier nicht steht, wird beim POST ignoriert (Allow-List = Schutz gegen manipulierte
/// Requests auf den Master).
/// </summary>
public static class StandortSettingsCatalog
{
    public const string GroupFirmendaten = "Firmendaten";
    public const string GroupMandantViews = "Mandant / Sage-Views";
    public const string GroupToggles = "Feature-Schalter";
    public const string GroupImport = "Struktur-Import";
    public const string GroupWerkbaenke = "Werkbänke (Sage-Arbeitsplätze)";
    public const string GroupHardwareScanner = "Hardware-Scanner";
    public const string GroupVault = "Vault (Zeichnungen)";
    public const string GroupEnaio = "enaio (Dokumente)";

    private const string ViewNameHint =
        "Wird roh gespeichert; ein ungültiger Name fällt erst beim nächsten Sync-Lauf als Fehlermail auf.";

    public static readonly IReadOnlyList<StandortField> Fields = new List<StandortField>
    {
        // Firmendaten — neue AppSettings-Keys (Definitionsort AppSettingKeys.cs, Fallback-Regel).
        new(AppSettingKeys.Firmenname,      "Firmenname",            StandortFieldType.String, StandortBackend.App,     GroupFirmendaten),
        new(AppSettingKeys.Firmenanschrift, "Anschrift / Adresse",   StandortFieldType.String, StandortBackend.App,     GroupFirmendaten),
        new(AppSettingKeys.BriefkopfVorlagePfad, "Briefkopf-Vorlage (Pfad)", StandortFieldType.String, StandortBackend.App, GroupFirmendaten,
            "Vollständiger Pfad zur briefkopf.html im Ordner Branding\\ des Installationsverzeichnisses. Leer oder Datei fehlt: Ersatz-Kopf aus Firmenname und Anschrift."),

        // Mandant + Sage-View-Namen (ServiceSettings).
        new("SData:Dataset",                        "Mandant / Dataset",              StandortFieldType.String, StandortBackend.Service, GroupMandantViews),
        new("Sync:FaHierarchyListeViewName",        "Sage-View FAListe (Stückliste)", StandortFieldType.String, StandortBackend.Service, GroupMandantViews, ViewNameHint),
        new("Sync:FaHierarchyInfosViewName",        "Sage-View FAInfos (PPS/Auftrag)",StandortFieldType.String, StandortBackend.Service, GroupMandantViews, ViewNameHint),
        new("Sync:ArticlesViewName",                "Sage-View Artikel (leer = bisheriger Select)", StandortFieldType.String, StandortBackend.Service, GroupMandantViews, ViewNameHint),
        new("Sync:ProductionWorkplaceSyncViewName", "Sage-Quelle Arbeitsplätze",      StandortFieldType.String, StandortBackend.Service, GroupMandantViews, ViewNameHint),
        // Arbeitsgang-Abgleich (v1.61.0): nur der View-Name hier; Schalter + Fremdsystem-relevant-Default bleiben in /ServiceSettings.
        new("Sync:OperationConfigSyncViewName",     "Sage-View Arbeitsgänge (Abgleich)", StandortFieldType.String, StandortBackend.Service, GroupMandantViews, ViewNameHint),

        // Feature-Schalter der Teile 2–5 (AppSettings).
        new(AppSettingKeys.FaHierarchyMaxDepth,                 "Baum-Maximaltiefe",             StandortFieldType.Int,  StandortBackend.App, GroupToggles),
        new(AppSettingKeys.FaHierarchyKommissionierlistenAktiv, "Kommissionierlisten aktiv",     StandortFieldType.Bool, StandortBackend.App, GroupToggles),
        new(AppSettingKeys.FaHierarchyBeschichtungAktiv,        "Beschichtungsauftrag aktiv",    StandortFieldType.Bool, StandortBackend.App, GroupToggles),
        new(AppSettingKeys.FaHierarchyVormontageAktiv,          "Vormontage-Listen aktiv",       StandortFieldType.Bool, StandortBackend.App, GroupToggles),

        // Struktur-Import (ServiceSettings).
        new("Sync:HierarchicalFaEnabled",           "FA-Struktur-Import aktiv",       StandortFieldType.Bool,   StandortBackend.Service, GroupImport),

        // Werkbank-Anlage aus Sage-Arbeitsplaetzen (ServiceSettings, Baustein a). Der Toggle
        // Sync:ProductionWorkplaceSyncEnabled bleibt bewusst in /ServiceSettings, nicht hier.
        new("Sync:ProductionWorkplaceSyncMandant",         "Mandant (Sage-Arbeitsplätze)",     StandortFieldType.Int,    StandortBackend.Service, GroupWerkbaenke),
        new("Sync:ProductionWorkplaceSyncAusschlussliste", "Ausschluss-Kürzel (kommagetrennt)", StandortFieldType.String, StandortBackend.Service, GroupWerkbaenke),

        // Hardware-Barcodescanner (v1.66.0, AppSettings). Geraete-Abschalter liegt im Browser (Mein Profil), nicht hier.
        new(AppSettingKeys.HardwareScannerAktiv, "Hardware-Scanner aktiv", StandortFieldType.Bool, StandortBackend.App, GroupHardwareScanner,
            "Scanner, die wie eine Tastatur schreiben, füllen dann auf allen Scan-Seiten dieselben Felder wie der Kamera-Scan. Jedes Gerät kann es unter „Mein Profil“ für sich ausschalten. Geräte mit Fokus-Eingabe (z. B. Netum): zuerst Feld oder Kamera-Knopf antippen, sonst kommt nichts an."),
        new(AppSettingKeys.HardwareScannerPraefix, "Präfix der Scanner-Ausgabe", StandortFieldType.String, StandortBackend.App, GroupHardwareScanner,
            "Zeichen, die der Scanner vor jeden Code setzt; als Abschluss Enter einstellen. Leer = ~09. Scanner auf deutsches Tastaturlayout stellen."),
        // 10..500 ms: unter 10 ms schwanken Browser-Zeitstempel und Android-Ereignistakt mehr als ein Scanner-Zeichen braucht (Scans
        // brechen ab); ab ~500 ms laege der Wert im Tempo schnellen menschlichen Tippens (Praefix wuerde als Scan erkannt) und ein
        // getipptes ~ stuende sichtbar verzoegert. Gleiche Grenzen im Client (hardware-scanner.js HwScanCore.abstand).
        new(AppSettingKeys.HardwareScannerMaxZeichenabstandMs, "Größter Zeichenabstand (ms)", StandortFieldType.Int, StandortBackend.App, GroupHardwareScanner,
            "Schneller als dieser Abstand getippte Zeichen gelten als Scan. Erlaubt 10 bis 500. Leer = Standard 50.",
            Min: 10, Max: 500, EmptyResetsToDefault: true),

        // Vault- und enaio-Links (v1.67.0, AppSettings mit Seed in Program.cs) + Quelle des enaio-Syncs (ServiceSetting, Katalog-Seed).
        // Der Sync-Schalter Sync:EnaioDmsEnabled bleibt bewusst in /ServiceSettings, nicht hier (wie bei den Sage-Views).
        new(AppSettingKeys.VaultLinksAktiv, "Vault-Links anzeigen", StandortFieldType.Bool, StandortBackend.App, GroupVault,
            "Zeichnungs-Symbol und „Im VAULT öffnen“ an Artikelnummern. Ohne Vault (IDEAL) ausschalten."),
        new(AppSettingKeys.VaultLinkVorlage, "Vault-Adresse (Vorlage)", StandortFieldType.String, StandortBackend.App, GroupVault,
            "{Artikelnummer} wird durch die kodierte Nummer ersetzt. Leer = keine Links."),
        new(AppSettingKeys.EnaioLinksAktiv, "enaio-Dokument-Links anzeigen", StandortFieldType.Bool, StandortBackend.App, GroupEnaio,
            "Dokument-Symbole an FA-Nummern (FA-Liste, Leitstand, FA-Vervollständigung, FA-Abarbeitung). Ohne enaio ausschalten."),
        new(AppSettingKeys.EnaioLinkVorlage, "enaio-Adresse (Vorlage)", StandortFieldType.String, StandortBackend.App, GroupEnaio,
            "{ObjektId} wird durch die enaio-Objekt-ID ersetzt. Leer = keine Links."),
        new("Sync:EnaioDmsViewName", "enaio-View (Sync-Quelle)", StandortFieldType.String, StandortBackend.Service, GroupEnaio,
            ViewNameHint + " Format [Datenbank].[Schema].[Name]; Spalten id, angelegt, Typ, WaNummer, WaNummerTrimmed. Leer = Standard. Sync-Schalter in den Service-Einstellungen."),
    };

    /// <summary>Der Master-Key — NUR read-only in dieser Maske angezeigt, nie geschrieben.</summary>
    public const string MasterKey = "ProduktionsauftragHierarchisch";

    public static readonly IReadOnlyList<string> GroupOrder = new[]
    {
        GroupFirmendaten, GroupMandantViews, GroupToggles, GroupImport, GroupWerkbaenke, GroupHardwareScanner, GroupVault, GroupEnaio
    };
}
