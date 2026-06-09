using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.SIS.Domain;
using ERP.SIS.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.SIS.Application.Commands;

public class UploadDocumentHandler : IRequestHandler<UploadDocumentCommand, Result<Guid>>
{
    private readonly ISisDbContext _db;
    private readonly IEncryptionService _encryption;
    private readonly IAzureBlobService _blob;

    public UploadDocumentHandler(ISisDbContext db, IEncryptionService encryption, IAzureBlobService blob)
    {
        _db = db;
        _encryption = encryption;
        _blob = blob;
    }

    public async Task<Result<Guid>> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId, cancellationToken);
        if (student is null)
            return Result.Failure<Guid>("Student not found.");

        var encryptedBytes = _encryption.EncryptBytes(request.FileContent);
        var blobName = $"students/{request.StudentId}/{Guid.NewGuid()}/{request.OriginalFileName}";
        var blobUrl = await _blob.UploadAsync("student-documents", blobName, encryptedBytes, request.ContentType, cancellationToken);

        var doc = new StudentDocument
        {
            TenantId = student.TenantId,
            StudentId = student.Id,
            DocumentType = request.DocumentType,
            OriginalFileName = request.OriginalFileName,
            BlobUrl = blobUrl,
            IsEncrypted = true,
            FileSizeBytes = request.FileContent.LongLength,
            UploadedAt = DateTime.UtcNow
        };

        _db.StudentDocuments.Add(doc);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(doc.Id);
    }
}
