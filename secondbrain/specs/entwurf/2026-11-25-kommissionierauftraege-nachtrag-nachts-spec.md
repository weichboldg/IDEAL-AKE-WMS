---
type: spec
title: Kommissionieraufträge – Nachtrag nachts im Dienst, Knopf „Fehlende … erzeugen“ nur für Admins
slug: 2026-11-25-kommissionierauftraege-nachtrag-nachts-spec
status: Entwurf
created: 2026-11-25
updated: 2026-11-25
source_backlog: "[[2026-11-25-kommissionierauftraege-nachtrag-automatisch-knopf-nur-admin]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: beide
affected_code:
  - "IDEALAKEWMSService/Services/KommissionierNachtragService.cs (neu: Lauf, Fälligkeit, Zählung, Aktivitäts-Protokoll)"
  - "IDEALAKEWMSService/Common/DienstBenutzer.cs (neu: ICurrentUserService für den Dienst, fester Name)"
  - "IDEALAKEWMSService/Workers/SyncWorker.cs (neuer RunResilientAsync-Schritt nach :543)"
  - "IDEALAKEWMSService/Program.cs (Registrierung Workflow + BOM-Graph über gemeinsame Methode, DienstBenutzer, neuer Service)"
  - "IdealAkeWms/Services/PickingOrderRegistrierung.cs (neu: eine Registrierungsmethode für BOM-Weiche + Kommissionierauftrag-Repos + PickingOrderWorkflow, genutzt von Web- und Dienst-Program.cs)"
  - "IdealAkeWms/Program.cs (:89-115, :130-132, :195 auf die gemeinsame Methode umstellen)"
  - "IdealAkeWms/Services/PickingOrderWorkflow.cs (Zeilenzähler in PickingOrderErzeugung, Fehlertext :143-144; ggf. Überdeckung je Rückfrage 2)"
  - "IdealAkeWms/Services/PickingOrderRestmengen.cs (nur bei Rückfrage 2 = b: Überdeckungs-Liste)"
  - "IdealAkeWms/Services/SyncLogger/SyncLogServices.cs (neuer Name in All + DisplayNames)"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs (2 Keys: Basis, Meta, Int-Bereich)"
  - "IdealAkeWms/Controllers/PickingLeitstandController.cs (AuftraegeNachziehen :532-548 Filter; AltbestandGeprueft :550-567 je Rückfrage 3; Index setzt IsAdmin)"
  - "IdealAkeWms/Models/ViewModels/PickingLeitstandViewModel.cs (IsAdmin)"
  - "IdealAkeWms/Views/PickingLeitstand/Index.cshtml (:86-99 beide Formulare nur für Admins)"
  - "IdealAkeWms/Views/Help/Index.cshtml (:1771-1773), Views/Picking/Bom.cshtml (:66), Views/Picking/Index.cshtml (:55-56), Views/Help/Changelog.cshtml"
  - "docs/TESTSZENARIEN.md (TS-191 neu, Hinweis an TS-120.2/120.6/120.13), docs/anwender/01-kommissionierung.md (:93-95, :262), docs/anwender/02-kommissionierbereiche.md (:100, :118), docs/betrieb/07-troubleshooting.md (:88-89), docs/betrieb/08-diagnose-abfragen.md (:413)"
  - "IDEALAKEWMSService.Tests (neu: KommissionierNachtragServiceTests inkl. DI-Test), IdealAkeWms.Tests (Leitstand-Filter, BomDiResolutionTests auf die gemeinsame Methode)"
open_questions:
  - "Was heißt „Admin“ für den Knopf: Rolle admin über das vorhandene [RequireAdminAccess] (Empfehlung) oder ein neues eigenes Recht?"
  - "Stückliste nach der Freigabe kleiner geworden (Menge gesenkt oder Position entfernt): (a) nur dokumentieren, (b) nichts ändern, aber im Nachtlauf je FA melden (Empfehlung), (c) offene Zeilen automatisch kürzen/entfallen lassen?"
  - "„Altbestand geprüft“ durch einen Leitstand-Benutzer ohne Admin: sofort die fehlenden Aufträge dieses einen FA anlegen (Empfehlung) oder bis zum Nachtlauf warten?"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

# Kommissionieraufträge – Nachtrag nachts im Dienst, Knopf nur für Admins

Bezug: [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]] (v1.88.0 E2, v1.89.0 Q11),
[[0021-kommissionierauftrag-als-beleg-neben-der-stueckliste]], [[0006-rollenkonzept-statische-keys-mit-admin-wildcard]],
[[0008-servicesettings-db-first-mit-typisiertem-katalog]], [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]],
[[0003-auditableentity-als-entity-basis]], [[0020-einstellungen-mit-festem-wertebereich-als-auswahl-und-mit-wirkungsbeschreibung]].

Code-Stand für alle Fundstellen: Bündel-Worktree `feature/2026-08-07-ideal-teile-1-5`, Version 1.99.69
(`IdealAkeWms/AppVersion.cs:5`).

## Ziel / Nutzen (das Warum)

Neue Freigaben legen ihre Kommissionieraufträge schon heute selbst an (Einzelfreigabe `PickingLeitstandController.cs:450-451`,
Sammelfreigabe `:512-518`, ohne Leitstand beim Öffnen der Stückliste `PickingController.cs:239-247`). Nicht automatisch kommen
Stücklisten-Nachträge **nach** der Freigabe an: neue Positionen oder höhere Mengen in Sage bzw. in der FA-Struktur. Dafür gibt es nur den
Leitstand-Knopf. Nach dem AKE-Prod-Update hat der Knopf einmal 39 Aufträge / 6 180 Zeilen angelegt; danach meldet er bei jedem Klick
„0 … angelegt; 43 FA ohne offene Restmenge“ und verwirrt.

