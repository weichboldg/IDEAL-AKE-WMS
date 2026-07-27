# Typisierte, vollständige ServiceSettings-Verwaltung — Design

**Datum:** 2026-07-08
**Status:** Approved-pending-review
**Branch/Worktree:** `feature/glas-bestellung` → `.claude/worktrees/glas-bestellung`
**Version:** in **v1.25.0** falten (kein Versionssprung).

Die WMS-Seite `/ServiceSettings` wird von einem Freitext-Key/Value-Editor zu einem **typisierten, vollständigen** Editor: Bool-Schalter erscheinen als **Aktiv/Inaktiv**-Toggle, Zahlen als Zahlenfelder, Texte als Textfelder — und **alle** Service-Einstellungen (auch die heute nur in `appsettings.json` liegenden) sind dort steuerbar. Ausgenommen bleiben nur DB-Connection-Strings und SMTP/MailSettings.

---

## Problem / Motivation

`ServiceSettings.GetBoolAsync/GetIntAsync/GetValueAsync` (`IDEALAKEWMSService/Common/ServiceSettings.cs`) lesen Runtime-Settings **ausschließlich aus der DB-Tabelle `[ServiceSettings]`** (kein appsettings-Fallback; `IConfiguration` liefert nur den Connection-String). Fehlt eine DB-Zeile → der im Aufruf übergebene **Default** greift.

Folgen im Ist-Zustand:
1. Ein in `appsettings.json` gesetzter `Sync:*`-Wert wird **ignoriert** (z. B. `ProductionOrderReconcileEnabled: true` → Service liest DB → keine Zeile → Default `false`). Genau das führte zur Fehldiagnose beim Reconcile-Test.
2. Die `/ServiceSettings`-Seite ist ein **generischer Freitext-Editor** — Bool-Werte als Text (`true`/`false`, fehleranfällig: `1` funktioniert NICHT, nur `true`), keine Typen, keine Vollständigkeit.
3. `Program.cs` seedet nur **~13** der Service-Keys — neue Keys (Reconcile u. a.) fehlen und tauchen daher gar nicht in der Seite auf.
4. Ein Teil der Service-Config (`WorkerSettings:*`, `ErrorNotification:*`, `Sync:Feiertag*`) wird direkt via `IConfiguration` gelesen und ist **gar nicht** DB-steuerbar.

Zum Vergleich: die AppSettings-Seite (`/Settings`, `SettingsController`) rendert Bool bereits als `form-switch`-Toggle mit „Aktiviert/Deaktiviert" — dieses Muster ist die Blaupause.

## Ziel

- Jeder **bool** Service-Schalter → **Aktiv/Inaktiv**-Toggle in der Settings-Seite.
- **Alle** Service-Settings in der Seite anzeigbar + steuerbar — inkl. der heute nur in appsettings.json liegenden.
- **Ausnahmen:** `ConnectionStrings:*` und **`MailSettings:*`** (SMTP inkl. Passwort) bleiben appsettings-only (Infrastruktur/Secrets).
- Kein Schema-Change, keine Migration.

---

## Architektur

### 1. Typisierter Katalog `ServiceSettingDefinitions` (neu)

`IdealAkeWms/Models/ServiceSettingDefinition.cs`:
```csharp
public enum ServiceSettingType { Bool, Int, String }

public sealed record ServiceSettingDefinition(
    string Key,
    ServiceSettingType Type,
    string DefaultValue,   // immer als String (DB-Format): bool -> "true"/"false", int -> "60", string -> Rohwert
    string Category,       // UI-Gruppierung, z.B. "Sync", "BOM-Cache", "Feiertage", "Fehlermail", "Worker", "BDE", "Lager"
    string Description,    // Anzeige in der UI
    bool Multiline = false // string-Listen (z.B. Recipients) als Textarea
);
```

`IdealAkeWms/Models/ServiceSettingDefinitions.cs`:
- `public static IReadOnlyList<ServiceSettingDefinition> All { get; }` — enthält **jede** Service-Einstellung (Single Source of Truth für Typ/Default/Kategorie/Beschreibung).
- Helfer: `TryGet(string key, out ServiceSettingDefinition def)`.

