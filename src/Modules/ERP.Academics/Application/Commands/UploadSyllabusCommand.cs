using ERP.Academics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Application.Commands;

public record UploadSyllabusCommand(Guid SubjectId, byte[] FileBytes, string FileName) : IRequest<Result<string>>;

public class UploadSyllabusHandler : IRequestHandler<UploadSyllabusCommand, Result<string>>
{
    private readonly IAcademicsDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly IAzureBlobService _blobService;

    public UploadSyllabusHandler(IAcademicsDbContext db, ICurrentTenant currentTenant, IAzureBlobService blobService)
    {
        _db = db;
        _currentTenant = currentTenant;
        _blobService = blobService;
    }

    public async Task<Result<string>> Handle(UploadSyllabusCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;

        var subject = await _db.Subjects
            .FirstOrDefaultAsync(x => x.Id == request.SubjectId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (subject is null)
            return Result.Failure<string>("Subject not found.");

        var blobName = $"{tenantId}/{request.SubjectId}/{request.FileName}";
        var url = await _blobService.UploadAsync("syllabi", blobName, request.FileBytes, "application/pdf", cancellationToken);

        subject.SyllabusUrl = url;
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(url);
    }
}
