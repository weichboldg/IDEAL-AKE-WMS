---
type: spec
status: In Umsetzung
created: 2026-11-18
source_backlog: "[[2026-11-18-sage-userfelder-als-einstellung]]"
freigabe_quelle: "/goal 2026-11-18 (Gerald 09:15: „bei den Userfeldern nicht hardcodiert, immer als Einstellung“)"
worktree: .claude/worktrees/2026-08-07-ideal-teile-1-5
branch: feature/2026-08-07-ideal-teile-1-5
version: 1.99.40
testszenario: TS-161
adr: "[[0033-sage-benutzerfelder-als-einstellung]]"
---
# Sage-Benutzerfelder (USER_*) als Einstellung – Spec

Freigabe: Der /goal-Auftrag vom 2026-11-18 ist die Freigabe (Schranke 1 durch den Auftrag genommen). Lauf-Notiz:
[[2026-11-18-goal-lauf-userfelder-einstellung]].

## Ziel
Jedes Sage-Benutzerfeld, das das WMS liest, kommt aus einer ServiceSetting. Der Default ist der heutige Feldname, AKE liest also
unverändert. Ein leerer Wert bedeutet, dass das Feld nicht abgefragt wird. Die abhängige Anzeige zeigt dann „nicht konfiguriert“, es
entsteht kein Fehler. Kennt Sage das Feld nicht, erscheint eine Meldung mit dem Einstellungsnamen, einmal je Lauf. Fehlt eine SData-Resource
(HTTP 404), wird das einmal verständlich gemeldet und nicht in jedem Lauf wiederholt.

## Vollständige Erhebung (Suche `USER_` im Worktree, Stand `2ae5d1da` v1.99.39)
| Fundstelle | Feld(er) | Leseweg | Einstellung (Key) |
|---|---|---|---|
| `KontokorrentSyncService.cs:49` | USER_EKMindestbestellwert | SData Kontokorrent (Kreditoren) | `Sync:Userfeld:Kreditoren:EKMindestbestellwert` |
| `KontokorrentSyncService.cs:57` | USER_KundenNameFA | SData Adressen (Debitoren) | `Sync:Userfeld:Debitoren:KundenNameFA` |
| `SdataArtikelReader.cs:59` | USER_LagerplatzID, USER_Mindestbestand | SData ArtikelVariantenExtended | `Sync:Userfeld:Artikel:LagerplatzID`, `…:Artikel:Mindestbestand` |
| `EinkaufBestellungenSyncService.cs:52-53` | KHKEKBelege_**USER_AB**, KHKEKBelege_**USER_Kom** (RealTimeData-Alias, Präfix bleibt Code) | RealTimeData Kopf | `Sync:Userfeld:Bestellungen:AB`, `…:Bestellungen:Kom` |
| `SageArbeitsplatzReader.cs:72` | USER_ArbeitsSchritt | SQL KHKPpsArbeitsplaetze | `Sync:Userfeld:Arbeitsplaetze:ArbeitsSchritt` |
| `BfArtikelFelder.cs:13-19` | 7 × USER_OS* (Blechfertigung) | SData Artikel (Nachlese) | `Sync:Userfeld:Blechfertigung:<Name>` |
| `SdataFaStrukturReader.cs:147-161` | 13 × USER_OS* (FA-Kopf mit Präfix KHKPpsFaBelege_, 8 davon auch am Artikel) | RealTimeData + SData Artikel | `Sync:Userfeld:FaStruktur:<Name>` |
| `ColumnDefinitions.cs:826`, `PurchaseProposals/Index.cshtml:179` | USER_Mindestbestand (Spaltentitel) | Anzeige | liest `…:Artikel:Mindestbestand` |

Nur Kommentar oder Präfix (bleibt): `ArtikelBenutzerfelderWriter.cs:121`, `SageTestAufbereitung.cs:65-69`, Modell-Kommentare, `SQL/105`, `SQL/156`.
Bereits Einstellung: `Sync:ArtikelBenutzerfelder`. Views auf Sage-Seite (vw_*) liegen außerhalb des WMS-Codes.

## Regeln
- **R1 Lesart:** Zeile fehlt → Default (heutiger Name). Leer → nicht konfiguriert, das Feld wird nicht angefragt. Ungültig (nicht `USER_…`) → wie leer, mit Warnung im Lauf.
- **R2 Prüfung beim Speichern:** `Muster` `^(USER_[A-Za-z0-9_]{1,100})?$`. Der Präfix (`KHKEKBelege_`, `KHKPpsFaBelege_`) steht nie in der Einstellung.
- **R3 Merkmale:** Blechfertigung schreibt die Merkmale weiter unter dem logischen Namen (= Default). Das Web liest dieselben Merkmale. Eine Umbenennung des Sage-Felds lässt also keine verwaisten Merkmale zurück.
- **R4 Ungültiger Spaltenname:** Die Meldung „Sage kennt das Feld „X“ nicht – Einstellung „<Anzeigename>“ (<Key>) prüfen oder leer setzen“ steht einmal je Lauf im Aktivitäts-Protokoll.
- **R5 HTTP 404:** Die Meldung lautet „Resource „R“ am Sage-Server nicht vorhanden – Abgleich <Name> in den Service-Einstellungen ausschalten“. Der Schritt wird danach 24 h nicht mehr ausgeführt (eine Mail, kein Fehler je Lauf).
- **R6 Anzeige:** Bestellvorschläge: Spaltentitel „Mindestbestand (Sage)“, Zellen „nicht konfiguriert“, wenn die Einstellung leer ist. BF-Aufträge: Hinweis, welche Blechfertigungs-Felder nicht konfiguriert sind.

