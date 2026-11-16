---
type: spec
title: FA-Struktur IDEAL über SData (Knotenfelder, beendete Haupt-FAs, orderBy, Stückliste live)
slug: 2026-11-16-fa-struktur-ideal-ueber-sdata-spec
status: Testbereit
created: 2026-11-16
updated: 2026-11-16
source_backlog: "[[2026-11-16-fa-struktur-ideal-ueber-sdata]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: IDEAL
affected_code:
  - IdealAkeWms/Services/SageSdata/SageSdataReadClient.cs (GetAsync orderBy-Pflicht bei where)
  - IdealAkeWms/Controllers/SageSdataFaController.cs (:89, :136 orderBy)
  - IdealAkeWms/Controllers/SageTestController.cs (LadeFaAsync :180-205, LadeArtikelAsync :161-178)
  - IdealAkeWms/Services/SageSdata/SageTestAufbereitung.cs (FaBaum :212-232)
  - IdealAkeWms/Views/SageTest/Stueckliste.cshtml (Kennzeichnung Artikel-Modus)
  - IDEALAKEWMSService/Services/Sdata/SdataFaStrukturReader.cs (Wurzel beendeter Haupt-FAs, Knotenfelder, Projizieren)
  - IDEALAKEWMSService/Services/Sdata/FaStrukturQuelleSwitch.cs (VergleichsFelder, Hybrid-Lesung im SData-Modus)
  - IDEALAKEWMSService/Services/SageFaStrukturReader.cs (FAInfos einzeln lesbar, Bool-Mapping wiederverwenden)
  - IdealAkeWms/Services/SageSdata/SageQuelleSperre.cs (P-F2 fuer die Struktur aufheben)
  - IdealAkeWms/Models/ServiceSettingDefinitions.cs (:108, :267 Beschreibung Sync:Quelle:FaStruktur)
  - scripts/deploy/nachlauf.psd1
open_questions:
  - "1. RealTimeData dtsFaBelegsuche im Dienst: darf der Dienst (Betrieb) RealTimeData lesen, obwohl die DPS-Freigabe fehlt - oder erst P-FS3 (include=SpecificUserfields an PpsFaBelegeInfo) abwarten?"
  - "2. Arbeitshypothese bis P-FS1 vorliegt: E4 mit der Feldquelle aus L11 bauen (Unter-FA-Felder vom FA-Kopf, Blattfelder vom Artikel) und den Vergleich entscheiden lassen?"
  - "3. Offene Felder Arbeitsbereich, Beschaffungsartikel, HauptArtnr: Kandidaten USER_OSAbteilung, Artikel.IstBestellartikel, Artikelnummer der Wurzel nur vergleichen und erst nach P-FS1/P-FS5 schreiben?"
  - "4. Blattfelder ohne gefundene SData-Quelle (Beschichtet, Breite, Hoehe, Tiefe, EKBedarf am Artikel nicht in L10): Umschalten auf SData sperren, bis jedes Feld eine Quelle hat, oder leer akzeptieren?"
  - "5. Schwelle fuer das Umschalten IDEAL von Vergleich auf SData: wie viele abweichungsfreie Laeufe, wer schaltet?"
  - "6. Reihenfolge zur Beschichtungslogik-Spec (v1.99.25): diese Spec danach bauen und Beschichtet im SData-Weg mitliefern?"
  - "7. Laufzeitgrenze: was ist fuer den Lauf FA-Hierarchie mit SData akzeptabel (P-FS6 misst Volumen und Dauer)?"
  - "8. Sync:ArtikelBenutzerfelder an IDEAL wirkt erst nach Aufhebung von P-R1 (Writer nur im SData-Modus) - P-R1 als eigene Folge-Spec?"
  - "9. Stueckliste live, L12: welche Ansicht meinte Gerald mit 'Verkaufsstueckliste' - reicht die Kennzeichnung im Artikel-Modus?"
  - "10. E1/E2 aendern Web-Diagnosen, die auch an AKE laufen: QA zusaetzlich mit kurzer AKE-Regression (TS-146/147 Negativfall)?"
epic: true
etappen:
  - "E1 orderBy-Pflicht fuer RealTimeData (Web)"
  - "E2 Stueckliste live: beendete Haupt-FAs + Kennzeichnung Artikel-Modus (Web)"
  - "E3 SData-Reader: Wurzel und Positionen beendeter Haupt-FAs (haengt an Rueckfrage 1)"
  - "E4 SData-Reader: Knotenfelder + erweiterter Vergleich (Arbeitshypothese, Rueckfragen 2-4)"
  - "E5 Hybrid-Lesung im SData-Modus + Sperre P-F2 nur fuer die Struktur aufheben (Umschalten haengt an Rueckfrage 5)"
  - "E6 Doku, Manifest, ADR 0029, Version 1.99.26, Brain"
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: "Empfehlungen 1–10 (1: nur Vergleich bis DPS-Antwort)"
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-16"
---

# FA-Struktur IDEAL über SData

Bezug: [[2026-11-16-fa-struktur-ideal-ueber-sdata]], [[sage-sdata-benutzerfelder-lesetest]] (L10–L12), [[2026-10-30-fa-rueckmeldung-an-sage]]
(IDEAL-Befund 16.11.), Vorgänger [[2026-11-06-sage-sdata-stammdaten-sync-spec]] (G11, P-F1, P-F2, P-R1),
[[2026-11-13-sdata-teil-2-userfelder-stueckliste-spec]] (Stückliste live), parallel [[2026-11-16-beschichtungslogik-je-standort-spec]].
Codestand: Bündel-Worktree v1.99.23. Alle Zeilenangaben beziehen sich auf den Worktree. Diese Spec betrifft nur **IDEAL**
(`ProduktionsauftragHierarchisch`). Ausnahme: E1 und E2 ändern Admin-Diagnosen, die an beiden Standorten laufen (Rückfrage 10).

## Ziel / Nutzen (das Warum)

Die FA-Struktur an IDEAL (`FaHierarchyNode`) kommt heute vollständig aus der Sage-View FAListe. Deren CREATE-Text liegt nicht im Repo
(`SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql:57` TODO). Der SData-Weg liefert bisher nur die Kanten
(`SdataFaStrukturReader.cs:190-239`) und ist gesperrt (`SageQuelleSperre.cs:17-23`, P-F2). Grundlage dafür war die Annahme G11
(Stammdaten-Spec, Nachtrag :164-165): Die Fachfelder sind über SData nicht erreichbar.

Der Lesetest vom 16.11. an IDEAL-Prod widerlegt diese Annahme für einen Teil der Felder. Unter-FAs tragen `USER_OS*`-Felder am FA-Kopf
(`dtsFaBelegsuche`, L11 :1092-1095), Blätter tragen sie am Artikel (L11 :1096-1097). Ziel ist, die Knotenfelder aus SData zu lesen, im
Modus „Vergleich“ gegen die View zu belegen und erst danach umzuschalten. FAInfos bleibt die SQL-View (L11 :1105-1107: über SData
nicht gefunden). Daneben werden drei Lücken geschlossen, die der Lesetest aufgedeckt hat:

- beendete Haupt-FAs fehlen in `PpsFaBelegeInfo`,
- an IDEAL antwortet Sage mit 500, wenn `orderBy` fehlt,
- „Stückliste live“ zeigt beendete Haupt-FAs nicht und vermischt Ressourcenliste und FA-Stückliste.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. Schwerpunkt 4: orderBy-Inventur aller SData-Aufrufe (Tabelle unten) und Härtung des einzigen ungeschützten Wegs (`GetAsync`).
2. Schwerpunkt 6: Stückliste live, FA-Modus mit beendeten Haupt-FAs; Artikel-Modus als „Ressourcenliste (Vorlage), nicht FA-Stückliste“ kennzeichnen.
3. Schwerpunkt 3: Der SData-Reader ergänzt Wurzel und Wurzel-Positionen beendeter Haupt-FAs.
4. Schwerpunkte 1 und 2: Der SData-Reader füllt die Knotenfelder. Die Vergleichsfelder werden erweitert. Der Modus Vergleich kommt zuerst.
5. Schwerpunkt 2: Im SData-Modus wird hybrid gelesen (Knoten aus SData, FAInfos aus der View). Die Sperre P-F2 fällt nur für die Struktur.
6. Schwerpunkt 5: Der Weg zur Aufhebung von P-R1 wird beschrieben. Der geplante IDEAL-Wert für `Sync:ArtikelBenutzerfelder` wird festgehalten. Beides ist Doku, kein Code.

**Out-of-Scope**
- FAInfos über SData. L11 hat dafür keine Quelle gefunden (Pfad über VK-Beleg bzw. `Seriennummer` ungeprüft).
- Aufhebung von P-R1, also der Artikel-Abgleich über SData an IDEAL (eigene Folge-Spec, Rückfrage 8).
- Jedes Schreiben an Sage. Rückmeldungen siehe [[2026-10-30-fa-rueckmeldung-an-sage]].
- AKE: dort gibt es keine FA-Struktur, AKE ist flach.
- Setzen von Einstellungen auf Test oder Prod. Das macht Gerald nach dem Deploy (Manifest-Eintrag).

## Fachliche Anforderungen

**F1 orderBy (Schwerpunkt 4).** Jede SData-Leseabfrage mit `where` sendet ein `orderBy`. Ohne `orderBy` antwortet IDEAL mit 500, auch bei
`PpsFaBelegeInfo` und `dtsFaBelegsuche` (L11 :1087, L10 :1074). Fehlt das `orderBy`, bricht der Code vor dem Aufruf ab (Programmierfehler),
statt Sage aufzurufen.

**F2 Stückliste live (Schwerpunkt 6).**
- FA-Modus: Liefert `PpsFaBelegeInfo` die eingegebene BelID nicht, wird sie über `dtsFaBelegsuche.Sage.PPS` gesucht. Ist es ein beendeter Haupt-FA
  (1059463), erscheint die ganze Struktur mit der Wurzel oben und deren Positionen.
- Artikel-Modus: Überschrift und Hinweis lauten „Ressourcenliste (Vorlage) – nicht die FA-Stückliste“. Grund (L12 :1113-1116): Die Ressourcenliste
  von Artikel 80141025 hat 6 Positionen, FA 1059463 hat 13.

