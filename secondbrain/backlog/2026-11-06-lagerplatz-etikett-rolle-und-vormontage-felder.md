# Lagerplatz-Etikett 93 × 55 mm (Rollendrucker) und neue Felder in den Vormontage-Listen

**Aufgenommen 2026-11-06** (Gerald, über die Koordination). **Beide Teile in einer Spec und einem Dev-Lauf** (Wunsch Gerald).
Code-Grundlage: Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (v1.71.1), nur gelesen.

## Teil 1 – zusätzliche Etikettengröße für den Lageretikettendruck

Gerald: „Druckprozessor für zusätzliche Etikettengröße – benötigt für Lageretikettendruck. 9,3 × 5,5 Etiketten für Rollendrucker,
im Standort einstellbar.“

Ist-Stand:
- Lagerplatz-Etiketten werden heute über die Browser-Druckansicht gedruckt: `Views/StorageLocations/PrintLabels.cshtml`
  (A4 hoch, `@page size: A4 portrait` Z. 123-126, Etikett 85 mm hoch Z. 61-73, Seitenumbruch nach je 3 Etiketten Z. 145).
  Aufrufe: „Alle Etiketten drucken“ (`StorageLocations/Index.cshtml:17`, `PrintLabels`) und je Lagerplatz (`:127`, `PrintLabel`);
  Controller `StorageLocationsController.cs:157-186`, Barcode Code128 (`:188-197`).
- `Services/PrintService.cs` (rundll32/mshtml) wird von keinem Controller benutzt (grep `IPrintService` in `Controllers/`: kein Treffer).

Soll:
- Neues Format **93 × 55 mm, ein Etikett je Seite** (Rollendrucker), Inhalt wie heute (Code, Beschreibung, Barcode, Zone),
  passend skaliert. Das bisherige A4-Format bleibt.
- Auswahl des Formats als **Standort-Einstellung** (Standorteinstellungen, `Models/Standort/StandortSettingsCatalog.cs`),
  Standard = heutiges A4-Format (keine Verhaltensänderung ohne Umstellen).

Offen für den Spec-Agent: „Druckprozessor“ – gemeint ist vermutlich die Druckansicht/das Layout (Browser-Druck), kein
serverseitiger Druck. Bitte als Rückfrage mit Empfehlung „Browser-Druckansicht“ stellen.

## Teil 2 – neue Felder in den Vormontage-Listen

Gerald: „In den Vormontagelisten benötigen wir noch folgende Felder: HauptFA, [BemerkungPN], [FertigungsInfoPN], Kunde,
[FE_Termin], Abteilung. Wenn neue Felder, dann immer alle Vormontagelisten gleich und über Settings ein-/ausblendbar machen.“

Ist-Stand:
- Zwei Sichten: `Views/FaHierarchyVormontage/Index.cshtml` (Einzeln, `viewKey` `FaHierarchyVormontageEinzeln`) und
  `Summiert.cshtml`. Spaltenwahl je Benutzer über `column-config` + `column-preferences.js` (Index Z. 150-166).
- Einzeln: Spalten hauptfa, hauptartnr, artnr, matchcode, sollmenge, fertigungmenge, vmbedarf, material (Z. 75-82).
  Gruppenkopf je HauptFA zeigt `MontageAbteilung` und `Kunde` (Z. 101-125), aber nicht als Spalte.
- Summiert: nur matchcode, sollmenge, fertigungmenge (Z. 80-82, 114-116) – HauptFA fehlt dort (Summierung über Matchcode).
- Datenquellen: `BemerkungPN`, `FertigungsInfoPN`, `Arbeitsbereich` je Strukturzeile (`Models/FaHierarchyNode.cs:37-40`);
  `MontageAbteilung`, `Kunde`, `FE_Termin` je HauptFA (`Models/FaHierarchyOrderInfo.cs:21, 25, 32`).

Soll:
- Die sechs Felder in **allen** Vormontage-Listen gleich, jeweils ein-/ausblendbar.

**Entscheidung Gerald 2026-11-06:** Die Entwurfs-Spec [[2026-10-06-vormontage-abteilung-spalte-filter-spec]] wird in die
neue Spec **aufgenommen** (ersetzt sie). Ihr Inhalt – Spalte `arbeitsbereich` je Position, Label „Arbeitsbereich“, Server-Spaltenfilter,
Position nach HauptFA/Matchcode, Filter in Summiert vor dem Zusammenzählen, Keyuser-Antworten 2026-10-06/07 und Prüfabfrage P5 –
ist Teil dieses Auftrags. Die alte Spec wird danach nicht mehr umgesetzt.

Offen für den Spec-Agent (Rückfragen mit Empfehlung):
1. **„Abteilung“ – welches Feld?** `MontageAbteilung` (FAInfos, je HauptFA, heute im Gruppenkopf) oder `Arbeitsbereich`
   (Sage `USER_OSAbteilung`, je Position)? **Achtung Überschneidung:** Die Entwurfs-Spec
   [[2026-10-06-vormontage-abteilung-spalte-filter-spec]] plant bereits eine Spalte `arbeitsbereich` (Label „Arbeitsbereich“,
   Keyuser 2026-10-07, „Abteilung“ dort verworfen). Empfehlung prüfen: diese Spec in die neue aufnehmen (ersetzen) statt
   zwei Läufe auf dieselben Views.
2. **„Über Settings ein-/ausblendbar“** – die vorhandene Spaltenwahl je Benutzer (column-preferences) oder ein
   Standort-Schalter je Feld? Empfehlung: vorhandene Spaltenwahl je Benutzer, neue Spalten standardmäßig sichtbar.
3. **Summiert-Sicht:** Felder je HauptFA bzw. je Position (HauptFA, Kunde, FE_Termin, BemerkungPN …) passen nicht zu einer
   Summe über Matchcode. Gruppierschlüssel erweitern, Werte zusammenfassen (z. B. „mehrere“) oder dort nur einzelne Felder?
4. Gehören auch Druckansichten/Exporte der Vormontage zu „allen Vormontagelisten“? (Im Ordner gibt es nur Index und Summiert;
   beim /spec auf weitere Aufrufe prüfen.)
5. Befüllung von `BemerkungPN`/`FertigungsInfoPN` in den IDEAL-Daten ungeprüft – Prüfabfrage für Gerald formulieren.

Version und Testkapitel vergibt der Dev-Lauf.
