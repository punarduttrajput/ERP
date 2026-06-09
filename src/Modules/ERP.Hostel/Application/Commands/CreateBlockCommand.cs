using ERP.Hostel.Domain;
using ERP.Hostel.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Hostel.Application.Commands;

public record CreateBlockCommand(
    string Name,
    string Gender
) : IRequest<Result<Guid>>;

public class CreateBlockCommandHandler : IRequestHandler<CreateBlockCommand, Result<Guid>>
{
    private readonly IHostelDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public CreateBlockCommandHandler(IHostelDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(CreateBlockCommand request, CancellationToken cancellationToken)
    {
        var block = new HostelBlock
        {
            TenantId = _currentTenant.TenantId ?? Guid.Empty,
            Name = request.Name,
            Gender = request.Gender
        };

        _db.HostelBlocks.Add(block);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(block.Id);
    }
}
