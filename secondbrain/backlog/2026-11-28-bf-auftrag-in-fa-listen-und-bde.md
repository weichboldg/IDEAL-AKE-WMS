---
type: backlog
typ: feature
status: offen
jira: WMS-10
created: 2026-11-28
standort: IDEAL
quelle: "Mail „WMS Anpassungen“ (Benjamin Andl-Braun, IDEAL) Punkte 3 + 4, weitergeleitet von Gerald im Chat 2026-11-28"
anhaenge:
  - anhaenge/2026-11-28-mail-wms-anpassungen-2.png
---
# BF-Bestellungen in den FA-Listen und im BDE (sehen, abarbeiten, rückmelden)

## Wunsch (Mail)
- Punkt 3: In den BF-Bestellungen sollen die bestellten **Baugruppen die Ressourcenliste ziehen** und auch die **Teile angezeigt/bestellt** werden.
- Punkt 4: Die BF-Bestellungen sollen **auch in den FA-Listen auftauchen**, damit die Mitarbeiter sie **übers BDE sehen, abarbeiten und
  rückmelden** können.

## Einordnung (2026-11-28)
- Punkt 3 ist Umfang des Entwurfs [[2026-11-24-bf-auftrag-baugruppen-aufloesen-und-als-fa-zeigen-spec]] (BG → Einzelteile aus der
  Ressourcenliste, CSV an WiCAM) – **dort** weiterführen, keine zweite Spec.
- Punkt 4 ist die dort ausgeklammerte **Folge-Spec „Stufe D“** (Out-of-Scope, Rückfragen 11/12). Die Mail beantwortet Rückfrage 12 teilweise:
  Ziel sind **FA-Listen + BDE**, Tätigkeit **sehen, abarbeiten, rückmelden**. Diese Notiz ist der Backlog für Stufe D.
- Offen bleibt Rückfrage 11 (interner WMS-FA oder FA in Sage anlegen und über den FA-Abgleich holen). Die BDE-Teileliste schließt BF/WiCAM heute
  aus ([[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]]).

## Rückfragen (Vorschlag)
- „Rückmelden“: nur im WMS (BDE-Status) oder auch an Sage (Fertigmeldung)?
- Welche FA-Listen genau (FA-Liste, FA-Struktur, Leitstand, Kommissionierliste)?
