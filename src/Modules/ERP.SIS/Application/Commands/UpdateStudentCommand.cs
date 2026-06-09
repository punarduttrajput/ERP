using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.SIS.Application.Commands;

public record UpdateStudentCommand(
    Guid StudentId,
    string FirstName,
    string LastName,
    string? MiddleName,
    string MobileNumber,
    DateOnly DateOfBirth,
    string Gender,
    string? BloodGroup,
    string? PermanentAddress,
    string? CurrentAddress,
    string Category
) : IRequest<Result>;
