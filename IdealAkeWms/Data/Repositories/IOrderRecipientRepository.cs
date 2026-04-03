using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IOrderRecipientRepository
{
    // Gruppen
    Task<List<OrderRecipientGroup>> GetAllGroupsAsync();
    Task<OrderRecipientGroup?> GetGroupByIdAsync(int id);
    Task AddGroupAsync(OrderRecipientGroup group);
    Task UpdateGroupAsync(OrderRecipientGroup group);
    Task<bool> DeleteGroupAsync(int id);
    Task<bool> GroupHasOpenRequisitionsAsync(int groupId);

    // Empfänger
    Task<OrderRecipient?> GetRecipientByIdAsync(int id);
    Task AddRecipientAsync(OrderRecipient recipient);
    Task UpdateRecipientAsync(OrderRecipient recipient);
    Task DeleteRecipientAsync(int id);

    // Mappings
    Task<List<ArticleGroupRecipientMapping>> GetMappingsAsync();
    Task<List<OrderRecipientGroup>> GetGroupsByArticleGroupAsync(string articleGroup);
    Task SetMappingsForArticleGroupAsync(string articleGroup, List<int> groupIds, string createdBy, string createdByWindows);

    // Alle bekannten Artikelgruppen
    Task<List<string>> GetDistinctArticleGroupsAsync();
}
