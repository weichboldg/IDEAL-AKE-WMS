---
type: testnotiz
title: "Bestandsaufnahme AKE-Testsystem akenet01 (v1.30.1) — Verhalten je Feature, Konfiguration, Testdaten"
system: "https://akenet01.ake.at:4444/ (v1.30.1)"
ausfuehrung: Claude Cowork (Koordination, nur lesend)
datum: 2026-10-20
verwendet_in: "[[2026-10-20-ake-regression-ideal-version-browser-testprotokoll]]"
---

# Bestandsaufnahme AKE-Testsystem akenet01 — 2026-10-20

Zweck: Referenz für das Regressions-Playbook [[2026-10-20-ake-regression-ideal-version-browser-testprotokoll]]. Hier steht,
**wie sich jedes Feature am AKE-Standort heute verhält**, damit die IDEAL-Version (Bündel `feature/2026-08-07-ideal-teile-1-5`)
auf einem neuen Testsystem mit AKE-Konfiguration dagegen geprüft werden kann.

**Vorgehen und Grenzen.** Ich habe am 20.10.2026 zwischen 07:55 und 08:05 als Gerald Weichbold (Admin, Windows-Anmeldung) jede
Seite aus Menü und Dashboard sowie je Liste eine Detail-/Bearbeiten-Seite **nur geladen**: Formulare, Felder, Pflichtfelder,
Spalten, Knöpfe, Hinweise und Datenstand ausgelesen. Nichts gespeichert, gebucht, freigegeben oder gelöscht. Was nur durch
Bedienen sichtbar wird (Scan, Speichern, Validierungsmeldungen, Druck), stammt aus der **Hilfe-Seite** (`/Help`) und dem
**Anwender-Changelog** (`/Help/Changelog`) des Systems und ist mit *(laut Hilfe)* bzw. *(laut Changelog vX)* gekennzeichnet —
das ist dokumentiertes Soll, **nicht beobachtet**.

Version: `/Help/Changelog` zeigt als neuesten Eintrag **v1.30.1 (29.09.2026)** — AKE-Hotfix (Stücklisten-Druck per POST,
Ist-Menge der Lagerbestellung bestätigen).

---

## 1. Kennzahlen des Datenstands (20.10.2026, ca. 08:00)

| Bereich | Wert | Quelle |
|---|---|---|
| Artikel | 105.632 | `/Articles` „Einträge 1–25 von 105632“ |
| Lagerplätze | 3.293, davon 10 buchbar | `/StorageLocations`, Filter „Nur buchbare“ |
| Werkbänke | 72, davon BDE-aktiv: A1, A2, A3, Stanzarei | `/ProductionWorkplaces?pageSize=100` |
| Offene FAs (FA-Liste) | 14 | `/ProductionOrders` „Einträge 1–14 von 14“ |
| Bestandszeilen | ca. 4.500 (180 Seiten à 25, 0-Bestände ausgeblendet) | `/StockOverview` |
| Lagerbewegungen | ca. 19.400 (776 Seiten à 25) | `/StockMovements` |
| OSEON-Kundenaufträge | 490 (20 Seiten) | `/Tracking/OseonIndex` |
| OSEON-AGs im Reporting | 1.367 Zeilen; Banner „10518 AG(s) ohne Config-Eintrag“ | `/OseonReporting/OperationsOverview` |
| Benutzer / Rollen | 20 / 17 | `/Users`, `/Roles` |
| Lagerbestellungen (Lager, ohne Entwurf) | 19 (12 Abgeschickt, 7 Teilgeliefert, siehe §5) | `/WarehousePicking` |
| Offene Fehlteile / Wird nicht nachgeliefert | 14 / 3 | `/MissingPartsLager` |
| Bedarfsmeldungen offen | 9 | `/PartRequisitions` |
| Sage-Lagerbuchungen (Queue) | 16: 10 Bestätigt, 6 Fehler | `/SageBookingQueue` |
| BDE-Buchungen | Buchung #39–#63 seit 22.04.2026, heute keine | `/BdeBookings?from=2026-01-01` |
| Feiertage | 45 Einträge | `/Settings` |

---

## 2. Konfiguration

### 2.1 Einstellungen (`/Settings`, AppSettings) — Werte auf akenet01, zum Vergleich idealweb01 (v1.63.0)

