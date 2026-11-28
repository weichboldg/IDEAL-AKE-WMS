---
type: backlog
typ: feature
status: in-spec
jira: WMS-10
created: 2026-11-28
standort: IDEAL
quelle: "Mail „WMS Anpassungen“ (Benjamin Andl-Braun, IDEAL) Punkte 5 + 6 + Ergänzung Gerald im Chat 2026-11-28 („Tabellen, welche den FA betreffen … IstLagerartikel, Verwendungsart, Teileart ins Zahnrad“)"
anhaenge:
  - anhaenge/2026-11-28-mail-wms-anpassungen-2.png
---
# Zahnrad aller FA-Tabellen: Ist Lagerartikel, Verwendungsart, Teileart; FA-Struktur mit erweitertem Zahnrad

## Wunsch
- Mail Punkt 5: Im Zahnrad braucht es die Felder **IstLagerartikel, Verwendungsart, Teileart**.
- Gerald: **alle Tabellen, die den FA betreffen**, haben diese Felder anscheinend → dort ins Zahnrad bringen.
- Mail Punkt 6: Die **FA-Struktur** braucht den **erweiterten Zahnrad-Umfang**, „wie er bereits in vielen Bereichen angeboten wird“.

## Einordnung (2026-11-28)
- Der Entwurf [[2026-11-26-bde-zahnrad-artikelfelder-spec]] deckt **nur die BDE-Teileliste** (Einzeln, Summiert, FA-Sicht) und nimmt die
  FA-Struktur ausdrücklich aus. Diese Notiz ist die **Erweiterung auf die übrigen FA-Tabellen**. Dieselben Quellen wiederverwenden
  (Verwendungsart aus FA-Position/Sage-View, Teileart `USER_OSArtikeltyp`, Ist Lagerartikel `USER_OSIstLagerartikel`); Rückfrage 1 dort
  (Quelle Verwendungsart) gilt hier mit.
- „Anscheinend vorhanden“ ist zu belegen: je Tabelle prüfen, ob die drei Werte schon im Modell stehen. Die FA-Struktur gleicht 13
  FA-Positionsfelder über `SageUserfelder` FaStruktur ab, Verwendungsart nicht (Befund in [[2026-11-26-bde-zahnrad-artikelfelder]]).

## Rückfragen (Vorschlag)
- Welche Tabellen genau: FA-Liste, FA-Struktur, Stückliste/Kommissionierauftrag, Kommissionierliste, Leitstand, Vormontage?
- Punkt 6 „erweiterter Umfang“: welche Spalten sind gemeint – alle Stücklisten-Eigenschaftsfelder der FA-Position (wie in der Stückliste)?
- Mit [[2026-11-26-bde-zahnrad-artikelfelder-spec]] zusammenlegen (eine Spec, Etappen) oder getrennt nach der BDE-Spec bauen?
