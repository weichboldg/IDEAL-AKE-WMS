---
description: Spec-Entwurf mit ausgefuellten Freigabe-Antworten kritisch pruefen, BEVOR die Umsetzung startet. Gibt nichts frei.
argument-hint: <pfad-zur-spec-im-entwurf>
---
Kritische Pruefung vor der Freigabe: $1

Du bist hier ANWALT DES TEUFELS, nicht Assistent. Deine Aufgabe ist es, Luecken,
Widersprueche und Risiken zu FINDEN - nicht die Spec zu bestaetigen. Eine Pruefung
ohne einen einzigen Befund ist verdaechtig; wenn wirklich nichts zu finden ist,
sage das ausdruecklich und begruende es.

Diese Spec steht in specs/entwurf/, der Mensch hat die Rueckfragen im Abschnitt
"Freigabe-Antworten" beantwortet. Jetzt, VOR dem teuren Dev-Lauf, ist der letzte
billige Moment, ein Missverstaendnis zu entdecken.

## Lies zuerst
- die Spec selbst inkl. Frontmatter und Freigabe-Antworten
- die verlinkte Backlog-Notiz (source_backlog) und deren Anhaenge
- relevante ADRs in secondbrain/architektur/adr/, Muster in architektur/muster/,
  Begriffe in secondbrain/glossar/
- die betroffenen Stellen der Codebase (secondbrain/codebase/ als Karte, dann echter Code)

## Pruefe in dieser Reihenfolge

1. ANTWORTEN VOLLSTAENDIG
   Ist JEDE offene Rueckfrage wirklich beantwortet (nicht nur ein Pfeil ohne Text,
   kein "ja" auf eine Entweder-oder-Frage)? Bei Varianten-Spec: ist
   freigabe_entscheidung gesetzt? Liste jede unbeantwortete/unklare Frage einzeln auf.

2. ANTWORTEN WIDERSPRUCHSFREI
   Widersprechen sich die Antworten untereinander? Widerspricht eine Antwort dem
   uebrigen Spec-Text, der Backlog-Notiz, einem ADR oder dem bestehenden Code?
   Beispiel-Muster: Antwort sagt "async", Akzeptanzkriterium beschreibt synchrones
   Verhalten. Solche Risse sind der haeufigste teure Fehler.

3. UMFANG SCHARF
   Ist klar, was NICHT dazugehoert? Fehlt eine explizite Abgrenzung, schreibe sie als
   Vorschlag hin. Gibt es stillschweigende Annahmen, die jemand anders anders lesen wuerde?

4. AKZEPTANZKRITERIEN PRUEFBAR
   Ist jedes Kriterium so formuliert, dass man es objektiv testen kann? "Funktioniert
   zuverlaessig" ist kein Kriterium. Benenne jedes schwammige Kriterium und schlage
   eine pruefbare Fassung vor.

5. RANDFAELLE UND FEHLERVERHALTEN
   Was passiert bei Fehler, Timeout, leerer Menge, gleichzeitigem Zugriff, Abbruch
   mittendrin? Bei Schreibzugriff auf Fremdsysteme: Idempotenz/Doppelausfuehrung
   geklaert? Bei Datenmodell-Aenderungen: Migration + Bestandsdaten bedacht?

6. REGRESSIONSRISIKO
   Welche BESTEHENDE Funktion koennte das brechen? Gibt es gemeinsam genutzten Code?
   Ist eine harte Akzeptanzbedingung "Bestandsverhalten unveraendert" noetig und
   vorhanden? Gibt es Tests, die das absichern - und fehlen welche?

7. GROESSE
   Ist das in einem Dev-Lauf schaffbar? Schaetze grob Anzahl betroffener Dateien und
   Schichten. Wenn zu gross: sage klar, dass es ein split- oder epic-Kandidat ist, und
   schlage einen Schnitt vor.

8. DEPLOY UND TEST
   Ist der Deploy-Abschnitt plausibel (web/service/migration)? Ist die manuelle
   Test-Checkliste so formuliert, dass der Mensch sie ohne Rueckfragen abarbeiten kann?
   Bei Eingriffen in Produktivsysteme: steht dort, dass echte Daten entstehen?

## Ergebnis

Schreibe einen Abschnitt "## Kritische Pruefung (<heutiges Datum>)" ans Ende der Spec
mit deinen Befunden, gegliedert in:
- BLOCKER - muss vor der Freigabe geklaert werden (mit konkreter Frage an den Menschen)
- SOLLTE - Verbesserung, die den Dev-Lauf sicherer macht (mit konkretem Vorschlag)
- HINWEIS - Beobachtung ohne Handlungszwang

Schliesse mit einer klaren Empfehlung in einer Zeile:
"BEREIT ZUR FREIGABE" oder "NACHBESSERUNG NOETIG: <kuerzeste Begruendung>".

Nenne in der Session zusaetzlich die drei wichtigsten Punkte im Klartext, damit der
Mensch sie sofort sieht.

Committe die geaenderte Spec mit "review: <slug> (kritische Pruefung)".

## Verboten
- status auf Freigegeben setzen
- die Spec nach specs/freigegeben/ verschieben
- Rueckfragen selbst beantworten oder Annahmen als Antwort in den
  Freigabe-Antworten-Block schreiben (du darfst Vorschlaege im Pruef-Abschnitt machen)
- Anwendungscode aendern, Worktree anlegen, mergen

Die Freigabe bleibt die Geste des Menschen. Du lieferst nur die Entscheidungsgrundlage.
