---
typ: feature
---
# Vormontage-Listen: Abteilung als Spalte und Filter

**Aufgenommen 2026-10-06** (Anforderung Gerald, Koordination).

## Anforderung (Gerald)

In den VM-Listen soll die Abteilung als Spalte bzw. Filter angezeigt werden.

## Ausgangslage (am Code belegt, Bündel-Worktree)

- Vormontage-Ansichten: `Views/FaHierarchyVormontage/Index.cshtml` (Einzelteile) und `Summiert.cshtml`.
  Spalten heute (`data-col-key`): `hauptfa`, `hauptartnr`, `artnr`, `matchcode`, `material`,
  `sollmenge`, `fertigungmenge`, `vmbedarf` – **keine Abteilung**.
- Die Montage-Abteilung liegt **nicht am Strukturknoten** (`FaHierarchyNode` hat kein Feld dafür),
  sondern am Auftrag: `FaHierarchyOrderInfo.MontageAbteilung`, verknüpft über `HauptFA`.
- **Kombinationsgeräte:** ein HauptFA kann mehrere Abteilungen haben; ein Join an die Positionszeilen
  verdoppelt Positionen. Genau das ist offen in [[2026-08-06-kombinationsgeraete-montageabteilung]].

**Nachtrag 2026-10-06 (Sage-Screenshot, siehe [[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]):**
In Sage steht eine Spalte „Abteilung“ **je Strukturzeile** (z. B. `H1-03`, `S-01`, `H2-02`, `K-02`).
`FaHierarchyNode` hat ein Feld `Arbeitsbereich` (`Models/FaHierarchyNode.cs:40`). Möglich ist, dass mit
„Abteilung“ in den VM-Listen **dieses Knotenfeld** gemeint ist und nicht die auftragsbezogene
`MontageAbteilung` – dann entfällt das Kombinationsgeräte-Problem. **Erste Frage an Gerald:** Welche
Abteilung ist gemeint? Prüfabfrage siehe die verlinkte Notiz.

## Für die Spec zu klären (Empfehlungen der Koordination in Klammern)

1. **Kombinationsgeräte:** Was zeigt die Spalte, wenn ein HauptFA mehrere Abteilungen hat (alle,
   kommagetrennt – Empfehlung), und wie verhält sich der Filter (Treffer, wenn eine davon passt)?
   Positionen dürfen dabei nicht verdoppelt werden.
2. **Summierte Ansicht:** Abteilung nur Anzeige oder Teil des Summierschlüssels?
3. Spaltenfilter im Server-Mode (ADR 0005), Spaltenauswahl-Registrierung ([[fallstricke]] §3).
4. Bezug: Löst diese Notiz [[2026-08-06-kombinationsgeraete-montageabteilung]] teilweise mit, oder
   bleibt die Trennung dort ein eigener Schritt?

## Hinweis zur Reihenfolge

Baut auf Teil 5 im Bündel auf – Umsetzung nach Schranke 2 des Bündels.
