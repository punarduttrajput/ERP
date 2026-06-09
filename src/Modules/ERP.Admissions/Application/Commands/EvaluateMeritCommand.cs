using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Admissions.Application.Commands;

public record EvaluateMeritCommand(Guid ProgramId, int AcademicYear) : IRequest<Result<int>>;