**Der Katalog treibt sowohl das Seeding als auch die UI.** Liegt im **Web-Projekt** `IdealAkeWms` (der Service referenziert das Web-Projekt bereits → beide Seiten können den Katalog nutzen).

**Katalog-Inhalt** (vollständig per Grep zu verifizieren — mindestens diese Keys; Kategorien indikativ):

*Bool (Aktiv/Inaktiv):*
`Sync:ProductionOrdersEnabled`(true), `Sync:ArticlesEnabled`(true), `Sync:OseonArticleCategoryEnabled`(false), `Sync:OseonTrackingEnabled`(false), `Sync:EnaioDmsEnabled`(false), `Sync:PartRequisitionEmailEnabled`(false), `Sync:WarehouseRequisitionEmailEnabled`(false), `Sync:BomCacheEnabled`(false), `Sync:FaWorkStepDetectionEnabled`(false), `Sync:CoatingDetectionEnabled`(false), `Sync:FeiertagSyncEnabled`(false), `Sync:LagerplaetzeEnabled`(false), `Sync:LagerbestandEnabled`(false), `Sync:ProductionOrderReconcileEnabled`(false), `WorkerSettings:SyncDryRun`(false), `ErrorNotification:Enabled`(false), `Notifications:MeldebestandEnabled`(true — falls im bestehenden Seed/gelesen).

*Int:*
`Sync:BomCacheWeeks`(8), `Sync:BomCacheMaxOrders`(200), `Sync:BomCacheMaxAgeHours`(24), `Sync:BdeAutoPauseIntervalMinutes`(60), `Sync:LagerbestandIntervalMinutes`(0), `Sync:ReconcileMaxCancelPerRun`(100), `Sync:FeiertagJahreVoraus`(2), `WorkerSettings:SyncIntervalMinutes`(15), `WorkerSettings:NotificationCheckIntervalMinutes`(60).

*String:*
`Sync:FeiertagCountryCode`("AT"), `Sync:FeiertagRegion`(""), `ErrorNotification:Recipients`("" — Komma-Liste, Multiline), `Notifications:Recipients`("" — falls vorhanden).

> **Verbindlich:** Der Plan MUSS per Grep ALLE `ServiceSettings.GetBoolAsync/GetIntAsync/GetValueAsync`-Aufrufe im Service + ALLE bereits in `Program.cs` geseedeten ServiceSettings-Keys erfassen und in den Katalog aufnehmen (Default = der im jeweiligen Aufruf übergebene Default). Kein service-gelesener Key darf im Katalog fehlen (Test sichert das ab, siehe unten).

### 2. Seeding (Program.cs)

Der bestehende partielle `serviceSettingSeed`-Block wird ersetzt durch eine Schleife über `ServiceSettingDefinitions.All` (idempotent, bestehendes Muster):
```csharp
foreach (var def in ServiceSettingDefinitions.All)
    if (!db.ServiceSettings.Any(s => s.Key == def.Key))
        db.ServiceSettings.Add(new ServiceSetting {
            Key = def.Key, Value = def.DefaultValue,
            Category = def.Category, Description = def.Description });
db.SaveChanges();
```
→ Nach dem nächsten Start existiert **jede** Katalog-Zeile in der DB (inkl. Reconcile). Bestehende Zeilen werden **nicht** überschrieben (User-Werte bleiben).

### 3. Typisierte UI (`ServiceSettingsController` + `Views/ServiceSettings/Index.cshtml`)

`[RequireAdminAccess]` bleibt.

**ViewModel** `IdealAkeWms/Models/ViewModels/ServiceSettingsViewModel.cs`:
- `List<ServiceSettingGroup>` (Category → `List<ServiceSettingItem>`), Reihenfolge der Kategorien stabil.
- `ServiceSettingItem { string Key; ServiceSettingType Type; string Value; string Description; bool Multiline; }` — Wert = DB-Wert (aus `GetAllAsync`), Typ/Beschreibung/Kategorie aus Katalog.
- `List<ServiceSetting> OrphanEntries` — DB-Zeilen, deren Key NICHT im Katalog ist (Freitext-Fallback, damit nichts versteckt wird).

