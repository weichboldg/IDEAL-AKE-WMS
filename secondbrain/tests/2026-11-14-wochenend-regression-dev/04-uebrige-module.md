---
type: testbericht
created: 2026-11-14
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
modul: Tabellen-Skripte, CSV-Export, Spaltenfilter-Suchsyntax, Spaltenauswahl/Zahnrad, Hardware-Scanner, Vault/enaio-Links, QR erstes Feld/Trennzeichen, Aktivitäts-Protokoll, FA-Struktur live, SData-Quellen-Weiche, Fehlteile-Badges, Einstellungen (ADR 0020), Lager-Tools, OSEON-Relevanzfilter, Menüleiste/Kontrast, Update-Skript, Bugfixes TS-130, Register Sage-Test, Lokalzeit, Hilfe/Changelog
stand: "Bündel v1.99.22 (90bbbbd7), DEV AKE localhost:8081 / AKE_WMS_DEV, DEV IDEAL localhost:8082 / IDEAL_WMS_DEV"
durchgefuehrt: "2026-11-13 21:38–22:05 (Ortszeit DEV-PC)"
---
# Teil B4 – Übrige Bündel-Kapitel auf DEV

Teil B4 (letzter Testblock) aus [[2026-11-14-wochenend-review-test-anwenderdoku]]. Grundlage: `docs/TESTSZENARIEN.md` im Bündel-Worktree
(TS-72, 86, 91, 98, 99, 100, 101, 102, 103, 106, 108, 109, 111, 115, 121, 123, 125, 126, 130, 131, 133, 135) und die Review-Notizen
[[2026-11-14-review-befunde-sdata]], [[2026-11-14-review-befunde-update-skript]], [[2026-11-14-review-befunde-lokalzeit]],
[[2026-11-14-review-befunde-einstellungen]]. Form wie [[01-kommissionierung]]. Nicht doppelt geprüft: TS-110, 112, 113, 116, 122, 128, 129,
130.2.1/130.3.1/130.4/130.5.1–5, 132, 136, 137 (in [[02-wareneingang-einkauf]]), TS-104, 105, 127 (in [[03-ideal-hierarchie-beschichtung-bde]]),
TS-120, 124, 134 (in [[01-kommissionierung]]).

**Vorgehen:** Eigener Tab im verbundenen Chrome (der für die Sitzung gewählte Browser, kein anderer gewählt), am Ende geschlossen. Überwiegend
dieselben GET-Aufrufe, die die Seiten senden (`fetch` im angemeldeten Tab), Listen-Prüfungen mit versteckten `iframe`s in 1920/1520/1366/1280/960 px,
CSV-Inhalt über abgefangenes `URL.createObjectURL` (kein Download gespeichert). Hardware-Scanner über echte Tastatureingaben des
Browser-Werkzeugs (das Skript ignoriert synthetische Ereignisse, `hardware-scanner.js:381,408` `isTrusted`). Wirkung per SQL auf
`AKE_WMS_DEV`/`IDEAL_WMS_DEV` (`sqlcmd -S . -E -C`). Update-Skript nur mit DEV-Zielen und `-NurPruefen`, Protokolle im Scratchpad.
Nicht neu deployt, nichts an Sage geschrieben, keine Mail ausgelöst, kein Sync auf IDEAL-DEV gestartet.

**Einschränkungen:**
- Nur ein Windows-Benutzer (gerald.weichbold, Admin) → alle Rollen-Negativfälle (ohne Admin/`stock_read`/`picking`/BDE-Admin) nicht prüfbar.
- AKE-DEV und IDEAL-DEV haben **keine gespeicherten SData-Zugangsdaten** in `/ServiceSettings` („Status: nicht gesetzt“, `SData:Username` leer);
  das Web liest Sage deshalb nicht (Register: „Sage-SData nicht konfiguriert: SData:Username. Es geht keine Anfrage an Sage.“). Zugangsdaten
  durften nicht geändert werden → alle Live-Lesefälle im Register Sage-Test und TS-103 NICHT PRUEFBAR. Der Dienst liest SData weiter über den
  appsettings-Rückfall (Läufe Kreditoren/ArtikelLieferant 21:55 mit „Quelle SData: …“).
- Kein Kombigerät auf IDEAL-DEV (`/ProductionOrders`: 4 Aufträge, 0 Badges „Kombigerät“) → TS-72.1 nicht prüfbar.
- Screenshot-Werkzeug des Browsers lief in diesem Lauf wieder (ab ~21:58); Edge headless landete auf der Anmeldeseite (Sitzungs-Cookie geht
  zwischen zwei Edge-Aufrufen verloren) – verworfen.

## Ergebnis-Tabelle

