---
type: spec
title: Artikel-Abgleich – IDEAL-Sperre P-R1 für SData abschaltbar
slug: 2026-11-28-artikel-sdata-sperre-p-r1-abschaltbar-spec
status: Entwurf
created: 2026-11-28
updated: 2026-11-28
source_backlog: "[[2026-11-28-artikel-sdata-sperre-p-r1-abschaltbar]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: IDEAL
affected_code:
  - IdealAkeWms/Services/SageSdata/SageQuelleSperre.cs
  - IdealAkeWms/Models/ServiceSettingDefinitions.cs
  - IdealAkeWms/Views/ServiceSettings/Index.cshtml
  - IDEALAKEWMSService/Services/Sdata/ArtikelQuelleSwitch.cs
  - IDEALAKEWMSService/Services/SageImportService.cs
  - IDEALAKEWMSService.Tests/Services/Sdata/ArtikelSdataTests.cs
open_questions:
  - "Artikel, die SData liefert, die View aber nicht (neue Articles-Zeilen): A anlegen wie heute, B bei aufgehobener Sperre gar nicht neu anlegen, C nur anlegen, was auch die View liefert (View nicht lesbar = keine Neuanlage)? Empfehlung C."
  - "Blechteile-Felder-Nachlese (V-B) läuft im SData-Modus heute nicht (SageImportService.cs:566-577): bei aufgehobener Sperre mitlaufen lassen? Empfehlung ja."
  - "Einschaltbedingung IDEAL-Produktion: erst nach Prüfabfrage P-R1b bzw. einem Vergleichslauf am IDEAL-Test mit erklärter Mengen-Differenz? Empfehlung ja."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Heute wirkt `Sync:Quelle:Artikel = SData` an IDEAL immer wie `View`: Ist `Sync:ArticlesViewName` gesetzt, liefert
`SageQuelleSperre.Grund("Artikel", view)` den Grund „P-R1 …“ (`IdealAkeWms/Services/SageSdata/SageQuelleSperre.cs:22-23`),
`ArtikelQuelleSwitch.SdataGesperrtBisAsync` reicht ihn an die Weiche (`IDEALAKEWMSService/Services/Sdata/ArtikelQuelleSwitch.cs:43-49`), und
`SageQuelleSchalter.Parse` macht daraus View mit Warnung (`IDEALAKEWMSService/Services/Sdata/SageQuelleSchalter.cs:37-38`). Gerald will die Sperre
abschalten können („Deaktivieren soll auch möglich sein“, Chat 2026-11-28), z. B. damit `ArticleSageDetails` und die Benutzerfeld-Merkmale an
IDEAL gefüllt werden. Diese füllt nur der SData-Weg (`SageImportService.cs:556-572`).

Die Sperre hat einen belegten Grund, und genau den muss die Aufhebung sichtbar machen. Der SData-Reader liest an IDEAL **nicht „alle Artikel“**,
sondern die AKE-Menge: alle Ressourcenummern aus `PpsRessourcenPositionenInfo` ∪ `IstBestellartikel = -1 AND Aktiv = -1`
(`SdataArtikelReader.cs:78-82`, `:214-231`). Das ist dieselbe Menge wie der alte Select `SageArticleSql.LegacySql` (`SageArticleSql.cs:11-53`).
An IDEAL hat genau dieser alte Select die „00er“-Zeilen angelegt, die das Einmal-Skript
`SQL/Einmalig/2026-10-15_IDEAL-Sage-Views_Bereinigung00erArtikel.sql` gelöscht hat (Kopf `:16-20`, Warnung `:35`: „der alte Select legt die 00er mit
neuer Id wieder an“). Bleibt der Upsert unverändert, legt die Aufhebung diese Zeilen wieder an (Rückfrage 1).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neue ServiceSetting `Sync:ArtikelSdataSperreAufheben` (Bool, Default `false` = Sperre aktiv, Verhalten unverändert).
- Die Sperr-Regel bleibt an EINEM Ort (`SageQuelleSperre`, Fallstrick „Quellensperren an EINEM Ort“, `fallstricke.md:1834-1836`). Sie bekommt den
  Aufhebe-Wert als Parameter. Dienst-Weiche und `/ServiceSettings` lesen dieselbe Regel.
