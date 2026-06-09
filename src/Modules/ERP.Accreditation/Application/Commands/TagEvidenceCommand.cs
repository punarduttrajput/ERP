using System.Text.RegularExpressions;
using ERP.Accreditation.Domain;
using ERP.Accreditation.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Accreditation.Application.Commands;

public record TagEvidenceCommand(
    Guid TenantId,
    string ModuleName,
    string RecordId,
    string RecordLabel,
    string NaacCriterion,
    string NaacIndicator,
    string? Notes,
    Guid TaggedBy
) : IRequest<Result<Guid>>;

public class TagEvidenceHandler : IRequestHandler<TagEvidenceCommand, Result<Guid>>
{
    private static readonly Regex CriterionRegex = new(@"^[1-7]\.[1-9]$", RegexOptions.Compiled);

    private readonly IAccreditationDbContext _db;

    public TagEvidenceHandler(IAccreditationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(TagEvidenceCommand request, CancellationToken cancellationToken)
    {
        if (!CriterionRegex.IsMatch(request.NaacCriterion))
            return Result.Failure<Guid>($"Invalid NAAC criterion format '{request.NaacCriterion}'. Expected pattern: [1-7].[1-9]");

        var tag = new EvidenceTag
        {
            TenantId = request.TenantId,
            ModuleName = request.ModuleName,
            RecordId = request.RecordId,
            RecordLabel = request.RecordLabel,
            NaacCriterion = request.NaacCriterion,
            NaacIndicator = request.NaacIndicator,
            Notes = request.Notes,
            TaggedBy = request.TaggedBy
        };

        _db.EvidenceTags.Add(tag);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(tag.Id);
    }
}
