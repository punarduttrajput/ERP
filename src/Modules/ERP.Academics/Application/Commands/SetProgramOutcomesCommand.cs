using ERP.Academics.Domain;
using ERP.Academics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Application.Commands;

public record SetProgramOutcomesCommand(
    Guid ProgramId,
    IReadOnlyList<OutcomeItem> POs,
    IReadOnlyList<OutcomeItem> PSOs) : IRequest<Result>;

public class SetProgramOutcomesHandler : IRequestHandler<SetProgramOutcomesCommand, Result>
{
    private readonly IAcademicsDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public SetProgramOutcomesHandler(IAcademicsDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result> Handle(SetProgramOutcomesCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;

        var programExists = await _db.AcademicPrograms
            .AnyAsync(x => x.Id == request.ProgramId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (!programExists)
            return Result.Failure("Program not found.");

        var existingPos = await _db.ProgramOutcomes
            .Where(x => x.ProgramId == request.ProgramId && x.TenantId == tenantId && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var existingPsos = await _db.ProgramSpecificOutcomes
            .Where(x => x.ProgramId == request.ProgramId && x.TenantId == tenantId && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        _db.ProgramOutcomes.RemoveRange(existingPos);
        _db.ProgramSpecificOutcomes.RemoveRange(existingPsos);

        foreach (var po in request.POs)
        {
            _db.ProgramOutcomes.Add(new ProgramOutcome
            {
                TenantId = tenantId,
                ProgramId = request.ProgramId,
                Code = po.Code,
                Description = po.Description
            });
        }

        foreach (var pso in request.PSOs)
        {
            _db.ProgramSpecificOutcomes.Add(new ProgramSpecificOutcome
            {
                TenantId = tenantId,
                ProgramId = request.ProgramId,
                Code = pso.Code,
                Description = pso.Description
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