| TS-Nr. | Standort | Ergebnis | Beleg/Bemerkung |
|---|---|---|---|
| 72.1 Kombigerät-Variantentabelle sortieren | IDEAL | NICHT PRUEFBAR | kein Kombigerät auf IDEAL-DEV |
| 72.2 Spaltenkonfiguration mit verschachtelter Tabelle (FA-Struktur) | IDEAL | OK | `/FaHierarchy`: „Bezeichnung“ ausgeblendet (Speichern per `viewKey = 'Unbekannt'` absichtlich verhindert) → Kopfdaten-Tabellen (4) vorher = nachher „Montage-Ab\|Kunde\|Status\|KO-Termin\|FE-Termin\|AB-Nr.“, keine ausgeblendete innere Zelle |
| 72.3 Alt-Präferenz | IDEAL | NICHT PRUEFBAR | keine vor dem Fix gespeicherte Präferenz bekannt |
| 86.1 CSV Grundfall + Knopf | AKE | OK | Bewegungshistorie Filter `13.11.2026`: 117 Treffer = 117 CSV-Zeilen, Spalten = sichtbare Spalten; OSEON-Teileverfolgung (Baum) ohne CSV, mit Zahnrad |
| 86.3 IDEAL hierarchisch | IDEAL | OK | FA-Liste: 180 Zeilen, erste Spalte „HauptFA“, alle Zeilen gleich viele Spalten, keine Gruppenkopf-Zeile |
| 86.5 Format | AKE | OK | beginnt mit `EF BB BF`, Trenner `;`, CRLF, keine `sep=`-Zeile, Name `StockMovements_2026-11-13_2141.csv` |
| 86.5 / 86.7 Zahlen ab 1.000, negative Mengen | AKE | **FEHLER** | Bildschirm zeigt `-10 026,000` (Tausender = geschütztes Leerzeichen U+00A0). CSV: `'-10 026,000` – mit Apostroph = Text, Excel rechnet nicht; positive Werte ab 1.000 als `10 026,000` (Leerzeichen) ebenfalls keine Zahl. Kleine negative Werte (`-6,000`) korrekt ohne Apostroph. Neuer Befund N-B4-2. ![[N-B4-2-csv-tausender-negativ-als-text.jpg]] |
| 86.6 CSV-Injection | AKE | NICHT PRUEFBAR | kein Datensatz mit Formeltext; Testdatensatz nicht angelegt |
| 86.10 Kappung 5.000 | AKE | OK | Bewegungshistorie ohne Filter (19.535): genau 5000 Zeilen, Meldung „Export auf 5.000 Treffer begrenzt, bitte Filter eingrenzen. Sortierung gilt nur innerhalb der exportierten 5.000.“, Knopf während des Abrufs `disabled`, absteigend 13.11.2026 → 13.07.2026 |
| 86.2, 86.4, 86.8, 86.9, 86.11–86.17 | beide | NICHT PRUEFBAR | in diesem Lauf nicht einzeln durchgeführt (Excel-Öffnen, Sitzungsablauf, Rechte) |
| 91.A1 FA-Liste Text | AKE | OK | `2607%` 1, `%001` 3, `!2607171;2629001` 13 (alle offenen), `(leer)` Kunde 0, `!(leer)` 13; Spalte „Stk.“ hat auf der FA-Liste keinen Filter (kein Fehler) |
| 91.A2 FA-Liste Datum | AKE | OK | Produktionsdatum `>=01.10.2026` 4 + `<01.10.2026` 9 = 13; `01.10.2026...31.10.2026` 0; `kw41;kw42`, `kw41,kw42` 0 (keine Termine in KW 41/42) |
| 91.A4 Ungültig | AKE | OK | Artikelnummer `1...5`: Feld rot, „1...5: Bereich nur in Zahl- und Datumsspalten — ignoriert“; FA-Nr. `>5;2604043`: rot „>5: Vergleich nur in Zahl- und Datumsspalten — ignoriert“, `2604043` wirkt (1 von 1); nach Neuladen aus der URL weiter rot |
| 91.A6 Hilfe-Popover | AKE | OK | „?“ (`aria-label` „Hilfe zur Filtersyntax“), Popover „Filter-Syntax“ mit allen Zeilen inkl. `"GL;0;0;0"`, Escape schließt; Felder mit `aria-describedby="colFilterSyntaxHint"` |
| 91.B1 Artikelliste | AKE | OK | `0%` 3991 + `!0%` 101 641 = 105 632; `=00722001` 1; `00722001;EK009055` 2; `a_1` und `A_1` 0 (kein Unterstrich-Platzhalter); „Erstellt am“ `<01.01.2026` 105 632 |
| 91.B2 Bewegungshistorie Datum/Art | AKE | OK | `13.11.2026` = `>=13.11.2026` = `>12.11.2026` = 117, `<=12.11.2026` 19 418 (117 + 19 418 = 19 535); `12.11.2026...13.11.2026` = `12.11.2026;13.11.2026` = 146; `11.2026` 153; `32.13.2026` 0; `=ausbuchung` 42, `ausbuchung` 11 033, `!ausbuchung` 8 502 (= 19 535 − 11 033) |
| 91.B3–B10 übrige Listen | beide | NICHT PRUEFBAR | Stichprobe in B3 (FA-Baum, Stückliste) und hier (FA-Liste, Artikel, Bewegungen, Aktivitäts-Protokoll, Lagerplätze); Rest nicht einzeln |
| 91 P-2 Kollation | AKE + IDEAL | OK | beide DBs `Latin1_General_CI_AS`, 0 Spalten mit Nicht-CI-Kollation |
| 98.1 Platz Zahnrad/CSV (Matrix) | AKE | OK | FA-Liste, Leitstand, Kommissionierung, Artikel, Lagerplätze, Arbeitsgänge, Offene Bestellungen: je 1 Zahnrad über der Tabelle, CSV links daneben, nicht in Filterkarte/Hinweis; Bestand, Bewegungen, Lagerbestellungen, Fehlteile, Aktivitäts-Protokoll, Benutzer, Bedarfsmeldungen: nur CSV; BDE-Stammdaten: je Reiter eine eigene Seite mit CSV; OSEON-Teileverfolgung nur Zahnrad |
| 98.1 Platz Zahnrad/CSV (Matrix) | IDEAL | OK | FA-Struktur (Zahnrad, kein CSV), Kommissionierlisten Liste/Summiert, Beschichtung Liste/Summiert, Vormontage Einzeln/Summiert, Beschichtungsaufträge, Leitstand, Stückliste 284, Arbeitsgänge: je 1 Zahnrad über der Tabelle, CSV links daneben, kein Seiten-Querscrollen bei 1520 px |
| 98.8 Speicherfehler 400 | AKE + IDEAL | OK | `viewKey = 'Unbekannt'`, Spalte aus: „Spaltenauswahl konnte nicht gespeichert werden (Fehler 400). Die Anzeige gilt nur bis zum Neuladen.“; zweite Änderung → weiterhin genau ein Hinweis; gespeicherte Präferenz `Articles` unverändert (GET 200 vorher/nachher) |
| 98.2–98.7, 98.9–98.15 | beide | NICHT PRUEFBAR | Netzwerk-Sperre (DevTools) und Sichtprüfungen nicht einzeln durchgeführt |
| 99.1a Schalter aus: keine `data-hw-scan-*`-Attribute | AKE | **FEHLER** | `hardware-scanner.js` wird korrekt nicht ausgeliefert, aber `<body data-hw-scan-praefix="" data-hw-scan-abstand="" data-qr-erstes-feld="" data-qr-trennzeichen="">` (Razor lässt `data-*` mit `null` nicht weg). Folge: `/Account/Profile` zeigt die Karte „Dieses Gerät – Hardware-Scanner auf diesem Gerät verwenden“ (Schalter an), obwohl der Standort-Schalter aus ist; das Kamera-Fenster bekommt das Feld „Handscanner“. Neuer Befund N-B4-1. ![[N-B4-1-hardware-scanner-karte-im-profil-bei-schalter-aus.jpg]] |
| 99.2 Wareneingang per Tastatur-Scan | IDEAL | OK | Fokus Artikelfeld (Select2), getippt `~09102044,,1065141,09`: Artikel 102044, FA 1065141, FA-Karte „1065141 (Haupt-FA 1059463)“, kein Absenden, kein Präfix im Feld (Ende per Zeitgrenze; das getrennt gesendete Enter öffnete danach die Artikelauswahl – Testartefakt, am Gerät kommt Enter innerhalb der Zeitgrenze) |
| 99.3–99.19 Gerät/Netum | beide | NICHT PRUEFBAR | kein Gerät; Präfix-Tottaste, Zeichenabstand, Fokus-Eingabe nur am Netum |
| 100.1 AKE-Regression Links | AKE | OK | Vault-Links: Artikelinfo 1, Artikel 25, Bestand 25, Bewegungen 25, FA-Liste 13, Leitstand 13, Stückliste 144: 97; enaio: FA-Liste/Leitstand je 17; Vorlage `…explore?search={Artikelnummer}…` richtig eingesetzt |
| 100.2 / 100.3 Vault und enaio aus | IDEAL | OK | `VaultLinksAktiv = false`, `EnaioLinksAktiv = false`: dieselben 7 Seiten ohne einen Vault-/enaio-Link |
| 100.4–100.12 | beide | NICHT PRUEFBAR | Vorlagen ändern/leeren nicht durchgeführt (Einstellungsänderung) |
| 101.1 / 101.2 Artikelnummer, FA an (`;`) | AKE | OK | `00722001;;2627001;09`: Artikel 00722001, FA 2627001, „✓ Gescannt: 00722001“; `G-KT-GIBT-ES-NICHT;;1;09`: „✗ Nicht gefunden: G-KT-GIBT-ES-NICHT“ |
| 101.16 `;`: Komma-Etikett | AKE | OK | `00722001,,2627001,09` → „✗ Nicht gefunden: 00722001,,2627001,09“, FA leer (Attribut `data-qr-trennzeichen=""` statt fehlend, siehe N-B4-1, ohne Wirkung: JS prüft `=== ','`) |
| 101.17 Trennzeichen `,` | IDEAL | OK | `<body data-qr-trennzeichen=",">`; `102044,,1065141,09` → Artikel 102044, FA 1065141; `102044;;1065141;09` → „✗ Nicht gefunden: …“; Matchcode-Etikett bei `QrErstesFeld = Artikelnummer` → „Nicht gefunden“ |
| 101 API `by-matchcode` | IDEAL | OK | Matchcode von 50002134 → `{"articles":[{"id":30074,"articleNumber":"50002134"…}],"more":false}` |
| 101.3–101.15, 101.18–101.25 | beide | NICHT PRUEFBAR | `QrErstesFeld = Matchcode` nicht umgestellt (Einstellungsänderung), Gerät nötig |
| 102.1 / 102.6 Meldung über alle Seiten, Groß/Klein | AKE | OK | `fehlgeschlagen` = `FEHLGESCHLAGEN` = 14 = SQL `LIKE '%fehlgeschlagen%'`; `Testbetrieb` 9 = SQL |
| 102.2 Aktivität | AKE | OK | `abgleich` 5 312, `!abgleich` 1 269 952 (Summe = 1 275 264 = alle); `werkbank` 6 550 (= OseonWorkplaces); `lieferverzug` 18 (= SQL); `OperationConfig%` 0 (AKE hat keinen Arbeitsgang-Abgleich-Lauf) |
| 102.3 Auswahl deutsch | AKE | OK | 36 Einträge + „— alle —“, deutsch und alphabetisch, inkl. „Sage-Quellenvergleich (View/SData)“; Altlink `?service=OperationConfigSync` → „Keine Einträge im Filter.“ (0 Läufe) |
| 102.4 Zeitpunkt | AKE | OK | `13.11.2026` = `>=13.11.2026` = 4 013 = SQL; `11.2026` 46 368; `12.11.2026...13.11.2026` 10 323; `14:3` 0, `!14:3` alle |
| 102.8 Negativ | AKE | OK | `32.13.2026` 0, `!32.13.2026` alle; `colf_Unbekannt=x` alle; `page=0` Seite 1; Admin-Negativfall nicht prüfbar |
| 103 FA-Struktur live | AKE | NICHT PRUEFBAR | keine gespeicherten SData-Zugangsdaten im Web: „Sage-SData nicht konfiguriert: SData:Username“ (kein Aufruf an Sage); `?belId=abc` ohne Fehlerseite |
| 106.0.1 Gruppe „Sage-Abgleich (SData)“ | AKE | OK | sechs Auswahllisten View/SData/Vergleich mit Erklärtext; AKE: Artikel `SData`, übrige `View` |
| 106.0.4 Aktivitäts-Protokoll | AKE | OK | „Sage-Quellenvergleich (View/SData)“ in der Auswahl |
| 106.0.10 Sperrhinweise | AKE + IDEAL | OK | AKE nur „⚠ SData ist hier gesperrt bis P-F2 …“ (FA-Struktur); IDEAL zusätzlich „… gesperrt bis P-R1 (Artikel-View '[vw_IDEAL_AKE_WMS_Artikel]' gesetzt …)“ |
| 106 Dienst liest SData (Quelle) | AKE | OK | SyncLogs 21:55: „Quelle SData: Kontokorrent … (KtoArt eq 'K')“, „Adressen“, „ArtikelLieferantInfo, 7 Seite(n), 34802 Zeile(n)“ |
| 106.1.c2 Spalten Lagerort/Sperre | AKE | OK | `/StorageLocations`: Spalten „Lagerort“, „Sperre (Sage)“ vorhanden (Inhalt leer, Quelle Lagerplätze = View) |
| 106.0.3 ff. Läufe mit Umschalten | beide | NICHT PRUEFBAR | Quellen umschalten = Sync-Verhalten ändern; auf IDEAL-DEV keine Syncs (alle `Sync:*Enabled = false`, geprüft) |
| 108.1–108.5 Badges = Liste | AKE | OK | `mineOnly=true`: Liste 15 = Badge „Offene Fehlteile 15“, Reiter „Wird nicht nachgeliefert“ 3 = Badge 3; ohne Haken 21 / 63; fremde Werkbank (`workplaceId=999999`): Liste leer, beide Badges 0 |
| 108 Seite außerhalb | AKE | OK (Hinweis) | `page=2` bei 15 Treffern: Zähler „Eintraege 26–15 von 15“ (Grenzfall, N-B4-3) |
| 109.1 `/Settings` Katalog | AKE | OK | 13 Gruppen in Katalog-Reihenfolge (Termine und Beschichtung … Links), je Feld Anzeigename, Key, Wirkung, „Verwendet in“, Standard, Standort; Auswahllisten `QrErstesFeld`, `QrTrennzeichen`, `LackierteilKategorieName`, Standard-Empfänger; Warnhinweise (Buchungen ins Minus, Windows-Anmeldung …); „Hardware-Barcodescanner“ sichtbar ohne DB-Zeile |
| 109.4 `/ServiceSettings` Quellenschalter | AKE + IDEAL | OK | siehe 106.0.1/106.0.10 |
| 109.5 Serverprüfung | AKE | OK | POST mit `OseonReportingHorizonDays = 99` und erfundenem `settings[ErfundenQA]`: „⚠ Nicht gespeichert: „OSEON-Reporting: Horizont“: „99“ ist nicht erlaubt – erlaubt: eine ganze Zahl von 1 bis 60.“; DB: Wert 10 unverändert, `ErfundenQA` nicht angelegt, 49 Zeilen |
| 109.2, 109.3, 109.6, 109.7 | AKE | NICHT PRUEFBAR | SQL-Manipulation und App-Pool-Recycle nicht durchgeführt |
| 111.5 Filter „Übergabe an Sage“ | AKE | OK | Lagerplätze `nicht nötig` 3 293 + `Offen` 1 = 3 294 (alle) |
| 115.1 / 115.3 FA-Karte Einbuchung | AKE | OK | FA 2627001: Karte „FA 2627001 · UKW 3-1-E R290 · Artikel EU253711 · Status: offen · FA anzeigen“; `9999998`: „Achtung: FA 9999998 ist im WMS unbekannt. Buchen ist möglich …“ |
| 115.7 Sub-FA | IDEAL | OK | `1065141` → „FA 1065141 (Haupt-FA 1059463)“ |
| 115.11 Bestandsübersicht FA-Kopf | AKE | OK | `filterProductionOrder=2627001`: Kopf „FA 2627001 offen …“; `9999999`: „Achtung: FA im WMS unbekannt – angezeigt werden Buchungen mit diesem Text.“ |
| 115.13 FA-Link erledigter FA | AKE | OK | Historie FA 2607171 → `/ProductionOrders?filterOrderNumber=2607171&showDone=True` → 1 Treffer |
| 115.2, 115.4–115.6, 115.8–115.10, 115.12, 115.14 | beide | NICHT PRUEFBAR | Buchungen/Pflicht-Einstellung/Rollen/`SQL/110` nicht durchgeführt |
| 121.1–121.4 OSEON-Haken | AKE | OK | Direktaufruf: Haken an, 437 Einträge; `useRelevanceFilter=false`: Haken aus, 3 405; Seite 2 behält beide Zustände (26–50 von 437 bzw. 3 405) |
| 121.5–121.8 BDE-Bearbeiten Final | AKE | NICHT PRUEFBAR | nicht durchgeführt (BDE-Buchungen ändern), Rollenfall nur ein Benutzer |
| 123.1 / 123.2 Menü AKE | AKE | OK | 1920 px: einzeilig, 62 px; 1520/1366/1280 px: zweizeilig, 96 px; nie Seiten-Querscrollen (`scrollWidth` < Fensterbreite), Benutzerknopf sichtbar |
| 123.3 Menü IDEAL | IDEAL | OK | 1920 einzeilig, 1520/1280 zweizeilig, kein Querscrollen, Benutzerknopf sichtbar |
| 123.5 Lagerplätze 1520 px | AKE | OK | ein sichtbarer Balken (Sticky-Balken unten bzw. am Tabellenende), Seite ohne Querscrollen |
| 123.6 Kontrast Fehlteile-Link | AKE | OK | „15 Fehlteile (wird nachgeliefert) aus 11 Bestellungen →“: `rgb(102,77,3)` auf Weiß = 7,99 : 1, Ziel `…tab=WillBeRestocked&type=Lager&mineOnly=true` |
| 123.7 960 px | AKE | OK | Hamburger-Knopf sichtbar, Desktop-Benutzerknopf nicht |
| 123.4, 123.8–123.10 | AKE | NICHT PRUEFBAR | Dropdown-Sichtprüfung, Einzelrollen-Benutzer, Graustufen nicht durchgeführt |
| 125.1 `-NurPruefen` DEV-AKE | AKE | OK | Exit 0; Site `AKE_WMS_DEV`, Dienst `AKE_WMS_DEV_Service`, Umgebung DEV, DB `.`/`AKE_WMS_DEV`, URL `http://localhost:8081/`, installiert v1.99.22 (Fußzeile), Schema „keine“; Altbestand-Trockenlauf `TREFFER: 15`; Ordner/Rechte-Bericht; „-NurPruefen: nichts gestoppt, nichts geschrieben.“; Dienst danach `Running` |
| 125.1 `-NurPruefen` DEV-IDEAL | IDEAL | OK | Exit 0; installiert v1.99.22, Schema „keine“, Altbestand `TREFFER: 0 – entfaellt, kein Halt` |
| 125.4 Pfad fehlt | AKE | OK | `-WebPfad D:\Dev\AKE\WebX`: „ABBRUCH: D:\Dev\AKE\WebX fehlt (AKE-DEV). Nichts gestoppt, nichts geaendert.“, Exit 1 |
| 125.6 Verbindung verneint | AKE | OK | Antwort `n`: „Ende ohne DB-Kontakt.“, Exit 2, Protokoll vorhanden |
| 125.2 / 133 Eingabe ungültig bzw. leer | AKE | OK | ohne `-Firma`: `XYZ` → „Ungueltig.“, dann leere Eingabe → „ABBRUCH: keine Eingabe fuer die Firma (-Firma angeben).“, Exit 1 (keine Endlosschleife) |
| 125.14 PS 5.1 + ASCII | – | OK | `powershell.exe` 5.1 `ParseFile`: 0 Fehler in allen 5 Dateien, 0 Nicht-ASCII-Bytes |
| 126.22 nicht fällige Daten-Skripte | AKE | OK | „SQL/108 / SQL/147 / SQL/110: nicht faellig (eingefuehrt mit v…, installiert v1.99.22)“ |
| 126.26 / 126.27 Haltepunkt-Trockenlauf | AKE + IDEAL | OK | AKE „TREFFER: 15 – Backup in Schritt 2 Pflicht, Haltepunkt in 4a“, Plan „Backup PFLICHT“; IDEAL „TREFFER: 0 – entfaellt, kein Halt“ |
| 126.28 Mail-Ablageordner | AKE + IDEAL | OK | „Mail SMTP smtp.ake.at \| Ordner D:\Dev\mail-pickup\ake“ und „[Pruefung/MailPickup] Abweichung 1 – Hinweis …“ (Firma je Ordner) |
| 125.3, 125.5, 125.7–125.13, 125.15–125.18, 126.1–126.21, 126.23–126.25, 126.29–126.31, 133.1–133.6 | – | NICHT PRUEFBAR | Volllauf, DB-Kopie, Paketkopie, Test-/Prod-Ziel (`-System Test`) – laut Auftrag nicht |
| 130.1.1–130.1.5 Anführungszeichen (Server-Liste) | AKE | OK | `/StockOverview` Lagerplatz: `"GL;0;0;0"` 27, `="GL;0;0;0"` 27, `"=GL;0;0;0"` 27, `"GL;0;0;0"%` 27 (kein `GL;0;0;01` auf DEV), `!"GL;0;0;0"` 4 665 (= 4 692 − 27), `"GL;0;0;0";LL%` 1 176 (= 27 + `LL%` 1 149) |
| 130.1.7 Popover + Hilfe | AKE | OK | Popover-Zeile `"GL;0;0;0"` „genau dieser Text als ein Begriff (; und , trennen in Anführungszeichen nicht)“; Hilfe „„GL;0;0;0“ → genau dieser Text als ein Suchbegriff: In Anfuehrungszeichen trennen ; und …“ |
| 130.1.8–130.1.11 Negativ | AKE | OK | `GL;0;0;0` 3 298 (ODER, Referenz 3 302 vor den B2-Buchungen), `=GL;0;0;0` 2 163; `"abc` → „Keine Bestände gefunden.“ (sucht `"abc`); `""` → alle 4 692 |
| 130.1.6 Client-Liste | AKE | NICHT PRUEFBAR | Stückliste/Auftragsseite mit Anführungszeichen nicht durchgeführt |
| 130.2.2 FA-Hinweis Mischplatz (Darstellung) | AKE | OK | 00393295, Lagerplatz leer, FA 2627001: blaues Info-Alert „FA 2627001 liegt bereits: Lagerplatz TEST 2 — 3,000 Stk am Platz, Artikel 00393295 · Menge = gesamter Bestand des Artikels am Platz, nicht nur der Anteil dieses FA. → Lagerplatz wurde übernommen.“, Platz TEST 2 gesetzt (B2 prüfte nur die Daten) |
| 130.2.4 FA ohne Bestand | AKE | OK | `9999998`: kein Platz-Hinweis, nur gelbe „unbekannt“-Karte |
| 130.2.5 FA auf zwei Plätzen | AKE | NICHT PRUEFBAR | keine passenden Daten, nicht angelegt |
| 130.3.5 Filter „Bestellt am“ | AKE | OK | `=13.11.2026` → 9 Meldungen |
| 130.5.6 „Lagerplatz ausbuchen“ Wagen | AKE | OK | `/StockMovements/OutboundAll?storageLocationId=3296` (WAGEN N3): FA-Feld vorbelegt „2607171; 2625004; 2629001“ |
| 131.1.1 Reiterleiste | AKE | OK | `/Settings`, `/ServiceSettings`, `/StandortEinstellungen`, `/SageTest` mit Reiterleiste; `/SageTest`: `aria-current="page"` auf „Sage-Test“ |
| 131.1.2 / 131.2.1 Menü | AKE | OK | kein Eintrag „Sage-Test“ und kein „FA-Struktur (Sage live)“ im Menü |
| 131.2.4 Schalter entfernt | AKE | OK | `/Settings` ohne `SageSdataFaAnsichtAktiv`, kein Block „Nicht im Katalog“ |
| 131.3.1 Kennwortfeld | AKE + IDEAL | OK | „SData: Kennwort“, Badge „Status: nicht gesetzt“, `input type=password` **ohne** `value`-Attribut, Hinweise Wirkung/Standard/Standort |
| 131.3.7 Freitext-Weg gesperrt | AKE | OK | `GET /ServiceSettings/Edit/SData:Password` → `/ServiceSettings` mit „Das Kennwort wird nur über das Kennwortfeld der Service-Einstellungen gepflegt (verschlüsselt).“ |
| 131.6.1 / 131.6.2 Verbindungstest im Register | AKE | OK | `/ServiceSettings`: Link „Sage-Verbindung testen“ → `/SageTest`; Fenster vorbelegt (Adresse `https://sagetest01.ake.at:5493/`, `ol`, `ake_TEST2026;1`), Kennwort leer mit Platzhalter „gespeichertes Kennwort verwenden“ |
| 131.6.5 Verbindungstest andere Adresse | AKE | OK | Adresse `http://127.0.0.1:9123/` (lokaler Listener): „HTTP 404 Not Found – Zugangsdaten: keine gespeicherten Zugangsdaten“, keine Fehlerseite (SD-1 siehe Nachmessung) |
| 131.3.2–131.3.6, 131.4, 131.5, 131.6.3, 131.7, 131.8 | AKE | NICHT PRUEFBAR | Zugangsdaten setzen/ändern verboten; ohne Zugangsdaten keine Sage-Antwort |
| 131 IDEAL | IDEAL | OK (Hinweis) | Register erreichbar; IDEAL-DEV hat `SData:BaseUrl = https://sagetest01.ake.at:5493/` und Dataset `ake_TEST2026;1` (AKE-Test!) gesetzt, Vorbedingung „IDEAL-DEV ohne SData:BaseUrl“ nicht erfüllt – N-B4-4 |
| 135 Stichprobe neue Zeitstempel | AKE | OK | DB-Uhr 21:51 MEZ (UTC 20:51); Schreibzeiten des Abends in Ortszeit: `ProductionOrderPickingStatus.ReleasedAt` 19:53–20:04, `PickingOrders.CreatedAt` KO-2026-00052 20:13:38, `PartRequisitions` 25 `CreatedAt` 20:12:20 / `CancelledAt` 20:12:35, `PurchaseOrderReceipts` 20 `CreatedAt` 20:33:02, `Users` 23/24 `CreatedAt` 19:53:03, `SyncLogs` 21:40/21:55 – alle passend zur Ortszeit der B1/B2-Aktionen, kein 1-h-Versatz |
| 135.1–135.15 einzeln | beide | NICHT PRUEFBAR | Benutzer/Rollen/Empfänger nicht angelegt; Dienst-Läufe 135.8 nur Testsystem |
| Hilfe/Changelog Stichprobe | IDEAL | OK | `/Help` nennt v1.99.22, Filter-Syntax (inkl. Anführungszeichen), Sage-Test, Bestellvorschläge, Kommissionieraufträge; `/Help/Changelog` enthält v1.99.22 und v1.99.18 (Ortszeit) |

