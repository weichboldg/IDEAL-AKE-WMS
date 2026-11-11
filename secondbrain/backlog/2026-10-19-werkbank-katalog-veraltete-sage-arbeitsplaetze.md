---
typ: bug
---
# Werkbank-Katalog: veraltete Sage-Arbeitsplätze machen Kürzel mehrdeutig -> keine BDE-Arbeitsgänge

**Aufgenommen 2026-10-19** (Gerald: BDE-Terminal „Entgraten Fläche“ zeigt für FA 1059463 keine Arbeitsgänge).

## Befund (Gerald, IDEAL-Test, 2026-10-19)

- Sub-FA 1065150 (offen, nicht kommissioniert-fertig) hat nur den Arbeitsgang **ST** (Werkbank 63 „Stanzen“, BDE aus).
  **EG** und **KA** fehlen, obwohl die Struktur „ST EG KA“ liefert (Teil 50000404).
- Kürzel **EG** steht an **zwei** Werkbänken: Id 19 „Flächen entgr beendet“ (Sage-Nr. 2150, BDE aus) und Id 65
  „Entgraten Fläche“ (Sage-Nr. 140, BDE aktiv).
- Sync-Protokoll `WorkOperationStructureDetection` (2026-10-19 08:13): **Mehrdeutig (8): EG (93 Aufträge), KA (94),
  PL (3), SL (16), SÄ (17), VL (3), VM (58), VP (3)** – kein Arbeitsgang angelegt. **Unbekannt (3): MK (15), ZS (5),
  MO (3).**

## Ursache (Koordination, am Code belegt, Bündel v1.62.0)

- Die Struktur-Erkennung nimmt als Katalog **alle** Werkbänke mit `ArbeitsschrittCode`
  (`WorkOperationStructureDetectionService.cs:81-88`); trifft ein Kürzel mehrere, legt sie nichts an (`:118-122`, `:194-203`).
- Die Werkbank-Anlage legt an und zieht nach, **deaktiviert oder entfernt aber nie** Werkbänke, die nicht mehr in der
  Quelle stehen. Seit der Umstellung auf `vw_IDEAL_AKE_WMS_Arbeitsgaenge` (27 Zeilen, 1 Arbeitsplatz je AG) bleiben die
  aus `KHKPpsArbeitsplaetze` angelegten Werkbänke mit ihren Kürzeln stehen (WMS: 87 Werkbänke mit Kürzel).
  **Vermutung, ungeprüft:** Id 19 (Sage 2150) steht nicht mehr in der View -> Prüfabfragen unten.
- Bestehende Arbeitsgänge werden nie umgehängt („nur hinzufügen“, `:165`).

## Prüfabfragen

```sql
-- WMS: Werkbaenke der mehrdeutigen Kuerzel
SELECT ArbeitsschrittCode, Id, Name, SageArbeitsplatznummer, BdeAktiv
FROM dbo.ProductionWorkplaces
WHERE LTRIM(RTRIM(ArbeitsschrittCode)) IN ('EG','KA','PL','SL','SÄ','VL','VM','VP')
ORDER BY ArbeitsschrittCode, SageArbeitsplatznummer;

-- Sage: was liefert die View fuer diese Kuerzel
SELECT AG, Arbeitsgangnummer, Bezeichnung FROM dbo.vw_IDEAL_AKE_WMS_Arbeitsgaenge
WHERE AG IN ('EG','KA','PL','SL','SÄ','VL','VM','VP','MK','ZS','MO') ORDER BY AG;
```

## Vorschlag für die Spec (Koordination)

> **Erweitert 2026-10-19:** Gerald will denselben Arbeitsgang an mehreren Werkbänken stempeln können -> gemeinsam mit
> [[2026-10-19-bde-lastenheft-abgleich]] („Neue Anforderung“) in einer Spec. Mehrdeutigkeit ist dann kein Ausschlussgrund mehr.

1. Werkbank-Anlage: Sage-Werkbänke, deren Sage-Nr. nicht mehr in der Quelle steht, kennzeichnen (z. B. `SageMissingSince`,
   Muster Arbeitsgang-Abgleich) und **aus dem Kürzel-Katalog der Struktur-Erkennung nehmen**. Nie löschen.
2. Struktur-Erkennung: Katalog nur aus Werkbänken, die aktuell in der Quelle stehen.
3. Offene, nie gebuchte Arbeitsgänge an einer gekennzeichneten Werkbank auf die eindeutige aktuelle Werkbank umhängen
   (Rückfrage: automatisch oder nur melden).
4. MK, ZS, MO: Sage-Pflege (Arbeitsplatz mit passendem `USER_ArbeitsSchritt` bzw. View).

## Bestätigung (Gerald, 2026-10-19)

- WMS: je mehrdeutigem Kürzel **zwei** Werkbänke – eine aktuelle (3-stellige Sage-Nr., z. B. EG 140, KA 150, PL 320,
  SÄ 240, SL 161, VL 260, VM 230, VP 330) und eine alte „W1“-Werkbank (4-stellig: 2150, 2300, 2550, 2900, 2700, 9000,
  3002, 8000), alle alten mit BDE aus. Nur „Entgraten Fläche“ (65) ist BDE-aktiv.
- Sage-View `vw_IDEAL_AKE_WMS_Arbeitsgaenge` liefert je Kürzel **genau eine** Nummer, nämlich die aktuelle (140, 150, 320,
  240, 161, 260, 230, 330). **MK, ZS, MO stehen nicht in der View.**
- Ursache damit bestätigt: Die alten Werkbänke stammen aus der früheren Quelle `KHKPpsArbeitsplaetze` und werden von der
  Werkbank-Anlage nie gekennzeichnet; sie machen die Kürzel mehrdeutig.
- Achtung für Punkt 3: Bereits angelegte Arbeitsgänge (Log „163 übersprungen“) können an alten W1-Werkbänken hängen.
