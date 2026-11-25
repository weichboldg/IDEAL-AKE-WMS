---
type: aufgabe
status: offen
created: 2026-11-24
updated: 2026-11-24
quelle: Zuruf Gerald 2026-11-24 (ohne Spec), QA durch qa-agent
worktree: .claude/worktrees/2026-08-07-ideal-teile-1-5
branch: feature/2026-08-07-ideal-teile-1-5
commits: 3929f146 (Fix), 595dc46c (QA-Korrektur)
---
# Hotfix Deploy: AKE-Test-Pfade + Einmalhilfe Set-Startkennwort - QA

Anlass: `Update-IdealAkeWms.ps1` brach auf akenet01 mit "C:\inetpub\wwwroot\IDEALAKEWMS_Test fehlt (AKE-Test)" ab;
AKE-Prod blockiert `Pruefung/BenutzerOhneKennwort` (`scripts/deploy/nachlauf.psd1:156`) mit 17 lokalen Benutzern ohne Kennwort.
Entscheid Gerald: gemeinsames Startkennwort + Pflichtwechsel (Regel P, ADR 0027).

## Gepruefter Umfang
- `git show 3929f146`: `scripts/deploy/ziele.psd1:12`, `docs/betrieb/02-systeme-umgebungen.md:41`, `docs/TESTSZENARIEN.md` TS-125.3,
  neu `scripts/deploy/Set-Startkennwort.ps1`.
- Gegengelesen: `IdealAkeWms/Services/PasswordService.cs` (Hash-Format), `nachlauf.psd1:156-159` (Pruefung),
  `Update-IdealAkeWms.ps1:138-160` (Get-Umgebung/Get-Einstellung), `IdealAkeWms.Tests/Deploy/NachlaufManifestDriftTests.cs`,
  `IdealAkeWms/Models/User.cs`, `UsersController.cs:82`.
- Alte AKE-Test-Pfade sonst nirgends erwartet: kein Test, kein Drift-Guard, `Install-IdealAkeWms.ps1`/`IdealAkeWms.Deploy.psm1` ohne Pfade.
  `README.md:616` nennt `IDEALAKEWMS_Test` nur als Beispiel (gilt fuer IDEAL-Test weiter, richtig).

## Befunde

| Nr | Schwere | Fundstelle | Befund | Behandlung |
|---|---|---|---|---|
| B-1 | hoch | `Set-Startkennwort.ps1` Rueckfrage `"... $aktion? (ja/nein)"` | `?` gehoert in PS zum Variablennamen: unter StrictMode Abbruch "Variable $aktion? nicht festgelegt" - `-Schritt Vorher`/`Nachher` brachen immer vor der Rueckfrage ab (Rollback, keine Daten geaendert). Der vorhandene Drift-Test `DeploySkripte_KeineVariableMitFragezeichenImText` war dadurch **rot** - der Commit wurde ohne Testlauf erstellt. | korrigiert `$($aktion)?` (595dc46c) |
| B-2 | mittel | Verbindungsermittlung | fest `appsettings.Production.json` vor `appsettings.json`, `ASPNETCORE_ENVIRONMENT` aus `web.config` ignoriert. Auf DEV (`DEV`) haette das Skript die Basis-Datei gelesen = **AKESQL20/IDEAL_AKE_WMS**; gleiches Risiko auf jedem Testsystem mit eigener Umgebung. | korrigiert: Umgebung aus `web.config` wie `Update-IdealAkeWms.ps1` Get-Umgebung; Ausgabe "Umgebung X, Datenbank ..." vor jeder Eingabe |
| B-3 | mittel | Kennwort-Eingabe Vorher | nur eine verdeckte Eingabe: ein Tippfehler setzt allen Betroffenen ein unbekanntes Kennwort, `-Schritt Nachher` findet sie dann nicht (kein Pflichtwechsel). | korrigiert: Vorher fragt zweimal (`-cne`), Abbruch ohne Aenderung |
| B-4 | niedrig | Filter Vorher vs. Pruefung | Pruefung zaehlt bei `WindowsAuthAktiv` aus auch Benutzer **mit** WindowsUserName; das Skript fasst sie bewusst nicht an - die Aussage "danach besteht die Pruefung" stimmt dann nicht. | korrigiert: Vorher gibt nach dem Commit die Zaehlung mit der Pruefungs-Abfrage aus und nennt den Rest |
| B-5 | niedrig | BSTR | Klartext-BSTR wurde nicht freigegeben. | korrigiert: `ZeroFreeBSTR` |
| I-1 | Info | Sicherheit | Gemeinsames Startkennwort: bis zur ersten Anmeldung kann jeder mit Benutzername + Startkennwort dieses Konto nutzen. Von Gerald so entschieden; Zeitfenster kurz halten, Startkennwort nicht per Sammelmail. | offen fuer den Menschen |
| I-2 | Info | Manifest | Skript ist kein Eintrag in `nachlauf.psd1` (manuelle Einmalhilfe nur AKE-Prod); Pruefungstext verweist weiter auf `/Users/Edit`. Ausreichend fuer einen Einmallauf. | keine |

