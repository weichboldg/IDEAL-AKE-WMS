---
type: testbericht
created: 2026-11-14
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
modul: Kommissionierung, Kommissionierbereiche
stand: "Bündel v1.99.22 (90bbbbd7), DEV AKE localhost:8081 / AKE_WMS_DEV, DEV IDEAL localhost:8082 / IDEAL_WMS_DEV"
durchgefuehrt: "2026-11-13 19:40–20:25 (Ortszeit DEV-PC)"
---
# Teil B1 – Kommissionierung und Kommissionierbereiche auf DEV

Teil B1 aus [[2026-11-14-wochenend-review-test-anwenderdoku]]. Grundlage: `docs/TESTSZENARIEN.md` im Bündel-Worktree (TS-120, TS-79, TS-80,
TS-107.6, TS-124, TS-130, TS-134) und die Review-Notizen [[2026-11-14-review-befunde-kommissionierung]] und
[[2026-11-14-review-befunde-kommissionierbereiche]].

**Vorgehen:** Browser 2 (eigener Tab, am Ende geschlossen). Teils über die Oberfläche (Klicks), teils über dieselben POST-Aufrufe, die
die Seiten senden (`fetch` mit Antiforgery-Token im angemeldeten Tab). Prüfung der Wirkung mit SQL auf der DEV-DB und im Web-Log
`D:\Dev\AKE\Web\logs\idealakewms-20261113_004.log`. Nichts an Sage geschrieben (Umbuchungen gehen nie in die Sage-Queue, geprüft:
`SageBookingQueueItems` für alle 101 Test-Bewegungen = 0). Keine Mails (Bedarfsmeldungs-Mails sind in Web und Dienst aus). Nicht neu deployt.

**Einschränkungen:**
- Nur ein Windows-Benutzer (gerald.weichbold, Admin) → Rollen-Negativfälle (P ohne Leitstand, R nur lesend, ohne `picking`) nicht prüfbar.
- AKE-DEV hat keine Sage-Sicht `ake.dbo.vw_AKE_Kommissionierung_StuecklistenDB`; Stücklisten gibt es nur für die 12 Artikel im BOM-Cache
  (`CachedBomHeaders`). Darum nur FAs mit EU253711, EU253691, S0119385, S0311556 usw. getestet.
- AKE-DEV hatte zu Beginn **keinen aktiven Kommissionierer** (nur `gerald.berger`, inaktiv) bei `KommissionierungMitZuweisung = true`.
  Für die Freigaben zwei Test-Benutzer angelegt (Spur), am Ende deaktiviert.
- Screenshots: Das Screenshot-Werkzeug fiel auf der Auftragsseite (`/Picking/Auftrag/…`) mehrfach mit Zeitüberschreitung aus. Wo kein
  Bild ging, steht der DOM-/SQL-Beleg in der Tabelle.

## Ergebnis-Tabelle

