using Microsoft.EntityFrameworkCore;
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IEnaioDmsDocumentRepository : IRepository<EnaioDmsDocument>
{
    Task<List<EnaioDmsDocument>> GetByOrderNumberAsync(string orderNumber);
    Task<Dictionary<string, List<EnaioDmsDocumentLink>>> GetByOrderNumbersAsync(IEnumerable<string> orderNumbers);
}

public class EnaioDmsDocumentLink
{
    public long EnaioDmsObjectId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
}

public class EnaioDmsDocumentRepository : Repository<EnaioDmsDocument>, IEnaioDmsDocumentRepository
{
    public EnaioDmsDocumentRepository(ApplicationDbContext context) : base(context) { }

    public async Task<List<EnaioDmsDocument>> GetByOrderNumberAsync(string orderNumber)
    {
        return await _dbSet
            .Where(d => d.OrderNumber == orderNumber)
            .OrderByDescending(d => d.CreatedInEnaio)
            .ToListAsync();
    }

    public async Task<Dictionary<string, List<EnaioDmsDocumentLink>>> GetByOrderNumbersAsync(
        IEnumerable<string> orderNumbers)
    {
        var orderList = orderNumbers.ToList();
        if (orderList.Count == 0) return new();

        var docs = await _dbSet
            .Where(d => d.OrderNumber != null && orderList.Contains(d.OrderNumber))
            .Select(d => new { d.OrderNumber, d.EnaioDmsObjectId, d.DocumentType })
            .ToListAsync();

        return docs
            .GroupBy(d => d.OrderNumber!)
            .ToDictionary(
                g => g.Key,
                g => g.Select(d => new EnaioDmsDocumentLink
                    {
                        EnaioDmsObjectId = d.EnaioDmsObjectId,
                        DocumentType = d.DocumentType
                    })
                    // Typ-Vorrang: Werkstattauftrag+Zeichnung zuerst, dann Werkstattauftrag, dann Rest.
                    .OrderBy(l => DocumentTypeSortRank(l.DocumentType))
                    .ThenBy(l => l.EnaioDmsObjectId)
                    .ToList()
            );
    }

    /// <summary>
    /// Sortier-Rang fuer die Anzeige der enaio-Badges: kleinere Werte erscheinen zuerst.
    /// Werkstattauftrag+Zeichnung = 0, Werkstattauftrag = 1, sonst (Zeichnung/unbekannt) = 2.
    /// </summary>
    private static int DocumentTypeSortRank(string documentType) => documentType switch
    {
        "Werkstattauftrag+Zeichnung" => 0,
        "Werkstattauftrag" => 1,
        _ => 2
    };
}
