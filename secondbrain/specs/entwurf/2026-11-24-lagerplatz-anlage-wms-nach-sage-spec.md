---
type: spec
title: Lagerplätze im WMS erfassen und per Direkt-SQL in Sage anlegen (über die Outbox, austauschbar gegen SData)
slug: 2026-11-24-lagerplatz-anlage-wms-nach-sage-spec
status: Entwurf
created: 2026-11-24
updated: 2026-11-24
source_backlog: "[[2026-11-20-lagerplatz-anlage-wms-nach-sage]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: beide
affected_code:
  - "Outbox-Helfer: IdealAkeWms/Data/SageStammdatenOutbox.cs:14 (TypLagerplatz), :18-27 (Vormerken, speichert nicht), :30-45 (ErledigenAsync -> Bestaetigt)"
  - "Outbox-Entität: IdealAkeWms/Models/SageStammdatenOutboxItem.cs:12-37 (EntityType, EntityKey = WMS-Id, PayloadJson nur Nachvollziehbarkeit :24-26, Status SageBookingQueueStatus :29, AttemptCount :32, LastError :36); Tabelle ApplicationDbContext.cs:1267-1282; SQL/107"
  - "Status-Enums: IdealAkeWms/Models/SageBookingQueueStatus.cs:11-17 (Offen 0, Gesendet 1, Bestaetigt 2, Fehler 3); IdealAkeWms/Models/Creditor.cs:150-156 (SageUebergabeStatus NichtNoetig/Offen/Uebergeben/Fehler), :158-172 (SageUebergabeStatusText, IstOffen = Offen|Fehler)"
  - "Schreiber der Outbox heute: IdealAkeWms/Data/Repositories/StorageLocationRepository.cs:17-27 (AddAsync merkt bei Status Offen vor); IdealAkeWms/Controllers/StorageLocationsController.cs:103-120 (Create setzt Offen :111)"
  - "Leser/Erlediger der Outbox heute: IDEALAKEWMSService/Services/LagerplatzSyncService.cs:174-189 (manueller Platz mit Übergabe offen + gleicher Code -> Source Sage, Uebergeben, ErledigenAsync); sonst kein Leser, kein Worker"
  - "Liste „Offene Übergaben an Sage“: StorageLocationsController.cs:55-92 (Filter onlyUebergabeOffen :70-71, Zähler :81, ColumnMap :30-45); Views/StorageLocations/Index.cshtml:32-51 (Hinweistext „Das WMS sendet nichts an Sage“ :38-39), :103/:144 (Spalte Übergabe an Sage)"
  - "Lagerplatz-Modell: IdealAkeWms/Models/StorageLocation.cs:12-15 (Code 50), :36-38 (Source), :61-71 (SageLagerkennung, SageLagerplatzId), :78-80 (StorageAreaId), :109-117 (Übergabe-Felder), :121-130 (Validate: manuelle Codes max. 12 Zeichen); Unique-Index Code ApplicationDbContext.cs:236"
  - "Lagerort-Modell: IdealAkeWms/Models/StorageArea.cs:11-37 (nur vom Lagerplatz-Abgleich bei Quelle SData geschrieben :7); Unique Lagerkennung ApplicationDbContext.cs:267"
  - "StorageArea-Schreiber (einzige): IDEALAKEWMSService/Services/LagerplatzSyncService.cs:152 und :217 (nur Sage-Plätze), :313-379 (SyncStorageAreasAsync, nur wenn Quelle Lagerorte liefert :319-320)"
  - "Lese-Abgleich: IDEALAKEWMSService/Services/SageLagerplatzReader.cs:18-29 (SageConnection, KHKLagerorte LEFT JOIN KHKLagerplaetze, Mandant = 1, Aktiv = -1); LagerplatzSyncService.cs:80-98 (Dubletten Kurzbezeichnung -> übersprungen), :124-126 (Code = Kurzbezeichnung, Zone = Lagerkennung), :136-167 (Neuanlage, IstBuchbar false), :191-231 (Sage-Zweig überschreibt Description/Zone), :232-238 (manueller Platz = Konflikt), :243-261 (nicht im Feed -> deaktiviert)"
  - "Sage-Verbindung: IDEALAKEWMSService/appsettings.json:4 (ConnectionStrings:SageConnection, Trusted_Connection); IDEALAKEWMSService/Common/ConnectionStrings.cs:17; Web hat KEINE Sage-SQL-Verbindung (IdealAkeWms/appsettings.json:2-5); ConnectionStrings bewusst nicht im ServiceSettings-Katalog (IdealAkeWms/Models/ServiceSettingDefinitions.cs:14)"
  - "Kein bestehender Direkt-Schreibweg nach Sage: Suche nach INSERT/UPDATE KHK*, spSysTan, KHKTan in *.cs/*.sql ohne Treffer (Bündel-Worktree, 2026-11-24)"
  - "Worker-Muster: IDEALAKEWMSService/Workers/SageBookingWorker.cs:14-15 (genau eine Instanz), :136-150 (Schritt mit eigenem try), :265-292 (ProcessSageFaArbeitsgaengeAsync: Schalter nicht Safe lesen :268, Fehler nur einmal melden :272-278); Program.cs:161-177 (DI der Schritte)"
  - "Schalter-Muster: IdealAkeWms/Services/SageFaArbeitsgaenge.cs:16-21 (ToggleKey, ServiceActor); ServiceSettingDefinitions.cs:75-78 (Meta mit Standort/Warnhinweis); Web liest Schalter über IServiceSettingRepository (SageFaArbeitsgaengeController.cs:23, :121, :161)"
  - "Aktivitäts-Protokoll: IdealAkeWms/Services/SyncLogger/SyncLogServices.cs:44, :66, :108 (Muster SageFaArbeitsgaenge)"
  - "Rollen: secondbrain/codebase/controller.md:53-54 (RequireMasterDataAccess = admin, masterdata; Read = + masterdata_read)"
open_questions:
  - "1. Umfang: nur Lagerplätze in bestehenden Sage-Lagern (Empfehlung) oder auch Lager (Lagerort, KHKLagerorte) aus dem WMS in Sage anlegen? Backlog-Titel nennt beides, G5 und die Outbox kennen nur Lagerplätze."
  - "2. Schreib-Login: eigener Connection String ConnectionStrings:SageSchreibConnection mit eigenem Login und Minimalrechten (Empfehlung) oder die bestehende SageConnection um Schreibrechte erweitern?"
  - "3. Rolle für „In Sage anlegen“ und „Erneut senden“: masterdata wie die heutige Lagerplatz-Neuanlage (Empfehlung) oder nur admin wie „Arbeitsgänge → Sage“?"
  - "4. Standort: nur IDEAL einschalten (Mitschnitt nur am IDEAL-Testmandanten) und AKE dauerhaft aus, oder ist AKE ebenfalls vorgesehen (dann P-1 bis P-7 auch gegen die AKE-Sage-DB)?"
  - "5. Testsystem für den Abnahmetest: welches WMS-Testsystem (Dienst) wird an den Testmandanten IDEAL_TEST_2026_05_03 auf idealsql2\\sage100test angebunden, und wer legt dort in Sage die zwei identischen Testlager an?"
  - "6. DPS: soll DPS (Michael Wiesner) über den Direkt-SQL-Schreibweg informiert bzw. um Bestätigung gebeten werden (Support/Gewährleistung bei Sage-Updates), bevor am Produktivmandanten eingeschaltet wird?"
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

## Ziel / Nutzen (das Warum)

Neue Lagerplätze entstehen heute zweimal: im WMS (Stammdaten → Lagerplätze → „Neuen Lagerplatz anlegen“) und ein zweites Mal von Hand in
Sage. Seit v1.73.0 merkt das WMS einen neu angelegten Platz zwar als „Offene Übergabe an Sage“ vor (Outbox `SageStammdatenOutbox`), **sendet
aber nichts** (`SageStammdatenOutboxItem.cs:7`, Hinweistext `Index.cshtml:38-39`). Grund war G5 der SData-Spec: Es gibt keinen SData-Service für
die Lagerplatz-Anlage.

