---
type: spec
title: "Lagerplatz ausbuchen: FA-Spalte in der Bestandstabelle + FA-Feld als echter Filter"
slug: 2026-08-05-wms-bugs-improvements-teil-6-spec
status: Entwurf
created: 2026-08-05
updated: 2026-11-20
source_backlog: "[[2026-08-05-WmsBugs&Improvements]]"
depends_on: "[[2026-08-05-wms-bugs-improvements-teil-1-spec]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: beide
version_vorschlag: "1.99.46 (2026-11-20 geprueft: Worktree steht auf 1.99.45, IdealAkeWms/AppVersion.cs:5; 1.99.46 kommt in keiner .cs/.cshtml/.psd1/.md des Worktrees vor; BF-Warenkorb und Dashboard sind in v1.99.45 aufgegangen; 1.99.47 Sage-AG-Bugfix, 1.99.48 BDE-Liste vorgeschlagen. Dev-Lauf prueft erneut)"
testszenario: "TS-173 (TS-169 ist seit v1.99.45 durch BF-Warenkorb belegt, docs/TESTSZENARIEN.md:13351; TS-171 BDE-Liste, TS-172 Sage-AG-Bugfix reserviert. Dev-Lauf prueft erneut)"
affected_code:
  - IdealAkeWms/Services/FaBuchungsText.cs (neue Helfer Nummern + PruefeZeile inkl. Herkunft Zugang/Ausbuchung)
  - IdealAkeWms/Data/Repositories/IStockMovementRepository.cs (neue Methode + Record FaAmPaar)
  - IdealAkeWms/Data/Repositories/StockMovementRepository.cs (GetProductionOrdersAtLocationAsync abgeleitet, neue Methode je Artikel mit Zugang/Abgang und Zugang ohne FA)
  - IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs (nur Wiederverwendung GetBySubOrderNumbersAsync :271-282, keine Aenderung)
  - IdealAkeWms/Controllers/StockMovementsController.cs (Ctor + IServiceSettingRepository, OutboundAll mit optionalem productionOrder, OutboundAllConfirm mit treffendGezeigt/gemischtGezeigt/gemischtAusbuchen/stueckGezeigt)
  - IdealAkeWms/Models/ViewModels/OutboundAllViewModel.cs
  - IdealAkeWms/Views/StockMovements/OutboundAll.cshtml (Spalte, Hinweis bleibt am Wagen/Platz, Bootstrap-Modal mit Ja/Nein je gemischter Zeile, Inline-JS)
  - IdealAkeWms.Tests/Repositories/StockMovementRepositoryProductionOrderTests.cs
  - IdealAkeWms.Tests/Controllers/LagerbuchungFaVerknuepfungTests.cs (Ctor + Mocks + Aufrufe :109/:117 mit treffendGezeigt/stueckGezeigt anpassen, Assertions unveraendert)
  - IdealAkeWms.Tests/Services/ (neue Unit-Tests FaBuchungsText.PruefeZeile)
  - IdealAkeWms.Tests/Controllers/StockMovementsControllerBulkInboundTests.cs (nur Ctor-Parameter IServiceSettingRepository)
  - IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs + Views/Help/Changelog.cshtml
  - docs/TESTSZENARIEN.md (TS-173 neu; TS-2.4 inhaltlich neu gefasst, Wagen mit Vorbelegung/Modal)
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "ENTSCHIEDEN (Gerald 2026-11-19): Variante A – das FA-Feld filtert; nur Artikel, die laut Bewegungen zu diesem FA gehoeren, werden ausgebucht."
  - "ENTSCHIEDEN (Gerald 2026-11-19): alle FA kommagetrennt in der FA-Spalte."
  - "ENTSCHIEDEN (Gerald 2026-11-19): wie Teil 1 – echter Bestand als Grundlage, FA nur aus Bewegungen mit Restbestand."
  - "ENTSCHIEDEN (Gerald 2026-11-19 20:40): Wagen-Vorbelegung bleibt und filtert; Artikel ohne FA-Vermerk werden sichtbar als 'bleibt am Wagen' gemeldet."
  - "ENTSCHIEDEN (Gerald 2026-11-19 20:40): je gemischter Zeile im Bestaetigungsdialog Ja/Nein nachfragen – kein stilles Ausbuchen, kein stilles Auslassen."
  - "ENTSCHIEDEN (Gerald 2026-11-19 20:40): an IDEAL schliesst eine eingegebene Haupt-FA die Sub-FA ihrer Struktur ein; AKE unveraendert Textvergleich."
  - "ENTSCHIEDEN (Gerald 2026-11-20 12:50): eingegebene Sub-FA trifft nur ihre eigene Nummer (keine Nachfahren); Haupt-FA schliesst alle Sub-FA ein."
  - "ENTSCHIEDEN (Gerald 2026-11-20 12:50): Server prueft treffendGezeigt + gemischtGezeigt per SetEquals und die Stueckzahl je Zeile; jede Abweichung -> nichts buchen, Warnung, Seite neu laden (deckt S-3). Dazu B-3 (a): Zugang ohne FA-Text oder FA nur aus Ausbuchung -> Zeile gemischt."
  - "Antwort 6 nennt 'ueber ProductionOrderId/Hierarchie'. Umgesetzt wird der Textweg: FA-Text der Bewegung -> SubOrderNumber -> OrderNumber (jede Bewegung mit FK traegt dieselbe Nummer im Text, der Textweg trifft zusaetzlich Listen-Bewegungen mit FK null). Bestaetigt?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

> [!info] Überarbeitung 2026-11-19 (spec-agent, Rework)
> Rumpf gegen den Bündel-Stand v1.99.44 (Worktree `2026-08-07-ideal-teile-1-5`) neu belegt und die
> Freigabe-Antworten 1–3 eingearbeitet. Der Ist-Zustand und der Lösungsentwurf von 2026-08-05 sind
> unten als **überholt** aufbewahrt. Neue Rückfragen 4–6 (Wagen-Vorbelegung, gemischte Zeile,
> IDEAL Haupt/Sub-FA). Antwort-, Prüf- und Nachtragsabschnitte unverändert.

> [!info] Überarbeitung 2026-11-20 (spec-agent, Rework Runde 2)
> Antworten 4–6 („Freigabe-Antworten Runde 2“) in Umfang, Anforderungen, Ist-Zustand (Hierarchie
> belegt), Lösungsentwurf, AK und Testszenario eingearbeitet. Testszenario-Nummer **TS-169 → TS-173**
> (TS-169 ist seit v1.99.45 durch den BF-Warenkorb belegt). Version 1.99.46 geprüft und frei. Durch die
> Antworten überholte Stellen (Empfehlung (a) zu Rückfrage 5, „vorbehaltlich“-Formulierungen) sind als
> **überholt** markiert. Neue Rückfragen 7–8. Antwort-, Prüf- und Nachtragsabschnitte unverändert.

> [!info] Überarbeitung 2026-11-20 nachmittags (spec-agent, Rework Runde 3)
> „Freigabe-Antworten Runde 3“ (7, 8, B-3 (a)) und die Befunde S-1 bis S-7 sowie H-1 der „Kritischen
> Pruefung (2026-11-20)“ eingearbeitet: Serverprüfung über treffende **und** gemischte Zeilen plus
> Stückzahl je Zeile (Anforderung 11), „gemischt“ auch bei Zugang ohne FA-Text bzw. FA nur aus Ausbuchung
> (Anforderungen 3/4, Repository-Abfrage im Lösungsentwurf 2), Knopf-Sperre gegen Doppel-Absenden,
> IDEAL-Anzeigeformat, TS-2.4 neu gefasst. Überholte Stellen sind markiert. Neue Rückfrage 9 (S-1,
> Bestätigung Textweg). TS-173 und Version 1.99.46 bleiben. Antwort-, Prüf- und Nachtragsabschnitte
> unverändert; einzige Ausnahme ist die vom Menschen beauftragte Überholt-Markierung (H-1) unter Runde 1.

## Ziel / Nutzen (das Warum)

Auf der Seite „Lagerplatz ausbuchen" (`/StockMovements/OutboundAll`) soll der Lagermitarbeiter
erkennen können, zu welchem Fertigungsauftrag die auf dem Lagerplatz liegenden Artikel gehören,
und gezielt nur die Artikel eines bestimmten FA ausbuchen können. Heute zeigt die Tabelle keine
FA-Spalte, und das FA-Feld ändert nur den Text der neuen Ausbuchungen. Ausgebucht wird **immer**
der gesamte Bestand des Lagerplatzes (Beleg unten, Ist-Zustand).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. FA-Spalte „Fertigungsauftrag" in der Bestandstabelle von `OutboundAll`: je Artikelzeile **alle**
   FA-Nummern kommagetrennt (Antwort 2), ermittelt nur aus Bewegungen an Artikel/Lagerplatz-Paaren
   mit echtem Restbestand > 0 (Antwort 3, Regel wie Teil 1).
2. FA-Feld als echter Filter (Antwort 1): Steht ein Wert im Feld, werden beim Bestätigen **nur** die
   Artikel ausgebucht, deren FA-Spalte eine der eingegebenen Nummern enthält.
3. Knopftext und Bestätigungsdialog zeigen, wie viele bzw. welche Artikel tatsächlich ausgebucht
   werden. Kein stiller Fallback auf „alles ausbuchen", wenn der Filter nichts trifft.
4. Kommissionierwagen (Antwort 4): Vorbelegung des FA-Felds bleibt und filtert; Artikel ohne
   FA-Vermerk werden vor und nach dem Buchen sichtbar als „bleibt am Wagen“ gemeldet.
5. Gemischte Zeilen (Antwort 5): Bestätigungsdialog als Bootstrap-Modal mit Ja/Nein je gemischter
   Zeile; der Server prüft beim Absenden die gezeigten treffenden und gemischten Zeilen und die
   Stückzahl je Zeile gegen den neu ermittelten Stand (Antwort 8) und bucht bei jeder Abweichung nichts;
   Sage-Meldung nur für tatsächlich ausgebuchte Zeilen.
6. IDEAL (Antwort 6): eine eingegebene Haupt-FA schließt **alle** Sub-FA ihrer Struktur ein (jede
   Ebene); eine eingegebene Sub-FA trifft nur ihre eigene Nummer (Antwort 7). Standort = Master
   `ProduktionsauftragHierarchisch`. AKE unverändert reiner Textvergleich. Der Einschluss läuft über den
   **FA-Text** der Bewegung → `SubOrderNumber` → `OrderNumber`, nicht über `StockMovement.ProductionOrderId`
   (Abweichung vom Wortlaut „über ProductionOrderId“ in Antwort 6, begründet im Ist-Zustand
   „Hierarchie“ und in der Kritischen Prüfung 2026-11-20 Abschnitt 2b; Bestätigung offen als Rückfrage 9).
7. Bestand ohne FA in einer FA-Zeile (B-3 (a)): Eine treffende Zeile gilt auch dann als gemischt und wird
   im Dialog abgefragt, wenn am Paar ein Zugang **ohne** FA-Text liegt oder die getroffene FA nur aus
   Ausbuchungen stammt.

**Out-of-Scope**
- Leeres FA-Feld: Verhalten bleibt wie heute (gesamter Lagerplatz-Bestand, heutiger `confirm()`-Dialog).
- `LocationTransfer`/`LocationTransferConfirm` (Lagerplatz umbuchen) – das Backlog nennt nur
  „Lagerplatz ausbuchen".
- FA-genaue Mengen (Bestand ist nach der Buchung nicht FA-attributiert, Teil-1-Spec S1/AK7). Eine
  ausgebuchte Zeile umfasst immer den echten Platz-Bestand des Artikels; gemischte Zeilen werden
  deshalb erfragt (Antwort 5), nicht aufgeteilt.
- Einschluss der Nachfahren einer eingegebenen **Sub**-FA (entschieden durch Antwort 7: nein).
- Zeitliche Zuordnung „Zugang ohne FA ist schon verbraucht“: jeder Zugang ohne FA-Text am Paar zählt,
  auch ein alter (Bestand ist nicht FA-attributiert; lieber einmal mehr fragen als still ausbuchen).
- Auswahl einzelner Zeilen per Checkbox (Variante B, verworfen durch Antwort 1).
- Änderung an `GetStockByProductionOrderAsync` (Teil 1) und am Sage-Enqueue.

## Fachliche Anforderungen

1. Die Tabelle zeigt nach Artikelnummer, Bezeichnung, Einheit eine Spalte „Fertigungsauftrag" vor
   „Bestand". Inhalt: alle FA-Nummern der Zeile, einzeln, jede einmal, sortiert, mit „, " getrennt;
   leer, wenn keine Bewegung am Paar einen FA-Text trägt. An IDEAL (Master an) wird eine Nummer, die
   Sub-FA eines FA ist, als „S1 (Haupt-FA H)“ angezeigt (Format wie `StockApiController.cs:49`); Haupt-FA
   und unbekannte Nummern erscheinen ohne Zusatz. Verglichen wird immer nur die reine Nummer, nie der
   Anzeigetext (S-2).
2. „Gehört zu FA" heißt: Am Paar (Artikel, dieser Lagerplatz) liegt echter Bestand > 0 **und**
   mindestens eine Bewegung mit `StorageLocationId` = dieser Platz trägt die Nummer im FA-Text
   (Texte wie „A;B" oder „A, B" werden in Einzelnummern zerlegt). Das ist dieselbe Regel, die heute
   lagerplatzweit für die Wagen-Vorbelegung und den Wagen-Konflikt gilt, nur je Artikel.
