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
  - scripts/deploy/nachlauf.psd1
open_questions:
  - "BEANTWORTET (A): Artikel, die nur SData liefert, werden wie heute angelegt; die bereinigten 00er kommen mit neuer Id zurück (sichtbar in Warnhinweis, Banner, Sammelmeldung, Deploy)."
  - "BEANTWORTET (nein): Keine Blechteile-Nachlese, wenn SData liefert; else-if SageImportService.cs:574 bleibt; USER_OS…-Felder kommen über Sync:ArtikelBenutzerfelder (IDEAL-Wert als Deploy-Schritt)."
  - "BEANTWORTET (Vergleichslauf): Einschalten an IDEAL-Prod erst nach einem Lauf mit Sync:Quelle:Artikel = Vergleich mit erklärter Differenz; P-R1a/b nur noch optional."
  - "NEU (Konflikt aus Antwort 2): Zeigt Prüfabfrage P-BF1 an IDEAL für Sync:Userfeld:Blechfertigung:* einen vom Default abweichenden Sage-Namen, setzt der SData-Weg die Merkmale NICHT fort (Merkmal unter dem Sage-Namen, BF-Aufträge lesen den logischen Namen, BfAuftragRepository.cs:61-67). Dann Sperre nicht aufheben (Empfehlung) oder Namens-Abbildung im SData-Benutzerfeld-Weg nachbauen (neuer Code)?"
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
neuer Id wieder an“). Der Upsert bleibt unverändert (Antwort 1 = A): Die Aufhebung legt diese Zeilen mit neuer Id wieder an. Das ist gewollt
und wird an drei Stellen sichtbar gemacht (Warnhinweis der Einstellung, Banner, Sammelmeldung „nur in SData“) sowie im Deploy-Abschnitt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neue ServiceSetting `Sync:ArtikelSdataSperreAufheben` (Bool, Default `false` = Sperre aktiv, Verhalten unverändert).
- Die Sperr-Regel bleibt an EINEM Ort (`SageQuelleSperre`, Fallstrick „Quellensperren an EINEM Ort“, `fallstricke.md:1834-1836`). Sie bekommt den
  Aufhebe-Wert als Parameter. Dienst-Weiche und `/ServiceSettings` lesen dieselbe Regel.
- Warnbanner in `/ServiceSettings`, solange die Sperre aufgehoben ist und eine Artikel-View gesetzt ist.
- Sammelmeldung (Warnung) je Lauf „Artikel“ bei aufgehobener Sperre und Quelle SData: Anzahl SData, Anzahl View, nur in SData, nur in View, bis
  zu 20 Beispielnummern. Ist die View nicht lesbar, steht das in der Meldung.
- Neue Artikel, die nur SData liefert: Der Upsert legt sie an wie heute (Antwort 1 = A, kein Code). Die Folge (00er zurück) steht sichtbar
  in Warnhinweis, Banner und Sammelmeldung.
- Deploy-Schritt IDEAL: Wert von `Sync:ArtikelBenutzerfelder` enthält die Blechteile-Felder `USER_OS…`, plus Prüfabfrage P-BF1 (Antwort 2).

**Out-of-Scope**
- Blechteile-Felder-Nachlese im SData-Modus (Antwort 2 = nein): Das `else if` `SageImportService.cs:574` bleibt unverändert,
  der Schalter `Sync:BlechteileFelderNachlesen` (`BfArtikelFelder.cs:11`, `ServiceSettingDefinitions.cs:360`) bleibt wie er ist.
- Filter oder Sonderbehandlung neuer Artikel (Varianten B/C aus Rückfrage 1, verworfen).
- Änderung des Katalog-Defaults von `Sync:ArtikelBenutzerfelder` (`ServiceSettingDefinitions.cs:359`, AKE-Liste).
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
   nicht auf `SData`, sagt der Satz zusätzlich: „wirkt erst mit Quelle SData“. Zweiter Satz, immer: „Artikel, die nur SData liefert, legt der
   Abgleich neu an – die 2026-10 bereinigten 00er-Artikel kommen mit neuer Id zurück. Blechteile-Felder kommen dann nur über
   Sync:ArtikelBenutzerfelder (keine Nachlese).“ Der bisherige Sperrhinweis an `Sync:Quelle:Artikel`
   (`_SettingField.cshtml:202-206`) entfällt dann, weil die Regel `null` liefert.
