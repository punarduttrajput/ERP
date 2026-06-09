using ERP.Exams.Domain;
using ERP.Exams.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Exams.Application.Commands;

public record GradeRuleDto(
    decimal MinMarks,
    decimal MaxMarks,
    string GradeLetter,
    decimal GradePoints);

public record CreateGradingSchemeCommand(
    Guid TenantId,
    string Name,
    bool IsDefault,
    IReadOnlyList<GradeRuleDto> Rules) : IRequest<Result<Guid>>;

public class CreateGradingSchemeHandler : IRequestHandler<CreateGradingSchemeCommand, Result<Guid>>
{
    private readonly IExamsDbContext _db;

    public CreateGradingSchemeHandler(IExamsDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateGradingSchemeCommand request, CancellationToken cancellationToken)
    {
        // If this is default, unset any existing default for the tenant
        if (request.IsDefault)
        {
            var existing = await _db.GradingSchemes
                .Where(g => g.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var g in existing)
                g.IsDefault = false;
        }

        var scheme = new GradingScheme
        {
            TenantId = request.TenantId,
            Name = request.Name,
            IsDefault = request.IsDefault
        };

        foreach (var rule in request.Rules)
        {
            scheme.GradeRules.Add(new GradeRule
            {
                TenantId = request.TenantId,
                GradingSchemeId = scheme.Id,
                MinMarks = rule.MinMarks,
                MaxMarks = rule.MaxMarks,
                GradeLetter = rule.GradeLetter,
                GradePoints = rule.GradePoints
            });
        }

        _db.GradingSchemes.Add(scheme);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(scheme.Id);
    }
}
