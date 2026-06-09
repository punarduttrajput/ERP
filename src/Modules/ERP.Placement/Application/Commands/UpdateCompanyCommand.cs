using ERP.Placement.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Placement.Application.Commands;

public record UpdateCompanyCommand(
    Guid Id,
    string Name,
    string Industry,
    string? Website,
    string? Description,
    string? LogoUrl,
    string? ContactPersonName,
    string? ContactEmail,
    string? ContactMobile,
    bool IsActive
) : IRequest<Result>;

public class UpdateCompanyHandler : IRequestHandler<UpdateCompanyCommand, Result>
{
    private readonly IPlacementDbContext _db;

    public UpdateCompanyHandler(IPlacementDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (company is null)
            return Result.Failure("Company not found.");

        company.Name = request.Name;
        company.Industry = request.Industry;
        company.Website = request.Website;
        company.Description = request.Description;
        company.LogoUrl = request.LogoUrl;
        company.ContactPersonName = request.ContactPersonName;
        company.ContactEmail = request.ContactEmail;
        company.ContactMobile = request.ContactMobile;
        company.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
