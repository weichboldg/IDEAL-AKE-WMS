---
type: backlog
created: 2026-11-14
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
status: neu
modul: Kommissionierung (Kommissionieraufträge KO, Leitstand, Wagen, Druck, Altbestand)
stand: "Bündel v1.99.22, 90bbbbd7, Diff main...HEAD (Merge-Base 1753c79e)"
---
# Review-Befunde Kommissionierung (Teil A1, lesend)

Lesendes Code-Review des Bündel-Worktrees `.claude/worktrees/2026-08-07-ideal-teile-1-5` gegen `main`, Schwerpunkt
Kommissionieraufträge ab v1.88.0. Grundlage: [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]] (inkl. QA Q1–Q16,
Entscheidungen Q9–Q11), [[2026-11-13-doppelte-kommissionier-prio-nach-ruecknahme-spec]], [[fallstricke]]. Befunde aus den
früheren QA-Läufen (Q1–Q16) werden nicht wiederholt. Pfade sind Worktree-relativ. **Nichts behoben** (Schranke 1).

Bereichs-/Typ-Befunde stehen in [[2026-11-14-review-befunde-kommissionierbereiche]].

## Übersicht

| ID | Schwere | Kurz |
|---|---|---|
| K-1 | hoch | Doppelter Abschluss „Teilweise“ legt einen zweiten Folgeauftrag an (Status wird im Wiederholversuch nicht neu geprüft) |
| K-2 | mittel | Zeilen des Auftrags, die in der aktuellen Stückliste fehlen, verschwinden still von der Auftragsseite |
| K-3 | mittel | Knopf „erledigt“ (`ToggleDone`) setzt `IsDonePicking` an den Aufträgen vorbei – offene Aufträge verschwinden still aus `/Picking` |
| K-4 | mittel | FA wird „vollständig kommissioniert“, obwohl Stücklisten-Reste ohne Auftrag bestehen (u. a. „Altbestand prüfen“ wird umgangen) |
| K-5 | mittel | „Vollständig abgeschlossen“ ohne jede Umbuchung: alle Zeilen „Entfallen“, FA erledigt – ohne Rückfrage, ohne Rückweg |
| K-6 | mittel | Freigabe-Knopf ist ein Umschalter: veraltete Leitstand-Seite storniert per Klick die Aufträge eines schon freigegebenen FA |
| K-7 | niedrig | Nebenläufige Änderung (RowVersion) endet als HTTP 500 statt Meldung (Umbuchen, Rücknahme, Prio/Kommissionierer) |
| K-8 | niedrig | Öffnen der Stückliste ohne Leitstand erzeugt Aufträge auch für erledigte/stornierte FAs (GET mit Schreibwirkung) |
| K-9 | niedrig | Sammelfreigabe mit Kommissionierer schreibt den neuen Kommissionierer nicht auf bestehende Aufträge durch |
| K-10 | niedrig | Leitstand verlinkt Aufträge, die Rolle `leitstand` ohne `picking` nicht öffnen darf |
| K-11 | niedrig | Auftragsliste lädt bei „Status: alle“ die gesamte Auftragstabelle in den Speicher |
| K-12 | niedrig | Testlücken für die riskanten Pfade (Controller Nachziehen/Abschluss/Altbestand geprüft, Abschluss auf abgeschlossenem Auftrag) |
| K-13 | Hinweis | Altbestand-Skript nimmt Sub-FAs mit (kein `IsHauptFa`-Filter) – Prüfabfrage |
| K-14 | Hinweis | Wagen „belegt“: Konfliktprüfung und Buchung nicht atomar, Bestandsprüfung ohne Sperre (vorbestehend) |
| K-15 | Hinweis | Doppelte Stücklistenschlüssel: zwei Zeilen zeigen auf dieselbe Auftragszeile, Auswahl beider bricht mit Fehlermeldung ab |

---

## K-1 – Doppelter Abschluss „Teilweise“ erzeugt zweiten Folgeauftrag
- **Schwere:** hoch
- **Fundstelle:** `IdealAkeWms/Services/PickingOrderWorkflow.cs:162-168` (Statusprüfung nur **vor** der Transaktion),
  `:170-174` (Neulesen im Versuch ohne erneute Statusprüfung), `:180-218`; `IdealAkeWms/Views/Picking/Bom.cshtml:1668-1686`
  (Knöpfe `.set-status-btn` werden beim Klick nicht gesperrt).