**F3 Beendete Haupt-FAs (Schwerpunkt 3).** Liegt eine Struktur im Lesefenster des Readers, fehlt aber ihr Haupt-FA in `PpsFaBelegeInfo`, dann
holt der Reader den Kopf über `dtsFaBelegsuche` (`where` BelID-Block, `orderBy=BelID`) und liest auch dessen Positionen. Heute entsteht
keine Wurzel, und die Positionen der Wurzel fallen weg:
- Wurzel nur, wenn der Kopf in der Menge ist (`SdataFaStrukturReader.cs:206-213`),
- Positionen nur für Köpfe der Menge (`:219`),
- `HauptArtnr` ist dann null (`:205`, `:227`).

Gemeldet wird nur noch, was auch `dtsFaBelegsuche` nicht liefert (Warnung wie `:235-237`).

**F4 Knotenfelder (Schwerpunkte 1 und 2).** Der SData-Reader füllt alle Felder von `FaHierarchyNode` (`FaHierarchyNode.cs:13-63`). Die Quelle
je Feld steht in der Zuordnungstabelle im Lösungsentwurf. Bis Prüfabfrage P-FS1 den View-Text liefert, gilt die Zuordnung als Arbeitshypothese
und wird **nur verglichen** (Rückfrage 2). Kein Feld wird aus seinem Namen zugeordnet. Arbeitsbereich, Beschaffungsartikel und HauptArtnr
bleiben ausdrücklich offen (Rückfrage 3).

**F5 Vergleich zuerst.** Mit `Sync:Quelle:FaStruktur = Vergleich` schreibt der Lauf weiter nur aus der View
(`FaStrukturQuelleSwitch.cs:52-76`, `SageQuelleWeiche.cs:74-132`). Die Abweichungen je Feld landen im Protokoll „Sage-Quellenvergleich“
(Anzahl plus die ersten 20).

**F6 SData-Modus hybrid (Schwerpunkt 2).** Mit `Sync:Quelle:FaStruktur = SData` kommen die Knoten aus SData und die FAInfos weiter aus der
View. Nur Strukturen mit FAInfos-Zeile werden geschrieben, dieselbe Grenze wie das `EXISTS` der View (`FaHierarchySql.cs:139-152`). Heute
liefert der SData-Reader `InfosAnzahl = 0` (`SdataFaStrukturReader.cs:106`). Der Empty-Guard (`FaHierarchySyncService.cs:59-75`) würde den
Lauf dann überspringen, deshalb ist der Hybrid Pflicht. Die Sperre P-F2 fällt nur für diesen Bereich. Umgeschaltet wird erst nach Rückfrage 5.

**F7 Artikel IDEAL (Schwerpunkt 5).** Es gibt keine Codeänderung. Belegt ist (`ArtikelBenutzerfelderWriter.cs:8-9`, `SageImportService.cs:552-557`):
Benutzerfelder werden nur im SData-Modus des Artikel-Abgleichs geschrieben. An IDEAL ist SData gesperrt, solange `Sync:ArticlesViewName` gesetzt ist
(`SageQuelleSperre.cs:17-23`, `ServiceSettingDefinitions.cs:266`). Ein IDEAL-Wert in `Sync:ArtikelBenutzerfelder` wirkt daher **erst nach
P-R1**. Der Weg dorthin steht im Lösungsentwurf, Abschnitt „Weg zu P-R1“. Die Blattfelder der FA-Struktur hängen **nicht** an P-R1: Der
FA-Reader liest die Artikel-`USER_OS*` selbst (E4).

## Technischer Loesungsentwurf

Muster: Quellen-Weiche `SageQuelleWeiche` (View | SData | Vergleich, Protokoll „Sage-Quellenvergleich“, Sperrregel nur in
`SageQuelleSperre.Grund`, siehe [[services]] und [[integrationen]]). Pro Abfrage gelten Block-Abfragen mit `OrBloecke`
(`SageSdataFaModels.cs:90-97`, höchstens 50 BelIDs). Es kommen keine neuen Klassen, Interfaces oder Settings dazu.

### orderBy-Inventur (Schwerpunkt 4)

Der Client verlangt `orderBy` strukturell bei `QueryAllAsync` (`SageSdataReadClient.cs:182-183`) und `QueryPageAsync` (`:225-226`). Ohne
`orderBy` werfen beide eine `ArgumentException`. `GetAsync` (RealTimeData) hängt `orderBy` nur an, wenn es gesetzt ist (`:78`). Diese
Lücke ist die einzige. Einziger Aufrufer von `GetAsync` ist `SageSdataFaController.cs:73`; der Dienst ruft `GetAsync` nicht auf (grep
`_client.GetAsync`, nur dieser Treffer).

| Datei:Zeile | Methode | where | orderBy | Ergebnis |
|---|---|---|---|---|
| `SageSdataFaController.cs:89` | GetAsync dtsFaBelegsuche | `BelID eq {id}` | **null** | **Fehler an IDEAL (500)**, an AKE lief es (QA 131.2.3) |
| `SageSdataFaController.cs:112` | GetAsync dtsFaBelegsuche | `StrukturID eq {id}` | BelID | ok |
| `SageSdataFaController.cs:136` | GetAsync Teileverwendungsnachweis/RueckKorrektur/Fertigmeldungen (`:148-149`) | BelID-Block | **null** | **Fehler an IDEAL (500)** |
| `SageTestController.cs:105` | QueryPageAsync (Feldliste) | Eingabe | Eingabe, Pflicht (`:65-66`) | ok |
| `SageTestController.cs:163` | QueryPageAsync PpsRessourcenListenInfo | Mandant+Artikel | Artikelnummer | ok |
| `SageTestController.cs:182` | QueryPageAsync PpsFaBelegeInfo | BelID | BelID | ok (aber Lücke F2) |
| `SageTestController.cs:190` | QueryAllAsync PpsFaBelegeInfo | StrukturID | BelID | ok |
| `SageTestController.cs:197` | QueryAllAsync PpsFaBelegePositionenInfo | BelID-Block | BelPosID | ok |
| `SdataFaStrukturReader.cs:78-79` | Köpfe | WhereKoepfe | BelID | ok |
| `SdataFaStrukturReader.cs:87-88` | Positionen | BelID-Block | BelPosID | ok |
| `SdataFaStrukturReader.cs:94-95` | LagerplaetzeInfo | – | PlatzID | ok |
| `SdataArtikelReader.cs:142-143` | Artikel (Feldprobe) | InFilter | Artikelnummer | ok |
| `SdataArtikelReader.cs:190-191` | Artikel | WhereBestellartikel | Artikelnummer | ok |
| `SdataArtikelReader.cs:193-194` | Ressourcen | WhereRessourcen | OrderByRessourcen | ok (Pflicht im Client) |
| `SdataArtikelReader.cs:212-213` | LagerplaetzeInfo | – | PlatzID | ok |
| `SdataArtikelReader.cs:302` | InBloeckenAsync | InFilter | Parameter (`:132`, `:152`, `:207` Artikelnummer; `:210` Artikelnummer,AuspraegungID) | ok |
| `SdataBestandReader.cs:55-56` | Bestand | WhereBestand | OrderByBestand | ok (Pflicht im Client) |
| `SdataBestandReader.cs:57-58` | LagerplaetzeInfo | – | PlatzID | ok |
| `SdataBestandReader.cs:100` | Zählen | – | OrderByZaehlen | ok |
| `SdataLagerplatzReader.cs:45-48` | Lagerorte / -plätze | WhereLagerorte / – | Lagerkennung / PlatzID | ok |
| `SdataArbeitsplatzReader.cs:41-42` | Arbeitsplätze | – | Arbeitsplatznummer | ok |
| `SdataBuchungLookupReader.cs:42-43` | Buchung | Where(marker) | OrderBy | ok (Pflicht im Client) |
| `SdataWareneingangLookupReader.cs:65` | WE-Beleg | where | BelID | ok |
| `ArtikelLieferantSyncService.cs:80`, `:84`, `:251` | Lieferanten / Varianten / letzte Bestellung | –, WhereVarianten, WhereLetzteBestellung | OrderBy-Konstanten | ok (Pflicht im Client) |
| `EinkaufBestellungenSyncService.cs:76` | EK-Positionen | Where(art) | OrderBy | ok (Pflicht im Client) |
| `KontokorrentSyncService.cs:102`, `:314` | Konten | where / – | Kto / Parameter | ok |
| `PpsMitarbeiterSyncService.cs:53` | Mitarbeiter | – | Nummer | ok |
| `StillstandsgruendeSyncService.cs:51` | Stillstandsgründe | – | Stillstandnummer | ok |
| `ServiceSettingsController.cs:176-199` | TestSageConnection, freie Ressource als Text | Freitext | Freitext | Diagnose-Werkzeug, Text des Admins, keine Änderung (Nebenbefund) |

Bei „ok (Pflicht im Client)“ ist der Wert der Konstanten nicht einzeln nachgelesen. Ohne `orderBy` wirft der Client vor dem Aufruf, Sage
wird also nie ohne `orderBy` erreicht.

**E1-Lösung:**
- `GetAsync` erhält dieselbe Prüfung wie `QueryAllAsync`: `where` gesetzt und `orderBy` leer → `ArgumentException`. Das ist ein Wächter an
  der gemeinsamen Stelle (Root Cause).
- `SageSdataFaController.cs:89` und `:136` übergeben `"BelID"`. `BelID` steht in allen vier Selects (`SageSdataFaModels.cs:83-87`).
- Ob RealTimeData an IDEAL `orderBy=BelID` bei diesen drei Quellen annimmt, ist ungeprüft (P-FS7).

### Stückliste live (E2, Schwerpunkt 6)

