---
type: testbericht
created: 2026-11-14
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
modul: FA-Hierarchie (IDEAL Teil 2–8), FA-Liste, BOM-Bridge, Kommissionierung nur am HauptFA, Kommissionierliste, Arbeitsgänge im FA-Baum, Beschichtungsauftrag, Arbeitsgang-Stammdaten, Stückliste, BDE Stufe 1 / BDE-Terminal, PDF, Vormontage, Lagerplatz-Etiketten
stand: "Bündel v1.99.22 (90bbbbd7), DEV AKE localhost:8081 / AKE_WMS_DEV, DEV IDEAL localhost:8082 / IDEAL_WMS_DEV"
durchgefuehrt: "2026-11-13 21:05–21:40 (Ortszeit DEV-PC)"
---
# Teil B3 – IDEAL-Hierarchie, Beschichtung, BDE, Stückliste auf DEV

Teil B3 aus [[2026-11-14-wochenend-review-test-anwenderdoku]]. Grundlage: `docs/TESTSZENARIEN.md` im Bündel-Worktree (TS-58, 61, 63, 67,
69, 70, 73, 74, 78, 79, 80, 84, 85, 87, 88, 89, 92, 94, 95, 96, 97, 104, 105, 127) und [[2026-11-14-review-befunde-bde]]. Form wie
[[01-kommissionierung]].

**Vorgehen:** Eigener Tab im verbundenen Chrome (der für die Sitzung gewählte Browser; kein anderer gewählt), am Ende geschlossen.
Teils Oberfläche (Select2-Dropdown, Spaltenauswahl, BDE-Terminal mit Tastatur/Maus), überwiegend dieselben GET/POST-Aufrufe, die die
Seiten senden (`fetch` mit Antiforgery-Token im angemeldeten Tab). Wirkung per SQL auf `IDEAL_WMS_DEV`/`AKE_WMS_DEV`. Nicht neu deployt.

**Schutz geprüft:** IDEAL-DEV alle `Sync:*Enabled = false` (kein Sync gestartet), `SageLagerbuchungAktiv`, `SageLagerbuchungUmbuchungAktiv`,
`SageWareneingangBelegAktiv = false` auf beiden DEV-DBs. Master `ProduktionsauftragHierarchisch`: IDEAL `true` (gesperrt, Daten
vorhanden), AKE `false` – Einwegtor **nicht** ausgelöst, nur Zustand gelesen. Keine Mails (kein Mail-Weg in diesem Umfang).

**Einschränkungen:**
- Nur ein Windows-Benutzer (gerald.weichbold) → Rollen-Negativfälle (ohne `picking`/`vorbau`/`beschichtungsauftrag_schreiben`,
  `masterdata_read`, `bde_shiftlead`) nicht prüfbar.
- IDEAL-DEV hat nur **4 HauptFAs in der Struktur** (1059463, 1060414, 1064052, 1065036) → Gruppen-Paging (Mindestseitengröße 25),
  Kombigerät, Prio aus Sage, „gleiche Artnr. mit zwei Zielen / gleicher Matchcode“ ohne Daten.
- Alles, was einen Sync-Lauf braucht (Materialisierung TS-64/71, Lookup TS-65, Arbeitsgang-Erkennung TS-77, Werkbank-Abgleich TS-96.1/96.2,
  Arbeitsgang-Abgleich TS-94.2 ff., Sage-Views TS-93), ist NICHT PRUEFBAR (Syncs auf IDEAL-DEV verboten).
- **Screenshot-Werkzeug** des Browsers fiel ab etwa 21:26 dauerhaft mit Zeitüberschreitung aus. Das FEHLER-Bild wurde mit Edge
  headless (Windows-Anmeldung, gleiches DEV) erstellt. Edge headless mit 412 px Breite zeigte abgeschnittene Seiten – Nachmessung im
  412-px-iframe ergab `scrollWidth = clientWidth` (kein Querscrollen); das war ein Darstellungsartefakt von headless, kein Befund.

## Ergebnis-Tabelle

