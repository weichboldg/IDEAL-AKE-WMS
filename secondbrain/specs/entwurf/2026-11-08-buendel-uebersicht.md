---
type: uebersicht
title: Bündel 2026-11-08 – Teil A Nachbesserungen, Teil B neue Module, Teil C Einkauf und Kommissionierung
status: Entwurf
created: 2026-11-08
updated: 2026-11-08
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
specs:
  - "[[2026-11-08-bereinigungen-nach-buendel-2026-11-07-spec]]"
  - "[[2026-11-08-artikeldetails-lieferanten-anzeigen-spec]]"
  - "[[2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas-spec]]"
  - "[[2026-11-08-oseon-teileverfolgung-doppelter-klickhandler-spec]]"
  - "[[2026-11-08-lagerbuchungen-fa-feld-und-verlinkung-spec]]"
  - "[[2026-11-08-wareneingang-aus-sage-bestellungen-spec]]"
  - "[[2026-11-08-versandmasken-geraete-fas-spec]]"
  - "[[2026-11-08-qualitaetsmasken-fragebogen-foto-enaio-spec]]"
  - "[[2026-11-08-einkauf-bestellvorschlaege-spec]]"
  - "[[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]]"
vorgaenger: "[[2026-11-07-buendel-uebersicht]]"
---

# Bündel 2026-11-08

Zehn Entwurfs-Specs aus dem Backlog vom 07.11. Schranke 1 gilt je Spec. Codebasis beim Spezifizieren: Bündel-Worktree
`.claude/worktrees/2026-08-07-ideal-teile-1-5`, Stand **v1.78.0**, letzte SQL-Datei `SQL/107`, letztes Testszenario TS-110.

> [!warning] Basis v1.78.0 hängt an der EAN-Entscheidung
> v1.78.0 ist der EAN-Commit `8534da23`, umgesetzt trotz zurückgestellter Freigabe (siehe [[2026-11-07-buendel-2026-11-07-goal-lauf]]).
> Nimmt Gerald ihn heraus, rücken alle Versionen unten um eine Stelle nach vorn (1.78.0 … 1.87.0); der Dev-Lauf zieht die Specs nach.

## Läufe, Reihenfolge, Nummern

| Lauf | # | Spec | Typ | Version | SQL (reserviert → genutzt) | TS | Standort |
|---|---|---|---|---|---|---|---|
| **A** | A1 | [[2026-11-08-bereinigungen-nach-buendel-2026-11-07-spec]] | Nachbesserung | 1.79.0 | 108 → nur bei Frage A1-3 = ja | TS-111 | beide |
| A | A2 | [[2026-11-08-artikeldetails-lieferanten-anzeigen-spec]] | Feature klein | 1.80.0 | – | TS-112 | beide |
| A | A3 | [[2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas-spec]] ([[2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas-bug]]) | Bug | 1.81.0 | – | TS-113 | beide |
| A | A4 | [[2026-11-08-oseon-teileverfolgung-doppelter-klickhandler-spec]] ([[2026-11-08-oseon-teileverfolgung-doppelter-klickhandler-bug]]) | Bug, erst nachstellen | 1.82.0 (nur bei Codeänderung) | – | TS-114 | beide (gemeinsames JS) |
| A | A5 | [[2026-11-08-lagerbuchungen-fa-feld-und-verlinkung-spec]] | **Epic** E1–E4 | 1.83.0 | 109–111 → 109, 110 | TS-115 | beide |
| **B** | B1 | [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]] | **Epic** E1–E4 (+E5 blockiert) | 1.84.0 | 112–116 → 112, 113 (114 nur E5) | TS-116 | AKE |
| B | B2 | [[2026-11-08-versandmasken-geraete-fas-spec]] | **Epic** E1–E7 (+E8 blockiert) | 1.85.0 | 117–121 → 117–120 (121 nur E8) | TS-117 | beide |
| B | B3 | [[2026-11-08-qualitaetsmasken-fragebogen-foto-enaio-spec]] | **Epic** E1–E6 | 1.86.0 | 122–127 → 122–124 | TS-118 | beide |
| **C** | C1 | [[2026-11-08-einkauf-bestellvorschlaege-spec]] | **Epic** E1–E6 | 1.87.0 | 128–132 → 128–130 | TS-119 | AKE |
| C | C2 | [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]] | **Epic** E1–E6 (+E7 blockiert) | 1.88.0 | 133–139 → 133, 134 (135 bedingt) | TS-120 | beide |

