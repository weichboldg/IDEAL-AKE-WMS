---
type: spec
title: Wareneingang-CSV – ohne Kopfzeile, Erfasser = Windows-Kontoname (sAMAccountName)
slug: 2026-11-14-wareneingang-csv-erfasser-samaccountname-spec
status: Testbereit
created: 2026-11-16
updated: 2026-11-16
source_backlog: "[[2026-11-14-wareneingang-csv-erfasser-samaccountname]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: AKE
version: "1.99.29"
testszenario: "TS-153"
affected_code:
  - IDEALAKEWMSService/Services/WareneingangCsvService.cs
  - IDEALAKEWMSService.Tests/Services/WareneingangCsvServiceTests.cs
  - IdealAkeWms/Views/Help/Index.cshtml
  - IdealAkeWms/Views/Help/Changelog.cshtml
  - IdealAkeWms/AppVersion.cs
  - IDEALAKEWMSService/AppVersion.cs
  - scripts/deploy/nachlauf.psd1
  - docs/TESTSZENARIEN.md
open_questions: []
beantwortete_fragen:
  - "1 -> BEANTWORTET: enaio-Import darf keine Ueberschriften haben; ohne Kopfzeile ist genau das Zielformat, keine Import-Umstellung (Freigabe-Antwort 1)"
  - "2 -> BEANTWORTET: noch nicht produktiv, keine Altdateien, kein Uebergang (Freigabe-Antwort 2)"
  - "3 -> BEANTWORTET: Schreibweise egal -> unveraendert wie gespeichert ausgeben, kein Zusatzcode; @ kommt im Kontonamen nicht vor (Freigabe-Antwort 3)"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: "Antworten Gerald 1–3 (16.11.)"
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-16"
---

# Wareneingang-CSV – ohne Kopfzeile, Erfasser = Windows-Kontoname

> [!info] Nummern (beim Start geprüft, 2026-11-16)
> Version **1.99.29**, Testszenario **TS-153**, **keine** SQL-Nummer, **kein** ADR.
> Geprüft: Worktree `AppVersion.cs` = 1.99.25; reserviert 1.99.24–1.99.28 (SQL/151, /153, /154; TS-139–152; ADR 0027–0030).
> `secondbrain/` enthält weder „1.99.29“ noch TS-153–159 (Grep über specs/ und aufgaben/ ohne Treffer). Höchstes TS in
> `docs/TESTSZENARIEN.md` des Worktrees: TS-144. Der Dev-Lauf prüft die Nummern erneut. Wird die Reihenfolge im Bündel
> geändert, verschiebt sich nur die Version, nicht der Inhalt.

## Ziel / Nutzen (das Warum)

Den Lieferschein-Export liest **enaio** (Gerald 2026-11-16). Zwei Änderungen am Dateiformat werden zusammen umgesetzt, damit enaio
einmal abgestimmt und einmal getestet wird:

1. **Keine Kopfzeile mehr.** Die Datei enthält nur die Datenzeile.
2. **Erfasser = Windows-Kontoname (sAMAccountName) ohne Domäne** statt des WMS-Anzeigenamens. Das ist eindeutig, und enaio kann
   es einem Benutzer zuordnen. Ein lokaler WMS-Benutzer ohne Windows-Konto behält seinen WMS-Namen.

Grundlage ist die Vorgänger-Spec [[2026-11-12-wareneingang-barcode-lieferschein-csv-export-spec]] (v1.99.11, TS-129). Dort heißt es
in Antwort 2: „Den enaio-Import gestalten wir selbst.“ Die Datei ohne Kopfzeile ist **genau das Zielformat** des enaio-Imports, der
keine Überschriften verträgt. Am Import muss nichts umgestellt werden (Freigabe-Antwort 1). Der Export ist **noch nicht produktiv**
(Freigabe-Antwort 2), deshalb gibt es keine Altdateien und keinen Übergang.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- `WareneingangCsvService.CsvInhalt`: Kopfzeile entfällt. Spaltenreihenfolge, Trennzeichen `;`, CRLF, UTF-8 mit BOM, Datumsformat und
  Quoting bleiben unverändert.
- Spalte 7 „Erfasser“: neue Herkunft (siehe F2).
- Die Leseabfrage in `RunOnceAsync` lädt Bewegung und Benutzer mit.
- Unit-Tests anpassen und ergänzen.
- Die Hilfe beschreibt das Format mit Spaltenreihenfolge (ohne Überschrift ist die Reihenfolge der Vertrag). Dazu Anwender-Changelog,
  Version, TS-153, ein Hinweis an TS-129.11 und der Manifest-Eintrag.

**Out-of-Scope** (Gerald 2026-11-16 16:52: nur die CSV)
- **Alle anderen Ausgaben bleiben unverändert**, ebenso alle gespeicherten Werte:
  - `PurchaseOrderReceipt.CreatedBy` und `StockMovement.CreatedBy` bleiben der Anzeigename beim Buchen (`PurchaseOrdersController.Receive.cs:120,142,155`).
  - Die Sage-Wareneingangsbelege enthalten keinen Benutzer. Kopf-Memo = Kennung (`SageWareneingangPayload.cs:100`), Positionsmemo =
    Eingabe + Kennung + LS + Platz (`SageWareneingangPayload.cs:63-67,89`). Die Sage-Lagerbuchung trägt im Memo nur die Kennung
    (`SageLagerbuchungPayloadBuilder.cs:71`).
  - Die Mails mit „Erfasser“ (Lagerbestellungen) nehmen weiter `CreatedBy` (`WarehouseRequisitionEmailService.cs:152,179,199,225`).
  - Wareneingangs-Maske und Liste „Sage-Wareneingänge“ zeigen keinen Erfasser: kein `CreatedBy` in `Views/PurchaseOrders/*` und
    `PurchaseOrdersController.SageWe.cs` (Grep ohne Treffer).
- Bereits geschriebene Dateien werden nicht umgeschrieben. Auf Prod gibt es keine, weil der Export noch nicht produktiv ist
  (Freigabe-Antwort 2). Betroffen sind nur Testdateien aus TS-129 auf DEV.
- Keine Umformung der Schreibweise (z. B. auf Kleinbuchstaben). Laut Gerald ist die Schreibweise egal (Freigabe-Antwort 3), also wird
  ohne Zusatzcode ausgegeben, was gespeichert ist.
