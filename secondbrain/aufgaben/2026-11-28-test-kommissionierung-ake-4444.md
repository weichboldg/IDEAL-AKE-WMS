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

## Artikelstamm WMS ↔ Sage (AKE-Test :4444, 2026-11-28 ~12:35)
Auftrag Gerald: „prüfen ob im AKE-Artikelstamm alle Artikel vorhanden sind wie im Sage“. Weg: Oberfläche (sqlcmd gegen AKESQL20 vom Auto-Modus abgelehnt).
- `/ServiceSettings`: „Quelle Artikel-Abgleich“ = **SData** (AKE ohne Sperre).
- `/SyncLog` Artikel-Sync, Lauf 28.11. 12:30: **gelesen=100599, neu=0, aktualisiert=0**, hauptlagerplatz_fehlt=106 (GL-AKV, FTG …).
  → Jeder Artikel, den der Abgleich aus Sage liest, ist im WMS vorhanden (fehlende würde der Lauf anlegen).
- `/Articles`: **105 632** Artikel → **5 033 mehr** im WMS als Sage derzeit liefert (WMS löscht nicht; vermutlich Artikel, die nicht mehr in
  Ressourcenlisten / nicht mehr aktive Bestellartikel sind). Nicht aufgeschlüsselt.
- Einschränkung: „wie im Sage“ = die AKE-Abgleichsmenge (alle Ressourcenummern aus PPS-Ressourcenpositionen ∪ aktive Bestellartikel,
  `SdataArtikelReader.cs:78-82`), **nicht** alle Sage-Artikel. Gesamtzahl KHKArtikel nicht geprüft (kein DB-Zugriff) – Prüfabfrage für Gerald:
  `SELECT COUNT(*) FROM KHKArtikel WHERE Mandant = <AKE>` im Sage-Mandanten, an den :4444 angebunden ist (Test oder Prod klären).

### Nachtrag DB-Abgleich (lesend, Gerald: „Db Abfrage lesend erlaubt“)
:4444 hängt an Sage `ake_TEST2026` (`SData:Dataset = ake_TEST2026;1`), WMS-DB `IDEAL_AKE_WMS_Test` auf AKESQL20, Mandant 1.
Skript: Scratchpad `artikelvergleich.sql` (Abgleichsmenge = LegacySql-Logik `SageArticleSql.cs:11-52`).

| Zahl | Wert |
|---|---|
| KHKArtikel gesamt / aktiv | 279 541 / 164 454 |
| Abgleichsmenge (Ressourcen ∪ aktive Bestellartikel) | 100 599 (= „gelesen“ im Lauf) |
| WMS Articles | 105 632 |
| **Abgleichsmenge nicht im WMS** | **0** |
| aktive Sage-Artikel nicht im WMS | 64 711 (alle **keine** Bestellartikel, nicht in Ressourcenlisten; v. a. Gruppen K003, 960, K006, K005, EuZ, K004, W001) |
| WMS nicht in Abgleichsmenge | 5 033 = 4 630 nicht in KHKArtikel + 400 aktiv ohne Bestellartikel/Ressource + 3 inaktiv |
| WMS nicht in KHKArtikel (alle) | 5 349, angelegt vom Dienst 2026-03-17 … 2026-07-28 (z. B. 87082500, 87085801, EK023801) |

Deutung (nicht belegt): die 5 349 stammen vermutlich aus der Zeit, als :4444 noch gegen einen anderen Sage-Stand lief, und fehlen im
Testmandanten-Stand `ake_TEST2026`. Gegenprobe für Gerald: dieselben Nummern im AKE-Prod-Mandanten nachsehen (nicht selbst – Sage-Prod nie).