Entscheidung Gerald 2026-11-25: Der Nachtrag läuft **nachts im Windows-Dienst** für alle freigegebenen, offenen HauptFAs, mit derselben
idempotenten Logik `PickingOrderWorkflow.ErzeugeOffeneAsync` (`IdealAkeWms/Services/PickingOrderWorkflow.cs:56`). Der Knopf bleibt als
Notfall-Werkzeug, aber **nur für Admins** sichtbar und serverseitig geschützt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neuer Dienst-Lauf „Kommissionier-Nachtrag“ im `SyncWorker`: einmal täglich ab einer einstellbaren Stunde, über alle FAs aus
  `GetReleasedForPickingAsync` (`ProductionOrderPickingStatusRepository.cs:189-200`: HauptFA, freigegeben, nicht erledigt, nicht storniert,
  nicht `IsDonePicking`), je FA `ErzeugeOffeneAsync`.
- Aktivitäts-Protokoll mit deutschen Zähl-Schlüsseln und Warnzeilen je FA.
- Zwei Service-Einstellungen (Schalter, Uhrzeit), Katalog, Startup-Seed, Drift-Guard.
- `DienstBenutzer` als `ICurrentUserService` im Dienst: `CreatedBy` und `CreatedByWindows` = Lauf-Name.
- Eine gemeinsame Registrierungsmethode für BOM-Weiche, Kommissionierauftrag-Repositories und `PickingOrderWorkflow`, genutzt von Web und
  Dienst.
- Leitstand: beide Formulare zu `AuftraegeNachziehen` (Leiste „markierte“ `Index.cshtml:86-93` und „alle“ `:95-99`) nur für Admins sichtbar;
  Action serverseitig nur für Admins. Beide Knöpfe hängen an derselben Action, deshalb gilt die Admin-Sperre für beide (Geralds Verweis
  `Index.cshtml:91/98`).
- Texte, die auf den Knopf verweisen: Hilfe, Stückliste, Auftragsliste, Fehlertext im Workflow, Meldung nach „Altbestand geprüft“, Anwender- und
  Betriebsdoku, Testszenarien.

**Out-of-Scope**
- Verhalten bei kleiner gewordener Stückliste über eine Meldung hinaus (Rückfrage 2).
- FAs mit `IsDonePicking = true` (alle Aufträge erledigt): Eine spätere Stücklisten-Erweiterung bekommt keinen Auftrag mehr. Das ist schon
  heute so (der Knopf nimmt dieselbe Liste, `PickingLeitstandController.cs:538`) und bleibt so.
- Modus ohne Leitstand (`LeitstandAktiv` aus): dort entstehen Aufträge weiter beim Öffnen der Stückliste (`PickingController.cs:241-247`).
  Der Nachtlauf nimmt nur FAs mit `IsReleasedForPicking`; an diesem Weg ändert sich nichts.
- Ein Web-Endpunkt, den der Dienst ruft, und eine zweite Implementierung der Restmengen-Logik im Dienst (siehe Lösungsentwurf, verworfen).
- Mehrere Läufe pro Tag.

## Fachliche Anforderungen

F1 – **Nachtlauf.** Einmal pro Tag ab der eingestellten Stunde (Default 2 Uhr) ruft der Dienst für jeden freigegebenen, offenen HauptFA
`ErzeugeOffeneAsync` auf. Für FAs ohne neue Restmenge passiert nichts (idempotent, `PickingOrderRestmengen.cs:30-41`).

F2 – **Fälligkeit und Nachholen.** Der Lauf ist fällig, wenn der jüngste Termin (heute oder gestern zur eingestellten Stunde) nach dem letzten
Start liegt. Merker ist die jüngste Startmeldung „Run gestartet“ dieses Laufs im Aktivitäts-Protokoll (`SyncLogger.cs:21`). Ein verpasster
Termin (Dienst aus, Server neu gestartet) wird nach dem Dienststart **einmal** nachgeholt. Das gilt auch für den ersten Dienststart nach dem
Deploy: Der erste Lauf kommt dann sofort und nicht erst in der Nacht. Dieselbe Regel hat `LetzteBestellungPlaner.Faellig`
(`IDEALAKEWMSService/Services/Sdata/LetzteBestellungSyncService.cs:18-26`), sie wird wiederverwendet.

F3 – **Schalter.** `Sync:KommissionierNachtragEnabled` (Default an) schaltet den Lauf ab, ohne neu zu deployen. Im DryRun des Dienstes
(`WorkerSettings:SyncDryRun`, `SyncWorker.cs:33`) läuft der Nachtrag nicht. `ErzeugeOffeneAsync` hat keinen Trockenmodus. Der Dienst
schreibt dazu eine Log-Zeile „Kommissionier-Nachtrag im DryRun übersprungen“ und setzt keinen Merker.

F4 – **Audit.** Aufträge und Zeilen aus dem Nachtlauf tragen `CreatedBy = CreatedByWindows = "KommissionierNachtrag"` (ADR 0003: Dienste
schreiben ihren Namen; Muster `LetzteBestellungSyncService.cs:40,173,195`).