- Am Benutzerstamm ändert sich nichts. Kein Bereinigungsskript für `Users.WindowsUserName`, keine neue Prüfung beim Anlegen.
- Kein Schreiben nach Sage, keine Einstellungen auf Test oder Prod.

> [!note] Überholt im Backlog
> Der Vorschlag „Kopfzeile `Erfasser` unverändert lassen“ gilt nicht mehr. Die Ergänzung vom 16.11. 16:31 hat ihn ersetzt:
> **keine Kopfzeile**.

## Fachliche Anforderungen

**F1 – Ohne Kopfzeile.** Die Datei besteht aus genau einer Zeile mit 9 Feldern und CRLF am Ende. Das BOM setzt weiterhin der Schreiber.
Die Reihenfolge der Felder ist unverändert (`WareneingangCsvService.cs:140-141`):

| Pos. | Feld (bisheriger Spaltenname) | Quelle |
|---|---|---|
| 1 | Barcode | `WareneingangCsvExport.Barcode` |
| 2 | Kreditornummer | `PurchaseOrderReceipt.LieferantKto` |
| 3 | Kreditorname | `PurchaseOrderReceipt.LieferantName` |
| 4 | Bestellnummer | `PurchaseOrderReceipt.Belegnummer` |
| 5 | Lieferschein | `PurchaseOrderReceipt.Lieferschein` |
| 6 | SageBelegnummer | `PurchaseOrderReceipt.SageWeBelegnummer` |
| 7 | Erfasser | **neu, siehe F2** |
| 8 | Datum (`yyyy-MM-dd HH:mm`) | `PurchaseOrderReceipt.CreatedAt` |
| 9 | UebergebenAm (`yyyy-MM-dd HH:mm`) | `PurchaseOrderReceipt.SageWeAm` |

Alle Felder beziehen sich auf das erste Receipt des Vorgangs (`MinBy(Id)`, `WareneingangCsvService.cs:64-66`), wie bisher.

**F2 – Erfasser.** Die erste zutreffende Regel gilt:
1. Hat die Bewegung des Receipts (`PurchaseOrderReceipt.StockMovement`) einen Benutzer (`StockMovement.UserId` → `User`) mit nicht
   leerem `WindowsUserName`, wird dieser **ohne Domäne** ausgegeben. Aus `DOMAIN\name` wird `name`, aus `name@domain` wird `name`.
   Getrimmt, Schreibweise wie gespeichert. Das ist entschieden: Die Schreibweise ist egal (Freigabe-Antwort 3), also keine Umformung.
2. Hat der Benutzer kein `WindowsUserName` (lokaler WMS-Benutzer), wird `User.Name` ausgegeben.
3. Hat die Bewegung keine `UserId` (Altbestand, oder der Benutzer wurde gelöscht: FK `SetNull`, `ApplicationDbContext.cs:410-413`),
   wird wie bisher `PurchaseOrderReceipt.CreatedBy` ausgegeben.

Ob der Benutzer aktiv ist, spielt keine Rolle: Ausgegeben wird, wer gebucht hat. Maßgeblich ist der Benutzerstamm zum Zeitpunkt, an
dem die Datei geschrieben wird. Die CSV entsteht erst nach der Übergabe an Sage.

**`CreatedByWindows` und `StockMovement.WindowsUser` werden nicht verwendet.** Sie enthalten die Windows-Identität der Sitzung, nicht
die des Benutzers: `DOMAIN\sam` aus `User.Identity.Name` (`WindowsAutoLoginMiddleware.cs:118`, `AccountController.cs:108`).
Bei einer Formular-Anmeldung ohne Windows-Identität steht dort `""` bzw. `"SYSTEM"` (`CurrentUserService.cs:33-41`,
[[fallstricke]] „Antiforgery-Wurzel-Fix“). An einem Terminal mit Windows-Identität ist es das Konto des Terminals.

**F3 – Escaping unverändert.** Ein Feld mit `;`, `"`, CR oder LF steht in Anführungszeichen, ein inneres `"` wird verdoppelt
(`WareneingangCsvService.cs:136-137`). Das gilt auch für den neuen Erfasser-Wert. `User.Name` ist Freitext (bis 200 Zeichen,
`User.cs:7-10`).

### Belege: Woher kommt der Erfasser heute?

- Spalte 7 = `F(r.CreatedBy)` (`WareneingangCsvService.cs:141`).
- `r.CreatedBy = name`, `name = _user.GetDisplayName()` (`PurchaseOrdersController.Receive.cs:120,155`).
- `GetDisplayName()` = Session-Wert `AppUserName`, sonst Windows-Identität ohne Domäne (`CurrentUserService.cs:43-53`).
- `AppUserName` = `user.Name` bei Formular- und Windows-Anmeldung (`AccountController.cs:105`, `WindowsAutoLoginMiddleware.cs:115`).
  Der Erfasser ist heute also der **WMS-Benutzername** (`User.Name`) zum Buchungszeitpunkt. Bei AD-Benutzern ist das der beim Anlegen
  gewählte Anzeigename, ersatzweise der sam (`UsersController.cs:202`).
- `StockMovement.UserId = _user.GetCurrentAppUserId()` (Session) wird beim Wareneingang gesetzt (`Receive.cs:119,139`). Ein Receipt
  entsteht **nur** auf diesem Weg (einziges `new PurchaseOrderReceipt` außerhalb der Tests: `Receive.cs:145`). Die Navigation
  `PurchaseOrderReceipt.StockMovement` ist vorhanden (`PurchaseOrderReceipt.cs:36-37`), ebenso `StockMovement.User` (`StockMovement.cs:37,53`).

### Belege: Alle Schreibwege von `Users.WindowsUserName`