| Schlüssel | akenet01 (AKE) | idealweb01 (IDEAL) | Bedeutung (Hilfetext der Seite) |
|---|---|---|---|
| KommissionierTage | 4 | 4 | Arbeitstage vor Fertigungstermin für Kommissionierung |
| VorkommissionierTage | 1 | 1 | zusätzliche Tage vor Kommissioniertermin |
| BeschichtungTage | 10 | 10 | Arbeitstage vor Kommissionierung für Beschichtung |
| BeschichtungAbholtage | Dienstag,Donnerstag | Dienstag,Donnerstag | Wochentage Beschichtungs-Abholung |
| LackierteilKategorieName | **Lackierteile** | (leer) | leer = Feature inaktiv |
| WarningThresholdPercent / CriticalThresholdPercent | 150 / 100 | 150 / 100 | Meldebestand-Schwellen |
| NegativeBuchungErlaubt | **AN** | aus | negative Buchungen erlauben |
| NegativeBuchungLagerplatz | NAN | NAN | Fallback-Lagerplatz |
| TeileverfolgungAktiv | **AN** | aus | Teileverfolgung-Modul |
| OseonRueckmeldungAktiv / SageRueckmeldungAktiv | aus / aus | aus / aus | Rückschreiben |
| OseonAmpelGelbTage / OseonAmpelBlauTage | 1 / 2 | 1 / 2 | Ampel |
| BestellungenAktiv | **AN** | aus | Bedarfsmeldungen aus Stückliste |
| LagerbestellungAktiv | AN | AN | Lagerbestellungs-Modul |
| DefaultLagerbestellempfaengerId | **1** | (leer) | leer = Abschicken blockiert |
| DefaultGlasbestellempfaengerId | **4** | (leer) | leer = Abschicken blockiert |
| GlasArtikelgruppen | **880,881,882,883,884,885,886** | (leer) | Glas-Gruppen |
| GemeinsameArtikelgruppen | EUZ | EUZ | in Lager und Glas |
| LeitstandAktiv | AN | AN | Leitstand |
| KommissionierungMitZuweisung | **AN** | aus | Picker-Zuweisung |
| FaCompletionAktiv | **AN** | aus | FA-Vervollständigung + FA-Abarbeitungsliste |
| WindowsAuthAktiv | **AN** | aus | Windows-Auto-Login |
| WindowsAuthBerechtigungsgruppe | IDEAL-AKE-WMS_Users | (leer) | AD-Gruppe für „AD-Benutzer anlegen“ |
| QrMitFaNummer | **AN** | aus | QR enthält FA an 3. Stelle |
| BdeAktiv | AN | AN | BDE |
| BdeNurFaMeldung | **AN** | aus | vereinfachter BDE-Modus (Buchung auf FA) |
| BdeDefaultArbeitsgang | **PRODUKTION** | (leer) | AG im vereinfachten Modus |
| BdeMehrfachBuchungProOperator | **AN** | aus | parallele Buchungen je Mitarbeiter |
| BdeMehrfachBuchungProArbeitsgang | **AN** | aus | parallele Buchungen je AG |
| BdeGleichzeitigerAbschlussBeiMehrfachStart | aus | aus | Gruppenabschluss |
| BdeSchichtkalenderAktiv | **AN** | aus | Schichtkalender + Auto-Pause |
| OseonReportingHorizonDays / OverdueLookbackDays | 10 / 90 | 10 / 90 | Reporting |
| StammdatenADGruppe | BDE_Stammdaten | — (nicht angezeigt) | AD-Gruppe Stammdaten |
| *nur IDEAL-Version:* FaHierarchyKommissionierlistenAktiv, FaHierarchyBeschichtungAktiv, FaHierarchyVormontageAktiv | — (gibt es in v1.30.1 nicht) | AN / AN / AN | IDEAL-Module — **für AKE aus** |
| *nur IDEAL-Version:* FaHierarchyMaxTiefe, BriefkopfVorlagePfad, Firmenname, Firmenanschrift | — | 500 / Pfad / IDEAL Kältetechnik / Gmunden | Standorteinstellungen |

Unterseite **Arbeitsgang-Konfiguration (OSEON)** `/Settings/OperationConfig` (in der IDEAL-Version laut Changelog v1.54 nach
Stammdaten › Arbeitsgänge verschoben, alte Adresse leitet um): 12 Einträge — A-BT (Offset 0, nicht relevant), B (−1), BG (2),
BG-SaP1 (2), EG (0), MS (4), RE (5), RO (2), RS (4), SL (5), ST (0), ZB (0, nicht relevant). Offset = Arbeitstage zum
Stanztermin. *Die Spaltenzuordnung habe ich aus Eingabefeldern gelesen; Anzeigenamen nicht sicher erfasst.*

### 2.2 Service-Einstellungen (`/ServiceSettings`) — akenet01 vs. idealweb01

| Schlüssel | akenet01 | idealweb01 | Hinweis |
|---|---|---|---|
| Sync:ProductionOrdersEnabled / ArticlesEnabled | AN / AN | AN / AN | |
| Sync:OseonArticleCategoryEnabled / OseonTrackingEnabled / EnaioDmsEnabled | **AN / AN / AN** | aus / aus / aus | AKE-only-Quellen |
| Sync:PartRequisitionEmailEnabled / WarehouseRequisitionEmailEnabled | **AN / AN** | aus / aus | Mailversand |
| Sync:LagerplaetzeEnabled / LagerbestandEnabled | AN / AN | AN / AN | |
| Sync:LagerbestandIntervalMinutes | 15 | 5 | |
| Sync:LagerbestandNullsetzenMaxPerRun | 700 | 156 | Cap |
| Sync:ProductionOrderReconcileEnabled | **AN** | aus | Cap ReconcileMaxCancelPerRun 2499 (AKE) |
| Sync:FaZusatzinfoEnabled | **AN** | aus | Cap AutoDoneMaxPerRun 900 (AKE) / 100 |
| SageLagerbuchungAktiv | **AN** | aus | |
| SData:BaseUrl / Dataset | https://sagetest01.ake.at:5493/ / ake_TEST2026;1 | leer / leer | |
| SageLagerbuchungSslZertifikatPruefen | **aus** (Banner „nur für Testsysteme zulässig“) | AN | |
| Sync:SageLagerbuchung… Interval/Batch/Retries/MaxErrors/Stuck | 20 s / 50 / 5 / 50 / 10 min | gleich | |
| Sync:BomCacheEnabled / Weeks / MaxOrders / MaxAgeHours | **AN** / 10 / 500 / 24 | aus / 8 / 200 / 24 | |
| Sync:FaWorkStepDetectionEnabled | **AN** | aus | FA-Vorbau-AG-Erkennung aus BOM-Cache |
| Sync:CoatingDetectionEnabled | **AN** | aus | Lackierteil-Erkennung |
| Sync:BdeAutoPauseIntervalMinutes | 60 | 60 | |
| Sync:FeiertagSyncEnabled / CountryCode / Region / JahreVoraus | AN / AT / leer / 2 | gleich | |
| Cleanup:AktivitaetsprotokollAufbewahrungTage | 90 | 180 | |
| WorkerSettings:SyncIntervalMinutes / NotificationCheckIntervalMinutes / SyncDryRun | 15 / 60 / aus | gleich | |
| ErrorNotification:Enabled / Recipients | AN / gerald.weichbold@… | aus / leer | |
| Notifications:MeldebestandEnabled / Recipients / AppBaseUrl | AN / leer / https://akenet01.ake.at:4444 | AN / leer / leer | |
| *nur IDEAL-Version:* Sync:FaZusatzinfoAutoErledigtEnabled | — | aus | laut Feldhilfe „Default an (AKE-Verhalten)“ — **für AKE an** |
| *nur IDEAL-Version:* Sync:ArticlesViewName | — | [vw_IDEAL_AKE_WMS_Artikel] | „Leer = bisheriger Select (AKE)“ — **für AKE leer** |
| *nur IDEAL-Version:* Sync:HierarchicalFaEnabled, FaHierarchyListeViewName, FaHierarchyInfosViewName | — | AN / Views | **für AKE aus** |
| *nur IDEAL-Version:* Sync:WorkOperationStructureDetectionEnabled, ProductionWorkplaceSync* | — | AN / Mandant 1 / View / Ausschlussliste | laufen laut Feldhilfe nur bei Master „ProduktionsauftragHierarchisch“ — **für AKE aus** |
| *nur IDEAL-Version:* Sync:OperationConfigSyncEnabled / ViewName / FremdsystemRelevant | — | AN / [vw_IDEAL_AKE_WMS_Arbeitsgaenge] / aus | „beide Standorte; AKE trägt die eigene View gleicher Spalten ein“ — **für AKE erst aus** (keine AKE-View bekannt) |

