---
typ: feature
---
# IDEAL: Sage-Zugriffe auf die neuen WMS-Views umstellen (FAInfos, FAListe, Arbeitsgänge, Artikel)

**Aufgenommen 2026-10-14 (Gerald).** In der **Live-Umgebung IDEAL** (Sage-DB) sind vier Views angelegt. Einige Selects,
die heute Sage-Tabellen direkt abfragen, sollen auf diese Views umgestellt werden.

## Anforderung (vom Menschen, wörtlich zusammengefasst)

1. **`vw_IDEAL_AKE_WMS_FAInfos`** — gleiche Datenstruktur wie bisher; alle Zusatzinformationen aus AV bzw. PPS (z. B.
   Termine, Kunden, Infos zu Beschichtung). „Es muss nur der Pfad geändert werden."
2. **`vw_IDEAL_AKE_WMS_FAListe`** — gleiche Datenstruktur wie bisher; Stücklisten der zu fertigenden Geräte inkl.
   Kommissionier- und Arbeitsanweisungen. „Es muss nur der Pfad geändert werden."
3. **`vw_IDEAL_AKE_WMS_Arbeitsgaenge`** — neue View, ersetzt den Select auf `[dbo].[KHKPpsArbeitsplaetze]`
   (Spalten Arbeitsplatznummer, Mandant, Bezeichnung1/2, Matchcode, Memo, Schichtplannummer, Wartezeit…, Leistungsgrad,
   Kostenfelder, Kostenstelle/-träger, Pausenabzug, Fertigungsgemeinkostenzuschlag, NurBarcode, Aktiv, BdeTerminalId,
   Timestamp, USER_ArbeitsSchritt, USER_PufferZeit).
4. **`vw_IDEAL_AKE_WMS_Artikel`** — Infos zu allen Artikeln, die in den FAs verwendet werden oder Beschaffungsartikel sind.
   „Ersetzt aus meiner Sicht `[dbo].[Articles]`" (die per Artikelsync aus Sage befüllt wird). **Behebt das Problem der
   00er-Artikelnummern.**

## Ist-Stand (Koordination, Bündel `d4108f1a`, nur gelesen)

- FAListe/FAInfos: View-Namen sind **Einstellungen** `Sync:FaHierarchyListeViewName` / `Sync:FaHierarchyInfosViewName`
  (`Models/ServiceSettingDefinitions.cs:38-39`, Standort-Katalog `StandortSettingsCatalog.cs:44-45`, Default
  `[vw_IDEAL-AKE_Kommissionierung_FAListe]`/`…FAInfos]`, Fallback hart in `FaHierarchySyncService.cs:53/56`).
  → Bei identischer Spaltenstruktur **reine Einstellungsänderung, kein Code** (Defaults/Fallback/Doku ggf. nachziehen).
- Arbeitsplätze: `IDEALAKEWMSService/Services/SageArbeitsplatzReader.cs:25-29` liest
  `Arbeitsplatznummer, Bezeichnung1, USER_ArbeitsSchritt FROM KHKPpsArbeitsplaetze WHERE Mandant=@mandant AND Aktiv=-1`
  (Werkbank-Sync, gated `Sync:ProductionWorkplaceSyncEnabled`). → Tabellenname durch View ersetzen; Mandant/Aktiv-Filter
  müssen in der View als Spalten vorhanden sein.
- Artikel: Artikel-Sync `IDEALAKEWMSService/Services/SageImportService.cs:369` (`SyncArticlesAsync`), Sage-Select
  `:382-421` = UNION aus `KHKPpsRessourcenPositionen`(→`KHKArtikel`) und Bestellartikeln, schreibt in die WMS-Tabelle
  `Articles`. Läuft für **AKE und IDEAL** (`Sync:ArticlesEnabled`). → Die View ersetzt vermutlich die **Quelle** des
  Syncs (den Sage-Select), **nicht** die WMS-Tabelle `Articles` (FKs, Matchcode, Bestand hängen daran) — Rückfrage.
- Weitere direkte Sage-Artikelzugriffe: `SageBestandReader.cs:29-30` (`KHKArtikel`/`KHKArtikelVarianten`) — prüfen, ob
  mit umzustellen.

## Offen / Rückfragen an Gerald

- Was genau ist das **„Problem der 00er-Artikelnummern"**? (im Brain nicht dokumentiert)
- Gibt es die Views auch in der **Sage-Test-DB** des IDEAL-Testsystems? Sonst bricht der Sync dort nach der Umstellung.
- AKE bleibt bei den Tabellen (keine Views dort)? → Umstellung je Standort einstellbar.