- `LadeFaAsync` (`SageTestController.cs:180-205`): Ist die Suche bei `:182` leer, sucht die Methode über
  `GetAsync(dtsFaBelegsuche, "BelID eq {id}", "BelID,StrukturID,Artikelnummer,Matchcode,…", "BelID")`.
  - Wie in `SageSdataFaController` wird `StrukturID` bestimmt.
  - Die Wurzelzeile wird in `fas` übernommen, wenn sie in `PpsFaBelegeInfo where StrukturID eq` (`:190`) fehlt.
  - Die BelID der Wurzel kommt in den Positionsblock (`:193-199`).
  - `PpsFaBelegePositionenInfo` liefert die 13 Positionen von 1059463 (L12 :1114).
  - Heute fehlt die Wurzel in `faJeId`: `FaBaum` bricht dann für die Wurzel ab (`SageTestAufbereitung.cs:221`) und hängt die Unter-FAs flach
    ans Ende (`:230`).
  - Im Aufruf-Protokoll (`vm.Aufrufe`) erscheint die Zeile „Kopf über dtsFaBelegsuche (in PpsFaBelegeInfo nicht vorhanden, beendet?)“.
- `LadeArtikelAsync` (`:161-178`): `vm.Titel` (`:172`) beginnt mit „Ressourcenliste (Vorlage)“. Dazu kommt ein dauerhafter Hinweis: „Das ist
  die Ressourcenliste des Artikels, nicht die Stückliste eines Fertigungsauftrags. Die FA-Stückliste zeigt der FA-Modus.“ Er erscheint in
  der View `Views/SageTest/Stueckliste.cshtml` (Datei nicht gelesen; vorhandenes Hinweis-/Alert-Muster der Seite nutzen, Skill
  `frontend-design` Pflicht, Kontrast AA, nicht nur Farbe).

### Beendete Haupt-FAs im Dienst (E3, Schwerpunkt 3)

- Nach `KoepfeFiltern` (`SdataFaStrukturReader.cs:114-125`) gilt: `fehlend = StrukturIDs der Menge ohne Kopf mit BelID = StrukturID`.
- Für diese Strukturen holt der Reader die Köpfe über `dtsFaBelegsuche` in BelID-Blöcken (`OrBloecke`, `orderBy=BelID`, Select wie
  `SdataFaKopfZeile` plus Knotenfelder aus E4). Die Positionen dieser Köpfe kommen mit in die Positionsblöcke (`:85-88`).
- Danach bleibt `Projizieren` unverändert: Die Wurzel ist jetzt in `kopfJeId`.
- Nicht ergänzt werden fehlende **Unter-FAs**. Ob beendete Unter-FAs ebenfalls fehlen, ist ungeprüft (P-FS4); die 54 Unter-FAs von 1059463
  kamen (L11 :1088).
- Hängt an Rückfrage 1: RealTimeData im Dienst. Ist die Antwort „nein“ und funktioniert P-FS3 nicht, bleibt E3 als Blocker stehen. Die
  Warnung „ohne Wurzel“ (`:235-237`) bleibt dann der sichtbare Hinweis.

**Gegenprobe (Schwerpunkt 3):** Belegt ist nur ein Fall (1059463: `IstBeendet -1`, `Istendtermin 2026-10-30`, L12 :1118-1120). Dass
`PpsFaBelegeInfo` beendete Haupt-FAs grundsätzlich filtert, ist **ungeprüft**. Die Ursache sagt L11 :1089 „ungeklärt (Ressourcenfilter?)“.
P-FS4 prüft das mit weiteren beendeten Haupt- und Unter-FAs. Am Fenster `IstBeendet eq 0 or Istendtermin ge heute-30`
(`SdataFaStrukturReader.cs:67-68`) liegt es nicht: 1059463 (30.10.) liegt am 16.11. innerhalb von 30 Tagen.

### Knotenfelder (E4, Schwerpunkte 1 und 2)

Quelle der Spalte „View-Herkunft“ ist nur die Repo-Doku `vw_IDEAL-AKE_Kommissionierung_FAListe.sql:21-48`. Sie ist als „laut Anhang“ markiert
und **nicht** der View-Text. Sie widerspricht L11 an einer Stelle: Die Doku sagt „Position bevorzugt, Beleg als Fallback“. L11 fand die
Werte der Blätter am **Artikel** (:1096-1097), nicht an Position oder Beleg. Den Widerspruch löst nur P-FS1 bzw. P-FS2.

| Feld (`FaHierarchyNode`) | View-Herkunft laut Repo-Doku | SData-Kandidat (Arbeitshypothese) | Status |
|---|---|---|---|
| HauptFA | KHKPpsFaBelege.StrukturID (:23) | Kopf.StrukturID | belegt (L10 :1070), heute schon |
| VaterFA, Position, SubFA | Positionen.BelID/Position/BelIDNachfolger (:25-27) | PpsFaBelegePositionenInfo | belegt, heute schon |
| HauptArtnr | KHKPpsFaBelege.Artikelnummer (Wurzel) (:24) | Artikelnummer des Wurzelkopfs (`:205`) | **offen** (Rückfrage 3), wird verglichen |
| Artnr | Positionen.RessourceNummer (:28); Wurzel unbelegt | Position.RessourceNummer; Wurzel = Kopf.Artikelnummer (`:210`) | Wurzel offen (wird wie heute nicht verglichen, `FaStrukturQuelleSwitch.cs:18`) |
| Sollmenge, Fertigungmenge | Positionen.Menge (:32-33); Wurzel unbelegt | Position.Menge | wie Artnr |
| Matchcode | Position bevorzugt, KHKArtikel Fallback (:29) | Artikel.Matchcode (`SdataArtikelReader.cs:47`) | offen: ob die Position ein Matchcode-Feld hat, ist ungeprüft |
| Bezeichnung1/2 | Sub-FA-Beleg bevorzugt, Stammdaten Fallback (:30-31) | FA-Kopf (Unter-FA), sonst Artikel.Bezeichnung1/2 | Feldnamen am FA-Kopf ungeprüft (P-FS5) |
| Beschaffungsartikel | KHKArtikel.IstBestellartikel, Ja/Nein (:34) | Artikel.IstBestellartikel (-1 = true, Filter `SdataArtikelReader.cs:63`) | **offen** (Rückfrage 3) |
| Artikelgruppe | „CODE - Bezeichnung“ (:35) | Artikel.Artikelgruppe + Bezeichnung der Gruppe | offen: Gruppenbezeichnung braucht eine Artikelgruppen-Quelle (P-FS5) |
| Hauptlagerplatz | KHKLagerplaetze.Kurzbezeichnung (:36) | Artikel-Hauptlagerplatz → LagerplaetzeInfo.Kurzbezeichnung (Muster `SdataArtikelReader.cs:232`) | offen: ob die View den Platz des Artikels oder den der Position nimmt |
| BemerkungPN, FertigungsInfoPN, Kommissionieren, Arbeitsschritte, Artikeltyp, Material, VMBedarf | Position bevorzugt, Beleg Fallback (:37-48) | FA-Knoten: `USER_OS<Feld>` am Unter-FA (dtsFaBelegsuche, L10 :1073); Blatt: `USER_OS<Feld>` am Artikel (L10 :1074) | Hypothese, wird verglichen |
| Arbeitsbereich | Position bevorzugt, Beleg Fallback, „positionsbezogen!“ (:40) | `USER_OSAbteilung`? | **offen** (Rückfrage 3), Name allein ist kein Beleg |
| Beschichtet, Breite, Hoehe, Tiefe, EKBedarf | Position bevorzugt, Beleg Fallback (:43, :46-47) | FA-Knoten: `USER_OSBeschichtet/OSBreite/OSHoehe/OSTiefe/OSEKBedarf` (L10 :1073); Blatt: **keine Quelle gefunden**, denn diese Felder fehlen in der Artikelliste (L10 :1074) | **Lücke** (Rückfrage 4) |

Umsetzung E4:
- **Lesen.**
  - FA-Felder je Unter-FA und Wurzel: ein Block-Aufruf `dtsFaBelegsuche` (`where` BelID-Block, `orderBy=BelID`, Select nur die nötigen
    `USER_OS*`) oder, wenn P-FS3 trägt, `PpsFaBelegeInfo` mit `include=SpecificUserfields` (Rückfrage 1). Ein Filter auf USER-Felder ist
    verboten (500, L10 :1073).
  - Blattfelder: Artikel-Abfrage in Blöcken über die Artnr der Blätter. Muster und Fehlerbehandlung wie `BenutzerfelderLesenAsync`
    (`SdataArtikelReader.cs:123-173`): Ein unbekanntes Feld wird zur Warnung und lässt den Lauf nicht scheitern. Die Methode wird
    wiederverwendet bzw. sichtbar gemacht, nicht kopiert.
- **Abbilden** in `Projizieren`, über ein Feld-Mapping an **einer** Stelle.
  - Booleans mit derselben Regel wie der View-Weg (`SageFaStrukturReader.cs:180-195`: Ja/true/-1/1/Zahl ≠ 0 → true, NULL → false). Die
    Regel wird gemeinsam genutzt, nicht dupliziert.
  - Dezimalzahlen wie im View-Weg: NULL → 0.
- **Vergleich.** `VergleichsFelder` (`FaStrukturQuelleSwitch.cs:20-30`) nehmen alle Knotenfelder auf. Für Blattfelder ohne Quelle (Lücke)
  bleibt der Wert im SData-Weg leer und wird verglichen. Die Abweichung ist dann gewollt sichtbar (Melden statt still).
- **Last.** Je Lauf kommen ⌈FAs/50⌉ RealTimeData-Aufrufe und ⌈Blatt-Artnr/…⌉ Artikel-Aufrufe dazu. Volumen und Dauer sind ungeprüft
  (P-FS6, Rückfrage 7). RealTimeData ignoriert `count` (`SageTestController.cs:106`).

### Hybrid im SData-Modus + Sperre (E5)

- `FaStrukturQuelleSwitch.LesenAsync`: Der SData-Zweig liest zusätzlich **nur die FAInfos** über den View-Reader und gibt
  `Lesung(sdataKnoten gefiltert auf mitInfos, viewInfos, …, InfosAnzahl = viewInfos)` zurück.
  - Dafür bekommt `SageFaStrukturReader` eine Methode, die nur den FAInfos-Teil ausführt (`FaHierarchySql.cs:155-163` und die Lesung
    `SageFaStrukturReader.cs:137-169` gibt es schon). Neu ist nur der Einstieg, kein neuer SQL-Text.
  - Heute filtert nur der Vergleich mit `mitInfos` (`:54-71`). Im SData-Modus gilt derselbe Filter für das, was geschrieben wird.