3. Bewegungsarten und Herkunft (B-3 (a)): Für Spalte und Treffer zählen alle Bewegungen mit
   `StorageLocationId` = Platz, auch eine Ausbuchung mit FA-Text (wie `StockMovementRepository.cs:527-533`
   und die Teil-1-Kandidatensuche `:198-202`). Für „gemischt“ wird zusätzlich die Herkunft je Paar
   unterschieden, mit denselben Vorzeichen wie die Bestandsberechnung (`StockMovementRepository.cs:65-72`,
   `:497-503`):
   - **Zugang** = `Einbuchung`, `SageEinbuchung`, `Umbuchung` (Vorzeichen +; eine `Umbuchung` mit
     `StorageLocationId` = Platz ist immer die Zielseite, die Quellseite steht in `SourceStorageLocationId`
     und wird hier wie heute nicht gelesen, `:81-85`).
   - **Abgang** = `Ausbuchung`, `SageAusbuchung` (Vorzeichen −).
   - **Zugang ohne FA-Text** = Zugang, dessen Text nach Zerlegung keine Nummer enthält (null, leer,
     nur Leerzeichen/Trennzeichen).
   Die Paar-Eigenschaften sind also: alle Nummern (Spalte), Nummern aus Zugängen, und ob es einen Zugang
   ohne FA-Text gibt. Schreibwege belegt siehe Ist-Zustand „Bewegungsarten und Schreibwege“.
4. FA-Feld mit Wert: Das Feld wird in Einzelnummern zerlegt (Trennzeichen `;` und `,`, getrimmt).
   Je FA-Nummer einer Zeile gilt: sie ist **abgedeckt**, wenn sie **exakt** einer eingegebenen Nummer
   entspricht (Groß-/Kleinschreibung egal; kein Teilstring: „123" trifft „1234567" nicht) **oder – nur
   an IDEAL –** sie die Sub-FA-Nummer eines FA ist, dessen Haupt-FA-Nummer eingegeben wurde
   (Antwort 6, jede Ebene, Regel siehe Ist-Zustand „Hierarchie“). Eine eingegebene Sub-FA deckt nur
   sich selbst ab – weder ihre Nachfahren noch ihre Haupt-FA (Antwort 7). Eine Zeile **trifft**, wenn
   mindestens eine ihrer Nummern abgedeckt ist. Sie ist **gemischt**, wenn sie trifft **und** mindestens
   einer dieser Gründe vorliegt (jeder Grund wird im Dialog genannt):
   - (G1) sie trägt eine nicht abgedeckte Nummer → „enthält auch FA …“;
   - (G2) am Paar liegt ein Zugang ohne FA-Text → „enthält auch Bestand ohne FA“ (B-3 (a));
   - (G3) keine abgedeckte Nummer kommt aus einem Zugang, die getroffene FA stammt also nur aus
     Ausbuchungen → „FA … nur aus Ausbuchung“ (B-3 (a)).
   Beispiel B-3: Wareneingang 100 Stk ohne FA, danach Einzel-Ausbuchung 5 Stk mit FA 111 → Spalte „111“,
   Filter 111 → gemischt (G2 + G3), Rückfrage mit 95 Stk.
5. Menge je ausgebuchter Zeile = echter Platz-Bestand des Artikels beim Absenden (wie heute).
6. Trifft der Filter keine Zeile: nichts buchen, Warnung „Kein Artikel auf <Platz> gehört zu FA
   <Eingabe> – nichts ausgebucht.", zurück auf die Seite mit demselben Lagerplatz und derselben Eingabe.
7. Gefilterte Ausbuchung erfolgreich: Erfolgsmeldung nennt Anzahl und FA; verbleiben Artikel, nennt
   die Meldung deren Anzahl und Artikelnummern mit dem Zusatz „bleibt am Wagen“ (Kommissionierwagen)
   bzw. „bleibt am Platz“ (sonst) – dazu zählen auch gemischte Zeilen mit „Nein“. Rückkehr auf die
   Seite **mit** demselben Lagerplatz, damit der Rest sichtbar ist. Ungefilterte Ausbuchung: Rückkehr
   wie heute ohne Lagerplatz.
8. FA-Text und FA-Verknüpfung der neuen Ausbuchungen bleiben wie heute: Text = Feldinhalt,
   `ProductionOrderId` nur bei eindeutiger Auflösung (Liste → null, v1.87.0 Antwort 5; an IDEAL wird
   eine eingegebene Haupt-FA über `FindForBookingAsync` mit der Haupt-FA verknüpft, auch wenn die
   Zeile über eine Sub-FA getroffen wurde).
9. Die Seite zeigt vor dem Absenden, was passiert: Knopf „Alle N Artikel ausbuchen" bei leerem Feld,
   „M von N Artikeln ausbuchen (FA …)" bei Wert (M = treffende Zeilen inkl. gemischter), deaktiviert mit
   Hinweistext bei M = 0. Nicht treffende Zeilen tragen bei Wert sichtbar den Text „bleibt am Wagen“
   bzw. „bleibt am Platz“. Der Server entscheidet selbst neu (Seite und POST können auseinanderliegen);
   die Seite ist nur Vorschau.
10. **Gemischte Zeilen (Antwort 5):** Bei Wert im Feld öffnet der Knopf einen Bestätigungsdialog
    (Bootstrap-Modal) mit drei Blöcken: „Wird ausgebucht“ (treffende, nicht gemischte Zeilen:
    Artikelnummer, Bezeichnung, Bestand), „Bitte entscheiden“ (je gemischter Zeile Artikelnummer,
    Bestand, getroffene FA, **jeder zutreffende Grund** G1–G3 aus Anforderung 4 als Text und zwei
    Optionsfelder **Ausbuchen** / **Nicht ausbuchen**, keines vorausgewählt) und „Bleibt am
    Wagen“/„Bleibt am Platz“. „Jetzt ausbuchen“ ist gesperrt, solange eine gemischte Zeile unbeantwortet
    ist (Hinweis „Noch n Zeilen offen“) oder die Summe der auszubuchenden Zeilen 0 ist.
    „Abbrechen“/Schließen/Escape: kein POST, nichts gebucht, Feld und Seite bleiben unverändert.
    **Doppel-Absenden (S-4):** Nach dem ersten Klick auf „Jetzt ausbuchen“ – und im leeren-Feld-Weg nach
    „OK“ im `confirm()` auf „Alle N Artikel ausbuchen“ – wird der Knopf sofort `disabled` und zeigt
    „Wird ausgebucht …“; ein zweiter Klick sendet nichts. (Zwei echt gleichzeitige POSTs aus zwei Tabs
    bleiben wie heute möglich, Kritische Prüfung S-4; nicht Teil dieser Spec.)
11. **Serverprüfung beim Absenden (Antwort 8, deckt S-3):** Mitgesendet werden Artikel-IDs und
    Stückzahlen, nie die zu buchende Menge selbst:
    - `treffendGezeigt` = IDs der Zeilen unter „Wird ausgebucht“ (treffend, nicht gemischt);
    - `gemischtGezeigt` = IDs aller Zeilen unter „Bitte entscheiden“;
    - `gemischtAusbuchen` = Teilmenge von `gemischtGezeigt` mit „Ausbuchen“;
    - `stueckGezeigt[<ArticleId>]` = angezeigter Bestand je Zeile aus `treffendGezeigt` ∪ `gemischtGezeigt`
      (Text in invarianter Schreibweise, weil die App auf `de-DE` läuft, `Program.cs:14-15`).
    Der Server ermittelt Bestand, FA-Eigenschaften je Zeile, treffende und gemischte Zeilen neu und bucht
    **nichts**, warnt „Bestand oder FA-Zuordnung auf <Platz> hat sich geändert – bitte Auswahl erneut
    prüfen. Nichts ausgebucht.“ und lädt die Seite neu (Redirect mit Lagerplatz und Eingabe), wenn
    **eine** dieser Bedingungen zutrifft:
    - neu ermittelte treffende, nicht gemischte Zeilen ≠ `treffendGezeigt` (Mengenvergleich, `SetEquals`;
      erfasst eine neu treffende Zeile ebenso wie eine weggefallene);
    - neu ermittelte gemischte Zeilen ≠ `gemischtGezeigt` (`SetEquals`; erfasst auch eine bisher
      treffende Zeile, die durch einen zusätzlichen fremden FA oder einen Zugang ohne FA gemischt wurde);
    - `gemischtAusbuchen` enthält eine ID außerhalb von `gemischtGezeigt`;
    - für eine Zeile aus beiden Mengen fehlt `stueckGezeigt`, ist nicht lesbar oder ≠ neu ermittelter
      Bestand (exakter Dezimalvergleich);
    - fehlende Parameter zählen als leere Menge (POST ohne Auswahl bei vorhandenen Treffern → Abweichung).
    Sonst bucht er `treffendGezeigt` plus `gemischtAusbuchen`; ergibt das 0 Zeilen: nichts buchen,
    Warnung „Nichts ausgebucht – alle gemischten Zeilen mit ‚Nicht ausbuchen‘ beantwortet.“, Rückkehr mit
    Lagerplatz und Eingabe. Die gebuchte Menge ist der Server-Bestand (nach bestandener Prüfung gleich
    der angezeigten Stückzahl). Grenze: Bekommt eine schon gemischte Zeile einen weiteren fremden FA, ohne
    dass sich ihr Bestand ändert (Zu- und Abgang gleicher Menge zwischen Seite und POST), bleibt das
    unerkannt – jede einzelne Bewegung ändert den Bestand, nur ihre Summe nicht.
12. **Kommissionierwagen (Antwort 4):** Die Vorbelegung bleibt (alle FA des Wagens) und wirkt als
    Filter; Artikel ohne FA-Vermerk treffen nicht und werden nach 9/10/7 als „bleibt am Wagen“ gezeigt
    und gemeldet. Weil die Vorbelegung alle Wagen-FA enthält, entsteht dort ohne Änderung des Felds kein
    Grund G1. G2/G3 sind am Wagen möglich – z. B. ein Artikel, der per Kommissionierung mit FA und
    zusätzlich per manueller Umbuchung ohne FA-Text auf den Wagen kam (`StockMovementsController.cs:463`
    mit leerem Feld, `:618-631`, `PickingTransferService.cs:75,216` ohne FA am Auftrag) – und werden
    dann abgefragt.
13. **Standort (Antwort 6):** IDEAL-Einschluss nur bei Master `ProduktionsauftragHierarchisch` = true;
    an AKE (Master false/leer) gilt ausschließlich der Textvergleich aus 4.

> [!note]- Anforderungen 4, 6, 7, 9 vom 2026-11-19 – **überholt** durch Antworten 4–6, nur Audit
> 4. „Ausgebucht werden genau die Zeilen, deren FA-Nummern mindestens eine davon **exakt** enthalten“ –
> ohne IDEAL-Einschluss und ohne Begriff „gemischt“. 6. Rückkehr nur „mit demselben Lagerplatz“.
> 7. Meldung nannte nur die Anzahl verbleibender Artikel. 9. „Der Bestätigungsdialog nennt bei Filter
> die Artikelnummern der M Zeilen“ (einfacher `confirm()`, keine Ja/Nein-Wahl).

> [!note]- Anforderungen 3, 4, 10, 11, 12 vom 2026-11-20 (Runde 2) – **überholt** durch Runde 3, nur Audit
> 3. „Bewegungsarten: alle zählen (auch eine Ausbuchung mit FA-Text)“ – ohne Herkunft Zugang/Abgang.
> 4. „gemischt, wenn sie trifft und zusätzlich mindestens eine nicht abgedeckte Nummer trägt“ – nur G1.
> 10. Dialog nannte nur „enthält auch FA …“; keine Knopf-Sperre nach dem Klick.
> 11. „Mitgesendet werden nur Artikel-IDs – `gemischtGezeigt` … und `gemischtAusbuchen` … Stückzahl kommt
> nie aus dem Client (Rückfrage 8).“ – Prüfung nur der gemischten Zeilen, ohne `treffendGezeigt` und
> ohne Stückzahlvergleich.
> 12. „Weil die Vorbelegung alle Wagen-FA enthält, gibt es dort ohne Änderung des Felds keine gemischten
> Zeilen.“ – gilt seit B-3 (a) nur noch für G1.

## Ist-Zustand (Code-Referenzen, Bündel-Stand v1.99.44)

Belegt im Worktree `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`. Am 2026-11-20
gegen v1.99.45 nachgeprüft: Controller `:482-557`, View, `StockMovementRepository.cs:514-542` unverändert.

**Controller** `IdealAkeWms/Controllers/StockMovementsController.cs` (keine Klassen-Filter, `:11`):
- `OutboundAll(int? storageLocationId)` `:482-511`, `[RequireStockKeyUserAccess]` `:482`. Platzliste
  ohne Kommissionierwagen (`:487` → `StorageLocationRepository.cs:43-49`: `IsActive && !IsPickingTransport && IstBuchbar`).
  Zeilen = `GetCurrentStockAsync(filterStorageLocationId)` mit `CurrentQuantity > 0` (`:497-499`).
  Nur für Wagen: FA-Feld vorbelegt mit `string.Join("; ", GetProductionOrdersAtLocationAsync(...))`
  (`:501-507`).
- `OutboundAllConfirm(int storageLocationId, string? productionOrder)` `:513-557`,
  `[RequireStockKeyUserAccess]`, `[HttpPost]`, `[ValidateAntiForgeryToken]`. Auswahl
  `itemsToOutbound` = alle Zeilen > 0 (`:518-520`) – **`productionOrder` wirkt nicht auf die
  Auswahl**. Leer → Warnung (`:522-526`). FA-Auflösung einmal (`:532`, Kommentar `:531`: Liste →
  nicht verknüpft, keine Pflichtprüfung). Je Zeile `StockMovement` mit `Quantity = CurrentQuantity`,
  `ProductionOrder = productionOrder`, `ProductionOrderId = fa.Order?.Id`, `MovementType.Ausbuchung`,
  Audit `CreatedAt/CreatedBy/CreatedByWindows` (`:534-553`), über `AddAsync` (`:551`). Erfolg →
  Redirect **ohne** Lagerplatz (`:555-556`).

