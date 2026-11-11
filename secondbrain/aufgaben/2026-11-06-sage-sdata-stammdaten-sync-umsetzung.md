---
typ: notiz
spec: "[[2026-11-06-sage-sdata-stammdaten-sync-spec]]"
status: Testbereit
updated: 2026-11-06
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Sage-Abgleiche auf SData (Epic)

Spec [[2026-11-06-sage-sdata-stammdaten-sync-spec]], Bündel-Worktree, Start v1.72.0, Ziel **v1.73.0** für das ganze Epic.

**Auftrag:** Gerald 2026-11-06 per /dev + /goal, obwohl die Spec noch in `specs/entwurf/` liegt und G1, G2, G4–G8 im
Freigabe-Block unbeantwortet sind. Für diese Fragen gelten die Empfehlungen der Spec bzw. die Vorgaben des Auftrags:
G1/G2 entwickeln gegen den AKE-Test (sagetest01, ake_TEST2026;1), IDEAL bleibt auf View; G4 A9 unverändert; G5 nur Outbox
ohne Senden; G6 Herkunft Sage | WMS; G7 nur Datenbasis; G8 Vollabgleich offene FAs.
Zurückgestellt (G13): E5 Stückliste, A4 AKE-FA flach.

## Vorgehen
Je Etappe ein Commit im Worktree, Reihenfolge laut Etappentabelle. Alle Schalter Default `View` bzw. aus.
Teile, die an Prüfabfragen hängen (P-F1, P-F2, P-R1, P-S1): Reader + Vergleichsmodus, Umschalten gesperrt, TODO mit Verweis.

## Stand
Siehe Etappentabelle in der Spec.