F5 – **Aktivitäts-Protokoll** (Dienstname `KommissionierNachtrag`, Anzeige „Kommissionier-Nachtrag“). Zähl-Schlüssel:
`fa_geprueft`, `fa_mit_nachtrag`, `auftraege_angelegt`, `zeilen_angelegt`, `ohne_rest`, `altbestand_pruefen`, `ohne_stueckliste`, `fehler`
(bei Rückfrage 2 = b zusätzlich `stueckliste_kleiner`). Zeilen:
- Info je FA mit Nachtrag: „FA <Nr>: <KO-Nummern> (<n> Zeilen)“.
- Warnung je FA mit „Altbestand prüfen“, ohne Stückliste oder mit Bereichs-Meldung (`PickingOrderErzeugung.Meldungen`, Workflow `:67`,
  `:75`, `:112`).
- Warnung je FA mit `Fehler`. Das sind fehlender Standardbereich (`:93`) und Nummernkreis nach 3 Versuchen (`:143`). Wirft der
  Workflow selbst eine Ausnahme, etwa weil die Stückliste nicht lesbar ist (Sage-View oder OSEON bei Cache-Fehltreffer,
  `BomRepository.cs:27-57`), zählt das als `fehler` mit Ausnahmetext. Der Lauf geht mit dem nächsten FA weiter.
- Schlägt der Lauf als Ganzes fehl (FA-Liste nicht lesbar), endet er mit `FinishFailedAsync` und wirft weiter. `RunResilientAsync` meldet das
  über die Fehlermail (`SyncWorker.cs:615-627`).

F6 – **Knopf nur für Admins.** Beide Formulare zu `AuftraegeNachziehen` sehen nur Admins. Ein POST eines Nicht-Admins endet auf
`Account/AccessDenied` wie bei jedem `[RequireAdminAccess]` (`Filters/RequireAdminAccessAttribute.cs:23-27`). Das Verhalten für Admins
(markierte oder alle, Prüfung „nur freigegebene“, Sammelmeldung) bleibt wie in `PickingLeitstandController.cs:535-548`.

F7 – **„Altbestand geprüft“** hängt an Rückfrage 3. Empfehlung: Die Action legt danach sofort die fehlenden Aufträge **dieses FA** an (ein
Aufruf `ErzeugeOffeneAsync(id)`, Sammelmeldung wie bei der Freigabe). So muss ein Leitstand-Benutzer ohne Admin nicht bis zur Nacht warten.
Die heutige Meldung `:561-562` verweist auf den Knopf, den er nicht mehr sieht.

F8 – **Kleiner gewordene Stückliste** hängt an Rückfrage 2 (heutiges Verhalten unten belegt).

F9 – **Texte.** Alle Verweise auf den Knopf nennen künftig den Nachtlauf, der Knopf nur noch als Admin-Notfall:
- `Views/Help/Index.cshtml:1771-1773`. Der Backlog nennt `:1742`, im Worktree steht der Text jetzt bei `:1773`.
- `Views/Picking/Bom.cshtml:66`, `Views/Picking/Index.cshtml:55-56`.
- `PickingOrderWorkflow.cs:143-144`, neuer Text: „… – der nächtliche Lauf versucht es erneut; sofort: Admin im Leitstand „Fehlende
  Kommissionieraufträge erzeugen“.“
- `PickingLeitstandController.cs:561-562`, Text je Rückfrage 3.

### Heutiges Verhalten bei kleiner gewordener Stückliste (Beleg für Rückfrage 2)

- `PickingOrderRestmengen.Berechne` geht nur über die **Soll**-Positionen der aktuellen Stückliste und gibt nur Reste `> 0` zurück
  (`PickingOrderRestmengen.cs:36-41`). Eine gesenkte Menge oder eine entfernte Position erzeugt also nichts und ändert nichts.
- `SollMenge` wird nur beim Anlegen geschrieben (`PickingOrderWorkflow.cs:130` Erzeugen, `:214` Folgeauftrag). Weitere Schreiber gibt es
  nicht: Die Suche nach `SollMenge =` im ganzen Worktree (ohne Tests) trifft nur diese zwei Stellen und die Migration. Die Suche nach
  `UPDATE … PickingOrderLines` / `SetProperty(…SollMenge` trifft nichts.
- Ergebnis: Offene Zeilen behalten ihr altes Soll. Ein entfernter Artikel bleibt im offenen Auftrag, und der Kommissionierer pickt mehr als
  die Stückliste verlangt. Darüber informiert heute niemand.

## Technischer Loesungsentwurf

### Wie die Erzeugung in den Dienst kommt (Optionen)

Befund:
- Der Dienst referenziert das Web-Projekt (`IDEALAKEWMSService/IDEALAKEWMSService.csproj:33-35`).
- Er nutzt bereits Web-Klassen über DI:
  - `SyncLogger` (`IDEALAKEWMSService/Program.cs:47-48`)
  - `IPurchaseOrderReceiptRepository` „Rest-Regel über das Web-Repository“ (`:89-90`)
  - `IProductionOrderPickingStatusRepository` (`:93`)
  - `CachedSettingRepository` (`:44-45`)
- Es gibt **keinen** Fall, in dem der Dienst einen Web-Endpunkt ruft. Die Suche nach `BaseUrl`/`/api/` im Dienst trifft nur Mail-Links
  (`BfAuftragEmailService.cs:122-124`) und Sage-SData.
- Es gibt **keine** `ICurrentUserService`-Implementierung außerhalb von Web und Tests (`CurrentUserService.cs:6`,
  `IdealAkeWms.Tests/Helpers/FakeCurrentUserService.cs:5`).