- `SageQuelleSperre.Grund`: `"FaStruktur"` liefert keinen Sperrgrund mehr (`:17-23`). Das `TODO(P-F2)` entfällt in
  `SdataFaStrukturReader.cs:28-33` und `FaStrukturQuelleSwitch.cs:45-46`.
- Verhalten bei SData-Fehlern im SData-Modus bleibt wie in `SageQuelleWeiche` (unverändert). Der Full-Refresh läuft in einer Transaktion
  (`FaHierarchySyncService.cs:97-106`), ein Abbruch schreibt also nichts halb.
- Umschalten ist eine Einstellung von Gerald (Manifest), kein Code (Rückfrage 5). Rückweg: `View` setzen, der nächste Lauf schreibt wieder
  aus der View.

### Weg zu P-R1 (Schwerpunkt 5, nur Doku)

1. P-R1 ausführen (Stammdaten-Spec :894: `OBJECT_DEFINITION` der View aus `Sync:ArticlesViewName`). Ergebnis: WHERE-Filter des IDEAL-Artikelbestands.
2. Den Filter in `SdataArtikelReader` als IDEAL-Variante nachbilden. Heute liest der Reader die AKE-Menge (`ServiceSettingDefinitions.cs:266`
   „dieselbe Menge wie der AKE-SQL-Filter“).
3. Vergleichslauf Artikel an IDEAL (Vergleich ist trotz Sperre erlaubt? → im Folge-Spec prüfen, `ArtikelQuelleSwitch.cs:41-49`).
4. Sperre in `SageQuelleSperre.Grund` für Artikel lockern.
5. Erst dann `Sync:ArtikelBenutzerfelder` an IDEAL setzen. Geplanter Wert aus L10 :1074:
   `USER_OSIstLagerartikel,USER_OSArtikeltyp,USER_OSArbeitsschritte,USER_OSMaterial,USER_OSMaterialnummerPN,USER_OSAbteilung,USER_OSFertigungsInfoPN,USER_OSBemerkungPN,USER_OSBlechdicke,USER_OSKommissionieren,USER_OSVMBedarf`.

   Der Default enthält AKE-Felder (`ServiceSettingDefinitions.cs:270`). An IDEAL würden sie jeweils einzeln geprüft und als unbekannt
   gemeldet (`SdataArtikelReader.cs:135-149`). Deshalb muss der Wert an IDEAL ersetzt werden, nicht ergänzt.
   `USER_OSBeschichtet` ist am Artikel nicht belegt (L10 :1074) und steht nicht in der Liste.

### Wechselwirkung mit [[2026-11-16-beschichtungslogik-je-standort-spec]]

- Dort ist `SageFaStrukturReader` der einzige Schreiber von `FaHierarchyNode.Beschichtet` (Beschichtungs-Spec :152-156). Der SData-Reader
  liefert es heute nicht. Nach E4/E5 schreibt im SData-Modus auch der SData-Weg `Beschichtet`. Für Blätter gibt es keine belegte Quelle
  (Rückfrage 4). Wird umgeschaltet, bevor die Lücke geklärt ist, kippt `Beschichtet` an Blättern auf false. Das verändert die
  Beschichtungslogik. Deshalb steht `Beschichtet` in den Vergleichsfeldern, und Rückfrage 4 blockiert das Umschalten.
- `Start_Beschichtung` und die übrigen FAInfos bleiben die View (F6). Die Termin-Quelle der Beschichtungs-Spec ist unberührt.
- Die Prüfabfrage P-I4 der Beschichtungs-Spec (`OBJECT_DEFINITION` FAListe/FAInfos) ist dieselbe wie P-FS1. Gerald führt sie einmal aus;
  das Ergebnis gilt für beide Specs.
- Reihenfolge: Rückfrage 6.

## Migrations-/SQL-Auswirkungen

Keine. `FaHierarchyNode` hat alle Spalten schon (`FaHierarchyNode.cs:13-63`); es werden nur zusätzliche Felder gefüllt. Es gibt keine
Migration, kein `SQL/153`, keine Änderung an `00_FreshInstall.sql` und an `SQL/AgentJobs/`. Die Nummer SQL/153 bleibt frei. Ergebnis von
P-FS1 gehört als **Doku** nach `SQL/sage-views/` (TODO `FAListe.sql:57`). Das ist keine WMS-Migration und kein Manifest-Schema-Eintrag.

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `Sync:Quelle:FaStruktur` (geändert: Beschreibung + Standort-Text, `ServiceSettingDefinitions.cs:108`, `:267`) | Choice (`:192`, `QuellenWerte`) | View \| SData \| Vergleich (Auswahlliste, Server prüft) | View (unverändert) | View = wie bisher. Vergleich = liest beide und schreibt aus der View; Abweichungen je Knotenfeld im Protokoll „Sage-Quellenvergleich“. SData = Knoten und Felder aus SData, Auftragsdaten (FAInfos) weiter aus der View, nur Strukturen mit FAInfos. Die Sperre P-F2 entfällt. | Dienst: Lauf „FA-Hierarchie“ | IDEAL (Text ersetzt „gesperrt bis P-F2“) | vorhandener `WarnSData` bleibt. Zusatz: „SData erst nach abweichungsfreiem Vergleich (Spec 2026-11-16-fa-struktur-ideal-ueber-sdata) umschalten.“ |
| `Sync:ArtikelBenutzerfelder` (nur Doku, keine Codeänderung, `:111`, `:270`) | String, kommagetrennt | `USER_[A-Za-z0-9_]{1,100}` (Server/Reader prüft, `SdataArtikelReader.cs:98-114`) | AKE-Liste (unverändert) | Wirkt nur im SData-Modus des Artikel-Abgleichs. An IDEAL erst nach P-R1. | Dienst: Artikel-Abgleich | beide (IDEAL-Wert siehe „Weg zu P-R1“) | – |

Sonst gibt es keine neuen Keys. `BeendetFensterTage = 30` bleibt eine Konstante (`SdataFaStrukturReader.cs:45`); eine Änderung entscheidet
P-FS1 (WHERE der View).

## Audit-Feld-Auswirkungen

Keine. `FaHierarchyNode` ist bewusst keine `AuditableEntity`, sondern eine Cache-Tabelle mit Full-Refresh (`FaHierarchyNode.cs:8-10`).
Nachvollziehbar bleibt sie über das Aktivitäts-Protokoll und `SyncedAt` (Lokalzeit, ADR 0025). `ProductionOrderPositions` und
`FaHierarchyOrderInfo` ändern ihr Schreibverhalten nicht. Es gibt keine neuen Rollen: `SageTestController` und `SageSdataFaController` sind
`[RequireAdminAccess]` (`SageTestController.cs:17`). Keine neue Listen-View, also keine Pflichten nach dem Listen-View-Pattern.

## Akzeptanzkriterien

1. `SageSdataReadClient.GetAsync` mit `where` und ohne `orderBy` wirft eine `ArgumentException`; kein HTTP-Aufruf (Unit-Test).
2. Kein `GetAsync`-Aufruf im Code übergibt bei gesetztem `where` `orderBy = null` (Unit-Test bzw. Wächter-Test über den Controller-Pfad).
   An IDEAL-DEV liefert FA-Struktur live für 1069595 kein HTTP 500 mehr.
3. Stückliste live, FA-Modus, BelID 1059463 an IDEAL-DEV: Die Wurzel 1059463 steht oben mit 13 Positionen, darunter die Unter-FAs. Das
   Aufruf-Protokoll zeigt den Weg über `dtsFaBelegsuche`.
4. Stückliste live, FA-Modus, eine Unter-FA-BelID (z. B. 1065143): Die ganze Struktur 1059463 erscheint mit dem Hinweis „gehört zur Struktur“.
5. Stückliste live, Artikel-Modus, 80141025: Die Überschrift beginnt mit „Ressourcenliste (Vorlage)“, der Hinweis „nicht die Stückliste
   eines Fertigungsauftrags“ ist sichtbar und nicht nur farblich unterschieden.
6. Unit-Test `SdataFaStrukturReader`: Köpfe ohne Haupt-FA plus ein über `dtsFaBelegsuche` gelieferter Haupt-FA ergeben eine Wurzel und die
   Knoten der Wurzel-Positionen mit `HauptArtnr`. Ein auch dort fehlender Haupt-FA erzeugt die Warnung „ohne Wurzel“.
7. Unit-Test `Projizieren`: Bekommt ein Unter-FA-Knoten FA-Felder, ein Blatt Artikelfelder, werden alle Felder gefüllt.
   Bool-Regel identisch zum View-Weg: `-1`, `Ja` → true; NULL → false.
8. Unit-Test Vergleich: Abweichung nur in `Arbeitsschritte` an einem Blatt ergibt genau eine abweichende Zeile mit Feldname.
9. IDEAL-DEV, `Sync:Quelle:FaStruktur = Vergleich`: Der Lauf FA-Hierarchie schreibt aus der View (Knotenzahl wie mit View). Das Protokoll
   „Sage-Quellenvergleich“ zeigt die Schlüssel View/SData und die Anzahl abweichend je Lauf.
10. IDEAL-DEV, `= SData`: Der Lauf überspringt nicht (InfosAnzahl > 0) und schreibt Knoten nur für Strukturen mit FAInfos. Die FAInfos
    entsprechen der View-Lesung. Es gibt keine Warnung „gesperrt (P-F2)“.
11. Zurück auf `View`: Der nächste Lauf schreibt wieder aus der View.
12. Ein unbekanntes Artikel-`USER_OS*`-Feld im Blatt-Select ergibt eine Warnung im Protokoll; der Lauf läuft weiter.
13. Build und alle Tests grün, `NachlaufManifestDriftTests` grün.

## Test-Szenarien

Neu in `docs/TESTSZENARIEN.md` (Kapitel Sage-SData / FA-Struktur IDEAL), danach [[testszenarien-index]]:

- **TS-146 orderBy RealTimeData**
  - Vorbedingung: IDEAL-DEV mit SData-Zugang, Admin.
  - Schritte: FA-Struktur live 1069595 und 1059463.
  - Erwartet: kein 500, Positionen, Rückmeldungen und Fertigmeldungen geladen (oder sauberer Fehlertext je Quelle).
  - Negativfall AKE-DEV: dieselbe Ansicht mit einem AKE-FA funktioniert wie vorher (Rückfrage 10).