- Warnbanner in `/ServiceSettings`, solange die Sperre aufgehoben ist und eine Artikel-View gesetzt ist.
- Sammelmeldung (Warnung) je Lauf „Artikel“ bei aufgehobener Sperre und Quelle SData: Anzahl SData, Anzahl View, nur in SData, nur in View, bis
  zu 20 Beispielnummern. Ist die View nicht lesbar, steht das in der Meldung.
- Behandlung neuer Artikel, die nur SData liefert: nach Rückfrage 1.

**Out-of-Scope**
- Nachbau des IDEAL-View-Filters im `SdataArtikelReader` (bleibt P-R1).
- Die FA-Struktur-Sperre (`"FaStruktur"`, `SageQuelleSperre.cs:21`). Sie bleibt unverändert.
- AKE: Dort ist `Sync:ArticlesViewName` leer und SData ohnehin frei (`SageQuelleSperre.cs:22`). Der neue Schalter wirkt dort nicht.
- Löschen von Artikeln: Der Artikel-Abgleich löscht nie. Er kennt nur UPDATE/INSERT (`SageImportService.cs:463-500`). Der einzige
  `DELETE FROM dbo.Articles` im Repo steht im genannten Einmal-Skript (`:151`).

## Fachliche Anforderungen

1. **F1 Schalter.** `Sync:ArtikelSdataSperreAufheben = false` (oder Zeile fehlt) bedeutet: Das Verhalten ist zeichengleich wie heute (Sperre P-R1,
   Warnung im Lauf „Sage-Quellenvergleich“ und im Lauf „Artikel“, gelber Sperrhinweis an `Sync:Quelle:Artikel`).
   `true` bedeutet: Die P-R1-Sperre entfällt, `SData` wirkt an IDEAL als SData. `Vergleich` und `View` bleiben unverändert.
2. **F2 Fail-safe.** Ist der Schalter nicht lesbar (DB-Fehler), gilt die Sperre. Das ist dieselbe Regel wie heute bei unlesbarem
   `Sync:ArticlesViewName` (`ArtikelQuelleSwitch.cs:45-47`). Ein ungültiger Bestandswert gilt als `false`.
3. **F3 Banner `/ServiceSettings`.** Ist der Schalter `true` und `Sync:ArticlesViewName` gesetzt, steht oben auf der Seite ein Warnbanner (⚠ + Text,
   nicht nur Farbe): „P-R1-Sperre aufgehoben: Der Artikel-Abgleich liest bei Quelle SData ohne den Filter der Artikel-View '<View>' – auch Artikel
   außerhalb der View (u. a. 00er-Ressourcenummern). Zahlen je Lauf im Aktivitäts-Protokoll, Lauf „Artikel“.“ Steht `Sync:Quelle:Artikel` dabei
   nicht auf `SData`, sagt der Satz zusätzlich: „wirkt erst mit Quelle SData“. Der bisherige Sperrhinweis an `Sync:Quelle:Artikel`
   (`_SettingField.cshtml:202-206`) entfällt dann, weil die Regel `null` liefert.
4. **F4 Sammelmeldung.** Je Lauf „Artikel“ mit Quelle SData und aufgehobener Sperre genau eine Warnzeile, höchstens 1000 Zeichen (`SyncLogs.Message`):
   „P-R1-Sperre aufgehoben: SData ohne View-Filter – <s> Artikel aus SData, <v> in der View '<View>', <nurS> nur in SData, <nurV> nur in View.
   Beispiele nur SData: …“. Ist die View nicht lesbar oder fehlt sie, lautet die Zeile: „… <s> Artikel aus SData, Vergleich mit der View nicht
   verfügbar: <Grund>.“ Die Count-Keys des Laufs (`gelesen`, `neu`, `aktualisiert`, `hauptlagerplatz_fehlt`) bleiben unverändert
   (`SageImportService.cs:579-585`).
