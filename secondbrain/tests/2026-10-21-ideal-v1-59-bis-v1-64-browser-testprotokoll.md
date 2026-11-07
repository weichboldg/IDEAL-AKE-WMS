---
type: testprotokoll
title: "Browser-Testprotokoll IDEAL — neue Entwicklungen v1.59 bis v1.64.1 (TS-92 bis TS-97)"
ausfuehrung: "Teil A + C: Gerald · Teil B: Claude Cowork (Abnahme)"
system: "http://idealweb01.ideal.ideal-ake.at:88/ (nur IDEAL, Stand v1.64.1 laut Fußzeile)"
spec:
  - "[[2026-10-13-briefkopf-fusszeile-word-vorlage-spec]]"
  - "[[2026-10-14-ideal-sage-views-umstellung-spec]]"
  - "[[2026-10-16-arbeitsgang-stammdaten-abgleich-aus-sage-view-spec]]"
  - "[[2026-10-16-stueckliste-matchcode-lagerplatz-limit-spec]]"
  - "[[2026-10-19-tabelle-seitenbreite-doppelter-scrollbalken-bug]]"
  - "[[2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung-spec]]"
  - "[[2026-10-19-bde-terminal-liste-scan-ohne-ag-prio-spec]]"
created: 2026-10-21
---

# Browser-Testprotokoll IDEAL — neue Entwicklungen v1.59 bis v1.64.1

Deckt alles ab, was seit der letzten Browser-Abnahme (TS-91, v1.58.1) auf IDEAL dazugekommen ist: Fußzeile im Druck (TS-92),
Sage-Views (TS-93, nur Ergebnis), Arbeitsgang-Abgleich (TS-94), Stückliste und Scrollbalken (TS-95), BDE Stufe 1 (TS-96) und
BDE-Terminal (TS-97). Reihenfolge in Teil B: BDE zuerst (B0–B8), dann B9–B12.

Grundlage: TS-92 bis TS-97 in `docs/TESTSZENARIEN.md` im Bündel (Worktree `2026-08-07-ideal-teile-1-5`,
v1.64.1) und die Checklisten „Manuelle Test-Checkliste (Schranke 2)“ der Specs. Teil B läuft unter der Arbeitsweise
ABNAHME. **Teil B prüft nur, was ohne Buchung geht.** Alles, was bucht, eine Einstellung ändert oder einen manipulierten POST
braucht, steht in Teil C.

**Testdaten (Gerald 2026-10-21):** Personalnummer `123`, Haupt-FA `1059463` (Sub-FA `1065150` mit EG laut TS-96.2),
Terminal-Werkbank Id `65` „Entgraten Fläche“.

**Belegt gegen den Code im Bündel:**
- Adressen: Terminal `/BdeTerminal/Index?workplaceId=65`, Werkbänke `/ProductionWorkplaces`, Einstellungen `/Settings`,
  Aktivitäts-Protokoll `/SyncLog`, FA-Struktur `/FaHierarchy`, Hilfe über den Link „Hilfe“ in der **Fußzeile**
  (`Views/Shared/_Layout.cshtml` Z. 109, 318, 321, 407).
- Terminal-Texte (`wwwroot/js/bde-terminal.js`, `Views/BdeTerminal/Index.cshtml`): Scanfeld „Oder FA-Nr scannen (optional ,AG-Nr)…“
  (Index Z. 76), Knopf „Weitere … anzeigen“ (Z. 599), Badge „Prio …“ (Z. 591), „ohne Termin“ (Z. 585), Zeile „Gewählt: …“ (Z. 365),
  Auswahl „… Treffer – bitte auswählen“ (Z. 291), Meldung „Dieser Auftrag hat an dieser Werkbank keinen offenen Arbeitsgang.“ (Z. 226).
- Werkbänke-Spalten (`Views/ProductionWorkplaces/Index.cshtml`): Bezeichnung, Sage-Nr., Nicht in Sage seit, Arbeitsschritt,
  Halle, BDE, Benutzer, Abw. Vorkommissioniertage; Badges „nur Fertigmeldung“ (Z. 67), Datum gelb (Z. 51).
- Hilfe: Absatz „Andere Listen“ (`Views/Help/Index.cshtml` Z. 243), „Terminal-Bedienung (Schritt fuer Schritt)“ (Z. 1130).

## Teil A — Gerald, vor Teil B

