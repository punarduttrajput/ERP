using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.SIS.Application.Commands;

public record GenerateIdCardCommand(Guid StudentId) : IRequest<Result<byte[]>>;