- **TS-147 Stückliste live**
  - FA-Modus 1059463 (beendet): Wurzel plus 13 Positionen. Unter-FA 1065143: ganze Struktur.
  - Artikel-Modus 80141025: Kennzeichnung „Ressourcenliste (Vorlage)“.
  - Negativ: BelID, die es nirgends gibt → „in Sage nicht gefunden“. Artikelnummer mit `'` → Eingabefehler, kein Aufruf.
- **TS-148 Vergleichslauf Knotenfelder**
  - Vorbedingung: `Sync:Quelle:FaStruktur = Vergleich` (nur DEV).
  - Schritte: Lauf starten, Protokoll öffnen.
  - Erwartet: View-Daten im WMS unverändert, Abweichungen je Feld sichtbar.
  - Stichprobe 1059463 und 1069595 je Feld gegen P-FS5.
  - Negativ: SData nicht erreichbar → Lauf schreibt aus der View, Fehlermail, Protokoll „fehlgeschlagen“ (`SageQuelleWeiche.cs:110-116`).
- **TS-149 Beendeter Haupt-FA im Dienst**
  - Erwartet: Im Vergleich erscheint 1059463 mit Wurzel und erster Ebene, keine Warnung „ohne Wurzel“ für 1059463.
- **TS-150 Umschalten SData + Rückweg**
  - `= SData`: FA-Struktur-Ansicht und Kommissionierung für 1069595 sehen aus wie mit View (bis auf freigegebene Abweichungen).
  - Zurück `= View`: wieder die View-Daten.
  - Negativ: Struktur ohne FAInfos-Zeile erscheint nicht.

## Etappen (nur bei epic: true)

**Warum Epic:** Sechs Schwerpunkte in Web und Dienst, die aufeinander aufbauen: zuerst Vergleich, dann Umschalten. Das ist ein Paket für
einen Worktree mit einem Merge, ohne einzeln mergbare Teile. Mehrere Etappen hängen an Prüfabfragen bzw. Rückfragen und bekommen deshalb
eigene Zeilen. Jede Etappe baut und testet grün, keine Etappe ändert das Schema. Während der Arbeit den Zweig mit
`scripts/sync-worktree.ps1 -Slug 2026-08-07-ideal-teile-1-5` aktuell halten.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | E1 orderBy-Pflicht in `GetAsync` + `SageSdataFaController` :89/:136 + Tests (AK 1, 2). Keine Abhängigkeit. | erledigt | 21b3fb65 |
| 2 | E2 Stückliste live: Kopf über dtsFaBelegsuche, Wurzel in `FaBaum`, Kennzeichnung Artikel-Modus (AK 3–5). Keine Abhängigkeit (Web-Diagnose nutzt RealTimeData schon). | erledigt | 28dfdc4c |
| 3 | E3 Dienst: Wurzel und Positionen beendeter Haupt-FAs (AK 6). **Hängt an Rückfrage 1** (RealTimeData im Dienst bzw. P-FS3). Ohne Antwort: Code hinter der Entscheidung nicht bauen, Blocker festhalten, weiter mit E4 (Blattfelder). | erledigt (RealTimeData nur im Vergleich, Freigabe-Antwort 1) | a3214677 |
| 4 | E4 Dienst: Knotenfelder (FA-Felder über die Quelle aus Rückfrage 1, Blattfelder über Artikel) + Vergleichsfelder (AK 7, 8, 12). Arbeitshypothese nach Rückfrage 2. Die Feldzuordnung wird nach P-FS1/P-FS5 nur in der Mapping-Stelle korrigiert. | erledigt (Arbeitshypothese) | 24ad7354 |
| 5 | E5 Hybrid SData-Modus + Sperre P-F2 nur für die Struktur (AK 9–11). Baubar. **Umschalten auf IDEAL hängt an Rückfragen 4 und 5** (Einstellung Gerald, nicht Teil des Laufs). | erledigt, Sperre bleibt (Auflage Gerald, AK 10 nicht erreichbar) | 9fa447f1 |
| 6 | E6 Doku/Brain: `docs/betrieb/04-konfiguration.md` (Sync:Quelle:FaStruktur), `docs/betrieb/05-schnittstellen.md` (SData-Quellen FA-Struktur inkl. dtsFaBelegsuche), README AppSettings/ServiceSettings, Hilfe + `Views/Help/Changelog.cshtml`, Version 1.99.26 in beiden `AppVersion.cs`, TS-146..150, ADR 0029, Manifest, Brain (siehe Deploy). | erledigt | 5163df17 |

Brain-Pflichten des Dev-Laufs (Hauptcheckout `secondbrain/`, nicht im Worktree):
- ADR **0029** „FA-Struktur IDEAL: Knoten aus SData, FAInfos aus der View (hybrid); Quelle beendeter Haupt-FAs“. Er nimmt G11 für die
  Knotenfelder zurück, ändert die ADRs 0008/0014/0024 nicht und ersetzt keinen ADR.
- [[fallstricke]]:
  - „IDEAL: SData ohne `orderBy` → 500, auch RealTimeData. `GetAsync` prüft jetzt.“
  - „PpsFaBelegeInfo liefert beendeten Haupt-FA 1059463 nicht, dtsFaBelegsuche schon.“
  - „Ressourcenliste ≠ FA-Stückliste (L12).“
- [[services]] (FaStrukturQuelleSwitch hybrid), [[integrationen]] (dtsFaBelegsuche im Dienst, falls Rückfrage 1 = ja), [[feature-map]],
  [[sage-sdata-benutzerfelder-lesetest]] (Ergebnisse P-FS*), changelog `secondbrain/changelog/2026-11-xx-v1-99-26-fa-struktur-ideal-sdata.md`,
  [[testszenarien-index]].
- Stammdaten-Spec: Bei G11/P-F2 einen Vermerk „überholt für Knotenfelder durch [[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]]“ anbringen,
  additiv, nichts löschen.

## Deploy

**Final (QA 2026-11-16, gegen den echten Diff `3f247f11..5163df17`):**
- `deploy.web: true` (IdealAkeWms/: SageSdataReadClient, SageSdataFaController, SageTestController, View Stueckliste, ServiceSettingDefinitions, SageQuelleSperre, Hilfe/Changelog, AppVersion).
- `deploy.service: true` (IDEALAKEWMSService/: SdataFaStrukturReader, FaStrukturQuelleSwitch, SageFaStrukturReader, SdataArtikelReader, AppVersion; dazu SageQuelleSperre/ServiceSettingDefinitions über das gemeinsame Projekt).
- `deploy.migration: false` (keine Datei unter `*/Migrations/`, kein SQL).
- Paket: `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` (IDEALAKEWMSWeb + IDEALAKEWMSWebService + SQL + deploy), Version 1.99.26, Commit `5163df1751b0429938dad6ad8fd64f40d81aa1b4`, erstellt 2026-11-16 17:28.
  Beide Komponenten müssen deployt werden. Reihenfolge: Web und Dienst gemeinsam (kein Backup-Zwang, kein Schema). Nachlauf: Manifest-Einträge `v1.99.26-IDEAL-FaStrukturVergleich` (Einstellung, nur anzeigen) und `v1.99.26-Info` sind vorhanden (`scripts/deploy/nachlauf.psd1`).
- Ablauf Mensch: publish aus dem Worktree → Testsystem → Test → Merge. Nach dem Merge nur dann neu aus main publizieren, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat.

*Vorläufig (Spec-Agent), durch den Abschnitt oben ersetzt:*