**View** `IdealAkeWms/Views/StockMovements/OutboundAll.cshtml`: Kopfzeile Artikelnummer/
Bezeichnung/Einheit/Bestand, **keine FA-Spalte** (`:61-67`), Zeilen `:70-78`, Fußzeile
`colspan="3"` (`:80-85`). Formular `:90-103`, FA-Feld `name="productionOrder"` (`:95`), Knopf
„Alle @Model.Items.Count Artikel ausbuchen" (`:98-100`). Kein eigenes JS-Modul: Inline-Skript
`confirm(...)` → `submit()` (`:117-123`); `barcode-scanner.js` (`:116`) nur für den Lagerplatz-Scan.
Wagen-Hinweis „FA-Nummer wurde automatisch ermittelt" `:45-53`.

**ViewModel** `IdealAkeWms/Models/ViewModels/OutboundAllViewModel.cs:3-11`: kein FA je Zeile.
`StockOverviewItem` (`StockOverviewViewModel.cs:19-34`): kein FA-Feld (wird breit genutzt, deshalb
nicht erweitern).

**Service-Schicht:** Es gibt keinen OutboundAll-Service; der Controller bucht direkt über das
Repository. `IStockMovementRepository` ist als `SageBookingEnqueueingStockMovementRepository`
registriert (`Program.cs:78`), eine Unterklasse von `StockMovementRepository`, die nur `AddAsync`
überschreibt (`SageBookingEnqueueingStockMovementRepository.cs:21,41-80`). Neue Lesemethoden erbt
sie ungeändert. `hatFaBezug` bremst nur Umbuchungen (`SageBookingEnqueueDecision.cs:66-75`),
Ausbuchungen gehen je nach Schalter + Lagerplatz-Flag `SageBuchungErlaubt` an Sage
(`SageBookingEnqueueingStockMovementRepository.cs:51-69`).

**Repository** `IdealAkeWms/Data/Repositories/StockMovementRepository.cs`:
- `GetCurrentStockAsync` `:16-193`: echter Bestand je Paar (Ziel-Summe `:41-79` + Umbuchungs-Quelle
  `:81-131`, Merge `:133-156`).
- `GetStockByProductionOrderAsync(string, bool onlyActualStock = true)` `:195-295` – **das ist die
  Teil-1-Umsetzung** (Spec [[2026-08-05-wms-bugs-improvements-teil-1-spec]], status Gemerged, v1.29.0):
  Kandidaten über `ProductionOrder.Contains(...)` (`:198-202`, Teilstring), `true`-Pfad = echter
  Bestand je Paar über `GetCurrentStockAtLocationAsync` (`:259-289`, `:492-512`), nur > 0.
  Aufrufer: `StockApiController` (Einbuchungs-Hinweis, Tracking-Modal) mit `true`, StockOverview mit `false`.
- `GetProductionOrdersAtLocationAsync(int)` `:514-542`: genau die Regel „FA nur aus Bewegungen mit
  Restbestand", aber **lagerplatzweit**: Artikel mit Bestand > 0 am Platz (`:518-522`), FA-Texte aller
  Bewegungen mit `StorageLocationId` = Platz für diese Artikel (`:527-533`), seit v1.99.13 in
  Einzelnummern zerlegt, distinct, ordinal sortiert (`:535-541`). Aufrufer: Wagen-Vorbelegung
  (`StockMovementsController.cs:504`) und Wagen-Konfliktprüfung (`PickingTransferService.cs:82-103`).
  Test: `StockMovementRepositoryProductionOrderTests.cs:20-35`.

**Hierarchie Haupt-/Sub-FA (für Antwort 6, belegt 2026-11-20):**
- Es gibt **keine** Spalte `ParentProductionOrderId` und kein gespeichertes `IsHauptFa`-Feld. Die
  Beziehung steckt in drei Textspalten von `ProductionOrder` (`Models/ProductionOrder.cs`):
  `OrderNumber` = Sage StrukturID/Haupt-FA, nach der Inversion **nicht** eindeutig (`:14-17`);
  `SubOrderNumber` = Sage BelID, eindeutig (`:19-22`, Index `ApplicationDbContext.cs:503`);
  `ParentSubOrderNumber` = echter Elternzeiger VaterFA, nötig für mehrstufige Strukturen (`:42-47`).
  `IsHauptFa` ist eine Expression `SubOrderNumber == "" || SubOrderNumber == OrderNumber` (`:29-30`),
  `IsSubFa` daraus kompiliert (`:35`).
- **Schreibweg** (der eine, der Sub-FA anlegt): `IDEALAKEWMSService/Services/FaMaterializationSyncService.cs:149-161`
  setzt je Knoten `OrderNumber = n.HauptFA`, `SubOrderNumber = n.SubFA`, `ParentSubOrderNumber = n.VaterFA` –
  **jede** Ebene einer Struktur trägt also dieselbe `OrderNumber`; Updates (`:172-182`) ändern `OrderNumber`
  nicht. Blätter (`SubFA = 0`) werden nicht materialisiert (`:134-140`). Dieselbe Lesart nutzt
  `IProductionOrderRepository.CountByOrderNumberAsync` („alle materialisierten Nachfahren-Sub-FAs einer
  Gruppe“, `IProductionOrderRepository.cs:57-62`). AKE (flach): `SubOrderNumber = OrderNumber`
  (Backfill, `ProductionOrder.cs:16-17`; `SageProductionOrderSql.cs:5`).
- `StockMovement.ProductionOrderId` verknüpft eine Bewegung mit **einem** FA (bei Text = Sub-FA mit der
  Sub-FA, bei Haupt-FA-Text mit der Haupt-FA, `ProductionOrderRepository.cs:224-238`); aus ihr allein
  folgt keine Hierarchie, und Altbewegungen mit Liste/unbekanntem Text haben `null`. Deshalb läuft der
  Einschluss über den **Text** der Bewegung → `SubOrderNumber` → `OrderNumber`, nicht über die FK.
  Die Kritische Prüfung 2026-11-20 (Abschnitt 2b) hat das über alle Schreibwege der FK bestätigt: jede
  Bewegung mit FK trägt dieselbe Nummer (bzw. deren `OrderNumber`) auch im Text
  (`StockMovementsController.cs:151/152, :263/264, :370/371, :463/464, :541/542` aus `FindForBookingAsync`
  desselben Texts; `PickingTransferService.cs:75,216-217`); der Textweg trifft also alles, was der FK-Weg
  trifft, und zusätzlich Listen-Bewegungen mit FK `null`. Ein-Zeilen-Bestätigung durch den Menschen:
  Rückfrage 9 (S-1).
- Wiederverwendbar: `GetBySubOrderNumbersAsync(IReadOnlyCollection<string>)`
  (`ProductionOrderRepository.cs:271-282`, ein Read für alle Nummern; heutiger Aufrufer
  `PlannedConsumptionService.cs:106`).
- **Standort-Ermittlung:** ServiceSetting `ProduktionsauftragHierarchisch`
  (`HierarchischeStrukturKeys.Master`, `Services/HierarchischeStruktur/HierarchischeStrukturKeys.cs:10`),
  gelesen über `IServiceSettingRepository.GetValueAsync` und ausgewertet mit
  `FaBuchungsText.IstHierarchisch` (`Services/FaBuchungsText.cs:51`) – Muster `StockApiController.cs:81-82`.
  `StockMovementsController` hat heute **kein** `IServiceSettingRepository` (Ctor `:22-30`, nur
  `IAppSettingRepository`). Anzeige „Sub-FA (Haupt-FA …)“ gibt es schon: `StockApiController.cs:49`.

**Trennzeichen-Konvention** `;`/`,`: `ProductionOrderRepository.cs:228` (Liste erkennen),
`StockMovementRepository.cs:538` (zerlegen). Wagen-Bewegungen tragen zusammengesetzte Texte wie
„A;B" (`PickingTransferService.cs:101-102`, Bewegung `:210-218`).

**Bewegungsarten und Schreibwege (für B-3, belegt 2026-11-20):** `MovementType` hat genau fünf Werte
(`Models/MovementType.cs:5-9`). Vorzeichen in der Bestandsberechnung: `Einbuchung`, `SageEinbuchung`,
`Umbuchung` (Ziel) +, `Ausbuchung`, `SageAusbuchung` − (`StockMovementRepository.cs:65-72`, `:497-503`),
Umbuchungs-Quelle über `SourceStorageLocationId` − (`:81-131`, `:505-509`). Alle `new StockMovement`
außerhalb der Tests (Suche über den Worktree, kein raw-SQL-INSERT auf `StockMovements` gefunden):

| Schreibweg | Art | FA-Text |
|---|---|---|
| Einbuchen `StockMovementsController.cs:146-153` | Einbuchung | Feldinhalt `vm.ProductionOrder` (`:151`), leer möglich |
| Sammel-Einbuchung `:258-265` | Einbuchung | Feldinhalt (`:263`), leer möglich |
| Einzel-Ausbuchung `:365-372` | Ausbuchung | Feldinhalt (`:370`) |
| Umbuchen `:457-465` | Umbuchung | Feldinhalt (`:463`), leer möglich |
| Lagerplatz ausbuchen `:536-543` | Ausbuchung | Feldinhalt (`:541`) |
| Lagerplatz umbuchen `:618-631` | Umbuchung | **nie** (kein `ProductionOrder` gesetzt) |
| Wareneingang `PurchaseOrdersController.Receive.cs:140-151` | Einbuchung | **nie** |
| Sage-Korrektur `IDEALAKEWMSService/Services/LagerbestandSyncService.cs:179-192, :247-257` | SageEin-/SageAusbuchung | **nie** |
| Kommissionierung `PickingTransferService.cs:210-218` | Umbuchung | `auftrag.ProductionOrder?.OrderNumber` (`:75`), null ohne FA; bei „Trotzdem buchen“ Liste, leerer String möglich (`:102`) |

Folge für B-3: Zugänge ohne FA-Text entstehen regulär (Wareneingang, Sage-Korrektur Plus, Lagerplatz
umbuchen, Einbuchung/Umbuchung mit leerem Feld). Ein FA-Text nur aus Abgängen entsteht über Einzel- und
Lagerplatz-Ausbuchung. Die heutige Abfrage `:527-533` filtert `ProductionOrder != null` und sieht deshalb
Zugänge ohne Text nicht; die Art wird nicht gelesen.

**Wagen-Einstieg:** Wagen stehen nicht in der Platzauswahl, kommen aber über den Konflikt-Dialog
der Kommissionierung auf die Seite (`Views/Picking/Bom.cshtml:1655-1660`, `conflictLocationId` aus
`:1627`).

**Rechte:** `RequireStockKeyUserAccess` → `CanTransferStockAsync` = admin, stock_keyuser, picking
(`CurrentUserService.cs:134-135`).

