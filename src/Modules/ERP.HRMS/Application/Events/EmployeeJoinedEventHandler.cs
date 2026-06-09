using ERP.HRMS.Infrastructure;
using ERP.Users.Application.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Events;

public class EmployeeJoinedEventHandler : INotificationHandler<EmployeeJoinedEvent>
{
    private readonly IMediator _mediator;
    private readonly IHrmsDbContext _db;

    public EmployeeJoinedEventHandler(IMediator mediator, IHrmsDbContext db)
    {
        _mediator = mediator;
        _db = db;
    }

    public async Task Handle(EmployeeJoinedEvent notification, CancellationToken cancellationToken)
    {
        // Temp password — employee must change on first login
        var tempPassword = Guid.NewGuid().ToString("N")[..12];

        var result = await _mediator.Send(new CreateUserCommand(
            notification.Email,
            tempPassword,
            notification.FirstName,
            notification.LastName,
            notification.MobileNumber,
            null,
            null
        ), cancellationToken);

        if (result.IsSuccess)
        {
            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.Id == notification.EmployeeId, cancellationToken);

            if (employee is not null)
            {
                employee.UserId = result.Value;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