Ziel: Ein im WMS erfasster Lagerplatz wird vom Dienst **automatisch** in Sage angelegt, mit genau dem Ergebnis, das der Sage-Dialog
„Lagerplätze anlegen“ erzeugt (Anhang [[2026-11-20-WMS_Lagerplatzanlage_Sage_v2]]). Danach ist der Platz ein normaler Sage-Platz im WMS (Quelle
„Sage“, Sage-Platz-Id gesetzt, buchbar nach Sage), ohne zweite Erfassung und ohne Dublette.

**Entscheid Gerald 2026-11-24 (nicht erneut fragen):**
1. Jetzt umsetzen, mit **Direkt-SQL in Sage** nach dem Anhang (Nummernvergabe über `spSysTan` wie im Mitschnitt).
2. Liefert DPS (Wiesner, Block 5 in [[2026-11-20-dps-consulting-tag-fragen]]) später einen SData-Service, wird umgebaut. Deshalb liegt der
   Schreibweg hinter **einer** austauschbaren Stelle (Interface mit einer Implementierung, hier ausdrücklich gerechtfertigt, weil der Austausch
   angekündigt ist). Die Übergabe läuft über die bestehende Outbox.
3. Eigene **neue ADR** für den Direkt-SQL-Schreibweg (Rechte SQL-Login, Updatefestigkeit bei Sage-Versionswechsel, Support DPS, Transaktion,
   Idempotenz, Rückbau auf SData) als Teil der Umsetzung.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neue Erfassung „Lagerplätze in Sage anlegen“ in Stammdaten → Lagerplätze: Lagerort wählen (aus `StorageAreas`), Bereich je Dimension von–bis,
  Buchbar/Sage-Buchung erlaubt. Das WMS legt die Plätze sofort an (Status „Offen“, Outbox-Eintrag je Platz).
- Neuer Schritt im `SageBookingWorker`: Outbox-Einträge dieser Plätze per Direkt-SQL in Sage anlegen (eine Transaktion je Platz), danach den
  WMS-Platz übernehmen (Quelle Sage, `SageLagerplatzId`, Status „Übergeben“, Outbox „Bestätigt“).
- Austauschbare Schreibstelle `ISageLagerplatzAnlage` mit genau einer Implementierung (Direkt-SQL).
- Fehler sichtbar in der Lagerplatzliste (Filter „Offene Übergaben an Sage“, neue Spalte „Übergabe-Fehler“, Knopf „Erneut senden“) und im
  Aktivitäts-Protokoll.
- Schalter `SageLagerplatzAnlageAktiv` (ServiceSetting, Default aus, je Standort-DB).
- Neue ADR 0036 (Nummer prüft der Dev-Lauf), Hilfe, README, Testszenario mit dem Abnahmetest aus Anhang Abschnitt 9.

**Out-of-Scope (Scope-Grenzen aus Anhang Abschnitt 9 und 7)**
- **Lager (Lagerort) anlegen** (`KHKLagerorte`-INSERT, Anhang 4 und 8.1): nicht in diesem Paket, solange Rückfrage 1 nicht anders entscheidet.
  Damit entfallen hier auch WarenflussKZ, WE/WA-Kennzeichen und die Sperre von `HWE`/`HWA`. Falls Rückfrage 1 = ja: Folge-Spec, dort gilt
  zwingend: nur `SWF`, `WE` und (nach Kurztest) `WA` zulassen, `HWE`/`HWA` nie (Anhang 4.5, 2.8).
- **Lager umbenennen** (Anhang 6, 8.3): nicht angeboten. Die Variante „Nein“ ist nicht mitgeschnitten (Anhang 6, Abschnitt 9 Punkt 5).
- **Lagertechnik eines bestehenden Lagers ändern**: nicht angeboten (Anhang Abschnitt 9 Punkt 8, nicht untersucht).
- **Löschen und Deaktivieren in Sage**: das WMS löscht nichts in Sage (Anhang 7). Ein im WMS deaktivierter Platz wird nicht gesendet bzw. bleibt
  in Sage unverändert.
- **Platzbezeichnung ändern** in Sage: nein. Nach der Übernahme gilt die Sage-Platzbezeichnung (Sage führend, wie E9).
- **BDE-Mitarbeiter** (`TypBdeMitarbeiter`, `SageStammdatenOutbox.cs:15`): nicht in diesem Paket.
- **Alt-Einträge**: Plätze, die seit v1.73.0 über „Neuen Lagerplatz anlegen“ mit Status „Offen“ entstanden sind (frei gewählter Code, kein
  Lagerort), sendet der Dienst **nicht**. Für sie gilt weiter „in Sage von Hand mit demselben Code anlegen“ (E9-Übernahme bleibt).

## Fachliche Anforderungen

**F1 Erfassung.** In Stammdaten → Lagerplätze gibt es neben „Neuen Lagerplatz anlegen“ den Knopf „Lagerplätze in Sage anlegen“ (nur bei
eingeschaltetem Schalter und Schreibrecht, Rolle siehe Rückfrage 3). Das Formular fragt:
- Lagerort: Auswahl aus `StorageAreas` mit `IsActive = true` und `SageMissingSince IS NULL` (Anzeige Lagerkennung + Bezeichnung + Lagertechnik).
- Dimension 1 von–bis (ganze Zahl ≥ 1), Dimension 2 von–bis, Dimension 3 von–bis (≥ 0; „0–0“ = Dimension nicht genutzt). Hinweis am Formular:
  „Nicht genutzte Dimensionen auf 0 lassen. Ob die Koordinaten zur Lagertechnik passen, prüft Sage beim Anlegen.“
- Buchbar, Sage-Buchung erlaubt (Vorbelegung wie bei „Neuen Lagerplatz anlegen“).

Die Lagerort-Liste ist bewusst die WMS-Tabelle `StorageAreas`: Das Web hat keine Sage-SQL-Verbindung (`IdealAkeWms/appsettings.json:2-5`).
`StorageAreas` füllt nur der Lagerplatz-Abgleich mit Quelle SData (`StorageArea.cs:7`, `LagerplatzSyncService.cs:319-320`). Ist die Liste leer,
zeigt das Formular statt der Auswahl: „Keine Sage-Lagerorte im WMS. Der Lagerplatz-Abgleich muss mit Quelle SData laufen
(Service-Einstellung Sync:Quelle:Lagerplaetze), damit Lagerorte zur Auswahl stehen.“ (Prüfschritt P-8.)

**F2 Code und Platzanlage im WMS.** Je Koordinate des kartesischen Bereichs (Reihenfolge D1 außen, D3 innen, wie Sage, Anhang 5.1) entsteht ein
WMS-Platz:
- `Code` = `BarcodeValue` = `<Lagerkennung>;<D1>;<D2>;<D3>`, immer drei Zahlen, ungenutzte als 0 (= Sage-Kurzbezeichnung, Anhang 5.2). Das ist der
  Schlüssel, über den der Lese-Abgleich Plätze zuordnet (`LagerplatzSyncService.cs:124`, `:136`).
- `Zone` = Lagerkennung (so schreibt sie der Abgleich, `LagerplatzSyncService.cs:125`), `StorageAreaId` = gewählter Lagerort, `Description` leer
  bis zur Übergabe.
- `Source = Manual`, `SageUebergabeStatus = Offen`, Outbox-Eintrag je Platz über das bestehende `StorageLocationRepository.AddAsync`
  (`StorageLocationRepository.cs:17-27`); Payload um Lagerkennung und D1–D3 ergänzt (nur Nachvollziehbarkeit, `SageStammdatenOutboxItem.cs:24`).
- Koordinaten, deren Code im WMS schon existiert, werden übersprungen (wie Sage vorhandene Kombinationen überspringt, Anhang 5.1). Rückmeldung:
  „n Lagerplätze angelegt, m übersprungen (Code im WMS schon vorhanden: …)“ als `SuccessMessage` bzw. `WarningMessage`.