- **SQL:** Die Blöcke bleiben fest, auch wo Nummern frei bleiben. So hängt keine Spec an der Nummer einer anderen, und die Läufe B und C
  können getrennt starten. Nicht genutzte Nummern hinterlassen Lücken; ADR 0004 verlangt nur aufsteigend und idempotent. Will Gerald keine
  Lücken: vor dem Lauf B/C neu nummerieren (Frage Ü2).
- **Versionen:** ein Epic = eine Version. Ändert sich die Reihenfolge, wandern die Versionen mit; SQL-Blöcke bleiben an der Spec.
- **Blockierte Etappen** (schreiben an Sage, nur hinter Schalter Default aus) baut kein /goal-Lauf. Sie werden als Blocker vermerkt:
  B1-E5, B2-E8, C2-E7 und C1-E5 bis zur Freigabe von Prüfschritt P5.

### Abhängigkeiten

```
A5 Lagerbuchungen FA-Feld ──────────────► C2 Kommissionierung (E3 setzt ProductionOrderId je Umbuchung)
B1 Wareneingang E1 (Cache offene Bestellungen) ──► C1 Einkauf E2 (liest denselben Cache, kein zweiter Reader)
A2 Artikeldetails/Lieferanten ─ ─ ─ ─ ─ ─► C1 Einkauf (Lieferantenanzeige, lose)
B3 QM E1–E2 (Prüfmaske + Aufrufvertrag) ─► spätere Andock-Schritte in B1/B2 (dort nur Link auf /QualityInspections/Create)
B2 Versand: Cache offene Verkaufspositionen ─ ─► C2 Kommissionierung (wiederverwendbar, lose)
OSEON-Hotfix main (Z1–Z3 Picking-Views) ─► Konfliktstelle beim Sync für C2 (Werkbank-Spalte wandert in die Auftragsliste)
```

### Lauf-Vorschlag

- **Lauf A (ein /goal-Lauf):** A1 → A2 → A3 → A4 → A5. Keine Spec teilt Code mit einer anderen; gemeinsam sind nur die Abschlussdateien
  (AppVersion, Changelog, TESTSZENARIEN, Index), daher nacheinander. A5 zuletzt, weil es die einzige Migration in Lauf A ist.
- **Lauf B:** B1 → B3 → B2. QM vor Versand, damit Versand gleich auf die fertige Prüfmaske verlinken kann; B1 zuerst, weil C1 seinen
  Cache braucht. Bei Bedarf B als drei Einzelläufe.
- **Lauf C:** C2 → C1. C2 hängt nur an Lauf A; C1 braucht B1-E1. Wenn B noch nicht gelaufen ist: nur C2.
- **Worktree:** siehe Frage Ü1.

## Vorbedingungen (Prüfabfragen, vor Schranke 1)

Was nur in Sage oder den Produktiv-DBs steht, ist in den Specs als Prüfabfrage formuliert (lesend). Ohne Antwort blockiert:

| Spec | Prüfabfragen | blockiert |
|---|---|---|
| A1 | P-1 … P-4 (AppSettings-Altwerte `1`/`0`/Leerzeichen, Lagerplätze „Übergabe an Sage“) | Normalisierung, F6 |
| A2 | P1 Kontoformat `LetzterLieferant` | Link „letzter Lieferant“ |
| A5 | P1 Formen der FA-Texte, P2 Inhalt `Note` – je Standort | E2 Altdaten |
| B1 | P-W1 … P-W4 Feldschema `EinkaufPositionenInfo`/`EinkaufBelegekopfInfo`, P-W5, P-W7 | E1 (Feldnamen nicht belegt; falscher Name im `select` = HTTP 500) |
| B2 | P-V1 … P-V3 (Verkauf-Ressourcen, AB-Belegarten, offene Positionen), P-V4 … P-V6 | E1, E2, E6 |
| C1 | P1/P2 Einheiten, P4, **P5 schreibt** in den Dispo-Stapel (nur mit Geralds Freigabe, Testposition wieder löschen) | E4, E5 |
| C2 | P1 … P5 (Kommissionierziele IDEAL, AKE-Gruppen, **P3 Umfang Mengenfehler B1**, LeitstandAktiv, Altbestand) | E1 Regeln, E5 |

## Offene Fragen (alle, je mit Empfehlung)

Nummern wie in der jeweiligen Spec; dort steht die Begründung.

### Übergreifend
- **Ü1 Worktree:** Lauf A im bestehenden Bündel-Worktree (braucht v1.78.0). **Empfehlung:** danach Schranke 2 und EIN Merge des Bündels
  (v1.31–v1.83 liegen ungemergt), erst dann B und C je in einem frischen Worktree von `main` – der Zweig ist sonst kaum noch abnehmbar.
- **Ü2 SQL-Lücken:** feste Blöcke mit Lücken behalten. **Empfehlung:** ja.
- **Ü3 EAN 1.78.0:** behalten oder herausnehmen (Lauf-Notiz 2026-11-07). **Empfehlung:** vor Lauf A entscheiden; die Versionen hier folgen.

### A1 Bereinigungen
1. Trim-Widerspruch B3: Leser nicht ändern, Altwerte einmal durch Speichern je Standort normalisieren. **Achtung:** ein Altwert `1` wirkt bei
   Schaltern, die nur auf `true` reagieren, heute als **aus** – je Schalter vorher entscheiden. **Empfehlung:** so.
2. Falsche Groß/Klein-Schreibung bleibt „ungültig“ mit Hinweistext. **Empfehlung:** so lassen.
3. `OseonRueckmeldungAktiv`/`SageRueckmeldungAktiv` entfernen + DB-Zeilen per SQL/108 löschen. **Empfehlung:** ja (Rückmelde-Epic dann nachziehen).
4. „Übergabe an Sage“ leer = Bestand vor Deploy, gewollt; künftig gedämpft „nicht nötig“ zeigen, vorher P-3. **Empfehlung:** ja.
5. SData Q2 „WMS-eigen“ nicht angleichen. **Empfehlung:** ja.

### A2 Artikeldetails
1. Artikelinfo `/Articles/Info` erweitern und von Liste/Bearbeiten verlinken (keine neue Detailseite). **Empfehlung:** ja.
2. Artikel je Lieferant auf der Kreditorseite: nein, gehört zu C1. 3. Wiederbeschaffungszeit ja, Einzelpreis nein. 4. Keine Sage-Spalten in der Artikelliste.

### A3 Fehlteile-Karte
1. Karte zählt nur den aktiven Bestelltyp (kehrt frühere Entscheidung „über Typen“ um). **Empfehlung:** ja.
2. Häkchen „Nur meine Werkbänke“ hier mit verstecktem `false`-Feld reparieren. **Empfehlung:** ja.

### A4 OSEON-Klickhandler
1. „Reproduziert“ = sichtbare Fehlfunktion; zwei Listener allein → keine Änderung. **Empfehlung:** ja.
2. `LocationTransfer.cshtml:128` (gleiches Muster) mit beheben. **Empfehlung:** ja.