**Controller:**
- `Index()`: `GetAllAsync()` + Katalog mergen → gruppiertes ViewModel; Orphans separat.
- `SaveSettings(Dictionary<string,string> settings)` (POST, neu, Muster = `SettingsController.SaveSettings`): je Eintrag validieren + `UpsertAsync(key, value, category, description)` (Kategorie/Beschreibung aus Katalog). **Validierung:** Int-Keys → `int.TryParse` (sonst ModelState-Fehler, kein Speichern der Zeile); Bool-Keys → auf `"true"`/`"false"` normalisieren. `TempData["SuccessMessage"]`.
- Die bestehenden `Create`/`Edit`/`Delete` bleiben für **Orphan-/Ad-hoc-Keys** erhalten (Freitext), sind aber nicht mehr der Hauptpfad.

**View** (`Index.cshtml`), gruppiert nach Kategorie, ein `<form asp-action="SaveSettings">`:
- **Bool** → `form-check form-switch` Checkbox + Hidden-Input (Name = Key) nach dem etablierten `.bool-toggle`/`.bool-hidden`-Muster aus `Views/Settings/Index.cshtml` (Fallstrick „Hidden+Checkbox gleicher name" beachten), Label „Aktiviert"/„Deaktiviert".
- **Int** → `<input type="number" name="settings[Key]">` (min=0 wo sinnvoll).
- **String** → `<input type="text">` bzw. `<textarea>` bei `Multiline`.
- Beschreibung als `form-text` je Feld.
- „Sonstige/Erweitert"-Sektion (eingeklappt) für Orphans mit Bearbeiten/Löschen + „Neu".
- JS wiederverwenden/analog `Views/Settings/Index.cshtml` (Bool-Toggle-Label-Sync).

### 4. Lesestellen-Umstellung (Service → DB-first)

Die 8 heute nur via `IConfiguration` gelesenen Keys werden auf `ServiceSettings.GetXxx(config, key, default, ct)` umgestellt (Default = bisheriger Wert):

| Key | Typ | Datei (laut Analyse) |
|-----|-----|------|
| `WorkerSettings:SyncIntervalMinutes` | int(15) | `Workers/SyncWorker.cs:31` |
| `WorkerSettings:SyncDryRun` | bool(false) | `Workers/SyncWorker.cs:32` |
| `WorkerSettings:NotificationCheckIntervalMinutes` | int(60) | `Workers/NotificationWorker.cs:25` |
| `ErrorNotification:Enabled` | bool(false) | `Services/SyncErrorNotifier.cs` |
| `ErrorNotification:Recipients` | string(Komma-Liste) | `Services/SyncErrorNotifier.cs` |
| `Sync:FeiertagCountryCode` | string("AT") | `Workers/SyncWorker.cs` (~356) |
| `Sync:FeiertagRegion` | string("") | `Workers/SyncWorker.cs` |
| `Sync:FeiertagJahreVoraus` | int(2) | `Workers/SyncWorker.cs` |

Details:
- `ErrorNotification:Recipients` war ein `string[]` (appsettings-Array) → jetzt **Komma-getrennter String** in der DB; `SyncErrorNotifier` splittet auf `,` (Trim, Leere raus). Analog `Notifications:Recipients` falls vorhanden.
- **Resilienz:** Die Interval-/DryRun-Reads (die den Worker-Takt steuern) MÜSSEN einen sicheren Fallback haben — ein transienter DB-Fehler beim Settings-Read darf den Worker-Loop nicht abwürgen (try/catch → Default, geloggt). Ein kleiner Helper (`ServiceSettings.GetIntSafeAsync`/`GetBoolSafeAsync` mit try/catch → Default) ODER lokaler try/catch an den Takt-relevanten Reads.
- **MailSettings bleiben `IConfiguration`-only** (SmtpHost/Port/UseSsl/Username/Password/Sender/FromAddress) — nicht umstellen. ConnectionStrings ebenfalls nicht.

### 5. Präzedenz-/Doku-Folge

Nach der Umstellung gilt für alle Katalog-Keys: **DB gewinnt** (Katalog-Default → DB-Override). Die `Sync:`/`WorkerSettings:`/`ErrorNotification:`/`Feiertag`-Werte in `appsettings.json` werden vom Service **nicht mehr gelesen** (nur noch Doku/Default-Referenz; die Seed-Defaults leben im Katalog). Das ist die dauerhafte Behebung der ursprünglichen Verwirrung — **die GUI ist ab jetzt die einzige Steuerungsquelle.** Nur `MailSettings:*` + `ConnectionStrings:*` bleiben appsettings-getrieben. (In CLAUDE.md + Changelog klarstellen.)

---

## Nicht im Scope

- AppSettings-Seite (`/Settings`) bleibt **unverändert** (hat Toggles bereits).
- Keine Zusammenführung der zwei Settings-Seiten.
- MailSettings/SMTP + ConnectionStrings NICHT DB-steuerbar.
- Kein Caching der ServiceSettings (bewusst: Änderungen wirken sofort beim nächsten Sync).
- Kein Schema-Change / keine Migration / keine FreshInstall-Schema-Änderung.

## Tests

- **Katalog-Konsistenz** (`ServiceSettingDefinitionsTests`, InMemory nicht nötig — reine Daten): keine doppelten Keys; jeder `Bool`-Default parst als `"true"`/`"false"`; jeder `Int`-Default parst als int; jede Kategorie/Description nicht leer. **Drift-Guard:** ein Test, der (per Reflection/fester Liste) sicherstellt, dass die dokumentierten service-gelesenen Keys im Katalog sind.
- **`ServiceSettingsController`** (neu, InMemory): `Index` merged Katalog∪DB (bool/int/string korrekt typisiert, Orphans separat); `SaveSettings` upsert + Validierung (int-Parsefehler → keine Speicherung + ModelState; bool normalisiert `true`/`false`).
- **Lesestellen-Umstellung** (Worker/Service, raw/BackgroundService) = **Manual-UAT** (nicht InMemory-testbar) — TESTSZENARIEN.

## Deploy

- Kein Schema-Change. Beim ersten Start nach Deploy seedet `Program.cs` alle fehlenden Katalog-Zeilen (Defaults) — bestehende DB-Werte bleiben.
- **Verhaltensänderung dokumentieren:** appsettings.json `Sync:`/`WorkerSettings:`/`ErrorNotification:`/`Feiertag` werden nach dem Deploy nicht mehr gelesen; Steuerung ausschließlich über `/ServiceSettings`.
- Manuelle Abnahme: TESTSZENARIEN — jeden Typ (Bool-Toggle, Int, String) speichern + Wirkung im nächsten Sync/Protokoll prüfen; int-Validierung; Reconcile-Flag via Toggle scharfschalten.

## Betroffene Dateien (Überblick)

- **Neu:** `Models/ServiceSettingDefinition.cs`, `Models/ServiceSettingDefinitions.cs`, `Models/ViewModels/ServiceSettingsViewModel.cs`, Tests (`ServiceSettingDefinitionsTests`, `ServiceSettingsControllerTests`).
- **Geändert (Web):** `Controllers/ServiceSettingsController.cs`, `Views/ServiceSettings/Index.cshtml` (+ ggf. Create/Edit-Views für Orphans), `Program.cs` (Seed-Loop).
- **Geändert (Service):** `Workers/SyncWorker.cs`, `Workers/NotificationWorker.cs`, `Services/SyncErrorNotifier.cs`, ggf. `Common/ServiceSettings.cs` (Safe-Reader-Helper).
- **Doku:** `CLAUDE.md`, `Views/Help/Changelog.cshtml`, `docs/TESTSZENARIEN.md`, `PROJECT_STATUS.md`.