| TS-Nr. | Standort | Ergebnis | Beleg/Bemerkung |
|---|---|---|---|
| 58.1 Baum mehrstufig, Alle auf | IDEAL | OK | `/FaHierarchy`: 4 Strukturen, 771 Zeilen, Ebenen 0–5; „Alle auf“ zeigt alle Zeilen |
| 58.2 Struktur-Pagination | IDEAL | NICHT PRUEFBAR | 4 Strukturen < kleinste Seitengröße 25 |
| 58.3 Server-Filter, UND-Verknüpfung | IDEAL | OK | `filterText=50002313` → nur Struktur 1060414 (176 Zeilen); `ZZZNIX` → „Keine Strukturen gefunden.“; `filterHauptFa=1060414` + `filterText=50000418` → 0; `filterCustomer=Klumaier` → nur 1059463 |
| 58.4 Hervorheben + Auto-Expand | IDEAL | NICHT PRUEFBAR | nicht durchgeführt |
| 58.5/58.6 Tiefen-Cap konfigurierbar | IDEAL | OK | `FaHierarchyMaxTiefe = 3` (über Standorteinstellungen): Banner „Mindestens eine Struktur konnte wegen eines Tiefen-Cap- oder Zyklenfehlers nicht vollstaendig aufgebaut werden …“, je Struktur „Fehlerhaft — Tiefen-Cap 3 erreicht“; zurück auf 500 |
| 58.7/58.8 Waise, Kombigerät | IDEAL | NICHT PRUEFBAR | keine Daten (je HauptFA genau eine FAInfos-Zeile) |
| 58.10 Regression AKE | AKE | OK | AKE `/FaHierarchy` „Keine Strukturen gefunden.“, FA-Liste flach (0 Gruppenköpfe). Menüpunkt sichtbar → Hinweis N-B3-4 |
| 63.4/63.6 Einwegtor gesperrt, keine Schreibstelle | IDEAL | OK | `/HierarchieUmstellung`: „Hierarchisch aktiviert — gesperrt — hierarchische Daten vorhanden … Nicht mehr über die Anwendung umkehrbar“, kein Formular; `/ServiceSettings` Zeile `ProduktionsauftragHierarchisch`: „Einwegtor — hier nur Anzeige“, nur Link; Standorteinstellungen: Master ohne Eingabefeld |
| 63.10 / 67.3 AKE flach | AKE | OK | „Status: Flach (AKE-Standard)“, nur Formular `Enable` (nicht ausgelöst); Standorteinstellungen zeigt „Flach (AKE-Standard)“ |
| 63.1–63.3, 63.5, 63.7–63.9 Umschalten | IDEAL/AKE | NICHT PRUEFBAR | Umschalten ausdrücklich ausgeschlossen |
| 64, 65, 71, 77 Materialisierung, Lookup, Erkennung | IDEAL | NICHT PRUEFBAR | Service-Läufe; Sync auf IDEAL-DEV verboten |
| 66 Sub-FA-Scan-Auflösung | IDEAL | OK | Terminal 65: Scan `1065150` → EG der Sub-FA gewählt; `1059463,ST` → „Dieser Auftrag hat an dieser Werkbank keinen offenen Arbeitsgang.“; `9999999` → „Arbeitsgang nicht gefunden“ |
| 67.1/67.4 Standorteinstellungen, atomar | IDEAL | OK | 25 Felder gruppiert; POST mit gültigem Briefkopf-Pfad + `FaHierarchyMaxTiefe = abc` → „Nicht gespeichert: Baum-Maximaltiefe erwartet eine ganze Zahl (war: abc).“, beide Werte unverändert (SQL). Gültiges Speichern ändert genau ein Feld (SQL-Diff vor/nach). Erfolgsmeldung doppelt → N-B3-2 |
| 67.6 Admin-only | IDEAL | NICHT PRUEFBAR | nur Admin-Benutzer |
| 69 / 73 FA-Liste hierarchiefähig | IDEAL | OK | `/ProductionOrders`: 4 Gruppenköpfe „HauptFA … n Sub-FAs“, 184 Zeilen, Spalten u. a. Matchcode, Prio, Montage-Abt., Vater-Sub-FA; `colf_matchcode=KTE` → 1 Gruppe; Pagination zählt Gruppen („1–4 von 4“) |
| 70 BOM-Bridge | IDEAL | OK | `/Picking/Bom/284` 171 Positionen, `/Picking/Bom/131` 263 Positionen aus der Struktur; Spalten Ebene, Vater-Sub-FA, Komm.-Ziel |
| 74 PDF-Erzeugung | IDEAL | OK | `Pdf/1059463` (Liste) 347 kB, `PdfSummiert/1059463` 106 kB in 1,7 s, `Beschichtungsauftraege/Pdf/4` 77 kB bzw. 136 kB mit Briefkopf, je `%PDF-`; Dateiname `Beschichtungsauftrag_BA-2026-00004_20261113-2118.pdf` |
| 78 Arbeitsgänge-Modal | IDEAL | OK | `/FaHierarchy/WorkOperations?hauptFa=1060414`: „35 Sub-FAs · alle mit Arbeitsgang“, Kürzel/Werkbank/Sage-Nr.; unbekannter HauptFA 999 → 404; Knopf an allen 4 Köpfen |
| 79.1 Relevanzregel | IDEAL | NICHT PRUEFBAR | reine Regression, automatisiert |
| 79.2 „Alle Ziele“ + Negativfall a/b | IDEAL | OK | FA 1060414: erste Option „Alle Ziele (nur kommissionier-relevant)“, dann H1-03 … x; „Alle Ziele“ → 79 Zeilen (= SQL 79 mit Ziel), Badge „Gefiltert: Alle Ziele …“; H2-02 aktiv + „Alle Ziele“ → nur `!(leer)`; „Alle Ziele“ + K-01 → nur `K-01` |
| 79.3 Druck-Leerzustand / Gegenprobe | IDEAL | OK | ohne sichtbare Zeile: kein Formular abgeschickt, gelbe Box „Keine sichtbaren Positionen — nichts zu drucken.“; mit Treffern: Druck 79 Zeilen, Kopf „Gefilterte Ansicht — Komm.-Ziel=Alle Ziele (nur kommissionier-relevant)“ |
| 79.4 neues Ziel ohne Neu-Setzen | IDEAL | NICHT PRUEFBAR | braucht Sage-Sync |
| 79.5 Profil `!(leer)`, `!(leer),K-01`, `!(LEER)` | IDEAL | OK | Profil: `!(leer),K-01` → gespeichert `!(leer)` + „Alle Ziele schließt Einzelziele aus — gespeichert wurde nur 'Alle Ziele'.“, Stückliste mit „Alle Ziele“ vorgewählt (79 Zeilen); `!(LEER)` → `!(leer)` ohne Hinweis. Benutzerverwaltung Create/Edit nicht geprüft |
| 79.6 Persistenz F5 a/b/c, Reset | IDEAL | OK | „Alle Ziele“ F5 → bleibt, 79 Zeilen; `K-01` F5 → bleibt; Profil `!(leer)` + Sitzung `K-02` F5 → `K-02` bleibt; „Alle Filter zurücksetzen“ + F5 → kein Filter |
| 79.7 leeres Dropdown | IDEAL | OK | Profil `!(leer)`, FA 1035235 (keine Strukturzeilen): Dropdown `disabled`, „Alle Ziele“ vorgewählt, Box „Standardfilter „Alle Ziele …“ aktiv — keine Treffer“ |
| 79.8 Altwert case-insensitiv | IDEAL | OK | `sessionStorage` `tableFilters:Bom = {"kommissionieren":"!(LEER),K-01"}` → genau ein Chip „Alle Ziele“, 79 Zeilen |
| 79.9 AKE ohne Komm.-Ziel | AKE | OK | `/Picking/Bom/144`: keine Spalte/kein Dropdown, Zahnrad vorhanden, Leerzustand-Markup vorhanden |
| 79.10 Kaskade entfernt, Fertigmelde-Kaskade da | IDEAL | OK | B1: `CascadeReleasePreview` 404; hier `CascadeDonePreview` 200, „Alle Sub-FAs fertigmelden“ im Gruppenkopf |
| 79.11 Bedienelemente nur am HauptFA | IDEAL | OK | Leitstand (184 Zeilen) und FA-Liste: Freigabe-Formular/Prio/Checkbox/Bom-Link nur für Ids 131, 199, 238, 284 (HauptFAs) |
| 79.12/79.13/79.14 | IDEAL/AKE | OK | in B1 geprüft ([[01-kommissionierung]]) |
| 79.15 Warteschlange strukturell | IDEAL | NICHT PRUEFBAR | automatisiert (InMemory) |
| 79.16 Warteschlange bereinigt | IDEAL | OK | Zählung = 2 (Altlast, N-B3-6); beide Sub-FAs erscheinen nicht in `/Picking`. Bereinigungs-SQL nicht ausgeführt (nur Testsystem) |
| 79.17 eine Zeile je HauptFA | IDEAL | OK | `/Picking`: KO-2026-00003/-00002/-00004 je HauptFA |
| 79.18 FaWorklist/Bom Sub-FA | IDEAL | NICHT PRUEFBAR | `FaCompletionAktiv = false` auf IDEAL-DEV → Abarbeitungsliste gesperrt |
| 79.19 Hilfe | IDEAL | OK | `/Help` enthält „nur am HauptFA“, „Alle Ziele“, `!(leer)` |
| 79.20 (F-A) kein Kasten ohne Standardfilter | IDEAL | OK | `#bomKzEmptyState` und `#bomDefaultFilterBadges` `hidden`, `display:none` |
| 79.20 (v1.47.3) Zahnrad | IDEAL | OK | im Leerzustand Toolbar-Host über der Tabelle (oben 462 px), nicht in der gelben Box (406 px), sichtbar |
| 79.21 Apostroph | IDEAL | OK | Profil `K'02`: Seite lädt, Badge „Komm.-Ziel = K'02“, keine Konsolenfehler |
| 79.22 FA-Suche nur HauptFAs | IDEAL | OK | `/api/productionorders/search?term=1060414` und `q=106` → nur HauptFA-Ids (24–27, 131, 199, 238, 284), keine Sub-FA 1067xxx |
| 80.1 Einheit-Prüfschritt Sage-DB | IDEAL | NICHT PRUEFBAR | Sage-DB IDEAL nicht im Umfang |
| 80.2 Summierung Grundfall | IDEAL | OK | 1059463 / 50000418 / KOM: Liste 4 Zeilen à 2,000, Summiert **eine** Zeile 8,000 (SQL: 4 Zeilen, Summe 8) |
| 80.3 / 80.4 Ziel bzw. Artnr. als Schlüssel | IDEAL | NICHT PRUEFBAR | keine Daten (SQL: keine Artnr. mit zwei Zielen, kein Matchcode mit zwei Artnr.) |
| 80.5 Standard-Einstieg | IDEAL | OK | in B1 |
| 80.6 Umschalter-Filter | IDEAL | OK | Liste → Summiert: `target`, `colf_artnr`, `colf_hauptlagerplatz` mitgenommen, `colf_arbeitsbereich`/`colf_sollmenge` nicht; Summiert → Liste: `target`, `kwVon`, `kwBis` |
| 80.7 Spalte aus, sofort drucken | IDEAL | OK | Zahnrad: Hauptlagerplatz aus, sofort „Drucken“ → `visibleColumns=hauptfa,artnr,matchcode,kommissionieren,sollmenge`, Druck ohne Hauptlagerplatz |
| 80.8 ohne Parameter/ohne Präferenz, `xyz` | IDEAL | OK | `PrintSummiert` alle Spalten, Kopf „Summierte Ansicht“; `visibleColumns=xyz` → keine Spaltenköpfe (dokumentierte Grenze) |
| 80.9 / 80.15 gespeicherte Präferenz | IDEAL | OK | `GET /api/user-view-preferences/FaHierarchyKommissionierSummiert` 200 mit 6 Spalten, `hauptlagerplatz visible:false`; `PrintSummiert` ohne Parameter ohne Hauptlagerplatz; danach „Auf Standard zurücksetzen“ → 204 |
| 80.10 Filter im Druck der Liste | IDEAL | OK | `Print?target=KOM&colf_artnr=50000418` → nur die 4 Zeilen, „Gefilterte Ansicht — dieses Dokument zeigt nur die gefilterten …“ |
| 80.11 Banner-Kombination | IDEAL | OK | ohne Filter nur „Summierte Ansicht“; mit `target=KOM` beide (3 Gruppen, 72 Zeilen) |
| 80.12 KW optional | IDEAL | OK | ohne KW alle 4 HauptFAs, kein KW-Hinweis; `kwVon=kwBis=2026-W27` → 1059463, 1060414 |
| 80.13 Gruppen-Paging | IDEAL | NICHT PRUEFBAR | 4 Gruppen < 25 |
| 80.14 PDF-Fehler-Rückweg | IDEAL | NICHT PRUEFBAR | Edge müsste ausfallen; `PdfSummiert/100` (unbekannt) → 404 |
| 80.16 Regression Vormontage/Beschichtung | IDEAL | OK | beide Sichten, Druck, PDF 200 (siehe 84, 105) |
| 80.17 Skript im Filterwert | IDEAL | OK | `LAGER");</script><script>alert(1)</script>` als `target` und `colf_artnr` in Liste, Summiert, Print, PrintSummiert: kein roher `<script>alert(1)` im HTML, nur URL-kodiert in Links |
| 80.18 Drift-Guard | – | NICHT PRUEFBAR | Unit-Test, nicht Teil der DEV-Regression |
| 80.19 WarehousePicking | IDEAL | OK | `/WarehousePicking` 200 (Print ohne Parameter 404 wie bisher) |
| 80.20 Gate AKE | AKE | OK | `Summiert`, `PrintSummiert`, `PdfSummiert/100` → Startseite mit „Das IDEAL-Kommissionierlisten-Modul ist deaktiviert.“; kein Menüpunkt |
| 84.1/84.2 Anlegen, Teilvergabe | IDEAL | NICHT PRUEFBAR | keine freien beschichteten Positionen (alle 16 von 1060414 in BA-2026-00004) |
| 84.3 Doppelvergabe | IDEAL | OK | `POST /Beschichtungsauftraege/Create hauptFa=1060414` → „Alle 16 beschichteten Positionen von HauptFA 1060414 sind bereits vergeben — kein neuer Auftrag: …“, kein Auftrag |
| 84.4 Entwurf bearbeiten | IDEAL | OK | BA-2026-00005: Menge `abc` → „Eingabe nicht gespeichert: Menge „abc“ ist keine Zahl.“, nichts geändert; `2,5`/`3.25`, Start leer, Dienstleister → „Entwurf gespeichert.“, Werte 2,5 / 3,25, Start leer |
| 84.5 Positionen hinzufügen | IDEAL | OK | ohne Haken „Positionen hinzufügen: keine Positionen ausgewählt.“; mit Haken „1 Position(en) hinzugefügt.“, 3 Positionen, 9 frei |
| 84.6 Dienstleister-Pflicht | IDEAL | OK | Dienstleister leer gespeichert (gelber Hinweis), „Übergeben“ → „Dienstleister fehlt — vor dem Uebergeben am Auftrag eintragen.“, Status Entwurf |
| 84.7 Druck im Entwurf | IDEAL | OK | `Print/5`, `Pdf/5` → Detail mit „Im Entwurf nicht druckbar — erst übergeben.“ |
| 84.8 / 82 / 92 Druck, Briefkopf, Fußzeile | IDEAL | OK | BA-2026-00004: Empfänger GL, „Beschichtungsauftrag BA-2026-00004 — HauptFA 1060414“, Barcode-Bild (alt = Nummer), RAL/Start/Retour, 16 Zeilen je Artnr. mit Positionen; Briefkopf (Logo, Fußbereich `briefkopf-fussbereich`, „Seite“) **nur mit temporär umgestelltem Pfad**; Standard-DEV-Pfad → Ersatz-Kopf mit Hinweis (N-B3-3) |
| 84.9 Reklamation | IDEAL | OK | aus BA-2026-00001 (Erledigt): ohne Haken „Reklamation: keine Positionen ausgewählt.“; 2 Teile → „Reklamation BA-2026-00005 mit 2 Position(en) angelegt.“, Dienstleister GL/RAL/Termine übernommen; aus Entwurf → „Reklamation nur aus „Erledigt“ — BA-2026-00005 steht auf Entwurf.“ |
| 84.10 Positionen des Ursprungs | IDEAL | OK | Auswahl zeigt genau die 10 übrigen der 12 Ursprungspositionen |
| 84.11 Ketten-Reklamation | IDEAL | NICHT PRUEFBAR | „Übergeben“ ist unumkehrbar → keine Testaufträge über Entwurf hinaus |
| 84.12 Storno | IDEAL | OK | Knopf mit Rückfrage (entschärft) → Status Storniert, Verlauf „Storniert 13.11.2026 21:17 · Gerald Weichbold“, kein Druck |
| 84.13 Parallelität (veraltete Version) | IDEAL | OK | Bemerkung und Status mit alter RowVersion → „BA-2026-00005 wurde inzwischen von jemand anderem geändert — bitte neu laden …“ |
| 84.14 Rechte | IDEAL | NICHT PRUEFBAR | nur Admin; AKE Schalter aus → Startseite „Das IDEAL-Beschichtungsauftrag-Modul ist deaktiviert.“ (OK) |
| 84.15 Liste | IDEAL | OK | `status=Storniert` → BA-2026-00005; `colf_status=übergeben` → 00004, 00003 |
| 84.16 / 60 / 81 Vorschlagsliste | IDEAL | OK | Liste 17 Zeilen, 16 mit „in BA-…“, `colf_beschichtet=Ja` 17; Summiert-Gruppenkopf „Aufträge: BA-2026-00004“, kein Anlege-Knopf; PrintSummiert „summiert“ |
| 85 / 88 AG-Symbole + Status | IDEAL | OK | 1 244 Symbole mit `aria-label` „Kürzel – Name“; Status nur an BDE-Werkbank EG: `fa-ag-erledigt` „EG – Entgraten Fläche – erledigt“, `fa-ag-ueberfaellig` „… – überfällig (Soll 16.07.2026)“ (Text + Farbe); nach den Test-Fertigmeldungen 44 → 56 erledigt |
| 87.2/87.3/87.8/87.10 Arbeitsgang-Stammdaten | IDEAL | OK | Reiter Operatoren/Aktivitäten/Terminals/Sage-Zuordnung/Arbeitsgänge; leer „Kürzel darf nicht leer sein.“; ` eg ` „… existiert bereits (Groß-/Kleinschreibung und Leerzeichen zählen nicht).“; ohne Offset (Anlegen und Ändern) „Offset (Arbeitstage) fehlt – nichts gespeichert.“; `EM` angelegt (Quelle Manual, `CreatedBy` Gerald Weichbold), Offset −1 gespeichert, `colf_name=EM` 1 Zeile, ungültige Id 404, gelöscht |
| 87.1 BDE aus | IDEAL | NICHT PRUEFBAR | `BdeAktiv` nicht umgeschaltet |
| 89 Menü Beschichtung, Hilfe | IDEAL | OK | Dropdown „Beschichtung“: Vorschlagsliste → `/FaHierarchyBeschichtung/Summiert`, Aufträge → `/Beschichtungsauftraege`; Hilfe nennt Beschichtungsauftrag, Reklamation |
| 90 Terminal-Status | IDEAL | OK | Terminal-Liste zeigt je Arbeitsgang „EG Entgraten Fläche“, pausierte Aufträge mit „Fortsetzen“ |
| 93, 94.2 ff. Sage-Views, Arbeitsgang-Abgleich | IDEAL | NICHT PRUEFBAR | Sync-Läufe |
| 94.13 Quelle | IDEAL/AKE | OK | IDEAL „Sage“/„Manuell“, AKE „Fremdsystem“ (12 Zeilen) |
| 95.1 Matchcode | IDEAL | OK | FA 1059463: Spalte „Matchcode“ nach „Bezeichnung 2“, Stichproben 50002134 → G-KT-FRR-510-2010--1760B-250-V01 usw., keine leere Zelle |
| 95.9 / 95.10 Limit + Popover | IDEAL | OK | 10 Zeilen mit „+n weitere“; 73000873: KELLER (1 966) – Hauptlagerplatz zuerst, dann H4-01, H4-02, „+2 weitere“, `aria-label` „Alle 5 Lagerplätze anzeigen“; Popover „Lagerplätze (5) … – Hauptlagerplatz … V-01“ |
| 95.13 verdeckter Platz filtern | IDEAL | OK | Lagerplatz-Filter `H4-04` (Platz 4) → Zeile 73000873 sichtbar; `ZZZ-NIX` → 0 |
| 95.14 / 95.17 viele Positionen, kein Querscrollen | IDEAL | OK | 263 Positionen, alles aufgeklappt, `scrollWidth 1521 = clientWidth 1521` |
| 95.2 AKE | AKE | OK | `/Picking/Bom/144` Spalte Matchcode vorhanden |
| 95.3, 95.6–95.8, 95.11, 95.12, 95.15, 95.16, 95.18–95.20 | IDEAL | NICHT PRUEFBAR | nicht einzeln durchgeführt (Touch/Tastatur/Kontrast/Druck) |
| 96.3 Terminal 65 findet EG | IDEAL | OK | Gruppe FA 1059463 aufgeklappt: Sub-FAs 1065143 … 1065150 mit „EG Entgraten Fläche“ |
| 96.7 nur AG-Fertigmeldung | IDEAL | OK | Werkbank 65 Option an: nur Knopf „Fertig melden“; Dialog „Sollmenge: 1“, Gut 1, Ausschuss 0; Buchung Start = Ende, Werkbank 65, Terminal 2. Negativ: Menge 0 → „Bitte eine Gut- oder Ausschussmenge angeben.“; `StartProduction` → „An dieser Werkbank wird nur fertig gemeldet.“; schon gemeldet → „Arbeitsgang ist bereits fertig gemeldet.“ |
| 96.7 / BDE-1 parallele Fertigmeldung | IDEAL | **FEHLER** | zwei gleichzeitige `ReportCompletion` für denselben AG: 4 von 6 Läufen zwei `IsFinal`-Buchungen (WO 526, 529, 532, 538). Siehe Nachmessung. ![[BDE-1-doppelte-ag-fertigmeldung.png]] |
| 96.8 Werkbank-Stammdaten, manipulierter POST | IDEAL | OK | Kürzel `XX` und leere Sage-Nr. per POST → EG/140 bleiben, `ModifiedBy` gesetzt. Zusatz: **Name** wird übernommen → N-B3-1 |
| 96.10 Familie, manipulierte POSTs | IDEAL | OK | `ReportCompletion` mit KA-AG (541) an 65 → „Dieser Arbeitsgang gehört nicht zu dieser Werkbank.“; unbekannte Id → abgewiesen, kein 500 |
| 96.1, 96.2, 96.4, 96.5, 96.9 | IDEAL/AKE | NICHT PRUEFBAR | Sync-Läufe bzw. zweiter Terminal-Benutzer nötig |
| 97.1 Gruppierung und Grenze | IDEAL | OK | 5 Köpfe, alle zu (▸), „Weitere 2 anzeigen (noch 2)“; `BdeTerminalMaxHauptFa = 5`. Werte 3/0/abc nicht geprüft |
| 97.2 Sortierung Prio/Termin | IDEAL | OK | ohne Prio nach Termin (14.07., 16.07., 20.07., 21.07., ohne Termin); Prio in `FaHierarchyOrderInfos` kurz gesetzt (1064052 = 2, 1065036 = 9) → diese beiden oben mit Badge „Prio 2“/„Prio 9“; zurück auf NULL |
| 97.3 Aufklappen | IDEAL | OK | `aria-expanded=true`, ▾, Sub-FA-Blöcke mit EG |
| 97.4 Markierung, Knöpfe | IDEAL | OK | `aria-pressed=true` am gewählten AG, „Gewählt: 1059463 / 1065150 · EG Entgraten Fläche“, „Rüsten starten“/„Produktion starten“ |
| 97.5 Scan ohne AG | IDEAL | OK | b) `1065150` gewählt; f) `9999999` „Arbeitsgang nicht gefunden“; g) `1059463,ST` „… keinen offenen Arbeitsgang.“ |
| 97.6 negative Mengen | IDEAL | OK | `ReportCompletion` Gut −5 → „Mengen dürfen nicht negativ sein.“ (übrige Wege nicht geprüft) |
| 97.8 Hilfe | IDEAL | OK | Hilfe nennt `BdeTerminalMaxHauptFa`, Scan ohne AG, „nur AG-Fertigmeldung“ |
| 97.9 AKE-Regression | AKE | OK | AKE-Terminal (Stanzarei, Operator 4444): keine Gruppenköpfe, kein „Weitere“; Liste auf DEV leer („Keine offenen Produktionsaufträge an dieser Werkbank.“) |
| 97.10 NurFA | IDEAL | NICHT PRUEFBAR | `BdeNurFaMeldung` nicht umgeschaltet |
| 104.1/104.9 Etikett A4 bei Schalter aus | AKE | OK | `/StorageLocations/PrintLabels`: nur `@page { size: A4 portrait }`, kein Rollen-Hinweis |
| 104.2/104.3/104.4 Rollenformat | IDEAL | OK (Markup) | Schalter an: zusätzlich `@page { size: 93mm 55mm; margin: 0 }`, Leiste „Rollenformat 93 × 55 mm: im Druckdialog den Rollendrucker wählen …“; Sammel- (230 Bilder) und Einzeldruck (`PrintLabel/146`). Druckbild/Scan (104.5) NICHT PRUEFBAR (kein Rollendrucker/Scanner) |
| 105.1/105.5 Vormontage neue Spalten | IDEAL | OK | Einzeln: hauptfa, arbeitsbereich, montage-abteilung, hauptartnr, artnr, matchcode, sollmenge, fertigungmenge, vmbedarf, material, kunde, fe-termin, bemerkungpn, fertigungsinfopn; Summiert: matchcode, arbeitsbereich, montage-abteilung, sollmenge, fertigungmenge, hauptfa, kunde, fe-termin, bemerkungpn, fertigungsinfopn |
| 127.1/127.2/127.5 Abteilung | IDEAL | OK | Spalte „Abteilung“ rechts von Arbeitsbereich; `colf_montage-abteilung=TK` → nur Gruppe 1064052 (5 Zeilen), `!TK` → 18 Zeilen ohne 1064052; Summiert mit TK-Filter |
| 127.3/127.4 Kombigerät, ohne Abteilung | IDEAL | NICHT PRUEFBAR | keine Daten |
| 105.10/127.8 AKE Toggle aus | AKE | OK | `/FaHierarchyVormontage` → Startseite „Das IDEAL-Vormontage-Modul ist deaktiviert.“ |
| Terminal-Sicht (412 px) | IDEAL | OK | 412-px-iframe: `/Picking`, `/BdeTerminal`, `/Beschichtungsauftraege`, `/FaHierarchy` ohne Seiten-Querscrollen (`scrollWidth = clientWidth`) |

