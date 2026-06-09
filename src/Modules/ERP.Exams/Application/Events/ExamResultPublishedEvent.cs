using MediatR;

namespace ERP.Exams.Application.Events;

public record ExamResultPublishedEvent(
    Guid TenantId,
    Guid SemesterId,
    int StudentCount) : INotification;
