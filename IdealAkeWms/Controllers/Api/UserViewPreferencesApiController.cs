using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers.Api;

[ApiController]
[Route("api/user-view-preferences")]
public class UserViewPreferencesApiController : ControllerBase
{
    private static readonly HashSet<string> AllowedViewKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "ProductionOrders", "Picking", "OseonTracking", "Bom"
    };

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
        var userId = _currentUserService.GetCurrentAppUserId();
        if (!_currentUserService.IsLoggedIn() || userId == null)
            return Unauthorized();

        if (!AllowedViewKeys.Contains(viewKey))
            return BadRequest($"Invalid view key: {viewKey}");

        var pref = await _repository.GetByUserAndViewAsync(userId.Value, viewKey);
        if (pref == null)
            return NoContent();

        return Ok(pref.SettingsJson);
    }

    [HttpPut("{viewKey}")]
    public async Task<IActionResult> Put(string viewKey, [FromBody] string settingsJson)
    {
        var userId = _currentUserService.GetCurrentAppUserId();
        if (!_currentUserService.IsLoggedIn() || userId == null)
            return Unauthorized();

        if (!AllowedViewKeys.Contains(viewKey))
            return BadRequest($"Invalid view key: {viewKey}");

        if (settingsJson != null && settingsJson.Length > 65536)
            return BadRequest("Settings too large (max 64KB)");

        await _repository.SaveAsync(
            userId.Value,
            viewKey,
            settingsJson ?? "{}",
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        return Ok();
    }

    [HttpDelete("{viewKey}")]
    public async Task<IActionResult> Delete(string viewKey)
    {
        var userId = _currentUserService.GetCurrentAppUserId();
        if (!_currentUserService.IsLoggedIn() || userId == null)
            return Unauthorized();

        if (!AllowedViewKeys.Contains(viewKey))
            return BadRequest($"Invalid view key: {viewKey}");

        await _repository.DeleteByUserAndViewAsync(userId.Value, viewKey);
        return Ok();
    }
}