- [x] A1 SQL/97 eingespielt: `sys.columns` von `dbo.ProductionWorkplaces` enthält `SageMissingSince` und `BdeNurAgFertigmeldung`.
- [x] A2 Werkbank-Anlage einmal im **DryRun** gelaufen, `nicht_in_sage_neu` notiert: ______ ; danach scharf gelaufen.
- [x] A3 Anzahl gekennzeichneter Werkbänke (`SageMissingSince IS NOT NULL`): ______ (erwartet: die alten W1-Werkbänke, u. a. Id 19)
- [x] A4 Struktur-Erkennung nach A2 mindestens einmal gelaufen (Sub-FA 1065150 hat EG an Werkbank 65).
- [x] A5 `BdeNurFaMeldung` aus, `ProduktionsauftragHierarchisch` an, Werkbank 65 BDE-aktiv.
- [x] A6 Arbeitsgang-Abgleich: `Sync:OperationConfigSyncEnabled` an und mindestens ein Lauf erfolgt (TS-94.2), sonst B10 nur teilweise.
- [x] A7 Briefkopf-Vorlage mit Fußbereich liegt im `Branding\`-Ordner (TS-92), sonst B12 NICHT PRUEFBAR.
- [x] A8 Ein Haupt-FA mit Beschichtungsauftrag im Status „Übergeben“ für B12: ______

## Freigabe-Block (Gerald kreuzt an — ohne Haken ist der Schritt NICHT PRUEFBAR)

- [x] F1 Am Terminal 65 mit Personalnummer `123` anmelden und mit „Wechseln“ ab- und wieder anmelden (keine Buchung).
- [x] F2 Am Terminal Arbeitsgänge **antippen** (nur Auswahl). Die Knöpfe „Rüsten starten“, „Produktion starten“,
      „Fertig melden“ werden NIE gedrückt.
- [x] F3 Ins Scanfeld Nummern eintippen und mit Enter bestätigen (Schritte B4).
- [x] F4 Bearbeiten-Seite einer Werkbank öffnen, ohne zu speichern.
- [x] F5 Spaltenauswahl (Zahnrad) der Stückliste und der Arbeitsgänge kurz ändern und am Ende wiederherstellen.

---

## Teil B — Auftrag an Claude Cowork (ab hier kopieren)

Arbeitsweise ABNAHME. System: `http://idealweb01.ideal.ideal-ake.at:88/`. Nur prüfen, nichts buchen, nichts speichern, keine
Einstellung ändern. Spaltenfilter und Spaltenauswahl zu Beginn notieren, am Ende wiederherstellen. Bericht nach Projektanweisung.

### B0 Rahmen
1. Startseite öffnen. Fußzeile: Version notieren. **Erwartet:** v1.64.1 (oder höher).
2. `/Settings` öffnen, Gruppe BDE lesen, nichts ändern. **Erwartet:** Eintrag `BdeTerminalMaxHauptFa` mit Wert 5 (TS-97.1).

### B1 Terminal: Liste, Gruppen, Grenze (TS-97.1, 97.2) — braucht F1
1. `/BdeTerminal/Index?workplaceId=65` öffnen. Oben steht Werkbank „Entgraten Fläche“. Personalnummer `123` ins Feld
   „Personalnummer scannen…“, Enter.
2. Unter „2. Arbeitsgang wählen“ die Gruppenköpfe zählen und je Kopf notieren: Haupt-FA, Prio-Badge (falls vorhanden), Termin.
   **Erwartet:** höchstens 5 Köpfe, alle zugeklappt; Reihenfolge: Köpfe mit Prio zuerst (kleinere Zahl oben), dann nach Termin,
   „ohne Termin“ zuletzt.
3. Gibt es den Knopf „Weitere … anzeigen“: einmal drücken, Köpfe erneut zählen. **Erwartet:** 5 weitere. Gibt es ihn nicht und
   sind es weniger als 6 Köpfe: OK mit Vermerk „weniger als 6 Haupt-FAs an 65“.
4. „Wechseln“, erneut `123` anmelden. **Erwartet:** alle Gruppen wieder zugeklappt, wieder höchstens 5 Köpfe.

### B2 Aufklappen 1059463 (TS-97.3)
1. Kopf „1059463“ antippen (ggf. über „Weitere … anzeigen“ einblenden). **Erwartet:** Sub-FA-Blöcke (z. B. 1065142, 1065143,
   1065144, 1065150) mit Knopf **EG** und Statuszeichen; **kein** ST, **kein** KA. Kopf hervorgehoben.
