using ERP.Fees.Domain;
using ERP.Fees.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Fees.Application.Commands;

public record CreateInstallmentScheduleCommand(
    Guid TenantId,
    Guid FeeStructureId,
    IReadOnlyList<InstallmentDto> Installments
) : IRequest<Result>;

public record InstallmentDto(int InstallmentNumber, DateOnly DueDate, decimal Amount, decimal LateFinePerDay, decimal MaxLateFine);

public class CreateInstallmentScheduleHandler : IRequestHandler<CreateInstallmentScheduleCommand, Result>
{
    private readonly IFeesDbContext _db;

    public CreateInstallmentScheduleHandler(IFeesDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(CreateInstallmentScheduleCommand request, CancellationToken cancellationToken)
    {
        var structureExists = await _db.FeeStructures.AnyAsync(x => x.Id == request.FeeStructureId, cancellationToken);
        if (!structureExists)
            return Result.Failure("Fee structure not found.");

        var schedules = request.Installments.Select(i => new InstallmentSchedule
        {
            TenantId = request.TenantId,
            FeeStructureId = request.FeeStructureId,
            InstallmentNumber = i.InstallmentNumber,
            DueDate = i.DueDate,
            Amount = i.Amount,
            LateFinePerDay = i.LateFinePerDay,
            MaxLateFine = i.MaxLateFine
        }).ToList();

        foreach (var s in schedules)
            _db.InstallmentSchedules.Add(s);

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