4. **F4 Sammelmeldung.** Je Lauf „Artikel“ mit Quelle SData und aufgehobener Sperre genau eine Warnzeile, höchstens 1000 Zeichen (`SyncLogs.Message`):
   „P-R1-Sperre aufgehoben: SData ohne View-Filter – <s> Artikel aus SData, <v> in der View '<View>', <nurS> nur in SData (fehlende werden
   angelegt, u. a. 00er), <nurV> nur in View. Beispiele nur SData: …“ (bis zu 20 Nummern, gekürzt auf 1000 Zeichen). Ist die View nicht lesbar oder fehlt sie, lautet die Zeile: „… <s> Artikel aus SData, Vergleich mit der View nicht
   verfügbar: <Grund>.“ Die Count-Keys des Laufs (`gelesen`, `neu`, `aktualisiert`, `hauptlagerplatz_fehlt`) bleiben unverändert
   (`SageImportService.cs:579-585`).
5. **F5 Neue Artikel nur aus SData (Antwort 1 = A).** Der Upsert bleibt unverändert und legt jeden Artikel an, den SData liefert und `Articles`
   noch nicht kennt (`SageImportService.cs:463-500`). Kein Code. Folge: Die mit dem Einmal-Skript 2026-10-15 gelöschten 00er-Artikel kommen mit
   neuer Id zurück (Skript `:35`). Sichtbar über Warnhinweis (Einstellungen), Banner (F3), Sammelmeldung (F4, Zahl „nur in SData“) und den
   Manifest-Text (Deploy). Die Zahl der tatsächlich angelegten Artikel steht im vorhandenen Count-Key `neu`.
   *Überholt (Entwurf vor Antwort 1):* Empfehlung C (nur anlegen, was die View liefert, Zähler „nicht angelegt“) und Variante B entfallen.
