using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Services;

/// <summary>
/// v1.88.0: gemeinsame Nummernvergabe <c>PRAEFIX-JJJJ-NNNNN</c> (Beschichtungsauftrag „BA“, Kommissionierauftrag „KO“). <c>MAX+1</c> im Jahr,
/// der UNIQUE-Index entscheidet bei parallelem Anlegen, der Verlierer verwirft seine Aenderungen und wiederholt (auch als Deadlock-Opfer
/// einer Serializable-Transaktion). Aus <c>BeschichtungsauftragWorkflow</c> gezogen; die BA-Tests sind der Waechter.
/// </summary>
public static class Nummernkreis
{
    public static string Format(string praefix, int jahr, int laufendeNummer) => $"{praefix}-{jahr}-{laufendeNummer:D5}";

    public static bool IstDeadlock(Exception ex) => ex is SqlException { Number: 1205 } || ex.InnerException is SqlException { Number: 1205 };

    /// <summary>Fuehrt <paramref name="anlage"/> bis zu <paramref name="maxVersuche"/>-mal aus. Rueckgabe <c>Ok=false</c>, wenn alle Versuche
    /// am Speichern scheiterten (Aufrufer formuliert die Meldung).</summary>
    public static async Task<(bool Ok, T? Wert)> MitVersuchenAsync<T>(int maxVersuche, Func<Task<T>> anlage, Action verwerfen,
        ILogger logger, string was)
    {
        for (var versuch = 1; ; versuch++)
        {
            try
            {
                return (true, await anlage());
            }
            catch (Exception ex) when (ex is DbUpdateException || IstDeadlock(ex))
            {
                // Jeder Versuch in einer neuen Transaktion; die alte ist mit der Ausnahme zurueckgerollt (Dispose).
                // ponytail: es wird jeder DbUpdateException wiederholt (Unique-Konflikt der Nummer ist ohne SqlException-
                // Nummer nicht sicher erkennbar); andere DB-Fehler kosten so maxVersuche Versuche — deshalb jeder Versuch im Log.
                verwerfen();
                logger.LogWarning(ex, "{Was}: Versuch {Versuch}/{Max} gescheitert.", was, versuch, maxVersuche);
                if (versuch >= maxVersuche) return (false, default);
            }
        }
    }
}
