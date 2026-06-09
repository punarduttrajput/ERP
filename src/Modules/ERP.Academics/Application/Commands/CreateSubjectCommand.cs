using ERP.Academics.Domain;
using ERP.Academics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Application.Commands;

public record CreateSubjectCommand(
    Guid ProgramId,
    string Code,
    string Name,
    int Credits,
    int ContactHoursPerWeek,
    string SubjectType) : IRequest<Result<Guid>>;

public class CreateSubjectHandler : IRequestHandler<CreateSubjectCommand, Result<Guid>>
{
    private readonly IAcademicsDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public CreateSubjectHandler(IAcademicsDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(CreateSubjectCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;

        var programExists = await _db.AcademicPrograms
            .AnyAsync(x => x.Id == request.ProgramId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (!programExists)
            return Result.Failure<Guid>("Program not found.");

        var duplicate = await _db.Subjects
            .AnyAsync(x => x.TenantId == tenantId && x.Code == request.Code && !x.IsDeleted, cancellationToken);

        if (duplicate)
            return Result.Failure<Guid>($"Subject with code '{request.Code}' already exists.");

        var subject = new Subject
        {
            TenantId = tenantId,
            ProgramId = request.ProgramId,
            Code = request.Code,
            Name = request.Name,
            Credits = request.Credits,
            ContactHoursPerWeek = request.ContactHoursPerWeek,
            SubjectType = request.SubjectType
        };

        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(subject.Id);
    }
}