- **Befund:** `AbschliessenAsync` prüft `Status is Offen or InArbeit` einmal außerhalb von `Nummernkreis.MitVersuchenAsync`. Im Versuch
  wird der Auftrag neu gelesen (`:174`), der Status aber nicht erneut geprüft. Zwei fast gleichzeitige Anfragen (Doppeltipp am
  Terminal, zweiter Kommissionierer) bestehen beide die Vorprüfung. Die zweite scheitert zunächst am `RowVersion`
  (`DbUpdateConcurrencyException` ist eine `DbUpdateException` → wird von `MitVersuchenAsync` gefangen, `ChangeTracker.Clear()`),
  der Wiederholversuch liest dann den schon abgeschlossenen Auftrag frisch und läuft ohne Prüfung durch: übertragene Zeilen werden
  übersprungen, umgebuchte Zeilen mit Ist &lt; Soll liefern erneut einen Rest → **zweiter Folgeauftrag** mit derselben Differenz.
  Ohne Ist-Abweichung kippt der Kopfstatus von `TeilweiseAbgeschlossen` auf `Abgeschlossen`; `AbgeschlossenAm/-Von` werden überschrieben.
- **Szenario:** Zeile Soll 10 / Ist 4 umgebucht, „Teilweise“ doppelt getippt → KO-…-A (Rest 6) und KO-…-B (Rest 6). Das Lager
  kommissioniert 12 statt 6. Bei Mischung „Teilweise“ + „Vollständig“ (zwei Personen) entsteht ein widersprüchlicher Kopfstatus.
- **Vorschlag:** Statusprüfung **innerhalb** des Versuchs nach dem Neulesen wiederholen (bei Abschluss → Meldung „bereits …“, kein
  Wiederholen). Zusätzlich die Knöpfe beim Klick sperren (wie `submitBtn.disabled` beim Umbuchen, `Bom.cshtml:1438`). Unit-Test:
  `AbschliessenAsync` zweimal hintereinander auf denselben Auftrag → zweiter Aufruf Fehler, genau ein Folgeauftrag.
- **DEV-Nachmessung 2026-11-14:** nicht reproduziert (zweiter Folgeauftrag entstand nicht), aber nur durch einen anderen Fehler maskiert. AKE-DEV, KO-2026-00021 (Zeile EK019437 Soll 2/Ist 1 umgebucht): zwei gleichzeitige `POST /Picking/AuftragAbschliessen vollstaendig=false` → 1× 200 (Folgeauftrag KO-2026-00023), 1× **HTTP 500** (Deadlock-Opfer, Log `idealakewms-20261113_004.log` 19:58:28, `PickingOrderWorkflow.cs:222`). Die Wiederholung in `Nummernkreis.MitVersuchenAsync` greift nicht, weil EF den Deadlock als `InvalidOperationException` → `DbUpdateException` → `SqlException 1205` verpackt und `IstDeadlock` nur eine Ebene tief prüft (`Nummernkreis.cs:15`). Wird das behoben, ist der K-1-Pfad (Wiederholversuch ohne Statusprüfung) offen. Zweiter Versuch mit 150 ms Abstand (KO-2026-00023): Meldung „bereits teilweise abgeschlossen“, genau ein Folgeauftrag KO-2026-00024. Knöpfe werden beim Klick nicht gesperrt (Code `Bom.cshtml:1668-1686`). Bericht [[01-kommissionierung]].

## K-2 – Auftragszeilen ohne Treffer in der aktuellen Stückliste verschwinden still
- **Schwere:** mittel
- **Fundstelle:** `IdealAkeWms/Controllers/PickingController.cs:291-293` (`bomItems` wird auf Schlüssel der Auftragszeilen gefiltert,
  die Anzeige geht von der **Stückliste** aus, nicht von den Zeilen).
- **Befund:** Die Auftragsseite zeigt nur Stücklistenpositionen, deren Schlüssel (Ressourcenummer + Position,
  `PickingOrderRestmengen.Schluessel`) in den Auftragszeilen vorkommt. Eine Auftragszeile, deren Schlüssel in der aktuellen
  Stückliste fehlt (Stückliste in Sage geändert, IDEAL-Pfad durch Kollisions-Suffix `~n` verschoben
  `FaHierarchyBomRepository.cs:154-160`, Altbestand-Positionen aus `PickingItems`, leere/fehlende Stückliste), wird weder angezeigt
  noch gemeldet. Der Druck (`RenderAuftragDruckenAsync`, geht von `auftrag.Zeilen` aus) zeigt sie dagegen.
