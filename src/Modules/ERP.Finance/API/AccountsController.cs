using ERP.Finance.Application.Commands;
using ERP.Finance.Application.Queries;
using ERP.Finance.Domain;
using ERP.Finance.Infrastructure;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Finance.API;

[ApiController]
[Route("api/finance/accounts")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public AccountsController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateAccountCommand(
            _currentTenant.TenantId!.Value,
            request.Code,
            request.Name,
            request.AccountType,
            request.ParentAccountId,
            request.IsControl
        ), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> GetChartOfAccounts(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetChartOfAccountsQuery(_currentTenant.TenantId!.Value), ct);
        return Ok(result.Value);
    }

    [HttpPost("setup/default")]
    public async Task<IActionResult> SeedDefaultAccounts([FromServices] IFinanceDbContext db, CancellationToken ct)
    {
        await DefaultChartOfAccountsSeeder.SeedAsync(db, _currentTenant.TenantId!.Value, ct);
        return Ok(new { message = "Default chart of accounts seeded." });
    }
}

public record CreateAccountRequest(
    string Code,
    string Name,
    AccountType AccountType,
    Guid? ParentAccountId,
    bool IsControl
);
