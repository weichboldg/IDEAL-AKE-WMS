Buendel-Lauf im /goal-Modus: <SPEC_PATHS> [Worktree: <WORKTREE>]

Nur interaktiv (keine Turn-Grenze, der Mensch setzt vorher /goal). Der Watcher nutzt weiter dev.md und
epic-stage.md; fuer sie aendert sich nichts.

Eingabe: eine Liste von Spec-Pfaden (Epic und Nicht-Epic gemischt), optional ein vorhandener
Buendel-Worktree/Branch. Gibt es eine Buendel-Uebersicht (type: uebersicht), gilt deren Reihenfolge,
sonst die Reihenfolge des Auftrags.

Zielbedingung fuer /goal (der Mensch setzt sie vor dem Start, Pfade einsetzen):
  "Alle Specs <liste> sind Testbereit (qa-agent mit gruenem Beweis) ODER tragen einen dokumentierten
   Blocker (unbeantwortete Freigabefrage, roter Test nach 2 Reparaturversuchen, fehlende Pruefabfrage);
   Zwischenstand in der Lauf-Notiz aktuell; kein Merge, kein Push."

Was gilt (wie dev.md): Rumpf der Spec ist der Auftrag, ein FREIGABE-NACHTRAG geht ihm vor, Antwort- und
Pruefabschnitte sind Begruendung. Liefere, was die Spec verlangt - Zusatzfunde als Befund in die Spec.

0. SCHREIBZIELE (CLAUDE.md "Das Brain wird NICHT verzweigt"): Anwendungscode, SQL/, docs/, Tests,
   Versions-Bump -> IN DEN WORKTREE. Alle secondbrain/-Aenderungen (Spec-Status, worktree/branch,
   Etappen-Tabelle, QA-Nachweis, Deploy-Abschnitt, Aufgaben-/Lauf-Notiz, codebase-Karte, Testindex,
   changelog) -> IMMER in den HAUPTCHECKOUT C:\Git\IDEAL-AKE-WMS\secondbrain\, NIE in
   <worktree>\secondbrain\. Code und Brain getrennt committen, im Brain nur explizite Pfade stagen.

1. Vorpruefung je Spec, BEVOR irgendetwas gebaut wird:
   - status: Freigegeben. Ausnahme nur, wenn der Auftrag die Spec ausdruecklich trotz anderem Status
     nennt - dann in der Aufgaben-Notiz vermerken, dass der Auftrag die Freigabe ersetzt.
   - worktree/branch leer ODER gleich dem Buendel-Worktree (dann Wiederaufnahme).
   - Freigabe-Antworten vollstaendig (keine Zeile nur mit Pfeil), Varianten-Spec mit
     freigabe_entscheidung, keine offenen BLOCKER in 'Kritische Pruefung', kein ungeloester
     Widerspruch Rumpf/Antworten.
   Faellt eine Pruefung durch: Spec ueberspringen, Grund in die Spec schreiben (Status nicht aendern),
   in der Lauf-Notiz als Blocker fuehren. Specs, die laut Uebersicht von ihr abhaengen, ebenfalls.

2. Lauf-Notiz anlegen bzw. fortschreiben: secondbrain/aufgaben/YYYY-MM-DD-<buendel-slug>-goal-lauf.md
   mit einer Tabelle Spec | Typ (Epic/Spec) | SQL-Block | Version | Status | Commits | Blocker.
   Nach jeder Etappe bzw. Spec aktualisieren - sie ist der Wiedereinstieg nach einem Abbruch.

3. Worktree: vorhandenen Buendel-Worktree weiterverwenden, sonst EINEN anlegen:
   powershell -ExecutionPolicy Bypass -File scripts/new-worktree.ps1 -Slug <buendel-slug>
   In jede bearbeitete Spec worktree/branch + status: InUmsetzung + Aufgaben-Notiz wie dev.md Schritt 1.

4. Specs in der festgelegten Reihenfolge nacheinander abarbeiten:
   - SQL-Nummern fortlaufend ueber alle Specs: vor jeder Spec die naechste freie Nummer unter SQL/ im
     Worktree bestimmen (dreistellig ab 100, ADR 0004). Weicht sie von einem reservierten Block ab,
     die tatsaechliche Nummer in Spec und Lauf-Notiz eintragen.
   - Version: je Spec die naechste Version (ein Epic = eine Version).
   - Epic (epic: true): ALLE offenen Etappen nacheinander, Ablauf je Etappe wie epic-stage.md Schritt 2
     (Commit "epic <slug>: Etappe N - <titel>", Etappen-Tabelle auf erledigt + Hash). Zurueckgestellte
     Etappen bleiben zurueckgestellt.
   - Nicht-Epic: Ablauf wie dev.md Schritt 2-5 (Plan, Umsetzung, "wip: <slug> feature-complete").
   - frontend-design-Pflicht bei Views/CSS/Oberflaechen-JS wie in dev.md.
   - Haengt ein Teil an einer offenen Pruefabfrage (Sage/Fremdsystem): bauen, was ohne sie geht,
     Umschalten sperren bzw. TODO mit Verweis; ist ohne sie nichts Pruefbares lieferbar -> Blocker.

5. Je Spec am Ende (Epic: nach der letzten Etappe, nicht je Etappe): dotnet build + dotnet test gruen,
   docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md, dann qa-agent aufrufen und sein
   Ergebnis abwarten (Build/Test, Review, Testszenarien auf DEV, Deploy-Abschnitt). Nur bei Erfolg setzt
   er Testbereit; danach manuelle Test-Checkliste ans Spec-Ende, Brain-Commit "brain: <slug> testbereit".
   Kommt sein Ergebnis nicht zurueck: QA nicht erfolgt, nicht Testbereit, als Blocker fuehren.

6. Rote Build/Tests oder QA-Befunde: hoechstens 2 Reparaturversuche je Spec (superpowers:systematic-debugging).
   Danach: Stand als "wip: <slug>" committen, Blocker mit erstem Fehler in Spec + Lauf-Notiz, Status
   InUmsetzung lassen, mit der naechsten NICHT abhaengigen Spec weitermachen.

7. Ende: Lauf-Notiz abschliessen und melden - je Spec Testbereit (Commits, Version, SQL-Nummern) oder
   Blocker (Grund, was der Mensch tun muss).

Verboten: Merge nach main, git push, Worktree loeschen, status Gemerged setzen, Spec nach freigegeben/
verschieben, Freigabe-Antworten selbst ausfuellen (Schranken 1 und 2 = Mensch).
