---
type: backlog
status: spezifiziert
spec: "[[2026-11-18-sage-userfelder-als-einstellung-spec]]"
created: 2026-11-18
quelle: Gerald, Chat 2026-11-18 09:15 („bei den Userfeldern nicht hardcodiert, immer als Einstellung“)
---
# Sage-Benutzerfelder (USER_*) nie im Code, immer als Einstellung

## Anlass (IDEAL-Testsystem :88, v1.99.39, Aktivitäts-Protokoll 18.11. 09:08, nur gelesen)
- **Kreditoren-Abgleich:** SData `Kontokorrent` HTTP 500 „Ungültiger Spaltenname "USER_EKMindestbestellwert"“ – das Feld gibt es nur in Sage AKE.
  Fest im Code: `IDEALAKEWMSService/Services/Sdata/KontokorrentSyncService.cs:49` (`SelectKreditoren = SelectBasis + ",USER_EKMindestbestellwert"`).
- **Artikel-Lieferanten-Abgleich:** SData HTTP 404 „Die Resource 'ArtikelLieferantInfo' ist nicht definiert“ (IDEAL-Vertrag hat die Resource nicht).
- **Produktionsaufträge-Sync:** „Ungültiger Objektname dbo.vw_AKE_Kommissionierung_WAListe“ – AKE-View am IDEAL-System; Fehler besteht schon
  seit mindestens 17.11. 20:35 (vor dem Update) → Einstellungsfrage am Test (Sync:ProductionOrdersEnabled / View-Name), kein Fehler von v1.99.39.
- 08:46 HTTP 401 (Offene Bestellungen, Debitoren) – danach nicht mehr; vermutlich Kennwort neu gesetzt.

## Fest codierte USER-Felder (Worktree, Stand 2026-11-18)
| Fundstelle | Felder | Standort |
|---|---|---|
| `IDEALAKEWMSService/Services/Sdata/KontokorrentSyncService.cs:49` | USER_EKMindestbestellwert | AKE |
| `IdealAkeWms/Services/BfArtikelFelder.cs:13-19` | USER_OSIstLagerartikel, USER_OSArtikeltyp, USER_OSMaterial, USER_OSMaterialnummerPN, USER_OSFertigungsInfoPN, USER_OSArbeitsschritte, USER_OSAbteilung | IDEAL |
| `IDEALAKEWMSService/Services/Sdata/SdataFaStrukturReader.cs:149-151` | USER_OSKommissionieren, USER_OSArbeitsschritte, USER_OSAbteilung, USER_OSFertigungsInfoPN, USER_OSBemerkungPN, USER_OSArtikeltyp, USER_OSBeschichtet, USER_OSBreite, USER_OSHoehe, USER_OSTiefe, USER_OSMaterial, USER_OSVMBedarf, USER_OSEKBedarf | IDEAL |
| `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs:826` | USER_Mindestbestand (Spalte) | – |
Bereits als Einstellung: `Sync:ArtikelBenutzerfelder` (Liste), `ArtikelBenutzerfelderWriter.cs:121` nur Präfix.
Nicht erfasst: Feldnamen in SQL-Views (eigene Views) und `RealTimeData`-Aliasse (`KHKEKBelege_USER_AB/_Kom`) – im Lauf mit prüfen.

## Vorschlag
Je Feld eine ServiceSetting (ADR 0020: Anzeigename, Wirkung, Default = heutiger Name, Standort), leer = Feld wird nicht gelesen und die
Funktion zeigt „nicht konfiguriert“ statt eines Fehlers. Abgleiche, deren SData-Resource fehlt (404), melden das einmal verständlich
(„Resource am Sage-Server nicht vorhanden – Abgleich in den Service-Einstellungen ausschalten“) statt bei jedem Lauf zu scheitern.

## Stand 2026-11-18
Umgesetzt in v1.99.40 (`127f7e3f`). Die vollständige Erhebung (27 Felder inkl. Präfix-Verkettungen `KHKEKBelege_USER_AB/_Kom` und `KHKPpsFaBelege_USER_OS*`) steht in [[2026-11-18-sage-userfelder-als-einstellung-spec]]; Katalog im Code: `SageUserfelder.Katalog`. Nicht erfasst, weil schon Einstellung: `Sync:ArtikelBenutzerfelder`.
