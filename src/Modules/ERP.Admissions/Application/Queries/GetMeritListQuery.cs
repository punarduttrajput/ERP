using ERP.Admissions.Domain;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Admissions.Application.Queries;

public record GetMeritListQuery(Guid ProgramId, int AcademicYear, string? Category = null)
    : IRequest<Result<IReadOnlyList<MeritListEntryDto>>>;

public record MeritListEntryDto(
    Guid ApplicationId,
    int Rank,
    string ApplicantName,
    string Category,
    decimal? Score,
    ApplicationState State
);
