---
type: spec
title: "BDE: Fertig an Buchung/Gruppe schreibt zweite End-Menge (IsFinal) auf einen schon fertigen Arbeitsgang"
slug: 2026-11-26-bde-finish-doppelte-endmenge-spec
status: Entwurf
created: 2026-11-26
updated: 2026-11-28
source_backlog: "[[2026-11-26-bde-finish-doppelte-endmenge]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: beide
affected_code:
  - IdealAkeWms/Services/BdeBookingService.cs
  - IdealAkeWms/Services/IBdeBookingService.cs
  - IdealAkeWms/Controllers/BdeTerminalController.cs
  - IdealAkeWms/wwwroot/js/bde-fa-sicht.js
  - IdealAkeWms/Views/Help/Index.cshtml
  - IdealAkeWms/Views/Help/Changelog.cshtml
  - IdealAkeWms/AppVersion.cs
  - IDEALAKEWMSService/AppVersion.cs
  - IdealAkeWms.Tests/Services/BdeMengenRegelTests.cs
  - docs/TESTSZENARIEN.md
  - docs/anwender/11-bde-terminal.md
open_questions:
  - "Entscheidung: Variante A (je Buchung eine End-Menge, mehrere IsFinal je Arbeitsgang zulässig, Summe zählt), B1 (je Arbeitsgang genau eine End-Menge; spätere Fertig-Meldungen werden als Teilmenge gespeichert, Buchung wird beendet, Hinweis) oder B2 (je Arbeitsgang genau eine; spätere Fertig-Meldung wird abgewiesen, Buchung läuft weiter)? Empfehlung: B1."
  - "(Fachbereich: Walter/Benjamin) Kommt es an IDEAL vor, dass zwei Werker am selben Arbeitsgang arbeiten oder ein Werker seine Pause fortsetzt, nachdem ein Kollege den Arbeitsgang schon fertig gemeldet hat? Was soll die zweite Fertig-Meldung dann bedeuten: zusätzliche Menge (Teilmenge) oder eigener Abschluss?"
  - "Prüfabfragen P-1 bis P-3 (Abschnitt Migrations-/SQL-Auswirkungen) auf DEV AKE, DEV IDEAL, Prod AKE und Prod IDEAL ausführen und Ergebnis eintragen. Bei Treffern in P-1: Bestand unverändert lassen (Empfehlung) oder per Einmal-Skript je Arbeitsgang nur die früheste End-Menge final lassen?"
  - "Admin-Bearbeitung /BdeBookings/Edit kann auf jeder Buchung eine Mengenzeile auf Final setzen, ohne den Arbeitsgang zu prüfen. Gehört das in diese Spec (gleiche Prüfung, Meldung im Formular) oder bleibt es draußen? Empfehlung: draußen, weil es eine bewusste Korrektur durch den BDE-Admin ist."
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