- **Szenario:** Druck nennt 12 Zeilen, Auftragsseite zeigt 11. Der Kommissionierer schließt „Vollständig“ ab → die unsichtbare Zeile
  wird `Entfallen`, ohne dass jemand sie gesehen hat.
- **Vorschlag:** Anzeige von den Auftragszeilen aus aufbauen (Stückliste nur zur Anreicherung) oder mindestens Hinweis-Banner
  „n Zeilen dieses Auftrags fehlen in der aktuellen Stückliste: …“ (Melden statt still behandeln).
- **DEV-Nachmessung 2026-11-14:** reproduziert. AKE-DEV KO-2026-00020 (82 Zeilen), Zeile 2641 per SQL auf Position `99.99` gesetzt (simuliert geänderte Sage-Stückliste, danach zurückgesetzt): Auftragsseite „81 Positionen“, Zeile 2641 fehlt, kein Hinweis; `AuftragDrucken/53` zeigt 82 Zeilen inkl. `99.99`. Bericht [[01-kommissionierung]].

## K-3 – `ToggleDone` setzt `IsDonePicking` an den Kommissionieraufträgen vorbei
- **Schwere:** mittel
- **Fundstelle:** `IdealAkeWms/Controllers/PickingController.cs:194-214`; Knöpfe `IdealAkeWms/Views/PickingLeitstand/_PickingLeitstandRow.cshtml:210-230`,
  `IdealAkeWms/Views/ProductionOrders/_ProductionOrderRow.cshtml:130`; Listenfilter `IdealAkeWms/Data/Repositories/PickingOrderRepository.cs:156-159`.
- **Befund:** Der „erledigt“-Knopf (Rolle `picking`) schaltet `IsDonePicking` direkt um. Offene/in Arbeit befindliche Aufträge des FA
  bleiben `Offen`/`InArbeit`, fallen aber aus der Standardliste `/Picking` heraus, weil `GetListeAsync(nurOffene)` FAs mit
  `IsDonePicking` ausschließt. Keine Meldung. Umgekehrt öffnet „Als offen markieren“ nach einem Auftragsabschluss den FA wieder,
  ohne dass ein Auftrag existiert. Die Spec nennt die weiteren Schreiber bewusst („gespeichert, nicht berechnet“), aber nicht die
  Folge für die Auftragsliste.
- **Szenario:** Kommissionierer hakt den FA im Leitstand „erledigt“, während KO-…-Glas noch offen ist → der Glas-Auftrag ist in
  `/Picking` unsichtbar, steht im Leitstand aber weiter als „Offen“.
- **Vorschlag:** Beim Setzen auf erledigt offene Aufträge des FA nennen (Warnung) oder den Knopf sperren, solange Aufträge offen
  sind; Entscheidung Gerald. Mindestens Warnung im TempData.
- **DEV-Nachmessung 2026-11-14:** reproduziert. AKE-DEV FA 2625005 (Id 3043) mit drei offenen Aufträgen KO-2026-00028/-29/-30: `POST /Picking/ToggleDone` → keine Meldung, `IsDonePicking = 1`; `/Picking?showAll=true` zeigt die drei Aufträge vorher, danach keinen; Aufträge bleiben `Offen`. (Danach zurückgeschaltet.) Bericht [[01-kommissionierung]].

## K-4 – FA „vollständig“, obwohl Reste ohne Auftrag bestehen
- **Schwere:** mittel
- **Fundstelle:** `IdealAkeWms/Services/PickingOrderWorkflow.cs:234-242`.
- **Befund:** Nach dem Abschluss gilt der FA als fertig, sobald kein Auftrag mehr `Offen`/`InArbeit` ist. Ob die Stückliste noch
  Restmengen ohne Auftrag hat, wird nicht geprüft. Solche Reste entstehen u. a. (a) bei FAs mit `AltbestandPruefen` (Q11: Erzeugung
  übersprungen, bis der Leitstand bestätigt), (b) bei Freigabe im Zeitfenster ohne aktiven Standardbereich (siehe
  [[2026-11-14-review-befunde-kommissionierbereiche]] B-1), (c) bei neuen Stücklistenpositionen vor dem Nachziehen.
  Danach setzt `SetIsDonePickingAsync(true)`; `AuftraegeNachziehen` überspringt den FA (`GetReleasedForPickingAsync` filtert
  `IsDonePicking`). 
