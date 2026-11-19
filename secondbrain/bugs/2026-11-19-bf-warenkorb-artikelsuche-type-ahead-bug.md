---
type: bug
title: BF-Warenkorb – Artikelsuche ohne Vorschläge beim Tippen (Type-ahead)
status: offen
severity: niedrig
created: 2026-11-19
standort: IDEAL
affected_code:
  - "IdealAkeWms/Views/BfAuftraege/Details.cshtml:63-75 (Suchformular, nur Server-Suche, kein JS)"
  - "IdealAkeWms/Controllers/Api/BfAuftraegeApiController.cs:19-26 (GET api/bf-auftraege/artikel – vorhanden, von keiner View/keinem JS aufgerufen)"
  - "IdealAkeWms/Data/Repositories/BfAuftragRepository.cs:43-68 (SucheArtikelAsync + Lagerteile(), der eine BF-Filter)"
source_backlog: "[[2026-11-19-bugfix-bf-warenkorb-artikelsuche-type-ahead]]"
spec: "[[2026-11-19-bf-warenkorb-artikelsuche-type-ahead-bug-spec]]"
---

## Symptom
Im BF-Warenkorb (Blechfertigung, IDEAL) erscheinen beim Tippen in „Artikelnummer, Matchcode oder Bezeichnung“ keine Vorschläge.
Treffer kommen erst nach „Suchen“ bzw. Enter (Seitenneuladen). Andere Masken (Lagerbestellung, Einbuchung, Beschichtung) schlagen beim
Tippen vor. Meldung Gerald 2026-11-19 20:10.

## Reproduktion
1. IDEAL-DEV, `BlechfertigungAktiv` = true, Benutzer mit Lager-Bestellrecht oder `schachtler`.
2. Menü *Blechfertigung → BF-Warenkorb*.
3. Im Suchfeld drei Zeichen einer bekannten BG/FT-Artikelnummer tippen und warten → keine Vorschlagsliste.

## Root Cause
Kein Fehler im Filter, sondern fehlende Verdrahtung: Die Ursprungs-Spec
[[2026-11-13-blechfertigung-bf-auftraege-wicam-csv-spec]] (Abschnitt „Web: Controller, Views, Menü“, Z. 279-282) sah den Endpunkt
`GET api/bf-auftraege/artikel` und das Bootstrap-Muster der Lagerbestellung vor. Gebaut wurde der Endpunkt
(`BfAuftraegeApiController.cs:19-26`), die View aber bewusst als reine Server-Suche („kein eigenes JS“, `Details.cshtml:63`). Eine Suche nach
`api/bf-auftraege` über `Views/` und `wwwroot/` findet keinen Aufrufer (nur die Route selbst, `BfAuftraegeApiController.cs:10`).

## Fix / Verweis
[[2026-11-19-bf-warenkorb-artikelsuche-type-ahead-bug-spec]]: Vorschläge über den vorhandenen Endpunkt (derselbe Filter
`BfAuftragRepository.Lagerteile()` wie die Server-Suche), natives `<datalist>` am bestehenden Suchfeld; Scanner, Enter und Server-Suche ohne
JS bleiben.