- Höchstens 200 Plätze je Absenden; mehr = Formularfehler mit der berechneten Anzahl (kurze Transaktionen, Anhang 2.6).
- **Kennzeichen „Sage-Anlage“** = `Source == Manual` und `StorageAreaId != null`. Das ist eindeutig, weil `StorageAreaId` heute nur der Abgleich
  setzt, und nur an Sage-Plätzen (`LagerplatzSyncService.cs:152`, `:217`, einzige Schreibstellen, Suche 2026-11-24). Das bestehende
  `Create` muss deshalb `StorageAreaId` aus dem POST ignorieren (Überposting, AK 3). Keine neue Spalte.

**F3 Senden (Dienst).** Bei eingeschaltetem Schalter verarbeitet der Dienst im Takt des `SageBookingWorker` alle Outbox-Einträge
`EntityType = 'StorageLocation'` mit Status `Offen` (sowie hängende `Gesendet`, F5), deren Platz Sage-Anlage ist (F2) und `IsActive = true`, in
Reihenfolge der Outbox-Id. Je Eintrag:
1. Outbox-Status vor dem Sage-Aufruf auf `Gesendet`, `AttemptCount + 1`, Audit (Muster „Status vor dem Call“, `SageBookingWorker.cs:14-20`).
2. Aufruf `ISageLagerplatzAnlage.AnlegenAsync(Lagerkennung, D1, D2, D3)`. Lagerkennung aus `StorageArea.Lagerkennung`, Koordinaten aus dem Code;
   passt der Code nicht zum Muster `<Lagerkennung>;<int>;<int>;<int>` mit derselben Lagerkennung, wird nichts gesendet (fachlicher Fehler).
3. Ergebnis **angelegt** oder **in Sage vorhanden**: WMS-Platz übernehmen: `Source = Sage`, `SageLagerplatzId` = PlatzID, `SageLagerkennung` =
   Code, `Description` = Sage-Platzbezeichnung, `SageUebergabeStatus = Uebergeben`, `SageUebergabeAm = jetzt`, `SageUebergabeFehler = null`,
   Outbox `Bestaetigt` (über `SageStammdatenOutbox.ErledigenAsync` erweitert um `Gesendet`), Audit. Info im Protokoll: „Lagerplatz <Code> in Sage
   angelegt (PlatzID n)“ bzw. „… war in Sage schon vorhanden (PlatzID n) – übernommen“.
4. **Fachlicher Fehler** (Konflikt, Lager inaktiv, Koordinaten passen nicht, Text zu lang …): Outbox `Fehler` + `LastError`, Platz
   `SageUebergabeStatus = Fehler` + `SageUebergabeFehler` = Klartext. Nichts in Sage geschrieben (Rollback). Warnung im Protokoll.
5. **Technischer Fehler** (Verbindung, Rechte, Timeout, Deadlock): Sage-Transaktion ist zurückgerollt; Outbox zurück auf `Offen`, `LastError` und
   `SageUebergabeFehler` = „Letzter Versuch fehlgeschlagen: …“, Platzstatus bleibt `Offen`. Rest des Takts wird übersprungen; der Lauf meldet
   denselben Fehlertext nur einmal (Muster `SageBookingWorker.cs:272-278`), Fehlermail über `NotifyErrorAsync` wie die anderen Schritte.

**F4 Datenhoheit nach der Übergabe.** Ab der Übernahme ist der Platz ein Sage-Platz: Der Lese-Abgleich findet ihn über den Code im Sage-Zweig
(`LagerplatzSyncService.cs:191-231`) und legt **keine** Dublette an; `Zone`, `Description`, `SageLagerplatzId` stimmen bereits, also kein Diff.
`IstBuchbar`/`SageBuchungErlaubt` fasst der Sage-Zweig nie an (E9-Regel, SData-Spec F11). Die Bearbeitung folgt der Sage-Regel
(`StorageLocationsController.cs:147-155`). Folge wie bei E9, als Info ausgewiesen: ab dem nächsten Lauf gleicht der Lagerbestand-Abgleich den
Bestand dieses Platzes mit Sage ab. Kommt der Lese-Abgleich zwischen Sage-Commit und WMS-Speichern (F5), übernimmt er den Platz selbst über den
bestehenden E9-Zweig (`LagerplatzSyncService.cs:174-189`) – gleiches Ergebnis.

Umkehr der Datenhoheit nach [[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]]: Das WMS führt nur **bis zur Übergabe** (Anlage),
danach ist Sage führend. Das ist das Muster „Sage führend, Abweichung melden“: Jeder Konflikt (F5) wird gemeldet, nie still überschrieben.

**F5 Idempotenz und Doppelanlage.** Der Sage-Schreibbefehl ist selbst idempotent: In derselben Transaktion prüft er mit Sperre, ob es den Platz
schon gibt, und legt nur an, wenn nicht. Fälle:
- Platz mit gleichen Koordinaten **und** gleicher Kurzbezeichnung existiert genau einmal → „in Sage vorhanden“, PlatzID zurück, nichts geschrieben.
- Koordinaten existieren mit **anderer** Kurzbezeichnung, oder die Kurzbezeichnung existiert mit **anderen** Koordinaten, oder es gibt mehrere
  Treffer → fachlicher Fehler „Konflikt in Sage: …“ (Klartext mit PlatzID/Kurzbezeichnung), nichts geschrieben. Hintergrund: Sage hat auf
  `KHKLagerplaetze` keinen Unique-Index außer `(PlatzID, Mandant)` (Anhang 2.4).
- Hängender `Gesendet`-Eintrag (Dienst-Abbruch nach dem Sage-Commit, vor dem WMS-Speichern): Er wird ab `Sync:SageLagerbuchungStuckMinutes`
  (vorhandener Key, `SageBookingWorker.cs:285`) einfach erneut verarbeitet; der idempotente Befehl liefert „vorhanden“ → Übernahme. Kein eigener
  Wiederherstellungs-Lookup.
- Restrisiko: Der Sage-Dialog sperrt nicht wie das WMS. Legt ein Anwender gleichzeitig denselben Platz in Sage an, kann eine Dublette entstehen.
  Sie wird sichtbar: der Lese-Abgleich meldet doppelte Kurzbezeichnungen als Warnung und überspringt sie (`LagerplatzSyncService.cs:80-98`).

**F6 Fehler sichtbar, nicht still.** Die Lagerplatzliste bekommt die Spalte „Übergabe-Fehler“ (`SageUebergabeFehler`, Listen-View-Pattern), im
Filter „Offene Übergaben an Sage“ je Zeile mit Status „Fehler“ den Knopf „Erneut senden“ (Outbox `Fehler → Offen`, `AttemptCount = 0`,
Platz `Fehler → Offen`, Fehlertext leer, Audit des Anwenders). Der Hinweistext `Index.cshtml:38-39` unterscheidet nach Schalter:
- Schalter aus: wie heute („Bitte in Sage mit demselben Code anlegen … Das WMS sendet nichts an Sage.“).
- Schalter an: „Über ‚Lagerplätze in Sage anlegen‘ erfasste Plätze legt der Dienst in Sage an (Status ‚Offen‘ = wartet, ‚Fehler‘ = siehe Spalte
  Übergabe-Fehler). Andere offene Plätze bitte in Sage mit demselben Code anlegen.“
Ein Platz mit Fehler bleibt im Zähler der offenen Übergaben (`IstOffen` = Offen|Fehler, `Creditor.cs:171`).

**F7 Bearbeiten eines Sage-Anlage-Platzes vor der Übergabe.** `Code`, `Description`, `Zone` sind nicht änderbar (Code ist der Schlüssel zu Sage);
`IsActive`, `IstBuchbar`, `SageBuchungErlaubt`, `Capacity`, `IsPickingTransport` schon. Das Speichern darf nicht an der 12-Zeichen-Regel für
manuelle Codes scheitern (`StorageLocation.cs:124`). Falsch erfasste Plätze: deaktivieren und neu erfassen; ein inaktiver Platz wird nicht
gesendet (F3).

## Technischer Loesungsentwurf

**Bausteine (Leiter: vorhandene Muster wiederverwenden)**

