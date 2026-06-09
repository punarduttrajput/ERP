using ERP.HRMS.Domain;
using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.HRMS.Application.Commands;

public record SalaryComponentInput(
    string Name,
    ComponentType ComponentType,
    bool IsPercentage,
    decimal? Amount,
    decimal? Percentage,
    string? BaseComponent,
    bool IsStatutory
);

public record CreateSalaryStructureCommand(
    Guid TenantId,
    string Name,
    DateOnly EffectiveFrom,
    IReadOnlyList<SalaryComponentInput> Components
) : IRequest<Result<Guid>>;

public class CreateSalaryStructureHandler : IRequestHandler<CreateSalaryStructureCommand, Result<Guid>>
{
    private readonly IHrmsDbContext _db;

    public CreateSalaryStructureHandler(IHrmsDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateSalaryStructureCommand request, CancellationToken cancellationToken)
    {
        var structure = new SalaryStructure
        {
            TenantId = request.TenantId,
            Name = request.Name,
            EffectiveFrom = request.EffectiveFrom,
            IsActive = true
        };

        foreach (var c in request.Components)
        {
            structure.Components.Add(new SalaryComponent
            {
                TenantId = request.TenantId,
                SalaryStructureId = structure.Id,
                Name = c.Name,
                ComponentType = c.ComponentType,
                IsPercentage = c.IsPercentage,
                Amount = c.Amount,
                Percentage = c.Percentage,
                BaseComponent = c.BaseComponent,
                IsStatutory = c.IsStatutory
            });
        }

        _db.SalaryStructures.Add(structure);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(structure.Id);
    }
}