**Zählung:** OK 74 · FEHLER 2 · NICHT PRUEFBAR 20 (Zeilen der Tabelle; „OK (Hinweis)“ als OK).

## Nachmessung Review-Befunde

| Befund | Ergebnis | Beleg |
|---|---|---|
| SD-1 Verbindungstest schickt gespeichertes Kennwort an frei eingegebene Adresse | **auf DEV nicht reproduzierbar**, im Code unverändert | Lokaler Listener `http://127.0.0.1:9123/` (nur Protokoll „Header ja/nein“): **Authorization-Header vorhanden: nein**. Grund: AKE-DEV hat im Web keine gespeicherten Zugangsdaten („keine gespeicherten Zugangsdaten“). Der Weg `ServiceSettingsController.cs:181-191` setzt sie bei leerem Kennwort ohne Adressabgleich ein; mit gesetztem `SData:Password` ginge der Header mit – Befund bleibt offen |
| SD-3 Geheim-Key mit anderer Schreibweise | **trifft zu** (lesend belegt) | `ServiceSettings.Key` Kollation `Latin1_General_CI_AS` auf AKE-DEV und IDEAL-DEV → `sdata:password` trifft die Zeile `SData:Password`; nicht ausgeführt |
| SD-4 Prüfung sieht nur Dienst-Datei | **bestätigt** | `-NurPruefen` AKE/IDEAL: „[Pruefung/SDataKennwortAppsettings] … Klartext in D:\Dev\…\Service\appsettings.DEV.json“ – keine Aussage zur Web-Datei |
| UP-1 Neustart nach Abbruch ab Schritt 5 | **im Code bestätigt**, nicht gefahrlos auslösbar | `Update-IdealAkeWms.ps1:836-837` schreibt `deploy-info.txt` beim Kopieren, `:682-683` liest sie als installierte Version; `:268` `Test-Neuer`. `-NurPruefen` zeigt den Versionsfilter: „SQL/110: nicht faellig (eingefuehrt mit v1.87.0, installiert v1.99.22)“. Abbruch in Schritt 5/6 auf DEV hieße Volllauf mit Stopp → nicht gemacht |
| UP-3 Marker mit `SYSDATETIME()` | im Code unverändert (`:336`) | auf DEV ohne Wirkung (SQL-Server = DEV-PC) |
| LZ-1 OSEON-Stammdaten mit `GETDATE()` | im Code unverändert (`OseonSyncService.cs:744,751`), auf DEV ohne sichtbaren Versatz | `ArticleCategories` „Lackierteile“ `ModifiedAt` 2026-11-12 19:34:12.95 ≈ Lauf 19:34:14 (Ortszeit) – auf DEV ist der SQL-Server der lokale PC, Zeitzone gleich; der Befund betrifft nur Server mit anderer Zeitzone |
| LZ-2 Wächter ohne `GETDATE()` | **bestätigt** | `ZeitstempelKonventionTests.cs:25` Muster `UtcNow\|GETUTCDATE\|SYSUTCDATETIME` |
| EI-1 Fallback ohne Meta | nicht messbar (alle Keys haben Anzeigenamen) | `/ServiceSettings` AKE: kein Feld mit technischem Namen statt Anzeigename gesehen |

