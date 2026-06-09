using MediatR;

namespace ERP.Admissions.Application.Events;

// Published when AcceptOfferHandler transitions application to Enrolled.
// SIS module will handle this in v2.0 to create the student profile.
public record StudentEnrolledEvent(
    Guid ApplicationId,
    Guid TenantId,
    string ApplicantName,
    string ApplicantEmail,
    string ApplicantMobile,
    Guid ProgramId,
    string ProgramName,
    int AcademicYear
) : INotification;