5. **F5 Neue Artikel nur aus SData.** *Vorbehaltlich Rückfrage 1 (Empfehlung C):* Bei aufgehobener Sperre legt der Upsert nur Artikel neu an, die
   auch die View liefert. Ist die View nicht lesbar, legt er keine Artikel neu an. Bestehende Artikel werden aus SData aktualisiert.
   Nicht angelegte Artikel stehen als Zahl in der Sammelmeldung („<n> nicht angelegt (nicht in der View)“).
   - Variante A: Der Upsert bleibt unverändert und legt alle an (kein Code, die 00er kommen zurück).
   - Variante B: Bei aufgehobener Sperre legt er gar nichts neu an.
6. **F6 Blechteile-Felder.** *Vorbehaltlich Rückfrage 2 (Empfehlung ja):* Die V-B-Nachlese (`BlechteileFelderNachlese.LaufAsync`) läuft bei
   aufgehobener Sperre auch mit Quelle SData. Heute schließt das `else if` sie im SData-Modus aus (`SageImportService.cs:566-577`). Der
   SData-Hauptweg liest die Blechteile-Felder nicht: `SdataArtikelReader.LesenAsync` liest nur `Sync:ArtikelBenutzerfelder`
   (`SdataArtikelReader.cs:205-254`), die Blechteile-Felder liest nur `BlechteileFelderLesenAsync` (`:340 ff.`). Ohne die Änderung würden
   IstLagerartikel, Material, Bestellmenge usw. an IDEAL nicht mehr aktualisiert.

## Technischer Loesungsentwurf

Muster: Quellen-Weiche [[0019-quellen-weiche-view-sdata-je-abgleich]], Sperr-Regel an einem Ort (`SageQuelleSperre`), ServiceSetting im Katalog
([[0008-servicesettings-db-first-mit-typisiertem-katalog]], [[0020-einstellungen-mit-festem-wertebereich-als-auswahl-und-mit-wirkungsbeschreibung]]).
Das Gerüst ist schon da: `SageQuelleWeiche`/`SageQuelleSchalter` bleiben unverändert, und `SdataVergleich.Vergleiche` (`SdataVergleich.cs:21-51`)
liefert die Zahlen für F4.

1. **`SageQuelleSperre` (Web-Projekt, vom Dienst referenziert).**
   - `public const string ArtikelSperreAufhebenKey = "Sync:ArtikelSdataSperreAufheben";`
   - `Grund(string bereich, string? artikelView, bool artikelSperreAufgehoben = false)`: Der Zweig `"Artikel" when …` greift nur bei
     `!artikelSperreAufgehoben`. Bestehende Aufrufer mit zwei Argumenten bleiben gültig.
   - `static string? AufhebungsHinweis(string? artikelView, bool aufgehoben, string? quelleArtikel)` erzeugt den Bannertext aus F3, sonst `null`.
     Der Bannertext steht damit an EINEM Ort; die Sammelmeldung F4 nennt dieselbe Ursache.
   - **Key bewusst NICHT unter `Sync:Quelle:`:** `ServiceSettingDefinitions.Ergaenzen` macht jeden `Sync:Quelle:*`-Key zur Auswahl View/SData/Vergleich
     (`ServiceSettingDefinitions.cs:260`), und `Index.cshtml:122-123` rechnet für ihn einen Sperrgrund.
2. **`ServiceSettingDefinitions`.** Neuer Basis-Eintrag in der Kategorie „Sage-Abgleich (SData)“ direkt nach `Sync:Quelle:Artikel` (`:355`), Typ Bool,
   Default `"false"`. Dazu ein `Meta`-Eintrag (Anzeigename, Verwendung, Standort, Warnhinweis), sonst schlägt der Drift-Guard fehl
   (`SettingValidatorTests.cs:145`). Den `Meta`-Standort von `Sync:Quelle:Artikel` (`:150`) ergänzen um „… IDEAL gesperrt bis P-R1 (aufhebbar über
   „Artikel über SData trotz Artikel-View“)“.
   Den Beschreibungstext von `Sync:Quelle:Artikel` (`:355`) ebenso ergänzen.
