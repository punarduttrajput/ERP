using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Admissions.Application.Commands;

public record SubmitApplicationCommand(
    string ApplicantName,
    string ApplicantEmail,
    string ApplicantMobile,
    Guid ProgramId,
    string ProgramName,
    string Category,
    int AcademicYear,
    IReadOnlyList<DocumentUpload> Documents
) : IRequest<Result<Guid>>;

public record DocumentUpload(string DocumentType, string BlobUrl, string FileName);