Die Zeilen „DEV-Nachmessung 2026-11-14“ stehen in [[2026-11-14-review-befunde-sdata]], [[2026-11-14-review-befunde-update-skript]],
[[2026-11-14-review-befunde-lokalzeit]] und [[2026-11-14-review-befunde-einstellungen]].

## Neue Befunde

Nicht behoben (Schranke 1). Nummern N-B4-… (Fortsetzung nach N-1 … N-10, N-B3-1 … N-B3-6). Nachträge: N-B4-1 in
[[2026-11-14-review-befunde-einstellungen]], N-B4-2 und N-B4-3 in [[2026-11-14-review-befunde-tabellen]] (neu), N-B4-4 in
[[2026-11-14-review-befunde-sdata]].

### N-B4-1 – Hardware-Scanner-Teile erscheinen bei ausgeschaltetem Standort-Schalter (niedrig)
- **Wo:** `IdealAkeWms/Views/Shared/_Layout.cshtml:18-19,33` (Kommentar „null-Attribute lässt Razor weg“), Leser
  `IdealAkeWms/Views/Account/Profile.cshtml:206` und `IdealAkeWms/wwwroot/js/barcode-scanner.js:113` (`hasAttribute('data-hw-scan-praefix')`).
- **Befund:** Razor rendert `data-*`-Attribute auch bei `null` (als leeren Wert). AKE-DEV (Schalter nie gesetzt = aus): `<body data-hw-scan-praefix="" …>`.
  Dadurch zeigt „Mein Profil“ die Karte „Dieses Gerät – Hardware-Scanner auf diesem Gerät verwenden / Scanner-Diagnose / Scannen ohne Feld“, und
  das Kamera-Fenster bekommt das Feld „Handscanner“, obwohl `hardware-scanner.js` nicht geladen ist. Ein Anwender, der dort scannt, erwartet eine
  Wirkung, die es nicht gibt. TS-99.1a und 101.16 („`<body>` ohne Attribut“) sind damit nicht erfüllt; die übrigen Leser prüfen den Wert
  (`=== ','`, `=== 'Matchcode'`, `|| '~09'`) und sind unberührt.