### 2.3 Hintergrund-Jobs (`/SyncLog`, letzter Lauf je Dienst, 20.10.2026)

| Dienst | Letzter Lauf | Ergebnis |
|---|---|---|
| ProductionOrder / ProductionOrderReconciliation | 07:51 | neu=0, aktualisiert=0 / storniert=0, reaktiviert=0 |
| Article | 07:52 | gelesen=100600, neu=0, aktualisiert=0, **hauptlagerplatz_fehlt=106** (Warnungen je Artikel) |
| FaZusatzinfo | 07:51 | **Warning: View `dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen` nicht vorhanden — Sync übersprungen.** Die Spalten Kältemittel/Ventil/… zeigen trotzdem Werte (letzter Stand bleibt laut Changelog v1.26 sichtbar). |
| Lagerplatz | 07:52 | alles 0 |
| Lagerbestand | 07:52 | ein-/ausbuchungen=0, nullgesetzt=0, **übersprungen=140** („Artikel … nicht im WMS“) |
| BomCache | 07:52 | FA im Fenster 14, gecacht 14, Artikel ohne BOM 1 (S0416233) |
| FaWorkStepDetection | 07:52 | neu=0, übersprungen=54, Suchbegriffe 10 mit Treffer |
| CoatingDetection | 07:52 | geprüft=500, mit Lackierteilen=0 |
| OseonTracking / OseonWorkplaces / OseonArticleCategories | 07:52 | neu=69, aktualisiert=24564 / 0 / 9 |
| EnaioDms | 07:52 | aktualisiert=31264 |
| SageLagerbuchung | alle 20 s | gesendet=0 |
| PartRequisitionEmail | 07:52 | versendet=0, **ohne_empfaenger=3** (Warning „Keine aktiven Empfänger“, z. B. FA 2604043) |
| WarehouseRequisitionEmail | 07:52 | 0 |
| BdeAutoPause | 07:19 | geprüft=0 |
| Holiday | 19.10. 12:10 | geholt=45 |
| CleanupAktivitaetsprotokoll | 19.10. 10:10 | gelöscht=0, Stichtag 21.07.2026 |

Fehler der letzten Wochen: nur **OseonTracking** (Ausführungstimeout 01.10., 05.10., 12.10. ×3, 16.10.; Deadlock 13.10.) und
**OseonArticleCategories** (Deadlock 15.10.). Bekanntes Bild, kein Bezug zur Version.

### 2.4 Rollen (17, alle System) mit Benutzerzahl
admin 10 · masterdata_read 4 · lagerbestellung 2 · glasbestellung 2 · masterdata 4 · picking 3 · stock 6 · stock_read 3 ·
stock_keyuser 4 · tracking 5 · reporting 2 · leitstand 4 · fa_completion 4 · bde_user 2 · bde_shiftlead 1 · bde_admin 1 ·
vorbau 4. Die Rollen-Übersicht `/Users/RoleOverview` beschreibt je Rolle den Zugang (hand-gepflegt).
In der IDEAL-Version kommen laut Changelog v1.51 die Rollen `beschichtungsauftrag` und `beschichtungsauftrag_schreiben` dazu.

---

## 3. Menü (Admin-Sicht, akenet01)

| Menü | Einträge → Adresse |
|---|---|
| Lager | Einbuchung `/StockMovements/Inbound` · Mehrfach-Einbuchung `/StockMovements/InboundBulk` · Ausbuchung `/StockMovements/Outbound` · Umbuchung `/StockMovements/Transfer` · Bestände `/StockOverview` · Bewegungshistorie `/StockMovements` · Sage-Lagerbuchungen `/SageBookingQueue` · Lagerplatz ausbuchen `/StockMovements/OutboundAll` · Lagerplatz umbuchen `/StockMovements/LocationTransfer` |
| Fertigungsaufträge | FA-Liste `/ProductionOrders` · FA-Vervollständigen `/FaCompletion` · FA-Abarbeitungsliste `/FaWorklist` |
| Leitstand | Kommissionierung `/PickingLeitstand` |
| Kommissionierung (Link mit Badge, heute „1“) | `/Picking` |
| Bestellungen | Bedarfsmeldungen `/PartRequisitions` · Lagerbestellungen `/WarehouseRequisitions` · Meine Fehlteile `/MissingParts` · Lager: Eingehende Listen `/WarehousePicking` · Lager: Fehlteile `/MissingPartsLager` |
| Teileverfolgung | Rückmeldungen `/Tracking` · OSEON Aufträge `/Tracking/OseonIndex` |
| BDE | Terminal `/BdeTerminal` · Cockpit `/BdeCockpit` · Buchungen `/BdeBookings` · Stammdaten `/BdeMasterData` · Schichtkalender `/BdeShiftCalendar` |
| Reporting | OSEON AG-Übersicht `/OseonReporting/OperationsOverview` |
| Stammdaten | Artikel `/Articles` · Lagerplätze `/StorageLocations` · Werkbänke `/ProductionWorkplaces` · Artikelkategorien `/ArticleCategories` · Artikelmerkmale `/ArticleAttributes` · FA-Vorbau-AG `/WorkSteps` · FA-Merkmale `/FaAttributes` · Empfänger `/OrderRecipients` · Artikelgruppen-Zuordnung `/OrderRecipients/ArticleGroupMappings` · Benutzer `/Users` · Arbeitsplätze `/Workstations` · Rollen `/Roles` · Einstellungen `/Settings` · Service-Einstellungen `/ServiceSettings` · Aktivitäts-Protokoll `/SyncLog` |
| Benutzer-Menü | Mein Profil `/Account/Profile` |
| Dashboard `/` | Kacheln: Einbuchung, Ausbuchung, Bestände, Bewegungen · Kommissionierung, Fertigungsaufträge, Artikelinfo `/Articles/Info` · Leitstand-Kommissionierung · Teileverfolgung OSEON · BDE Terminal/Cockpit/Buchungen · OSEON AG-Übersicht · Artikel, Lagerplätze, Benutzer, Arbeitsplätze |