| TS-Nr. | Standort | Ergebnis | Beleg/Bemerkung |
|---|---|---|---|
| 120.1 Bereich anlegen/ändern | AKE | OK | „Bereich „QA Testbereich WE14“ angelegt.“; doppelter Name: „Bereich nicht angelegt: Name „Lager“ ist schon vergeben.“ |
| 120.1 zweiter Standard | AKE | OK | Glas als Standard: „Es gibt bereits einen Standardbereich („Lager“). Zuerst dort abwählen.“, nichts gespeichert |
| 120.1 Standard deaktivieren | AKE | OK | „Der Standardbereich muss aktiv sein.“ |
| 120.1 Standard abwählen | AKE | OK | Warnung „Es gibt jetzt keinen aktiven Standardbereich …“, danach neu setzen ohne Fehler |
| 120.1 Regel Artikelgruppe, Doppelregel, manipulierter POST | AKE | OK | „Regel Artikelgruppe „850“ angelegt.“; 850 → Lager: „… ist schon dem Bereich Glas zugeordnet.“; `XYZ-gibtsnicht`: „… kommt als Artikelgruppe im Stamm nicht vor.“ |
| 120.1 / 120.12 Benutzer R ohne Knöpfe, P ohne Leitstand-Knöpfe | AKE | NICHT PRUEFBAR | nur ein Windows-Benutzer (Admin) verfügbar |
| 120.2 Freigabe → Aufträge je Bereich | AKE | OK | FA 2607171: KO-2026-00020 (Lager, 82 Z.), -00021 (Glas, 3 Z.), -00022 (VK, 12 Z.) = 97 Stücklistenzeilen; Prio 42 und Kommissionierer wie FA |
| 120.2 erneut erzeugen | AKE | OK | „0 Kommissionierauftrag/-aufträge für 0 FA angelegt; 1 FA ohne offene Restmenge.“ |
| 120.2 Sammelfreigabe mit FA ohne Stückliste | AKE | NICHT PRUEFBAR | DEV ohne Sage-Stücklistensicht → HTTP 500 (siehe Neue Befunde N-3); fachlich auf IDEAL geprüft |
| 120.2 Sammelfreigabe mit FA ohne Stückliste + Sub-FA | IDEAL | OK | IDs 27 (1043153) + 28 (Sub-FA 1043154): „FA 1043153 ohne Stückliste – kein Kommissionierauftrag. · Übersprungen (Sub-FA — Kommissionierung nur am HauptFA): 1043154“ |
| 120.2 `LeitstandAktiv` aus → Aufträge beim Öffnen | AKE | OK | Einstellung kurz auf `false`, `/Picking/Bom/868` → „3 … angelegt (KO-2026-00050 … -00052)“; Einstellung zurück auf `true` (zugleich K-8 reproduziert, siehe unten) |
| 120.3 Umbuchen auf Wagen, „Vollständig“ (Oberfläche) | AKE | OK | KO-2026-00022 über die Seite: alle 12 Zeilen markiert, Wagen 2, Konflikt-Modal, „Trotzdem buchen“, Status-Modal „Vollständig“ → Status 40, 12 Bewegungen mit `ProductionOrderId = 144`, `SageBookingQueueItems` = 0 |
| 120.3 Auftragsseite zeigt alle Zeilen des Auftrags | AKE | **FEHLER** | Glas-/VK-Aufträge: Unterpositionen, deren Baugruppe im Lager-Auftrag liegt, sind unsichtbar, auch nach „Alle aufklappen“ (KO-2026-00048: 2 von 2 Zeilen; KO-2026-00022: 8 von 12). Neuer Befund N-1. ![[TS-120.3-AKE-glasauftrag-unterpositionen-unsichtbar.jpg]] (Bild zeigt den Zustand mit Standardfilter; DOM nach Entfernen des Filters und „Alle aufklappen“: alle `tr` `display:none`) |
| 120.3 Auftragsseite zeigt alle Zeilen des Auftrags | IDEAL | **FEHLER** | KO-2026-00005 (FA 1060414, Lager): „79 Positionen“, sichtbar 6 (Pos. 1, 4, 11–14), 73 Zeilen mit Vater-Position 3, 5–10 … nicht im Auftrag → nie sichtbar, „Alle aufklappen“ wirkungslos. N-1. ![[TS-120.3-IDEAL-auftrag-73-von-79-zeilen-unsichtbar.jpg]] |
| 120.4 Ist-Menge 0, −1, „abc“, > Soll | AKE | OK | je HTTP 400: „Ist-Menge 0 muss größer als 0 und höchstens Soll 2 sein.“, „„abc“ ist keine Zahl.“; nichts gebucht |
| 120.4 „Teilweise“ → Folgeauftrag, dann „Vollständig“ | AKE | OK | KO-2026-00020: Zeile 00730608 Soll 48 / Ist 40 → KO-2026-00034 „Rest aus KO-2026-00020“, Soll 8, FA „teilkommissioniert“; -00034 vollständig → `faAbgeschlossen: true`, FA „abgeschlossen“ |
| 120.5 Lager fertig, Glas offen | AKE | OK | nach Abschluss VK und Glas-Folgeauftrag, Lager noch offen: FA „teilkommissioniert“, `IsDonePicking = 0` |
| 120.6 Stücklistenposition nachträglich ergänzen | AKE | NICHT PRUEFBAR | Testsystem mit Sage-Stückliste nötig |
| 120.7 Prio/Kommissionierer am FA ändern | AKE | OK | FA 2629001: Prio 47, Kommissionierer 2 → KO-2026-00031/-32/-33 folgen |
| 120.7 Rücknahme ohne / mit Fortschritt | AKE | OK | ohne: „3 … storniert (KO-2026-00025, -00026, -00027)“; mit: Warnung „… bleiben bestehen – bitte klären: FA 2629001: KO-2026-00031“, -00032/-00033 storniert |
| 120.8 Liste: Paging, Bereich, Status, Spaltenfilter, Nur meine, Werkbank | AKE | OK | `status=alle`: 49 Treffer, Seite 2 = 26–49; Bereich Glas/VK je 1; `colf_nummer`, `colf_order-number`, `colf_picking-date=kw34` (7), `colf_workbench=F3` (2), `colf_status=storniert` (28); ohne „aller Kommissionierer“ nur eigene (3); VK-Auftrag fällig 1 Tag vor Lager (Vorlauf) |
| 120.8 leerer Bereich | AKE | OK | Filter „QA Testbereich WE14“: 0 Zeilen, keine Fehlerseite |
| 120.9 Auftrag drucken | AKE | OK | KO-2026-00020: Kopf Nummer/Bereich/Status/Fällig/Artikel/FA/Fertigungstermin, Barcode, Spalten Pos./Artikelnummer/Bezeichnung/Lagerplatz/Soll/Ist/Status, alle „gepickt“; Handscanner-Teil nicht geprüft (kein Gerät), Filter „Auftrag“ per Eingabe findet KO-2026-00034 |
| 120.10 zwei gleichzeitige Freigaben | AKE | **FEHLER** | 4 Läufe mit zwei parallelen `ToggleRelease` (FA 2627001 + 2645001): 1× beide OK, 3× einer HTTP 500 (Deadlock-Opfer, „deadlocked … chosen as the deadlock victim“). Folge: FA 2645001 freigegeben **ohne** Auftrag. Neuer Befund N-2. ![[TS-120.10-parallele-freigabe-fehler500.jpg]] ![[TS-120.10-fa-freigegeben-ohne-auftrag-nach-deadlock.jpg]] |
| 120.11 Altbestand-Skript auf Prod-Kopie | AKE | NICHT PRUEFBAR | Prod-Kopie, daten-umbauend |
| 120.13 „Zurück zur Kommissionierung“ | AKE | OK | Link `/Picking` |
| 120.13 HauptFA + Sub-FA markieren, Nachziehen | IDEAL | OK | IDs 284 + 275: „1 markierte FA übersprungen (nicht freigegeben, erledigt, storniert oder Sub-FA).“ |
| 120.13 Standard Lager → Glas → Lager | AKE | OK | je abwählen (Warnung) und setzen, danach genau ein Standard (Lager) |
| 120.14 Altbestand prüfen (SQL/141 + Skript) | AKE | NICHT PRUEFBAR | daten-umbauendes Einmal-Skript, DEV-Kopie nötig |
| 120.15 kein Kommissionierer | AKE | OK | Ausgangszustand AKE-DEV: Dialog gelb „Kein Benutzer als Kommissionierer gekennzeichnet …“, Knopf „Freigeben“ `disabled`; Server-POST ohne Kommissionierer: „Bitte einen Kommissionierer zuweisen.“ (Sammel- und Ändern-Dialog nicht einzeln geöffnet) |
| 120.16 Druck in Sortierreihenfolge | AKE | OK | KO-2026-00022, „Bezeichnung 1“ ▲ und ▼: Druckreihenfolge = Seitenreihenfolge (12 Zeilen) |
| 120.17 Matchcode Liste/Filter/Druck | IDEAL | OK | Spalte „Matchcode“ nach „Artikelnummer“; `colf_matchcode=KTE-L3` → 1 Treffer; Druckkopf „Matchcode: KTE-L3-76-3T RAL 2024“. Zahnrad aus/ein nicht geprüft |
| 120.17 Matchcode | AKE | NICHT PRUEFBAR | kein FA mit Matchcode **und** Stücklisten-Cache auf AKE-DEV |
| Wagen-Konflikt / 130.5 | AKE | OK | Modal „Kommissionierwagen belegt … 2614027; 2616156 … Neuer Fertigungsauftrag: 2607171“; „Trotzdem buchen“: Text „2625004;2607171“ bzw. „2607171;2625004;2629001“, wächst bei weiteren Buchungen nicht |
| 130.4 Druck Spalte Lagerplatz | AKE | OK | umgebuchte Zeilen zeigen den tatsächlichen Quell-Platz (NAN, Negativbuchung erlaubt) |
| 130.3 Bedarfsmeldung aus dem KO-Auftrag, Storno | AKE | OK | KO-2026-00048 → „Bestellen“ → PartRequisition 25, „Bestellt am“ 13.11.2026 20:12 (Ortszeit); storniert „Bedarfsmeldung storniert.“. Hinweis: Dialog „Keine Empfänger konfiguriert“, Anlage trotzdem möglich (N-5) |
| 134.1 Prio nach Rücknahme | AKE | OK | X = 2629001: 43, Rücknahme behält 43, Y = 2625005: 44, X erneut 43; Prüfabfrage 1 nur Altbestand 1, 13, 17, 32 |
| 134.2 Sammelfreigabe Prio | AKE | OK | drei FAs → 48, 49, 50 (eindeutig) |
| 79.10 Freigabe-Kaskade entfernt | IDEAL | OK | `GET /PickingLeitstand/CascadeReleasePreview` → 404 |
| 79.12 Sub-FA direkt (Bom, ToggleRelease) | IDEAL | OK | `/Picking/Bom/275` → `/Picking` mit „Kommissionierung erfolgt nur am HauptFA 1060414.“; `ToggleRelease 275` → dieselbe Warnung, keine Änderung |
| 79.13 BulkRelease gemischt | IDEAL | OK | siehe 120.2 IDEAL |
| 79.2–79.9, 79.20–79.22 Stückliste „Alle Ziele“, Druck-Leerzustand | IDEAL | NICHT PRUEFBAR | in diesem Lauf nicht durchgeführt (Manual-UAT Dropdown) |
| 80.5 Standard-Einstieg Summiert | IDEAL | OK | Menü „Kommissionierlisten“ → `/FaHierarchyKommissionierListen/Summiert`; Liste, Summiert, Print, PrintSummiert je HTTP 200 |
| 80.2–80.4, 80.6–80.20 Einzelschritte | IDEAL | NICHT PRUEFBAR | in diesem Lauf nur Smoke (Seiten/Druck laden) |
| 107.6 Werkbank-Spalte | AKE | OK | Spalte „Werkbank“ nach „Bezeichnung“, Filter `F3` serverseitig (2 Treffer) |
| 124.1 Foto ohne Anmeldung | AKE + IDEAL | OK | `curl` ohne Anmeldung → 302 `/Account/Login?returnUrl=…` |
| 124.2 Pfad mit Punkt | AKE | OK | `/Home/Index.x` → 302 Anmeldung |
| 124.3 Foto mit Recht | AKE | OK | angemeldet: 200 `image/jpeg` |
| 124.4 Foto ohne Recht | AKE | NICHT PRUEFBAR | kein Benutzer ohne `picking` |
| 124.6 Löschen fremdes Foto | AKE | OK | `POST /api/photos/delete` FA 2614027 + Datei `9999999_…` → 400, Datei bleibt |
| 124.5 / 124.7 Hochladen, Migration | AKE | NICHT PRUEFBAR | nicht durchgeführt (Datei-Upload, Deploy-Schritt) |
| Fehlteile (Meine Fehlteile, Lager) | AKE + IDEAL | OK | Smoke: `/MissingParts`, `?tab=glas`, `/MissingPartsLager` je 200 |
| Negativ: kein Standardbereich | AKE | OK | Meldung „Kein aktiver Standardbereich – bitte unter Stammdaten → Kommissionierbereiche einen Standard festlegen.“ (aber Freigabe bleibt gesetzt = B-1) |
| Negativ: Umbuchen/Abschließen auf abgeschlossenem Auftrag | AKE | OK | „Auftrag KO-2026-00034 ist abgeschlossen – Umbuchen nicht möglich.“; doppelte Umbuchung derselben Zeile: „… ist bereits umgebucht oder abgeschlossen.“; zweiter Abschluss: „… ist bereits teilweise abgeschlossen.“ |
| Negativ: Doppelklick „Teilweise“ | AKE | **FEHLER** | zwei gleichzeitige Abschlüsse: 1× OK, 1× HTTP 500 (Deadlock, N-2; Fehlerseite wie im Bild zu 120.10). Kein zweiter Folgeauftrag (K-1 siehe unten) |
| Negativ: Doppelklick „Freigeben“ / veraltete Seite | AKE | **FEHLER** | = K-6 reproduziert: zweiter Klick storniert die gerade angelegten Aufträge. ![[K-6-K-4-doppelfreigabe-storniert-fa-erledigt.jpg]] |
| Negativ: unbekannte Auftrags-Id | AKE | OK | `/Picking/Auftrag/999999`, `/Picking/AuftragDrucken/999999` → 404 |
| Terminal-Sicht (Handy/Terminal) | AKE | NICHT PRUEFBAR | Fensterbreite im Lauf nicht verstellbar; nicht geprüft |

