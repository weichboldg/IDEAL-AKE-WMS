---
type: adr
id: 0015
title: Einmalige Datenskripte liegen in SQL/Einmalig/, ausserhalb der nummerierten Reihe
status: accepted
date: 2026-09-29
supersedes: ""
superseded_by: ""
---

## Kontext und Problem

[[0004-migrations-und-sql-disziplin]] regelt Schema-Aenderungen: jedes `SQL/XX_*.sql` gehoert zu einer
EF-Migration, ist idempotent (Objekt-Guard) und wird im Wartungsfenster der Reihe nach eingespielt. Der
Hotfix v1.30.1 ([[2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist-spec]]) brauchte erstmals ein
reines **Datenskript** ohne Schema-Aenderung: Autosave-Nullen offener Lagerbestellungen einmalig auf `NULL`
zuruecksetzen. Ein solches Skript ist **nicht** idempotent im Sinne der Reihe — es gibt kein Objekt, gegen
das ein Guard pruefen koennte, und ein zweiter Lauf ist fachlich schaedlich: nach dem Hotfix ist eine
getippte `0` eine bewusste Bestaetigung, die ein erneuter Lauf still loeschen wuerde.

## Betrachtete Optionen

- **Naechste Nummer in der Reihe (`SQL/94_*`), ohne History-Eintrag** — auffindbar und chronologisch, aber
  die Reihe ist zum Der-Reihe-nach-Ausfuehren da; beim naechsten Durchgang (anderes System, neuer DBA,
  Wiederaufbau) laeuft das Skript mit.
- **Als EF-Migration mit `Sql(...)`** — liefe automatisch bei jedem `Migrate()` auf jedem System, auch auf
  einem frischen, und ist an den App-Start gekoppelt statt an einen bewussten Deploy-Schritt.
- **Eigener Ordner `SQL/Einmalig/`, keine Nummer** — gewaehlt.

## Entscheidung

Einmalige Datenskripte liegen in **`SQL/Einmalig/`**, benannt `YYYY-MM-DD_<Anlass>_<Zweck>.sql`, **ohne**
laufende Nummer, **ohne** `__EFMigrationsHistory`-Eintrag und **ohne** Eintrag in `SQL/00_FreshInstall.sql`
(ein frisches System hat nichts zu bereinigen). Pflicht je Skript:
- Warnkopf **„NICHT ERNEUT AUSFUEHREN"** mit dem fachlichen Grund, warum ein zweiter Lauf schadet;
- Zaehl-`SELECT` vor dem `UPDATE`, `@@ROWCOUNT` danach;
- Audit-Felder (`ModifiedAt`, `ModifiedBy` = Anlass-Kennung, `ModifiedByWindows = SYSTEM_USER`), siehe
  [[0003-auditableentity-als-entity-basis]];
- Reihenfolge im Kopf (Backup, Deploy, dann Skript);
- Ausfuehrung **je System** im Deploy-Protokoll der Spec vermerken (System, Datum, Zaehlergebnis).

Der Dev-Lauf legt das Skript an, fuehrt es **nie** aus; die Ausfuehrung ist ein Deploy-Schritt des Menschen.

## Konsequenzen

- Die nummerierte Reihe bleibt „alles darin darf der Reihe nach laufen" — ein Durchgang kann nichts
  Einmaliges wiederholen.
- Ob ein Einmal-Skript auf einem System schon lief, weiss nur das Deploy-Protokoll (kein technischer
  Marker). Bewusst: ein Marker-Tabelleneintrag waere Schema fuer einen Einzelfall. Bei haeufigerem Bedarf
  neu entscheiden.
- Beim Vorwaerts-Merge in andere Zweige (z. B. das IDEAL-Buendel) kollidieren Einmal-Skripte nicht mit
  Nummern — der Ordner ist nummernfrei.