| Baustein | Ort | Muster/Vorlage |
|---|---|---|
| `ISageLagerplatzAnlage` + `SqlSageLagerplatzAnlage` | `IDEALAKEWMSService/Services/` | Reader-Interfaces (`ISageLagerplatzReader.cs:15-18`); einzige gerechtfertigte Abstraktion (Entscheid 2) |
| `SageLagerplatzAnlageService.RunOnceAsync` | `IDEALAKEWMSService/Services/` | `SageFaArbeitsgangService` (Schritt im Worker, `Program.cs:161-168`) |
| Schritt `ProcessSageLagerplatzAnlageAsync` | `SageBookingWorker` | eigener try-Block wie `:136-150`, Schalter nicht Safe lesen wie `:268` |
| `SyncLogServices.SageLagerplatzAnlage`, Anzeige „Lagerplätze → Sage“ | `SyncLogServices.cs` | `:44`, `:66`, `:108`; Counts `angelegt`, `vorhanden`, `fehler`, `uebersprungen` |
| Schalter-Konstanten `SageLagerplatzAnlage.ToggleKey`, `ServiceActor = "system:sage-lagerplatz-anlage"` | `IdealAkeWms/Services/` | `SageFaArbeitsgaenge.cs:16-21` |
| Formular `CreateInSage` (GET/POST) + ViewModel | `StorageLocationsController`, `Views/StorageLocations/CreateInSage.cshtml` | Create-Action `:94-120` |
| „Erneut senden“ (POST) | `StorageLocationsController` | `[RequireMasterDataAccess]` (Rückfrage 3), `ValidateAntiForgeryToken` |

**Schnittstelle (eine Implementierung, Austausch gegen SData angekündigt)**

```csharp
public enum SageLagerplatzAnlageArt { Angelegt, Vorhanden, Abgelehnt }
public record SageLagerplatzAnlageErgebnis(SageLagerplatzAnlageArt Art, int? PlatzId, string? Platzbezeichnung, string? Grund);
public interface ISageLagerplatzAnlage
{
    // Fachliche Ablehnung = Ergebnis Abgelehnt mit Grund; technische Fehler = Exception (Aufrufer: F3 Punkt 5).
    Task<SageLagerplatzAnlageErgebnis> AnlegenAsync(string lagerkennung, int d1, int d2, int d3, CancellationToken ct);
}
```

Die Outbox, der Status-Automat, die Übernahme in F3 Punkt 3 und die gesamte Oberfläche hängen nur an diesem Vertrag. Rückbau auf SData = neue
Implementierung registrieren, `SqlSageLagerplatzAnlage` löschen, ADR 0036 superseden.

**Sage-Schreibbefehl je Platz (eine Transaktion)** – Ableitung aus Anhang 8.2 für genau einen Platz, mit Prüfungen aus F5. Parametrisiert
(`SqlParameter`, keine Literale), `Mandant = 1` als Konstante wie im Lese-Abgleich (`SageLagerplatzReader.cs:27`,
`// ponytail: Mandant fest 1 wie die Reader, Setting erst bei zweitem Mandanten`). Fehler 50010–50019 = fachlich.

```sql
SET XACT_ABORT ON;
BEGIN TRAN;
  DECLARE @LagerBez varchar(40), @B1 varchar(50), @B2 varchar(50), @B3 varchar(50), @Aktiv smallint, @Ebenen smallint,
          @InvUeb datetime, @InvZaehl datetime, @InvJahr smallint, @PlatzID int, @Kurz varchar(50), @Bez varchar(200), @n int;
  SELECT @LagerBez = o.Bezeichnung, @Aktiv = o.Aktiv,
         @B1 = t.BezeichnungDimension1, @B2 = t.BezeichnungDimension2, @B3 = t.BezeichnungDimension3
  FROM KHKLagerorte o WITH (UPDLOCK, HOLDLOCK)
  JOIN KHKLagertechnikenArten t ON t.Lagertechnikkennung = o.Lagertechnik
  WHERE o.Mandant = @Mandant AND o.Lagerkennung = @Lager;
  IF @@ROWCOUNT = 0 THROW 50010, 'Lager oder Lagertechnik in Sage nicht gefunden', 1;
  IF @Aktiv <> -1 THROW 50013, 'Lager ist in Sage inaktiv', 1;      -- inaktive Lager liest der Abgleich nicht (SageLagerplatzReader.cs:28)
  SET @Ebenen = IIF(@B1 IS NULL,0,1) + IIF(@B2 IS NULL,0,1) + IIF(@B3 IS NULL,0,1);
  IF @Ebenen = 0 THROW 50011, 'Lagertechnik ohne Dimensionen - keine Plaetze moeglich', 1;
  IF @D1 < 1 OR (@Ebenen >= 2 AND @D2 < 1) OR (@Ebenen < 2 AND @D2 <> 0) OR (@Ebenen >= 3 AND @D3 < 1) OR (@Ebenen < 3 AND @D3 <> 0)
     THROW 50012, 'Koordinaten passen nicht zur Lagertechnik', 1;     -- Klartext im C# mit @B1..@B3 ergänzen
  SET @Kurz = CONCAT(@Lager, ';', @D1, ';', @D2, ';', @D3);
  SET @Bez  = CONCAT(@LagerBez, ';', @B1, ' ', @D1, IIF(@Ebenen >= 2, CONCAT(';', @B2, ' ', @D2), ''), IIF(@Ebenen >= 3, CONCAT(';', @B3, ' ', @D3), ''));
  IF LEN(@Bez) > 110 THROW 50015, 'Platzbezeichnung laenger als 110 Zeichen', 1;
  -- F5: vorhanden / Konflikt (kein Unique-Index in Sage, Anhang 2.4)
  SELECT @n = COUNT(*), @PlatzID = MIN(PlatzID) FROM KHKLagerplaetze WITH (UPDLOCK, HOLDLOCK)
  WHERE Mandant = @Mandant AND Lagerkennung = @Lager
    AND ((Dimension1 = @D1 AND Dimension2 = @D2 AND Dimension3 = @D3) OR Kurzbezeichnung = @Kurz);
  IF @n > 1 OR (@n = 1 AND NOT EXISTS (SELECT 1 FROM KHKLagerplaetze WHERE PlatzID = @PlatzID AND Mandant = @Mandant
                                        AND Dimension1 = @D1 AND Dimension2 = @D2 AND Dimension3 = @D3 AND Kurzbezeichnung = @Kurz))
     THROW 50014, 'Konflikt in Sage: Koordinaten oder Kurzbezeichnung schon anders belegt', 1;
  IF @n = 0
  BEGIN
    SELECT @InvUeb = LetztesInventurUebernahmedatum, @InvZaehl = LetztesInventurZaehldatum, @InvJahr = LetztesInventurjahr
    FROM KHKLagerplaetze WHERE Mandant = @Mandant AND Lagerkennung = @Lager AND Dimension1 = 0 AND Dimension2 = 0 AND Dimension3 = 0;
    EXEC spSysTan @Table = 'KHKLagerplaetze', @Mandant = @Mandant, @Increment = 1, @Tan = @PlatzID OUTPUT;   -- nie MAX+1 (Anhang 2.1)
    INSERT INTO KHKLagerplaetze (PlatzID, Mandant, Lagerkennung, Dimension1, Dimension2, Dimension3, Dimensionsebene, Kurzbezeichnung,
           Platzbezeichnung, IstGesperrt, Laenge, Breite, Hoehe, Volumen, Tragkraft, Status, Inventurmethode, IstGesperrtInventur,
           LetztesInventurUebernahmedatum, LetztesInventurZaehldatum, LetztesInventurjahr)
    VALUES (@PlatzID, @Mandant, @Lager, @D1, @D2, @D3, @Ebenen, @Kurz, @Bez, 0, 0, 0, 0, 0, 0, 0, 0, 0, @InvUeb, @InvZaehl, @InvJahr);
    UPDATE o SET AnzahlDimension1 = x.M1, AnzahlDimension2 = x.M2, AnzahlDimension3 = x.M3, AnzahlLagerplaetze = x.Anz, IstPlatzverwaltet = -1
    FROM KHKLagerorte o CROSS APPLY (SELECT MAX(Dimension1) M1, MAX(Dimension2) M2, MAX(Dimension3) M3, COUNT(*) Anz
                                     FROM KHKLagerplaetze p WHERE p.Mandant = o.Mandant AND p.Lagerkennung = o.Lagerkennung) x
    WHERE o.Mandant = @Mandant AND o.Lagerkennung = @Lager;          -- Zähler aus den Daten, in derselben Transaktion (Anhang 5.4, besser als Sage)
  END
  SELECT @PlatzID AS PlatzID, IIF(@n = 0, 1, 0) AS Angelegt, (SELECT Platzbezeichnung FROM KHKLagerplaetze WHERE PlatzID = @PlatzID AND Mandant = @Mandant) AS Platzbezeichnung;
COMMIT;
```