- **Web-App:** ja (E1, E2: SageSdataReadClient, SageSdataFaController, SageTestController, View Stueckliste, ServiceSettingDefinitions-Text).
- **Service:** ja (E3–E5: SdataFaStrukturReader, FaStrukturQuelleSwitch, SageFaStrukturReader, SageQuelleSperre über das gemeinsame Projekt).
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`):
  - `@{ Id='v1.99.26-IDEAL-FaStrukturVergleich'; Version='1.99.26'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'`
    `Text='Sync:Quelle:FaStruktur = Vergleich setzen (/ServiceSettings), Lauf FA-Hierarchie abwarten, Protokoll Sage-Quellenvergleich pruefen. SData erst nach Freigabe (Spec 2026-11-16-fa-struktur-ideal-ueber-sdata, Rueckfragen 4/5).'; Quelle='2026-11-xx-v1-99-26-fa-struktur-ideal-sdata' }`
  - `@{ Id='v1.99.26-Info'; Version='1.99.26'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'`
    `Text='Sage-Test > Stueckliste live: Artikel-Modus heisst jetzt Ressourcenliste (Vorlage); FA-Modus zeigt auch beendete Haupt-FAs.'; Quelle='…' }`
  - Kein Schema-, Daten-, Einmal- oder Prüfungs-Eintrag: Es gibt kein SQL, und die Quelleinstellung prüft der Dienst selbst (`SageQuelleSchalter.cs:33-40`).
  - Texte nur ASCII (`NachlaufManifestDriftTests.DeploySkripte_NurAscii_OhneSage_RechteNurHinzufuegend`). Die Ids sind eindeutig
    (`Manifest_IdsEindeutig_FelderGueltig`).
  - `Sync:ArtikelBenutzerfelder` an IDEAL bekommt **keinen** Eintrag in dieser Version (wirkt erst nach P-R1).
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- **Reihenfolge:** Web und Dienst gemeinsam. Default `View` bedeutet: Nach dem Deploy ändert sich nichts, bis Gerald `Vergleich` setzt.
  DEV AKE nie mit main deployen (Bündelregel).

### Prüfabfragen für Gerald (nur lesen; Ergebnisse in [[sage-sdata-benutzerfelder-lesetest]])

**P-FS1 – View-Texte** (= P-F1 / P-I4 der Beschichtungs-Spec):
```sql
-- WMS-DB IDEAL
SELECT [Key], [Value] FROM dbo.ServiceSettings
WHERE [Key] IN ('Sync:FaHierarchyListeViewName','Sync:FaHierarchyInfosViewName','Sync:ArticlesViewName');
-- Sage-DB IDEAL, je Name (ohne Klammern einsetzen)
SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.<ViewName>'));
```
Festhalten:
- WHERE der FAListe (welche Strukturen, beendete?),
- Herkunft jeder Spalte, besonders Arbeitsbereich, Beschaffungsartikel, HauptArtnr, Hauptlagerplatz,
- was „Position“ und „Beleg“ bei Blättern heißt.

Den Text nach `SQL/sage-views/` übernehmen (Doku).

**P-FS2 – USER-Spalten** (erweitert P-F2):
```sql
SELECT OBJECT_NAME(object_id) AS Tabelle, name, TYPE_NAME(user_type_id) AS Typ, max_length
FROM sys.columns
WHERE object_id IN (OBJECT_ID(N'dbo.KHKPpsFaBelege'), OBJECT_ID(N'dbo.KHKPpsFaBelegePositionen'), OBJECT_ID(N'dbo.KHKArtikel'))
  AND name LIKE N'USER[_]OS%'
ORDER BY Tabelle, name;
```

**P-FS3 – Benutzerfelder ohne RealTimeData?** Dataset `IDEAL;1`, Basis `https://idealsage100tes.ideal.ideal-ake.at:5493/sdata/ol/`:
- `CommonWawiServicesResources/IDEAL;1/PpsFaBelegeInfo?where=BelID eq 1065143&orderBy=BelID&include=SpecificUserfields`
- `CommonWawiServicesResources/IDEAL;1/PpsFaBelegePositionenInfo?where=BelID eq 1059463&orderBy=BelPosID&include=SpecificUserfields`

Erwartung offen. Kommen `USER_OS*`, entfällt RealTimeData im Dienst für die Felder (Rückfrage 1).

**P-FS4 – Gegenprobe beendet:**
- `CommonWawiServicesResources/IDEAL;1/PpsFaBelegeInfo?where=IstBeendet eq -1&orderBy=BelID desc&count=5&select=BelID,StrukturID,VerursacherTyp,IstBeendet`
  (0 Zeilen = die Ressource filtert beendete FAs; Feldname `IstBeendet` an dieser Ressource ungeprüft, bei 500 ohne `where` mit `select`
  prüfen).
- Danach zwei weitere beendete Haupt-FAs und zwei beendete Unter-FAs (BelIDs aus `RealTimeData/IDEAL;1/dtsFaBelegsuche.Sage.PPS?where=IstBeendet eq -1&orderBy=BelID desc&select=BelID,StrukturID,VerursacherTyp,Istendtermin`)
  je einzeln gegen `PpsFaBelegeInfo?where=BelID eq <id>&orderBy=BelID`.

**P-FS5 – Stichprobe je Feld:**
```sql
-- Sage-DB IDEAL, View-Name aus P-FS1
SELECT * FROM dbo.<FAListe> WHERE HauptFA IN (1059463, 1069595) ORDER BY HauptFA, VaterFA, Position;
```
Dagegen:
- `RealTimeData/IDEAL;1/dtsFaBelegsuche.Sage.PPS?where=BelID eq 1065143 or BelID eq 1065144 or BelID eq 1059463&orderBy=BelID`
  (alle Felder),
- `CommonWawiServicesResources/IDEAL;1/Artikel?where=Artikelnummer eq '73002467'&orderBy=Artikelnummer`
  (Matchcode, Bezeichnung1/2, Artikelgruppe, IstBestellartikel, Hauptlagerplatz-Feld, `USER_OS*`).

Je Spalte notieren: gleich / anders / fehlt. Pflicht für Arbeitsbereich ↔ `USER_OSAbteilung`, Beschaffungsartikel ↔ `IstBestellartikel`,
HauptArtnr ↔ Artikelnummer der Wurzel, Artikelgruppe (Format „CODE - Bezeichnung“).

**P-FS6 – Volumen/Dauer RealTimeData:**
- `RealTimeData/IDEAL;1/dtsFaBelegsuche.Sage.PPS?where=IstBeendet eq 0&orderBy=BelID&select=BelID`
  (Zeilen und Dauer im Browser-Netzwerkreiter),
- `…?where=StrukturID eq 1059463&orderBy=BelID&select=BelID,StrukturID` (55 erwartet: Kopf plus 54).

**P-FS7 – orderBy an RealTimeData** (für E1):
- `RealTimeData/IDEAL;1/dtsTeileverwendungsnachweis.Sage.PPS?where=BelID eq 1059463&orderBy=BelID`
- dasselbe für `dtsRueckKorrektur.Sage.PPS` und `dtsFertigmeldungen.Sage.PPS`

→ 200 erwartet.

**P-FS8 – Blattfelder am Artikel:**
- `CommonWawiServicesResources/IDEAL;1/Artikel?where=Artikelnummer eq '73002467'&orderBy=Artikelnummer&select=Artikelnummer,USER_OSBeschichtet,USER_OSBreite,USER_OSHoehe,USER_OSTiefe,USER_OSEKBedarf`

500 = mindestens ein Feld fehlt. Dann einzeln prüfen (Rückfrage 4).

## Offene Rueckfragen

1. **RealTimeData im Dienst.** Laut DPS ist RealTimeData nicht für den Betrieb freigegeben (Lesetest U4/U9 :1056, :1061). Die Web-Diagnosen
   nutzen es schon (Admin). Für E3/E4 bräuchte der Dienst `dtsFaBelegsuche` (Köpfe beendeter Haupt-FAs, `USER_OS*` am FA). Darf er das,
   oder zuerst P-FS3 abwarten? *Empfehlung:* zuerst P-FS3. Trägt es nicht: RealTimeData im Dienst nur lesend und zuerst nur im Modus
   Vergleich, mit ausdrücklicher Freigabe von Gerald und Nachfrage bei DPS (U9).
2. **Arbeitshypothese bis P-FS1.** E4 mit der Feldquelle aus L11 bauen (Unter-FA-Felder vom FA-Kopf, Blattfelder vom Artikel), obwohl die
   Repo-Doku „Position bevorzugt, Beleg Fallback“ sagt? *Empfehlung:* ja. Der Vergleich schreibt nichts und zeigt die Wahrheit je Feld. Die
   Korrektur bleibt eine Stelle im Mapping.
3. **Arbeitsbereich / Beschaffungsartikel / HauptArtnr.** Die Kandidaten `USER_OSAbteilung`, `Artikel.IstBestellartikel` und die
   Artikelnummer der Wurzel nur vergleichen und vor dem Umschalten mit P-FS1/P-FS5 belegen? *Empfehlung:* ja. Kein Umschalten, solange
   eines der drei Felder im Vergleich abweicht und nicht erklärt ist.
4. **Blattfelder ohne Quelle** (Beschichtet, Breite, Hoehe, Tiefe, EKBedarf am Artikel nicht in L10 :1074). Umschalten auf SData sperren,
   bis jedes Feld eine Quelle hat, oder leer akzeptieren? *Empfehlung:* sperren, zumindest wegen `Beschichtet` (Beschichtungslogik).
   P-FS8 und P-FS1 klären es.
5. **Schwelle für das Umschalten IDEAL.** *Empfehlung:* drei aufeinanderfolgende Läufe im Modus Vergleich mit 0 Abweichungen oder nur mit
   Abweichungen, die Gerald einzeln freigibt. Gerald setzt die Einstellung.
6. **Reihenfolge zur Beschichtungslogik-Spec.** *Empfehlung:* diese Spec nach der Beschichtungslogik (v1.99.25) bauen. `Beschichtet` steht
   in den Vergleichsfeldern. Beide Specs teilen P-FS1 = P-I4.
7. **Laufzeitgrenze** Lauf FA-Hierarchie mit SData. *Empfehlung:* Grenze nach P-FS6 festlegen, z. B. höchstens so lang wie der View-Lauf
   plus 2 Minuten. Bei Überschreitung bleibt der Modus View.
8. **`Sync:ArtikelBenutzerfelder` an IDEAL** wirkt erst nach P-R1 (`ArtikelBenutzerfelderWriter.cs:8-9`). P-R1 als eigene Folge-Spec?
   *Empfehlung:* ja, eigene Backlog-Notiz. Hier nur Weg und geplanter Wert.
9. **„Verkaufsstückliste“** (L12 :1121): Welche Ansicht war gemeint? Reicht die Kennzeichnung im Artikel-Modus? *Empfehlung:* ja, gemeint
   ist vermutlich der Artikel-Modus (Ressourcenliste). Sonst bitte die Ansicht nennen.
10. **AKE-Regression.** E1/E2 ändern Admin-Diagnosen, die auch an AKE laufen (`standort: IDEAL` im Frontmatter). QA zusätzlich mit kurzer
    AKE-Regression (Negativfälle TS-146/147)? *Empfehlung:* ja, nur diese zwei Ansichten.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. RealTimeData dtsFaBelegsuche im Dienst erlaubt oder erst P-FS3? → **Gerald 2026-11-16 15:59 „passt“ (Empfehlung Koordination): Gerald fragt DPS zur Freigabe von RealTimeData für den Betrieb. Bis zur Antwort RealTimeData im Dienst nur lesend und nur im Modus „Vergleich“; Umschalten auf SData erst nach DPS-Antwort bzw. P-FS3.**
2. Arbeitshypothese L11 für E4 bis P-FS1? → **Gerald 2026-11-16 15:59 „passt“ (Empfehlung Koordination): ja.**
3. Arbeitsbereich/Beschaffungsartikel/HauptArtnr nur vergleichen, vor Umschalten belegen? → **Gerald 2026-11-16 15:59 „passt“ (Empfehlung Koordination): ja; kein Umschalten bei unerklärter Abweichung.**
4. Blattfelder ohne Quelle: Umschalten sperren oder leer akzeptieren? → **Gerald 2026-11-16 15:59 „passt“ (Empfehlung Koordination): sperren (mindestens wegen Beschichtet), bis P-FS8/P-FS1 je Feld eine Quelle belegen.**
5. Schwelle und Verantwortlicher für das Umschalten IDEAL auf SData? → **Gerald 2026-11-16 15:59 „passt“ (Empfehlung Koordination): drei Vergleichsläufe ohne Abweichung oder jede Abweichung einzeln von Gerald freigegeben; Gerald stellt um.**
6. Reihenfolge nach Beschichtungslogik-Spec? → **Gerald 2026-11-16 15:59 „passt“ (Empfehlung Koordination): ja, nach v1.99.25.**
7. Laufzeitgrenze Lauf FA-Hierarchie mit SData? → **Gerald 2026-11-16 15:59 „passt“ (Empfehlung Koordination): nach P-FS6 festlegen (z. B. View-Lauf + 2 min), bei Überschreitung bleibt View.**
8. P-R1 als eigene Folge-Spec? → **Gerald 2026-11-16 15:59 „passt“ (Empfehlung Koordination): ja, eigene Backlog-Notiz.**
9. „Verkaufsstückliste“ – Kennzeichnung im Artikel-Modus ausreichend? → **Gerald 2026-11-16 15:29: „dann ist das eh korrekt mit der FA-Liste“ – ja, Kennzeichnung reicht.**
10. AKE-Regression für E1/E2 in der QA? → **Gerald 2026-11-16 15:59 „passt“ (Empfehlung Koordination): ja, nur die zwei Ansichten (TS-146/147 Negativfälle).**

## QA-Nachweis (qa-agent, 2026-11-16)

Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, HEAD `5163df17`, geprüfter Bereich
`3f247f11..5163df17` (E1 21b3fb65, E2 28dfdc4c, E3 a3214677, E4 24ad7354, E5 9fa447f1, E6 5163df17). Arbeitsbaum sauber.
Verbindliche Auflage Gerald für diesen Lauf (ersetzt AK 10): RealTimeData im Dienst nur lesend und nur im Modus Vergleich; Umschalten
auf SData bleibt gesperrt.

### 1. Build und Tests
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (Altbestand, u. a. CS8601/CS8321, NU1902/1903).
- `dotnet test`: **IdealAkeWms.Tests** 2920 bestanden, 1 übersprungen, 0 Fehler (2921); **IDEALAKEWMSService.Tests** 652 bestanden,
  0 übersprungen, 0 Fehler. `NachlaufManifestDriftTests` ist Teil davon und grün (AK 13).

### 2. Code-Review (selbst, in diesem Lauf)
Geprüft wurde der gesamte Produktionsdiff, Web: `SageSdataReadClient`, `SageSdataFaController`, `SageTestController`, `Stueckliste.cshtml`,
`ServiceSettingDefinitions`, `SageQuelleSperre`. Dienst: `SdataFaStrukturReader`, `FaStrukturQuelleSwitch`, `SageFaStrukturReader`,
`SdataArtikelReader`, `SageQuelleWeiche`. Dazu der umgebende Code: `SageQuelleWeiche.LeseAsync`, `SageQuelleSchalter.Parse`,
`FaHierarchySyncService` (Hinweis-Protokoll), `SyncWorker` (Gate), `PositionenProjizieren`, `BenutzerfelderLesenAsync`, außerdem Manifest und TS-Text.

**Auflagen belegt (alle Wege):**
- Sperre: `SageQuelleSperre.cs:21` liefert für `FaStruktur` immer einen Grund. `SageQuelleSchalter.cs:37-38` macht daraus für `SData` die
  Quelle `View` mit Warnung. `SageQuelleWeiche.LeseAsync` ruft im Fall View nur `view(ct)`. Der SData-Lambda und damit `HybridAsync`
  (`FaStrukturQuelleSwitch.cs:95`) werden nicht erreicht. Tests: `Weiche_SData_BleibtGesperrt_…`, `Sperre_FaStruktur_BleibtGesetzt_…`.
- RealTimeData nur im Vergleich: Einziger Dienst-Aufrufer von `GetAsync` ist `SdataFaStrukturReader.cs:200` (grep `GetAsync<` über
  `IDEALAKEWMSService`). Er ist nur über `FaKoepfeLesenAsync` erreichbar, hinter `if (!RealTimeDataErlaubt)` (`:171`). Der Schalter steht per
  Default auf false (`:68`). Gesetzt wird er nur von der Weiche (`FaStrukturQuelleSwitch.cs:92`), mit der Regel `quelle == Vergleich` (`:118`).
  `SdataFaStrukturReader` hat keinen anderen Verwender (nur die Registrierung `SdataRegistrierung.cs:88-91`). Nur GET, kein Schreibweg.
- orderBy bei where: Der Wächter sitzt an der gemeinsamen Stelle `SageSdataReadClient.BuildUrl` (`SageSdataReadClient.cs:81`), vor dem HTTP-Aufruf.
  Alle `GetAsync`-Aufrufer übergeben `"BelID"`: `SageSdataFaController.cs:89` und `:136`, `SageTestController.cs:220`,
  `SdataFaStrukturReader.cs:200`. `QueryAllAsync`/`QueryPageAsync` erzwingen orderBy schon vorher. Auf DEV beobachtet: alle URLs mit `&orderBy=BelID`.

**Befunde:**

| Nr | Schwere | Fundstelle | Befund | Behandlung |
|---|---|---|---|---|
| Q1 | niedrig (latent) | `SdataFaStrukturReader.cs:171-175`, `FaStrukturQuelleSwitch.cs:118` | Hebt man später nur die Sperre in `SageQuelleSperre` auf, läuft der SData-Modus mit `RealTimeDataErlaubt = false`. Die FA-Knotenfelder bleiben leer, beendete Wurzeln fehlen, und gemeldet wird das nur als **Info**-Zeile, nicht als Warnung. Heute unerreichbar (Sperre). | Offen, kein Fix in diesem Lauf (Auflage). Beim Entsperren beide Regeln gemeinsam ändern oder den Hinweis zur Warnung machen. In die Folge-Arbeit „Umschalten“ aufnehmen. |
| Q2 | niedrig | `SdataFaStrukturReader.cs:177` (Select mit allen `USER_OS*` an `dtsFaBelegsuche`) | Ein unbekanntes FA-Kopffeld ergibt HTTP 500. Das wirft `SdataLeseException`, und der ganze Vergleichslauf scheitert (Fehlermail je Lauf). Eine Feldprobe wie am Artikel gibt es nicht. | Abweichung der Umsetzung, gemeldet und akzeptiert: Der Fehler ist laut, nicht still, und die Namen sind in L10 belegt. Bei Bedarf Feldprobe nachrüsten. |
| Q3 | niedrig (Text) | `SdataArtikelReader.cs:152`, `:174` | Die Warnungen sagen „Benutzerfeld …“ auch für Stammfelder (Matchcode, Bezeichnung1/2, Artikelgruppe, IstBestellartikel, PlatzID), die der FA-Reader jetzt über dieselbe Methode liest. „In keiner Zeile geliefert“ erscheint auch, wenn ein Feld an allen Blättern leer ist. | Hinweis, kein Fix (kosmetisch, die Meldung bleibt sichtbar). |
| Q4 | Info | `SdataFaStrukturReader.cs:254` (`Dezimal`) | Nicht lesbarer Text bei Breite/Hoehe/Tiefe wird still zu 0 (NULL → 0 ist Spec-Regel wie im View-Weg). | Im Vergleich als Abweichung sichtbar; akzeptiert. |
| Q5 | Info | `SdataFaStrukturReader.cs:178-183` | Über `dtsFaBelegsuche` ergänzte Wurzeln laufen nicht durch `KoepfeFiltern` (eingelastet/aktiv). Ihre Positionen landen nur in der Struktur, nicht in `ProductionOrderPositions`: `PositionenProjizieren` überspringt sie, wie im Kommentar beabsichtigt. | Akzeptiert. |

Bewertung der Abweichungen, die der Auftrag zur Beurteilung nennt:
- AK 10 ist nicht erreichbar, weil die Sperre bleibt. Das entspricht der Auflage und ist korrekt umgesetzt.
- Bezeichnung1/2 kommen nur vom Artikel (`:230`, TODO P-FS5), Artikelgruppe nur als Code (`:232`). Das sind erwartete Vergleichsabweichungen, in
  TS-148.3 benannt.
- Ein `dtsFaBelegsuche`-Durchlauf für E3 und E4: ok (eine Stelle, weniger Last).
- Die gekürzte Beschreibung von `Sync:Quelle:FaStruktur` ist ok. Der Kerninhalt ist vorhanden (Vergleich, SData hybrid, Sperre, Warnhinweis).

Geprüfte Defektklassen:
- kein `@Html.Raw` und kein Skriptwert im View-Diff,
- kein `hidden` zusammen mit `d-*`,
- keine Array-Bindung,
- Validierung serverseitig (`SageTestController` prüft die Eingabe vor jedem Aufruf),
- Bool-Regel an einer Stelle (`SageFaStrukturReader.SageBool`, von beiden Wegen genutzt),
- stille Rückfälle: kein neuer außer Q4.

„Melden statt still“: Die neuen Wege melden die Wurzel-Ergänzung, den Verzicht auf RealTimeData (als Info, siehe Q1), unbekannte Artikelfelder
(Warnung) und Lesefehler (Exception → Vergleich fehlgeschlagen + Mail).

### 3. Nachweisart je AK
| AK | Nachweis |
|---|---|
| 1 | Automatisch: `SageSdataReadClientTests.WhereOhneOrderBy_ArgumentException_OhneRequest` (+ DEV: URLs mit orderBy) |
| 2 | Automatisch: `SageSdataFaControllerTests` (alle URLs `&orderBy=BelID`). Teil „IDEAL-DEV 1069595 kein 500“: **Manual-UAT** (DEV zeigt auf ake_TEST2026) |
| 3, 4 | Automatisch (Fake): `SageTestSdataTests.Stueckliste_BeendeterHauptFa_…`, `…_UnterFaEinesBeendetenHauptFa_…`. Echte IDEAL-Daten: **Manual-UAT** |
| 5 | Automatisch: `…_ArtikelModus_HinweisRessourcenlisteMitText`. Im DEV-Browser mit AKE-Artikel EU253711 belegt (Text sichtbar, nicht nur Farbe) |
| 6 | Automatisch: `FaStrukturSdataTests.LesenAsync_BeendeterHauptFa_…`, `…_HauptFaAuchInBelegsucheNicht_WarnungOhneWurzel` |
| 7 | Automatisch: `Projizieren_UnterFaBekommtFaFelder_BlattArtikelfelder_…`, `SageBool_EineRegel_ViewUndSdata` |
| 8 | Automatisch: `Vergleich_NurArbeitsschritteAnEinemBlatt_…` |
| 9 | **Manual-UAT** (Lauf FA-Hierarchie mit echter View + SData; auf DEV nicht startbar) |
| 10 | Entfällt per Auflage (Sperre bleibt). Hybrid nur simuliert: `Weiche_SData_Hybrid_BeiSimulierterFreigabe_…`, nicht als grün gezählt |
| 11 | **Manual-UAT** (Lauf). Rückfall bei Sperre automatisch: `Weiche_SData_BleibtGesperrt_…` |
| 12 | Automatisch: `LesenAsync_UnbekanntesArtikelfeld_Warnung_LaufLaeuftWeiter` |
| 13 | Automatisch: Build/Tests grün, `NachlaufManifestDriftTests` grün |

### 4. Geänderte bestehende Tests
- `ArtikelSdataTests` (Theory Sperrgrund): `"P-F2"` → `"P-FS3"`. Verhalten bewusst geändert (neuer Sperrgrund, E5/Auflage).
- `SageSdataReadClientTests` (7 Tests): `orderBy` `null` → `"BelID"`. Bewusst geändert durch AK 1 (ohne orderBy wirft der Client jetzt).
- `SageSdataFaControllerTests`: nur eine Zusatz-Assertion (AK 2), keine Fixture-Änderung.
- `FaStrukturSdataTests.Weiche_SData_GesperrtBisPF2_…` → `Weiche_SData_BleibtGesperrt_WirktWieView_MitWarnungUndGrund`: Text des Grundes geändert
  (Auflage). Das Verhalten (SData wirkt wie View) ist unverändert und wird strenger geprüft.

Kategorie „sonstige“: keine.

### 5. Checkliste CLAUDE.md / ADR 0020
- Migration, SQL, FreshInstall, AgentJobs: entfällt (kein Schema). Audit: entfällt (Cache-Tabellen).
- Version 1.99.26 in beiden `AppVersion.cs`. `Views/Help/Changelog.cshtml` und Hilfe ergänzt.
- Brain: Changelog `2026-11-16-v1-99-26-fa-struktur-ideal-sdata`, ADR 0029, fallstricke und feature-map vorhanden.
- `docs/TESTSZENARIEN.md` TS-146..150 vorhanden; die Testindex-Zeile hat QA ergänzt.
- ADR 0020 für `Sync:Quelle:FaStruktur`: Auswahlliste (Combobox auf `/ServiceSettings` belegt), Server prüft (bestehendes `QuellenWerte`).
  Anzeigename, Wirkung, Verwendung, Default, Standort und Warnhinweis vorhanden (DEV-Screenshot). Keine neuen Keys.
- Manifest: beide Einträge der Spec vorhanden, ASCII.

### 6. DEV-Deploy und Testlauf
Standort laut Frontmatter IDEAL, zusätzlich AKE (Freigabe-Antwort 10). Elevated Shell bestätigt.
- `dev-deploy.ps1 -Standort IDEAL -Quelle <worktree>`: Exit 0, „HTTP 200, Fusszeile v1.99.26“, „Fertig: IDEAL v1.99.26 auf http://localhost:8082/“.
- `dev-deploy.ps1 -Standort AKE -Quelle <worktree>`: Exit 0, „HTTP 200, Fusszeile v1.99.26“, „Fertig: AKE v1.99.26 auf http://localhost:8081/“.
- DEV-Lage IDEAL: `SData:BaseUrl = https://sagetest01.ake.at:5493/`, Dataset `ake_TEST2026;1`. Das ist der AKE-Testmandant, nicht IDEAL. Es liefen
  nur GET-Lesungen über die Web-Diagnosen.
- DEV-Lage AKE: `SData:Username` ist leer. Die Seiten melden „Sage-SData nicht konfiguriert“, es gibt keinen Aufruf.
- Keine Einstellung geändert (`Sync:Quelle:FaStruktur` blieb `View`), nichts an Sage geschrieben, keine Mails.

| Schritt | Ausgeführt | Ergebnis | Status |
|---|---|---|---|
| 146.1 / 146.2 | IDEAL-FAs 1069595 / 1059463 | Gibt es im verbundenen Mandanten ake_TEST2026 nicht | NICHT MÖGLICH (DEV zeigt nicht auf IDEAL-Sage) |
| 146.3 | IDEAL-DEV, FA-Struktur live 835 (AKE-Testmandant) | 5 Aufrufe, alle 200 OK, alle mit `&orderBy=BelID` (Belegsuche BelID, StrukturID, Teileverwendung, RückKorrektur, Fertigmeldungen) | OK |
| 146.4 | AKE-Regression: derselbe Code mit AKE-FA 835 (über IDEAL-DEV, Sage AKE-Test) | Struktur 835 mit 6 Sub-FAs, Positionen, Rück-/Fertigmeldungen wie bisher | OK. Auf AKE-DEV selbst NICHT MÖGLICH (SData-Benutzer leer); die Seite rendert mit Konfig-Hinweis |
| 147.1 / 147.2 | IDEAL-FAs 1059463 / 1065143 | Nicht im verbundenen Mandanten | NICHT MÖGLICH. Ersatz: 1059463 eingegeben → PpsFaBelegeInfo 0 Zeilen → Aufruf „Kopf über dtsFaBelegsuche (in PpsFaBelegeInfo nicht vorhanden, beendet?)“ mit `orderBy=BelID` → „FA 1059463 in Sage nicht gefunden.“ (Rückfallweg läuft) |
| 147.2 (Regression) | Stückliste live FA 836 (Sub-FA, AKE) | „FA 836 gehört zur Struktur 835 – angezeigt wird die ganze Struktur“, Wurzel 835 oben, 11 Positionen, Sub-FAs eingehängt, 3 Aufrufe (kein RealTimeData nötig) | OK |
| 147.3 | Artikel-Modus EU253711 (AKE; 80141025 nicht im Mandanten) | Überschrift „Ressourcenliste (Vorlage) EU253711 …“, Hinweis „Ressourcenliste (Vorlage) – nicht die FA-Stückliste. Das ist die Ressourcenliste des Artikels, nicht die Stückliste eines Fertigungsauftrags …“ als Text | OK (mit 80141025 an IDEAL offen) |
| 147.4 | FA 999999999 | „FA 999999999 in Sage nicht gefunden.“, 2 Aufrufe (Suche + Belegsuche-Rückfall), keine weiteren | OK |
| 147.5 | Artikelnummer `80141'025` | „Artikelnummer ohne ', & und #.“, kein Aufruf | OK |
| 148.1 – 148.6 | Lauf FA-Hierarchie im Vergleich | Der Lauf hängt an `Sync:HierarchicalFaEnabled` (`SyncWorker.cs:337`); Einschalten ist auf DEV verboten | NICHT MÖGLICH |
| 149.1 – 149.2 | wie 148 | wie 148 | NICHT MÖGLICH (149.2 per Unit-Test belegt) |
| 150.1 | `/ServiceSettings` IDEAL-DEV | Combobox „View – bisheriger SQL-Weg“; Hinweis „SData ist hier gesperrt bis P-FS3 (DPS-Freigabe RealTimeData im Dienst offen; Rückfragen 4/5 …)“ und Achtung-Text „SData erst nach abweichungsfreiem Vergleich …“ | OK |
| 150.2 – 150.3 | Lauf mit `= SData` / zurück auf `View` | Lauf auf DEV nicht startbar | NICHT MÖGLICH |
| 150.4 | nach späterer Freigabe | nicht Teil von v1.99.26 | entfällt |

Kein FEHLER.

### 7. Publish
Ziel `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\`, Zielordner vorher geleert. Inhalt: `IDEALAKEWMSWeb`,
`IDEALAKEWMSWebService`, `SQL`, `deploy` und `publish-info.txt` (Version 1.99.26, Commit `5163df1751b0429938dad6ad8fd64f40d81aa1b4`, Branch,
Spec, Erstellt 2026-11-16 17:28, Komponenten Web + Dienst). Beide `dotnet publish` mit Exit 0.

**Status-Entscheidung:** Testbereit.
- Build und Tests grün, DEV-Deploy an beiden Standorten grün, keine FEHLER, nur niedrige Befunde.
- Das Epic ist im Rahmen der Auflage vollständig gebaut; das gesperrte Umschalten ist dokumentiert.

### Manuelle Checkliste für den Menschen (offen)
1. Testsystem IDEAL (zeigt auf IDEAL-Sage), TS-146.1/146.2: FA-Struktur live 1069595 und 1059463. Kein 500, alle URLs mit `orderBy` (P-FS7).
2. TS-147.1: Stückliste live 1059463 zeigt die Wurzel oben, 13 Positionen und den Aufruf „Kopf über dtsFaBelegsuche“. TS-147.2 mit 1065143,
   TS-147.3 mit Artikel 80141025.
3. TS-146.4/147 am AKE-Test mit konfiguriertem SData-Benutzer kurz gegenprüfen (auf AKE-DEV ohne Benutzer nicht möglich).
4. TS-148: `Sync:Quelle:FaStruktur = Vergleich` setzen (Manifest `v1.99.26-IDEAL-FaStrukturVergleich`), Lauf FA-Hierarchie abwarten, Protokoll
   „Sage-Quellenvergleich“ prüfen. Stichprobe 1059463/1069595 gegen P-FS5. Erwartete Abweichungen: Artikelgruppe nur Code, Bezeichnung1/2 an
   Unter-FAs, an Blättern Beschichtet/Breite/Hoehe/Tiefe/EKBedarf. Laufzeit gegen P-FS6/Rückfrage 7 notieren.
5. TS-149.1: Hinweis „… über dtsFaBelegsuche ergänzt: 1059463“, keine Warnung „ohne Haupt-FA“ für 1059463.
6. TS-148.5: Bei einem SData-Fehler im Vergleich schreibt der Lauf aus der View, der Lauf „Sage-Quellenvergleich“ endet fehlgeschlagen, es kommt die Fehlermail.
7. TS-150.2/150.3: `= SData` wirkt wie View, mit der Warnung „gesperrt bis P-FS3“. Danach zurück auf `View` bzw. `Vergleich`.
8. Prüfabfragen P-FS1..P-FS8 (Gerald) und DPS-Antwort zu RealTimeData. Vor dem Entsperren Befund Q1 beachten.
9. Danach Merge (Schranke 2).