> Bugfix-Spec zum Nebenfund aus dem Fix-Lauf [[2026-11-26-fix-lauf-v1-99-81]]. Code gelesen im Bündel-Worktree
> `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Stand v1.99.81 (HEAD cf750e19 laut Paket 21). Die Zeilennummern der Backlog-Notiz
> sind neu belegt. Bezug: ADR [[0037-teilfertigmeldung-nur-abschliessende-meldung-eindeutig]], [[0026-schreibaktionen-senden-die-absicht-der-server-prueft-am-schreibort]],
> [[0039-bde-werker-personalnummer-identifiziert-nicht-authentisiert]].
> Einen Bug-Datensatz unter `secondbrain/bugs/` gibt es noch nicht. Die Koordination hat nur diese Spec beauftragt. Falls einer gewünscht ist:
> `secondbrain/bugs/2026-11-26-bde-finish-doppelte-endmenge-bug.md`.
> Nummern sind **vorläufig**: Version erst nach v1.99.82 (die beansprucht [[2026-11-26-bde-nur-fa-meldung-entfernen-spec]]). Zwei weitere
> Entwürfe entstehen parallel. Diese Spec braucht **keine** SQL-Nummer. TS-Nummer: die nächste freie nach TS-202, sie wird bei der Bündelplanung vergeben.

## Ziel / Nutzen (das Warum)

Ein Arbeitsgang (AG) gilt im WMS als **erledigt**, sobald irgendeine nicht stornierte Buchung auf ihm eine End-Menge (`BdeBookingQuantities.IsFinal = 1`) trägt.
Belege dafür: `WorkOperationRepository.cs:162-166`, `BdeFaAngebot.cs:69-73`, `BdeScanResolver.cs:170-173` und `:221-222` sowie `BdeApiController.cs:230-233`.
Die Fertigmeldung ohne Zeit (`ReportCompletionCoreAsync`) verhindert eine zweite End-Menge auf dem AG, und zwar unter Sperre (BDE-1, ADR 0026).
Die zwei Wege, über die ein Werker seine laufende Buchung fertig meldet, prüfen das nicht: Einzel-Fertig (`FinishAsync`) und Gruppen-Abschluss (`FinishGroupAsync`).
Die Folgen:
- Ein AG kann zweimal „fertig gemeldet“ werden.
- Wird eine der beiden Buchungen storniert, bleibt der AG trotzdem erledigt.
- Für eine spätere Rückmeldung an Sage ([[2026-10-30-fa-rueckmeldung-an-sage]], noch nicht gebaut) ist nicht eindeutig, welche Meldung der Abschluss ist.

Ziel: eine Regel für alle Schreibwege. Welche Regel das ist, entscheidet Gerald (Rückfrage 1).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- `BdeBookingService.FinishAsync` und `FinishGroupAsync`: nach der Entscheidung aus Rückfrage 1 an die Regel aus `ReportCompletionCoreAsync` angleichen (Variante B1 oder B2). Bei Variante A wird stattdessen die Mehrfach-End-Menge dokumentiert und per Test festgeschrieben.
- Eine Prüfung „AG hat schon eine End-Menge“ an **einem** Ort, die alle drei Schreibwege nutzen.
- Sperre je AG (`sp_getapplock`, dieselbe Ressource wie `ReportCompletionCoreAsync`) auch für Einzel- und Gruppen-Fertig.
- Hinweistext an den Werker (FA-Sicht `bde-fa-sicht.js`).
- Hilfe, Changelog, Anwenderdoku Kapitel 11, Testszenario, ADR 0037 „Folgen“ ergänzen.

**Out-of-Scope**
- Admin-Bearbeitung `/BdeBookings/Edit` (`BdeBookingsController.cs:203-265`), außer Rückfrage 4 holt sie herein.
- Start- und Fortsetzen-Sperre auf schon fertigen AGs. `StartPlannedAsync` (`BdeBookingService.cs:36-112`) und `ResumeAsync` (`:175-252`) prüfen heute keine End-Menge.
  Eine Sperre dort würde pausierte Buchungen stranden lassen: Für den Werker bliebe nur „Abschließen“, und das bucht **ohne** Menge (`ClosePausedAsync` `:525-534`).
  Die Regel greift deshalb am Schreibort der Menge (ADR 0026).
- Bereinigung von Altbestand. Über einen Einmal-Skript-Eintrag entscheidet nur Rückfrage 3.
- Keine Rückmeldung an Sage: Es gibt keinen Leser im Dienst und keinen Sage-Schreibweg für BDE-Mengen (siehe Technischer Lösungsentwurf, „Leser“).

## Fachliche Anforderungen

### Wie die zweite End-Menge heute entsteht (aus dem Code abgeleitet, auf DEV nicht nachgestellt)

Alle Schreibwege für `IsFinal`:

| # | Schreibweg | Stelle | Prüft „AG schon fertig“? |
|---|---|---|---|
| S1 | Fertigmeldung ohne Zeit (FA-Sicht NurAg, BDE-Teileliste, Baugruppen-Weiterreichung an den Sub-FA-AG `BdeTeilelisteService.cs:784-806`) | `BdeBookingService.cs:267-328`, Prüfung `:293-295`, Sperre `:272` | **ja**, unter Sperre |
| S2 | Einzel-Fertig einer laufenden oder pausierten Buchung | `FinishAsync` `:330-345` → `FinishInternalAsync` `:473-486` (`q.IsFinal = true` `:482`) | **nein** |
| S3 | Gruppen-Abschluss | `FinishGroupAsync` `:382-425` (`q.IsFinal = entry.IsFinal` `:419`), Checkbox standardmäßig an (`bde-fa-sicht.js:215`) | **nein** |
| S4 | Admin-Bearbeitung | `BdeBookingsController.cs:210` (höchstens eine Final-Zeile **je Buchung**), `:245-258` | nur je Buchung |
| – | Pause, Teilmenge | `PauseAsync` `:166-169`, `ReportPartialQuantityAsync` `:355`, `CreatePartialQuantity` `:507` (`IsFinal = false`) | schreibt nie final |
| – | Automatisches Schließen (Regel 1–3 beim Start) | `FinishAndSaveAsync(b, null, null)` `:86`, `:92`, `:96`, `:128`, `:215`, `:219` | ohne Menge, also keine End-Menge (`:479`) |

Die Datenbank schützt nur **je Buchung**: Der gefilterte UNIQUE-Index `IX_BdeBookingQuantities_Booking_Final` liegt auf `BdeBookingId WHERE IsFinal = 1` (`ApplicationDbContext.cs:1632-1635`).
Ein Index je AG ist nicht möglich, weil `IsFinal` und `WorkOperationId` in zwei Tabellen liegen (`BdeBookingService.cs:271`, ADR 0026).

Drei Wege führen zur zweiten End-Menge:
1. **Ohne jede Mehrfach-Einstellung (Standard):**
   - Werker A pausiert. Die Pause hat `EndedAt` gesetzt ([[fallstricke]] §6).
   - Werker B startet denselben AG. Die Kollisionsprüfung sieht nur Buchungen mit `EndedAt == null` (`:65-67`), das Angebot filtert nur fertige AGs (`BdeFaAngebot.cs:83`).
   - B meldet Fertig: erste End-Menge.
   - A setzt fort. `ResumeAsync` und `FortsetzenSperreAsync` (`:521-523`) prüfen keine End-Menge.
   - A meldet Fertig über S2: zweite End-Menge.
2. **`BdeMehrfachBuchungProArbeitsgang` = true:**
   - A und B buchen parallel. A meldet Fertig, der Dialog „Andere beenden“ erscheint (`BdeTerminalController.cs:193-213`).
   - Antwortet A mit „Nein, nur meine“, laufen die anderen weiter (`bde-fa-sicht.js:182`).
   - B meldet später Fertig über S2 oder S3: zweite End-Menge.
3. **`BdeMehrfachBuchungProOperator` = true:**
   - Ein Werker kann denselben AG zweimal parallel starten, denn eine Kollision zählt nur bei einem fremden Werker (`:69`). Bei laufender eigener Produktion geht es mit multiOp weiter (`:105-107`).
   - Mit `BdeGleichzeitigerAbschlussBeiMehrfachStart` stehen beide Buchungen im Gruppen-Dialog. Sind beide Checkboxen gesetzt, entstehen zwei End-Mengen in **einem** Request.

Ein Doppeltipp auf dieselbe Buchung ist heute schon durch den Index abgefangen. Die zweite Anfrage scheitert dann mit `DbUpdateException`, und der Werker sieht den festen Text „Serverfehler – nichts sicher gebucht“ (`BdeTerminalController.cs:174-178`, `:297-301`).
Mit der Sperre aus dieser Spec bekommt er stattdessen „Bereits beendet.“ (`:335-336`).

### Rolle der Einstellungen (belegt)

Alle drei Einstellungen sind bool, haben den Default `false`, werden geseedet und gelten an beiden Standorten (`AppSettingDefinitions.cs:226-232`). Auf DEV standen sie laut QA v1.99.81 auf `false`.

| Einstellung | Wirkung im Code | Bezug zum Fehler |
|---|---|---|
| `BdeMehrfachBuchungProArbeitsgang` | Lässt laufende Produktionsbuchungen **verschiedener** Werker auf demselben AG zu: Start `:58`, `:63-71`; Fortsetzen `:195`, `:199-206`; Fertigmeldung ohne Zeit `:304`. Steuert den Dialog „Andere beenden“ (`BdeTerminalController.cs:197-199`). | Hauptweg 2. Die Backlog-Notiz spricht von „Mehrfachbuchung“; gemeint ist **diese** Einstellung, nicht ProOperator. |
| `BdeMehrfachBuchungProOperator` | Lässt mehrere laufende Produktionen **eines** Werkers zu: `:59`, `:105`, `:196`, `:226`. Voraussetzung für den Gruppen-Dialog (`BdeTerminalController.cs:133-136`). | Weg 3 (derselbe AG zweimal) und Zugang zu S3. |
| `BdeGleichzeitigerAbschlussBeiMehrfachStart` | Nur `BdeTerminalController.cs:131-136`: Ab 2 laufenden Produktionen an derselben Werkbank liefert „Fertig“ `GroupFinishRequired`, und der Gruppen-Abschluss S3 läuft statt S2. | Schaltet S3 frei. Ohne diese Einstellung läuft alles über S2. |

Weg 1 braucht **keine** dieser Einstellungen. Der Fehler betrifft also auch Standorte mit Standardeinstellungen.

### Varianten (Entscheidung Gerald, Rückfrage 1)

| | A: „je Buchung eine End-Menge, Summe zählt“ | **B1: je AG genau eine, spätere als Teilmenge** (Empfehlung) | B2: je AG genau eine, spätere abgewiesen |
|---|---|---|---|
| Regel | Mehrere `IsFinal` je AG sind zulässig. S1 bleibt die Ausnahme: Doppelschutz gegen zwei Fertigmeldungen ohne Zeit (BDE-1). | Höchstens eine nicht stornierte End-Menge je AG, über S1–S3. | wie B1 |
| Fertig auf einem schon fertigen AG (S2/S3) | wie heute: zweite End-Menge | Buchung wird beendet. Die Menge wird mit `IsFinal = false` gespeichert. Hinweis: „Arbeitsgang war bereits fertig gemeldet – Ihre Menge ist als Teilmenge gebucht.“ | Abweisung „Arbeitsgang ist bereits fertig gemeldet.“ Nichts wird geschrieben, die Buchung läuft weiter oder bleibt pausiert. |
| Mengen in den Summen | unverändert | unverändert, denn alle Summen-Leser zählen jede Menge ohne Blick auf `IsFinal` (siehe „Leser“) | Die Menge des Werkers fehlt, bis er Pause oder Teilmenge bucht |
| Aufwand | Test und Doku | S, ein Service, zwei kleine JS-Stellen | S, aber der Werker steckt fest (Fortsetzen geht, Fertig nicht) |
| Spätere Sage-Rückmeldung | muss die „richtige“ End-Menge selbst wählen | eindeutig | eindeutig |

**Empfehlung B1.** Gründe:
- Dieselbe Bedeutung wie die Teileliste: höchstens **ein** Abschluss je Zeile (ADR 0037).
- Dieselbe Bedeutung wie die Fertigmeldung ohne Zeit (S1).
- Der Werker verliert weder Zeit noch Menge.
- Kein Leser muss geändert werden.
- Eine Rückmeldung an Sage findet später genau einen Abschluss je AG.

B2 blockiert den Werker an einer Stelle, an der er nichts falsch gemacht hat. A lässt die Uneindeutigkeit bestehen.

## Technischer Loesungsentwurf

Muster: Prüfung am Schreibort unter Sperre (ADR 0026). Der Entwurf beschreibt Variante B1. B2 und A stehen am Ende.

1. **Ein Prädikat:** `private Task<bool> AgHatEndMengeAsync(int workOperationId)` in `BdeBookingService` übernimmt die Abfrage aus `:293-294` (`IsFinal && !IsCancelled && WorkOperationId == id`).
   `ReportCompletionCoreAsync` ruft es statt der Inline-Abfrage auf. Verhalten und Text „Arbeitsgang ist bereits fertig gemeldet.“ bleiben gleich.
2. **`FinishAsync` (S2):**
   - Nur bei `BookingType == Production` mit Menge und `WorkOperationId` läuft das Ganze in `InTransactionAsync`.
   - Zuerst die `WorkOperationId` lesen (`AsNoTracking`), dann `SperreArbeitsgangAsync(woId)` (`:632`, Ressource `BDE-Fertigmeldung:{id}`, dieselbe wie S1).
   - **Danach** die Buchung laden und die Statusprüfung `:335-341` ausführen. So wird im Wiederholfall unter der Sperre neu geprüft (ADR 0026).
   - Bei Zeitüberschreitung der Sperre gilt derselbe Text wie in `:273`.
   - `istFinal = !await AgHatEndMengeAsync(woId)` und `FinishInternalAsync(b, good, scrap, istFinal)`. Der neue Parameter wird in `:482` statt `true` gesetzt.
   - Bei `istFinal == false` liefert der Service `BdeBookingResult.Success(b, Hinweis)`; die Überladung mit Text gibt es schon (`:360`).
   - Rüsten und Tätigkeit ohne Menge bleiben ohne Transaktion wie heute.
3. **`FinishGroupAsync` (S3):**
   - Läuft in einer Transaktion. `InTransactionAsync` wird generisch (`Task<T>`), weil der Rückgabetyp `GroupFinishResult` ist.
   - Nach den Prüfungen `:384-407` werden die verschiedenen `WorkOperationId` der Zeilen **aufsteigend sortiert** gesperrt. Die feste Reihenfolge verhindert Deadlocks zwischen zwei Gruppen.
   - Je Zeile gilt `final = entry.IsFinal && !AgHatEndMenge(wo) && wo noch nicht in diesem Request final gesetzt`.
   - Herabgestufte Zeilen sammeln sich in einem Hinweis. `GroupFinishResult` (`IBdeBookingService.cs:32`) bekommt dafür `string? Hinweis = null`.
4. **Controller/JS:**
   - `BdeTerminalController.FinishGroup` (`:231`) gibt `message = result.Hinweis` mit.
   - `bde-fa-sicht.js:234` hängt `r.message` an den Erfolgstext.
   - `bde-fa-sicht.js:75`: Der Pfad „Andere beenden“ verschluckt heute `json.message`. Künftig bekommt `andereBeenden` den Text `okText + ' ' + json.message`, wenn ein Hinweis da ist.
   - S2 zeigt `json.message` schon heute (`:76`).
5. **Sperrreihenfolge geprüft:**
   - Die Teileliste nimmt erst die Schlüssel-Sperre (`BdeTeilelisteService.cs:716`), dann in S1 die AG-Sperre.
   - S2 nimmt nur eine AG-Sperre, S3 nur AG-Sperren in sortierter Reihenfolge.
   - Niemand hält eine AG-Sperre und wartet auf eine Schlüssel-Sperre, also gibt es keinen Zyklus.
6. **InMemory:** `InTransactionAsync` und die Sperre sind dort wirkungslos (`:621`, `:638`). Die Unit-Tests prüfen das Prädikat. Den echten Gleichstand prüft ein Manual-UAT ([[fallstricke]] §8, ADR 0026).

**Leser, die von „eine End-Menge je AG“ abhängen könnten:** Alle wurden geprüft. Keiner braucht eine Änderung.
- *Existenz* („erledigt“ bzw. „anbieten“), alle über `Any` oder `Distinct`, also robust gegen Dubletten:
  - `WorkOperationRepository.cs:162-166`, dahinter `ArbeitsgangStatusRegel.Ableiten` über `FaAgStatusService.cs:45` und `BdeFaAngebot.cs:109`
  - `BdeFaAngebot.cs:69-73`
  - `BdeScanResolver.cs:170-173` und `:221-222`
  - `BdeApiController.cs:230-233`
  - `BdeBookingService.cs:293` und `:375` (0/0-Regel, liest `!IsFinal`)
- *Summen* ohne Blick auf `IsFinal`:
  - `BdeBookingService.BisherGutAsync` `:365-367`
  - `BdeFaAngebot.BisherGutAsync` `:49-54`
  - `BdeKopfBuchungen.cs:49`
  - `BdeTageshistorie.cs:51-52`
  - `BdeTimeSplitService.cs:145`
  - `BdeApiController.cs:167-168`
  - `BdeBookingsController.cs:87-88`
- *Dienst:* `IDEALAKEWMSService` liest weder `IsFinal` noch `BdeBookingQuantities` (Grep: 0 Treffer). Er fasst BDE-Buchungen nur in `BdeAutoPauseService.cs` an.
- *Sage:* Es gibt keinen Sage-Schreibweg für BDE-Mengen. Die Sage-Queue (`SageBooking*`) nutzt weder `BdeBooking` noch `WorkOperation`. `SageRueckmeldungAktiv` wurde in v1.79.0 entfernt (`AppSettingDefinitions.cs:245`).
- *Storno:* `BdeBookingsController.cs:365` und `BdeTeilelisteService.cs:840-845`. Mit B1 öffnet das Stornieren der einzigen End-Menge den AG wieder. Mit A bleibt er erledigt, solange eine zweite existiert.

**B2 statt B1:** In Schritt 2 und 3 wird bei vorhandener End-Menge `Invalid("Arbeitsgang ist bereits fertig gemeldet.")` bzw. `GroupFinishResult(0, …)` zurückgegeben, ohne Schreiben. Die JS-Schritte 4 entfallen.
**A statt B:** Kein Schreibweg wird geändert. Ein Test hält fest, dass S2 und S3 eine zweite End-Menge schreiben. ADR 0037 bekommt unter „Folgen“: „mehrere End-Mengen je AG bei Buchungen zulässig, nur S1 sperrt“. Hilfe-Text `Index.cshtml:2185` bleibt.

## Migrations-/SQL-Auswirkungen

- Keine Migration, kein Schema, kein Skript unter `SQL/`, also keine SQL-Nummer. `00_FreshInstall.sql` bleibt unberührt.
- Ein Index je AG ist technisch nicht möglich (zwei Tabellen). Den Schutz übernimmt `sp_getapplock` (ADR 0026).
- **Prüfabfragen** gegen die WMS-Datenbank. Sie werden **nicht** vom Spec-Lauf ausgeführt (Rückfrage 3). Ausführen auf DEV AKE, DEV IDEAL, Prod AKE und Prod IDEAL, nur lesend:

P-1 (aus der Backlog-Notiz, wörtlich): AGs mit mehr als einer End-Menge.
```sql
SELECT b.WorkOperationId, COUNT(*) FROM BdeBookingQuantities q JOIN BdeBookings b ON b.Id=q.BdeBookingId WHERE q.IsFinal=1 AND b.IsCancelled=0 GROUP BY b.WorkOperationId HAVING COUNT(*)>1
```

P-2: Einzelheiten zu den Treffern aus P-1. Sie zeigen, welcher Weg die Dublette erzeugt hat: parallel, über Fortsetzen (`ParentBookingId`) oder in einer Gruppe (gleiches `EndedAt`).
```sql
SELECT b.WorkOperationId, b.Id AS BookingId, b.BdeOperatorId, b.ProductionWorkplaceId, b.Status, b.StartedAt, b.EndedAt,
       b.ParentBookingId, q.Id AS QuantityId, q.GoodQuantity, q.ScrapQuantity, q.ReportedAt, q.CreatedBy
