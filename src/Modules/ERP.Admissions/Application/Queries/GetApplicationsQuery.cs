using ERP.Admissions.Domain;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Admissions.Application.Queries;

public record GetApplicationsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? ProgramId = null,
    int? AcademicYear = null,
    ApplicationState? State = null
) : IRequest<Result<PagedResult<ApplicationSummaryDto>>>;

public record ApplicationSummaryDto(
    Guid Id,
    string ApplicantName,
    string ApplicantEmail,
    string ProgramName,
    string Category,
    int AcademicYear,
    ApplicationState State,
    decimal? MeritScore,
    int? MeritRank,
    DateTime CreatedAt
);