2. Kopf erneut antippen. **Erwartet:** Gruppe klappt zu.
3. Screenshot der aufgeklappten Gruppe in `bilder\`.

### B3 Markierung und Knöpfe (TS-97.4) — braucht F2
1. Gruppe 1059463 aufklappen, den EG-Knopf eines Sub-FA antippen.
   **Erwartet:** dunkler Rahmen und „✓“ am Knopf; per `read_page` `aria-pressed="true"`; direkt unter der Karte „2. Arbeitsgang
   wählen“ die Zeile „Gewählt: 1059463 / <Sub-FA> · EG …“ und die Knöpfe „Rüsten starten“ und „Produktion starten“, ohne Scrollen
   sichtbar. Unter beiden Spalten am Seitenende **keine** Knöpfe.
2. Den EG-Knopf eines anderen Sub-FA antippen. **Erwartet:** nur dieser ist markiert, „Gewählt“ zeigt ihn.
3. Screenshot. **Die Start-Knöpfe nicht drücken.**

### B4 Scan ohne AG (TS-97.5) — braucht F3
Jeweils ins Feld „Oder FA-Nr scannen (optional ,AG-Nr)…“ tippen, Enter, Ergebnis notieren:
- a) `1059463` → Gruppe offen und im Sichtbereich, nichts vorgewählt.
- b) eine Sub-FA-Nummer aus B2 mit offenem EG (notieren welche) → EG direkt gewählt und markiert, Knöpfe sichtbar.
- c) `1059463,EG` → bei mehreren Sub-FAs Auswahl „… Treffer – bitte auswählen“ bzw. direkt gewählt; Gruppe offen.
- d) ein Haupt-FA aus B1, der erst nach „Weitere … anzeigen“ erscheint (falls vorhanden) → wird eingeblendet und aufgeklappt.
- f) `9999999` → „Arbeitsgang nicht gefunden“.
- g) `1059463,ST` → „Dieser Auftrag hat an dieser Werkbank keinen offenen Arbeitsgang.“
- h) leeres Feld, Enter → nichts passiert.
Danach mit „Wechseln“ abmelden und das Terminal verlassen.

### B5 Werkbänke (TS-96.8 Lesen, TS-96.1 Ergebnis)
1. `/ProductionWorkplaces` öffnen. **Erwartet:** Spalte „Nicht in Sage seit“ vorhanden.
2. Filter „Nicht in Sage seit“ = `!(leer)`. Treffer zählen, Bezeichnung und Sage-Nr. notieren. **Erwartet:** Anzahl wie A3;
   Sage-Nr. 2150 (Id 19) und 2300 dabei; „Entgraten Fläche“ (140) **nicht** dabei. Filter löschen.
3. Werkbank „Entgraten Fläche“: Spalte Arbeitsschritt = `EG`, BDE aktiv, kein Badge „nur Fertigmeldung“.
4. (braucht F4) Bearbeiten von „Entgraten Fläche“ öffnen. **Erwartet:** Kürzel und Bezeichnung nicht bearbeitbar (Sage-geführt),
   Checkbox „Nur AG-Fertigmeldung (ohne Start/Stop)“ vorhanden und aus. Seite verlassen, **nicht speichern**.

### B6 FA-Struktur (TS-96.6, TS-97.7)
1. `/FaHierarchy`, Haupt-FA 1059463 suchen und aufklappen bis Sub-FA 1065150.
2. In der Spalte Arbeitsgänge das EG-Symbol: Tooltip bzw. `aria-label` lesen.
   **Erwartet:** „EG – Entgraten Fläche …“ mit Status (offen/überfällig/läuft/erledigt), nicht leer und nicht „kein BDE-Arbeitsgang“.
   KA ebenso mit Werkbank-Name; ST unverändert.
3. Screenshot.

### B7 Aktivitäts-Protokoll (TS-96.1, 96.2 — Ergebnis lesen)
1. `/SyncLog`, Dienst `ProductionWorkplaceSync`, letzter Lauf öffnen. Zählwerte `nicht_in_sage`, `nicht_in_sage_neu` und
   Warnzeilen notieren. **Erwartet:** Warnzeile „Nicht mehr in Sage …“ beim ersten scharfen Lauf; im letzten Lauf
   `nicht_in_sage_neu = 0`.
2. Dienst `WorkOperationStructureDetection`, letzter Lauf. **Erwartet:** kein Zähler `arbeitsschritt_mehrdeutig`;
   unbekannte Kürzel (z. B. MK, ZS, MO) dürfen weiter erscheinen.
3. Filter wiederherstellen.

### B8 Hilfe (TS-97.8)
1. Fußzeile › Hilfe. Absatz „Andere Listen“ lesen. **Erwartet:** BDE-Buchungen filtern serverseitig (nicht „nur im Browser“).
2. Abschnitt „Terminal-Bedienung (Schritt fuer Schritt)“. **Erwartet:** beschreibt Gruppen nach Haupt-FA, Prio, Auswahl mit
   Markierung, Scan ohne AG und `BdeTerminalMaxHauptFa`.

### B9 Aktivitäts-Protokoll Sage-Views (TS-93, nur Ergebnis)
1. `/SyncLog`, je letzter Lauf von `FaHierarchy`, `ProductionWorkplaceSync` und `Article`: Status und Zählwerte notieren.
   **Erwartet:** alle erfolgreich, kein „Ungültiger Spaltenname“, kein „nicht vorhanden oder keine Leseberechtigung“.
2. Standorteinstellungen (Admin) nur lesen: die Sage-View-Namen notieren. **Erwartet:** FAListe, FAInfos, Arbeitsplätze und Artikel
   zeigen `[vw_IDEAL_AKE_WMS_…]`. Nichts ändern.

### B10 Arbeitsgang-Stammdaten (TS-94.1, 94.5–94.7, 94.13, 94.16)
1. Stammdaten › Arbeitsgänge (`/OperationConfigs`). **Erwartet:** Spalte „Quelle“ mit Sage / Manuell / Fremdsystem; Zahnrad über der
   Tabelle; bei eingeschaltetem Abgleich an Kürzeln, die Sage nicht kennt, das Badge „nicht in Sage“ und ein gelber Hinweis oben.
2. Filter Kürzel `=SL` → genau eine Zeile (oder keine, falls SL gelöscht ist; notieren). Filter löschen.
3. Filter Quelle `=Manuell`, dann `!Fremdsystem` → nur passende Zeilen. Filter löschen.
4. (braucht F5) Zahnrad: „Quelle“ ausblenden, Seite neu laden → bleibt aus; wieder einblenden. „Kürzel“ lässt sich nicht ausblenden.
5. Karte „Nicht konfigurierte Kürzel“ lesen und notieren (z. B. MK, ZS, MO).

### B11 Stückliste (TS-95.1, 95.4–95.6, 95.9–95.14, 95.17–95.20)
1. `/Picking/Bom/131` (FA 1059463). Falls die Id nicht passt: FA-Liste › 1059463 › Stückliste. **Nichts kommissionieren, nichts
   umbuchen.**
2. **Erwartet:** Spalte „Matchcode“ rechts von „Bezeichnung 2“; 3 Stichproben-Zeilen mit Matchcode notieren.
3. „Alle aufklappen“. Lagerplatz-Spalte: Zeilen mit mehr als 3 Plätzen zeigen genau 3 und „+n weitere“. Eine solche Zeile notieren.
4. Mauszeiger auf „+n weitere“ → Popover mit allen Plätzen, Hauptlagerplatz benannt; Tab-Taste darauf → Popover, Escape schließt.
5. Filter Matchcode: Teil eines Matchcodes in Kleinschrift → Treffer. Filter Lagerplatz: einen Platz an Position ≥ 4 aus Schritt 4
   → die Zeile bleibt sichtbar. Filter `(leer)` / `!(leer)` auf Matchcode. Filter löschen.
6. (braucht F5) Zahnrad: Matchcode ausblenden, neu laden → bleibt aus; wieder einblenden.
7. Scrollbalken (v1.64.1), Fenster ca. 950 px breit (`resize_window`), „Alle aufklappen“:
   - Keine Querscrollleiste der Seite; per `javascript_tool` lesen:
     `document.documentElement.scrollWidth === document.documentElement.clientWidth` → `true`.
   - Oben: am unteren Fensterrand ein mitlaufender blauer Balken. Ganz nach unten zum Tabellenende scrollen → nur noch der Balken
     der Tabelle, kein zweiter. Screenshot oben und unten.
8. Druckansicht der Stückliste öffnen (nicht drucken, Druckdialog mit Escape schließen). **Erwartet:** Matchcode-Spalte, Lagerplatz
   als volle Liste.
9. Gegenprobe Fertigungsaufträge (`/ProductionOrders`): genau ein Balken wie in Schritt 7.

### B12 Druck Beschichtungsauftrag mit Fußzeile (TS-92.1, 92.9, 92.12)
1. Beschichtung › Aufträge, den Auftrag aus A8 öffnen, Druckansicht öffnen (nicht drucken, Druckdialog mit Escape schließen).
2. **Erwartet:** Briefkopf oben auf Seite 1; Fußzeile mit IDEAL- und AKE-Anschrift, www.ideal-ake.at und dem zweifarbigen Balken;
   unten rechts „Seite X von Y“; nichts von der Fußzeile verdeckt. Am Bildschirm kein schwebender Balken beim Scrollen.
3. Beschichtung › Vorschlagsliste, Ansicht „Summiert“ ohne Filter, Druckansicht: Fußzeile ja, **keine** Seitenzählung (mehrere HauptFA).
4. PDF-Download nur nach Rückfrage im Chat.

---

## Teil C — Gerald, von Hand (bucht oder ändert)

- [ ] TS-96.3 Schritt 4: an 65 EG starten und beenden → Buchungsliste Werkbank „Entgraten Fläche“, FA-Baum EG erledigt.
- [ ] TS-96.4: Werkbank „Entgraten 2 (Test)“ ohne Sage-Nr. mit Kürzel EG anlegen → beide Terminals zeigen den AG, Kollisionsdialog.
- [ ] TS-96.7: Option „Nur AG-Fertigmeldung“ an einer Testwerkbank → nur „Fertig melden“, Vorbelegung Sollmenge, Buchung Start = Ende.
- [ ] TS-96.10: manipulierte POSTs (fremder AG, Fortsetzen an fremder Werkbank, ReportCompletion, StartProductionForOrder).
- [ ] TS-97.1 Schritt 5: `BdeTerminalMaxHauptFa = 3` → 3 Köpfe; danach zurück auf 5.
- [ ] TS-97.5 i: echtes Sage-Papier mit dem Scanner → Gruppe 1059463 klappt auf. Ergebnis: ______
- [ ] TS-97.6: negative Mengen bei Pause, Teilmeldung, Beenden, Fertig melden, Admin-Korrektur → abgewiesen.
- [ ] TS-96.9 / TS-97.9 / TS-97.10: AKE-Regression auf `https://akenet01.ake.at:4445/` (eigenes Protokoll
      [[2026-10-20-ake-regression-ideal-version-browser-testprotokoll]]).