| Weg | Schreibt `WindowsUserName`? | Form | Beleg |
|---|---|---|---|
| AD-Benutzer anlegen (`/Users/CreateAdUser`) | **ja**, einziger Weg | `vm.SamAccountName.Trim()`. Die Auswahlliste stammt aus `UserPrincipal.SamAccountName` (ohne Domäne). | `UsersController.cs:177,203`; `ActiveDirectoryService.cs:56-57`; `CreateAdUser.cshtml:18-19` |
| Lokalen Benutzer anlegen (`/Users/Create`) | nein (bleibt NULL) | – | `UsersController.cs:114-134` |
| Benutzer bearbeiten (`/Users/Edit`) | nein: Das Feld ist im VM nur zur Anzeige, der POST übernimmt es nicht | – | `UsersController.cs:247` (nur GET), `290-312` (POST ohne WindowsUserName); `UserEditViewModel.cs:15-17` |
| Profil (`/Account/Profile`) | nein | – | `AccountController.cs:205-229` |
| Formular-Anmeldung | nein (setzt nur die Session) | – | `AccountController.cs:104-108` |
| Windows-Anmeldung (Middleware) | nein (liest nur, `ExtractSam` + Lookup) | – | `WindowsAutoLoginMiddleware.cs:110-118`; `UserRepository.cs:42-52` |
| Admin-Seed beim Start / FreshInstall | nein (NULL) | – | `Program.cs:263-272`; `SQL/00_FreshInstall.sql:1544-1547` |
| SQL-Skripte, AgentJobs, Deploy-Skripte | nein: kein `UPDATE`/`INSERT` auf `Users` mit `WindowsUserName` | – | Grep `(UPDATE\|INSERT INTO) …Users` über `*.cs,*.sql,*.ps1,*.psd1`: nur FreshInstall:1544; `SQL/73` legt nur Spalte und Index an |
| AD-Import/-Sync im Dienst | gibt es nicht. Der Dienst schreibt keine `Users`. | – | Grep `.Users.(Add\|Update)` / `_userRepository.(Add\|Update)Async`: nur `UsersController.cs:139,210,314`, `AccountController.cs:229`, `Program.cs:272` |

**Schluss:** Im normalen Ablauf ist der Wert domänenfrei, denn er kommt aus dem AD-Attribut sAMAccountName. **Garantiert ist das nicht.**
`CreateAdUser` prüft den geposteten `SamAccountName` nicht gegen die AD-Liste: `candidate` darf `null` sein, und der Benutzer wird
trotzdem angelegt (`UsersController.cs:197-209`). Direkte DB-Pflege lässt sich ebenfalls nicht ausschließen (Prüfabfrage P-1).
Deshalb **wird beim Lesen normalisiert**, über den vorhandenen, getesteten Helfer `WindowsAccountHelper.ExtractSam`
(`IdealAkeWms/Services/WindowsAccountHelper.cs:7-22`, Tests `WindowsAccountHelperTests.cs:10-17`: Backslash-Form, `@`-Form, leer → null).
Den Helfer nutzt auch die Windows-Anmeldung. Der Dienst referenziert `IdealAkeWms.Services` bereits (`WareneingangCsvService.cs:4`).

> [!note] Grenze von ExtractSam (unkritisch)
> Ein sAMAccountName mit `@` würde am `@` abgeschnitten. Laut Gerald kommt ein `@` im Kontonamen nicht vor (Freigabe-Antwort 3).
> P-1 bleibt nur als Kontrolle, ist kein Blocker, und es gibt keinen Zusatzcode dafür.
>
> *Überholt (Entwurf vom 2026-11-16):* „Bei Treffern meldet der Dev-Lauf das vor dem Umsetzen, statt still abzuschneiden.“

## Technischer Loesungsentwurf

Kleinste Lösung (ponytail): eine Datei Produktivcode, kein neuer Typ, keine neue Abhängigkeit.

1. **`RunOnceAsync`** (`WareneingangCsvService.cs:64-65`): Die Abfrage `erste` bekommt
   `.Include(r => r.StockMovement).ThenInclude(m => m.User)`. AsNoTracking bleibt.
2. **`CsvInhalt`** (`WareneingangCsvService.cs:131-142`):
   - Die Zeile mit der Kopfzeile entfällt. Rückgabe ist nur noch `string.Join(";", …) + "\r\n"`.
   - Erfasser:
     `var u = r.StockMovement?.User;`
     `var erfasser = WindowsAccountHelper.ExtractSam(u?.WindowsUserName) ?? u?.Name ?? r.CreatedBy;`
     Feld 7 = `F(erfasser)`. `?.` auf `StockMovement` ist nötig: Die Eigenschaft ist zwar `= null!` deklariert, der statische Test
     (`WareneingangCsvServiceTests.cs:217`) baut aber ein Receipt ohne Bewegung.
   - Den XML-Kommentar anpassen: „eine Datenzeile ohne Kopfzeile (v1.99.29), Erfasser = …“.
3. Signatur, Schreiber, Fehlerpfade, Wiederholung und Ausgangstabelle bleiben unverändert.

Muster: kein Repository-Umbau. Der Dienst arbeitet hier schon direkt auf dem `ApplicationDbContext` (Bestand v1.99.11).
Modul: [[module]] Wareneingang / Dienst `SageBookingWorker` → `WareneingangCsvService` (`SageBookingWorker.cs:153`).

**Wer schreibt die CSV?** Ausschließlich der **Dienst**: `IDEALAKEWMSService/Program.cs:154` (DI) und `SageBookingWorker.cs:153` (Aufruf).
Die Web-App schreibt keine Datei. Sie ändert sich nur bei Hilfe, Changelog und Version.

## Migrations-/SQL-Auswirkungen

Keine. Keine Schemaänderung, kein `SQL/XX`, keine FreshInstall-Änderung, keine AgentJobs. Gelesen werden nur vorhandene Spalten:
`StockMovements.UserId` und `Users.WindowsUserName`/`Name`.

## Einstellungen

keine. `WareneingangCsvAktiv`, `WareneingangCsvPfad` usw. bleiben unverändert. Das neue Format gilt ohne Schalter: Gerald hat es
entschieden, und eine Umschaltung wäre YAGNI.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Es wird keine Entität neu geschrieben oder geändert. Die Audit-Felder an `WareneingangCsvExporte` setzt der Lauf wie bisher
(`WareneingangCsvService.cs:49,94`). `CreatedBy`/`CreatedByWindows` an Receipt und Bewegung bleiben, wie sie sind.
Rollen und Zugriffsfilter: keine Änderung. Listen-View-Pattern: keine neue Tabelle oder Ansicht.

## Akzeptanzkriterien

1. **Ohne Kopfzeile:** Eine neu geschriebene `<Barcode>.csv` besteht aus BOM + genau einer Zeile mit 9 durch `;` getrennten Feldern
   + CRLF. `Barcode;Kreditornummer;…` kommt nicht vor.
2. **Reihenfolge:** Die Felder 1–6, 8 und 9 haben dieselben Werte und dasselbe Format wie die Datenzeile vor v1.99.29 (Unit-Test mit
   den Werten aus `WareneingangCsvServiceTests.cs:82-83` ohne Kopfzeile).
