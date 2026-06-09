using ERP.Research.Domain;
using ERP.Research.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Research.Application.Commands;

public record AddProjectMemberCommand(
    Guid TenantId,
    Guid ProjectId,
    Guid UserId,
    string MemberName,
    MemberRole Role,
    DateOnly JoinedAt) : IRequest<Result<Guid>>;

public class AddProjectMemberHandler : IRequestHandler<AddProjectMemberCommand, Result<Guid>>
{
    private readonly IResearchDbContext _db;

    public AddProjectMemberHandler(IResearchDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var projectExists = await _db.ResearchProjects
            .IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == request.TenantId && x.Id == request.ProjectId && !x.IsDeleted, cancellationToken);

        if (!projectExists)
            return Result<Guid>.Failure("Research project not found.");

        var duplicate = await _db.ProjectMembers
            .IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == request.TenantId && x.ProjectId == request.ProjectId && x.UserId == request.UserId && !x.IsDeleted, cancellationToken);

        if (duplicate)
            return Result<Guid>.Failure("This user is already a member of the project.");

        var member = new ProjectMember
        {
            TenantId = request.TenantId,
            ProjectId = request.ProjectId,
            UserId = request.UserId,
            MemberName = request.MemberName,
            Role = request.Role,
            JoinedAt = request.JoinedAt
        };

        await _db.ProjectMembers.AddAsync(member, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(member.Id);
    }
}