**Zählung:** OK 41 · FEHLER 5 · NICHT PRUEFBAR 11 (Zeilen der Tabelle).

## Nachmessung Review-Befunde

| Befund | Ergebnis | Beleg |
|---|---|---|
| K-1 Doppelter „Teilweise“ → zweiter Folgeauftrag | **nicht reproduziert** (maskiert) | Parallel: 1× 200 (KO-2026-00023), 1× HTTP 500 Deadlock – der Wiederholversuch, in dem K-1 entstehen würde, findet wegen N-2 nicht statt. Mit 150 ms Abstand: „bereits teilweise abgeschlossen“. Wird N-2 behoben, wird K-1 akut → beide zusammen beheben. |
| K-2 Zeilen ohne Stücklisten-Treffer verschwinden | **reproduziert** | KO-2026-00020, Zeile 2641 per SQL auf Pos. `99.99` (zurückgesetzt): Seite 81, Druck 82 Zeilen, kein Hinweis |
| K-3 `ToggleDone` an den Aufträgen vorbei | **reproduziert** | FA 2625005: drei offene Aufträge verschwinden ohne Meldung aus `/Picking` |
| K-4 FA „vollständig“ trotz Resten ohne Auftrag | **reproduziert** | FA 2629001: Glas/VK storniert, Lager „Vollständig“ → FA erledigt; Bild oben (K-6/K-4) |
| K-6 Freigabe-Umschalter storniert | **reproduziert** | FA 2629001: zweiter `ToggleRelease` storniert KO-2026-00025 … -00027 |
| B-1 Fenster ohne Standardbereich | **reproduziert** | FA 2607171 freigegeben ohne Auftrag, Meldung nur beim Freigebenden. ![[B-1-freigegeben-ohne-kommissionierauftrag.jpg]] |