3. **`Views/ServiceSettings/Index.cshtml`.** Neben `artikelView` (`:9`) den wirksamen Wert des neuen Keys und von `Sync:Quelle:Artikel` lesen und an
   `Grund(…, aufgehoben)` (`:123`) übergeben. Ist `AufhebungsHinweis(…)` nicht null, steht oben das Banner `alert alert-warning`, `role="alert"`,
   `&#9888;` + Text. Muster wie `sageWeHinweis` (`:59-64`). Kein neues Partial, keine neue CSS.
4. **`ArtikelQuelleSwitch` (Dienst).**
   - `SdataGesperrtBisAsync`: Im bestehenden `try` (`:46`) zusätzlich den Schalter **ohne** Safe-Variante lesen
     (`ServiceSettings.GetBoolAsync(…, ArtikelSperreAufhebenKey, false, ct)`), dann `SageQuelleSperre.Grund(Bereich, view, aufgehoben)`. Ein DB-Fehler
     landet im vorhandenen `catch`, also gesperrt (F2). Für Tests eine virtuelle Lesemethode wie `LeseArtikelViewAsync` (`:52-53`).
     Den `TODO(P-R1)`-Kommentar (`:41`) nachziehen: Die Sperre ist aufhebbar, der Nachbau bleibt offen.
   - `GetAllAsync` (`:62-64`): Nach `LeseAsync` gilt: Ist `GenutzteQuelle == SData` und die Sperre wegen gesetzter View aufgehoben, liest es
     `_view.GetAllAsync` in einem eigenen `try`. Der Fehler wird nur Meldungstext. Dann berechnet es
     `SdataVergleich.Vergleiche(view.Artikel, sdata.Artikel, Schlüssel, Array.Empty<…>())`. Das Ergebnis (nur Schlüssel-Mengen) steht in einer
     neuen Eigenschaft `ViewFilterMeldung` (string?) und in `ViewSchluessel` (HashSet, `null` = View nicht verfügbar) für F5.
     `QuelleFehltMeldung` der View-Lesung bedeutet „nicht verfügbar“.
     *ponytail:* zweites Lesen der View je Lauf. Das ist dieselbe Abfrage wie im heutigen View-Betrieb und fällt weg, sobald P-R1 den Filter
     nachbaut.
5. **`SageImportService.SyncArticlesAsync`.**
   - `LogArtikelQuelleAsync` (`:598-608`): `ViewFilterMeldung` als Warnung loggen (F4).
   - F5 nach Rückfrage 1, Empfehlung C: Ist `ViewSchluessel` gesetzt bzw. der Modus „aufgehoben“, schreibt der Upsert für Nummern außerhalb der
     View-Menge nur den UPDATE-Zweig. Minimal: vor dem Upsert `if (nurUpdate && !bekannt) { nichtAngelegt++; continue; }`, mit einem einmaligen
     `SELECT ArticleNumber FROM Articles` als HashSet.
     Alternative ohne Lesen: ein Parameter `@InsertErlaubt` im ELSE-Zweig des vorhandenen SQL (`:489-499`). Der Dev-Lauf wählt die kleinere
     Änderung. Die Zahl geht in die Sammelmeldung, nicht in neue Count-Keys.
   - F6 nach Rückfrage 2: Die Bedingung `:574` lautet dann `GenutzteQuelle: not SData` **oder** Sperre aufgehoben. Dafür das `else if` in ein eigenes
     `if` umbauen, damit es auch nach dem Benutzerfeld-Schreiben (`:566-572`) läuft.
6. **Keine neue Rolle, kein neuer Filter.** `/ServiceSettings` ist `[RequireAdminAccess]` (`ServiceSettingsController.cs:19`).

**Prüfabfragen für den Menschen (Sage IDEAL; Viewtext unbekannt, Geralds Konto hat keinen Zugriff auf IDEAL_TEST2026).** Abhängig davon und daher
**unbelegt** sind: die tatsächliche Zahl „nur in SData“ an IDEAL, ob darunter nur 00er sind, und ob `PpsRessourcenPositionenInfo` an IDEAL
nicht leer ist (H2 der Ursprungs-Spec, Zeile 1427/1479; sonst wirft der Reader, `SdataArtikelReader.cs:222-223`).

