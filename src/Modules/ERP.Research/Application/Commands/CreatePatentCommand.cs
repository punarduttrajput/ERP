using ERP.Research.Domain;
using ERP.Research.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Research.Application.Commands;

public record CreatePatentCommand(
    Guid TenantId,
    string Title,
    string Inventors,
    string? ApplicationNumber,
    DateOnly? FilingDate,
    string PatentOffice,
    Guid? ResearchProjectId) : IRequest<Result<Guid>>;

public class CreatePatentHandler : IRequestHandler<CreatePatentCommand, Result<Guid>>
{
    private readonly IResearchDbContext _db;

    public CreatePatentHandler(IResearchDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreatePatentCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<Guid>.Failure("Title is required.");

        if (string.IsNullOrWhiteSpace(request.Inventors))
            return Result<Guid>.Failure("At least one inventor is required.");

        var patent = new Patent
        {
            TenantId = request.TenantId,
            Title = request.Title,
            Inventors = request.Inventors,
            ApplicationNumber = request.ApplicationNumber,
            FilingDate = request.FilingDate,
            Status = PatentStatus.Filed,
            PatentOffice = request.PatentOffice,
            ResearchProjectId = request.ResearchProjectId
        };

        await _db.Patents.AddAsync(patent, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(patent.Id);
    }
}
