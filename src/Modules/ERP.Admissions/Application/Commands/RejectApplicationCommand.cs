using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Admissions.Application.Commands;

public record RejectApplicationCommand(Guid ApplicationId, string Reason) : IRequest<Result>;