Korrekt befunden: Hash (PBKDF2-HMACSHA256, 100000, 16 Salt + 32 Hash, Base64, UTF8) = `PasswordService`; Verify-Pfad
(Base64-Fehler und Laenge != 48 -> false); Transaktion (Rollback bei "nein"/leer, `ExecuteNonQuery -ne 1` -> throw, Dispose ohne Commit
= Rollback); SQL nur parametrisiert (`@h`, `@w`, `@id`); Audit `ModifiedAt/By/ByWindows` gesetzt; erneuter Lauf idempotent
(Vorher: `PasswordHash IS NULL`-Guard im UPDATE, Nachher: `MussKennwortAendern = 0` im Filter).

## Nachweis
- Build `dotnet build IdealAkeWms.slnx`: 0 Fehler.
- Tests vor Korrektur: IdealAkeWms.Tests **1 Fehler** (`DeploySkripte_KeineVariableMitFragezeichenImText`), 3492 ok, 1 uebersprungen; Service 790/790.
- Tests nach 595dc46c: IdealAkeWms.Tests 3493 ok / 1 uebersprungen / 0 Fehler; IDEALAKEWMSService.Tests 790 ok / 0 / 0.
- Parse PS 5.1.26100: 0 Fehler.
- Echter Lauf Windows PowerShell 5.1 gegen `AKE_WMS_DEV` (`-WebPfad D:\Dev\AKE\Web`, Eingaben ueber Read-Host-Ersatz):
  Abweichende Wiederholung -> Abbruch, nichts geaendert; "nein" -> nichts geaendert; Vorher "ja" -> 7 Benutzer gesetzt, Rest 5 gemeldet
  (WindowsAuthAktiv dafuer kurz auf false); Vorher erneut -> "keine Benutzer"; Nachher falsches Kennwort -> "keine Benutzer";
  Nachher richtig -> 7 Benutzer `MussKennwortAendern = 1` (admin unberuehrt); Nachher erneut -> "keine Benutzer".
  DB: `ModifiedBy = Set-Startkennwort`, `ModifiedByWindows = AKENB\gerald.weichbold`, Hash 64 Zeichen.
- Gegenprobe: ein vom Skript erzeugter Hash verifiziert mit `Microsoft.AspNetCore.Cryptography.KeyDerivation.Pbkdf2` = True.
- DEV danach zurueckgesetzt (PasswordHash NULL, MussKennwortAendern 0, alte Audit-Werte, WindowsAuthAktiv true).
- Kein DEV-Deploy (keine Anwendungscode-Aenderung), kein Publish.

## Manuelle Pruefung (offen, Mensch)
1. akenet01: `Update-IdealAkeWms.ps1 -Firma AKE -System Test -NurPruefen` findet `C:\inetpub\wwwroot\IdealAkeWMS-TEST` und `C:\AKE\IDEALAKEWMSService-Test`, Dienst/Pool passen.
2. AKE-Prod vor dem Update: DB-Backup; `Set-Startkennwort.ps1 -Schritt Vorher -WebPfad C:\inetpub\wwwroot\IDEALAKEWMS` - angezeigte Umgebung/Datenbank pruefen, Liste der 17 Benutzer gegen P-1 der Spec abgleichen, Restzaehlung muss 0 sein.
3. Ausfuehrender Windows-Benutzer braucht Schreibrecht auf `dbo.Users` (Verbindung mit Trusted_Connection laeuft unter dem eigenen Konto, nicht dem App-Pool).
4. Nach dem Update: `-Schritt Nachher`, dann Anmeldung eines betroffenen Benutzers -> Pflichtwechsel-Maske erscheint.
5. Nicht gepusht, nicht gemergt.

## QA Hotfix SData PpsRessourcenPositionenInfo (Commit c505688f)

Anlass: AKE-Prod Artikel-Sync fehlgeschlagen, HTTP 500 "q.BelPosID ... nicht in GROUP BY" (SData gruppiert bei `select`).
Fix: `IDEALAKEWMSService/Services/Sdata/SdataArtikelReader.cs:86` `OrderByRessourcen = "Ressourcenummer"` (Aufruf `:220-221`).

### Befunde

