using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers.Api;

[ApiController]
[Route("api/user-view-preferences")]
public class UserViewPreferencesApiController : ControllerBase
{
    private readonly IUserViewPreferenceRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public UserViewPreferencesApiController(
        IUserViewPreferenceRepository repository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    [HttpGet("{viewKey}")]
    public async Task<IActionResult> Get(string viewKey)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        if (ColumnDefinitions.GetByViewKey(viewKey) == null)
            return BadRequest($"Invalid view key: {viewKey}");

        var pref = await _repository.GetByUserAndViewAsync(userId, viewKey);
        if (pref == null)
            return NoContent();

        return Ok(pref.SettingsJson);
    }

    [HttpPut("{viewKey}")]
    public async Task<IActionResult> Put(string viewKey, [FromBody] string settingsJson)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        if (ColumnDefinitions.GetByViewKey(viewKey) == null)
            return BadRequest($"Invalid view key: {viewKey}");

        if (settingsJson != null && settingsJson.Length > 65536)
            return BadRequest("Settings too large (max 64KB)");

        await _repository.SaveAsync(
            userId,
            viewKey,
            settingsJson ?? "{}",
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        return Ok();
    }

    [HttpDelete("{viewKey}")]
    public async Task<IActionResult> Delete(string viewKey)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        if (ColumnDefinitions.GetByViewKey(viewKey) == null)
            return BadRequest($"Invalid view key: {viewKey}");

        await _repository.DeleteByUserAndViewAsync(userId, viewKey);
        return Ok();
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var id = _currentUserService.GetCurrentAppUserId();
        userId = id ?? 0;
        return id.HasValue;
    }
}