| Option | Kurz | Bewertung |
|---|---|---|
| **A (empfohlen)** | Dienst löst `PickingOrderWorkflow` direkt aus DI auf; eigene kleine `ICurrentUserService`-Implementierung `DienstBenutzer` | Keine doppelte Restmengen-Logik, folgt dem Muster „Web-Klasse im Dienst“, Transaktion und Nummernkreis bleiben an einem Ort |
| B | Web-`CurrentUserService` im Dienst registrieren | Ohne `HttpContext` schreibt er `"SYSTEM"` (`CurrentUserService.cs:40,49`) statt des Lauf-Namens und widerspricht ADR 0003. Braucht zusätzlich `AddHttpContextAccessor`, Rollen- und User-Repository. Verworfen |
| C | Dienst ruft einen Web-Endpunkt | Kein Vorbild, braucht Dienstkonto als WMS-Benutzer, Windows-Auth-Handshake und Fehlerpfad über HTTP. Verworfen |
| D | Logik im Dienst nachbauen | Zwei Restmengen-Regeln, die auseinanderlaufen. Gerald: keine Duplikation. Verworfen |

**A im Detail:**

1. **Gemeinsame Registrierung** `IdealAkeWms/Services/PickingOrderRegistrierung.cs` (statische Methode
   `AddKommissionierauftraege(IServiceCollection)`). Sie enthält genau die heutigen Web-Zeilen:
   - `ServiceSettingRepository` + `IServiceSettingRepository` als `GuardedServiceSettingRepository` (`IdealAkeWms/Program.cs:89-93`)
   - BOM-Graph mit lazy Weiche (`:97-110`)
   - FA-Hierarchie-Knoten mit Cache (`:113-114`)
   - `FaHierarchyTreeBuilder` (`:195`)
   - `IPickingAreaRepository`, `IPickingOrderRepository`, `PickingOrderWorkflow` (`:130-132`)

   Web- und Dienst-`Program.cs` rufen sie auf. `IProductionOrderRepository` und `IProductionOrderPickingStatusRepository` bleiben, wo sie
   sind (Web `:79-80`, Dienst `:93`). Im Dienst kommt `IProductionOrderRepository` dazu.

   Grund für die gemeinsame Methode statt einer Kopie: Der BOM-Graph ist absichtlich zyklisch und nur durch die lazy Delegates auflösbar.
   `BomDiResolutionTests.cs:15-31` existiert genau, weil er leicht bricht. Zwei Kopien würden still auseinanderlaufen.
   `BomDiResolutionTests.BuildProvider` ruft danach die gemeinsame Methode statt des Spiegels (`:60-72`).

   Abhängigkeiten im Dienst:
   - `IMemoryCache` ist da (`IDEALAKEWMSService/Program.cs:41`).
   - `OseonConnection` braucht `BomRepository` im Ctor (`BomRepository.cs:18-19`). Er wird nur im Flachmodus lazy gebaut
     (`BomRepositoryMasterSwitch.cs:44`), ist in `IDEALAKEWMSService/appsettings.json:5` vorhanden und wird bei IDEAL
     (hierarchisch) nie gebaut.
2. **`IDEALAKEWMSService/Common/DienstBenutzer.cs`** implementiert `ICurrentUserService`:
   - `GetDisplayName()` und `GetWindowsUserName()` geben den festen Namen zurück.
   - `GetCurrentAppUserId`/`GetCurrentAppUserName` → `null`, `IsLoggedIn` → `false`, `GetDefaultPageSizeAsync` → `null`.
   - Alle Rollen-Abfragen werfen `NotSupportedException("Der Dienst hat keine Rollen")`.

   Der Workflow nutzt im Erzeugungsweg nur die beiden Namensmethoden (`PickingOrderWorkflow.cs:126,131`). Eine Rollenabfrage im Dienst wäre
   ein Fehler und soll laut scheitern statt still `false` zu liefern. Registrierung:
   `AddScoped<ICurrentUserService>(_ => new DienstBenutzer(KommissionierNachtragService.ServiceUser))`.

   ponytail: Der Name ist fest auf diesen Lauf gesetzt. Nutzt später ein zweiter Dienst-Lauf Web-Workflows, wird der Name je Scope gesetzt.
3. **`IDEALAKEWMSService/Services/KommissionierNachtragService.cs`** nach dem Muster `LetzteBestellungSyncService`:
   - Ctor `(ApplicationDbContext, IServiceScopeFactory, IConfiguration, ILogger<…>, ISyncLogger)`, `ISyncLogger` als letzter Parameter
     (ADR 0010).
   - Konstanten: `ServiceUser = "KommissionierNachtrag"`, `KeyEnabled`, `KeyUhrzeit`, `DefaultUhrzeit = 2`.
   - `RunWennFaelligAsync(bool dryRun, DateTime jetzt, CancellationToken)`:
     - Schalter lesen (`ServiceSettings.GetBoolSafeAsync`), bei DryRun Log-Zeile und `false`.
     - Uhrzeit lesen (`GetIntSafeAsync`).
     - Merker aus `SyncLogs` lesen (Service = neuer Name, Message „Run gestartet“, wie `LetzteBestellungSyncService.cs:76-78`).
     - `LetzteBestellungPlaner.Faellig(jetzt, letzterStart, new[] { uhrzeit })`, dann `RunAsync`.
   - `RunAsync`:
     - `BeginRunAsync`.
     - FA-Ids aus `IProductionOrderPickingStatusRepository.GetReleasedForPickingAsync()`.
     - **je FA ein eigener Scope** (`IServiceScopeFactory`): `PickingOrderWorkflow` auflösen, `ErzeugeOffeneAsync(id)` in try/catch.
       Grund: Ein Scope über alle FAs sammelt Tausende Zeilen im Change-Tracker (Einmal-Lauf AKE 6 180 Zeilen). Ein unerwarteter Fehler soll
       den nächsten FA nicht mit einem verschmutzten Kontext treffen. `VerwerfeAenderungen` greift nur im Nummernkreis-Pfad
       (`PickingOrderWorkflow.cs:139`, `Nummernkreis.cs:39-46`).
     - Zählen, Zeilen loggen, `FinishSuccessAsync(counts)`.
   - Der FA-Name für die Meldung kommt aus der FA-Liste (`OrderNumber`), ohne zweiten Lesezugriff.