| Nr | Schwere | Fundstelle | Befund | Behandlung |
|---|---|---|---|---|
| S-1 | hoch | `IDEALAKEWMSService/Services/Sdata/ArtikelLieferantSyncService.cs:37` + `:42` (Aufruf `:76-77`) | Gleiches Muster: select `Artikelnummer,Hauptlieferant`, orderBy `Artikelnummer,AuspraegungID`. Vom Hauptagenten auf AKE-Prod bestaetigt (SyncLogs Id 4883802). | Hauptagent baut zentralen Fix in `SageSdataReadClient.BuildQueryUrl` (fehlende orderBy-Felder an select anhaengen); QA dieses Commits folgt separat |
| S-2 | mittel (Risiko, unbelegt) | `IdealAkeWms/Services/SageSdata/SageSdataReadClient.cs:206-213` | Unter GROUP BY ist offen, ob `$totalResults` die Gruppen oder die Rohzeilen zaehlt. Zaehlt Sage Rohzeilen, bricht QueryAllAsync mit "Unvollstaendig gelesen" ab (leere Seite vor gesamt) - laut, nicht still. | Manual-UAT: naechster Artikel-Sync-Lauf auf AKE-Prod im Aktivitaets-Protokoll |
| I-1 | Info | `SdataArtikelReader.cs:220-227` | Unter GROUP BY liefert Prod nur verschiedene Ressourcenummern (deutlich < 240.552). Waechter 0 Zeilen / alle leer bleiben korrekt; Weiterverarbeitung `:231-232` nutzt nur die Menge (DistinctBy) - kein Verlust. Zusammenfassung/Hinweis zeigt dann die kleinere Zahl (kein Fehler). | keine |
| I-2 | Info | Begruendung Paging | Ohne Gruppierung (AKE-Test) haben gleichwertige Sortwerte dieselben gelesenen Werte -> der k-te Wert der Folge ist eindeutig, Seiten stabil. Grenzfall CI-Kollation ('A1'/'a1' bzw. Leerzeichen am Ende gleichrangig) kann ueber Seitengrenzen die Schreibweise tauschen; wirkungslos, da `Schluessel`/OrdinalIgnoreCase ohnehin zusammenfasst. Seitengroesse 5000 unveraendert unkritisch. | keine |
| I-3 | Info | Kommentar `SdataArtikelReader.cs:83-85` | Nach dem zentralen Fix (S-1) waere die orderBy-Aenderung nicht mehr noetig, schadet aber nicht (weniger Zeilen unter Gruppierung). Kommentar dann ggf. auf den zentralen Fix verweisen. | Hauptagent |

Geprueft ohne Befund (orderBy-Felder im select bzw. ohne select): `ArtikelLieferantSyncService.cs:72` (Artikelnummer,Lieferant), `EinkaufBestellungenSyncService.cs:113` (BelPosId), `:286` RealTimeData (BelID),
`KontokorrentSyncService.cs:134` (Kto), `:148` (Adresse), `:150` (Nummer), `LetzteBestellungSyncService.cs:112` (BelPosId), `PpsMitarbeiterSyncService.cs:53` (Nummer),
`SdataArbeitsplatzReader.cs:41`, `SdataArtikelReader.cs:156/166/176/217/234/240/248/366/390`, `SdataBestandReader.cs:55/100` (ohne select), `:57`, `SdataBuchungLookupReader.cs:42`,
`SdataFaStrukturReader.cs:88/110/135/224`, `SdataLagerplatzReader.cs:45/47`, `SdataWareneingangLookupReader.cs:65`, `StillstandsgruendeSyncService.cs:51`,
Web `SageFaArbeitsgaenge.cs:94/96`, `SageTestController.cs:105/163/183/194/207/220` (QueryPageAsync ohne select), `SageSdataFaController.cs:89/112/136` (BelID im select).

### Nachweis
- `dotnet build IdealAkeWms.slnx`: 0 Fehler.
- IdealAkeWms.Tests 3493 ok / 1 uebersprungen / 0 Fehler; IDEALAKEWMSService.Tests 790 ok / 0 / 0 (Stand c505688f).
- Live-Nachweis (3): NICHT MOEGLICH am Test - AKE-Test gruppiert nicht: DEV-Dienst (SData = sagetest01, ake_TEST2026;1) las am 2026-11-13
  `PpsRessourcenPositionenInfo` mit select Ressourcenummer + orderBy Ressourcenummer,BelPosID fehlerfrei, 240552 Zeilen in 49 Seiten
  (`D:\Dev\AKE\Service\logs\service-20261113.log` 08:40:59). Der Fehler ist dort nicht nachstellbar, also auch der Fix nicht. Kein DEV-Deploy, kein Prod-Zugriff durch QA.