- [ ] TS-92.3/92.5: PDF und Papierausdruck Beschichtungsauftrag (Schriften eingebettet, Balken auch ohne Hintergrundgrafiken).
- [ ] TS-93.5: Kommissionierung umbuchen → je Position eine Bewegung, Sage-Lagerbuchung erfolgreich.
- [ ] TS-94.3/94.15: Offset/Anzeigename eines Kürzels ändern, Quelle-Wechsel Manuell → Sage nach dem nächsten Lauf.
- [ ] TS-95.7/95.16: CSV der Stückliste (Matchcode, volle Lagerplatz-Liste); Bestellen einer Baugruppen-Zeile.
- [ ] TS-95.11: Popover am Tablet (Touch).
- [ ] Werker/Schichtleitung informiert: Fortsetzen nur an Werkbänken mit demselben Kürzel.

## Übersicht der Schritte

| Schritt | TS | Wer | braucht |
| --- | --- | --- | --- |
| B0 | 97.1 | Cowork | — |
| B1 | 97.1, 97.2 | Cowork | F1 |
| B2 | 97.3 | Cowork | F1 |
| B3 | 97.4 | Cowork | F1, F2 |
| B4 | 97.5 | Cowork | F1, F3 |
| B5 | 96.1, 96.8 | Cowork | F4 (nur 4) |
| B6 | 96.6, 97.7 | Cowork | — |
| B7 | 96.1, 96.2 | Cowork | — |
| B8 | 97.8 | Cowork | — |
| B9 | 93 | Cowork | — |
| B10 | 94 | Cowork | F5 (nur 4) |
| B11 | 95 | Cowork | F5 (nur 6) |
| B12 | 92 | Cowork | A7, A8 |
| Teil C | 92.3/5, 93.5, 94.3/15, 95.7/11/16, 96.3/4/7/9/10, 97.1/5/6/9/10 | Gerald | — |