### A5 Lagerbuchungen FA-Feld
Befund: Der FA steht nicht in der Bemerkung, sondern schon im Textfeld `StockMovements.ProductionOrder`; neu ist der Fremdschlüssel
`ProductionOrderId`.
1. Altdaten aus dem Textfeld übernehmen (statt aus der Notiz). 2. Bestandsübersicht: Artikel-QR übernimmt nie mehr den FA (nimmt einen Teil
von v1.76.0 zurück, sichtbar für Anwender). 3. Pflicht = FA eindeutig gefunden; erledigt/storniert nur Warnung. 4. IDEAL Vorschläge nur
Haupt-FAs, exakte Sub-FA trotzdem verknüpfen. 5. „Lagerplatz ausbuchen“ nur verknüpfen, ohne Pflicht. 6. Ergebnis SQL/110 als Datei in der
Deploy-Aufgabe. 7. „Buchen für FA“ / Auswertung je FA als eigener Backlog-Punkt. **Empfehlung:** überall ja.

### B1 Wareneingang aus Sage-Bestellungen
1. **Doppelbestand:** WMS-Einbuchung meldet den Zugang an Sage, schließt die Bestellposition aber nicht; bucht der Einkauf in Sage den
   WE-Beleg mit Lagerbuchung, steigt der Bestand doppelt. **Empfehlung:** Einkauf schließt die Position in Sage ohne Lagerbuchung, bis E5
   das ablöst; geht das nicht, nur Plätze mit `SageBuchungErlaubt = 0`. **Muss vor E3 entschieden sein.**
2. Rest offen = Sage offen − im WMS erfasst, beide Spalten. 3. Belegarten EBB, EBZ (nicht EBD). 4. Überlieferung mit Pflicht-Häkchen,
Liefersperre nur Warnung. 5. Ziel-Lagerplatz aus Sage vorbelegt, änderbar. 6. Belegnummer scannen/eingeben + Auswahl über Lieferant.
7. IDEAL aus bis SData-Endpunkt. 8. Kein Etikettendruck. 9. E1–E4 unabhängig von DPS-MDE. 10. Charge/Serie nur Hinweis.
11. Cache je Dienstzyklus mit „Stand“. 12. Keine neue Rolle. 13. E5 als Folge-Spec. **Empfehlung:** jeweils wie genannt.

### B2 Versandmasken
1. P-V1 … P-V3 vor Schranke 1. 2. Belegarten VVA, VVN, VVG (ohne VV2). 3. Offen = nicht erledigt und geliefert < bestellt. 4. Position auf
mehrere Ladungen mit Teilmenge. 5. Mehrere Kunden je Ladung erlaubt, Warnung ohne „Sammelgut“. 6. Liefertermin-Arten Fix/bis/KW/offen.
7. Status geplant → vergeben → verladen. 8. Verkehrskürzel als AppSetting-Liste. 9. Frachtkosten netto EUR. 10. Kosten nur für
`versand_schreiben`/Admin. 11. Zwei neue Rollen `versand`, `versand_schreiben`. 12. Kunden-Bestellnummer erst Eingabe. 13. FA-Bezug nur
IDEAL (AKE erst nach P-V6/DPS). 14. Kein Rückschreiben an Sage in diesem Epic. 15. Lieferadresse als Momentaufnahme, änderbar.
**Empfehlung:** jeweils wie genannt. Befund: AB-Nummer am FA gibt es nur an IDEAL (`FaHierarchyOrderInfos`).

### B3 Qualitätsmasken
1. Fotos/PDF bis zur Übergabe in der DB. 2. 1920 px / JPEG 0,75, max. 10 MB und 20 Fotos. 3. Prüfdaten dauerhaft, Dateiinhalte 90 Tage
nach Übergabe leeren. 4. enaio-Admin fragen, was nach dem Import mit den Dateien passiert. 5. Freigabe-Pfad je Standort, Dienstkonto mit
Änderungsrecht; IDEAL aus bis beantwortet. 6. enaio-Admin bestätigt das CSV-Format. 7. Zwei Rollen `quality_inspection`,
`quality_inspection_templates`. 8. Endprüfung IDEAL nur am HauptFA. 9. Seriennummer optional als Eingabe. 10. Eine Vorlage je Prüfart.
11. n.i.O. nur dokumentieren. **Empfehlung:** jeweils wie genannt.

