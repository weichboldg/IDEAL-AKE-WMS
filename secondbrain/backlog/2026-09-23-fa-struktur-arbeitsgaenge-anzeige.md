---
typ: feature
---
# FA-Struktur: erkannte BDE-Arbeitsgänge je (Sub-)FA sichtbar machen (Modal)

**Aufgenommen 2026-09-23.**

## Worum es geht

Seit dem BDE-Arbeitsgänge-Epic ([[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]], v1.44.0)
legt die Struktur-Erkennung echte `WorkOperation`-Zeilen je materialisiertem Sub-FA an. **Operativ**
sind sie am BDE-Terminal sichtbar/buchbar (Baustein c). Es fehlt aber ein **Überblick zur
Verifikation**: Wo sieht ein Mensch je Auftrag, **welche** Arbeitsgänge erkannt wurden? Heute nur im
Aktivitäts-Protokoll (SyncLog).

**Wunsch (Mensch, 2026-09-23):** In der **FA-Struktur** je Auftrag ein Knopf, der die erkannten
Arbeitsgänge zeigt — am HauptFA die FA-Nummer mit ihrer AG-Liste, und je Sub-FA-Nummer die jeweilige
AG-Liste.

## Entschieden (Schranke-1-Vorlage, 2026-09-23)

- **Umfang: nur erkannte AGs.** Nur die tatsächlich angelegten `WorkOperation`s je (Sub-)FA anzeigen —
  **keine** Lücken-/Unbekannt-Analyse in dieser Ansicht (die bleibt im SyncLog/der Sammelmeldung).
- **Darstellung: Modal je FA-Nummer.** Ein Knopf in der Struktur-Zeile öffnet einen Dialog, der die
  Arbeitsgänge listet. Titel = FA-Nummer. Read-only (keine Bearbeitung — AGs sind Sync-geführt).
- **Aufbau des Modals** (entspricht dem Wortlaut des Wunsches): am HauptFA die HauptFA-Nummer mit ihrer
  AG-Liste **und** darunter je Sub-FA-Nummer die jeweilige AG-Liste (konsolidierte Sicht des ganzen
  FA-Baums). Je AG: `OperationNumber` (Kürzel), `Name`/Werkbank, Sage-Arbeitsplatznummer.

## Technische Hinweise (am Code, für die Spec)

- Ort: FA-Struktur-Baum `FaHierarchyController.Index` → `Views/FaHierarchy/Index.cshtml` +
  `_FaHierarchyNode.cshtml`. Knopf nur auf Zeilen mit `SubFA != 0` (materialisierte Aufträge; Blatt-/
  Materialzeilen `SubFA == 0` haben keinen Auftrag → kein Knopf).
- Datenzugriff existiert: `IWorkOperationRepository.GetByProductionOrderIdWithWorkplaceAsync` bzw. ein
  FA-Nummer-basierter Lookup. Mapping: `FaHierarchyNode.HauptFA`/`SubFA` → `ProductionOrder.OrderNumber`/
  `SubOrderNumber` → `WorkOperation` (inkl. `ProductionWorkplace` für Werkbank-Name/Sage-Nr.).
- **Kein N+1:** die AGs **einmal gebündelt** je sichtbarem Auftrag laden und je Sub-FA gruppieren
  (analog wie Matchcode/Kopfdaten im Baum geladen werden), in das Baum-View-Model hängen.
- `frontend-design`-Skill ist **Pflicht** (Modal = View-/JS-Änderung). Konsistenz vor Eigenständigkeit
  (Bootstrap-5-Modal, bestehende Muster), Kontrast WCAG AA.
- Nur-Lesen: kein neuer Zugriffsfilter (die FA-Struktur hat ihren bestehenden `RequireXxxAccess`).

## Abhängigkeit / Reihenfolge

- Setzt das Epic [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] (v1.44.0) voraus — ohne die
  angelegten `WorkOperation`s zeigt das Modal nichts. Das Epic ist **Testbereit**, wartet auf Schranke 2.
- **Eigene Spec, nicht Teil des laufenden Epics** (das nicht wieder aufmachen). Kann im selben
  Bündel-Worktree gebaut werden, nachdem/während das Epic dort liegt.

## Offene Rückfragen (für die Spec)

- Knopf **nur** auf der HauptFA-Zeile (konsolidiertes Modal für den ganzen Baum) — oder **zusätzlich**
  je Sub-FA-Zeile ein Knopf, der nur die AGs dieses einen Sub-FA zeigt?
- Verhalten bei **0 erkannten AGs** für einen Auftrag: Knopf ausblenden, oder anzeigen und im Modal
  „keine Arbeitsgänge erkannt" ausgeben (Hinweis auf noch nicht gepflegte Werkbank/Kürzel)?
- Soll das Modal (trotz „nur Treffer") die Sub-FAs **ohne** AG wenigstens mit „—" listen, damit die
  Vollständigkeit über den Baum erkennbar bleibt?
