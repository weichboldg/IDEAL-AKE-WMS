using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class OrderRecipientRepository : IOrderRecipientRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRecipientRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<OrderRecipientGroup>> GetAllGroupsAsync()
    {
        return await _context.OrderRecipientGroups
            .Include(g => g.Recipients)
            .Include(g => g.ArticleGroupMappings)
            .OrderBy(g => g.Name)
            .ToListAsync();
    }

    public async Task<OrderRecipientGroup?> GetGroupByIdAsync(int id)
    {
        return await _context.OrderRecipientGroups
            .Include(g => g.Recipients)
            .Include(g => g.ArticleGroupMappings)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task AddGroupAsync(OrderRecipientGroup group)
    {
        _context.OrderRecipientGroups.Add(group);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateGroupAsync(OrderRecipientGroup group)
    {
        _context.OrderRecipientGroups.Update(group);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteGroupAsync(int id)
    {
        var group = await _context.OrderRecipientGroups.FindAsync(id);
        if (group == null) return false;

        _context.OrderRecipientGroups.Remove(group);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> GroupHasOpenRequisitionsAsync(int groupId)
    {
        return await _context.PartRequisitions
            .AnyAsync(r => r.OrderRecipientGroupId == groupId && r.Status == PartRequisitionStatus.Offen);
    }

    public async Task<OrderRecipient?> GetRecipientByIdAsync(int id)
    {
        return await _context.OrderRecipients.FindAsync(id);
    }

    public async Task AddRecipientAsync(OrderRecipient recipient)
    {
        _context.OrderRecipients.Add(recipient);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRecipientAsync(OrderRecipient recipient)
    {
        _context.OrderRecipients.Update(recipient);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRecipientAsync(int id)
    {
        var recipient = await _context.OrderRecipients.FindAsync(id);
        if (recipient != null)
        {
            _context.OrderRecipients.Remove(recipient);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<ArticleGroupRecipientMapping>> GetMappingsAsync()
    {
        return await _context.ArticleGroupRecipientMappings
            .Include(m => m.OrderRecipientGroup)
            .OrderBy(m => m.ArticleGroup)
            .ToListAsync();
    }

    public async Task<List<OrderRecipientGroup>> GetGroupsByArticleGroupAsync(string articleGroup)
    {
        var groupIds = await _context.ArticleGroupRecipientMappings
            .Where(m => m.ArticleGroup == articleGroup)
            .Select(m => m.OrderRecipientGroupId)
            .ToListAsync();

        return await _context.OrderRecipientGroups
            .Include(g => g.Recipients.Where(r => r.IsActive))
            .Where(g => groupIds.Contains(g.Id))
            .ToListAsync();
    }

    public async Task SetMappingsForArticleGroupAsync(string articleGroup, List<int> groupIds, string createdBy, string createdByWindows)
    {
        var existing = await _context.ArticleGroupRecipientMappings
            .Where(m => m.ArticleGroup == articleGroup)
            .ToListAsync();

        _context.ArticleGroupRecipientMappings.RemoveRange(existing);

        foreach (var groupId in groupIds)
        {
            _context.ArticleGroupRecipientMappings.Add(new ArticleGroupRecipientMapping
            {
                ArticleGroup = articleGroup,
                OrderRecipientGroupId = groupId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                CreatedByWindows = createdByWindows
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<string>> GetDistinctArticleGroupsAsync()
    {
        return await _context.Articles
            .Where(a => !string.IsNullOrEmpty(a.ArticleGroup))
            .Select(a => a.ArticleGroup!)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();
    }
}
