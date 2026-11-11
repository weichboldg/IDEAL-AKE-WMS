---
type: testbericht
title: "Browser-Testbericht IDEAL v1.59 bis v1.64.1 — Teil B"
protokoll: "[[2026-10-21-ideal-v1-59-bis-v1-64-browser-testprotokoll]]"
system: "http://idealweb01.ideal.ideal-ake.at:88/ (v1.64.1 laut Fußzeile)"
datum: 2026-10-21
ausfuehrung: "Claude Cowork (Abnahme), Chrome mit Geralds Anmeldung"
ergebnis: "45 OK, 2 FEHLER, 1 nicht pruefbar, 0 Protokoll passt nicht"
---

# Browser-Testbericht IDEAL v1.59 bis v1.64.1 — Teil B

**Lauf:** 21.10.2026, 10:22–10:52. Teil A und Freigabe-Block F1–F5 waren angehakt (Datei 10:19).

**Abweichende Reihenfolge:** Das Protokoll verlangt BDE zuerst. Gelaufen ist zuerst der ADMIN-Teil (B0, B5–B11), dann
B1–B4 unter Benutzer `wb1`. Grund: Einstellungen, Aktivitäts-Protokoll und Standorteinstellungen brauchen Admin-Rechte,
das Terminal braucht `wb1`. Gerald hat sich jeweils selbst angemeldet.

**Offene Testdaten:** A3 (erwartete Anzahl „nicht in Sage“) und A8 (Haupt-FA mit Beschichtungsauftrag „Übergeben“) waren leer.

**Fenstergröße:** Das Chrome-Fenster war maximiert (1920 px), `resize_window` griff nicht. B11.7 ist deshalb bei 1905 px
Seitenbreite gemessen, nicht bei 950 px.

**Wiederhergestellt:** Spaltenauswahl Arbeitsgänge (Quelle wieder an) und Stückliste (Matchcode wieder an, Stand wie
vorher laut `/api/user-view-preferences/Bom`). Alle Spaltenfilter geleert. Terminal-Operator mit „Wechseln“ abgemeldet.
Nichts gebucht, nichts gespeichert, keine Einstellung geändert. Start-Knöpfe am Terminal nie gedrückt.

## Ergebnis je Schritt

