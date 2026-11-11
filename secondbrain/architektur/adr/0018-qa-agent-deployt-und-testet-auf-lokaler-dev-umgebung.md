---
type: adr
id: 0018
title: Der qa-agent deployt je Standort auf die lokale DEV-Umgebung und fuehrt dort die Testszenarien aus
status: accepted
date: 2026-10-30
supersedes: ""
superseded_by: ""
---

## Kontext und Problem

Bisher endete die QA mit Build, Tests, Code-Review und einer manuellen Checkliste. Alles, was EF InMemory
nicht beweisen kann (raw SQL, echtes Model Binding, Browser-JavaScript, Dienst-Verhalten), ging ungeprueft an
den Menschen weiter und fiel erst auf dem Testsystem auf. Seit 2026-10-29 gibt es auf Geralds PC je Standort
eine DEV-Umgebung mit Kopie der Test-DB, IIS-Site und Windows-Dienst ([[2026-10-29-dev-umgebung-lokal]]).
Seit 2026-10-30 laufen Dienste und App-Pools unter `AKENB\svc_idealakewms` (`db_owner` nur auf den DEV-DBs).

## Entscheidung

Der qa-agent hat einen Pflichtschritt 6 „DEV-Deploy und Testlauf“ (`.claude/agents/qa-agent.md`):

1. Standort aus dem Spec-Frontmatter `standort: AKE | IDEAL | beide` (neu in `_templates/spec.md`, gesetzt
   vom spec-agent; fehlt es, leitet der qa-agent es ab und traegt es ein; unklar = beide).
2. Je Standort **Web und Dienst** aus dem Worktree der Spec veroeffentlichen:
   `D:\Dev\scripts\dev-deploy.ps1 -Standort <AKE|IDEAL> -Quelle <worktree>`. Immer beide Komponenten, damit
   DEV einen einheitlichen Code-Stand hat. Fusszeilen-Version muss der `AppVersion` des Zweigs entsprechen.
3. Die Testszenarien der Spec auf der DEV-Site ausfuehren (Browser, `sqlcmd` gegen die DEV-DB, Dienst-Log)
   und je Schritt OK / FEHLER / NICHT MOEGLICH festhalten. Deploy-Fehler oder FEHLER = kein Testbereit.

Sage-Buchung, Syncs und Mail-Schalter bleiben in DEV aus; Schritte, die sie brauchen, bleiben fuer den Menschen.

## Folgen

- Schranke 2 bleibt beim Menschen: Testsystem-Lauf und Merge sind unveraendert.
- Ein Worktree-Deploy hebt die DEV-DB auf den Migrationsstand des Zweigs; ein spaeterer Deploy eines
  aelteren Stands (z. B. AKE-`main`) laeuft dann gegen eine neuere DB. Fuer DEV bewusst hingenommen;
  bei Problemen Restore aus `D:\akewms.bak` / `D:\idealwms.bak`.
- Der qa-agent braucht eine Administrator-Shell (Dienst/App-Pool stoppen); ohne sie gilt die QA als nicht bestanden.
- Fuer die Browser-Schritte braucht der qa-agent die `claude-in-chrome`-Werkzeuge in seiner `tools:`-Zeile.

## Nachtrag 2026-11-02

Auf Geralds Wunsch deployt der qa-agent **automatisch direkt nach gruenem Build und gruenen Tests** (Punkt 1),
ohne Rueckfrage und noch vor dem Code-Review. Der Testlauf auf DEV (6c) folgt nach den Punkten 2–5. Faellt ein
spaeterer Punkt durch, bleibt der Zweig-Stand auf DEV stehen; rote Builds oder Tests werden nie deployt.