- **Vorschlag:** In beiden Lesern `getAttribute('data-hw-scan-praefix')` auf nicht leer prüfen **oder** im Layout die Attribute nur bei aktivem
  Schalter ausgeben (`@if`). Wächter-Test: Layout bei Schalter aus enthält kein `data-hw-scan-praefix=`.

### N-B4-2 – CSV: Zahlen ab 1.000 werden nicht als Zahl erkannt, negative als Text mit Apostroph (mittel)
- **Wo:** `IdealAkeWms/wwwroot/js/csv-export.js:16` (`PLAIN_NUMBER = /^[+-]?(\d{1,3}(\.\d{3})+|\d+)(,\d+)?$/`), `:21-24` (`\s+` → Leerzeichen,
  danach Injection-Schutz); Server-Kultur ohne feste `CultureInfo` (`Program.cs` setzt keine) → DEV formatiert Tausender mit U+00A0.
- **Befund:** AKE-DEV `/StockOverview` Artikel 00000905: Bildschirm `-10 026,000`, CSV `'-10 026,000` (Apostroph = Text). Positive Werte ab
  1.000 stehen als `10 026,000` in der CSV – Excel erkennt das nicht als Zahl (TS-86.5 verlangt `1.234,500` als Zahl). Betrifft jede Spalte mit
  Mengen ab 1.000 (Bestände, Bewegungen, Summiert-Sichten).
