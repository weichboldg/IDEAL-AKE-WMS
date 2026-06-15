using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[ApiController]
[Route("api/fa-work-steps")]
public class FaWorkStepsApiController : ControllerBase
{
    private readonly IFaWorkStepRepository _faWorkStepRepository;
    private readonly IWorkStepRepository _workStepRepository;
    private readonly ICurrentUserService _currentUserService;

    public FaWorkStepsApiController(IFaWorkStepRepository faWorkStepRepository,
        IWorkStepRepository workStepRepository, ICurrentUserService currentUserService)
    {
        _faWorkStepRepository = faWorkStepRepository;
        _workStepRepository = workStepRepository;
        _currentUserService = currentUserService;
    }

    public record ToggleRequest(int ProductionOrderId, string WorkStepCode, bool Value);
    public record ToggleCompletedRequest(int FaWorkStepId, bool Value);

    [HttpPost("toggle")]
    [RequirePickingOrFaCompletionAccess] // wie alter assembly-groups-Endpoint
    public async Task<IActionResult> Toggle([FromBody] ToggleRequest req)
    {
        var step = await _workStepRepository.GetByCodeAsync(req.WorkStepCode);
        if (step == null || !step.IsActive)
            return BadRequest(new { error = $"Unbekannter Arbeitsgang: {req.WorkStepCode}" });

        await _faWorkStepRepository.SetActiveAsync(req.ProductionOrderId, step.Id, req.Value,
            _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());
        return Ok();
    }

    [HttpPost("toggle-completed")]
    [RequireVorbauOrPickingOrLeitstandAccess] // Abarbeitungsliste (vorbau) + Leitstand-VK-VA (picking/leitstand)
    public async Task<IActionResult> ToggleCompleted([FromBody] ToggleCompletedRequest req)
    {
        var row = await _faWorkStepRepository.GetByIdAsync(req.FaWorkStepId);
        if (row == null) return NotFound();

        await _faWorkStepRepository.SetIsCompletedAsync(req.FaWorkStepId, req.Value,
            _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());
        return Ok();
    }
}