**Zählung:** OK 82 (davon 1 nur Markup) · FEHLER 1 · NICHT PRUEFBAR 24 (Zeilen der Tabelle).

## Nachmessung Review-Befunde

| Befund | Ergebnis | Beleg |
|---|---|---|
| BDE-1 Doppeltipp OK → doppelte Fertigmeldung | **reproduziert (serverseitig)**, am Desktop-Doppelklick **nicht** | Echter Doppelklick auf „Bestätigen“: eine Buchung (Id 7), Modal schließt vorher. Zwei gleichzeitige POSTs: 4 von 6 Läufen zwei `Finished`+`IsFinal`-Buchungen (Ids 9/10, 11/12, 13/14, 16/17), Gutmenge doppelt. Handgerät-Doppeltipp nicht gemessen (kein Touch-Gerät). Bild oben |
| BDE-2 unmaskierte Texte in `innerHTML` | **bestätigt (statisch)** | ausgelieferte `bde-terminal.js` auf IDEAL-DEV, Zeilen 498, 532, 534, 547, 934, 973 ohne `escapeHtml`; kein Live-Versuch (hätte Stammdaten geändert) |

Die Zeilen „DEV-Nachmessung 2026-11-14“ stehen in [[2026-11-14-review-befunde-bde]].

## Neue Befunde

