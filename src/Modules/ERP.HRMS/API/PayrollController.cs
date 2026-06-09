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
[Route("api/hrms/payroll")]
public class PayrollController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _tenant;
    private readonly ICurrentUser _user;

    public PayrollController(IMediator mediator, ICurrentTenant tenant, ICurrentUser user)
    {
        _mediator = mediator;
        _tenant = tenant;
        _user = user;
    }

    [HttpPost("structures")]
    public async Task<IActionResult> CreateStructure([FromBody] CreateStructureRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateSalaryStructureCommand(
            _tenant.TenantId!.Value, dto.Name, dto.EffectiveFrom,
            dto.Components.Select(c => new SalaryComponentInput(
                c.Name, c.ComponentType, c.IsPercentage,
                c.Amount, c.Percentage, c.BaseComponent, c.IsStatutory
            )).ToList()
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpPost("run")]
    public async Task<IActionResult> RunPayroll([FromBody] RunPayrollRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new RunPayrollCommand(
            _tenant.TenantId!.Value, dto.Month, dto.Year, _user.UserId!.Value
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpGet("runs/{id:guid}")]
    public async Task<IActionResult> GetRun(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPayrollRunQuery(id, _tenant.TenantId!.Value), ct);
        if (!result.IsSuccess) return NotFound(new { result.Error });
        return Ok(result.Value);
    }

    [HttpGet("entries/{entryId:guid}/payslip")]
    public async Task<IActionResult> GetPayslip(Guid entryId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GeneratePayslipCommand(entryId, _tenant.TenantId!.Value), ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return File(result.Value!, "application/pdf", $"payslip-{entryId}.pdf");
    }

    [HttpPost("runs/{id:guid}/post-to-gl")]
    public async Task<IActionResult> PostToGl(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new PostPayrollToGlCommand(id, _tenant.TenantId!.Value), ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }
}

public record CreateStructureRequest(
    string Name, DateOnly EffectiveFrom,
    IReadOnlyList<SalaryComponentRequest> Components
);

public record SalaryComponentRequest(
    string Name, ComponentType ComponentType, bool IsPercentage,
    decimal? Amount, decimal? Percentage, string? BaseComponent, bool IsStatutory
);

public record RunPayrollRequest(int Month, int Year);