6. **F6 Blechteile-Felder (Antwort 2 = nein, keine Nachlese).** Liefert SData (Quelle SData, Sperre aufgehoben), läuft die V-B-Nachlese nicht.
   Das `else if` `SageImportService.cs:574` bleibt zeichengleich, der Schalter `Sync:BlechteileFelderNachlesen` bleibt wie er ist (im
   SData-Modus ohne Wirkung, bei Rückkehr zu View/Vergleich greift er wieder). Die Felder `BfArtikelFelder.SageFelder`
   (`IdealAkeWms/Services/BfArtikelFelder.cs:13-21`, Liste `:27-28`) kommen dann über den normalen SData-Weg `Sync:ArtikelBenutzerfelder`
   (`SdataArtikelReader.cs:104-111`, `:251-258` → `ArtikelBenutzerfelderWriter`, `SageImportService.cs:566-572`).
   - **Fortsetzung statt Dubletten (belegt):** Beide Wege schreiben über denselben `ArtikelBenutzerfelderWriter.SchreibenAsync`
     (Nachlese `BlechteileFelderNachlese.cs:39`, Hauptweg `SageImportService.cs:568`). Er sucht die Definition über `SyncSource = "Sage"` +
     `SyncFieldName` ohne Groß/Klein (`ArtikelBenutzerfelderWriter.cs:40`) und legt sie nur an, wenn keine passt (Name = Feld ohne `USER_`,
     `:43`, `:120-121`). Die Nachlese schreibt unter dem **logischen** Namen (= Katalog-Default, `SdataArtikelReader.cs:348`, `:383`), der
     Hauptweg unter dem **in `Sync:ArtikelBenutzerfelder` eingetragenen Sage-Namen** (`SdataArtikelReader.cs:186-189`, keine Abbildung über
     `SageUserfelder`). Die vorhandenen Merkmale laufen also genau dann weiter, wenn die IDEAL-Werte von `Sync:Userfeld:Blechfertigung:*`
     den Defaults gleichen (Zeile fehlt oder gleicher Name, `SageUserfelder.cs:50-57`, `:86`). Unbelegt für IDEAL → Prüfabfrage P-BF1;
     weicht ein Name ab, gilt Rückfrage 4.
   - **Pflicht vor dem Einschalten (Deploy-Schritt, kein Code-Default):** Der IDEAL-Wert von `Sync:ArtikelBenutzerfelder` enthält mindestens
     `USER_OSIstLagerartikel,USER_OSArtikeltyp,USER_OSMaterial,USER_OSMaterialnummerPN,USER_OSFertigungsInfoPN,USER_OSArbeitsschritte,USER_OSAbteilung,USER_OSBlechdicke`
     (162 Zeichen; `ServiceSettings.Value` höchstens 500, `ApplicationDbContext.cs:1040`). Heute steht an IDEAL vermutlich der AKE-Default
     (Seed fehlender Keys aus dem Katalog, `Program.cs:286-301`; unbelegt → P-BF1). Der Katalog-Default (AKE) bleibt unverändert.
     Fehlt die Liste, schreibt der SData-Modus an IDEAL keine Blechteile-Felder mehr, die BF-Artikelsuche (`BfAuftragRepository.cs:59-68`)
     arbeitet dann mit eingefrorenen Werten.
   - **Verluste im SData-Modus (bewusst, Antwort 2):**
     1. Merkmal `Bestellmenge` mit Einheiten-Prüfung (Basis- ≠ Lagermengeneinheit → kein Wert, `SdataArtikelReader.cs:386-405`) wird nicht
        mehr geschrieben und bleibt mit dem letzten Wert stehen. Die BF-Aufträge nehmen ohnehin zuerst `ArticleSageDetails.Bestellmenge`
        (`BfAuftragRepository.cs:85-87`), die der SData-Weg füllt (`SageImportService.cs:558-563`), aber **ohne** Einheiten-Prüfung
        (`SdataArtikelReader.cs:329`). Folge: Bei abweichender Einheit zeigt der BF-Auftrag die Sage-Bestellmenge statt „Menge frei“.
     2. Abräumen „kein Lagerartikel mehr“ (`BlechteileFelderNachlese.cs:53-63`) entfällt. Für Artikel in der SData-Menge schreibt der Hauptweg
        den aktuellen Wert von `USER_OSIstLagerartikel` (z. B. `0`), die BF-Suche blendet `0`/leer aus (`BfAuftragRepository.cs:64-65`).
        Artikel außerhalb der SData-Menge (weder PPS-Ressource noch Bestellartikel, `SdataArtikelReader.cs:214-231`) behalten ihre alten
        Merkmale; die Nachlese las dagegen alle Lagerartikel (`:365-367`).
     3. Die Feldnamen-Einstellungen `Sync:Userfeld:Blechfertigung:*` wirken im SData-Modus nicht; maßgeblich ist allein die Liste
        `Sync:ArtikelBenutzerfelder`. Ebenso entfällt die Regel „Blechdicke nicht konfiguriert → Wert leeren“ (`SdataArtikelReader.cs:360-364`, `:384`).

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
     neuen Eigenschaft `ViewFilterMeldung` (string?). `QuelleFehltMeldung` der View-Lesung bedeutet „nicht verfügbar“.
     *Überholt (Antwort 1 = A):* Die Eigenschaft `ViewSchluessel` für F5 entfällt.
     *ponytail:* zweites Lesen der View je Lauf. Das ist dieselbe Abfrage wie im heutigen View-Betrieb und fällt weg, sobald P-R1 den Filter
     nachbaut.
5. **`SageImportService.SyncArticlesAsync`.**
   - `LogArtikelQuelleAsync` (`:598-608`): `ViewFilterMeldung` als Warnung loggen (F4).
   - F5 (Antwort 1 = A): **keine Änderung** am Upsert (`:463-500`).
   - F6 (Antwort 2 = nein): **keine Änderung** an `:566-577`; das `else if` `:574` bleibt.
   - Damit ändert sich in `SageImportService` nur `LogArtikelQuelleAsync`.
   - *Überholt (Entwurf vor den Antworten):* der Insert-Filter für F5/C (HashSet bzw. `@InsertErlaubt`) und der Umbau des `else if` zu
     eigenem `if` für F6.
6. **Keine neue Rolle, kein neuer Filter.** `/ServiceSettings` ist `[RequireAdminAccess]` (`ServiceSettingsController.cs:19`).

**Einschaltbedingung IDEAL-Produktion (Antwort 3).** Der Schalter geht an IDEAL-Prod erst an, nachdem ein Lauf mit
`Sync:Quelle:Artikel = Vergleich` gelaufen ist und seine Differenz („Schlüssel View“ / „Schlüssel SData“ / „fehlt in View“) erklärt ist (Ablauf
unten „Ohne Sage-Zugang erreichbar“). Dazu P-BF1 ohne Abweichung (Rückfrage 4). P-R1a/b sind **keine Vorbedingung** mehr, nur noch optionale
Prüfungen für den, der Zugriff auf die Sage-DB IDEAL hat. **Unbelegt** bleiben bis zum Vergleichslauf: die tatsächliche Zahl „nur in SData“ an
IDEAL, ob darunter nur 00er sind, und ob `PpsRessourcenPositionenInfo` an IDEAL nicht leer ist (H2 der Ursprungs-Spec, Zeile 1427/1479;
sonst wirft der Reader, `SdataArtikelReader.cs:222-223`).