4. **`PickingOrderErzeugung`** bekommt `int ZeilenAngelegt`, gesetzt in der Schleife `PickingOrderWorkflow.cs:118-136` und übernommen bei
   `:147-149`. Das ist der kleinste Weg zum Zähler `zeilen_angelegt`, ohne die Aufträge nachzulesen.
5. **`SyncWorker`**: neuer Block nach der Arbeitsgang-Erkennung (`SyncWorker.cs:543`), also nach FA-Import (`:44`), BOM-Cache (`:205`),
   FA-Hierarchie (`:352`) und Materialisierung (`:372`). Damit sieht der Lauf die frischste Stückliste:
   - AKE: Cache zuerst (`BomRepository.cs:27-30`), gepflegt vom BOM-Cache-Sync.
   - IDEAL: lokale `FaHierarchyNodes`, gepflegt vom FA-Hierarchie-Sync.

   Ausführung über `RunResilientAsync("Kommissionier-Nachtrag", …)` mit eigenem Scope, wie `SyncWorker.cs:491-496`.
6. **Leitstand (Web):**
   - `AuftraegeNachziehen` bekommt `[RequireAdminAccess]` statt `[RequireLeitstandAccess]` (`PickingLeitstandController.cs:534`). Admin
     ist in `CanManagePickingReleaseAsync` enthalten (`CurrentUserService.cs:137-138`), der Klassenfilter `[RequirePickingOrLeitstandAccess]`
     (`:12`) bleibt.
   - `PickingLeitstandViewModel` bekommt `IsAdmin`, gesetzt in `Index` aus `IsAdminAsync()` (`CurrentUserService.cs:82-83`).
   - `Index.cshtml:86-93` und `:95-99` werden zusätzlich in `@if (Model.IsAdmin)` gefasst. Das JS ist bereits null-sicher (`:664`, `:733`).
   - `AltbestandGeprueft` (`:550-567`) je Rückfrage 3.
7. **Nebenläufigkeit** mit Freigaben, Knopf oder einem zweiten Lauf: kein neuer Mechanismus nötig, weil der Workflow das abdeckt.
   - Die Restmengen werden **innerhalb** der Serializable-Transaktion aus den aktiven Zeilen berechnet (`PickingOrderWorkflow.cs:100-103`,
     `PickingOrderRepository.cs:17-20, 72-79`). Die nächste Nummer wird dort ebenfalls gelesen (`:117`).
   - Eindeutige Indizes auf `(Jahr, LaufendeNummer)` und `Nummer` (`SQL/133_AddPickingAreasAndOrders.sql:166-171`).
   - Bei Deadlock oder Unique-Verletzung verwirft `Nummernkreis.MitVersuchenAsync` den Tracker und versucht es bis zu 3-mal in einer neuen
     Transaktion (`Nummernkreis.cs:30-49`). Der zweite Versuch rechnet die Reste neu und sieht die Zeilen der Gegenseite. Es entsteht also
     nichts doppelt.
   - Unter InMemory läuft die Aktion ohne Transaktion (`PickingOrderRepository.cs:74`). Gleichzeitigkeit ist damit nur manuell prüfbar (TS-191.9).
   - Die Altbestand-Prüfung (`:64`) und das Lesen der Stückliste (`:71`) liegen außerhalb der Transaktion, wie heute bei der Freigabe.

Muster: Repository-Pattern + Decorator (ADR 0001) unverändert; Aktivitäts-Protokoll (ADR 0010); ServiceSettings-Katalog (ADR 0008/0020);
Read/Edit-Split mit Action-Filter (ADR 0006). Keine neue Listen-View.

## Migrations-/SQL-Auswirkungen

Keine Migration, kein `SQL/XX_*.sql`, `00_FreshInstall.sql` unverändert. Die beiden neuen ServiceSettings legt der Web-Start aus dem Katalog an
(`IdealAkeWms/Program.cs:310-324`, idempotent). `SQL/AgentJobs/` ist nicht betroffen: Es gibt kein neues Pflichtfeld und keinen Folge-MERGE.

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `Sync:KommissionierNachtragEnabled` | Bool | `true` / `false` (Auswahl) | `true` | An: Der Dienst legt einmal täglich fehlende Kommissionieraufträge für Stücklisten-Nachträge freigegebener, offener HauptFAs an. Aus: kein Nachtrag; nur Admins können ihn im Leitstand auslösen. | Dienst: Lauf „Kommissionier-Nachtrag“ (`SyncWorker`); Aktivitäts-Protokoll | beide | „Legt Kommissionieraufträge an, die danach in der Kommissionierung erscheinen. Nach dem Einschalten bzw. dem ersten Dienststart läuft der Nachtrag einmal sofort.“ |
| `Sync:KommissionierNachtragUhrzeit` | Int | 0–23 (Bereich `(0, 23, "Uhr")` wie `ServiceSettingDefinitions.cs:231-233`, Server prüft beim Speichern) | `2` | Ab dieser Stunde läuft der Nachtrag einmal pro Tag. Ein verpasster Termin wird nach dem Dienststart einmal nachgeholt. | Dienst: Lauf „Kommissionier-Nachtrag“ | beide | – |

