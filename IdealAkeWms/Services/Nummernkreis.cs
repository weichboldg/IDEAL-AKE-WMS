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

    /// <summary>v1.99.23 (N-2): Deadlock in jeder Tiefe der Ausnahme-Kette (EF verpackt ihn z. B. als
    /// InvalidOperationException → DbUpdateException → SqlException 1205).</summary>
    public static bool IstDeadlock(Exception ex) => IstSqlFehler(ex, 1205);

    /// <summary>v1.99.23: true, wenn irgendeine Ausnahme der InnerException-Kette eine SqlException mit einer der Nummern ist
    /// (1205 Deadlock, 2601/2627 UNIQUE-Verletzung).</summary>
    public static bool IstSqlFehler(Exception ex, params int[] nummern)
    {
        for (Exception? e = ex; e != null; e = e.InnerException)
            if (e is SqlException sql && nummern.Contains(sql.Number)) return true;
        return false;
    }

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
