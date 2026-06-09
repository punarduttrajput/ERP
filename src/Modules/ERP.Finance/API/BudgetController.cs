using ERP.Finance.Application.Commands;
using ERP.Finance.Application.Queries;
using ERP.Finance.Infrastructure;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Finance.API;

[ApiController]
[Route("api/finance/budgets")]
[Authorize]
public class BudgetController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly IFinanceDbContext _db;

    public BudgetController(IMediator mediator, ICurrentTenant currentTenant, IFinanceDbContext db)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBudget([FromBody] CreateBudgetRequest request, CancellationToken ct)
    {
        var lines = request.Lines
            .Select(l => new BudgetLineInput(l.AccountId, l.AccountName, l.AllocatedAmount))
            .ToList();

        var result = await _mediator.Send(new CreateBudgetCommand(
            _currentTenant.TenantId!.Value,
            request.DepartmentId,
            request.DepartmentName,
            request.AcademicYear,
            lines
        ), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> ListBudgets([FromQuery] Guid? departmentId, [FromQuery] int? academicYear, CancellationToken ct)
    {
        var tenantId = _currentTenant.TenantId!.Value;
        var query = _db.Budgets.Where(b => b.TenantId == tenantId);

        if (departmentId.HasValue)
            query = query.Where(b => b.DepartmentId == departmentId.Value);
        if (academicYear.HasValue)
            query = query.Where(b => b.AcademicYear == academicYear.Value);

        var budgets = await query
            .Select(b => new
            {
                b.Id,
                b.DepartmentId,
                b.DepartmentName,
                b.AcademicYear,
                b.TotalAllocated,
                b.TotalSpent,
                b.IsLocked
            })
            .ToListAsync(ct);

        return Ok(budgets);
    }

    [HttpGet("{id:guid}/utilization")]
    public async Task<IActionResult> GetUtilization(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBudgetUtilizationQuery(_currentTenant.TenantId!.Value, id), ct);

        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }
}

public record CreateBudgetRequest(
    Guid DepartmentId,
    string DepartmentName,
    int AcademicYear,
    List<BudgetLineRequest> Lines
);

public record BudgetLineRequest(Guid AccountId, string AccountName, decimal AllocatedAmount);
