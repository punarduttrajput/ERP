using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Admissions.Application.Commands;

public record VerifyDocumentsCommand(
    Guid ApplicationId,
    bool Approved,
    string? RejectionReason
) : IRequest<Result>;