## Einstellungen (ADR 0020)
Je Abgleich eine eigene Kategorie „Sage-Benutzerfelder: <Abgleich>“. Anzeigename, Wirkung (Description), Default = heutiger Name.
Standort: Kreditoren und Artikel AKE, Blechfertigung und FA-Struktur IDEAL, Rest beide. Warnhinweis: „Falscher Name → Lauf meldet Fehler.“
Manifest IDEAL: `Sync:Userfeld:Kreditoren:USER_EKMindestbestellwert` leer setzen (Key = `Sync:Userfeld:<Abgleich>:<Standardname>`).

## Plan (kompakt)
1. `IdealAkeWms/Services/SageSdata/SageUserfelder.cs`: Katalog (Key, Default, Kategorie, Anzeigename, Wirkung, Standort), `AusWerten(Func<key,string?>)`, `Feld(key)`, `Erklaeren(meldung)`. In `ServiceSettingDefinitions.Basis()` und `Meta` eintragen. Test: Defaults = heutige Strings, leer → null, ungültig → null.
2. Dienst `ServiceSettings.GetByPrefixAsync` (eine Abfrage) und `SageUserfelderLeser.LesenAsync(config)`.
3. Kontokorrent, Artikel-Varianten, Bestellungen-Kopf, Arbeitsplatz-SQL, BF-Nachlese und FA-Struktur auf aufgelöste Namen umstellen. Tests: AKE-Select zeichengleich, leer → Feld fehlt im Select.
4. 404 in `SdataLeseException` plus 24-h-Sperre in `SyncWorker.RunResilientAsync`. „Ungültiger Spaltenname“ über `Erklaeren` an den Fangstellen.
5. Web: Spalte Bestellvorschläge und BF-Hinweis (frontend-design).
6. Version 1.99.40, Changelogs, Hilfe, README, TS-161, Manifest, ADR 0033, Fallstrick 51, Brain.

## Akzeptanz
- AKE ohne Einstellungszeilen liest zeichengleich wie v1.99.39 (Test `SageUserfelderTests.Default_selects_unveraendert`).
- Ist ein Feld leer, wird es nicht angefragt und die Anzeige zeigt „nicht konfiguriert“.
- HTTP 500 „Ungültiger Spaltenname“ ergibt die Meldung mit dem Einstellungsnamen, HTTP 404 die Meldung „Resource … nicht vorhanden“, und es gibt kein Wiederholen in jedem Lauf.

## Umsetzung (2026-11-18, Commit `127f7e3f`)
- Katalog `SageUserfelder.Katalog` (27): Kreditoren 1, Debitoren 1, Artikel 2, Offene Bestellungen 2, Arbeitsplätze 1, Blechfertigung 7, FA-Struktur 13.
  `ServiceSettingDefinitions.All` = Basis + Userfelder (Metadaten aus dem Katalog). Der Dienst lädt alle mit einer Abfrage (`ServiceSettings.UserfelderSafeAsync`; DB nicht lesbar → Defaults), das Web mit `SageUserfelder.AusAsync`.
- Abweichender Sage-Name: Typisierte SData-Zeilen haben `[JsonExtensionData] Weitere`; `SageUserfelder.Text` nimmt die typisierte Eigenschaft nur beim Default-Namen. Arbeitsplatz-SQL: `TableSqlFuer(feld)`, leer → `CAST(NULL …) AS USER_ArbeitsSchritt` + `ArbeitsschrittGeliefert: false` (bestehende Warnung P-A1).
- „nicht konfiguriert“ ist sichtbar in: Kreditor-/Debitor-Details, Artikel-Sage-Karte (Lagerplatz-ID, Mindestbestand), Bestellvorschläge-Spalte, Hinweis-Kasten BF-Aufträge. AB/Kom und FA-Struktur: Info im Lauf, die Anzeige bleibt leer. CSV-Export Bestellvorschläge: leere Zelle.
- 404: Meldung in `SdataLeseException`, Sperre 24 h je Schritt in `SyncWorker.RunResilientAsync` (im Speicher, ein Dienst-Neustart setzt sie zurück; `ponytail:`-Kommentar).
- Build 0 Fehler. Tests bestanden: Dienst 778, Web 3254 (+1 übersprungen). Neu: `SageUserfelderTests` (9).

## QA-Nachweis
(qa-agent)