| Schritt | Ergebnis | Beobachtung | Screenshot |
| --- | --- | --- | --- |
| B0.1 | OK | Fußzeile „© 2026 IDEAL-AKE WMS v1.64.1“ | nein |
| B0.2 | OK | `/Settings`: `BdeTerminalMaxHauptFa` = 5 | nein |
| B1.1 | OK | Terminal 65 „Entgraten Fläche“, Personalnummer 123 → „Hans Guck“ | ja |
| B1.2 | OK | 5 Köpfe, alle zu: 1059463 (16.07.2026), 1065036 (20.07.), 1064052 (21.07.), 1035235 (ohne Termin), 1037045 (ohne Termin). Kein Prio-Badge vorhanden, Reihenfolge nach Termin, „ohne Termin“ zuletzt | ja |
| B1.3 | OK | Knopf „Weitere 1 anzeigen (noch 1)“ → 6. Kopf 1043153 (ohne Termin), danach kein Knopf mehr. Vermerk: nur 6 Haupt-FAs an 65, daher 1 statt 5 weitere | nein |
| B1.4 | OK | Nach „Wechseln“ + 123: wieder 5 Köpfe, alle zu | nein |
| B2.1 | OK | 1059463 aufgeklappt: 44 Sub-FA-Blöcke (1065143 … 1065202, u. a. 1065150), je 1 Knopf **EG** mit Statuszeichen (Warndreieck = überfällig). Kein ST, kein KA. Kopf mit Rahmen hervorgehoben. Vermerk: 1065142 ist nicht dabei | ja |
| B2.2 | OK | Erneut antippen → `aria-expanded=false`, 0 EG-Knöpfe sichtbar | nein |
| B2.3 | OK | Screenshot | ja |
| B3.1 | OK | EG 1065143: „✓“ (CSS `::before`), `aria-pressed="true"`, dunkler Rahmen (Outline 3 px). „Gewählt: 1059463 / 1065143 · EG Entgraten Fläche“, darunter „Rüsten starten“ und „Produktion starten“ direkt nach der Karte, sonst keine Aktionsknöpfe. Vermerk: Die Seite springt dabei selbst ans Ende (Gruppe mit 44 AGs), die Knöpfe sind im Bild, der gewählte AG nicht mehr | ja |
| B3.2 | OK | EG 1065144 → nur dieser markiert (1 × `aria-pressed=true`), „Gewählt: 1059463 / 1065144 · EG …“ | ja |
| B3.3 | OK | Screenshot, Start-Knöpfe nicht gedrückt | ja |
| B4 a | OK | `1059463` → Gruppe offen, Kopf im Bild (top 417 px), nichts vorgewählt | nein |
| B4 b | OK | `1065150` → EG direkt gewählt und markiert, „Gewählt: 1059463 / 1065150 · EG Entgraten Fläche“, beide Knöpfe sichtbar | ja |
| B4 c | **FEHLER** | `1059463,EG` → Gruppe offen, aber weder Auswahl „… Treffer – bitte auswählen“ noch direkte Wahl. Einmal wiederholt (frisch angemeldet): gleich | ja |
| B4 d | OK | `1043153` (vorher nur über „Weitere“) → eingeblendet und aufgeklappt, Kopf im Bild, nichts vorgewählt | ja |
| B4 f | OK | `9999999` → „Arbeitsgang nicht gefunden“ | nein |
| B4 g | OK | `1059463,ST` → „Dieser Auftrag hat an dieser Werkbank keinen offenen Arbeitsgang.“ | nein |
| B4 h | OK | Leeres Feld + Enter → keine Änderung (Auswahl, offene Gruppen, Meldungen gleich) | nein |
| B5.1 | OK | `/ProductionWorkplaces`: Spalte „Nicht in Sage seit“ vorhanden | nein |
| B5.2 | OK | Filter `!(leer)`: **61 Treffer** (alle „19.10.2026“), darunter 2150 „Flächen entgr beendet“ und 2300 „Kanterei W1“; 140 „Entgraten Fläche“ nicht dabei. Vermerk: A3 leer, Abgleich nicht möglich, siehe Zusatzbefund Z4 | ja |
| B5.3 | OK | „Entgraten Fläche“: Sage-Nr. 140, Arbeitsschritt EG, BDE Aktiv, kein Badge „nur Fertigmeldung“ | nein |
| B5.4 | OK | `/ProductionWorkplaces/Edit/65`: Sage-Nr., Kürzel und Bezeichnung `readonly`, Hinweis „Sage-geführt“. Checkbox „Nur AG-Fertigmeldung (ohne Start/Stop)“ vorhanden und aus. Nicht gespeichert | nein |
| B6.1 | OK | `/FaHierarchy`, HauptFA 1059463, alles aufgeklappt bis 1065150 und darunter | ja |
| B6.2 | **FEHLER** | EG-Symbole unter 1065150: „EG – Entgraten Fläche – überfällig (Soll 16.07.2026)“, anderswo „… – erledigt“, also wie erwartet. **KA zeigt nur „KA – Kanten“, ohne Werkbank-Namen** (erwartet: KA ebenso mit Werkbank-Name). ST „ST – Stanzen“ | ja |
| B6.3 | OK | Screenshot | ja |
| B7.1 | OK | `ProductionWorkplaceSync`, letzter Lauf 21.10. 10:23:05: `nicht_in_sage=61, nicht_in_sage_neu=0`. Warnzeile beim scharfen Lauf 19.10.2026 13:38:16: „Nicht mehr in Sage (61 neu gekennzeichnet, …)“. Jeder Lauf zusätzlich: „Werkbank Endmontage TK W2 (4101) ist BDE-aktiv, steht aber nicht mehr in Sage.“ | nein |
| B7.2 | OK | `WorkOperationStructureDetection` 10:23:05: kein `arbeitsschritt_mehrdeutig`; `arbeitsschritt_unbekannt=4` (MK, ZS, ISO, MO) | nein |
| B7.3 | OK | Filter zurückgesetzt („— alle —“) | nein |
| B8.1 | OK | Hilfe › „Andere Listen“: „Fast alle Listen filtern auf dem Server ueber alle Seiten, auch BDE-Buchungen …“ | nein |
| B8.2 | OK | „Terminal-Bedienung (Schritt fuer Schritt)“: Gruppen je Haupt-FA, Prio, `BdeTerminalMaxHauptFa`, „Weitere … anzeigen“, Markierung „dunkler Rahmen und ein ✓“, „Gewaehlt: …“, Scan ohne AG | nein |
| B9.1 | OK | Letzte Läufe 10:23:05 alle „erfolgreich beendet“: FaHierarchy (neu 586, geloescht 586), ProductionWorkplaceSync (s. B7.1), Article (gelesen 12525, hauptlagerplatz_fehlt 2259). Fehler „Ungültiger Spaltenname“ / „keine Leseberechtigung“ nur alt (15.10.2026) | nein |
| B9.2 | OK | `/StandortEinstellungen`: FAListe `[vw_IDEAL_AKE_WMS_FAListe]`, FAInfos `[vw_IDEAL_AKE_WMS_FAInfos]`, Artikel `[vw_IDEAL_AKE_WMS_Artikel]`, Arbeitsplätze `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]`, Arbeitsgänge (Abgleich) `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]` | nein |
| B10.1 | OK | `/OperationConfigs`: Spalte „Quelle“ (Sage / Fremdsystem), Zahnrad vorhanden, 30 Kürzel. Kein Badge „nicht in Sage“ und kein gelber Hinweis; passt zu `OperationConfigSync` 10:23:05 `nicht_in_sage:0` | nein |
| B10.2 | OK | Kürzel `=SL` → genau 1 Zeile „SL · Schlosser · 5 · Fremdsystem“ | nein |
| B10.3 | OK | Quelle `=Manuell` → „Keine Arbeitsgänge für diese Filterung“ (es gibt keine manuellen). `!Fremdsystem` → 24 Zeilen, alle „Sage“ (30 − 6 Fremdsystem) | nein |
| B10.4 | OK | Quelle ausgeblendet, neu geladen → bleibt aus; wieder eingeblendet. „Kürzel“ = Pflichtspalte, Checkbox gesperrt | nein |
| B10.5 | OK | „Nicht konfigurierte Kürzel“: EM, ISO, LS, PG, SW (je „WMS-Arbeitsgang“) | nein |
| B11.1 | OK | `/Picking/Bom/131` = FA 1059463, 263 Positionen | nein |
| B11.2 | OK | Spalte „Matchcode“ vorhanden, alle 263 Zeilen haben einen. Stichproben: 50002134 „G-KT-FRR-510-2010--1760B-250-V01“, 50000871 „A-BLATT-GN-385-225-LK95-V02“, 73000529 „Fühler PT 1000 3,5m gelb Abtau Ideal/Danfoss/Störk“. Vermerk: In Geralds gespeicherter Spaltenauswahl steht Matchcode nach „Ressourcenummer“; in der Standardreihenfolge (Druck) rechts von „Bezeichnung 2“ | nein |
| B11.3 | OK | 9 Zeilen mit Badge, z. B. 73004290: „KELLER;0;0;0 (147,000), WEL-W1;0;0;0 (11,000), H2-02;0;0;0 (1,000) +5 weitere“ | nein |
| B11.4 | OK | Hover → Popover „Lagerplätze (8)“, KELLER als „Hauptlagerplatz“ markiert. Tab auf Badge → Fokusrahmen + Popover, Escape → 0 Popover | ja |
| B11.5 | OK | Matchcode `lade-2z` → 4 Zeilen (Pos. 5–8 „G-KT-Lade-2Z-…“). `(leer)` → 0, `!(leer)` → alle 13 Köpfe. Lagerplatz `H5-02` (bei 73004290 Platz 8) → nach „Alle aufklappen“ 9.2.1, 9.2.2 (73004290), 11.2 sichtbar. Vermerk: vor dem Aufklappen 0 Zeilen ohne Hinweis, s. Z6 | ja |
| B11.6 | OK | Matchcode ausgeblendet, neu geladen → bleibt aus; wieder eingeblendet | nein |
| B11.7 | OK | Gemessen bei 1905 px statt 950 px (Fenster maximiert). Alles aufgeklappt: `scrollWidth === clientWidth` (1905), `.table-responsive` `position: relative`. Oben ein mitlaufender Balken (`.table-sticky-scrollbar`), am Tabellenende `is-hidden`, nur noch der Balken der Tabelle | ja (2) |
| B11.8 | OK | „Stückliste drucken“ → `/Picking/PrintBom/131`: Spalte Matchcode; 73004290 Lagerplatz volle Liste mit 8 Plätzen, kein „weitere“. Nicht gedruckt, Tab geschlossen | nein |
| B11.9 | OK | `/ProductionOrders`: `scrollWidth = clientWidth` (1905); oben 1 mitlaufender Balken, am Tabellenende `is-hidden` | nein |
| B12 | NICHT PRUEFBAR | A8 (Haupt-FA mit Beschichtungsauftrag „Übergeben“) nicht angegeben | nein |