- **Szenario (a):** Altbestand-Auftrag (In Arbeit, Menge 3, Häkchen „Altbestand prüfen“) wird umgebucht und „Vollständig“
  abgeschlossen, bevor der Leitstand prüft → FA erledigt, die Prüfung aus Entscheidung Q11 läuft ins Leere, der Rest (B1) geht still verloren.
- **Vorschlag:** Vor dem Setzen von `IsDonePicking` die Restmengen rechnen (`PickingOrderRestmengen.Berechne` gegen die Stückliste) bzw.
  `AltbestandPruefen` prüfen; bei Rest nur „teilkommissioniert“ + Warnung „FA hat noch Positionen ohne Auftrag“.
- **DEV-Nachmessung 2026-11-14:** reproduziert (Variante Rücknahme mit Fortschritt). AKE-DEV FA 2629001 (Id 2802): drei Aufträge, eine Zeile von KO-2026-00031 (Lager) umgebucht, Sammel-Rücknahme → KO-2026-00032 (Glas, 2 Zeilen) und -00033 (VK, 9 Zeilen) storniert, -00031 bleibt; -00031 „Vollständig“ → Antwort `faAbgeschlossen: true`, FA `IsDonePicking = 1`, „abgeschlossen“, obwohl 11 Positionen nie kommissioniert wurden und 108 Zeilen „Entfallen“ (zugleich K-5). Bericht [[01-kommissionierung]].

## K-5 – „Vollständig abgeschlossen“ ohne Umbuchung, ohne Rückfrage
- **Schwere:** mittel
- **Fundstelle:** `IdealAkeWms/Views/Picking/Bom.cshtml:469-472` (Knopf „Auftrag abschließen …“ jederzeit, QA Q3), `:561-566`,
  `IdealAkeWms/Services/PickingOrderWorkflow.cs:185-196`, Meldung `IdealAkeWms/Controllers/PickingController.cs:481`.
- **Befund:** Ein Auftrag ohne eine einzige umgebuchte Zeile lässt sich mit einem Tipp „Vollständig“ abschließen: alle Zeilen
  `Entfallen`, Kopf `Abgeschlossen`, ggf. FA `IsDonePicking`. `Entfallen` zählt in `PickingOrderRestmengen.Abgedeckt` als gedeckt –
  Nachziehen legt nichts neu an. Es gibt keinen Weg zurück (kein Wiedereröffnen) und die Erfolgsmeldung nennt nicht, wie viele Zeilen
  entfallen sind.
- **Vorschlag:** Bei `vollstaendig` und offenen Zeilen Rückfrage mit Anzahl („n Zeilen werden nicht kommissioniert“) und Nennung in der
  Erfolgsmeldung; Wiedereröffnen bzw. Storno durch den Leitstand als Folgeaufgabe prüfen.

## K-6 – Freigabe-Knopf als Umschalter: veraltete Seite storniert Aufträge
- **Schwere:** mittel
- **Fundstelle:** `IdealAkeWms/Controllers/PickingLeitstandController.cs:395` (`newReleased = !ps.IsReleasedForPicking`), `:417-420`.
- **Befund:** `ToggleRelease` kehrt den **gespeicherten** Zustand um, nicht den angezeigten. Ein Leitstand-Benutzer mit veralteter
  Seite (FA dort „nicht freigegeben“, inzwischen von Kollege oder Sammelfreigabe freigegeben) nimmt mit „Freigeben“ die Freigabe
  zurück; seit v1.88.0 werden dabei alle Aufträge ohne Fortschritt **storniert** und der Kommissionierer am FA gelöscht. Dasselbe bei
  Doppelklick. Vor v1.88.0 war der Umschalter harmlos, jetzt hat er Storno-Wirkung.
- **Vorschlag:** Gewünschten Zielzustand mitsenden (`release=true/false`) und bei Abweichung melden statt umschalten (wie `BulkRelease`).
- **DEV-Nachmessung 2026-11-14:** reproduziert. AKE-DEV FA 2629001 (Id 2802), zweimal `POST /PickingLeitstand/ToggleRelease` (wie Doppelklick bzw. veraltete Seite, jeweils mit Kommissionierer): 1. „3 … angelegt (KO-2026-00025, -00026, -00027)“, 2. „3 … storniert (KO-2026-00025, -00026, -00027)“, FA nicht mehr freigegeben, Kommissionierer gelöscht. Bericht [[01-kommissionierung]].

