using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdealAkeWms.Data.Repositories;

/// <summary>
/// Enqueue-Decorator (Subclassing, ADR 0001 fuer die DI-Registrierung): erbt das gesamte
/// Verhalten von <see cref="StockMovementRepository"/> und ueberschreibt AUSSCHLIESSLICH
/// <see cref="AddAsync"/>. Fuer alle uebrigen Methoden ist er ein reiner Pass-through — bei
/// deaktiviertem Toggle ist das Verhalten bit-identisch zum Ist-Zustand (S7).
///
/// Nach dem Speichern einer manuellen Ein-/Ausbuchung (bzw. Umbuchung, Schalter
/// <c>SageLagerbuchungUmbuchungAktiv</c>, v1.73.0 E7) wird — kumulativ aus globalem Toggle und
/// Lagerplatz-Flag — ein Sage-Queue-Eintrag angelegt. Ein Enqueue-Fehler wird gefangen und
/// protokolliert, aber NIE geworfen (B4): die WMS-Buchung ist bereits committed und darf weder
/// scheitern noch den Anwender zu einer Doppelbuchung verleiten. Verpasste Enqueues faengt der
/// Reconciliation-Sweep des Workers nachtraeglich ein.
/// </summary>
public class SageBookingEnqueueingStockMovementRepository : StockMovementRepository
{
    private readonly IServiceSettingRepository _serviceSettings;
    private readonly ISageBookingQueueRepository _queue;
    private readonly ILogger<SageBookingEnqueueingStockMovementRepository> _logger;

    private const string GlobalToggleKey = "SageLagerbuchungAktiv";

    public SageBookingEnqueueingStockMovementRepository(
        ApplicationDbContext context,
        IServiceSettingRepository serviceSettings,
        ISageBookingQueueRepository queue,
        ILogger<SageBookingEnqueueingStockMovementRepository> logger)
        : base(context)
    {
        _serviceSettings = serviceSettings;
        _queue = queue;
        _logger = logger;
    }

    public override async Task<StockMovement> AddAsync(StockMovement entity)
    {
        var saved = await base.AddAsync(entity);

        // Verteidigung in der Tiefe: nur Ein-/Ausbuchung (und Umbuchung, falls freigeschaltet) kommt ueberhaupt in Frage.
        if (!SageBookingEnqueueDecision.IsBookableType(saved.MovementType, umbuchungAktiv: true))
            return saved;

        try
        {
            var globalToggle = ParseBool(await _serviceSettings.GetValueAsync(GlobalToggleKey));
            if (!globalToggle)
                return saved;

            // Umbuchung (v1.73.0 E7): eigener Schalter, Default aus. Nur bei einer Umbuchung lesen (Ein-/Ausbuchung unveraendert).
            var umbuchungAktiv = saved.MovementType == MovementType.Umbuchung
                && ParseBool(await _serviceSettings.GetValueAsync(SageBookingEnqueueDecision.UmbuchungToggleKey));

            var location = await _context.Set<StorageLocation>().FindAsync(saved.StorageLocationId);
            var locationAllows = location?.SageBuchungErlaubt ?? false;
            var sourceAllows = false;
            if (umbuchungAktiv && saved.SourceStorageLocationId is int sourceId)
                sourceAllows = (await _context.Set<StorageLocation>().FindAsync(sourceId))?.SageBuchungErlaubt ?? false;

            if (SageBookingEnqueueDecision.ShouldEnqueue(saved.MovementType, globalToggle, umbuchungAktiv, locationAllows, sourceAllows))
                await _queue.EnqueueAsync(saved);
        }
        catch (Exception ex)
        {
            // B4: Enqueue-Fehler darf die (bereits gespeicherte) WMS-Buchung nicht mitreissen.
            _logger.LogError(ex,
                "Sage-Enqueue fuer StockMovement {StockMovementId} fehlgeschlagen — Buchung bleibt gueltig, " +
                "Reconciliation-Sweep holt sie nach.", saved.Id);
        }

        return saved;
    }

    private static bool ParseBool(string? value)
        => value is not null && value.Equals("true", StringComparison.OrdinalIgnoreCase);
}