Zum Vergleich idealweb01 (v1.63.0, IDEAL-Konfiguration): zusätzlich **FA-Struktur** `/FaHierarchy`, **Beschichtung**
(Vorschlagsliste `/FaHierarchyBeschichtung/Summiert`, Aufträge `/Beschichtungsauftraege`), **Vormontage** `/FaHierarchyVormontage`,
**Kommissionierung** als Dropdown (Kommissionierung `/Picking`, Kommissionierlisten `/FaHierarchyKommissionierListen/Summiert`),
Stammdaten › **Arbeitsgänge** `/OperationConfigs` und **Standorteinstellungen** `/StandortEinstellungen`. Dort fehlen wegen der
Schalter FA-Vervollständigen, FA-Abarbeitungsliste, Bedarfsmeldungen, Teileverfolgung und Empfänger.

---

## 4. Verhalten je Feature

Legende: **Beobachtet** = am 20.10. auf akenet01 gesehen. *(laut Hilfe/Changelog)* = dokumentiertes Soll.

### 4.1 Lager

**Einbuchung** `/StockMovements/Inbound` (Rollen stock, stock_keyuser, picking, admin)
- Beobachtet: Formular POST mit Artikel (Such-Dropdown „-- Artikel suchen --“), Menge (Zahl, min 0,001, **vorbelegt 1**),
  Lagerplatz (Dropdown, nur buchbare: 1;1;1, asdfasdf, GL;0;0;0, NAN, qwert, Test, TEST 1, TEST 2 — Wagen 1/2 fehlen hier),
  Fertigungsauftrag (Text), Knopf „Einbuchung speichern“. Darunter Tabelle offener Bedarfsmeldungen (FA-Nr., Menge, ME,
  Besteller, Datum, Priorität, Bemerkung) mit „alle wählen“.
- *(laut Hilfe)* Artikel/Lagerplatz per Scan; offene Bedarfsmeldungen zum Artikel werden angeboten und bei Auswahl als erfüllt
  verknüpft. FA-Lagerplatz-Hinweis: FA an genau einem Platz mit Bestand → Platz wird übernommen; an mehreren → gelbe Warnung
  mit Mengen; inaktiver/nicht buchbarer Platz → gelbe Warnung (seit v1.29 nur Plätze mit tatsächlichem Bestand).
