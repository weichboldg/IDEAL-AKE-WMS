---
type: backlog
title: qa-agent klickt die Testszenarien auf DEV automatisch durch (Playwright statt Agent-Klicks)
slug: 2026-10-30-qa-agent-ui-tests-playwright
status: Idee
created: 2026-10-30
updated: 2026-10-30
prioritaet: hoch
quelle: Gerald, 2026-10-30 ("der qa agent soll automatisch nun immer den deploy am dev system durchfuehren und zb. ueber playwright oder chrome die tests durchklicken")
bezug: "[[0018-qa-agent-deployt-und-testet-auf-lokaler-dev-umgebung]], [[2026-10-29-dev-umgebung-lokal]]"
---

# qa-agent klickt die Testszenarien auf DEV automatisch durch

## Ausgangslage (belegt 2026-10-30)

- `.claude/agents/qa-agent.md`, Schritt 6: DEV-Deploy je Standort und Testlauf sind bereits **Pflicht** nach den
  Punkten 1-5. `/dev` (`.claude/commands/dev.md`, Schritt 4) ruft den qa-agent immer auf. Strukturell laeuft es also
  schon "automatisch immer" - es fehlt nur die Faehigkeit, im Browser zu klicken.
- Offen laut Nachtrag in [[2026-10-29-dev-umgebung-lokal]]: `tools:`-Zeile des qa-agent (Zeile 4) um die
  `mcp__claude-in-chrome__*`-Werkzeuge erweitern - traegt Gerald selbst ein (Selbst-Rechteerweiterung wird abgelehnt).
- `D:\Dev\scripts\dev-deploy.ps1 -Quelle <Worktree>`: Parse-Check ok, noch kein echter Lauf.

## Empfehlung der Koordination

Zwei Stufen, die zweite ist die eigentliche Loesung:

**Stufe 1 - sofort (kein Code):** `tools:`-Zeile ergaenzen. Der qa-agent klickt die Browser-Schritte dann ueber die
Chrome-Erweiterung durch. Reicht fuer den Erstlauf, ist aber langsam, braucht eine offene Chrome-Sitzung mit
Erweiterung neben der Administrator-Sitzung, blockiert den Browser waehrend der QA und liefert je Lauf andere
Beobachtungen (Agent interpretiert Seiten, kein fester Pruefpfad).

**Stufe 2 - Spec:** Playwright-UI-Tests im Repo, die der qa-agent ausfuehrt statt selbst zu klicken.
- Eigenes Testprojekt (Vorschlag `IdealAkeWms.UiTests`, Microsoft.Playwright + xUnit, passt zu `dotnet test`;
  Alternative Node-Playwright unter `tests/ui/`). Basis-URL per Umgebungsvariable (`WMS_UI_BASEURL`
  = http://localhost:8081 bzw. 8082), Browser headless, Screenshot je Fehlschlag nach `TestResults/`.
- **Ein Test je Testszenario-Schritt**, benannt nach `docs/TESTSZENARIEN.md` (`TS103_1_SchalterAus_KeinMenue`), damit
  der QA-Nachweis je Schritt OK/FEHLER ausweist. Der Dev-Lauf schreibt die Tests fuer seine neuen TS-Schritte mit
  (CLAUDE.md-Checkliste ergaenzen); der qa-agent fuehrt `dotnet test IdealAkeWms.UiTests --filter TS<nr>` aus und
  traegt das Ergebnis als Beweis ein. Schritte, die DEV nicht kann (Sage live, Mails, Syncs), markiert der Test selbst
  als `Skip` mit Grund -> NICHT MOEGLICH im Nachweis.
- Nicht aus dem normalen `dotnet test` der Loesung heraus laufen lassen (braucht eine laufende Site): eigenes Projekt
  ausserhalb von `IdealAkeWms.slnx` oder per Kategorie ausgeschlossen.

## Offene Fragen (vor der Spec klaeren)

1. **Anmeldung auf DEV:** Die DEV-Sites laufen mit Windows-Anmeldung (`WindowsAuthAktiv=true`, siehe
   [[2026-10-29-dev-umgebung-lokal]]). Ob headless Chromium die integrierte Anmeldung gegen `localhost` ohne Zutun
   durchfuehrt, ist **ungeprueft**. Robuster: ADR 0002 Dual-Auth nutzen und auf DEV einen lokalen Testbenutzer mit
   Formular-Anmeldung anlegen; Kennwort nur in einer lokalen, nicht versionierten Datei (z. B.
   `D:\Dev\scripts\ui-test.local.json`), nie im Repo, nie im Brain.
2. **Rollen je Test:** Viele TS-Schritte pruefen "Benutzer ohne Rolle -> Zugriff verweigert". Dafuer braucht DEV zwei
   bis drei Testbenutzer mit festen Rollen (admin, picking, keine) - Teil des DEV-Seeds, nicht der Tests.
3. **Datenlage:** DEV-DBs sind Restores von Test-Staenden; Tests duerfen keine festen Datensaetze voraussetzen, die der
   naechste Restore wegnimmt -> eigene Testdaten anlegen oder per Abfrage suchen.
4. **Playwright-Browser-Installation** auf dem Dev-PC (`playwright install chromium`) einmalig durch Gerald; der
   qa-agent installiert nichts.

## Naechster Schritt

Spec durch den spec-agent, sobald Stufe 1 (Erstlauf qa-agent mit Chrome auf der Sage-SData-FA-Ansicht) gezeigt hat, wo
der Agent beim Klicken haengt - das sind die Faelle, die die Playwright-Tests zuerst abdecken sollten.