3. **AD-Benutzer:** Bucht ein Benutzer mit `WindowsUserName = jmuster`, steht in Feld 7 `jmuster`, nicht der Anzeigename.
4. **Domänenformen:** `WindowsUserName = AKE\jmuster` oder `jmuster@ake.at` ergibt in Feld 7 `jmuster` (Unit-Test).
5. **Lokaler Benutzer:** Ist `WindowsUserName` NULL oder leer, steht in Feld 7 `User.Name`.
6. **Ohne UserId:** Hat die Bewegung `UserId = NULL`, steht in Feld 7 `PurchaseOrderReceipt.CreatedBy`.
7. **Nicht die Sitzung:** Feld 7 ist nie der Wert von `CreatedByWindows` oder `StockMovement.WindowsUser`. Test: Benutzer
   `WindowsUserName = jmuster`, Receipt `CreatedByWindows = "AKE\\terminal01"` → `jmuster`.
8. **Include wirkt:** Ein Test über `RunOnceAsync` mit Benutzer in der DB liefert in der Datei den Kontonamen, nicht `CreatedBy`.
   Das schützt gegen ein vergessenes `Include`, das sonst still auf `CreatedBy` zurückfällt.
9. **Escaping:** `User.Name = Muster; "Max"` (lokaler Benutzer) ergibt Feld 7 `"Muster; ""Max"""`.
10. **Bestandsverhalten unverändert:** Der Diff ändert keine andere Ausgabe. Unverändert bleiben `Receive.cs` (CreatedBy der
    Receipts/Bewegungen), `SageWareneingangPayload.cs`, `SageLagerbuchungPayloadBuilder.cs`, `WarehouseRequisitionEmailService.cs`,
    die Views unter `Views/PurchaseOrders/` und `PurchaseOrdersController.SageWe.cs`. Bestehende Tests dieser Klassen bleiben grün,
    ohne Anpassung.
11. **Erneut schreiben:** „CSV erneut schreiben“ für einen Vorgang, der schon vor v1.99.29 übergeben wurde (DEV), schreibt das neue
    Format. *Überholt:* der Teil „vor dem Deploy geschriebene Datei bleibt unverändert“ als Übergangsfall. Es gibt keine Prod-Altdateien
    (Freigabe-Antwort 2), und eine vorhandene Datei überschreibt der Dienst ohnehin nie (Bestand v1.99.11).
12. **Hilfe:** `/Help` (Abschnitt Wareneingang, bisher `Index.cshtml:119-121`) sagt „ohne Kopfzeile“ und nennt die 9 Felder in
    Reihenfolge. Feld 7 ist dort beschrieben als „Windows-Kontoname ohne Domäne, sonst WMS-Benutzername“. Der Anwender-Changelog
    v1.99.29 nennt beide Änderungen.
13. **IDEAL:** Auf DEV IDEAL ändert sich nichts Sichtbares (Schalter aus), es gibt keinen Protokoll-Eintrag „Wareneingang-CSV“.
14. Build und `dotnet test` grün. `NachlaufManifestDriftTests` ist mit dem neuen Eintrag grün.

## Test-Szenarien

**Unit-Tests** (`WareneingangCsvServiceTests.cs`):
- Anpassen: Zeile 82-83 (erwarteter Text ohne Kopfzeile), Zeile 189 (`StartWith("BC1;")` statt `"Barcode;"`), Zeile 218
  (`Split("\r\n")[0]`). Zusätzlich prüfen, dass genau eine Zeile entsteht.
- Neu, `[Theory]` über `CsvInhalt` mit Receipt → `StockMovement` → `User` im Speicher. Fälle: `jmuster` → `jmuster`;
  `AKE\jmuster` → `jmuster`; `jmuster@ake.at` → `jmuster`; ` jmuster ` → `jmuster`; NULL/`""`/`"  "` → `User.Name`;
  ohne Bewegung bzw. ohne User → `CreatedBy`; `CreatedByWindows = AKE\terminal01` ändert nichts (AK 3–7).
- Neu: Escaping-Fall mit `;`/`"` im `User.Name` (AK 9).
- Neu: `RunOnceAsync` mit `Users`-Eintrag und `StockMovement.UserId` → Datei enthält den Kontonamen (AK 8).

**Manuell: neues Kapitel `TS-153` in `docs/TESTSZENARIEN.md`.** Dazu bekommt TS-129.11 den Hinweis „ab v1.99.29 ohne Kopfzeile,
Erfasser siehe TS-153“. Die Zeile bleibt sonst stehen. Danach `secondbrain/tests/testszenarien-index.md` nachziehen.

Vorbedingungen wie TS-129: DEV AKE, Worktree-Stand v1.99.29 für Web und Dienst, Test-Freigabe, Übergabe per SQL bzw. „Als übergeben
markieren“. Kein Sage-Schreiben durch Agenten. Ein AD-Benutzer A (`WindowsUserName` gesetzt) und ein lokaler Benutzer L
(`WindowsUserName` NULL). SQL-Eingriffe nur auf DEV, Ausgangswerte vorher notieren und danach zurücksetzen.

| Nr | Schritt | Erwartet |
|---|---|---|
| 153.1 | A meldet sich per Windows an, bucht einen Wareneingang mit Barcode, Übergabe, ein Takt | Genau eine Datei. BOM, **eine** Zeile, 9 Felder, CRLF. Feld 7 = sam von A ohne Domäne. Felder 1–6, 8, 9 wie in TS-129.11. **Keine** Kopfzeile (AK 1–3) |
| 153.2 | A meldet sich per **Formular** an (z. B. an einem Terminal) und bucht | Feld 7 = sam von A, nicht `SYSTEM`, nicht das Terminal-Konto. SQL: `CreatedByWindows` des Receipts ist leer bzw. eine andere Identität (AK 7) |
| 153.3 | L bucht | Feld 7 = `User.Name` von L (AK 5) |
| 153.4 | Negativ, Domänenform: `UPDATE dbo.Users SET WindowsUserName = N'AKE\<sam>' WHERE Id = <A>`, buchen, übergeben; dann `N'<sam>@ake.at'`; danach Ausgangswert zurück | Beide Male Feld 7 = `<sam>` (AK 4) |
| 153.5 | Negativ, ohne UserId: vor der Übergabe `UPDATE dbo.StockMovements SET UserId = NULL WHERE Id = <Bewegung des ersten Receipts>` | Feld 7 = `PurchaseOrderReceipts.CreatedBy` dieses Receipts (AK 6) |
| 153.6 | Negativ, Sonderzeichen: Name von L auf `Muster; "Max"` setzen, buchen, übergeben | Feld 7 = `"Muster; ""Max"""`. Die Zeile hat weiter 9 Felder (AK 9) |
| 153.7 | DEV: „CSV erneut schreiben“ für einen Vorgang aus TS-129 (die Datei vorher von der Test-Freigabe entfernen) | Die neu geschriebene Datei ist ohne Kopfzeile, Feld 7 nach F2 (AK 11). Kein Übergangstest: Der Export ist noch nicht produktiv (Freigabe-Antwort 2) |
| 153.8 | Bestand: zu 153.1 SQL `SELECT CreatedBy FROM dbo.PurchaseOrderReceipts WHERE Barcode = …`; Sage-WE-Memo in der Liste „Sage-Wareneingänge“ bzw. im Payload-Log | `CreatedBy` = Anzeigename wie vorher. Memo ohne Benutzer, Format wie TS-116 (AK 10) |
| 153.9 | `/Help` → Wareneingang | „ohne Kopfzeile“, 9 Felder in Reihenfolge, Erfasser-Regel. Changelog v1.99.29 (AK 12) |
| 153.10 | DEV IDEAL nach Deploy | Keine Änderung sichtbar, kein Protokoll-Eintrag „Wareneingang-CSV“ (AK 13) |