- **P-R1a – Viewtext:** `SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.vw_IDEAL_AKE_WMS_Artikel')) AS Viewtext;`
- **P-R1b – Mengen** (gleiche Datenbank; die SData-Menge ist dem `LegacySql` nachgebildet, Abweichungen durch die SData-Ressource sind möglich):
  ```sql
  WITH S AS (
      SELECT CAST(r.Ressourcenummer AS nvarchar(100)) AS n FROM dbo.KHKPpsRessourcenPositionen r
      WHERE r.Ressourcenummer IS NOT NULL AND r.Ressourcenummer <> ''
      UNION
      SELECT CAST(a.Artikelnummer AS nvarchar(100)) FROM dbo.KHKArtikel a WHERE a.IstBestellartikel = -1 AND a.Aktiv = -1),
  V AS (
      SELECT DISTINCT CAST(ArticleNumber AS nvarchar(100)) AS n FROM dbo.vw_IDEAL_AKE_WMS_Artikel
      WHERE ArticleNumber IS NOT NULL AND CAST(ArticleNumber AS nvarchar(100)) <> '')
  SELECT
      (SELECT COUNT(*) FROM S) AS SData_Menge,
      (SELECT COUNT(*) FROM V) AS View_Menge,
      (SELECT COUNT(*) FROM S WHERE NOT EXISTS (SELECT 1 FROM V WHERE V.n = S.n)) AS nur_SData,
      (SELECT COUNT(*) FROM S WHERE S.n LIKE '00%' AND NOT EXISTS (SELECT 1 FROM V WHERE V.n = S.n)) AS nur_SData_00er,
      (SELECT COUNT(*) FROM V WHERE NOT EXISTS (SELECT 1 FROM S WHERE S.n = V.n)) AS nur_View;
  ```
- **Ohne Sage-Zugang erreichbar:** Am IDEAL-Test-WMS (:88) `Sync:Quelle:Artikel = Vergleich` setzen. Vergleich ist an IDEAL schon heute erlaubt
  (`SageQuelleSchalter.cs:36`) und schreibt nur aus der View (`SageQuelleWeiche.cs:103-131`). Der Lauf „Sage-Quellenvergleich“ zeigt danach
  „Schlüssel View“, „Schlüssel SData“, „abweichend“ und 20 Beispiele „(fehlt in View)“. Voraussetzung: Der SData-Zugang ist am IDEAL-Test eingerichtet.
  Das ist nicht belegt; die BF-Nachlese nutzt SData an IDEAL (`ServiceSettingDefinitions.cs:114-116`), ob sie dort läuft, bitte im Protokoll prüfen.

## Migrations-/SQL-Auswirkungen

Keine Migration, kein `SQL/XX_*.sql`, keine Änderung an `SQL/00_FreshInstall.sql`. ServiceSettings werden beim Web-Start aus dem Katalog geseedet,
idempotent und nur fehlende Keys (`IdealAkeWms/Program.cs:286-301`). Belegt: Neuere Keys wie `Sync:KommissionierNachtragEnabled`,
`Sync:Quelle:*` und `BfArtikelFelder.NachlesenKey` stehen nicht in `00_FreshInstall.sql` (Suche ohne Treffer). Startet der Dienst vor der Web-App,
fehlt die Zeile. `GetBoolAsync(…, false)` liest dann `false`, die Sperre bleibt aktiv (F2). `SQL/AgentJobs/` ist nicht betroffen.

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `Sync:ArtikelSdataSperreAufheben` (Anzeigename „Artikel über SData trotz Artikel-View (P-R1-Sperre aufheben)“) | Bool (Schalter) | an / aus | aus (`false`) = Sperre aktiv | An: Ist `Sync:ArticlesViewName` gesetzt (IDEAL), wirkt `Sync:Quelle:Artikel = SData` als SData statt wie View. SData liest dann ohne den View-Filter (AKE-Menge: Ressourcenummern ∪ Bestellartikel), je Lauf eine Sammelmeldung mit SData-/View-Zahlen; neue Artikel nach F5. Aus: unverändert gesperrt. Wirkt nur mit Quelle SData; ohne Artikel-View (AKE) ohne Wirkung. | Dienst: Lauf „Artikel“ (`ArtikelQuelleSwitch`, `SageImportService`); Web: `/ServiceSettings` (Banner, Sperrhinweis an „Quelle Artikel-Abgleich“) | IDEAL (AKE ohne Wirkung) | „Hebt die Prüfabfrage-Sperre P-R1 auf: Der Artikel-Abgleich liest an IDEAL Artikel außerhalb der Artikel-View (u. a. 00er-Ressourcenummern) und überschreibt Bezeichnung, Einheit, Gruppe, Meldebestand, Hauptlagerplatz und Matchcode aus SData. Erst nach Prüfabfrage P-R1b bzw. einem Vergleichslauf einschalten. Schreibt nie nach Sage.“ |
| `Sync:Quelle:Artikel` (geändert: nur Texte) | unverändert (Auswahl View/SData/Vergleich) | unverändert | `View` | Beschreibung + Standort nennen die Aufhebung | unverändert | „AKE frei; IDEAL gesperrt bis P-R1 (aufhebbar)“ | unverändert (`WarnSData`) |