**Bestehende Tests/Szenarien, die den Pfad berühren:**
`LagerbuchungFaVerknuepfungTests.cs:109` (`OutboundAllConfirm(2, "1065141")`, Mock `GetCurrentStockAsync`
→ Artikel 9, `:76-77`) und `:115-119` (`"A; B"` → nicht verknüpft); beide Testklassen bauen den
Controller mit 8 Ctor-Argumenten (`LagerbuchungFaVerknuepfungTests.cs:89-90`,
`StockMovementsControllerBulkInboundTests.cs:42-44`). `docs/TESTSZENARIEN.md` TS-2.4
(`:272-289`, Wagen „Alle Artikel ausbuchen"), Zeile 115.12 (`:11352`), 130.5.6 (`:11810`, Wagen-Vorbelegung).

### Was seit v1.2x schon umgesetzt ist (ehrlich)

- **Nicht umgesetzt:** FA-Spalte (View `:61-67`) und Filterwirkung (`:518-520`) – der Kern dieser
  Spec ist offen.
- **Umgesetzt, nutzbar:** Teil 1 (`onlyActualStock`, v1.29.0, gemerged); die lagerplatzweite
  Restbestand-Regel mit Zerlegung in Einzelnummern (v1.99.13); FA-Verknüpfung `ProductionOrderId`
  beim Lagerplatz-Ausbuchen (v1.87.0, `:532,542`). Nichts davon liefert FA je Artikelzeile.

> [!note]- Ist-Zustand vom 2026-08-05 (v1.2x) – **überholt**, nur Audit
> Zeilenangaben veraltet; Aussage „productionOrder wirkt nur als Tag" gilt weiterhin (jetzt `:518-541`).
> Der damalige Verweis auf `GetProductionOrdersAtLocationAsync` „:448-470, nur Wagen" ist überholt:
> heute `:514-542`, zusätzlich vom Wagen-Konflikt genutzt, mit Zerlegung (v1.99.13).
>
> `IdealAkeWms/Views/StockMovements/OutboundAll.cshtml:60-106`: Tabelle zeigt Spalten
> Artikelnummer/Bezeichnung/Einheit/Bestand — **keine** FA-Spalte. Das Formular
> (Zeilen 90-104) hat ein Textfeld `name="productionOrder"`, das per
> `OutboundAllConfirm(int storageLocationId, string? productionOrder)`
> (`IdealAkeWms/Controllers/StockMovementsController.cs:364-405`) verarbeitet wird.
> **Befund:** `productionOrder` beeinflusst **ausschließlich** das Tag der neu erzeugten
> Ausbuchungs-Bewegungen, **nicht** die Auswahl `itemsToOutbound`.
> `OutboundAllViewModel` (`:1-11`) und `StockOverviewItem` (`StockOverviewViewModel.cs:17-32`) haben
> **kein** Feld für einen FA-Bezug je Zeile. Vorbild `GetProductionOrdersAtLocationAsync`
> (`StockMovementRepository.cs:448-470`) — aggregiert über den ganzen Lagerplatz, nur Kommissionierwagen
> (`StockMovementsController.cs:352-358`).

## Technischer Lösungsentwurf

Kein neues Muster, keine Schemaänderung. Die vorhandene lagerplatzweite Regel wird **je Artikel**
geliefert und die alte Methode daraus abgeleitet – eine Regel an einem Ort.

1. **Zerlegen und Prüfen an einem Ort** – `FaBuchungsText` (`Services/FaBuchungsText.cs`, schon die FA-Text-Hilfe):
   - `public static IEnumerable<string> Nummern(string? text)` = Split an `;`/`,`, `TrimEntries |
     RemoveEmptyEntries`. `StockMovementRepository.cs:538` nutzt ihn statt des eigenen Splits.
   - `public static (bool Trifft, List<string> Fremd, bool OhneFa, bool NurAusAusbuchung) PruefeZeile(FaAmPaar? paar,
     IReadOnlyDictionary<string, string> hauptJeSub, IReadOnlySet<string> filter)` – reine Funktion nach
     Anforderung 4: Nummer abgedeckt ⇔ `filter.Contains(nr)` oder (`hauptJeSub.TryGetValue(nr, out h)` und
     `filter.Contains(h)`); `Trifft` = eine Nummer aus `paar.Nummern` abgedeckt; `Fremd` = nicht abgedeckte
     (G1); `OhneFa` = `paar.ZugangOhneFa` (G2); `NurAusAusbuchung` = `Trifft` und keine abgedeckte Nummer in
     `paar.ZugangNummern` (G3). Gemischt ⇔ `Trifft && (Fremd.Count > 0 || OhneFa || NurAusAusbuchung)`
     (öffentliche Hilfe `IstGemischt` oder Tupel-Auswertung an einer Stelle). `paar == null` → nichts trifft.
     An AKE ist `hauptJeSub` leer → reiner Textvergleich (Antwort 6). Controller-GET, -POST und die Tests
     nutzen nur diese Funktion; das JS bildet dieselbe Regel nach (Punkt 6).
2. **Repository** – neue Methode in `IStockMovementRepository` + `StockMovementRepository`:
   `Task<Dictionary<int, FaAmPaar>> GetProductionOrdersByArticleAtLocationAsync(int storageLocationId)`
   mit `public sealed record FaAmPaar(List<string> Nummern, HashSet<string> ZugangNummern, bool ZugangOhneFa)`
   (im selben Interface-File; `Nummern` distinct, ordinal sortiert = Spalteninhalt; `ZugangNummern` mit
   `StringComparer.OrdinalIgnoreCase`). Abfrage:
   - Artikel mit Bestand > 0 am Platz wie heute (`GetCurrentStockAsync(filterStorageLocationId)`, `:518-522`).
   - **ein** Read: `_dbSet.AsNoTracking().Where(sm => sm.StorageLocationId == platz &&
     artikelMitBestand.Contains(sm.ArticleId)).Select(sm => new { sm.ArticleId, sm.MovementType,
     sm.ProductionOrder }).Distinct().ToListAsync()` – anders als heute **ohne** `ProductionOrder != null`
     (`:529`), weil G2 die Zugänge ohne Text braucht. `Distinct` in SQL hält die Menge klein (ein Satz je
     Artikel/Art/Text).
   - In C# je `ArticleId` gruppieren: `Nummern` = `FaBuchungsText.Nummern(text)` über alle Sätze;
     `ZugangNummern` = dasselbe nur für `MovementType` ∈ {`Einbuchung`, `SageEinbuchung`, `Umbuchung`};
     `ZugangOhneFa` = es gibt einen Zugang-Satz mit `!FaBuchungsText.Nummern(text).Any()`.
     `Ausbuchung`/`SageAusbuchung` liefern nur `Nummern`. Die Zuordnung der Arten steht als eine private
     Hilfe `IstZugang(MovementType)` neben der Methode (gleiche Vorzeichen wie `:65-72`).
   - Artikel mit Bestand > 0, aber ganz ohne FA-Text, stehen mit leeren `Nummern` im Dictionary oder fehlen
     – beides heißt „trifft nicht“ (Aufrufer: fehlend = `null` an `PruefeZeile`).
   `GetProductionOrdersAtLocationAsync` wird zu: Vereinigung aller `Nummern`, distinct, ordinal sortiert –
   Ergebnis gleich (Texte ohne Nummer liefern nichts; bestehender Test `:20-35` bleibt grün; Wagen-Konflikt
   unverändert; Hinweis H-2 zur SQL-Kollation gilt weiter).
3. **ViewModel** – `OutboundAllViewModel` bekommt `Dictionary<int, FaAmPaar> FaJeArtikel` und
   `Dictionary<string, string> HauptJeSub` (beide leer initialisiert). `StockOverviewItem` bleibt unberührt.
4. **Controller-Ctor** – zusätzlicher Parameter `IServiceSettingRepository serviceSettings` (DI ist
   registriert, viele Controller nutzen ihn). Private Hilfe im Controller (ein Ort für GET und POST):
   `LadeFaAsync(int platz)` → `FaJeArtikel` aus Punkt 2, **null-sicher** als
   `await _stockMovementRepository.GetProductionOrdersByArticleAtLocationAsync(platz) ?? new()` (S-7: ein
   loser Moq liefert für `Task<Dictionary<…>>` `null`, kein leeres Dictionary); wenn `FaBuchungsText.IstHierarchisch(await
   _serviceSettings.GetValueAsync(HierarchischeStrukturKeys.Master))`: alle Nummern sammeln,
   `_productionOrderRepository.GetBySubOrderNumbersAsync(...)` einmal aufrufen und
   `HauptJeSub[SubOrderNumber] = OrderNumber` nur für Sub-FA (`IsSubFa`, Vergleich `OrdinalIgnoreCase`)
   befüllen; sonst `HauptJeSub` leer (AKE).
5. **`OutboundAll` (GET)** – neuer optionaler Parameter `string? productionOrder` (nur für die Rückkehr
   aus Warnfällen) → `vm.ProductionOrder`. Nach `:499` einmal `LadeFaAsync`. Die Wagen-Vorbelegung
   `:501-507` bleibt (Antwort 4) und bildet ihren Text aus `FaJeArtikel.Values.SelectMany(p => p.Nummern)`
   (distinct, ordinal – gleiches Ergebnis wie heute, ein Aufruf weniger); der vorhandene Guard `string.IsNullOrEmpty(vm.ProductionOrder)` (`:502`)
   lässt eine zurückgegebene Eingabe stehen.
6. **View** – `<th>Fertigungsauftrag</th>` vor „Bestand", Zelle je Nummer `nr` bzw. an IDEAL bei Sub-FA
   `nr (Haupt-FA h)` (Formulierung wie `StockApiController.cs:49`), mit „, " getrennt; Zeile
   `data-article-id`, `data-menge` (Bestand `ToString(CultureInfo.InvariantCulture)`, Anzeige bleibt
   de-DE), `data-fa='@Json.Serialize(...)'` als Objekt `{ nummern: [{nr, haupt, zugang}], ohneFa }`
   (haupt = null an AKE; `zugang` = Nummer steht in `ZugangNummern`).
   Fußzeile `colspan="4"`. Neben der FA-Zelle ein Text-Badge (z. B. `badge text-bg-secondary`) „bleibt am
   Wagen“/„bleibt am Platz“ für nicht treffende Zeilen, nur bei Wert im Feld. Inline-Skript (`:117-123`)
   ergänzen: Regel aus Punkt 1 in JS (Zerlegung `/[;,]/`, trim, `toLowerCase`), bei `input` Knopftext,
   `disabled`, Hinweis, Badges und `table-warning` für treffende Zeilen setzen (Farbe nie allein –
   Badge und Zahl tragen die Bedeutung). Leeres Feld → heutiger `confirm()` unverändert. Wert im Feld →
   Bootstrap-Modal nach Anforderung 10, gerendert im View (kein neues Partial, keine neue Bibliothek):
   je gemischter Zeile zwei Radios `name="wahl-<articleId>"` ohne Vorauswahl und je Grund G1–G3 eine
   Textzeile; „Jetzt ausbuchen“ setzt sich sofort `disabled` (S-4) und schreibt vor `submit()` hidden
   inputs ins Formular: je Zeile unter „Wird ausgebucht“ `name="treffendGezeigt" value="<articleId>"`, je
   gemischter Zeile `name="gemischtGezeigt"`, für „Ausbuchen“ zusätzlich `name="gemischtAusbuchen"`, und
   für beide Mengen `name="stueckGezeigt[<articleId>]" value="<data-menge>"`. Vorher angelegte hidden
   inputs werden entfernt (erneutes Öffnen nach Abbrechen erzeugt keine Dubletten). Die Radios selbst
   liegen außerhalb des Formulars. Abbrechen schließt nur das Modal. Leeres Feld: nach „OK“ im `confirm()`
   ebenfalls `disabled` vor `submit()`. `frontend-design`-Skill im Dev-Lauf aufrufen; Optionsfelder mit
   `<label>`, per Tastatur bedienbar, Kontrast AA.
7. **`OutboundAllConfirm` (POST)** – Signatur `OutboundAllConfirm(int storageLocationId, string?
   productionOrder, int[]? treffendGezeigt = null, int[]? gemischtGezeigt = null, int[]? gemischtAusbuchen =
   null, Dictionary<int, string>? stueckGezeigt = null)` (alle neu optional; Form-Binding
   `stueckGezeigt[9]=3` → Dictionary; Wert als `string`, geparst mit `decimal.TryParse(…,
   NumberStyles.Number, CultureInfo.InvariantCulture)`, weil die App auf `de-DE` läuft und ein
   `decimal`-Binding die Kultur der Anfrage nähme). Die bestehenden Aufrufe
   `LagerbuchungFaVerknuepfungTests.cs:109` (`(2, "1065141")`) und `:117` (`(2, "A; B")`) kompilieren weiter,
   würden mit Filter aber an der Prüfung scheitern (fehlende Mengen = leer) → sie übergeben
   `treffendGezeigt: [9], stueckGezeigt: new() { [9] = "3" }`, und der Mock der neuen Repository-Methode
   liefert für Artikel 9 einen Zugang mit `"1065141"` bzw. `"A"` + `"B"` (nicht gemischt). Ihre
   Assertions (`:111`, `:118`) bleiben unverändert.
   `var filter = FaBuchungsText.Nummern(productionOrder).ToHashSet(StringComparer.OrdinalIgnoreCase)`.
   - `filter` leer → Pfad wie heute, unverändert (alle neuen Parameter werden ignoriert).
   - sonst `LadeFaAsync`, je Zeile `PruefeZeile`; `treffendNeu` = Trifft && !gemischt, `gemischtNeu` =
     gemischt (Anforderung 4, G1–G3).
   - keine treffende Zeile → Warnung (Anforderung 6), Redirect `OutboundAll` mit `storageLocationId` und
     `productionOrder`, nichts buchen.
   - Prüfung nach Anforderung 11: `treffendNeu.SetEquals(treffendGezeigt ?? [])`,
     `gemischtNeu.SetEquals(gemischtGezeigt ?? [])`, `(gemischtAusbuchen ?? []).All(gemischtNeu.Contains)`,
     Stückzahl je ID aus `treffendNeu ∪ gemischtNeu` gegen `CurrentQuantity`; eine Abweichung → Warnung
     „hat sich geändert … Nichts ausgebucht.“, Redirect wie oben, nichts buchen.
   - `itemsToOutbound` = `treffendNeu` ∪ (`gemischtNeu` ∩ `gemischtAusbuchen`); leer → Warnung
     „alle mit Nicht ausbuchen“, Redirect wie oben.
   - Buchungsschleife `:534-553` unverändert (Menge = Server-Bestand, Text, FK, Audit, `AddAsync` →
     Sage-Enqueue über den Decorator, kein Bypass) – nur für `itemsToOutbound`, also keine Sage-Meldung
     für „Nicht ausbuchen“ und nicht treffende Zeilen.
   - Erfolgsmeldung nach Anforderung 7; „Wagen“ über `_storageLocationRepository.GetByIdAsync(...)
     ?.IsPickingTransport` (wie GET `:493-495`, `null` → „Platz“). Redirect mit `storageLocationId`.
8. **IDEAL-Einschluss (Antwort 6)** steckt vollständig in `HauptJeSub` (Punkt 4) + `PruefeZeile`
   (Punkt 1); keine zweite Regel, keine Änderung an `FindForBookingAsync`, `GetStockByProductionOrderAsync`
   oder am Wagen-Konflikt. Bewusst Textweg statt `ProductionOrderId` (Umfang 6, Ist-Zustand „Hierarchie“,
   Rückfrage 9): gleiche Treffer wie der FK-Weg plus Listen-Bewegungen; eine Abfrage weniger, weil der
   Bewegungstext ohnehin gelesen wird. `HauptJeSub` wird nur aus `IsSubFa`-Sätzen gebaut; eine eingegebene
   Sub-FA hat dort keinen Eintrag als Haupt-FA und deckt deshalb nur sich selbst ab (Antwort 7).

> [!note]- Lösungsentwurf-Punkte 1, 2, 6, 7 vom 2026-11-20 (Runde 2) – **überholt** durch Runde 3, nur Audit
> 1. `PruefeZeile(IEnumerable<string> zeilenNummern, …)` → `(Trifft, Fremd)`, gemischt ⇔ `Trifft && Fremd.Count > 0`.
> 2. `Task<Dictionary<int, List<string>>>`, „Inhalt = heutiger Rumpf `:518-541`, nur mit `GroupBy(ArticleId)`“
> (mit `ProductionOrder != null`, ohne Bewegungsart).
> 6. hidden inputs nur `gemischtGezeigt`/`gemischtAusbuchen`; `data-fa` als Liste `{nr, haupt}`; keine Knopf-Sperre.
> 7. Signatur `OutboundAllConfirm(int, string?, int[]? gemischtGezeigt = null, int[]? gemischtAusbuchen = null)`;
> „bestehende Testaufrufe kompilieren weiter“ ohne Hinweis, dass sie mit Filter an der neuen Prüfung scheitern.

> [!note]- Lösungsentwurf-Punkte 3–7 vom 2026-11-19 – **überholt** durch Antworten 4–6, nur Audit
> 4. „Verhalten der Vorbelegung bleibt bis zur Antwort auf Rückfrage 4 wie heute“ – jetzt entschieden (bleibt, filtert).
> 5. POST schränkte über `FaJeArtikel[ArticleId].Overlaps(filter)` ein, Redirect nur mit `storageLocationId`.
> 6. View mit `data-fa="a;b"` und einfachem Bestätigungstext.
> 7. **Gemischte Zeilen (Rückfrage 5, Empfehlung a): ganze Zeile ausbuchen; der Dialog nennt Zeilen,
> deren FA-Liste weitere Nummern außer den gefilterten enthält („enthält auch FA …“).** – überholt durch
> Antwort 5 (Ja/Nein je gemischter Zeile).

Ponytail: keine neue Klasse außer dem Daten-Record `FaAmPaar`, kein Service, kein neues Partial, keine
neue Repository-Methode für die Hierarchie (`GetBySubOrderNumbersAsync` wiederverwendet); zweimal `GetCurrentStockAsync` pro Aufruf
(Controller + Repository) wird in Kauf genommen – lagerplatzgefiltert und klein. Erst zusammenlegen,
wenn es messbar stört.

> [!note]- Lösungsentwurf vom 2026-08-05 – **überholt**, nur Audit
> Neue Methode „analog `GetProductionOrdersAtLocationAsync`, gruppiert nach ArticleId" (bleibt, jetzt
> Punkt 2); Feld `ProductionOrders` in `StockOverviewItem` (verworfen – VM-Dictionary statt breit
> genutzter Klasse); Variante A mit `Contains`-Semantik wie `GetStockByProductionOrderAsync`
> (verworfen – exakte Einzelnummer, weil hier tatsächlich ausgebucht wird; Teilstring „123" würde
> „1234567" mit ausbuchen, Hinweis H1 der Teil-1-Prüfung); Variante B Checkboxen (durch Antwort 1 verworfen).

## Migrations-/SQL-Auswirkungen

Keine. Reine Lese-/Filterlogik auf `StockMovement`; keine Migration, kein `SQL/XX`, keine Änderung an
`SQL/00_FreshInstall.sql` oder `SQL/AgentJobs/`.

## Einstellungen

keine (keine neue oder geänderte Einstellung; der vorhandene Master `ProduktionsauftragHierarchisch`
wird nur gelesen).

## Audit-Feld-Auswirkungen

Keine Änderung. Neue Ausbuchungen werden wie heute angelegt (`CreatedAt/CreatedBy/CreatedByWindows`
aus `ICurrentUserService`, `StockMovementsController.cs:544-549`); `StockMovement` wird nicht
aktualisiert, also kein `Modified*`-Pfad.

## Rollen- und Zugriffsfilter-Auswirkungen

Keine. Beide Aktionen bleiben unter `[RequireStockKeyUserAccess]` (admin, stock_keyuser, picking,
`CurrentUserService.cs:134-135`). Keine neue Rolle → `controller.md`/`RoleOverview` unberührt.

## Listen-View-Pattern-Pflichten

`OutboundAll` ist eine unpaginierte Einzelplatz-Ansicht, keine Liste im Sinne von ADR 0005 – bleibt
begründete Ausnahme. Die neue Spalte bringt keine Spaltenfilter-/Pagination-Pflicht (eine Tabelle
über den Bestand **eines** Lagerplatzes).

## Akzeptanzkriterien

1. **Spalte:** Platz L mit Artikel A (eingebucht mit FA `1234567`), Artikel B (ohne FA), Artikel C
   (zweimal eingebucht, FA `111` und `222`). Die Tabelle zeigt A „1234567", B leer, C „111, 222".
2. **Zerlegung:** Eine Bewegung mit Text „A;B" am Paar ergibt in der Spalte „A, B"; Wiederholungen
   erscheinen einmal.
3. **Restbestand (Antwort 3):** Artikel D mit FA `333` eingebucht und ohne FA vollständig ausgebucht
   erscheint nicht in der Tabelle; ein FA-Text eines Artikels ohne Restbestand taucht in keiner Spalte
   auf und wird von keinem Filter getroffen.
4. **Filter (Antwort 1):** FA-Feld `1234567`, bestätigen → nur A ausgebucht (Menge = Platz-Bestand),
   B und C bleiben unverändert. Ausbuchung trägt Text `1234567` und, falls der FA im WMS eindeutig
   ist, `ProductionOrderId`.
5. **Exakt:** FA-Feld `123` trifft A (`1234567`) **nicht**; Groß-/Kleinschreibung egal, Leerzeichen
   um Nummern egal.
6. **Liste im Feld:** FA-Feld `111; 1234567` → A und C ausgebucht, B bleibt; Ausbuchungen ohne
   Verknüpfung (Liste, wie v1.87.0).
7. **Kein Treffer:** FA-Feld `999` → nichts gebucht, Warnmeldung nach Anforderung 6, Seite zeigt
   weiter Platz L mit `999` im Feld; der Knopf war schon vor dem Absenden deaktiviert mit Hinweis.
8. **Leer = wie heute (Regression):** FA-Feld leer → alle Artikel mit Bestand > 0 ausgebucht,
   Meldung und Redirect wie bisher.
9. **Rückmeldung:** nach gefilterter Ausbuchung nennt die Erfolgsmeldung Anzahl, FA sowie Anzahl und
   Artikelnummern der verbleibenden Artikel mit „bleibt am Platz“; die Seite zeigt Platz L mit dem Rest.
10. **Vorschau:** Knopftext „Alle N Artikel ausbuchen" (leer) bzw. „M von N Artikeln ausbuchen (FA …)";
    nicht treffende Zeilen tragen bei Wert den Text „bleibt am Platz“ (Wagen: „bleibt am Wagen“).
11. **Gemischte Zeile – Ja (Antwort 5):** FA-Feld `111` → Dialog zeigt C unter „Bitte entscheiden“ mit
    „enthält auch FA 222“, keine Option vorausgewählt, „Jetzt ausbuchen“ gesperrt mit „Noch 1 Zeile
    offen“; „Ausbuchen“ wählen, bestätigen → C mit vollem Platz-Bestand ausgebucht.
12. **Gemischte Zeile – Nein:** wie 11, aber „Nicht ausbuchen“ und eine weitere treffende Zeile
    vorhanden → nur die andere Zeile ausgebucht, C bleibt, Erfolgsmeldung nennt C als „bleibt am Platz“.
    Ist C die einzige treffende Zeile, ist „Jetzt ausbuchen“ nach „Nicht ausbuchen“ gesperrt; per
    manipuliertem POST → Warnung „alle gemischten Zeilen mit ‚Nicht ausbuchen‘“, nichts gebucht.
13. **Abbruch:** Dialog mit „Abbrechen“, Schließen-Kreuz oder Escape verlassen → kein POST, keine
    Bewegung, kein Queue-Eintrag; Feld und Seite unverändert.
14. **Serverprüfung (Antworten 5 und 8):** Jeder der folgenden POSTs mit Filter `111` (Ausgangslage
    173.4: N treffend mit 2, C gemischt mit 3) ergibt die Warnung „Bestand oder FA-Zuordnung auf L hat
    sich geändert – bitte Auswahl erneut prüfen. Nichts ausgebucht.“, **keine** Bewegung, **keinen**
    Queue-Eintrag und die Seite L mit `111` im Feld:
    (a) ohne `treffendGezeigt`/`gemischtGezeigt`;
    (b) `gemischtAusbuchen` enthält eine ID, die nicht in `gemischtGezeigt` steht;
    (c) **neu treffende Zeile:** zwischen Seitenaufruf und POST wird ein Artikel R mit FA `111` auf L
    eingebucht (R treffend, nicht gemischt, nicht in `treffendGezeigt`);
    (d) **zusätzlicher fremder FA:** N bekommt zwischen Seite und POST eine Einbuchung mit FA `444` (N ist
    jetzt gemischt, steht aber in `treffendGezeigt`); ebenso eine Einbuchung ohne FA bei N (G2);
    (e) **geänderte Stückzahl:** zwischen Seite und POST wird 1 Stk von C einzeln ausgebucht (gezeigt 3,
    Server 2); ebenso ein manipuliertes `stueckGezeigt[N]` ≠ 2 oder ein fehlender/nicht lesbarer Wert;
    (f) C wird zwischenzeitlich vollständig ausgebucht (fällt aus `gemischtGezeigt`).
    (g) Positiv: unveränderter Stand → Buchung; gebuchte Menge = Server-Bestand = angezeigte Stückzahl;
    `stueckGezeigt` wird nur verglichen, nie als Buchungsmenge verwendet. Invariante Schreibweise: ein
    Bestand von 2,5 wird als `2.5` gesendet und auf DEV (de-DE) korrekt als gleich erkannt.
15. **Wagen-Konflikt unverändert:** `GetProductionOrdersAtLocationAsync` liefert für denselben Platz
    dasselbe wie vorher (bestehender Test grün); Konflikt-Dialog der Kommissionierung unverändert.
16. **Wagen-Vorbelegung (Antwort 4):** Wagen mit Artikeln von FA A und B plus einem Artikel ohne FA →
    Feld vorbelegt „A; B", Knopf zeigt „2 von 3", die Zeile ohne FA trägt „bleibt am Wagen“, der Dialog
    listet sie unter „Bleibt am Wagen“ und hat keinen Block „Bitte entscheiden“; nach Bestätigen bleibt
    der Artikel auf dem Wagen und die Erfolgsmeldung nennt ihn mit „bleibt am Wagen“.
17. **Wagen, Feld geleert:** Vorbelegung gelöscht → Knopf „Alle 3 Artikel ausbuchen“, `confirm()` wie
    heute, alles ausgebucht ohne FA-Text (TS-2.4 unverändert für diesen Weg).
18. **IDEAL Haupt-FA (Antwort 6):** Master `ProduktionsauftragHierarchisch` = true; FA H (Haupt) mit
    Sub-FA S1 (Ebene 1) und S2 (Ebene 2, Vater S1), alle `OrderNumber` = H. Platz L: Artikel E
    eingebucht mit FA-Text `S1`, F mit `S2`, G mit `H`, K mit `S1` und `X` (fremde Struktur) – alle Zugänge
    mit FA-Text, kein Zugang ohne FA (sonst G2). Spalte zeigt E „S1 (Haupt-FA H)“, F „S2 (Haupt-FA H)“,
    G „H“. FA-Feld `H` → E, F, G treffen ohne Rückfrage; K steht unter „Bitte entscheiden“ mit „enthält
    auch FA X“. Ausbuchungen tragen Text `H`; `ProductionOrderId` = Haupt-FA **falls im WMS eindeutig**
    (`FindForBookingAsync("H")` liefert `Gefunden`: genau ein Satz mit `SubOrderNumber = H` oder genau eine
    Haupt-FA mit `OrderNumber = H`, `ProductionOrderRepository.cs:229-237`), sonst `null` (S-6, wie AK 4).
19. **IDEAL Sub-FA eingegeben (Antwort 7):** gleiche Daten, FA-Feld `S1` → genau E und K treffen
    (K gemischt wegen X); F (`S2`, Kind von S1) trifft **nicht** – eine eingegebene Sub-FA schließt ihre
    Nachfahren nicht ein; G (`H`) trifft ebenfalls **nicht** – eine Sub-FA schließt auch ihre Haupt-FA
    nicht ein. Spalte und Knopf („2 von 4“) entsprechend.
20. **AKE unverändert (Antwort 6, negativ):** Master false/leer; Platz mit Bewegung FA-Text `S1`, und in
    den `ProductionOrders` existiert (Testdaten) ein Satz `OrderNumber = H`, `SubOrderNumber = S1`. FA-Feld
    `H` → kein Treffer, Warnung nach Anforderung 6; `GetBySubOrderNumbersAsync` wird nicht aufgerufen.
21. **IDEAL ohne Sub-FA-Satz (negativ):** Bewegungstext `S9`, zu dem kein `ProductionOrder` existiert →
    `H` trifft die Zeile nicht; Spalte zeigt nur „S9“.
22. **Sage:** Je ausgebuchter Zeile genau eine Ausbuchung über `AddAsync`; nicht ausgebuchte Zeilen
    (nicht treffend, „Nicht ausbuchen“, Abbruch, Warnfälle) erzeugen keinen Queue-Eintrag (Regel des
    Decorators unverändert).
23. **Tests:** Repository-Tests für `GetProductionOrdersByArticleAtLocationAsync` (AK 1–3, 24: Zugang
    vs. Ausbuchung, Zugang ohne Text inkl. leerem String, Umbuchungs-Ziel zählt als Zugang, Umbuchungs-Quelle
    wird nicht gelesen); Unit-Tests `FaBuchungsText.PruefeZeile` (exakt, case, Haupt→Sub jede Ebene, Sub
    nur selbst, AKE leeres Mapping, G1/G2/G3 einzeln und kombiniert, `paar == null`, AK 18–21, 24);
    Controller-Tests für AK 4–8, 11, 12, 14 (a)–(g), 16, 18, 20, 24; `LagerbuchungFaVerknuepfungTests` und
    `StockMovementsControllerBulkInboundTests` an den neuen Ctor angepasst (Mock `IServiceSettingRepository`);
    in `LagerbuchungFaVerknuepfungTests` zusätzlich Mock der neuen Repository-Methode und Aufrufe `:109`/`:117`
    mit `treffendGezeigt`/`stueckGezeigt` (Lösungsentwurf 7), Assertions unverändert und grün. Ein Test
    deckt S-7 ab: Repository-Mock ohne Setup (liefert `null`) → kein Absturz, Warnung „Kein Artikel …“.
24. **Bestand ohne FA in einer FA-Zeile (B-3 (a)):** Platz P, Artikel Y: Wareneingang 100 Stk ohne FA
    (Wareneingang oder Einbuchung mit leerem Feld), danach Einzel-Ausbuchung 5 Stk mit FA `111`. Spalte bei
    Y „111“, Bestand 95. FA-Feld `111` → Y steht unter „Bitte entscheiden“ mit „enthält auch Bestand ohne
    FA“ und „FA 111 nur aus Ausbuchung“, nichts vorausgewählt. „Nicht ausbuchen“ → Y bleibt mit 95,
    Meldung „bleibt am Platz“ (bzw. „Jetzt ausbuchen“ gesperrt, wenn Y die einzige Zeile ist);
    „Ausbuchen“ → 95 ausgebucht. Zweiter Artikel Z: Einbuchung 10 mit FA `111` + Einbuchung 5 ohne FA →
    gemischt nur mit „enthält auch Bestand ohne FA“. Gegenprobe: Artikel A (nur Zugang mit FA, AK 1) bleibt
    nicht gemischt.
25. **Doppel-Absenden (S-4):** Nach dem ersten Klick auf „Jetzt ausbuchen“ (bzw. „OK“ im `confirm()` bei
    leerem Feld) ist der Knopf deaktiviert; ein schneller Doppelklick erzeugt genau einen POST (Netzwerk-Tab)
    und je Zeile genau eine Ausbuchung.
26. **TS-2.4 neu gefasst (S-5):** `docs/TESTSZENARIEN.md` TS-2.4 beschreibt den Wagen mit vorbelegtem Feld,
    Knopf „N von M Artikeln ausbuchen (FA …)“, Modal statt `confirm()` und „bleibt am Wagen“ für Artikel ohne
    FA, plus den Weg „Feld leeren = alles wie bisher“ (Wortlaut im Abschnitt Test-Szenarien).

> [!note]- AK 9–15 vom 2026-11-19 – **überholt** durch Antworten 4–6, nur Audit
> 10 „Bestätigungsdialog listet bei Filter die Artikelnummern“; **11 „Gemischte Zeile (vorbehaltlich
> Rückfrage 5, Empfehlung a): FA-Feld `111` → C wird mit vollem Platz-Bestand ausgebucht; der Dialog nennt
> ‚C enthält auch FA 222‘“**; 13 „Wagen-Vorbelegung (vorbehaltlich Rückfrage 4, Empfehlung a)“; 12/14/15
> inhaltlich übernommen als 15/22/23.

> [!note]- AK 14, 18, 19, 23 vom 2026-11-20 (Runde 2) – **überholt** durch Runde 3, nur Audit
> 14 prüfte nur `gemischtGezeigt`/`gemischtAusbuchen`; „(d) Gebuchte Stückzahl ist immer der Server-Bestand
> beim Absenden, nie ein Wert aus dem Formular“ ohne Stückzahlvergleich. 18 „Ausbuchungen tragen Text `H`
> und `ProductionOrderId` der Haupt-FA“ (ohne „falls eindeutig“). 19 „nur E und K treffen (Text), F (S2)
> nicht – vorbehaltlich Rückfrage 7“. 23 ohne G2/G3-Fälle und ohne Anpassung der Testaufrufe.

## Test-Szenarien

Neues Szenario **TS-173** in `docs/TESTSZENARIEN.md` (Lagerplatz ausbuchen mit FA) und **TS-2.4
inhaltlich neu gefasst** (S-5, Wortlaut unten; der frühere Plan „nur Verweis aus TS-2.4“ ist **überholt**);
danach `secondbrain/tests/testszenarien-index.md` nachziehen. Die frühere Nummer TS-169 ist
**überholt** (seit v1.99.45 BF-Warenkorb, `docs/TESTSZENARIEN.md:13351`). Standort: **AKE und IDEAL** –
die Seite hat keinen eigenen Standortschalter, der IDEAL-Einschluss hängt am Master (173.10–173.13).

- **Vorbedingungen:** Rolle `stock_keyuser`. Platz L: A eingebucht 5 mit FA `1234567`; B 3 ohne FA;
  C 2 mit FA `111` + 1 mit FA `222`; D 4 mit FA `333` eingebucht, danach ohne FA 4 ausgebucht;
  N 2 mit FA `111` (für 173.5). Für 173.8 ein Wagen W mit Artikeln von FA A und B (Kommissionierung)
  plus ein Artikel ohne FA (manuelle Umbuchung). Für 173.10–173.12 auf **DEV IDEAL** eine Struktur H mit
  Sub-FA S1 und darunter S2 (Abfrage: `SELECT OrderNumber, SubOrderNumber, ParentSubOrderNumber FROM
  dbo.ProductionOrders WHERE OrderNumber = '<H>'`) und Platz M mit E (eingebucht mit FA-Text S1),
  F (S2), G (H), K (S1 + FA einer anderen Struktur X) – alle Zugänge mit FA-Text. Für 173.15 Platz P:
  Y 100 per Wareneingang bzw. Einbuchung ohne FA, danach Einzel-Ausbuchung 5 mit FA `111`; Z 10 mit FA
  `111` + 5 ohne FA eingebucht. Alle Zugänge auf L mit FA-Text (außer B), damit dort nur G1 wirkt.
  Sage-Lagerbuchung auf DEV aus oder L/M/P ohne `SageBuchungErlaubt`, außer bei 173.9.
- **Schritte / erwartet (DEV AKE und DEV IDEAL, sofern nicht anders genannt):**
  - 173.1 `/StockMovements/OutboundAll`, Platz L laden → Spalte „Fertigungsauftrag": A `1234567`, B leer,
    C `111, 222`, N `111`; D fehlt.
  - 173.2 Feld `1234567` tippen → Knopf „1 von 4 Artikeln ausbuchen (FA 1234567)", A markiert, B/C/N
    tragen „bleibt am Platz“; Dialog ohne Block „Bitte entscheiden“; bestätigen → nur A weg, Meldung nennt
    3 verbleibende mit Artikelnummern, Seite zeigt L.
  - 173.3 Feld `123` → Knopf deaktiviert, Hinweis „Kein Artikel gehört zu diesem FA"; per manipuliertem
    POST trotzdem senden → Warnung, nichts gebucht, Feld zeigt `123`.
  - 173.4 Feld `111` → Knopf „2 von 3“; Dialog: N unter „Wird ausgebucht“, C unter „Bitte entscheiden“
    mit „enthält auch FA 222“, nichts vorausgewählt, „Jetzt ausbuchen“ gesperrt („Noch 1 Zeile offen“).
    „Abbrechen“ → nichts gebucht, Seite unverändert (Bewegungshistorie prüfen). Erneut öffnen, Escape →
    dito.
  - 173.5 Feld `111`, C „Nicht ausbuchen“ → nur N ausgebucht, Meldung nennt C „bleibt am Platz“.
    Danach Feld `111` erneut: C ist jetzt einzige treffende Zeile; „Nicht ausbuchen“ → „Jetzt
    ausbuchen“ gesperrt; „Ausbuchen“ → C komplett (3) ausgebucht.
  - 173.6 Feld `333` → kein Treffer (D ohne Restbestand).
  - 173.7 **Serverprüfung (negativ):** Dialog für C offen lassen, in zweitem Tab C komplett ausbuchen
    (Feld leer wäre alles – stattdessen Einzel-Ausbuchung), im ersten Tab „Ausbuchen“ + bestätigen →
    Warnung „hat sich geändert … Nichts ausgebucht.“. Zusätzlich per Browser-Werkzeug ein
    `gemischtAusbuchen` mit fremder Artikel-ID einschleusen → dieselbe Warnung, nichts gebucht.
    Weitere Negativfälle (jeweils Seite mit Feld `111` öffnen, Dialog offen lassen, im zweiten Tab
    ändern, im ersten Tab bestätigen → Warnung „hat sich geändert … Nichts ausgebucht.“, Seite neu
    geladen, Bewegungshistorie ohne neue Ausbuchung):
    - 173.7a **neu treffende Zeile:** Artikel R mit FA `111` auf L einbuchen;
    - 173.7b **zusätzlicher fremder FA:** bei N eine Einbuchung mit FA `444` (N wird gemischt);
    - 173.7c **geänderte Stückzahl:** 1 Stk von C einzeln ausbuchen (ohne FA); zusätzlich per
      Browser-Werkzeug `stueckGezeigt[<N>]` auf einen anderen Wert setzen → dieselbe Warnung.
    - Danach Seite neu laden, unverändert bestätigen → Buchung läuft (Positivkontrolle).
  - 173.8 **Wagen** (Antwort 4): Aufruf über den Konflikt-Dialog der Kommissionierung → Feld vorbelegt
    „A; B“, Knopf „2 von 3“, Artikel ohne FA trägt „bleibt am Wagen“, Dialog listet ihn unter „Bleibt am
    Wagen“; bestätigen → Artikel bleibt auf W, Meldung „… bleibt am Wagen“. Danach Feld leeren →
    „Alle 1 Artikel ausbuchen“ mit `confirm()` wie TS-2.4.
  - 173.9 **Sage** (nur wenn auf DEV aktiv): je ausgebuchter Zeile ein Queue-Eintrag; für „Nicht
    ausbuchen“, nicht treffende Zeilen, Abbruch und Warnfälle keiner.
  - 173.10 **IDEAL Haupt-FA:** Platz M, Feld `H` → E, F, G unter „Wird ausgebucht“, K unter „Bitte
    entscheiden“ („enthält auch FA X“); Spalte zeigt bei E „S1 (Haupt-FA H)“. „Ausbuchen“ für K →
    E, F, G, K weg; Bewegungshistorie: Text `H`, verknüpft mit der Haupt-FA.
  - 173.11 **IDEAL Sub-FA eingegeben (Antwort 7):** neu befüllen, Feld `S1` → genau E und K treffen
    (K unter „Bitte entscheiden“ wegen X); F (`S2`, Kind von S1) trägt „bleibt am Platz“ – keine
    Nachfahren; G (`H`) trägt ebenfalls „bleibt am Platz“ – keine Haupt-FA. Knopf „2 von 4“.
  - 173.12 **IDEAL ohne Sub-FA-Satz (negativ):** Bewegung mit FA-Text `S9`, für den kein FA existiert →
    Feld `H` trifft diese Zeile nicht.
  - 173.13 **AKE (negativ):** auf DEV AKE (Master aus) Feld mit der Haupt-FA-Nummer eines Testsatzes,
    dessen Bewegung eine andere Nummer trägt → kein Treffer; Verhalten = reiner Textvergleich.
  - 173.14 **Regression:** Feld leer → alle restlichen Artikel ausgebucht, Verhalten wie TS-2.4 Schritt 7.
  - 173.15 **Bestand ohne FA (B-3 (a)):** Platz P, Spalte Y „111“ (Bestand 95), Z „111“. Feld `111` →
    beide unter „Bitte entscheiden“; Y mit „enthält auch Bestand ohne FA“ und „FA 111 nur aus
    Ausbuchung“, Z nur mit „enthält auch Bestand ohne FA“; nichts vorausgewählt. Y „Nicht ausbuchen“,
    Z „Ausbuchen“ → Z (15) ausgebucht, Y bleibt mit 95, Meldung „bleibt am Platz“ nennt Y. Feld `111`
    erneut, Y „Ausbuchen“ → 95 ausgebucht.
  - 173.16 **Doppel-Absenden (S-4):** Feld `1234567` (neu befüllt), Dialog, „Jetzt ausbuchen“ schnell
    doppelt klicken → Knopf sofort deaktiviert, genau ein POST (Browser-Netzwerk-Tab), genau eine
    Ausbuchung. Dasselbe mit leerem Feld nach „OK“ im `confirm()`.
- **Negativfälle:** 173.3, 173.4 (Abbruch), 173.6, 173.7 inkl. 7a–7c, 173.11 (F/G), 173.12, 173.13,
  173.16; Rolle `stock` ohne `stock_keyuser` → Zugriff verweigert (wie TS-2.4).
- **TS-2.4 neu (S-5)** – ersetzt in `docs/TESTSZENARIEN.md:272-289` Vorbedingungen, Schritte und
  Erwartung; Negativfall (Rolle `stock`) bleibt. Der alte Schritt „Lagerplatz `WAGEN-01` auswählen“ ist
  schon heute nicht ausführbar, weil Wagen nicht in der Platzauswahl stehen
  (`StorageLocationRepository.cs:43-49`), deshalb Aufruf über Konflikt-Dialog oder URL:
  - *Vorbedingungen:* Rolle `stock_keyuser` oder `admin`. Wagen `WAGEN-01` mit Artikeln aus der
    Kommissionierung für FA A (mit FA-Text) und einem Artikel ohne FA (manuelle Umbuchung mit leerem
    FA-Feld).
  - *Schritte:* 1. `/StockMovements/OutboundAll?storageLocationId=<Id WAGEN-01>` öffnen (oder über den
    Konflikt-Dialog der Kommissionierung). 2. Feld „Fertigungsauftrag“ ist mit „A“ vorbelegt; Knopf
    „1 von 2 Artikeln ausbuchen (FA A)“, Artikel ohne FA trägt „bleibt am Wagen“. 3. Knopf klicken →
    Modal: Artikel von A unter „Wird ausgebucht“, Artikel ohne FA unter „Bleibt am Wagen“. 4. „Jetzt
    ausbuchen“. 5. Meldung prüfen. 6. Seite zeigt den Wagen mit dem Restartikel (Feld leer, weil der Rest
    keinen FA trägt, H-5). 7. „Alle 1 Artikel ausbuchen“ → `confirm()` bestätigen.
  - *Erwartet:* nach 4 nur die Artikel von A ausgebucht (Text „A“), Erfolgsmeldung nennt den Restartikel
    mit „bleibt am Wagen“; nach 7 ist der Wagen leer (Bestand 0), Ausbuchung ohne FA-Text.
- **Automatisch:** `StockMovementRepositoryProductionOrderTests` (neue Fälle je Artikel + bestehender
  Zerlegungstest), Unit-Tests `FaBuchungsText.PruefeZeile`, Controller-Tests zu `OutboundAllConfirm`
  (AK 23).

## Etappen (nur bei epic: true)

Kein Epic – ein Dev-Lauf, ein Commit. Reihenfolge im Lauf: Helfer + Repository + Tests → Controller
+ Tests → View/JS → Doku/Version.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (epic: false) | – | – |

## Abgleich mit der Kritischen Prüfung vom 2026-08-05

Der Prüfabschnitt unten bleibt unverändert. Stand der Merkposten:
- **Kern-Entscheidung A/B:** erledigt durch Antwort 1 (A).
- **Mehrere FA je Zeile:** erledigt durch Antwort 2 (alle, kommagetrennt).
- **Konsistenz mit Teil 1:** erledigt durch Antwort 3; Teil 1 ist gemerged (`onlyActualStock`).
  Abweichung bewusst: exakte Einzelnummer statt Teilstring, weil hier gebucht wird (siehe Entwurf).
- **Sage-Enqueue:** weiter gültig und eingehalten – Buchung bleibt über `AddAsync` des Decorators
  (AK 14). Neu sichtbar dadurch: gemischte Zeilen melden auch fremde FA-Anteile an Sage → Rückfrage 5.
  *Nachtrag 2026-11-20:* durch Antwort 5 gelöst – gemischte Zeilen gehen nur nach „Ausbuchen“ an Sage
  (heute AK 22); die AK-Nummer 14 oben bezieht sich auf die alte Zählung.
- **Empfehlung „normale /review-Runde nachholen":** steht noch aus (vor Schranke 1).
  *Nachtrag 2026-11-20 (Runde 3):* Die Kritische Prüfung 2026-11-20 hat stattgefunden; B-1/B-2/B-3 sind
  durch „Freigabe-Antworten Runde 3“ entschieden und eingearbeitet (Anforderungen 3, 4, 11; AK 14, 19, 24),
  S-1 als Rückfrage 9, S-2 bis S-7 im Rumpf (Anforderung 1, 10, 11; AK 18, 25, 26; Lösungsentwurf 4, 7),
  H-1 markiert. Eine erneute Prüfung des überarbeiteten Rumpfs ist Sache des Menschen.

## Deploy

**Vorläufig – der QA-Lauf bestätigt gegen den echten Diff.**

- **Version:** Vorschlag 1.99.46 – am 2026-11-20 geprüft: Worktree auf 1.99.45
  (`IdealAkeWms/AppVersion.cs:5`), „1.99.46“ kommt im Worktree nirgends vor; die Specs, die 1.99.45–1.99.47
  vorgeschlagen hatten (Updater, BF-Warenkorb, Dashboard), sind im Sammellauf v1.99.45 aufgegangen;
  1.99.47 (Sage-AG-Bugfix) und 1.99.48 (BDE-Liste) zählen 1.99.46 bereits dieser Spec zu. Dev-Lauf prüft
  erneut; beide `AppVersion.cs` + Anwender-Changelog + Brain-Changelog.
- **Testszenario:** TS-173 (siehe Test-Szenarien).
- **Web-App:** ja (Controller, Repository, ViewModel, View).
- **Service:** nein (nur Versionsnummer, falls im Bündel üblich mitgezogen).
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): kein SQL, keine Einstellung, kein
  Leitstand-Schritt, keine Ordner/Rechte. Weil sich das Verhalten am Kommissionierwagen sichtbar ändert
  (Artikel ohne FA bleiben am Wagen), ein Info-Eintrag:
  `@{ Id='v1.99.46-LagerplatzAusbuchenFaFilter'; Version='1.99.46'; Typ='Info'; Firma='beide';
  Phase='Nachher'; Ausfuehrung='anzeigen'; Text='Lagermitarbeiter informieren: Lagerplatz ausbuchen
  bucht bei FA-Eintrag nur noch Artikel dieses FA aus; am Kommissionierwagen bleiben Artikel ohne
  FA-Vermerk am Wagen (Feld leeren = alles). Gemischte Artikel (weitere FA, Bestand ohne FA oder FA nur
  aus einer Ausbuchung) werden im Dialog einzeln abgefragt; aendert sich der Bestand zwischen Anzeige und
  Bestaetigen, wird nichts gebucht und die Seite neu geladen. IDEAL: Haupt-FA schliesst ihre Sub-FA ein,
  eine Sub-FA nur sich selbst.'; Quelle='<Brain-Changelog-Slug v1.99.46>' }` (Schreibweise
  wie `nachlauf.psd1:337-339`; Version im Dev-Lauf an die endgültige Nummer anpassen).
