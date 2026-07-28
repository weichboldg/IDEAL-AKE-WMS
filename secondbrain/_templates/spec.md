---
type: spec
title: <Kurztitel>
slug: <yyyy-mm-dd-slug>
status: Entwurf
created: <yyyy-mm-dd>
updated: <yyyy-mm-dd>
source_backlog: ""        # [[backlog-datei]] als Wikilink, NICHT als Pfad-String
task: ""
worktree: ""
branch: ""
affected_code: []
open_questions: []
deploy:
  web: false            # muss die Web-App neu deployt werden?
  service: false        # muss der Windows-Service neu deployt werden?
  migration: false      # bringt die Aenderung eine EF-Migration mit?
freigabe:
  entscheidung: ""      # Schranke 1: A | B bei Varianten-Specs, sonst "freigegeben"
  von: ""               # Kuerzel des Freigebers
  am: ""                # yyyy-mm-dd
---

## Ziel / Nutzen (das Warum)

## Umfang (In-Scope / Out-of-Scope)

## Fachliche Anforderungen

## Technischer Loesungsentwurf
<!-- Muster nennen (Repository/Decorator, Listen-View-Pattern), betroffene Module verlinken -->

## Migrations-/SQL-Auswirkungen
<!-- Migration + SQL/XX_*.sql mit OBJECT_ID-Guard? FreshInstall? -->

## Audit-Feld-Auswirkungen

## Akzeptanzkriterien
<!-- testbar formuliert, nummeriert -->

## Test-Szenarien
<!-- Verweis auf docs/TESTSZENARIEN.md Kapitel; neue Szenarien skizzieren -->

## Deploy
<!--
  Vom Dev-Lauf ausgefuellt. Was muss deployt werden und wie.
  Setze oben im Frontmatter deploy.web / deploy.service / deploy.migration.

  ABLAUF (Reihenfolge des Menschen): Publish aus dem WORKTREE -> Testsystem ->
  testen -> erst dann Merge (Schranke 2). Der getestete Worktree-Stand IST
  der Deploy-Stand, solange der Merge danach konfliktfrei und ohne
  Ueberschneidung mit parallelen main-Aenderungen ist (Normalfall).
  Nur falls der Merge tatsaechlich getestete Dateien mit fremden Aenderungen
  zusammenfuehrt, nach dem Merge vom main-Stand neu publishen.

  Publish-Vorlage (im Worktree ausfuehren; nach dem Merge waere der Pfad das
  Repo-Root):
    dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
    dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService

  Nur die tatsaechlich betroffene(n) Komponente(n) auflisten. Bei Migration:
  Reihenfolge/Hinweis (DB zuerst? Service-Stop noetig?) ergaenzen.
-->

- **Web-App:** ja/nein
- **Service:** ja/nein
- **Migration:** ja/nein
- **Publish-Befehle:**

## Offene Rueckfragen
<!-- nummeriert; jede Frage bekommt unten im Freigabe-Block dieselbe Nummer -->

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe.entscheidung im Frontmatter setzen.
-->

1. →
