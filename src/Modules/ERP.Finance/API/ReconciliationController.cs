using ERP.Finance.Application.Commands;
using ERP.Finance.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Finance.API;

[ApiController]
[Route("api/finance/reconciliation")]
[Authorize]
public class ReconciliationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public ReconciliationController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    [HttpPost("statements")]
    public async Task<IActionResult> ImportStatement([FromBody] ImportStatementRequest request, CancellationToken ct)
    {
        var lines = request.Lines
            .Select(l => new BankStatementLineInput(l.TransactionDate, l.Description, l.Debit, l.Credit, l.Balance))
            .ToList();

        var result = await _mediator.Send(new ImportBankStatementCommand(
            _currentTenant.TenantId!.Value,
            request.AccountId,
            request.BankName,
            request.AccountNumber,
            request.StatementDate,
            request.OpeningBalance,
            request.ClosingBalance,
            lines
        ), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { id = result.Value });
    }

    [HttpGet("statements/{id:guid}/unreconciled")]
    public async Task<IActionResult> GetUnreconciledLines(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUnreconciledLinesQuery(_currentTenant.TenantId!.Value, id), ct);
        return Ok(result.Value);
    }

    [HttpPost("reconcile")]
    public async Task<IActionResult> ReconcileLine([FromBody] ReconcileRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ReconcileLineCommand(
            _currentTenant.TenantId!.Value,
            request.BankStatementLineId,
            request.JournalLineId
        ), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok();
    }
}

public record ImportStatementRequest(
    Guid AccountId,
    string BankName,
    string AccountNumber,
    DateOnly StatementDate,
    decimal OpeningBalance,
    decimal ClosingBalance,
    List<StatementLineRequest> Lines
);

public record StatementLineRequest(DateOnly TransactionDate, string Description, decimal Debit, decimal Credit, decimal Balance);
public record ReconcileRequest(Guid BankStatementLineId, Guid JournalLineId);