Der Server validiert Bool wie jeden Bool-Key (`SettingItem`/`Einstellungspruefung`, `Einstellungspruefung.cs:16-38`). Die Abhängigkeit von Quelle SData
steht in der Beschreibung, nicht als `AbhaengigVon`: `Sync:Quelle:Artikel` ist eine Auswahl, kein Schalter.

## Audit-Feld-Auswirkungen

Keine neuen Felder. Der Artikel-Upsert setzt `CreatedAt/By/ByWindows` bzw. `ModifiedAt/By/ByWindows` mit `'IDEALAKEWMSService'` wie bisher
(`SageImportService.cs:474-476`, `:494-497`). F5 ändert daran nichts. Das Speichern in `/ServiceSettings` läuft über den bestehenden Controller,
der Weg ist unverändert.

## Akzeptanzkriterien

1. `Sync:ArtikelSdataSperreAufheben` steht in `ServiceSettingDefinitions.All` (Bool, Default `false`, Anzeigename, Verwendung, Standort IDEAL,
   Warnhinweis). Die Drift-Guard-Tests sind grün.
2. Schalter aus (bzw. Zeile fehlt), View gesetzt, Quelle `SData`: Der Lauf liest die View. Die Warnung „gesperrt bis P-R1“ steht in
   „Sage-Quellenvergleich“ und im Lauf „Artikel“, wie heute (bestehender Test `ArtikelSdataTests` um Zeile 210-223 unverändert grün).
3. Schalter an, View gesetzt, Quelle `SData`: Der SData-Reader wird gelesen (`GenutzteQuelle == SData`), keine Sperr-Warnung.
4. Schalter nicht lesbar (DB-Fehler simuliert): gesperrt, Grund beginnt mit „P-R1“.
5. `SageQuelleSperre.Grund("Artikel", "vw_X", true)` liefert `null`, `Grund("Artikel", "vw_X", false)` beginnt mit „P-R1“, und
   `Grund("FaStruktur", null, true)` beginnt weiter mit „P-FS3“ (Theory `SageQuelleSperre_Regel` erweitert).
6. Schalter an, View gesetzt: `/ServiceSettings` zeigt oben das Warnbanner mit ⚠ und View-Namen. Der Sperrhinweis an „Quelle Artikel-Abgleich“
   fehlt. Bei Quelle ≠ SData enthält das Banner „wirkt erst mit Quelle SData“. Bei Schalter aus kein Banner.
7. Schalter an, View leer (AKE): kein Banner, Verhalten wie heute.
8. Lauf „Artikel“ mit Schalter an + Quelle SData: genau eine Warnzeile „P-R1-Sperre aufgehoben …“ mit SData-Zahl, View-Zahl, „nur in SData“,
   „nur in View“ (höchstens 1000 Zeichen). Count-Keys unverändert.
9. Wie 8, aber die View-Lesung wirft bzw. die View fehlt: Der Lauf endet erfolgreich mit SData-Daten, die Warnzeile nennt „Vergleich mit der View
   nicht verfügbar: <Grund>“.