Unverändert aus dem Anhang übernommen und **nicht** abgewandelt: `KHKBoolean` `-1/0` (nie `1`), `Timestamp` nie schreiben, Inventurmethode und
-sperre fest 0, Inventurdaten vom Basisplatz, Dimensionsebene = Anzahl Dimensionen der Lagertechnik, keine Trigger beim INSERT/UPDATE (Anhang 1;
für den Zielmandanten Prüfschritt P-2), `spCreateClassHistory` entfällt (Anhang 1). Das Referenz-SQL ist laut Anhang **nicht gegen die
Datenbank getestet**; vor jedem Einschalten am Produktivmandanten ist der Abnahmetest (TS, Schritt .k) Pflicht.

**Verbindung.** Der Dienst schreibt, nicht das Web (Web ohne Sage-SQL-Verbindung, `IdealAkeWms/appsettings.json:2-5`). Heute liest der Dienst
über `ConnectionStrings:SageConnection` mit `Trusted_Connection` (`IDEALAKEWMSService/appsettings.json:4`), also als Dienstkonto. Empfehlung
(Rückfrage 2): eigener Connection String `ConnectionStrings:SageSchreibConnection` mit eigenem Login und nur diesen Rechten: `SELECT` auf
`KHKLagerorte`, `KHKLagerplaetze`, `KHKLagertechnikenArten`; `INSERT` auf `KHKLagerplaetze`; `UPDATE` auf `KHKLagerorte`; `EXECUTE` auf
`spSysTan` (Schreiben in `KHKTan` über Ownership-Chaining, P-4). Fehlt der Connection String bei eingeschaltetem Schalter: Lauf
`FinishFailed` „Sage-Schreibverbindung nicht konfiguriert – nichts gesendet“, einmal gemeldet, nichts verändert.

**ADR 0036 (Dev-Lauf schreibt sie in den Hauptcheckout, Nummer vorher prüfen).** Titel-Vorschlag: „Lagerplatz-Anlage in Sage per Direkt-SQL
hinter austauschbarer Schnittstelle, bis DPS einen SData-Service liefert“. Pflichtinhalte:
- Kontext: G5 (kein SData-Service), Entscheid Gerald 2026-11-24, Anhang als Referenz, bisher nur SData-Schreibwege (Lagerbuchung, Wareneingang,
  AG-Anlage).
- Rechte: eigener Login, Minimalrechte (Liste oben), kein `db_datawriter`, keine Rechte auf andere Tabellen; das Lese-Login bleibt lesend.
- Updatefestigkeit: Sage-Versionswechsel können Spalten, Defaults, Trigger, `spSysTan` ändern → nach jedem Sage-Update vor dem Wiedereinschalten
  P-1 bis P-3 erneut prüfen; Schalter bis dahin aus. Neue `USER_`-Pflichtfelder ohne Default brechen den INSERT ab (sichtbarer Fehler, kein
  Teilschreiben).
- Support DPS: Direktschreiben ist kein von Sage/DPS unterstützter Weg (Rückfrage 6); Abgrenzung im Fehlerfall: Outbox + Protokoll zeigen jeden
  WMS-Schreibvorgang mit PlatzID und Zeit.
- Transaktion: je Platz eine Transaktion inkl. Zähler-UPDATE, `XACT_ABORT`, kurz (`spSysTan` sperrt `KHKTan` bis Commit, Anhang 2.6).
- Idempotenz: Prüfen-und-Anlegen in einer Transaktion mit `UPDLOCK, HOLDLOCK`; Wiederholung ist gefahrlos; Restrisiko gleichzeitiger
  Sage-Dialog (F5).
- Rückbau: SData-Implementierung des Interfaces, Outbox/Status/Oberfläche bleiben; ADR wird superseded.

**Zusatzfund (nicht mitbauen, melden):** `SageLagerplatzReader.cs:26` verbindet `KHKLagerorte` und `KHKLagerplaetze` nur über `Lagerkennung`, ohne
`Mandant`. Mit nur Mandant 1 ohne Wirkung (SData-Spec: `LagerplaetzeInfo where Mandant ne 1` → 0 am AKE-Test); bei einem zweiten Mandanten in
derselben DB entstünden Fremdplätze.

## Migrations-/SQL-Auswirkungen

- **Keine EF-Migration, kein `SQL/XX_*.sql`, kein FreshInstall-Nachzug** – alle Felder existieren: Outbox (SQL/107), Übergabe-Felder am Platz
  (SQL/107), `StorageAreaId` (SQL/98). Der Dev-Lauf prüft das am Diff; braucht er doch eine Spalte, gilt ADR 0004 vollständig und die nächste
  freie SQL-Nummer im Bündel.
- `SQL/AgentJobs/`: nicht betroffen (keine Referenz auf `StorageLocations`/`SageStammdatenOutbox`, Suche 2026-11-24).
- **Sage-Datenbank (Außenwirkung):** Der Dienst schreibt `KHKLagerplaetze` (INSERT), `KHKLagerorte` (UPDATE Zähler) und über `spSysTan`
  `KHKTan`. Nicht destruktiv (nur Anlage, kein Löschen, kein Ändern bestehender Plätze); trotzdem vor dem ersten Einschalten am Produktivmandanten
  ein Sage-DB-Backup (Betrieb Sage).

**Prüfabfragen für den Menschen (Sage steht nicht im Code; Aussagen dazu gelten bis zur Prüfung als unbelegt).** Ausführen in SSMS gegen die
Sage-Mandanten-DB, zuerst `IDEAL_TEST_2026_05_03`, vor dem Produktiv-Einschalten gegen die Produktiv-DB (AKE nur falls Rückfrage 4 = ja):

- **P-1 Spalten/Defaults** (neue `USER_`-Pflichtfelder brechen den INSERT):
  `SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE, COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME IN ('KHKLagerplaetze','KHKLagerorte') ORDER BY TABLE_NAME, ORDINAL_POSITION;`
  Erwartet: jede Spalte von `KHKLagerplaetze`, die nicht im INSERT steht, ist `IS_NULLABLE = YES` oder hat `COLUMN_DEFAULT`; Längen
  Kurzbezeichnung 50, Platzbezeichnung 110 (Anhang 5.3).
- **P-2 Trigger:** `SELECT OBJECT_NAME(t.parent_id) AS Tabelle, t.name, te.type_desc, t.is_disabled FROM sys.triggers t JOIN sys.trigger_events te ON te.object_id = t.object_id WHERE t.parent_id IN (OBJECT_ID('dbo.KHKLagerplaetze'), OBJECT_ID('dbo.KHKLagerorte'));`
  Erwartet: nur `DELETE` (Anhang 1).
- **P-3 Zähler:** `SELECT p.name, TYPE_NAME(p.user_type_id) AS Typ, p.is_output FROM sys.parameters p WHERE p.object_id = OBJECT_ID('dbo.spSysTan'); SELECT Tan FROM KHKTan WHERE Tabelle = 'KHKLagerplaetze' AND Mandant = 1; SELECT MAX(PlatzID) FROM KHKLagerplaetze WHERE Mandant = 1;`
  Erwartet: Parameter `@Table`, `@Mandant`, `@Increment`, `@Tan OUTPUT`; `Tan ≥ MAX(PlatzID)`.
