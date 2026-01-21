using AureliLeads.Api.DTOs;
using AureliLeads.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AureliLeads.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/automation-events")]
public sealed class AutomationEventsController : ControllerBase
{
    private readonly IAutomationService _automationService;

    public AutomationEventsController(IAutomationService automationService)
    {
        _automationService = automationService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<AutomationEventListDto>> GetEvents()
    {
        // TODO: implement automation event list retrieval.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<AutomationEventDetailDto> GetEvent(Guid id)
    {
        // TODO: implement automation event detail retrieval.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpPost]
    public ActionResult<AutomationEventDetailDto> CreateEvent([FromBody] CreateAutomationEventRequest request)
    {
        // TODO: implement automation event creation.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpPost("{id:guid}/dispatch")]
    public IActionResult DispatchEvent(Guid id)
    {
        // TODO: implement immediate dispatch.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }
}
