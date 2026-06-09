using ERP.Research.Domain;
using ERP.Research.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Research.Application.Commands;

public record CreateGrantCommand(
    Guid TenantId,
    string Title,
    string FundingAgency,
    string? GrantNumber,
    decimal SanctionedAmount,
    DateOnly? StartDate,
    DateOnly? EndDate,
    Guid PrincipalInvestigatorId,
    Guid? ResearchProjectId) : IRequest<Result<Guid>>;

public class CreateGrantHandler : IRequestHandler<CreateGrantCommand, Result<Guid>>
{
    private readonly IResearchDbContext _db;

    public CreateGrantHandler(IResearchDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateGrantCommand request, CancellationToken cancellationToken)
    {
        if (request.SanctionedAmount <= 0)
            return Result<Guid>.Failure("Sanctioned amount must be positive.");

        var grant = new Grant
        {
            TenantId = request.TenantId,
            Title = request.Title,
            FundingAgency = request.FundingAgency,
            GrantNumber = request.GrantNumber,
            SanctionedAmount = request.SanctionedAmount,
            DisbursedAmount = 0,
            UtilizedAmount = 0,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = GrantStatus.Proposal,
            PrincipalInvestigatorId = request.PrincipalInvestigatorId,
            ResearchProjectId = request.ResearchProjectId
        };

        await _db.Grants.AddAsync(grant, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(grant.Id);
    }
}
