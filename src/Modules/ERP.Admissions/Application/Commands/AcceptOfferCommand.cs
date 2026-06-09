using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Admissions.Application.Commands;

public record AcceptOfferCommand(Guid ApplicationId) : IRequest<Result>;
