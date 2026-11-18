namespace IdealAkeWms.Models;

/// <summary>
/// Status des gespeicherten Beschichtungsauftrags (Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-3, Antwort 2).
/// Erlaubt: Entwurf → Uebergeben → Versendet → Erledigt (Versendet seit v1.99.41) und Entwurf → Storniert; keine
/// Rueckwaertsschritte. Werte mit Luecken.
/// </summary>
public enum BeschichtungsauftragStatus : byte
{
    Entwurf = 10,

    // 20 = reserviert fuer einen spaeteren Freigabe-Schritt („Freigegeben", Antwort 6) — bewusst KEIN Mitglied,
    // keine Spalten, kein Code. Wer den Schritt baut, legt ihn hier an.

    Uebergeben = 30,

    /// <summary>v1.99.41 (Spec 2026-11-18-beschichtungsauftrag-ausbau, A5): Ware ist beim Lackierer. Nur aus Uebergeben und nur
    /// mit allen Positionen verpackt; danach nur noch Erledigt. tinyint ohne CHECK — kein Schema-Eingriff fuer den Wert.</summary>
    Versendet = 35,
    Erledigt = 40,
    Storniert = 90,
}