## Fehler

**B4 c — Scan `1059463,EG` ohne Auswahlliste.** Erwartet war bei mehreren passenden Sub-FAs eine Auswahl „… Treffer – bitte
auswählen“ (bei 1059463 hat jeder der 44 Sub-FAs einen offenen EG), sonst die direkte Wahl. Gesehen: Die Gruppe klappt auf,
es ist nichts gewählt, nirgends steht „Treffer“. Das Ergebnis gleicht dem Scan ohne AG (B4 a). Bei der ersten Ausführung
blieb zusätzlich die Wahl aus B4 b (1065150) stehen. Die Wiederholung nach „Wechseln“ ergab dasselbe.

![[B4-c-1059463-EG-keine-auswahl.jpg]]

**B6.2 — KA ohne Werkbank-Namen.** Erwartet: KA-Symbol in der FA-Struktur mit Werkbank-Namen wie bei EG. Gesehen:
`aria-label` „KA – Kanten“ ohne Namen und ohne Status, an allen KA-Symbolen unter 1059463. Beobachtung dazu: Beide
KA-Werkbänke (2300 „Kanterei W1“, 2350 „Kanterei W2“) sind seit 19.10.2026 als „nicht in Sage“ gekennzeichnet (B5.2).

![[B6-fa-struktur-1065150-eg.jpg]]

