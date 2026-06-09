using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Admissions.Application.Commands;

public record MakeOfferCommand(Guid ProgramId, int AcademicYear) : IRequest<Result<int>>;