10. (nach Rückfrage 1 = C) Eine Nummer, die nur SData liefert und die nicht in `Articles` steht, wird nicht angelegt. Eine vorhandene wird
    aktualisiert. Ist die View nicht verfügbar, wird nichts neu angelegt. Die Zahl „nicht angelegt“ steht in der Sammelmeldung.
11. (nach Rückfrage 2 = ja) Schalter an + Quelle SData + `BfArtikelFelder.NachlesenKey` an: Info-Zeile „Blechteile-Felder (SData-Nachlese)“
    erscheint im Lauf „Artikel“.

## Test-Szenarien

Neues Kapitel **TS-217** in `docs/TESTSZENARIEN.md`. Die Nummer ist vorläufig: TS-210 bis TS-216 sind in Specs und Aufgaben vergeben, der Dev-Lauf
prüft sie nach. Danach `secondbrain/tests/testszenarien-index.md` nachziehen. Ort: IDEAL-Test (:88), Admin.

- **TS-217.1 Standard unverändert:** Vorbedingung `Sync:ArticlesViewName` gesetzt, `Sync:Quelle:Artikel = SData`, neuer Schalter aus.
  Lauf „Artikel“ abwarten. Erwartet: Quelle View, Warnung „gesperrt bis P-R1“, kein Banner, Sperrhinweis an der Einstellung sichtbar.
- **TS-217.2 Banner:** Schalter an, speichern. Erwartet: Banner mit ⚠ und View-Namen, Sperrhinweis an „Quelle Artikel-Abgleich“ weg. Quelle auf
  View stellen: Banner mit „wirkt erst mit Quelle SData“.
- **TS-217.3 Lauf mit aufgehobener Sperre:** vorher DB-Backup der Test-DB und `SELECT COUNT(*) FROM dbo.Articles` notieren. Schalter an,
  Quelle SData, Lauf abwarten. Erwartet: „Quelle: SData“, Sammelmeldung mit Zahlen. Sie passen zu P-R1b bzw. zum Vergleichslauf (Abweichungen
  erklären). Count-Keys plausibel. Nach F5/C: `Articles`-Zahl nicht um „nur in SData“ gewachsen, keine neuen `00%`-Zeilen:
  `SELECT COUNT(*) FROM dbo.Articles WHERE ArticleNumber LIKE '00%'` vorher/nachher gleich.
- **TS-217.4 View nicht verfügbar:** `Sync:ArticlesViewName` kurz auf einen nicht vorhandenen Namen setzen (Schalter an, Quelle SData). Erwartet:
  Der Lauf läuft mit SData, die Sammelmeldung sagt „nicht verfügbar“, nach F5/C keine Neuanlage. Danach den Wert zurücksetzen.
- **TS-217.5 Zurückschalten:** Schalter aus. Erwartet: Der nächste Lauf liest wieder View mit Sperr-Warnung, das Banner ist weg.
- **TS-217.6 (Rückfrage 2 = ja) Blechteile:** `BfArtikelFelder.NachlesenKey` an. Erwartet: Info-Zeile „Blechteile-Felder (SData-Nachlese)“ auch bei
  Quelle SData.
- **Negativ:** Ein ungültiger Bestandswert des Schalters per SQL (z. B. `'ja'`) wird als aus gelesen. `Einstellungspruefung` meldet ihn beim
  Web-Start. AKE-Test: Schalter an hat keine sichtbare Wirkung (kein Banner, Artikel-Lauf wie vorher).

Unit-Tests: `ArtikelSdataTests` (Theory `SageQuelleSperre_Regel`, Weiche mit Schalter an/aus/Lesefehler, Sammelmeldung mit View ok / View wirft),
Drift-Guard automatisch. Manual-UAT bleibt Pflicht: raw SQL des Upserts und SData-Lesen an IDEAL sind nicht InMemory-testbar.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | kein Epic | – | – |

## Deploy

*Vorläufig, der QA-Lauf bestätigt das am echten Diff.*

