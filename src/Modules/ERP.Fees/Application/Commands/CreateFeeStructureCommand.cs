using ERP.Fees.Domain;
using ERP.Fees.Infrastructure;
using ERP.Shared.Application.Common;
using ERP.Shared.Application.Abstractions;
using MediatR;

namespace ERP.Fees.Application.Commands;

public record CreateFeeStructureCommand(
    Guid TenantId,
    Guid ProgramId,
    string ProgramName,
    int SemesterNumber,
    string Category,
    int AcademicYear,
    IReadOnlyList<ComponentDto> Components
) : IRequest<Result<Guid>>;

public record ComponentDto(string Name, decimal Amount, bool IsRefundable);

public class CreateFeeStructureHandler : IRequestHandler<CreateFeeStructureCommand, Result<Guid>>
{
    private readonly IFeesDbContext _db;

    public CreateFeeStructureHandler(IFeesDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateFeeStructureCommand request, CancellationToken cancellationToken)
    {
        var components = request.Components.Select(c => new FeeComponent
        {
            TenantId = request.TenantId,
            Name = c.Name,
            Amount = c.Amount,
            IsRefundable = c.IsRefundable
        }).ToList();

        var structure = new FeeStructure
        {
            TenantId = request.TenantId,
            ProgramId = request.ProgramId,
            ProgramName = request.ProgramName,
            SemesterNumber = request.SemesterNumber,
            Category = request.Category,
            AcademicYear = request.AcademicYear,
            TotalAmount = components.Sum(c => c.Amount),
            Components = components
        };

        _db.FeeStructures.Add(structure);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(structure.Id);
    }
}
