using ERP.Shared.Application.Common;
using ERP.SIS.Domain;
using MediatR;

namespace ERP.SIS.Application.Commands;

public record GenerateCertificateCommand(
    Guid StudentId,
    CertificateType CertificateType
) : IRequest<Result<byte[]>>;