- **P-4 Rechte des Schreib-Logins:** `EXECUTE AS LOGIN = N'<Schreib-Login>'; SELECT HAS_PERMS_BY_NAME('dbo.KHKLagerplaetze','OBJECT','INSERT') AS InsPlatz, HAS_PERMS_BY_NAME('dbo.KHKLagerorte','OBJECT','UPDATE') AS UpdOrt, HAS_PERMS_BY_NAME('dbo.spSysTan','OBJECT','EXECUTE') AS ExecTan, HAS_PERMS_BY_NAME('dbo.KHKLagertechnikenArten','OBJECT','SELECT') AS SelTechnik; REVERT;`
  plus Eigentümer: `SELECT name, USER_NAME(OBJECTPROPERTY(object_id,'OwnerId')) AS Owner FROM sys.objects WHERE name IN ('spSysTan','KHKTan');`
  Erwartet: alle 1; gleicher Owner (sonst braucht das Login zusätzlich `UPDATE` auf `KHKTan`).
- **P-5 Lagertechniken/Lager:** `SELECT Lagertechnikkennung, BezeichnungDimension1, BezeichnungDimension2, BezeichnungDimension3 FROM KHKLagertechnikenArten; SELECT Lagerkennung, Bezeichnung, Lagertechnik, Aktiv, IstPlatzverwaltet, AnzahlLagerplaetze FROM KHKLagerorte WHERE Mandant = 1;`
  Erwartet: Techniken wie Anhang 4.4.
- **P-6 abweichende Kurzbezeichnungen** (Konfliktfälle F5): `SELECT COUNT(*) FROM KHKLagerplaetze WHERE Mandant = 1 AND Kurzbezeichnung <> CONCAT(Lagerkennung, ';', Dimension1, ';', Dimension2, ';', Dimension3);`
- **P-7 Dubletten:** `SELECT Lagerkennung, Dimension1, Dimension2, Dimension3, COUNT(*) FROM KHKLagerplaetze WHERE Mandant = 1 GROUP BY Lagerkennung, Dimension1, Dimension2, Dimension3 HAVING COUNT(*) > 1;`
- **P-8 (WMS-DB des Standorts) Lagerort-Liste:** `SELECT [Key], [Value] FROM ServiceSettings WHERE [Key] IN ('Sync:LagerplaetzeEnabled','Sync:Quelle:Lagerplaetze'); SELECT COUNT(*) FROM StorageAreas WHERE IsActive = 1 AND SageMissingSince IS NULL;`
  Erwartet: Lagerplatz-Abgleich an, Quelle `SData`, Anzahl > 0. Sonst bleibt das Formular leer (F1).

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `SageLagerplatzAnlageAktiv` (ServiceSetting, in `ServiceSettingDefinitions.All` + `Meta`, Drift-Guard) | Bool | Ja/Nein (Auswahl, Server validiert) | `false` | An: Web zeigt „Lagerplätze in Sage anlegen“ und „Erneut senden“; der Dienst legt erfasste Plätze in Sage an. Aus: Formular-Knopf weg, Dienst sendet nichts, offene Einträge bleiben unverändert stehen. | Web: Stammdaten → Lagerplätze; Dienst: Sage-Buchungs-Worker, Lauf „Lagerplätze → Sage“ | IDEAL (AKE aus, Rückfrage 4); wirkt je Standort-DB | „Schreibt direkt per SQL in die Sage-Tabellen KHKLagerplaetze, KHKLagerorte und KHKTan (ohne Sage-Oberfläche, ohne SData). Angelegte Plätze löscht das WMS nicht. Nur nach bestandenem Abnahmetest am Testmandanten und nach jedem Sage-Update erst nach den Prüfabfragen der Spec einschalten.“ |
| `ConnectionStrings:SageSchreibConnection` (Dienst-`appsettings.json`, bewusst nicht in `/ServiceSettings`, `ServiceSettingDefinitions.cs:14`) – nur bei Rückfrage 2 = eigener Login | Text (Connection String) | gültiger SQL-Server-Connection-String auf die Sage-Mandanten-DB | leer | Verbindung des Schreibwegs; leer bei Schalter an = Lauf schlägt sichtbar fehl, nichts gesendet | Dienst: `SqlSageLagerplatzAnlage` | je Standort | „Login nur mit den Rechten aus der Spec/ADR 0036 anlegen; zeigt die Verbindung auf die Produktiv-DB, wirkt jeder Lauf dort.“ |

Kein AppSetting. Die Bool-Lesart „leer = Default“ gilt wie bei den anderen Schaltern; im Dienst den Schalter **nicht** mit der Safe-Variante
lesen (`SageBookingWorker.cs:267-268`).

## Audit-Feld-Auswirkungen

- WMS-Neuanlage (F2): `CreatedAt/By/ByWindows` aus `ICurrentUserService` an Platz und Outbox (Outbox übernimmt die Werte vom Platz,
  `SageStammdatenOutbox.cs:24-26`).
- Dienst (F3): `ModifiedAt = DateTime.Now` (Lokalzeit, ADR 0025), `ModifiedBy = "system:sage-lagerplatz-anlage"`, `ModifiedByWindows =
  Environment.MachineName` an Platz und Outbox, bei jeder Statusänderung (auch `Gesendet` und technischer Fehler).
- „Erneut senden“ (F6) und Bearbeiten (F7): `ModifiedAt/By/ByWindows` des Anwenders an Platz und Outbox.
- Sage-Seite: Der Mitschnitt zeigt keine Benutzerspalten in `KHKLagerplaetze`/`KHKLagerorte` (Anhang 4.2, 5.2); nachvollziehbar ist der
  WMS-Vorgang über Outbox (`CreatedBy`, `ModifiedAt`, `AttemptCount`, `LastError`) und das Aktivitäts-Protokoll mit PlatzID.

## Akzeptanzkriterien

1. Schalter aus (Default): Lagerplatzliste, „Neuen Lagerplatz anlegen“ und Hinweistext unverändert wie heute; kein Knopf „Lagerplätze in Sage
   anlegen“; `GET/POST CreateInSage` liefert eine `WarningMessage` „ausgeschaltet“ und legt nichts an; der Dienst schreibt nichts nach Sage
   (Lauf „Lagerplätze → Sage“ erscheint nicht im Protokoll).
2. Schalter an, Benutzer ohne Schreibrecht (Rolle nach Rückfrage 3): kein Knopf; direkter Aufruf von `CreateInSage` bzw. „Erneut senden“ →
   AccessDenied.
3. Bestehendes `Create` ignoriert `StorageAreaId` aus dem POST (Test mit manipuliertem Formular: Platz hat danach `StorageAreaId = null`).
4. `CreateInSage` mit Lagerort L (Kennung `TB`), D1 1–2, D2 1–3, D3 0–0 legt 6 Plätze `TB;1;1;0` … `TB;2;3;0` an, alle `Source = Manual`,
   `StorageAreaId` = L, `Zone = TB`, `SageUebergabeStatus = Offen`, je ein Outbox-Eintrag `StorageLocation/<Id>` Status `Offen`; Reihenfolge der
   Outbox-Ids = D1 außen, D3 innen.
5. Existiert `TB;1;2;0` im WMS schon, werden 5 angelegt und 1 gemeldet („übersprungen, Code im WMS schon vorhanden: TB;1;2;0“).
6. Mehr als 200 Koordinaten oder D1 < 1 oder von > bis: Formularfehler, nichts angelegt.
7. Ohne aktive `StorageAreas`: Formular zeigt den Hinweis aus F1 statt der Auswahl, Absenden nicht möglich.
8. Dienst, Schalter an, Testmandant: für jeden Eintrag aus AK 4 entsteht in Sage genau ein Platz; WMS-Platz danach `Source = Sage`,
   `SageLagerplatzId` = Sage-PlatzID, `SageLagerkennung = Code`, `Description` = Sage-Platzbezeichnung, Status `Uebergeben`, `SageUebergabeAm`
   gesetzt; Outbox `Bestaetigt`; Protokoll „Lagerplätze → Sage“ mit Count `angelegt = 6`.
9. Sage-PlatzIDs kommen aus `spSysTan`: `KHKTan.Tan` ist nach dem Lauf um 6 höher; die IDs steigen in der Reihenfolge aus AK 4.
10. `KHKLagerorte` von `TB` nach dem Lauf: `AnzahlDimension1/2/3`, `AnzahlLagerplaetze` (inkl. Basisplatz) = MAX/COUNT aus `KHKLagerplaetze`,
    `IstPlatzverwaltet = -1`.
