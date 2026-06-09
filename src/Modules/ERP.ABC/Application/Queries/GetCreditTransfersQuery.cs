using ERP.ABC.Domain;
using ERP.ABC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.ABC.Application.Queries;

public record CreditTransferDto(
    Guid Id,
    Guid StudentId,
    string AbcId,
    TransferDirection Direction,
    string SourceInstitution,
    string? DestinationInstitution,
    string SubjectCode,
    string SubjectName,
    int CreditsRequested,
    int? CreditsApproved,
    int AcademicYear,
    TransferStatus Status,
    Guid? ApprovedBy,
    DateTime? ApprovedAt,
    string? RejectionReason,
    string? AbcRegistryReference,
    DateTime CreatedAt);

public record GetCreditTransfersQuery(
    Guid TenantId,
    Guid? StudentId,
    TransferStatus? Status,
    TransferDirection? Direction,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<CreditTransferDto>>;

public class GetCreditTransfersHandler : IRequestHandler<GetCreditTransfersQuery, PagedResult<CreditTransferDto>>
{
    private readonly IAbcDbContext _db;

    public GetCreditTransfersHandler(IAbcDbContext db) => _db = db;

    public async Task<PagedResult<CreditTransferDto>> Handle(GetCreditTransfersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.CreditTransfers
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted);

        if (request.StudentId.HasValue)
            query = query.Where(x => x.StudentId == request.StudentId.Value);
        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);
        if (request.Direction.HasValue)
            query = query.Where(x => x.Direction == request.Direction.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new CreditTransferDto(
                x.Id, x.StudentId, x.AbcId, x.Direction,
                x.SourceInstitution, x.DestinationInstitution,
                x.SubjectCode, x.SubjectName, x.CreditsRequested, x.CreditsApproved,
                x.AcademicYear, x.Status, x.ApprovedBy, x.ApprovedAt,
                x.RejectionReason, x.AbcRegistryReference, x.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<CreditTransferDto>(items, total, request.Page, request.PageSize);
    }
}