- Sage-Meldung: Einbuchung auf **GL;0;0;0** (einziger Platz mit „Sage-Buchung = Ja“) erzeugt einen Eintrag in der
  Sage-Lagerbuchungs-Queue (beobachtet: Queue #7–#16 sind solche Einbuchungen, bestätigt).

**Mehrfach-Einbuchung** `/StockMovements/InboundBulk`
- Beobachtet: Lagerplatz + FA einmal, Schalter „Bei Scan immer neue Zeile erzwingen“, Zeilen Artikel + Menge (vorbelegt 1),
  Knöpfe „+ Zeile hinzufügen“, „Artikel scannen“, „×“ je Zeile, „Einbuchung speichern“, Link „Einzel-Einbuchung“.
- *(laut Changelog v1.29)* gleicher Artikel per Scan zählt hoch; unvollständige Zeile → nichts gebucht, Eingaben bleiben,
  Zeile markiert.

**Ausbuchung** `/StockMovements/Outbound` — Artikel, Menge (**vorbelegt 0**, min 0,001), Lagerplatz, FA, „Ausbuchung speichern“.
*(laut Hilfe)* verfügbare Menge wird geprüft. Hinweis: NegativeBuchungErlaubt=AN mit Fallback NAN — wie sich das bei
Unterdeckung genau äußert, habe ich nicht gesehen.

**Umbuchung** `/StockMovements/Transfer` — Artikel, Menge (vorbelegt 0), Von-Lagerplatz, Auf-Lagerplatz, FA, „Umbuchung speichern“.
Umbuchungen werden nicht an Sage gemeldet *(laut Hilfe)*.

**Bestände** `/StockOverview` (auch stock_read)
- Beobachtet: Filter Artikelnr./Bezeichnung, Lagerplatz (alle 3.294 Optionen inkl. nicht buchbarer), FA-Nr., Bestand min/max,
  „Filtern“; Tabelle mit Server-Spaltenfilter: Artikelnummer | Bezeichnung | Lagerplatz | Einheit | Meldebestand | Aktueller
  Bestand; VAULT-Link je Zeile; Pagination.
- *(laut Hilfe)* 0-Bestände ausgeblendet; QR-Scan setzt Artikel (und FA, wenn QrMitFaNummer); FA-Filter zeigt Netto-Bestand je
  Platz und ignoriert die übrigen Filter; orange/rot nach Meldebestand-Schwellen; Hauptlagerplatz zuerst mit ★ „Haupt“.

**Bewegungshistorie** `/StockMovements` — Filter Datum von/bis, Artikel, Lagerplatz, Bewegungsart (Einbuchung, Ausbuchung,
Umbuchung, Sage-Einbuchung, Sage-Ausbuchung), Benutzer, FA (mit Scan; *laut Changelog v1.29* WA-Code `2610063-1` → `2610063`).
Spalten Datum/Zeit | Artikel | Menge | Lagerplatz | Bewegungsart | Benutzer | Fertigungsauftrag | Notiz. *(laut Changelog v1.30)*
Spaltenfilter Bewegungsart nach Anzeigename, Datum/Zeit nach Tag/Monat/Jahr; Menge ohne Textfilter.

**Sage-Lagerbuchungen** `/SageBookingQueue` — gelbes Banner „TLS-Zertifikatsprüfung deaktiviert … nur für Testsysteme“;
Statusfilter Alle/Offen/Gesendet/Bestätigt/Fehler; Spalten # | Status | Bewegung | Artikel | Lagerplatz | Menge | Erstellt |
Gesendet | Bestätigt | Vers. | Fehler; „Erneut senden“ nur an Fehler-Zeilen (#1–#6). Erklärtext: Requeue prüft vorher per
Sage-Memo-Lookup (kein Doppelbuchen).

**Lagerplatz ausbuchen** `/StockMovements/OutboundAll` / **Lagerplatz umbuchen** `/StockMovements/LocationTransfer`
(stock_keyuser, admin) — erst Lagerplatz wählen + „Laden“ (GET), dann Bestand des Platzes; *(laut Hilfe)* „Alle X Artikel
umbuchen“ mit Bestätigung. Auffällig: Ausbuchen bietet nur buchbare Plätze, Umbuchen-Quelle alle 3.294.

### 4.2 Fertigungsaufträge

**FA-Liste** `/ProductionOrders`
- Beobachtet: Filter FA-Nummer, Artikelnummer, Kunde, „Erledigte anzeigen“; Server-Spaltenfilter; Spalten FA Nr. | Stk. |
  Kunde | Artikelnummer | Bezeichnung 1 | Bezeichnung 2 | Werkbank | Kältemittel | Ventil | Ausführung E/Z | Maschine |
  Sage-Status | Beschicht. | BG-Termin | Komm. | Fert.-Termin | Liefertermin | Lack-T. Je Zeile: Stückliste `/Picking/Bom/<Id>`,
  enaio-Icons (Werkstattauftrag + Zeichnung, Dokument → `/oscontentviewer/viewer/<Nr>/`), VAULT, OSEON-i `/Tracking/OseonIndex`.
- Datumsspalten mit KW („12.08.2026 KW33“). 14 offene FAs (Liste §5.1).
- *(laut Hilfe)* Kalender-Picker im Datumsfilter (KW-Klick/Tag), Filter-Syntax `a,b` ODER, `!a` NICHT, `KW24`; Server-Filter erst
  bei ENTER (v1.24); Zahnrad: Spalten ein/aus, Reihenfolge, Breite, Standardsortierung, „Auf Standard zurücksetzen“.

**FA-Vervollständigen** `/FaCompletion` (fa_completion, Schalter FaCompletionAktiv)
- Liste: Filter wie FA-Liste; Spalten FA Nr. | Stk. | Kunde | Artikelnummer | Bezeichnung 1 | Fert.-Termin | Werkbank | Anwendbar
  | Vervollständigt | Ausprägungen; „Bearbeiten“ → `/FaCompletion/Edit/<Id>`; Stückliste read-only `/FaCompletion/Bom/<Id>`.
- Edit (FA 2604043): Reiter **ALLGEMEIN** + je FA-Vorbau-AG (VK, VL, VT, VA mit ●); Werkbank-Auswahl (SetWorkplace),
  „+ FA-Vorbau-AG“ (bietet nur noch nicht zugeordnete, hier VE), „FA-Vorbau-AG entfernen“, Schalter „Vollständig definiert“
  (Hinweis: blendet NICHT aus der Abarbeitungsliste aus), Merkmal-Dropdowns/JA-NEIN (SaveAttributeValue), Spezifikationen
  (Artikel, Beschreibung Pflicht, Menge, Notizen, Sort.) mit „+“.

**FA-Abarbeitungsliste** `/FaWorklist` (vorbau, Schalter FaCompletionAktiv)
- Ohne gewählten AG: Hinweis „Bitte zuerst einen FA-Vorbau-Arbeitsgang auswählen …“; Filter FA-Vorbau-AG (VK/VL/VE/VT/VA),
  Werkbänke (kommasepariert), Erledigte anzeigen.
- Mit VK (`?workStepId=1`): 13 Zeilen; Spalten FA Nr. | Werkbank | Artikelnummer | Bezeichnung 1 | Bezeichnung 2 | Stk. |
  Beschicht. | BG-Termin | Komm. | Fert.-Termin | Kältemittel | Ventil | Ausführung E/Z | Maschine | Sage-Status | dann die dem
  AG zugeordneten FA-Merkmale (Verdampfergroesse, Leitungsausgang, Verdampfergehaeuse, Ventil aussenliegend, CO2 Klassifizierung,
  Dichtheitsprüfung) | Status-Spalte „VK Vorfertigung Kälte“ (Offen / in Bearbeitung / Fertig). Stückliste read-only
  `/FaWorklist/Bom/<Id>` mit Lagerbestellung-Knöpfen.
- *(laut Hilfe)* Statuswahl speichert sofort; nur „Fertig“ blendet aus; Profil-Standard-AG/-Werkbänke/-Bezeichnungsfilter.

### 4.3 Leitstand und Kommissionierung

**Leitstand › Kommissionierung** `/PickingLeitstand` (leitstand, picking; Schalter LeitstandAktiv)
- Beobachtet: Filter wie FA-Liste; Spalten wie FA-Liste plus Glas | Zukauf | VK | VL | VE | VT | VA | Status | Freigabe |
  Kommissionierer. VK–VA-Zellen sind 3-Wert-Auswahl (Offen / in Bearbeitung / Fertig), leer wo der AG nicht zugeordnet ist.
  Je Zeile „Freigeben“ (ToggleRelease); Mehrfachauswahl mit „Markierte freigeben“/„Markierte zurücknehmen“ (BulkRelease);
  Modal „Kommissionierer zuweisen“ (Pflicht-Dropdown, da KommissionierungMitZuweisung=AN), Modal „Kommissionierer ändern“.
- FA 2614027 ist freigegeben (Prio 15), Kommissionierer „test“ — dort fehlt der Freigeben-Knopf.
- *(laut Hilfe)* Prio wird bei Freigabe automatisch vorgeschlagen (nächste freie), inline änderbar; FA ohne Artikelnummer
  werden bei Massenfreigabe übersprungen; Zurücknehmen lässt Gepicktes erhalten.

**Kommissionierung** `/Picking` (Badge im Menü)
- Beobachtet: Badge zeigt **1**; für Gerald (nicht zugewiesen) Meldung „Aktuell sind keine Aufträge für Sie zur
  Kommissionierung freigegeben.“; mit „Alle anzeigen“ (`?showAll=true`) 1 Zeile: Prio 15 | 2614027 | S0416233 | … | Komm.-Termin
  18.11.2026 KW47 | Offen | test. Spalten konfigurierbar (viewKey „Picking“, Reorder).
- **Stückliste** `/Picking/Bom/<Id>` (FA 2604043, Quelle-Badge OSEON, 106 Zeilen): Filter Text + „Artikel scannen“; Spalten Pos.
  | Baugruppe | Ressourcenummer | Bezeichnung 1 | Bezeichnung 2 | Menge | Beschaffung | Artikelgruppe | Kategorie | Lagerplatz |
  Quell-Lagerplatz | Bestellen | Lagerbestellung. Umbuchen auf Wagen (Wagen 1/2) „Gepickte Artikel umbuchen“; Modals
  Kommissionierwagen belegt, Artikel nicht gefunden, Kommissionierung Status (Teilkommissioniert / Abgeschlossen), Bedarfsmeldung
  erstellen, Lagerbestellung erstellen/Auswahl; Fotos; „Stückliste drucken“; „Zurück zur FA-Liste“.
- *(laut Hilfe)* Quelle SAGE, Fallback OSEON, sonst KEINE DATEN; Auswahl rein visuell bis Umbuchen; Wagen nicht in
  Bestand/Quell-Dropdowns; Profil-Standardfilter (Beschaffung, Artikelgruppe, Bezeichnung 1 — Gerald: „Verdampfer“), Rekursive
  Suche; Druck v1.30.1 per POST, große Listen druckbar.

### 4.4 Bestellungen

**Bedarfsmeldungen** `/PartRequisitions` (Schalter BestellungenAktiv) — Suche + „Alle anzeigen (inkl. Erfüllt/Storniert)“;
Spalten Status | Priorität | FA-Nr. | Kunde | Ressourcen-Nr. | Bezeichnung | Menge | ME | Bestellt von | Bestellt am | E-Mail |
Bemerkung | Aktionen; „Stornieren“ je Zeile mit Bestätigungs-Modal. 9 offen. Prioritäten Normal/Dringend/Eilt. Mailversand
zurzeit ohne Empfänger (SyncLog).

**Lagerbestellungen — meine Listen** `/WarehouseRequisitions` — Reiter Lager / Glas; „+ Neue Liste“ (CreateDraft); Spalten # |
Werkbank | Pos | Status | Erstellt | Submit. Glas-Reiter: Entwurf #52 (A1, 0 Pos.). Edit `/WarehouseRequisitions/Edit/<Id>`:
„Wird automatisch gespeichert.“, „Abschicken“, „Stornieren“ mit Grund.

**Lager: Eingehende Listen** `/WarehousePicking` — Reiter Lager/Glas; Statusfilter (Alle außer Entwurf, Abgeschickt,
Teilgeliefert, Erledigt, Storniert), Werkbank; Spalten # | Werkbank | Erfasser | Submit | Kommentar | Pos | Status.
Detail `/WarehousePicking/Details/53`: je Position Ist-Menge (**leer**, min 0), Notiz Lager, Notiz EK, Radio „Fehlteil“ /
„Wird nicht nachgeliefert“; Knöpfe „Drucken und Abschließen“, „Speichern + Abschließen“, „Stornieren“.
*(laut Changelog v1.30.1)* Abschließen nur, wenn jede Position Ist-Menge (auch 0) oder Fehlteil-Markierung hat, sonst gelber
Hinweis mit offenen Positionen; leere Ist wird nie als 0 gespeichert; keine Rückfrage „Soll = Ist“.

**Meine Fehlteile** `/MissingParts` / **Lager: Fehlteile** `/MissingPartsLager` — Reiter „Offene Fehlteile 14“ / „Wird nicht
nachgeliefert 3“; Werkbank-Filter (bei Meine Fehlteile gesperrt, „Nur meine Werkbänke“); Spalten Bestell-ID | Werkbank |
Artikel-Nr | Bezeichnung | Soll | Geliefert | Fehlt | (nur Lager: Lagerplatz (Bestand)) | Notiz Lager | Notiz EK | Erfasst von |
Abgeschlossen am.

### 4.5 Teileverfolgung und Reporting (Schalter TeileverfolgungAktiv)

**Rückmeldungen** `/Tracking` — Filter FA, Werkbank, „Rückgemeldete anzeigen“; 62 Zeilen, gruppiert je FA mit
Fortschritt und Unter-Tabellen (# | AG-Nr. | Bezeichnung | Werkbank | Status | Rückgemeldet), „Rückmelden“ je AG.

**OSEON Aufträge** `/Tracking/OseonIndex` — Suche, Artikelnummer, Werkbank, „Fertige anzeigen“, „OSEON-Relevanz“; 490
Kundenaufträge, 25/Seite; Spalten Auftrag | Artikelnr. | Bezeichnung | Werkbank | Status | Soll / Ist | Endtermin;
Lagerbestand-Modal „Details in Bestandsübersicht →“. *(laut Hilfe)* Lazy-Load der Subaufträge, Ampel rot/gelb/blau/grün/grau.

**OSEON AG-Übersicht** `/OseonReporting/OperationsOverview` (reporting) — Filter Werkbank, AG-Name, Kundenauftrag, FA, Horizont
(10); Banner „10518 AG(s) ohne Config-Eintrag“; 1.367 Zeilen; Spalten Kunde | Auftrag | Pos | AG | Werkbank | Soll-Termin (calc.)
| Status | Bereich.

### 4.6 BDE (BdeAktiv=AN, **BdeNurFaMeldung=AN**, Mehrfachbuchung AN, Schichtkalender AN)

**Terminal** `/BdeTerminal` — Gerald ist als Terminal an Werkbank Stanzarei eingetragen; Kopf „Werkbank: A1 A2 A3 Stanzarei“,
„Angemeldet: Gerald Weichbold | Session läuft bis: —“. Schritte „1. Operator“ (Scan Personalnummer), Hinweis „Sie haben pausierte
Aufträge“, „2. Arbeitsgang wählen“, „Aktuelle Buchungen“ („Keine aktive Buchung“), „Tageshistorie“. Modals: Mengen melden,
Arbeitsgang bereits in Arbeit, Ungeplante Tätigkeit, Alle Produktionsaufträge abschließen (Tabelle FA/AG | Sollmenge | Gut |
Ausschuss | Abschl.; „Alle fertig melden“), Weitere aktive Buchungen („Nein, nur meine“ / „Ja, alle beenden“).
*(laut Hilfe)* Ablauf Operator scannen → AG wählen → Rüsten → Produktion → Teilmenge → Pause → Beenden; Toast nach jeder Aktion;
FA mit Sage-Status verpackt/abgeholt gesperrt.

**Cockpit** `/BdeCockpit` — „Live-Status aller Werkbänke · Server: —“ (per JS, 5-s-Takt *laut Hilfe*; Inhalt beim Laden leer).

**Buchungen** `/BdeBookings` — Filter Von/Bis (Standard heute), Operator (Fred Ferdl, Hans GuckindieLuft), Werkbank (A1, A2, A3,
Stanzarei); Spalten Start | Ende | Effektive Zeit | Operator | Werkbank | Typ | Ziel | Gut | Ausschuss | Status | Aktionen
(„Bearbeiten“, „Stornieren“ mit Stornogrund). Ziel-Format im Nur-FA-Modus `<FA>/01`. Status-Werte Finished, AutoPaused, Resumed.

**Stammdaten** `/BdeMasterData` — Reiter Operatoren (Personalnummer | Name | Aktiv | WMS-Benutzer; 2: Fred Ferdl 4444, Hans
GuckindieLuft), Aktivitäten (Rüsten, PRODUKTION, Putzen, WKR), Terminals (terminal_halle_a_a1 → A1, till.zoehrer → A2, Gerald
Weichbold → Stanzarei).

**Schichtkalender** `/BdeShiftCalendar` — Default-Plan je Wochentag (Mo–Fr mit je einer Schicht, Sa/So leer), „+ Schicht
hinzufügen“ (Name, Beginn, Ende Pflicht). Werkbank-eigener Plan in der Werkbank-Maske.

### 4.7 Stammdaten und Verwaltung

| Seite | Beobachtetes Verhalten |
|---|---|
| Artikel `/Articles` | Suche + Seitengröße 100/250/500; Spalten Artikelnummer, Bezeichnung, Einheit, Artikelgruppe, Kategorie, je Artikelmerkmal eine Spalte (test, Test23, Kommissionierung, Derdampfer), Erstellt am; „Neuen Artikel anlegen“. Edit: Nummer/Bezeichnung/Einheit editierbar, Artikelgruppe read-only, Kategorie, Meldebestand, Hauptlagerplatz (nur buchbare Plätze), Merkmale. |
| Artikelinfo `/Articles/Info` | Eingabe/Scan; QR-Format `Artikelnummer;Feld2;…`. Beispiel 00102251: Bezeichnung, Einheit, Gruppe 100, Merkmale, Meldebestand 0, Gesamt-/Verfügbarer Bestand 342, Bestand je Platz (GL;0;0;0), VAULT-Link. |
| Lagerplätze `/StorageLocations` | „Nur buchbare Plätze zeigen“, Spalten Code, Bezeichnung, Bereich/Zone, Kapazität, Typ, Quelle (Manuell/Sage), Buchbar, Sage-Buchung, Erstellt; Etikett je Platz und „Alle Etiketten drucken“. Edit: Code (max. 12 Zeichen manuell), Aktiv, „Für Buchungen freigegeben“, „An Sage melden“, „Kommissionierwagen“. |
| Werkbänke `/ProductionWorkplaces` | Spalten Bezeichnung, Halle, BDE, Benutzer, Abw. Vorkommissioniertage (A2 = 0 Tage, C1 = 15 Tage, sonst Standard). Edit: Name, Halle, Abw. Vorkommissioniertage, Benutzer (Mehrfachauswahl), FA-Vorbau-AG-Checkboxen, BDE aktiv, Default-AG, eigener Schichtplan. |
| Artikelkategorien / Artikelmerkmale | Inline-Bearbeitung; 9 Kategorien (Quelle OSEON markiert); 4 Merkmale (Boolean/Dropdown, Typ nach Anlage fest). |
| FA-Vorbau-AG `/WorkSteps` | VK (Verdampfer, Maschinenfach), VL (Lüfter, Luefter), VE (Steuerung), VT (Tür, Tuer), VA (Aufbau, Spange, Rahmen); Löschen-Knopf je Zeile. |
| FA-Merkmale `/FaAttributes` | 9 Merkmale (Boolean/Dropdown/Text) mit AG-Zuordnung; Optionen in Verwendung nicht löschbar. |
| Empfänger `/OrderRecipients` | 4 Gruppen (u. a. Blechteile, glas); Artikelgruppen-Zuordnung als Matrix 94 Gruppen × 4 Empfängergruppen. |
| Benutzer `/Users` | 20 Benutzer, Typ AD/Lokal; „AD-Benutzer anlegen“ schlägt Mitglieder der AD-Gruppe vor; Edit mit Rollen, „Ist Kommissionierer“, Profil-Standards. |
| Rollen `/Roles` | Schlüssel und Sortierung bei Systemrollen read-only. |
| Arbeitsplätze `/Workstations` | leer („Keine Arbeitsplätze vorhanden“). |
| Mein Profil `/Account/Profile` | Standard-Filter Beschaffung/Artikelgruppe/Bezeichnung 1 (Stückliste), Rekursive Suche, E-Mail, Meldebestand-Mail, Einträge pro Seite, Standard-AG/-Werkbänke/-Bezeichnung 1 (Abarbeitungsliste), Passwort. |
| Hilfe `/Help`, Änderungen `/Help/Changelog` | Hilfe mit Kapiteln je Modul; Changelog bis v1.30.1. |

---

## 5. Testdaten-Inventar (was ein neues AKE-Testsystem mindestens braucht)

### 5.1 Offene FAs (Leitstand, 20.10.2026)

| FA | Stk | Kunde | Artikel | Werkbank | Komm.-Termin | Besonderheit |
|---|---|---|---|---|---|---|
| 2604043 | 1 | Monsberger | S0309613 | A3 | 13.08.2026 KW33 | Stückliste OSEON-Quelle, 106 Zeilen, Bedarfsmeldung offen |
| 2607170 / 2607171 | 2 | IDEAL Kältetechnik | S0119384 / S0119385 | F3 | 18.08.2026 KW34 | R744 |
| 2614027 | 2 | Nadler Pius AG | S0416233 | — | 18.11.2026 KW47 | **freigegeben, Prio 15, Picker „test“**; Artikel ohne BOM |
| 2614167 / 2614168 | 1 | Nadler Pius AG | S0416237 / S0416238 | D1 | 30.10.2026 KW44 | |
| 2618090 / 2618092 | 1 | AKE | S0311537 / S0311539 | C1b / C2 | 09.06.2026 KW24 | überfällig |
| 2621027 | 1 | Monsberger | S0309613 | A3 | 07.08.2026 KW32 | |
| 2625004 / 2625005 | 1 | NORDCAP | S0311557 / S0311556 | — | 16.07.2026 KW29 | Beschichtungstermin gesetzt, keine Zusatzinfos |
| 2627001 / 2638001 | 1 / 2 | !!!Test - AKE - Test!!! | EU253711 / EU253691 | — | KW26 / KW36 | Test-Kunde |
| 2629001 | 5 | NordCap Nederland | EU253691 | H3 | 09.07.2026 KW28 | |

### 5.2 Weitere Bezugsdaten
- Buchbare Lagerplätze: 1;1;1, asdfasdf, **GL;0;0;0 (Sage, Sage-Buchung ja)**, NAN (Fallback), qwert, Test (Kap. 100), TEST 1,
  TEST 2, **Wagen 1, Wagen 2 (Typ Wagen)**.
- Lagerbestellungen: Entwurf #52 (Glas), Abgeschickt #34–#57 (A1/A2), Teilgeliefert #8, #15, #28–#33; Detail #53 mit 1 Position.
- BDE: Operatoren 4444 Fred Ferdl, Hans GuckindieLuft; Terminals s. 4.6; Aktivitäten Rüsten/PRODUKTION/Putzen/WKR.
- Sage-Queue: Fehler #1–#6 vom 03.08. (SSL, 401, 400, 500) — gut zum Prüfen von „Erneut senden“.
- Artikel mit Bestand: 00102251 (342 Stk auf GL;0;0;0).

---

## 6. Auffälligkeiten (Befunde zur Einordnung, keine Fehler der Version)

1. **FaZusatzinfo-View fehlt** auf der AKE-Sage-Testdatenbank (`dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen`). Der Sync ist an,
   überspringt aber jeden Lauf. Folge für das Playbook: Auto-Erledigt und BDE-Sperre bei „verpackt/abgeholt“ lassen sich so nicht
   prüfen. *Ungeprüft:* ob die View auf der Sage-Testdatenbank angelegt werden soll oder nur dort fehlt.
2. **106 Artikel mit Hauptlagerplatz, der im WMS fehlt**, und **140 übersprungene Bestandspaare** (Artikel nicht im WMS) — Datenlage,
   kein Code-Thema; im neuen System als Ausgangswert mitschreiben.
3. **Bedarfsmeldungs-Mails ohne Empfänger** (3 offen): Artikelgruppen-Zuordnung deckt die Gruppen nicht ab.
4. **Kommissionierungs-Badge 1, Worklist für Gerald leer** — erwartetes Verhalten bei Picker-Zuweisung (Auftrag gehört „test“).
5. **OSEON: 10.518 AGs ohne Config** und wiederkehrende Timeouts/Deadlocks im OSEON-Tracking.
6. **TLS-Prüfung der Sage-Lagerbuchung aus** (gelbes Banner, nur Testsystem zulässig).
7. **BDE-Buchungen an fremden Werkbänken** (NurFA-Modus):
   - #56 „2614157/01“, #58 „2609192/01“ und #62 „2605166/01“ wurden an **Stanzarei** gebucht.
   - Die FAs tragen laut FA-Liste („Erledigte anzeigen“, heutiger Stand) die Werkbank A1 bzw. A2.
   - v1.30.1 lässt das zu. Für die IDEAL-Version siehe Playbook B6-S3 (TS-96.10 c).