## K-7 – Nebenläufige Änderung endet als HTTP 500
- **Schwere:** niedrig
- **Fundstelle:** `IdealAkeWms/Controllers/PickingController.cs:467` (fängt nur `InvalidOperationException`),
  `IdealAkeWms/Services/PickingTransferService.cs:256-262` (wirft weiter), `IdealAkeWms/Services/PickingOrderWorkflow.cs:250-306`
  (`FreigabeZuruecknehmenAsync`/`DurchschreibenAsync` ohne Fang), Aufrufer `PickingLeitstandController.cs:418-420`, `:473`, `:584`, `:604`.
- **Befund:** `PickingOrders`/`PickingOrderLines` haben `RowVersion` (`ApplicationDbContext.cs`, `IsRowVersion`). Bucht ein zweiter
  Kommissionierer dieselben Zeilen oder ändert der Leitstand den Auftrag gleichzeitig, wirft `SaveChanges` eine
  `DbUpdateConcurrencyException`. Die Daten bleiben korrekt (Rollback), der Benutzer bekommt aber einen 500er bzw. das AJAX-`alert`
  mit Fehlerseiten-HTML. In `ToggleRelease` ist die Freigabe-Rücknahme dann schon gespeichert (`SetReleaseAsync` vorher), die
  Storno-Meldung fehlt.
- **Vorschlag:** `DbUpdateConcurrencyException` an diesen Stellen fangen → „Auftrag wurde zwischenzeitlich geändert – Seite neu laden.“

## K-8 – Öffnen der Stückliste (ohne Leitstand) erzeugt Aufträge auch für erledigte FAs
- **Schwere:** niedrig
- **Fundstelle:** `IdealAkeWms/Controllers/PickingController.cs:239-247` (`GET Bom` ruft `ErzeugeOffeneAsync`),
  `IdealAkeWms/Services/PickingOrderWorkflow.cs:56-69` (prüft nur `IsSubFa` und Altbestand).
- **Befund:** QA Q4 hat die Serverprüfung (freigegeben, nicht erledigt/storniert) nur in `AuftraegeNachziehen` ergänzt. Der Weg ohne
  Leitstand (`LeitstandAktiv = false`, Auswahlliste über alle FAs `_Select2ProductionOrderPartial`) legt beim bloßen Ansehen der
  Stückliste eines erledigten oder stornierten FA Aufträge an – per GET (auch durch Vorschau/Link-Prefetch auslösbar).
  `ToggleRelease` prüft `IsDone/IsDonePicking` ebenfalls nicht.
- **Vorschlag:** Prüfung `!IsDone && !IsCancelled && !IsDonePicking` in `ErzeugeOffeneAsync` selbst (einziger Erzeugungsweg) mit Meldung.
  Prüfabfrage P4 der Spec klärt, ob ein Produktivstandort ohne Leitstand läuft.

## K-9 – Sammelfreigabe schreibt den Kommissionierer nicht auf bestehende Aufträge
- **Schwere:** niedrig
- **Fundstelle:** `IdealAkeWms/Data/Repositories/ProductionOrderPickingStatusRepository.cs:268-280` (bereits freigegebene FAs werden
  erneut verarbeitet, `AssignedPicker` überschrieben), `IdealAkeWms/Controllers/PickingLeitstandController.cs:457-489` (kein
  `KommissioniererDurchschreibenAsync`).
- **Befund:** Markiert der Leitstand einen schon freigegebenen FA erneut und gibt mit anderem Kommissionierer frei, steht am FA der neue,
  an den offenen Aufträgen der alte Kommissionierer. „Nur meine“ in `/Picking` filtert nach dem Auftrag → der neue Kommissionierer sieht
  nichts. `ChangeAssignedPicker` schreibt dagegen durch (Antwort 6).
- **Vorschlag:** In `BulkRelease` für `ProcessedIds` mit `assignedPickerId` `KommissioniererDurchschreibenAsync` aufrufen.

## K-10 – Auftrags-Links im Leitstand für Rolle `leitstand` gesperrt
- **Schwere:** niedrig
- **Fundstelle:** `IdealAkeWms/Views/PickingLeitstand/_PickingLeitstandRow.cshtml:179-184` (Link immer gerendert);
  `IdealAkeWms/Controllers/PickingController.cs:252` (`Auftrag` nur `[RequirePickingAccess]`), `CurrentUserService.CanPickAsync` = Admin/Picking.
