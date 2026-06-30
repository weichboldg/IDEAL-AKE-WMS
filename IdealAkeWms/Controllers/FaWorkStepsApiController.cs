using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
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
    public record SetStatusRequest(int FaWorkStepId, int Status);

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

    [HttpPost("set-status")]
    [RequireVorbauOrPickingOrLeitstandAccess] // Abarbeitungsliste (vorbau) + Leitstand-VK-VA (picking/leitstand)
    public async Task<IActionResult> SetStatus([FromBody] SetStatusRequest req)
    {
        if (!Enum.IsDefined(typeof(FaWorkStepStatus), req.Status))
            return BadRequest(new { error = $"Ungueltiger Status: {req.Status}" });

        var row = await _faWorkStepRepository.GetByIdAsync(req.FaWorkStepId);
        if (row == null) return NotFound();

        await _faWorkStepRepository.SetStatusAsync(req.FaWorkStepId, (FaWorkStepStatus)req.Status,
            _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());
        return Ok();
    }
}
