---
typ: feature
---
# IDEAL-Nachlese: gesammelte Restarbeiten nach dem Buendel-Merge

Erhebung am 2026-08-18 aus allen acht Teil-Specs, der Uebersicht (drei QA-Runden) und den
Epic-Abschluss-Checklisten. Zweck: **einmal gesammelt abarbeiten** statt verstreut nachziehen.

> **Ergaenzung 2026-09-08:** Ein vollstaendiger Code-Review des Buendels (sechs parallele Reviewer)
> hat zusaetzliche Befunde + Testluecken erhoben → eigene Liste
> [[2026-09-08-ideal-code-review-nachlese]]. Kein funktionaler Blocker; Substanz sind ein
> config-abhaengiges Risiko (H1), Post-UAT-Toter-Code und Testluecken.

> ## KORREKTUR nach Verifikation am Code/an den Dateien (2026-08-18)
>
> Die Ersterhebung stuetzte sich auf die **Prosa der QA-Runden** („Nicht in dieser QA-Runde
> nachgezogen … stehen weiterhin offen") statt auf die Dateien selbst. Diese Prosa war ihrerseits
> veraltet. **Fuenf Punkte sind bereits erledigt oder gegenstandslos:**
>
> | Punkt | Tatsaechlicher Stand |
> |---|---|
> | 2–5 (ADR 0005, fallstricke, controller.md, feature-map/changelog) | **Inhaltlich erledigt.** Offen sind nur die **Haken** in `aufgaben/2026-08-07-ideal-teile-1-5.md` — reiner Tracking-Nachzug, keine Arbeit. |
> | 7 (Teil 6 Standorteinstellungen) | **Umgesetzt und `Testbereit` (v1.34.0)** — nicht offen. |
> | 9 (Alt-Spec archivieren) | **Erledigt** — liegt in `specs/archiv/` mit `status: Ueberholt`. |
> | 17 (View-Namen ohne Schema-Praefix) | **Gegenstandslos** — durch Pre-Merge-Fix `28cd3f6` behoben. |
>
> **Bestaetigt offen bleibt Punkt 1** (BOM-Guard) — per grep im Worktree verifiziert: noch nicht
> gebaut.
>
> **Lehre, die ueber diese Notiz hinausgeht:** Ein Dokument, das sagt „X ist offen", altert genauso
> wie ein Dokument, das sagt „X ist erledigt". Vor einer Restarbeiten-Liste gehoert der Blick in die
> Dateien, nicht in den Bericht ueber die Dateien.

## A — Vor dem Merge (nur das Noetigste)

1. **BOM-Knopf faengt im hierarchischen Modus ab, statt zu werfen.**
   `/Picking/Bom/<id>` laeuft gegen `[ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB]` — auf
   einem IDEAL-System HTTP 500. Nach der Materialisierung stehen IDEAL-Auftraege in
   `ProductionOrders` und damit im Picking; die UAT trifft den Fehler.
   **Minimal-Fix:** Guard + Hinweis („Stueckliste ueber die FA-Struktur einzusehen", Verweis auf
   `/FaHierarchy`). **Kein** neues Repository, kein Cache-Umbau — die vollstaendige Loesung ist
   [[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]] und laeuft NACH dem Merge.
   *Begruendung fuer das Vorziehen:* Der Fehler ist ohne Checkliste aufgetreten; er steht in keinem
   der 30 UAT-Punkte, ist aber im normalen Gebrauch erreichbar.

## B — Brain-Nachtraege (klein, aber wirksam — ohne sie wiederholt sich der Fehler)

Alle vier stehen offen auf der Epic-Abschluss-Checkliste
(`secondbrain/aufgaben/2026-08-07-ideal-teile-1-5.md`) und waren bewusst nicht QA-Scope:

2. **ADR 0005 — additiver Nachtrag „Spaltenpraeferenzen" als VIERTER Pflichtbestandteil.**
   Der wichtigste Punkt dieser Liste: Ohne ihn entsteht die naechste neue Liste wieder ohne
   Spaltenauswahl. Heute nennt ADR 0005 nur Pagination, Filterkarte und Spaltenfilter —
   `column-preferences.js` taucht dort ausschliesslich als Init-Reihenfolge-*Risiko* auf. Details:
   [[2026-08-12-listen-spaltenauswahl-spec]].
3. **`secondbrain/architektur/fallstricke.md`:** ColumnDefinitions-Registrierungspflicht (ohne
   Eintrag antwortet die Prefs-API mit 400 und speichert stillschweigend nichts) + der
   Mehr-`<tbody>`-Sortier-Fallstrick.
4. **`secondbrain/codebase/controller.md`:** die neuen Controller, Access-Filter und Toggles der
   Teile 2-5 (+ HierarchieUmstellung aus Teil 7).
5. **`secondbrain/feature-map.md`** und **`secondbrain/changelog/`**: Eintraege fuer v1.31.0 /
   v1.32.0 / v1.33.0.

## C — Nach dem Merge, funktional

6. **AKE-View-Abhaengigkeiten vollstaendig** — [[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]]:
   eigenes `FaHierarchyBomRepository` **plus Sweep** ueber alle `vw_AKE_`/`[ake].[dbo]`-Fundstellen
   in Web und Service. Der BOM-Knopf war nur der erste, der angeklickt wurde.
7. **Teil 6 — Standorteinstellungen-Maske.** Einziger Teil des Pakets, der noch nicht umgesetzt ist
   (`Freigegeben`, kein Epic). Haengt an Teil 3 (Firmendaten-Keys) und Teil 7 (Master-Anzeige).
8. **`supportsSortDefault: false` — Grund weggefallen.** Die drei IDEAL-Listen tragen den Schalter
   defensiv wegen des Sortier-Defekts; der ist mit Etappe 6 behoben
   ([[2026-08-12-tabellen-sortierung-nur-erste-gruppe-bug]]). Entscheiden, ob er auf `true` geht —
   **nicht stillschweigend belassen**, sonst bleibt eine Einschraenkung ohne erkennbaren Grund.
9. **Alt-Spec archivieren:** `2026-07-28-ideal-anpassungen-neu-nachbilden-spec` liegt weiterhin in
   `specs/entwurf/` und ist durch B1/B5 ueberholt. Gleiche Falle wie der tote AgentJob-SQL — nach
   `specs/archiv/` mit Kopfvermerk.

## D — Fachlich zu klaeren (nicht Code, aber blockierend fuer den Produktivgang)

10. **Sage-View-DDL sind Platzhalter.** `SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql`
    und `..._FAInfos.sql` sind „Struktur laut Anhang"/TODO — sie legen am Zielsystem **nichts** an.
    Die realen Views muessen am IDEAL-Sage-System existieren bzw. final abgestimmt werden.
11. **Dienstleister-Layout (Teil 4).** Das Druckdokument fuer den Beschichter ist mit einem
    vorlaeufigen Layout gebaut; die Corporate-Design-Vorlage steht aus. Vor Produktivgang muss es
    sich ansehen, wer die Beschichter-Beziehung verantwortet — das Dokument verlaesst das Haus.
12. **Kombinationsgeraete** — [[2026-08-06-kombinationsgeraete-montageabteilung]]: Wurzel ist der
    fehlende Trennschluessel auf Positionsebene. Ohne ihn ist keine echte Trennung moeglich; heute
    werden mehrdeutige Kopfzeilen nur sichtbar gemacht.
13. **PDF-Erzeugung** — [[2026-08-06-pdf-erzeugung-fahierarchy-druck]]: entschieden ist der Weg
    (Headless Edge, ein PDF je HauptFA, Download), die Spec fehlt noch.
14. **Isolierfraesen-Export** — [[2026-08-06-vormontage-isolierfraesen-export]]: wartet auf die
    Format-Spezifikation der Zielsoftware.
15. **Sub-FA-Barcodes (Teil 8, TS-66.10).** Der Fallback-Zweig `SubOrderNumber` im Scan ist bewusst
    als Vorruestung gebaut und bleibt **unerreichbar**, bis Barcodes Sub-FA-Nummern tragen.
    Fachlich zu klaeren, ob das kommt — sonst waehlt der Werker dauerhaft aus der Liste.
16. **Wochenbezug `Neuer_PT_PPS` (Teil 5).** Etappe 8 hat die KW-Filter auf `KO_Termin`
    (Kommissionier-Summiert) und `FE_Termin` (Vormontage-Summiert) gelegt. Ob `Neuer_PT_PPS`
    zusaetzlich als Filter gebraucht wird, ist mit dem Fachbereich zu klaeren.

## E — Betriebshinweise, die leicht untergehen

17. **View-Namen ohne Schema-Praefix konfigurieren.** `Sync:FaHierarchyListeViewName` /
    `Sync:FaHierarchyInfosViewName` nur als reiner Objektname (`ViewName` oder `[ViewName]`),
    **nicht** `dbo.ViewName` — sonst entsteht ein fehlerhafter Drei-Teile-Bezeichner.
18. **RCSI muss AN sein.** Der gebaute Full-Refresh-Weg (DELETE + Neubefuellung in einer
    Transaktion) setzt das voraus; ein Fallback wurde bewusst **nicht** gebaut. Vor dem ersten
    Produktivlauf einmalig pruefen (TS-57.0).
19. **Alle Feature-Toggles stehen default aus** und muessen am IDEAL-Zielsystem bewusst aktiviert
    werden — inklusive des Masters, der eine Einwegtuer ist.

## Reihenfolge

**A** vor dem Merge (ein kleiner Fix, danach QA erneut). **B** unmittelbar nach dem Merge — klein,
aber es verhindert die Wiederholung des Musters. **C** als naechster Entwicklungsblock. **D** laeuft
parallel und ausserhalb der Pipeline. **E** gehoert in die Deploy-Checkliste, nicht in einen
Dev-Lauf.
