using MediatR;

namespace ERP.HRMS.Application.Events;

public record EmployeeJoinedEvent(
    Guid TenantId,
    Guid EmployeeId,
    string Email,
    string FirstName,
    string LastName,
    string? MobileNumber
) : INotification;
