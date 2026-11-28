---
type: backlog
created: 2026-11-14
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
status: offen
modul: Kommissionierbereiche und -typen (PickingArea, PickingAreaRule; Lager, Glas, Vorkommissionierung)
stand: "Bündel v1.99.22, 90bbbbd7, Diff main...HEAD (Merge-Base 1753c79e)"
---
# Review-Befunde Kommissionierbereiche (Teil A1, lesend)

Ergänzt [[2026-11-14-review-befunde-kommissionierung]]. Grundlage: [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]]
(AK 2–4, ADR 0020-Maske, Entscheidung Q10). Pfade Worktree-relativ. **Nichts behoben.**

| ID | Schwere | Kurz |
|---|---|---|
| B-1 | mittel | Standardwechsel nur über „abwählen → setzen“: Zeitfenster ohne Standard, Freigaben darin bleiben ohne Auftrag |
| B-2 | niedrig | Druck rechnet die Fälligkeit „Vorkommissionierung“ ohne Werkbank-Override – weicht von der Liste ab |
| B-3 | niedrig | Bereich deaktivieren/Regel löschen ohne Hinweis auf offene Aufträge bzw. ohne Rückfrage |
| B-4 | Hinweis | Kommentar in `Program.cs` sagt „die Pflege schützt den Standard“ – seit Q10 nicht mehr zutreffend |
| B-5 | Hinweis | Typ „Glas“ an AKE hängt nur an Artikel-Regeln; das FA-Häkchen `HasGlass` erzeugt keinen Glas-Auftrag |

## B-1 – Zeitfenster ohne Standardbereich beim Wechsel
- **Schwere:** mittel
- **Fundstelle:** `IdealAkeWms/Controllers/PickingAreasController.cs:72-81`, `:99-102`; `IdealAkeWms/Services/PickingOrderWorkflow.cs:89-95`;
  Freigabe-Hooks `IdealAkeWms/Controllers/PickingLeitstandController.cs:417-418`.
- **Befund:** Seit Q10 lehnt die Maske einen zweiten Standard ab; gewechselt wird in **zwei** Speichervorgängen (alten abwählen, neuen
  setzen). Dazwischen gibt es keinen aktiven Standard – die Maske warnt nur. Jede Freigabe in diesem Fenster setzt die Freigabe trotzdem
  (`SetReleaseAsync` vor `ErzeugeOffeneAsync`), legt aber **keinen** Auftrag an (Meldung „Kein aktiver Standardbereich“ nur beim Freigebenden).
  Die Entscheidung Q10 sah „Transaktion aus Q13 bleibt für den Wechsel“ vor; umgesetzt ist sie nicht (Begründung im Spec-Nachtrag v1.89.0:
  „nie zwei Standards“). Folge zusätzlich mit K-4: FA kann später „vollständig“ werden, obwohl diese Positionen nie einen Auftrag hatten.
- **Vorschlag:** Knopf „Als Standard setzen“, der alten und neuen Standard in **einer** Transaktion tauscht (zwei `SaveChanges` wegen
  gefiltertem UNIQUE-Index, Q2); Abwählen des letzten Standards wieder sperren (ADR 0020-Maske der Spec: „den letzten Standard nicht abwählbar“).
  Entscheidung Gerald.
- **DEV-Nachmessung 2026-11-14:** reproduziert. AKE-DEV: Lager als Standard abgewählt (Warnung „keinen aktiven Standardbereich“), dann FA 2607171 (Id 144) freigegeben → Meldung „Kein aktiver Standardbereich …“ + „0 Kommissionierauftrag/-aufträge für 0 FA angelegt“, FA trotzdem freigegeben (Prio 42, Kommissionierer gesetzt), kein Auftrag (Screenshot `B-1-freigegeben-ohne-kommissionierauftrag.jpg`). Erholung: Standard wieder gesetzt, „Fehlende Kommissionieraufträge erzeugen“ → KO-2026-00020/-21/-22. Bericht [[01-kommissionierung]].

## B-2 – Druck-Fälligkeit „Vorkommissionierung“ ohne Werkbank-Override
- **Schwere:** niedrig
- **Fundstelle:** `IdealAkeWms/Controllers/PickingController.cs:631-639` (liest `order.ProductionWorkplace?.OverridePrePickingDays`),
  `IdealAkeWms/Data/Repositories/PickingOrderRepository.cs:163-165` (`GetMitZeilenAsync` lädt `ProductionOrder` **ohne** `ProductionWorkplace`;
  kein Lazy Loading im Projekt).
- **Befund:** Im Druck ist `ProductionWorkplace` immer `null` → `PrePickingDaysResolver.Resolve(null, global)` nimmt den globalen
  `VorkommissionierTage`. Die Auftragsliste (`GetListeAsync` mit `ThenInclude(ProductionWorkplace)`) nutzt den Override (v1.27.0).
  Bei Werkbänken mit abweichenden Vorkommissioniertagen zeigt der Ausdruck ein anderes Fälligkeitsdatum als die Liste.