- **P-BF1 – Benutzerfeld-Einstellungen (WMS-DB IDEAL, Pflicht vor dem Einschalten):**
  ```sql
  SELECT [Key], [Value] FROM dbo.ServiceSettings
  WHERE [Key] LIKE N'Sync:Userfeld:Blechfertigung:%' OR [Key] IN (N'Sync:ArtikelBenutzerfelder', N'Sync:BlechteileFelderNachlesen')
  ORDER BY [Key];
  SELECT Id, Name, SyncFieldName, IsActive FROM dbo.ArticleAttributeDefinitions WHERE SyncSource = N'Sage' ORDER BY SyncFieldName;
  ```
  Erwartet: Jede `Sync:Userfeld:Blechfertigung:<Default>`-Zeile fehlt oder hat den Wert `<Default>`; die Definitionen der Nachlese stehen mit
  `SyncFieldName` = `USER_OS…`. Weicht ein Wert ab: nicht einschalten, Rückfrage 4.
- **P-R1a – Viewtext (optional):** `SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.vw_IDEAL_AKE_WMS_Artikel')) AS Viewtext;`
- **P-R1b – Mengen (optional)** (gleiche Datenbank; die SData-Menge ist dem `LegacySql` nachgebildet, Abweichungen durch die SData-Ressource sind möglich):
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
- **Vergleichslauf (Einschaltbedingung, ohne Sage-Zugang erreichbar):** Am IDEAL-Test-WMS (:88) bzw. an IDEAL-Prod (gefahrlos, s. u.) `Sync:Quelle:Artikel = Vergleich` setzen. Vergleich ist an IDEAL schon heute erlaubt
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
| `Sync:ArtikelSdataSperreAufheben` (Anzeigename „Artikel über SData trotz Artikel-View (P-R1-Sperre aufheben)“) | Bool (Schalter) | an / aus | aus (`false`) = Sperre aktiv | An: Ist `Sync:ArticlesViewName` gesetzt (IDEAL), wirkt `Sync:Quelle:Artikel = SData` als SData statt wie View. SData liest dann ohne den View-Filter (AKE-Menge: Ressourcenummern ∪ Bestellartikel), je Lauf eine Sammelmeldung mit SData-/View-Zahlen; Artikel, die nur SData liefert, werden neu angelegt (F5); Blechteile-Felder nur über `Sync:ArtikelBenutzerfelder`, keine Nachlese (F6). Aus: unverändert gesperrt. Wirkt nur mit Quelle SData; ohne Artikel-View (AKE) ohne Wirkung. | Dienst: Lauf „Artikel“ (`ArtikelQuelleSwitch`, `SageImportService`); Web: `/ServiceSettings` (Banner, Sperrhinweis an „Quelle Artikel-Abgleich“) | IDEAL (AKE ohne Wirkung) | „Hebt die Prüfabfrage-Sperre P-R1 auf: Der Artikel-Abgleich liest an IDEAL Artikel außerhalb der Artikel-View und legt sie neu an – die 2026-10 bereinigten 00er-Artikel kommen mit neuer Id zurück. Er überschreibt Bezeichnung, Einheit, Gruppe, Meldebestand, Hauptlagerplatz und Matchcode aus SData. Die Blechteile-Felder-Nachlese läuft dann nicht; die USER_OS…-Felder müssen in ‚Artikel-Benutzerfelder aus Sage‘ stehen (Bestellmenge-Merkmal mit Einheiten-Prüfung und Abräumen ‚kein Lagerartikel mehr‘ entfallen). Erst nach einem Vergleichslauf (Quelle Artikel = Vergleich) mit erklärter Differenz einschalten, vorher DB-Backup. Schreibt nie nach Sage.“ |
| `Sync:Quelle:Artikel` (geändert: nur Texte) | unverändert (Auswahl View/SData/Vergleich) | unverändert | `View` | Beschreibung + Standort nennen die Aufhebung | unverändert | „AKE frei; IDEAL gesperrt bis P-R1 (aufhebbar)“ | unverändert (`WarnSData`) |
| `Sync:ArtikelBenutzerfelder` (kein Code; nur Wert an IDEAL, Deploy-Schritt) | unverändert (String, Liste `USER_…`, Server prüft wie heute) | unverändert | Katalog-Default unverändert (AKE-Liste, `ServiceSettingDefinitions.cs:359`) | IDEAL-Wert enthält zusätzlich mindestens die 8 Felder aus `BfArtikelFelder.SageFelder` (F6) | unverändert | Wert IDEAL | unverändert |
| `Sync:BlechteileFelderNachlesen` | unverändert | unverändert | unverändert | im SData-Modus ohne Wirkung (else-if `SageImportService.cs:574`), sonst wie heute | unverändert | unverändert | unverändert |

