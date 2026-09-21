---
typ: notiz
spec: "[[2026-09-18-stueckliste-kommissionierziel-filter-spec]]"
status: InUmsetzung
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Stückliste Komm.-Ziel-Dropdown-Filter + gespeicherter Standardfilter (Badge für alle vier)

Spec [[2026-09-18-stueckliste-kommissionierziel-filter-spec]] · **bestehender Bündel-Worktree** (kein
neuer, Deploy-Abschnitt: „als zusätzliche Etappe"). Migration **92**, Deploy web/keine Service/Migration.

**Reihenfolge (Vorgabe):** Bildschirm-Teil (In-Scope 1–6) ZUERST, Druck (In-Scope 7) ZULETZT. Wird der
Druck größer als erwartet → nach dem Bildschirm-Teil anhalten und melden (dann Druck = eigene Folge-Spec).

## Pre-Flight-Verdikt (2026-09-21)

- status Freigegeben, open_questions [], freigabe_entscheidung/_von/_am gesetzt.
- Freigabe-Antworten 1–6 in den Rumpf gezogen; Kritische Prüfung B1 (Badge alle vier) durch
  freigabe_entscheidung „Badge für alle vier" geklärt → kein offener Blocker.
- Frontend-Arbeit (Views/JS) → `frontend-design`-Skill PFLICHT vor Umsetzung.

## Auftrag (Rumpf = maßgeblich, Antwort-Abschnitte = Begründung)

**Bildschirm (In-Scope 1–6):**
1. `kommissionieren`-Spaltenfilter in `Bom.cshtml` von Freitext auf **Select2-Dropdown** (nur
   hierarchischer Modus), Werte = DISTINCT der geladenen `Model.Items.Kommissionieren`, Mehrfachauswahl OR.
2. Neues Feld `User.DefaultFilterBomKommissionierziel` (`string?`, `[StringLength(200)]`, Migration 92) —
   1:1 wie `DefaultFilterBomDescription1`; Einstellungen (Users/Edit, Account/Profile) als **Textfeld**
   (kein Dropdown!); Anwendung via `window.setColumnFilter('kommissionieren', …)`.
3. **Badge + Ein-Klick-Reset für ALLE VIER** still vorbelegten Standardfilter (Beschaffung,
   Artikelgruppe, Bezeichnung 1, Komm.-Ziel) — ein Bedienelement, je Feld konfiguriert; schließt
   vorbestehenden Mangel (Beschaffung/Artikelgruppe filtern heute still).
4. **Benannter Leerzustand** (0 Treffer): „Standardfilter Kommissionierziel `<Wert>` aktiv — keine
   Treffer in dieser Stückliste" + Reset. Teiltreffer (OR) → keine Meldung.
5. **Auto-Aufklappen** der Treffer-Äste bei Filterauswahl (Muster FA-Liste).
6. **Einstellungsfeld nur bei Master `true`** — NEUE Verdrahtung: `UsersController` + `AccountController`
   lesen `ProduktionsauftragHierarchisch` (heute keiner) und reichen ins ViewModel; `@if` in beiden Views.

**Druck (In-Scope 7, ZULETZT):**
7. `PrintBom.cshtml`: neue `ShowCol`-Kopf-/Zellblöcke für kommissionieren/hauptlagerplatz/Ebene,
   `colNames`-Map (Bom.cshtml) erweitern, „Gefilterte Ansicht"-Hinweis. Schließt offenen BOM-Bridge-Druckbefund.

## Arbeitsstand

- 2026-09-21: Spec Freigegeben → InUmsetzung. Referenz-Muster (`DefaultFilterBomDescription1`) lesen, dann Umsetzung.