Zusätzlich beobachtet (nicht angefragt): **K-5** reproduziert (108 Zeilen „Entfallen“ ohne Rückfrage, KO-2026-00031), **K-8** reproduziert
(stornierter FA 2622079 bekommt beim Öffnen der Stückliste mit `LeitstandAktiv = false` drei Aufträge), **K-9** reproduziert (Sammelfreigabe
mit anderem Kommissionierer: FA „QA Kommissionierer WE14“, Aufträge KO-2026-00031 … -00033 behalten „QA Kommissionierer2 WE14“),
**B-3** bestätigt im Code (`Regeln.cshtml:59-63`, Löschen ohne Rückfrage; Löschen von Regel 850/838 ohne Nachfrage durchgeführt).
Die Zeilen „DEV-Nachmessung 2026-11-14“ stehen in den beiden Review-Notizen.

## Neue Befunde

Nicht behoben (Schranke 1). Je Befund Vorschlag.

### N-1 – Auftragsseite blendet Unterpositionen ohne Vater-Zeile dauerhaft aus (hoch)
- **Wo:** `IdealAkeWms/Views/Picking/Bom.cshtml:233` (`style="display:none;"` bei `TreeLevel > 0`), `:950-980` (`updateBomVisibility`:
  `treeVisible = !parentPos || expandedState[parentPos] === true`), `:1042-1050` („Alle aufklappen“ setzt nur Baugruppen-Zeilen, die im DOM sind).
