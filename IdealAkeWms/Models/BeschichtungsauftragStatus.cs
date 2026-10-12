namespace IdealAkeWms.Models;

/// <summary>
/// Status des gespeicherten Beschichtungsauftrags (Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-3, Antwort 2).
/// Erlaubt: Entwurf → Uebergeben → Erledigt und Entwurf → Storniert; keine Rueckwaertsschritte. Werte mit Luecken.
/// </summary>
public enum BeschichtungsauftragStatus : byte
{
    Entwurf = 10,

    // 20 = reserviert fuer einen spaeteren Freigabe-Schritt („Freigegeben", Antwort 6) — bewusst KEIN Mitglied,
    // keine Spalten, kein Code. Wer den Schritt baut, legt ihn hier an.

    Uebergeben = 30,
    Erledigt = 40,
    Storniert = 90,
}
