using ERP.Finance.Application.Commands;
using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Commands;

public record PostPayrollToGlCommand(Guid PayrollRunId, Guid TenantId) : IRequest<Result>;

public class PostPayrollToGlHandler : IRequestHandler<PostPayrollToGlCommand, Result>
{
    private readonly IHrmsDbContext _db;
    private readonly IMediator _mediator;
    private readonly ERP.Finance.Infrastructure.IFinanceDbContext _financeDb;

    public PostPayrollToGlHandler(
        IHrmsDbContext db,
        IMediator mediator,
        ERP.Finance.Infrastructure.IFinanceDbContext financeDb)
    {
        _db = db;
        _mediator = mediator;
        _financeDb = financeDb;
    }

    public async Task<Result> Handle(PostPayrollToGlCommand request, CancellationToken cancellationToken)
    {
        var run = await _db.PayrollRuns
            .Include(r => r.Entries)
            .FirstOrDefaultAsync(r => r.Id == request.PayrollRunId && r.TenantId == request.TenantId, cancellationToken);

        if (run is null)
            return Result.Failure("Payroll run not found.");

        if (run.IsPostedToGl)
            return Result.Failure("Payroll has already been posted to GL.");

        var totalPfEmployee = run.Entries.Sum(e => e.PfEmployee);
        var totalPfEmployer = run.Entries.Sum(e => e.PfEmployer);
        var totalEsiEmployee = run.Entries.Sum(e => e.EsiEmployee ?? 0m);
        var totalEsiEmployer = run.Entries.Sum(e => e.EsiEmployer ?? 0m);
        var totalTds = run.Entries.Sum(e => e.TdsAmount);
        var totalNetPay = run.TotalNetPay;
        var totalGrossWithEmployerContrib = run.TotalGrossPay + totalPfEmployer + totalEsiEmployer;

        // Resolve GL account IDs by code
        var accountCodes = new[] { "5000", "2200", "2210", "2220", "2100" };
        var accounts = await _financeDb.GlAccounts
            .Where(a => a.TenantId == request.TenantId && accountCodes.Contains(a.Code))
            .ToDictionaryAsync(a => a.Code, cancellationToken);

        foreach (var code in accountCodes)
        {
            if (!accounts.ContainsKey(code))
                return Result.Failure($"GL account with code '{code}' not found. Ensure chart of accounts is seeded.");
        }

        var lines = new List<JournalLineInput>
        {
            new(accounts["5000"].Id, totalGrossWithEmployerContrib, 0m, "Salary Expense"),
            new(accounts["2200"].Id, 0m, totalPfEmployee + totalPfEmployer, "PF Payable"),
            new(accounts["2210"].Id, 0m, totalEsiEmployee + totalEsiEmployer, "ESI Payable"),
            new(accounts["2220"].Id, 0m, totalTds, "TDS Payable"),
            new(accounts["2100"].Id, 0m, totalNetPay, "Net Salary Payable")
        };

        var entryDate = new DateOnly(run.Year, run.Month, DateTime.DaysInMonth(run.Year, run.Month));

        var createResult = await _mediator.Send(new CreateJournalEntryCommand(
            request.TenantId,
            entryDate,
            $"Payroll for {run.Month}/{run.Year}",
            $"PAYROLL-{run.Year}-{run.Month:D2}",
            lines
        ), cancellationToken);

        if (!createResult.IsSuccess)
            return Result.Failure($"Failed to create journal entry: {createResult.Error}");

        var postResult = await _mediator.Send(new PostJournalEntryCommand(
            request.TenantId,
            createResult.Value,
            null
        ), cancellationToken);

        if (!postResult.IsSuccess)
            return Result.Failure($"Failed to post journal entry: {postResult.Error}");

        run.IsPostedToGl = true;
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
