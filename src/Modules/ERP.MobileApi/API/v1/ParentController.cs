using ERP.MobileApi.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.MobileApi.API.v1;

[ApiController]
[Route("api/mobile/v1/parent")]
[Authorize]
public class ParentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public ParentController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] Guid childStudentId, CancellationToken ct)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var userId = _currentUser.UserId ?? Guid.Empty;
        var result = await _mediator.Send(new GetParentDashboardQuery(tenantId, userId, childStudentId), ct);
        return Ok(result);
    }
}
