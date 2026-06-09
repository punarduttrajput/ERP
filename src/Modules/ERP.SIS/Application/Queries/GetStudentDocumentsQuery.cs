using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.SIS.Application.Queries;

public record GetStudentDocumentsQuery(Guid StudentId) : IRequest<Result<List<StudentDocumentDto>>>;

public record StudentDocumentDto(
    Guid Id,
    string DocumentType,
    string OriginalFileName,
    string BlobUrl,
    bool IsEncrypted,
    long FileSizeBytes,
    DateTime UploadedAt
);
