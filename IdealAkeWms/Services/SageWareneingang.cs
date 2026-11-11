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
    public static string Memo(int ersteReceiptId) => $"WE#{ersteReceiptId}#";

    public static string UnklarText(int ersteReceiptId) =>
        $"Ergebnis unklar – bitte in Sage prüfen, ob ein Wareneingangsbeleg mit Memo {Memo(ersteReceiptId)} existiert. Nicht automatisch neu gesendet.";
}