- **Befund:** Ein Benutzer mit `leitstand` ohne `picking` sieht die KO-Nummern als Links, landet beim Klick aber auf „Zugriff verweigert“
  (auch Druck). Read/Edit-Split: Lesen der Auftragsseite wäre für den Leitstand fachlich sinnvoll, Umbuchen nicht.
- **Vorschlag:** Entweder Link nur bei `CanPick` rendern, oder `Auftrag`/`AuftragDrucken` auf `RequirePickingOrLeitstandAccess` mit
  `ReadOnly` für Nicht-Kommissionierer. Bei neuem Filter drei Stellen (Attribut, [[controller]], `RoleOverview.cshtml`).

## K-11 – Auftragsliste „alle“ lädt die ganze Tabelle
- **Schwere:** niedrig
- **Fundstelle:** `IdealAkeWms/Data/Repositories/PickingOrderRepository.cs:149-161`, `IdealAkeWms/Controllers/PickingController.cs:136-188`.
- **Befund:** Filter, Spaltenfilter und Paging laufen in C# über **alle** Aufträge (mit FA, Werkbank, PickingStatus). Bei
  „Status: alle“ wächst das mit jedem Auftrag (Folgeaufträge, Altbestand) unbegrenzt; Bereich-Filter ebenfalls erst im Speicher.
  Für die Fälligkeit ist C# korrekt (ADR 0005: Datum nach Terminberechnung), die übrigen Filter nicht.
- **Vorschlag:** Bereich, Status und SQL-fähige Spaltenfilter vor `ToListAsync`; nur Fälligkeit in C#. Bis dahin beobachten.

## K-12 – Testlücken riskanter Pfade
- **Schwere:** niedrig
- **Fundstelle:** `IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs`, `IdealAkeWms.Tests/Services/PickingOrderWorkflowTests.cs`.
- **Befund (gegrept):** Keine Tests für `AuftraegeNachziehen` (Server-Guard Q4: nicht freigegeben/Sub-FA übersprungen),
  `AuftragAbschliessen` (Controller), `AltbestandGeprueft` (Controller), `AbschliessenAsync` auf bereits abgeschlossenem Auftrag (K-1),
  `FreigabeZuruecknehmen` über `BulkRelease`, Druck-Fälligkeit im Bereich Vorkommissionierung (B-2 in [[2026-11-14-review-befunde-kommissionierbereiche]]). Nebenläufigkeit (Nummernkreis,
  RowVersion) ist InMemory nicht prüfbar → nur Manual-UAT (TS-120.10, bisher „NICHT MÖGLICH“).
- **Vorschlag:** Je ein Test für Q4-Guard, Abschluss-Wiederholung (K-1) und Nachziehen-Übersprung; TS-120.10 auf dem Testsystem nachholen.

## K-13 – Altbestand-Skript ohne HauptFA-Filter
- **Schwere:** Hinweis
- **Fundstelle:** `SQL/Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand.sql:67-78`.
- **Befund:** `#Fa` nimmt jeden FA mit `PickingItems`-Fortschritt, auch Sub-FAs (IDEAL vor v1.46.0 konnte an Sub-FAs kommissionieren).
  Für solche FAs entstünden Aufträge mit `ProductionOrderId` = Sub-FA, die der neue Code sonst nie anlegt (`ErzeugeOffeneAsync:61`).
  Auf IDEAL-DEV gab es 0 FAs mit Fortschritt (E0 P5) – Produktion ungeprüft.
- **Prüfabfrage (Mensch, je Standort):**
  ```sql
  SELECT COUNT(DISTINCT p.ProductionOrderId) AS SubFaMitFortschritt
  FROM dbo.PickingItems p JOIN dbo.ProductionOrders po ON po.Id = p.ProductionOrderId
  WHERE (p.IsPicked = 1 OR p.IsTransferred = 1)
    AND NOT (po.SubOrderNumber = '' OR po.SubOrderNumber = po.OrderNumber);
  ```
- **Vorschlag:** Bei Treffer > 0 im Skript ausschließen oder auf den HauptFA umhängen und im Trockenlauf melden.