### Manuelle Pruefung (offen, Mensch)
1. Nach Deploy auf AKE-Prod: Artikel-Sync-Lauf im Aktivitaets-Protokoll erfolgreich, Hinweis `PpsRessourcenPositionenInfo` mit Zeilenzahl > 0 und ohne "Unvollstaendig gelesen" (S-2).
2. Artikel-Lieferanten-Lauf auf AKE-Prod erfolgreich (nach zentralem Fix S-1), `hauptlieferant` > 0.
3. Optional lesend im Browser auf AKE-Prod: `.../PpsRessourcenPositionenInfo?format=json&select=Ressourcenummer&where=Ressourcenummer like '_%'&orderBy=Ressourcenummer&count=5&startIndex=0` -> 200, `$totalResults` notieren.

## QA zentraler Fix f420e23e + Revert c476a628

Loesung: `IdealAkeWms/Services/SageSdata/SageSdataReadClient.cs:176` ruft `MitSortfeldern(select, orderBy)` (`:186-196`) auf; jedes orderBy-Feld,
das im select fehlt, wird angehaengt. `c476a628` nimmt c505688f zurueck (`SdataArtikelReader.cs:83` wieder `Ressourcenummer,BelPosID`).
Damit erledigt: S-1 (ArtikelLieferant), S-2 (mit eindeutigem BelPosID im select ist GROUP BY wirkungslos, `$totalResults` = gelieferte Zeilen), I-1.

### Pruefung MitSortfeldern
- Gross/Klein: `OrdinalIgnoreCase`, Schreibweise aus select bleibt (Test `"a, B"` + `"b desc, c asc"` -> `a,B,c`). Leerzeichen: `TrimEntries` + leere Teile entfernt.
  `asc`/`desc`: erstes Wort des Teils. Leeres select: unveraendert kein `&select` (Guard `:176`). orderBy ist nie leer (Guard `QueryAllAsync`).
- Nebenwirkung: select wird normalisiert (Leerzeichen nach Kommas entfallen) - harmlos.

### Wirkung je Aufrufer
Anhaengen passiert nur, wo ein Sortfeld fehlte:
- `ArtikelLieferantSyncService.cs:76-77`: + `AuspraegungID`. Typisierter Record `SdataHauptlieferantZeile`, unbekannte JSON-Felder ignoriert (`PagingJsonOptions`
  ohne UnmappedMemberHandling). Mehrere Varianten je Artikel landen im HashSet `:103-105` (Duplikate egal), Waechter `haupt.Count > 0` `:99` unveraendert.
- `SdataArtikelReader.cs:220-221`: + `BelPosID`. Record `SdataRessourceZeile`, `DistinctBy` `:231-232`, Waechter 0 Zeilen/alle leer `:223-227` unveraendert.
  Zeilenzahl wie am AKE-Test (240.552) statt der verschiedenen Nummern - langsamer, aber konsistent.
- Alle anderen Aufrufer aus der Liste oben: select enthielt die Sortfelder schon -> URL inhaltlich gleich (keine Dictionary-Aufrufer mit Zusatzfeld,
  Benutzerfeld-Pruefung "in keiner Zeile geliefert" unberuehrt).
- Gewollte Gruppierung: keine. Kein Aufrufer verlaesst sich auf GROUP BY (am AKE-Test gruppiert Sage nicht; alle Aufrufer verarbeiten Einzelzeilen).

### Befunde
| Nr | Schwere | Fundstelle | Befund | Behandlung |
|---|---|---|---|---|
| R-1 | Info | `SageSdataReadClient.cs:83` (RealTimeData `GetAsync`) | Eigener URL-Bau, nicht ueber `MitSortfeldern`. Heute unkritisch: alle Aufrufer (`SageSdataFaController.cs:89/112/136`, `SageTestController.cs:220`, `EinkaufBestellungenSyncService.cs:286`, `SdataFaStrukturReader.cs:224`) haben `BelID` in select und orderBy. Ob RealTimeData auf Prod gruppiert, ist unbelegt. | Fallstrick 63 nennt die Luecke |
| R-2 | Info | `SdataArtikelReader.cs:83` | Annahme `BelPosID` eindeutig in `PpsRessourcenPositionenInfo` unbelegt; waere sie es nicht, faellt nur ein Duplikat (Ressourcenummer,BelPosID) zusammen - fuer die Nummernmenge folgenlos. | keine |

Keine Code-Aenderung durch QA.

### Nachweis
- `dotnet build IdealAkeWms.slnx`: 0 Fehler (HEAD c476a628).
- IdealAkeWms.Tests 3497 ok / 1 uebersprungen / 0 Fehler; IDEALAKEWMSService.Tests 790 ok / 0 / 0.
- Live: am AKE-Test nicht nachstellbar (gruppiert nicht, s. oben); kein Prod-Zugriff durch QA. Manuelle Pruefung 1-2 oben gilt weiter (Artikel-Sync
  und Artikel-Lieferanten-Lauf auf AKE-Prod nach Deploy; Punkt 3 jetzt mit `orderBy=Ressourcenummer,BelPosID`, select wird `Ressourcenummer,BelPosID`).
