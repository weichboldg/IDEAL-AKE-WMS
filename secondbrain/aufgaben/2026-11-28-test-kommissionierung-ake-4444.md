---
type: aufgabe
status: erledigt
created: 2026-11-28
quelle: "Gerald im Chat 2026-11-28: „Mir kommi rechte geben und testen“"
---
# Test Kommissionierung AKE-Test :4444 (v1.99.89)

Anmeldung „Mit Windows anmelden“ als Gerald Weichbold (AD, ohne Kennwort). Gehört zu [[2026-11-28-rauchtest-ideal-test-88]].

## Einrichtung
- `/Users/Edit/14` (Gerald Weichbold, AD): Rolle **Kommissionierer** + **Ist Kommissionierer** gesetzt → gespeichert, Liste zeigt „Administrator Kommissionierer“.
  Damit ist der Hinweis „kein Benutzer als Kommissionierer gekennzeichnet“ am Leitstand behoben (Auswahl zeigt jetzt Gerald Weichbold).

## Ablauf und Ergebnis
| Schritt | Ergebnis |
|---|---|
| `/PickingLeitstand` → „Zur Kommissionierung freigeben“, Kommissionierer Gerald Weichbold | OK: „1 Kommissionierauftrag … angelegt (KO-2026-00017)“, FA 2618092 |
| `/Picking` | OK: KO-2026-00017, Bereich Lager, Status Offen, Kommissionierer Gerald Weichbold |
| `/Picking/Auftrag/17` Stückliste | OK, 76 Positionen; ein alter Spaltenfilter „Bezeichnung 1 = Verdampfer“ war noch aktiv (gespeicherte Ansicht) – Chip-× entfernt ihn |
| Pos. 10 (BG 87058429 Lüfterblech) markieren, Quelle NAN, Ziel Wagen 1 | OK: Warnung „Kommissionierwagen belegt“ (2602011; 2602108; 2618052) → Abbrechen |
| Ziel Wagen 2 | Warnung belegt (2616156) → „Trotzdem buchen“ → Abfrage „Kommissionierung Status“ → „Weiter kommissionieren“ |
| `/StockMovements` | OK: 28.11. 12:14 87058429 1,000 NAN → Wagen 2, Umbuchung, Gerald Weichbold, FA 2618092 |
| `/SageBookingQueue` | kein neuer Eintrag – erwartet: Umbuchungen gehen nur mit `SageLagerbuchungUmbuchungAktiv=true` an Sage (Default false) |

**Keine neuen Fehler.** Nicht getestet: Auftrag abschließen (teilweise/vollständig), Ausbuchung an Sage aus der Kommissionierung, Fehlteil/Bestellen.

## Beobachtungen (kein Jira, offen für Gerald)
- Lagerplatz **„NAN“** (Id 1) ist Quell-Lagerplatz aller Positionen – Testdaten-Artefakt am :4444? Prüfen, ob es den Platz in Sage gibt.
- Wagen 1/2 sind mit Teilen alter FAs belegt (2602011, 2602108, 2618052, 2616156) – Testdaten, ggf. „Lagerplatz ausbuchen“.
- Freigabe-Knopf: geklickt über die Element-Suche (nicht nach Bildschirmposition), angelegt wurde FA 2618092 statt der optisch ersten Zeile 2604043. Ursache unklar (Zuordnung der Element-Suche); bei Gerald mit Klick nachprüfen, bevor es ein Bug wird.
