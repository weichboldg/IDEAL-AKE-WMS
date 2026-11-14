---
type: adr
id: 0026
title: Schreibaktionen senden die Absicht, der Server prüft den Ausgangszustand am Schreibort; die Knopf-Sperre ist nur Komfort
status: accepted
date: 2026-11-14
supersedes: ""
superseded_by: ""
---

> Spec [[2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-spec]] (v1.99.23, Freigabe-Antworten 1–6 und Freigabe-Nachtrag
> 2026-11-14 09:00, Gerald). Befunde K-1, N-2, K-6, WE-1/WE-4, BDE-1 aus [[2026-11-14-review-befunde-kommissionierung]],
> [[2026-11-14-review-befunde-wareneingang]], [[2026-11-14-review-befunde-bde]] und der DEV-Regression [[01-kommissionierung]].

## Kontext und Problem

Fünf Schreibwege führten eine Anfrage zweimal aus, wenn sie zweimal ankam (Doppeltipp am Terminal, Netz-Wiederholung am Handgerät, zweite
Person, veraltete Seite): zweiter Folgeauftrag beim Abschluss „Teilweise“, stornierte Aufträge über den Freigabe-Umschalter, doppelte
Einbuchung mit zwei Sage-Belegen im Wareneingang, doppelte AG-Fertigmeldung. Gemeinsame Ursache: Der Server prüfte den Ausgangszustand
nicht dort, wo geschrieben wird (bzw. nicht im Wiederholversuch), und der Client sendete „dreh um“ statt des Ziels. Dazu wurde ein
EF-verpackter Deadlock nicht als Deadlock erkannt (500 statt Wiederholung). Desktop-Chrome unterdrückt den Doppelklick schon heute; die
Netz-Wiederholung und das zweite Terminal nicht – eine reine Browser-Sperre löst das Problem nicht.

## Betrachtete Optionen

1. **Nur Knopf-Sperre im Browser** – billig, hilft nicht gegen Netz-Wiederholung, zweites Gerät, veraltete Seite.
2. **Allgemeiner Idempotenz-Schlüssel je Anfrage** (Middleware + Tabelle für alle POSTs) – neue Infrastruktur für fünf Stellen, deren
   Zustand ohnehin in der DB liegt; Antwort-Wiedergabe wäre nötig.
3. **Regel je Schreibstelle:** Client sendet die Absicht (Zielzustand oder Kennung des Absendens), der Server prüft den Ausgangszustand am
   Schreibort – in der Transaktion bzw. unter Sperre und in jedem Wiederholversuch –, ein DB-Schutz deckt den echten Gleichstand; dazu
   eine globale Knopf-Sperre als Komfort.

## Entscheidung

Option 3.

- Der Client sendet die **Absicht**, nie „dreh um“: Leitstand `ToggleRelease(release=true/false)`, Wareneingang `absendekennung` (Guid aus dem
  GET, auch nach Prüffehler dieselbe).
- Der Server prüft den **Ausgangszustand am Schreibort** und in **jedem** Wiederholversuch erneut. Passt er nicht: nichts schreiben,
  sichtbare Meldung „bereits … – nichts geändert“ (MVC `TempData["WarningMessage"]`, AJAX die Servermeldung im vorhandenen Kanal) –
  nie still, nie HTTP 500.
- Der **DB-Schutz** richtet sich danach, wo der Zustand liegt: Serializable + RowVersion + Wiederholung (Abschluss), gefilterter UNIQUE
  `(Absendekennung, BelPosId)` (Wareneingang, SQL/150), `sp_getapplock` je Arbeitsgang mit Transaktions-Besitz (BDE, kein Index möglich,
  weil `IsFinal` und Arbeitsgang in zwei Tabellen liegen).
- DB-Fehler werden über die **ganze** `InnerException`-Kette erkannt: `Nummernkreis.IstSqlFehler(ex, nummern)` (1205 Deadlock, 2601/2627 UNIQUE).
- **Knopf-Sperre global** in `site.js` (Freigabe-Nachtrag): alle POST-Formulare, ausgenommen GET, `target="_blank"` (auch `formtarget`) und
  `data-mehrfach-erlaubt`; Sperre erst nach dem Absenden (Name/Wert des Knopfs bleibt in den Formulardaten), keine Sperre, wenn ein
  Seiten-Listener abbricht; Freigabe nach 10 s und bei `pageshow`. AJAX-Knöpfe über `window.wmsSperren(knoepfe)`.

## Konsequenzen

- **Positiv:** ein Muster für jede neue Schreibstelle; eine zweite Anfrage endet mit Meldung statt Doppelbuchung; Bedienung am Terminal
  bekommt sichtbare Rückmeldung („wird gesendet …“).
- **Negativ:** Seiten von vor dem Update (ohne `release`/`absendekennung`) werden abgewiesen („Seite ist veraltet“) – Info-Schritt im
  Update-Manifest. Ein Wareneingang, dessen Zeile 3 nach gespeicherten Zeilen 1–2 scheitert, kann nicht mit derselben Seite nachgebucht
  werden (WE-2, eigene Spec) – die Meldung sagt deshalb „Stand prüfen“.
- **Restfenster** (bewusst): zwei *verschiedene* Absenden mit demselben Barcode gleichzeitig (WE-4); RowVersion-Konflikt bei zwei gleichzeitigen
  Rücknahmen endet weiter als 500 (K-7).
- **Neue Schreibstelle** mit Doppel-Risiko: diese Regel anwenden; Formulare, die bewusst mehrfach senden (z. B. ohne Seitenwechsel), bekommen
  `data-mehrfach-erlaubt`.
- Gleichstände sind nicht InMemory-prüfbar ([[fallstricke]] §8) – Nachweis per Manual-UAT TS-138.1b/138.3/138.4b/138.5b.
