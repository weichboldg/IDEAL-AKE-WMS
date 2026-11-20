using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

/// <summary>v1.87.0 (Spec 2026-11-08-lagerbuchungen-fa-feld-und-verlinkung): Texte zum FA an einer Lagerbuchung – Status, Warnung nach dem
/// Buchen, Pflicht-Fehler, FA-Leserecht. Ein Ort fuer Pruefkarte (API), Rueckmeldung (Controller) und Anzeige (Historie, Bestandsuebersicht).</summary>
public static class FaBuchungsText
{
    /// <summary>Status als Text (Farbe ist nie alleiniger Bedeutungstraeger).</summary>
    public static string Status(ProductionOrder o) =>
        o.IsCancelled ? "storniert"
        : o.IsDone ? "erledigt"
        : o.SageMissingSince is { } seit ? $"in Sage vermisst seit {seit:dd.MM.yyyy}"
        : "offen";

    /// <summary>Erledigt, storniert oder in Sage vermisst → Warnung (Antwort 3: nie Sperre).</summary>
    public static bool IstWarnStatus(ProductionOrder o) => o.IsCancelled || o.IsDone || o.SageMissingSince != null;

    /// <summary>Warnung fuer eine gespeicherte Buchung (Anforderung 4) oder null.</summary>
    public static string? Warnung(string? text, FaAufloesung fa)
    {
        var t = text?.Trim();
        return fa.Status switch
        {
            FaAufloesungStatus.Gefunden when IstWarnStatus(fa.Order!) =>
                $"FA {t} ist {Status(fa.Order!)} – Buchung trotzdem gespeichert und verknüpft.",
            FaAufloesungStatus.Unbekannt => $"FA {t} ist im WMS unbekannt – Buchung ohne Verknüpfung gespeichert.",
            FaAufloesungStatus.Mehrdeutig => $"FA {t} ist im WMS mehrdeutig – Buchung ohne Verknüpfung gespeichert.",
            FaAufloesungStatus.Liste => $"Im FA-Feld stehen mehrere FAs („{t}“) – Buchung ohne Verknüpfung gespeichert.",
            _ => null,
        };
    }

    /// <summary>Grund, warum die Pflicht nicht erfuellt ist (Anforderung 5), oder null.</summary>
    public static string? PflichtFehler(string buchungsart, string? text, FaAufloesung fa) => fa.Status switch
    {
        FaAufloesungStatus.Gefunden => null,
        FaAufloesungStatus.Leer => $"Für „{buchungsart}“ ist ein FA Pflicht.",
        FaAufloesungStatus.Liste => $"Für „{buchungsart}“ ist genau ein FA Pflicht – im Feld stehen mehrere.",
        FaAufloesungStatus.Mehrdeutig => $"Für „{buchungsart}“ ist ein eindeutiger FA Pflicht – FA {text?.Trim()} ist im WMS mehrdeutig.",
        _ => $"Für „{buchungsart}“ ist ein FA Pflicht, der im WMS existiert – FA {text?.Trim()} ist unbekannt.",
    };

    /// <summary>Ist <paramref name="buchungsart"/> in der Einstellung <see cref="AppSettingKeys.FaPflichtBuchungsarten"/> (Komma-Liste)?</summary>
    public static bool IstPflicht(string? einstellung, string buchungsart) =>
        (einstellung ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Contains(buchungsart, StringComparer.OrdinalIgnoreCase);

    /// <summary>v1.99.47: FA-Text in Einzelnummern zerlegen (Trennzeichen <c>;</c> und <c>,</c>, getrimmt, leere weg) – EIN Ort fuer
    /// Repository (Spalte, Wagen-Vorbelegung), Controller (Filterfeld) und Tests.</summary>
    public static IEnumerable<string> Nummern(string? text) =>
        (text ?? "").Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>v1.99.47 (Teil 6, Anforderung 4): trifft die Artikelzeile den Filter, und warum ist sie ggf. „gemischt“?
    /// Nummer abgedeckt ⇔ im Filter oder (nur IDEAL) ihre Haupt-FA laut <paramref name="hauptJeSub"/> im Filter. G1 = <c>Fremd</c>
    /// (nicht abgedeckte Nummern), G2 = <c>OhneFa</c> (Zugang ohne FA-Text am Paar), G3 = <c>NurAusAusbuchung</c> (keine abgedeckte
    /// Nummer stammt aus einem Zugang). <paramref name="filter"/> und <paramref name="hauptJeSub"/> mit OrdinalIgnoreCase anlegen.</summary>
    public static (bool Trifft, List<string> Fremd, bool OhneFa, bool NurAusAusbuchung) PruefeZeile(
        FaAmPaar? paar, IReadOnlyDictionary<string, string> hauptJeSub, IReadOnlySet<string> filter)
    {
        if (paar == null) return (false, new(), false, false);
        bool Abgedeckt(string nr) => filter.Contains(nr) || (hauptJeSub.TryGetValue(nr, out var h) && filter.Contains(h));
        var abgedeckt = paar.Nummern.Where(Abgedeckt).ToList();
        if (abgedeckt.Count == 0) return (false, new(), false, false);
        return (true, paar.Nummern.Where(n => !Abgedeckt(n)).ToList(), paar.ZugangOhneFa, !abgedeckt.Any(paar.ZugangNummern.Contains));
    }

    /// <summary>Gemischt ⇔ trifft und mindestens ein Grund G1–G3 (Anforderung 4).</summary>
    public static bool IstGemischt((bool Trifft, List<string> Fremd, bool OhneFa, bool NurAusAusbuchung) p) =>
        p.Trifft && (p.Fremd.Count > 0 || p.OhneFa || p.NurAusAusbuchung);

    /// <summary>Master <c>ProduktionsauftragHierarchisch</c> an? (Link-Ziel FA-Struktur statt FA-Liste)</summary>
    public static bool IstHierarchisch(string? masterWert) => string.Equals(masterWert?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>FA-Leserecht = Regel des Filters <c>RequirePickingOrTrackingOrLeitstandAccess</c> (EIN Ort; der Filter ruft diese Methode).
    /// Erweiterungsmethode statt Interface-Member, damit Mocks/Fakes von <see cref="ICurrentUserService"/> unveraendert bleiben.</summary>
    public static async Task<bool> CanViewProductionOrdersAsync(this ICurrentUserService u) =>
        await u.CanPickAsync() || await u.CanViewTrackingAsync() || await u.CanManagePickingReleaseAsync() || await u.HasVorbauAccessAsync();
}
