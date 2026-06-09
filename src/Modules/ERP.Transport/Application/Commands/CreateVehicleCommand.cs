using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Transport.Domain;
using ERP.Transport.Infrastructure;
using MediatR;

namespace ERP.Transport.Application.Commands;

public record CreateVehicleCommand(
    string RegistrationNumber,
    string Make,
    string Model,
    int Capacity,
    DateOnly FitnessExpiryDate,
    DateOnly InsuranceExpiryDate,
    DateOnly PollutionExpiryDate
) : IRequest<Result<Guid>>;

public class CreateVehicleCommandHandler : IRequestHandler<CreateVehicleCommand, Result<Guid>>
{
    private readonly ITransportDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public CreateVehicleCommandHandler(ITransportDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(CreateVehicleCommand request, CancellationToken cancellationToken)
    {
        var vehicle = new Vehicle
        {
            TenantId = _currentTenant.TenantId ?? Guid.Empty,
            RegistrationNumber = request.RegistrationNumber,
            Make = request.Make,
            Model = request.Model,
            Capacity = request.Capacity,
            FitnessExpiryDate = request.FitnessExpiryDate,
            InsuranceExpiryDate = request.InsuranceExpiryDate,
            PollutionExpiryDate = request.PollutionExpiryDate
        };

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(vehicle.Id);
    }
}
