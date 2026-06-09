using ERP.Finance.Application.Commands;
using ERP.Finance.Domain;
using ERP.Finance.Infrastructure;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Finance.API;

[ApiController]
[Route("api/finance/journal")]
[Authorize]
public class JournalController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser? _currentUser;
    private readonly IFinanceDbContext _db;

    public JournalController(IMediator mediator, ICurrentTenant currentTenant, IFinanceDbContext db, ICurrentUser? currentUser = null)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> CreateEntry([FromBody] CreateJournalEntryRequest request, CancellationToken ct)
    {
        var lines = request.Lines
            .Select(l => new JournalLineInput(l.AccountId, l.Debit, l.Credit, l.Narration))
            .ToList();

        var result = await _mediator.Send(new CreateJournalEntryCommand(
            _currentTenant.TenantId!.Value,
            request.EntryDate,
            request.Description,
            request.Reference,
            lines
        ), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> ListEntries(
        [FromQuery] EntryStatus? status,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        CancellationToken ct)
    {
        var tenantId = _currentTenant.TenantId!.Value;
        var query = _db.JournalEntries
            .Where(e => e.TenantId == tenantId);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);
        if (dateFrom.HasValue)
            query = query.Where(e => e.EntryDate >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(e => e.EntryDate <= dateTo.Value);

        var entries = await query
            .OrderByDescending(e => e.EntryDate)
            .Select(e => new
            {
                e.Id,
                e.EntryNumber,
                e.EntryDate,
                e.Description,
                e.Reference,
                e.Status,
                e.TotalDebit,
                e.TotalCredit,
                e.PostedAt
            })
            .ToListAsync(ct);

        return Ok(entries);
    }

    [HttpPost("{id:guid}/post")]
    public async Task<IActionResult> PostEntry(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new PostJournalEntryCommand(
            _currentTenant.TenantId!.Value,
            id,
            _currentUser?.UserId
        ), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok();
    }

    [HttpPost("{id:guid}/reverse")]
    public async Task<IActionResult> ReverseEntry(Guid id, [FromBody] ReverseEntryRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ReverseJournalEntryCommand(
            _currentTenant.TenantId!.Value,
            id,
            request.ReverseDate,
            request.Reason,
            _currentUser?.UserId
        ), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { reversalEntryId = result.Value });
    }
}

public record CreateJournalEntryRequest(
    DateOnly EntryDate,
    string Description,
    string? Reference,
    List<JournalLineRequest> Lines
);

public record JournalLineRequest(Guid AccountId, decimal Debit, decimal Credit, string? Narration);
public record ReverseEntryRequest(DateOnly ReverseDate, string Reason);