Katalog: Basisliste (Kategorie „Sync“, Beschreibung ≤ 500 Zeichen, `ServiceSettingDefinitionsTests.cs:50-60`), `Meta` mit Anzeigename,
Verwendung, Standort leer = beide, Warnhinweis (`ServiceSettingDefinitions.cs:35`), Int-Bereich (`:228-236`). Das erfüllt den Drift-Guard
(ADR 0008). Keine AppSetting-Änderung.

## Audit-Feld-Auswirkungen

- Neue `PickingOrder`/`PickingOrderLine` aus dem Nachtlauf: `CreatedAt = DateTime.Now` (Workflow `:53`), `CreatedBy` und `CreatedByWindows` =
  `KommissionierNachtrag` (über `DienstBenutzer`).
- Der Nachtlauf schreibt keine Updates an bestehenden Zeilen. Kommt Rückfrage 2 = (c), braucht es `ModifiedAt/By/ByWindows` an den
  geänderten Zeilen.
- Leitstand-Knopf (Admin) und „Altbestand geprüft“: unverändert aus `ICurrentUserService` des angemeldeten Benutzers.

## Akzeptanzkriterien

1. Mit Schalter an und Uhrzeit H startet der Dienst im ersten Zyklus ab H genau einen Lauf „Kommissionier-Nachtrag“. Weitere Zyklen am selben
   Tag starten keinen zweiten (Merker = „Run gestartet“ im Aktivitäts-Protokoll).
2. Ist seit dem letzten Termin kein Lauf erfolgt (z. B. erster Dienststart nach dem Deploy), startet der Lauf im ersten Zyklus nach dem Dienststart
   genau einmal.
3. Bei Schalter aus oder `WorkerSettings:SyncDryRun = true` gibt es keinen Lauf und keinen Merker. Im DryRun erscheint eine Log-Zeile.
4. Für einen freigegebenen, offenen HauptFA mit nach der Freigabe ergänzter Stücklistenposition legt der Lauf einen Auftrag an, der genau den
   Rest enthält (gleicher Bereich nach Regel, Prio und Kommissionierer vom FA). Ein zweiter Lauf legt nichts an (`ohne_rest`).
5. Aufträge und Zeilen aus dem Lauf haben `CreatedBy = CreatedByWindows = "KommissionierNachtrag"`.
6. FAs mit „Altbestand prüfen“, ohne Stückliste oder mit Fehler werden übersprungen bzw. gezählt (`altbestand_pruefen`, `ohne_stueckliste`,
   `fehler`) und je FA als Warnung protokolliert. Eine Ausnahme bei einem FA bricht den Lauf nicht ab.
7. Das Aktivitäts-Protokoll zeigt den Lauf unter „Kommissionier-Nachtrag“ mit den Zählern aus F5. `SyncLogServices.All` und `DisplayNames`
   enthalten ihn.
8. Sub-FAs, nicht freigegebene, erledigte, stornierte und vollständig kommissionierte FAs werden nicht angefasst.
9. Ein Benutzer mit Rolle `leitstand` ohne `admin` sieht im Leitstand keinen der beiden Knöpfe „Fehlende Kommissionieraufträge …“. Ein direkter
   POST auf `/PickingLeitstand/AuftraegeNachziehen` leitet auf AccessDenied um und legt nichts an.
10. Ein Admin sieht beide Knöpfe, und sie wirken wie bisher (TS-120.2/120.13).
11. Hilfe (`Help/Index`), Stückliste (`Picking/Bom`), Auftragsliste (`Picking/Index`), Workflow-Fehlertext und Altbestand-Meldung nennen den
    Nachtlauf und den Knopf nur noch als Admin-Notfall.
12. Die neuen Keys stehen in `/ServiceSettings` mit Anzeigename, Wirkung, Default und Warnhinweis. Die Uhrzeit außerhalb 0–23 wird beim Speichern
    abgelehnt.
13. Die DI-Auflösung im Dienst gelingt mit `ValidateOnBuild` (`KommissionierNachtragService`, `PickingOrderWorkflow` mit `DienstBenutzer`).
    Web und Dienst registrieren den BOM-Graph über dieselbe Methode, und `BomDiResolutionTests` ist grün.
14. *(nur bei Rückfrage 2 = b)* Liegt die bereits beauftragte Menge eines Stücklistenschlüssels über dem aktuellen Soll (oder ist die Position
    entfallen), schreibt der Lauf je FA eine Warnung mit Artikel, Position, Soll und beauftragter Menge und zählt `stueckliste_kleiner`.
    Offene Zeilen bleiben unverändert.
15. *(nur bei Rückfrage 3 = Empfehlung)* „Altbestand geprüft“ durch einen Leitstand-Benutzer legt danach sofort die fehlenden Aufträge dieses
    FA an und meldet sie wie die Freigabe.

## Test-Szenarien