FROM dbo.BdeBookingQuantities q
JOIN dbo.BdeBookings b ON b.Id = q.BdeBookingId
WHERE q.IsFinal = 1 AND b.IsCancelled = 0
  AND b.WorkOperationId IN (SELECT b2.WorkOperationId FROM dbo.BdeBookingQuantities q2 JOIN dbo.BdeBookings b2 ON b2.Id = q2.BdeBookingId
                            WHERE q2.IsFinal = 1 AND b2.IsCancelled = 0 GROUP BY b2.WorkOperationId HAVING COUNT(*) > 1)
ORDER BY b.WorkOperationId, q.ReportedAt;
```

P-3: Stand der Mehrfach-Einstellungen (Status-Werte: 1 Running, 2 Paused, 3 Finished, 4 Resumed, 5 AutoPaused, `Models/BdeBookingStatus.cs:5-9`).
```sql
SELECT [Key], [Value] FROM dbo.AppSettings
WHERE [Key] IN (N'BdeMehrfachBuchungProArbeitsgang', N'BdeMehrfachBuchungProOperator', N'BdeGleichzeitigerAbschlussBeiMehrfachStart');
```

Ob die Einstellungen in Prod je eingeschaltet waren, ist unbekannt. Erst P-1 und P-3 zeigen, ob der Fehler in Prod schon Daten erzeugt hat.

## Einstellungen

keine. Die drei BDE-Mehrfach-Einstellungen werden nur gelesen, ihre Rolle steht unter „Fachliche Anforderungen“.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

- Es gibt kein neues Feld. Die Buchung bekommt `ModifiedAt`, `ModifiedBy` und `ModifiedByWindows` wie bisher über `SetAuditModified` (`FinishInternalAsync` `:477`, Gruppe `:415`).
- Die Mengenzeile bekommt `CreatedAt`, `CreatedBy` und `CreatedByWindows` über `CreatePartialQuantity` → `SetAudit` (`:510`).
- In den Daten lässt sich eine herabgestufte End-Menge (B1) nicht von einer bewusst nicht finalen Gruppenzeile unterscheiden: Beide sind `Finished` mit `IsFinal = 0`.
  Das ist bewusst so; eine eigene Kennzeichnung wäre ein Schemawechsel ohne Leser. Sichtbar wird die Herabstufung im Hinweis an den Werker.

## Akzeptanzkriterien

Die Kriterien gelten für Variante B1. Bei B2 oder A gelten die Abweichungen aus AK-9 bzw. AK-10.

1. Eine Produktionsbuchung auf einem AG **ohne** End-Menge wird über „Fertig“ (S2) beendet und schreibt genau eine Mengenzeile mit `IsFinal = 1`. Das Verhalten ist unverändert.
2. Eine Produktionsbuchung auf einem AG **mit** nicht stornierter End-Menge wird über „Fertig“ beendet. Ihre Mengenzeile hat `IsFinal = 0`. Die Antwort ist `Success` mit dem Hinweis „Arbeitsgang war bereits fertig gemeldet – Ihre Menge ist als Teilmenge gebucht.“, und die FA-Sicht zeigt ihn an.
3. Wie AK-2, aber nach dem Dialog „Andere beenden“ (`BdeMehrfachBuchungProArbeitsgang` = true): Der Hinweis geht nicht verloren.
4. Im Gruppen-Abschluss (S3) bekommt eine Zeile mit gesetzter Checkbox auf einem schon fertigen AG `IsFinal = 0`, und der Hinweis nennt die betroffenen AGs. Die übrigen Zeilen werden wie gewählt gespeichert.
5. Stehen zwei Zeilen desselben AG in einer Gruppe, beide mit Checkbox, entsteht genau **eine** End-Menge für diesen AG.
6. Nach jedem Weg aus AK-1 bis AK-5 liefert P-1 für die Test-AGs keine Zeile. Die Summe Gut (Kopf „bisher“, Tageshistorie) enthält alle gemeldeten Mengen.
7. Zwei gleichzeitige „Fertig“-Anfragen auf zwei Buchungen desselben AG (Mehrfachbuchung) ergeben genau eine End-Menge. Ein zweites „Fertig“ auf **dieselbe** Buchung ergibt „Bereits beendet.“ statt „Serverfehler“. Nachweis per Manual-UAT, nicht InMemory.
8. Diese Wege verhalten sich wie vor der Änderung: `ReportCompletionCoreAsync` (Text „Arbeitsgang ist bereits fertig gemeldet.“), die 0/0-Regel (`NullMengeFehlerAsync`), Rüsten- und Tätigkeit-Fertig ohne Menge sowie die Baugruppen-Weiterreichung der Teileliste. Die bestehenden Tests bleiben grün.
9. (nur B2) In AK-2 und AK-4 wird abgewiesen, und die Datenbank bleibt unverändert: Die Buchung bleibt `Running` bzw. `Paused`, es entsteht keine Mengenzeile.
10. (nur A) Ein Test hält fest, dass S2 und S3 eine zweite End-Menge schreiben. ADR 0037 und die Hilfe beschreiben die Regel.
11. Der Hilfe-Text zur Abschluss-Checkbox (`Views/Help/Index.cshtml:2185`) und das Anwenderkapitel 11 nennen die Regel. Die Version steht in beiden `AppVersion.cs` und im Changelog.

## Test-Szenarien

**Unit-Tests** (`IdealAkeWms.Tests/Services/BdeMengenRegelTests.cs`, InMemory):
- `Finish_AgOhneEndMenge_SchreibtFinal`
- `Finish_AgMitEndMenge_SchreibtTeilmengeMitHinweis`
- `Finish_AgMitStornierterEndMenge_SchreibtFinal`
- `FinishGroup_ZeileAufFertigemAg_Herabgestuft`
- `FinishGroup_ZweiZeilenGleicherAg_EineEndMenge`
- `ReportCompletionCore_NachFinish_WeiterAbgewiesen` (Regression S1)
- Bei B2 stattdessen die Varianten `…_Abgewiesen_DbUnveraendert`.

**docs/TESTSZENARIEN.md:** neues Kapitel „TS-2xx — BDE: eine End-Menge je Arbeitsgang“. Die Nummer ist vorläufig, die nächste freie nach TS-202 wird bei der Bündelplanung vergeben. Bezug TS-138 (BDE-1) und TS-202.2/202.3.
- **2xx.1 Fortsetzen nach fremdem Abschluss** (ohne Mehrfach-Einstellungen):
  - Werker A startet Produktion und pausiert mit Gut 1.
  - Werker B startet denselben AG und meldet Fertig mit Gut 1.
  - A setzt fort und meldet Fertig mit Gut 1: Hinweis „bereits fertig gemeldet …“.
  - DB: P-1 liefert keine Zeile, die Summe Gut für den AG ist 3.
- **2xx.2 Mehrfachbuchung je AG** (`BdeMehrfachBuchungProArbeitsgang` vorübergehend true):
  - A und B laufen parallel. A meldet Fertig und antwortet „Nein, nur meine“.
  - B meldet Fertig: Hinweis sichtbar, P-1 liefert keine Zeile.
- **2xx.3 Gruppe** (ProOperator und gemeinsamer Abschluss vorübergehend true):
  - Eine Gruppe mit einem AG, den ein anderer Werker schon fertig gemeldet hat: Hinweis nennt den AG.
  - Derselbe AG zweimal in der Gruppe: genau eine End-Menge.
- **2xx.4 Regressionen:**
  - Fertig ohne vorherige End-Menge schreibt `IsFinal = 1`.
  - Fertig 0/0 ohne Teilmenge wird weiter abgewiesen.
  - Teileliste Baugruppe: Ein Sub-FA-AG, der schon fertig ist, erzeugt weiter „nur die Teilmeldung gespeichert“ (`BdeTeilelisteService.cs:802-806`).
- **2xx.5 Gleichstand (Manual-UAT):**
  - Zwei gleichzeitige `POST /BdeTerminal/Finish` auf zwei Buchungen desselben AG ergeben genau eine End-Menge.
  - Ein doppelter POST auf dieselbe Buchung ergibt „Bereits beendet.“.
- **2xx.6 Aufräumen:**
  - Einstellungen zurück auf `false` (P-3).
  - Testbuchungen stornieren.
  - `SageBookingQueueItems` unverändert (nichts an Sage).

Standort: beide. Der Code ist gemeinsam, und die FA-Sicht bucht an beiden Standorten über `BdeTerminalController`. Auf DEV AKE und DEV IDEAL prüfen.

## Etappen (nur bei epic: true)

entfällt (epic: false)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

**Vorläufig.** Der QA-Lauf bestätigt ihn gegen den echten Diff.

- **Web-App:** ja (`BdeBookingService`, `BdeTerminalController`, `bde-fa-sicht.js`, Hilfe)
- **Service:** ja, nur `AppVersion`. Das Paket enthält immer beide.
- **Migration:** nein
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine. Es gibt kein SQL-Skript, keine Einstellung und keinen Leitstand-Schritt.
  - Eine Prüfung mit P-1 als dauerhaften Eintrag ist bewusst nicht vorgesehen: Bleibt Altbestand stehen (Empfehlung Rückfrage 3), würde sie bei jedem Update erneut melden.
  - Entscheidet Gerald in Rückfrage 3 für eine Bereinigung, kommt ein Einmal-Skript `SQL/Einmalig/…` mit Eintrag `Typ='Einmalig'`, `Firma='beide'`, `Phase='Nachher'`, `Ausfuehrung='Trockenlauf'` dazu.
    Für das Skript gelten die Regeln aus ADR 0023: genau ein `DECLARE @Trockenlauf BIT = 1;`, im Trockenlauf eine Zeile `TREFFER: n`, die Wiederholungssperre gibt `TREFFER: 0` aus. Die Spec wird dann nachgezogen.
- **Publish-Befehle** (im Worktree):
  - `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  - `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

## Offene Rueckfragen

1. Entscheidung: Variante A (je Buchung eine End-Menge, mehrere `IsFinal` je Arbeitsgang zulässig, Summe zählt), B1 (je Arbeitsgang genau eine End-Menge; spätere Fertig-Meldungen werden als Teilmenge gespeichert, die Buchung wird beendet, mit Hinweis) oder B2 (je Arbeitsgang genau eine; eine spätere Fertig-Meldung wird abgewiesen, die Buchung läuft weiter)? **Empfehlung: B1** (Begründung unter „Varianten“).
2. (Fachbereich: Walter/Benjamin) Kommt es an IDEAL vor, dass zwei Werker am selben Arbeitsgang arbeiten oder ein Werker seine Pause fortsetzt, nachdem ein Kollege den Arbeitsgang schon fertig gemeldet hat? Was soll die zweite Fertig-Meldung dann bedeuten: zusätzliche Menge (Teilmenge) oder eigener Abschluss?
3. Prüfabfragen P-1 bis P-3 auf DEV AKE, DEV IDEAL, Prod AKE und Prod IDEAL ausführen und das Ergebnis eintragen. Bei Treffern in P-1: Bestand unverändert lassen (Empfehlung: Die Summen stimmen, und „erledigt“ ist richtig) oder per Einmal-Skript je Arbeitsgang nur die früheste End-Menge final lassen?
4. Die Admin-Bearbeitung `/BdeBookings/Edit` (`BdeBookingsController.cs:210`, `:245-258`) kann auf jeder Buchung eine Mengenzeile auf Final setzen, ohne den Arbeitsgang zu prüfen. Gehört das in diese Spec (gleiche Prüfung, Meldung im Formular) oder bleibt es draußen? **Empfehlung: draußen**, weil es eine bewusste Korrektur durch den BDE-Admin ist.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Variante A / B1 / B2 →
2. (Fachbereich: Walter/Benjamin) Zweiter Werker bzw. Fortsetzen nach fremdem Abschluss – Bedeutung der zweiten Fertig-Meldung →
3. Ergebnis P-1 bis P-3 je System; Altbestand lassen oder bereinigen →
4. Admin-Bearbeitung /BdeBookings/Edit in Scope oder draußen →