- **Web-App:** ja (Katalog, `SageQuelleSperre`, `/ServiceSettings`-Banner; der Seed des neuen Keys läuft beim Web-Start).
- **Service:** ja (`ArtikelQuelleSwitch`, `SageImportService`). Reihenfolge: zuerst die Web-App (Seed), dann der Dienst. Umgekehrt ist es unkritisch,
  weil die fehlende Zeile `false` bedeutet.
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`):
  - `@{ Id='<Version>-IDEAL-P-R1-Schalter'; Version='<Version>'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen';
    Text='Neuer Schalter Sync:ArtikelSdataSperreAufheben bleibt aus (Default). Einschalten erst nach Pruefabfrage P-R1b bzw. Vergleichslauf und
    Entscheidung Rueckfrage 3; vorher DB-Backup. Wirkt nur mit Sync:Quelle:Artikel = SData.'; Quelle='<changelog-slug>' }`
  - Keine Pruefung-Zeile: Der Zustand „aus“ entsteht durch den Start-Seed (`Program.cs:288-300`).
- **Publish-Befehle** (im Worktree):
  - `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  - `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

## Offene Rueckfragen

1. **Neue Artikel nur aus SData:** Was soll mit Artikeln geschehen, die SData liefert, die View aber nicht, und die im WMS noch nicht stehen?
   - A: anlegen wie heute (kein Code; die 2026-10 bereinigten 00er kommen mit neuer Id zurück, Einmal-Skript `:35`).
   - B: bei aufgehobener Sperre gar nichts neu anlegen.
   - C: nur anlegen, was auch die View liefert; ist die View nicht lesbar, keine Neuanlage. Bestehende Artikel werden aus SData aktualisiert.

   **Empfehlung C:** Die Artikelmenge bleibt die der View, die 00er-Bereinigung bleibt gültig, und SData füllt trotzdem Details und Merkmale. Die
   View-Menge liest der Lauf für die Sammelmeldung ohnehin. Nachteil: Ein neuer Artikel braucht weiter die View. Will Gerald bewusst die volle
   SData-Menge, ist es A.
2. **Blechteile-Felder:** Die V-B-Nachlese läuft im SData-Modus nicht (`SageImportService.cs:566-577`). IstLagerartikel, Material, Bestellmenge usw.
   würden an IDEAL bei aufgehobener Sperre nicht mehr aktualisiert. Soll sie bei aufgehobener Sperre mitlaufen? **Empfehlung ja:** eine Bedingung,
   die BF-Aufträge bleiben aktuell.
3. **Einschaltbedingung IDEAL-Produktion:** Darf der Schalter dort erst an, wenn P-R1b (bzw. ein Vergleichslauf am IDEAL-Test) gelaufen ist und die
   Differenz „nur in SData“ / „nur in View“ erklärt ist? **Empfehlung ja**, so steht es im Warnhinweis und im Manifest-Text. Wer führt P-R1a/b
   aus? Geralds Konto hat keinen Zugriff auf IDEAL_TEST2026.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)
<!--
  Von der Koordination eingetragen, wortgetreu, je Antwort:
  <Nr>. <Name>, <yyyy-mm-dd>, [<Jira-Schluessel> Kommentar](<Link>): "<Antwort>"
-->

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Neue Artikel nur aus SData – A anlegen / B nicht anlegen / C nur was die View liefert (Empfehlung C) → **A – anlegen wie heute** (Gerald im Chat 2026-11-28: „1a“; Folge: die bereinigten 00er-Artikel kommen mit neuer Id zurück, siehe Rückfrage 1)
2. Blechteile-Felder-Nachlese bei aufgehobener Sperre mitlaufen lassen (Empfehlung ja) → *(offen – Gerald fragt im Chat 2026-11-28, ob die Felder nicht über SData-Userfelder kommen; Antwort siehe Chat/Nachtrag)*
3. Einschalten an IDEAL-Prod erst nach P-R1b bzw. Vergleichslauf; wer führt P-R1a/b aus? → **Vergleichslauf ok** (Gerald im Chat 2026-11-28) – Einschalten an IDEAL-Prod erst nach einem Vergleichslauf (Quelle „Vergleich“) mit erklärter Differenz; P-R1a/b entfällt als Vorbedingung.
