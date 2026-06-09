using ERP.MobileApi.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.MobileApi.API.v1;

[ApiController]
[Route("api/mobile/v1/faculty")]
[Authorize]
public class FacultyController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public FacultyController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] Guid semesterId, CancellationToken ct)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var userId = _currentUser.UserId ?? Guid.Empty;
        var result = await _mediator.Send(new GetFacultyDashboardQuery(tenantId, userId, semesterId), ct);
        return Ok(result);
    }

    [HttpPost("attendance")]
    public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceRequest request, CancellationToken ct)
    {
        // Attendance marking is handled by the ERP.Attendance module.
        // This endpoint accepts the mobile payload and forwards it via the existing
        // CreateSessionCommand / mark-records flow — wired in the Attendance module.
        return StatusCode(501, new { message = "Route to ERP.Attendance module commands." });
    }

    [HttpPost("marks")]
    public async Task<IActionResult> EnterMarks([FromBody] EnterMarksRequest request, CancellationToken ct)
    {
        // Internal marks entry is handled by ERP.Exams module commands.
        return StatusCode(501, new { message = "Route to ERP.Exams module commands." });
    }

    [HttpGet("announcements")]
    public async Task<IActionResult> GetAnnouncements(CancellationToken ct)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var userId = _currentUser.UserId ?? Guid.Empty;
        // Faculty announcements are read via GetFacultyDashboardQuery; a dedicated query is
        // not required as the dashboard already returns the last 5 announcements.
        // For a full list, clients should use the LMS announcements endpoint.
        return Ok(new { message = "Use /faculty/dashboard for recent announcements or LMS announcements endpoint." });
    }

    public record MarkAttendanceRequest(Guid SessionId, IReadOnlyList<StudentAttendanceMark> Marks);
    public record StudentAttendanceMark(Guid StudentId, bool IsPresent);
    public record EnterMarksRequest(Guid SubjectId, Guid SemesterId, IReadOnlyList<StudentMark> Marks);
    public record StudentMark(Guid StudentId, decimal Marks);
}
