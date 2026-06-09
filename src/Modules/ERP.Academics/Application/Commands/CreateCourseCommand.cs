using ERP.Academics.Domain;
using ERP.Academics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Application.Commands;

public record CreateCourseCommand(Guid ProgramId, string Code, string Name) : IRequest<Result<Guid>>;

public class CreateCourseHandler : IRequestHandler<CreateCourseCommand, Result<Guid>>
{
    private readonly IAcademicsDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public CreateCourseHandler(IAcademicsDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;

        var programExists = await _db.AcademicPrograms
            .AnyAsync(x => x.Id == request.ProgramId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (!programExists)
            return Result.Failure<Guid>("Program not found.");

        var course = new Course
        {
            TenantId = tenantId,
            ProgramId = request.ProgramId,
            Code = request.Code,
            Name = request.Name
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(course.Id);
    }
}
