---
type: adr
id: 0037
title: Teilfertigmeldung – mehrere Meldungen je Zeile, eindeutig nur die abschließende (IstAbschliessend)
status: accepted
date: 2026-11-25
supersedes: ""
superseded_by: ""
---

> Spec [[2026-11-25-bde-masken-vereinheitlichung-spec]], Etappe 3 (v1.99.75, TS-196, SQL/169), Freigabe Gerald 2026-11-25. Ergänzt
> [[0030-teilmeldung-mit-fachlichem-schluessel-neben-workoperation]] und [[0038-teilmeldung-auch-fuer-lagerbestell-positionen]].
> Bündel `feature/2026-08-07-ideal-teile-1-5`, Commit `94be5444`.

## Kontext und Problem

- Bis v1.99.74 gab es je Teileliste-Zeile (fachlicher Schlüssel bzw. Lagerposition + Kürzel) genau **eine** nicht stornierte Meldung; der
  gefilterte UNIQUE-Index erzwang das. Werker fertigen aber oft in mehreren Durchgängen (3 von 10, dann 4, dann der Rest).
- Gebraucht: beliebig viele Teilmeldungen, aber weiterhin höchstens **ein** Abschluss je Zeile – auch bei zwei Geräten gleichzeitig.

## Entscheidung

- Neue Spalte `BdeTeilMeldungen.IstAbschliessend` bit NOT NULL. **Fertig** = 1 (schließt die Zeile), **Teilfertig** = 0 (Zeile bleibt offen).
  Bestand gilt als abschließend (DB-Default 1 beim ALTER füllt alle vorhandenen Zeilen).
- **Beide** gefilterten UNIQUE-Indizes bekommen zusätzlich `[IstAbschliessend] = 1`:
  - `UX_BdeTeilMeldungen_Schluessel` … `WHERE [IsCancelled] = 0 AND [IstAbschliessend] = 1 AND [WarehouseRequisitionItemId] IS NULL`
  - `UX_BdeTeilMeldungen_Lagerbestellung` … `WHERE [IsCancelled] = 0 AND [IstAbschliessend] = 1 AND [WarehouseRequisitionItemId] IS NOT NULL`
  Auch die Lagerbestell-Zeilen dürfen Teilfertig – ein Dialog, eine Regel für beide Zeilenarten.
- **Kein EF-`HasDefaultValue`** (bool-Sentinel, [[fallstricke]] „HasDefaultValue bei bool“): die Defaults stehen nur in Migration, SQL/169 und
  FreshInstall; die Anwendung setzt `IstAbschliessend` bei **jeder** Meldung ausdrücklich.
- Doppelschutz unverändert dreistufig: App-Prüfung „schon eine abschließende Meldung?“ (InMemory-testbar), `sp_getapplock` je Schlüssel,
  Index als letzte Instanz (2601/2627 → „bereits gemeldet“ mit dem Gewinner).
- Mengen: „Fertig“ ist mit der Restmenge (Soll − Summe Gut, mindestens 0) vorbelegt, auch kleiner erlaubt; „Teilfertig“ braucht Gut > 0;
  Übermeldung per Teilfertig wird gespeichert, Hinweis „Sollmenge erreicht – bitte Fertig melden“ (WarningMessage). Block/Summen-Haken melden
  immer die Restmenge als Fertig.
- Baugruppe: Teilfertig bucht über `ReportCompletionCoreAsync(..., istFinal: false)` eine nicht finale Menge am Sub-FA-Arbeitsgang; Fertig final.

## Begründung

- Ein Index, der nur den Abschluss eindeutig macht, ist die kleinste Änderung: Rücknahme, Admin-Storno, Teilmeldungen-Liste und Audit bleiben,
  wie sie sind; keine zweite Tabelle für Teilmengen.
- Filter wird nur **enger** – der Neubau kann am Bestand nicht scheitern (SQL/169 prüft trotzdem vorab und meldet Dubletten statt still zu brechen).

## Folgen

- Je Zeile sind jetzt **mehrere** Meldungen normal. Jeder Leseweg muss wählen: alle Meldungen (Summe „gemeldet X von Soll“, Liste) oder nur die
  abschließende („fertig gemeldet“, Doppelschutz, gemeldete ausblenden, AG-Status erledigt). Teilgemeldet ohne Abschluss = AG-Status „läuft“.
- Waisen werden je Schlüssel gezählt, nicht je Meldung.
- **Down nicht verlustfrei:** Gibt es eine Zeile mit mehreren nicht stornierten Meldungen, scheitert der alte UNIQUE beim Rückbau – vorher
  Teilmeldungen stornieren oder Backup einspielen.
