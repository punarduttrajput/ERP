using ERP.MobileApi.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.MobileApi.API.v1;

[ApiController]
[Route("api/mobile/v1/student")]
[Authorize]
public class StudentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public StudentController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var userId = _currentUser.UserId ?? Guid.Empty;
        var result = await _mediator.Send(new GetStudentDashboardQuery(tenantId, userId, DateTime.UtcNow), ct);
        return Ok(result);
    }

    [HttpGet("timetable")]
    public async Task<IActionResult> GetTimetable(
        [FromQuery] Guid semesterId,
        [FromQuery] Guid batchId,
        CancellationToken ct)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var userId = _currentUser.UserId ?? Guid.Empty;
        var result = await _mediator.Send(new GetStudentTimetableQuery(tenantId, userId, semesterId, batchId), ct);
        return Ok(result);
    }

    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendance(CancellationToken ct)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var userId = _currentUser.UserId ?? Guid.Empty;
        var result = await _mediator.Send(new GetStudentAttendanceQuery(tenantId, userId), ct);
        return Ok(result);
    }

    [HttpGet("results")]
    public async Task<IActionResult> GetResults(CancellationToken ct)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var userId = _currentUser.UserId ?? Guid.Empty;
        var result = await _mediator.Send(new GetStudentResultsQuery(tenantId, userId), ct);
        return Ok(result);
    }

    [HttpGet("fees")]
    public async Task<IActionResult> GetFees(CancellationToken ct)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var userId = _currentUser.UserId ?? Guid.Empty;
        var result = await _mediator.Send(new GetStudentFeesQuery(tenantId, userId), ct);
        return Ok(result);
    }
}
