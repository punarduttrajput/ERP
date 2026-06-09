using ERP.LMS.Domain;
using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.LMS.Application.Commands;

public record CreateAssignmentCommand(
    Guid TenantId,
    Guid SubjectId,
    Guid BatchId,
    string Title,
    string Description,
    DateTime DueDate,
    decimal MaxMarks,
    Guid CreatedBy) : IRequest<Result<Guid>>;

public class CreateAssignmentHandler : IRequestHandler<CreateAssignmentCommand, Result<Guid>>
{
    private readonly ILmsDbContext _db;

    public CreateAssignmentHandler(ILmsDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateAssignmentCommand cmd, CancellationToken ct)
    {
        var assignment = new Assignment
        {
            TenantId    = cmd.TenantId,
            SubjectId   = cmd.SubjectId,
            BatchId     = cmd.BatchId,
            Title       = cmd.Title,
            Description = cmd.Description,
            DueDate     = cmd.DueDate,
            MaxMarks    = cmd.MaxMarks,
            IsVisible              = true,
            AssignmentCreatedBy    = cmd.CreatedBy
        };

        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync(ct);
        return Result.Success(assignment.Id);
    }
}
