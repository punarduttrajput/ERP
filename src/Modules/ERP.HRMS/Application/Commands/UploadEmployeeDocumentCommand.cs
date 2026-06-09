using ERP.HRMS.Domain;
using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Commands;

public record UploadEmployeeDocumentCommand(
    Guid EmployeeId,
    Guid TenantId,
    string DocumentType,
    string FileName,
    byte[] Content,
    string ContentType
) : IRequest<Result<Guid>>;

public class UploadEmployeeDocumentHandler : IRequestHandler<UploadEmployeeDocumentCommand, Result<Guid>>
{
    private readonly IHrmsDbContext _db;
    private readonly IAzureBlobService _blob;

    public UploadEmployeeDocumentHandler(IHrmsDbContext db, IAzureBlobService blob)
    {
        _db = db;
        _blob = blob;
    }

    public async Task<Result<Guid>> Handle(UploadEmployeeDocumentCommand request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.TenantId == request.TenantId, cancellationToken);

        if (employee is null)
            return Result.Failure<Guid>("Employee not found.");

        var blobName = $"hrms/{request.TenantId}/{request.EmployeeId}/{Guid.NewGuid()}_{request.FileName}";
        var url = await _blob.UploadAsync("employee-documents", blobName, request.Content, request.ContentType, cancellationToken);

        var doc = new EmployeeDocument
        {
            TenantId = request.TenantId,
            EmployeeId = request.EmployeeId,
            DocumentType = request.DocumentType,
            FileName = request.FileName,
            BlobUrl = url
        };

        _db.EmployeeDocuments.Add(doc);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(doc.Id);
    }
}