Der Server validiert Bool wie jeden Bool-Key (`SettingItem`/`Einstellungspruefung`, `Einstellungspruefung.cs:16-38`). Die Abhängigkeit von Quelle SData
steht in der Beschreibung, nicht als `AbhaengigVon`: `Sync:Quelle:Artikel` ist eine Auswahl, kein Schalter.

## Audit-Feld-Auswirkungen

Keine neuen Felder. Der Artikel-Upsert setzt `CreatedAt/By/ByWindows` bzw. `ModifiedAt/By/ByWindows` mit `'IDEALAKEWMSService'` wie bisher
(`SageImportService.cs:474-476`, `:494-497`). F5 ändert daran nichts (Upsert unverändert). Die Benutzerfeld-Merkmale setzt
`ArtikelBenutzerfelderWriter` mit Audit wie heute (`ArtikelBenutzerfelderWriter.cs:54`, `:97`, `:106-108`). Das Speichern in `/ServiceSettings` läuft über den bestehenden Controller,
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
6. Schalter an, View gesetzt: `/ServiceSettings` zeigt oben das Warnbanner mit ⚠, View-Namen und dem Satz zu den 00er-Artikeln (neue Id) und
   zur fehlenden Nachlese. Der Sperrhinweis an „Quelle Artikel-Abgleich“ fehlt. Bei Quelle ≠ SData enthält das Banner „wirkt erst mit Quelle SData“. Bei Schalter aus kein Banner.
7. Schalter an, View leer (AKE): kein Banner, Verhalten wie heute.
8. Lauf „Artikel“ mit Schalter an + Quelle SData: genau eine Warnzeile „P-R1-Sperre aufgehoben …“ mit SData-Zahl, View-Zahl, „nur in SData“,
   „nur in View“ und dem Zusatz „fehlende werden angelegt, u. a. 00er“ (höchstens 1000 Zeichen). Count-Keys unverändert.
9. Wie 8, aber die View-Lesung wirft bzw. die View fehlt: Der Lauf endet erfolgreich mit SData-Daten, die Warnzeile nennt „Vergleich mit der View
   nicht verfügbar: <Grund>“.
10. (Antwort 1 = A) Schalter an + Quelle SData: Eine Nummer, die nur SData liefert und nicht in `Articles` steht, wird angelegt (Count-Key `neu`).
    Der Upsert-Code (`SageImportService.cs:463-500`) ist im Diff unverändert.
11. (Antwort 2 = nein) Schalter an + Quelle SData + `Sync:BlechteileFelderNachlesen` an: Keine Info-Zeile „Blechteile-Felder (SData-Nachlese)“,
    dafür die Zeile „Sage-Benutzerfelder (Merkmale) …“. `SageImportService.cs:566-577` ist im Diff unverändert.
12. (Antwort 2) Enthält `Sync:ArtikelBenutzerfelder` die `USER_OS…`-Felder und gleichen die `Sync:Userfeld:Blechfertigung:*` den Defaults,
    legt der SData-Lauf für diese Felder **keine** neue Merkmal-Definition an (P-BF1 vorher/nachher gleiche Definitionen) und aktualisiert die
    vorhandenen Werte; die BF-Artikelsuche findet die Teile wie vorher.
    Unit-Test: Definition mit `SyncFieldName = "USER_OSMaterial"` vorhanden → `ArtikelBenutzerfelderWriter` mit Benutzerfeld `USER_OSMaterial`
    liefert `DefinitionenNeu = 0` und aktualisiert den Wert.

## Test-Szenarien

Neues Kapitel **TS-217** in `docs/TESTSZENARIEN.md`. Die Nummer ist vorläufig: TS-210 bis TS-216 sind in Specs und Aufgaben vergeben, der Dev-Lauf
prüft sie nach. Danach `secondbrain/tests/testszenarien-index.md` nachziehen. Ort: IDEAL-Test (:88), Admin.