- **Vorschlag:** In `PLAIN_NUMBER` auch Leerzeichen/geschütztes Leerzeichen als Tausendertrenner zulassen und beim Export entfernen
  (z. B. `-10026,000`), oder Server-Kultur auf `de-DE`-Zahlenformat festlegen. Prüffrage an Gerald: Welche Kultur haben akenet01/idealweb01
  (Zahl ab 1.000 in der FA-Liste/Bestand: Punkt oder Leerzeichen)? Test im `Kern_NodeSelbstpruefung`: `-10 026,000` → Zahl ohne Apostroph.

### N-B4-3 – Fehlteile: Seite außerhalb des Bereichs zeigt „Eintraege 26–15 von 15“ (Hinweis)
- **Wo:** `/MissingParts` und `/MissingPartsLager` (Pagination-Zähler).
- **Befund:** `/MissingParts?mineOnly=true&page=2` bei 15 Treffern: „Eintraege 26–15 von 15“, leere Tabelle; `/MissingPartsLager?page=99`:
  „Eintraege 2451–21 von 21“. Andere Listen zeigen in dem Fall „Keine … gefunden.“ (auch nicht ideal, aber ohne Widerspruch). Nur über
  handgebaute oder veraltete Links erreichbar (z. B. Lesezeichen nach Abarbeiten).