Nicht behoben (Schranke 1). Ausführlich mit Fundstelle und Vorschlag in [[2026-11-14-review-befunde-ideal-hierarchie]] (N-B3-1 … N-B3-4,
N-B3-6) und im Nachtrag von [[2026-11-14-review-befunde-bde]] (N-B3-5).

| Nr. | Schwere | Kurz | Vorschlag |
|---|---|---|---|
| N-B3-1 | niedrig | Name einer Sage-Werkbank per manipuliertem POST überschreibbar (Kürzel/Sage-Nr. geschützt) | Name serverseitig wie Kürzel behandeln |
| N-B3-2 | Hinweis | „Standorteinstellungen gespeichert.“ doppelt (View + Layout) | Block in `StandortEinstellungen/Index.cshtml:13-15` entfernen |
| N-B3-3 | Hinweis | IDEAL-DEV `BriefkopfVorlagePfad` zeigt auf Testsystem-Pfad; Schriftdateien fehlen in `Branding`; Anschrift Einstellung (38, 4840) ≠ Vorlage (28, 4810) | Prüffrage an Gerald, DEV-Branding-Ordner |
| N-B3-4 | Hinweis | Menüpunkt „FA-Struktur“ bei AKE sichtbar, Seite leer | an Master koppeln oder dokumentieren |
| N-B3-5 | Hinweis | BDE-Buchungen „Ziel“ ohne Sub-FA (`1059463/EG`) → Doppel nicht erkennbar | `HauptFA / Sub-FA / AG` bei Master an |
| N-B3-6 | Hinweis | IDEAL-DEV: 2 Sub-FAs `IsReleasedForPicking = 1` (Altlast, unsichtbar) | TS-79.16-Bereinigung auf DEV |