- **Befund:** Die Auftragsseite zeigt nur die Zeilen des Auftrags. Liegt die Baugruppe (Vater-Position) nicht im selben Auftrag – AKE:
  Glas-/VK-Aufträge, deren Teile unter Baugruppen des Lager-Auftrags hängen; IDEAL: Positionen unter Sub-FA-Baugruppen –, bleibt die Zeile
  unsichtbar. Kein Aufklappen möglich, Zähler zeigt trotzdem „n Positionen“. IDEAL-DEV KO-2026-00005: 73 von 79 Zeilen unsichtbar; AKE-DEV
  KO-2026-00048 (Glas): 2 von 2, KO-2026-00022 (VK): 8 von 12. Ein Spaltenfilter macht sie ohne `RecursiveFilterSearch` nicht sichtbar.
  Folge: Kommissionierer sieht die Teile nicht, „Vollständig“ setzt sie auf „Entfallen“ (mit K-5).
- **Vorschlag:** Auf der Auftragsseite Zeilen ohne vorhandene Vater-Zeile als oberste Ebene behandeln (Vater-Position nur setzen, wenn die
  Vater-Zeile im Modell ist) oder die Baugruppen-Vater-Zeilen als nicht anhakbare Gliederung mitrendern. Test: Auftrag mit Kind ohne Vater →
  Zeile sichtbar.

