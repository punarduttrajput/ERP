using ERP.Exams.Domain;
using ERP.Exams.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Exams.Application.Commands;

public record RegisterArrearCommand(
    Guid TenantId,
    Guid StudentId,
    Guid SubjectId,
    Guid SemesterId,
    Guid ExamSemesterId) : IRequest<Result<Guid>>;

public class RegisterArrearHandler : IRequestHandler<RegisterArrearCommand, Result<Guid>>
{
    private readonly IExamsDbContext _db;

    public RegisterArrearHandler(IExamsDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(RegisterArrearCommand request, CancellationToken cancellationToken)
    {
        // Verify the student has a Fail/Arrear result for this subject in the original semester
        var result = await _db.StudentResults
            .FirstOrDefaultAsync(r =>
                r.StudentId == request.StudentId &&
                r.SubjectId == request.SubjectId &&
                r.SemesterId == request.SemesterId &&
                r.IsPublished &&
                (r.Status == ResultStatus.Fail || r.Status == ResultStatus.Arrear),
                cancellationToken);

        if (result is null)
            return Result<Guid>.Failure("No published Fail or Arrear result found for this student and subject in the specified semester.");

        var registration = new ArrearRegistration
        {
            TenantId = request.TenantId,
            StudentId = request.StudentId,
            SubjectId = request.SubjectId,
            SemesterId = request.SemesterId,
            ExamSemesterId = request.ExamSemesterId,
            RegisteredAt = DateTime.UtcNow,
            IsApproved = false
        };

        _db.ArrearRegistrations.Add(registration);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(registration.Id);
    }
}