11. Abnahmetest (TS Schritt .k): identische Plätze in zwei identischen Lagern, einmal über den Sage-Dialog, einmal über das WMS – der
    spaltenweise Vergleich liefert in beide Richtungen 0 Abweichungen für `KHKLagerplaetze` und `KHKLagerorte`.
12. Idempotenz: Existiert der Platz in Sage schon mit gleichen Koordinaten und gleicher Kurzbezeichnung, schreibt der Dienst nichts in Sage
    (`KHKTan` unverändert), übernimmt den WMS-Platz mit dieser PlatzID und protokolliert „war in Sage schon vorhanden – übernommen“.
13. Konflikt: Koordinaten in Sage mit anderer Kurzbezeichnung belegt (oder umgekehrt, oder mehrfach) → nichts geschrieben, WMS-Platz Status
    `Fehler`, Spalte „Übergabe-Fehler“ nennt PlatzID/Kurzbezeichnung, Outbox `Fehler`, Warnung im Protokoll.
14. Fachliche Ablehnungen sichtbar mit Klartext, nichts in Sage geschrieben: Lager inaktiv (50013), Koordinaten passen nicht zur Lagertechnik
    (50012, Text nennt die Dimensionen der Technik), Lagertechnik ohne Dimensionen (50011), Platzbezeichnung > 110 (50015), Code passt nicht zum
    Muster.
15. Technischer Fehler (falsche Schreibverbindung oder fehlendes Recht): kein Teilschreiben in Sage; Outbox wieder `Offen`, Platz `Offen` mit
    „Letzter Versuch fehlgeschlagen: …“; der gleiche Fehlertext steht nur einmal im Protokoll, bis er sich ändert; nach Behebung läuft der
    Eintrag ohne Zutun durch.
16. Hängender `Gesendet`-Eintrag (simuliert: Sage-Platz angelegt, WMS-Speichern nicht erfolgt) wird nach `Sync:SageLagerbuchungStuckMinutes`
    erneut verarbeitet und als „vorhanden“ übernommen – keine zweite Sage-Zeile.
17. Rücklesen: Ein Lagerplatz-Abgleich nach der Übergabe meldet für diese Plätze weder „neu“ noch „Konflikt“ noch „aktualisiert“ und legt keine
    zweite WMS-Zeile an; `IstBuchbar`/`SageBuchungErlaubt` bleiben wie erfasst.
18. Alt-Einträge (Status Offen, ohne `StorageAreaId`) und inaktive Sage-Anlage-Plätze sendet der Dienst nicht; ihr Outbox-Eintrag bleibt
    unverändert.
19. „Erneut senden“ bei Status `Fehler`: Platz und Outbox `Offen`, Fehlertext leer, `AttemptCount = 0`, Audit des Anwenders; der nächste Takt
    verarbeitet den Eintrag.
20. Bearbeiten eines Sage-Anlage-Platzes vor der Übergabe: Code/Bezeichnung/Zone nicht änderbar, Deaktivieren und Buchbar-Schalter speicherbar,
    kein Fehler „max. 12 Zeichen“.
21. Hinweistext der Liste unterscheidet Schalter an/aus (F6); neue Spalte „Übergabe-Fehler“ mit `data-col-key`, Server-Spaltenfilter
    (`ColumnMap`), Eintrag in `column-config`; Pagination und Filter bleiben über Seitenwechsel erhalten.
22. AKE-Regression mit Schalter aus: Lagerplatz-Neuanlage, Lagerplatz-Abgleich, Sage-Lagerbuchung unverändert.
23. ADR 0036 liegt im Hauptcheckout mit den Pflichtinhalten aus dem Lösungsentwurf; `fallstricke.md` bekommt den Eintrag „Direkt-SQL in Sage:
    nach Sage-Update erst P-1 bis P-3, dann Schalter an“ mit Warum.

Unit-testbar (InMemory + Mock `ISageLagerplatzAnlage`): AK 1–8 (WMS-Seite), 12–19 (Status-Automat, Übernahme, Wiederholung, Einmal-Meldung).
Der SQL-Befehl selbst (AK 9–11, 13–14 Sage-Seite) ist **nicht** InMemory-testbar → Manual-UAT am Testmandanten (fallstricke „Tests, EF, SQL
Server“). UNIQUE auf `Code` erzwingt InMemory nicht: der Überspring-Check in F2 ist App-Code und wird getestet.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (nächste freie Nummer, Stand 2026-11-24 höchste vergebene TS-187; Dev-Lauf prüft) „Lagerplätze im WMS
erfassen und in Sage anlegen (Direkt-SQL)“, Index in `secondbrain/tests/testszenarien-index.md`. Standort: IDEAL (Testmandant), AKE nur
Regression.

**Vorbedingungen:** WMS-Testsystem mit Dienst auf `IDEAL_TEST_2026_05_03` (Rückfrage 5); P-1 bis P-8 erledigt; in Sage zwei Lager mit identischen
Eingaben angelegt, z. B. `TS-SAGE` und `TS-WMS`, Bezeichnung gleich lang, Lagertechnik `LTD2SF` (zusätzlich ein Paar `LTD3HR`, ein Lager mit
Basisplatz-Inventurdaten ≠ NULL); Lagerplatz-Abgleich mit Quelle SData einmal gelaufen (beide Lager in `StorageAreas`); Benutzer A mit
Schreibrecht, Benutzer R nur `masterdata_read`.

| Schritt | Aktion | Erwartet |
|---|---|---|
| .a | Schalter aus, Lagerplatzliste öffnen | wie heute, kein Knopf „Lagerplätze in Sage anlegen“, Hinweistext „Das WMS sendet nichts an Sage“ |
| .b | Schalter an; Benutzer R | kein Knopf; URL `/StorageLocations/CreateInSage` → Zugriff verweigert |
| .c | Benutzer A: Lagerort `TS-WMS`, D1 1–3, D2 1–2, D3 0–0 | 6 Plätze `TS-WMS;1;1;0` …, Status Offen, Outbox 6 Zeilen Status 0 |
| .d | Dienst-Takt abwarten (≤ 1 min) | alle 6 „Übergeben“, Quelle Sage, Spalte Übergabe-Fehler leer; Protokoll „Lagerplätze → Sage“ angelegt = 6 |
| .e | In Sage `TS-SAGE` dieselben Plätze über den Dialog „Lagerplätze anlegen“ anlegen | – |
| .f | Lagerplatz-Abgleich starten | `TS-SAGE`-Plätze neu im WMS; `TS-WMS`-Plätze weder neu noch Konflikt noch aktualisiert, keine Dublette |
| .g | Denselben Bereich für `TS-WMS` nochmals erfassen | alle 6 übersprungen („Code im WMS schon vorhanden“) |
| .h | In Sage `TS-WMS;3;3;0` von Hand anlegen, dann im WMS D1 3–3, D2 3–3 erfassen | Dienst: „war in Sage schon vorhanden – übernommen“, `KHKTan` unverändert |
| .i | D1 1–1, D2 1–1, D3 1–1 für `TS-WMS` (2-dimensional) | Status Fehler, Text „Koordinaten passen nicht zur Lagertechnik (Reihe, Platz)“, nichts in Sage |
| .j | Lager `TS-WMS` in Sage kurz inaktiv setzen, Platz `TS-WMS;3;1;0` erfassen; dann Lager wieder aktiv, „Erneut senden“ | erst Fehler „Lager ist in Sage inaktiv“, nichts in Sage; nach „Erneut senden“ Offen → nächster Takt Übergeben. Fehler-Platz aus .i deaktivieren: wird nie gesendet, Zähler offener Übergaben sinkt |
| .k | **Abnahmevergleich** (SQL unten) | beide Richtungen 0 Zeilen für `KHKLagerplaetze` und `KHKLagerorte` |
| .l | Schreib-Connection-String auf falsches Kennwort, Platz erfassen | Platz Offen, „Letzter Versuch fehlgeschlagen: …“, Fehler einmal im Protokoll; Kennwort korrigieren → läuft durch |
| .m | Dienst nach Sage-Commit hart beenden (Debug-Haltepunkt bzw. Prozess beenden), neu starten | nach Stuck-Minuten „vorhanden – übernommen“, keine zweite Sage-Zeile |
| .n | AKE-DEV mit Schalter aus: Lagerplatz anlegen, Lagerplatz-Abgleich, Ein-/Ausbuchung | unverändert |

