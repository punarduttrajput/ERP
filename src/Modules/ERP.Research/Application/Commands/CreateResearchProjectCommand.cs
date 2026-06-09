using ERP.Research.Domain;
using ERP.Research.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Research.Application.Commands;

public record CreateResearchProjectCommand(
    Guid TenantId,
    string Title,
    Guid PrincipalInvestigatorId,
    string PrincipalInvestigatorName,
    string FundingAgency,
    string? FundingScheme,
    decimal SanctionedAmount,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? SanctionNumber,
    string? Abstract,
    string? Domain) : IRequest<Result<Guid>>;

public class CreateResearchProjectHandler : IRequestHandler<CreateResearchProjectCommand, Result<Guid>>
{
    private readonly IResearchDbContext _db;

    public CreateResearchProjectHandler(IResearchDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateResearchProjectCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<Guid>.Failure("Title is required.");

        if (request.SanctionedAmount < 0)
            return Result<Guid>.Failure("Sanctioned amount cannot be negative.");

        if (request.EndDate.HasValue && request.EndDate.Value <= request.StartDate)
            return Result<Guid>.Failure("End date must be after start date.");

        var project = new ResearchProject
        {
            TenantId = request.TenantId,
            Title = request.Title,
            PrincipalInvestigatorId = request.PrincipalInvestigatorId,
            PrincipalInvestigatorName = request.PrincipalInvestigatorName,
            FundingAgency = request.FundingAgency,
            FundingScheme = request.FundingScheme,
            SanctionedAmount = request.SanctionedAmount,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = ProjectStatus.Proposed,
            SanctionNumber = request.SanctionNumber,
            Abstract = request.Abstract,
            Domain = request.Domain
        };

        await _db.ResearchProjects.AddAsync(project, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(project.Id);
    }
}
