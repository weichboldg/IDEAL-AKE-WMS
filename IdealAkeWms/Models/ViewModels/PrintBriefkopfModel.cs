using System.Collections.Generic;
using System.Linq;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// Modell des Kopf-Partials <c>_PrintBriefkopf</c> (Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-2): der Teil
/// unter dem Briefkopf der IT — Empfaenger, Dokument, Datum. Empfaenger = Dienstleister aus der Sage-Kopfzeile
/// (<see cref="FaHierarchyOrderInfo.Dienstleister"/>); fehlt er oder ist der Kopf mehrdeutig (Kombigeraet), steht
/// ein sichtbarer Text statt einer Leerstelle (Anforderung 3, „melden statt still behandeln").
/// </summary>
public sealed record PrintBriefkopfModel(string Dokument, int HauptFA, IReadOnlyList<FaHierarchyOrderInfo> OrderInfos)
{
    public const string DokumentBeschichtung = "Beschichtungsauftrag";
    public const string DokumentBeschichtungSummiert = "Beschichtungsauftrag (summiert)";
    public const string EmpfaengerFehlt = "Empfänger fehlt — in Sage nachpflegen";
    public const string EmpfaengerMehrdeutig = "mehrdeutig — siehe Kopfzeilen";

    /// <summary>Gespeicherter Auftrag ohne Dienstleister — nach Antwort 9 (Pflicht ab Uebergeben) eigentlich unmoeglich,
    /// deshalb sichtbar statt leer (Invarianten-Anzeige, Teil 4).</summary>
    public const string EmpfaengerFehltAmAuftrag = "Empfänger fehlt — am Auftrag nachtragen";

    /// <summary>Gesetzt: Empfaenger ist der Dienstleister des gespeicherten Auftrags (Teil 4, Antwort 5), nicht die
    /// Sage-Kopfzeile. Dann wird <see cref="AuftragsEmpfaenger"/> statt <see cref="OrderInfos"/> verwendet.</summary>
    public bool EmpfaengerVomAuftrag { get; init; }
    public string? AuftragsEmpfaenger { get; init; }

    public const string AnschriftFehlt = "Keine Anschrift — Lackierer aus den Kreditoren wählen.";
    public const string AnschriftUnvollstaendig = "Anschrift des Kreditors unvollständig (Straße, PLZ, Ort) — in den Kreditoren pflegen.";

    /// <summary>v1.99.41 A11 g: Brief-Anschrift des Kreditors zum Druckzeitpunkt (Name, Strasse, PLZ Ort, Land nur wenn gefuellt);
    /// leer = nur Empfaenger-Text.</summary>
    public IReadOnlyList<string> Anschrift { get; init; } = new List<string>();

    /// <summary>Hinweis nur am Bildschirm (.briefkopf-hinweis), nicht im Ausdruck/PDF.</summary>
    public string? AnschriftHinweis { get; init; }

    public static PrintBriefkopfModel FuerAuftrag(string nummer, int hauptFa, string? dienstleister, Creditor? kreditor = null)
    {
        var anschrift = new List<string>();
        string? hinweis = AnschriftFehlt;
        if (kreditor is not null)
        {
            var ortZeile = string.Join(" ", new[] { kreditor.PostalCode, kreditor.City }.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t!.Trim()));
            anschrift.AddRange(new[] { kreditor.Name, kreditor.Street, ortZeile, kreditor.Country }
                .Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t!.Trim()));
            hinweis = string.IsNullOrWhiteSpace(kreditor.Street) || string.IsNullOrWhiteSpace(kreditor.PostalCode)
                      || string.IsNullOrWhiteSpace(kreditor.City) ? AnschriftUnvollstaendig : null;
        }
        return new($"{DokumentBeschichtung} {nummer}", hauptFa, new List<FaHierarchyOrderInfo>())
        {
            EmpfaengerVomAuftrag = true,
            AuftragsEmpfaenger = anschrift.Count > 0 ? anschrift[0] : dienstleister,
            Anschrift = anschrift,
            AnschriftHinweis = hinweis,
        };
    }

    public bool EmpfaengerIstWarnung => Empfaenger is EmpfaengerFehlt or EmpfaengerMehrdeutig or EmpfaengerFehltAmAuftrag;

    public string Empfaenger => EmpfaengerVomAuftrag
        ? (string.IsNullOrWhiteSpace(AuftragsEmpfaenger) ? EmpfaengerFehltAmAuftrag : AuftragsEmpfaenger.Trim())
        : OrderInfos.Count switch
    {
        > 1 => EmpfaengerMehrdeutig,
        1 when !string.IsNullOrWhiteSpace(OrderInfos[0].Dienstleister) => OrderInfos[0].Dienstleister!.Trim(),
        _ => EmpfaengerFehlt,
    };
}
