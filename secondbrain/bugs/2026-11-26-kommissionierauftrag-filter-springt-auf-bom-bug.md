---
type: bug
title: "Kommissionierauftrag: Filtern und „Zurücksetzen“ verlassen die Auftragsseite (ohne Umschalter Stückliste/Liste)"
status: offen
severity: mittel
created: 2026-11-26
affected_code:
  - IdealAkeWms/Views/Picking/Bom.cshtml (Filterformular Z. 114-115, „Zurücksetzen“ Z. 126)
  - IdealAkeWms/Controllers/PickingController.cs (Bom Z. 220-249 mit Nebenwirkung ErzeugeOffeneAsync Z. 239-247; Auftrag Z. 255-261; KannListe Z. 334-338)
source_backlog: "[[2026-11-26-kommissionierauftrag-filter-springt-auf-bom]]"
spec: "[[2026-11-26-kommissionierauftrag-filter-springt-auf-bom-spec]]"
---

## Symptom

Auf einem Kommissionierauftrag (`/Picking/Auftrag/{id}`) führen „Filtern“ und „Zurücksetzen“ auf die FA-Stückliste
`/Picking/Bom/{ProductionOrderId}` statt zurück auf den Auftrag. Die Seite dort ist nur lesbar (`ReadOnly = auftrag == null`,
`PickingController.cs:323`) und zeigt nur die Auftragsliste des FA – der Kommissionierer verliert seinen Auftrag und muss zurücknavigieren.
Betroffen, sobald der Umschalter Stückliste/Liste **nicht** sichtbar ist: AKE immer; IDEAL, wenn `FaHierarchyKommissionierlistenAktiv` aus
ist oder die Stückliste nicht aus der FA-Struktur kommt.

## Reproduktion

1. AKE-DEV (oder IDEAL mit `FaHierarchyKommissionierlistenAktiv = false`), Benutzer mit Picking-Recht.
2. Kommissionierauftrag öffnen (`/Picking/Auftrag/{id}`).
3. Im Feld „Filter“ einen Text eingeben, „Filtern“ → URL ist `/Picking/Bom/{FA-Id}?filterText=…`, Kopf „Kommissionierauftrag …“ fehlt,
   keine Haken/Umbuchung.
4. Wieder auf dem Auftrag mit Filter (URL `/Picking/Auftrag/{id}?filterText=x` direkt): „Zurücksetzen“ → ebenfalls `/Picking/Bom/{FA-Id}`.

## Root Cause

`Bom.cshtml:114` und `:126` wählen das Ziel nach `Model.KannListe` statt danach, ob die Seite im Auftragsmodus ist (`Model.Auftrag != null`).
`KannListe` ist nur `true` am Auftrag **mit** FA-Struktur und aktiven Kommissionierlisten (`PickingController.cs:334-338`); in allen
anderen Auftragsfällen fällt das Formular auf `Url.Action("Bom", new { id = Model.ProductionOrderId })`. Vor v1.99.89 war das Ziel immer
`asp-action="Bom"` (main `Views/Picking/Bom.cshtml:39`, `:50`) – v1.99.89 hat nur den Fall mit Umschalter korrigiert.

Nebenwirkung: `Bom` legt bei `LeitstandAktiv = false` fehlende Kommissionieraufträge an (`PickingController.cs:239-247`,
`ErzeugeOffeneAsync`) – ein Filterklick auf dem Auftrag kann also einen Erzeugungslauf auslösen (idempotent, aber ungewollt).

## Fix / Verweis

[[2026-11-26-kommissionierauftrag-filter-springt-auf-bom-spec]]: Ziel von Formular und „Zurücksetzen“ nach `Model.Auftrag != null`;
`ansicht` nur bei `KannListe` mitgeben.
