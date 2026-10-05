---
type: changelog
version: 1.45.0
date: 2026-09-24
---
# v1.45.0 — FA-Struktur: erkannte Arbeitsgänge je (Sub-)FA im Modal

Umsetzung von [[2026-09-23-fa-struktur-arbeitsgaenge-anzeige-spec]] im **Bündel-**Worktree
`feature/2026-08-07-ideal-teile-1-5`, Umsetzungsnotiz [[2026-09-24-fa-struktur-arbeitsgaenge-anzeige-umsetzung]].
Setzt v1.44.0 ([[2026-09-22-v1-44-0-ideal-bde-arbeitsgaenge-werkbank-anlage]]) voraus. **Keine Migration.**

**Warum:** Nach v1.44.0 legt die Struktur-Erkennung `WorkOperation`-Zeilen an, aber sichtbar war das nur
zeilenweise im SyncLog. Die Frage aus der Fertigung ist „welches Teil hat **gar keinen** Arbeitsgang" —
deshalb zeigt das Modal alle Sub-FAs und benennt die Lücken, statt nur Treffer zu listen.

## Kernpunkte

- Knopf **Arbeitsgänge** am Ende der Struktur-Kopfzeile, nur bei ≥ 1 erkanntem Arbeitsgang (sonst gar
  nicht gerendert — kein Layout-Sprung).
- **Zweistufig:** Seitenaufbau = ein Existenz-Check (`GetOrderNumbersWithWorkOperationsAsync`, nur
  Schlüssel); Modal-Inhalt erst beim Öffnen per `GET /FaHierarchy/WorkOperations?hauptFa=` (Partial,
  nur die Knoten dieser HauptFA über den gecachten `GetByHauptFaAsync`).
- Modal: Zählzeile „N Sub-FAs · M ohne Arbeitsgang", HauptFA-Abschnitt (Wurzel), alle Sub-FAs mit
  Kürzel/Werkbank/Sage-Nr.; Lücken grau mit „— kein Arbeitsgang". Ladezustand, Fehlermeldung, nie leer.
- **Review-Fix:** Modal lädt per `OrderNumber == HauptFA` — derselbe Schlüssel wie der Knopf. Arbeitsgänge
  einer Sub-FA, die nicht (mehr) im Baum steht (`SageMissingSince`, Tiefen-Cap, Zyklus), erscheinen als
  gelbe Warnung statt still zu verschwinden (sonst: Knopf da, Modal nur Striche).
- **AK 10 gemessen:** `Contains` über 5000 Schlüssel → ein `OPENJSON`-Parameter ([[fallstricke]] §8).

## Commits

`912e6bea` (Repository + Tests), `71ffeb18` (feature-complete), `2d06d1f0` (Review-Fix). Merge-Commit: folgt mit dem Bündel.

## Deploy-Risiken

Nur Web-Publish. Keine Migration, kein Service. Risiko gering (Leseansicht).