- **Publish-Befehle:**

```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

## Offene Rückfragen

1. **ENTSCHIEDEN (Antwort 1):** Variante A – das FA-Feld filtert.
2. **ENTSCHIEDEN (Antwort 2):** alle FA kommagetrennt in der FA-Spalte.
3. **ENTSCHIEDEN (Antwort 3):** wie Teil 1 – echter Bestand als Grundlage, FA nur aus Bewegungen
   mit Restbestand.

> [!note] Stand 2026-11-20: Rückfragen 4–6 sind durch „Freigabe-Antworten Runde 2“ **entschieden**
> (4: Vorbelegung bleibt und filtert, „bleibt am Wagen“ melden; 5: Ja/Nein je gemischter Zeile;
> 6: IDEAL Haupt-FA schließt Sub-FA ein, AKE Textvergleich). Die Fragetexte unten bleiben als Audit;
> die darin genannten Empfehlungen sind, wo abweichend entschieden (5 und 6), **überholt**.

4. **Kommissionierwagen-Vorbelegung:** Auf einem Wagen ist das FA-Feld heute automatisch mit allen
   FA des Wagens vorbelegt (`StockMovementsController.cs:501-507`). Mit Antwort 1 wirkt das als
   Filter: Artikel **ohne** FA-Text bleiben auf dem Wagen, heute wird alles ausgebucht (TS-2.4).
   (a) so lassen, Restartikel sichtbar melden (**Empfehlung** – Wagen werden über die Kommissionierung
   in der Regel mit FA-Text befüllt, `PickingTransferService.cs:75,210-218`; ohne FA am
   Kommissionierauftrag ist der Text leer – wie oft das vorkommt, ist ungeprüft; Text und Verknüpfung der Ausbuchung bleiben
   erhalten), (b) auf Wagen nicht mehr vorbelegen (leer = alles, Ausbuchung ohne FA-Text, TS-115.12 und
   130.5.6 ändern sich), (c) unveränderte Vorbelegung gilt als „kein Filter" (verdeckte Sonderregel)?
5. **Gemischte Zeile:** Trägt ein Artikel am Platz FA X und Y und gefiltert wird nach X, wird – da
   Bestand nicht FA-genau ist (Antwort 3) – der **ganze** Platz-Bestand ausgebucht, inklusive des
   Anteils von Y, mit Sage-Meldung. (a) ganze Zeile ausbuchen, im Bestätigungsdialog benennen
   (**Empfehlung**), (b) gemischte Zeilen beim Filter auslassen und melden?
6. **IDEAL Haupt-/Sub-FA:** Der Filter vergleicht den FA-**Text** der Bewegungen (wie Teil 1). Wer auf
   IDEAL die Haupt-FA eingibt, trifft keine Bewegungen, die mit der Sub-FA-Nummer (BelID) getaggt sind
   (Auflösung beider Formen nur in `FindForBookingAsync`, Test `LagerbuchungFaVerknuepfungTests.cs:29-47`).
   Reicht der Textvergleich (**Empfehlung**, ein Ort, wie Teil 1), oder sollen auf IDEAL zusätzlich die
   über `ProductionOrderId` verknüpften Sub-FA einer eingegebenen Haupt-FA mitzählen?
7. **Mehrstufige IDEAL-Struktur:** Antwort 6 nennt nur die Haupt-FA. Umgesetzt wird: eine eingegebene
   Haupt-FA schließt **alle** Sub-FA ihrer Struktur ein (gleiche `OrderNumber`, jede Ebene,
   `FaMaterializationSyncService.cs:151`). Eine eingegebene Sub-FA der mittleren Ebene trifft nur ihre
   eigene Nummer, nicht die darunter hängenden Sub-FA (`ParentSubOrderNumber`, AK 19 / TS-173.11).
   Passt das, oder soll auch eine eingegebene Sub-FA ihre Nachfahren einschließen (dann rekursiv über
   `ParentSubOrderNumber`, mehr Code)?
8. **„Gegen die neu ermittelte Menge“ (Antwort 5):** Umgesetzt wird: der Server ermittelt die
   **Menge der gemischten Zeilen** neu und bucht bei Abweichung zur angezeigten nichts (Anforderung 11);
   die gebuchte **Stückzahl** ist wie heute der Platz-Bestand beim Absenden. Reicht das, oder soll
   zusätzlich eine zwischenzeitlich geänderte Stückzahl einer gemischten Zeile die Buchung stoppen
   (angezeigte Stückzahl würde mitgesendet und verglichen)?

> [!note] Stand 2026-11-20 nachmittags: Rückfragen 7 und 8 sowie Review-Befund B-3 sind durch
> „Freigabe-Antworten Runde 3“ **entschieden** (7: Sub-FA nur selbst; 8: `treffendGezeigt` +
> `gemischtGezeigt` per `SetEquals` plus Stückzahl je Zeile; B-3 (a): Zugang ohne FA bzw. FA nur aus
> Ausbuchung = gemischt). Die Fragetexte 7/8 oben bleiben als Audit; die darin beschriebene
> Teil-Prüfung (nur gemischte Zeilen, Stückzahl nie verglichen) ist **überholt**. Offen ist nur Rückfrage 9.

9. **Textweg statt `ProductionOrderId` (Review S-1):** Antwort 6 nennt „über ProductionOrderId/Hierarchie“.
   Umgesetzt wird: FA-**Text** der Bewegung → `SubOrderNumber` → `OrderNumber` (Ist-Zustand „Hierarchie“,
   Lösungsentwurf 8). Grund: jede Bewegung mit `ProductionOrderId` trägt dieselbe Nummer auch im Text
   (alle Schreibwege belegt, Kritische Prüfung 2026-11-20 Abschnitt 2b); der Textweg trifft also alles,
   was der FK-Weg trifft, und zusätzlich Listen-Bewegungen („S1;X“, FK `null`). Bestätigt (ja), oder
   sollen ausschließlich über `ProductionOrderId` verknüpfte Bewegungen den Haupt-FA-Einschluss auslösen
   (Listen-Bewegungen träfen dann nur per exaktem Text)?

## Freigabe-Antworten (Mensch füllt aus — Schranke 1)

1. → **A: das FA-Feld filtert** – nur Artikel, die laut Bewegungen zu diesem FA gehören, werden ausgebucht (Gerald 2026-11-19 20:10)
2. → **alle FA kommagetrennt** in der FA-Spalte (Gerald 2026-11-19 20:10)
3. → **wie Teil 1**: echter Bestand als Grundlage, FA nur aus Bewegungen mit Restbestand (Gerald 2026-11-19 20:10)

klären wir später - bitte im backlog belassen. *(**überholt** – Vertagung vom 2026-08-05, aufgehoben durch die Wiederaufnahme Gerald 2026-11-19 20:10 mit den Antworten 1–3 oben; Kennzeichnung laut Review H-1 auf Auftrag des Menschen, Text nicht gelöscht)*

## Freigabe-Antworten Runde 2 (Rückfragen 4–6, Mensch füllt aus — Schranke 1)

4. → **(a) Vorbelegung bleibt und filtert, Artikel ohne FA-Vermerk sichtbar als „bleibt am Wagen“ melden** (Gerald 2026-11-19 20:40)
5. → **abweichend: je gemischter Zeile im Bestätigungsdialog nachfragen (ausbuchen ja/nein), kein stilles Ausbuchen und kein stilles Auslassen** (Gerald 2026-11-19 20:40)
6. → **abweichend: an IDEAL schließt eine eingegebene Haupt-FA die verknüpften Sub-FA ein (über ProductionOrderId/Hierarchie), AKE unverändert Textvergleich** (Gerald 2026-11-19 20:40)

## Freigabe-Antworten Runde 3 (Rückfragen 7–8, Mensch füllt aus — Schranke 1)

7. → **eingegebene Sub-FA trifft nur ihre eigene Nummer** (keine Nachfahren); Haupt-FA schließt alle Sub-FA ein (Gerald 2026-11-20 12:50)
8. → **Server prüft beim Absenden die gezeigten treffenden und gemischten Zeilen (ID-Mengen, `SetEquals`) und die Stückzahl je Zeile; bei jeder Abweichung wird nichts gebucht, Seite neu laden** – deckt Review S-3 mit ab (Gerald 2026-11-20 12:50)
B-3 (Kritische Prüfung 2026-11-20) → **(a) Zeile mit Bestand ohne FA (Zugang ohne FA-Text) bzw. deren FA nur aus einer Ausbuchung stammt, gilt als gemischt und wird im Bestätigungsdialog ja/nein abgefragt** (Gerald 2026-11-20 12:50)

## Freigabe-Antworten Runde 4 (Rückfrage 9, Mensch füllt aus — Schranke 1)

9. →

## Kritische Pruefung (2026-08-05)

**ZURUECKGESTELLT — keine Freigabe-Pruefung durchgefuehrt.** Der Mensch hat diese Anforderung
bewusst vertagt („klären wir später - bitte im backlog belassen", Command-Notiz „wir werden diese
anforderung später angehen"). Die drei Freigabe-Antworten sind **unbeantwortet** (nur Pfeile) — eine
Anwalt-des-Teufels-Freigabe-Pruefung waere gegenstandslos, weil es noch keine Entscheidungen zu
pruefen gibt. Die Spec bleibt als Entwurf in `specs/entwurf/` liegen und wird **nicht** finalisiert.

**Damit beim spaeteren Wiederaufgreifen nichts verloren geht** (kein Ersatz fuer eine echte Pruefung,
nur ein Merkzettel):
- **Kern-Entscheidung zuerst (open_question 1):** Was heisst „Aktivierung von FA-Ausbuchung"? —
  Variante **A** (das FA-Textfeld auf `OutboundAllConfirm` wirkt als **echter Filter**: nur die zu
  dieser FA gehoerenden Artikel werden ausgebucht, statt heute ALLE am Platz) vs. Variante **B**
  (Checkbox/Auswahl **je Zeile**). Ohne diese Wahl ist der Fix nicht spezifizierbar — das ist der
  erste Punkt, den der Mensch beim Wiederaufnehmen beantworten muss.
- **Mehrdeutige FA je Zeile (open_question 2):** wenn eine Artikel/Lagerplatz-Kombination aus
  mehreren FA-getaggten Bewegungen stammt — Anzeige als Liste oder nur juengste/haeufigste FA?
- **Konsistenz mit Teil 1:** Die FA-Spalten-Datenquelle beruehrt dieselbe „Bestand vs.
  Bewegungssaldo"-Frage wie Teil 1 (Bugfix FA-Hinweis). Beim Wiederaufgreifen mit dem dann
  finalisierten Teil-1-Verhalten (Variante B / nur Ist-Bestand) abgleichen.
- **Cross-Feature-Merkposten:** `OutboundAll` erzeugt `Ausbuchung`en ueber
  `IStockMovementRepository.AddAsync` — seit v1.28.0 haengt daran der Sage-Lagerbuchungs-Enqueue.
  Wenn die FA-Filterung (Variante A) die Menge der ausgebuchten Zeilen aendert, aendert sich damit
  auch die Menge der Sage-Meldungen; bei der Umsetzung mitdenken (kein Bypass des Repository-Pfads).

### Empfehlung

**ZURUECKGESTELLT (bewusst, durch den Menschen). Nicht freigeben; im Backlog/Entwurf belassen, bis
die Kern-Entscheidung (Variante A vs. B) getroffen ist. Danach normale /review-Runde nachholen.**

> [!important] Wiederaufnahme Gerald 2026-11-19 20:10: Kern-Entscheidungen 1–3 beantwortet (oben). Spec stammt aus v1.2x – Zeilenangaben und Code-Stand sind veraltet; vor Freigabe Überarbeitung durch den spec-agent gegen den Bündel-Stand v1.99.44.

> [!important] Gerald 2026-11-19 20:40: Antworten 5 und 6 erweitern den Umfang (Dialog je gemischter Zeile; Haupt-FA→Sub-FA an IDEAL). Rumpf, AK und TS durch den spec-agent nachziehen, dann Freigabe.

## Kritische Pruefung (2026-11-20)

Geprüft gegen den Rumpf nach Rework Runde 2 und den Code im Bündel-Worktree `2026-08-07-ideal-teile-1-5`
(HEAD `c39a9423`, v1.99.45). Zeilenangaben beziehen sich auf diesen Stand.

### 1. Antworten vollständig
- Runde 1 (1–3) und Runde 2 (4–6) sind beantwortet, mit Text und Datum.
- **Runde 3 (7, 8) ist leer** (nur Pfeile) → BLOCKER B-1, B-2.
- Unter Antwort 3 steht noch „klären wir später - bitte im backlog belassen.“ (Vertagung 2026-08-05). Das ist durch die
  Wiederaufnahme 2026-11-19 überholt, aber nicht so gekennzeichnet (H-1).

### 2. Antworten widerspruchsfrei / 2b. Aussagen belegt

**Antwort 6 („über ProductionOrderId/Hierarchie“) gegen den Textweg der Spec:** Das ist fachlich gleichwertig, der Textweg trifft sogar etwas mehr.

Belegt über alle Schreibwege von `StockMovement.ProductionOrderId`:
- `StockMovementsController.cs:152, :264, :371, :464, :542`: immer aus `FindForBookingAsync` **desselben** Texts
  (`ProductionOrderRepository.cs:224-238`: zuerst `SubOrderNumber == t`, sonst Haupt-FA, Liste → null).
- `PickingTransferService.cs:217`: FK = FA der Zeile, Text = `auftrag.ProductionOrder?.OrderNumber` (`:75`) bzw. die
  zusammengeführte Liste „A;B“ (`:101-102`).
- `LagerbestandSyncService.cs:179, :247` und `PurchaseOrdersController.Receive.cs:140` setzen weder Text noch FK.

Folge: Jede Bewegung mit FK trägt die Nummer dieses FA (bzw. dessen `OrderNumber`) auch im Text. Der Textweg trifft also alles,
was der FK-Weg trifft, und zusätzlich die Listen-Bewegungen mit FK `null`.

Die Hierarchie ist ebenfalls belegt:
- `FaMaterializationSyncService.cs:151-153` setzt `OrderNumber = n.HauptFA` für **jede** Ebene, die Updates (`:163-181`) ändern
  `OrderNumber` nie.
- `SubOrderNumber` ist eindeutig (`ProductionOrder.cs:15-22`), `IsHauptFa`/`IsSubFa` stehen in `:29-35`.

→ Die Abweichung vom Wortlaut ist begründet und richtig. Sie steht aber nur im Ist-Zustand, nicht als Bestätigungsfrage (S-1).

**Standort-Erkennung belegt:**
- Key `ProduktionsauftragHierarchisch` (`HierarchischeStrukturKeys.cs:10`), `FaBuchungsText.IstHierarchisch` (`FaBuchungsText.cs:51`).
- Muster in `StockApiController.cs:81-82`, Anzeigeformat in `:49`.
- `IServiceSettingRepository` ist registriert (`Program.cs:90`) und wird von vielen Controllern gleich gelesen
  (z. B. `ProductionOrdersController.cs:145`, `TrackingController.cs:68`).
- Der Ctor `StockMovementsController.cs:22-30` hat ihn heute nicht (bestätigt).

**Riss im Rumpf:** Anforderung 1 sagt „alle FA-Nummern … einzeln“. Lösungsentwurf 6, AK 18 und TS-173.10 zeigen an IDEAL
dagegen „S1 (Haupt-FA H)“. Das Format fehlt in der Anforderung (S-2).

### BLOCKER

- **B-1 Rückfrage 7 unbeantwortet.** Davon hängen AK 19 und TS-173.11 ab.
  *Frage an Gerald:* Soll eine eingegebene **Sub**-FA nur ihre eigene Nummer treffen (Vorschlag der Spec, kein Zusatzcode)?
  Oder auch alle darunter hängenden Sub-FA über `ParentSubOrderNumber` (rekursiv, neue Abfrage)?
- **B-2 Rückfrage 8 unbeantwortet.** Die Lücke ist größer als nur die Stückzahl, siehe S-3.
  *Frage an Gerald:* Reicht der Abgleich der gemischten **Zeilen**? Oder sollen auch geänderte Stückzahlen und neu
  hinzugekommene treffende Zeilen die Buchung stoppen?
- **B-3 Bestand ohne FA in einer FA-Zeile wird still ausgebucht (Widerspruch zu Antwort 4 und 5).** Die Regel „gehört zu FA“
  (Anforderung 2/3, `StockMovementRepository.cs:527-533`) wertet **irgendeine** Bewegung mit FA-Text am Paar. Bewegungen ohne Text
  sieht sie nicht, und Ausbuchungen mit FA-Text zählen mit. Eine Zeile ist damit „treffend, nicht gemischt“, obwohl ihr Bestand
  ganz oder teilweise keinem FA gehört.

  Realistischer Fall:
  1. Regal L, Artikel Y, 100 Stk aus dem Wareneingang, ohne FA (`PurchaseOrdersController.Receive.cs:140-151` setzt keinen Text).
  2. Einzel-Ausbuchung von 5 Stk mit FA 111 (`StockMovementsController.cs:365-372`, `StorageLocationId` = L).
  3. Die FA-Spalte zeigt bei Y „111“.
  4. „Lagerplatz ausbuchen“ mit Filter 111 bucht die restlichen 95 Stk **ohne Rückfrage** aus, samt Sage-Meldung.

  Dasselbe passiert bei Altbewegungen: Das Paar war einmal mit FA bebucht, ging auf 0 und wurde ohne FA neu befüllt. AK 3 deckt
  nur „Paar ganz ohne Bestand“ ab. Teil 1 war eine Anzeige, hier wird gebucht. Die Spec wechselt selbst aus genau diesem Grund
  von Teilstring auf exakte Nummer.

  *Frage an Gerald:* Wie soll eine Zeile behandelt werden, die neben FA-Bewegungen auch Zugänge **ohne** FA hat, oder deren
  FA nur aus einer Ausbuchung stammt?
  - (a) Sie gilt als gemischt („enthält auch Bestand ohne FA“) und wird im Dialog abgefragt. Das ist am nächsten an Antwort 5,
    braucht aber eine Zusatzabfrage.
  - (b) Nur Zugänge auf den Platz zählen, Ausbuchungstexte nicht. Das schließt nur den Fall aus Schritt 2.
  - (c) Bewusst hinnehmen und in Abgrenzung und Hilfe dokumentieren.

### SOLLTE

- **S-1** Den Textweg statt der FK (siehe 2b) in Runde 3 als Ein-Zeilen-Bestätigung zu Antwort 6 aufnehmen, oder als Satz in
  Umfang Punkt 6.
- **S-2** Anforderung 1 um das IDEAL-Anzeigeformat „Sub (Haupt-FA H)“ ergänzen. Der Vergleich bleibt auf der reinen Nummer.
- **S-3 Die Serverprüfung deckt nur die gemischten Zeilen ab** (Anforderung 11, `SetEquals` auf `gemischtGezeigt`).
  - Ungesehen ausgebucht wird eine Zeile, die zwischen Seite und POST **neu** treffend und nicht gemischt wird: eine neue
    Einbuchung mit 111, oder eine bisher nicht treffende Zeile bekommt 111.
  - Unentdeckt bleibt auch eine gemischte Zeile, deren Fremd-FA wechselt (222 → 222, 333).
  - Beides widerspricht „kein stilles Ausbuchen“.
  - Vorschlag: zusätzlich `treffendGezeigt` (IDs aus „Wird ausgebucht“) senden und ebenfalls per `SetEquals` prüfen. Zusammen
    mit B-2 entscheiden.
- **S-4 Doppel-POST.**
  - Knopf „Jetzt ausbuchen“ nach dem Klick `disabled` setzen und als AK aufnehmen.
  - Ein zweiter POST, der danach verarbeitet wird, ist harmlos, warnt aber irreführend „Kein Artikel gehört zu FA …“.
  - Zwei gleichzeitige POSTs buchen doppelt (negativer Bestand, doppelte Sage-Meldung). Das ist heute schon so und kein
    neuer Fehler.
- **S-5 TS-2.4 ändert sich inhaltlich, ein „Verweis“ reicht nicht** (`docs/TESTSZENARIEN.md:272-289`: Wagen,
  „Alle Artikel ausbuchen“, `confirm()`, alles ausgebucht). Mit Antwort 4 gilt dort:
  - Das Feld ist vorbelegt, der Knopf heißt „N von N Artikeln ausbuchen (FA …)“.
  - Statt `confirm()` öffnet das Modal.
  - Artikel ohne FA bleiben stehen.

  Schritte und Erwartung von TS-2.4 explizit anpassen und in `affected_code` nennen. 115.12 (`:11352`) und 130.5.6 (`:11810`)
  bleiben gültig.
- **S-6** AK 18 „`ProductionOrderId` der Haupt-FA“ nur „falls im WMS eindeutig“ (wie AK 4, `ProductionOrderRepository.cs:229-237`),
  sonst scheitert die QA auf DEV IDEAL an einer fehlenden Haupt-FA-Zeile.
- **S-7** `LagerbuchungFaVerknuepfungTests.cs:109` / `:115-119` buchen nur noch, wenn der Mock der neuen Methode für Artikel 9 die
  passende Nummer liefert. Im Controller den Null-Fall abfangen (`?? new()`), weil der lose Moq-Default kein brauchbarer Leerwert ist.

### HINWEIS

- **H-1** „klären wir später – bitte im backlog belassen.“ unter Runde 1 als überholt kennzeichnen.
- **H-2 „Bit-gleich“:** Heute läuft `Distinct()` in SQL (`StockMovementRepository.cs:527-533`; SQL Server ohne Rücksicht auf
  Groß-/Kleinschreibung und Leerzeichen am Ende), danach ordinal in C#. Je Artikel gruppiert kann eine abweichende Schreibweise
  derselben Nummer zweimal erscheinen. Bei numerischen FA ist das folgenlos, der InMemory-Test (`:20-35` der Testklasse) kann es
  nicht zeigen.
- **H-3** Die Regel steht doppelt (C# + JS). Bei einer Abweichung verweigert der Server mit einer irreführenden „hat sich
  geändert“-Meldung. Nachweis nur per Manual-UAT 173.4/173.10.
- **H-4** `AddAsync` läuft je Zeile. Ein Abbruch mittendrin hinterlässt eine Teilbuchung (wie heute, `StockMovementsController.cs:534-553`).
- **H-5** TS-173.8 „Danach Feld leeren“: Der Redirect nach dem Erfolg (mit Lagerplatz, ohne `productionOrder`) baut die
  Vorbelegung aus den Restartikeln neu. Bei Artikeln nur ohne FA ist das Feld schon leer.
- **H-6** Der Wagen-Konflikt (`PickingTransferService.cs:82-103`) und der Mock `PickingTransferServiceAggregationTests.cs:150-151`
  bleiben unberührt, solange die Interface-Methode erhalten bleibt.
- **H-7 Nummern frei:**
  - Version: Worktree auf 1.99.45 (`IdealAkeWms/AppVersion.cs:5`). „1.99.46“ steht sonst nur in der in v1.99.45 aufgegangenen
    Spec `2026-11-19-bf-warenkorb-artikelsuche-type-ahead-bug-spec`.
  - Testszenario: `docs/TESTSZENARIEN.md` endet bei TS-170 (`:13379`), TS-171/172 sind reserviert, TS-174 belegt die
    Dashboard-Spec. **TS-173 ist frei.**
- **H-8 Größe:** etwa 12 Dateien in 4 Schichten plus Tests und Doku. In einem Dev-Lauf machbar, wenn B-3 nicht zu (a) mit neuer
  Abfrage führt.
- **H-9 Deploy** ist plausibel: nur Web, keine Migration, Info-Eintrag im Manifest. Den Info-Text nach der Entscheidung zu B-3 ergänzen.

### Empfehlung

**NACHBESSERUNG NOETIG: Rückfragen 7/8 offen; B-3 (Bestand ohne FA in einer FA-Zeile wird still ausgebucht) braucht eine Entscheidung; S-3/S-5 vor Freigabe in den Rumpf.**
