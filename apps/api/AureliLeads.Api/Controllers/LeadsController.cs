using AureliLeads.Api.DTOs;
using AureliLeads.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AureliLeads.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/leads")]
public sealed class LeadsController : ControllerBase
{
    private readonly ILeadService _leadService;

    public LeadsController(ILeadService leadService)
    {
        _leadService = leadService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LeadListDto>>> GetLeads(CancellationToken cancellationToken)
    {
        var leads = await _leadService.GetLeadsAsync(cancellationToken);
        return Ok(leads);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<LeadDetailDto> GetLead(Guid id)
    {
        // TODO: implement lead detail retrieval.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpPost]
    public ActionResult<LeadDetailDto> CreateLead([FromBody] CreateLeadRequest request)
    {
        // TODO: implement lead creation.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpPut("{id:guid}")]
    public ActionResult<LeadDetailDto> UpdateLead(Guid id, [FromBody] UpdateLeadRequest request)
    {
        // TODO: implement lead update.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpDelete("{id:guid}")]
    public IActionResult DeleteLead(Guid id)
    {
        // TODO: implement lead deletion.
        return StatusCode(StatusCodes.Status501NotImplemented);
    }
}