### Beobachtung ohne Fehler
- Edge headless mit `--window-size=412,…` stellt die Seiten abgeschnitten dar; im 412-px-iframe kein Querscrollen → Darstellungsartefakt.
- Ungültiges KW-Format per URL (`kwVon=27/2026`) wird ohne Hinweis ignoriert; die Oberfläche nutzt `type=week`, daher nur bei handgebauten URLs.
- `/FaWorklist` auf IDEAL-DEV → AccessDenied, weil `FaCompletionAktiv = false` (gewollt, `FaWorklistController.cs:88-93`).

## Testdaten-Spur

| Was | Wo | Id / Nummer | Zurückgesetzt? |
|---|---|---|---|
| BDE-Terminal für gerald.weichbold (Werkbank 65, Beschreibung „QA Wochenend-Regression B3“) | IDEAL `BdeTerminals` | 2 | **nein** – stornierte Buchungen 7–17 verweisen darauf (FK). Bei Bedarf Buchungen + Terminal per SQL löschen |
| Fertigmeldungen (Test BDE-1/96.7) | IDEAL `BdeBookings`/`BdeBookingQuantities` | 7–17 | storniert (`IsCancelled = 1`, Grund „QA Wochenend-Regression B3 Testbuchung“); `WorkOperations.IsReported` unverändert 0 |
| Werkbank 65 „Nur AG-Fertigmeldung“ an/aus, Name „Manipuliert“ | IDEAL `ProductionWorkplaces` 65 | – | ja, Option 0, Name „Entgraten Fläche“; `ModifiedAt/By` zeigen den Test |
| `FaHierarchyOrderInfos.Prio` 1064052 = 2, 1065036 = 9 | IDEAL | – | ja, NULL (Cache, Sync aus) |
| Profil `DefaultFilterBomKommissionierziel` (gerald.weichbold) | IDEAL `Users` 8 | – | ja, leer (vorher NULL; `ModifiedAt/By` gesetzt) |
| Spalten-Präferenz `FaHierarchyKommissionierSummiert` | IDEAL `UserViewPreferences` | – | ja, „Auf Standard zurücksetzen“ (GET → 204) |
| Standorteinstellungen `BriefkopfVorlagePfad` (kurz Worktree-Pfad), `FaHierarchyMaxTiefe` (kurz 3) | IDEAL `AppSettings` | – | ja, SQL-Diff aller 25 Masken-Werte vor/nach identisch |
| Beschichtungsauftrag (Reklamation zu BA-2026-00001) | IDEAL `Beschichtungsauftraege` | BA-2026-00005 (Id 5) | storniert; Nummer verbraucht |
| Arbeitsgang `EM` | IDEAL `OperationConfigs` | 37 | ja, gelöscht |
| sessionStorage `tableFilters:Bom` | Browser-Tab | – | geleert, Tab geschlossen |

