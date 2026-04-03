using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers.Api;

[ApiController]
[Route("api/partrequisitions")]
[RequirePickingAccess]
public class PartRequisitionsApiController : ControllerBase
{
    private readonly IPartRequisitionRepository _requisitionRepository;
    private readonly IOrderRecipientRepository _recipientRepository;
    private readonly ICurrentUserService _currentUserService;

    public PartRequisitionsApiController(
        IPartRequisitionRepository requisitionRepository,
        IOrderRecipientRepository recipientRepository,
        ICurrentUserService currentUserService)
    {
        _requisitionRepository = requisitionRepository;
        _recipientRepository = recipientRepository;
        _currentUserService = currentUserService;
    }

    [HttpGet("recipients")]
    public async Task<IActionResult> GetRecipients([FromQuery] string articleGroup)
    {
        if (string.IsNullOrWhiteSpace(articleGroup))
            return Ok(new List<RecipientGroupInfo>());

        var groups = await _recipientRepository.GetGroupsByArticleGroupAsync(articleGroup);

        var result = groups.Select(g => new RecipientGroupInfo
        {
            GroupId = g.Id,
            GroupName = g.Name,
            Recipients = g.Recipients.Select(r => new RecipientInfo
            {
                Id = r.Id,
                Name = r.Name,
                Email = r.Email
            }).ToList()
        }).ToList();

        return Ok(result);
    }

    [HttpGet("check")]
    public async Task<IActionResult> CheckExisting([FromQuery] int productionOrderId, [FromQuery] string articleNumber)
    {
        var hasOpen = await _requisitionRepository.HasOpenRequisitionAsync(productionOrderId, articleNumber);
        if (!hasOpen) return Ok(new { exists = false });

        var openItems = await _requisitionRepository.GetOpenByArticleNumberAsync(articleNumber);
        var match = openItems.FirstOrDefault(r => r.ProductionOrderId == productionOrderId);
        return Ok(new
        {
            exists = true,
            createdBy = match?.CreatedBy,
            createdAt = match?.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
            quantity = match?.Quantity
        });
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreatePartRequisitionRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest("Keine Artikel ausgewählt.");

        var sentToEmails = string.Join(",", request.SelectedEmails.Where(e => !string.IsNullOrWhiteSpace(e)));
        var displayName = _currentUserService.GetDisplayName();
        var windowsUser = _currentUserService.GetWindowsUserName();

        var requisitions = request.Items.Select(item => new PartRequisition
        {
            ProductionOrderId = request.ProductionOrderId,
            ArticleNumber = item.ArticleNumber,
            ArticleDescription = item.ArticleDescription,
            ArticleGroup = item.ArticleGroup,
            Position = item.Position,
            Quantity = item.Quantity,
            Unit = item.Unit,
            Status = PartRequisitionStatus.Offen,
            Priority = request.Priority,
            Notes = request.Notes,
            SentToEmails = string.IsNullOrEmpty(sentToEmails) ? null : sentToEmails,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = displayName,
            CreatedByWindows = windowsUser
        }).ToList();

        await _requisitionRepository.AddRangeAsync(requisitions);

        return Ok(new { count = requisitions.Count });
    }

    [HttpPost("cancel/{id}")]
    public async Task<IActionResult> Cancel(int id)
    {
        await _requisitionRepository.CancelAsync(
            id,
            _currentUserService.GetDisplayName(),
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        return Ok();
    }

    [HttpGet("open")]
    [RequireStockAccess]
    public async Task<IActionResult> GetOpenByArticle([FromQuery] string articleNumber)
    {
        if (string.IsNullOrWhiteSpace(articleNumber)) return Ok(new List<OpenRequisitionForInbound>());

        var open = await _requisitionRepository.GetOpenByArticleNumberAsync(articleNumber);
        var result = open.Select(r => new OpenRequisitionForInbound
        {
            Id = r.Id,
            OrderNumber = r.ProductionOrder.OrderNumber,
            Quantity = r.Quantity,
            Unit = r.Unit,
            CreatedBy = r.CreatedBy,
            CreatedAt = r.CreatedAt,
            Notes = r.Notes,
            Priority = r.Priority
        }).ToList();

        return Ok(result);
    }
}
