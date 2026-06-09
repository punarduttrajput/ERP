using ERP.Shared.Application.Common;
using ERP.SIS.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.SIS.Application.Queries;

public class GetStudentDocumentsHandler : IRequestHandler<GetStudentDocumentsQuery, Result<List<StudentDocumentDto>>>
{
    private readonly ISisDbContext _db;

    public GetStudentDocumentsHandler(ISisDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<StudentDocumentDto>>> Handle(GetStudentDocumentsQuery request, CancellationToken cancellationToken)
    {
        var docs = await _db.StudentDocuments
            .Where(d => d.StudentId == request.StudentId)
            .Select(d => new StudentDocumentDto(
                d.Id, d.DocumentType, d.OriginalFileName, d.BlobUrl,
                d.IsEncrypted, d.FileSizeBytes, d.UploadedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(docs);
    }
}
