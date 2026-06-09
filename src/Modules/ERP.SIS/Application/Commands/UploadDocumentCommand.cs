using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.SIS.Application.Commands;

public record UploadDocumentCommand(
    Guid StudentId,
    string DocumentType,
    string OriginalFileName,
    byte[] FileContent,
    string ContentType
) : IRequest<Result<Guid>>;