- **Vorschlag:** Seite auf die letzte vorhandene Seite begrenzen (wie `PaginationState` es vorsieht) oder Zähler „0 von 15“.

### N-B4-4 – IDEAL-DEV zeigt mit `SData:BaseUrl` auf den AKE-Test (Hinweis, nur DEV)
- **Befund:** `IDEAL_WMS_DEV.ServiceSettings`: `SData:BaseUrl = https://sagetest01.ake.at:5493/`, `SData:Dataset = ake_TEST2026;1`. TS-131-Vorbedingung
  „IDEAL-DEV ohne SData:BaseUrl“. Ohne Zugangsdaten passiert nichts; sobald jemand dort Zugangsdaten einträgt, liest IDEAL-DEV im Register und in
  `Vergleich`/`SData`-Läufen AKE-Daten.
- **Vorschlag:** Auf IDEAL-DEV leeren (Gerald), in der DEV-Einrichtung festhalten.

### Beobachtung ohne Fehler
- Update-Skript: zwei Läufe in derselben Minute schreiben in dieselbe Protokolldatei `yyyy-MM-dd_HHmm_<Ziel>_v<Version>.log` (angehängt, nichts
  verloren; „Vorpruefung“ zweimal in einer Datei).
- Einbuchung: Der FA-Hinweis „FA … liegt bereits“ gilt je FA, nicht je gewähltem Artikel (IDEAL: Artikel 102044 gewählt, Hinweis „Artikel
  50002134 … Lagerplatz NAN übernommen“). So im Code gewollt (`/api/stock/by-order`, FA-Platz), für die Anwender-Doku als Hinweis notieren.