### N-2 – Deadlock wird nicht wiederholt, sondern endet als HTTP 500 (mittel)
- **Wo:** `IdealAkeWms/Services/Nummernkreis.cs:15` (`IstDeadlock` prüft `ex` und `ex.InnerException`), `:28` (`catch … when (ex is DbUpdateException || IstDeadlock(ex))`).
- **Befund:** EF Core verpackt den Deadlock als `InvalidOperationException` („… transient failure … EnableRetryOnFailure“) → `DbUpdateException`
  → `SqlException 1205`. Die äußere Ausnahme ist keine `DbUpdateException`, die innere keine `SqlException` → kein Wiederholen, HTTP 500.
  Gemessen: parallele Freigaben (3 von 4 Läufen ein 500er) und paralleler Auftragsabschluss. Bei der Freigabe ist `SetReleaseAsync` schon
  gespeichert → FA freigegeben **ohne** Auftrag, ohne Meldung (wie B-1). Widerspricht „Geprüft ohne Befund – Nummernkreis … Deadlock-Opfer
  wiederholt“ in [[2026-11-14-review-befunde-kommissionierung]]; betrifft auch den BA-Nummernkreis (gemeinsamer Baustein).
- **Vorschlag:** `IstDeadlock` über die ganze `InnerException`-Kette prüfen (z. B. Schleife oder `ex.GetBaseException()`); Test mit
  verschachtelter Ausnahme. Danach K-1 (Statusprüfung im Versuch) zwingend mitbeheben.

### N-3 – Fehler beim Stücklisten-Lesen während der Freigabe → HTTP 500 nach gespeicherter Freigabe (niedrig)
- **Wo:** `PickingLeitstandController.BulkRelease` (`SetReleaseBatchAsync` vor `ErzeugeAsync`), Stücklisten-Lesen `vw_AKE_Kommissionierung_StuecklistenDB`.
- **Befund:** AKE-DEV, Sammelfreigabe FA 2602050/2619077/2623003 (ohne Stücklisten-Cache): „Invalid object name …StuecklistenDB“ → HTTP 500;
  alle drei FAs sind freigegeben (Prio 48–50), kein Auftrag, keine Meldung. Auf DEV Umgebungsursache; in Produktion bei Sage-Ausfall/Timeout gleich.