## K-14 – Wagen „belegt“: Prüfung und Buchung nicht atomar
- **Schwere:** Hinweis (vorbestehend, nicht durch das Bündel eingeführt)
- **Fundstelle:** `IdealAkeWms/Services/PickingTransferService.cs:80-104` (Konfliktprüfung vor der Transaktion), `:128-207` (Bestand ohne Sperre),
  `IdealAkeWms/Data/Repositories/StockMovementRepository.cs:510-538`.
- **Befund:** Zwei Kommissionierer, die gleichzeitig verschiedene FAs auf denselben leeren Wagen buchen, bestehen beide die Prüfung –
  der Wagen trägt zwei FAs ohne „Trotzdem buchen“. Ebenso kann gleichzeitige Entnahme vom selben Quellplatz den Bestand negativ machen.
  Zusätzlich zählt „belegt“ historische FA-Texte aller Umbuchungen eines Artikels, der **heute** Bestand am Wagen hat (auch wenn die
  frühere Menge wieder abgebucht wurde) → mögliche Schein-Konflikte.
- **Vorschlag:** Beobachten; bei Bedarf Prüfung in die Transaktion (Serializable/UPDLOCK auf Wagen-Platz).

## K-15 – Doppelte Stücklistenschlüssel
- **Schwere:** Hinweis
- **Fundstelle:** `IdealAkeWms/Services/PickingOrderRestmengen.cs:35-38` (Duplikate zu einer Zeile summiert),
  `IdealAkeWms/Controllers/PickingController.cs:288-290`, `:322-326`, `IdealAkeWms/Services/PickingTransferService.cs:55-56`.
- **Befund:** Kommt (Ressourcenummer, Position) in einer flachen Stückliste doppelt vor, legt die Erzeugung **eine** Zeile mit der
  Summe an; die Auftragsseite zeigt aber beide Stücklistenzeilen, beide mit derselben `PickingItemId` und der Summenmenge. Wer beide
  hakt, bekommt „Eine Zeile ist mehrfach ausgewählt – bitte Seite neu laden“ – Neuladen hilft nicht. Menge erscheint doppelt.
- **Vorschlag:** Auftragsseite je Auftragszeile einmal rendern (hängt mit K-2 zusammen); Häufigkeit per Abfrage auf `BomCache` prüfen.

---

## Geprüft ohne Befund
- Nummernkreis `KO-JJJJ-NNNNN`: Vergabe in Serializable-Transaktion, UNIQUE (`Nummer`, `Jahr`+`LaufendeNummer`), Wiederholung mit
  `ChangeTracker.Clear` – parallele Freigaben ergeben keine Doppelnummer (Deadlock-Opfer wiederholt) (`Nummernkreis.cs`, `PickingOrderRepository.cs:171-178`).
- Idempotenz der Erzeugung: Restmengen innerhalb der Transaktion gelesen; Stornierte zählen nicht; „Vollständig“ deckt Ist &lt; Soll (`PickingOrderRestmengen.cs`).
- Umbuchung: Auswahl vollständig vor der ersten Buchung geprüft (Zugehörigkeit, Status, Ist &gt; 0 und ≤ Soll, leer, doppelt), eine
  Transaktion, `RowVersion` auf Zeilen verhindert Doppelbuchung derselben Zeile; Bewegung direkt über den Context → nie Sage-Queue
  ([[fallstricke]]), `ProductionOrderId` + Text gesetzt.
- Audit-Felder (ADR 0003): gesetzt in Erzeugung, Abschluss (Kopf + geänderte Zeilen + Folgeauftrag), Umbuchung, Rücknahme,
  Durchschreiben, „Altbestand geprüft“.
- Lokalzeit (ADR 0025): kein `UtcNow` in Workflow, Transfer, Picking-Controllern, `ProductionOrderPickingStatusRepository`.
- Zugriffsschutz: alle POSTs mit `[ValidateAntiForgeryToken]`; Leitstand-Aktionen `[RequireLeitstandAccess]`, Klasse
  `[RequirePickingOrLeitstandAccess]`; Abarbeiten `[RequirePickingAccess]`; `returnUrl` überall mit `Url.IsLocalUrl`.
