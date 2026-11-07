using Microsoft.Extensions.Hosting;

namespace IDEALAKEWMSService.Tests.Workers;

/// <summary>
/// Spec 2026-11-08-bereinigungen-nach-buendel-2026-11-07 F3: Worker-Tests warten auf ein Signal statt auf ein festes 150/200-ms-Fenster
/// (unter Last las der Worker DB-first seine Einstellungen laenger als das Fenster, der Mock wurde nie erreicht -> zufaellig rot).
/// </summary>
internal static class WorkerTestLauf
{
    /// <param name="erreicht">Signal: der zuletzt geprueften Mock-Aufruf ist erfolgt (z. B. <c>mock.Invocations.Count &gt; 0</c>).</param>
    public static async Task RunUntilAsync(BackgroundService worker, Func<bool> erreicht)
    {
        await worker.StartAsync(CancellationToken.None);
        try
        {
            var bis = DateTime.UtcNow.AddSeconds(10);
            while (!erreicht() && DateTime.UtcNow < bis)
                await Task.Delay(10);
        }
        finally
        {
            // StopAsync wartet, bis ExecuteAsync die laufende Iteration beendet hat -> Verify danach ist stabil.
            await worker.StopAsync(CancellationToken.None);
        }
    }
}
