using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

/// <summary>
/// v1.99.4 (Spec 2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe F2): gemeinsame Regeln des Sage-Wareneingangsbelegs (ELL) fuer
/// Web (Status beim Buchen, Platzregel) und Dienst (Sender). Ein Ort fuer Schalter-Key und Platzregel.
/// </summary>
public static class SageWareneingang
{
    /// <summary>ServiceSetting (Default aus). Steht es aus, verhaelt sich alles wie Teil 1 (Lagerbuchungs-Queue).</summary>
    public const string ToggleKey = "SageWareneingangBelegAktiv";

    /// <summary>Audit-Kennung des Dienstes bei Statuswechseln (Muster SageBookingQueueRepository.ServiceActor).</summary>
    public const string ServiceActor = "system:sage-wareneingang";

    /// <summary>Sage-Belegkennzeichen des Wareneingangs (P-W7: ELL, Konstante, kein Setting).</summary>
    public const string BelegKennzeichen = "ELL";

    /// <summary>Nur exakt "true" (ohne Gross/Klein, getrimmt) schaltet ein.</summary>
    public static bool IstAn(string? wert) => string.Equals(wert?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>Freigabe-Antwort 3: buchbar nur Plaetze, die in Sage vorhanden sind (SageLagerkennung + SageLagerplatzId) UND
    /// "Sage-Buchung erlaubt" haben (dasselbe Flag wie Decorator und Nacherfassung).</summary>
    public static bool IstSageWePlatz(StorageLocation? l) =>
        l != null && l.SageBuchungErlaubt && !string.IsNullOrWhiteSpace(l.SageLagerkennung) && l.SageLagerplatzId != null;

    public static string PlatzFehler(string code) =>
        $"Ziel-Platz {code} ist kein Sage-Platz mit Sage-Buchung (nicht in Sage vorhanden oder „Sage-Buchung erlaubt“ aus) – Sage-Wareneingang nicht möglich. Bitte einen Sage-Platz wählen.";

    /// <summary>Korrelation am Sage-Beleg (Memo): WE#&lt;erste Receipt-Id der Gruppe&gt;# (Trennzeichen-Regel wie SM#&lt;id&gt;#).</summary>
    /// v1.99.9: mit System-Kürzel <c>WE#&lt;K&gt;#&lt;id&gt;#</c> (<see cref="SageSystemKuerzel"/>).
    public static string Memo(int receiptId, string? kuerzel = null) => SageSystemKuerzel.Kennung("WE", kuerzel, receiptId);

    /// <summary>v1.99.9: Kennung eines Receipts - die beim Senden gespeicherte; nie gesendet (Versuche 0) = mit dem aktuellen Kürzel;
    /// gesendet vor v1.99.9 (ohne gespeicherte Kennung) = altes Format ohne Kürzel. Kopf-Memo = Kennung des ersten Receipts der Gruppe.</summary>
    public static string Kennung(PurchaseOrderReceipt r, string? kuerzel) =>
        r.SageWeKennung ?? (r.SageWeVersuche == 0 ? Memo(r.Id, kuerzel) : Memo(r.Id));

    public static string UnklarText(string kennung) =>
        $"Ergebnis unklar – keine Antwort von Sage. Vor dem nächsten Versuch prüft der Dienst in Sage, ob ein Wareneingangsbeleg mit Memo {kennung} existiert.";

    // ---- v1.99.7 E5 (Variante A) und Q-2 ----

    /// <summary>ServiceSetting: automatische Wiederholungen je Wareneingang nach einem Fehler (0 = keine, nur „Erneut senden“).</summary>
    public const string MaxWiederholungenKey = "Sync:SageWareneingangMaxWiederholungen";
    /// <summary>ServiceSetting: Mindestabstand in Minuten zwischen Fehler und automatischem Wiederholen.</summary>
    public const string WiederholAbstandKey = "Sync:SageWareneingangWiederholAbstandMinuten";

    /// <summary>Status, die noch nicht an Sage übergeben sind und bei ausgeschaltetem Schalter liegen bleiben (Q-2-Hinweis).</summary>
    public static readonly SageWareneingangStatus[] NichtUebergeben =
        { SageWareneingangStatus.Offen, SageWareneingangStatus.Fehler, SageWareneingangStatus.Klaeren };

    public static string KlaerenText(IEnumerable<string> belege) =>
        $"Möglicher Beleg in Sage gefunden, Memo fehlt/geändert: {string.Join(", ", belege)}. Nicht automatisch gesendet – in Sage prüfen, dann „Als übergeben markieren“ oder „Erneut senden“.";

    public const string AusText = "Sage-Wareneingang war ausgeschaltet – nicht gesendet und beim Wiedereinschalten nicht automatisch nachgeschickt. In Sage prüfen, dann „Erneut senden“ oder „Als übergeben markieren“.";

    /// <summary>Q-2: Hinweis bei ausgeschaltetem Schalter und liegengebliebenen Einträgen; null = kein Hinweis.</summary>
    public static string? AusHinweis(bool an, int nichtUebergeben) => an || nichtUebergeben == 0 ? null :
        $"Sage-Wareneingang ist aus – {nichtUebergeben} {(nichtUebergeben == 1 ? "Eintrag" : "Einträge")} offen, werden nicht gesendet. " +
        "Auch nach dem Wiedereinschalten nur über „Erneut senden“.";
}