- **TS-217.1 Standard unverändert:** Vorbedingung `Sync:ArticlesViewName` gesetzt, `Sync:Quelle:Artikel = SData`, neuer Schalter aus.
  Lauf „Artikel“ abwarten. Erwartet: Quelle View, Warnung „gesperrt bis P-R1“, kein Banner, Sperrhinweis an der Einstellung sichtbar.
- **TS-217.2 Banner:** Schalter an, speichern. Erwartet: Banner mit ⚠ und View-Namen, Sperrhinweis an „Quelle Artikel-Abgleich“ weg. Quelle auf
  View stellen: Banner mit „wirkt erst mit Quelle SData“.
- **TS-217.0 Vergleichslauf (Einschaltbedingung):** Schalter aus, `Sync:Quelle:Artikel = Vergleich`, Lauf abwarten. Erwartet: Lauf
  „Sage-Quellenvergleich“ mit „Schlüssel View“, „Schlüssel SData“, „abweichend“ und Beispielen „(fehlt in View)“; `Articles` unverändert (Vergleich
  schreibt nur aus der View). Differenz notieren und erklären. P-BF1 ausführen und Ergebnis notieren.
- **TS-217.3 Lauf mit aufgehobener Sperre:** vorher DB-Backup der Test-DB, `SELECT COUNT(*) FROM dbo.Articles` und
  `SELECT COUNT(*) FROM dbo.Articles WHERE ArticleNumber LIKE '00%'` notieren, `Sync:ArtikelBenutzerfelder` um die 8 `USER_OS…`-Felder ergänzt.
  Schalter an, Quelle SData, Lauf abwarten. Erwartet: „Quelle: SData“, Sammelmeldung mit Zahlen, „nur in SData“ passt zum Vergleichslauf
  (TS-217.0). `Articles` wächst um den Count-Key `neu` (höchstens „nur in SData“); die `00%`-Zahl ist gewachsen (Antwort 1 = A, erwartet). Danach P-BF1:
  keine neuen Definitionen für `USER_OS…`.
- **TS-217.4 View nicht verfügbar:** `Sync:ArticlesViewName` kurz auf einen nicht vorhandenen Namen setzen (Schalter an, Quelle SData). Erwartet:
  Der Lauf läuft mit SData und endet erfolgreich, die Sammelmeldung sagt „nicht verfügbar“. Danach den Wert zurücksetzen.
- **TS-217.5 Zurückschalten:** Schalter aus. Erwartet: Der nächste Lauf liest wieder View mit Sperr-Warnung, das Banner ist weg; ist
  `Sync:BlechteileFelderNachlesen` an, erscheint wieder „Blechteile-Felder (SData-Nachlese)“.
- **TS-217.6 Blechteile ohne Nachlese:** Schalter an, Quelle SData, `Sync:BlechteileFelderNachlesen` an. Erwartet: keine Zeile
  „Blechteile-Felder (SData-Nachlese)“, dafür „Sage-Benutzerfelder (Merkmale) …“. BF-Auftrag anlegen (Rolle mit BF-Recht): Artikelsuche findet
  ein bekanntes BG/FT-Lagerteil mit Material/Artikeltyp wie vor dem Umschalten.
- **Negativ Blechteile:** `Sync:ArtikelBenutzerfelder` ohne `USER_OS…`-Felder, Schalter an, Quelle SData: weder Nachlese noch Aktualisierung der
  Blechteile-Merkmale (Werte bleiben eingefroren) – dokumentiertes Verhalten, deshalb Deploy-Schritt.
- **Negativ:** Ein ungültiger Bestandswert des Schalters per SQL (z. B. `'ja'`) wird als aus gelesen. `Einstellungspruefung` meldet ihn beim
  Web-Start. AKE-Test: Schalter an hat keine sichtbare Wirkung (kein Banner, Artikel-Lauf wie vorher).

Unit-Tests: `ArtikelSdataTests` (Theory `SageQuelleSperre_Regel`, Weiche mit Schalter an/aus/Lesefehler, Sammelmeldung mit View ok / View wirft),
Writer-Fortsetzung aus AK 12, Drift-Guard automatisch. Manual-UAT bleibt Pflicht: raw SQL des Upserts und SData-Lesen an IDEAL sind nicht InMemory-testbar.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | kein Epic | – | – |

## Deploy

*Vorläufig, der QA-Lauf bestätigt das am echten Diff.*

