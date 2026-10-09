using System.Collections.Generic;

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

    public string Empfaenger => OrderInfos.Count switch
    {
        > 1 => EmpfaengerMehrdeutig,
        1 when !string.IsNullOrWhiteSpace(OrderInfos[0].Dienstleister) => OrderInfos[0].Dienstleister!.Trim(),
        _ => EmpfaengerFehlt,
    };
}