- **Vorschlag:** Erzeugung je FA in try/catch, Fehler in die Sammelmeldung („FA …: Stückliste nicht lesbar – kein Auftrag, später
  „Fehlende Kommissionieraufträge erzeugen““) statt 500.

### N-4 – Freigabe erledigter FAs (`IsDonePicking = 1`) möglich (Hinweis)
- **Befund:** FA 2602050, 2619077, 2623003 hatten schon `IsDonePicking = 1` (Altdaten) und wurden per Sammelfreigabe freigegeben, bekamen Prio
  48–50. Ergänzt K-8 („`ToggleRelease` prüft `IsDone/IsDonePicking` ebenfalls nicht“) um `BulkRelease`.
- **Vorschlag:** mit K-8 zusammen lösen.

### N-5 – Bedarfsmeldung ohne Empfänger anlegbar (Hinweis)
- **Befund:** Artikelgruppe 850 hat keine Empfängergruppe; der Dialog zeigt „Keine Empfänger konfiguriert.“, „Bestellen“ legt die Meldung
  trotzdem an (PartRequisition 25, `OrderRecipientGroupId` NULL). Ob der Dienst sie je verschickt, nicht geprüft (Mail-Schalter aus).
- **Vorschlag:** Klären, ob gewollt; sonst Hinweis „wird niemandem gemeldet“ oder Sperre.

### N-6 – Altlast: offener Auftrag an nicht freigegebenem FA (Hinweis)
- **Befund:** Vor dem Test stand KO-2026-00014 (FA 2607170) auf „Offen“, FA aber nicht freigegeben (`IsReleasedForPicking = 0`). Erscheint in
  `/Picking`. Entstehung unklar (früherer Test). Prüfabfrage:
  `SELECT o.Nummer FROM PickingOrders o JOIN ProductionOrderPickingStatus ps ON ps.ProductionOrderId=o.ProductionOrderId WHERE o.Status IN (10,20) AND ps.IsReleasedForPicking=0`
  (AKE-DEV: KO-2026-00008 – gewollt, Rücknahme mit Fortschritt – und KO-2026-00014).

### Beobachtung ohne Fehler
- IDEAL-DEV: Druck „Auftrag drucken“ hat leere Spalte „Bezeichnung“, weil die DEV-Stückliste keine Bezeichnung 1 trägt (Matchcode ist gefüllt).
- Der persönliche Standardfilter „Bezeichnung 1 = Verdampfer“ (Benutzer gerald.weichbold) gilt auch auf der Auftragsseite – Chip sichtbar, Zähler
  „3 Positionen“ bei leerer Tabelle. Kein Fehler, für die Anwender-Doku als Stolperfalle notieren.

## Testdaten-Spur