**Abnahmevergleich (Schritt .k, Anhang 9)** – gegen die Sage-Test-DB; Kennung und Bezeichnung werden in den Texten ersetzt, PlatzID,
Lagerkennung und Timestamp ausgenommen. Alle übrigen Spalten (auch `USER_`) kommen aus `INFORMATION_SCHEMA`:

```sql
DECLARE @A varchar(10) = 'TS-SAGE', @B varchar(10) = 'TS-WMS';
DECLARE @BezA varchar(40) = (SELECT Bezeichnung FROM KHKLagerorte WHERE Mandant = 1 AND Lagerkennung = @A),
        @BezB varchar(40) = (SELECT Bezeichnung FROM KHKLagerorte WHERE Mandant = 1 AND Lagerkennung = @B);
DECLARE @cols nvarchar(max) = (SELECT STRING_AGG(QUOTENAME(COLUMN_NAME), ',') FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'KHKLagerplaetze' AND COLUMN_NAME NOT IN ('PlatzID','Lagerkennung','Timestamp','Kurzbezeichnung','Platzbezeichnung'));
DECLARE @sql nvarchar(max) = N'
SELECT ' + @cols + N', REPLACE(Kurzbezeichnung, @A, ''#'') K, REPLACE(Platzbezeichnung, @BezA, ''#'') P FROM KHKLagerplaetze WHERE Mandant = 1 AND Lagerkennung = @A
EXCEPT
SELECT ' + @cols + N', REPLACE(Kurzbezeichnung, @B, ''#''), REPLACE(Platzbezeichnung, @BezB, ''#'') FROM KHKLagerplaetze WHERE Mandant = 1 AND Lagerkennung = @B;';
EXEC sp_executesql @sql, N'@A varchar(10), @B varchar(10), @BezA varchar(40), @BezB varchar(40)', @A, @B, @BezA, @BezB;
-- danach mit vertauschten @A/@B und @BezA/@BezB; dasselbe Muster für KHKLagerorte (ausgenommen Lagerkennung, Bezeichnung, Timestamp)
```

**Negativfälle** zusammengefasst: Schalter aus, fehlendes Recht, leere Lagerort-Liste, > 200 Koordinaten, WMS-Dublette, Sage-Dublette/Konflikt,
inaktives Lager, falsche Koordinaten, technischer Fehler, Abbruch nach Commit, Alt-Einträge ohne Lagerort, inaktiver Platz.

## Etappen (nur bei epic: true)

Kein Epic. Empfohlene Commit-Reihenfolge im Dev-Lauf: (1) Web-Erfassung + Überposting-Schutz + Bearbeiten, (2) Interface + Dienst-Schritt mit
Mock-Tests, (3) `SqlSageLagerplatzAnlage`, (4) Liste/„Erneut senden“/Hinweistexte, (5) Doku, ADR, Testszenario.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt | – | – |

## Deploy

**Vorläufig** (QA-Lauf bestätigt am echten Diff).

- **Web-App:** ja (Formular, Liste, Hinweistexte, „Erneut senden“, Hilfe)
- **Service:** ja (Worker-Schritt, Schreibstelle, neuer Connection String bei Rückfrage 2 = eigener Login)
- **Migration:** nein
- **Reihenfolge:** Publish aus dem Worktree → IDEAL-Testsystem (Schalter aus) → P-1 bis P-8 gegen den Testmandanten → Schalter am Testsystem an →
  Testszenario inkl. Abnahmevergleich → Merge (Schranke 2). Produktiv: Deploy mit Schalter **aus**; Einschalten erst nach P-1 bis P-7 gegen die
  Produktiv-Sage-DB, Sage-DB-Backup und (Rückfrage 6) DPS-Abstimmung.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023; Texte nur ASCII, Drift-Test `NachlaufManifestDriftTests.cs:304-314`;
  Version = Version des Dev-Laufs):
  - `Id='v<X>-SageLagerplatzSchreibLogin'`, Typ `Info`, Firma `IDEAL` (bzw. `beide` bei Rückfrage 4 = AKE auch), Phase `Vorher`, Ausfuehrung
    `anzeigen`, Text: „Lagerplatz-Anlage in Sage: Login fuer ConnectionStrings:SageSchreibConnection anlegen (SELECT KHKLagerorte/KHKLagerplaetze/
    KHKLagertechnikenArten, INSERT KHKLagerplaetze, UPDATE KHKLagerorte, EXECUTE spSysTan) und in die appsettings.json des Dienstes eintragen;
    Pruefabfragen P-1 bis P-7 der Spec 2026-11-24-lagerplatz-anlage-wms-nach-sage-spec.“
  - `Id='v<X>-SageLagerplatzAnlageAktiv'`, Typ `Einstellung`, Firma `IDEAL`, Phase `Nachher`, Ausfuehrung `anzeigen`, Text: „SageLagerplatzAnlageAktiv
    bleibt aus. Einschalten erst nach Abnahmetest am Testmandanten, P-1 bis P-7 gegen die Produktiv-Sage-DB und Sage-DB-Backup.“
  - `Id='Pruefung/LagerorteFuerSageAnlage'`, Typ `Pruefung`, Firma `IDEAL`, Phase `Nachher`, Ausfuehrung `anzeigen`: Skript nach Muster der
    vorhandenen `Pruefung/*`-Einträge, zeigt `Sync:Quelle:Lagerplaetze`, `Sync:LagerplaetzeEnabled` und die Anzahl aktiver `StorageAreas` (P-8).
  - Keine Schema-, Daten- oder Einmal-Einträge.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

## Offene Rueckfragen

1. **Umfang Lager:** Nur Lagerplätze in bestehenden Sage-Lagern (Empfehlung: Lager entstehen selten, Lagertechnik und Warenfluss sind
   Sage-Entscheidungen, die Outbox kennt nur Plätze) oder auch Lager (Lagerort, `KHKLagerorte`, Anhang 4/8.1) aus dem WMS anlegen? Bei „ja“:
   eigene Folge-Spec mit WarenflussKZ nur `SWF`/`WE`/`WA`, nie `HWE`/`HWA`.
2. **Schreib-Login:** eigener Connection String `ConnectionStrings:SageSchreibConnection` mit eigenem Login und Minimalrechten (Empfehlung: das
   Lese-Login aller Abgleiche bleibt lesend) oder die bestehende `SageConnection` (Dienstkonto, `Trusted_Connection`) um Schreibrechte erweitern?
3. **Rolle:** „Lagerplätze in Sage anlegen“ und „Erneut senden“ mit `masterdata` wie die heutige Neuanlage (Empfehlung; der Schalter begrenzt die
   Außenwirkung) oder nur `admin` wie „Arbeitsgänge → Sage“?
4. **Standort:** nur IDEAL (Mitschnitt nur am IDEAL-Testmandanten, die `USER_`-Felder sind mandantenspezifisch, Anhang 4.2) und AKE dauerhaft aus,
   oder ist AKE vorgesehen (dann P-1 bis P-7 auch gegen die AKE-Sage-DB und Abnahmetest an AKE)?
5. **Testsystem:** Welches WMS-Testsystem (Dienst) wird an `IDEAL_TEST_2026_05_03` auf `idealsql2\sage100test` angebunden, und wer legt dort in
   Sage die zwei identischen Testlager für den Abnahmevergleich an?
6. **DPS:** Soll DPS (Michael Wiesner) über den Direkt-SQL-Schreibweg informiert bzw. um Bestätigung gebeten werden (Support und Gewährleistung
   bei Sage-Updates), bevor am Produktivmandanten eingeschaltet wird?

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Umfang Lager (nur Plätze oder auch Lager anlegen) →
2. Schreib-Login (eigener SageSchreibConnection oder SageConnection erweitern) →
3. Rolle (masterdata oder admin) →
4. Standort (nur IDEAL oder auch AKE) →
5. Testsystem am Testmandanten und Anlage der Testlager →
6. DPS informieren/Bestätigung vor Produktiv →