- **Web-App:** ja (Katalog, `SageQuelleSperre`, `/ServiceSettings`-Banner; der Seed des neuen Keys läuft beim Web-Start).
- **Service:** ja (`ArtikelQuelleSwitch`, `SageImportService.LogArtikelQuelleAsync`). Reihenfolge: zuerst die Web-App (Seed), dann der Dienst. Umgekehrt ist es unkritisch,
  weil die fehlende Zeile `false` bedeutet.
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`):
  - `@{ Id='<Version>-IDEAL-P-R1-Schalter'; Version='<Version>'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen';
    Text='Neuer Schalter Sync:ArtikelSdataSperreAufheben bleibt aus (Default). Einschalten erst nach einem Lauf mit Sync:Quelle:Artikel = Vergleich,
    dessen Differenz erklaert ist, und P-BF1 ohne Abweichung; vorher DB-Backup. Wirkt nur mit Sync:Quelle:Artikel = SData. FOLGE: Artikel nur
    aus SData werden angelegt - die 2026-10 bereinigten 00er-Artikel kommen mit neuer Id zurueck. Blechteile-Nachlese laeuft dann nicht.';
    Quelle='<changelog-slug>' }`
  - `@{ Id='<Version>-IDEAL-Benutzerfelder-OS'; Version='<Version>'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen';
    Text='Vor dem Einschalten von Sync:ArtikelSdataSperreAufheben: Sync:ArtikelBenutzerfelder (Artikel-Benutzerfelder aus Sage) an IDEAL um
    USER_OSIstLagerartikel,USER_OSArtikeltyp,USER_OSMaterial,USER_OSMaterialnummerPN,USER_OSFertigungsInfoPN,USER_OSArbeitsschritte,USER_OSAbteilung,USER_OSBlechdicke
    ergaenzen (max. 500 Zeichen); sonst werden die Blechteile-Merkmale im SData-Modus nicht mehr aktualisiert. Katalog-Default (AKE) bleibt.';
    Quelle='<changelog-slug>' }`
  - `@{ Id='Pruefung/BlechfertigungUserfeldDefault'; Version='<Version>'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen';
    Pruefung='SELECT COUNT(*) FROM dbo.ServiceSettings WHERE [Key] LIKE N''Sync:Userfeld:Blechfertigung:%'' AND [Value] <> SUBSTRING([Key], 30, 200)';
    Text='P-BF1: Sync:Userfeld:Blechfertigung:* weicht vom Default ab - im SData-Modus liefen die Blechteile-Merkmale nicht weiter. Sync:ArtikelSdataSperreAufheben nicht einschalten, Rueckfrage 4 der Spec 2026-11-28-artikel-sdata-sperre-p-r1-abschaltbar-spec.';
    Quelle='<changelog-slug>' }` (`LEN(N'Sync:Userfeld:Blechfertigung:') = 29`, Default = Key-Endung laut `SageUserfelder.Key`, `SageUserfelder.cs:28`;
    Muster wie `Pruefung/BeschichterKreditoren`, `nachlauf.psd1:144-146`). Nur lesend; ein leerer Wert zählt als Abweichung (Feld nicht konfiguriert).
  - Keine Pruefung-Zeile für den Schalter selbst: Der Zustand „aus“ entsteht durch den Start-Seed (`Program.cs:288-300`).
- **Publish-Befehle** (im Worktree):
  - `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  - `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

## Offene Rueckfragen

*Rückfragen 1–3 sind beantwortet (Freigabe-Antworten 1–3, eingearbeitet in F5, F6, Prüfabfragen/Einschaltbedingung). Die Fragetexte bleiben
als Verlauf stehen; die Empfehlungen zu 1 (C) und 2 (ja) sind überholt.*

1. **[Beantwortet: A]** **Neue Artikel nur aus SData:** Was soll mit Artikeln geschehen, die SData liefert, die View aber nicht, und die im WMS noch nicht stehen?
   - A: anlegen wie heute (kein Code; die 2026-10 bereinigten 00er kommen mit neuer Id zurück, Einmal-Skript `:35`).
   - B: bei aufgehobener Sperre gar nichts neu anlegen.
   - C: nur anlegen, was auch die View liefert; ist die View nicht lesbar, keine Neuanlage. Bestehende Artikel werden aus SData aktualisiert.

   **Empfehlung C:** Die Artikelmenge bleibt die der View, die 00er-Bereinigung bleibt gültig, und SData füllt trotzdem Details und Merkmale. Die
   View-Menge liest der Lauf für die Sammelmeldung ohnehin. Nachteil: Ein neuer Artikel braucht weiter die View. Will Gerald bewusst die volle
   SData-Menge, ist es A.
2. **[Beantwortet: nein]** **Blechteile-Felder:** Die V-B-Nachlese läuft im SData-Modus nicht (`SageImportService.cs:566-577`). IstLagerartikel, Material, Bestellmenge usw.
   würden an IDEAL bei aufgehobener Sperre nicht mehr aktualisiert. Soll sie bei aufgehobener Sperre mitlaufen? **Empfehlung ja:** eine Bedingung,
   die BF-Aufträge bleiben aktuell.
3. **[Beantwortet: Vergleichslauf]** **Einschaltbedingung IDEAL-Produktion:** Darf der Schalter dort erst an, wenn P-R1b (bzw. ein Vergleichslauf am IDEAL-Test) gelaufen ist und die
   Differenz „nur in SData“ / „nur in View“ erklärt ist? **Empfehlung ja**, so steht es im Warnhinweis und im Manifest-Text. Wer führt P-R1a/b
   aus? Geralds Konto hat keinen Zugriff auf IDEAL_TEST2026.
4. **NEU – Konflikt aus Antwort 2 (Namensabbildung):** Antwort 2 setzt voraus, dass der normale SData-Weg die vorhandenen Blechteile-Merkmale
   fortsetzt. Das gilt nur, wenn an IDEAL `Sync:Userfeld:Blechfertigung:*` den Defaults gleicht: Die Nachlese schreibt unter dem logischen Namen
   (`SdataArtikelReader.cs:383`), der Hauptweg unter dem Sage-Namen aus `Sync:ArtikelBenutzerfelder` (`:186-189`), und die BF-Artikelsuche liest
   nur den logischen Namen (`BfAuftragRepository.cs:61-67`). Weicht ein Wert ab (P-BF1, Manifest-Prüfung `Pruefung/BlechfertigungUserfeldDefault`),
   entstünde unter dem Sage-Namen ein zweites Merkmal (bzw. ein Namenskonflikt), und die BF-Suche sähe eingefrorene Werte.
   - A: Dann die Sperre an IDEAL nicht aufheben (kein Code, die Prüfung ist Einschaltbedingung).
   - B: Den Benutzerfeld-Weg um die Abbildung Sage-Name → logischer Name für die Blechfertigungs-Felder erweitern (neuer Code, größerer Umfang).

   **Empfehlung A:** Solange P-BF1 keine Abweichung zeigt, ist die Frage gegenstandslos; B erst, wenn es eine gibt.

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
2. Blechteile-Felder-Nachlese bei aufgehobener Sperre mitlaufen lassen (Empfehlung ja) → **Nein – wenn SData liefert, keine Nachlese** (Gerald im Chat 2026-11-28: „Wenn sdata liefert keine nachlese“). Die Blechteile-Felder `USER_OS…` kommen dann über den normalen SData-Weg (Benutzerfelder); Bestellmenge-Merkmal mit Einheiten-Prüfung und Abräumen „kein Lagerartikel mehr“ entfallen im SData-Modus.
3. Einschalten an IDEAL-Prod erst nach P-R1b bzw. Vergleichslauf; wer führt P-R1a/b aus? → **Vergleichslauf ok** (Gerald im Chat 2026-11-28) – Einschalten an IDEAL-Prod erst nach einem Vergleichslauf (Quelle „Vergleich“) mit erklärter Differenz; P-R1a/b entfällt als Vorbedingung.
4. Weicht an IDEAL ein `Sync:Userfeld:Blechfertigung:*` vom Default ab: Sperre dort nicht aufheben (Empfehlung) oder Namens-Abbildung im SData-Benutzerfeld-Weg nachbauen? →

## Nachtrag 2026-11-28 – P-BF1 am IDEAL-Test
Abfrage `IDEAL_AKE_WMS_TEST` (idealsql2\SAGE100TEST, lesend): alle 8 `Sync:Userfeld:Blechfertigung:USER_OS…` = Default (gleicher Name) →
am **Test keine Abweichung**, Rückfrage 4 stellt sich dort nicht. IDEAL-Prod (`IDEAL_AKE_WMS`) nicht geprüft – vor dem Einschalten dort wiederholen.
Nebenbefund: am Test steht `Sync:Quelle:Artikel = SData` (wirkt heute wegen der Sperre wie View mit Warnung), `Sync:BlechteileFelderNachlesen = true`,
`Sync:ArtikelBenutzerfelder` = Katalog-Default (ohne `USER_OS…`).