## Etappen (nur bei epic: true)

Entfällt (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

> [!success] Final (QA 2026-11-16 gegen `git diff 4559e0a2..f12f46f7`)
> - **Web: ja** (nur `Views/Help/Index.cshtml`, `Views/Help/Changelog.cshtml`, `AppVersion.cs` – keine Logik). **Service: ja**
>   (`IDEALAKEWMSService/Services/WareneingangCsvService.cs`, `AppVersion.cs`). **Migration: nein** (keine Datei unter `*/Migrations/`,
>   kein `SQL/`). Frontmatter `deploy` stimmt.
> - **Paket:** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` – v1.99.29, `publish-info.txt` Commit
>   `8f24ec516516114560d435b858e26277de101fd6` (= HEAD, enthält `f12f46f7`; danach nur `docs/` und `nachlauf.psd1`), beide Komponenten,
>   `publish\deploy\nachlauf.psd1` enthält `v1.99.29-WareneingangCsvFormat` (Z. 411). `publish\deploy.zip` (07:31) ist ein Altstand und nicht
>   Teil dieses Pakets.
> - **Reihenfolge:** kein DB-Schritt; Web und Dienst gemeinsam per `deploy\Update-IdealAkeWms.ps1` (Info-Eintrag Nachher, nur AKE).
> - Ablauf Mensch: Publish aus dem Worktree → Testsystem → Test → Merge. **Nach dem Merge nur dann aus `main` neu publizieren, wenn der Merge
>   getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat.**

*Vorläufig (Spec-Agent). Durch den QA-Abschnitt oben bestätigt.*

> [!info] Abgleich gegen den echten Diff (Dev-Lauf 2026-11-16, Commit `f12f46f7`, `git diff 4559e0a2..f12f46f7`)
> 9 Dateien: `IDEALAKEWMSService/Services/WareneingangCsvService.cs` (Include + `CsvInhalt`), `IDEALAKEWMSService.Tests/Services/WareneingangCsvServiceTests.cs`,
> beide `AppVersion.cs` (1.99.29), `Views/Help/Changelog.cshtml`, `Views/Help/Index.cshtml`, `README.md` (Abschnitt Wareneingang-CSV: „ohne
> Kopfzeile“ statt „Kopfzeile“, 9 Felder – zusätzlich zu `affected_code`, weil README das alte Format nannte), `docs/TESTSZENARIEN.md` (TS-153,
> Hinweis TS-129.11), `scripts/deploy/nachlauf.psd1` (ein Info-Eintrag). Keine Migration, kein SQL, keine Einstellung, keine Rolle.
> **Abweichung:** Manifest-`Quelle` = `2026-11-16-v1-99-29-wareneingang-csv-enaio` (Name der Brain-Changelog-Datei laut Auftrag), nicht
> `…-wareneingang-csv-erfasser` wie unten im Entwurf. Unverändert laut Diff: `Receive.cs`, `SageWareneingangPayload.cs`,
> `SageLagerbuchungPayloadBuilder.cs`, `WarehouseRequisitionEmailService.cs`, `Views/PurchaseOrders/*`, `PurchaseOrdersController.SageWe.cs` (AK 10).

- **Web-App:** ja, nur Hilfe, Anwender-Changelog und `AppVersion` (keine Logik).
- **Service:** ja. `WareneingangCsvService` liegt im Dienst und schreibt die CSV (`SageBookingWorker.cs:153`).
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023), ein Eintrag. Der Text ist nur ASCII, weil PS 5.1 die
  `.psd1` ohne BOM als ANSI liest (Dateikopf Zeile 2):
  ```powershell
  @{ Id='v1.99.29-WareneingangCsvFormat'; Version='1.99.29'; Typ='Info'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
     Text='Wareneingang-CSV: Format ohne Kopfzeile, Erfasser = Windows-Kontoname ohne Domaene - das ist das enaio-Zielformat.'
     Quelle='2026-11-16-v1-99-29-wareneingang-csv-erfasser' }
  ```
  - **Empfehlung: Eintrag behalten.** Gerald wollte einen Hinweis „Formatänderung für enaio“. Der Eintrag kostet eine Zeile und
    dokumentiert den Formatwechsel für den enaio-Betreuer. Er ist kein Hand-Schritt mehr.
  - **Typ Info, Phase Nachher:** Eine Abstimmung vor dem Deploy ist nicht mehr nötig (Freigabe-Antworten 1 und 2), daher Nachher.
    *Überholt:* Phase Vorher und der Text „vor Deploy mit enaio-Betreuer abstimmen … Import umstellen … Freigabe vorher leer“.
  - **Firma AKE:** Der Export ist auf AKE im Einsatz, IDEAL hat den Schalter aus. Belege: `Pruefung/WareneingangCsv` und
    `v1.99.11-WareneingangCsv` mit `Firma='AKE'` (`nachlauf.psd1:142,338`), Vorgänger-Spec `standort: AKE`, TS-129.18.
  - Kein Pruefung-Eintrag: Es gibt keinen SQL-prüfbaren Zustand.
  - Einordnung im Block „Nachher“ nach Version. Die Reihenfolge ist hier nicht deploy-kritisch.
- **Publish-Befehle** (im Worktree):
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```

### Prüfabfragen (nur lesend, AKE-DB `IDEAL_AKE_WMS`; Ergebnis vor dem Dev-Lauf bzw. vor dem Deploy an Gerald)

```sql
-- P-1: Formen von WindowsUserName im Benutzerstamm (nur Kontrolle, kein Blocker; '@' kommt laut Freigabe-Antwort 3 nicht vor)
SELECT Form, COUNT(*) AS Anzahl
FROM (SELECT CASE
        WHEN WindowsUserName IS NULL THEN '1 NULL (lokal)'
        WHEN LTRIM(RTRIM(WindowsUserName)) = N'' THEN '2 leer'
        WHEN WindowsUserName LIKE N'%\%' THEN '3 mit Backslash (DOMAIN\name)'
        WHEN WindowsUserName LIKE N'%@%' THEN '4 mit @ (UPN oder sam mit @)'
        WHEN WindowsUserName <> LTRIM(RTRIM(WindowsUserName)) THEN '5 Leerzeichen am Rand'
        ELSE '6 sam' END AS Form
      FROM dbo.Users) x
GROUP BY Form ORDER BY Form;

-- P-2: Wareneingänge mit Barcode, deren Bewegung keinen Benutzer hat (Rückfall auf CreatedBy)
SELECT COUNT(*) AS OhneUserId
FROM dbo.PurchaseOrderReceipts r JOIN dbo.StockMovements m ON m.Id = r.StockMovementId
WHERE r.Barcode IS NOT NULL AND m.UserId IS NULL;

-- P-3: Vorschau bisheriger und künftiger Erfasser (letzte 20 Wareneingänge mit Barcode)
SELECT TOP 20 r.Id, r.Barcode, r.CreatedBy AS ErfasserBisher, u.WindowsUserName, u.Name AS UserName, r.CreatedByWindows
FROM dbo.PurchaseOrderReceipts r
JOIN dbo.StockMovements m ON m.Id = r.StockMovementId
LEFT JOIN dbo.Users u ON u.Id = m.UserId
WHERE r.Barcode IS NOT NULL ORDER BY r.Id DESC;

```

- *Überholt (Freigabe-Antworten 1 und 2):* **P-4** (Schalterstand des Exports, zu Rückfrage 1) und **P-5** (Dateien auf der Freigabe vor
  dem Deploy, zu Rückfrage 2). Der Export ist noch nicht produktiv, und eine Import-Umstellung gibt es nicht, deshalb entfallen beide.

### Brain-Pflichten des Dev-Laufs

- [[fallstricke]] §32 ergänzen: „Ohne Kopfzeile ist die Spaltenreihenfolge der Vertrag mit enaio. Eine neue Spalte nur am Ende anhängen
  und vorher mit enaio abstimmen. **Warum:** Der Import ordnet nach Position zu.“
- Ein Changelog-Eintrag `2026-11-16-v1-99-29-wareneingang-csv-erfasser` (= `Quelle` im Manifest).
- [[feature-map]], [[testszenarien-index]].

## Nachtrag 2026-11-16: Freigabe-Antworten in den Rumpf gezogen

Die Antworten von Gerald (Block „Freigabe-Antworten“, unverändert) sind in den Rumpf übernommen. Geändert wurde:
- **Frontmatter:** `open_questions: []`, neu `beantwortete_fragen` (1–3). `status` und `freigabe_*` sind unverändert.
- **Ziel / Nutzen:** Ohne Kopfzeile ist das enaio-Zielformat, keine Import-Umstellung; der Export ist noch nicht produktiv (Antworten 1, 2).
- **Umfang / Out-of-Scope:** Keine Prod-Altdateien. Keine Umformung der Schreibweise (Antwort 3, Ponytail: kein Zusatzcode).
- **F2:** „Schreibweise wie gespeichert“ ist entschieden.
- **Grenze von ExtractSam:** von Warnung auf Hinweis herabgestuft. Ein `@` kommt nicht vor (Antwort 3), P-1 bleibt nur als Kontrolle.
  Der alte Satz ist als überholt markiert.
- **AK 11 und TS-153.7:** Der Übergangsfall entfällt (überholt markiert). Es bleibt nur „CSV erneut schreiben“ schreibt das neue Format.
- **Deploy/Manifest:** Info-Eintrag behalten, aber Phase **Nachher** mit knappem Text „ist enaio-Zielformat“. Die Abstimmung vor dem
  Deploy entfällt (überholt markiert).
- **Prüfabfragen:** P-4 und P-5 entfallen (überholt markiert). P-1 ist nur Kontrolle, P-2 und P-3 bleiben.
- Das Risiko „enaio liest ab dem Deploy nichts mehr“ besteht nicht mehr, weil der Import keine Kopfzeile erwartet und der Export
  nicht produktiv ist.

## Offene Rueckfragen

1. **enaio-Import umstellen und Zeitpunkt.** Überspringt der enaio-Import heute die erste Zeile als Kopfzeile, oder ordnet er über
   Spaltennamen zu? Dann liest er ab dem Deploy die Datenzeile nicht mehr bzw. findet die Spalten nicht.
   *Empfehlung:* Den Import vor dem Deploy auf 9 Spalten nach Position ohne Kopfzeile umstellen, Umstellung und Deploy im selben
   Fenster. Ist `WareneingangCsvAktiv` auf AKE-Prod noch aus (P-4), entfällt das Risiko bis zum Einschalten.
2. **Dateien im Übergang.** Dateien im alten Format (mit Kopfzeile), die beim Deploy noch auf der Freigabe liegen, schreibt das WMS nicht
   um. Soll die Freigabe vor dem Deploy leer sein (enaio hat alles abgeholt), oder muss der Import beide Formate verkraften?
   *Empfehlung:* Freigabe vor dem Deploy leer (P-5). Das steht so im Manifest-Text.
3. **Schreibweise des Kontonamens.** Das WMS gibt `WindowsUserName` so aus, wie es gespeichert ist (Groß-/Kleinschreibung aus dem AD,
   z. B. `JMuster`). Braucht enaio eine feste Schreibweise, z. B. klein?
   *Empfehlung:* Unverändert ausgeben, enaio vergleicht ohne Groß-/Kleinschreibung. Eine Umformung wäre eine weitere Formatregel ohne
   belegten Bedarf.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. enaio-Import umstellen und Zeitpunkt → **Gerald 2026-11-16: Der enaio-Import darf keine Überschriften haben – die Datenzeile ohne Kopfzeile ist genau das Zielformat, keine Umstellung des Imports nötig.**
2. Dateien im Übergang (Freigabe vor Deploy leer?) → **Gerald 2026-11-16: Ist noch nicht produktiv – es gibt keine Altdateien, kein Übergang zu regeln.**
3. Schreibweise des Kontonamens (unverändert oder fest klein?) → **Gerald 2026-11-16: Kann immer klein sein, egal. Zusatz: Ein `@` sollte im Kontonamen nicht vorkommen (P-1-Grenze von `ExtractSam` damit unkritisch).**

## QA-Nachweis (qa-agent, 2026-11-16)

**Ergebnis: Testbereit.** Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Zweig `feature/2026-08-07-ideal-teile-1-5`, HEAD `8f24ec51`,
reviewt: `4559e0a2..f12f46f7` (9 Dateien). Keine Fix-Commits.

### 1. Build und Tests (HEAD `8f24ec51`)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (Bestand).
- `dotnet test`: `IDEALAKEWMSService.Tests` 663 bestanden / 0 übersprungen / 0 Fehler; `IdealAkeWms.Tests` 2952 bestanden / 1 übersprungen
  (Bestand) / 0 Fehler.

### 2. Code-Review (selbst, in diesem Lauf)
Geprüft: kompletter Diff `4559e0a2..f12f46f7`, `WareneingangCsvService.cs` ganz (Lauf, Schreiben, `CsvInhalt`), `WindowsAccountHelper.ExtractSam`,
`SageBookingWorker.cs:64-154` (Gate), einziger Aufrufer von `CsvInhalt` = `WareneingangCsvService.cs:118` (Grep), `CsvErneutSchreibenAsync`
(`PurchaseOrderReceiptRepository.cs:159`), Hilfe, Changelog, README, TESTSZENARIEN, Manifest.
- Nur die CSV geändert: `Receive.cs`, `SageWareneingangPayload.cs`, `SageLagerbuchungPayloadBuilder.cs`, `WarehouseRequisitionEmailService.cs`,
  `Views/PurchaseOrders/*`, `PurchaseOrdersController.SageWe.cs` sind nicht im Diff (`git diff --stat`) – AK 10 belegt.
- Erfasser-Regel `ExtractSam(u?.WindowsUserName) ?? u?.Name ?? r.CreatedBy` (`WareneingangCsvService.cs:141-142`): `CreatedByWindows` und
  `StockMovement.WindowsUser` werden nicht gelesen. `Include(StockMovement).ThenInclude(User)` auf der `AsNoTracking`-Abfrage (Z. 64).
- Kopfzeile entfernt; `F`/`D`, Feldreihenfolge, CRLF, BOM (Schreiber Z. 118) unverändert.
- Defektklassen der Checkliste: kein Script-Embedding, kein `hidden`, keine reine Browser-Validierung, eine einzige Implementierungsstelle;
  der Rückfall `?? r.CreatedBy` ist fachlich gewollt (F2 Regel 3), kein stiller Ersatzwert.

Befunde: **keine blockierenden.** Hinweise (ohne Änderung):
- H-1: `ExtractSam("AKE\")` liefert `""` statt `null` → Feld 7 wäre leer statt `User.Name`. Nur bei von Hand kaputtem `WindowsUserName` möglich
  (einziger Schreibweg ist `CreateAdUser` aus der AD-Liste); laut Spec kein Zusatzcode → akzeptiert.
- H-2: Die Hilfe nennt beim Quoting nur Strichpunkt und Anführungszeichen, nicht Zeilenumbruch (der Code quotet auch CR/LF). Kosmetisch.
- H-3: `publish\deploy.zip` (16.11. 07:31) ist ein Altstand im Paketordner – nicht verwenden.

### 3. Nachweisart je AK
| AK | Nachweis |
|---|---|
| 1 ohne Kopfzeile | automatisiert (`WareneingangCsvServiceTests` Z. 82, `CsvInhalt_LeereWerteLeer…`) + DEV (Bytes: BOM, 1 Zeile, CRLF) |
| 2 Reihenfolge | automatisiert (Z. 83, Werte wie vorher, ohne Kopfzeile) + DEV |
| 3 AD-Benutzer | automatisiert (`CsvInhalt_Erfasser_AusBenutzerDerBewegung`) + DEV (`gerald.weichbold` statt `Gerald Weichbold`) |
| 4 Domänenformen | automatisiert + DEV (`AKE\…`, `…@ake.at` → sam) |
| 5 lokaler Benutzer | automatisiert (NULL/""/"  ") + DEV (lokaler Benutzer Id 21) |
| 6 ohne UserId | automatisiert + DEV (`UserId = NULL` → `CreatedBy`) |
| 7 nicht die Sitzung | automatisiert (`CreatedByWindows = AKE\terminal01`); auf DEV erscheint `CreatedByWindows = AKENB\…` des Receipts nicht. Formular-Anmeldung am Terminal: **Manual-UAT** |
| 8 Include wirkt | automatisiert (InMemory, `ChangeTracker.Clear`) **und** auf echtem SQL Server (DEV-Sonde, s. 6c) |
| 9 Escaping | automatisiert + DEV (`"Muster; ""Max"""`) |
| 10 Bestand | Diff-Review (s. 2); Bestandstests der Klassen ohne Anpassung grün |
| 11 erneut schreiben | DEV (Knopf „CSV erneut schreiben“ + Lauf → neues Format) |
| 12 Hilfe | DEV-Browser AKE (`/Help` „Format (seit v1.99.29 ohne Kopfzeile)“, Changelog v1.99.29) |
| 13 IDEAL | DEV SQL (`SyncLogs` Service `WareneingangCsv` = 0, Schalter aus) |
| 14 Build/Tests/Manifest | automatisiert (`NachlaufManifestDriftTests` im Testlauf grün) |

Der echte Dienstweg (Worker-Takt) ist auf DEV nicht prüfbar: Der CSV-Schritt läuft nur bei `SageWareneingangBelegAktiv = an`
(`SageBookingWorker.cs:73,93`), und das schreibt Sage-Belege → **Manual-UAT** auf dem Testsystem.

### 4. Geänderte Bestandstests
`WareneingangCsvServiceTests.cs` Z. 82-83, 189, 218: Erwartung ohne Kopfzeile – **Verhalten absichtlich geändert (AK 1/2)**. Sonst keine
Bestandstests geändert, kein „Sonstiges“.

### 5. Checkliste
Keine Migration/SQL/FreshInstall/AgentJobs (nicht betroffen). Version 1.99.29 in beiden `AppVersion.cs`, Anwender-Changelog, Brain-Changelog
[[2026-11-16-v1-99-29-wareneingang-csv-enaio]], README, `docs/TESTSZENARIEN.md` TS-153 + Hinweis TS-129.11, [[testszenarien-index]] Zeile 153,
[[fallstricke]] (Spaltenreihenfolge = Vertrag), [[feature-map]] (jetzt Testbereit). Manifest `v1.99.29-WareneingangCsvFormat` (Info, AKE,
Nachher, ASCII) vorhanden; `Quelle` = Name der Brain-Changelog-Datei (stimmt). Audit-Felder `WareneingangCsvExporte` wie bisher.
**5a ADR 0020:** keine neue oder geänderte Einstellung.

### 6. DEV
- a. Standort laut Frontmatter **AKE**; IDEAL nur Gegenprobe AK 13.
- b. Kein Neu-Deploy: DEV AKE und IDEAL laufen mit dem HEAD-Build aus dem vorigen QA-Lauf (Binärdateien 16.11. 18:24, nach `f12f46f7`;
  `8f24ec51` ändert nur `docs/` und das Manifest). Fußzeile AKE `v1.99.29` (Browser), IDEAL-Changelog zeigt v1.99.29; beide Dienste Running.
- c. TS-153 mit DEV-Daten: Receipt 10, Barcode `AKE1235564654`, Vorgang `8CC7C3DB…`, Bewegung 19402, Benutzer 14 (AD) und 21 (lokal). Der
  Dienst-Schritt ist ohne Sage-Schreiben nicht auslösbar. Ersatzweise rief eine Wegwerf-Konsole (Scratchpad, referenziert das Dienstprojekt
  des Worktrees) `WareneingangCsvService.RunOnceAsync` gegen `AKE_WMS_DEV` auf (echter SQL Server, echter `SyncLogger`), Ziel
  `D:\Dev\csv-ausgang\ake`. Die alte Datei (mit Kopfzeile) liegt als `AKE1235564654.v1-99-11-mit-kopfzeile.csv.bak` daneben. SQL-Eingriffe
  zurückgesetzt (Users 14/21, Bewegung 19402 `UserId = 14`, geprüft). Export-Zeile 3 steht auf Status 2, `Datei` jetzt `D:\…` statt UNC (DEV-Testdatum).

| Nr | Erledigt | Ergebnis | Status |
|---|---|---|---|
| 153.1 | Äquivalent: AD-Benutzer `gerald.weichbold`, Lauf über die Sonde | `AKE1235564654;K31705;Schachermayer GmbH;2027-168594;WMS-TEST-CSV;TEST-CSV-1;gerald.weichbold;2026-11-12 17:24;2026-11-12 17:26` – BOM 239,187,191, 1 Zeile, Ende CR LF, keine Kopfzeile; `SyncLogs` WareneingangCsv „geschrieben=1, fehler=0“ | OK (echte Buchung + Dienst-Takt: NICHT MÖGLICH, braucht Sage-Schreiben) |
| 153.2 | – | Formular-Anmeldung am Terminal + Buchung + Übergabe braucht Sage-WE an | NICHT MÖGLICH (Unit-Test AK 7 grün) |
| 153.3 | `UserId` → lokaler Benutzer 21 | Feld 7 = `User.Name` | OK (mit 153.6 kombiniert) |
| 153.4 | `WindowsUserName = AKE\gerald.weichbold`, dann `gerald.weichbold@ake.at` | beide Male `gerald.weichbold` | OK |
| 153.5 | `StockMovements.UserId = NULL` | Feld 7 = `Gerald Weichbold` (= `CreatedBy`) | OK |
| 153.6 | Name von 21 = `Muster; "Max"` | Feld 7 = `"Muster; ""Max"""`, 9 Felder | OK |
| 153.7 | Export per SQL auf Fehler, Knopf „CSV erneut schreiben“ im Browser → „CSV zum erneuten Schreiben vorgemerkt“, Lauf | neue Datei ohne Kopfzeile, Feld 7 nach F2 | OK (Lauf per Sonde statt Dienst) |
| 153.8 | SQL Receipt 10 | `CreatedBy = Gerald Weichbold` unverändert, Memo `WE#DEV#10#` ohne Benutzer | OK |
| 153.9 | Browser `/Help` und `/Help/Changelog` | „Format (seit v1.99.29 ohne Kopfzeile)“, 9 Felder, Erfasser-Regel; Changelog v1.99.29 | OK |
| 153.10 | SQL `IDEAL_WMS_DEV` | `WareneingangCsvAktiv`/`SageWareneingangBelegAktiv` false, 0 `SyncLogs` WareneingangCsv; Changelog v1.99.29 sichtbar | OK |

Keine Einstellung auf DEV umgeschaltet; nichts an Sage, Test oder Prod.

### 7. Publish
Im vorigen QA-Lauf erstellt; HEAD seitdem unverändert, daher nicht wiederholt: `…\2026-08-07-ideal-teile-1-5\publish\` (IDEALAKEWMSWeb,
IDEALAKEWMSWebService, SQL, deploy), `publish-info.txt` Version 1.99.29, Commit `8f24ec51…` (= HEAD).

### Manuelle Testcheckliste (offen für Gerald)
1. Testsystem AKE: Paket einspielen (Web + Dienst), Fußzeile v1.99.29, Manifest-Info `v1.99.29-WareneingangCsvFormat` erscheint.
2. TS-153.1 echt: AD-Benutzer per Windows bucht einen WE mit Barcode, Übergabe an Sage (Testsystem mit `SageWareneingangBelegAktiv` und
   `WareneingangCsvAktiv` an), ein Takt → Datei ohne Kopfzeile, Feld 7 = sam.
3. TS-153.2: derselbe Benutzer per Formular an einem Terminal → Feld 7 = sam, nicht `SYSTEM` bzw. Terminal-Konto.
4. TS-153.3: lokaler Benutzer bucht → Feld 7 = WMS-Name.
5. enaio liest die Datei (9 Felder nach Position) korrekt ein.
6. Optional: Prüfabfragen P-1–P-3 auf Prod vor dem Deploy.