- Prio v1.99.17: `GetMaxPickingPriorityAsync` ohne `IsReleasedForPicking`, nur HauptFA, offen; Race bewusst offen (Spec „Grenzen“).
- Listen-Muster `/Picking`: Pagination, Filterkarte, Server-Spaltenfilter, alle `<th>` mit `data-col-key`, Fälligkeit nach Terminberechnung.
- Auftragsdruck v1.97.0/v1.99.13: Reihenfolge stabil, Quell-Vorschlag dieselbe Regel wie die Auftragsseite (`QuellVorschlag`), Matchcode v1.99.8.
- Altbestand-Skript: Wiederholungssperre (`CreatedBy`), Trockenlauf-Default, Guards SQL/133 + SQL/141, Nummernvergabe mit `UPDLOCK, HOLDLOCK`, eine Transaktion, `TREFFER`-Zeile.
- SQL/133 und SQL/141: `OBJECT_ID`/`COL_LENGTH`-Guards, DDL je Batch, History separat; FreshInstall an beiden Stellen (`00_FreshInstall.sql:2831 ff.`, `:3234-3236`).
- Fehlteile (`MissingPartsController`/`MissingPartsLagerController`, nur Controller-Diff gelesen): Werkbank-Filter vor Count/Paging, fremde Werkbank gemeldet.
- Sage-Schreibwege: im Kommissionier-Diff keiner (E7 nicht gebaut).

## Tippfehler (nur gemeldet, nicht geändert)
- `IdealAkeWms/Views/Picking/Bom.cshtml:198` – Tooltip „Position laut Sage-**Stueckliste**“ → „Stückliste“ (neu im Bündel).
- `IdealAkeWms/Views/PickingLeitstand/Index.cshtml:297` und `IdealAkeWms/Views/PickingLeitstand/_PickingLeitstandRow.cshtml:284` –
  „Kommissionierer **aendern**“ → „ändern“ (vorbestehend aus `main`).

## Nachtrag aus der DEV-Regression (B1, 2026-11-14)

Neue Befunde aus [[01-kommissionierung]] (Abschnitt „Neue Befunde“, dort mit Beleg und Vorschlag):
- **N-1 (hoch):** Unterpositionen in Glas-/VK-Aufträgen (und IDEAL Sub-FA-Baugruppen) unsichtbar, wenn die Vater-Zeile nicht im Auftrag ist
  (`Bom.cshtml:233`, `:950-980`); AKE KO-2026-00048 2/2, KO-2026-00022 8/12, IDEAL KO-2026-00005 73/79 Zeilen unsichtbar.
- **N-2 (mittel):** `Nummernkreis.IstDeadlock` (`Nummernkreis.cs:15`) prüft nur eine Ausnahme-Ebene → gleichzeitige Freigabe/„Teilweise“ endet als
  HTTP 500 (Deadlock 1205 verpackt), FA freigegeben ohne Auftrag; betrifft auch den BA-Nummernkreis. **Mit K-1 zusammen beheben.**
- **N-3 (niedrig):** Sammelfreigabe bei nicht lesbarer Stückliste → HTTP 500, Freigabe aber gespeichert.
- **N-4 (Hinweis):** Sammelfreigabe gibt auch FAs mit `IsDonePicking = 1` frei (ergänzt K-8).
- **N-5 (Hinweis):** Bedarfsmeldung ohne konfigurierten Empfänger anlegbar.
- **N-6 (Hinweis):** Altlast KO-2026-00014 offen, obwohl FA 2607170 nicht freigegeben.

## Nachtrag aus den Antworten Gerald (2026-11-13 22:47, [[2026-11-14-wochenend-goal-lauf]])

- **K-16 (Folge-Spec, mittel):** Zahl am Menüpunkt „Kommissionierung“ zählt heute **freigegebene FAs** (seit v1.14.0); Gerald will **offene
  Kommissionieraufträge** (Status Offen/In Arbeit). Vorschlag Koordination: für Kommissionierer die eigenen, für Leitstand/Admin alle. In der
  Anwender-Anleitung 01 bewusst nicht beschrieben, bis umgesetzt.
- **Entscheidungsbedarf (Montag, Gerald + Lager/Keyuser):** produktive Glas-/Vorkommissionierungs-Regeln je Standort (B-5); auf AKE-DEV gibt es nur
  die Artikelregel 87068574 → Glas.

## Sichtung 2026-11-28 (gegen main v1.99.89)

Erledigt: K-1, K-6, Tippfehler, N-1, N-2. Teilweise: K-2 (Hinweis nur in Maske Liste), K-7 (nur Umbuchen), K-12. Offen: K-4, K-5, K-9, K-10, K-11, K-14, K-15, K-16. Frage/Prüfabfrage offen: K-3 (Warnung oder Sperre), K-8 (P4), K-13. N-3 bis N-6 nicht geprüft.
