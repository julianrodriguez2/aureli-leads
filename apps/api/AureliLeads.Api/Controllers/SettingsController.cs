using AureliLeads.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AureliLeads.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/settings")]
public sealed class SettingsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<SettingDto>> GetSettings()
    {
        // TODO: implement settings list retrieval.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpGet("{key}")]
    public ActionResult<SettingDto> GetSetting(string key)
    {
        // TODO: implement setting retrieval.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpPut("{key}")]
    public ActionResult<SettingDto> UpdateSetting(string key, [FromBody] SettingDto request)
    {
        // TODO: implement setting update.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }
}