Neues Kapitel **TS-191 — Kommissionier-Nachtrag nachts, Knopf nur für Admins** in `docs/TESTSZENARIEN.md`. Stand Worktree: höchstes Kapitel
TS-190 (`docs/TESTSZENARIEN.md:13864`). Der Dev-Lauf prüft die Nummer erneut, weil andere Entwürfe vom 2026-11-24 ebenfalls „nächste freie“
beanspruchen. Danach `secondbrain/tests/testszenarien-index.md` nachziehen. In TS-120.2, 120.6 und 120.13 einen Hinweis ergänzen: „Knopf
seit v1.99.x nur Admin; Nachtrag automatisch → TS-191“.

Vorbedingungen: DEV AKE und DEV IDEAL, Leitstand aktiv, ein Admin- und ein Leitstand-Konto (ohne `admin`). Zu Unit-Tests: Die Gleichzeitigkeit
ist unter InMemory nicht prüfbar (`PickingOrderRepository.cs:74`), Rollen-UAT braucht ein echtes Nicht-Admin-Konto.

| Nr | Schritte | Erwartet |
|---|---|---|
| 191.1 | AKE: freigegebenen FA mit Aufträgen wählen; Testsystem: Stücklistenposition ergänzen bzw. Menge erhöhen (Sage-View/BOM-Cache); `Sync:KommissionierNachtragUhrzeit` = aktuelle Stunde; Merker vorher per Diagnose prüfen; einen Dienst-Zyklus abwarten | Ein neuer KO-Auftrag nur mit dem Rest; `CreatedBy` = KommissionierNachtrag; Protokoll „Kommissionier-Nachtrag“ mit `auftraege_angelegt ≥ 1`, Info-Zeile mit FA und KO-Nummer |
| 191.2 | Nächsten Zyklus abwarten; dann Uhrzeit auf die nächste Stunde setzen und danach abwarten | Kein zweiter Lauf in derselben Stunde; der nächste Lauf legt 0 an, `ohne_rest` = Zahl der FAs |
| 191.3 | FA mit „Altbestand prüfen“ freigegeben; Lauf auslösen | `altbestand_pruefen` = 1, Warnzeile mit FA; kein Auftrag |
| 191.4 | Freigegebener FA ohne Stückliste | `ohne_stueckliste` = 1, Warnung |
| 191.5 | Schalter aus → Zyklus; Schalter an + `WorkerSettings:SyncDryRun = true` → Zyklus | Kein Lauf im Protokoll; im DryRun Log-Hinweis |
| 191.6 | Dienst stoppen, Uhrzeit auf eine schon vergangene Stunde, Dienst starten | Genau ein nachgeholter Lauf |
| 191.7 | IDEAL (hierarchisch): HauptFA freigegeben, Position mit Kommissionieren-Kennzeichen im Struktur-Test ergänzen; Lauf | Auftrag nur mit relevanten Positionen (`KommissionierRelevanzFilter`); Sub-FAs nie |
| 191.8 | Leitstand als Leitstand-Benutzer öffnen; markieren; POST `/PickingLeitstand/AuftraegeNachziehen` (z. B. per Browser-Konsole mit Token) | Beide Knöpfe fehlen; POST → AccessDenied, kein Auftrag. Als Admin: beide Knöpfe sichtbar und wirksam |
| 191.9 | Negativ/Gleichzeitig: Lauf auslösen und während des Laufs denselben FA im Leitstand (Admin) nachziehen | Keine doppelten Zeilen (Diagnose unten liefert nichts); höchstens Warnzeile „Versuch … gescheitert“ im Log |
| 191.10 | *(Rückfrage 2 = b)* Stücklistenmenge eines beauftragten Artikels senken; Lauf | Warnung „Stückliste kleiner“ mit Artikel/Position/Soll/beauftragt; offene Zeile unverändert |
| 191.11 | *(Rückfrage 3 = Empfehlung)* Als Leitstand-Benutzer „Altbestand geprüft“ | Sofort Auftrag/Aufträge für diesen FA, Meldung mit Nummern |
| 191.12 | Hilfe, Stückliste ohne Aufträge, leere Auftragsliste ansehen | Texte nennen den Nachtlauf, den Knopf nur als Admin |

Diagnose-Abfrage (TS-191.9; gleicher Stücklistenschlüssel mehrfach durch den Nachtlauf angelegt, soll leer sein; `Storniert = 90`,
`Models/PickingOrder.cs:16`, gespeichert als Zahl):

```sql
SELECT l.ProductionOrderId, l.ArticleNumber, l.BomPosition, COUNT(*) AS Zeilen, SUM(l.SollMenge) AS Soll
FROM dbo.PickingOrderLines l
JOIN dbo.PickingOrders o ON o.Id = l.PickingOrderId
WHERE o.Status <> 90
  AND o.CreatedBy = 'KommissionierNachtrag'
GROUP BY l.ProductionOrderId, l.ArticleNumber, l.BomPosition
HAVING COUNT(*) > 1;
```

Grenze: Die Abfrage sieht nur Doppel innerhalb der Nachtlauf-Aufträge. Ein Doppel zwischen Nachtlauf und gleichzeitiger Freigabe findet man nur,
indem man für den Test-FA `SUM(SollMenge)` je Schlüssel über alle nicht stornierten Aufträge mit dem Stücklisten-Soll vergleicht (von Hand,
TS-191.9).

## Etappen (nur bei epic: true)

Kein Epic.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Vorläufig, der QA-Lauf bestätigt es gegen den echten Diff.*

