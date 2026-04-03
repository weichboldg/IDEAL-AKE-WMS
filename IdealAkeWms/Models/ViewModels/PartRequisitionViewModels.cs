using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class PartRequisitionIndexViewModel
{
    public List<PartRequisitionListItem> Items { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public bool ShowAll { get; set; }
    public string? SearchTerm { get; set; }
}

public class PartRequisitionListItem
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public int ProductionOrderId { get; set; }
    public string? Customer { get; set; }
    public string ArticleNumber { get; set; } = string.Empty;
    public string? ArticleDescription { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EmailSentAt { get; set; }
    public string? Notes { get; set; }
}

public class CreatePartRequisitionRequest
{
    public int ProductionOrderId { get; set; }
    public List<CreatePartRequisitionItem> Items { get; set; } = new();
    public string Priority { get; set; } = PartRequisitionPriority.Normal;
    public string? Notes { get; set; }
    public List<string> SelectedEmails { get; set; } = new();
}

public class CreatePartRequisitionItem
{
    public string ArticleNumber { get; set; } = string.Empty;
    public string? ArticleDescription { get; set; }
    public string? ArticleGroup { get; set; }
    public string? Position { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
}

public class RecipientGroupInfo
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public List<RecipientInfo> Recipients { get; set; } = new();
}

public class RecipientInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class OpenRequisitionForInbound
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
    public string Priority { get; set; } = string.Empty;
}
