using IdealAkeWms.Models;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Ergebnis eines FA-Struktur-Lesevorgangs (A5, IDEAL). <paramref name="ListeAnzahl"/>/<paramref name="InfosAnzahl"/> sind die
/// ungefilterten Roh-Zeilenzahlen fuer den Empty-Guard des Abgleichs; ist eine davon 0, sind <paramref name="Knoten"/> und
/// <paramref name="Infos"/> leer (es wurde nichts weiter gelesen) und der Abgleich ueberspringt ohne Mutation.
/// <paramref name="Positionen"/> (E4b): FA-Positionen aus SData; <c>null</c> = diesmal nicht aus SData gelesen (View) → die Tabelle
/// <c>ProductionOrderPositions</c> bleibt unberuehrt.
/// </summary>
public sealed record FaStrukturLesung(List<FaHierarchyNode> Knoten, List<FaHierarchyOrderInfo> Infos, long ListeAnzahl, long InfosAnzahl,
    List<ProductionOrderPosition>? Positionen = null);

/// <summary>
/// Quelle des FA-Struktur-Abgleichs (<see cref="FaHierarchySyncService"/>). View/SQL = <see cref="SageFaStrukturReader"/>,
/// SData = <c>Sdata.SdataFaStrukturReader</c> (nur Kanten, G11). Wirft <see cref="FaHierarchyViewNameException"/> bei ungueltigem
/// View-Namen, bevor SQL laeuft (der Abgleich meldet und ueberspringt dann wie bisher).
/// </summary>
public interface ISageFaStrukturReader
{
    Task<FaStrukturLesung> LesenAsync(CancellationToken ct = default);
}
