using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Admissions.Application.Commands;

public record WithdrawApplicationCommand(Guid ApplicationId) : IRequest<Result>;