| Was | Wo | Id / Nummer | Zurückgesetzt? |
|---|---|---|---|
| Test-Benutzer „QA Kommissionierer WE14“, „QA Kommissionierer2 WE14“ (IsPicker, ohne Windows-Login) | AKE_WMS_DEV `Users` | 23, 24 | deaktiviert (`IsActive = 0`, `IsPicker = 0`); Zeilen bleiben (Namen stehen an Aufträgen) |
| Bereich „QA Testbereich WE14“ | AKE `PickingAreas` | 4 | ja, per SQL gelöscht (keine Verweise) |
| Regeln Artikelgruppe 850 → Glas, 838 → VK | AKE `PickingAreaRules` | 3, 4 | ja, über „Löschen“ |
| Standardbereich Lager abgewählt/gewechselt | AKE `PickingAreas` 1/2 | – | ja, Lager wieder Standard (Audit-Felder geändert) |
| `LeitstandAktiv` kurz `false` | AKE `AppSettings` | – | ja, `true` |
| Kommissionieraufträge KO-2026-00020 … -00052 | AKE `PickingOrders` | Id 53–88 | nicht löschbar; offene storniert (Leitstand-Rücknahme bzw. 86–88 per SQL Status 90, `ModifiedBy = 'QA Wochenend-Regression'`); abgeschlossene bleiben |
| Umbuchungen auf WAGEN N3 / Wagen 2 (Quelle NAN, negativ) | AKE `StockMovements` | 19432–19532 (101) | nein – NAN-Bestand negativ, Wagen tragen FA-Texte „…;2607171;2629001“; keine Sage-Queue-Einträge |
| Freigaben/Prio | AKE `ProductionOrderPickingStatus` FA 144, 2802, 3043, 3045, 3047, 742, 2284, 9 | – | Freigaben zurückgenommen; Prio bleibt gesetzt (Regel TS-134): 144 = 42, 2802 = 47, 3043 = 44, 3047 = 34 (vorher), 3045 = 41 (vorher), 9 = 48, 742 = 49, 2284 = 50 |
| `IsDonePicking` durch Abschluss | AKE FA 144 (2607171), 2802 (2629001) | – | nein, bleibt 1 (Aufträge abgeschlossen); FA 3043 per „erledigt“ an und wieder aus |
| Bedarfsmeldung | AKE `PartRequisitions` | 25 | storniert |
| Freigabe FA 1060414 + 1043153 | IDEAL `ProductionOrderPickingStatus` 284, 27 | – | zurückgenommen; Prio bleibt 7 bzw. 8 |
| Kommissionierauftrag | IDEAL `PickingOrders` | KO-2026-00005 (Id 5) | storniert |
| Regressionsauftrag KO-2026-00008 | AKE | Id 41 | nicht angefasst (In Arbeit) |

Nicht angefasst: Benutzer-/Rollenzuordnungen von gerald.weichbold, Sync-/Einkaufs-Einstellungen, Sage.

## Screenshot-Kandidaten für die Anwender-Doku

Personendaten beachten: Spalte „Kommissionierer“ und „Gedruckt von“ zeigen echte Namen → mit Test-Kommissionierer aufnehmen oder Spalte
ausblenden. Kunde „!!!Test - AKE - Test!!!“ (FA 2627001) ist unkritisch.

| URL | Zustand | Zweck |
|---|---|---|
| `http://localhost:8081/PickingLeitstand?filterOrderNumber=2607171` (nach rechts scrollen) | FA freigegeben, Prio, Kommissionierer, Auftragsliste in „Status“ | Leitstand: Freigabe-Spalte, KO-Links mit Bereich-Kürzel |
| Leitstand → „Freigeben“ an einem nicht freigegebenen FA | Dialog „Kommissionier-Freigabe“ (mit/ohne Kommissionierer-Hinweis) | Freigabe-Schritt |
| `http://localhost:8081/Picking?showAll=true` | Liste mit Bereich, Fällig, Status | Kommissionierung-Einstieg, Filterkarte |
| `http://localhost:8081/Picking/Auftrag/<offener Lager-Auftrag>` | Zeilen markiert, Ist-Mengen, Wagen gewählt | Umbuchen auf Wagen |
| dieselbe Seite nach „Gepickte Artikel umbuchen“ | Modal „Kommissionierwagen belegt“ | Wagen-Konflikt / „Trotzdem buchen“ |
| dieselbe Seite | Modal „Kommissionierung Status“ (Teilweise / Vollständig / Weiter kommissionieren) | Abschluss |
| `http://localhost:8081/Picking/AuftragDrucken/53` | KO-2026-00020 mit Barcode | Druck |
| `http://localhost:8081/PickingAreas` | 3 Bereiche, Standard-Badge, Regeln-Zähler | Stammdaten Bereiche |
| `http://localhost:8081/PickingAreas/Regeln?bereichId=2` | Regelliste Glas + Formular „Regel anlegen“ | Zuordnungsregeln |
| `http://localhost:8082/FaHierarchyKommissionierListen/Summiert` | IDEAL Kommissionierliste summiert | IDEAL-Kommissionierliste |
| `http://localhost:8081/PartRequisitions` | Liste Bedarfsmeldungen | Bedarfsmeldung |

Hinweis: Auftragsseiten mit Unterpositionen (N-1) erst nach dem Fix fotografieren; bis dahin einen AKE-Lager-Auftrag nehmen.
