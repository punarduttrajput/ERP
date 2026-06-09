using ERP.Research.Domain;
using ERP.Research.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Research.Application.Commands;

public record UpdateProjectStatusCommand(
    Guid TenantId,
    Guid ProjectId,
    ProjectStatus NewStatus,
    string? Notes) : IRequest<Result>;

public class UpdateProjectStatusHandler : IRequestHandler<UpdateProjectStatusCommand, Result>
{
    private readonly IResearchDbContext _db;

    // Valid transitions per spec
    private static readonly HashSet<(ProjectStatus From, ProjectStatus To)> _allowedTransitions = new()
    {
        (ProjectStatus.Proposed,  ProjectStatus.Approved),
        (ProjectStatus.Approved,  ProjectStatus.Active),
        (ProjectStatus.Active,    ProjectStatus.Completed),
        (ProjectStatus.Active,    ProjectStatus.Terminated),
        (ProjectStatus.Approved,  ProjectStatus.Terminated)
    };

    public UpdateProjectStatusHandler(IResearchDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateProjectStatusCommand request, CancellationToken cancellationToken)
    {
        var project = await _db.ResearchProjects
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == request.TenantId && x.Id == request.ProjectId && !x.IsDeleted, cancellationToken);

        if (project is null)
            return Result.Failure("Research project not found.");

        if (!_allowedTransitions.Contains((project.Status, request.NewStatus)))
            return Result.Failure($"Transition from {project.Status} to {request.NewStatus} is not allowed.");

        project.Status = request.NewStatus;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