Nicht angefasst: KO-2026-00008, Rollenzuordnung gerald.weichbold, AKE-Einstellungen `Sync:KreditorenEnabled`, `Sync:ArtikelLieferantEnabled`,
`Sync:EinkaufBestellungenEnabled`, `EinkaufAktiv`, Master `ProduktionsauftragHierarchisch` (beide DEV), BA-2026-00001 … -00004, Sage.

## Screenshot-Kandidaten für die Anwender-Doku

Personendaten: Spalten „Gedruckt von“, „Operator“, Statusverlauf „· Gerald Weichbold“ zeigen echte Namen → mit Test-Operator „Hans Guck“
bzw. ausgeblendet aufnehmen.

| URL | Zustand | Zweck |
|---|---|---|
| `http://localhost:8082/FaHierarchy` | Struktur 1059463 aufgeklappt, Legende offen | FA-Struktur, Symbole, Status erledigt/überfällig |
| `http://localhost:8082/FaHierarchy` → „Arbeitsgänge“ am Kopf 1060414 | Modal „35 Sub-FAs · alle mit Arbeitsgang“ | erkannte Arbeitsgänge |
| `http://localhost:8082/Picking/Bom/284` | Komm.-Ziel „Alle Ziele“, Badge sichtbar | Stückliste IDEAL, Ziel-Filter |
| `http://localhost:8082/Picking/Bom/131` | Popover „+2 weitere“ an 73000873 | Lagerplatz-Limit |
| `http://localhost:8082/FaHierarchyKommissionierListen/Summiert` | Standard-Einstieg, Zahnrad offen | Kommissionierliste summiert, Spaltenauswahl |
| `http://localhost:8082/FaHierarchyBeschichtung/Summiert` | Gruppenkopf mit „Aufträge: BA-2026-00004“ | Vorschlagsliste Beschichtung |
| `http://localhost:8082/Beschichtungsauftraege/Details/2` | Entwurf mit Statusleiste, „Dienstleister fehlt“ | Beschichtungsauftrag bearbeiten |
| `http://localhost:8082/Beschichtungsauftraege/Print/4` | erst nach Klärung N-B3-3 (Briefkopf) | Druck Beschichtungsauftrag |
| `http://localhost:8082/BdeTerminal` | Operator 123, Gruppe 1059463 offen, EG gewählt | BDE-Terminal: Gruppen, Auswahl, Knöpfe |
| `http://localhost:8082/OperationConfigs` | Liste mit Quelle, „Nicht konfigurierte Kürzel“ | Arbeitsgang-Stammdaten |
| `http://localhost:8082/FaHierarchyVormontage` | Reiter ISO, Spalte Abteilung | Vormontage-Listen |
| `http://localhost:8082/StandortEinstellungen` | Gruppen Firmendaten/Druck | Standorteinstellungen (Admin) |
| `http://localhost:8082/StorageLocations/PrintLabel/146` | Rollenformat-Hinweis | Lagerplatz-Etikett |