## Zusatzbefunde (außerhalb der Schritte)

- **Z1 Seiten-Querscrollleiste durch die Menüleiste.** Um 10:23 war das Fenster 1384 px breit (Seite 1369 px). Da war
  `/ProductionWorkplaces` **1600 px** breit. Ursache laut DevTools: `#navbarNav` (`collapse navbar-collapse`, Admin mit vielen
  Menüpunkten) reicht bis x = 1600. Tabellen-Elemente ragen nicht heraus, `.table-responsive` ist `relative`. Graue
  Seitenleiste im Screenshot B5-2 sichtbar. Bei 1905 px tritt es nicht auf. Am 19.10. bei 958 px stand das Menü als
  Hamburger, das Fenster dazwischen (ca. 992–1600 px) ist also betroffen.
- **Z2 Terminal-Werkbankauswahl** enthält „Endmontage TK W2“ (4101), die laut Sync „BDE-aktiv, aber nicht mehr in Sage“ ist.
- **Z3 Terminal 65** zeigt bei FA 1043153 / 1043167 den AG „EG Flächen entgr beendet“, also den Namen der als „nicht in Sage“
  gekennzeichneten Werkbank 2150 (Status offen, kein Termin).
- **Z4 Umfang „nicht in Sage“:** 61 von 88 Werkbänken, darunter auch W2-Werkbänke, Verkauf (1000), Konstruktion (1700),
  Kommissionierung (6000), Verladung (9000/9100). Die Quelle der Arbeitsplätze ist `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]`, dieselbe View
  wie für den Arbeitsgang-Abgleich (B9.2). Erwartet war laut A3 „die alten W1-Werkbänke“. Nur Beobachtung, A3 leer.
- **Z5 Quell-Lagerplatz** zeigt in der Stückliste in jeder Zeile „NAN“ (auch am 19.10. gesehen).
- **Z6 Stückliste, Filter mit Treffern nur in zugeklappten Ebenen:** Die Tabelle ist leer und zeigt keinen Hinweis, bis man
  „Alle aufklappen“ drückt (B11.5, Baum-Vorrang).

## Eigener Fehler im Lauf

Nach einem Neuladen der Stückliste hat sich der vom Werkzeug ermittelte Feldverweis verschoben. Zwei Eingaben (`G-KT-Lade`,
`(leer)`) landeten im Filter „Pos.“ statt „Matchcode“. Bemerkt habe ich es über die DevTools (Filterwert in `position`). Ich
habe den Filter gelöscht, B11.5 mit Fokus über `data-col-key` wiederholt und die Ergebnisse oben nur aus der Wiederholung
genommen. Am Ende waren alle Filter leer und die Spaltenauswahl unverändert.
