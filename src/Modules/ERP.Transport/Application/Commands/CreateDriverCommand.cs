using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Transport.Domain;
using ERP.Transport.Infrastructure;
using MediatR;

namespace ERP.Transport.Application.Commands;

public record CreateDriverCommand(
    string Name,
    string LicenseNumber,
    DateOnly LicenseExpiryDate,
    string MobileNumber
) : IRequest<Result<Guid>>;

public class CreateDriverCommandHandler : IRequestHandler<CreateDriverCommand, Result<Guid>>
{
    private readonly ITransportDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public CreateDriverCommandHandler(ITransportDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(CreateDriverCommand request, CancellationToken cancellationToken)
    {
        var driver = new Driver
        {
            TenantId = _currentTenant.TenantId ?? Guid.Empty,
            Name = request.Name,
            LicenseNumber = request.LicenseNumber,
            LicenseExpiryDate = request.LicenseExpiryDate,
            MobileNumber = request.MobileNumber
        };

        _db.Drivers.Add(driver);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(driver.Id);
    }
}