- IDEAL-DEV: Lauf „FA-Materialisierung“ läuft alle 15 min, obwohl alle `Sync:*Enabled = false` (rein lokal, kein Sage-Zugriff, `angelegt=0`).
- Bei `QrErstesFeld = Artikelnummer` bleibt nach „Nicht gefunden“ der vorher gewählte Artikel stehen (wie vor v1.68).

## Testdaten-Spur

| Was | Wo | Id / Nummer | Zurückgesetzt? |
|---|---|---|---|
| Nicht gespeicherte Settings-POST (`OseonReportingHorizonDays = 99`, `ErfundenQA`) | AKE `AppSettings` | – | nichts gespeichert (Wert 10, 49 Zeilen geprüft) |
| Spaltenpräferenz-Versuche mit `viewKey = 'Unbekannt'` (Articles, FaHierarchyStructure) | AKE/IDEAL `UserViewPreferences` | – | nichts gespeichert (PUT 400); Seiten neu geladen |
| Verbindungstest an `http://127.0.0.1:9123/` | lokaler Listener | – | Listener nach einer Anfrage beendet; Datei enthält nur Pfad und „Header ja/nein“ |
| Scan-Simulationen auf Einbuchungsmasken | AKE/IDEAL | – | nicht gebucht (Formulare nicht abgeschickt) |
| Update-Skript `-NurPruefen` (AKE-DEV, IDEAL-DEV, Negativfälle) | Protokolle im Scratchpad `…\scratchpad\deploylog\` | `2026-11-13_2144_*.log`, Trockenlauf-Ausgaben | nichts gestoppt, nichts geschrieben (Altbestand nur Trockenlauf) |
| Edge-headless-Profil | Scratchpad | – | gelöscht |

Nicht angefasst: KO-2026-00008, Rollenzuordnung gerald.weichbold, AKE-DEV `Sync:KreditorenEnabled`, `Sync:ArtikelLieferantEnabled`,
`Sync:EinkaufBestellungenEnabled`, `EinkaufAktiv`, SData-Zugangsdaten (weder gelesen noch geändert), alle übrigen Einstellungen, Sage.

## Screenshot-Kandidaten für die Anwender-Doku

Personendaten: Spalten „Benutzer“, Benutzerknopf oben rechts → ausblenden oder mit Testbenutzer aufnehmen.

| URL | Zustand | Zweck |
|---|---|---|
| `http://localhost:8081/StockMovements/Inbound` | Artikel 00393295 gewählt, FA 2627001 eingetippt → blaues „FA … liegt bereits“-Alert + FA-Karte | Einbuchen mit FA |
| `http://localhost:8081/StockMovements/Outbound` | Artikel, Lagerplatz, Menge, FA-Feld | Ausbuchen |
| `http://localhost:8081/StockMovements/Transfer` | Artikel, Quell- und Ziel-Platz mit Scan-Knöpfen | Umbuchen |
| `http://localhost:8081/StockMovements/OutboundAll?storageLocationId=3296` | FA-Feld vorbelegt mit drei FAs | Lagerplatz/Wagen ausbuchen |
| `http://localhost:8081/StockOverview?colf_storage-location=%22GL%3B0%3B0%3B0%22` | Filter in Anführungszeichen, 27 Treffer, Popover „?“ offen | Bestände + Spaltenfilter-Syntax |
| `http://localhost:8081/ProductionOrders?colf_article-number=1...5` | rotes Filterfeld mit Erklärtext | Spaltenfilter: ungültige Eingabe |
| `http://localhost:8081/StockMovements?colf_datetime=13.11.2026` | Datumsfilter, CSV-Knopf neben dem Zahnrad | Bewegungen, CSV-Export |
| `http://localhost:8082/BdeTerminal` | (siehe [[03-ideal-hierarchie-beschichtung-bde]]) | BDE-Terminal |
| `http://localhost:8081/SageTest` | Register mit Reiterleiste, Fenster „Sage-Verbindung testen“ (Kennwort leer) | Register Sage-Test (Admin) – erst wenn Zugangsdaten gesetzt sind, sonst nur Leerzustand |
| `http://localhost:8081/SageTest/Feldliste` | Leerzustand „Sage-SData nicht konfiguriert …“ | Register Sage-Test, Feldliste |
| `http://localhost:8081/Settings` | Gruppe „QR-Code und Scanner“ mit Auswahllisten | Einstellungen (Admin) |
| `http://localhost:8081/SyncLog?colf_Service=lieferverzug` | 18 Einträge, deutsche Namen | Aktivitäts-Protokoll |
| `http://localhost:8081/WarehouseRequisitions` | Karte „Meine Fehlteile“ (Kontrast-Link) | Fehlteile |
