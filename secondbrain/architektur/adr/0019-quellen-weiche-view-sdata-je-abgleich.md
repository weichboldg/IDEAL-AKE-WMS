---
type: adr
id: 0019
title: Sage-Lesequelle je Abgleich ueber eine Weiche an der Reader-Schnittstelle (View | SData | Vergleich), View bleibt Notfall-Rueckfall
status: accepted
date: 2026-11-06
supersedes: ""
superseded_by: ""
---

## Kontext und Problem

Alle Sage-Abgleiche lasen per Direkt-SQL auf Views/Tabellen der Sage-DB. Mit dem DPS-Lesevertrag `CommonWawiServicesResources`
gibt es einen offiziellen SData-Lesepfad ([[2026-11-06-sage-sdata-stammdaten-sync-spec]]). Viele SData-Feldnamen sind nur teilweise
belegt, IDEAL hat noch keinen Endpunkt, und die Abgleiche laufen produktiv. Eine harte Umstellung haette Abgleichslogik und Quelle
gleichzeitig geaendert und keinen schnellen Rueckweg gelassen.

## Entscheidung

- Je Abgleich EINE Reader-Schnittstelle (bestehend oder neu herausgezogen: `ISageArtikelReader`, `ISageFaStrukturReader`) mit zwei
  Implementierungen: der bisherige SQL-Reader **unveraendert** und ein `Sdata…Reader`.
- Eine Weiche `…QuelleSwitch : SageQuelleWeiche` (Muster [[0013-bom-bridge-repository-schnittstelle-statt-cache-kopie]]) ist unter der
  Schnittstelle registriert (Factory in `SdataRegistrierung`), liest bei jedem Aufruf `Sync:Quelle:<Bereich>` (ServiceSettings, DB-first)
  und delegiert. Die Abgleich-Services bleiben unveraendert (Upsert, Reconciliation, Count-Keys).
- Werte: `View` (Default, unbekannt = View + Warnung), `SData`, `Vergleich` (liest beide, gibt View-Zeilen zurueck, Diff je Schluessel in
  Lauf `SageQuellenvergleich`; SData-Ausfall im Vergleich = Fehlerlauf + Mail, View-Abgleich laeuft weiter).
- `SdataGesperrtBis = "P-xx"`: solange eine Pruefabfrage offen ist, wirkt `SData` wie `View` + Warnung.
- SData-Reader werfen bei jedem Fehler und pruefen Pflichtfelder (nie „leer“ statt Fehler → kein Deaktivieren/Nullsetzen/Doppelbuchen).
- Neue, nur aus SData gelieferte Felder sind nullable; `null` wird nie geschrieben; Artikel-Zusatzfelder liegen im Satelliten
  `ArticleSageDetails` ([[0009-app-status-in-satelliten-tabellen-neben-sage-master]]), den nur der SData-Weg schreibt.

## Konsequenzen

**Positiv**
- Umschalten und Rueckfall ohne Deploy/Migration; Vergleichsmodus macht die Abnahme risikolos.
- Abgleichslogik existiert einmal; SQL-Weg bleibt bit-gleich (Notfall).

**Negativ / Risiken**
- Weiche ist scoped (Zustand `GenutzteQuelle`); `Program.cs` muss den Registrierungs-Helper rufen, sonst DI-Luecke (DI-Test Pflicht).
- Teilen sich zwei Abgleiche eine Weiche (Werkbank + Arbeitsgang), liest SData zweimal je Zyklus.
- Modus `SData` mailt die Weiche nicht (Mail kommt aus `RunResilientAsync` bzw. dem Abgleich), sonst doppelte Mails.
