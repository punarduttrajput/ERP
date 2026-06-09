using ERP.Placement.Domain;
using ERP.Placement.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Placement.Application.Commands;

public record CreateCompanyCommand(
    Guid TenantId,
    string Name,
    string Industry,
    string? Website,
    string? Description,
    string? LogoUrl,
    string? ContactPersonName,
    string? ContactEmail,
    string? ContactMobile
) : IRequest<Result<Guid>>;

public class CreateCompanyHandler : IRequestHandler<CreateCompanyCommand, Result<Guid>>
{
    private readonly IPlacementDbContext _db;

    public CreateCompanyHandler(IPlacementDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = new Company
        {
            TenantId = request.TenantId,
            Name = request.Name,
            Industry = request.Industry,
            Website = request.Website,
            Description = request.Description,
            LogoUrl = request.LogoUrl,
            ContactPersonName = request.ContactPersonName,
            ContactEmail = request.ContactEmail,
            ContactMobile = request.ContactMobile
        };

        _db.Companies.Add(company);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(company.Id);
    }
}
