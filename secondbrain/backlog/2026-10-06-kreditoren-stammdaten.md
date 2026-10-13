---
typ: feature
---
# Neuer Stammdatenbereich: Kreditoren (Dienstleister, Beschichter)

**Aufgenommen 2026-10-06** (Anforderung Gerald, Koordination).

## Anforderung (Gerald)

- Neuer Stammdatenbereich für Kreditoren mit allen wichtigen Stammdaten (Adresse, Mail usw.).
- Den Sync dafür erweitern wir in Zukunft.
- In der WMS-Oberfläche soll der Dienstleister (Beschichter) editierbar sein.

## Ausgangslage (am Code belegt, Bündel-Worktree)

- Es gibt **keine** Kreditor-/Lieferanten-Entität: Suche nach `Kreditor|Supplier|Lieferant|Dienstleister`
  in `Models` und `Controllers` trifft nur `FaHierarchyOrderInfo` und `WarehouseRequisitionItem`.
- Der Dienstleister ist heute ein **Freitext aus Sage** (`FaHierarchyOrderInfo.Dienstleister`, gefüllt aus
  der View `FAInfos`); er erscheint im Beschichtungsauftrag (`Views/FaHierarchyBeschichtung/Print.cshtml`).

## Für die Spec zu klären (Empfehlungen der Koordination in Klammern)

1. **Felder:** Name, Kurzname, Adresse (Straße, PLZ, Ort, Land), E-Mail, Telefon, Ansprechpartner,
   Bemerkung, aktiv/inaktiv, **Sage-Kreditorennummer** als Schlüssel für den späteren Sync.
2. **Zuordnung zum Sage-Freitext `Dienstleister`:** wie wird ein Text einem Kreditor zugeordnet
   (Kurzname exakt, Zuordnungstabelle, manuell)? Nicht-zuordenbare Texte sichtbar melden, nicht still
   übergehen.
3. **Datenhoheit:** bis zum Sync führt das WMS; danach Sage führend? (Muster ADR 0014: Sage führend mit
   Abweichungsmeldung.) Die Sage-Quelle für den Sync ist **ungeprüft** – Tabelle/View ermittelt Gerald.
4. Muster-Pflichten: `AuditableEntity`, Migration dreifach (ADR 0004), Listen-View nach ADR 0005,
   Zugriff über neue oder bestehende Stammdaten-Rolle (bei neuer Rolle: drei Stellen laut CLAUDE.md).
5. **Standortbezug:** gelten Kreditoren für AKE und IDEAL gemeinsam oder je Standort?

## Bezug

Voraussetzung für den Dienstleister-Teil von [[2026-10-06-beschichtungsauftrag-ausbau]].