- **Web-App:** ja (Leitstand-Filter/View, Texte, gemeinsame Registrierung, Katalog-Seed der neuen ServiceSettings)
- **Service:** ja (neuer Lauf, DI)
- **Migration:** nein
- **Reihenfolge:** Web zuerst (seedet die Keys, `IdealAkeWms/Program.cs:310-324`), dann Dienst. Ohne Seed liest der Dienst die Defaults
  (`GetBoolSafeAsync`/`GetIntSafeAsync`). Der erste Dienststart holt den Lauf sofort einmal nach (F2), auch tagsüber. Das ist gewollt und
  idempotent, an AKE nach dem Einmal-Lauf vom 2026-11-24 voraussichtlich ohne neue Aufträge.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022; Version = nächste freie, Stand Worktree 1.99.69 → voraussichtlich 1.99.70):
  - `@{ Id='v1.99.70-KommNachtrag-Lauf'; Version='1.99.70'; Typ='Einstellung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='Neuer Dienst-Lauf "Kommissionier-Nachtrag": Sync:KommissionierNachtragEnabled (Default true), Sync:KommissionierNachtragUhrzeit (Default 2). Der erste Dienststart holt den Lauf einmal sofort nach - danach Aktivitaetsprotokoll "Kommissionier-Nachtrag" pruefen (Zaehler fehler/altbestand_pruefen).'; Quelle='2026-11-25-kommissionierauftraege-nachtrag-nachts-spec' }`
  - `@{ Id='v1.99.70-KommNachtrag-Knopf'; Version='1.99.70'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='Leitstand: Knoepfe "Fehlende Kommissionierauftraege ... erzeugen" nur noch fuer Admins; Nachtraege legt der Dienst nachts an. Leitstand-Benutzer informieren.'; Quelle='2026-11-25-kommissionierauftraege-nachtrag-nachts-spec' }`
  - Keine Schema-, Daten-, Einmal- oder Prüfungs-Einträge: Es gibt kein SQL. Der Seed läuft beim Web-Start.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` und
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

**Brain-Nachzug (Dev-Lauf, Hauptcheckout):**
- `codebase/services.md`: Lauf und Keys
- `codebase/controller.md:165`: `AuftraegeNachziehen` → `[RequireAdminAccess]`
- `feature-map.md`, Changelog
- `fallstricke.md`: „Web-Workflow im Dienst: `ICurrentUserService` gibt es dort nur als `DienstBenutzer`; Rollenabfragen werfen“ (mit Warum)
- Anwender- und Betriebsdoku laut `affected_code`

## Offene Rueckfragen

1. **Was heißt „Admin“?**
   - Empfehlung: die Rolle `admin` über das vorhandene `[RequireAdminAccess]` (`Filters/RequireAdminAccessAttribute.cs:23`,
     `CurrentUserService.cs:82-83`). `admin` ist die Wildcard des Rollenkonzepts (`codebase/controller.md:47-48`, ADR 0006). Keine neue
     Rolle, keine Änderung an `RoleOverview`.
   - Alternative: ein eigenes Recht, z. B. `leitstand_nachtrag`. Das braucht laut CLAUDE.md drei Stellen (Attribut, `controller.md`,
     `Views/Users/RoleOverview.cshtml`) und eine Rollenpflege an beiden Standorten. Lohnt nur, wenn Nicht-Admins (Leitstand-Leitung) den
     Notfall-Knopf behalten sollen.
2. **Stückliste nach der Freigabe kleiner geworden** (Menge gesenkt oder Position entfernt). Heute bleibt das offene Soll unverändert und es gibt
   keine Meldung (Beleg im Abschnitt „Heutiges Verhalten“). Optionen:
   - (a) nur dokumentieren
   - (b) **Empfehlung:** nichts ändern, aber der Nachtlauf meldet je FA eine Warnung „Stückliste kleiner: Artikel, Position, Soll,
     beauftragt“ und zählt `stueckliste_kleiner`. Eine kleine reine Funktion neben `PickingOrderRestmengen.Berechne`, gleiche Abdeckungs-Regel
     `Abgedeckt` (`PickingOrderRestmengen.cs:19-27`). Die Leitstand-Leitung entscheidet dann selbst (Zeile im Auftrag nicht umbuchen,
     „Vollständig“ abschließen).
   - (c) offene Zeilen automatisch kürzen bzw. auf „Entfallen“ setzen. Das greift in laufende Kommissionierung ein: Zeilen können schon
     „gepickt“ sein. Es braucht eigene Regeln für gepickt/umgebucht und Audit-Updates. Die Empfehlung ist, (c) erst mit einer eigenen Spec
     anzugehen.
3. **„Altbestand geprüft“ ohne Admin-Knopf.** Nach der Bestätigung (`PickingLeitstandController.cs:550-567`) verweist die Meldung heute auf den
   Knopf, den ein Leitstand-Benutzer künftig nicht mehr sieht. Optionen:
   - **Empfehlung:** Die Action ruft danach für diesen einen FA `ErzeugeOffeneAsync(id)` auf und meldet wie die Freigabe. Das ist eine Zeile,
     derselbe Erzeugungsweg, und die Aufträge sind sofort da.
   - Alternative: Die Meldung lautet „Aufträge legt der Nachtlauf an“, und der FA wartet bis zur nächsten Nacht.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

(keine – kein Jira-Vorgang)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. „Admin“ = Rolle `admin` über `[RequireAdminAccess]` oder eigenes Recht? →
2. Kleiner gewordene Stückliste: (a) dokumentieren, (b) melden im Nachtlauf, (c) offene Zeilen kürzen? →
3. „Altbestand geprüft“: sofort Aufträge für diesen FA anlegen oder bis zur Nacht warten? →
