---
type: changelog
version: 1.99.31
date: 2026-11-17
---
# v1.99.31 — Offene Bestellungen: sofort neu lesen nach Sage-WE, Einkauf-Ansichten, Datumsvorbelegung Liefertermin, „Überfällige anzeigen“

Bündel `feature/2026-08-07-ideal-teile-1-5`, Commits `8b7d8252` (Dienst F1), `2f84a012` (Web F2/F2a/F3/F4), `9c623d4c` (wip feature-complete:
Version, Doku, TS-155, Manifest). Spec [[2026-11-16-offene-bestellungen-sofort-neu-lesen-einkauf-kw-filter-spec]]. Nicht gemergt. Spec nannte
1.99.32 – vergeben 1.99.31 (1.99.27 reserviert Arbeitsgang-Mapping). Lauf-Notiz [[2026-11-17-goal-lauf-offene-bestellungen]].

## Kernpunkte

- **F1 Anstoß nach Sage-WE (Dienst):** `SageWareneingangService.RunOnceAsync` gibt die Zahl übergebener Belege zurück; der `SageBookingWorker`
  sammelt sie und startet danach `EinkaufBestellungenSyncService.TryRunExclusiveAsync` (Info „Anlass: Sage-Wareneingang (n Beleg(e)
  übergeben)“). Entprellung über `Sync:EinkaufBestellungenNachWeMinuten` (Default 2, 0–60, 0 = aus), Entscheidung als
  `SageBookingWorker.AnstossEntscheidung` (statisch, getestet). Gates wie der reguläre Lauf (`Sync:EinkaufBestellungenEnabled`,
  `WorkerSettings:SyncDryRun`); Gate aus verwirft den Anstoß. **Prozessweite Sperre** (`static SemaphoreSlim`) in
  `EinkaufBestellungenSyncService`: regulärer Lauf wartet, Anstoß überspringt (bleibt offen) – nie zwei Komplett-Ersetzungen parallel. Fehler im
  Anstoß → Warnungslauf unter „Sage-Wareneingang“, keine Fehlermail, der reguläre Lauf holt nach.
- **F2/F2a Einkauf-Ansichten:** neuer `EinkaufPurchaseOrdersController` (`[RequireEinkaufAccess]` + `[RequireEinkaufAktiv]`, kein Lager-Recht nötig)
  ruft die Listen des `PurchaseOrdersController` auf (per DI, `AddScoped<PurchaseOrdersController>`; eine Implementierung, zwei Aufrufer). Menü
  Einkauf → „Offene Bestellungen“ und „Sage-Wareneingänge (offen)“ (`PurchaseOrdersController.OffeneSageWeStatus` = Offen, Gesendet, Fehler,
  Klären, NurInSage; gilt auch bei `status=Uebergeben` per URL). Knopf Wareneingang / Aktionen nur mit Lager-Recht bzw. `CanTransferStock` **und**
  `WareneingangBestellungAktiv` (sonst wären die Ziele 404). Formulare der gemeinsamen Views mit `asp-controller="PurchaseOrders"`.
  `RequireEinkaufAktiv`-Text jetzt allgemein „Einkauf ist deaktiviert (Einstellung „Einkauf aktiv“).“
- **F3 Datumsvorbelegung:** Feld „Wochen“ und Horizont entfallen. Erstaufruf (kein `colf_liefertermin`, kein Merker `kw`) → Redirect mit
  `colf_liefertermin=<Mo aktuelle ISO-KW>...<So N-te KW>` + `kw=1` (`LieferterminVorbelegung`, jahresgenau, KW 53). Zelle und ColumnMap
  `dd.MM.yyyy KWnn` (`LieferterminText`). AppSetting `OffeneBestellungenVorschauWochen` bleibt (Key, Default 2, 1–26), Anzeigename/Wirkung neu.
- **F4 „Überfällige anzeigen“:** Checkbox je Benutzer und Ansicht in `UserViewPreferences` (`PurchaseOrders.Ueberfaellige` /
  `EinkaufPurchaseOrders.Ueberfaellige`, JSON `{"an":bool}`, Default Lager aus / Einkauf an, Default wird nicht gespeichert). Sichtbarkeit
  `Sichtbarkeit(...)`: ohne Termin immer, überfällig nur mit Checkbox (Liefertermin-Filter wirkt auf sie nicht), sonst Liefertermin-Filter;
  „nur überfällig“ schließt die Checkbox ein. Ausgeblendete werden gezählt (Hinweiszeile, melden statt still).

## Abweichungen zur Spec

- Anlass-Text zählt **Belege** („n Beleg(e) übergeben“) statt Positionen: der Zähler `uebergeben` im `SageWareneingangService` zählt Vorgänge.
- Überschrift der Einkauf-Liste „Offene Bestellungen (Einkauf)“ zur Unterscheidung vom Lager.

## Migration / SQL

- Keine. Neue ServiceSetting im Katalog (`ServiceSettingDefinitions`, Drift-Guard grün), Seed über den Katalog.

## Deploy-Risiken

- Web und Dienst aus demselben Stand. Default 2 greift sofort nach dem Dienststart – wer keinen Anstoß will, vorher 0 setzen.
- Jeder Anstoß liest alle offenen Bestellungen; in der Zeit wartet die nächste Sage-Lagerbuchung (Laufzeit P-1 beobachten).
- Manifest `scripts/deploy/nachlauf.psd1`: `v1.99.31-NachWeMinuten` (Einstellung, AKE), `v1.99.31-VorschauWochen` (Einstellung, beide),
  `v1.99.31-EinkaufAnsichten` (Info, beide), Beschreibung der Rolle `einkauf` im Reparatur-Insert `Pruefung/RolleEinkauf`.

## Tests

- `dotnet build IdealAkeWms.slnx` 0 Fehler; `dotnet test`: Web 3013 grün (1 übersprungen), Dienst 682 grün.
- TS-155.1–155.11 in `docs/TESTSZENARIEN.md`.
