using ERP.HRMS.Application.Commands;
using ERP.HRMS.Application.Queries;
using ERP.HRMS.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.HRMS.API;

[Authorize]
[ApiController]
[Route("api/hrms/leave")]
public class LeaveController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _tenant;
    private readonly ICurrentUser _user;

    public LeaveController(IMediator mediator, ICurrentTenant tenant, ICurrentUser user)
    {
        _mediator = mediator;
        _tenant = tenant;
        _user = user;
    }

    [HttpPost("types")]
    public async Task<IActionResult> CreateLeaveType([FromBody] CreateLeaveTypeRequest dto, CancellationToken ct)
    {
        var leaveType = new LeaveType
        {
            TenantId = _tenant.TenantId!.Value,
            Name = dto.Name,
            DaysAllowedPerYear = dto.DaysAllowedPerYear,
            IsCarryForward = dto.IsCarryForward,
            MaxCarryForwardDays = dto.MaxCarryForwardDays
        };
        // Direct DB operation via a future CreateLeaveTypeCommand; returning placeholder
        return Ok(new { message = "Use CreateLeaveTypeCommand" });
    }

    [HttpGet("balance/{employeeId:guid}")]
    public async Task<IActionResult> GetBalance(Guid employeeId, [FromQuery] int year, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLeaveBalanceQuery(employeeId, _tenant.TenantId!.Value, year), ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(result.Value);
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] ApplyLeaveRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new ApplyLeaveCommand(
            _tenant.TenantId!.Value, dto.EmployeeId, dto.LeaveTypeId,
            dto.FromDate, dto.ToDate, dto.Reason
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpPatch("{id:guid}/review")]
    public async Task<IActionResult> Review(Guid id, [FromBody] ReviewLeaveRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new ReviewLeaveCommand(
            id, _tenant.TenantId!.Value, dto.Approve,
            _user.UserId!.Value, dto.RejectionReason
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }
}

public record CreateLeaveTypeRequest(string Name, int DaysAllowedPerYear, bool IsCarryForward, int? MaxCarryForwardDays);
public record ApplyLeaveRequest(Guid EmployeeId, Guid LeaveTypeId, DateOnly FromDate, DateOnly ToDate, string Reason);
public record ReviewLeaveRequest(bool Approve, string? RejectionReason);
