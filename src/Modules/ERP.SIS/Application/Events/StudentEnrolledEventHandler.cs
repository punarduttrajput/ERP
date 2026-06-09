using ERP.Admissions.Application.Events;
using ERP.SIS.Domain;
using ERP.SIS.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.SIS.Application.Events;

public class StudentEnrolledEventHandler : INotificationHandler<StudentEnrolledEvent>
{
    private readonly ISisDbContext _db;
    private readonly ILogger<StudentEnrolledEventHandler> _logger;

    public StudentEnrolledEventHandler(ISisDbContext db, ILogger<StudentEnrolledEventHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(StudentEnrolledEvent notification, CancellationToken cancellationToken)
    {
        if (notification.TenantId == Guid.Empty)
        {
            _logger.LogWarning("StudentEnrolledEvent received with empty TenantId for ApplicationId {ApplicationId}. Skipping.", notification.ApplicationId);
            return;
        }

        var sequence = await _db.Students
            .CountAsync(s => s.ProgramId == notification.ProgramId && s.AcademicYear == notification.AcademicYear, cancellationToken) + 1;

        var programPrefix = notification.ProgramId.ToString()[..4].ToUpper();
        var studentNumber = $"{notification.AcademicYear}-{programPrefix}-{sequence:D5}";

        var nameParts = notification.ApplicantName.Trim().Split(' ', 2);
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

        var student = new Student
        {
            TenantId = notification.TenantId,
            ApplicationId = notification.ApplicationId,
            StudentNumber = studentNumber,
            ProgramId = notification.ProgramId,
            ProgramName = notification.ProgramName,
            AcademicYear = notification.AcademicYear,
            EnrolledAt = DateTime.UtcNow,
            FirstName = firstName,
            LastName = lastName,
            Email = notification.ApplicantEmail,
            MobileNumber = notification.ApplicantMobile,
            DateOfBirth = DateOnly.MinValue,
            Gender = string.Empty,
            Category = string.Empty,
            Semester = 1,
            IsActive = true
        };

        _db.Students.Add(student);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created student profile {StudentNumber} for ApplicationId {ApplicationId}", studentNumber, notification.ApplicationId);
    }
}