### C1 Einkauf / Bestellvorschläge
1. Offene Lagerbestellungen und Bedarfsmeldungen einrechnen. 2. Default „Feste Bestellmenge“. 3. `USER_Mindestbestand` nur anzeigen.
4. Bestand ohne Kommissionierwagen. 5. Ein fester WMS-Dispo-Stapel. 6. Übergabe synchron mit Protokolltabelle. 7. Direktbestellung als
Folge-Spec. 8. Eine Rolle `einkauf`. 9. Nur auf Mindestbestellmenge runden. 10. An AKE `Sync:Quelle:Artikel = SData` und
`Sync:ArtikelLieferantEnabled = true` als Deploy-Voraussetzung. **Empfehlung:** jeweils wie genannt.

### C2 Kommissionierung
1. Bereichsregel: Artikel → Kommissionierziel → Lagerort → Artikelgruppe → Standard. 2. AKE alle Positionen, IDEAL nur `Kommissionieren`.
3. Soll = hochgerechnete Menge (vorher P3). 4. Freigabe zurück: ohne Fortschritt stornieren, sonst warnen. 5. Folgeauftrag bei „teilweise“
automatisch. 6. Priorität/Kommissionierer am Auftrag, Leitstand schreibt durch. 7. Ohne Leitstand erzeugt das Öffnen die Aufträge.
8. Vorkommissionier-Bereiche über `PrePickingDaysResolver`. 9. Bereiche flach. 10. Altbestand nur FAs mit Fortschritt per Skript.
11. `PickingItems` nur lesend behalten. 12. Sage-Buchung (E7) ins Rückmelde-Epic. 13. Kein globaler Schalter.
**Empfehlung:** jeweils wie genannt.

## Zusatzfunde (nicht in diesem Bündel gebaut)

| Fund | Quelle | Vorschlag |
|---|---|---|
| **Sicherheit:** Kommissionierfotos unter `wwwroot/Fotos` ohne Anmeldung abrufbar (Login-Middleware lässt Pfade mit Punkt durch, `Program.cs:429`; `UseStaticFiles` `:447`); `PhotoController.Delete` prüft die FA-Zugehörigkeit nicht | B3 | **dringend**, eigener Fix vor Lauf B: [[2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar]] |
| Meldebestand-Mail: `StockCheckService` zählt Umbuchungen als Zugang und lässt Artikel ohne Bewegung weg (`StockCheckService.cs:41-42, 58`) → Bestand zu hoch, Mail bleibt aus | C1 | [[2026-11-08-meldebestand-mail-bestand-falsch-berechnet]] |
| AKE: Kommissionierung bucht Stücklistenmenge je Stück, Stückliste zeigt Menge × Auftragsmenge (`PickingTransferService.cs:296` vs. `BomQuantityResolver.cs:16`) | C2 (B1) | Umfang mit P3 messen; ist er > 0, vor C2 als eigener Bug |
| `Article.MinimumStock` heißt `Article.ReorderLevel` (Backlog, feature-map Z. 25 falsch) | C1 | feature-map beim Bündel-Abschluss korrigieren |
| EF-Länge `StockMovement.ProductionOrder` 100, DB 500 (`ApplicationDbContext.cs:378`) | A5 | Migration ohne `AlterColumn`; Angleichen später |
| Altcode Kommissionierung setzt keine `Modified*`-Felder; Löschregel PickingItems→FA Modell `Restrict`, FreshInstall `CASCADE` | C2 | in C2 E3 bzw. eigener Punkt |
| `GetByOrderNumberAsync` an IDEAL ohne HauptFA-Filter (`ProductionOrderRepository.cs:193-196`) | B3 | B3 nutzt eigene Suche über `IsHauptFa` |
| html5-qrcode vom unpkg-CDN statt `~/lib` (`LocationTransfer.cshtml:124`, `Articles/Info.cshtml:278`) | A4 | in A4 mitnehmen oder Backlog |
| Verwaiste AppSetting-Zeile `StammdatenADGruppe` (sollte `SQL/32` löschen) | A1 | ins Lösch-Skript SQL/108 |
