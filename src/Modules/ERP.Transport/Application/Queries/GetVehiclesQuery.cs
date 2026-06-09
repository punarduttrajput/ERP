using ERP.Shared.Application.Common;
using ERP.Transport.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Transport.Application.Queries;

public record VehicleDto(
    Guid Id,
    string RegistrationNumber,
    string Make,
    string Model,
    int Capacity,
    DateOnly FitnessExpiryDate,
    DateOnly InsuranceExpiryDate,
    DateOnly PollutionExpiryDate,
    bool IsActive);

public record DriverDto(
    Guid Id,
    string Name,
    string LicenseNumber,
    DateOnly LicenseExpiryDate,
    string MobileNumber,
    bool IsActive);

public record GetVehiclesQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<VehicleDto>>;

public record GetDriversQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<DriverDto>>;

public class GetVehiclesQueryHandler : IRequestHandler<GetVehiclesQuery, PagedResult<VehicleDto>>
{
    private readonly ITransportDbContext _db;

    public GetVehiclesQueryHandler(ITransportDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<VehicleDto>> Handle(GetVehiclesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Vehicles.Where(v => !v.IsDeleted);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(v => v.RegistrationNumber)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(v => new VehicleDto(v.Id, v.RegistrationNumber, v.Make, v.Model, v.Capacity,
                v.FitnessExpiryDate, v.InsuranceExpiryDate, v.PollutionExpiryDate, v.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<VehicleDto>(items, total, request.Page, request.PageSize);
    }
}

public class GetDriversQueryHandler : IRequestHandler<GetDriversQuery, PagedResult<DriverDto>>
{
    private readonly ITransportDbContext _db;

    public GetDriversQueryHandler(ITransportDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<DriverDto>> Handle(GetDriversQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Drivers.Where(d => !d.IsDeleted);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(d => d.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DriverDto(d.Id, d.Name, d.LicenseNumber, d.LicenseExpiryDate, d.MobileNumber, d.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<DriverDto>(items, total, request.Page, request.PageSize);
    }
}
