using ERP.ABC.Domain;
using ERP.ABC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.ABC.Application.Commands;

public record RequestCreditTransferCommand(
    Guid TenantId,
    Guid StudentId,
    string AbcId,
    TransferDirection Direction,
    string SourceInstitution,
    string? DestinationInstitution,
    string SubjectCode,
    string SubjectName,
    int CreditsRequested,
    int AcademicYear) : IRequest<Result<Guid>>;

public class RequestCreditTransferHandler : IRequestHandler<RequestCreditTransferCommand, Result<Guid>>
{
    private readonly IAbcDbContext _db;

    public RequestCreditTransferHandler(IAbcDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(RequestCreditTransferCommand request, CancellationToken cancellationToken)
    {
        var profileExists = await _db.StudentAbcProfiles
            .IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == request.TenantId && x.StudentId == request.StudentId && x.IsVerified && !x.IsDeleted, cancellationToken);

        if (!profileExists)
            return Result<Guid>.Failure("Student does not have a verified ABC profile. Link and verify ABC ID first.");

        var transfer = new CreditTransfer
        {
            TenantId = request.TenantId,
            StudentId = request.StudentId,
            AbcId = request.AbcId,
            Direction = request.Direction,
            SourceInstitution = request.SourceInstitution,
            DestinationInstitution = request.DestinationInstitution,
            SubjectCode = request.SubjectCode,
            SubjectName = request.SubjectName,
            CreditsRequested = request.CreditsRequested,
            AcademicYear = request.AcademicYear,
            Status = TransferStatus.Pending
        };

        await _db.CreditTransfers.AddAsync(transfer, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(transfer.Id);
    }
}