- **Vorschlag:** `ThenInclude(p => p.ProductionWorkplace)` in `GetMitZeilenAsync`; Test „Druck Vorkommissionierung mit Override“.

## B-3 – Deaktivieren/Löschen ohne Hinweis
- **Schwere:** niedrig
- **Fundstelle:** `IdealAkeWms/Controllers/PickingAreasController.cs:60-82` (Deaktivieren), `:192-201` (`RegelLoeschen`, harte Löschung),
  `IdealAkeWms/Views/PickingAreas/Regeln.cshtml:59-63` (Knopf ohne Rückfrage).
- **Befund:** Ein Bereich mit offenen Aufträgen lässt sich ohne Hinweis deaktivieren (Aufträge bleiben dort, neue Zeilen gehen in den Standard
  mit Meldung). Regeln werden per Klick ohne Rückfrage hart gelöscht; danach ist nicht nachvollziehbar, wer welche Regel entfernt hat (die Regel trägt
  zwar Audit-Felder, verschwindet aber).
- **Vorschlag:** Beim Deaktivieren Anzahl offener Aufträge melden; Löschen mit `confirm` und Log-Eintrag (Benutzer, Typ, Wert, Bereich).

## B-4 – Veralteter Kommentar zum Standard-Schutz
- **Schwere:** Hinweis
- **Fundstelle:** `IdealAkeWms/Program.cs:357-359`.
- **Befund:** „Nur bei leerer Tabelle – die Pflege schützt den Standard (PickingAreasController)“. Seit v1.89.0 (Q10) erlaubt die Pflege das
  Abwählen des einzigen Standards (`PickingAreasController.cs:74-80`). Der Seed greift trotzdem nicht (Tabelle nicht leer) – Kommentar irreführend.
- **Vorschlag:** Kommentar nachziehen (zusammen mit B-1).

## B-5 – Glas an AKE nur über Artikel-Regeln
- **Schwere:** Hinweis
- **Fundstelle:** `IdealAkeWms/Services/KommissionierbereichRegel.cs:77-98`; Spec Ist-Stand „AKE-Glas: `HasGlass` ist ein manuelles FA-Häkchen“.
- **Befund:** Der Bereich „Glas“ greift nur, wenn je Glas-Artikel (oder Artikelgruppe/Lagerort) eine Regel gepflegt ist. Die Regel „Lagerort“
  greift auf DEV nie (E0 P2: `Lagerkennung` leer). Ein FA mit Häkchen „Glas“ im Leitstand bekommt ohne Regeln nur einen Lager-Auftrag – kein Fehler,
  aber für Anwender nicht offensichtlich.
- **Vorschlag:** In der Anwender-Anleitung (Teil C) erklären; Regeln je Standort vor Go-live pflegen (Prüfabfrage P2 der Spec auf Produktion).

## Geprüft ohne Befund
- Rangfolge Artikel → Kommissionierziel → Lagerort → Artikelgruppe → Standard, Regel auf inaktivem Bereich → Standard **mit** Meldung
  (`KommissionierbereichRegel.cs`, Tests `Bereichsregel_*_AK4`).
- Maske nach ADR 0020: Regeltyp feste Auswahl (`Enum.IsDefined`), Wert aus Stammdaten (Artikel über Id, Ziel/Lagerort/Gruppe gegen Stammliste),
  Eindeutigkeit (Typ, Wert) normiert (Trim, ohne Groß/Klein) plus UNIQUE-Index; Name/Kürzel Pflicht, Länge, eindeutig; Standard muss aktiv sein.
- Rollen: Klasse `[RequireMasterDataReadAccess]`, Schreibaktionen `[RequireMasterDataAccess]` + Antiforgery; `CanEdit` steuert die Knöpfe.
- Listen: Bereiche Client-Mode (klein, unpaginiert), Regeln Server-Mode mit Pagination und `data-col-key` (ADR 0005).
- Audit beim Anlegen/Bearbeiten (Bereich, Regel) gesetzt; Seed in SQL/133 und `Program.cs` nur bei leerer Tabelle.
- Fälligkeit Vorkommissionierung in der Auftragsliste: Kommissioniertermin minus Vorkommissioniertage inkl. Werkbank-Override (wie Leitstand).

## Tippfehler
- keine in diesem Bereich (siehe [[2026-11-14-review-befunde-kommissionierung]] für die Funde in Leitstand/Stückliste).

## Sichtung 2026-11-28 (gegen main v1.99.89)

Teilweise: B-5 (Doku ja, Regelpflege vor Go-live). Offen: B-1, B-2, B-3, B-4.
