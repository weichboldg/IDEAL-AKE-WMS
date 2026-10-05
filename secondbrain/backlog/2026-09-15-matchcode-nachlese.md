---
typ: feature
---
# Matchcode-Nachlese — Folge-Arbeiten aus v1.40.0

Herausgeloest aus [[2026-09-13-matchcode-artikelstamm-spec]] (umgesetzt v1.40.0). Drei bewusst
verschobene Punkte, jeder eigenstaendig, keiner blockierend.

## 1. Matchcode auch in OSEON-Teileverfolgung und BOM-Komponentenebene

Beide zeigen bislang keinen Matchcode (In-Scope-Grenze v1.40.0):
- **OSEON** (`OseonProductionOrderRepository`/`OseonGroupViewModelBuilder`): eigene Sync-Pipeline ohne
  bestehenden Article-Join — ein Join waere zusaetzliche, ungepruefte Kopplung.
- **BOM-Komponentenebene** (`Picking/Bom.cshtml`, `PrintBom`, `PrintPicking`, `WarehousePicking/Details`):
  von der Schwester-Spec (2026-09-10, Antwort 7) bereits ausgeschlossen.

## 2. FA-Struktur-Baum auf `Article.Matchcode` umstellen

Der Baum (`/FaHierarchy`) liest weiter `FaHierarchyNode.Matchcode`, die Listen `Article.Matchcode`
(v1.40.0). Solange beide aus demselben Sage-Feld stammen, unsichtbar; weichen sie ab, zeigen Baum und
Listen fuer denselben Knoten unterschiedliche Werte (so gewollt, Artikel gewinnt in den Listen). Den Baum
spaeter ebenfalls auf den Artikelstamm umzustellen macht es einheitlich. Details: [[fallstricke]] §12.

## 3. `Articles`-Sync um gefertigte Artikel erweitern — WENN der Fehltreffer-Zaehler > 0 zeigt

`Articles` ist eine gefilterte Projektion von `KHKArtikel`; gefertigte Endgeraete (HauptFA-Artikel)
koennen fehlen → Matchcode-Join liefert `NULL`. Der AK-14-Fehltreffer-Zaehler (Log, eine Zahl je
Listenaufbau) macht das beim ersten echten Lauf sichtbar. **Zeigt er im Betrieb > 0:** den
`SageImportService.SyncArticlesAsync`-Filter erweitern, damit gefertigte Artikel aufgenommen werden — das
repariert zugleich die Artikelinfo, die solche Artikel heute ebenfalls nicht findet. Ursache beheben,
nicht am Auftrag duplizieren. Details: [[fallstricke]] §12.

## Bezug

Spec [[2026-09-13-matchcode-artikelstamm-spec]] · Changelog
[[2026-09-15-v1-40-0-matchcode-artikelstamm]] · [[fallstricke]] §12.
